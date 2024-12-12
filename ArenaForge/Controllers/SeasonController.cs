namespace ArenaForge.Controllers;

using Microsoft.AspNetCore.Mvc;
using ArenaForge.Data;
using ArenaForge.Models;
using Microsoft.EntityFrameworkCore;

[Route("api/seasons")]
[ApiController] 
public class SeasonController : ControllerBase
{
    private readonly ArenaDbContext _context;

    public SeasonController(ArenaDbContext context)
    {
        _context = context;
    }

    [HttpGet("current")]
    public async Task<IActionResult> GetCurrentSeason()
    {
        var currentSeason = await _context.Seasons
            .OrderByDescending(s => s.StartTime)
            .AsNoTracking()
            .Select(s => new {
                s.Id,
                s.StartTime,
                s.EndTime,
            })
            .FirstOrDefaultAsync(s => s.StartTime <= DateTime.UtcNow && s.EndTime >= DateTime.UtcNow);

        if (currentSeason == null)
        {
            return NotFound("No active season found.");
        }

        return Ok(currentSeason);
    }
}