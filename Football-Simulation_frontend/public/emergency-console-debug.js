// IMMEDIATE DEBUG AND FIX SCRIPT - Paste this directly into browser console

console.log('🚨⚡ EMERGENCY DEBUG AND FIX SCRIPT STARTING...');

// 1. Fix SignalR method case sensitivity issues
function fixSignalRMethodCasing() {
  console.log('🔧 Fixing SignalR method casing issues...');

  if (window.signalRService && window.signalRService.notificationConnection) {
    const connection = window.signalRService.notificationConnection;

    // Register lowercase handlers for common methods
    const methodsToFix = [
      'sendmatchendnotificationasync',
      'sendmatchupdatenotificationasync',
      'sendmatchstartnotificationasync',
      'sendnotificationasync',
      'sendsimulationupdatenotificationasync',
    ];

    methodsToFix.forEach((method) => {
      try {
        connection.on(method, (...args) => {
          console.log(`🎯 [SignalR] Received ${method}:`, args);

          // Try to find and call the PascalCase version
          const pascalMethod = method.charAt(0).toUpperCase() + method.slice(1);
          if (connection.callbacks && connection.callbacks[pascalMethod]) {
            connection.callbacks[pascalMethod].forEach((callback) =>
              callback(...args)
            );
          }
        });
        console.log(`✅ Registered handler for ${method}`);
      } catch (error) {
        console.warn(`❌ Failed to register ${method}:`, error);
      }
    });
  } else {
    console.warn('❌ SignalR service not found or not connected');
  }
}

// 2. Monitor and optimize slow API calls
function optimizeApiCalls() {
  console.log('⚡ Setting up API call optimization...');

  if (!window.originalFetch) {
    window.originalFetch = window.fetch;
  }

  window.fetch = function (...args) {
    const url = args[0];
    const startTime = performance.now();

    // Special handling for slow endpoints
    if (url.includes('simulatematch')) {
      console.log(
        '🎮 [API] Match simulation started - monitoring performance...'
      );

      return window.originalFetch
        .apply(this, args)
        .then(async (response) => {
          const duration = performance.now() - startTime;
          console.log(
            `🎮 [API] Match simulation completed in ${duration.toFixed(2)}ms`
          );

          if (duration > 10000) {
            console.warn(
              `🐌 [API] SLOW MATCH SIMULATION: ${duration.toFixed(2)}ms - Consider backend optimization`
            );
          }

          return response;
        })
        .catch((error) => {
          const duration = performance.now() - startTime;
          console.error(
            `❌ [API] Match simulation failed after ${duration.toFixed(2)}ms:`,
            error
          );
          throw error;
        });
    }

    return window.originalFetch.apply(this, args);
  };

  console.log('✅ API optimization monitoring active');
}

// 3. Emergency SignalR reconnection
function emergencySignalRReconnect() {
  console.log('🚑 Emergency SignalR reconnection...');

  if (window.signalRService) {
    try {
      window.signalRService.disconnect();
      setTimeout(() => {
        window.signalRService.connect();
        console.log('✅ Emergency SignalR reconnection completed');
      }, 1000);
    } catch (error) {
      console.error('❌ Emergency reconnection failed:', error);
    }
  }
}

// 4. Performance monitoring
function showPerformanceStats() {
  console.log('📊 PERFORMANCE STATS:');
  console.log('====================');

  // Memory usage
  if (performance.memory) {
    console.log('🧠 Memory Usage:');
    console.log(
      `  Used: ${(performance.memory.usedJSHeapSize / 1024 / 1024).toFixed(2)} MB`
    );
    console.log(
      `  Total: ${(performance.memory.totalJSHeapSize / 1024 / 1024).toFixed(2)} MB`
    );
    console.log(
      `  Limit: ${(performance.memory.jsHeapSizeLimit / 1024 / 1024).toFixed(2)} MB`
    );
  }

  // Connection status
  if (window.signalRService) {
    console.log('🔗 SignalR Status:');
    console.log(
      `  Match Simulation: ${window.signalRService.isMatchSimulationConnected ? '✅ Connected' : '❌ Disconnected'}`
    );
    console.log(
      `  Notifications: ${window.signalRService.isNotificationConnected ? '✅ Connected' : '❌ Disconnected'}`
    );
  }

  // Navigation timing
  if (performance.timing) {
    const loadTime =
      performance.timing.loadEventEnd - performance.timing.navigationStart;
    console.log(`⚡ Page Load Time: ${loadTime}ms`);
  }
}

// 5. Test API performance
async function testApiPerformance() {
  console.log('🧪 Testing API performance...');

  const testEndpoints = ['/api/matches', '/api/teams', '/api/players'];

  for (const endpoint of testEndpoints) {
    try {
      const startTime = performance.now();
      const response = await fetch(endpoint);
      const duration = performance.now() - startTime;

      console.log(
        `${duration > 2000 ? '🐌' : '⚡'} ${endpoint}: ${duration.toFixed(2)}ms`
      );
    } catch (error) {
      console.error(`❌ ${endpoint}: Failed`, error);
    }
  }
}

// Run all fixes immediately
console.log('🚀 Applying all emergency fixes...');

fixSignalRMethodCasing();
optimizeApiCalls();

// Make functions available globally
window.emergencySignalRReconnect = emergencySignalRReconnect;
window.showPerformanceStats = showPerformanceStats;
window.testApiPerformance = testApiPerformance;
window.fixSignalRMethodCasing = fixSignalRMethodCasing;

console.log('✅ Emergency fixes applied! Available commands:');
console.log('  • window.emergencySignalRReconnect() - Reconnect SignalR');
console.log('  • window.showPerformanceStats() - Show performance data');
console.log('  • window.testApiPerformance() - Test API endpoints');
console.log('  • window.fixSignalRMethodCasing() - Re-apply SignalR fixes');

// Auto-run performance stats in 5 seconds
setTimeout(() => {
  console.log('📊 Auto-showing performance stats...');
  showPerformanceStats();
}, 5000);
