using CarWashSelfService.API.Data;
using CarWashSelfService.API.DTOs;
using CarWashSelfService.API.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarWashSelfService.API.Controllers;

[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/stats")]
public class StatsController(ApplicationDbContext db) : ControllerBase
{
    // GET /api/stats/overview
    [HttpGet("overview")]
    public async Task<ActionResult<OverviewDto>> GetOverview()
    {
        var overview = new OverviewDto(
            Users: await db.Users.CountAsync(),
            WashPosts: await db.WashPosts.CountAsync(),
            FreePosts: await db.WashPosts.CountAsync(p => p.Status == PostStatus.Free),
            ActiveSessions: await db.WashSessions.CountAsync(s => s.Status == SessionStatus.Active),
            CompletedSessions: await db.WashSessions.CountAsync(s => s.Status == SessionStatus.Completed),
            SucceededPayments: await db.PaymentTransactions.CountAsync(p => p.Status == PaymentStatus.Succeeded),
            Revenue: await db.PaymentTransactions
                .Where(p => p.Status == PaymentStatus.Succeeded)
                .SumAsync(p => p.Amount));
        return Ok(overview);
    }

    // GET /api/stats/revenue-by-day?days=7
    [HttpGet("revenue-by-day")]
    public async Task<ActionResult<List<RevenueDayDto>>> GetRevenueByDay([FromQuery] int days = 7)
    {
        var from = DateTime.UtcNow.Date.AddDays(-days);

        var list = await db.PaymentTransactions.AsNoTracking()
            .Where(p => p.Status == PaymentStatus.Succeeded && p.PaidAt >= from)
            .GroupBy(p => p.PaidAt!.Value.Date)
            .Select(g => new RevenueDayDto(g.Key, g.Sum(x => x.Amount)))
            .OrderByDescending(d => d.Date)
            .ToListAsync();
        return Ok(list);
    }
}