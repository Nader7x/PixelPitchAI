'use client';

import { useState, useEffect } from 'react';
import signalRService from '@/Services/SignalRService';
import authService from '@/Services/AuthenticationService';

export default function SignalREarlyTestPage() {
  const [isAuthenticated, setIsAuthenticated] = useState(false);
  const [connectionStatus, setConnectionStatus] = useState<any>({});
  const [logs, setLogs] = useState<string[]>([]);

  const addLog = (message: string) => {
    const timestamp = new Date().toLocaleTimeString();
    setLogs((prev) => [`[${timestamp}] ${message}`, ...prev.slice(0, 19)]);
    console.log(`[EarlyTest] ${message}`);
  };

  const refreshStatus = () => {
    const authStatus = authService.isAuthenticated();
    const connectionStats = signalRService.getConnectionStats();
    const isGloballyConnected = signalRService.isGloballyConnected();

    setIsAuthenticated(authStatus);
    setConnectionStatus({
      ...connectionStats,
      isGloballyConnected,
      matchSimulationState: signalRService.getMatchSimulationConnectionState(),
      notificationState: signalRService.getNotificationConnectionState(),
    });

    addLog(
      `Auth: ${authStatus}, Global: ${isGloballyConnected}, Match: ${connectionStats.matchSimulation?.isConnected}, Notification: ${connectionStats.notification?.isConnected}`
    );
  };

  useEffect(() => {
    addLog('🚀 Early SignalR test page loaded');
    refreshStatus();

    // Refresh every 2 seconds
    const interval = setInterval(refreshStatus, 2000);

    return () => clearInterval(interval);
  }, []);

  const testSmartConnection = async () => {
    addLog('🧪 Testing smart connection...');
    try {
      const connected = await signalRService.ensurePageConnection();
      addLog(`Smart connection result: ${connected}`);
      refreshStatus();
    } catch (error) {
      addLog(`Smart connection error: ${error}`);
    }
  };

  const simulateMatchStart = () => {
    addLog('🎬 Simulating match_start event...');
    // This would normally come from the backend, but we can trigger the global handler directly
    if ((signalRService as any).globalMatchEventHandler) {
      (signalRService as any).globalMatchEventHandler(
        'SendMatchEventAsync',
        'test-match-123',
        {
          event_type: 'match_start',
          home_team: 'Early Test Home',
          away_team: 'Early Test Away',
          Score: { home: 0, away: 0 },
          time_seconds: 0,
          timestamp: new Date().toISOString(),
          match_id: 'test-match-123',
        }
      );
      addLog('✅ Simulated match_start event sent to global handler');
    } else {
      addLog('❌ Global handler not found');
    }
  };

  return (
    <div className="min-h-screen bg-gray-100 p-8">
      <div className="mx-auto max-w-4xl">
        <h1 className="mb-6 text-3xl font-bold text-gray-800">
          ⚡🚀 ULTRA-AGGRESSIVE SignalR Test
        </h1>

        <div className="mb-6 rounded-lg border border-red-400 bg-red-100 p-4">
          <h2 className="mb-2 text-lg font-semibold text-red-800">
            🚨 ULTRA-AGGRESSIVE MODE ACTIVE
          </h2>
          <p className="text-sm text-red-700">
            This implementation uses the most aggressive connection strategy
            possible to intercept events with ZERO latency. Check browser
            console for instant event capture logs.
          </p>
        </div>

        <div className="grid grid-cols-1 gap-6 md:grid-cols-2">
          {/* Status Panel */}
          <div className="rounded-lg bg-white p-6 shadow-lg">
            <h2 className="mb-4 text-xl font-semibold text-gray-800">
              Connection Status
            </h2>
            <div className="space-y-2 text-sm">
              <div
                className={`flex items-center gap-2 ${isAuthenticated ? 'text-green-600' : 'text-red-600'}`}
              >
                <span>{isAuthenticated ? '✅' : '❌'}</span>
                <span>Authenticated: {isAuthenticated.toString()}</span>
              </div>
              <div
                className={`flex items-center gap-2 ${connectionStatus.isGloballyConnected ? 'text-green-600' : 'text-orange-600'}`}
              >
                <span>
                  {connectionStatus.isGloballyConnected ? '🌐' : '🔌'}
                </span>
                <span>
                  Globally Connected:{' '}
                  {connectionStatus.isGloballyConnected?.toString()}
                </span>
              </div>
              <div
                className={`flex items-center gap-2 ${connectionStatus.matchSimulation?.isConnected ? 'text-green-600' : 'text-red-600'}`}
              >
                <span>
                  {connectionStatus.matchSimulation?.isConnected ? '⚽' : '❌'}
                </span>
                <span>
                  Match Simulation: {connectionStatus.matchSimulationState}
                </span>
              </div>
              <div
                className={`flex items-center gap-2 ${connectionStatus.notification?.isConnected ? 'text-green-600' : 'text-red-600'}`}
              >
                <span>
                  {connectionStatus.notification?.isConnected ? '📢' : '❌'}
                </span>
                <span>Notifications: {connectionStatus.notificationState}</span>
              </div>
            </div>
          </div>

          {/* Control Panel */}
          <div className="rounded-lg bg-white p-6 shadow-lg">
            <h2 className="mb-4 text-xl font-semibold text-gray-800">
              Test Controls
            </h2>
            <div className="space-y-3">
              <button
                onClick={testSmartConnection}
                className="w-full rounded-lg bg-blue-600 px-4 py-2 text-white transition-colors hover:bg-blue-700"
              >
                🧪 Test Smart Connection
              </button>
              <button
                onClick={simulateMatchStart}
                className="w-full rounded-lg bg-green-600 px-4 py-2 text-white transition-colors hover:bg-green-700"
              >
                🎬 Simulate match_start Event
              </button>
              <button
                onClick={refreshStatus}
                className="w-full rounded-lg bg-gray-600 px-4 py-2 text-white transition-colors hover:bg-gray-700"
              >
                🔄 Refresh Status
              </button>
            </div>
          </div>
        </div>

        {/* Logs Panel */}
        <div className="mt-6 rounded-lg bg-black p-6 shadow-lg">
          <h2 className="mb-4 text-xl font-semibold text-green-400">
            Real-time Logs
          </h2>
          <div className="max-h-96 space-y-1 overflow-y-auto font-mono text-sm text-green-300">
            {logs.map((log, index) => (
              <div key={index} className="whitespace-pre-wrap">
                {log}
              </div>
            ))}
          </div>
        </div>

        {/* Info Panel */}
        <div className="mt-6 rounded-lg bg-blue-50 p-6">
          <h2 className="mb-4 text-xl font-semibold text-blue-800">
            📋 Test Information
          </h2>
          <div className="space-y-2 text-sm text-blue-700">
            <p>
              <strong>Purpose:</strong> This page tests the early SignalR
              connection implementation.
            </p>
            <p>
              <strong>Expected:</strong> SignalR should connect automatically
              when you're authenticated, very early in the app lifecycle.
            </p>
            <p>
              <strong>Problem Solved:</strong> Before this fix, SignalR
              connected too late and missed early events like match_start.
            </p>
            <p>
              <strong>Check localStorage:</strong> After simulating match_start,
              check browser localStorage for team data.
            </p>
          </div>
        </div>
      </div>
    </div>
  );
}
