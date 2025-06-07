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
            match.HomeTeamPasses = IncrementValue(match.HomeTeamPasses);
        else
            match.AwayTeamPasses = IncrementValue(match.AwayTeamPasses);

        // Process specific pass types
        switch (matchEvent.type)
        {
            case "Free Kick":
                if (IsHomeTeam(matchEvent, match))
                    match.HomeTeamFreeKicks = IncrementValue(match.HomeTeamFreeKicks);
                else
                    match.AwayTeamFreeKicks = IncrementValue(match.AwayTeamFreeKicks);
                break;

            case "Goal Kick":
                if (IsHomeTeam(matchEvent, match))
                    match.HomeTeamGoalKicks = IncrementValue(match.HomeTeamGoalKicks);
                else
                    match.AwayTeamGoalKicks = IncrementValue(match.AwayTeamGoalKicks);
                break;
        }

        // Process pass outcomes
        switch (matchEvent.outcome)
        {
            case "Pass Offside":
                if (IsHomeTeam(matchEvent, match))
                    match.HomeTeamOffsides = IncrementValue(match.HomeTeamOffsides);
                else
                    match.AwayTeamOffsides = IncrementValue(match.AwayTeamOffsides);
                break;

            case "Complete":
                if (IsHomeTeam(matchEvent, match))
                {
                    match.HomeTeamPassesCompleted = IncrementValue(match.HomeTeamPassesCompleted);
                    if (matchEvent.long_pass == true)
                        match.HomeAccurateLongBalls = IncrementValue(match.HomeAccurateLongBalls);
                }
                else
                {
                    match.AwayTeamPassesCompleted = IncrementValue(match.AwayTeamPassesCompleted);
                    if (matchEvent.long_pass == true)
                        match.AwayAccurateLongBalls = IncrementValue(match.AwayAccurateLongBalls);
                }

                break;
        }

        // Process long balls
        if (matchEvent.long_pass == true)
        {
            if (IsHomeTeam(matchEvent, match))
                match.HomeLongBalls = IncrementValue(match.HomeLongBalls);
            else
                match.AwayLongBalls = IncrementValue(match.AwayLongBalls);
        }
    }

    public override void ProcessEventCounters(FootballMatchEvent matchEvent, MatchEvents matchEvents, Match match)
    {
        matchEvents.TotalPasses++;

        switch (matchEvent.type)
        {
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
                matchEvents.TotalOuts++;
                break;

            case "Pass Offside":
                matchEvents.TotalOffsides++;
                break;
        }
    }
}