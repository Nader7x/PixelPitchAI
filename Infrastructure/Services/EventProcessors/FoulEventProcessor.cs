using Domain.Models;

namespace Infrastructure.Services.EventProcessors;

public class FoulEventProcessor : BaseEventProcessor
{
    public override bool CanProcess(FootballMatchEvent matchEvent)
    {
        return matchEvent.action is "foul committed" or "foul won";
    }

    public override void ProcessMatchEvent(FootballMatchEvent matchEvent, Match match)
    {
        if (matchEvent.action == "foul committed")
        {
            // Update fouls counter for the team that committed the foul
            if (IsHomeTeam(matchEvent, match))
                match.HomeTeamFouls = IncrementValue(match.HomeTeamFouls);
            else
                match.AwayTeamFouls = IncrementValue(match.AwayTeamFouls);
        }
        
        if (matchEvent.action == "foul won")
        {
            // When a foul is won, the opposing team gets a free kick
            if (IsHomeTeam(matchEvent, match))
                match.HomeTeamFreeKicks = IncrementValue(match.HomeTeamFreeKicks);
            else
                match.AwayTeamFreeKicks = IncrementValue(match.AwayTeamFreeKicks);
        }
    }

    public override void ProcessEventCounters(FootballMatchEvent matchEvent, MatchEvents matchEvents, Match match)
    {
        if (matchEvent.action == "foul committed")
        {
            matchEvents.TotalFouls++;
            
            if (matchEvent.outcome is "Penalty" or "penalty")
                matchEvents.TotalPenalties++;
        }
        
        if (matchEvent.action == "foul won")
            matchEvents.TotalFreeKicks++;
    }
}
