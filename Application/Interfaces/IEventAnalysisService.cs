using Domain.Models;

namespace Application.Interfaces;

public interface IEventAnalysisService
{
    Task UpdateMatchStatistics(FootballMatchEvent matchEvent, MatchEvents matchEventsEntity, Match match);
}

