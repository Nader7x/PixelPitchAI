'use client';

import { useCallback, useEffect, useRef, useState } from 'react';
import matchStreamServiceV2 from '@/Services/MatchStreamServiceV2';
import { InterpBuffer } from '@/lib/InterpBuffer';
import {
  CommentaryCueV2,
  KeyMomentBookmark,
  LiveTacticalOverridePayload,
  MatchLifecycleV2,
  PlaybackSpeed,
  StreamMode,
  StructuredEventV2,
  TelemetrySnapshotV2,
} from '@/types/MatchV2';
import { storeMatchTime, storeScores } from '@/lib/teamStorage';

export interface TelemetryPoint {
  clockSeconds: number;
  homeXg: number;
  awayXg: number;
  momentumIndex: number;
}

export interface UseMatchStreamV2Result {
  events: StructuredEventV2[];
  telemetry: TelemetrySnapshotV2 | null;
  telemetryHistory: TelemetryPoint[];
  commentary: CommentaryCueV2[];
  lifecycle: MatchLifecycleV2 | null;
  keyMoments: KeyMomentBookmark[];
  isConnected: boolean;
  lastEventId: string | null;
  interpBufferRef: React.MutableRefObject<InterpBuffer>;
  streamMode: StreamMode;
  setStreamMode: (mode: StreamMode) => void;
  playbackSpeed: PlaybackSpeed;
  setPlaybackSpeed: (speed: PlaybackSpeed) => void;
  isPaused: boolean;
  setIsPaused: (paused: boolean) => void;
  jumpToMoment: (simTimestampMs: number) => void;
  applyTacticalOverride: (
    payload: LiveTacticalOverridePayload
  ) => Promise<{ succeeded: boolean; message?: string }>;
}

function isKeyMoment(ev: StructuredEventV2): KeyMomentBookmark | null {
  const clock = ev.match_clock_seconds ?? 0;
  const simMs = ev.sim_timestamp_ms ?? clock * 1000;
  const action = ev.action_type.toLowerCase();
  const outcome = ev.outcome.toLowerCase();

  if (action === 'shot' && outcome === 'goal') {
    return {
      event_index: ev.event_index,
      match_clock_seconds: clock,
      sim_timestamp_ms: simMs,
      label: `Goal — ${ev.player_name}`,
      category: 'goal',
      team_side: ev.possessing_team_side,
    };
  }
  if (ev.card === 'Red' || ev.card === 'SecondYellow') {
    return {
      event_index: ev.event_index,
      match_clock_seconds: clock,
      sim_timestamp_ms: simMs,
      label: `Red Card — ${ev.player_name}`,
      category: 'red_card',
      team_side: ev.possessing_team_side,
    };
  }
  if (ev.xg > 0.15) {
    return {
      event_index: ev.event_index,
      match_clock_seconds: clock,
      sim_timestamp_ms: simMs,
      label: `Big Chance (${ev.xg.toFixed(2)} xG) — ${ev.player_name}`,
      category: 'big_chance',
      team_side: ev.possessing_team_side,
    };
  }
  if (Math.abs(ev.xt_delta) > 0.08) {
    return {
      event_index: ev.event_index,
      match_clock_seconds: clock,
      sim_timestamp_ms: simMs,
      label: `xT Surge (+${ev.xt_delta.toFixed(2)}) — ${ev.player_name}`,
      category: 'xt_surge',
      team_side: ev.possessing_team_side,
    };
  }
  if (action === 'substitution') {
    return {
      event_index: ev.event_index,
      match_clock_seconds: clock,
      sim_timestamp_ms: simMs,
      label: `Sub — ${ev.player_name}`,
      category: 'sub',
      team_side: ev.possessing_team_side,
    };
  }
  return null;
}

export function useMatchStreamV2(
  matchId: number,
  initialMode: StreamMode = 'broadcast'
): UseMatchStreamV2Result {
  const [events, setEvents] = useState<StructuredEventV2[]>([]);
  const [telemetry, setTelemetry] = useState<TelemetrySnapshotV2 | null>(null);
  const [telemetryHistory, setTelemetryHistory] = useState<TelemetryPoint[]>([]);
  const [commentary, setCommentary] = useState<CommentaryCueV2[]>([]);
  const [lifecycle, setLifecycle] = useState<MatchLifecycleV2 | null>(null);
  const [keyMoments, setKeyMoments] = useState<KeyMomentBookmark[]>([]);
  const [isConnected, setIsConnected] = useState(false);
  const [lastEventId, setLastEventId] = useState<string | null>(null);

  const [streamMode, setStreamModeState] = useState<StreamMode>(initialMode);
  const [playbackSpeed, setPlaybackSpeedState] = useState<PlaybackSpeed>(1);
  const [isPaused, setIsPausedState] = useState(false);

  const interpBufferRef = useRef<InterpBuffer>(new InterpBuffer(initialMode));

  const setStreamMode = useCallback((mode: StreamMode) => {
    setStreamModeState(mode);
    interpBufferRef.current.setMode(mode);
  }, []);

  const setPlaybackSpeed = useCallback((speed: PlaybackSpeed) => {
    setPlaybackSpeedState(speed);
    interpBufferRef.current.setSpeed(speed);
  }, []);

  const setIsPaused = useCallback((paused: boolean) => {
    setIsPausedState(paused);
    interpBufferRef.current.setPaused(paused);
  }, []);

  const jumpToMoment = useCallback((simTimestampMs: number) => {
    interpBufferRef.current.seekToSimMs(simTimestampMs);
  }, []);

  const applyTacticalOverride = useCallback(
    async (payload: LiveTacticalOverridePayload) => {
      if (!matchId || matchId <= 0) {
        return { succeeded: false, message: 'Invalid match ID' };
      }
      return matchStreamServiceV2.applyLiveTacticalOverride(matchId, payload);
    },
    [matchId]
  );

  useEffect(() => {
    if (!matchId || matchId <= 0) return;

    interpBufferRef.current.clear();

    const disconnect = matchStreamServiceV2.connectToMatchStream(matchId, {
      onConnected: () => {
        setIsConnected(true);
      },
      onSpatialFrame: (frame, id) => {
        // Zero React setState per spatial frame — push directly into ring buffer!
        interpBufferRef.current.push(frame);
        if (id) setLastEventId(id);
      },
      onEvent: (ev, id) => {
        if (id) setLastEventId(id);

        // Trigger 3D parabolic ball arc on passes/shots/crosses
        const action = ev.action_type.toLowerCase();
        if (action === 'pass' || action === 'shot' || action === 'cross') {
          const dx = ev.end_x - ev.start_x;
          const dy = ev.end_y - ev.start_y;
          const dist = Math.hypot(dx, dy);
          const durationMs = Math.min(1800, Math.max(240, (dist / 22) * 1000));
          const startMs =
            ev.sim_timestamp_ms ??
            (ev.match_clock_seconds ?? 0) * 1000;
          interpBufferRef.current.triggerBallArc(
            startMs,
            durationMs,
            ev.pass_height || 'Ground',
            ev.end_z ?? 0
          );
        }

        setEvents((prev) => {
          if (prev.some((e) => e.event_index === ev.event_index)) {
            return prev;
          }
          return [...prev, ev];
        });

        const moment = isKeyMoment(ev);
        if (moment) {
          setKeyMoments((prev) => {
            if (prev.some((m) => m.event_index === moment.event_index)) {
              return prev;
            }
            return [...prev, moment];
          });
        }

        if (typeof ev.home_score === 'number' && typeof ev.away_score === 'number') {
          storeScores(ev.home_score, ev.away_score);
        }
        if (typeof ev.match_clock_seconds === 'number') {
          storeMatchTime(ev.match_clock_seconds);
        }
      },
      onTelemetry: (snap, id) => {
        if (id) setLastEventId(id);
        setTelemetry(snap);
        setTelemetryHistory((prev) => [
          ...prev.slice(-119),
          {
            clockSeconds: snap.match_clock_seconds ?? prev.length * 10,
            homeXg: snap.home_xg,
            awayXg: snap.away_xg,
            momentumIndex: snap.momentum_index,
          },
        ]);
      },
      onCommentary: (cue, id) => {
        if (id) setLastEventId(id);
        setCommentary((prev) => {
          if (
            prev.some(
              (c) =>
                c.anchor_event_index === cue.anchor_event_index &&
                c.tier === cue.tier
            )
          ) {
            return prev;
          }
          return [cue, ...prev].slice(0, 80);
        });
      },
      onLifecycle: (lc, id) => {
        if (id) setLastEventId(id);
        setLifecycle(lc);
        if (typeof lc.home_score === 'number' && typeof lc.away_score === 'number') {
          storeScores(lc.home_score, lc.away_score);
        }
      },
      onError: () => {
        setIsConnected(false);
      },
    });

    return () => {
      disconnect();
      setIsConnected(false);
    };
  }, [matchId]);

  return {
    events,
    telemetry,
    telemetryHistory,
    commentary,
    lifecycle,
    keyMoments,
    isConnected,
    lastEventId,
    interpBufferRef,
    streamMode,
    setStreamMode,
    playbackSpeed,
    setPlaybackSpeed,
    isPaused,
    setIsPaused,
    jumpToMoment,
    applyTacticalOverride,
  };
}

export default useMatchStreamV2;
