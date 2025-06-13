using Application.Interfaces;
using Application.Services;
using Domain.Interfaces;
using Domain.Models;
using Infrastructure.Services.EventProcessors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace Footex.Controllers;

[Route("api/[controller]")]
public class AnalysisTestController : ControllerBase
{
    private readonly IEventAnalysisService _eventAnalysisService;
    private readonly IEnumerable<IEventProcessor> _eventProcessors;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IHubContext<MatchHub, IMatchHub> _hubContext;


    public AnalysisTestController(IEventAnalysisService eventAnalysisService, IEnumerable<IEventProcessor> eventProcessors, IUnitOfWork unitOfWork, IHubContext<MatchHub, IMatchHub> hubContext)
    {
        _eventAnalysisService = eventAnalysisService;
        _eventProcessors = eventProcessors;
        _unitOfWork = unitOfWork;
        _hubContext = hubContext;
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
        match.IsLive = true;
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
    [HttpGet("TestStatisticsBroadcasting")]
    public async Task<IActionResult> TestStatisticsBroadcasting()
    {
        // Simulate a match update
        var match = await _unitOfWork.Matches.GetByIdWithDetailsAsync(12);
        if (match == null)
        {
            return NotFound("Match not found.");
        }
        

        // Broadcast the updated statistics to all connected clients
        var matchStatistics = new
            {
                matchId = match.Id,
                timeStamp = "65:00",
                homeTeam = new
                {
                    name = match.HomeTeam?.Name ?? match.HomeTeamInMatchName,
                    score = match.HomeTeamScore ?? 0,
                    shots = match.HomeTeamShots ?? 0,
                    shotsOnTarget = match.HomeTeamShotsOnTarget ?? 0,
                    possession = match.HomeTeamPossession ?? 0,
                    passes = match.HomeTeamPasses ?? 0,
                    passAccuracy = match.HomeTeamPassAccuracy ?? 0,
                    corners = match.HomeTeamCorners ?? 0,
                    fouls = match.HomeTeamFouls ?? 0,
                    yellowCards = match.HomeTeamYellowCards ?? 0,
                    redCards = match.HomeTeamRedCards ?? 0,
                    offsides = match.HomeTeamOffsides ?? 0
                },
                awayTeam = new
                {
                    name = match.AwayTeam?.Name ?? match.AwayTeamInMatchName,
                    score = match.AwayTeamScore ?? 0,
                    shots = match.AwayTeamShots ?? 0,
                    shotsOnTarget = match.AwayTeamShotsOnTarget ?? 0,
                    possession = match.AwayTeamPossession ?? 0,
                    passes = match.AwayTeamPasses ?? 0,
                    passAccuracy = match.AwayTeamPassAccuracy ?? 0,
                    corners = match.AwayTeamCorners ?? 0,
                    fouls = match.AwayTeamFouls ?? 0,
                    yellowCards = match.AwayTeamYellowCards ?? 0,
                    redCards = match.AwayTeamRedCards ?? 0,
                    offsides = match.AwayTeamOffsides ?? 0
                },
                matchInfo = new
                {
                    status = match.MatchStatus,
                    isLive = match.IsLive,
                    currentMinute = 65, // Assuming the match is in the 90th minute
                    lastEventTime = 65, // Assuming no events have occurred yet
                    eventType = "Goal", // Example event type
                    eventTeam = match.HomeTeamInMatchName // Example team
                },
                lastUpdated = DateTime.UtcNow
            };
            await _hubContext.Clients.Group($"MatchStatistics-{match.Id.ToString()}")
                .SendMatchStatisticsAsync("match_statistics_update", match.Id,
                    matchStatistics);

        return Ok("Match statistics broadcasted successfully.");
    }
    [HttpPost("ResetMatchStatistics")]
    public async Task<IActionResult> ResetMatchStatistics()
    {
        var match = await _unitOfWork.Matches.GetByIdWithDetailsAsync(12);
        if (match == null)
        {
            return NotFound("Match not found.");
        }

        // Reset match statistics
        match.ResetStatistics();
        await _unitOfWork.SaveChangesAsync();

        return Ok("Match statistics reset successfully.");
    }
}