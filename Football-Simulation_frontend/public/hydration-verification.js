// HYDRATION ERROR VERIFICATION SCRIPT
// Paste this into browser console to verify hydration issues are resolved

console.log('🔍 VERIFYING HYDRATION ERROR FIXES...');

// Check for hydration warnings in console
let hydrationErrorCount = 0;
const originalConsoleError = console.error;

console.error = function (...args) {
  if (args[0] && typeof args[0] === 'string' && args[0].includes('hydration')) {
    hydrationErrorCount++;
    console.log(
      `❌ HYDRATION ERROR DETECTED (#${hydrationErrorCount}):`,
      args[0]
    );
  }
  originalConsoleError.apply(console, args);
};

// Check HTML structure for whitespace issues
function checkHTMLStructure() {
  console.log('🔍 Checking HTML structure for whitespace issues...');

  const htmlElement = document.documentElement;
  const headElement = document.head;
  const bodyElement = document.body;

  console.log('HTML Element:', htmlElement);
  console.log('Head Element:', headElement);
  console.log('Body Element:', bodyElement);

  // Check for text nodes between elements
  const htmlChildren = Array.from(htmlElement.childNodes);
  const textNodes = htmlChildren.filter(
    (node) => node.nodeType === Node.TEXT_NODE
  );

  if (textNodes.length > 0) {
    console.warn('⚠️ Found text nodes in HTML element:');
    textNodes.forEach((node, index) => {
      console.log(
        `  Text Node ${index + 1}:`,
        JSON.stringify(node.textContent)
      );
    });
  } else {
    console.log('✅ No problematic text nodes found in HTML element');
  }

  // Check for emergency scripts
  const scripts = Array.from(document.querySelectorAll('script'));
  const emergencyScripts = scripts.filter(
    (script) =>
      script.src.includes('emergency') ||
      script.src.includes('ultra-aggressive')
  );

  console.log(`📜 Found ${emergencyScripts.length} emergency scripts:`);
  emergencyScripts.forEach((script) => {
    console.log(`  ✅ ${script.src}`);
  });
}

// Check SignalR status
function checkSignalRStatus() {
  console.log('🔗 Checking SignalR status...');

  if (window.signalRService) {
    console.log('✅ SignalR Service found');
    console.log(
      `  Match Simulation Connected: ${window.signalRService.isMatchSimulationConnected ? '✅' : '❌'}`
    );
    console.log(
      `  Notification Connected: ${window.signalRService.isNotificationConnected ? '✅' : '❌'}`
    );
  } else {
    console.log('❌ SignalR Service not found');
  }
}

// Check emergency fixes
function checkEmergencyFixes() {
  console.log('🚑 Checking emergency fixes status...');

  const fixes = {
    'Emergency Hot Fix': typeof window.emergencyFixAll !== 'undefined',
    'Emergency Show Stats': typeof window.emergencyShowStats !== 'undefined',
    'Emergency SignalR Fix': typeof window.emergencySignalRFix !== 'undefined',
    'Cancel All Simulations':
      typeof window.cancelAllSimulations !== 'undefined',
  };

  Object.entries(fixes).forEach(([fix, available]) => {
    console.log(`  ${available ? '✅' : '❌'} ${fix}`);
  });
}

// Performance check
function checkPerformance() {
  console.log('⚡ Checking performance...');

  if (performance.memory) {
    const memory = performance.memory;
    console.log(
      `🧠 Memory: ${(memory.usedJSHeapSize / 1024 / 1024).toFixed(2)} MB used`
    );
  }

  if (performance.timing) {
    const timing = performance.timing;
    const loadTime = timing.loadEventEnd - timing.navigationStart;
    console.log(`⏱️ Page Load Time: ${loadTime}ms`);
  }
}

// Run all checks
setTimeout(() => {
  console.log('🚀 RUNNING COMPREHENSIVE VERIFICATION...');
  console.log('=====================================');

  checkHTMLStructure();
  checkSignalRStatus();
  checkEmergencyFixes();
  checkPerformance();

  console.log('=====================================');
  console.log(`📊 HYDRATION ERROR COUNT: ${hydrationErrorCount}`);
  console.log(
    hydrationErrorCount === 0
      ? '✅ NO HYDRATION ERRORS DETECTED!'
      : `❌ ${hydrationErrorCount} HYDRATION ERRORS FOUND`
  );
  console.log('🎯 VERIFICATION COMPLETE');
}, 2000);

// Monitor for new hydration errors for 30 seconds
setTimeout(() => {
  console.error = originalConsoleError;
  console.log('🔍 Hydration monitoring stopped');
  console.log(`📈 Final hydration error count: ${hydrationErrorCount}`);
}, 30000);
