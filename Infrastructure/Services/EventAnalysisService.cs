using Application.Interfaces;
using Domain.Models;
using Infrastructure.Services.EventProcessors;

namespace Infrastructure.Services;

public class EventAnalysisService(IEnumerable<IEventProcessor> eventProcessors) : IEventAnalysisService
{
    public async Task<MatchEvents> UpdateMatchStatistics(FootballMatchEvent matchEvent, MatchEvents matchEventsEntity, Match match, bool withCounters = true)
    {
        if (match == null)
            throw new ArgumentNullException(nameof(match), "Match object cannot be null.");
        if (matchEventsEntity == null)
            throw new ArgumentNullException(nameof(matchEventsEntity), "MatchEvents object cannot be null.");

        // First, update the match statistics
        await UpdateMatchStatistics(matchEvent, match);

        // Then, if requested, update the event counters
        if (withCounters)
        {
            // Find the appropriate processor and process the event
            var processor = eventProcessors.FirstOrDefault(p => p.CanProcess(matchEvent));
            processor?.ProcessEventCounters(matchEvent, matchEventsEntity, match);
            
            // Increment the total events counter
            matchEventsEntity.TotalEvents++;
        }
        
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
        CalculatePassAccuracy(match);
        
        // Update match timestamp
        match.UpdatedAt = DateTime.UtcNow;
        
        return Task.FromResult(match);
    }
    
    private static void CalculatePassAccuracy(Match match)
    {
        // Home team pass accuracy
        if (match.HomeTeamPasses.HasValue && match.HomeTeamPassesCompleted.HasValue && match.HomeTeamPasses > 0)
        {
            match.HomeTeamPassAccuracy = Math.Round(
                (double)match.HomeTeamPassesCompleted.Value * 100 / match.HomeTeamPasses.Value, 2);
        }
        else
        {
            match.HomeTeamPassAccuracy = 0;
        }
        
        // Away team pass accuracy (missing in original code, added for completeness)
        if (match.AwayTeamPasses.HasValue && match.AwayTeamPassesCompleted.HasValue && match.AwayTeamPasses > 0)
        {
            match.AwayTeamPassAccuracy = Math.Round(
                (double)match.AwayTeamPassesCompleted.Value * 100 / match.AwayTeamPasses.Value, 2);
        }
        else
        {
            match.AwayTeamPassAccuracy = 0;
        }
    }
}
