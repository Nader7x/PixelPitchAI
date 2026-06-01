'use client';

import { useEffect, useState } from 'react';

export default function EmergencyTestPage() {
  const [status, setStatus] = useState<string[]>([]);
  const [fixesApplied, setFixesApplied] = useState<any>({});

  const addStatus = (message: string) => {
    setStatus((prev) => [
      `[${new Date().toLocaleTimeString()}] ${message}`,
      ...prev.slice(0, 19),
    ]);
  };

  useEffect(() => {
    addStatus('🚨 EMERGENCY TEST PAGE LOADED');

    // Check if emergency fixes are loaded
    const checkEmergencyFixes = () => {
      const fixes = {
        ultraAggressiveInterceptor:
          typeof (window as any).signalRService !== 'undefined',
        emergencyHotFix:
          typeof (window as any).emergencySignalRFix !== 'undefined',
        emergencyApiOptimizer:
          typeof (window as any).emergencyApiOptimizer !== 'undefined',
        emergencyPerformanceStats:
          typeof (window as any).emergencyShowStats !== 'undefined',
      };

      setFixesApplied(fixes);

      Object.entries(fixes).forEach(([fix, loaded]) => {
        addStatus(
          `${loaded ? '✅' : '❌'} ${fix}: ${loaded ? 'LOADED' : 'MISSING'}`
        );
      });
    };

    // Check immediately and after a delay
    checkEmergencyFixes();
    setTimeout(checkEmergencyFixes, 2000);

    // Test API performance
    const testApiPerformance = async () => {
      addStatus('🧪 Testing API performance...');

      try {
        if ((window as any).emergencyApiOptimizer) {
          const testResult = await (window as any).emergencyApiOptimizer(
            '/api/matches',
            { method: 'GET' }
          );
          addStatus(
            `⚡ API test completed: ${JSON.stringify(testResult).substring(0, 50)}...`
          );
        }
      } catch (error) {
        addStatus(`❌ API test failed: ${error}`);
      }
    };

    setTimeout(testApiPerformance, 3000);
  }, []);

  const runEmergencyFix = () => {
    addStatus('🔧 Running emergency SignalR fix...');
    if ((window as any).emergencySignalRFix) {
      (window as any).emergencySignalRFix();
      addStatus('✅ Emergency SignalR fix executed');
    } else {
      addStatus('❌ Emergency SignalR fix not available');
    }
  };

  const showPerformanceStats = () => {
    addStatus('📊 Showing performance stats...');
    if ((window as any).emergencyShowStats) {
      (window as any).emergencyShowStats();
      addStatus('✅ Performance stats displayed in console');
    } else {
      addStatus('❌ Performance stats not available');
    }
  };

  const testSlowApiCall = async () => {
    addStatus('🐌 Testing slow API call simulation...');
    try {
      const testMatchId = 'ce91b630-5b0e-4407-8a5a-234699af0f45';

      // Use emergency API optimizer if available
      if ((window as any).emergencyApiOptimizer) {
        addStatus('⚡ Using emergency API optimizer...');
        const result = await (window as any).emergencyApiOptimizer(
          `/api/matches/simulatematch/${testMatchId}`,
          { method: 'POST' },
          10000 // 10 second timeout
        );
        addStatus('✅ Fast API call completed successfully');
      } else {
        addStatus('❌ Emergency API optimizer not available');
      }
    } catch (error) {
      addStatus(`❌ Slow API test failed: ${error}`);
    }
  };

  return (
    <div className="min-h-screen bg-gradient-to-br from-red-50 to-orange-50 p-6">
      <div className="mx-auto max-w-4xl">
        <div className="rounded-xl border-l-4 border-red-500 bg-white p-6 shadow-2xl">
          <div className="mb-6 flex items-center gap-3">
            <div className="flex h-8 w-8 items-center justify-center rounded-full bg-red-500">
              <span className="font-bold text-white">🚨</span>
            </div>
            <h1 className="text-2xl font-bold text-gray-800">
              Emergency Fixes Test Dashboard
            </h1>
          </div>

          <div className="mb-6 grid grid-cols-1 gap-6 md:grid-cols-2">
            <div className="rounded-lg bg-gray-50 p-4">
              <h3 className="mb-3 font-semibold text-gray-700">
                🔧 Emergency Controls
              </h3>
              <div className="space-y-2">
                <button
                  onClick={runEmergencyFix}
                  className="w-full rounded bg-red-500 px-4 py-2 text-white hover:bg-red-600"
                >
                  🚑 Emergency SignalR Fix
                </button>
                <button
                  onClick={showPerformanceStats}
                  className="w-full rounded bg-blue-500 px-4 py-2 text-white hover:bg-blue-600"
                >
                  📊 Show Performance Stats
                </button>
                <button
                  onClick={testSlowApiCall}
                  className="w-full rounded bg-orange-500 px-4 py-2 text-white hover:bg-orange-600"
                >
                  🧪 Test API Optimization
                </button>
              </div>
            </div>

            <div className="rounded-lg bg-gray-50 p-4">
              <h3 className="mb-3 font-semibold text-gray-700">
                ✅ Fix Status
              </h3>
              <div className="space-y-1 text-sm">
                {Object.entries(fixesApplied).map(([fix, loaded]) => (
                  <div
                    key={fix}
                    className={`flex items-center gap-2 ${loaded ? 'text-green-600' : 'text-red-600'}`}
                  >
                    <span>{loaded ? '✅' : '❌'}</span>
                    <span>{fix}</span>
                  </div>
                ))}
              </div>
            </div>
          </div>

          <div className="h-80 overflow-y-auto rounded-lg bg-gray-900 p-4 font-mono text-sm text-green-400">
            <div className="mb-2 text-gray-400">// Emergency Test Console</div>
            {status.map((line, index) => (
              <div key={index} className="mb-1">
                {line}
              </div>
            ))}
          </div>

          <div className="mt-4 rounded-lg border border-yellow-200 bg-yellow-50 p-4">
            <h4 className="mb-2 font-semibold text-yellow-800">
              🎯 Issues Being Fixed:
            </h4>
            <ul className="space-y-1 text-sm text-yellow-700">
              <li>
                • <strong>SignalR Method Warning:</strong> Missing
                'sendmatchendnotificationasync' method
              </li>
              <li>
                • <strong>Slow API Calls:</strong> 15+ second match simulation
                requests
              </li>
              <li>
                • <strong>Hydration Errors:</strong> Whitespace in HTML head
                causing React errors
              </li>
              <li>
                • <strong>Event Interceptor Timeouts:</strong> Ultra-aggressive
                monitoring causing issues
              </li>
            </ul>
          </div>
        </div>
      </div>
    </div>
  );
}
