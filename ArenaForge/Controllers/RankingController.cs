namespace ArenaForge.Controllers;

using Microsoft.AspNetCore.Mvc;
using ArenaForge.Data;
using ArenaForge.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

[Route("api/ranking")]
[ApiController]
public class RankingController : ControllerBase
{
    private readonly ArenaDbContext _context;

    public RankingController(ArenaDbContext context)
    {
        _context = context;
    }

    [HttpGet("list")]
    public async Task<IActionResult> GetRankings(int pageNumber = 1, int pageSize = 10)
    {
        var currentSeason = await _context.Seasons
            .OrderByDescending(s => s.StartTime)
            .FirstOrDefaultAsync(s => s.StartTime <= DateTime.UtcNow && s.EndTime >= DateTime.UtcNow);

        if (currentSeason == null)
        {
            return NotFound("No active season found.");
        }

        var totalRankings = await _context.LeaderboardEntries.CountAsync(le => le.SeasonId == currentSeason.Id);
        var rankings = await _context.LeaderboardEntries
            .Where(le => le.SeasonId == currentSeason.Id)
            .OrderByDescending(le => le.TotalScore)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Include(le => le.Participant)
            .Select(le => new
            {
                le.Rank,
                le.Participant.Nickname,
                le.TotalScore
            })
            .ToListAsync();

        var result = new
        {
            TotalRankings = totalRankings,
            PageNumber = pageNumber,
            PageSize = pageSize,
            Rankings = rankings
        };

        return Ok(result);
    }
}
