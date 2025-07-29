'use client';
import React, { useState, useEffect, Suspense } from 'react';
import { useSearchParams, useRouter } from 'next/navigation';
import { MatchStats } from '@/app/Components/MatchStats/MatchStats';
import Sidebar, { SidebarLayout } from '@/app/Components/Sidebar/Sidebar';
import { SidebarItem } from '@/app/Components/Sidebar/SidebarItem';
import {
  Calendar,
  ClubIcon,
  LayoutDashboardIcon,
  LogOutIcon,
  Package,
  Settings,
} from 'lucide-react';
import Navbar from '@/app/Components/Navbar/Navbar';
import { LogoBackground } from '@/app/Components/Logo3d/logo3d';
import Link from 'next/link';
import matchService, { MatchDetail } from '@/Services/MatchService';
import ProtectedRoute from '@/app/Components/ProtectedRoute/ProtectedRoute';
import liveMatchStorageService from '@/Services/LiveMatchStorageService';

function MatchDetailsContent() {
  const [activeTab, setActiveTab] = useState<'stats' | 'lineup' | 'info'>(
    'stats'
  );
  const [matchData, setMatchData] = useState<MatchDetail | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string>('');
  const [isTabChanging, setIsTabChanging] = useState(false);
  const [isRefreshing, setIsRefreshing] = useState(false);
  const [autoRefresh, setAutoRefresh] = useState(false);
  const [lastRefreshed, setLastRefreshed] = useState<Date | null>(null);
  const searchParams = useSearchParams();
  const router = useRouter();
  const matchId = searchParams.get('matchId');
  const simulationId = localStorage.getItem('simulationId');

  // Enhanced tab change handler with animation
  const handleTabChange = (tab: 'stats' | 'lineup' | 'info') => {
    if (tab === activeTab) return;

    setIsTabChanging(true);
    setTimeout(() => {
      setActiveTab(tab);
      setTimeout(() => setIsTabChanging(false), 100);
    }, 200);
  };

  // Function to refresh match stats
  const refreshStats = async () => {
    if (!matchId || isRefreshing) return;

    try {
      setIsRefreshing(true);
      const details = await matchService.getMatchDetails(parseInt(matchId));
      setMatchData(details);
      setLastRefreshed(new Date());

      // Show a brief success feedback
      const refreshButton = document.getElementById('refresh-stats-btn');
      if (refreshButton) {
        refreshButton.classList.add('animate-pulse');
        setTimeout(() => {
          refreshButton.classList.remove('animate-pulse');
        }, 1000);
      }
    } catch (err) {
      console.error('Error refreshing match details:', err);
      setError('Failed to refresh match details');
    } finally {
      setIsRefreshing(false);
    }
  };

  // Auto-refresh effect for live matches
  useEffect(() => {
    let intervalId: NodeJS.Timeout;

    if (
      (autoRefresh && matchData?.matchStatus === 'Live') ||
      (matchData?.isLive == true && !isRefreshing)
    ) {
      intervalId = setInterval(() => {
        refreshStats();
      }, 30000); // Refresh every 30 seconds
    }

    return () => {
      if (intervalId) {
        clearInterval(intervalId);
      }
    };
  }, [autoRefresh, matchData?.matchStatus, isRefreshing, matchId]);
  useEffect(() => {
    const fetchMatchDetails = async () => {
      if (!matchId) {
        setError('No match ID provided');
        setLoading(false);
        return;
      }

      try {
        setLoading(true);
        const details = await matchService.getMatchDetails(parseInt(matchId));
        setMatchData(details);
        setLastRefreshed(new Date()); // Set initial load time
      } catch (err) {
        console.error('Error fetching match details:', err);
        setError('Failed to load match details');
      } finally {
        setLoading(false);
      }
    };

    fetchMatchDetails();
  }, [matchId]);

  if (loading) {
    return (
      <div className="flex h-screen items-center justify-center">
        <div className="h-8 w-8 animate-spin rounded-full border-t-2 border-b-2 border-[#4CAF50]"></div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="flex h-screen flex-col items-center justify-center">
        <div className="mb-4 text-red-500">⚠️ {error}</div>
        <Link href="/dashboard">
          <button className="btn btn-primary">Back to Dashboard</button>
        </Link>
      </div>
    );
  }

  return (
    <ProtectedRoute allowedRoles={['User', 'Admin', 'Coach', 'Player']}>
      <SidebarLayout
        sidebar={
          <>
            <SidebarItem
              icon={<LayoutDashboardIcon size={20} />}
              text="Dashboard"
            />
            <SidebarItem icon={<ClubIcon size={20} />} text="Teams" />
            <SidebarItem icon={<Package size={20} />} text="Products" alert />
            <SidebarItem icon={<Settings size={20} />} text="Settings" />
            <SidebarItem icon={<LogOutIcon size={20} />} text="Logout" />
          </>
        }
      >
        <div
          className="relative min-h-screen w-full flex-1"
          style={{
            backgroundImage: "url('/images/greenPitch.jpg')",
            backgroundSize: 'cover', // Ensures the image covers the div
            backgroundPosition: 'center', // Centers the image
            backgroundRepeat: 'no-repeat', // Prevents tiling
          }}
        >
          <Navbar></Navbar>

          {/* Enhanced Floating Logo Backgrounds - Repositioned to avoid overlap */}
          <div className="pointer-events-none fixed inset-0 z-0 overflow-hidden opacity-20">
            {/* Home team logo - positioned in top left */}
            <div className="animate-float-slow absolute top-20 left-4 w-1/4">
              <div className="absolute inset-0 rounded-full bg-gradient-to-br from-blue-400/30 to-purple-400/30 blur-3xl"></div>
              <LogoBackground
                logoUrl={matchData?.homeTeam.logo || '/logos/barcelona.png'}
              />
            </div>

            {/* Away team logo - positioned in bottom right */}
            <div className="animate-float-delayed absolute right-4 bottom-20 w-1/4">
              <div className="absolute inset-0 rounded-full bg-gradient-to-br from-red-400/30 to-pink-400/30 blur-3xl"></div>
              <LogoBackground
                logoUrl={matchData?.awayTeam.logo || '/logos/real madrid.png'}
              />
            </div>
          </div>
          {/* Enhanced Container with Glass Effect */}
          <div className="mx-auto w-full max-w-4xl px-6 py-10">
            {/* Match Header */}
            <div className="mb-8 text-center">
              <div className="relative">
                <div className="absolute inset-0 rounded-full bg-gradient-to-r from-blue-400/20 via-purple-400/20 to-pink-400/20 blur-2xl"></div>
                <h1 className="relative bg-gradient-to-r from-blue-600 via-purple-600 to-pink-600 bg-clip-text text-4xl font-bold text-transparent">
                  Match Details
                </h1>
              </div>
              {matchData && (
                <div className="mt-4 flex items-center justify-center gap-4">
                  <div className="flex items-center gap-2">
                    <img
                      src={matchData.homeTeam.logo || '/logos/barcelona.png'}
                      alt={matchData.homeTeam.name}
                      className="h-8 w-8 rounded-full border-2 border-white/30 shadow-lg"
                    />
                    <span className="text-lg font-semibold text-white">
                      {matchData.homeTeam.shortName || matchData.homeTeam.name}
                    </span>
                  </div>
                  <div className="rounded-lg border border-white/20 bg-black/40 px-4 py-2 backdrop-blur-sm">
                    <span className="text-2xl font-bold text-white">
                      {matchData.homeTeamScore || 0} -{' '}
                      {matchData.awayTeamScore || 0}
                    </span>
                  </div>
                  <div className="flex items-center gap-2">
                    <span className="text-lg font-semibold text-white">
                      {matchData.awayTeam.shortName || matchData.awayTeam.name}
                    </span>
                    <img
                      src={matchData.awayTeam.logo || '/logos/real madrid.png'}
                      alt={matchData.awayTeam.name}
                      className="h-8 w-8 rounded-full border-2 border-white/30 shadow-lg"
                    />
                  </div>
                </div>
              )}

              {/* Live Match Status and Refresh Button */}
              {matchData &&
                (matchData.matchStatus === 'Live' ||
                  matchData.isLive == true) && (
                  <div className="mt-4 flex flex-col items-center gap-3">
                    {/* Live Indicator */}
                    <div className="flex items-center gap-2 rounded-full border border-red-400/30 bg-red-500/20 px-3 py-1 backdrop-blur-sm">
                      <div className="h-2 w-2 animate-pulse rounded-full bg-red-500"></div>
                      <span className="text-sm font-semibold text-red-400">
                        LIVE
                      </span>
                    </div>

                    {/* Refresh Controls */}
                    <div className="flex items-center gap-3">
                      {/* Manual Refresh Button */}
                      <button
                        id="refresh-stats-btn"
                        onClick={refreshStats}
                        disabled={isRefreshing}
                        className="group relative overflow-hidden rounded-lg border border-blue-400/30 bg-blue-500/20 px-4 py-2 text-sm font-semibold text-blue-400 backdrop-blur-sm transition-all duration-300 hover:bg-blue-500/30 hover:text-blue-300 disabled:cursor-not-allowed disabled:opacity-50"
                      >
                        <div className="flex items-center gap-2">
                          <svg
                            className={`h-4 w-4 ${isRefreshing ? 'animate-spin' : 'group-hover:rotate-180'} transition-transform duration-500`}
                            fill="none"
                            stroke="currentColor"
                            viewBox="0 0 24 24"
                          >
                            <path
                              strokeLinecap="round"
                              strokeLinejoin="round"
                              strokeWidth={2}
                              d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15"
                            />
                          </svg>
                          {isRefreshing ? 'Refreshing...' : 'Refresh Stats'}
                        </div>

                        {/* Hover effect background */}
                        <div className="absolute inset-0 -translate-x-full transform bg-gradient-to-r from-transparent via-white/10 to-transparent transition-transform duration-1000 group-hover:translate-x-full"></div>
                      </button>

                      {/* Auto-refresh Toggle */}
                      <div className="flex items-center gap-2 rounded-lg border border-green-400/30 bg-green-500/20 px-3 py-2 backdrop-blur-sm">
                        <label className="flex cursor-pointer items-center gap-2">
                          <input
                            type="checkbox"
                            checked={autoRefresh}
                            onChange={(e) => setAutoRefresh(e.target.checked)}
                            className="h-4 w-4 rounded border-green-400/50 bg-green-500/20 text-green-400 focus:ring-green-400/50 focus:ring-offset-0"
                          />
                          <span className="text-sm font-medium text-green-400">
                            Auto-refresh (30s)
                          </span>
                        </label>
                      </div>

                      {/* Simulation View Button */}
                      <button
                        onClick={() => {
                          const simulationUrl =
                            liveMatchStorageService.getSimulationViewUrl();
                          if (simulationUrl) {
                            router.push(simulationUrl);
                          } else if (
                            matchId &&
                            liveMatchStorageService.isCurrentLiveMatch(
                              parseInt(matchId)
                            )
                          ) {
                            // This is the current live match, attempt to navigate using matchId
                            router.push(`/simulationview/${simulationId}`);
                          }
                        }}
                        className="group relative overflow-hidden rounded-lg border border-purple-400/30 bg-purple-500/20 px-4 py-2 text-sm font-semibold text-purple-400 backdrop-blur-sm transition-all duration-300 hover:bg-purple-500/30 hover:text-purple-300"
                      >
                        <div className="flex items-center gap-2">
                          <svg
                            className="h-4 w-4 transition-transform duration-300 group-hover:scale-110"
                            fill="none"
                            stroke="currentColor"
                            viewBox="0 0 24 24"
                          >
                            <path
                              strokeLinecap="round"
                              strokeLinejoin="round"
                              strokeWidth={2}
                              d="M13 10V3L4 14h7v7l9-11h-7z"
                            />
                          </svg>
                          Live Simulation
                        </div>

                        {/* Hover effect background */}
                        <div className="absolute inset-0 -translate-x-full transform bg-gradient-to-r from-transparent via-white/10 to-transparent transition-transform duration-1000 group-hover:translate-x-full"></div>
                      </button>
                    </div>

                    {/* Auto-refresh Status Indicator */}
                    {autoRefresh && (
                      <div className="flex items-center gap-2 text-xs text-green-400/70">
                        <div className="h-1.5 w-1.5 animate-pulse rounded-full bg-green-400"></div>
                        <span>Auto-refreshing every 30 seconds</span>
                      </div>
                    )}
                  </div>
                )}
            </div>

            {/* Enhanced Tabs Navigation */}
            <div className="mb-8 flex justify-center">
              <div className="relative rounded-2xl border border-white/20 bg-black/30 p-1 shadow-2xl backdrop-blur-xl">
                {/* Background Slider */}
                <div
                  className={`absolute top-1 h-12 rounded-xl bg-gradient-to-r from-blue-500/80 to-purple-500/80 shadow-lg backdrop-blur-sm transition-all duration-500 ease-out ${
                    activeTab === 'stats'
                      ? 'left-1 w-24'
                      : activeTab === 'lineup'
                        ? 'left-[calc(33.33%+0.125rem)] w-24'
                        : 'left-[calc(66.66%+0.125rem)] w-20'
                  }`}
                />

                {/* Tab Buttons */}
                <div className="relative flex">
                  <button
                    onClick={() => handleTabChange('stats')}
                    disabled={isTabChanging}
                    className={`relative z-10 rounded-xl px-6 py-3 text-sm font-bold transition-all duration-300 ease-out ${
                      activeTab === 'stats'
                        ? 'text-white shadow-lg'
                        : 'text-white/70 hover:bg-white/10 hover:text-white'
                    } ${isTabChanging ? 'pointer-events-none' : ''}`}
                  >
                    <span className="flex items-center gap-2">📊 Stats</span>
                  </button>

                  <button
                    onClick={() => handleTabChange('lineup')}
                    disabled={isTabChanging}
                    className={`relative z-10 rounded-xl px-6 py-3 text-sm font-bold transition-all duration-300 ease-out ${
                      activeTab === 'lineup'
                        ? 'text-white shadow-lg'
                        : 'text-white/70 hover:bg-white/10 hover:text-white'
                    } ${isTabChanging ? 'pointer-events-none' : ''}`}
                  >
                    <span className="flex items-center gap-2">👥 Lineup</span>
                  </button>

                  <button
                    onClick={() => handleTabChange('info')}
                    disabled={isTabChanging}
                    className={`relative z-10 rounded-xl px-5 py-3 text-sm font-bold transition-all duration-300 ease-out ${
                      activeTab === 'info'
                        ? 'text-white shadow-lg'
                        : 'text-white/70 hover:bg-white/10 hover:text-white'
                    } ${isTabChanging ? 'pointer-events-none' : ''}`}
                  >
                    <span className="flex items-center gap-2">ℹ️ Info</span>
                  </button>
                </div>
              </div>
            </div>

            {/* Enhanced Tabs Content */}
            <div className="relative">
              {/* Content Container with Glass Effect */}
              <div className="relative overflow-hidden rounded-3xl border border-white/20 bg-black/40 shadow-2xl backdrop-blur-xl">
                {/* Animated Background Gradient */}
                <div className="absolute inset-0 animate-pulse bg-gradient-to-br from-blue-400/10 via-purple-400/5 to-pink-400/10"></div>

                {/* Decorative Corner Elements */}
                <div className="absolute top-4 right-4 h-2 w-2 animate-pulse rounded-full bg-gradient-to-br from-blue-400/60 to-purple-400/60"></div>
                <div className="absolute bottom-4 left-4 h-1.5 w-1.5 animate-pulse rounded-full bg-gradient-to-br from-purple-400/60 to-pink-400/60 delay-300"></div>

                {/* Tab Content */}
                <div className="relative z-10 p-8">
                  {/* Stats Tab */}
                  <div
                    className={`transition-all duration-500 ease-in-out ${
                      activeTab === 'stats'
                        ? 'translate-x-0 transform opacity-100'
                        : 'pointer-events-none absolute inset-0 translate-x-4 transform opacity-0'
                    }`}
                  >
                    {activeTab === 'stats' && (
                      <div className="space-y-6">
                        {/* Stats Header */}
                        <div className="mb-6 text-center">
                          <h2 className="mb-2 text-2xl font-bold text-white">
                            Match Statistics
                          </h2>
                          <div className="mx-auto h-1 w-24 rounded-full bg-gradient-to-r from-blue-500 to-purple-500"></div>

                          {/* Last Refreshed Indicator */}
                          {matchData?.matchStatus === 'Live' ||
                            (matchData?.isLive == true && lastRefreshed && (
                              <div className="mt-3 flex items-center justify-center gap-2 text-xs text-white/60">
                                <svg
                                  className="h-3 w-3"
                                  fill="none"
                                  stroke="currentColor"
                                  viewBox="0 0 24 24"
                                >
                                  <path
                                    strokeLinecap="round"
                                    strokeLinejoin="round"
                                    strokeWidth={2}
                                    d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z"
                                  />
                                </svg>
                                <span>
                                  Last updated:{' '}
                                  {lastRefreshed.toLocaleTimeString()}
                                </span>
                              </div>
                            ))}
                        </div>

                        <MatchStats
                          teamA={{
                            name:
                              matchData?.homeTeam.shortName ||
                              matchData?.homeTeam.name ||
                              'Barcelona',
                            stats: [
                              matchData?.homeTeamShots || 0,
                              matchData?.homeTeamShotsOnTarget || 0,
                              `${matchData?.homeTeamPossession || 0}%`,
                              matchData?.homeTeamPasses || 0,
                              `${matchData?.homeTeamPassAccuracy || 0}%`,
                              matchData?.homeTeamFouls || 0,
                              matchData?.homeTeamYellowCards || 0,
                              matchData?.homeTeamRedCards || 0,
                              matchData?.homeTeamOffsides || 0,
                              matchData?.homeTeamCorners || 0,
                            ],
                            color:
                              matchData?.homeTeam.primaryColor || '#3b82f6',
                            logoUrl:
                              matchData?.homeTeam.logo ||
                              '/logos/barcelona.png',
                          }}
                          teamB={{
                            name:
                              matchData?.awayTeam.shortName ||
                              matchData?.awayTeam.name ||
                              'Real Madrid',
                            stats: [
                              matchData?.awayTeamShots || 0,
                              matchData?.awayTeamShotsOnTarget || 0,
                              `${matchData?.awayTeamPossession || 0}%`,
                              matchData?.awayTeamPasses || 0,
                              `${matchData?.awayTeamPassAccuracy || 0}%`,
                              matchData?.awayTeamFouls || 0,
                              matchData?.awayTeamYellowCards || 0,
                              matchData?.awayTeamRedCards || 0,
                              matchData?.awayTeamOffsides || 0,
                              matchData?.awayTeamCorners || 0,
                            ],
                            color:
                              matchData?.awayTeam.primaryColor || '#ef4444',
                            logoUrl:
                              matchData?.awayTeam.logo ||
                              '/logos/real madrid.png',
                          }}
                          labels={[
                            'Shots',
                            'Shots on target',
                            'Possession',
                            'Passes',
                            'Pass accuracy',
                            'Fouls',
                            'Yellow cards',
                            'Red cards',
                            'Offsides',
                            'Corners',
                          ]}
                        />
                      </div>
                    )}
                  </div>

                  {/* Lineup Tab */}
                  <div
                    className={`transition-all duration-500 ease-in-out ${
                      activeTab === 'lineup'
                        ? 'translate-x-0 transform opacity-100'
                        : 'pointer-events-none absolute inset-0 translate-x-4 transform opacity-0'
                    }`}
                  >
                    {activeTab === 'lineup' && (
                      <div className="space-y-6">
                        {/* Lineup Header */}
                        <div className="mb-8 text-center">
                          <h2 className="mb-2 text-2xl font-bold text-white">
                            Team Lineups
                          </h2>
                          <div className="mx-auto h-1 w-24 rounded-full bg-gradient-to-r from-green-500 to-blue-500"></div>
                        </div>

                        {/* Lineup Content */}
                        <div className="flex min-h-[400px] items-center justify-center">
                          <div className="space-y-4 text-center">
                            {/* Football Field Illustration */}
                            <div className="relative mx-auto flex h-40 w-64 items-center justify-center rounded-2xl border-2 border-white/20 bg-gradient-to-br from-green-500/20 to-green-600/20">
                              <div className="absolute inset-4 rounded-lg border-2 border-white/30"></div>
                              <div className="h-4 w-4 rounded-full bg-white/60"></div>
                              <div className="absolute top-1/2 left-0 h-16 w-8 -translate-y-1/2 transform rounded-r-lg border-2 border-white/30"></div>
                              <div className="absolute top-1/2 right-0 h-16 w-8 -translate-y-1/2 transform rounded-l-lg border-2 border-white/30"></div>
                            </div>

                            <div className="space-y-2">
                              <h3 className="text-xl font-semibold text-white">
                                ⚽ Lineup Coming Soon
                              </h3>
                              <p className="text-white/70">
                                Detailed team formations and player positions
                                will be available here.
                              </p>
                              <div className="mt-4 flex justify-center gap-2">
                                <div className="h-2 w-2 animate-bounce rounded-full bg-blue-500/60"></div>
                                <div className="h-2 w-2 animate-bounce rounded-full bg-purple-500/60 delay-100"></div>
                                <div className="h-2 w-2 animate-bounce rounded-full bg-pink-500/60 delay-200"></div>
                              </div>
                            </div>
                          </div>
                        </div>
                      </div>
                    )}
                  </div>

                  {/* Info Tab */}
                  <div
                    className={`transition-all duration-500 ease-in-out ${
                      activeTab === 'info'
                        ? 'translate-x-0 transform opacity-100'
                        : 'pointer-events-none absolute inset-0 translate-x-4 transform opacity-0'
                    }`}
                  >
                    {activeTab === 'info' && (
                      <div className="space-y-6">
                        {/* Info Header */}
                        <div className="mb-8 text-center">
                          <h2 className="mb-2 text-2xl font-bold text-white">
                            Match Information
                          </h2>
                          <div className="mx-auto h-1 w-24 rounded-full bg-gradient-to-r from-emerald-500 to-cyan-500"></div>
                        </div>

                        {/* Match Info Grid */}
                        <div className="grid grid-cols-1 gap-6 md:grid-cols-2">
                          {/* Match Details */}
                          <div className="space-y-4">
                            <h3 className="mb-4 flex items-center gap-2 text-lg font-semibold text-white">
                              📋 Match Details
                            </h3>

                            <div className="space-y-3">
                              <div className="flex items-center justify-between rounded-lg border border-white/10 bg-white/5 p-3">
                                <span className="text-white/70">Match ID</span>
                                <span className="font-semibold text-white">
                                  #{matchId || 'N/A'}
                                </span>
                              </div>

                              <div className="flex items-center justify-between rounded-lg border border-white/10 bg-white/5 p-3">
                                <span className="text-white/70">
                                  Competition
                                </span>
                                <span className="font-semibold text-white">
                                  Premier League
                                </span>
                              </div>

                              <div className="flex items-center justify-between rounded-lg border border-white/10 bg-white/5 p-3">
                                <span className="text-white/70">Stadium</span>
                                <span className="font-semibold text-white">
                                  Camp Nou
                                </span>
                              </div>

                              <div className="flex items-center justify-between rounded-lg border border-white/10 bg-white/5 p-3">
                                <span className="text-white/70">Referee</span>
                                <span className="font-semibold text-white">
                                  Anthony Taylor
                                </span>
                              </div>
                            </div>
                          </div>

                          {/* Team Information */}
                          <div className="space-y-4">
                            <h3 className="mb-4 flex items-center gap-2 text-lg font-semibold text-white">
                              👥 Team Information
                            </h3>

                            <div className="space-y-4">
                              {/* Home Team */}
                              <div className="rounded-lg border border-white/10 bg-white/5 p-4">
                                <div className="mb-3 flex items-center gap-3">
                                  <img
                                    src={
                                      matchData?.homeTeam.logo ||
                                      '/logos/barcelona.png'
                                    }
                                    alt="Home Team"
                                    className="h-8 w-8 rounded-full"
                                  />
                                  <div>
                                    <div className="font-semibold text-white">
                                      {matchData?.homeTeam.name || 'Barcelona'}
                                    </div>
                                    <div className="text-sm text-white/60">
                                      Home Team
                                    </div>
                                  </div>
                                </div>
                                <div className="grid grid-cols-2 gap-2 text-sm">
                                  <div className="text-white/70">Manager:</div>
                                  <div className="text-white">
                                    Xavi Hernández
                                  </div>
                                  <div className="text-white/70">
                                    Formation:
                                  </div>
                                  <div className="text-white">4-3-3</div>
                                </div>
                              </div>

                              {/* Away Team */}
                              <div className="rounded-lg border border-white/10 bg-white/5 p-4">
                                <div className="mb-3 flex items-center gap-3">
                                  <img
                                    src={
                                      matchData?.awayTeam.logo ||
                                      '/logos/real madrid.png'
                                    }
                                    alt="Away Team"
                                    className="h-8 w-8 rounded-full"
                                  />
                                  <div>
                                    <div className="font-semibold text-white">
                                      {matchData?.awayTeam.name ||
                                        'Real Madrid'}
                                    </div>
                                    <div className="text-sm text-white/60">
                                      Away Team
                                    </div>
                                  </div>
                                </div>
                                <div className="grid grid-cols-2 gap-2 text-sm">
                                  <div className="text-white/70">Manager:</div>
                                  <div className="text-white">
                                    Carlo Ancelotti
                                  </div>
                                  <div className="text-white/70">
                                    Formation:
                                  </div>
                                  <div className="text-white">4-3-3</div>
                                </div>
                              </div>
                            </div>
                          </div>
                        </div>

                        {/* Weather & Conditions */}
                        <div className="mt-8 rounded-xl border border-white/20 bg-gradient-to-r from-blue-500/10 to-purple-500/10 p-6">
                          <h3 className="mb-4 flex items-center gap-2 text-lg font-semibold text-white">
                            🌤️ Match Conditions
                          </h3>
                          <div className="grid grid-cols-2 gap-4 md:grid-cols-4">
                            <div className="rounded-lg bg-white/5 p-3 text-center">
                              <div className="mb-1 text-2xl">☀️</div>
                              <div className="text-xs text-white/70">
                                Weather
                              </div>
                              <div className="text-sm font-semibold text-white">
                                Sunny
                              </div>
                            </div>
                            <div className="rounded-lg bg-white/5 p-3 text-center">
                              <div className="mb-1 text-2xl">🌡️</div>
                              <div className="text-xs text-white/70">
                                Temperature
                              </div>
                              <div className="text-sm font-semibold text-white">
                                22°C
                              </div>
                            </div>
                            <div className="rounded-lg bg-white/5 p-3 text-center">
                              <div className="mb-1 text-2xl">💨</div>
                              <div className="text-xs text-white/70">Wind</div>
                              <div className="text-sm font-semibold text-white">
                                Light
                              </div>
                            </div>
                            <div className="rounded-lg bg-white/5 p-3 text-center">
                              <div className="mb-1 text-2xl">👥</div>
                              <div className="text-xs text-white/70">
                                Attendance
                              </div>
                              <div className="text-sm font-semibold text-white">
                                99,354
                              </div>
                            </div>
                          </div>
                        </div>
                      </div>
                    )}
                  </div>
                </div>
              </div>
            </div>
          </div>

          {/* Enhanced Action Buttons */}
          <div className="relative z-10 mt-12 flex justify-center gap-4">
            <Link href="/dashboard">
              <button className="group relative overflow-hidden rounded-2xl bg-gradient-to-r from-blue-600 to-purple-600 px-8 py-4 font-bold text-white shadow-2xl transition-all duration-300 hover:scale-105 hover:shadow-blue-500/25">
                <div className="absolute inset-0 bg-gradient-to-r from-blue-700 to-purple-700 opacity-0 transition-opacity duration-300 group-hover:opacity-100"></div>
                <span className="relative flex items-center gap-2">
                  🏠 Back to Dashboard
                </span>
              </button>
            </Link>

            <button
              onClick={() => window.location.reload()}
              className="group relative overflow-hidden rounded-2xl bg-gradient-to-r from-emerald-600 to-teal-600 px-8 py-4 font-bold text-white shadow-2xl transition-all duration-300 hover:scale-105 hover:shadow-emerald-500/25"
            >
              <div className="absolute inset-0 bg-gradient-to-r from-emerald-700 to-teal-700 opacity-0 transition-opacity duration-300 group-hover:opacity-100"></div>
              <span className="relative flex items-center gap-2">
                🔄 Refresh Match
              </span>
            </button>
          </div>

          {/* Additional Floating Elements */}
          <div className="pointer-events-none absolute inset-0 overflow-hidden">
            {/* Top floating particles */}
            <div className="animate-float absolute top-20 left-1/4 h-2 w-2 rounded-full bg-blue-400/40"></div>
            <div className="animate-float-delayed absolute top-40 right-1/3 h-1.5 w-1.5 rounded-full bg-purple-400/40"></div>
            <div className="animate-float-slow absolute top-60 left-1/2 h-1 w-1 rounded-full bg-pink-400/40"></div>

            {/* Bottom floating particles */}
            <div className="animate-float absolute right-1/4 bottom-32 h-2 w-2 rounded-full bg-emerald-400/40"></div>
            <div className="animate-float-delayed absolute bottom-48 left-1/3 h-1.5 w-1.5 rounded-full bg-teal-400/40"></div>
            <div className="animate-float-slow absolute right-1/2 bottom-64 h-1 w-1 rounded-full bg-cyan-400/40"></div>
          </div>

          {/*<div className=" w-210" >*/}
          {/*    */}
          {/*</div>*/}
        </div>

        {/*<main className="flex-1 bg-gray-50 p-6">{children}</main>*/}
      </SidebarLayout>
    </ProtectedRoute>
  );
}

export default function MatchDetailsPage() {
  return (
    <ProtectedRoute allowedRoles={['User', 'Admin', 'Coach', 'Player']}>
      <Suspense
        fallback={
          <div className="flex min-h-screen items-center justify-center bg-gray-100">
            <div className="text-center">
              <div className="mx-auto h-32 w-32 animate-spin rounded-full border-b-2 border-green-500"></div>
              <p className="mt-4 text-gray-600">Loading match details...</p>
            </div>
          </div>
        }
      >
        <MatchDetailsContent />
      </Suspense>
    </ProtectedRoute>
  );
}
