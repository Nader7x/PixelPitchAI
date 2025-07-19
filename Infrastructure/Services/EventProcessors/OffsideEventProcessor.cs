using Domain.Models;

namespace Infrastructure.Services.EventProcessors;

public class OffsideEventProcessor : BaseEventProcessor
{
    public override bool CanProcess(FootballMatchEvent matchEvent)
    {
        return matchEvent.action == "offside"
            || matchEvent is { action: "pass", outcome: "Pass Offside" };
    }

    public override void ProcessMatchEvent(FootballMatchEvent matchEvent, Match match)
    {
        if (IsHomeTeam(matchEvent, match))
        {
            if (match.MatchStatistics != null)
                match.MatchStatistics.HomeTeamOffsides = IncrementValue(
                    match.MatchStatistics.HomeTeamOffsides
                );
        }
        else if (match.MatchStatistics != null)
        {
            match.MatchStatistics.AwayTeamOffsides = IncrementValue(
                match.MatchStatistics.AwayTeamOffsides
            );
        }
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
