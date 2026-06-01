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
            match.MatchStatistics.HomeTeamClearances = IncrementValue(
                match.MatchStatistics.HomeTeamClearances
            );
        else
            match.MatchStatistics.AwayTeamClearances = IncrementValue(
                match.MatchStatistics.AwayTeamClearances
            );
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
