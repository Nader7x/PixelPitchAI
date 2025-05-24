namespace Footex.Configuration;

public class SimulationServiceOptions
{
    public const string SectionName = "SimulationService";
    
    public string BaseUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
}
