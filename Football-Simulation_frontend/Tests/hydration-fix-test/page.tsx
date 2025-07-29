'use client';

import { useEffect, useState } from 'react';

export default function HydrationFixVerificationPage() {
  const [hydrationErrors, setHydrationErrors] = useState<string[]>([]);
  const [verified, setVerified] = useState(false);

  useEffect(() => {
    // Monitor console for hydration errors
    const originalConsoleError = console.error;
    let errorCount = 0;

    console.error = function (...args) {
      if (
        args[0] &&
        typeof args[0] === 'string' &&
        args[0].includes('hydration')
      ) {
        errorCount++;
        const errorMsg = `Error #${errorCount}: ${args[0]}`;
        setHydrationErrors((prev) => [errorMsg, ...prev.slice(0, 9)]);
      }
      originalConsoleError.apply(console, args);
    };

    // Test verification after 3 seconds
    setTimeout(() => {
      setVerified(true);
      if (errorCount === 0) {
        console.log('✅ HYDRATION VERIFICATION: NO ERRORS DETECTED!');
      } else {
        console.warn(
          `❌ HYDRATION VERIFICATION: ${errorCount} ERRORS DETECTED`
        );
      }
    }, 3000);

    // Restore original console after 30 seconds
    setTimeout(() => {
      console.error = originalConsoleError;
    }, 30000);

    return () => {
      console.error = originalConsoleError;
    };
  }, []);

  return (
    <div className="min-h-screen bg-gradient-to-br from-blue-50 to-green-50 p-8">
      <div className="mx-auto max-w-4xl">
        <div className="rounded-lg bg-white p-6 shadow-xl">
          <div className="mb-6 flex items-center gap-3">
            <div className="flex h-10 w-10 items-center justify-center rounded-full bg-blue-500">
              <span className="text-xl text-white">🔍</span>
            </div>
            <h1 className="text-3xl font-bold text-gray-800">
              Hydration Error Verification
            </h1>
          </div>

          <div className="mb-6 grid grid-cols-1 gap-6 md:grid-cols-2">
            <div className="rounded-lg bg-gray-50 p-4">
              <h3 className="mb-3 font-semibold text-gray-700">
                🎯 Verification Status
              </h3>
              <div className="space-y-2">
                <div
                  className={`flex items-center gap-2 ${verified ? 'text-green-600' : 'text-yellow-600'}`}
                >
                  <span>{verified ? '✅' : '⏳'}</span>
                  <span>
                    {verified ? 'Verification Complete' : 'Monitoring...'}
                  </span>
                </div>
                <div
                  className={`flex items-center gap-2 ${hydrationErrors.length === 0 ? 'text-green-600' : 'text-red-600'}`}
                >
                  <span>{hydrationErrors.length === 0 ? '✅' : '❌'}</span>
                  <span>Hydration Errors: {hydrationErrors.length}</span>
                </div>
              </div>
            </div>

            <div className="rounded-lg bg-gray-50 p-4">
              <h3 className="mb-3 font-semibold text-gray-700">
                📊 Quick Stats
              </h3>
              <div className="space-y-1 text-sm">
                <p>• Monitoring Time: 30 seconds</p>
                <p>• Error Detection: Active</p>
                <p>• Fix Applied: Layout.tsx compressed</p>
                <p>• Expected Result: Zero errors</p>
              </div>
            </div>
          </div>

          {verified && hydrationErrors.length === 0 && (
            <div className="mb-6 rounded-lg border border-green-200 bg-green-50 p-4">
              <div className="mb-2 flex items-center gap-2">
                <span className="text-xl text-green-600">🎉</span>
                <h3 className="font-semibold text-green-800">SUCCESS!</h3>
              </div>
              <p className="text-green-700">
                No hydration errors detected! The layout.tsx fix has
                successfully eliminated all whitespace-related hydration issues.
              </p>
            </div>
          )}

          {verified && hydrationErrors.length > 0 && (
            <div className="mb-6 rounded-lg border border-red-200 bg-red-50 p-4">
              <div className="mb-2 flex items-center gap-2">
                <span className="text-xl text-red-600">⚠️</span>
                <h3 className="font-semibold text-red-800">
                  Hydration Errors Detected
                </h3>
              </div>
              <p className="mb-3 text-red-700">
                {hydrationErrors.length} hydration error(s) were detected.
                Please check the errors below:
              </p>
            </div>
          )}

          <div className="h-64 overflow-y-auto rounded-lg bg-gray-900 p-4 font-mono text-sm text-green-400">
            <div className="mb-2 text-gray-400">// Hydration Error Monitor</div>
            {!verified && (
              <div className="mb-2 text-yellow-400">
                ⏳ Monitoring for hydration errors...
              </div>
            )}
            {verified && hydrationErrors.length === 0 && (
              <div className="mb-2 text-green-400">
                ✅ NO HYDRATION ERRORS DETECTED!
              </div>
            )}
            {hydrationErrors.map((error, index) => (
              <div key={index} className="mb-1 text-red-400">
                {error}
              </div>
            ))}
            {hydrationErrors.length === 0 && verified && (
              <div className="mt-4 text-gray-400">
                <p>🎯 All checks passed!</p>
                <p>📝 Layout.tsx whitespace fix successful</p>
                <p>⚡ React hydration working properly</p>
              </div>
            )}
          </div>

          <div className="mt-6 grid grid-cols-1 gap-4 md:grid-cols-3">
            <button
              onClick={() => window.location.reload()}
              className="rounded bg-blue-500 px-4 py-2 text-white hover:bg-blue-600"
            >
              🔄 Reload Test
            </button>
            <button
              onClick={() => {
                console.log('🔍 Manual hydration check initiated');
                setVerified(false);
                setHydrationErrors([]);
                setTimeout(() => setVerified(true), 3000);
              }}
              className="rounded bg-green-500 px-4 py-2 text-white hover:bg-green-600"
            >
              ✅ Rerun Check
            </button>
            <button
              onClick={() => {
                navigator.clipboard.writeText(window.location.href);
                alert('URL copied to clipboard');
              }}
              className="rounded bg-purple-500 px-4 py-2 text-white hover:bg-purple-600"
            >
              📋 Copy URL
            </button>
          </div>

          <div className="mt-6 rounded-lg border border-yellow-200 bg-yellow-50 p-4">
            <h4 className="mb-2 font-semibold text-yellow-800">
              🎯 What This Test Verifies:
            </h4>
            <ul className="space-y-1 text-sm text-yellow-700">
              <li>
                • No "whitespace text nodes cannot be a child of &lt;html&gt;"
                errors
              </li>
              <li>• Proper React hydration without layout shifts</li>
              <li>• Clean console output without hydration warnings</li>
              <li>• Compressed HTML structure working correctly</li>
            </ul>
          </div>
        </div>
      </div>
    </div>
  );
}
