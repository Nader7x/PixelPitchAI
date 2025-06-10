using Domain.Models;

namespace Infrastructure.Services.EventProcessors;

public class GoalkeeperEventProcessor : BaseEventProcessor
{
    public override bool CanProcess(FootballMatchEvent matchEvent)
    {
        return matchEvent.action == "Save" || 
               matchEvent.action == "goal keeper" ||
               (matchEvent.action == "shot" && matchEvent.outcome == "Saved");
    }

    public override void ProcessMatchEvent(FootballMatchEvent matchEvent, Match match)
    {
        if (matchEvent.action == "Save")
        {
            // Direct save action with various outcomes
            if (IsHomeTeam(matchEvent, match))
                match.HomeTeamSaves = IncrementValue(match.HomeTeamSaves);
            else
                match.AwayTeamSaves = IncrementValue(match.AwayTeamSaves);
            
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
        }
        else if (matchEvent.action == "goal keeper")
        {
            // General goalkeeper action - could be a catch, a punch, etc.
            if (IsHomeTeam(matchEvent, match))
                match.HomeTeamSaves = IncrementValue(match.HomeTeamSaves);
            else
                match.AwayTeamSaves = IncrementValue(match.AwayTeamSaves);
        }
        else if (matchEvent.action == "shot" && matchEvent.outcome == "Saved")
        {
            // Shot that was saved - credit save to opposing team
            if (IsHomeTeam(matchEvent, match))
            {
                // Home team shot was saved by away team
                match.AwayTeamSaves = IncrementValue(match.AwayTeamSaves);
                match.HomeTeamShotsOnTarget = IncrementValue(match.HomeTeamShotsOnTarget);
            }
            else
            {
                // Away team shot was saved by home team
                match.HomeTeamSaves = IncrementValue(match.HomeTeamSaves);
                match.AwayTeamShotsOnTarget = IncrementValue(match.AwayTeamShotsOnTarget);
            }
        }
    }

    public override void ProcessEventCounters(FootballMatchEvent matchEvent, MatchEvents matchEvents, Match match)
    {
        matchEvents.TotalGoalkeeperSaves++;
    }
}
