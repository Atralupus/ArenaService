namespace ArenaForge.Models;

public class Season
{
    public int Id { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public int TicketRefillInterval { get; set; }
    public ICollection<Battle> Battles { get; set; }
    public ICollection<LeaderboardEntry> LeaderboardEntries { get; set; }
}