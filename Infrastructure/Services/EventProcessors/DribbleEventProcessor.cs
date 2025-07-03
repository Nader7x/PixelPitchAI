using Domain.Models;

namespace Infrastructure.Services.EventProcessors;

public class DribbleEventProcessor : BaseEventProcessor
{
    public override bool CanProcess(FootballMatchEvent matchEvent)
    {
        return matchEvent.action == "dribble" || matchEvent.action == "carry";
    }

    public override void ProcessMatchEvent(FootballMatchEvent matchEvent, Match match)
    {
        // Both dribbles and carries are tracked as dribbles in match statistics
        if (IsHomeTeam(matchEvent, match))
            match.HomeTeamDribbles = IncrementValue(match.HomeTeamDribbles);
        else
            match.AwayTeamDribbles = IncrementValue(match.AwayTeamDribbles);
    }

    public override void ProcessEventCounters(
        FootballMatchEvent matchEvent,
        MatchEvents matchEvents,
        Match match
    )
    {
        matchEvents.TotalDribbles++;
    }
}
