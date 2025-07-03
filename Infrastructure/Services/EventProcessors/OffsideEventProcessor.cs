using Domain.Models;

namespace Infrastructure.Services.EventProcessors;

public class OffsideEventProcessor : BaseEventProcessor
{
    public override bool CanProcess(FootballMatchEvent matchEvent)
    {
        return matchEvent.action == "offside"
            || (matchEvent.action == "pass" && matchEvent.outcome == "Pass Offside");
    }

    public override void ProcessMatchEvent(FootballMatchEvent matchEvent, Match match)
    {
        if (IsHomeTeam(matchEvent, match))
            match.HomeTeamOffsides = IncrementValue(match.HomeTeamOffsides);
        else
            match.AwayTeamOffsides = IncrementValue(match.AwayTeamOffsides);
    }

    public override void ProcessEventCounters(
        FootballMatchEvent matchEvent,
        MatchEvents matchEvents,
        Match match
    )
    {
        matchEvents.TotalOffsides++;
    }
}
