'use client';
import { useEffect, useState } from 'react';
import { useRouter } from 'next/navigation';
import Link from 'next/link';
import Image from 'next/image';
import { SidebarSection } from '../Components/Sidebar/SidebarSection';
import {
  ArrowLeft,
  Play,
  Users,
  Calendar,
  Loader2,
  CheckCircle,
  AlertCircle,
  Zap,
} from 'lucide-react';
import Sidebar, { SidebarLayout } from '../Components/Sidebar/Sidebar';
import { SidebarItem } from '../Components/Sidebar/SidebarItem';
import {
  Settings,
  Package,
  LayoutDashboardIcon,
  ClubIcon,
  Bell,
  User,
  Search,
  Home,
} from 'lucide-react';
import Navbar from '@/app/Components/Navbar/Navbar';
import authService from '@/Services/AuthenticationService';
import teamService, { Team } from '@/Services/TeamService';
import ProtectedRoute from '@/app/Components/ProtectedRoute/ProtectedRoute';
import matchSimulationService, {
  TeamSeason,
  SimulateMatchRequest,
  SimulateMatchResponse,
} from '@/Services/MatchSimulationService';
import { useSettings } from '../contexts/EnhancedSettingsContext';

// Force dynamic rendering to prevent prerender errors
export const dynamic = 'force-dynamic';

interface SimulationState {
  status: 'idle' | 'loading' | 'success' | 'error';
  data?: SimulateMatchResponse;
  error?: string;
}

export default function MatchSimulation() {
  // Settings context for theme and preferences
  const { isDarkMode, playSound } = useSettings();

  const [teams, setTeams] = useState<Team[]>([]);
  const [homeTeam, setHomeTeam] = useState<Team | null>(null);
  const [awayTeam, setAwayTeam] = useState<Team | null>(null);
  const [homeSeasons, setHomeSeasons] = useState<TeamSeason[]>([]);
  const [awaySeasons, setAwaySeasons] = useState<TeamSeason[]>([]);
  const [homeSelectedSeason, setHomeSelectedSeason] = useState<string>('');
  const [awaySelectedSeason, setAwaySelectedSeason] = useState<string>('');
  const [homeSelectedSeasonId, setHomeSelectedSeasonId] = useState<number>(-1);
  const [awaySelectedSeasonId, setAwaySelectedSeasonId] = useState<number>(-1);
  const [isLoading, setIsLoading] = useState(true);
  const [isLoadingSeasons, setIsLoadingSeasons] = useState(false);
  const [simulation, setSimulation] = useState<SimulationState>({
    status: 'idle',
  });
  const [simulationStartTime, setSimulationStartTime] = useState<number | null>(
    null
  );
  const [isAdmin, setIsAdmin] = useState(false);
  const router = useRouter();

  useEffect(() => {
    // Check if user is authenticated
    if (!authService.isAuthenticated()) {
      router.push('/login');
      return;
    }

    fetchTeams();
    checkUserRole();
  }, [router]);

  const fetchTeams = async () => {
    setIsLoading(true);
    try {
      const response = await teamService.getAllTeams();
      if (Array.isArray(response)) {
        setTeams(response);
      } else {
        console.error('Unexpected API response format for teams:', response);
        setTeams([]);
      }
    } catch (error) {
      console.error('Error fetching teams:', error);
      setTeams([]);
    } finally {
      setIsLoading(false);
    }
  };

  const checkUserRole = () => {
    setIsAdmin(authService.hasRole('Admin'));
  };

  const handleTeamSelection = async (team: Team, type: 'home' | 'away') => {
    setIsLoadingSeasons(true);
    try {
      const seasonsResponse = await matchSimulationService.getTeamSeasons(
        team.id
      );

      if (seasonsResponse.succeeded) {
        if (type === 'home') {
          setHomeTeam(team);
          setHomeSeasons(seasonsResponse.seasons);
          setHomeSelectedSeason('');
          setHomeSelectedSeasonId(-1);
        } else {
          setAwayTeam(team);
          setAwaySeasons(seasonsResponse.seasons);
          setAwaySelectedSeason('');
          setAwaySelectedSeasonId(-1);
        }
      } else {
        console.error(
          `Error fetching seasons for ${team.name}:`,
          seasonsResponse.error
        );
      }
    } catch (error) {
      console.error(`Error fetching seasons for ${team.name}:`, error);
    } finally {
      setIsLoadingSeasons(false);
    }
  };

  const canStartSimulation = () => {
    return (
      homeTeam &&
      awayTeam &&
      homeSelectedSeason &&
      awaySelectedSeason &&
      homeTeam.id !== awayTeam.id
    );
  };

  const startSimulation = async () => {
    if (!canStartSimulation()) return;

    const startTime = Date.now();
    setSimulationStartTime(startTime);
    setSimulation({ status: 'loading' });

    try {
      const userId = authService.getCurrentUserId();
      if (!userId) {
        throw new Error('User not authenticated');
      }

      const simulationRequest: SimulateMatchRequest = {
        homeTeamId: homeTeam!.id,
        awayTeamId: awayTeam!.id,
        homeTeamName: homeTeam!.name,
        awayTeamName: awayTeam!.name,
        homeTeamSeason: homeSelectedSeason,
        awayTeamSeason: awaySelectedSeason,
        homeSeasonId: homeSelectedSeasonId,
        awaySeasonId: awaySelectedSeasonId,
      };

      const response = await matchSimulationService.simulateMatch(
        userId,
        simulationRequest
      );

      const endTime = Date.now();
      const simulationTime = endTime - startTime;

      setSimulation({ status: 'success', data: response });
      localStorage.setItem('matchId', response.apiResponse.match_id.toString());
      localStorage.setItem(
        'simulationId',
        response.apiResponse.simulation_id.toString()
      );

      // Navigate to simulation view after a delay (simulation time x2)
      const redirectDelay = simulationTime * 2;
      setTimeout(() => {
        router.push(`/simulationview/${response.apiResponse.simulation_id}`);
      }, redirectDelay);
    } catch (error) {
      console.error('Error starting simulation:', error);
      setSimulation({
        status: 'error',
        error:
          error instanceof Error ? error.message : 'Failed to start simulation',
      });
    }
  };

  if (isLoading) {
    return (
      <div className="flex h-screen items-center justify-center bg-gradient-to-br from-emerald-900 via-green-800 to-teal-900">
        <div className="text-center text-white">
          <Loader2 size={48} className="mx-auto mb-4 animate-spin" />
          <h2 className="text-xl font-semibold">Loading Teams...</h2>
        </div>
      </div>
    );
  }

  return (
    <ProtectedRoute allowedRoles={['User', 'Admin', 'Coach', 'Player']}>
      <SidebarLayout
        sidebar={
          <>
            {/* Main Navigation */}
            <Link href="/dashboard">
              <SidebarItem
                icon={<LayoutDashboardIcon size={20} />}
                text="Dashboard"
                active
              />
            </Link>{' '}
            <Link href="/teams">
              <SidebarItem icon={<ClubIcon size={20} />} text="Teams" />
            </Link>
            <Link href="/players">
              <SidebarItem icon={<User size={20} />} text="Players" />
            </Link>
            <Link href="/coaches">
              <SidebarItem icon={<Users size={20} />} text="Coaches" />
            </Link>
            <Link href="/stadiums">
              <SidebarItem icon={<Home size={20} />} text="Stadiums" />
            </Link>
            {/* Admin Section - only show if user is admin */}
            {isAdmin && (
              <>
                <SidebarSection title="Admin" color="text-amber-600" />
                <Link href="/admin">
                  <SidebarItem
                    icon={<Settings size={20} />}
                    text="Admin Dashboard"
                  />
                </Link>{' '}
              </>
            )}
            {/* Notifications */}
            <Link href="/notifications">
              <SidebarItem icon={<Bell size={20} />} text="Notifications" />
            </Link>
            {/* Search & Settings */}
            <SidebarSection title="Other" />
            <Link href="/search">
              <SidebarItem icon={<Search size={20} />} text="Search" />
            </Link>
            <Link href="/settings">
              <SidebarItem icon={<Settings size={20} />} text="Settings" />
            </Link>
          </>
        }
      >
        <div className="relative min-h-screen flex-1">
          {' '}
          {/* Enhanced Background */}
          <div className="absolute inset-0 z-0">
            <div
              className={`absolute inset-0 ${
                isDarkMode
                  ? 'bg-gradient-to-br from-gray-900 via-gray-800 to-gray-900'
                  : 'bg-gradient-to-br from-emerald-50 via-green-50 to-teal-50'
              }`}
            ></div>
            <div className="absolute inset-0 opacity-[0.03]">
              <Image
                src="/images/Stadium dark.png"
                alt="Stadium Background"
                fill
                className="object-cover object-center"
              />
            </div>
          </div>
          <div className="relative z-10">
            <Navbar />

            <div className="p-6">
              {/* Header */}
              <div className="mb-6 flex items-center gap-4">
                {' '}
                <Link href="/dashboard">
                  <button
                    className={`rounded-full p-2 shadow-lg transition-all hover:shadow-xl ${
                      isDarkMode
                        ? 'bg-gray-800/90 hover:bg-gray-700/90'
                        : 'bg-white/90 hover:bg-white'
                    }`}
                  >
                    <ArrowLeft
                      size={24}
                      className={
                        isDarkMode ? 'text-green-400' : 'text-green-700'
                      }
                    />
                  </button>
                </Link>{' '}
                <div>
                  <h1
                    className={`text-3xl font-bold ${isDarkMode ? 'text-green-400' : 'text-green-800'}`}
                  >
                    Match Simulation
                  </h1>
                  <p className={isDarkMode ? 'text-gray-300' : 'text-gray-600'}>
                    Create and simulate custom matches between teams
                  </p>
                </div>
              </div>

              {/* Main Content */}
              <div className="grid grid-cols-1 gap-6 lg:grid-cols-3">
                {/* Team Selection */}
                <div className="space-y-6 lg:col-span-2">
                  {' '}
                  {/* Home Team Selection */}
                  <div
                    className={`rounded-2xl p-6 shadow-xl ${isDarkMode ? 'bg-gray-800/90' : 'bg-white/90'}`}
                  >
                    <div className="mb-4 flex items-center gap-2">
                      <Users className="text-green-600" size={20} />
                      <h2
                        className={`text-xl font-semibold ${isDarkMode ? 'text-green-400' : 'text-green-800'}`}
                      >
                        Home Team
                      </h2>
                    </div>
                    <div className="grid grid-cols-1 gap-4 md:grid-cols-2">
                      {' '}
                      <div>
                        <label
                          className={`mb-2 block text-sm font-medium ${isDarkMode ? 'text-gray-300' : 'text-gray-700'}`}
                        >
                          Select Team
                        </label>
                        <select
                          value={homeTeam?.id || ''}
                          onChange={(e) => {
                            const team = teams.find(
                              (t) => t.id === parseInt(e.target.value)
                            );
                            if (team) handleTeamSelection(team, 'home');
                          }}
                          className={`w-full rounded-lg border px-3 py-2 focus:ring-green-500 ${
                            isDarkMode
                              ? 'border-gray-600 bg-gray-700 text-white focus:border-green-400 focus:ring-green-400'
                              : 'border-gray-300 bg-white text-gray-900 focus:border-green-500'
                          }`}
                        >
                          <option value="">Choose home team...</option>
                          {teams.map((team) => (
                            <option key={team.id} value={team.id}>
                              {team.name}
                            </option>
                          ))}
                        </select>
                      </div>{' '}
                      <div>
                        <label
                          className={`mb-2 block text-sm font-medium ${isDarkMode ? 'text-gray-300' : 'text-gray-700'}`}
                        >
                          Select Season
                        </label>
                        <select
                          value={homeSelectedSeason}
                          onChange={(e) => {
                            const selectedSeasonName = e.target.value;
                            setHomeSelectedSeason(selectedSeasonName);

                            // Find and set the corresponding season ID
                            const selectedSeason = homeSeasons.find(
                              (season) =>
                                season.seasonName === selectedSeasonName
                            );
                            setHomeSelectedSeasonId(
                              selectedSeason ? selectedSeason.seasonId : -1
                            );
                          }}
                          disabled={!homeTeam || isLoadingSeasons}
                          className={`w-full rounded-lg border px-3 py-2 focus:ring-green-500 disabled:opacity-50 ${
                            isDarkMode
                              ? 'border-gray-600 bg-gray-700 text-white focus:border-green-400 focus:ring-green-400 disabled:bg-gray-800'
                              : 'border-gray-300 bg-white text-gray-900 focus:border-green-500 disabled:bg-gray-100'
                          }`}
                        >
                          <option value="">Choose season...</option>
                          {homeSeasons.map((season) => (
                            <option
                              key={season.seasonId}
                              value={season.seasonName}
                            >
                              {season.seasonName}
                            </option>
                          ))}
                        </select>
                      </div>
                    </div>{' '}
                    {homeTeam && (
                      <div
                        className={`mt-4 flex items-center gap-4 rounded-lg p-4 ${isDarkMode ? 'bg-green-900/50' : 'bg-green-50'}`}
                      >
                        <div
                          className={`flex h-12 w-12 items-center justify-center rounded-full ${isDarkMode ? 'bg-green-800' : 'bg-green-100'}`}
                        >
                          <ClubIcon className="text-green-600" size={24} />
                        </div>
                        <div>
                          <h3
                            className={`font-semibold ${isDarkMode ? 'text-green-400' : 'text-green-800'}`}
                          >
                            {homeTeam.name}
                          </h3>
                          <p
                            className={`text-sm ${isDarkMode ? 'text-gray-300' : 'text-gray-600'}`}
                          >
                            {homeSelectedSeason
                              ? `Season: ${homeSelectedSeason}`
                              : 'Select a season'}
                          </p>
                        </div>
                      </div>
                    )}
                  </div>{' '}
                  {/* Away Team Selection */}
                  <div
                    className={`rounded-2xl p-6 shadow-xl ${isDarkMode ? 'bg-gray-800/90' : 'bg-white/90'}`}
                  >
                    <div className="mb-4 flex items-center gap-2">
                      <Users className="text-blue-600" size={20} />
                      <h2
                        className={`text-xl font-semibold ${isDarkMode ? 'text-blue-400' : 'text-blue-800'}`}
                      >
                        Away Team
                      </h2>
                    </div>
                    <div className="grid grid-cols-1 gap-4 md:grid-cols-2">
                      {' '}
                      <div>
                        <label
                          className={`mb-2 block text-sm font-medium ${isDarkMode ? 'text-gray-300' : 'text-gray-700'}`}
                        >
                          Select Team
                        </label>
                        <select
                          value={awayTeam?.id || ''}
                          onChange={(e) => {
                            const team = teams.find(
                              (t) => t.id === parseInt(e.target.value)
                            );
                            if (team) handleTeamSelection(team, 'away');
                          }}
                          className={`w-full rounded-lg border px-3 py-2 focus:ring-blue-500 ${
                            isDarkMode
                              ? 'border-gray-600 bg-gray-700 text-white focus:border-blue-400 focus:ring-blue-400'
                              : 'border-gray-300 bg-white text-gray-900 focus:border-blue-500'
                          }`}
                        >
                          <option value="">Choose away team...</option>
                          {teams
                            .filter((team) => team.id !== homeTeam?.id)
                            .map((team) => (
                              <option key={team.id} value={team.id}>
                                {team.name}
                              </option>
                            ))}
                        </select>
                      </div>{' '}
                      <div>
                        <label
                          className={`mb-2 block text-sm font-medium ${isDarkMode ? 'text-gray-300' : 'text-gray-700'}`}
                        >
                          Select Season
                        </label>
                        <select
                          value={awaySelectedSeason}
                          onChange={(e) => {
                            const selectedSeasonName = e.target.value;
                            setAwaySelectedSeason(selectedSeasonName);

                            // Find and set the corresponding season ID
                            const selectedSeason = awaySeasons.find(
                              (season) =>
                                season.seasonName === selectedSeasonName
                            );
                            setAwaySelectedSeasonId(
                              selectedSeason ? selectedSeason.seasonId : -1
                            );
                          }}
                          disabled={!awayTeam || isLoadingSeasons}
                          className={`w-full rounded-lg border px-3 py-2 focus:ring-blue-500 disabled:opacity-50 ${
                            isDarkMode
                              ? 'border-gray-600 bg-gray-700 text-white focus:border-blue-400 focus:ring-blue-400 disabled:bg-gray-800'
                              : 'border-gray-300 bg-white text-gray-900 focus:border-blue-500 disabled:bg-gray-100'
                          }`}
                        >
                          <option value="">Choose season...</option>
                          {awaySeasons.map((season) => (
                            <option
                              key={season.seasonId}
                              value={season.seasonName}
                            >
                              {season.seasonName}
                            </option>
                          ))}
                        </select>
                      </div>
                    </div>{' '}
                    {awayTeam && (
                      <div
                        className={`mt-4 flex items-center gap-4 rounded-lg p-4 ${isDarkMode ? 'bg-blue-900/50' : 'bg-blue-50'}`}
                      >
                        <div
                          className={`flex h-12 w-12 items-center justify-center rounded-full ${isDarkMode ? 'bg-blue-800' : 'bg-blue-100'}`}
                        >
                          <ClubIcon className="text-blue-600" size={24} />
                        </div>
                        <div>
                          <h3
                            className={`font-semibold ${isDarkMode ? 'text-blue-400' : 'text-blue-800'}`}
                          >
                            {awayTeam.name}
                          </h3>
                          <p
                            className={`text-sm ${isDarkMode ? 'text-gray-300' : 'text-gray-600'}`}
                          >
                            {awaySelectedSeason
                              ? `Season: ${awaySelectedSeason}`
                              : 'Select a season'}
                          </p>
                        </div>
                      </div>
                    )}
                  </div>
                </div>

                {/* Simulation Panel */}
                <div className="space-y-6">
                  {' '}
                  {/* Match Preview */}
                  <div
                    className={`rounded-2xl p-6 shadow-xl ${isDarkMode ? 'bg-gray-800/90' : 'bg-white/90'}`}
                  >
                    <h3
                      className={`mb-4 text-lg font-semibold ${isDarkMode ? 'text-white' : 'text-gray-800'}`}
                    >
                      Match Preview
                    </h3>

                    {homeTeam && awayTeam ? (
                      <div className="space-y-4">
                        <div className="text-center">
                          {' '}
                          <div
                            className={`mb-4 text-2xl font-bold ${isDarkMode ? 'text-white' : 'text-gray-800'}`}
                          >
                            {homeTeam.name}
                            <span
                              className={`mx-2 ${isDarkMode ? 'text-gray-400' : 'text-gray-400'}`}
                            >
                              VS
                            </span>
                            {awayTeam.name}
                          </div>
                          <div
                            className={`text-sm ${isDarkMode ? 'text-gray-300' : 'text-gray-600'}`}
                          >
                            {homeSelectedSeason && awaySelectedSeason ? (
                              <div className="space-y-1">
                                <div>Home: {homeSelectedSeason}</div>
                                <div>Away: {awaySelectedSeason}</div>
                              </div>
                            ) : (
                              'Select seasons for both teams'
                            )}
                          </div>
                        </div>

                        {/* Start Simulation Button */}
                        <button
                          onClick={startSimulation}
                          disabled={
                            !canStartSimulation() ||
                            simulation.status === 'loading'
                          }
                          className={`w-full rounded-xl px-6 py-4 font-semibold text-white transition-all duration-300 ${
                            canStartSimulation() &&
                            simulation.status !== 'loading'
                              ? 'bg-gradient-to-r from-green-600 to-blue-600 hover:scale-105 hover:from-green-700 hover:to-blue-700 hover:shadow-xl'
                              : 'cursor-not-allowed bg-gray-400'
                          }`}
                        >
                          {simulation.status === 'loading' ? (
                            <div className="flex items-center justify-center gap-2">
                              <Loader2 size={20} className="animate-spin" />
                              Starting Simulation...
                            </div>
                          ) : (
                            <div className="flex items-center justify-center gap-2">
                              <Play size={20} />
                              Start Simulation
                            </div>
                          )}
                        </button>
                      </div>
                    ) : (
                      <div
                        className={`py-8 text-center ${isDarkMode ? 'text-gray-400' : 'text-gray-500'}`}
                      >
                        <Play
                          size={48}
                          className="mx-auto mb-4 text-gray-300"
                        />
                        <p>Select both teams to preview the match</p>
                      </div>
                    )}
                  </div>
                  {/* Simulation Status */}
                  {simulation.status !== 'idle' && (
                    <div className="rounded-2xl bg-white/90 p-6 shadow-xl">
                      <h3 className="mb-4 text-lg font-semibold text-gray-800">
                        Simulation Status
                      </h3>

                      {simulation.status === 'loading' && (
                        <div className="py-4 text-center">
                          <Loader2
                            size={32}
                            className="mx-auto mb-3 animate-spin text-blue-600"
                          />
                          <p className="font-medium text-blue-600">
                            Initializing match simulation...
                          </p>
                          <p
                            className={`mt-1 text-sm ${isDarkMode ? 'text-gray-400' : 'text-gray-500'}`}
                          >
                            This may take a few moments
                          </p>
                        </div>
                      )}

                      {simulation.status === 'success' && simulation.data && (
                        <div className="py-4 text-center">
                          <CheckCircle
                            size={32}
                            className="mx-auto mb-3 text-green-600"
                          />
                          <p className="mb-2 font-medium text-green-600">
                            Simulation Started Successfully!
                          </p>
                          <div
                            className={`space-y-1 text-sm ${isDarkMode ? 'text-gray-300' : 'text-gray-600'}`}
                          >
                            <p>
                              Match ID: {simulation.data.apiResponse.match_id}
                            </p>
                            <p>
                              Simulation ID:{' '}
                              {simulation.data.apiResponse.simulation_id}
                            </p>
                            <p>Status: {simulation.data.apiResponse.status}</p>
                            {simulationStartTime && (
                              <p>
                                Setup Time: {Date.now() - simulationStartTime}ms
                              </p>
                            )}
                          </div>
                          <div className="mt-4 rounded-lg bg-blue-50 p-3">
                            <p className="text-sm text-blue-800">
                              {simulationStartTime
                                ? `Redirecting to simulation view in ${Math.ceil(((Date.now() - simulationStartTime) * 2) / 1000)} seconds...`
                                : 'Redirecting to simulation view...'}
                            </p>
                          </div>
                        </div>
                      )}

                      {simulation.status === 'error' && (
                        <div className="py-4 text-center">
                          <AlertCircle
                            size={32}
                            className="mx-auto mb-3 text-red-600"
                          />
                          <p className="mb-2 font-medium text-red-600">
                            Simulation Failed
                          </p>
                          <p
                            className={`text-sm ${isDarkMode ? 'text-gray-300' : 'text-gray-600'}`}
                          >
                            {simulation.error}
                          </p>
                          <button
                            onClick={() => setSimulation({ status: 'idle' })}
                            className="mt-4 rounded-lg bg-red-600 px-4 py-2 text-white hover:bg-red-700"
                          >
                            Try Again
                          </button>
                        </div>
                      )}
                    </div>
                  )}{' '}
                  {/* Tips */}
                  <div
                    className={`rounded-2xl border p-6 shadow-xl ${
                      isDarkMode
                        ? 'border-yellow-600/30 bg-gradient-to-br from-yellow-900/30 to-orange-900/30'
                        : 'border-yellow-200 bg-gradient-to-br from-yellow-50 to-orange-50'
                    }`}
                  >
                    <div className="mb-3 flex items-center gap-2">
                      <Zap
                        className={
                          isDarkMode ? 'text-yellow-400' : 'text-yellow-600'
                        }
                        size={20}
                      />
                      <h3
                        className={`text-lg font-semibold ${isDarkMode ? 'text-yellow-400' : 'text-yellow-800'}`}
                      >
                        Tips
                      </h3>
                    </div>
                    <ul
                      className={`space-y-2 text-sm ${isDarkMode ? 'text-yellow-300' : 'text-yellow-700'}`}
                    >
                      <li>• Different seasons can create unique matchups</li>
                      <li>• Teams cannot play against themselves</li>
                      <li>
                        • Simulations may take a few moments to initialize
                      </li>
                      <li>
                        • You'll be redirected to watch the live simulation
                      </li>
                    </ul>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </SidebarLayout>
    </ProtectedRoute>
  );
}
