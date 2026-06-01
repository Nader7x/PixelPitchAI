'use client';

import { useEffect } from 'react';
import signalRService from '@/Services/SignalRService';
import authService from '@/Services/AuthenticationService';

/**
 * INSTANT SignalR Connector - Connects IMMEDIATELY on app load
 * This component mounts at the very top level and establishes SignalR connection
 * with ZERO delay to catch events as fast as humanly possible
 */
export function InstantSignalRConnector() {
  useEffect(() => {
    // IMMEDIATE CONNECTION - NO WAITING, NO DELAYS
    const connectInstantly = async () => {
      console.log('🚀⚡ INSTANT SIGNALR CONNECTOR - CONNECTING NOW!');

      let connected = false;
      let attempts = 0;
      const maxAttempts = 10;

      // Keep trying until connected (aggressive approach)
      while (!connected && attempts < maxAttempts) {
        try {
          attempts++;
          console.log(`⚡ Attempt ${attempts}: Connecting at MAXIMUM SPEED...`);

          // Try to connect immediately
          connected = await signalRService.connect();
          if (connected) {
            console.log('🏆⚡ INSTANT CONNECTION SUCCESSFUL!');
            console.log('🎯 READY TO INTERCEPT EVENTS AT LIGHT SPEED!');

            // Verify global handler is active
            signalRService.ensureGlobalMatchEventHandler();
            console.log('🔥 GLOBAL EVENT HANDLER ARMED AND READY!');

            // Enable comprehensive debugging for missing methods
            signalRService.enableComprehensiveDebugMode();
            signalRService.addUniversalMethodHandlers();
            console.log(
              '🔍 DEBUG MODE ENABLED - CATCHING ALL MISSING METHODS!'
            );

            break;
          } else {
            console.log(`❌ Attempt ${attempts} failed, retrying in 100ms...`);
            await new Promise((resolve) => setTimeout(resolve, 100));
          }
        } catch (error) {
          console.log(`⚠️ Attempt ${attempts} error:`, error);
          await new Promise((resolve) => setTimeout(resolve, 100));
        }
      }

      if (!connected) {
        console.warn(
          '❌ Could not establish instant connection after max attempts'
        );
      }
    };

    // Execute IMMEDIATELY - not even waiting for next tick
    connectInstantly();

    // Also try when authentication changes
    const handleAuthChange = () => {
      if (authService.isAuthenticated()) {
        console.log('🔥 AUTH DETECTED - RECONNECTING INSTANTLY!');
        connectInstantly();
      }
    };

    // Listen for auth changes
    const interval = setInterval(() => {
      if (
        authService.isAuthenticated() &&
        !signalRService.isGloballyConnected()
      ) {
        handleAuthChange();
      }
    }, 500); // Check every 500ms

    // Cleanup after 30 seconds to avoid infinite checking
    const cleanup = setTimeout(() => {
      clearInterval(interval);
    }, 30000);

    return () => {
      clearInterval(interval);
      clearTimeout(cleanup);
    };
  }, []); // No dependencies - run once immediately

  // This component renders nothing - it's just for the instant connection effect
  return null;
}
