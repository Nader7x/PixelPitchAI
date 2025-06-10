using Domain.Models;

namespace Infrastructure.Services.EventProcessors;

public class BadBehaviourEventProcessor : BaseEventProcessor
{
    public override bool CanProcess(FootballMatchEvent matchEvent)
    {
        return matchEvent.action == "bad behaviour";
    }

    public override void ProcessMatchEvent(FootballMatchEvent matchEvent, Match match)
    {
        // Bad behavior can result in cards
        if (matchEvent.card == "Yellow Card")
        {
            if (IsHomeTeam(matchEvent, match))
                match.HomeTeamYellowCards = IncrementValue(match.HomeTeamYellowCards);
            else
                match.AwayTeamYellowCards = IncrementValue(match.AwayTeamYellowCards);
        }
        else if (matchEvent.card == "Red Card")
        {
            if (IsHomeTeam(matchEvent, match))
                match.HomeTeamRedCards = IncrementValue(match.HomeTeamRedCards);
            else
                match.AwayTeamRedCards = IncrementValue(match.AwayTeamRedCards);
        }
    }

    public override void ProcessEventCounters(FootballMatchEvent matchEvent, MatchEvents matchEvents, Match match)
    {
        if (matchEvent.card != null && matchEvent.card != "No Card")
        {
            matchEvents.TotalCards++;
            
            if (matchEvent.card == "Yellow Card")
                matchEvents.TotalYellowCards++;
            else if (matchEvent.card == "Red Card")
                matchEvents.TotalRedCards++;
        }
        
        matchEvents.TotalEvents++;
    }
}
