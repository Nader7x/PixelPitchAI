using Domain.Models;

namespace Infrastructure.Services.EventProcessors;

public class PressureEventProcessor : BaseEventProcessor
{
    public override bool CanProcess(FootballMatchEvent matchEvent)
    {
        return matchEvent.action == "pressure";
    }

    public override void ProcessMatchEvent(FootballMatchEvent matchEvent, Match match)
    {
        // Pressure events are tracked in the match events entity only
        // The current Match model doesn't have specific fields for tracking pressure
    }

    public override void ProcessEventCounters(FootballMatchEvent matchEvent, MatchEvents matchEvents, Match match)
    {
        // Track pressure events in the counter
        matchEvents.TotalEvents++;
    }
}
