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
public class TariffsController(ApplicationDbContext db) : ControllerBase
{
    // 1) GET /api/tariffs?onlyActive=true
    [HttpGet]
    public async Task<ActionResult<List<TariffDto>>> GetAll([FromQuery] bool onlyActive = false)
    {
        var query = db.Tariffs.AsNoTracking().AsQueryable();
        if (onlyActive)
            query = query.Where(t => t.IsActive);

        var list = await query
            .OrderBy(t => t.Price)
            .Select(t => new TariffDto(t.Id, t.Name, t.Description, t.Price, t.DurationMinutes, t.IsActive))
            .ToListAsync();

        return Ok(list);
    }

    // 2) GET /api/tariffs/5
    [HttpGet("{id:int}")]
    public async Task<ActionResult<TariffDto>> GetById(int id)
    {
        var t = await db.Tariffs.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (t is null)
            return NotFound(new { message = $"Tariff {id} not found" });

        return Ok(new TariffDto(t.Id, t.Name, t.Description, t.Price, t.DurationMinutes, t.IsActive));
    }

    // 3) POST /api/tariffs
    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<TariffDto>> Create([FromBody] TariffCreateDto dto)
    {
        var tariff = new Tariff
        {
            Name = dto.Name,
            Description = dto.Description,
            Price = dto.Price,
            DurationMinutes = dto.DurationMinutes,
            IsActive = dto.IsActive
        };

        db.Tariffs.Add(tariff);
        await db.SaveChangesAsync();

        var result = new TariffDto(tariff.Id, tariff.Name, tariff.Description,
            tariff.Price, tariff.DurationMinutes, tariff.IsActive);

        return CreatedAtAction(nameof(GetById), new { id = tariff.Id }, result);
    }

    // 4) PUT /api/tariffs/5
    [Authorize(Roles = "Admin")]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] TariffUpdateDto dto)
    {
        var tariff = await db.Tariffs.FindAsync(id);
        if (tariff is null)
            return NotFound(new { message = $"Tariff {id} not found" });

        tariff.Name = dto.Name;
        tariff.Description = dto.Description;
        tariff.Price = dto.Price;
        tariff.DurationMinutes = dto.DurationMinutes;
        tariff.IsActive = dto.IsActive;

        await db.SaveChangesAsync();
        return NoContent();
    }

    // 5) DELETE /api/tariffs/5
    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var tariff = await db.Tariffs.FindAsync(id);
        if (tariff is null)
            return NotFound(new { message = $"Tariff {id} not found" });

        // Не даём удалить тариф, на который уже ссылаются сеансы
        var inUse = await db.WashSessions.AnyAsync(s => s.TariffId == id);
        if (inUse)
            return Conflict(new { message = "Tariff is used by wash sessions" });

        db.Tariffs.Remove(tariff);
        await db.SaveChangesAsync();
        return NoContent();
    }
}