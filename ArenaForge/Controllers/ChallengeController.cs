namespace ArenaForge.Controllers;

using Microsoft.AspNetCore.Mvc;
using ArenaForge.Data;
using ArenaForge.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

[Route("api/challenge")]
[ApiController]
public class ChallengeController : ControllerBase
{
    private readonly ArenaDbContext _context;
    private static readonly Dictionary<string, (double Min, double Max, int WinScore, int LoseScore)> GroupSettings = new()
    {
        { "Upper1", (0.2, 0.4, 24, -1) },
        { "Upper2", (0.4, 0.8, 22, -2) },
        { "Equal", (0.8, 1.2, 20, -3) },
        { "Lower1", (1.2, 1.8, 18, -4) },
        { "Lower2", (1.8, 3.0, 16, -5) }
    };

    public ChallengeController(ArenaDbContext context)
    {
        _context = context;
    }

    [HttpGet("available-opponents")]
    public async Task<IActionResult> GetAvailableOpponents(int participantId)
    {
        var currentSeason = await _context.Seasons
            .OrderByDescending(s => s.StartTime)
            .FirstOrDefaultAsync(s => s.StartTime <= DateTime.UtcNow && s.EndTime >= DateTime.UtcNow);

        if (currentSeason == null)
        {
            return NotFound("No active season found.");
        }

        var availableOpponents = await _context.AvailableOpponents
            .Where(ao => ao.ParticipantId == participantId && ao.SeasonId == currentSeason.Id)
            .Include(ao => ao.Opponent)
            .Select(ao => new
            {
                ao.Opponent.Id,
                ao.Opponent.Nickname
            })
            .ToListAsync();

        if (!availableOpponents.Any())
        {
            return NotFound("No available opponents found.");
        }

        return Ok(availableOpponents);
    }

    [HttpPost("generate-opponents")]
    public async Task<IActionResult> GenerateOpponents(int participantId)
    {
        var currentSeason = await _context.Seasons
            .OrderByDescending(s => s.StartTime)
            .FirstOrDefaultAsync(s => s.StartTime <= DateTime.UtcNow && s.EndTime >= DateTime.UtcNow);

        if (currentSeason == null)
        {
            return BadRequest("No active season available.");
        }

        var participantEntry = await _context.LeaderboardEntries
            .FirstOrDefaultAsync(le => le.ParticipantId == participantId && le.SeasonId == currentSeason.Id);

        if (participantEntry == null)
        {
            return NotFound("Participant not found in current season.");
        }

        var totalParticipants = await _context.LeaderboardEntries.CountAsync(le => le.SeasonId == currentSeason.Id);
        var opponents = GenerateOpponentList(participantEntry.Rank, totalParticipants, participantId, currentSeason.Id);

        // Clear existing opponents
        var existingOpponents = _context.AvailableOpponents
            .Where(ao => ao.ParticipantId == participantId && ao.SeasonId == currentSeason.Id);
        _context.AvailableOpponents.RemoveRange(existingOpponents);

        // Add new opponents
        _context.AvailableOpponents.AddRange(opponents);
        await _context.SaveChangesAsync();

        return Ok(opponents.Select(o => new { o.OpponentId, o.Opponent.Nickname }));
    }

    private List<AvailableOpponent> GenerateOpponentList(int participantRank, int totalParticipants, int participantId, int seasonId)
    {
        var opponents = new List<AvailableOpponent>();
        var random = new Random();

        foreach (var group in GroupSettings)
        {
            var (min, max, _, _) = group.Value;
            var minRank = (int)(participantRank * min);
            var maxRank = (int)(participantRank * max);

            var potentialOpponents = _context.LeaderboardEntries
                .Where(le => le.Rank >= minRank && le.Rank <= maxRank && le.Rank != participantRank)
                .Select(le => le.Participant)
                .ToList(); // Fetch data from the database

            if (potentialOpponents.Count > 0)
            {
                var selectedOpponent = potentialOpponents.OrderBy(_ => random.Next()).FirstOrDefault(); // Randomize in memory
                if (selectedOpponent != null)
                {
                    opponents.Add(new AvailableOpponent
                    {
                        ParticipantId = participantId,
                        OpponentId = selectedOpponent.Id,
                        SeasonId = seasonId,
                        RefillTime = DateTime.UtcNow
                    });
                }
            }
        }

        // Fill missing opponents from lower groups if necessary
        while (opponents.Count < 5)
        {
            var lowerOpponents = _context.LeaderboardEntries
                .Where(le => le.Rank > participantRank)
                .Select(le => le.Participant)
                .ToList(); // Fetch data from the database

            if (lowerOpponents.Count > 0)
            {
                var selectedOpponent = lowerOpponents.OrderBy(_ => random.Next()).FirstOrDefault(); // Randomize in memory
                if (selectedOpponent != null)
                {
                    opponents.Add(new AvailableOpponent
                    {
                        ParticipantId = participantId,
                        OpponentId = selectedOpponent.Id,
                        SeasonId = seasonId,
                        RefillTime = DateTime.UtcNow
                    });
                }
            }
            else
            {
                break;
            }
        }

        return opponents;
    }
} 