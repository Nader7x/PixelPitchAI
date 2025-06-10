using Application.Interfaces;
using Domain.Models;

namespace Infrastructure.Services;

public class OldEAS : IEventAnalysisService
{    public Task<MatchEvents> UpdateMatchStatistics(FootballMatchEvent matchEvent, MatchEvents matchEventsEntity, Match match , bool withCounters = true)
    {
        // Match object is now passed as a parameter, no need to fetch it here.
        if (match == null)
            throw new ArgumentNullException(nameof(match), "Match object cannot be null.");
        if (matchEventsEntity == null)
            throw new ArgumentNullException(nameof(matchEventsEntity), "MatchEvents object cannot be null.");

        // First, update the match statistics using the shared logic
        UpdateMatchStatistics(matchEvent, match);

        // Then, update the MatchEvents entity specific counters
        if (withCounters)
            UpdateMatchEventsCounters(matchEvent, matchEventsEntity, match);
        
        return (Task<MatchEvents>)Task.CompletedTask;
    }

    /// <summary>
    /// Updates the MatchEvents entity counters based on the match event.
    /// This method is called by the full UpdateMatchStatistics overload.
    /// </summary>
    private static void UpdateMatchEventsCounters(FootballMatchEvent matchEvent, MatchEvents matchEventsEntity, Match match)
    {
        // Update basic event counters specific to MatchEvents entity
        switch (matchEvent.action)
        {
            case "shot":
                matchEventsEntity.TotalShots++;
                if (matchEvent.type == "Free Kick")
                {
                    matchEventsEntity.TotalFreeKicks++;
                }

                switch (matchEvent.outcome)
                {
                    case "Blocked":
                        matchEventsEntity.TotalBlocks++;
                        break;
                    case "Goal":
                        matchEventsEntity.TotalGoals++;
                        if (matchEvent.team == match.HomeTeamInMatchName)
                            matchEventsEntity.GoalsHomeTeam++;
                        else
                            matchEventsEntity.GoalsAwayTeam++;
                        break;
                    case "Saved":
                        matchEventsEntity.TotalGoalkeeperSaves++;
                        break;
                }
                break;

            case "pass":
                matchEventsEntity.TotalPasses++;
                switch (matchEvent.type)
                {
                    case "Free Kick":
                        matchEventsEntity.TotalFreeKicks++;
                        break;
                    case "Goal Kick":
                        matchEventsEntity.TotalGoalKicks++;
                        break;
                    case "Interception":
                        matchEventsEntity.TotalInterceptions++;
                        break;
                    case "Kick Off":
                        matchEventsEntity.TotalPasses++;
                        break;
                    case "Recovery":
                        matchEventsEntity.TotalPossessionWon++;
                        break;
                    case "Throw-in":
                        matchEventsEntity.TotalThrowIns++;
                        break;
                }

                switch (matchEvent.outcome)
                {
                    case "Out":
                        matchEventsEntity.TotalOuts++;
                        break;
                    case "Pass Offside":
                        matchEventsEntity.TotalOffsides++;
                        break;
                }
                break;

            case "foul committed":
                matchEventsEntity.TotalFouls++;
                if (matchEvent.outcome is "Penalty" or "penalty")
                {
                    matchEventsEntity.TotalPenalties++;
                }

                if (matchEvent.card != "No Card")
                {
                    matchEventsEntity.TotalCards++;
                    switch (matchEvent.card)
                    {
                        case "Yellow Card":
                            matchEventsEntity.TotalYellowCards++;
                            break;
                        case "Red Card":
                            matchEventsEntity.TotalRedCards++;
                            break;
                    }
                }
                break;

            case "Offside":
                matchEventsEntity.TotalOffsides++;
                break;
            case "Corner":
                matchEventsEntity.TotalCorners++;
                break;
            case "substitution":
                matchEventsEntity.TotalSubstitutions++;
                break;
            case "injury stoppage":
                matchEventsEntity.TotalInjuries++;
                break;
            case "own goal against":
                matchEventsEntity.TotalGoals++;
                if (matchEvent.team == match.HomeTeamInMatchName)
                    matchEventsEntity.GoalsAwayTeam++;
                else
                    matchEventsEntity.GoalsHomeTeam++;
                break;
            case "Save":
                matchEventsEntity.TotalGoalkeeperSaves++;
                break;
            case "interception":
                matchEventsEntity.TotalInterceptions++;
                break;
            case "block":
                matchEventsEntity.TotalBlocks++;
                break;
            case "clearance":
                matchEventsEntity.TotalClearances++;
                break;
            case "carry":
            case "dribble":
                matchEventsEntity.TotalDribbles++;
                break;
            case "duel":
            case "50/50":
                matchEventsEntity.TotalDuels++;
                break;
            case "shield":
                matchEventsEntity.TotalDuels++;
                break;
            case "ball recovery":
                matchEventsEntity.TotalPossessionWon++;
                break;
            case "miscontrol":
                matchEventsEntity.TotalOuts++;
                break;
            case "error":
                matchEventsEntity.TotalErrors++;
                break;
            case "foul won":
                matchEventsEntity.TotalFreeKicks++;
                break;
            case "bad behaviour":
                if (matchEvent.card != null && matchEvent.card != "No Card")
                {
                    matchEventsEntity.TotalCards++;
                    if (matchEvent.card == "Yellow Card")
                    {
                        matchEventsEntity.TotalYellowCards++;
                    }
                    else if (matchEvent.card == "Red Card")
                    {
                        matchEventsEntity.TotalRedCards++;
                    }
                }
                break;
        }

        matchEventsEntity.TotalEvents++;
    }
    

     public Task<Match> UpdateMatchStatistics(FootballMatchEvent matchEvent, Match match)
    {
        // Match object is now passed as a parameter, no need to fetch it here.
        if (match == null)
            throw new ArgumentNullException(nameof(match), "Match object cannot be null.");

        // Update basic event counters
        switch (matchEvent.action)
        {
            case "shot":
                if (matchEvent.type == "Free Kick")
                {
                    if (matchEvent.team == match.HomeTeamInMatchName)
                        match.HomeTeamFreeKicks = (match.HomeTeamFreeKicks ?? 0) + 1;
                    else
                        match.AwayTeamFreeKicks = (match.AwayTeamFreeKicks ?? 0) + 1;
                }

                if (matchEvent.team == match.HomeTeamInMatchName)
                    match.HomeTeamShots = (match.HomeTeamShots ?? 0) + 1;
                else
                    match.AwayTeamShots = (match.AwayTeamShots ?? 0) + 1;

                switch (matchEvent.outcome)
                {
                    case "Blocked":
                        break;
                    case "Goal":
                        if (matchEvent.team == match.HomeTeamInMatchName)
                        {
                            match.HomeTeamShotsOnTarget = (match.HomeTeamShotsOnTarget ?? 0) + 1;
                            match.HomeTeamScore = (match.HomeTeamScore ?? 0) + 1; // Score update
                        }
                        else
                        {
                            match.AwayTeamShotsOnTarget = (match.AwayTeamShotsOnTarget ?? 0) + 1;
                            match.AwayTeamScore = (match.AwayTeamScore ?? 0) + 1; // Score update
                        }

                        UpdateMatchResult(match);
                        break;
                    case "Wayward":
                    case "Off T":
                        if (matchEvent.team == match.AwayTeamInMatchName)
                        {
                            match.AwayTeamShotsOffTarget = (match.AwayTeamShotsOffTarget ?? 0) + 1;
                        }
                        else
                        {
                            match.HomeTeamShotsOffTarget = (match.HomeTeamShotsOffTarget ?? 0) + 1;
                        }

                        break;
                    case "Post":
                        break;
                    case "Saved":
                        if (matchEvent.team == match.HomeTeamInMatchName)
                        {
                            match.HomeTeamShotsOnTarget = (match.HomeTeamShotsOnTarget ?? 0) + 1;
                            match.AwayTeamSaves = (match.AwayTeamSaves ?? 0) + 1;
                        }
                        else
                        {
                            match.AwayTeamShotsOnTarget = (match.AwayTeamShotsOnTarget ?? 0) + 1;
                            match.HomeTeamSaves = (match.HomeTeamSaves ?? 0) + 1;
                        }

                        break;
                    case "Saved Off Target":
                        break;
                }

                break;

            case "pass":
                if (matchEvent.team == match.HomeTeamInMatchName)
                    match.HomeTeamPasses = (match.HomeTeamPasses ?? 0) + 1;
                else
                    match.AwayTeamPasses = (match.AwayTeamPasses ?? 0) + 1;

                switch (matchEvent.type)
                {
                    case "Free Kick":
                        if (matchEvent.team == match.HomeTeamInMatchName)
                            match.HomeTeamFreeKicks = (match.HomeTeamFreeKicks ?? 0) + 1;
                        else
                            match.AwayTeamFreeKicks = (match.AwayTeamFreeKicks ?? 0) + 1;
                        break;
                    case "Goal Kick":
                    {
                        if (matchEvent.team == match.HomeTeamInMatchName)
                            match.HomeTeamGoalKicks = (match.HomeTeamGoalKicks ?? 0) + 1;
                        else
                            match.AwayTeamGoalKicks = (match.AwayTeamGoalKicks ?? 0) + 1;
                        break;
                    }
                    case "Interception":
                    case "Kick Off":
                    case "Recovery":
                    case "Throw-in":
                        break;
                }

                switch (matchEvent.outcome)
                {
                    case "Out":
                        break;
                    case "Pass Offside":
                        if (matchEvent.team == match.HomeTeamInMatchName)
                            match.HomeTeamOffsides = (match.HomeTeamOffsides ?? 0) + 1;
                        else
                            match.AwayTeamOffsides = (match.AwayTeamOffsides ?? 0) + 1;
                        break;
                    case "Complete":
                        if (matchEvent.team == match.HomeTeamInMatchName)
                        {
                            match.HomeTeamPassesCompleted = (match.HomeTeamPassesCompleted ?? 0) + 1;
                            if (matchEvent.long_pass == true)
                            {
                                match.HomeAccurateLongBalls = (match.HomeAccurateLongBalls ?? 0) + 1;
                            }
                        }
                        else
                        {
                            match.AwayTeamPassesCompleted = (match.AwayTeamPassesCompleted ?? 0) + 1;
                            if (matchEvent.long_pass == true)
                            {
                                match.AwayAccurateLongBalls = (match.AwayAccurateLongBalls ?? 0) + 1;
                            }
                        }

                        break;
                }

                if (matchEvent.long_pass is true)
                {
                    if (matchEvent.team == match.HomeTeamInMatchName)
                    {
                        match.HomeLongBalls = (match.HomeLongBalls ?? 0) + 1;
                    }
                    else
                        match.AwayLongBalls = (match.AwayLongBalls ?? 0) + 1;
                }

                break;

            case "foul committed":
                if (matchEvent.team == match.HomeTeamInMatchName)
                    match.HomeTeamFouls = (match.HomeTeamFouls ?? 0) + 1;
                else
                    match.AwayTeamFouls = (match.AwayTeamFouls ?? 0) + 1;

                if (matchEvent.card != "No Card")
                {
                    switch (matchEvent.card)
                    {
                        case "Yellow Card":
                        {
                            if (matchEvent.team == match.HomeTeamInMatchName)
                                match.HomeTeamYellowCards = (match.HomeTeamYellowCards ?? 0) + 1;
                            else
                                match.AwayTeamYellowCards = (match.AwayTeamYellowCards ?? 0) + 1;
                            break;
                        }
                        case "Red Card":
                        {
                            if (matchEvent.team == match.HomeTeamInMatchName)
                                match.HomeTeamRedCards = (match.HomeTeamRedCards ?? 0) + 1;
                            else
                                match.AwayTeamRedCards = (match.AwayTeamRedCards ?? 0) + 1;
                            break;
                        }
                    }
                }

                break;
            case "Offside":
                if (matchEvent.team == match.HomeTeamInMatchName)
                    match.HomeTeamOffsides = (match.HomeTeamOffsides ?? 0) + 1;
                else
                    match.AwayTeamOffsides = (match.AwayTeamOffsides ?? 0) + 1;
                break;
            case "Corner":
                if (matchEvent.team == match.HomeTeamInMatchName)
                    match.HomeTeamCorners = (match.HomeTeamCorners ?? 0) + 1;
                else
                    match.AwayTeamCorners = (match.AwayTeamCorners ?? 0) + 1;
                break;
            case "substitution":
            case "injury stoppage":
                break;
            case "own goal against":
                if (matchEvent.team == match.HomeTeamInMatchName)
                {
                    match.AwayTeamScore = (match.AwayTeamScore ?? 0) + 1;
                }
                else
                {
                    match.HomeTeamScore = (match.HomeTeamScore ?? 0) + 1;
                }

                UpdateMatchResult(match);
                break;
            case "Save":
                if (matchEvent.team == match.HomeTeamInMatchName)
                    match.HomeTeamSaves = (match.HomeTeamSaves ?? 0) + 1;
                else
                    match.AwayTeamSaves = (match.AwayTeamSaves ?? 0) + 1;
                break;
            case "interception":
                if (matchEvent.team == match.HomeTeamInMatchName)
                    match.HomeTeamPossessionWon = (match.HomeTeamPossessionWon ?? 0) + 1;
                else
                    match.AwayTeamPossessionWon = (match.AwayTeamPossessionWon ?? 0) + 1;
                break;
            case "block":
                break;
            case "clearance":
                if (matchEvent.team == match.HomeTeamInMatchName)
                    match.HomeTeamClearances = (match.HomeTeamClearances ?? 0) + 1;
                else
                    match.AwayTeamClearances = (match.AwayTeamClearances ?? 0) + 1;
                break;
            case "carry":
            case "dribble":
                if (matchEvent.team == match.HomeTeamInMatchName)
                    match.HomeTeamDribbles = (match.HomeTeamDribbles ?? 0) + 1;
                else
                    match.AwayTeamDribbles = (match.AwayTeamDribbles ?? 0) + 1;
                break;
            case "duel":
            case "50/50":
                if (matchEvent.team == match.HomeTeamInMatchName)
                {
                    match.HomeTeamDuels = (match.HomeTeamDuels ?? 0) + 1;
                }
                else
                {
                    match.AwayTeamDuels = (match.AwayTeamDuels ?? 0) + 1;
                }

                if (matchEvent.outcome == "won")
                {
                    if (matchEvent.team == match.AwayTeamInMatchName)
                    {
                        match.AwayTeamDuelsWon = (match.AwayTeamDuelsWon ?? 0) + 1;
                    }
                    else
                    {
                        match.HomeTeamDuelsWon = (match.HomeTeamDuelsWon ?? 0) + 1;
                    }
                }

                break;
            case "shield":
                if (matchEvent.team == match.HomeTeamInMatchName)
                    match.HomeTeamDuels = (match.HomeTeamDuels ?? 0) + 1;
                else
                    match.AwayTeamDuels = (match.AwayTeamDuels ?? 0) + 1;
                break;
            case "ball recovery":
                if (matchEvent.team == match.HomeTeamInMatchName)
                {
                    match.HomeTeamPossessionWon = (match.HomeTeamPossessionWon ?? 0) + 1;
                    match.HomeTeamRecoveries = (match.HomeTeamRecoveries ?? 0) + 1;
                }
                else
                {
                    match.AwayTeamPossessionWon = (match.AwayTeamPossessionWon ?? 0) + 1;
                    match.AwayTeamRecoveries = (match.AwayTeamRecoveries ?? 0) + 1;
                }

                break;
            case "miscontrol":
            case "error":
            case "foul won":
                break;
            case "bad behaviour":
                if (matchEvent.card != "No Card")
                {
                    switch (matchEvent.card)
                    {
                        case "Yellow Card":
                        {
                            if (matchEvent.team == match.HomeTeamInMatchName)
                                match.HomeTeamYellowCards = (match.HomeTeamYellowCards ?? 0) + 1;
                            else
                                match.AwayTeamYellowCards = (match.AwayTeamYellowCards ?? 0) + 1;
                            break;
                        }
                        case "Red Card":
                        {
                            if (matchEvent.team == match.HomeTeamInMatchName)
                                match.HomeTeamRedCards = (match.HomeTeamRedCards ?? 0) + 1;
                            else
                                match.AwayTeamRedCards = (match.AwayTeamRedCards ?? 0) + 1;
                            break;
                        }
                    }
                }

                break;
            case "player on":
            case "player off":
                break;
        }

        match.UpdatedAt = DateTime.UtcNow;

        UpdatePossession(match, matchEvent);
        // Calculate pass accuracy
        if (match.HomeTeamPasses.HasValue && match.HomeTeamPassesCompleted.HasValue &&
            match.HomeTeamPasses > 0)
        {
            match.HomeTeamPassAccuracy = Math.Round(
                (double)match.HomeTeamPassesCompleted.Value * 100 / match.HomeTeamPasses.Value, 2);
        }
        else
        {
            match.HomeTeamPassAccuracy = 0;
        }
        
        return (Task<Match>)Task.CompletedTask;
    }

    private static void UpdatePossession(Match match, FootballMatchEvent currentEvent)
    {
        if (match.LastEventTimestampSeconds.HasValue && match.LastEventPossessingTeamName != null)
        {
            var durationSeconds = currentEvent.time_seconds - match.LastEventTimestampSeconds.Value;
            if (durationSeconds > 0)
            {
                if (match.LastEventPossessingTeamName == match.HomeTeamInMatchName)
                {
                    match.HomeTeamPossessionDurationSeconds =
                        (match.HomeTeamPossessionDurationSeconds ?? 0) + durationSeconds;
                }
                else if (match.LastEventPossessingTeamName == match.AwayTeamInMatchName)
                {
                    match.AwayTeamPossessionDurationSeconds =
                        (match.AwayTeamPossessionDurationSeconds ?? 0) + durationSeconds;
                }
            }
        }

        // Determine possessing team after the current event
        // This logic assumes that the team performing an action maintains possession unless the action implies a loss of possession
        // or the opponent performs a possession-gaining action.
        var currentPossessingTeam = currentEvent.team; // Default to the team performing the action

        switch (currentEvent.action)
        {
            case "pass":
                currentPossessingTeam = currentEvent.outcome == "Complete"
                    ? currentEvent.team
                    : GetOpponentTeam(currentEvent.team, match);
                break;
            case "shot":
                // If shot is saved or goes out, opponent (usually GK) gets possession for goal kick/throw-in
                if (currentEvent.outcome is "Saved" or "Out" or "Blocked" or "Post" or "Wayward" or "Off T")
                    currentPossessingTeam = GetOpponentTeam(currentEvent.team, match);
                // If it's a goal, possession resets (usually to the team that conceded for kickoff)
                else if (currentEvent.outcome == "Goal")
                    currentPossessingTeam = GetOpponentTeam(currentEvent.team, match);
                break;
            case "interception":
            case "ball recovery":
            case "Save": // Goalkeeper save
                currentPossessingTeam = currentEvent.team; // Team performing these actions gains/retains possession
                break;
            case "foul committed": // Team that committed the foul loses possession
                currentPossessingTeam = GetOpponentTeam(currentEvent.team, match);
                break;
            case "dispossessed":
            case "miscontrol":
            case "error": // if error leads to turnover
                currentPossessingTeam = GetOpponentTeam(currentEvent.team, match);
                break;
            case "clearance":
                // Possession after clearance is ambiguous without more context (e.g., where the clearance went)
                // For simplicity, assume opponent might gain possession or it becomes contested.
                // Can be refined if more data is available (e.g. clearance outcome)
                currentPossessingTeam =
                    null; // Or GetOpponentTeam(currentEvent.team, match) if clearance usually results in opponent possession
                break;
            case "duel":
            case "50/50":
                // Outcome of duel determines possession. If not specified, it's ambiguous.
                // For now, let's assume the event.team is the one who *won* or initiated and retained.
                // This needs refinement based on how duel outcomes are logged.
                if (currentEvent.outcome == "success" || currentEvent.outcome == "won") // Hypothetical outcomes
                    currentPossessingTeam = currentEvent.team;
                else if (currentEvent.outcome == "lost")
                    currentPossessingTeam = GetOpponentTeam(currentEvent.team, match);
                else
                    currentPossessingTeam = null; // Ambiguous
                break;
                // Actions like "own goal against", "bad behaviour", "player on/off", "substitution" don't directly change possession in a standard way here
                // Meta events like match_start, half_end also don't change possession in this context.
        }

        match.LastEventTimestampSeconds = currentEvent.time_seconds;
        match.LastEventPossessingTeamName = currentPossessingTeam;

        // After processing all events, or periodically, calculate percentage
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