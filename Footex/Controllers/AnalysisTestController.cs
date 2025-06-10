using Application.Interfaces;
using Domain.Interfaces;
using Domain.Models;
using Infrastructure.Services.EventProcessors;
using Microsoft.AspNetCore.Mvc;

namespace Footex.Controllers;

[Route("api/[controller]")]
public class AnalysisTestController : ControllerBase
{
    private readonly IEventAnalysisService _eventAnalysisService;
    private readonly IEnumerable<IEventProcessor> _eventProcessors;
    private readonly IUnitOfWork _unitOfWork;

    public AnalysisTestController(IEventAnalysisService eventAnalysisService, IEnumerable<IEventProcessor> eventProcessors, IUnitOfWork unitOfWork)
    {
        _eventAnalysisService = eventAnalysisService;
        _eventProcessors = eventProcessors;
        _unitOfWork = unitOfWork;
    }

    [HttpPost("update-match-statistics")]
    public async Task<IActionResult> UpdateMatchStatistics()
    {
        // Check if any event processors are registered
        if (!_eventProcessors.Any())
        {
            return StatusCode(500, "No event processors registered. Check your dependency injection setup.");
        }

        var match = await _unitOfWork.Matches.GetByIdWithDetailsAsync(12);
        // reset match statistics
        if (match == null)
        {
            return NotFound("Match not found.");
        }
        match.ResetStatistics();

        // Load events from the JSON file
        var events = System.Text.Json.JsonSerializer.Deserialize<List<FootballMatchEvent>>(
            await System.IO.File.ReadAllTextAsync("match_12_sim_parsed.json"));
        
        if (events == null || !events.Any())
            return BadRequest("No events found in the file.");

        // Count events by type for debugging
        var eventsByAction = events.GroupBy(e => e.action)
            .Select(g => new { Action = g.Key, Count = g.Count() })
            .ToDictionary(x => x.Action, x => x.Count);

        // Initialize an empty MatchEvents object with just the match ID
        var matchEvents = new MatchEvents
        {
            MatchId = match.Id,
            EventsJson = "[]",
            LastUpdated = DateTime.UtcNow
        };

        int processedEvents = 0;
        int totalEvents = events.Count;
        
        // Process each event one by one
        foreach (var e in events)
        {
            try
            {
                // Check if any processor can process this event
                var processor = _eventProcessors.FirstOrDefault(p => p.CanProcess(e));
                if (processor != null)
                {
                    processedEvents++;
                }
                
                // This will update both the matchEvents and match objects with statistics from this event
                await _eventAnalysisService.UpdateMatchStatistics(e, matchEvents, match);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error processing event {e.event_index}: {ex.Message}");
            }
        }
        // matchEvents.SetEvents(events);
        match.MatchEvents = matchEvents;
        await _unitOfWork.SaveChangesAsync();

        // Add debug info to the response
        var debugInfo = new
        {
            match,
            debug = new
            {
                totalEvents,
                processedEventCount = processedEvents,
                eventsByAction,
                registeredProcessors = _eventProcessors.Select(p => p.GetType().Name).ToList()
            }
        };

        return Ok(debugInfo);
    }
}