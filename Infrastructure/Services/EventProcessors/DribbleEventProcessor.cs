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
        {
            if (match.MatchStatistics != null)
                match.MatchStatistics.HomeTeamDribbles = IncrementValue(
                    match.MatchStatistics.HomeTeamDribbles
                );
        }
        else if (match.MatchStatistics != null)
        {
            match.MatchStatistics.AwayTeamDribbles = IncrementValue(
                match.MatchStatistics.AwayTeamDribbles
            );
        }
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
