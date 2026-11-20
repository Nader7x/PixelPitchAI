'use client';

import React, { useMemo } from 'react';
import {
  CommentaryCueV2,
  TelemetrySnapshotV2,
} from '@/types/MatchV2';
import { TelemetryPoint } from '@/app/hooks/useMatchStreamV2';

interface LiveAnalyticsPanelProps {
  homeTeam: string;
  awayTeam: string;
  telemetry: TelemetrySnapshotV2 | null;
  history: TelemetryPoint[];
  commentary: CommentaryCueV2[];
}

export const LiveAnalyticsPanel: React.FC<LiveAnalyticsPanelProps> = ({
  homeTeam,
  awayTeam,
  telemetry,
  history,
  commentary,
}) => {
  const xgPaths = useMemo(() => {
    if (history.length < 2) {
      return { homePath: '', awayPath: '', maxXg: 1 };
    }
    const maxClock = Math.max(
      90 * 60,
      history[history.length - 1].clockSeconds || 1
    );
    const maxXg = Math.max(
      1,
      ...history.map((h) => Math.max(h.homeXg, h.awayXg))
    );
    const w = 280;
    const h = 88;

    const toPoints = (getter: (p: TelemetryPoint) => number) =>
      history
        .map((pt, idx) => {
          const x = (pt.clockSeconds / maxClock) * w;
          const y = h - (getter(pt) / maxXg) * (h - 8) - 4;
          return `${idx === 0 ? 'M' : 'L'} ${x.toFixed(1)} ${y.toFixed(1)}`;
        })
        .join(' ');

    return {
      homePath: toPoints((p) => p.homeXg),
      awayPath: toPoints((p) => p.awayXg),
      maxXg,
    };
  }, [history]);

  const momentumPct = useMemo(() => {
    const m = telemetry?.momentum_index ?? 0;
    return Math.max(-1, Math.min(1, m)) * 50; // -50% .. +50%
  }, [telemetry?.momentum_index]);

  const fieldTilt = telemetry?.home_field_tilt_pct ?? 50;

  return (
    <div className="flex flex-col gap-3 rounded-xl border border-white/15 bg-slate-950/90 p-3 text-xs text-white backdrop-blur-md">
      {/* Live xG Race Chart */}
      <div>
        <div className="mb-1 flex items-center justify-between font-semibold">
          <span className="text-gray-300">Live xG Race</span>
          <div className="flex gap-3 font-mono">
            <span className="text-blue-400">
              {homeTeam}: {(telemetry?.home_xg ?? 0).toFixed(2)}
            </span>
            <span className="text-red-400">
              {awayTeam}: {(telemetry?.away_xg ?? 0).toFixed(2)}
            </span>
          </div>
        </div>
        <div className="rounded-lg bg-slate-900/90 p-2">
          <svg viewBox="0 0 280 88" className="h-20 w-full">
            <line
              x1="0"
              y1="44"
              x2="280"
              y2="44"
              stroke="#334155"
              strokeDasharray="3,3"
              strokeWidth="1"
            />
            {xgPaths.homePath && (
              <path
                d={xgPaths.homePath}
                fill="none"
                stroke="#3B82F6"
                strokeWidth="2.2"
              />
            )}
            {xgPaths.awayPath && (
              <path
                d={xgPaths.awayPath}
                fill="none"
                stroke="#EF4444"
                strokeWidth="2.2"
              />
            )}
          </svg>
        </div>
      </div>

      {/* Rolling xT Momentum */}
      <div>
        <div className="mb-1 flex items-center justify-between text-gray-300">
          <span>3-Min xT Momentum</span>
          <span className="font-mono text-emerald-400">
            {(telemetry?.momentum_index ?? 0).toFixed(2)}
          </span>
        </div>
        <div className="relative h-3 w-full overflow-hidden rounded-full bg-slate-800">
          <div className="absolute top-0 left-1/2 h-full w-px bg-white/40" />
          <div
            className={`absolute top-0 h-full transition-all duration-300 ${
              momentumPct >= 0 ? 'bg-blue-500' : 'bg-red-500'
            }`}
            style={{
              left: momentumPct >= 0 ? '50%' : `${50 + momentumPct}%`,
              width: `${Math.abs(momentumPct)}%`,
            }}
          />
        </div>
      </div>

      {/* PPDA & Field Tilt Gauges */}
      <div className="grid grid-cols-2 gap-2">
        <div className="rounded-lg bg-slate-900/90 p-2">
          <div className="text-[11px] text-gray-400">PPDA (Pressing)</div>
          <div className="mt-1 flex items-baseline justify-between font-mono text-sm font-bold">
            <span className="text-blue-400">
              {(telemetry?.home_ppda ?? 0).toFixed(1)}
            </span>
            <span className="text-gray-500">vs</span>
            <span className="text-red-400">
              {(telemetry?.away_ppda ?? 0).toFixed(1)}
            </span>
          </div>
        </div>
        <div className="rounded-lg bg-slate-900/90 p-2">
          <div className="text-[11px] text-gray-400">Field Tilt</div>
          <div className="mt-1 flex items-baseline justify-between font-mono text-sm font-bold">
            <span className="text-blue-400">{fieldTilt.toFixed(0)}%</span>
            <span className="text-red-400">
              {(100 - fieldTilt).toFixed(0)}%
            </span>
          </div>
          <div className="mt-1 flex h-1.5 w-full overflow-hidden rounded-full bg-red-500/70">
            <div
              className="h-full bg-blue-500"
              style={{ width: `${Math.min(100, Math.max(0, fieldTilt))}%` }}
            />
          </div>
        </div>
      </div>

      {/* Broadcast Commentary Feed */}
      <div>
        <div className="mb-1 font-semibold text-gray-300">
          Broadcast Commentary
        </div>
        <div className="max-h-44 space-y-1.5 overflow-y-auto pr-1">
          {commentary.length === 0 ? (
            <div className="rounded-lg bg-slate-900/60 p-2 text-gray-500">
              Waiting for match commentary cues...
            </div>
          ) : (
            commentary.map((cue, idx) => (
              <div
                key={`${cue.anchor_event_index}-${cue.tier}-${idx}`}
                className={`rounded-lg border p-2 ${
                  cue.tier === 'llm_broadcast'
                    ? 'border-amber-500/40 bg-amber-950/30'
                    : cue.tier === 'tactical_insight'
                      ? 'border-emerald-500/40 bg-emerald-950/30'
                      : 'border-white/10 bg-slate-900/70'
                }`}
              >
                <div className="flex items-center justify-between text-[10px] text-gray-400">
                  <span className="uppercase">{cue.tier}</span>
                  {typeof cue.match_clock_seconds === 'number' && (
                    <span className="font-mono">
                      {Math.floor(cue.match_clock_seconds / 60)}&apos;
                    </span>
                  )}
                </div>
                {cue.headline && (
                  <div className="mt-0.5 font-semibold text-white">
                    {cue.headline}
                  </div>
                )}
                <div className="mt-0.5 text-gray-200">{cue.body}</div>
              </div>
            ))
          )}
        </div>
      </div>
    </div>
  );
};

export default LiveAnalyticsPanel;
