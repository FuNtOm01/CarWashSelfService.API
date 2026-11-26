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
public class PaymentsController(ApplicationDbContext db) : ControllerBase
{
    // 1) GET /api/payments
    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<ActionResult<List<PaymentDto>>> GetAll()
    {
        var list = await db.PaymentTransactions.AsNoTracking()
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new PaymentDto(p.Id, p.UserId, p.User!.Username, p.WashSessionId,
                p.Amount, p.Status, p.Method, p.ExternalTransactionId, p.PaidAt, p.CreatedAt))
            .ToListAsync();
        return Ok(list);
    }

    // 2) GET /api/payments/5
    [HttpGet("{id:int}")]
    public async Task<ActionResult<PaymentDto>> GetById(int id)
    {
        var p = await db.PaymentTransactions.AsNoTracking()
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (p is null) return NotFound(new { message = $"Payment {id} not found" });
        if (!User.IsAdmin() && p.UserId != User.CurrentUserId()) return Forbid();
        return Ok(Map(p));
    }

    // 3) GET /api/payments/session/3
    [HttpGet("session/{sessionId:int}")]
    public async Task<ActionResult<List<PaymentDto>>> GetBySession(int sessionId)
    {
        var session = await db.WashSessions.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == sessionId);
        if (session is null)
            return NotFound(new { message = $"Session {sessionId} not found" });

        // ← НОВОЕ: платежи чужого сеанса видит только админ
        if (!User.IsAdmin() && session.UserId != User.CurrentUserId()) return Forbid();

        var list = await db.PaymentTransactions.AsNoTracking()
            .Where(p => p.WashSessionId == sessionId)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new PaymentDto(p.Id, p.UserId, p.User!.Username, p.WashSessionId,
                p.Amount, p.Status, p.Method, p.ExternalTransactionId, p.PaidAt, p.CreatedAt))
            .ToListAsync();
        return Ok(list);
    }

    // 4) POST /api/payments — создать платёж (Pending) по активному сеансу
    [HttpPost]
    public async Task<ActionResult<PaymentDto>> Create([FromBody] PaymentCreateDto dto)
    {
        var session = await db.WashSessions.FindAsync(dto.SessionId);
        if (session is null) return NotFound(new { message = $"Session {dto.SessionId} not found" });
        if (session.Status != SessionStatus.Active)
            return Conflict(new { message = $"Session is {session.Status}, payment allowed only for Active" });

        if (!User.IsAdmin() && session.UserId != User.CurrentUserId()) return Forbid();

        var amount = dto.Amount ?? session.TotalAmount;
        if (amount <= 0) return BadRequest(new { message = "Amount must be positive" });

        var payment = new PaymentTransaction
        {
            UserId = session.UserId,          // плательщик наследуется из сеанса
            WashSessionId = session.Id,
            Amount = amount,
            Status = PaymentStatus.Pending,
            Method = dto.Method,
            ExternalTransactionId = dto.ExternalTransactionId,
            CreatedAt = DateTime.UtcNow
        };

        db.PaymentTransactions.Add(payment);
        await db.Entry(payment).Reference(p => p.User).LoadAsync();   // ← вот эта строка
                return CreatedAtAction(nameof(GetById), new { id = payment.Id }, Map(payment));
    }

    // 5) POST /api/payments/5/confirm — Pending -> Succeeded
    [HttpPost("{id:int}/confirm")]
    public async Task<ActionResult<PaymentDto>> Confirm(int id)
    {
        var payment = await db.PaymentTransactions
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (payment is null) return NotFound(new { message = $"Payment {id} not found" });
        if (!User.IsAdmin() && payment.UserId != User.CurrentUserId()) return Forbid();
        if (payment.Status != PaymentStatus.Pending)
            return Conflict(new { message = $"Payment is {payment.Status}, only Pending can be confirmed" });

        // Оплата с внутреннего баланса: проверяем и списываем
        if (payment.Method == PaymentMethod.AppBalance)
        {
            if (payment.User is null) return NotFound(new { message = "User not found" });
            if (payment.User.Balance < payment.Amount)
                return Conflict(new { message = $"Insufficient balance: {payment.User.Balance} < {payment.Amount}" });
            payment.User.Balance -= payment.Amount;
        }

        payment.Status = PaymentStatus.Succeeded;
        payment.PaidAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return Ok(Map(payment));
    }

    // 6) POST /api/payments/5/refund — Succeeded -> Refunded
    [Authorize(Roles = "Admin")]
    [HttpPost("{id:int}/refund")]
    public async Task<ActionResult<PaymentDto>> Refund(int id)
    {
        var payment = await db.PaymentTransactions
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (payment is null) return NotFound(new { message = $"Payment {id} not found" });
        if (payment.Status != PaymentStatus.Succeeded)
            return Conflict(new { message = $"Payment is {payment.Status}, only Succeeded can be refunded" });

        // Возврат на внутренний баланс
        if (payment.Method == PaymentMethod.AppBalance && payment.User is not null)
            payment.User.Balance += payment.Amount;

        payment.Status = PaymentStatus.Refunded;

        await db.SaveChangesAsync();
        return Ok(Map(payment));
    }

    private static PaymentDto Map(PaymentTransaction p) => new(
        p.Id, p.UserId, p.User!.Username, p.WashSessionId, p.Amount,
        p.Status, p.Method, p.ExternalTransactionId, p.PaidAt, p.CreatedAt);
}