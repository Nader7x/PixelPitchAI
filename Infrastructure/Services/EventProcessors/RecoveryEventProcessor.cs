using Domain.Models;

namespace Infrastructure.Services.EventProcessors;

public class RecoveryEventProcessor : BaseEventProcessor
{
    public override bool CanProcess(FootballMatchEvent matchEvent)
    {
        return matchEvent.action == "ball recovery" || 
               (matchEvent.action == "pass" && matchEvent.type == "Recovery");
    }

    public override void ProcessMatchEvent(FootballMatchEvent matchEvent, Match match)
    {
        if (IsHomeTeam(matchEvent, match))
            match.HomeTeamRecoveries = IncrementValue(match.HomeTeamRecoveries);
        else
            match.AwayTeamRecoveries = IncrementValue(match.AwayTeamRecoveries);
        
        // Also count as possession won
        if (IsHomeTeam(matchEvent, match))
            match.HomeTeamPossessionWon = IncrementValue(match.HomeTeamPossessionWon);
        else
            match.AwayTeamPossessionWon = IncrementValue(match.AwayTeamPossessionWon);
    }

    public override void ProcessEventCounters(FootballMatchEvent matchEvent, MatchEvents matchEvents, Match match)
    {
        matchEvents.TotalPossessionWon++;
    }
}
