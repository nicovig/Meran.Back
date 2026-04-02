using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Meran.Back.Data;
using Meran.Back.DTO;
using Meran.Back.Models;
using Meran.Back.Services;

namespace Meran.Back.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private static readonly JsonSerializerOptions TokenRequestJsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly ApplicationDbContext _dbContext;
        private readonly ITokenService _tokenService;
        private readonly IPasswordService _passwordService;
        private readonly MachineClientOptions _machineClientOptions;

        public AuthController(
            ApplicationDbContext dbContext,
            ITokenService tokenService,
            IPasswordService passwordService,
            IOptions<MachineClientOptions> machineClientOptions)
        {
            _dbContext = dbContext;
            _tokenService = tokenService;
            _passwordService = passwordService;
            _machineClientOptions = machineClientOptions.Value;
        }

        [HttpPost("login")]
        public async Task<ActionResult<AuthResponseDto>> Login(LoginRequestDto request, CancellationToken cancellationToken)
        {
            var email = request.Email.Trim().ToLowerInvariant();

            var user = await _dbContext.Administrators.SingleOrDefaultAsync(x => x.Email == email, cancellationToken);
            if (user == null)
            {
                return Unauthorized(new { message = "Invalid credentials" });
            }

            if (!_passwordService.VerifyPassword(request.Password, user.PasswordHash, user.Salt))
            {
                return Unauthorized(new { message = "Invalid credentials" });
            }

            if (!user.IsActive)
            {
                return Forbid();
            }

            var token = _tokenService.GenerateAccessToken(user);
            var expiresAt = _tokenService.GetAccessTokenExpirationUtc();

            var response = new AuthResponseDto
            {
                AccessToken = token,
                ExpiresAtUtc = expiresAt,
                User = ToUserDto(user)
            };

            return Ok(response);
        }

        [HttpPost("token")]
        [AllowAnonymous]
        public async Task<IActionResult> Token(CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(_machineClientOptions.ClientSecret))
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = "machine_to_machine_client_not_configured" });
            }

            Request.EnableBuffering();
            ClientCredentialsTokenRequestDto? dto;
            if (Request.HasFormContentType)
            {
                var form = await Request.ReadFormAsync(cancellationToken);
                dto = new ClientCredentialsTokenRequestDto
                {
                    grant_type = form["grant_type"].ToString(),
                    client_id = form["client_id"].ToString(),
                    client_secret = form["client_secret"].ToString()
                };
            }
            else
            {
                Request.Body.Position = 0;
                dto = await JsonSerializer.DeserializeAsync<ClientCredentialsTokenRequestDto>(Request.Body, TokenRequestJsonOptions, cancellationToken);
            }

            if (dto == null || string.IsNullOrWhiteSpace(dto.grant_type))
            {
                return BadRequest(new { error = "invalid_request" });
            }

            if (!string.Equals(dto.grant_type, "client_credentials", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { error = "unsupported_grant_type" });
            }

            if (!string.Equals(dto.client_id, _machineClientOptions.ClientId, StringComparison.Ordinal)
                || !SecureEquals(dto.client_secret, _machineClientOptions.ClientSecret))
            {
                return Unauthorized(new { error = "invalid_client" });
            }

            var expiresMinutes = _machineClientOptions.AccessTokenExpiresMinutes;
            var accessToken = _tokenService.GenerateMachineAccessToken(_machineClientOptions.ClientId, _machineClientOptions.Role, expiresMinutes);

            var response = new ClientCredentialsTokenResponseDto
            {
                access_token = accessToken,
                token_type = "Bearer",
                expires_in = expiresMinutes * 60
            };

            return Ok(response);
        }

        private static bool SecureEquals(string? a, string? b)
        {
            if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b))
            {
                return false;
            }

            var ha = SHA256.HashData(Encoding.UTF8.GetBytes(a));
            var hb = SHA256.HashData(Encoding.UTF8.GetBytes(b));
            return CryptographicOperations.FixedTimeEquals(ha, hb);
        }

        private static UserDto ToUserDto(Administrator user)
        {
            return new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                DisplayName = user.DisplayName,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt
            };
        }
    }
}

