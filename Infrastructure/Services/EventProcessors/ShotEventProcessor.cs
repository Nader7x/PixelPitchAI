using Domain.Models;

namespace Infrastructure.Services.EventProcessors;

public class ShotEventProcessor : BaseEventProcessor
{
    public override bool CanProcess(FootballMatchEvent matchEvent)
    {
        return matchEvent.action == "shot";
    }

    public override void ProcessMatchEvent(FootballMatchEvent matchEvent, Match match)
    {
        // Handle free kick and penalty shot types
        switch (matchEvent.type)
        {
            case "Free Kick":
                if (IsHomeTeam(matchEvent, match))
                    match.HomeTeamFreeKicks = IncrementValue(match.HomeTeamFreeKicks);
                else
                    match.AwayTeamFreeKicks = IncrementValue(match.AwayTeamFreeKicks);
                break;
            case "Penalty":
                // Penalty shots are tracked in the matchEvents entity
                break;
            case "Open Play":
                // Regular shot in open play
                break;
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

            case "Off T":
            case "Wayward":
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

            case "Blocked":
                // Shots that are blocked are still counted as shots, but not on target
                break;

            case "Post":
                // Shot hit the post - counted as a shot but not on target
                if (IsHomeTeam(matchEvent, match))
                    match.HomeTeamShotsOffTarget = IncrementValue(match.HomeTeamShotsOffTarget);
                else
                    match.AwayTeamShotsOffTarget = IncrementValue(match.AwayTeamShotsOffTarget);
                break;

            case "Saved Off Target":
                // Shot saved but was going off target anyway
                if (IsHomeTeam(matchEvent, match))
                {
                    match.HomeTeamShotsOffTarget = IncrementValue(match.HomeTeamShotsOffTarget);
                    match.AwayTeamSaves = IncrementValue(match.AwayTeamSaves);
                }
                else
                {
                    match.AwayTeamShotsOffTarget = IncrementValue(match.AwayTeamShotsOffTarget);
                    match.HomeTeamSaves = IncrementValue(match.HomeTeamSaves);
                }
                break;
        }
    }

    public override void ProcessEventCounters(
        FootballMatchEvent matchEvent,
        MatchEvents matchEvents,
        Match match
    )
    {
        matchEvents.TotalShots++;

        // Process shot type
        switch (matchEvent.type)
        {
            case "Free Kick":
                matchEvents.TotalFreeKicks++;
                break;
            case "Penalty":
                matchEvents.TotalPenalties++;
                break;
        }

        // Process outcome
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
            case "Saved Off Target":
                matchEvents.TotalGoalkeeperSaves++;
                break;
        }
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
