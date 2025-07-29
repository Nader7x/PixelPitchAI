'use client';

import { useGLTF } from '@react-three/drei';
import { useRef, useState, useEffect, useMemo } from 'react';
import * as THREE from 'three';
import { Event } from '@/types/Event';
import {
  renderPassEvent,
  renderShotEvent,
  renderFoulEvent,
  renderMatchStatusEvent,
  renderBallReceiptEvent,
  renderCarryEvent,
  renderPressureEvent,
  renderThrowInPassEvent,
  renderCardEvent,
  renderSaveEvent,
  ScoreboardDisplay,
} from './plotters/eventPlottingHandlers';
import Field from './Field';

// Props interface for the demo event plotter
interface DemoEventPlotterProps {
  events: Event[];
  timer: number;
  homeTeam: string;
  awayTeam: string;
}

const getFirstGoalkeeperNames = (events: Event[]) => {
  const keeperMap: Record<string, string> = {};
  for (const event of events) {
    if (
      event.player &&
      event.team &&
      event.player.toLowerCase().includes('goal keeper') &&
      !keeperMap[event.team]
    ) {
      keeperMap[event.team] = event.player;
    }
  }
  return keeperMap;
};

const normalizeGoalkeeperName = (
  event: Event,
  keeperMap: Record<string, string>
): Event => {
  if (
    event.player &&
    event.team &&
    event.player.toLowerCase().includes('goal keeper') &&
    keeperMap[event.team]
  ) {
    // Remove "goal keeper" (case-insensitive) from the name, trim spaces
    const cleanName = keeperMap[event.team]
      .replace(/goal keeper\s*/i, '')
      .trim();
    return { ...event, player: cleanName };
  }
  return event;
};

const DemoEventPlotter = ({
  events,
  timer,
  homeTeam,
  awayTeam,
}: DemoEventPlotterProps) => {
  const ballRef = useRef<THREE.Mesh>(null);
  const processedGoals = useRef(new Set<string>());

  // Scoreboard state - managed internally without localStorage
  const [homeScore, setHomeScore] = useState(0);
  const [awayScore, setAwayScore] = useState(0);
  const { scene } = useGLTF('/models/soccer_ball.glb');

  // Handle goal detection from events
  useEffect(() => {
    const goalEvents = events.filter(
      (event) => event.event_type === 'shot' && event.outcome === 'Goal'
    );

    goalEvents.forEach((goalEvent) => {
      const goalId = `${goalEvent.team}-${goalEvent.timestamp || goalEvent.event_index}`;

      if (!processedGoals.current.has(goalId)) {
        processedGoals.current.add(goalId);

        if (goalEvent.team === homeTeam) {
          setHomeScore((prev) => prev + 1);
        } else if (goalEvent.team === awayTeam) {
          setAwayScore((prev) => prev + 1);
        }
      }
    });
  }, [events, homeTeam, awayTeam]);

  // Reset scores when demo resets (when events length goes to 0)
  useEffect(() => {
    if (events.length === 0) {
      setHomeScore(0);
      setAwayScore(0);
      processedGoals.current.clear();
    }
  }, [events.length]);

  const moveBall = (pos: [number, number, number]) => {
    if (ballRef.current) {
      ballRef.current.position.set(pos[0], pos[1], pos[2]);
    }
  };

  // Normalize goalkeeper names dynamically
  const keeperMap = getFirstGoalkeeperNames(events);
  const normalizedEvents = events.map((e) =>
    normalizeGoalkeeperName(e, keeperMap)
  );

  const allowedTypes = [
    'match_start',
    'pass',
    'ball_receipt',
    'carry',
    'pressure',
    'duel',
    'interception',
    'block',
    'throw_in_pass',
    'foul_committed',
    'foul_won',
    'free_kick_pass',
    'dribble',
    'ball_recovery',
    'shot',
    'save',
    'corner_pass',
    'goal_kick_pass',
    'match_end',
    'first_half_end',
    'second_half_start',
  ] as const;

  // Track sent-off players
  const sentOffPlayers = new Set<string>();
  const filteredEvents: Event[] = [];

  for (const event of normalizedEvents) {
    // If this player has been sent off, skip their events
    if (event.player && sentOffPlayers.has(event.player)) continue;

    // If this event is a red card or second yellow, add player to sent-off list
    if (
      event.event_type === 'foul_committed' &&
      (event.card === 'Red Card' || event.card === 'Second Yellow') &&
      event.player
    ) {
      sentOffPlayers.add(event.player);
    }

    filteredEvents.push(event);
  }
  // Only show the last 5 relevant events, but handle system events differently
  const allRelevantEvents = filteredEvents.filter((event) =>
    allowedTypes.includes(event.event_type as (typeof allowedTypes)[number])
  );

  // For system events like match_start, first_half_end, match_end,
  // only show them if they are among the very recent events
  const visibleEvents = allRelevantEvents
    .slice(-5)
    .filter((event, index, arr) => {
      // System events only show if they are one of the last 3 events
      if (
        event.team === 'SYSTEM' &&
        ['match_start', 'first_half_end', 'match_end'].includes(
          event.event_type
        )
      ) {
        const systemEventIndex = arr.length - 1 - index; // How many events ago
        return systemEventIndex <= 2; // Only show if it's within the last 3 events
      }
      return true; // Show all non-system events
    });

  const renderEvent = (event: Event, key: number) => {
    switch (event?.event_type) {
      case 'match_start':
      case 'match_end':
      case 'first_half_end':
        return <group key={key}>{renderMatchStatusEvent(event)}</group>;
      case 'pass':
      case 'free_kick_pass':
      case 'corner_pass':
        return (
          <group key={key}>
            {renderPassEvent(event, moveBall, homeTeam, awayTeam)}
          </group>
        );
      case 'ball_receipt':
      case 'ball_recovery':
        return (
          <group key={key}>
            {renderBallReceiptEvent(event, moveBall, homeTeam, awayTeam)}
          </group>
        );
      case 'carry':
      case 'interception':
      case 'block':
      case 'foul_won':
      case 'dribble':
        return (
          <group key={key}>
            {renderCarryEvent(event, moveBall, homeTeam, awayTeam)}
          </group>
        );
      case 'pressure':
      case 'duel':
        return (
          <group key={key}>
            {renderPressureEvent(event, homeTeam, awayTeam)}
          </group>
        );
      case 'foul_committed':
        return (
          <group key={key}>{renderFoulEvent(event, homeTeam, awayTeam)}</group>
        );
      case 'throw_in_pass':
        return (
          <group key={key}>
            {renderThrowInPassEvent(event, moveBall, homeTeam, awayTeam)}
          </group>
        );
      case 'shot':
        return (
          <group key={key}>
            {renderShotEvent(event, moveBall, homeTeam, awayTeam)}
          </group>
        );
      case 'save':
        return (
          <group key={key}>
            {renderSaveEvent(event, moveBall, homeTeam, awayTeam)}
          </group>
        );
      default:
        return null;
    }
  };

  // Calculate match time from events or use timer
  const currentMatchTime = useMemo(() => {
    if (events.length === 0) {
      return 0;
    }

    // Get the latest event with time information
    const latestEvent = events[events.length - 1];

    // Use time_seconds directly if available
    if (latestEvent?.time_seconds !== undefined) {
      return latestEvent.time_seconds;
    }

    // Fallback to timer
    return timer;
  }, [events, timer]);

  // Get display names for teams
  const homeTeamDisplay = homeTeam?.replace('_', ' ') || 'Home Team';
  const awayTeamDisplay = awayTeam?.replace('_', ' ') || 'Away Team';
  return (
    <group>
      {/* Soccer Field */}
      <Field />

      {/* Ball */}
      <primitive
        ref={ballRef}
        object={scene}
        position={[0, 0, 0]}
        scale={[2, 2, 2]}
      />

      {/* 3D Scoreboard */}
      <ScoreboardDisplay
        homeTeam={homeTeamDisplay}
        awayTeam={awayTeamDisplay}
        homeScore={homeScore}
        awayScore={awayScore}
        timer={currentMatchTime}
      />

      {/* Only render the last 5 events */}
      {visibleEvents.map((event) =>
        renderEvent(event, event.event_index || Math.random())
      )}
    </group>
  );
};

export default DemoEventPlotter;
