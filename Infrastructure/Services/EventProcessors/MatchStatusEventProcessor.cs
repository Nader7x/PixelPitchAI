using Domain.Models;

namespace Infrastructure.Services.EventProcessors;

public class MatchStatusEventProcessor : BaseEventProcessor
{
    public override bool CanProcess(FootballMatchEvent matchEvent)
    {
        return matchEvent.action == "match_start" ||
               matchEvent.action == "match_end" ||
               matchEvent.action == "first_half_end" ||
               matchEvent.action == "second_half_start";
    }

    public override void ProcessMatchEvent(FootballMatchEvent matchEvent, Match match)
    {
        // Update match status based on the event
        switch (matchEvent.action)
        {
            case "match_start":
                match.MatchStatus = "In Progress";
                match.IsLive = true;
                break;
                
            case "match_end":
                match.MatchStatus = "Completed";
                match.IsLive = false;
                break;
                
            case "first_half_end":
                // Half time - match is still in progress but noting the half time
                match.MatchStatus = "Half Time";
                match.IsLive = true;
                break;
                
            case "second_half_start":
                // Second half - match continues
                match.MatchStatus = "In Progress";
                match.IsLive = true;
                break;
        }
    }

    public override void ProcessEventCounters(FootballMatchEvent matchEvent, MatchEvents matchEvents, Match match)
    {
        // These are system events that don't count toward statistical totals
        // but we still increment the total events counter
        matchEvents.TotalEvents++;
    }
}
