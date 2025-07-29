import { useEffect, useState } from 'react';
import { Event, EventType } from '@/types/Event';
import signalRService, { MatchEventData } from '@/Services/SignalRService';
import { storeTeamNames, storeScores, storeMatchTime } from '@/lib/teamStorage';

export default function useSignalREventStream(matchId: number) {
  const [streamedEvents, setStreamedEvents] = useState<Event[]>([]);
  const [isConnected, setIsConnected] = useState(false);
  const [retryCount, setRetryCount] = useState(0);
  const maxRetries = 5;
  const retryDelay = 2000;

  useEffect(() => {
    const connectAndJoinSimulation = async (attempt: number = 1) => {
      try {
        setIsConnected(false);

        const connected = await signalRService.ensurePageConnection();
        if (!connected) throw new Error('Failed to connect to SignalR');

        setIsConnected(true);
        setRetryCount(0);

        const joined = await signalRService.joinSimulation(matchId);
        if (!joined) throw new Error('Failed to join simulation room');
        signalRService.onMatchEvent(
          (method: string, match_id: string, eventData: MatchEventData) => {
            try {
              const event: Event = {
                timestamp: eventData.timestamp,
                time_seconds: eventData.time_seconds,
                minute: eventData.minute,
                second: eventData.second,
                team: eventData.team,
                player: eventData.player,
                action: eventData.action,
                event_type: eventData.event_type as EventType,
                position: eventData.position,
                outcome: eventData.outcome || null,
                height: eventData.height || null,
                card: eventData.card || null,
                pass_target: eventData.pass_target || null,
                shot_target: eventData.shot_target || null,
                body_part: eventData.body_part || null,
                event_index: eventData.event_index,
                match_id: eventData.match_id,
                home_team: eventData.home_team,
                away_team: eventData.away_team,
                Score: eventData.Score
                  ? { Home: eventData.Score.home, Away: eventData.Score.away }
                  : undefined,
              };

              // Store match_start event data in localStorage
              if (event.event_type === 'match_start') {
                // Store team names
                if (event.home_team && event.away_team) {
                  storeTeamNames(event.home_team, event.away_team);
                  console.log(
                    `[SignalR] Stored team names from match_start - Home: ${event.home_team}, Away: ${event.away_team}`
                  );
                }

                // Store scores if available
                if (event.Score) {
                  storeScores(event.Score.Home, event.Score.Away);
                  console.log(
                    `[SignalR] Stored scores from match_start - Home: ${event.Score.Home}, Away: ${event.Score.Away}`
                  );
                }

                // Store match time if available
                if (event.time_seconds !== undefined) {
                  storeMatchTime(event.time_seconds);
                  console.log(
                    `[SignalR] Stored match time from match_start: ${event.time_seconds}`
                  );
                }
              }

              // Add event directly to state without delay
              setStreamedEvents((prev) => [...prev, event]);
              console.log('🎉 Event received and added directly:', event);
            } catch (err) {
              console.error('❌ Error converting event:', err);
            }
          }
        );

        signalRService.onSimulationProgress((progressData) => {
          console.log('📈 Simulation Progress:', progressData);
        });

        signalRService.onSimulationComplete((simulationId, finalScore) => {
          console.log('🏁 Simulation Complete:', simulationId, finalScore);
        });

        signalRService.onSimulationError((simulationId, error) => {
          console.error('💥 Simulation Error:', simulationId, error);
        });
      } catch (error) {
        setIsConnected(false);
        setRetryCount(attempt);

        if (attempt < maxRetries) {
          const delay = retryDelay * attempt;
          setTimeout(() => connectAndJoinSimulation(attempt + 1), delay);
        } else {
          console.error('All connection attempts failed.');
        }
      }
    };
    if (matchId && matchId > 0) {
      connectAndJoinSimulation(1);
    }
    return () => {
      if (matchId > 0) {
        signalRService.leaveSimulation(matchId).catch(console.error);
        signalRService.removeAllListeners();
      }
      setIsConnected(false);
      setRetryCount(0);
    };
  }, [matchId]);

  return { events: streamedEvents, isConnected, retryCount };
}
