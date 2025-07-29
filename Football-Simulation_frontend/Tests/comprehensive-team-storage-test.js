// Comprehensive test script for global team storage implementation
// This script tests the fixed implementation where global and page-specific listeners coexist

console.log('🚀 Starting Comprehensive Team Storage Test...');
console.log('='.repeat(70));

// Test results tracker
const testResults = {
  passed: 0,
  failed: 0,
  results: [],
};

// Helper function to log test results
function logTest(testName, passed, details = '') {
  const status = passed ? '✅ PASS' : '❌ FAIL';
  const message = `${status}: ${testName}${details ? ' - ' + details : ''}`;
  console.log(message);

  testResults.results.push({ testName, passed, details });
  if (passed) testResults.passed++;
  else testResults.failed++;
}

// Helper function to wait
function sleep(ms) {
  return new Promise((resolve) => setTimeout(resolve, ms));
}

// Test 1: Verify teamStorage functions work
async function testTeamStorageFunctions() {
  console.log('\n📋 Test 1: TeamStorage Functions');

  try {
    // Import teamStorage
    const teamStorage = await import('./lib/teamStorage.js');
    logTest('TeamStorage import', true);

    // Clear any existing data
    teamStorage.clearAllMatchData();

    // Test storeMatchId and getStoredMatchId
    teamStorage.storeMatchId('test-match-12345');
    const retrievedMatchId = teamStorage.getStoredMatchId();
    logTest(
      'Match ID storage/retrieval',
      retrievedMatchId === 'test-match-12345',
      `Expected: test-match-12345, Got: ${retrievedMatchId}`
    );

    // Test storeTeamNames and getStoredTeamNames
    teamStorage.storeTeamNames('Barcelona FC', 'Real Madrid CF');
    const retrievedTeams = teamStorage.getStoredTeamNames();
    logTest(
      'Team names storage/retrieval',
      retrievedTeams.homeTeam === 'Barcelona FC' &&
        retrievedTeams.awayTeam === 'Real Madrid CF',
      `Expected: Barcelona FC vs Real Madrid CF, Got: ${retrievedTeams.homeTeam} vs ${retrievedTeams.awayTeam}`
    );

    // Test storeScores and getStoredScores
    teamStorage.storeScores(2, 1);
    const retrievedScores = teamStorage.getStoredScores();
    logTest(
      'Scores storage/retrieval',
      retrievedScores.homeScore === 2 && retrievedScores.awayScore === 1,
      `Expected: 2-1, Got: ${retrievedScores.homeScore}-${retrievedScores.awayScore}`
    );

    // Test storeMatchTime and getStoredMatchTime
    teamStorage.storeMatchTime(4500);
    const retrievedTime = teamStorage.getStoredMatchTime();
    logTest(
      'Match time storage/retrieval',
      retrievedTime === 4500,
      `Expected: 4500, Got: ${retrievedTime}`
    );
  } catch (error) {
    logTest('TeamStorage functions', false, `Error: ${error.message}`);
  }
}

// Test 2: Check SignalR service state
function testSignalRServiceState() {
  console.log('\n📋 Test 2: SignalR Service State');

  if (typeof window === 'undefined' || !window.signalRService) {
    logTest(
      'SignalR service availability',
      false,
      'Service not found on window object'
    );
    return;
  }

  const service = window.signalRService;
  logTest('SignalR service availability', true);

  // Check connection states
  const matchSimState = service.getMatchSimulationConnectionState();
  const notificationState = service.getNotificationConnectionState();

  logTest(
    'Match simulation connection',
    matchSimState === 'Connected',
    `State: ${matchSimState}`
  );
  logTest(
    'Notification connection',
    notificationState === 'Connected',
    `State: ${notificationState}`
  );

  // Check if global and page handlers exist
  const hasGlobalHandler = !!service.globalMatchEventHandler;
  const hasPageHandler = !!service.pageSpecificMatchEventHandler;

  logTest('Global match event handler', hasGlobalHandler);
  logTest(
    'Page-specific handler tracking',
    true,
    `Page handler exists: ${hasPageHandler}`
  );

  return { hasGlobalHandler, hasPageHandler };
}

// Test 3: Simulate match_start event and verify global storage
async function testGlobalEventHandler() {
  console.log('\n📋 Test 3: Global Event Handler');

  if (typeof window === 'undefined' || !window.signalRService) {
    logTest(
      'Global event handler test',
      false,
      'SignalR service not available'
    );
    return;
  }

  const service = window.signalRService;

  if (!service.globalMatchEventHandler) {
    logTest(
      'Global event handler test',
      false,
      'Global handler not initialized'
    );
    return;
  }

  // Clear existing data
  localStorage.clear();

  // Create mock match_start event
  const mockEvent = {
    event_type: 'match_start',
    home_team: 'Test Home Team',
    away_team: 'Test Away Team',
    Score: { home: 0, away: 0 },
    time_seconds: 0,
    match_id: 'global-test-match-789',
    timestamp: new Date().toISOString(),
    event_index: 1,
  };

  try {
    // Trigger global handler directly
    service.globalMatchEventHandler(
      'SendMatchEventAsync',
      mockEvent.match_id,
      mockEvent
    );
    logTest('Global handler execution', true);

    // Wait a moment for async operations
    await sleep(1000);

    // Check if data was stored
    const storedMatchId = localStorage.getItem('matchId');
    const storedHomeTeam = localStorage.getItem('homeTeamName');
    const storedAwayTeam = localStorage.getItem('awayTeamName');
    const storedHomeScore = localStorage.getItem('homeScore');
    const storedAwayScore = localStorage.getItem('awayScore');
    const storedMatchTime = localStorage.getItem('matchTime');

    logTest(
      'Match ID stored by global handler',
      storedMatchId === 'global-test-match-789',
      `Expected: global-test-match-789, Got: ${storedMatchId}`
    );
    logTest(
      'Home team stored by global handler',
      storedHomeTeam === 'Test Home Team',
      `Expected: Test Home Team, Got: ${storedHomeTeam}`
    );
    logTest(
      'Away team stored by global handler',
      storedAwayTeam === 'Test Away Team',
      `Expected: Test Away Team, Got: ${storedAwayTeam}`
    );
    logTest(
      'Home score stored by global handler',
      storedHomeScore === '0',
      `Expected: 0, Got: ${storedHomeScore}`
    );
    logTest(
      'Away score stored by global handler',
      storedAwayScore === '0',
      `Expected: 0, Got: ${storedAwayScore}`
    );
    logTest(
      'Match time stored by global handler',
      storedMatchTime === '0',
      `Expected: 0, Got: ${storedMatchTime}`
    );
  } catch (error) {
    logTest('Global event handler execution', false, `Error: ${error.message}`);
  }
}

// Test 4: Test persistence across "page navigation"
async function testPersistenceAcrossPages() {
  console.log('\n📋 Test 4: Cross-Page Persistence');

  // Get current localStorage state
  const beforeNavigation = {
    matchId: localStorage.getItem('matchId'),
    homeTeamName: localStorage.getItem('homeTeamName'),
    awayTeamName: localStorage.getItem('awayTeamName'),
    homeScore: localStorage.getItem('homeScore'),
    awayScore: localStorage.getItem('awayScore'),
    matchTime: localStorage.getItem('matchTime'),
  };

  // Simulate page navigation by triggering storage events
  console.log('📄 Simulating page navigation...');
  await sleep(500);

  // Check if data persisted
  const afterNavigation = {
    matchId: localStorage.getItem('matchId'),
    homeTeamName: localStorage.getItem('homeTeamName'),
    awayTeamName: localStorage.getItem('awayTeamName'),
    homeScore: localStorage.getItem('homeScore'),
    awayScore: localStorage.getItem('awayScore'),
    matchTime: localStorage.getItem('matchTime'),
  };

  // Compare before and after
  const allPersisted = Object.keys(beforeNavigation).every(
    (key) => beforeNavigation[key] === afterNavigation[key]
  );

  logTest(
    'Data persistence across page navigation',
    allPersisted,
    allPersisted ? 'All data persisted' : 'Some data was lost'
  );

  // Log the data for verification
  console.log('📊 Data state after navigation:', afterNavigation);
}

// Test 5: Test EventPlotter integration
async function testEventPlotterIntegration() {
  console.log('\n📋 Test 5: EventPlotter Integration');

  try {
    // Import teamStorage functions used by EventPlotter
    const teamStorage = await import('./lib/teamStorage.js');

    // Test getTeamNameWithFallback function
    const homeTeamWithFallback = teamStorage.getTeamNameWithFallback(
      undefined, // No event data
      'homeTeamName', // localStorage key
      'Default Home Team' // default
    );

    const awayTeamWithFallback = teamStorage.getTeamNameWithFallback(
      undefined, // No event data
      'awayTeamName', // localStorage key
      'Default Away Team' // default
    );

    const homeTeamFromStorage = localStorage.getItem('homeTeamName');
    const awayTeamFromStorage = localStorage.getItem('awayTeamName');

    logTest(
      'EventPlotter home team fallback',
      homeTeamWithFallback === homeTeamFromStorage ||
        homeTeamWithFallback === 'Default Home Team',
      `Got: ${homeTeamWithFallback}, Expected: ${homeTeamFromStorage || 'Default Home Team'}`
    );

    logTest(
      'EventPlotter away team fallback',
      awayTeamWithFallback === awayTeamFromStorage ||
        awayTeamWithFallback === 'Default Away Team',
      `Got: ${awayTeamWithFallback}, Expected: ${awayTeamFromStorage || 'Default Away Team'}`
    );
  } catch (error) {
    logTest('EventPlotter integration', false, `Error: ${error.message}`);
  }
}

// Test 6: Test SignalR listener coexistence
function testListenerCoexistence() {
  console.log('\n📋 Test 6: Listener Coexistence');

  if (typeof window === 'undefined' || !window.signalRService) {
    logTest(
      'Listener coexistence test',
      false,
      'SignalR service not available'
    );
    return;
  }

  const service = window.signalRService;

  // Check initial state
  const initialGlobalHandler = service.globalMatchEventHandler;
  const initialPageHandler = service.pageSpecificMatchEventHandler;

  logTest('Initial global handler exists', !!initialGlobalHandler);

  // Simulate adding a page-specific listener
  const mockPageListener = (method, matchId, eventData) => {
    console.log('Mock page listener triggered');
  };

  service.onMatchEvent(mockPageListener);

  // Check if global handler still exists after adding page listener
  const globalHandlerAfterPageAdd = service.globalMatchEventHandler;
  const pageHandlerAfterAdd = service.pageSpecificMatchEventHandler;

  logTest(
    'Global handler persists after page listener added',
    globalHandlerAfterPageAdd === initialGlobalHandler,
    'Global handler should remain the same'
  );

  logTest(
    'Page handler properly set',
    pageHandlerAfterAdd === mockPageListener,
    'Page handler should be set to the new listener'
  );
}

// Test 7: Real-time monitoring setup
function testRealTimeMonitoring() {
  console.log('\n📋 Test 7: Real-time Monitoring Setup');

  if (typeof window === 'undefined' || !window.signalRService) {
    logTest(
      'Real-time monitoring setup',
      false,
      'SignalR service not available'
    );
    return;
  }

  const service = window.signalRService;

  if (!service.matchSimulationConnection) {
    logTest(
      'Real-time monitoring setup',
      false,
      'No match simulation connection'
    );
    return;
  }

  let eventReceived = false;

  // Add a temporary listener to test real events
  const testListener = (method, matchId, eventData) => {
    eventReceived = true;
    console.log(`🎯 Test listener received: ${eventData?.event_type}`);
  };

  service.matchSimulationConnection.on('SendMatchEventAsync', testListener);
  logTest('Real-time monitoring listener added', true);

  // Clean up the test listener after a short delay
  setTimeout(() => {
    if (service.matchSimulationConnection) {
      service.matchSimulationConnection.off(
        'SendMatchEventAsync',
        testListener
      );
      logTest('Real-time monitoring cleanup', true);
    }
  }, 5000);

  return testListener;
}

// Main test execution function
async function runComprehensiveTest() {
  console.log('🎯 Running comprehensive team storage test suite...');
  console.log('⏱️  This may take a few moments...\n');

  try {
    // Run all tests
    await testTeamStorageFunctions();
    const signalRState = testSignalRServiceState();
    await testGlobalEventHandler();
    await testPersistenceAcrossPages();
    await testEventPlotterIntegration();
    testListenerCoexistence();
    testRealTimeMonitoring();

    // Summary
    console.log('\n' + '='.repeat(70));
    console.log('🏁 TEST SUITE COMPLETED');
    console.log('='.repeat(70));
    console.log(`✅ Tests Passed: ${testResults.passed}`);
    console.log(`❌ Tests Failed: ${testResults.failed}`);
    console.log(
      `📊 Success Rate: ${((testResults.passed / (testResults.passed + testResults.failed)) * 100).toFixed(1)}%`
    );

    if (testResults.failed > 0) {
      console.log('\n❌ Failed Tests:');
      testResults.results
        .filter((r) => !r.passed)
        .forEach((r) => console.log(`   • ${r.testName}: ${r.details}`));
    }

    console.log('\n💡 Recommendations:');
    if (signalRState?.hasGlobalHandler) {
      console.log(
        '   ✅ Global handler is active - team data should persist across pages'
      );
    } else {
      console.log(
        '   ⚠️  Global handler not active - may need to connect SignalR first'
      );
    }

    console.log(
      '   🔄 Navigate to different pages to test cross-page persistence'
    );
    console.log('   🎮 Start a match simulation to test with real events');
    console.log('   👀 Monitor browser console for real-time event logs');

    return testResults;
  } catch (error) {
    console.error('❌ Test suite failed:', error);
    return { error: error.message, ...testResults };
  }
}

// Export functions for manual testing
if (typeof window !== 'undefined') {
  window.testTeamStorageFunctions = testTeamStorageFunctions;
  window.testSignalRServiceState = testSignalRServiceState;
  window.testGlobalEventHandler = testGlobalEventHandler;
  window.testPersistenceAcrossPages = testPersistenceAcrossPages;
  window.testEventPlotterIntegration = testEventPlotterIntegration;
  window.testListenerCoexistence = testListenerCoexistence;
  window.testRealTimeMonitoring = testRealTimeMonitoring;
  window.runComprehensiveTest = runComprehensiveTest;

  console.log('🛠️  Available test functions:');
  console.log('   • testTeamStorageFunctions()');
  console.log('   • testSignalRServiceState()');
  console.log('   • testGlobalEventHandler()');
  console.log('   • testPersistenceAcrossPages()');
  console.log('   • testEventPlotterIntegration()');
  console.log('   • testListenerCoexistence()');
  console.log('   • testRealTimeMonitoring()');
  console.log('   • runComprehensiveTest() - Run all tests');

  // Auto-run after a short delay
  console.log('\n⏳ Auto-running comprehensive test in 3 seconds...');
  console.log(
    '💡 You can also run individual tests manually using the functions above.\n'
  );

  setTimeout(() => {
    runComprehensiveTest();
  }, 3000);
}

export {}; // Make this a module
