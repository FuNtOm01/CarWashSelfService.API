using System.Security.Claims;
using CarWashSelfService.API.Data;
using CarWashSelfService.API.DTOs;
using CarWashSelfService.API.Entities;
using CarWashSelfService.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarWashSelfService.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(ApplicationDbContext db, TokenService tokens) : ControllerBase
{
    // POST /api/auth/register — создание аккаунта + сразу токен
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponseDto>> Register(RegisterDto dto)
    {
        if (dto.Password.Length < 6)
            return BadRequest(new { message = "Password must be at least 6 characters" });
        if (await db.Users.AnyAsync(u => u.Email == dto.Email))
            return Conflict(new { message = "Email already registered" });

        var user = new User
        {
            Username = dto.Username,
            Email = dto.Email,
            PhoneNumber = dto.PhoneNumber,
            PasswordHash = PasswordHasher.Hash(dto.Password),
            Role = UserRole.Client,
            CreatedAt = DateTime.UtcNow
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        return Ok(new AuthResponseDto(tokens.CreateToken(user), user.Id, user.Username, user.Role.ToString()));
    }

    // POST /api/auth/login — проверка пароля + выдача токена
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login(LoginDto dto)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
        if (user is null || !PasswordHasher.Verify(dto.Password, user.PasswordHash))
            return Unauthorized(new { message = "Invalid email or password" });

        return Ok(new AuthResponseDto(tokens.CreateToken(user), user.Id, user.Username, user.Role.ToString()));
    }

    // GET /api/auth/me — «кто я» по токену (первый эндпоинт с [Authorize])
    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<UserDto>> Me()
    {
        var id = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var u = await db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (u is null) return NotFound();
        return Ok(new UserDto(u.Id, u.Username, u.Email, u.PhoneNumber, u.Balance, u.CreatedAt));
    }
}