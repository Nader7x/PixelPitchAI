using Domain.Models;

namespace Infrastructure.Services.EventProcessors;

/// <summary>
///     Base class for event processors providing common functionality
/// </summary>
public abstract class BaseEventProcessor : IEventProcessor
{
    public abstract bool CanProcess(FootballMatchEvent matchEvent);
    public abstract void ProcessMatchEvent(FootballMatchEvent matchEvent, Match match);
    public abstract void ProcessEventCounters(FootballMatchEvent matchEvent, MatchEvents matchEvents, Match match);

    /// <summary>
    ///     Helper method to safely increment a nullable int value
    /// </summary>
    protected static int IncrementValue(int? currentValue)
    {
        return (currentValue ?? 0) + 1;
    }

    /// <summary>
    ///     Determines if the event is from the home team
    /// </summary>
    protected static bool IsHomeTeam(FootballMatchEvent matchEvent, Match match)
    {
        return matchEvent.team == match.HomeTeamInMatchName;
    }

    /// <summary>
    ///     Gets the opposing team name
    /// </summary>
    protected static string? GetOpponentTeam(string currentTeamName, Match match)
    {
        if (currentTeamName == match.HomeTeamInMatchName)
            return match.AwayTeamInMatchName;
        if (currentTeamName == match.AwayTeamInMatchName)
            return match.HomeTeamInMatchName;
        return null;
    }
}