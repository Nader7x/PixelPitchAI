using Application.Interfaces;
using Domain.Models;
using Infrastructure.Services.EventProcessors;

namespace Infrastructure.Services;

public class EventAnalysisService(IEnumerable<IEventProcessor> eventProcessors)
    : IEventAnalysisService
{
    public async Task<MatchEvents> UpdateMatchStatistics(
        FootballMatchEvent matchEvent,
        MatchEvents matchEventsEntity,
        Match match,
        bool withCounters = true
    )
    {
        if (match == null)
            throw new ArgumentNullException(nameof(match), "Match object cannot be null.");
        if (matchEventsEntity == null)
            throw new ArgumentNullException(
                nameof(matchEventsEntity),
                "MatchEvents object cannot be null."
            );

        await UpdateMatchStatistics(matchEvent, match);

        if (!withCounters)
            return matchEventsEntity;
        var processor = eventProcessors.FirstOrDefault(p => p.CanProcess(matchEvent));
        processor?.ProcessEventCounters(matchEvent, matchEventsEntity, match);

        matchEventsEntity.TotalEvents++;

        return matchEventsEntity;
    }

    public Task<Match> UpdateMatchStatistics(FootballMatchEvent matchEvent, Match match)
    {
        if (match == null)
            throw new ArgumentNullException(nameof(match), "Match object cannot be null.");

        // Find the appropriate processor and process the event
        var processor = eventProcessors.FirstOrDefault(p => p.CanProcess(matchEvent));
        processor?.ProcessMatchEvent(matchEvent, match);

        // Update possession statistics
        PossessionCalculator.UpdatePossession(match, matchEvent);

        // Calculate pass accuracy
        if (match.MatchStatistics != null)
            CalculatePassAccuracy(match.MatchStatistics);

        // Update match timestamp
        match.UpdatedAt = DateTime.UtcNow;

        return Task.FromResult(match);
    }

    private static void CalculatePassAccuracy(MatchStatistics matchStatistics)
    {
        if (matchStatistics is { HomeTeamPasses: > 0, HomeTeamPassesCompleted: not null })
            matchStatistics.HomeTeamPassAccuracy = Math.Round(
                (double)matchStatistics.HomeTeamPassesCompleted.Value
                    * 100
                    / matchStatistics.HomeTeamPasses.Value,
                2
            );
        else
            matchStatistics.HomeTeamPassAccuracy = 0;

        if (matchStatistics is { AwayTeamPasses: > 0, AwayTeamPassesCompleted: not null })
            matchStatistics.AwayTeamPassAccuracy = Math.Round(
                (double)matchStatistics.AwayTeamPassesCompleted.Value
                    * 100
                    / matchStatistics.AwayTeamPasses.Value,
                2
            );
        else
            matchStatistics.AwayTeamPassAccuracy = 0;

        // Calculate Home Long Balls Accuracy
        if (matchStatistics is { HomeLongBalls: > 0, HomeAccurateLongBalls: not null })
            matchStatistics.HomeTeamLongBallsAccuracy = Math.Round(
                (double)matchStatistics.HomeAccurateLongBalls.Value
                    * 100
                    / matchStatistics.HomeLongBalls.Value,
                2
            );
        else
            matchStatistics.HomeTeamLongBallsAccuracy = 0;
        // Calculate Away Long Balls Accuracy
        if (matchStatistics is { AwayLongBalls: > 0, AwayAccurateLongBalls: not null })
            matchStatistics.AwayTeamLongBallsAccuracy = Math.Round(
                (double)matchStatistics.AwayAccurateLongBalls.Value
                    * 100
                    / matchStatistics.AwayLongBalls.Value,
                2
            );
        else
            matchStatistics.AwayTeamLongBallsAccuracy = 0;
    }
}
