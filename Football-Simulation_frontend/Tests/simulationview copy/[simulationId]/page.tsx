'use client';
import { useEffect, useState, useRef } from 'react';
import { useRouter, useParams } from 'next/navigation';
import {
  ArrowLeft,
  Play,
  Pause,
  Square,
  Clock,
  Trophy,
  Users,
  Target,
  Zap,
  Loader2,
  AlertCircle,
  CheckCircle,
} from 'lucide-react';
import {
  Settings,
  ClubIcon,
  Bell,
  User,
  Search,
  Home,
} from 'lucide-react';
import Navbar from '@/app/Components/Navbar/Navbar';
import authService from '@/Services/AuthenticationService';
import ProtectedRoute from '@/app/Components/ProtectedRoute/ProtectedRoute';
import matchSimulationService, {
  MatchEvent,
  SimulationResultResponse,
} from '@/Services/MatchSimulationService';
import signalRService, {
  MatchEventData,
  SimulationProgressData,
} from '@/Services/SignalRService';
import Sidebar from '@/app/Components/Sidebar/Sidebar';
import { SidebarItem } from '@/app/Components/Sidebar/SidebarItem';

interface SimulationState {
  status: 'loading' | 'playing' | 'paused' | 'completed' | 'error';
  progress: number;
  currentMinute: number;
  events: MatchEvent[];
  homeScore: number;
  awayScore: number;
  homeTeam: string;
  awayTeam: string;
}

interface FootballPitchProps {
  events: MatchEvent[];
  currentEventIndex: number;
  homeTeam: string;
  awayTeam: string;
  homeScore: number;
  awayScore: number;
  currentMinute: number;
}

const FootballPitch: React.FC<FootballPitchProps> = ({
  events,
  currentEventIndex,
  homeTeam,
  awayTeam,
  homeScore,
  awayScore,
  currentMinute,
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
      default:
        return '#6B7280'; // Gray
    }
  };

  return (
    <div className="relative h-full w-full overflow-hidden rounded-lg bg-gradient-to-b from-green-500 to-green-600">
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

        {/* Current event marker */}
        {currentEvent && currentEvent.position && (
          <g>
            <circle
              cx={50 + (currentEvent.position[0] / 100) * 700}
              cy={50 + (currentEvent.position[1] / 100) * 420}
              r="8"
              fill={getEventColor(currentEvent.event_type)}
              className="animate-pulse"
            />
            <circle
              cx={50 + (currentEvent.position[0] / 100) * 700}
              cy={50 + (currentEvent.position[1] / 100) * 420}
              r="15"
              fill="none"
              stroke={getEventColor(currentEvent.event_type)}
              strokeWidth="2"
              className="animate-ping"
            />
          </g>
        )}

        {/* Pass line */}
        {currentEvent && currentEvent.pass_target && currentEvent.position && (
          <line
            x1={50 + (currentEvent.position[0] / 100) * 700}
            y1={50 + (currentEvent.position[1] / 100) * 420}
            x2={50 + (currentEvent.pass_target[0] / 100) * 700}
            y2={50 + (currentEvent.pass_target[1] / 100) * 420}
            stroke={getEventColor(currentEvent.event_type)}
            strokeWidth="3"
            strokeDasharray="5,5"
            className="animate-pulse"
          />
        )}

        {/* Shot line */}
        {currentEvent && currentEvent.shot_target && currentEvent.position && (
          <line
            x1={50 + (currentEvent.position[0] / 100) * 700}
            y1={50 + (currentEvent.position[1] / 100) * 420}
            x2={50 + (currentEvent.shot_target[0] / 100) * 700}
            y2={50 + (currentEvent.shot_target[1] / 100) * 420}
            stroke={getEventColor(currentEvent.event_type)}
            strokeWidth="4"
            className="animate-pulse"
          />
        )}
      </svg>

      {/* Score overlay */}
      <div className="absolute top-4 left-1/2 -translate-x-1/2 transform rounded-lg bg-black/70 px-6 py-3 text-white backdrop-blur-sm">
        <div className="flex items-center space-x-4 text-lg font-bold">
          <span>{homeTeam}</span>
          <span className="text-2xl text-green-400">{homeScore}</span>
          <span className="text-gray-400">-</span>
          <span className="text-2xl text-green-400">{awayScore}</span>
          <span>{awayTeam}</span>
        </div>
        <div className="mt-1 text-center text-sm text-gray-300">
          {currentMinute}'
        </div>
      </div>

      {/* Current event info */}
      {currentEvent && (
        <div className="absolute bottom-4 left-4 max-w-xs rounded-lg bg-black/70 p-3 text-white backdrop-blur-sm">
          <div className="mb-1 flex items-center space-x-2">
            <div
              className="h-3 w-3 rounded-full"
              style={{
                backgroundColor: getEventColor(currentEvent.event_type),
              }}
            />
            <span className="text-sm font-semibold">
              {currentEvent.minute}'
            </span>
            <span className="text-sm capitalize">
              {currentEvent.event_type}
            </span>
          </div>
          <div className="text-sm text-gray-300">
            <div>
              <strong>{currentEvent.player}</strong> ({currentEvent.team})
            </div>
            <div className="capitalize">{currentEvent.action}</div>
            {currentEvent.outcome && (
              <div className="text-xs text-green-400 capitalize">
                {currentEvent.outcome}
              </div>
            )}
          </div>
        </div>
      )}
    </div>
  );
};

export default function SimulationView() {
  const router = useRouter();
  const params = useParams();
  const simulationId = params.simulationId as string;
  const matchId = parseInt(localStorage.getItem('matchId') || '');

  const [simulation, setSimulation] = useState<SimulationState>({
    status: 'loading',
    progress: 0,
    currentMinute: 0,
    events: [],
    homeScore: 0,
    awayScore: 0,
    homeTeam: '',
    awayTeam: '',
  });
  const [currentEventIndex, setCurrentEventIndex] = useState(0);
  const [isPlaying, setIsPlaying] = useState(false);
  const [playbackSpeed, setPlaybackSpeed] = useState(1000); // milliseconds between events
  const [isRealTime, setIsRealTime] = useState(false); // Toggle between real-time and replay mode
  const [signalRConnected, setSignalRConnected] = useState(false);
  const intervalRef = useRef<NodeJS.Timeout | null>(null);
  useEffect(() => {
    if (!authService.isAuthenticated()) {
      router.push('/login');
      return;
    }

    if (!matchId) {
      router.push('/dashboard');
      return;
    } // Initialize SignalR connection
    const initializeSignalR = async () => {
      try {
        const connected = await signalRService.ensurePageConnection();
        setSignalRConnected(connected);

        if (connected) {
          // Join simulation room
          await signalRService.joinSimulation(matchId);

          // Set up real-time event handlers
          signalRService.onMatchEvent(
            (method: string, match_id: string, eventData: MatchEventData) => {
              if (isRealTime) {
                // Add new event to the simulation in real-time
                setSimulation((prev) => ({
                  ...prev,
                  events: [...prev.events, convertToMatchEvent(eventData)],
                  currentMinute: eventData.minute,
                  homeScore: eventData.Score?.home || prev.homeScore,
                  awayScore: eventData.Score?.away || prev.awayScore,
                }));

                // Auto-advance to new event
                setCurrentEventIndex((prev) => prev + 1);
              }
            }
          );

          signalRService.onSimulationProgress(
            (progressData: SimulationProgressData) => {
              setSimulation((prev) => ({
                ...prev,
                progress: progressData.progress,
              }));
            }
          );

          signalRService.onSimulationComplete((simId, finalScore) => {
            if (simId === simulationId) {
              setSimulation((prev) => ({
                ...prev,
                status: 'completed',
                homeScore: finalScore.home,
                awayScore: finalScore.away,
              }));
              setIsPlaying(false);
            }
          });

          signalRService.onSimulationError((matchid, error) => {
            if (matchId === matchid) {
              console.error('Simulation error:', error);
              setSimulation((prev) => ({ ...prev, status: 'error' }));
            }
          });
        }
      } catch (error) {
        console.error('Failed to initialize SignalR:', error);
      }
    };

    initializeSignalR();
    loadSimulationData();

    // Cleanup on unmount
    return () => {
      if (signalRConnected) {
        signalRService.leaveSimulation(matchId);
        signalRService.removeAllListeners();
      }
    };
  }, [matchId, router, isRealTime]);

  // Helper function to convert SignalR event data to MatchEvent
  const convertToMatchEvent = (eventData: MatchEventData): MatchEvent => {
    return {
      timestamp: eventData.timestamp || new Date().toISOString(),
      time_seconds:
        eventData.time_seconds || eventData.minute * 60 + eventData.second,
      minute: eventData.minute,
      second: eventData.second,
      team: eventData.team,
      player: eventData.player,
      action: eventData.action,
      position: [eventData.position[0] || 0, eventData.position[1] || 0],
      outcome: eventData.outcome,
      height: eventData.height || '',
      card: eventData.card || null,
      pass_target: [
        eventData.pass_target[0] || 0,
        eventData.pass_target[1] || 0,
      ],
      shot_target: [
        eventData.shot_target[0] || 0,
        eventData.shot_target[1] || 0,
      ],
      body_part: eventData.body_part || '',
      event_type: eventData.event_type,
      type: eventData.type || eventData.event_type,
      event_index: eventData.event_index,
      match_id: eventData.match_id || matchId.toString(),
      home_team: eventData.home_team || simulation.homeTeam,
      away_team: eventData.away_team || simulation.awayTeam,
      long_pass: eventData.long_pass || false,
      pass_length: eventData.pass_length || 0,
      Score: {
        Home: eventData.Score?.home || 0,
        Away: eventData.Score?.away || 0,
      },
    };
  };

  useEffect(() => {
    if (
      isPlaying &&
      simulation.events.length > 0 &&
      currentEventIndex < simulation.events.length - 1
    ) {
      intervalRef.current = setInterval(() => {
        setCurrentEventIndex((prev) => {
          const next = prev + 1;
          if (next >= simulation.events.length) {
            setIsPlaying(false);
            setSimulation((prev) => ({ ...prev, status: 'completed' }));
            return prev;
          }

          // Update current minute and Score
          const event = simulation.events[next];
          setSimulation((prev) => ({
            ...prev,
            currentMinute: event.minute,
            homeScore: event.Score.Home,
            awayScore: event.Score.Away,
          }));

          return next;
        });
      }, playbackSpeed);
    } else {
      if (intervalRef.current) {
        clearInterval(intervalRef.current);
        intervalRef.current = null;
      }
    }

    return () => {
      if (intervalRef.current) {
        clearInterval(intervalRef.current);
      }
    };
  }, [isPlaying, simulation.events.length, currentEventIndex, playbackSpeed]);
  const loadSimulationData = async () => {
    try {
      setSimulation((prev) => ({ ...prev, status: 'loading' }));

      // First check if simulation is ready
      const trackResponse =
        await matchSimulationService.trackSimulation(simulationId);

      if (!trackResponse.succeeded || trackResponse.status !== 'completed') {
        // Poll for completion
        const completed = await pollSimulationCompletion(
          simulationId,
          (progress: number) => {
            setSimulation((prev) => ({ ...prev, progress }));
          }
        );

        if (!completed) {
          throw new Error('Simulation failed or timed out');
        }
      }

      // Load simulation results
      const result =
        await matchSimulationService.getSimulationResult(simulationId);

      if (!result.succeeded) {
        throw new Error(result.error || 'Failed to load simulation');
      }

      setSimulation({
        status: 'paused',
        progress: 100,
        currentMinute: 0,
        events: result.events,
        homeScore: 0,
        awayScore: 0,
        homeTeam: result.events[0]?.home_team || 'Home',
        awayTeam: result.events[0]?.away_team || 'Away',
      });
    } catch (error) {
      console.error('Error loading simulation:', error);
      setSimulation((prev) => ({
        ...prev,
        status: 'error',
      }));
    }
  };

  // Helper function to poll simulation completion
  const pollSimulationCompletion = async (
    simulationId: string,
    onProgress?: (progress: number) => void,
    pollingInterval: number = 2000
  ): Promise<boolean> => {
    return new Promise((resolve) => {
      const poll = async () => {
        try {
          const trackResponse =
            await matchSimulationService.trackSimulation(simulationId);

          if (trackResponse.succeeded) {
            if (onProgress) {
              onProgress(trackResponse.progress || 0);
            }

            if (trackResponse.status === 'completed') {
              resolve(true);
              return;
            }

            if (trackResponse.status === 'failed') {
              resolve(false);
              return;
            }
          }

          // Continue polling
          setTimeout(poll, pollingInterval);
        } catch (error) {
          console.error('Error during simulation polling:', error);
          resolve(false);
        }
      };

      poll();
    });
  };

  const handlePlay = () => {
    if (simulation.status === 'completed') {
      // Restart from beginning
      setCurrentEventIndex(0);
      setSimulation((prev) => ({
        ...prev,
        status: 'playing',
        currentMinute: 0,
        homeScore: 0,
        awayScore: 0,
      }));
    } else {
      setSimulation((prev) => ({ ...prev, status: 'playing' }));
    }
    setIsPlaying(true);
  };

  const handlePause = () => {
    setIsPlaying(false);
    setSimulation((prev) => ({ ...prev, status: 'paused' }));
  };

  const handleStop = () => {
    setIsPlaying(false);
    setCurrentEventIndex(0);
    setSimulation((prev) => ({
      ...prev,
      status: 'paused',
      currentMinute: 0,
      homeScore: 0,
      awayScore: 0,
    }));
  };

  const handleSpeedChange = (speed: number) => {
    setPlaybackSpeed(speed);
  };
  if (simulation.status === 'loading') {
    return (
      <div className="flex min-h-screen bg-gray-50">
        <Sidebar>
          <SidebarItem icon={<Home />} text="Dashboard" href="/dashboard" />
          <SidebarItem icon={<ClubIcon />} text="Teams" href="/teams" />
          <SidebarItem icon={<Users />} text="Players" href="/players" />
          <SidebarItem
            icon={<Bell />}
            text="Notifications"
            href="/notifications"
          />
          <SidebarItem icon={<Search />} text="Search" href="/search" />
          <SidebarItem icon={<Settings />} text="Settings" href="/settings" />
          <SidebarItem icon={<User />} text="Profile" href="/profile" />
        </Sidebar>

        <div className="flex flex-1 flex-col">
          <Navbar />

          <div className="flex flex-1 items-center justify-center">
            <div className="text-center">
              <Loader2 className="mx-auto mb-4 h-16 w-16 animate-spin text-green-500" />
              <h2 className="mb-2 text-2xl font-bold text-gray-800">
                Loading Simulation
              </h2>
              <p className="mb-4 text-gray-600">
                Preparing your match simulation...
              </p>
              <div className="mx-auto h-2 w-64 rounded-full bg-gray-200">
                <div
                  className="h-2 rounded-full bg-green-500 transition-all duration-300"
                  style={{ width: `${simulation.progress}%` }}
                />
              </div>
              <p className="mt-2 text-sm text-gray-500">
                {simulation.progress}% complete
              </p>
            </div>
          </div>
        </div>
      </div>
    );
  }
  if (simulation.status === 'error') {
    return (
      <div className="flex min-h-screen bg-gray-50">
        <Sidebar>
          <SidebarItem icon={<Home />} text="Dashboard" href="/dashboard" />
          <SidebarItem icon={<ClubIcon />} text="Teams" href="/teams" />
          <SidebarItem icon={<Users />} text="Players" href="/players" />
          <SidebarItem
            icon={<Bell />}
            text="Notifications"
            href="/notifications"
          />
          <SidebarItem icon={<Search />} text="Search" href="/search" />
          <SidebarItem icon={<Settings />} text="Settings" href="/settings" />
          <SidebarItem icon={<User />} text="Profile" href="/profile" />
        </Sidebar>

        <div className="flex flex-1 flex-col">
          <Navbar />

          <div className="flex flex-1 items-center justify-center">
            <div className="text-center">
              <AlertCircle className="mx-auto mb-4 h-16 w-16 text-red-500" />
              <h2 className="mb-2 text-2xl font-bold text-gray-800">
                Simulation Error
              </h2>
              <p className="mb-4 text-gray-600">
                Failed to load the simulation data.
              </p>
              <button
                onClick={() => router.push('/dashboard')}
                className="rounded-lg bg-green-500 px-6 py-2 text-white transition-colors hover:bg-green-600"
              >
                Back to Dashboard
              </button>
            </div>
          </div>
        </div>
      </div>
    );
  }

  return (
    <ProtectedRoute allowedRoles={['User', 'Admin', 'Coach', 'Player']}>
      <div className="flex min-h-screen bg-gray-50">
        <Sidebar>
          <SidebarItem icon={<Home />} text="Dashboard" href="/dashboard" />
          <SidebarItem icon={<ClubIcon />} text="Teams" href="/teams" />
          <SidebarItem icon={<Users />} text="Players" href="/players" />
          <SidebarItem
            icon={<Bell />}
            text="Notifications"
            href="/notifications"
          />
          <SidebarItem icon={<Search />} text="Search" href="/search" />
          <SidebarItem icon={<Settings />} text="Settings" href="/settings" />
          <SidebarItem icon={<User />} text="Profile" href="/profile" />
        </Sidebar>

        <div className="flex flex-1 flex-col">
          <Navbar />

          <div className="flex-1 p-6">
            {/* Header */}
            <div className="mb-6 flex items-center justify-between">
              <div className="flex items-center space-x-4">
                <button
                  onClick={() => router.push('/dashboard')}
                  className="flex items-center space-x-2 text-gray-600 transition-colors hover:text-gray-800"
                >
                  <ArrowLeft className="h-5 w-5" />
                  <span>Back to Dashboard</span>
                </button>
                <div>
                  <h1 className="text-2xl font-bold text-gray-800">
                    Match Simulation
                  </h1>
                  <p className="text-gray-600">
                    {simulation.homeTeam} vs {simulation.awayTeam}
                  </p>
                </div>
              </div>

              {/* Status indicator */}
              <div className="flex items-center space-x-2">
                {simulation.status === 'completed' && (
                  <div className="flex items-center space-x-2 text-green-600">
                    <CheckCircle className="h-5 w-5" />
                    <span className="font-medium">Completed</span>
                  </div>
                )}
                {simulation.status === 'playing' && (
                  <div className="flex items-center space-x-2 text-blue-600">
                    <Zap className="h-5 w-5" />
                    <span className="font-medium">Playing</span>
                  </div>
                )}
                {simulation.status === 'paused' && (
                  <div className="flex items-center space-x-2 text-yellow-600">
                    <Pause className="h-5 w-5" />
                    <span className="font-medium">Paused</span>
                  </div>
                )}
              </div>
            </div>

            {/* Main content */}
            <div className="grid grid-cols-1 gap-6 lg:grid-cols-4">
              {/* Football Pitch - Main view */}
              <div className="lg:col-span-3">
                <div className="h-[600px] rounded-lg bg-white p-4 shadow-md">
                  <FootballPitch
                    events={simulation.events}
                    currentEventIndex={currentEventIndex}
                    homeTeam={simulation.homeTeam}
                    awayTeam={simulation.awayTeam}
                    homeScore={simulation.homeScore}
                    awayScore={simulation.awayScore}
                    currentMinute={simulation.currentMinute}
                  />
                </div>

                {/* Controls */}
                <div className="mt-4 rounded-lg bg-white p-4 shadow-md">
                  <div className="flex items-center justify-between">
                    <div className="flex items-center space-x-4">
                      {' '}
                      <button
                        onClick={isPlaying ? handlePause : handlePlay}
                        disabled={isRealTime}
                        className="flex items-center space-x-2 rounded-lg bg-green-500 px-4 py-2 text-white transition-colors hover:bg-green-600 disabled:cursor-not-allowed disabled:opacity-50"
                      >
                        {isPlaying ? (
                          <Pause className="h-4 w-4" />
                        ) : (
                          <Play className="h-4 w-4" />
                        )}
                        <span>{isPlaying ? 'Pause' : 'Play'}</span>
                      </button>
                      <button
                        onClick={handleStop}
                        disabled={isRealTime}
                        className="flex items-center space-x-2 rounded-lg bg-gray-500 px-4 py-2 text-white transition-colors hover:bg-gray-600 disabled:cursor-not-allowed disabled:opacity-50"
                      >
                        <Square className="h-4 w-4" />
                        <span>Stop</span>
                      </button>
                    </div>
                    <div className="flex items-center space-x-6">
                      {/* Real-time toggle */}
                      <div className="flex items-center space-x-2">
                        <input
                          type="checkbox"
                          id="realtime-toggle"
                          checked={isRealTime}
                          onChange={(e) => setIsRealTime(e.target.checked)}
                          className="rounded"
                        />
                        <label
                          htmlFor="realtime-toggle"
                          className="text-sm text-gray-600"
                        >
                          Real-time
                        </label>
                        {signalRConnected ? (
                          <div
                            className="h-2 w-2 rounded-full bg-green-500"
                            title="Connected"
                          />
                        ) : (
                          <div
                            className="h-2 w-2 rounded-full bg-red-500"
                            title="Disconnected"
                          />
                        )}
                      </div>

                      {/* Speed control - disabled in real-time mode */}
                      <div className="flex items-center space-x-2">
                        <span className="text-sm text-gray-600">Speed:</span>
                        <select
                          value={playbackSpeed}
                          onChange={(e) =>
                            handleSpeedChange(Number(e.target.value))
                          }
                          disabled={isRealTime}
                          className="rounded border border-gray-300 px-2 py-1 text-sm disabled:opacity-50"
                        >
                          <option value={2000}>0.5x</option>
                          <option value={1000}>1x</option>
                          <option value={500}>2x</option>
                          <option value={250}>4x</option>
                        </select>
                      </div>
                    </div>
                  </div>

                  {/* Progress bar */}
                  <div className="mt-4">
                    <div className="mb-2 flex items-center justify-between text-sm text-gray-600">
                      <span>
                        Event {currentEventIndex + 1} of{' '}
                        {simulation.events.length}
                      </span>
                      <span>
                        {Math.round(
                          (currentEventIndex / simulation.events.length) * 100
                        )}
                        %
                      </span>
                    </div>
                    <div className="h-2 w-full rounded-full bg-gray-200">
                      <div
                        className="h-2 rounded-full bg-green-500 transition-all duration-300"
                        style={{
                          width: `${(currentEventIndex / simulation.events.length) * 100}%`,
                        }}
                      />
                    </div>
                  </div>
                </div>
              </div>

              {/* Side panel - Events and stats */}
              <div className="space-y-4">
                {/* Match info */}
                <div className="rounded-lg bg-white p-4 shadow-md">
                  <h3 className="mb-3 flex items-center font-bold text-gray-800">
                    <Trophy className="mr-2 h-5 w-5" />
                    Match Info
                  </h3>
                  <div className="space-y-2 text-sm">
                    <div className="flex justify-between">
                      <span className="text-gray-600">Duration:</span>
                      <span>{simulation.currentMinute}'</span>
                    </div>
                    <div className="flex justify-between">
                      <span className="text-gray-600">Total Events:</span>
                      <span>{simulation.events.length}</span>
                    </div>
                    <div className="flex justify-between">
                      <span className="text-gray-600">Current Event:</span>
                      <span>{currentEventIndex + 1}</span>
                    </div>
                  </div>
                </div>

                {/* Recent events */}
                <div className="rounded-lg bg-white p-4 shadow-md">
                  <h3 className="mb-3 flex items-center font-bold text-gray-800">
                    <Clock className="mr-2 h-5 w-5" />
                    Recent Events
                  </h3>
                  <div className="max-h-96 space-y-2 overflow-y-auto">
                    {simulation.events
                      .slice(
                        Math.max(0, currentEventIndex - 5),
                        currentEventIndex + 1
                      )
                      .reverse()
                      .map((event, index) => (
                        <div
                          key={event.event_index}
                          className={`rounded p-2 text-xs ${index === 0 ? 'border border-green-200 bg-green-50' : 'bg-gray-50'}`}
                        >
                          <div className="mb-1 flex items-center justify-between">
                            <span className="font-medium">{event.minute}'</span>
                            <span className="text-gray-600 capitalize">
                              {event.event_type}
                            </span>
                          </div>
                          <div className="text-gray-700">
                            <strong>{event.player}</strong> ({event.team})
                          </div>
                          <div className="text-gray-600 capitalize">
                            {event.action}
                          </div>
                          {event.outcome && (
                            <div className="text-green-600 capitalize">
                              {event.outcome}
                            </div>
                          )}
                        </div>
                      ))}
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </ProtectedRoute>
  );
}
