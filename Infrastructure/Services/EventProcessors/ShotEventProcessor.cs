using Domain.Models;

namespace Infrastructure.Services.EventProcessors;

public class ShotEventProcessor : BaseEventProcessor
{
    public override bool CanProcess(FootballMatchEvent matchEvent) => 
        matchEvent.action == "shot";

    public override void ProcessMatchEvent(FootballMatchEvent matchEvent, Match match)
    {
        // Handle free kick
        if (matchEvent.type == "Free Kick")
        {
            if (IsHomeTeam(matchEvent, match))
                match.HomeTeamFreeKicks = IncrementValue(match.HomeTeamFreeKicks);
            else
                match.AwayTeamFreeKicks = IncrementValue(match.AwayTeamFreeKicks);
        }

        // Update shots counter
        if (IsHomeTeam(matchEvent, match))
            match.HomeTeamShots = IncrementValue(match.HomeTeamShots);
        else
            match.AwayTeamShots = IncrementValue(match.AwayTeamShots);

        // Process outcome
        switch (matchEvent.outcome)
        {
            case "Goal":
                if (IsHomeTeam(matchEvent, match))
                {
                    match.HomeTeamShotsOnTarget = IncrementValue(match.HomeTeamShotsOnTarget);
                    match.HomeTeamScore = IncrementValue(match.HomeTeamScore);
                }
                else
                {
                    match.AwayTeamShotsOnTarget = IncrementValue(match.AwayTeamShotsOnTarget);
                    match.AwayTeamScore = IncrementValue(match.AwayTeamScore);
                }
                UpdateMatchResult(match);
                break;
                
            case "Wayward":
            case "Off T":
                if (IsHomeTeam(matchEvent, match))
                    match.HomeTeamShotsOffTarget = IncrementValue(match.HomeTeamShotsOffTarget);
                else
                    match.AwayTeamShotsOffTarget = IncrementValue(match.AwayTeamShotsOffTarget);
                break;
                
            case "Saved":
                if (IsHomeTeam(matchEvent, match))
                {
                    match.HomeTeamShotsOnTarget = IncrementValue(match.HomeTeamShotsOnTarget);
                    match.AwayTeamSaves = IncrementValue(match.AwayTeamSaves);
                }
                else
                {
                    match.AwayTeamShotsOnTarget = IncrementValue(match.AwayTeamShotsOnTarget);
                    match.HomeTeamSaves = IncrementValue(match.HomeTeamSaves);
                }
                break;
        }
    }

    public override void ProcessEventCounters(FootballMatchEvent matchEvent, MatchEvents matchEvents, Match match)
    {
        matchEvents.TotalShots++;
        
        if (matchEvent.type == "Free Kick")
        {
            matchEvents.TotalFreeKicks++;
        }
        
        switch (matchEvent.outcome)
        {
            case "Blocked":
                matchEvents.TotalBlocks++;
                break;
                
            case "Goal":
                matchEvents.TotalGoals++;
                if (IsHomeTeam(matchEvent, match))
                    matchEvents.GoalsHomeTeam++;
                else
                    matchEvents.GoalsAwayTeam++;
                break;
                
            case "Saved":
                matchEvents.TotalGoalkeeperSaves++;
                break;
        }
    }

    private static void UpdateMatchResult(Match match)
    {
        if (match is not { HomeTeamScore: not null, AwayTeamScore: not null }) return;
        
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
