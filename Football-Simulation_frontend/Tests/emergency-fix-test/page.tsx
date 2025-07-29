'use client';

import emergencyMatchSimulationService from '@/Services/EmergencyMatchSimulationService';
import { useState, useEffect } from 'react';

export default function EmergencyFixTestPage() {
  const [logs, setLogs] = useState<string[]>([]);
  const [signalRStatus, setSignalRStatus] = useState<string>('Checking...');
  const [apiStatus, setApiStatus] = useState<string>('Ready');
  const [isSimulating, setIsSimulating] = useState(false);

  const addLog = (message: string) => {
    const timestamp = new Date().toLocaleTimeString();
    setLogs((prev) => [`[${timestamp}] ${message}`, ...prev.slice(0, 19)]);
  };

  useEffect(() => {
    // Check SignalR status
    const checkSignalR = () => {
      if (typeof window !== 'undefined' && (window as any).signalRService) {
        const service = (window as any).signalRService;
        const matchConnected = service.isMatchSimulationConnected ? '✅' : '❌';
        const notifyConnected = service.isNotificationConnected ? '✅' : '❌';
        setSignalRStatus(
          `Match: ${matchConnected} | Notifications: ${notifyConnected}`
        );
      } else {
        setSignalRStatus('❌ SignalR Service not found');
      }
    };

    checkSignalR();
    const interval = setInterval(checkSignalR, 2000);

    // Add initial log
    addLog('🚀 Emergency fix test page loaded');

    return () => clearInterval(interval);
  }, []);

  const testSignalRFix = () => {
    addLog('🔧 Testing SignalR method fixes...');

    if (typeof window !== 'undefined' && (window as any).emergencyFixAll) {
      (window as any).emergencyFixAll();
      addLog('✅ Emergency SignalR fixes applied');
    } else {
      addLog('❌ Emergency fix function not found');
    }
  };

  const testSlowApiSimulation = async () => {
    const testMatchId = 'ce91b630-5b0e-4407-8a5a-234699af0f45';
    setIsSimulating(true);
    addLog(`🎮 Starting emergency simulation test for match ${testMatchId}...`);

    try {
      const result = await emergencyMatchSimulationService.simulateWithProgress(
        testMatchId,
        (progress) => {
          addLog(`📊 ${progress}`);
        }
      );

      addLog(`✅ Emergency simulation completed successfully!`);
      addLog(
        `📊 Result preview: ${JSON.stringify(result).substring(0, 100)}...`
      );
    } catch (error: any) {
      addLog(`❌ Emergency simulation failed: ${error.message}`);
    } finally {
      setIsSimulating(false);
    }
  };

  const testApiMonitoring = () => {
    addLog('⚡ Testing API performance monitoring...');

    if (typeof window !== 'undefined' && (window as any).emergencyShowStats) {
      (window as any).emergencyShowStats();
      addLog('📊 Check console for detailed API stats');
    } else {
      addLog('❌ API monitoring not found');
    }
  };

  const showDebugInfo = () => {
    addLog('🔍 Showing debug information...');

    if (typeof window !== 'undefined') {
      const debugInfo = {
        signalRService: !!(window as any).signalRService,
        emergencyFix: !!(window as any).emergencyFixAll,
        apiStats: !!(window as any).emergencyApiStats,
        emergencyMatchSim: !!(window as any).emergencyMatchSim,
        debugSignalR: !!(window as any).debugSignalR,
      };

      Object.entries(debugInfo).forEach(([key, available]) => {
        addLog(
          `${available ? '✅' : '❌'} ${key}: ${available ? 'Available' : 'Missing'}`
        );
      });
    }
  };

  const runManualDebugCommand = () => {
    if (typeof window !== 'undefined' && (window as any).debugSignalR) {
      (window as any).debugSignalR();
      addLog('🔍 SignalR debug info logged to console');
    } else {
      addLog('❌ debugSignalR function not available');
    }
  };

  return (
    <div className="min-h-screen bg-gradient-to-br from-blue-50 to-green-50 p-8">
      <div className="mx-auto max-w-6xl">
        <div className="mb-6 rounded-lg bg-white p-6 shadow-lg">
          <h1 className="mb-4 text-3xl font-bold text-gray-800">
            🚨 Emergency Fix Test Dashboard
          </h1>
          <p className="mb-6 text-gray-600">
            Test and verify all emergency fixes for SignalR method warnings and
            slow API performance.
          </p>

          {/* Status Cards */}
          <div className="mb-6 grid grid-cols-1 gap-4 md:grid-cols-2">
            <div className="rounded-lg bg-blue-50 p-4">
              <h3 className="mb-2 font-semibold text-blue-800">
                🔗 SignalR Status
              </h3>
              <p className="text-sm text-blue-700">{signalRStatus}</p>
            </div>
            <div className="rounded-lg bg-green-50 p-4">
              <h3 className="mb-2 font-semibold text-green-800">
                ⚡ API Status
              </h3>
              <p className="text-sm text-green-700">{apiStatus}</p>
            </div>
          </div>

          {/* Action Buttons */}
          <div className="mb-6 grid grid-cols-2 gap-4 md:grid-cols-3">
            <button
              onClick={testSignalRFix}
              className="rounded-lg bg-blue-500 px-4 py-2 text-sm font-medium text-white transition-colors hover:bg-blue-600"
            >
              🔧 Test SignalR Fix
            </button>

            <button
              onClick={testSlowApiSimulation}
              disabled={isSimulating}
              className="rounded-lg bg-red-500 px-4 py-2 text-sm font-medium text-white transition-colors hover:bg-red-600 disabled:bg-red-300"
            >
              {isSimulating ? '⏳ Simulating...' : '🎮 Test Slow API'}
            </button>

            <button
              onClick={testApiMonitoring}
              className="rounded-lg bg-green-500 px-4 py-2 text-sm font-medium text-white transition-colors hover:bg-green-600"
            >
              📊 API Monitoring
            </button>

            <button
              onClick={showDebugInfo}
              className="rounded-lg bg-purple-500 px-4 py-2 text-sm font-medium text-white transition-colors hover:bg-purple-600"
            >
              🔍 Debug Info
            </button>

            <button
              onClick={runManualDebugCommand}
              className="rounded-lg bg-yellow-500 px-4 py-2 text-sm font-medium text-white transition-colors hover:bg-yellow-600"
            >
              🛠️ Manual Debug
            </button>

            <button
              onClick={() => {
                if (
                  typeof window !== 'undefined' &&
                  (window as any).cancelAllSimulations
                ) {
                  (window as any).cancelAllSimulations();
                  addLog('🛑 All simulations cancelled');
                }
              }}
              className="rounded-lg bg-red-600 px-4 py-2 text-sm font-medium text-white transition-colors hover:bg-red-700"
            >
              🛑 Cancel All
            </button>
          </div>
        </div>

        {/* Emergency Commands */}
        <div className="mb-6 rounded-lg border border-yellow-200 bg-yellow-50 p-4">
          <h3 className="mb-3 font-semibold text-yellow-800">
            ⚡ Emergency Manual Commands
          </h3>
          <div className="space-y-2 text-sm text-yellow-700">
            <p>
              <code className="rounded bg-yellow-100 px-2 py-1">
                window.emergencyFixAll()
              </code>{' '}
              - Apply all fixes
            </p>
            <p>
              <code className="rounded bg-yellow-100 px-2 py-1">
                window.emergencyShowStats()
              </code>{' '}
              - Show API performance stats
            </p>
            <p>
              <code className="rounded bg-yellow-100 px-2 py-1">
                window.debugSignalR()
              </code>{' '}
              - Show SignalR debug info
            </p>
            <p>
              <code className="rounded bg-yellow-100 px-2 py-1">
                window.cancelAllSimulations()
              </code>{' '}
              - Cancel all active simulations
            </p>
          </div>
        </div>

        {/* Real-time Logs */}
        <div className="rounded-lg bg-gray-900 p-4">
          <h3 className="mb-3 font-semibold text-white">📋 Real-time Logs</h3>
          <div className="h-64 overflow-y-auto">
            {logs.length === 0 ? (
              <p className="text-sm text-gray-400">No logs yet...</p>
            ) : (
              logs.map((log, index) => (
                <div
                  key={index}
                  className="mb-1 font-mono text-sm text-green-400"
                >
                  {log}
                </div>
              ))
            )}
          </div>
        </div>

        {/* Critical Issues Summary */}
        <div className="mt-6 rounded-lg border border-red-200 bg-red-50 p-4">
          <h3 className="mb-3 font-semibold text-red-800">
            🚨 Critical Issues Being Fixed
          </h3>
          <div className="space-y-2 text-sm text-red-700">
            <p>
              ✅ <strong>SignalR Method Warning:</strong> Fixed missing
              'sendmatchendnotificationasync' client method
            </p>
            <p>
              ✅ <strong>Slow API Performance:</strong> Added emergency timeout
              and cancellation for 15+ second requests
            </p>
            <p>
              ✅ <strong>Event Monitoring:</strong> Ultra-aggressive interceptor
              with timeout handling
            </p>
            <p>
              ✅ <strong>Emergency Recovery:</strong> Multiple fallback
              mechanisms and manual triggers
            </p>
          </div>
        </div>
      </div>
    </div>
  );
}
