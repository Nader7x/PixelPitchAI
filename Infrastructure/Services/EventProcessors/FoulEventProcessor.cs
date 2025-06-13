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
            // If the foul results in a card, update the card count
            switch (matchEvent.card)
            {
                case "Yellow Card" when IsHomeTeam(matchEvent, match):
                    match.HomeTeamYellowCards = IncrementValue(match.HomeTeamYellowCards);
                    break;
                case "Yellow Card":
                    match.AwayTeamYellowCards = IncrementValue(match.AwayTeamYellowCards);
                    break;
                case "Red Card" when IsHomeTeam(matchEvent, match):
                    match.HomeTeamRedCards = IncrementValue(match.HomeTeamRedCards);
                    break;
                case "Red Card":
                    match.AwayTeamRedCards = IncrementValue(match.AwayTeamRedCards);
                    break;
            }
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
        switch (matchEvent.action)
        {
            case "foul committed":
            {
                matchEvents.TotalFouls++;
            
                if (matchEvent.outcome is "Penalty" or "penalty")
                    matchEvents.TotalPenalties++;
                if (matchEvent.card is not null)
                {
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

                break;
            }
            case "foul won":
                matchEvents.TotalFreeKicks++;
                break;
        }
    }
}
