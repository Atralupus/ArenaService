namespace ArenaForge.Models;

public class Participant
{
    public int Id { get; set; }
    public string AvatarAddress { get; set; }
    public string Nickname { get; set; }
    public ICollection<Battle> Battles { get; set; }
    public ICollection<LeaderboardEntry> LeaderboardEntries { get; set; }
}