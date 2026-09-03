using System.Globalization;
using Domain.Models;

namespace Infrastructure.Services;

/// <summary>
///     High-performance, zero-allocation event parser for raw simulation text lines.
///     Uses ReadOnlySpan<char> slicing and minimal allocations for Native AOT runtime efficiency.
/// </summary>
public static class ZeroAllocationEventParser
{
    private static readonly char[] TrimChars = [' ', '\t', '\r', '\n'];

    public static bool TryParseEvent(
        ReadOnlySpan<char> rawLine,
        string matchId,
        int eventIndex,
        ref int homeScore,
        ref int awayScore,
        string? homeTeamName,
        string? awayTeamName,
        out FootballMatchEvent? matchEvent
    )
    {
        matchEvent = null;
        var line = rawLine.Trim(TrimChars);
        if (line.IsEmpty)
            return false;

        // Check system / match control markers
        if (line.StartsWith("[MATCH START]", StringComparison.OrdinalIgnoreCase))
        {
            matchEvent = CreateSystemEvent("match_start", "00:00", 0, 0, 0, eventIndex, matchId, homeScore, awayScore);
            return true;
        }

        if (line.StartsWith("[END OF FIRST HALF]", StringComparison.OrdinalIgnoreCase))
        {
            matchEvent = CreateSystemEvent("first_half_end", "45:00", 2700, 45, 0, eventIndex, matchId, homeScore, awayScore);
            return true;
        }

        if (line.StartsWith("[SECOND HALF START]", StringComparison.OrdinalIgnoreCase) ||
            line.StartsWith("[SECOND HALF]", StringComparison.OrdinalIgnoreCase))
        {
            matchEvent = CreateSystemEvent("second_half_start", "45:00", 2700, 45, 0, eventIndex, matchId, homeScore, awayScore);
            return true;
        }

        if (line.StartsWith("[STOPPAGE TIME", StringComparison.OrdinalIgnoreCase))
        {
            matchEvent = CreateSystemEvent("stoppage_time_start", "90:00", 5400, 90, 0, eventIndex, matchId, homeScore, awayScore);
            return true;
        }

        if (line.StartsWith("[MATCH END]", StringComparison.OrdinalIgnoreCase))
        {
            matchEvent = CreateSystemEvent("match_end", "90:00", 5400, 90, 0, eventIndex, matchId, homeScore, awayScore);
            return true;
        }

        if (line.StartsWith("[EVENTS START]", StringComparison.OrdinalIgnoreCase) ||
            line.StartsWith("[", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        // Expected format: "MM:SS -  TeamName  - action [by Player] [at (x, y)] [, key: value...]"
        // First delimiter: " - "
        var firstDash = line.IndexOf(" - ", StringComparison.Ordinal);
        if (firstDash <= 0)
            return false;

        var timeSpan = line[..firstDash].Trim(TrimChars);
        var colonIdx = timeSpan.IndexOf(':');
        var minute = 0;
        var second = 0;
        if (colonIdx > 0)
        {
            _ = int.TryParse(timeSpan[..colonIdx], NumberStyles.Integer, CultureInfo.InvariantCulture, out minute);
            _ = int.TryParse(timeSpan[(colonIdx + 1)..], NumberStyles.Integer, CultureInfo.InvariantCulture, out second);
        }
        var totalSeconds = minute * 60 + second;

        var remainder = line[(firstDash + 3)..];
        var secondDash = remainder.IndexOf(" - ", StringComparison.Ordinal);
        if (secondDash <= 0)
            return false;

        var teamSpan = remainder[..secondDash].Trim(TrimChars);
        var detailsSpan = remainder[(secondDash + 3)..].Trim(TrimChars);

        var teamName = teamSpan.ToString();
        var action = string.Empty;
        string? player = null;
        float[]? position = null;
        string? outcome = null;
        string? height = null;
        string? card = null;
        float[]? passTarget = null;
        float[]? shotTarget = null;
        string? bodyPart = null;
        string? eventType = null;
        string? type = null;
        bool? longPass = null;
        decimal? passLength = null;

        // Parse action and player: e.g. "pass by Antoine Griezmann at (60.0, 40.0)..."
        // or "ball receipt* by Saúl Ñíguez at (50.0, 36.0)"
        var byIndex = detailsSpan.IndexOf(" by ", StringComparison.Ordinal);
        var atIndex = detailsSpan.IndexOf(" at (", StringComparison.Ordinal);
        var commaIndex = IndexOfTopLevelComma(detailsSpan);

        if (byIndex >= 0)
        {
            action = detailsSpan[..byIndex].Trim(TrimChars).ToString();
            var afterBy = detailsSpan[(byIndex + 4)..];
            var playerEnd = afterBy.IndexOf(" at (", StringComparison.Ordinal);
            if (playerEnd < 0)
                playerEnd = IndexOfTopLevelComma(afterBy);

            if (playerEnd >= 0)
            {
                player = afterBy[..playerEnd].Trim(TrimChars).ToString();
            }
            else
            {
                player = afterBy.Trim(TrimChars).ToString();
            }
        }
        else if (atIndex >= 0)
        {
            action = detailsSpan[..atIndex].Trim(TrimChars).ToString();
        }
        else if (commaIndex >= 0)
        {
            action = detailsSpan[..commaIndex].Trim(TrimChars).ToString();
        }
        else
        {
            action = detailsSpan.ToString();
        }

        eventType = DetermineEventType(action);

        // Parse coordinates " at (x, y)"
        if (atIndex >= 0)
        {
            position = ParseCoordinates(detailsSpan[(atIndex + 4)..]);
        }

        // Parse key-value properties: "to (x, y)", "aimed at (x, y)", "type: ...", "outcome: ...", "card: ...", "height: ...", "using: ..."
        if (commaIndex >= 0)
        {
            var propsSpan = detailsSpan[commaIndex..];
            while (!propsSpan.IsEmpty)
            {
                if (propsSpan.StartsWith(","))
                    propsSpan = propsSpan[1..].Trim(TrimChars);

                var nextComma = IndexOfTopLevelComma(propsSpan);
                var currentToken = nextComma >= 0 ? propsSpan[..nextComma].Trim(TrimChars) : propsSpan.Trim(TrimChars);

                if (currentToken.StartsWith("type: ", StringComparison.OrdinalIgnoreCase))
                {
                    type = currentToken[6..].Trim(TrimChars).ToString();
                }
                else if (currentToken.StartsWith("outcome: ", StringComparison.OrdinalIgnoreCase))
                {
                    outcome = currentToken[9..].Trim(TrimChars).ToString();
                }
                else if (currentToken.StartsWith("height: ", StringComparison.OrdinalIgnoreCase))
                {
                    height = currentToken[8..].Trim(TrimChars).ToString();
                    if (height.Contains("High", StringComparison.OrdinalIgnoreCase))
                        longPass = true;
                }
                else if (currentToken.StartsWith("card: ", StringComparison.OrdinalIgnoreCase))
                {
                    card = currentToken[6..].Trim(TrimChars).ToString();
                }
                else if (currentToken.StartsWith("to (", StringComparison.OrdinalIgnoreCase))
                {
                    passTarget = ParseCoordinates(currentToken[3..]);
                }
                else if (currentToken.StartsWith("aimed at (", StringComparison.OrdinalIgnoreCase))
                {
                    shotTarget = ParseCoordinates(currentToken[9..]);
                }
                else if (currentToken.StartsWith("using ", StringComparison.OrdinalIgnoreCase))
                {
                    bodyPart = currentToken[6..].Trim(TrimChars).ToString();
                }

                if (nextComma < 0)
                    break;
                propsSpan = propsSpan[(nextComma + 1)..];
            }
        }

        // Calculate pass length if pass target exists
        if (position is { Length: 2 } && passTarget is { Length: 2 })
        {
            var dx = passTarget[0] - position[0];
            var dy = passTarget[1] - position[1];
            passLength = (decimal)Math.Sqrt(dx * dx + dy * dy);
            if (passLength > 35m)
                longPass = true;
        }

        // Update score if goal
        if (string.Equals(outcome, "Goal", StringComparison.OrdinalIgnoreCase) ||
            action.Contains("goal", StringComparison.OrdinalIgnoreCase))
        {
            if (homeTeamName != null && teamName.Contains(homeTeamName, StringComparison.OrdinalIgnoreCase))
                homeScore++;
            else if (awayTeamName != null && teamName.Contains(awayTeamName, StringComparison.OrdinalIgnoreCase))
                awayScore++;
            else
                homeScore++; // default increment if ambiguous
        }

        matchEvent = new FootballMatchEvent
        {
            timestamp = timeSpan.ToString(),
            time_seconds = totalSeconds,
            minute = minute,
            second = second,
            team = teamName,
            player = player,
            action = action,
            position = position ?? [0f, 0f],
            outcome = outcome,
            height = height,
            card = card,
            pass_target = passTarget,
            shot_target = shotTarget,
            body_part = bodyPart,
            event_type = eventType,
            type = type,
            event_index = eventIndex,
            match_id = matchId,
            home_team = homeTeamName,
            away_team = awayTeamName,
            long_pass = longPass,
            pass_length = passLength,
            Score = new Score { Home = homeScore, Away = awayScore }
        };

        return true;
    }

    private static float[]? ParseCoordinates(ReadOnlySpan<char> span)
    {
        var openParen = span.IndexOf('(');
        var closeParen = span.IndexOf(')');
        if (openParen < 0 || closeParen <= openParen)
            return null;

        var coords = span.Slice(openParen + 1, closeParen - openParen - 1);
        var comma = coords.IndexOf(',');
        if (comma < 0)
            return null;

        if (float.TryParse(coords[..comma].Trim(TrimChars), NumberStyles.Float, CultureInfo.InvariantCulture, out var x) &&
            float.TryParse(coords[(comma + 1)..].Trim(TrimChars), NumberStyles.Float, CultureInfo.InvariantCulture, out var y))
        {
            return [x, y];
        }

        return null;
    }

    private static int IndexOfTopLevelComma(ReadOnlySpan<char> span)
    {
        var inParen = false;
        for (var i = 0; i < span.Length; i++)
        {
            var c = span[i];
            if (c == '(') inParen = true;
            else if (c == ')') inParen = false;
            else if (c == ',' && !inParen) return i;
        }
        return -1;
    }

    private static string DetermineEventType(string action)
    {
        var lower = action.ToLowerInvariant();
        if (lower.Contains("pass")) return "pass";
        if (lower.Contains("shot")) return "shot";
        if (lower.Contains("duel")) return "duel";
        if (lower.Contains("foul")) return lower.Contains("won") ? "foul won" : "foul committed";
        if (lower.Contains("carry")) return "carry";
        if (lower.Contains("receipt")) return "ball receipt*";
        if (lower.Contains("recovery")) return "ball recovery";
        if (lower.Contains("interception")) return "interception";
        if (lower.Contains("clearance")) return "clearance";
        if (lower.Contains("block")) return "block";
        if (lower.Contains("dribble")) return "dribble";
        if (lower.Contains("save")) return "goal keeper";
        if (lower.Contains("card")) return "card";
        if (lower.Contains("sub")) return "substitution";
        return action;
    }

    private static FootballMatchEvent CreateSystemEvent(
        string action,
        string timestamp,
        int seconds,
        int minute,
        int second,
        int eventIndex,
        string matchId,
        int homeScore,
        int awayScore
    )
    {
        return new FootballMatchEvent
        {
            timestamp = timestamp,
            time_seconds = seconds,
            minute = minute,
            second = second,
            team = "SYSTEM",
            action = action,
            event_type = action,
            position = [0f, 0f],
            event_index = eventIndex,
            match_id = matchId,
            Score = new Score { Home = homeScore, Away = awayScore }
        };
    }
}
