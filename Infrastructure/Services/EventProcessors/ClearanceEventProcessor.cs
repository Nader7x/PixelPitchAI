using Domain.Models;

namespace Infrastructure.Services.EventProcessors;

public class ClearanceEventProcessor : BaseEventProcessor
{
    public override bool CanProcess(FootballMatchEvent matchEvent)
    {
        return matchEvent.action == "clearance";
    }

    public override void ProcessMatchEvent(FootballMatchEvent matchEvent, Match match)
    {
        if (IsHomeTeam(matchEvent, match))
            match.HomeTeamClearances = IncrementValue(match.HomeTeamClearances);
        else
            match.AwayTeamClearances = IncrementValue(match.AwayTeamClearances);
    }

    public override void ProcessEventCounters(
        FootballMatchEvent matchEvent,
        MatchEvents matchEvents,
        Match match
    )
    {
        matchEvents.TotalClearances++;
    }
}
