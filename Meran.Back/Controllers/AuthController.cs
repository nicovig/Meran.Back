using System.Security.Cryptography;
using System.Text;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
        private readonly ApplicationDbContext _dbContext;
        private readonly ITokenService _tokenService;

        public AuthController(ApplicationDbContext dbContext, ITokenService tokenService)
        {
            _dbContext = dbContext;
            _tokenService = tokenService;
        }

        [HttpPost("register")]
        public async Task<ActionResult<AuthResponseDto>> Register(RegisterRequestDto request, CancellationToken cancellationToken)
        {
            var email = request.Email.Trim().ToLowerInvariant();

            var exists = await _dbContext.Administrators.AnyAsync(x => x.Email == email, cancellationToken);
            if (exists)
            {
                return Conflict(new { message = "Email already in use" });
            }

            var user = new Administrator
            {
                Id = Guid.NewGuid(),
                Email = email,
                PasswordHash = HashPassword(request.Password),
                DisplayName = request.DisplayName ?? email,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.Administrators.Add(user);
            await _dbContext.SaveChangesAsync(cancellationToken);

            var token = _tokenService.GenerateAccessToken(user);
            var expiresAt = _tokenService.GetAccessTokenExpirationUtc();

            var response = new AuthResponseDto
            {
                AccessToken = token,
                ExpiresAtUtc = expiresAt,
                User = ToUserDto(user)
            };

            return CreatedAtAction(nameof(GetMe), new { }, response);
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

            if (!VerifyPassword(request.Password, user.PasswordHash))
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

        [Authorize]
        [HttpGet("me")]
        public async Task<ActionResult<UserDto>> GetMe(CancellationToken cancellationToken)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ??
                              User.FindFirstValue("sub");

            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized();
            }

            var user = await _dbContext.Administrators
                .AsNoTracking()
                .Where(x => x.Id == userId)
                .SingleOrDefaultAsync(cancellationToken);

            if (user == null)
            {
                return NotFound();
            }

            return Ok(ToUserDto(user));
        }

        private static string HashPassword(string password)
        {
            var salt = "static_salt_for_now";
            var bytes = Encoding.UTF8.GetBytes(password + salt);

            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

        private static bool VerifyPassword(string password, string passwordHash)
        {
            var computed = HashPassword(password);
            return passwordHash == computed;
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

