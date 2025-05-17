using Domain.Models;

namespace Application.Interfaces;

public interface IEventAnalysisService
{
    Task<string> ProcessEventAsync<T>(T eventData);
    Task UpdateMatchStatistics(FootballMatchEvent matchEvent, MatchEvents matchEventsEntity);
}