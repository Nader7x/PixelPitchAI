using Domain.Models;

namespace Infrastructure.Services.EventProcessors;

public class PossessionCalculator
{
    /// <summary>
    ///     Updates possession statistics for a match based on the current event
    /// </summary>
    public static void UpdatePossession(Match match, FootballMatchEvent currentEvent)
    {
        UpdatePossessionDuration(match, currentEvent);
        DeterminePossessingTeam(match, currentEvent);
        CalculatePossessionPercentages(match);
    }

    private static void UpdatePossessionDuration(Match match, FootballMatchEvent currentEvent)
    {
        if (match.LastEventTimestampSeconds.HasValue && match.LastEventPossessingTeamName != null)
        {
            var durationSeconds = currentEvent.time_seconds - match.LastEventTimestampSeconds.Value;
            if (durationSeconds > 0)
            {
                if (match.LastEventPossessingTeamName == match.HomeTeamInMatchName)
                    match.HomeTeamPossessionDurationSeconds =
                        (match.HomeTeamPossessionDurationSeconds ?? 0) + durationSeconds;
                else if (match.LastEventPossessingTeamName == match.AwayTeamInMatchName)
                    match.AwayTeamPossessionDurationSeconds =
                        (match.AwayTeamPossessionDurationSeconds ?? 0) + durationSeconds;
            }
        }
    }

    private static void DeterminePossessingTeam(Match match, FootballMatchEvent currentEvent)
    {
        // Default to the team performing the action
        var currentPossessingTeam = currentEvent.team;

        switch (currentEvent.action)
        {
            case "pass":
                currentPossessingTeam = currentEvent.outcome == "Complete"
                    ? currentEvent.team
                    : GetOpponentTeam(currentEvent.team, match);
                break;

            case "shot":
                // If shot is saved or goes out, opponent gets possession
                if (currentEvent.outcome is "Saved" or "Out" or "Blocked" or "Post" or "Wayward" or "Off T")
                    currentPossessingTeam = GetOpponentTeam(currentEvent.team, match);
                // If goal, possession resets to the conceding team for kickoff
                else if (currentEvent.outcome == "Goal")
                    currentPossessingTeam = GetOpponentTeam(currentEvent.team, match);
                break;

            case "interception":
            case "ball recovery":
            case "Save": // Goalkeeper save
                currentPossessingTeam = currentEvent.team;
                break;

            case "foul committed": // Team that committed the foul loses possession
                currentPossessingTeam = GetOpponentTeam(currentEvent.team, match);
                break;

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
                if (currentEvent.outcome == "success" || currentEvent.outcome == "won")
                    currentPossessingTeam = currentEvent.team;
                else if (currentEvent.outcome == "lost")
                    currentPossessingTeam = GetOpponentTeam(currentEvent.team, match);
                else
                    currentPossessingTeam = null; // Ambiguous
                break;
        }

        match.LastEventTimestampSeconds = currentEvent.time_seconds;
        match.LastEventPossessingTeamName = currentPossessingTeam;
    }

    private static void CalculatePossessionPercentages(Match match)
    {
        if (match.HomeTeamPossessionDurationSeconds.HasValue && match.AwayTeamPossessionDurationSeconds.HasValue)
        {
            var totalPossessionSeconds = match.HomeTeamPossessionDurationSeconds.Value +
                                         match.AwayTeamPossessionDurationSeconds.Value;
            if (totalPossessionSeconds > 0)
            {
                match.HomeTeamPossession = (int)Math.Round((double)match.HomeTeamPossessionDurationSeconds.Value * 100 /
                                                           totalPossessionSeconds);
                match.AwayTeamPossession = 100 - match.HomeTeamPossession.Value;
            }
            else
            {
                match.HomeTeamPossession = 0;
                match.AwayTeamPossession = 0;
            }
        }
        else
        {
            match.HomeTeamPossession = 0;
            match.AwayTeamPossession = 0;
        }
    }

    private static string? GetOpponentTeam(string currentTeamName, Match match)
    {
        if (currentTeamName == match.HomeTeamInMatchName)
            return match.AwayTeamInMatchName;
        if (currentTeamName == match.AwayTeamInMatchName)
            return match.HomeTeamInMatchName;
        return null; // Should not happen if team names are correct
    }
}