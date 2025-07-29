import React from 'react';
import { MatchEvent } from '@/Services/MatchSimulationService';

interface FootballPitchProps {
  events: MatchEvent[];
  currentEventIndex: number;
  homeTeam: string;
  awayTeam: string;
  homeScore: number;
  awayScore: number;
  currentMinute: number;
  className?: string;
}

const FootballPitch: React.FC<FootballPitchProps> = ({
  events,
  currentEventIndex,
  homeTeam,
  awayTeam,
  homeScore,
  awayScore,
  currentMinute,
  className = '',
}) => {
  const currentEvent = events[currentEventIndex];

  const getEventColor = (eventType: string) => {
    switch (eventType.toLowerCase()) {
      case 'goal':
        return '#10B981'; // Green
      case 'shot':
        return '#F59E0B'; // Yellow
      case 'pass':
        return '#3B82F6'; // Blue
      case 'tackle':
        return '#EF4444'; // Red
      case 'card':
        return '#DC2626'; // Dark red
      case 'substitution':
        return '#8B5CF6'; // Purple
      case 'corner':
        return '#06B6D4'; // Cyan
      case 'freekick':
        return '#F97316'; // Orange
      case 'offside':
        return '#EC4899'; // Pink
      default:
        return '#6B7280'; // Gray
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

  // Display recent events as markers on the pitch
  const recentEvents = events.slice(
    Math.max(0, currentEventIndex - 4),
    currentEventIndex + 1
  );

  return (
    <div
      className={`relative h-full w-full overflow-hidden rounded-lg bg-gradient-to-b from-green-500 to-green-600 ${className}`}
    >
      {/* Football Pitch SVG */}
      <svg
        viewBox="0 0 800 520"
        className="h-full w-full"
        style={{
          background: 'linear-gradient(90deg, #16A34A 0%, #15803D 100%)',
        }}
      >
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

        {/* Recent events as fading markers */}
        {recentEvents.map((event, index) => {
          if (!event.position) return null;

          const opacity = (index + 1) / recentEvents.length;
          const size =
            index === recentEvents.length - 1
              ? 12
              : 8 - (recentEvents.length - index - 1) * 1.5;

          return (
            <g key={event.event_index} opacity={opacity}>
              <circle
                cx={50 + (event.position[0] / 100) * 700}
                cy={50 + (event.position[1] / 100) * 420}
                r={size}
                fill={getEventColor(event.event_type)}
                className={
                  index === recentEvents.length - 1 ? 'animate-pulse' : ''
                }
              />
              {index === recentEvents.length - 1 && (
                <circle
                  cx={50 + (event.position[0] / 100) * 700}
                  cy={50 + (event.position[1] / 100) * 420}
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

        {/* Pass line for current event */}
        {currentEvent && currentEvent.pass_target && currentEvent.position && (
          <line
            x1={50 + (currentEvent.position[0] / 100) * 700}
            y1={50 + (currentEvent.position[1] / 100) * 420}
            x2={50 + (currentEvent.pass_target[0] / 100) * 700}
            y2={50 + (currentEvent.pass_target[1] / 100) * 420}
            stroke={getEventColor(currentEvent.event_type)}
            strokeWidth="3"
            strokeDasharray="8,4"
            className="animate-pulse"
            opacity="0.8"
          />
        )}

        {/* Shot line for current event */}
        {currentEvent && currentEvent.shot_target && currentEvent.position && (
          <line
            x1={50 + (currentEvent.position[0] / 100) * 700}
            y1={50 + (currentEvent.position[1] / 100) * 420}
            x2={50 + (currentEvent.shot_target[0] / 100) * 700}
            y2={50 + (currentEvent.shot_target[1] / 100) * 420}
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
