// ULTRA-AGGRESSIVE Event Interceptor with Performance Monitoring
// This runs in the browser and hooks directly into SignalR AND fetch to monitor everything

console.log(
  '🚀⚡ ULTRA-AGGRESSIVE EVENT INTERCEPTOR WITH PERFORMANCE MONITORING LOADED!'
);

// Monitor ALL fetch requests for performance
function hookIntoFetch() {
  if (typeof window !== 'undefined' && window.fetch) {
    const originalFetch = window.fetch;

    window.fetch = function (...args) {
      const startTime = performance.now();
      const url = args[0];

      console.log(`🚀 [FETCH] Starting request: ${url}`);

      return originalFetch
        .apply(this, args)
        .then((response) => {
          const duration = performance.now() - startTime;
          const speed = duration > 5000 ? '🐌 SLOW' : '⚡ FAST';

          console.log(
            `${speed} [FETCH] ${url} completed in ${duration.toFixed(2)}ms`
          );

          if (duration > 5000) {
            console.warn(
              `🚨 [FETCH] SLOW REQUEST DETECTED: ${url} took ${duration.toFixed(2)}ms`
            );
            if (url.includes('simulatematch')) {
              console.warn(
                `🎮 [FETCH] Match simulation is slow - this could delay SignalR events!`
              );
            }
          }

          return response;
        })
        .catch((error) => {
          const duration = performance.now() - startTime;
          console.error(
            `❌ [FETCH] ${url} failed after ${duration.toFixed(2)}ms:`,
            error
          );
          throw error;
        });
    };

    console.log('🔗 [MONITOR] Fetch API hooked for performance monitoring');
  }
}

// Function to monitor SignalR for instant event capture (IMPROVED)
function setupUltraAggressiveEventMonitoring() {
  console.log('🔥 Setting up IMPROVED ULTRA-AGGRESSIVE event monitoring...');

  let monitoringActive = false;

  // Check every 500ms for SignalR connection (less aggressive to avoid timeout)
  const checkForSignalR = setInterval(() => {
    if (
      window.signalRService &&
      window.signalRService.matchSimulationConnection &&
      !monitoringActive
    ) {
      console.log('⚡ SIGNALR DETECTED - HOOKING INTO EVENTS!');

      try {
        const connection = window.signalRService.matchSimulationConnection;

        // Check if connection is actually ready
        if (connection.state === 'Connected' || connection.state === 1) {
          const originalOn = connection.on;

          // Intercept ALL incoming events
          connection.on = function (methodName, callback) {
            console.log(`🎯 HOOKED INTO EVENT: ${methodName}`);

            // Wrap the callback to log immediately
            const wrappedCallback = function (...args) {
              const eventStartTime = performance.now();
              console.log(`⚡🚨 INSTANT EVENT CAPTURED: ${methodName}`, args);

              // If it's a match event, log it with HIGH PRIORITY
              if (methodName === 'SendMatchEventAsync') {
                console.log('🏆🚨 MATCH EVENT INTERCEPTED INSTANTLY!', args);

                if (args[2] && args[2].event_type === 'match_start') {
                  console.log('🎉🚨 MATCH_START EVENT CAUGHT AT LIGHT SPEED!');
                  console.log('⚡ Event data:', args[2]);
                }
              }

              // Call the original callback
              const result = callback.apply(this, args);

              // Monitor event processing time
              const processingTime = performance.now() - eventStartTime;
              const speed =
                processingTime > 100 ? '🐌' : processingTime > 50 ? '⚠️' : '⚡';
              console.log(
                `${speed} [EVENT] ${methodName} processed in ${processingTime.toFixed(2)}ms`
              );

              return result;
            };

            // Call the original on method with our wrapped callback
            return originalOn.call(this, methodName, wrappedCallback);
          };

          monitoringActive = true;
          console.log('🔥✅ ULTRA-AGGRESSIVE MONITORING ACTIVE!');
          clearInterval(checkForSignalR);
        } else {
          console.log('⏳ SignalR found but not connected yet, waiting...');
        }
      } catch (error) {
        console.error('❌ Error setting up monitoring:', error);
      }
    }
  }, 500); // Reduced frequency to avoid timeout

  // Stop checking after 60 seconds (increased timeout)
  setTimeout(() => {
    if (!monitoringActive) {
      console.log(
        '⏰ Event monitoring setup timeout - SignalR may not be available'
      );
    }
    clearInterval(checkForSignalR);
  }, 60000);
}

// Hook into fetch immediately
hookIntoFetch();

// Start SignalR monitoring immediately
setupUltraAggressiveEventMonitoring();

// Also setup on DOM ready
if (document.readyState === 'loading') {
  document.addEventListener('DOMContentLoaded', () => {
    hookIntoFetch();
    setupUltraAggressiveEventMonitoring();
  });
} else {
  hookIntoFetch();
  setupUltraAggressiveEventMonitoring();
}

// And setup on window load
window.addEventListener('load', () => {
  hookIntoFetch();
  setupUltraAggressiveEventMonitoring();
});

console.log(
  '🚀 ULTRA-AGGRESSIVE EVENT INTERCEPTOR WITH PERFORMANCE MONITORING READY!'
);
