using Domain.Models;

namespace Infrastructure.Services.EventProcessors;

public class BallLossEventProcessor : BaseEventProcessor
{
    public override bool CanProcess(FootballMatchEvent matchEvent)
    {
        return matchEvent.action == "miscontrol"
            || matchEvent.action == "dispossessed"
            || matchEvent.action == "error";
    }

    public override void ProcessMatchEvent(FootballMatchEvent matchEvent, Match match)
    {
        // These events generally lead to ball possession changes
        // They don't directly update match statistics in the current data model
        // but they're tracked in the match events counters
    }

    public override void ProcessEventCounters(
        FootballMatchEvent matchEvent,
        MatchEvents matchEvents,
        Match match
    )
    {
        if (matchEvent.action == "miscontrol")
            matchEvents.TotalOuts++; // Miscontrol often leads to ball going out
        else if (matchEvent.action == "dispossessed")
            matchEvents.TotalPossessionWon++; // When a player is dispossessed, the other team wins possession
        else if (matchEvent.action == "error")
            matchEvents.TotalErrors++;

        matchEvents.TotalEvents++; // Increment the total events counter
    }
}
