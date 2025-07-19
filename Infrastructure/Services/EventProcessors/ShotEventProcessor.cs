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
        }

        // Update shots counter
        if (IsHomeTeam(matchEvent, match))
        {
            if (match.MatchStatistics != null)
                match.MatchStatistics.HomeTeamShots = IncrementValue(
                    match.MatchStatistics.HomeTeamShots
                );
        }
        else if (match.MatchStatistics != null)
        {
            match.MatchStatistics.AwayTeamShots = IncrementValue(
                match.MatchStatistics.AwayTeamShots
            );
        }

        // Process outcome
        switch (matchEvent.outcome)
        {
            case "Goal" when match.MatchStatistics is not null:
                if (IsHomeTeam(matchEvent, match))
                {
                    match.MatchStatistics.HomeTeamShotsOnTarget = IncrementValue(
                        match.MatchStatistics.HomeTeamShotsOnTarget
                    );
                    match.HomeTeamScore = IncrementValue(match.HomeTeamScore);
                }
                else
                {
                    match.MatchStatistics.AwayTeamShotsOnTarget = IncrementValue(
                        match.MatchStatistics.AwayTeamShotsOnTarget
                    );
                    match.AwayTeamScore = IncrementValue(match.AwayTeamScore);
                }

                UpdateMatchResult(match);
                break;

            case "Off T" when match.MatchStatistics is not null:
            case "Wayward" when match.MatchStatistics is not null:
                if (IsHomeTeam(matchEvent, match))
                    match.MatchStatistics.HomeTeamShotsOffTarget = IncrementValue(
                        match.MatchStatistics.HomeTeamShotsOffTarget
                    );
                else
                    match.MatchStatistics.AwayTeamShotsOffTarget = IncrementValue(
                        match.MatchStatistics.AwayTeamShotsOffTarget
                    );
                break;

            case "Saved" when match.MatchStatistics is not null:
                if (IsHomeTeam(matchEvent, match))
                {
                    match.MatchStatistics.HomeTeamShotsOnTarget = IncrementValue(
                        match.MatchStatistics.HomeTeamShotsOnTarget
                    );
                    match.MatchStatistics.AwayTeamSaves = IncrementValue(
                        match.MatchStatistics.AwayTeamSaves
                    );
                }
                else
                {
                    match.MatchStatistics.AwayTeamShotsOnTarget = IncrementValue(
                        match.MatchStatistics.AwayTeamShotsOnTarget
                    );
                    match.MatchStatistics.HomeTeamSaves = IncrementValue(
                        match.MatchStatistics.HomeTeamSaves
                    );
                }

                break;

            case "Post" when match.MatchStatistics is not null:
                // Shot hit the post - counted as a shot but not on target
                if (IsHomeTeam(matchEvent, match))
                    match.MatchStatistics.HomeTeamShotsOffTarget = IncrementValue(
                        match.MatchStatistics.HomeTeamShotsOffTarget
                    );
                else
                    match.MatchStatistics.AwayTeamShotsOffTarget = IncrementValue(
                        match.MatchStatistics.AwayTeamShotsOffTarget
                    );
                break;

            case "Saved Off Target" when match.MatchStatistics is not null:
                // Shot saved but was going off target anyway
                if (IsHomeTeam(matchEvent, match))
                {
                    match.MatchStatistics.HomeTeamShotsOffTarget = IncrementValue(
                        match.MatchStatistics.HomeTeamShotsOffTarget
                    );
                    match.MatchStatistics.AwayTeamSaves = IncrementValue(
                        match.MatchStatistics.AwayTeamSaves
                    );
                }
                else
                {
                    match.MatchStatistics.AwayTeamShotsOffTarget = IncrementValue(
                        match.MatchStatistics.AwayTeamShotsOffTarget
                    );
                    match.MatchStatistics.HomeTeamSaves = IncrementValue(
                        match.MatchStatistics.HomeTeamSaves
                    );
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
