using CarWashSelfService.API.Data;
using CarWashSelfService.API.DTOs;
using CarWashSelfService.API.Entities;
using CarWashSelfService.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarWashSelfService.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class WashSessionsController(ApplicationDbContext db) : ControllerBase
{
    // 1) GET /api/washsessions — все сеансы, свежие сверху
    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<ActionResult<List<SessionDto>>> GetAll()
    {
        var list = await db.WashSessions.AsNoTracking()
            .OrderByDescending(s => s.StartTime)
            .Select(s => new SessionDto(
                s.Id, s.UserId, s.User!.Username, s.WashPostId, s.WashPost!.Name,
                s.TariffId, s.Tariff!.Name, s.StartTime, s.EndTime, s.Status, s.TotalAmount))
            .ToListAsync();
        return Ok(list);
    }

    // 2) GET /api/washsessions/5
    [HttpGet("{id:int}")]
    public async Task<ActionResult<SessionDto>> GetById(int id)
    {
        var s = await db.WashSessions.AsNoTracking()
            .Include(x => x.User).Include(x => x.WashPost).Include(x => x.Tariff)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (s is null) return NotFound(new { message = $"Session {id} not found" });
        if (!User.IsAdmin() && s.UserId != User.CurrentUserId()) return Forbid();
        return Ok(Map(s));
    }

    // 3) GET /api/washsessions/user/1 — история клиента
    [HttpGet("user/{userId:int}")]
    public async Task<ActionResult<List<SessionDto>>> GetByUser(int userId)
    {
        if (!await db.Users.AnyAsync(u => u.Id == userId))
            return NotFound(new { message = $"User {userId} not found" });

        // ← НОВАЯ СТРОКА: чужую историю видит только админ
        if (!User.IsAdmin() && userId != User.CurrentUserId()) return Forbid();

        var list = await db.WashSessions.AsNoTracking()
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.StartTime)
            .Select(s => new SessionDto(
                s.Id, s.UserId, s.User!.Username, s.WashPostId, s.WashPost!.Name,
                s.TariffId, s.Tariff!.Name, s.StartTime, s.EndTime, s.Status, s.TotalAmount))
            .ToListAsync();
        return Ok(list);
    }

    // 4) POST /api/washsessions — старт сеанса: занять пост
    [HttpPost]
    public async Task<ActionResult<SessionDto>> Create([FromBody] SessionCreateDto dto)
    {
        // ← НОВАЯ СТРОКА: создать сеанс можно только себе (админу — любому)
        if (!User.IsAdmin() && dto.UserId != User.CurrentUserId()) return Forbid();

        var post = await db.WashPosts.FindAsync(dto.WashPostId);
        if (post is null) return NotFound(new { message = $"WashPost {dto.WashPostId} not found" });
        if (post.Status != PostStatus.Free)
            return Conflict(new { message = $"Post {post.Id} is not free (status: {post.Status})" });

        var tariff = await db.Tariffs.FindAsync(dto.TariffId);
        if (tariff is null) return NotFound(new { message = $"Tariff {dto.TariffId} not found" });
        if (!tariff.IsActive)
            return Conflict(new { message = "Tariff is disabled" });

        var user = await db.Users.FindAsync(dto.UserId);
        if (user is null) return NotFound(new { message = $"User {dto.UserId} not found" });

        var session = new WashSession
        {
            UserId = user.Id,
            WashPostId = post.Id,
            TariffId = tariff.Id,
            Status = SessionStatus.Active,
            StartTime = DateTime.UtcNow,
            TotalAmount = tariff.Price   // цена фиксируется в момент старта
        };
        post.Status = PostStatus.Occupied;   // атомарно с созданием сеанса

        db.WashSessions.Add(session);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = session.Id },
            new SessionDto(session.Id, session.UserId, user.Username, session.WashPostId,
                post.Name, session.TariffId, tariff.Name, session.StartTime,
                session.EndTime, session.Status, session.TotalAmount));
    }

    // 5) POST /api/washsessions/5/complete — завершить и освободить пост
    [HttpPost("{id:int}/complete")]
    public async Task<ActionResult<SessionDto>> Complete(int id)
    {
        var session = await db.WashSessions
            .Include(s => s.User).Include(s => s.WashPost).Include(s => s.Tariff)
            .FirstOrDefaultAsync(s => s.Id == id);
        if (session is null) return NotFound(new { message = $"Session {id} not found" });
        if (!User.IsAdmin() && session.UserId != User.CurrentUserId()) return Forbid();

        if (session.Status != SessionStatus.Active)
            return Conflict(new { message = $"Session is {session.Status}, only Active can be completed" });

        session.Status = SessionStatus.Completed;
        session.EndTime = DateTime.UtcNow;
        if (session.WashPost is not null) session.WashPost.Status = PostStatus.Free;

        await db.SaveChangesAsync();
        return Ok(Map(session));
    }

    // 6) POST /api/washsessions/5/cancel — отменить и освободить пост
    [HttpPost("{id:int}/cancel")]
    public async Task<ActionResult<SessionDto>> Cancel(int id)
    {
        var session = await db.WashSessions
            .Include(s => s.User).Include(s => s.WashPost).Include(s => s.Tariff)
            .FirstOrDefaultAsync(s => s.Id == id);
        if (session is null) return NotFound(new { message = $"Session {id} not found" });
        if (!User.IsAdmin() && session.UserId != User.CurrentUserId()) return Forbid();
        if (session.Status != SessionStatus.Active)
            return Conflict(new { message = $"Session is {session.Status}, only Active can be cancelled" });

        session.Status = SessionStatus.Cancelled;
        session.EndTime = DateTime.UtcNow;
        if (session.WashPost is not null) session.WashPost.Status = PostStatus.Free;

        await db.SaveChangesAsync();
        return Ok(Map(session));
    }

    private static SessionDto Map(WashSession s) => new(
        s.Id, s.UserId, s.User!.Username, s.WashPostId, s.WashPost!.Name,
        s.TariffId, s.Tariff!.Name, s.StartTime, s.EndTime, s.Status, s.TotalAmount);
}