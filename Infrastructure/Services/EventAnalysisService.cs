using Application.Interfaces;
using Domain.Interfaces;
using Domain.Models;

namespace Infrastructure.Services;

public class EventAnalysisService(IUnitOfWork unitOfWork) : IEventAnalysisService
{
    public async Task<string> ProcessEventAsync<T>(T eventData)
    {
        // Simulate some processing logic
        await Task.Delay(1000); // Simulate async work
        return "Event processed successfully";
    }

    public async Task UpdateMatchStatistics(FootballMatchEvent matchEvent, MatchEvents matchEventsEntity)
    {
        var match = matchEventsEntity.Match;
        if (match == null)
        {
            match = await unitOfWork.Matches.GetByIdAsync(matchEvent.match_id);
            if (match == null) return;
        }

        // Update basic event counters
        switch (matchEvent.event_type)
        {
            case "shot":
                if (matchEvent.team == match.HomeTeam.Name)
                    match.HomeTeamShots = (match.HomeTeamShots ?? 0) + 1;
                else
                    match.AwayTeamShots = (match.AwayTeamShots ?? 0) + 1;

                // Track shots on target
                if (matchEvent.outcome == "Goal" || matchEvent.outcome == "Saved")
                {
                    if (matchEvent.team == match.HomeTeam.Name)
                        match.HomeTeamShotsOnTarget = (match.HomeTeamShotsOnTarget ?? 0) + 1;
                    else
                        match.AwayTeamShotsOnTarget = (match.AwayTeamShotsOnTarget ?? 0) + 1;
                }

                break;

            case "goal":
                // Update score
                if (matchEvent.team == match.HomeTeam.Name)
                {
                    match.HomeTeamScore = (match.HomeTeamScore ?? 0) + 1;
                    matchEventsEntity.GoalsHomeTeam++;
                }
                else
                {
                    match.AwayTeamScore = (match.AwayTeamScore ?? 0) + 1;
                    matchEventsEntity.GoalsAwayTeam++;
                }

                // Update match result data
                UpdateMatchResult(match);
                break;

            case "pass":
                matchEventsEntity.TotalPasses++;
                break;

            case "foul committed":
                matchEventsEntity.TotalFouls++;
                if (matchEvent.team == match.HomeTeam.Name)
                    match.HomeTeamFouls = (match.HomeTeamFouls ?? 0) + 1;
                else
                    match.AwayTeamFouls = (match.AwayTeamFouls ?? 0) + 1;

                // Check for cards
                matchEventsEntity.TotalCards++;
                if (matchEvent.card == "Yellow Card")
                {
                    if (matchEvent.team == match.HomeTeam.Name)
                        match.HomeTeamYellowCards = (match.HomeTeamYellowCards ?? 0) + 1;
                    else
                        match.AwayTeamYellowCards = (match.AwayTeamYellowCards ?? 0) + 1;
                }
                else if (matchEvent.card == "Red Card")
                {
                    if (matchEvent.team == match.HomeTeam.Name)
                        match.HomeTeamRedCards = (match.HomeTeamRedCards ?? 0) + 1;
                    else
                        match.AwayTeamRedCards = (match.AwayTeamRedCards ?? 0) + 1;
                }

                break;
        }

        // Update the match's last updated timestamp
        match.UpdatedAt = DateTime.UtcNow;
    }

    private void UpdateMatchResult(Match match)
    {
        // Only update winner/loser if we have scores
        if (match.HomeTeamScore.HasValue && match.AwayTeamScore.HasValue)
        {
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


}