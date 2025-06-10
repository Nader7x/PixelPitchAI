using Domain.Models;

namespace Infrastructure.Services.EventProcessors;

public class InterceptionEventProcessor : BaseEventProcessor
{
    public override bool CanProcess(FootballMatchEvent matchEvent)
    {
        return matchEvent.action == "interception" || 
               (matchEvent.action == "pass" && matchEvent.type == "Interception");
    }

    public override void ProcessMatchEvent(FootballMatchEvent matchEvent, Match match)
    {
        // Interceptions in football are typically counted as possessions won
        if (IsHomeTeam(matchEvent, match))
            match.HomeTeamPossessionWon = IncrementValue(match.HomeTeamPossessionWon);
        else
            match.AwayTeamPossessionWon = IncrementValue(match.AwayTeamPossessionWon);
    }

    public override void ProcessEventCounters(FootballMatchEvent matchEvent, MatchEvents matchEvents, Match match)
    {
        matchEvents.TotalInterceptions++;
    }
}
