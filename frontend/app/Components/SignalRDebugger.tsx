// Enhanced SignalR Debug Component with comprehensive control and monitoring
'use client';

import { useState, useEffect } from 'react';
import signalRService, {
  MatchStatistics,
  NotificationData,
  MatchEventData,
  SimulationProgressData,
} from '@/Services/SignalRService';
import authService from '@/Services/AuthenticationService';

interface ConnectionStats {
  matchSimulation: {
    isConnected: boolean;
    connectionState: string;
    hasConnection: boolean;
  };
  notification: {
    isConnected: boolean;
    connectionState: string;
    hasConnection: boolean;
  };
  reconnectAttempts: number;
  maxReconnectAttempts: number;
  isAuthenticated: boolean;
}

interface DebugEvent {
  timestamp: string;
  type:
    | 'connection'
    | 'message'
    | 'error'
    | 'info'
    | 'statistics'
    | 'notification';
  data: any;
  source: 'match' | 'notification' | 'system';
}

export default function SignalRDebugger() {
  // Connection states
  const [matchSimulationConnected, setMatchSimulationConnected] =
    useState(false);
  const [notificationConnected, setNotificationConnected] = useState(false);
  const [connectionStats, setConnectionStats] =
    useState<ConnectionStats | null>(null);

  // Match statistics
  const [matchId, setMatchId] = useState<number>(12);
  const [joinedMatchGroup, setJoinedMatchGroup] = useState(false);
  const [receivedStats, setReceivedStats] = useState<MatchStatistics[]>([]);

  // Notifications
  const [receivedNotifications, setReceivedNotifications] = useState<
    NotificationData[]
  >([]);
  const [notificationListenerActive, setNotificationListenerActive] =
    useState(false);

  // Match events
  const [receivedMatchEvents, setReceivedMatchEvents] = useState<
    MatchEventData[]
  >([]);
  const [simulationProgress, setSimulationProgress] = useState<
    SimulationProgressData[]
  >([]);

  // Logging and debugging
  const [logs, setLogs] = useState<DebugEvent[]>([]);
  const [selectedTab, setSelectedTab] = useState<
    'overview' | 'logs' | 'statistics' | 'notifications' | 'events'
  >('overview');
  const [autoScroll, setAutoScroll] = useState(true);
  const [maxLogs, setMaxLogs] = useState(50);

  // Filters
  const [logFilter, setLogFilter] = useState<
    'all' | 'connection' | 'message' | 'error' | 'statistics' | 'notification'
  >('all');
  const [sourceFilter, setSourceFilter] = useState<
    'all' | 'match' | 'notification' | 'system'
  >('all');

  const addLog = (
    type: DebugEvent['type'],
    message: string,
    data?: any,
    source: DebugEvent['source'] = 'system'
  ) => {
    const event: DebugEvent = {
      timestamp: new Date().toISOString(),
      type,
      data: { message, ...data },
      source,
    };

    setLogs((prev) => {
      const newLogs = [event, ...prev.slice(0, maxLogs - 1)];
      return newLogs;
    });

    console.log(`[SignalR ${source.toUpperCase()}] ${message}`, data || '');
  };

  // Initialize SignalR connections
  useEffect(() => {
    const initSignalR = async () => {
      addLog(
        'connection',
        '🔄 Initializing SignalR connections...',
        null,
        'system'
      );

      if (!authService.isAuthenticated()) {
        addLog('error', '❌ User not authenticated', null, 'system');
        return;
      }

      const isAuthenticated = authService.isAuthenticated();
      const token = await authService.getValidAccessToken();
      const currentUser = authService.getCurrentUser();

      addLog(
        'info',
        `✅ Auth status: ${isAuthenticated}`,
        {
          hasToken: !!token,
          userId: currentUser?.claimNameId || currentUser?.sub,
          email: currentUser?.email,
          role: currentUser?.role,
        },
        'system'
      );

      try {
        // Connect match simulation hub
        const matchConnected = await signalRService.connectMatchSimulation();
        setMatchSimulationConnected(matchConnected);

        if (matchConnected) {
          addLog(
            'connection',
            '✅ Match simulation SignalR connected',
            {
              state: signalRService.getMatchSimulationConnectionState(),
            },
            'match'
          );
        } else {
          addLog(
            'error',
            '❌ Match simulation SignalR connection failed',
            null,
            'match'
          );
        }

        // Connect notification hub
        const notificationConnected =
          await signalRService.connectNotifications();
        setNotificationConnected(notificationConnected);

        if (notificationConnected) {
          addLog(
            'connection',
            '✅ Notification SignalR connected',
            {
              state: signalRService.getNotificationConnectionState(),
            },
            'notification'
          );
        } else {
          addLog(
            'error',
            '❌ Notification SignalR connection failed',
            null,
            'notification'
          );
        }

        // Update connection stats
        updateConnectionStats();
      } catch (error) {
        const errorMessage =
          error instanceof Error ? error.message : String(error);
        addLog(
          'error',
          `❌ SignalR initialization error`,
          { error: errorMessage },
          'system'
        );
      }
    };

    initSignalR();
  }, []);

  // Update connection statistics
  const updateConnectionStats = () => {
    const stats = signalRService.getConnectionStats();
    setConnectionStats(stats);
    addLog('info', '📊 Connection stats updated', stats, 'system');
  };

  // Connection control functions
  const connectMatchSimulation = async () => {
    addLog(
      'info',
      '🔄 Manually connecting match simulation hub...',
      null,
      'match'
    );
    try {
      const connected = await signalRService.connectMatchSimulation();
      setMatchSimulationConnected(connected);
      updateConnectionStats();
      addLog(
        'connection',
        connected
          ? '✅ Match simulation connected'
          : '❌ Match simulation connection failed',
        null,
        'match'
      );
    } catch (error) {
      const errorMessage =
        error instanceof Error ? error.message : String(error);
      addLog(
        'error',
        '❌ Match simulation connection error',
        { error: errorMessage },
        'match'
      );
    }
  };

  const disconnectMatchSimulation = async () => {
    addLog(
      'info',
      '🔄 Manually disconnecting match simulation hub...',
      null,
      'match'
    );
    try {
      await signalRService.disconnectMatchSimulation();
      setMatchSimulationConnected(false);
      setJoinedMatchGroup(false);
      updateConnectionStats();
      addLog('connection', '✅ Match simulation disconnected', null, 'match');
    } catch (error) {
      const errorMessage =
        error instanceof Error ? error.message : String(error);
      addLog(
        'error',
        '❌ Match simulation disconnection error',
        { error: errorMessage },
        'match'
      );
    }
  };

  const connectNotifications = async () => {
    addLog(
      'info',
      '🔄 Manually connecting notification hub...',
      null,
      'notification'
    );
    try {
      const connected = await signalRService.connectNotifications();
      setNotificationConnected(connected);
      updateConnectionStats();
      addLog(
        'connection',
        connected
          ? '✅ Notifications connected'
          : '❌ Notifications connection failed',
        null,
        'notification'
      );
    } catch (error) {
      const errorMessage =
        error instanceof Error ? error.message : String(error);
      addLog(
        'error',
        '❌ Notifications connection error',
        { error: errorMessage },
        'notification'
      );
    }
  };

  const disconnectNotifications = async () => {
    addLog(
      'info',
      '🔄 Manually disconnecting notification hub...',
      null,
      'notification'
    );
    try {
      await signalRService.disconnectNotifications();
      setNotificationConnected(false);
      setNotificationListenerActive(false);
      updateConnectionStats();
      addLog(
        'connection',
        '✅ Notifications disconnected',
        null,
        'notification'
      );
    } catch (error) {
      const errorMessage =
        error instanceof Error ? error.message : String(error);
      addLog(
        'error',
        '❌ Notifications disconnection error',
        { error: errorMessage },
        'notification'
      );
    }
  };

  const disconnectAll = async () => {
    addLog(
      'info',
      '🔄 Disconnecting all SignalR connections...',
      null,
      'system'
    );
    try {
      await signalRService.disconnect();
      setMatchSimulationConnected(false);
      setNotificationConnected(false);
      setJoinedMatchGroup(false);
      setNotificationListenerActive(false);
      updateConnectionStats();
      addLog('connection', '✅ All connections disconnected', null, 'system');
    } catch (error) {
      const errorMessage =
        error instanceof Error ? error.message : String(error);
      addLog(
        'error',
        '❌ Disconnect all error',
        { error: errorMessage },
        'system'
      );
    }
  };

  const resetConnections = async () => {
    addLog('info', '🔄 Resetting all SignalR connections...', null, 'system');
    try {
      const connected = await signalRService.resetConnection();
      setMatchSimulationConnected(signalRService.isMatchSimulationActive());
      setNotificationConnected(signalRService.isNotificationActive());
      updateConnectionStats();
      addLog(
        'connection',
        connected
          ? '✅ Connections reset successfully'
          : '❌ Connection reset failed',
        null,
        'system'
      );
    } catch (error) {
      const errorMessage =
        error instanceof Error ? error.message : String(error);
      addLog(
        'error',
        '❌ Reset connections error',
        { error: errorMessage },
        'system'
      );
    }
  };
  // Set up all SignalR event listeners
  useEffect(() => {
    if (!matchSimulationConnected && !notificationConnected) return;

    addLog(
      'info',
      '👂 Setting up comprehensive event listeners...',
      null,
      'system'
    );

    // Match statistics listener
    if (matchSimulationConnected) {
      signalRService.onMatchStatisticsUpdate(
        (
          method: String,
          receivedMatchId: number,
          statistics: MatchStatistics
        ) => {
          addLog(
            'statistics',
            `📊 Match statistics received`,
            {
              matchId: receivedMatchId,
              homeTeam: statistics.homeTeam.name,
              awayTeam: statistics.awayTeam.name,
              homeScore: statistics.homeTeam.score,
              awayScore: statistics.awayTeam.score,
              minute: statistics.matchInfo.currentMinute,
              status: statistics.matchInfo.status,
              eventType: statistics.matchInfo.eventType,
              eventTeam: statistics.matchInfo.eventTeam,
            },
            'match'
          );

          setReceivedStats((prev) => [statistics, ...prev.slice(0, 9)]);
        }
      );

      // Match events listener
      signalRService.onMatchEvent(
        (method: string, match_id: string, eventData: MatchEventData) => {
          addLog(
            'message',
            `⚽ Match event received`,
            {
              action: eventData.action,
              team: eventData.team,
              player: eventData.player,
              minute: eventData.minute,
              eventType: eventData.event_type,
              outcome: eventData.outcome,
            },
            'match'
          );

          setReceivedMatchEvents((prev) => [eventData, ...prev.slice(0, 19)]);
        }
      );

      // Simulation progress listener
      signalRService.onSimulationProgress(
        (progressData: SimulationProgressData) => {
          addLog(
            'message',
            `📈 Simulation progress update`,
            {
              simulationId: progressData.simulationId,
              matchId: progressData.matchId,
              progress: progressData.progress,
              status: progressData.status,
              currentEvent: progressData.currentEvent,
              totalEvents: progressData.totalEvents,
            },
            'match'
          );

          setSimulationProgress((prev) => [progressData, ...prev.slice(0, 9)]);
        }
      );

      // Simulation completion listener
      signalRService.onSimulationComplete(
        (simulationId: string, finalScore: { home: number; away: number }) => {
          addLog(
            'message',
            `🏁 Simulation completed`,
            {
              simulationId,
              finalScore,
            },
            'match'
          );
        }
      );

      // Simulation error listener
      signalRService.onSimulationError((matchId: number, error: string) => {
        addLog(
          'error',
          `❌ Simulation error`,
          {
            matchId,
            error,
          },
          'match'
        );
      });
    }

    // Notification listeners
    if (notificationConnected) {
      setNotificationListenerActive(true);

      signalRService.onNotification((notification: NotificationData) => {
        addLog(
          'notification',
          `🔔 Notification received`,
          {
            id: notification.id,
            title: notification.title,
            type: notification.type,
            isRead: notification.isRead,
          },
          'notification'
        );

        setReceivedNotifications((prev) => [
          notification,
          ...prev.slice(0, 19),
        ]);
      });

      signalRService.onMatchStartNotificationAsync(
        (notification: NotificationData, simulationId: string) => {
          addLog(
            'notification',
            `🚀 Match start notification`,
            {
              simulationId,
              title: notification.title,
            },
            'notification'
          );
        }
      );

      signalRService.onMatchEndNotificationAsync(
        (notification: NotificationData, simulationId: string) => {
          addLog(
            'notification',
            `🏁 Match end notification`,
            {
              simulationId,
              title: notification.title,
            },
            'notification'
          );
        }
      );

      signalRService.onSimulationStartNotification(
        (notification: NotificationData, simulationId: string) => {
          addLog(
            'notification',
            `🎯 Simulation start notification`,
            {
              simulationId,
              title: notification.title,
            },
            'notification'
          );
        }
      );
    }

    return () => {
      addLog('info', '🧹 Cleaning up event listeners', null, 'system');
      setNotificationListenerActive(false);
    };
  }, [matchSimulationConnected, notificationConnected]);
  // Match group control functions
  const joinMatchGroup = async () => {
    if (!matchSimulationConnected) {
      addLog('error', '❌ Match simulation not connected', null, 'match');
      return;
    }

    addLog(
      'info',
      `🔗 Joining match statistics group for match ${matchId}...`,
      { matchId },
      'match'
    );

    try {
      const joined = await signalRService.joinMatchStatistics(matchId);
      setJoinedMatchGroup(joined);

      if (joined) {
        addLog(
          'message',
          `✅ Successfully joined match ${matchId} statistics group`,
          { matchId },
          'match'
        );
      } else {
        addLog(
          'error',
          `❌ Failed to join match ${matchId} statistics group`,
          { matchId },
          'match'
        );
      }
    } catch (error) {
      const errorMessage =
        error instanceof Error ? error.message : String(error);
      addLog(
        'error',
        `❌ Error joining match group`,
        { matchId, error: errorMessage },
        'match'
      );
    }
  };

  const leaveMatchGroup = async () => {
    addLog(
      'info',
      `🚪 Leaving match statistics group for match ${matchId}...`,
      { matchId },
      'match'
    );

    try {
      await signalRService.leaveMatchStatistics(matchId);
      setJoinedMatchGroup(false);
      addLog(
        'message',
        `✅ Left match ${matchId} statistics group`,
        { matchId },
        'match'
      );
    } catch (error) {
      const errorMessage =
        error instanceof Error ? error.message : String(error);
      addLog(
        'error',
        `❌ Error leaving match group`,
        { matchId, error: errorMessage },
        'match'
      );
    }
  };

  const joinSimulation = async () => {
    if (!matchSimulationConnected) {
      addLog('error', '❌ Match simulation not connected', null, 'match');
      return;
    }

    addLog(
      'info',
      `🎮 Joining simulation for match ${matchId}...`,
      { matchId },
      'match'
    );

    try {
      const joined = await signalRService.joinSimulation(matchId);
      if (joined) {
        addLog(
          'message',
          `✅ Successfully joined simulation for match ${matchId}`,
          { matchId },
          'match'
        );
      } else {
        addLog(
          'error',
          `❌ Failed to join simulation for match ${matchId}`,
          { matchId },
          'match'
        );
      }
    } catch (error) {
      const errorMessage =
        error instanceof Error ? error.message : String(error);
      addLog(
        'error',
        `❌ Error joining simulation`,
        { matchId, error: errorMessage },
        'match'
      );
    }
  };

  const leaveSimulation = async () => {
    addLog(
      'info',
      `🚪 Leaving simulation for match ${matchId}...`,
      { matchId },
      'match'
    );

    try {
      await signalRService.leaveSimulation(matchId);
      addLog(
        'message',
        `✅ Left simulation for match ${matchId}`,
        { matchId },
        'match'
      );
    } catch (error) {
      const errorMessage =
        error instanceof Error ? error.message : String(error);
      addLog(
        'error',
        `❌ Error leaving simulation`,
        { matchId, error: errorMessage },
        'match'
      );
    }
  };
  // Testing and utility functions
  const testMatchMessage = async () => {
    if (!matchSimulationConnected) {
      addLog('error', '❌ Match simulation not connected', null, 'match');
      return;
    }

    addLog(
      'info',
      `🧪 Testing match simulation message for match ${matchId}...`,
      { matchId },
      'match'
    );

    try {
      await signalRService.sendMatchSimulationMessage(
        'TestMessage',
        matchId,
        'Debug test from frontend'
      );
      addLog(
        'message',
        `✅ Test message sent successfully`,
        { matchId },
        'match'
      );
    } catch (error) {
      const errorMessage =
        error instanceof Error ? error.message : String(error);
      addLog(
        'error',
        `❌ Error sending test message`,
        { matchId, error: errorMessage },
        'match'
      );
    }
  };

  const testNotificationMessage = async () => {
    if (!notificationConnected) {
      addLog(
        'error',
        '❌ Notification hub not connected',
        null,
        'notification'
      );
      return;
    }

    addLog('info', `🧪 Testing notification message...`, null, 'notification');

    try {
      await signalRService.sendNotificationMessage(
        'TestNotification',
        'Debug test from frontend'
      );
      addLog(
        'message',
        `✅ Test notification sent successfully`,
        null,
        'notification'
      );
    } catch (error) {
      const errorMessage =
        error instanceof Error ? error.message : String(error);
      addLog(
        'error',
        `❌ Error sending test notification`,
        { error: errorMessage },
        'notification'
      );
    }
  };

  const clearAllData = () => {
    setReceivedStats([]);
    setReceivedNotifications([]);
    setReceivedMatchEvents([]);
    setSimulationProgress([]);
    setLogs([]);
    addLog('info', '🧹 All data cleared', null, 'system');
  };

  const removeAllListeners = () => {
    signalRService.removeAllListeners();
    setNotificationListenerActive(false);
    addLog('info', '🧹 All event listeners removed', null, 'system');
  };

  const ensureConnection = async () => {
    addLog('info', '🔄 Ensuring SignalR connections...', null, 'system');
    try {
      const connected = await signalRService.ensureConnection();
      setMatchSimulationConnected(signalRService.isMatchSimulationActive());
      setNotificationConnected(signalRService.isNotificationActive());
      updateConnectionStats();
      addLog(
        'connection',
        connected
          ? '✅ Connections ensured'
          : '❌ Failed to ensure connections',
        null,
        'system'
      );
    } catch (error) {
      const errorMessage =
        error instanceof Error ? error.message : String(error);
      addLog(
        'error',
        '❌ Error ensuring connections',
        { error: errorMessage },
        'system'
      );
    }
  };

  // Filter functions
  const getFilteredLogs = () => {
    return logs.filter((log) => {
      const typeMatch = logFilter === 'all' || log.type === logFilter;
      const sourceMatch = sourceFilter === 'all' || log.source === sourceFilter;
      return typeMatch && sourceMatch;
    });
  };

  // Tab content renderers
  const renderOverviewTab = () => (
    <div className="space-y-6">
      {/* Connection Status Cards */}
      <div className="grid grid-cols-1 gap-4 md:grid-cols-2">
        <div
          className={`rounded-lg border p-4 ${matchSimulationConnected ? 'border-green-300 bg-green-50' : 'border-red-300 bg-red-50'}`}
        >
          <h3 className="mb-2 font-semibold text-gray-700">
            Match Simulation Hub
          </h3>
          <p
            className={`font-medium ${matchSimulationConnected ? 'text-green-600' : 'text-red-600'}`}
          >
            {matchSimulationConnected ? '✅ Connected' : '❌ Disconnected'}
          </p>
          <p className="mt-1 text-sm text-gray-600">
            State:{' '}
            {connectionStats?.matchSimulation.connectionState || 'Unknown'}
          </p>
          <div className="mt-3 space-x-2">
            {matchSimulationConnected ? (
              <button
                onClick={disconnectMatchSimulation}
                className="rounded bg-red-500 px-3 py-1 text-sm text-white hover:bg-red-600"
              >
                Disconnect
              </button>
            ) : (
              <button
                onClick={connectMatchSimulation}
                className="rounded bg-green-500 px-3 py-1 text-sm text-white hover:bg-green-600"
              >
                Connect
              </button>
            )}
          </div>
        </div>

        <div
          className={`rounded-lg border p-4 ${notificationConnected ? 'border-green-300 bg-green-50' : 'border-red-300 bg-red-50'}`}
        >
          <h3 className="mb-2 font-semibold text-gray-700">Notification Hub</h3>
          <p
            className={`font-medium ${notificationConnected ? 'text-green-600' : 'text-red-600'}`}
          >
            {notificationConnected ? '✅ Connected' : '❌ Disconnected'}
          </p>
          <p className="mt-1 text-sm text-gray-600">
            State: {connectionStats?.notification.connectionState || 'Unknown'}
          </p>
          <div className="mt-3 space-x-2">
            {notificationConnected ? (
              <button
                onClick={disconnectNotifications}
                className="rounded bg-red-500 px-3 py-1 text-sm text-white hover:bg-red-600"
              >
                Disconnect
              </button>
            ) : (
              <button
                onClick={connectNotifications}
                className="rounded bg-green-500 px-3 py-1 text-sm text-white hover:bg-green-600"
              >
                Connect
              </button>
            )}
          </div>
        </div>
      </div>

      {/* System Controls */}
      <div className="grid grid-cols-1 gap-4 md:grid-cols-2 lg:grid-cols-4">
        <div className="rounded-lg border bg-gray-50 p-4">
          <h4 className="mb-2 font-medium text-gray-700">System Controls</h4>
          <div className="space-y-2">
            <button
              onClick={resetConnections}
              className="w-full rounded bg-blue-500 px-3 py-2 text-sm text-white hover:bg-blue-600"
            >
              Reset All
            </button>
            <button
              onClick={disconnectAll}
              className="w-full rounded bg-red-500 px-3 py-2 text-sm text-white hover:bg-red-600"
            >
              Disconnect All
            </button>
            <button
              onClick={ensureConnection}
              className="w-full rounded bg-green-500 px-3 py-2 text-sm text-white hover:bg-green-600"
            >
              Ensure Connection
            </button>
          </div>
        </div>

        <div className="rounded-lg border bg-gray-50 p-4">
          <h4 className="mb-2 font-medium text-gray-700">Match Controls</h4>
          <div className="space-y-2">
            <input
              type="number"
              value={matchId}
              onChange={(e) => setMatchId(parseInt(e.target.value))}
              className="w-full rounded border px-2 py-1 text-sm"
              placeholder="Match ID"
            />
            <button
              onClick={joinedMatchGroup ? leaveMatchGroup : joinMatchGroup}
              disabled={!matchSimulationConnected}
              className={`w-full rounded px-3 py-2 text-sm text-white disabled:bg-gray-400 ${
                joinedMatchGroup
                  ? 'bg-red-500 hover:bg-red-600'
                  : 'bg-blue-500 hover:bg-blue-600'
              }`}
            >
              {joinedMatchGroup ? 'Leave Group' : 'Join Group'}
            </button>
            <button
              onClick={joinSimulation}
              disabled={!matchSimulationConnected}
              className="w-full rounded bg-purple-500 px-3 py-2 text-sm text-white hover:bg-purple-600 disabled:bg-gray-400"
            >
              Join Simulation
            </button>
          </div>
        </div>

        <div className="rounded-lg border bg-gray-50 p-4">
          <h4 className="mb-2 font-medium text-gray-700">Testing</h4>
          <div className="space-y-2">
            <button
              onClick={testMatchMessage}
              disabled={!matchSimulationConnected}
              className="w-full rounded bg-orange-500 px-3 py-2 text-sm text-white hover:bg-orange-600 disabled:bg-gray-400"
            >
              Test Match Msg
            </button>
            <button
              onClick={testNotificationMessage}
              disabled={!notificationConnected}
              className="w-full rounded bg-cyan-500 px-3 py-2 text-sm text-white hover:bg-cyan-600 disabled:bg-gray-400"
            >
              Test Notification
            </button>
            <button
              onClick={updateConnectionStats}
              className="w-full rounded bg-gray-500 px-3 py-2 text-sm text-white hover:bg-gray-600"
            >
              Refresh Stats
            </button>
          </div>
        </div>

        <div className="rounded-lg border bg-gray-50 p-4">
          <h4 className="mb-2 font-medium text-gray-700">Data & Listeners</h4>
          <div className="space-y-2">
            <button
              onClick={clearAllData}
              className="w-full rounded bg-yellow-500 px-3 py-2 text-sm text-white hover:bg-yellow-600"
            >
              Clear All Data
            </button>
            <button
              onClick={removeAllListeners}
              className="w-full rounded bg-red-500 px-3 py-2 text-sm text-white hover:bg-red-600"
            >
              Remove Listeners
            </button>
            <div className="mt-2 text-xs text-gray-600">
              <div>Stats: {receivedStats.length}</div>
              <div>Notifications: {receivedNotifications.length}</div>
              <div>Events: {receivedMatchEvents.length}</div>
            </div>
          </div>
        </div>
      </div>

      {/* Connection Statistics */}
      {connectionStats && (
        <div className="rounded-lg border bg-gray-50 p-4">
          <h4 className="mb-2 font-medium text-gray-700">
            Connection Statistics
          </h4>
          <div className="grid grid-cols-1 gap-4 text-sm md:grid-cols-2">
            <div>
              <p>
                <strong>Match Simulation:</strong>
              </p>
              <ul className="ml-4 text-gray-600">
                <li>
                  Connected:{' '}
                  {connectionStats.matchSimulation.isConnected ? 'Yes' : 'No'}
                </li>
                <li>
                  State: {connectionStats.matchSimulation.connectionState}
                </li>
                <li>
                  Has Connection:{' '}
                  {connectionStats.matchSimulation.hasConnection ? 'Yes' : 'No'}
                </li>
              </ul>
            </div>
            <div>
              <p>
                <strong>Notifications:</strong>
              </p>
              <ul className="ml-4 text-gray-600">
                <li>
                  Connected:{' '}
                  {connectionStats.notification.isConnected ? 'Yes' : 'No'}
                </li>
                <li>State: {connectionStats.notification.connectionState}</li>
                <li>
                  Has Connection:{' '}
                  {connectionStats.notification.hasConnection ? 'Yes' : 'No'}
                </li>
              </ul>
            </div>
            <div>
              <p>
                <strong>System:</strong>
              </p>
              <ul className="ml-4 text-gray-600">
                <li>
                  Reconnect Attempts: {connectionStats.reconnectAttempts}/
                  {connectionStats.maxReconnectAttempts}
                </li>
                <li>
                  Authenticated:{' '}
                  {connectionStats.isAuthenticated ? 'Yes' : 'No'}
                </li>
                <li>Match Group Joined: {joinedMatchGroup ? 'Yes' : 'No'}</li>
                <li>
                  Notification Listener:{' '}
                  {notificationListenerActive ? 'Active' : 'Inactive'}
                </li>
              </ul>
            </div>
          </div>
        </div>
      )}
    </div>
  );

  const renderLogsTab = () => (
    <div className="space-y-4">
      {/* Log Controls */}
      <div className="flex flex-wrap items-center gap-4 rounded-lg bg-gray-50 p-4">
        <div className="flex items-center gap-2">
          <label className="text-sm font-medium text-gray-700">Type:</label>
          <select
            value={logFilter}
            onChange={(e) => setLogFilter(e.target.value as any)}
            className="rounded border px-2 py-1 text-sm"
          >
            <option value="all">All</option>
            <option value="connection">Connection</option>
            <option value="message">Message</option>
            <option value="error">Error</option>
            <option value="statistics">Statistics</option>
            <option value="notification">Notification</option>
          </select>
        </div>

        <div className="flex items-center gap-2">
          <label className="text-sm font-medium text-gray-700">Source:</label>
          <select
            value={sourceFilter}
            onChange={(e) => setSourceFilter(e.target.value as any)}
            className="rounded border px-2 py-1 text-sm"
          >
            <option value="all">All</option>
            <option value="system">System</option>
            <option value="match">Match</option>
            <option value="notification">Notification</option>
          </select>
        </div>

        <div className="flex items-center gap-2">
          <label className="text-sm font-medium text-gray-700">Max Logs:</label>
          <input
            type="number"
            value={maxLogs}
            onChange={(e) => setMaxLogs(parseInt(e.target.value) || 50)}
            className="w-20 rounded border px-2 py-1 text-sm"
            min="10"
            max="500"
          />
        </div>

        <label className="flex items-center gap-2 text-sm">
          <input
            type="checkbox"
            checked={autoScroll}
            onChange={(e) => setAutoScroll(e.target.checked)}
            className="rounded"
          />
          Auto Scroll
        </label>

        <button
          onClick={() => setLogs([])}
          className="rounded bg-red-500 px-3 py-1 text-sm text-white hover:bg-red-600"
        >
          Clear Logs
        </button>
      </div>

      {/* Logs Display */}
      <div className="h-96 overflow-y-auto rounded-lg bg-gray-900 p-4 font-mono text-sm text-green-400">
        {getFilteredLogs().length === 0 ? (
          <p className="text-gray-500">No logs match the current filters...</p>
        ) : (
          getFilteredLogs().map((log, index) => (
            <div key={index} className="mb-2 border-b border-gray-700 pb-2">
              <div className="flex items-start justify-between">
                <div className="flex-1">
                  <span className="text-gray-400">
                    [{new Date(log.timestamp).toLocaleTimeString()}]
                  </span>
                  <span
                    className={`ml-2 rounded px-2 py-0.5 text-xs font-bold ${
                      log.type === 'error'
                        ? 'bg-red-500 text-white'
                        : log.type === 'connection'
                          ? 'bg-blue-500 text-white'
                          : log.type === 'statistics'
                            ? 'bg-green-500 text-white'
                            : log.type === 'notification'
                              ? 'bg-purple-500 text-white'
                              : 'bg-gray-500 text-white'
                    }`}
                  >
                    {log.type.toUpperCase()}
                  </span>
                  <span
                    className={`ml-2 rounded px-2 py-0.5 text-xs ${
                      log.source === 'match'
                        ? 'bg-orange-500 text-white'
                        : log.source === 'notification'
                          ? 'bg-cyan-500 text-white'
                          : 'bg-gray-600 text-white'
                    }`}
                  >
                    {log.source.toUpperCase()}
                  </span>
                  <div className="mt-1 text-green-300">{log.data.message}</div>
                  {log.data && Object.keys(log.data).length > 1 && (
                    <div className="mt-1 text-xs text-gray-400">
                      {JSON.stringify(log.data, null, 2)}
                    </div>
                  )}
                </div>
              </div>
            </div>
          ))
        )}
      </div>
    </div>
  );

  const renderStatisticsTab = () => (
    <div className="space-y-4">
      <h4 className="font-semibold text-gray-700">
        Match Statistics ({receivedStats.length})
      </h4>
      {receivedStats.length === 0 ? (
        <p className="py-8 text-center text-gray-500">
          No match statistics received yet...
        </p>
      ) : (
        <div className="space-y-3">
          {receivedStats.map((stats, index) => (
            <div
              key={index}
              className="rounded-lg border border-blue-200 bg-blue-50 p-4"
            >
              <div className="mb-2 flex items-start justify-between">
                <div>
                  <h5 className="font-medium text-gray-800">
                    {stats.homeTeam.name} {stats.homeTeam.score} -{' '}
                    {stats.awayTeam.score} {stats.awayTeam.name}
                  </h5>
                  <p className="text-sm text-gray-600">
                    Match {stats.matchId} | {stats.matchInfo.currentMinute}' |{' '}
                    {stats.matchInfo.status}
                  </p>
                  <p className="text-sm text-blue-600">
                    {stats.matchInfo.eventType} by {stats.matchInfo.eventTeam}
                  </p>
                </div>
                <span className="text-xs text-gray-500">{stats.timeStamp}</span>
              </div>

              <div className="grid grid-cols-2 gap-2 text-xs md:grid-cols-4">
                <div>
                  <strong>Possession:</strong> {stats.homeTeam.possession}% -{' '}
                  {stats.awayTeam.possession}%
                </div>
                <div>
                  <strong>Shots:</strong> {stats.homeTeam.shots} -{' '}
                  {stats.awayTeam.shots}
                </div>
                <div>
                  <strong>On Target:</strong> {stats.homeTeam.shotsOnTarget} -{' '}
                  {stats.awayTeam.shotsOnTarget}
                </div>
                <div>
                  <strong>Corners:</strong> {stats.homeTeam.corners} -{' '}
                  {stats.awayTeam.corners}
                </div>
                <div>
                  <strong>Fouls:</strong> {stats.homeTeam.fouls} -{' '}
                  {stats.awayTeam.fouls}
                </div>
                <div>
                  <strong>Yellow Cards:</strong> {stats.homeTeam.yellowCards} -{' '}
                  {stats.awayTeam.yellowCards}
                </div>
                <div>
                  <strong>Red Cards:</strong> {stats.homeTeam.redCards} -{' '}
                  {stats.awayTeam.redCards}
                </div>
                <div>
                  <strong>Pass Acc:</strong> {stats.homeTeam.passAccuracy}% -{' '}
                  {stats.awayTeam.passAccuracy}%
                </div>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );

  const renderNotificationsTab = () => (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h4 className="font-semibold text-gray-700">
          Notifications ({receivedNotifications.length})
        </h4>
        <div
          className={`rounded px-2 py-1 text-xs ${notificationListenerActive ? 'bg-green-100 text-green-600' : 'bg-red-100 text-red-600'}`}
        >
          Listener: {notificationListenerActive ? 'Active' : 'Inactive'}
        </div>
      </div>

      {receivedNotifications.length === 0 ? (
        <p className="py-8 text-center text-gray-500">
          No notifications received yet...
        </p>
      ) : (
        <div className="space-y-3">
          {receivedNotifications.map((notification, index) => (
            <div
              key={index}
              className="rounded-lg border border-purple-200 bg-purple-50 p-4"
            >
              <div className="flex items-start justify-between">
                <div className="flex-1">
                  <h5 className="font-medium text-gray-800">
                    {notification.title}
                  </h5>
                  <p className="mt-1 text-sm text-gray-600">
                    {notification.content}
                  </p>
                  <div className="mt-2 flex gap-2 text-xs">
                    <span
                      className={`rounded px-2 py-1 ${
                        notification.type === 'Success'
                          ? 'bg-green-100 text-green-600'
                          : notification.type === 'Error'
                            ? 'bg-red-100 text-red-600'
                            : notification.type === 'Warning'
                              ? 'bg-yellow-100 text-yellow-600'
                              : 'bg-blue-100 text-blue-600'
                      }`}
                    >
                      {notification.type}
                    </span>
                    <span
                      className={`rounded px-2 py-1 ${notification.isRead ? 'bg-gray-100 text-gray-600' : 'bg-blue-100 text-blue-600'}`}
                    >
                      {notification.isRead ? 'Read' : 'Unread'}
                    </span>
                  </div>
                </div>
                <span className="text-xs text-gray-500">
                  {new Date(notification.time).toLocaleTimeString()}
                </span>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );

  const renderEventsTab = () => (
    <div className="space-y-4">
      <div className="grid grid-cols-1 gap-4 md:grid-cols-2">
        {/* Match Events */}
        <div>
          <h4 className="mb-3 font-semibold text-gray-700">
            Match Events ({receivedMatchEvents.length})
          </h4>
          {receivedMatchEvents.length === 0 ? (
            <p className="py-4 text-center text-gray-500">
              No match events received yet...
            </p>
          ) : (
            <div className="max-h-64 space-y-2 overflow-y-auto">
              {receivedMatchEvents.map((event, index) => (
                <div
                  key={index}
                  className="rounded-lg border border-orange-200 bg-orange-50 p-3"
                >
                  <div className="flex items-start justify-between">
                    <div>
                      <p className="font-medium text-gray-800">
                        {event.action}
                      </p>
                      <p className="text-sm text-gray-600">
                        {event.team} - {event.player}
                      </p>
                      <p className="text-xs text-orange-600">
                        {event.minute}' | {event.event_type}
                      </p>
                    </div>
                    <span className="text-xs text-gray-500">
                      {new Date(event.timestamp).toLocaleTimeString()}
                    </span>
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>

        {/* Simulation Progress */}
        <div>
          <h4 className="mb-3 font-semibold text-gray-700">
            Simulation Progress ({simulationProgress.length})
          </h4>
          {simulationProgress.length === 0 ? (
            <p className="py-4 text-center text-gray-500">
              No simulation progress received yet...
            </p>
          ) : (
            <div className="max-h-64 space-y-2 overflow-y-auto">
              {simulationProgress.map((progress, index) => (
                <div
                  key={index}
                  className="rounded-lg border border-cyan-200 bg-cyan-50 p-3"
                >
                  <div className="flex items-start justify-between">
                    <div>
                      <p className="font-medium text-gray-800">
                        Match {progress.matchId}
                      </p>
                      <p className="text-sm text-gray-600">
                        Progress: {progress.progress}%
                      </p>
                      <p className="text-xs text-cyan-600">
                        {progress.status} | {progress.currentEvent}/
                        {progress.totalEvents} events
                      </p>
                    </div>
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>
      </div>
    </div>
  );

  return (
    <div className="mx-auto max-w-7xl rounded-lg bg-white p-6 shadow-lg">
      <h2 className="mb-6 text-3xl font-bold text-gray-800">
        Enhanced SignalR Debug Console
      </h2>

      {/* Information Banner */}
      <div className="mb-6 rounded-lg border border-blue-200 bg-blue-50 p-4">
        <h3 className="mb-2 font-semibold text-blue-800">
          📝 Comprehensive SignalR Management:
        </h3>
        <ul className="grid grid-cols-1 gap-1 space-y-1 text-sm text-blue-700 md:grid-cols-2">
          <li>• Complete control over both Match and Notification hubs</li>
          <li>• Real-time monitoring of all SignalR events</li>
          <li>
            • Statistics work for ANY match status (Live, Scheduled, etc.)
          </li>
          <li>• Advanced filtering and debugging capabilities</li>
          <li>• Match ID 12: Atlético Madrid vs Barcelona</li>
          <li>• Your user ID: e269bf43-56a9-414e-bcfc-4144d984edaa</li>
        </ul>
      </div>

      {/* Tab Navigation */}
      <div className="mb-6 flex flex-wrap gap-2 border-b">
        {[
          { key: 'overview', label: 'Overview', icon: '🏠' },
          { key: 'logs', label: 'Logs', icon: '📝' },
          { key: 'statistics', label: 'Statistics', icon: '📊' },
          { key: 'notifications', label: 'Notifications', icon: '🔔' },
          { key: 'events', label: 'Events', icon: '⚽' },
        ].map((tab) => (
          <button
            key={tab.key}
            onClick={() => setSelectedTab(tab.key as any)}
            className={`rounded-t-lg px-4 py-2 font-medium transition-colors ${
              selectedTab === tab.key
                ? 'border-b-2 border-blue-500 bg-blue-500 text-white'
                : 'text-gray-600 hover:bg-gray-50 hover:text-blue-500'
            }`}
          >
            {tab.icon} {tab.label}
          </button>
        ))}
      </div>

      {/* Tab Content */}
      <div className="min-h-96">
        {selectedTab === 'overview' && renderOverviewTab()}
        {selectedTab === 'logs' && renderLogsTab()}
        {selectedTab === 'statistics' && renderStatisticsTab()}
        {selectedTab === 'notifications' && renderNotificationsTab()}
        {selectedTab === 'events' && renderEventsTab()}
      </div>
    </div>
  );
}
