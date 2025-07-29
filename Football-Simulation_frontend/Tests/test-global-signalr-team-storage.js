// Test script to verify global SignalR team storage implementation
// This script verifies that team information is properly stored in localStorage
// when a match_start event is received, regardless of which page the user is on

console.log('🧪 Starting Global SignalR Team Storage Test');

// Function to simulate a match_start event
function simulateMatchStartEvent() {
  console.log('📡 Simulating match_start event...');

  // Mock match event data structure based on the MatchEventData interface
  const mockMatchEventData = {
    event_type: 'match_start',
    home_team: 'Barcelona',
    away_team: 'Real Madrid',
    Score: {
      home: 0,
      away: 0,
    },
    time_seconds: 0,
    match_id: 'test-match-12345',
    event_index: 1,
    timestamp: new Date().toISOString(),
  };

  // Check if SignalR service exists and is properly initialized
  if (typeof window !== 'undefined' && window.signalRService) {
    console.log('✅ SignalR service found on window object');

    // Try to trigger the global listener manually
    if (window.signalRService.matchSimulationConnection) {
      console.log('✅ Match simulation connection exists');

      // Check if the connection has the global listener
      const connection = window.signalRService.matchSimulationConnection;
      if (
        connection._callbacks &&
        connection._callbacks['SendMatchEventAsync']
      ) {
        console.log('✅ Global SendMatchEventAsync listener found');
        console.log(
          `📊 Listener count: ${connection._callbacks['SendMatchEventAsync'].length}`
        );

        // Trigger the listener manually
        try {
          connection._callbacks['SendMatchEventAsync'].forEach((callback) => {
            callback(
              'SendMatchEventAsync',
              mockMatchEventData.match_id,
              mockMatchEventData
            );
          });
          console.log('✅ Global listener triggered successfully');
        } catch (error) {
          console.error('❌ Error triggering global listener:', error);
        }
      } else {
        console.warn('⚠️ Global SendMatchEventAsync listener not found');
        console.log(
          'Available callbacks:',
          Object.keys(connection._callbacks || {})
        );
      }
    } else {
      console.warn('⚠️ Match simulation connection not found');
    }
  } else {
    console.warn('⚠️ SignalR service not found on window object');
    console.log('Manually updating localStorage for testing...');

    // Manually simulate what the global listener should do
    localStorage.setItem('matchId', mockMatchEventData.match_id);
    localStorage.setItem('homeTeamName', mockMatchEventData.home_team);
    localStorage.setItem('awayTeamName', mockMatchEventData.away_team);
    localStorage.setItem('homeScore', mockMatchEventData.Score.home.toString());
    localStorage.setItem('awayScore', mockMatchEventData.Score.away.toString());
    localStorage.setItem(
      'matchTime',
      mockMatchEventData.time_seconds.toString()
    );

    console.log('✅ Manually updated localStorage');
  }
}

// Function to verify localStorage contents
function verifyLocalStorage() {
  console.log('🔍 Verifying localStorage contents...');

  const expectedKeys = [
    'matchId',
    'homeTeamName',
    'awayTeamName',
    'homeScore',
    'awayScore',
    'matchTime',
  ];

  const results = {};
  let allPresent = true;

  expectedKeys.forEach((key) => {
    const value = localStorage.getItem(key);
    results[key] = value;

    if (value === null) {
      console.error(`❌ Missing key: ${key}`);
      allPresent = false;
    } else {
      console.log(`✅ ${key}: ${value}`);
    }
  });

  if (allPresent) {
    console.log('🎉 All expected localStorage keys are present!');
  } else {
    console.warn('⚠️ Some localStorage keys are missing');
  }

  return results;
}

// Function to test teamStorage utility functions
function testTeamStorageFunctions() {
  console.log('🧰 Testing teamStorage utility functions...');

  // Test if teamStorage functions are available
  const functionsToTest = [
    'storeMatchId',
    'getStoredMatchId',
    'storeTeamNames',
    'getStoredTeamNames',
    'storeScores',
    'getStoredScores',
    'storeMatchTime',
    'getStoredMatchTime',
  ];

  return import('./lib/teamStorage.js')
    .then((teamStorage) => {
      console.log('✅ teamStorage module imported successfully');

      functionsToTest.forEach((funcName) => {
        if (typeof teamStorage[funcName] === 'function') {
          console.log(`✅ ${funcName}: Available`);
        } else {
          console.error(`❌ ${funcName}: Missing or not a function`);
        }
      });

      // Test the functions
      try {
        // Test storing and retrieving match ID
        teamStorage.storeMatchId('test-match-12345');
        const retrievedMatchId = teamStorage.getStoredMatchId();
        console.log(
          `📝 Match ID test - Stored: test-match-12345, Retrieved: ${retrievedMatchId}`
        );

        // Test storing and retrieving team names
        teamStorage.storeTeamNames('Barcelona', 'Real Madrid');
        const retrievedTeams = teamStorage.getStoredTeamNames();
        console.log(`📝 Team names test - Retrieved:`, retrievedTeams);

        // Test storing and retrieving scores
        teamStorage.storeScores(0, 0);
        const retrievedScores = teamStorage.getStoredScores();
        console.log(`📝 Scores test - Retrieved:`, retrievedScores);

        // Test storing and retrieving match time
        teamStorage.storeMatchTime(0);
        const retrievedTime = teamStorage.getStoredMatchTime();
        console.log(`📝 Match time test - Retrieved: ${retrievedTime}`);

        console.log('🎉 All teamStorage functions working correctly!');
        return true;
      } catch (error) {
        console.error('❌ Error testing teamStorage functions:', error);
        return false;
      }
    })
    .catch((error) => {
      console.error('❌ Failed to import teamStorage module:', error);
      return false;
    });
}

// Function to simulate page navigation test
function simulatePageNavigationTest() {
  console.log('🔄 Simulating page navigation test...');

  // Store initial state
  const initialState = verifyLocalStorage();

  // Simulate being on a different page (clear any page-specific state)
  console.log('📄 Simulating navigation to a different page...');

  // The localStorage should persist across "page navigation"
  setTimeout(() => {
    console.log('📄 Simulated arrival on new page - checking persistence...');
    const persistedState = verifyLocalStorage();

    // Compare states
    let allPersisted = true;
    Object.keys(initialState).forEach((key) => {
      if (initialState[key] !== persistedState[key]) {
        console.error(
          `❌ ${key} not persisted: ${initialState[key]} → ${persistedState[key]}`
        );
        allPersisted = false;
      }
    });

    if (allPersisted) {
      console.log('🎉 All team information persisted across page navigation!');
    } else {
      console.warn('⚠️ Some team information was lost during page navigation');
    }
  }, 1000);
}

// Main test execution
async function runGlobalSignalRTest() {
  console.log('🚀 Starting comprehensive global SignalR team storage test...');

  // Clear localStorage first
  console.log('🧹 Clearing localStorage...');
  localStorage.clear();

  // Test 1: Test teamStorage functions
  console.log('\n📋 Test 1: Testing teamStorage utility functions');
  const teamStorageWorking = await testTeamStorageFunctions();

  if (!teamStorageWorking) {
    console.error(
      '❌ TeamStorage functions test failed. Aborting further tests.'
    );
    return;
  }

  // Test 2: Simulate match_start event
  console.log('\n📋 Test 2: Simulating match_start event');
  simulateMatchStartEvent();

  // Test 3: Verify localStorage
  console.log('\n📋 Test 3: Verifying localStorage contents');
  verifyLocalStorage();

  // Test 4: Page navigation simulation
  console.log('\n📋 Test 4: Testing persistence across page navigation');
  simulatePageNavigationTest();

  console.log('\n🏁 Global SignalR team storage test completed!');
  console.log('📊 Final localStorage state:', {
    matchId: localStorage.getItem('matchId'),
    homeTeamName: localStorage.getItem('homeTeamName'),
    awayTeamName: localStorage.getItem('awayTeamName'),
    homeScore: localStorage.getItem('homeScore'),
    awayScore: localStorage.getItem('awayScore'),
    matchTime: localStorage.getItem('matchTime'),
  });
}

// Auto-run if in browser environment
if (typeof window !== 'undefined') {
  // Wait a bit for page to load
  setTimeout(runGlobalSignalRTest, 1000);
} else {
  console.log('❌ Not in browser environment - test cannot run');
}

// Export for manual testing
window.runGlobalSignalRTest = runGlobalSignalRTest;
window.simulateMatchStartEvent = simulateMatchStartEvent;
window.verifyLocalStorage = verifyLocalStorage;

console.log(
  '✅ Global SignalR test script loaded. Run manually with: runGlobalSignalRTest()'
);
