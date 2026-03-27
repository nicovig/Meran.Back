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
        private readonly IPasswordService _passwordService;

        public AuthController(ApplicationDbContext dbContext, ITokenService tokenService, IPasswordService passwordService)
        {
            _dbContext = dbContext;
            _tokenService = tokenService;
            _passwordService = passwordService;
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

