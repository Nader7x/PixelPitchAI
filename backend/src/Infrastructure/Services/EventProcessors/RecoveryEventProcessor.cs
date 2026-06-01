using Domain.Models;

namespace Infrastructure.Services.EventProcessors;

public class RecoveryEventProcessor : BaseEventProcessor
{
    public override bool CanProcess(FootballMatchEvent matchEvent)
    {
        return matchEvent.action == "ball recovery"
            || matchEvent is { action: "pass", type: "Recovery" };
    }

    public override void ProcessMatchEvent(FootballMatchEvent matchEvent, Match match)
    {
        if (IsHomeTeam(matchEvent, match))
        {
            if (match.MatchStatistics == null)
                return;
            match.MatchStatistics.HomeTeamRecoveries = IncrementValue(
                match.MatchStatistics.HomeTeamRecoveries
            );
            match.MatchStatistics.HomeTeamPossessionWon = IncrementValue(
                match.MatchStatistics.HomeTeamPossessionWon
            );
        }
        else if (match.MatchStatistics != null)
        {
            match.MatchStatistics.AwayTeamRecoveries = IncrementValue(
                match.MatchStatistics.AwayTeamRecoveries
            );
            match.MatchStatistics.AwayTeamPossessionWon = IncrementValue(
                match.MatchStatistics.AwayTeamPossessionWon
            );
        }
    }

    public override void ProcessEventCounters(
        FootballMatchEvent matchEvent,
        MatchEvents matchEvents,
        Match match
    )
    {
        matchEvents.TotalPossessionWon++;
    }
}
