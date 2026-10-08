import { useEffect, useState } from 'react';
import { Event, EventType } from '@/types/Event';
import signalRService, { MatchEventData } from '@/Services/SignalRService';
import matchStreamService from '@/Services/MatchStreamService';
import authService from '@/Services/AuthenticationService';
import { storeTeamNames, storeScores, storeMatchTime } from '@/lib/teamStorage';

export default function useSignalREventStream(matchId: number) {
  const [streamedEvents, setStreamedEvents] = useState<Event[]>([]);
  const [isConnected, setIsConnected] = useState(false);
  const [retryCount, setRetryCount] = useState(0);
  const maxRetries = 5;
  const retryDelay = 2000;

  useEffect(() => {
    let cleanupSse: (() => void) | null = null;

    const processIncomingEvent = (eventData: any) => {
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
            ? {
                Home: eventData.Score.home ?? eventData.Score.Home,
                Away: eventData.Score.away ?? eventData.Score.Away,
              }
            : undefined,
        };

        if (event.event_type === 'match_start') {
          if (event.home_team && event.away_team) {
            storeTeamNames(event.home_team, event.away_team);
          }
          if (event.Score) {
            storeScores(event.Score.Home, event.Score.Away);
          }
          if (event.time_seconds !== undefined) {
            storeMatchTime(event.time_seconds);
          }
        }

        setStreamedEvents((prev) => {
          if (
            event.event_index !== undefined &&
            prev.some((e) => e.event_index === event.event_index)
          ) {
            return prev;
          }
          return [...prev, event];
        });
      } catch (err) {
        console.error('❌ Error converting event:', err);
      }
    };

    // Connect to high-performance SSE stream if available
    try {
      const token =
        typeof window !== 'undefined'
          ? (authService as any).getToken?.() ||
            localStorage.getItem('token') ||
            sessionStorage.getItem('token') ||
            ''
          : '';
      if (token && matchId > 0) {
        cleanupSse = matchStreamService.connectToMatchStream(matchId, token, {
          onConnected: () => {
            setIsConnected(true);
            setRetryCount(0);
          },
          onEvent: (eventData) => {
            processIncomingEvent(eventData);
          },
          onError: (err) => {
            console.warn('[SSE] Error in SSE stream, relying on SignalR:', err);
          },
        });
      }
    } catch (e) {
      console.warn('[SSE] Initialization warning:', e);
    }

    const connectAndJoinSimulation = async (attempt: number = 1) => {
      try {
        const connected = await signalRService.ensurePageConnection();
        if (!connected) throw new Error('Failed to connect to SignalR');

        setIsConnected(true);
        setRetryCount(0);

        const joined = await signalRService.joinSimulation(matchId);
        if (!joined) throw new Error('Failed to join simulation room');
        signalRService.onMatchEvent(
          (_method: string, _match_id: string, eventData: MatchEventData) => {
            processIncomingEvent(eventData);
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
        if (!cleanupSse) {
          setIsConnected(false);
          setRetryCount(attempt);
        }

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
      if (cleanupSse) {
        cleanupSse();
      }
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
