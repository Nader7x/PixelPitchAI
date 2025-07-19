using Domain.Models;

namespace Infrastructure.Services.EventProcessors;

public abstract class PossessionCalculator
{
    /// <summary>
    ///     Updates possession statistics for a match based on the current event
    /// </summary>
    public static void UpdatePossession(Match match, FootballMatchEvent currentEvent)
    {
        UpdatePossessionDuration(match, currentEvent);
        DeterminePossessingTeam(match, currentEvent);
        if (match.MatchStatistics != null)
            CalculatePossessionPercentages(match.MatchStatistics);
    }

    private static void UpdatePossessionDuration(Match match, FootballMatchEvent currentEvent)
    {
        if (
            match.MatchStatistics
            is not { LastEventTimestampSeconds: not null, LastEventPossessingTeamName: not null }
        )
            return;
        var durationSeconds =
            currentEvent.time_seconds - match.MatchStatistics.LastEventTimestampSeconds.Value;
        if (durationSeconds <= 0)
            return;
        if (match.MatchStatistics.LastEventPossessingTeamName == match.HomeTeamInMatchName)
            match.MatchStatistics.HomeTeamPossessionDurationSeconds =
                (match.MatchStatistics.HomeTeamPossessionDurationSeconds ?? 0) + durationSeconds;
        else if (match.MatchStatistics.LastEventPossessingTeamName == match.AwayTeamInMatchName)
            match.MatchStatistics.AwayTeamPossessionDurationSeconds =
                (match.MatchStatistics.AwayTeamPossessionDurationSeconds ?? 0) + durationSeconds;
    }

    private static void DeterminePossessingTeam(Match match, FootballMatchEvent currentEvent)
    {
        // Default to the team performing the action
        var currentPossessingTeam = currentEvent.team;

        switch (currentEvent.action)
        {
            case "pass":
                currentPossessingTeam =
                    currentEvent.outcome == "Complete"
                        ? currentEvent.team
                        : GetOpponentTeam(currentEvent.team, match);
                break;

            case "shot":
                currentPossessingTeam = currentEvent.outcome switch
                {
                    // If a shot is saved or goes out, the opponent gets possession
                    // If goal; possession resets to the conceding team for kickoff
                    "Saved" or "Out" or "Blocked" or "Post" or "Wayward" or "Off T" or "Goal" =>
                        GetOpponentTeam(currentEvent.team, match),
                    _ => currentPossessingTeam,
                };
                break;

            case "interception":
            case "ball recovery":
            case "Save": // Goalkeeper save
                currentPossessingTeam = currentEvent.team;
                break;

            case "foul committed": // The team that committed the foul loses possession
            case "dispossessed":
            case "miscontrol":
            case "error":
                currentPossessingTeam = GetOpponentTeam(currentEvent.team, match);
                break;

            case "clearance":
                // Possession after clearance is ambiguous
                currentPossessingTeam = null;
                break;

            case "duel":
            case "50/50":
                currentPossessingTeam = currentEvent.outcome switch
                {
                    "success" or "won" => currentEvent.team,
                    "lost" => GetOpponentTeam(currentEvent.team, match),
                    _ => null,
                };
                break;
        }

        if (match.MatchStatistics == null)
            return;
        match.MatchStatistics.LastEventTimestampSeconds = currentEvent.time_seconds;
        match.MatchStatistics.LastEventPossessingTeamName = currentPossessingTeam;
    }

    private static void CalculatePossessionPercentages(MatchStatistics matchStatistics)
    {
        if (
            matchStatistics is
            {
                HomeTeamPossessionDurationSeconds: not null,
                AwayTeamPossessionDurationSeconds: not null
            }
        )
        {
            var totalPossessionSeconds =
                matchStatistics.HomeTeamPossessionDurationSeconds.Value
                + matchStatistics.AwayTeamPossessionDurationSeconds.Value;
            if (totalPossessionSeconds > 0)
            {
                matchStatistics.HomeTeamPossession = (int)
                    Math.Round(
                        (double)matchStatistics.HomeTeamPossessionDurationSeconds.Value
                            * 100
                            / totalPossessionSeconds
                    );
                matchStatistics.AwayTeamPossession = 100 - matchStatistics.HomeTeamPossession.Value;
            }
            else
            {
                matchStatistics.HomeTeamPossession = 0;
                matchStatistics.AwayTeamPossession = 0;
            }
        }
        else
        {
            matchStatistics.HomeTeamPossession = 0;
            matchStatistics.AwayTeamPossession = 0;
        }
    }

    private static string? GetOpponentTeam(string currentTeamName, Match match)
    {
        if (currentTeamName == match.HomeTeamInMatchName)
            return match.AwayTeamInMatchName;
        return currentTeamName == match.AwayTeamInMatchName ? match.HomeTeamInMatchName : null;
    }
}
