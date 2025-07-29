'use client';

import Image from 'next/image';
import { useState, useEffect, useMemo } from 'react';
import { useRouter } from 'next/navigation';
import { useSettings } from '../../contexts/EnhancedSettingsContext';
import matchService, {
  LiveMatch,
  LiveMatchResponse,
} from '@/Services/MatchService';
import signalRService, { MatchStatistics } from '@/Services/SignalRService';
import authService from '@/Services/AuthenticationService';
import liveMatchStorageService from '@/Services/LiveMatchStorageService';

interface StatBarProps {
  leftValue: number;
  rightValue: number;
  label: string;
  leftColor: string;
  rightColor: string;
}

interface LiveMatchPanelProps {
  userId?: string;
  homeTeam?: {
    name: string;
    logo: string;
  };
  awayTeam?: {
    name: string;
    logo: string;
  };
  homeScore?: number;
  awayScore?: number;
  stats?: Array<{
    label: string;
    homeValue: number;
    awayValue: number;
  }>;
  matchTime?: string;
}

const StatBar = ({
  leftValue,
  rightValue,
  label,
  leftColor,
  rightColor,
}: StatBarProps) => {
  const { isDarkMode } = useSettings();
  const total = leftValue + rightValue || 1;
  const leftPercent = (leftValue / total) * 100;
  const rightPercent = (rightValue / total) * 100;

  return (
    <div className="my-3 w-full text-xs">
      <div
        className={`mb-2 flex items-center justify-between px-2 font-medium ${
          isDarkMode ? 'text-gray-300' : 'text-gray-600'
        }`}
      >
        <span className="font-semibold text-green-600">{leftValue}</span>
        <span
          className={`font-medium ${isDarkMode ? 'text-gray-400' : 'text-gray-500'}`}
        >
          {label}
        </span>
        <span className="font-semibold text-blue-600">{rightValue}</span>
      </div>
      <div
        className={`relative flex h-2.5 w-full overflow-hidden rounded-full backdrop-blur-sm ${
          isDarkMode ? 'bg-gray-700/50' : 'bg-gray-100/50'
        }`}
      >
        <div
          className="rounded-full transition-all duration-1000 ease-out"
          style={{
            width: `${leftPercent}%`,
            background: `linear-gradient(90deg, ${leftColor}, ${leftColor}dd)`,
          }}
        />
        <div
          className="rounded-full transition-all duration-1000 ease-out"
          style={{
            width: `${rightPercent}%`,
            background: `linear-gradient(90deg, ${rightColor}dd, ${rightColor})`,
          }}
        />
        <div className="absolute inset-0 rounded-full bg-gradient-to-t from-white/10 to-white/30"></div>
      </div>
    </div>
  );
};

export default function LiveMatchPanel({
  userId: propUserId,
  homeTeam,
  awayTeam,
  homeScore,
  awayScore,
  stats,
  matchTime,
}: LiveMatchPanelProps) {
  // All state variables
  const [isLoading, setIsLoading] = useState(false);
  const [isMounted, setIsMounted] = useState(false);
  const [liveMatchData, setLiveMatchData] = useState<LiveMatch | null>(null);
  const [hasLiveMatch, setHasLiveMatch] = useState(false);
  const [error, setError] = useState<string>('');
  const [realtimeStatistics, setRealtimeStatistics] =
    useState<MatchStatistics | null>(null);
  const [signalRConnected, setSignalRConnected] = useState(false);
  const [lastUpdateTime, setLastUpdateTime] = useState<Date | null>(null);
  const [updateCounter, setUpdateCounter] = useState(0);
  const [viewportKey, setViewportKey] = useState(0);

  // Enhanced features state
  const [autoIncrementingTime, setAutoIncrementingTime] = useState<string>('');
  const [baseMatchTime, setBaseMatchTime] = useState<number>(0);
  const [isHoverRefreshing, setIsHoverRefreshing] = useState(false);
  const [pendingStatisticsUpdate, setPendingStatisticsUpdate] =
    useState<MatchStatistics | null>(null);

  const router = useRouter();
  const { isDarkMode } = useSettings();

  // Get user ID
  const getUserId = (): string | null => {
    if (propUserId) return propUserId;
    const currentUser = authService.getCurrentUser();
    if (currentUser) {
      return currentUser.claimNameId || currentUser.sub;
    }
    return null;
  };

  const userId = getUserId();

  // Hover refresh function
  const handleHoverRefresh = async () => {
    if (!userId || isHoverRefreshing) return;

    try {
      setIsHoverRefreshing(true);
      const response = await matchService.getLiveMatchForUser(userId);

      if (response.succeeded && response.hasLiveMatch && response.liveMatch) {
        setLiveMatchData(response.liveMatch);
        setHasLiveMatch(true);
        liveMatchStorageService.setLiveMatchData({
          matchId: response.liveMatch.id,
          homeTeam: response.liveMatch.homeTeam.name,
          awayTeam: response.liveMatch.awayTeam.name,
          homeScore: response.liveMatch.homeTeamScore,
          awayScore: response.liveMatch.awayTeamScore,
          status: response.liveMatch.matchStatus,
        });
      }
    } catch (err) {
      console.error('Error during hover refresh:', err);
    } finally {
      setIsHoverRefreshing(false);
    }
  };

  // Format match time from UTC date
  const formatMatchTime = (utcDate: string): string => {
    const now = new Date();
    const matchDate = new Date(utcDate);
    const diffMs = now.getTime() - matchDate.getTime();
    const diffMinutes = Math.floor(diffMs / (1000 * 60));

    if (diffMinutes < 0) return 'Not Started';
    else if (diffMinutes <= 90) return `${diffMinutes}'`;
    else return 'Full Time';
  };

  // Get current match time with auto-increment
  const getCurrentMatchTime = (): string => {
    const isCurrentlyLive =
      realtimeStatistics?.matchInfo?.isLive ||
      realtimeStatistics?.matchInfo?.status === 'Live' ||
      liveMatchData?.isLive ||
      liveMatchData?.matchStatus === 'Live';

    if (autoIncrementingTime && realtimeStatistics && isCurrentlyLive) {
      return autoIncrementingTime;
    }

    if (realtimeStatistics) {
      if (realtimeStatistics.timeStamp) {
        return realtimeStatistics.timeStamp;
      }
      if (typeof realtimeStatistics.matchInfo.currentMinute === 'number') {
        return `${realtimeStatistics.matchInfo.currentMinute}'`;
      }
    }

    if (liveMatchData?.scheduledDateTimeUtc) {
      return formatMatchTime(liveMatchData.scheduledDateTimeUtc);
    }

    return '00:00';
  };

  // Get time elapsed since last update
  const getRealTimeTimestamp = (): string => {
    if (realtimeStatistics && lastUpdateTime) {
      const now = new Date();
      const diffMs = now.getTime() - lastUpdateTime.getTime();
      const diffSeconds = Math.floor(diffMs / 1000);
      const diffMinutes = Math.floor(diffSeconds / 60);
      const diffHours = Math.floor(diffMinutes / 60);

      if (diffSeconds < 5) return 'now';
      else if (diffSeconds < 10) return 'just now';
      else if (diffSeconds < 60) return `${diffSeconds}s ago`;
      else if (diffMinutes < 60) return `${diffMinutes}m ago`;
      else if (diffHours < 24) return `${diffHours}h ago`;
      else return 'offline';
    }
    return '';
  };

  // Get current statistics
  const getCurrentStatistics = () => {
    if (realtimeStatistics) {
      return {
        homeTeamScore: realtimeStatistics.homeTeam.score,
        awayTeamScore: realtimeStatistics.awayTeam.score,
        homeTeamPossession: realtimeStatistics.homeTeam.possession,
        awayTeamPossession: realtimeStatistics.awayTeam.possession,
        homeTeamShots: realtimeStatistics.homeTeam.shots,
        awayTeamShots: realtimeStatistics.awayTeam.shots,
        homeTeamShotsOnTarget: realtimeStatistics.homeTeam.shotsOnTarget,
        awayTeamShotsOnTarget: realtimeStatistics.awayTeam.shotsOnTarget,
        homeTeamCorners: realtimeStatistics.homeTeam.corners,
        awayTeamCorners: realtimeStatistics.awayTeam.corners,
        homeTeamFouls: realtimeStatistics.homeTeam.fouls,
        awayTeamFouls: realtimeStatistics.awayTeam.fouls,
        homeTeamYellowCards: realtimeStatistics.homeTeam.yellowCards,
        awayTeamYellowCards: realtimeStatistics.awayTeam.yellowCards,
        homeTeamRedCards: realtimeStatistics.homeTeam.redCards,
        awayTeamRedCards: realtimeStatistics.awayTeam.redCards,
        matchStatus: realtimeStatistics.matchInfo.status,
        isLive: realtimeStatistics.matchInfo.isLive,
      };
    } else if (liveMatchData) {
      return {
        homeTeamScore: liveMatchData.homeTeamScore,
        awayTeamScore: liveMatchData.awayTeamScore,
        homeTeamPossession: liveMatchData.homeTeamPossession,
        awayTeamPossession: liveMatchData.awayTeamPossession,
        homeTeamShots: liveMatchData.homeTeamShots,
        awayTeamShots: liveMatchData.awayTeamShots,
        homeTeamShotsOnTarget: liveMatchData.homeTeamShotsOnTarget,
        awayTeamShotsOnTarget: liveMatchData.awayTeamShotsOnTarget,
        homeTeamCorners: liveMatchData.homeTeamCorners,
        awayTeamCorners: liveMatchData.awayTeamCorners,
        homeTeamFouls: liveMatchData.homeTeamFouls,
        awayTeamFouls: liveMatchData.awayTeamFouls,
        homeTeamYellowCards: liveMatchData.homeTeamYellowCards,
        awayTeamYellowCards: liveMatchData.awayTeamYellowCards,
        homeTeamRedCards: liveMatchData.homeTeamRedCards,
        awayTeamRedCards: liveMatchData.awayTeamRedCards,
        matchStatus: liveMatchData.matchStatus,
        isLive: liveMatchData.isLive,
      };
    }
    return null;
  };

  // Handle navigation
  const handleMatchClick = () => {
    if (hasLiveMatch && liveMatchData?.id) {
      router.push(`/matchdetails?matchId=${liveMatchData.id}`);
    }
  };

  const handleSimulationClick = () => {
    const simulationUrl = liveMatchStorageService.getSimulationViewUrl();
    if (simulationUrl) {
      router.push(simulationUrl);
    } else if (hasLiveMatch && liveMatchData?.id) {
      router.push(`/matchdetails?matchId=${liveMatchData.id}`);
    }
  };
  // Main initialization effect
  useEffect(() => {
    setIsMounted(true);
    const { statistics, updateTime } =
      liveMatchStorageService.getRealtimeStatistics();
    if (statistics && updateTime) {
      setRealtimeStatistics(statistics);
      setLastUpdateTime(updateTime);

      // Set base time from stored statistics
      let storedTimeSeconds = 0;
      if (statistics.timeStamp && statistics.timeStamp.includes(':')) {
        const [minutes, seconds] = statistics.timeStamp.split(':').map(Number);
        storedTimeSeconds = minutes * 60 + (seconds || 0);
      } else if (typeof statistics.matchInfo.currentMinute === 'number') {
        storedTimeSeconds = statistics.matchInfo.currentMinute * 60;
      }
      setBaseMatchTime(storedTimeSeconds);
      console.log(
        `📱 Loaded stored statistics: Match time ${statistics.timeStamp || statistics.matchInfo.currentMinute} = ${storedTimeSeconds} seconds`
      );
    }

    const handleResize = () => setViewportKey((prev) => prev + 1);
    window.addEventListener('resize', handleResize);
    return () => window.removeEventListener('resize', handleResize);
  }, []);

  // SignalR connection effect
  useEffect(() => {
    let isSubscribed = true;

    const initSignalR = async () => {
      try {
        if (!authService.isAuthenticated()) return;
        const connected = await signalRService.ensurePageConnection();
        if (isSubscribed) setSignalRConnected(connected);
      } catch (error) {
        console.error('Failed to connect SignalR:', error);
        if (isSubscribed) setSignalRConnected(false);
      }
    };
    const handleVisibilityChange = () => {
      if (!document.hidden && signalRConnected && liveMatchData?.id) {
        signalRService.joinMatchStatistics(liveMatchData.id);
      }
      if (!document.hidden && liveMatchData?.id) {
        const { statistics, updateTime } =
          liveMatchStorageService.getRealtimeStatistics();
        if (
          statistics &&
          updateTime &&
          liveMatchStorageService.isStoredStatisticsValid(liveMatchData.id)
        ) {
          if (isSubscribed) {
            setRealtimeStatistics(statistics);
            setLastUpdateTime(updateTime);

            // Set base time from restored statistics
            let restoredTimeSeconds = 0;
            if (statistics.timeStamp && statistics.timeStamp.includes(':')) {
              const [minutes, seconds] = statistics.timeStamp
                .split(':')
                .map(Number);
              restoredTimeSeconds = minutes * 60 + (seconds || 0);
            } else if (typeof statistics.matchInfo.currentMinute === 'number') {
              restoredTimeSeconds = statistics.matchInfo.currentMinute * 60;
            }
            setBaseMatchTime(restoredTimeSeconds);
            console.log(
              `🔄 Restored statistics on tab focus: Match time ${statistics.timeStamp || statistics.matchInfo.currentMinute} = ${restoredTimeSeconds} seconds`
            );
          }
        }
      }
    };

    initSignalR();
    document.addEventListener('visibilitychange', handleVisibilityChange);

    return () => {
      isSubscribed = false;
      document.removeEventListener('visibilitychange', handleVisibilityChange);
      if (signalRConnected && liveMatchData?.id) {
        signalRService.leaveMatchStatistics(liveMatchData.id);
      }
    };
  }, [signalRConnected, liveMatchData?.id]);

  // SignalR statistics listener effect
  useEffect(() => {
    if (!signalRConnected || !liveMatchData?.id) return;

    signalRService.joinMatchStatistics(liveMatchData.id); // INSTANT STATISTICS UPDATE LISTENER
    signalRService.onMatchStatisticsUpdate(
      (method: string, matchId: number, statistics: MatchStatistics) => {
        if (matchId === liveMatchData.id) {
          // INSTANT UPDATE - Set pending for immediate visual feedback
          setPendingStatisticsUpdate(statistics);

          const now = new Date();

          // Process match time for auto-increment - START FROM LATEST TIMESTAMP
          let currentTimeSeconds = 0;
          let lasttimeevent = 0;

          // Priority 1: Use timeStamp if available (most accurate)
          if (statistics.timeStamp && statistics.timeStamp.includes(':')) {
            const [minutes, seconds] = statistics.timeStamp
              .split(':')
              .map(Number);
            currentTimeSeconds = minutes * 60 + (seconds || 0);
            console.log(
              `📊 Statistics update: Match time ${statistics.timeStamp} = ${currentTimeSeconds} seconds`
            );
          }
          // Priority 2: Use currentMinute if available
          else if (typeof statistics.matchInfo.currentMinute === 'number') {
            currentTimeSeconds = statistics.matchInfo.currentMinute * 60;
            lasttimeevent = statistics.matchInfo.lastEventTime;
            console.log(
              `📊 Statistics update: Match minute ${statistics.matchInfo.currentMinute} = ${currentTimeSeconds} seconds`
            );
          }

          // Update base time to start from the latest received timestamp
          setBaseMatchTime(lasttimeevent);
          setRealtimeStatistics(statistics);
          setLastUpdateTime(now);

          // Store data
          liveMatchStorageService.setRealtimeStatistics(statistics, now);
          liveMatchStorageService.setLiveMatchData({
            matchId: matchId,
            simulationId: undefined,
            homeTeam: statistics.homeTeam.name,
            awayTeam: statistics.awayTeam.name,
            homeScore: statistics.homeTeam.score,
            awayScore: statistics.awayTeam.score,
            status: statistics.matchInfo.status,
          });

          // Clear pending update after brief display
          setTimeout(() => setPendingStatisticsUpdate(null), 100);
        }
      }
    );

    return () => {
      signalRService.leaveMatchStatistics(liveMatchData.id);
    };
  }, [signalRConnected, liveMatchData?.id]);
  // Auto-incrementing timestamp effect
  useEffect(() => {
    let intervalId: NodeJS.Timeout;

    const isCurrentlyLive =
      realtimeStatistics?.matchInfo?.isLive ||
      realtimeStatistics?.matchInfo?.status === 'Live' ||
      liveMatchData?.isLive ||
      liveMatchData?.matchStatus === 'Live';

    if (realtimeStatistics && lastUpdateTime && isCurrentlyLive) {
      // Initialize the auto-incrementing time with the exact timestamp from statistics
      let initialTimeSeconds = baseMatchTime;

      // If we have a timestamp, use it as the starting point
      if (
        realtimeStatistics.timeStamp &&
        realtimeStatistics.timeStamp.includes(':')
      ) {
        const [minutes, seconds] = realtimeStatistics.timeStamp
          .split(':')
          .map(Number);
        initialTimeSeconds = minutes * 60 + (seconds || 0);
      } else if (
        typeof realtimeStatistics.matchInfo.currentMinute === 'number'
      ) {
        initialTimeSeconds = realtimeStatistics.matchInfo.currentMinute * 60;
      }

      intervalId = setInterval(() => {
        const now = new Date();
        const secondsSinceUpdate = Math.floor(
          (now.getTime() - lastUpdateTime.getTime()) / 1000
        );
        const currentTimeSeconds = initialTimeSeconds + secondsSinceUpdate;

        const minutes = Math.floor(currentTimeSeconds / 60);
        const seconds = currentTimeSeconds % 60;
        const timeString = `${minutes}:${seconds.toString().padStart(2, '0')}`;

        setAutoIncrementingTime(timeString);
      }, 1000);

      // Set initial auto-incrementing time immediately
      const initialMinutes = Math.floor(initialTimeSeconds / 60);
      const initialSecondsRemainder = initialTimeSeconds % 60;
      const initialTimeString = `${initialMinutes}:${initialSecondsRemainder.toString().padStart(2, '0')}`;
      setAutoIncrementingTime(initialTimeString);
    } else {
      // Clear auto-incrementing time when not live
      setAutoIncrementingTime('');
    }

    return () => {
      if (intervalId) clearInterval(intervalId);
    };
  }, [realtimeStatistics, lastUpdateTime, baseMatchTime, liveMatchData]);

  // Fetch live match data effect
  useEffect(() => {
    const fetchLiveMatch = async () => {
      if (!userId) return;

      try {
        setIsLoading(true);
        setError('');
        const response = await matchService.getLiveMatchForUser(userId);

        if (response.succeeded && response.hasLiveMatch && response.liveMatch) {
          setLiveMatchData(response.liveMatch);
          setHasLiveMatch(true);
          if (
            liveMatchStorageService.isStoredStatisticsValid(
              response.liveMatch.id
            )
          ) {
            const { statistics, updateTime } =
              liveMatchStorageService.getRealtimeStatistics();
            if (statistics && updateTime) {
              setRealtimeStatistics(statistics);
              setLastUpdateTime(updateTime);

              // Set base time from loaded statistics
              let loadedTimeSeconds = 0;
              if (statistics.timeStamp && statistics.timeStamp.includes(':')) {
                const [minutes, seconds] = statistics.timeStamp
                  .split(':')
                  .map(Number);
                loadedTimeSeconds = minutes * 60 + (seconds || 0);
              } else if (
                typeof statistics.matchInfo.currentMinute === 'number'
              ) {
                loadedTimeSeconds = statistics.matchInfo.currentMinute * 60;
              }
              setBaseMatchTime(loadedTimeSeconds);
              console.log(
                `🔃 Loaded stored statistics for match ${response.liveMatch.id}: Match time ${statistics.timeStamp || statistics.matchInfo.currentMinute} = ${loadedTimeSeconds} seconds`
              );
            }
          } else {
            liveMatchStorageService.clearRealtimeStatistics();
            setRealtimeStatistics(null);
            setLastUpdateTime(null);
            setBaseMatchTime(0); // Reset base time if no valid stored statistics
          }

          liveMatchStorageService.setLiveMatchData({
            matchId: response.liveMatch.id,
            homeTeam: response.liveMatch.homeTeam.name,
            awayTeam: response.liveMatch.awayTeam.name,
            homeScore: response.liveMatch.homeTeamScore,
            awayScore: response.liveMatch.awayTeamScore,
            status: response.liveMatch.matchStatus,
          });
        } else {
          setHasLiveMatch(false);
          setLiveMatchData(null);
          setRealtimeStatistics(null);
          liveMatchStorageService.clearLiveMatchData();
          if (!response.succeeded && response.error) {
            setError(response.error);
          } else {
            setError('');
          }
        }
      } catch (err) {
        console.error('Error fetching live match:', err);
        setError('Failed to load live match');
        setHasLiveMatch(false);
        setLiveMatchData(null);
        setRealtimeStatistics(null);
      } finally {
        setIsLoading(false);
      }
    };

    fetchLiveMatch();
    const pollInterval = setInterval(fetchLiveMatch, 120000);
    return () => clearInterval(pollInterval);
  }, [userId]);

  // Update counter effect
  useEffect(() => {
    let timeoutId: NodeJS.Timeout;

    if (realtimeStatistics) setUpdateCounter((prev) => prev + 1);

    if (realtimeStatistics && lastUpdateTime) {
      const scheduleNextUpdate = () => {
        timeoutId = setTimeout(() => {
          setUpdateCounter((prev) => prev + 1);
          scheduleNextUpdate();
        }, 1000); // Update every second
      };
      scheduleNextUpdate();
    }

    return () => {
      if (timeoutId) clearTimeout(timeoutId);
    };
  }, [realtimeStatistics, lastUpdateTime]);

  // Memoized values
  const currentStats = useMemo(
    () => getCurrentStatistics(),
    [realtimeStatistics, liveMatchData]
  );
  const currentMatchTime = useMemo(
    () => getCurrentMatchTime(),
    [realtimeStatistics, liveMatchData, autoIncrementingTime]
  );
  const realTimeTimestamp = useMemo(
    () => getRealTimeTimestamp(),
    [realtimeStatistics, lastUpdateTime, updateCounter]
  );

  // Display data
  const displayData = useMemo(() => {
    if (hasLiveMatch && liveMatchData && currentStats) {
      return {
        homeTeam: {
          name: liveMatchData.homeTeam.shortName || liveMatchData.homeTeam.name,
          logo: liveMatchData.homeTeam.logo || '/logos/PixelPitch.png',
        },
        awayTeam: {
          name: liveMatchData.awayTeam.shortName || liveMatchData.awayTeam.name,
          logo: liveMatchData.awayTeam.logo || '/logos/PixelPitch.png',
        },
        homeScore: currentStats.homeTeamScore,
        awayScore: currentStats.awayTeamScore,
        matchTime: currentMatchTime,
        realTimeTimestamp: realTimeTimestamp,
        isLive: currentStats.isLive || currentStats.matchStatus === 'Live',
        status: currentStats.matchStatus,
        hasRealTimeData: !!realtimeStatistics,
        stats: [
          {
            label: 'Possession',
            homeValue: currentStats.homeTeamPossession,
            awayValue: currentStats.awayTeamPossession,
          },
          {
            label: 'Shots on Target',
            homeValue: currentStats.homeTeamShotsOnTarget,
            awayValue: currentStats.awayTeamShotsOnTarget,
          },
          {
            label: 'Shots',
            homeValue: currentStats.homeTeamShots,
            awayValue: currentStats.awayTeamShots,
          },
          {
            label: 'Corners',
            homeValue: currentStats.homeTeamCorners,
            awayValue: currentStats.awayTeamCorners,
          },
          {
            label: 'Fouls',
            homeValue: currentStats.homeTeamFouls,
            awayValue: currentStats.awayTeamFouls,
          },
          {
            label: 'Yellow Cards',
            homeValue: currentStats.homeTeamYellowCards,
            awayValue: currentStats.awayTeamYellowCards,
          },
        ],
      };
    } else {
      return {
        homeTeam: homeTeam || { name: 'Team A', logo: '/logos/barcelona.png' },
        awayTeam: awayTeam || {
          name: 'Team B',
          logo: '/logos/real madrid.png',
        },
        homeScore: homeScore || 0,
        awayScore: awayScore || 0,
        matchTime: matchTime || '00:00',
        isLive: false,
        status: 'No Live Match' as const,
        hasRealTimeData: false,
        stats: stats || [
          { label: 'Possession', homeValue: 50, awayValue: 50 },
          { label: 'Shots on Target', homeValue: 5, awayValue: 3 },
          { label: 'Shots', homeValue: 10, awayValue: 7 },
        ],
      };
    }
  }, [
    hasLiveMatch,
    liveMatchData,
    currentStats,
    currentMatchTime,
    realTimeTimestamp,
    realtimeStatistics,
    homeTeam,
    awayTeam,
    homeScore,
    awayScore,
    stats,
    matchTime,
    updateCounter,
    viewportKey,
  ]);

  // Loading state
  if (isLoading) {
    return (
      <div className="mx-auto w-full max-w-md p-6">
        <div
          className={`relative overflow-hidden rounded-2xl border shadow-2xl backdrop-blur-xl ${
            isMounted && isDarkMode
              ? 'border-gray-700/20 bg-gradient-to-br from-gray-800/20 to-gray-900/5'
              : 'border-white/20 bg-gradient-to-br from-white/20 to-white/5'
          }`}
          suppressHydrationWarning
        >
          <div className="absolute inset-0 animate-pulse bg-gradient-to-br from-green-400/10 via-blue-400/10 to-purple-400/10"></div>
          <div className="relative z-10 flex flex-col items-center justify-center px-6 py-12">
            <div className="relative mb-6">
              <div className="absolute inset-0 animate-ping rounded-full bg-green-400/30"></div>
              <div className="relative rounded-full border border-green-400/30 bg-gradient-to-br from-green-400/20 to-emerald-400/20 p-4 backdrop-blur-sm">
                <div className="animate-spin">
                  <div className="h-8 w-8 rounded-full border-2 border-green-400 border-t-transparent"></div>
                </div>
              </div>
            </div>
            <h3
              className={`mb-2 text-lg font-semibold ${isMounted && isDarkMode ? 'text-gray-300' : 'text-gray-700'}`}
            >
              {isHoverRefreshing ? 'Refreshing Match...' : 'Loading Match...'}
            </h3>
            <div className="flex items-center space-x-1">
              <div className="h-2 w-2 animate-bounce rounded-full bg-green-400 delay-0"></div>
              <div className="h-2 w-2 animate-bounce rounded-full bg-green-400 delay-100"></div>
              <div className="h-2 w-2 animate-bounce rounded-full bg-green-400 delay-200"></div>
            </div>
          </div>
        </div>
      </div>
    );
  }

  // Error state
  if (error) {
    return (
      <div className="mx-auto w-full max-w-md p-6">
        <div
          className={`relative overflow-hidden rounded-2xl border shadow-2xl backdrop-blur-xl ${
            isMounted && isDarkMode
              ? 'border-red-800/50 bg-gradient-to-br from-red-900/80 to-red-800/60'
              : 'border-red-200/50 bg-gradient-to-br from-red-50/80 to-red-100/60'
          }`}
        >
          <div className="absolute inset-0 animate-pulse bg-gradient-to-br from-red-400/5 to-orange-400/5"></div>
          <div className="relative z-10 px-6 py-8 text-center">
            <div className="mb-4 animate-bounce text-4xl">⚠️</div>
            <h3
              className={`mb-2 text-lg font-semibold ${isMounted && isDarkMode ? 'text-red-400' : 'text-red-600'}`}
            >
              Connection Error
            </h3>
            <p
              className={`mb-4 text-sm ${isMounted && isDarkMode ? 'text-red-300' : 'text-red-500'}`}
            >
              {error}
            </p>
            <button
              onClick={() => window.location.reload()}
              className="rounded-lg bg-gradient-to-r from-red-500 to-red-600 px-4 py-2 text-sm font-medium text-white shadow-lg transition-all duration-300 hover:scale-105"
            >
              Retry Connection
            </button>
          </div>
        </div>
      </div>
    );
  }

  // No live match state
  if (!hasLiveMatch) {
    return (
      <div className="mx-auto w-full max-w-md p-6">
        <div
          className={`relative overflow-hidden rounded-2xl border shadow-2xl backdrop-blur-xl ${
            isMounted && isDarkMode
              ? 'border-gray-700/50 bg-gradient-to-br from-gray-800/80 to-gray-900/60'
              : 'border-gray-200/50 bg-gradient-to-br from-gray-50/80 to-gray-100/60'
          }`}
        >
          <div className="absolute inset-0 opacity-30">
            <div className="absolute top-4 left-4 h-2 w-2 animate-ping rounded-full bg-gray-300"></div>
            <div className="absolute right-6 bottom-6 h-1 w-1 animate-pulse rounded-full bg-gray-400 delay-300"></div>
            <div className="absolute top-1/2 right-4 h-1.5 w-1.5 animate-bounce rounded-full bg-gray-300 delay-500"></div>
          </div>
          <div className="relative z-10 px-6 py-10 text-center">
            <div className="mb-4 animate-bounce text-5xl">⚽</div>
            <h3 className="mb-3 text-xl font-bold text-gray-700">
              No Live Match
            </h3>
            <p className="mb-2 text-sm text-gray-500">
              You don't have any live matches at the moment.
            </p>
            <p className="mb-4 text-xs text-gray-400">
              Check back later for upcoming matches.
            </p>

            <button
              onClick={handleHoverRefresh}
              disabled={isHoverRefreshing}
              className={`inline-flex items-center gap-2 rounded-full border px-4 py-2 text-sm font-medium backdrop-blur-sm transition-all duration-300 ${
                isHoverRefreshing
                  ? 'cursor-not-allowed border-gray-400/50 bg-gray-500/20 text-gray-400'
                  : 'border-gray-300/50 bg-gray-100/20 text-gray-600 hover:scale-105 hover:border-gray-400/70 hover:bg-gray-200/30'
              }`}
            >
              <div
                className={`h-4 w-4 transition-transform duration-500 ${isHoverRefreshing ? 'animate-spin' : ''}`}
              >
                <svg
                  fill="none"
                  stroke="currentColor"
                  viewBox="0 0 24 24"
                  className="h-full w-full"
                >
                  <path
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    strokeWidth={2}
                    d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15"
                  />
                </svg>
              </div>
              <span>
                {isHoverRefreshing ? 'Checking...' : 'Check for Matches'}
              </span>
            </button>

            <div className="mt-6 flex justify-center space-x-2">
              <div className="h-2 w-2 animate-pulse rounded-full bg-gray-300"></div>
              <div className="h-2 w-2 animate-pulse rounded-full bg-gray-300 delay-100"></div>
              <div className="h-2 w-2 animate-pulse rounded-full bg-gray-300 delay-200"></div>
            </div>
          </div>
        </div>
      </div>
    );
  }

  // Main component render
  return (
    <div className="mx-auto w-full max-w-md p-6">
      <style jsx>{`
        @keyframes fade-in {
          from {
            opacity: 0;
            transform: scale(0.8);
          }
          to {
            opacity: 1;
            transform: scale(1);
          }
        }
        .animate-fade-in {
          animation: fade-in 0.3s ease-out;
        }
      `}</style>

      <div
        className={`relative overflow-hidden rounded-2xl border shadow-2xl backdrop-blur-xl transition-all duration-300 ${
          hasLiveMatch
            ? 'hover:shadow-3xl cursor-pointer border-white/30 bg-gradient-to-br from-white/25 to-white/10 hover:scale-[1.02] hover:border-white/50'
            : 'border-white/20 bg-gradient-to-br from-white/20 to-white/5'
        }`}
        onClick={hasLiveMatch ? handleMatchClick : undefined}
      >
        <div className="absolute inset-0 animate-pulse bg-gradient-to-br from-green-400/10 via-blue-400/5 to-purple-400/10"></div>

        {displayData.isLive && (
          <div className="absolute inset-0 animate-pulse rounded-2xl border-2 border-red-400/50"></div>
        )}

        <div className="relative z-10 p-6">
          {/* Hover Refresh Button */}
          {hasLiveMatch && (
            <div
              className="absolute top-2 right-2 z-20"
              onMouseEnter={handleHoverRefresh}
            >
              <div
                className={`group relative rounded-full border p-1.5 backdrop-blur-sm transition-all duration-300 ${
                  isHoverRefreshing
                    ? 'scale-110 border-blue-400/50 bg-blue-500/20'
                    : 'border-white/20 bg-white/10 hover:scale-110 hover:border-white/40 hover:bg-white/20'
                }`}
              >
                <div
                  className={`h-3 w-3 transition-transform duration-500 ${
                    isHoverRefreshing
                      ? 'animate-spin'
                      : 'group-hover:rotate-180'
                  }`}
                >
                  <svg
                    fill="none"
                    stroke="currentColor"
                    viewBox="0 0 24 24"
                    className={`h-full w-full ${isHoverRefreshing ? 'text-blue-400' : 'text-white/60 group-hover:text-white/80'}`}
                  >
                    <path
                      strokeLinecap="round"
                      strokeLinejoin="round"
                      strokeWidth={2}
                      d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15"
                    />
                  </svg>
                </div>

                {pendingStatisticsUpdate && (
                  <div className="absolute -top-1 -right-1 h-2 w-2 animate-ping rounded-full bg-green-400"></div>
                )}
              </div>
            </div>
          )}

          {/* Match Status and Time */}
          <div className="mb-4 flex items-center justify-center gap-3">
            {displayData.isLive && (
              <div className="flex items-center gap-2">
                <div className="h-3 w-3 animate-pulse rounded-full bg-red-500 shadow-lg shadow-red-500/50"></div>
                <span className="text-sm font-bold tracking-wide text-red-500">
                  LIVE
                </span>

                {displayData.hasRealTimeData && (
                  <div
                    className={`flex items-center gap-1 rounded-full px-2 py-1 text-xs transition-all duration-300 ${
                      pendingStatisticsUpdate
                        ? 'animate-pulse bg-green-500/40'
                        : 'bg-green-500/20'
                    }`}
                  >
                    <div
                      className={`h-1.5 w-1.5 rounded-full bg-green-400 ${
                        pendingStatisticsUpdate
                          ? 'animate-ping'
                          : 'animate-pulse'
                      }`}
                    ></div>
                    <span className="font-medium text-green-400">
                      {pendingStatisticsUpdate ? 'Updating...' : 'Real-time'}
                    </span>
                  </div>
                )}
              </div>
            )}

            <div className="rounded-full border border-white/30 bg-white/20 px-3 py-1 backdrop-blur-sm">
              <p
                className={`text-sm font-semibold ${displayData.isLive ? 'text-red-500' : 'text-green-600'}`}
              >
                {displayData.matchTime}
              </p>
            </div>
          </div>

          {/* Enhanced Real-time Update Timestamp */}
          {displayData.isLive && displayData.realTimeTimestamp && (
            <div className="mb-4 flex items-center justify-center">
              <div className="flex items-center gap-2 rounded-full border border-blue-300/30 bg-blue-100/20 px-3 py-1 backdrop-blur-sm">
                <div className="h-2 w-2 animate-pulse rounded-full bg-blue-400"></div>
                <span className="text-xs font-medium text-blue-600">
                  Updated: {displayData.realTimeTimestamp}
                </span>
              </div>
            </div>
          )}

          {/* Teams and Score */}
          <div className="mb-6 flex items-center justify-between">
            {/* Home Team */}
            <div className="group flex flex-col items-center">
              <div className="relative mb-3">
                <div className="absolute inset-0 animate-pulse rounded-full bg-gradient-to-br from-yellow-400/20 to-orange-400/20 blur"></div>
                <Image
                  src={displayData.homeTeam.logo}
                  alt={displayData.homeTeam.name}
                  width={52}
                  height={52}
                  className="relative rounded-full border-2 border-white/30 shadow-xl transition-transform duration-300 group-hover:scale-110"
                  onError={(e) => {
                    e.currentTarget.src = '/logos/Footex.png';
                  }}
                />
              </div>
              <span className="max-w-[70px] text-center text-xs leading-tight font-medium text-gray-700">
                {displayData.homeTeam.name}
              </span>
            </div>

            {/* Score */}
            <div className="relative flex-shrink-0">
              <div className="absolute inset-0 rounded-2xl bg-gradient-to-br from-white/30 to-white/10 blur"></div>
              <div className="relative min-w-[120px] rounded-2xl border border-white/30 bg-gradient-to-br from-white/40 to-white/20 px-8 py-5 shadow-xl backdrop-blur-sm">
                <div className="text-center text-3xl font-bold tracking-wide whitespace-nowrap text-gray-800">
                  {displayData.homeScore} - {displayData.awayScore}
                </div>
              </div>
            </div>

            {/* Away Team */}
            <div className="group flex flex-col items-center">
              <div className="relative mb-3">
                <div className="absolute inset-0 animate-pulse rounded-full bg-gradient-to-br from-blue-400/20 to-cyan-400/20 blur"></div>
                <Image
                  src={displayData.awayTeam.logo}
                  alt={displayData.awayTeam.name}
                  width={52}
                  height={52}
                  className="relative rounded-full border-2 border-white/30 shadow-xl transition-transform duration-300 group-hover:scale-110"
                  onError={(e) => {
                    e.currentTarget.src = '/logos/Footex.png';
                  }}
                />
              </div>
              <span className="max-w-[70px] text-center text-xs leading-tight font-medium text-gray-700">
                {displayData.awayTeam.name}
              </span>
            </div>
          </div>

          {/* Statistics with Instant Update Effects */}
          <div
            className={`space-y-2 transition-all duration-300 ${
              pendingStatisticsUpdate
                ? 'animate-pulse rounded-lg bg-gradient-to-r from-green-400/10 via-transparent to-green-400/10 p-2'
                : ''
            }`}
          >
            {/* Instant Update Indicator */}
            {pendingStatisticsUpdate && (
              <div className="mb-2 flex items-center justify-center">
                <div className="animate-fade-in flex items-center gap-2 rounded-full border border-green-400/50 bg-green-500/20 px-3 py-1 text-xs">
                  <div className="h-1.5 w-1.5 animate-ping rounded-full bg-green-400"></div>
                  <span className="font-medium text-green-400">
                    ⚡ Statistics Updated!
                  </span>
                </div>
              </div>
            )}

            {displayData.stats.map((stat, index) => (
              <div
                key={index}
                className={`transition-all duration-500 ${
                  pendingStatisticsUpdate
                    ? 'scale-[1.02] transform shadow-lg'
                    : ''
                }`}
              >
                <StatBar
                  label={stat.label}
                  leftValue={stat.homeValue}
                  rightValue={stat.awayValue}
                  leftColor="#f59e0b"
                  rightColor="#3b82f6"
                />
              </div>
            ))}

            {/* Auto-incrementing time display */}
            {autoIncrementingTime && displayData.isLive && (
              <div className="mt-3 flex items-center justify-center">
                <div className="flex items-center gap-2 rounded-full border border-blue-300/30 bg-blue-100/20 px-3 py-1 text-xs backdrop-blur-sm">
                  <div className="h-1.5 w-1.5 animate-pulse rounded-full bg-blue-400"></div>
                  <span className="font-medium text-blue-600">
                    Live: {autoIncrementingTime}
                  </span>
                </div>
              </div>
            )}
          </div>

          {/* Click indicator for live matches */}
          {hasLiveMatch && (
            <div className="mt-4 space-y-2">
              <div className="text-center">
                <div className="inline-flex items-center gap-2 rounded-full border border-white/30 bg-white/20 px-3 py-1 text-xs text-gray-600 backdrop-blur-sm transition-all duration-300 hover:bg-white/30">
                  <span>🔍</span>
                  <span>Click for details</span>
                </div>
              </div>

              {displayData.isLive && (
                <div className="text-center">
                  <button
                    onClick={handleSimulationClick}
                    className="inline-flex items-center gap-2 rounded-full border border-blue-400/50 bg-blue-500/20 px-4 py-2 text-xs font-medium text-blue-400 backdrop-blur-sm transition-all duration-300 hover:scale-105 hover:border-blue-400/70 hover:bg-blue-500/30"
                  >
                    <span>⚡</span>
                    <span>Live Simulation</span>
                  </button>
                </div>
              )}
            </div>
          )}
        </div>

        {/* Decorative corner elements */}
        <div className="absolute top-2 right-2 h-2 w-2 rounded-full bg-gradient-to-br from-white/40 to-transparent"></div>
        <div className="absolute bottom-2 left-2 h-1 w-1 rounded-full bg-gradient-to-br from-white/30 to-transparent"></div>
      </div>
    </div>
  );
}
