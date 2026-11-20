'use client';

import React, { useMemo, useState } from 'react';
import { MatchEvent } from '@/Services/MatchSimulationService';
import {
  PlayerSpatialStateV2,
  StructuredEventV2,
} from '@/types/MatchV2';

interface FootballPitchProps {
  events: MatchEvent[];
  currentEventIndex: number;
  homeTeam: string;
  awayTeam: string;
  homeScore: number;
  awayScore: number;
  currentMinute: number;
  className?: string;
  players?: PlayerSpatialStateV2[];
  ballPosition?: [number, number];
  v2Events?: StructuredEventV2[];
}

type OverlayMode = 'voronoi' | 'pass_network' | 'shot_map';

const PITCH_X0 = 50;
const PITCH_Y0 = 50;
const PITCH_W = 700;
const PITCH_H = 420;

const toSvgX = (x: number) =>
  PITCH_X0 + (Math.max(0, Math.min(120, x)) / 120) * PITCH_W;
const toSvgY = (y: number) =>
  PITCH_Y0 + (Math.max(0, Math.min(80, y)) / 80) * PITCH_H;

const FootballPitch: React.FC<FootballPitchProps> = ({
  events,
  currentEventIndex,
  homeTeam,
  awayTeam,
  homeScore,
  awayScore,
  currentMinute,
  className = '',
  players = [],
  ballPosition,
  v2Events = [],
}) => {
  const [overlays, setOverlays] = useState<Record<OverlayMode, boolean>>({
    voronoi: false,
    pass_network: false,
    shot_map: false,
  });
  const [selectedShot, setSelectedShot] = useState<StructuredEventV2 | null>(
    null
  );

  const toggleOverlay = (mode: OverlayMode) => {
    setOverlays((prev) => ({ ...prev, [mode]: !prev[mode] }));
  };

  const currentEvent = events[currentEventIndex];

  const getEventColor = (eventType: string) => {
    switch (eventType.toLowerCase()) {
      case 'goal':
        return '#10B981';
      case 'shot':
        return '#F59E0B';
      case 'pass':
        return '#3B82F6';
      case 'tackle':
        return '#EF4444';
      case 'card':
        return '#DC2626';
      case 'substitution':
        return '#8B5CF6';
      case 'corner':
        return '#06B6D4';
      case 'freekick':
        return '#F97316';
      case 'offside':
        return '#EC4899';
      default:
        return '#6B7280';
    }
  };

  const getEventIcon = (eventType: string) => {
    switch (eventType.toLowerCase()) {
      case 'goal':
        return '⚽';
      case 'shot':
        return '🎯';
      case 'pass':
        return '➡️';
      case 'tackle':
        return '🦵';
      case 'card':
        return '🟨';
      case 'substitution':
        return '🔄';
      case 'corner':
        return '📐';
      case 'freekick':
        return '🦶';
      case 'offside':
        return '🚩';
      default:
        return '⚪';
    }
  };

  const recentEvents = events.slice(
    Math.max(0, currentEventIndex - 4),
    currentEventIndex + 1
  );

  // Voronoi Pitch Control grid (24x16 cells in 120x80 StatsBomb space)
  const voronoiCells = useMemo(() => {
    if (!overlays.voronoi || players.length === 0) return [];
    const cols = 24;
    const rows = 16;
    const cellW = 120 / cols;
    const cellH = 80 / rows;
    const cells: Array<{
      x: number;
      y: number;
      w: number;
      h: number;
      isHome: boolean;
      margin: number;
    }> = [];

    for (let c = 0; c < cols; c++) {
      for (let r = 0; r < rows; r++) {
        const px = (c + 0.5) * cellW;
        const py = (r + 0.5) * cellH;
        let minHome = Infinity;
        let minAway = Infinity;

        for (const pl of players) {
          if (pl.is_sent_off) continue;
          // Slight velocity lead for realistic space control
          const tx = pl.x + pl.vx * 0.35;
          const ty = pl.y + pl.vy * 0.35;
          const d2 = (px - tx) * (px - tx) + (py - ty) * (py - ty);
          if (pl.slot <= 11) {
            if (d2 < minHome) minHome = d2;
          } else {
            if (d2 < minAway) minAway = d2;
          }
        }

        const isHome = minHome <= minAway;
        const diff = Math.abs(Math.sqrt(minHome) - Math.sqrt(minAway));
        const margin = Math.min(0.42, 0.12 + diff * 0.018);
        cells.push({
          x: PITCH_X0 + (c / cols) * PITCH_W,
          y: PITCH_Y0 + (r / rows) * PITCH_H,
          w: PITCH_W / cols + 0.5,
          h: PITCH_H / rows + 0.5,
          isHome,
          margin,
        });
      }
    }
    return cells;
  }, [overlays.voronoi, players]);

  // Pass Network from V2 events
  const passNetwork = useMemo(() => {
    if (!overlays.pass_network || v2Events.length === 0) {
      return { nodes: [], edges: [] };
    }

    const nodeStats = new Map<
      number,
      { slot: number; sumX: number; sumY: number; count: number }
    >();
    const edgeStats = new Map<
      string,
      { from: number; to: number; count: number; xtSum: number; isHome: boolean }
    >();

    for (const ev of v2Events) {
      if (
        ev.action_type.toLowerCase() === 'pass' &&
        ev.outcome.toLowerCase() === 'complete' &&
        ev.actor_slot > 0
      ) {
        const cur = nodeStats.get(ev.actor_slot) ?? {
          slot: ev.actor_slot,
          sumX: 0,
          sumY: 0,
          count: 0,
        };
        cur.sumX += ev.start_x;
        cur.sumY += ev.start_y;
        cur.count += 1;
        nodeStats.set(ev.actor_slot, cur);

        if (ev.recipient_slot > 0) {
          const rec = nodeStats.get(ev.recipient_slot) ?? {
            slot: ev.recipient_slot,
            sumX: 0,
            sumY: 0,
            count: 0,
          };
          rec.sumX += ev.end_x;
          rec.sumY += ev.end_y;
          rec.count += 1;
          nodeStats.set(ev.recipient_slot, rec);

          const key = `${ev.actor_slot}->${ev.recipient_slot}`;
          const edge = edgeStats.get(key) ?? {
            from: ev.actor_slot,
            to: ev.recipient_slot,
            count: 0,
            xtSum: 0,
            isHome: ev.actor_slot <= 11,
          };
          edge.count += 1;
          edge.xtSum += Math.max(0, ev.xt_delta);
          edgeStats.set(key, edge);
        }
      }
    }

    const nodes = Array.from(nodeStats.values()).map((n) => ({
      slot: n.slot,
      x: n.sumX / n.count,
      y: n.sumY / n.count,
      count: n.count,
      isHome: n.slot <= 11,
    }));
    const nodeMap = new Map(nodes.map((n) => [n.slot, n]));

    const edges = Array.from(edgeStats.values())
      .filter((e) => e.count >= 2 && nodeMap.has(e.from) && nodeMap.has(e.to))
      .map((e) => ({
        ...e,
        fromNode: nodeMap.get(e.from)!,
        toNode: nodeMap.get(e.to)!,
      }));

    return { nodes, edges };
  }, [overlays.pass_network, v2Events]);

  const shotEvents = useMemo(() => {
    if (!overlays.shot_map) return [];
    return v2Events.filter((e) => e.action_type.toLowerCase() === 'shot');
  }, [overlays.shot_map, v2Events]);

  return (
    <div
      className={`relative h-full w-full overflow-hidden rounded-lg bg-gradient-to-b from-green-500 to-green-600 ${className}`}
    >
      {/* Football Pitch SVG (Canonical StatsBomb 120 x 80) */}
      <svg
        viewBox="0 0 800 520"
        className="h-full w-full"
        style={{
          background: 'linear-gradient(90deg, #16A34A 0%, #15803D 100%)',
        }}
      >
        {/* Voronoi Pitch Control Layer */}
        {overlays.voronoi &&
          voronoiCells.map((cell, idx) => (
            <rect
              key={`vc-${idx}`}
              x={cell.x}
              y={cell.y}
              width={cell.w}
              height={cell.h}
              fill={cell.isHome ? '#3B82F6' : '#EF4444'}
              fillOpacity={cell.margin}
            />
          ))}

        {/* Pitch outline */}
        <rect
          x="50"
          y="50"
          width="700"
          height="420"
          fill="none"
          stroke="white"
          strokeWidth="3"
        />

        {/* Center circle */}
        <circle
          cx="400"
          cy="260"
          r="60"
          fill="none"
          stroke="white"
          strokeWidth="3"
        />
        <circle cx="400" cy="260" r="2" fill="white" />

        {/* Center line */}
        <line
          x1="400"
          y1="50"
          x2="400"
          y2="470"
          stroke="white"
          strokeWidth="3"
        />

        {/* Left penalty area */}
        <rect
          x="50"
          y="160"
          width="120"
          height="200"
          fill="none"
          stroke="white"
          strokeWidth="3"
        />

        {/* Left goal area */}
        <rect
          x="50"
          y="210"
          width="40"
          height="100"
          fill="none"
          stroke="white"
          strokeWidth="3"
        />

        {/* Left goal */}
        <rect
          x="30"
          y="235"
          width="20"
          height="50"
          fill="none"
          stroke="white"
          strokeWidth="3"
        />

        {/* Right penalty area */}
        <rect
          x="630"
          y="160"
          width="120"
          height="200"
          fill="none"
          stroke="white"
          strokeWidth="3"
        />

        {/* Right goal area */}
        <rect
          x="710"
          y="210"
          width="40"
          height="100"
          fill="none"
          stroke="white"
          strokeWidth="3"
        />

        {/* Right goal */}
        <rect
          x="750"
          y="235"
          width="20"
          height="50"
          fill="none"
          stroke="white"
          strokeWidth="3"
        />

        {/* Penalty spots */}
        <circle cx="130" cy="260" r="2" fill="white" />
        <circle cx="670" cy="260" r="2" fill="white" />

        {/* Left penalty arc */}
        <path
          d="M 90 160 A 60 60 0 0 1 90 360"
          fill="none"
          stroke="white"
          strokeWidth="3"
        />

        {/* Right penalty arc */}
        <path
          d="M 710 160 A 60 60 0 0 0 710 360"
          fill="none"
          stroke="white"
          strokeWidth="3"
        />

        {/* Corner arcs */}
        <path
          d="M 50 65 A 15 15 0 0 1 65 50"
          fill="none"
          stroke="white"
          strokeWidth="2"
        />
        <path
          d="M 735 50 A 15 15 0 0 1 750 65"
          fill="none"
          stroke="white"
          strokeWidth="2"
        />
        <path
          d="M 750 455 A 15 15 0 0 1 735 470"
          fill="none"
          stroke="white"
          strokeWidth="2"
        />
        <path
          d="M 65 470 A 15 15 0 0 1 50 455"
          fill="none"
          stroke="white"
          strokeWidth="2"
        />

        {/* Pass Network Overlay */}
        {overlays.pass_network && (
          <g>
            {passNetwork.edges.map((edge, idx) => (
              <line
                key={`pe-${idx}`}
                x1={toSvgX(edge.fromNode.x)}
                y1={toSvgY(edge.fromNode.y)}
                x2={toSvgX(edge.toNode.x)}
                y2={toSvgY(edge.toNode.y)}
                stroke={edge.isHome ? '#60A5FA' : '#F87171'}
                strokeWidth={Math.min(7, 1.5 + edge.count * 0.7)}
                strokeOpacity={Math.min(0.85, 0.35 + edge.xtSum * 2)}
              />
            ))}
            {passNetwork.nodes.map((node) => (
              <g key={`pn-${node.slot}`}>
                <circle
                  cx={toSvgX(node.x)}
                  cy={toSvgY(node.y)}
                  r={Math.min(14, 6 + Math.sqrt(node.count))}
                  fill={node.isHome ? '#2563EB' : '#DC2626'}
                  stroke="white"
                  strokeWidth="1.5"
                />
                <text
                  x={toSvgX(node.x)}
                  y={toSvgY(node.y) + 3}
                  textAnchor="middle"
                  fill="white"
                  fontSize="9"
                  fontWeight="bold"
                >
                  {node.slot <= 11 ? node.slot : node.slot - 11}
                </text>
              </g>
            ))}
          </g>
        )}

        {/* Shot Map Overlay */}
        {overlays.shot_map &&
          shotEvents.map((shot) => {
            const isGoal = shot.outcome.toLowerCase() === 'goal';
            const radius = Math.max(5, Math.min(18, 5 + shot.xg * 24));
            return (
              <g
                key={`shot-${shot.event_index}`}
                className="cursor-pointer"
                onClick={() => setSelectedShot(shot)}
              >
                <line
                  x1={toSvgX(shot.start_x)}
                  y1={toSvgY(shot.start_y)}
                  x2={toSvgX(shot.end_x)}
                  y2={toSvgY(shot.end_y)}
                  stroke={isGoal ? '#10B981' : '#FBBF24'}
                  strokeWidth={isGoal ? 2.5 : 1.5}
                  strokeDasharray={isGoal ? undefined : '4,3'}
                  opacity="0.7"
                />
                <circle
                  cx={toSvgX(shot.start_x)}
                  cy={toSvgY(shot.start_y)}
                  r={radius}
                  fill={isGoal ? '#10B981' : '#F59E0B'}
                  fillOpacity="0.8"
                  stroke="white"
                  strokeWidth={isGoal ? 2.5 : 1.5}
                />
              </g>
            );
          })}

        {/* 22 Live Players */}
        {players.map((pl) => {
          if (pl.is_sent_off) return null;
          const isHome = pl.slot <= 11;
          const isGk = pl.slot === 1 || pl.slot === 12;
          const cx = toSvgX(pl.x);
          const cy = toSvgY(pl.y);
          const fill = isHome
            ? isGk
              ? '#0EA5E9'
              : '#2563EB'
            : isGk
              ? '#F97316'
              : '#DC2626';

          return (
            <g key={`pl-${pl.slot}`}>
              {pl.has_ball && (
                <circle
                  cx={cx}
                  cy={cy}
                  r={14}
                  fill="none"
                  stroke="#FACC15"
                  strokeWidth="2.5"
                />
              )}
              <circle
                cx={cx}
                cy={cy}
                r={9}
                fill={fill}
                stroke="white"
                strokeWidth="2"
              />
              <text
                x={cx}
                y={cy + 3.5}
                textAnchor="middle"
                fill="white"
                fontSize="9"
                fontWeight="bold"
              >
                {isHome ? pl.slot : pl.slot - 11}
              </text>
            </g>
          );
        })}

        {/* Live Ball */}
        {ballPosition && (
          <circle
            cx={toSvgX(ballPosition[0])}
            cy={toSvgY(ballPosition[1])}
            r={5}
            fill="#FFFFFF"
            stroke="#111827"
            strokeWidth="2"
          />
        )}

        {/* Recent events as fading markers (canonical 120x80 normalization) */}
        {recentEvents.map((event, index) => {
          if (!event.position) return null;

          const opacity = (index + 1) / recentEvents.length;
          const size =
            index === recentEvents.length - 1
              ? 12
              : 8 - (recentEvents.length - index - 1) * 1.5;
          const cx = toSvgX(event.position[0]);
          const cy = toSvgY(event.position[1]);

          return (
            <g key={event.event_index} opacity={opacity}>
              <circle
                cx={cx}
                cy={cy}
                r={size}
                fill={getEventColor(event.event_type)}
                className={
                  index === recentEvents.length - 1 ? 'animate-pulse' : ''
                }
              />
              {index === recentEvents.length - 1 && (
                <circle
                  cx={cx}
                  cy={cy}
                  r={size + 8}
                  fill="none"
                  stroke={getEventColor(event.event_type)}
                  strokeWidth="2"
                  className="animate-ping"
                />
              )}
            </g>
          );
        })}

        {/* Pass line for current event (canonical 120x80 normalization) */}
        {currentEvent && currentEvent.pass_target && currentEvent.position && (
          <line
            x1={toSvgX(currentEvent.position[0])}
            y1={toSvgY(currentEvent.position[1])}
            x2={toSvgX(currentEvent.pass_target[0])}
            y2={toSvgY(currentEvent.pass_target[1])}
            stroke={getEventColor(currentEvent.event_type)}
            strokeWidth="3"
            strokeDasharray="8,4"
            className="animate-pulse"
            opacity="0.8"
          />
        )}

        {/* Shot line for current event (canonical 120x80 normalization) */}
        {currentEvent && currentEvent.shot_target && currentEvent.position && (
          <line
            x1={toSvgX(currentEvent.position[0])}
            y1={toSvgY(currentEvent.position[1])}
            x2={toSvgX(currentEvent.shot_target[0])}
            y2={toSvgY(currentEvent.shot_target[1])}
            stroke={getEventColor(currentEvent.event_type)}
            strokeWidth="4"
            className="animate-pulse"
            opacity="0.9"
          />
        )}
      </svg>

      {/* Score overlay */}
      <div className="absolute top-4 left-1/2 -translate-x-1/2 transform rounded-xl border border-white/20 bg-black/80 px-6 py-3 text-white backdrop-blur-sm">
        <div className="flex items-center space-x-4 text-lg font-bold">
          <span className="text-blue-300">{homeTeam}</span>
          <span className="font-mono text-3xl text-green-400">{homeScore}</span>
          <span className="text-gray-400">-</span>
          <span className="font-mono text-3xl text-green-400">{awayScore}</span>
          <span className="text-red-300">{awayTeam}</span>
        </div>
        <div className="mt-1 text-center font-mono text-sm text-gray-300">
          {currentMinute}'{' '}
          {currentMinute < 45
            ? '1st Half'
            : currentMinute < 90
              ? '2nd Half'
              : 'Extra Time'}
        </div>
      </div>

      {/* Tactical Overlay Toggles */}
      <div className="absolute top-4 left-4 flex flex-wrap gap-1.5 rounded-xl border border-white/20 bg-black/80 p-2 text-xs text-white backdrop-blur-sm">
        {(
          [
            { key: 'voronoi', label: 'Voronoi Control' },
            { key: 'pass_network', label: 'Pass Network' },
            { key: 'shot_map', label: 'Shot Map' },
          ] as const
        ).map(({ key, label }) => (
          <button
            key={key}
            type="button"
            onClick={() => toggleOverlay(key)}
            className={`rounded-lg px-2.5 py-1 font-medium transition ${
              overlays[key]
                ? 'bg-emerald-500 text-black'
                : 'bg-white/10 text-gray-200 hover:bg-white/20'
            }`}
          >
            {label}
          </button>
        ))}
      </div>

      {/* Selected Shot Inspector */}
      {selectedShot && (
        <div className="absolute bottom-20 left-4 max-w-xs rounded-xl border border-amber-400/40 bg-black/90 p-3 text-xs text-white backdrop-blur-sm">
          <div className="flex items-center justify-between gap-4">
            <span className="font-bold text-amber-300">
              Shot #{selectedShot.event_index} ({selectedShot.outcome})
            </span>
            <button
              type="button"
              onClick={() => setSelectedShot(null)}
              className="text-gray-400 hover:text-white"
            >
              ✕
            </button>
          </div>
          <div className="mt-1 text-gray-200">{selectedShot.player_name}</div>
          <div className="mt-1 flex gap-3 font-mono text-emerald-300">
            <span>xG: {selectedShot.xg.toFixed(2)}</span>
            <span>PSxG: {selectedShot.post_shot_xg.toFixed(2)}</span>
          </div>
        </div>
      )}

      {/* Current event info */}
      {currentEvent && (
        <div className="absolute bottom-4 left-4 max-w-xs rounded-xl border border-white/20 bg-black/80 p-4 text-white backdrop-blur-sm">
          <div className="mb-2 flex items-center space-x-3">
            <div className="text-xl">
              {getEventIcon(currentEvent.event_type)}
            </div>
            <div
              className="h-3 w-3 rounded-full"
              style={{
                backgroundColor: getEventColor(currentEvent.event_type),
              }}
            />
            <span className="text-sm font-bold text-green-400">
              {currentEvent.minute}'
            </span>
            <span className="text-sm font-medium text-blue-300 capitalize">
              {currentEvent.event_type}
            </span>
          </div>
          <div className="text-sm text-gray-100">
            <div className="font-bold text-white">{currentEvent.player}</div>
            <div className="text-gray-300">({currentEvent.team})</div>
            <div className="mt-1 text-gray-200 capitalize">
              {currentEvent.action}
            </div>
            {currentEvent.outcome && (
              <div className="mt-1 text-xs font-medium text-green-400 capitalize">
                {currentEvent.outcome}
              </div>
            )}
            {currentEvent.card && (
              <div className="mt-1 text-xs font-medium text-yellow-400">
                {currentEvent.card} Card
              </div>
            )}
          </div>
        </div>
      )}

      {/* Event legend */}
      <div className="absolute top-4 right-4 rounded-xl border border-white/20 bg-black/80 p-3 text-white backdrop-blur-sm">
        <h4 className="mb-2 text-xs font-bold text-gray-300">Events</h4>
        <div className="grid grid-cols-2 gap-1 text-xs">
          {[
            { type: 'goal', label: 'Goal' },
            { type: 'shot', label: 'Shot' },
            { type: 'pass', label: 'Pass' },
            { type: 'tackle', label: 'Tackle' },
            { type: 'card', label: 'Card' },
            { type: 'corner', label: 'Corner' },
          ].map(({ type, label }) => (
            <div key={type} className="flex items-center space-x-1">
              <div
                className="h-2 w-2 rounded-full"
                style={{ backgroundColor: getEventColor(type) }}
              />
              <span className="text-gray-300">{label}</span>
            </div>
          ))}
        </div>
      </div>

      {/* Match progress indicator */}
      <div className="absolute right-4 bottom-4 rounded-xl border border-white/20 bg-black/80 p-3 text-white backdrop-blur-sm">
        <div className="mb-1 text-xs text-gray-300">Match Progress</div>
        <div className="h-2 w-32 rounded-full bg-gray-700">
          <div
            className="h-2 rounded-full bg-green-500 transition-all duration-300"
            style={{ width: `${Math.min((currentMinute / 90) * 100, 100)}%` }}
          />
        </div>
        <div className="mt-1 text-center text-xs text-gray-400">
          {Math.round(Math.min((currentMinute / 90) * 100, 100))}%
        </div>
      </div>
    </div>
  );
};

export default FootballPitch;
