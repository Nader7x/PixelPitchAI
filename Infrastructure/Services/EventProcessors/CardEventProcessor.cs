using Domain.Models;

namespace Infrastructure.Services.EventProcessors;

public class CardEventProcessor : BaseEventProcessor
{
    public override bool CanProcess(FootballMatchEvent matchEvent)
    {
        return matchEvent is { action: "foul committed", card: "Yellow Card" or "Red Card" };
    }

    public override void ProcessMatchEvent(FootballMatchEvent matchEvent, Match match)
    {
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
        matchEvents.TotalCards++;
        
        if (matchEvent.card == "Yellow Card")
            matchEvents.TotalYellowCards++;
        else if (matchEvent.card == "Red Card")
            matchEvents.TotalRedCards++;
    }
}
