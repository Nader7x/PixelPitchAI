using Domain.Models;

namespace Infrastructure.Services.EventProcessors;

/// <summary>
/// Handles events related to ball receipt
/// </summary>
public class BallReceiptEventProcessor : BaseEventProcessor
{
    public override bool CanProcess(FootballMatchEvent matchEvent)
    {
        return matchEvent.action == "ball receipt*";
    }

    public override void ProcessMatchEvent(FootballMatchEvent matchEvent, Match match)
    {
        // Ball receipts don't update any specific match statistic
        // They're primarily used for tracking possession
    }

    public override void ProcessEventCounters(FootballMatchEvent matchEvent, MatchEvents matchEvents, Match match)
    {
        // Track in event counters for completeness
        matchEvents.TotalEvents++;
    }
}
