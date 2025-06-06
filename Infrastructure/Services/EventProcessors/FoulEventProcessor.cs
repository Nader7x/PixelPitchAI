using Domain.Models;

namespace Infrastructure.Services.EventProcessors;

public class FoulEventProcessor : BaseEventProcessor
{
    public override bool CanProcess(FootballMatchEvent matchEvent) => 
        matchEvent.action == "foul committed" || matchEvent.action == "foul won" || matchEvent.action == "bad behaviour";

    public override void ProcessMatchEvent(FootballMatchEvent matchEvent, Match match)
    {
        if (matchEvent.action == "foul committed")
        {
            if (IsHomeTeam(matchEvent, match))
                match.HomeTeamFouls = IncrementValue(match.HomeTeamFouls);
            else
                match.AwayTeamFouls = IncrementValue(match.AwayTeamFouls);

            ProcessCard(matchEvent, match);
        }
        else if (matchEvent.action == "bad behaviour")
        {
            ProcessCard(matchEvent, match);
        }
    }

    public override void ProcessEventCounters(FootballMatchEvent matchEvent, MatchEvents matchEvents, Match match)
    {
        if (matchEvent.action == "foul committed")
        {
            matchEvents.TotalFouls++;
            if (matchEvent.outcome is "Penalty" or "penalty")
            {
                matchEvents.TotalPenalties++;
            }

            ProcessCardCounters(matchEvent, matchEvents);
        }
        else if (matchEvent.action == "foul won")
        {
            matchEvents.TotalFreeKicks++;
        }
        else if (matchEvent.action == "bad behaviour")
        {
            ProcessCardCounters(matchEvent, matchEvents);
        }
    }

    private static void ProcessCard(FootballMatchEvent matchEvent, Match match)
    {
        if (matchEvent.card == null || matchEvent.card == "No Card") 
            return;
            
        switch (matchEvent.card)
        {
            case "Yellow Card":
                if (IsHomeTeam(matchEvent, match))
                    match.HomeTeamYellowCards = IncrementValue(match.HomeTeamYellowCards);
                else
                    match.AwayTeamYellowCards = IncrementValue(match.AwayTeamYellowCards);
                break;
                
            case "Red Card":
                if (IsHomeTeam(matchEvent, match))
                    match.HomeTeamRedCards = IncrementValue(match.HomeTeamRedCards);
                else
                    match.AwayTeamRedCards = IncrementValue(match.AwayTeamRedCards);
                break;
        }
    }
    
    private static void ProcessCardCounters(FootballMatchEvent matchEvent, MatchEvents matchEvents)
    {
        if (matchEvent.card == null || matchEvent.card == "No Card") 
            return;
            
        matchEvents.TotalCards++;
        switch (matchEvent.card)
        {
            case "Yellow Card":
                matchEvents.TotalYellowCards++;
                break;
                
            case "Red Card":
                matchEvents.TotalRedCards++;
                break;
        }
    }
}
