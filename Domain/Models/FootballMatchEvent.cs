namespace Domain.Models;

public sealed class FootballMatchEvent
{
    public string timestamp { get; set; }
    public int time_seconds { get; set; }
    public int minute { get; set; }
    public int second { get; set; }
    public string team { get; set; }
    public string player { get; set; }
    public string action { get; set; }
    public float[] position { get; set; }
    public string outcome { get; set; }
    public string height { get; set; }
    public string card { get; set; }
    public float[] pass_target { get; set; }
    public float[] shot_target { get; set; }
    public string body_part { get; set; }
    public string event_type { get; set; }
    public string type { get; set; }
    public int event_index { get; set; }
    public string match_id { get; set; }
    public string? home_team { get; set; }
    public string? away_team { get; set; }
    public bool? long_pass { get; set; }
    public decimal? pass_length { get; set; }
    public Score? Score { get; set; }
} 
public abstract class Score
{
    public int Home { get; set; }
    public int Away { get; set; }
}