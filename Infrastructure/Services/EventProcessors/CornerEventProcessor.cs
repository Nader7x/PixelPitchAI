using Domain.Models;

namespace Infrastructure.Services.EventProcessors;

public class CornerEventProcessor : BaseEventProcessor
{
    public override bool CanProcess(FootballMatchEvent matchEvent)
    {
        return matchEvent.action == "pass" && matchEvent.type == "Corner";
    }

    public override void ProcessMatchEvent(FootballMatchEvent matchEvent, Match match)
    {
        // Update corners counter
        if (IsHomeTeam(matchEvent, match))
            match.HomeTeamCorners = IncrementValue(match.HomeTeamCorners);
        else
            match.AwayTeamCorners = IncrementValue(match.AwayTeamCorners);
    }

    public override void ProcessEventCounters(FootballMatchEvent matchEvent, MatchEvents matchEvents, Match match)
    {
        matchEvents.TotalCorners++;
    }
}
