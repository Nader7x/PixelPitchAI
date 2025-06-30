namespace Footex.Configuration;

public class SimulationServiceOptions
{
    public string BaseUrl { get; set; } = "http://localhost:8000";
    public string ApiKey { get; set; } = string.Empty;
}