namespace ArenaForge.Controllers;

using Microsoft.AspNetCore.Mvc;
using ArenaForge.Data;
using ArenaForge.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

[Route("api/battle")]
[ApiController]
public class BattleController : ControllerBase
{
    private readonly ArenaDbContext _context;

    public BattleController(ArenaDbContext context)
    {
        _context = context;
    }

    [HttpPost("start")]
    public async Task<IActionResult> StartBattle(int participantId, int opponentId)
    {
        var currentSeason = await _context.Seasons
            .OrderByDescending(s => s.StartTime)
            .FirstOrDefaultAsync(s => s.StartTime <= DateTime.UtcNow && s.EndTime >= DateTime.UtcNow);

        if (currentSeason == null)
        {
            return BadRequest("No active season available for battles.");
        }

        var availableOpponent = await _context.AvailableOpponents
            .FirstOrDefaultAsync(ao => ao.ParticipantId == participantId && ao.OpponentId == opponentId && ao.SeasonId == currentSeason.Id);

        if (availableOpponent == null)
        {
            return BadRequest("Opponent is not available for battle.");
        }

        var participantEntry = await _context.LeaderboardEntries
            .FirstOrDefaultAsync(le => le.ParticipantId == participantId && le.SeasonId == currentSeason.Id);
        var opponentEntry = await _context.LeaderboardEntries
            .FirstOrDefaultAsync(le => le.ParticipantId == opponentId && le.SeasonId == currentSeason.Id);

        if (participantEntry == null || opponentEntry == null)
        {
            return NotFound("Participant or opponent not found in leaderboard.");
        }

        var random = new Random();
        bool participantWins = random.NextDouble() < 0.7;

        int participantScoreChange = participantWins ? 30 : -20;
        int opponentScoreChange = participantWins ? -20 : 30;

        participantEntry.TotalScore += participantScoreChange;
        opponentEntry.TotalScore += opponentScoreChange;

        var battle = new Battle
        {
            ParticipantId = participantId,
            OpponentId = opponentId,
            SeasonId = currentSeason.Id,
            BattleTime = DateTime.UtcNow,
            IsVictory = participantWins,
            ParticipantScoreChange = participantScoreChange,
            OpponentScoreChange = opponentScoreChange
        };

        _context.Battles.Add(battle);
        await _context.SaveChangesAsync();

        return Ok(new { Result = participantWins ? "Victory" : "Defeat", ScoreChange = participantScoreChange });
    }
}
