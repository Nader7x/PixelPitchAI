using Domain.Models;

namespace Infrastructure.Services.EventProcessors;

public class GoalkeeperEventProcessor : BaseEventProcessor
{
    public override bool CanProcess(FootballMatchEvent matchEvent)
    {
        return matchEvent.action == "Save"
            || matchEvent.action == "goal keeper"
            || matchEvent is { action: "shot", outcome: "Saved" };
    }

    public override void ProcessMatchEvent(FootballMatchEvent matchEvent, Match match)
    {
        switch (matchEvent.action)
        {
            case "Save" when match.MatchStatistics != null:
            {
                // Direct save action with various outcomes
                if (IsHomeTeam(matchEvent, match))
                    match.MatchStatistics.HomeTeamSaves = IncrementValue(
                        match.MatchStatistics.HomeTeamSaves
                    );
                else
                    match.MatchStatistics.AwayTeamSaves = IncrementValue(
                        match.MatchStatistics.AwayTeamSaves
                    );

                // Different save outcomes: In Play Danger, In Play Safe, No Touch, Saved Twice, Success, Touched Out
                // All are counted as saves but could be tracked separately if needed
                switch (matchEvent.outcome)
                {
                    case "In Play Danger":
                    case "In Play Safe":
                    case "No Touch":
                    case "Saved Twice":
                    case "Success":
                    case "Touched Out":
                        // All these outcomes count as successful saves
                        break;
                }

                break;
            }
            // General goalkeeper action - could be a catch, a punch, etc.
            case "goal keeper" when IsHomeTeam(matchEvent, match) && match.MatchStatistics != null:
                match.MatchStatistics.HomeTeamSaves = IncrementValue(
                    match.MatchStatistics.HomeTeamSaves
                );
                break;
            case "goal keeper" when match.MatchStatistics != null:
                match.MatchStatistics.AwayTeamSaves = IncrementValue(
                    match.MatchStatistics.AwayTeamSaves
                );
                break;
            case "shot" when matchEvent.outcome == "Saved" && match.MatchStatistics != null:
            {
                // Shot that was saved - credit save to an opposing team
                if (IsHomeTeam(matchEvent, match))
                {
                    // Away team saved Home team shot
                    match.MatchStatistics.AwayTeamSaves = IncrementValue(
                        match.MatchStatistics.AwayTeamSaves
                    );
                    match.MatchStatistics.HomeTeamShotsOnTarget = IncrementValue(
                        match.MatchStatistics.HomeTeamShotsOnTarget
                    );
                }
                else
                {
                    // The home team saved away team shot
                    match.MatchStatistics.HomeTeamSaves = IncrementValue(
                        match.MatchStatistics.HomeTeamSaves
                    );
                    match.MatchStatistics.AwayTeamShotsOnTarget = IncrementValue(
                        match.MatchStatistics.AwayTeamShotsOnTarget
                    );
                }

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
        matchEvents.TotalGoalkeeperSaves++;
    }
}
