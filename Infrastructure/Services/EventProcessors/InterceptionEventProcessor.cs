using Domain.Models;

namespace Infrastructure.Services.EventProcessors;

public class InterceptionEventProcessor : BaseEventProcessor
{
    public override bool CanProcess(FootballMatchEvent matchEvent)
    {
        return matchEvent.action == "interception"
            || (matchEvent.action == "pass" && matchEvent.type == "Interception");
    }

    public override void ProcessMatchEvent(FootballMatchEvent matchEvent, Match match)
    {
        // Interceptions in football are typically counted as possessions won
        if (IsHomeTeam(matchEvent, match) && match.MatchStatistics != null)
            match.MatchStatistics.HomeTeamPossessionWon = IncrementValue(
                match.MatchStatistics.HomeTeamPossessionWon
            );
        else if (match.MatchStatistics != null)
            match.MatchStatistics.AwayTeamPossessionWon = IncrementValue(
                match.MatchStatistics.AwayTeamPossessionWon
            );
    }

    public override void ProcessEventCounters(
        FootballMatchEvent matchEvent,
        MatchEvents matchEvents,
        Match match
    )
    {
        matchEvents.TotalInterceptions++;
    }
}
