namespace ArenaForge.Models;

public class LeaderboardEntry
{
    public int Id { get; set; }
    public int ParticipantId { get; set; }
    public Participant Participant { get; set; }
    public int SeasonId { get; set; }
    public Season Season { get; set; }
    public int Rank { get; set; }
    public int TotalScore { get; set; } = 1000;
} 