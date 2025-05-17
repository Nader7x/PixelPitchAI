namespace Domain.Models;

public class FootballMatchEvent
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
    public int event_index { get; set; }
    public int match_id { get; set; }
}