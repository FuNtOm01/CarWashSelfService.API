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
public class WashPostsController(ApplicationDbContext db) : ControllerBase
{
    // GET /api/washposts
    [HttpGet]
    public async Task<ActionResult<List<WashPostDto>>> GetAll()
    {
        var list = await db.WashPosts.AsNoTracking()
            .OrderBy(p => p.Id)
            .Select(p => new WashPostDto(p.Id, p.Name, p.Address, p.Status))
            .ToListAsync();
        return Ok(list);
    }

    // GET /api/washposts/free — только свободные
    [HttpGet("free")]
    public async Task<ActionResult<List<WashPostDto>>> GetFree()
    {
        var list = await db.WashPosts.AsNoTracking()
            .Where(p => p.Status == PostStatus.Free)
            .OrderBy(p => p.Id)
            .Select(p => new WashPostDto(p.Id, p.Name, p.Address, p.Status))
            .ToListAsync();
        return Ok(list);
    }

    // GET /api/washposts/{id}
    [HttpGet("{id:int}")]
    public async Task<ActionResult<WashPostDto>> GetById(int id)
    {
        var p = await db.WashPosts.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (p is null) return NotFound(new { message = $"WashPost {id} not found" });
        return Ok(new WashPostDto(p.Id, p.Name, p.Address, p.Status));
    }

    // POST /api/washposts
    [HttpPost]
    public async Task<ActionResult<WashPostDto>> Create([FromBody] WashPostCreateDto dto)
    {
        var post = new WashPost { Name = dto.Name, Address = dto.Address, Status = PostStatus.Free };
        db.WashPosts.Add(post);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = post.Id },
            new WashPostDto(post.Id, post.Name, post.Address, post.Status));
    }

    // PUT /api/washposts/{id}
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] WashPostUpdateDto dto)
    {
        var post = await db.WashPosts.FindAsync(id);
        if (post is null) return NotFound(new { message = $"WashPost {id} not found" });

        post.Name = dto.Name;
        post.Address = dto.Address;
        post.Status = dto.Status;

        await db.SaveChangesAsync();
        return NoContent();
    }

    // DELETE /api/washposts/{id}
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var post = await db.WashPosts.FindAsync(id);
        if (post is null) return NotFound(new { message = $"WashPost {id} not found" });

        var hasActive = await db.WashSessions.AnyAsync(s => s.WashPostId == id && s.Status == SessionStatus.Active);
        if (hasActive) return Conflict(new { message = "Post has an active session" });

        db.WashPosts.Remove(post);
        await db.SaveChangesAsync();
        return NoContent();
    }
}