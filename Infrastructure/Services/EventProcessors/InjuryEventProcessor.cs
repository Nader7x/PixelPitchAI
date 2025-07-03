using Domain.Models;

namespace Infrastructure.Services.EventProcessors;

public class InjuryEventProcessor : BaseEventProcessor
{
    public override bool CanProcess(FootballMatchEvent matchEvent)
    {
        return matchEvent.action == "injury stoppage";
    }

    public override void ProcessMatchEvent(FootballMatchEvent matchEvent, Match match)
    {
        // Injuries don't have a specific field in the Match model
        // They're tracked in the MatchEvents entity
    }

    public override void ProcessEventCounters(
        FootballMatchEvent matchEvent,
        MatchEvents matchEvents,
        Match match
    )
    {
        matchEvents.TotalInjuries++;
    }
}
