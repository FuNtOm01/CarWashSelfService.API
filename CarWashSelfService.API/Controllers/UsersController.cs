using CarWashSelfService.API.Data;
using CarWashSelfService.API.DTOs;
using CarWashSelfService.API.Entities;
using CarWashSelfService.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace CarWashSelfService.API.Controllers;

[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/[controller]")]
public class UsersController(ApplicationDbContext db) : ControllerBase
{
    // GET /api/users
    [HttpGet]
    public async Task<ActionResult<List<UserDto>>> GetAll()
    {
        var list = await db.Users.AsNoTracking()
            .OrderBy(u => u.Id)
            .Select(u => new UserDto(u.Id, u.Username, u.Email, u.PhoneNumber, u.Balance, u.CreatedAt))
            .ToListAsync();
        return Ok(list);
    }

    // GET /api/users/{id}
    [HttpGet("{id:int}")]
    public async Task<ActionResult<UserDto>> GetById(int id)
    {
        var u = await db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (u is null) return NotFound(new { message = $"User {id} not found" });
        return Ok(u);
    }

    // POST /api/users
    [HttpPost]
    public async Task<ActionResult<UserDto>> Create([FromBody] UserCreateDto dto)
    {
        if (await db.Users.AnyAsync(u => u.Email == dto.Email))
            return Conflict(new { message = "Email already registered" });

        var user = new User
        {
            Username = dto.Username,
            Email = dto.Email,
            PhoneNumber = dto.PhoneNumber,
            PasswordHash = PasswordHasher.Hash(dto.Password),
            CreatedAt = DateTime.UtcNow
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = user.Id },
            new UserDto(user.Id, user.Username, user.Email, user.PhoneNumber, user.Balance, user.CreatedAt));
    }

    // PUT /api/users/{id}
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UserUpdateDto dto)
    {
        var user = await db.Users.FindAsync(id);
        if (user is null) return NotFound(new { message = $"User {id} not found" });

        user.Username = dto.Username;
        user.Email = dto.Email;
        user.PhoneNumber = dto.PhoneNumber;

        await db.SaveChangesAsync();
        return NoContent();
    }

    // DELETE /api/users/{id}
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var user = await db.Users.FindAsync(id);
        if (user is null) return NotFound(new { message = $"User {id} not found" });

        var hasSessions = await db.WashSessions.AnyAsync(s => s.UserId == id);
        if (hasSessions) return Conflict(new { message = "User has wash sessions" });

        db.Users.Remove(user);
        await db.SaveChangesAsync();
        return NoContent();
    }

}