using Domain.Models;

namespace Infrastructure.Services.EventProcessors;

public class BlockEventProcessor : BaseEventProcessor
{
    public override bool CanProcess(FootballMatchEvent matchEvent)
    {
        return matchEvent.action == "block" || 
               (matchEvent.action == "shot" && matchEvent.outcome == "Blocked");
    }

    public override void ProcessMatchEvent(FootballMatchEvent matchEvent, Match match)
    {
        // Blocks don't have a specific field in the Match model
        // They're tracked in the MatchEvents entity
    }

    public override void ProcessEventCounters(FootballMatchEvent matchEvent, MatchEvents matchEvents, Match match)
    {
        matchEvents.TotalBlocks++;
    }
}
