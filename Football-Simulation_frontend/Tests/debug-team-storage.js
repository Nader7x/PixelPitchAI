// Debug script to troubleshoot team name storage issues
// Run this in the browser console to debug the global SignalR listener

console.log('🔍 SignalR Team Storage Debug Script Starting...');

// Function to check current localStorage state
function checkLocalStorageState() {
  console.log('📊 Current localStorage state:');
  const keys = [
    'matchId',
    'homeTeamName',
    'awayTeamName',
    'homeScore',
    'awayScore',
    'matchTime',
  ];
  const state = {};

  keys.forEach((key) => {
    const value = localStorage.getItem(key);
    state[key] = value;
    console.log(`  ${key}: ${value}`);
  });

  return state;
}

// Function to manually test teamStorage functions
async function testTeamStorageFunctions() {
  console.log('🧪 Testing teamStorage functions...');

  try {
    // Import teamStorage
    const teamStorage = await import('./lib/teamStorage.js');
    console.log('✅ teamStorage imported successfully');

    // Test storeMatchId
    teamStorage.storeMatchId('debug-test-match-123');
    console.log('✅ storeMatchId test completed');

    // Test storeTeamNames
    teamStorage.storeTeamNames('Debug Home Team', 'Debug Away Team');
    console.log('✅ storeTeamNames test completed');

    // Test storeScores
    teamStorage.storeScores(1, 2);
    console.log('✅ storeScores test completed');

    // Test storeMatchTime
    teamStorage.storeMatchTime(300);
    console.log('✅ storeMatchTime test completed');

    console.log('📊 localStorage after manual tests:');
    checkLocalStorageState();

    return true;
  } catch (error) {
    console.error('❌ Error testing teamStorage functions:', error);
    return false;
  }
}

// Function to check SignalR connection and listeners
function checkSignalRState() {
  console.log('🔗 Checking SignalR connection state...');

  if (typeof window === 'undefined' || !window.signalRService) {
    console.error('❌ SignalR service not found on window object');
    return false;
  }

  const service = window.signalRService;
  console.log('✅ SignalR service found');

  // Check connection states
  console.log('📡 Connection states:');
  console.log(
    `  Match simulation: ${service.getMatchSimulationConnectionState()}`
  );
  console.log(`  Notification: ${service.getNotificationConnectionState()}`);

  // Check if match simulation connection exists
  if (service.matchSimulationConnection) {
    console.log('✅ Match simulation connection exists');

    // Check if global listener is attached
    const connection = service.matchSimulationConnection;
    if (connection._callbacks && connection._callbacks['SendMatchEventAsync']) {
      const listenerCount = connection._callbacks['SendMatchEventAsync'].length;
      console.log(`✅ SendMatchEventAsync listeners found: ${listenerCount}`);

      // Check if our global listener is there
      if (service.globalMatchEventHandler) {
        console.log('✅ Global match event handler reference exists');

        // Check if it's actually in the listeners array
        const isAttached = connection._callbacks[
          'SendMatchEventAsync'
        ].includes(service.globalMatchEventHandler);
        console.log(`📋 Global handler attached: ${isAttached}`);
      } else {
        console.warn('⚠️ Global match event handler reference missing');
      }
    } else {
      console.warn('⚠️ No SendMatchEventAsync listeners found');
    }
  } else {
    console.error('❌ Match simulation connection not found');
    return false;
  }

  return true;
}

// Function to manually trigger a test match_start event
function simulateMatchStartEvent() {
  console.log('🎭 Simulating match_start event...');

  if (typeof window === 'undefined' || !window.signalRService) {
    console.error('❌ SignalR service not available');
    return;
  }

  const service = window.signalRService;

  if (!service.matchSimulationConnection || !service.globalMatchEventHandler) {
    console.error('❌ SignalR connection or global handler not available');
    return;
  }

  // Create a mock match_start event
  const mockEvent = {
    event_type: 'match_start',
    home_team: 'Test Home Team',
    away_team: 'Test Away Team',
    Score: {
      home: 0,
      away: 0,
    },
    time_seconds: 0,
    match_id: 'test-match-123',
    timestamp: new Date().toISOString(),
    event_index: 1,
  };

  console.log('📤 Triggering global handler with mock event:', mockEvent);

  try {
    // Call the global handler directly
    service.globalMatchEventHandler(
      'SendMatchEventAsync',
      mockEvent.match_id,
      mockEvent
    );
    console.log('✅ Global handler triggered successfully');

    // Check localStorage after a short delay
    setTimeout(() => {
      console.log('📊 localStorage after simulated event:');
      checkLocalStorageState();
    }, 1000);
  } catch (error) {
    console.error('❌ Error triggering global handler:', error);
  }
}

// Function to listen for real SignalR events
function startRealEventMonitoring() {
  console.log('👂 Starting real-time SignalR event monitoring...');

  if (typeof window === 'undefined' || !window.signalRService) {
    console.error('❌ SignalR service not available');
    return;
  }

  const service = window.signalRService;

  if (!service.matchSimulationConnection) {
    console.error('❌ Match simulation connection not available');
    return;
  }

  // Add a temporary debug listener
  const debugListener = (method, match_id, eventData) => {
    console.log('🎯 [DEBUG] Real SignalR event received:');
    console.log(`  Method: ${method}`);
    console.log(`  Match ID: ${match_id}`);
    console.log(`  Event Type: ${eventData?.event_type}`);
    console.log(`  Event Data:`, eventData);

    if (eventData?.event_type === 'match_start') {
      console.log('🎉 [DEBUG] MATCH_START EVENT DETECTED!');
      console.log(`  Home Team: ${eventData.home_team}`);
      console.log(`  Away Team: ${eventData.away_team}`);
    }
  };

  service.matchSimulationConnection.on('SendMatchEventAsync', debugListener);
  console.log('✅ Debug listener attached to SendMatchEventAsync');

  // Return a function to remove the debug listener
  return () => {
    if (service.matchSimulationConnection) {
      service.matchSimulationConnection.off(
        'SendMatchEventAsync',
        debugListener
      );
      console.log('🧹 Debug listener removed');
    }
  };
}

// Main debug function
async function runFullDiagnosis() {
  console.log('🚀 Running full SignalR team storage diagnosis...');
  console.log('='.repeat(60));

  // Step 1: Check initial localStorage state
  console.log('\n📋 Step 1: Initial localStorage state');
  const initialState = checkLocalStorageState();

  // Step 2: Test teamStorage functions
  console.log('\n📋 Step 2: Testing teamStorage functions');
  const teamStorageWorking = await testTeamStorageFunctions();

  if (!teamStorageWorking) {
    console.error('❌ TeamStorage functions failed. Stopping diagnosis.');
    return;
  }

  // Step 3: Check SignalR state
  console.log('\n📋 Step 3: Checking SignalR connection and listeners');
  const signalRWorking = checkSignalRState();

  if (!signalRWorking) {
    console.error('❌ SignalR not properly configured. Stopping diagnosis.');
    return;
  }

  // Step 4: Simulate match_start event
  console.log('\n📋 Step 4: Simulating match_start event');
  simulateMatchStartEvent();

  // Step 5: Start real event monitoring
  console.log('\n📋 Step 5: Starting real-time event monitoring');
  const stopMonitoring = startRealEventMonitoring();

  console.log('\n✅ Full diagnosis completed!');
  console.log('🔍 Monitor the console for real-time events...');
  console.log('🛑 To stop monitoring, run: stopEventMonitoring()');

  // Make stop function globally available
  window.stopEventMonitoring = stopMonitoring;

  return {
    initialState,
    teamStorageWorking,
    signalRWorking,
    stopMonitoring,
  };
}

// Export functions to global scope for manual testing
window.checkLocalStorageState = checkLocalStorageState;
window.testTeamStorageFunctions = testTeamStorageFunctions;
window.checkSignalRState = checkSignalRState;
window.simulateMatchStartEvent = simulateMatchStartEvent;
window.startRealEventMonitoring = startRealEventMonitoring;
window.runFullDiagnosis = runFullDiagnosis;

// Auto-run diagnosis
console.log('🎯 Global functions available:');
console.log('  - checkLocalStorageState()');
console.log('  - testTeamStorageFunctions()');
console.log('  - checkSignalRState()');
console.log('  - simulateMatchStartEvent()');
console.log('  - startRealEventMonitoring()');
console.log('  - runFullDiagnosis()');
console.log('\n🚀 Running automatic diagnosis in 2 seconds...');

setTimeout(() => {
  runFullDiagnosis();
}, 2000);
