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
        switch (matchEvent.action)
        {
            case "foul committed" when match.MatchStatistics != null:
            {
                // Update fouls counter for the team that committed the foul
                if (IsHomeTeam(matchEvent, match))
                    match.MatchStatistics.HomeTeamFouls = IncrementValue(
                        match.MatchStatistics.HomeTeamFouls
                    );
                else
                    match.MatchStatistics.AwayTeamFouls = IncrementValue(
                        match.MatchStatistics.AwayTeamFouls
                    );

                // If the foul results in a card, update the card count
                switch (matchEvent.card)
                {
                    case "Yellow Card" when IsHomeTeam(matchEvent, match):
                        match.MatchStatistics.HomeTeamYellowCards = IncrementValue(
                            match.MatchStatistics.HomeTeamYellowCards
                        );
                        break;
                    case "Yellow Card":
                        match.MatchStatistics.AwayTeamYellowCards = IncrementValue(
                            match.MatchStatistics.AwayTeamYellowCards
                        );
                        break;
                    case "Red Card" when IsHomeTeam(matchEvent, match):
                        match.MatchStatistics.HomeTeamRedCards = IncrementValue(
                            match.MatchStatistics.HomeTeamRedCards
                        );
                        break;
                    case "Red Card":
                        match.MatchStatistics.AwayTeamRedCards = IncrementValue(
                            match.MatchStatistics.AwayTeamRedCards
                        );
                        break;
                }

                break;
            }
            case "foul won" when match.MatchStatistics != null:
            {
                // When a foul is won, the opposing team gets a free kick
                if (IsHomeTeam(matchEvent, match))
                    match.MatchStatistics.HomeTeamFreeKicks = IncrementValue(
                        match.MatchStatistics.HomeTeamFreeKicks
                    );
                else
                    match.MatchStatistics.AwayTeamFreeKicks = IncrementValue(
                        match.MatchStatistics.AwayTeamFreeKicks
                    );
                break;
            }
        }
    }

    public override void ProcessEventCounters(
        FootballMatchEvent matchEvent,
        MatchEvents matchEvents,
        Match match
    )
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
