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
        switch (matchEvent.card)
        {
            // Bad behavior can result in cards
            case "Yellow Card" when match.MatchStatistics != null:
            {
                if (IsHomeTeam(matchEvent, match))
                    match.MatchStatistics.HomeTeamYellowCards = IncrementValue(
                        match.MatchStatistics.HomeTeamYellowCards
                    );
                else
                    match.MatchStatistics.AwayTeamYellowCards = IncrementValue(
                        match.MatchStatistics.AwayTeamYellowCards
                    );

                break;
            }
            case "Red Card" when match.MatchStatistics != null:
            {
                if (IsHomeTeam(matchEvent, match))
                    match.MatchStatistics.HomeTeamRedCards = IncrementValue(
                        match.MatchStatistics.HomeTeamRedCards
                    );
                else
                    match.MatchStatistics.AwayTeamRedCards = IncrementValue(
                        match.MatchStatistics.AwayTeamRedCards
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
        if (matchEvent.card != null && matchEvent.card != "No Card")
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

        matchEvents.TotalEvents++;
    }
}
