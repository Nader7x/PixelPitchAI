using Domain.Models;

namespace Infrastructure.Services.EventProcessors;

public class PassEventProcessor : BaseEventProcessor
{
    public override bool CanProcess(FootballMatchEvent matchEvent)
    {
        return matchEvent.action == "pass";
    }

    public override void ProcessMatchEvent(FootballMatchEvent matchEvent, Match match)
    {
        // Increment pass count
        if (IsHomeTeam(matchEvent, match))
        {
            if (match.MatchStatistics != null)
                match.MatchStatistics.HomeTeamPasses = IncrementValue(
                    match.MatchStatistics.HomeTeamPasses
                );
        }
        else if (match.MatchStatistics != null)
        {
            match.MatchStatistics.AwayTeamPasses = IncrementValue(
                match.MatchStatistics.AwayTeamPasses
            );
        }

        // Process specific pass types
        switch (matchEvent.type)
        {
            case "Corner" when match.MatchStatistics is not null:
                if (IsHomeTeam(matchEvent, match))
                    match.MatchStatistics.HomeTeamCorners = IncrementValue(
                        match.MatchStatistics.HomeTeamCorners
                    );
                else
                    match.MatchStatistics.AwayTeamCorners = IncrementValue(
                        match.MatchStatistics.AwayTeamCorners
                    );
                break;

            case "Free Kick" when match.MatchStatistics is not null:
                if (IsHomeTeam(matchEvent, match))
                    match.MatchStatistics.HomeTeamFreeKicks = IncrementValue(
                        match.MatchStatistics.HomeTeamFreeKicks
                    );
                else
                    match.MatchStatistics.AwayTeamFreeKicks = IncrementValue(
                        match.MatchStatistics.AwayTeamFreeKicks
                    );
                break;

            case "Goal Kick" when match.MatchStatistics is not null:
                if (IsHomeTeam(matchEvent, match))
                    match.MatchStatistics.HomeTeamGoalKicks = IncrementValue(
                        match.MatchStatistics.HomeTeamGoalKicks
                    );
                else
                    match.MatchStatistics.AwayTeamGoalKicks = IncrementValue(
                        match.MatchStatistics.AwayTeamGoalKicks
                    );
                break;

            case "Recovery" when match.MatchStatistics is not null:
                // Recovery passes usually follow a ball recovery
                if (IsHomeTeam(matchEvent, match))
                    match.MatchStatistics.HomeTeamRecoveries = IncrementValue(
                        match.MatchStatistics.HomeTeamRecoveries
                    );
                else
                    match.MatchStatistics.AwayTeamRecoveries = IncrementValue(
                        match.MatchStatistics.AwayTeamRecoveries
                    );
                break;
        }

        // Process pass outcomes
        switch (matchEvent.outcome)
        {
            case "Pass Offside" when match.MatchStatistics is not null:
                if (IsHomeTeam(matchEvent, match))
                    match.MatchStatistics.HomeTeamOffsides = IncrementValue(
                        match.MatchStatistics.HomeTeamOffsides
                    );
                else
                    match.MatchStatistics.AwayTeamOffsides = IncrementValue(
                        match.MatchStatistics.AwayTeamOffsides
                    );
                break;

            case "Complete" when match.MatchStatistics is not null:
                if (IsHomeTeam(matchEvent, match))
                {
                    match.MatchStatistics.HomeTeamPassesCompleted = IncrementValue(
                        match.MatchStatistics.HomeTeamPassesCompleted
                    );
                    if (matchEvent.long_pass == true)
                        match.MatchStatistics.HomeAccurateLongBalls = IncrementValue(
                            match.MatchStatistics.HomeAccurateLongBalls
                        );
                }
                else
                {
                    match.MatchStatistics.AwayTeamPassesCompleted = IncrementValue(
                        match.MatchStatistics.AwayTeamPassesCompleted
                    );
                    if (matchEvent.long_pass == true)
                        match.MatchStatistics.AwayAccurateLongBalls = IncrementValue(
                            match.MatchStatistics.AwayAccurateLongBalls
                        );
                }

                break;
        }

        // Process long balls
        if (matchEvent.long_pass != true || match.MatchStatistics is null)
            return;
        if (IsHomeTeam(matchEvent, match))
            match.MatchStatistics.HomeLongBalls = IncrementValue(
                match.MatchStatistics.HomeLongBalls
            );
        else
            match.MatchStatistics.AwayLongBalls = IncrementValue(
                match.MatchStatistics.AwayLongBalls
            );
    }

    public override void ProcessEventCounters(
        FootballMatchEvent matchEvent,
        MatchEvents matchEvents,
        Match match
    )
    {
        matchEvents.TotalPasses++;

        switch (matchEvent.type)
        {
            case "Corner":
                matchEvents.TotalCorners++;
                break;

            case "Free Kick":
                matchEvents.TotalFreeKicks++;
                break;

            case "Goal Kick":
                matchEvents.TotalGoalKicks++;
                break;

            case "Interception":
                matchEvents.TotalInterceptions++;
                break;

            case "Recovery":
                matchEvents.TotalPossessionWon++;
                break;

            case "Throw-in":
                matchEvents.TotalThrowIns++;
                break;
        }

        switch (matchEvent.outcome)
        {
            case "Out":
            case "Injury Clearance":
                matchEvents.TotalOuts++;
                break;

            case "Pass Offside":
                matchEvents.TotalOffsides++;
                break;
        }
    }
}
