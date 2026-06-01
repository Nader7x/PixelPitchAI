using Domain.Models;

namespace Infrastructure.Services.EventProcessors;

public class DuelEventProcessor : BaseEventProcessor
{
    public override bool CanProcess(FootballMatchEvent matchEvent)
    {
        return matchEvent.action is "duel" or "50/50" or "shield";
    }

    public override void ProcessMatchEvent(FootballMatchEvent matchEvent, Match match)
    {
        if (IsHomeTeam(matchEvent, match))
        {
            if (match.MatchStatistics == null)
                return;
            match.MatchStatistics.HomeTeamDuels = IncrementValue(
                match.MatchStatistics.HomeTeamDuels
            );
            if (matchEvent.outcome is "won" or "Success")
                match.MatchStatistics.HomeTeamDuelsWon = IncrementValue(
                    match.MatchStatistics.HomeTeamDuelsWon
                );
        }
        else
        {
            if (match.MatchStatistics == null)
                return;
            match.MatchStatistics.AwayTeamDuels = IncrementValue(
                match.MatchStatistics.AwayTeamDuels
            );
            if (matchEvent.outcome is "won" or "Success")
                match.MatchStatistics.AwayTeamDuelsWon = IncrementValue(
                    match.MatchStatistics.AwayTeamDuelsWon
                );
        }
    }

    public override void ProcessEventCounters(
        FootballMatchEvent matchEvent,
        MatchEvents matchEvents,
        Match match
    )
    {
        matchEvents.TotalDuels++;
    }
}
