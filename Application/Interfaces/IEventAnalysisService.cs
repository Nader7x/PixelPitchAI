using Domain.Models;

namespace Application.Interfaces;

public interface IEventAnalysisService
{
    Task<MatchEvents> UpdateMatchStatistics(
        FootballMatchEvent matchEvent,
        MatchEvents matchEventsEntity,
        Match match,
        bool withCounters = true
    );

    Task<Match> UpdateMatchStatistics(FootballMatchEvent matchEvent, Match match);
}
