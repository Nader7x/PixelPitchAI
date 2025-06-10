using Domain.Models;

namespace Infrastructure.Services.EventProcessors;

public class SubstitutionEventProcessor : BaseEventProcessor
{
    public override bool CanProcess(FootballMatchEvent matchEvent)
    {
        return matchEvent.action == "substitution" ||
               matchEvent.action == "player on" ||
               matchEvent.action == "player off";
    }

    public override void ProcessMatchEvent(FootballMatchEvent matchEvent, Match match)
    {
        // Substitutions don't have a specific field in the Match model
        // They're tracked in the MatchEvents entity
    }

    public override void ProcessEventCounters(FootballMatchEvent matchEvent, MatchEvents matchEvents, Match match)
    {
        // Only count substitutions once (when the "substitution" action occurs)
        // "player on" and "player off" are part of the same substitution process
        if (matchEvent.action == "substitution")
            matchEvents.TotalSubstitutions++;
    }
}
