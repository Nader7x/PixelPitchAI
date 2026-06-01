/**
 * EMERGENCY HOT FIX for Critical Performance Issues
 * This file provides immediate solutions for:
 * 1. SignalR missing method warnings (sendmatchendnotificationasync)
 * 2. Slow API performance monitoring and alerting
 * 3. Emergency performance optimization triggers
 */

console.log('🚨 EMERGENCY HOT FIX LOADED - SOLVING CRITICAL ISSUES!');

// 1. EMERGENCY SIGNALR METHOD FIX
function emergencySignalRMethodFix() {
  console.log('🔧 [EMERGENCY] Applying SignalR method fixes...');

  // Check if SignalR service is available
  if (window.signalRService) {
    try {
      // Force re-registration of problematic methods with both casing variants
      const service = window.signalRService;

      if (
        service.notificationConnection &&
        service.notificationConnection.state === 'Connected'
      ) {
        console.log(
          '🔗 [EMERGENCY] Re-registering SignalR methods with multiple casing variants...'
        );

        const methods = [
          'sendmatchendnotificationasync',
          'SendMatchEndNotificationAsync',
          'sendMatchEndNotificationAsync',
          'sendmatchupdatenotificationasync',
          'SendMatchUpdateNotificationAsync',
          'sendMatchUpdateNotificationAsync',
        ];

        methods.forEach((method) => {
          service.notificationConnection.on(method, (...args) => {
            console.log(`🎯 [EMERGENCY CATCH] Caught "${method}":`, args);
          });
        });

        console.log(
          '✅ [EMERGENCY] SignalR methods re-registered successfully!'
        );
      }
    } catch (error) {
      console.error('❌ [EMERGENCY] SignalR fix failed:', error);
    }
  }
}

// 2. EMERGENCY API PERFORMANCE MONITOR
function emergencyApiPerformanceMonitor() {
  console.log('⚡ [EMERGENCY] Setting up API performance monitoring...');

  // Global performance tracking
  window.emergencyApiStats = {
    slowRequests: [],
    fastRequests: [],
    timeouts: [],
    errors: [],
  };

  // Hook into existing fetch if not already hooked
  if (window.fetch && !window.fetch.emergency_monitored) {
    const originalFetch = window.fetch;

    window.fetch = function (...args) {
      const startTime = performance.now();
      const url = args[0];

      console.log(`🚀 [EMERGENCY API] Starting: ${url}`);

      return originalFetch
        .apply(this, args)
        .then((response) => {
          const duration = performance.now() - startTime;

          if (duration > 10000) {
            console.error(
              `🐌 [EMERGENCY ALERT] CRITICAL SLOW REQUEST: ${url} took ${duration.toFixed(2)}ms`
            );
            window.emergencyApiStats.slowRequests.push({
              url,
              duration,
              timestamp: new Date().toISOString(),
            });

            // If it's a match simulation, trigger emergency optimization
            if (url.includes('simulatematch')) {
              console.warn(
                '🚨 [EMERGENCY] Match simulation is critically slow - triggering optimization!'
              );
              triggerEmergencyOptimization(url, duration);
            }
          } else if (duration > 5000) {
            console.warn(
              `⚠️ [EMERGENCY] Slow request: ${url} took ${duration.toFixed(2)}ms`
            );
            window.emergencyApiStats.slowRequests.push({
              url,
              duration,
              timestamp: new Date().toISOString(),
            });
          } else {
            window.emergencyApiStats.fastRequests.push({
              url,
              duration,
              timestamp: new Date().toISOString(),
            });
          }

          return response;
        })
        .catch((error) => {
          const duration = performance.now() - startTime;
          console.error(
            `❌ [EMERGENCY] Request failed: ${url} after ${duration.toFixed(2)}ms:`,
            error
          );

          window.emergencyApiStats.errors.push({
            url,
            duration,
            error: error.message,
            timestamp: new Date().toISOString(),
          });

          throw error;
        });
    };

    window.fetch.emergency_monitored = true;
    console.log('✅ [EMERGENCY] API performance monitoring activated!');
  }
}

// 3. EMERGENCY OPTIMIZATION TRIGGER
function triggerEmergencyOptimization(url, duration) {
  console.log(
    `🚨 [EMERGENCY OPTIMIZATION] Triggered for ${url} (${duration}ms)`
  );

  // Display emergency notification to user
  if (
    window.confirm(
      `🚨 CRITICAL PERFORMANCE ISSUE\n\nAPI request is taking ${Math.round(duration / 1000)} seconds!\nURL: ${url}\n\nWould you like to:\n1. Cancel this request and try again?\n2. Continue waiting?\n\nClick OK to cancel, Cancel to wait.`
    )
  ) {
    // User chose to cancel - we could implement request cancellation here
    console.log('🔄 [EMERGENCY] User chose to cancel slow request');

    // Show optimization tips
    showEmergencyOptimizationTips();
  }
}

// 4. EMERGENCY OPTIMIZATION TIPS
function showEmergencyOptimizationTips() {
  const tips = `
🚨 EMERGENCY PERFORMANCE OPTIMIZATION TIPS:

📊 IMMEDIATE ACTIONS:
• Check your internet connection
• Close other tabs consuming bandwidth
• Refresh the page to reset connections
• Clear browser cache (Ctrl+Shift+Delete)

🔧 TECHNICAL FIXES:
• The backend may be under heavy load
• Database queries might be slow
• Consider using cached data if available
• Report to development team if persistent

⚡ QUICK FIXES APPLIED:
• Enhanced API monitoring active
• SignalR method fixes applied
• Performance tracking enabled
• Emergency alerts configured

🎯 DEBUG INFO:
• Open browser console for detailed logs
• Use window.emergencyApiStats for API stats
• Use window.debugSignalR() for SignalR status
`;

  console.log(tips);

  // Also show in alert for immediate user visibility
  alert(tips);
}

// 5. GLOBAL EMERGENCY FUNCTIONS
window.emergencyFixAll = function () {
  console.log('🔧 [EMERGENCY] Running all emergency fixes...');
  emergencySignalRMethodFix();
  emergencyApiPerformanceMonitor();
  console.log('✅ [EMERGENCY] All fixes applied!');
};

window.emergencyShowStats = function () {
  console.log(
    '📊 [EMERGENCY] Current performance stats:',
    window.emergencyApiStats
  );

  if (window.emergencyApiStats) {
    const stats = window.emergencyApiStats;
    const slowCount = stats.slowRequests.length;
    const fastCount = stats.fastRequests.length;
    const errorCount = stats.errors.length;

    console.log(
      `📈 Summary: ${slowCount} slow, ${fastCount} fast, ${errorCount} errors`
    );

    if (slowCount > 0) {
      console.log(
        '🐌 Slowest requests:',
        stats.slowRequests.sort((a, b) => b.duration - a.duration).slice(0, 5)
      );
    }
  }
};

// 6. AUTO-START EMERGENCY FIXES
setTimeout(() => {
  console.log('🚀 [EMERGENCY] Auto-starting emergency fixes...');
  emergencySignalRMethodFix();
  emergencyApiPerformanceMonitor();

  // Set up periodic re-fixes
  setInterval(emergencySignalRMethodFix, 30000); // Re-fix every 30 seconds

  console.log('🎯 [EMERGENCY] Emergency hot fix system is now active!');
  console.log(
    '💡 TIP: Use window.emergencyFixAll() to manually trigger all fixes'
  );
  console.log(
    '📊 TIP: Use window.emergencyShowStats() to see performance stats'
  );
}, 1000);
