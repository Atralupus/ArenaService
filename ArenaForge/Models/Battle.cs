namespace ArenaForge.Models;

public class Battle
{
    public int Id { get; set; }
    public int ParticipantId { get; set; }
    public Participant Participant { get; set; }
    public int OpponentId { get; set; }
    public Participant Opponent { get; set; }
    public int SeasonId { get; set; }
    public Season Season { get; set; }
    public DateTime BattleTime { get; set; }
    public bool IsVictory { get; set; }
    public int ParticipantScoreChange { get; set; }
    public int OpponentScoreChange { get; set; }
}
