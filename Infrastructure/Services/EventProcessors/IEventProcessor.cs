using Domain.Models;

namespace Infrastructure.Services.EventProcessors;

/// <summary>
/// Interface for event processors that handle different types of match events
/// </summary>
public interface IEventProcessor
{
    /// <summary>
    /// Determines if this processor can handle the specified match event
    /// </summary>
    bool CanProcess(FootballMatchEvent matchEvent);
    
    /// <summary>
    /// Process the match event and update match statistics
    /// </summary>
    void ProcessMatchEvent(FootballMatchEvent matchEvent, Match match);
    
    /// <summary>
    /// Process the match event and update match events counters
    /// </summary>
    void ProcessEventCounters(FootballMatchEvent matchEvent, MatchEvents matchEvents, Match match);
}
