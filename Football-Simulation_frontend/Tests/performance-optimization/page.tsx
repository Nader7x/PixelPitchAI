'use client';

import { useEffect, useState } from 'react';
import { optimizedApiService, perfMonitor } from '@/lib/performanceMonitor';

export default function PerformanceOptimizationPage() {
  const [stats, setStats] = useState<any>({});
  const [logs, setLogs] = useState<string[]>([]);
  const [isMonitoring, setIsMonitoring] = useState(false);

  const addLog = (message: string) => {
    const timestamp = new Date().toLocaleTimeString();
    setLogs((prev) => [`[${timestamp}] ${message}`, ...prev.slice(0, 29)]);
  };

  useEffect(() => {
    const updateStats = () => {
      setStats(perfMonitor.getStats());
    };

    updateStats();
    const interval = setInterval(updateStats, 2000);

    return () => clearInterval(interval);
  }, []);

  const testSlowApiCall = async () => {
    addLog('🧪 Testing slow API call simulation...');
    try {
      const testMatchId = 'ce91b630-5b0e-4407-8a5a-234699af0f45';
      addLog(`🎮 Starting match simulation: ${testMatchId}`);

      const result =
        await optimizedApiService.simulateMatchOptimized(testMatchId);
      addLog(`✅ Match simulation completed successfully`);
      addLog(`📊 Result: ${JSON.stringify(result).substring(0, 100)}...`);
    } catch (error) {
      addLog(`❌ Match simulation failed: ${error}`);
    }
  };

  const testFastApiCall = async () => {
    addLog('⚡ Testing fast API call...');
    try {
      const result = await optimizedApiService.request(
        '/api/matches',
        { method: 'GET' },
        5000
      );
      addLog(`✅ Fast API call completed`);
    } catch (error) {
      addLog(`❌ Fast API call failed: ${error}`);
    }
  };

  const startPerformanceMonitoring = () => {
    if (isMonitoring) return;

    setIsMonitoring(true);
    addLog('🔍 Started comprehensive performance monitoring');

    // Monitor console for performance logs
    const originalConsoleLog = console.log;
    console.log = function (...args) {
      if (
        args[0] &&
        typeof args[0] === 'string' &&
        args[0].includes('[PERF]')
      ) {
        addLog(args.join(' '));
      }
      originalConsoleLog.apply(console, args);
    };

    // Stop monitoring after 5 minutes
    setTimeout(() => {
      setIsMonitoring(false);
      console.log = originalConsoleLog;
      addLog('⏹️ Performance monitoring stopped');
    }, 300000);
  };

  const analyzeSlowRequests = () => {
    addLog('🔍 Analyzing slow request patterns...');

    addLog('📊 Common causes of slow API requests:');
    addLog('  • Large dataset processing (match simulations)');
    addLog('  • Database query optimization needed');
    addLog('  • Network latency to backend server');
    addLog('  • Insufficient server resources');
    addLog('  • Missing caching layers');

    addLog('🔧 Recommended optimizations:');
    addLog('  • Implement streaming responses for simulations');
    addLog('  • Use WebSocket progress updates during processing');
    addLog('  • Add Redis caching for frequently accessed data');
    addLog('  • Optimize database queries with indexes');
    addLog('  • Implement request queuing for heavy operations');
    addLog('  • Use CDN for static content delivery');
  };

  const checkSignalRPerformance = () => {
    addLog('⚡ Checking SignalR performance impact...');

    if (typeof window !== 'undefined' && (window as any).signalRService) {
      const service = (window as any).signalRService;
      const isConnected = service.isGloballyConnected();

      addLog(
        `🔌 SignalR Status: ${isConnected ? 'Connected' : 'Disconnected'}`
      );
      addLog(
        `📡 Connection State: ${service.getMatchSimulationConnectionState()}`
      );

      if (isConnected) {
        addLog('✅ SignalR is connected and ready for instant event capture');
        addLog(
          '🎯 Events will be received immediately when backend sends them'
        );
      } else {
        addLog('⚠️ SignalR not connected - events may be missed');
      }
    } else {
      addLog('❌ SignalR service not found');
    }
  };

  return (
    <div className="min-h-screen bg-gray-100 p-8">
      <div className="mx-auto max-w-6xl">
        <h1 className="mb-6 text-3xl font-bold text-gray-800">
          🚀 Performance Optimization Center
        </h1>

        <div className="mb-6 rounded-lg border border-yellow-400 bg-yellow-100 p-4">
          <h2 className="mb-2 text-lg font-semibold text-yellow-800">
            ⚠️ Slow API Request Detected
          </h2>
          <p className="text-sm text-yellow-700">
            Match simulation API taking 15+ seconds. This page helps diagnose
            and optimize performance issues that could affect SignalR event
            timing.
          </p>
        </div>

        <div className="grid grid-cols-1 gap-6 lg:grid-cols-2">
          {/* Stats Panel */}
          <div className="rounded-lg bg-white p-6 shadow-lg">
            <h2 className="mb-4 text-xl font-semibold text-gray-800">
              📊 Performance Stats
            </h2>
            <div className="space-y-2 text-sm">
              <div className="flex justify-between">
                <span>Active Requests:</span>
                <span className="font-mono">{stats.activeRequests || 0}</span>
              </div>
              <div className="flex justify-between">
                <span>Slow Request Threshold:</span>
                <span className="font-mono">
                  {stats.slowRequestThreshold || 0}ms
                </span>
              </div>
              <div className="flex justify-between">
                <span>Performance API:</span>
                <span
                  className={
                    stats.performanceSupported
                      ? 'text-green-600'
                      : 'text-red-600'
                  }
                >
                  {stats.performanceSupported ? 'Supported' : 'Not Available'}
                </span>
              </div>
              <div className="flex justify-between">
                <span>Monitoring Status:</span>
                <span
                  className={isMonitoring ? 'text-green-600' : 'text-gray-600'}
                >
                  {isMonitoring ? 'Active' : 'Inactive'}
                </span>
              </div>
            </div>
          </div>

          {/* Control Panel */}
          <div className="rounded-lg bg-white p-6 shadow-lg">
            <h2 className="mb-4 text-xl font-semibold text-gray-800">
              🔧 Testing Controls
            </h2>
            <div className="space-y-3">
              <button
                onClick={testSlowApiCall}
                className="w-full rounded-lg bg-red-600 px-4 py-2 text-white transition-colors hover:bg-red-700"
              >
                🐌 Test Slow API (Match Simulation)
              </button>
              <button
                onClick={testFastApiCall}
                className="w-full rounded-lg bg-green-600 px-4 py-2 text-white transition-colors hover:bg-green-700"
              >
                ⚡ Test Fast API
              </button>
              <button
                onClick={startPerformanceMonitoring}
                disabled={isMonitoring}
                className="w-full rounded-lg bg-blue-600 px-4 py-2 text-white transition-colors hover:bg-blue-700 disabled:opacity-50"
              >
                {isMonitoring
                  ? '🔍 Monitoring Active...'
                  : '🔍 Start Performance Monitoring'}
              </button>
              <button
                onClick={analyzeSlowRequests}
                className="w-full rounded-lg bg-purple-600 px-4 py-2 text-white transition-colors hover:bg-purple-700"
              >
                📊 Analyze Slow Requests
              </button>
              <button
                onClick={checkSignalRPerformance}
                className="w-full rounded-lg bg-orange-600 px-4 py-2 text-white transition-colors hover:bg-orange-700"
              >
                ⚡ Check SignalR Performance
              </button>
            </div>
          </div>
        </div>

        {/* Performance Logs */}
        <div className="mt-6 rounded-lg bg-black p-6 shadow-lg">
          <h2 className="mb-4 text-xl font-semibold text-green-400">
            📋 Performance Logs
          </h2>
          <div className="max-h-96 space-y-1 overflow-y-auto font-mono text-sm text-green-300">
            {logs.length === 0 && (
              <div className="text-gray-500">
                No performance logs yet. Start monitoring or run tests to see
                data.
              </div>
            )}
            {logs.map((log, index) => (
              <div key={index} className="whitespace-pre-wrap">
                {log}
              </div>
            ))}
          </div>
        </div>

        {/* Optimization Tips */}
        <div className="mt-6 rounded-lg bg-blue-50 p-6">
          <h2 className="mb-4 text-xl font-semibold text-blue-800">
            💡 Performance Optimization Tips
          </h2>
          <div className="space-y-2 text-sm text-blue-700">
            <div>
              <strong>For Slow Match Simulations (15+ seconds):</strong>
              <ul className="mt-1 ml-4 list-inside list-disc">
                <li>Implement streaming responses to show progress</li>
                <li>Use SignalR for real-time simulation updates</li>
                <li>Move heavy processing to background jobs</li>
                <li>Cache simulation results when possible</li>
              </ul>
            </div>
            <div>
              <strong>For SignalR Event Timing:</strong>
              <ul className="mt-1 ml-4 list-inside list-disc">
                <li>Ensure SignalR connects before heavy API calls</li>
                <li>Use the Ultra-Aggressive connection strategy</li>
                <li>Monitor event processing times (should be &lt;50ms)</li>
                <li>Avoid blocking operations in event handlers</li>
              </ul>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
