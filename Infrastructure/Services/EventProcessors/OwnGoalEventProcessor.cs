using Domain.Models;

namespace Infrastructure.Services.EventProcessors;

public class OwnGoalEventProcessor : BaseEventProcessor
{
    public override bool CanProcess(FootballMatchEvent matchEvent)
    {
        return matchEvent.action == "own goal against";
    }

    public override void ProcessMatchEvent(FootballMatchEvent matchEvent, Match match)
    {
        // For own goals, the team that "scores" actually concedes
        // So we increment the opponent's score
        if (IsHomeTeam(matchEvent, match))
            // Home team scored an own goal, so away team gets the point
            match.AwayTeamScore = IncrementValue(match.AwayTeamScore);
        else
            // Away team scored an own goal, so home team gets the point
            match.HomeTeamScore = IncrementValue(match.HomeTeamScore);

        // Update match result after goal
        UpdateMatchResult(match);
    }

    public override void ProcessEventCounters(
        FootballMatchEvent matchEvent,
        MatchEvents matchEvents,
        Match match
    )
    {
        matchEvents.TotalGoals++;

        // For own goals, increment the score for the opposing team
        if (IsHomeTeam(matchEvent, match))
            matchEvents.GoalsAwayTeam++;
        else
            matchEvents.GoalsHomeTeam++;
    }

    private static void UpdateMatchResult(Match match)
    {
        if (match is not { HomeTeamScore: not null, AwayTeamScore: not null })
            return;

        if (match.HomeTeamScore > match.AwayTeamScore)
        {
            match.WinningTeamId = match.HomeTeamId;
            match.LosingTeamId = match.AwayTeamId;
            match.IsDraw = false;
        }
        else if (match.AwayTeamScore > match.HomeTeamScore)
        {
            match.WinningTeamId = match.AwayTeamId;
            match.LosingTeamId = match.HomeTeamId;
            match.IsDraw = false;
        }
        else
        {
            match.WinningTeamId = null;
            match.LosingTeamId = null;
            match.IsDraw = true;
        }
    }
}
