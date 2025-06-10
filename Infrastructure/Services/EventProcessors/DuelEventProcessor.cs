using Domain.Models;

namespace Infrastructure.Services.EventProcessors;

public class DuelEventProcessor : BaseEventProcessor
{
    public override bool CanProcess(FootballMatchEvent matchEvent)
    {
        return matchEvent.action == "duel" || 
               matchEvent.action == "50/50" || 
               matchEvent.action == "shield";
    }

    public override void ProcessMatchEvent(FootballMatchEvent matchEvent, Match match)
    {
        if (IsHomeTeam(matchEvent, match))
        {
            match.HomeTeamDuels = IncrementValue(match.HomeTeamDuels);
            if (matchEvent.outcome == "won" || matchEvent.outcome == "Success")
                match.HomeTeamDuelsWon = IncrementValue(match.HomeTeamDuelsWon);
        }
        else
        {
            match.AwayTeamDuels = IncrementValue(match.AwayTeamDuels);
            if (matchEvent.outcome == "won" || matchEvent.outcome == "Success")
                match.AwayTeamDuelsWon = IncrementValue(match.AwayTeamDuelsWon);
        }
    }

    public override void ProcessEventCounters(FootballMatchEvent matchEvent, MatchEvents matchEvents, Match match)
    {
        matchEvents.TotalDuels++;
    }
}
