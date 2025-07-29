// EMERGENCY FIX - Immediate SignalR and Performance Solution
'use client';

import { useEffect } from 'react';
import signalRService from '@/Services/SignalRService';

export function EmergencySignalRFix() {
  useEffect(() => {
    console.log('🚨 EMERGENCY SIGNALR FIX ACTIVATED!');

    // IMMEDIATE connection with error handling
    const emergencyConnect = async () => {
      try {
        console.log('⚡ Attempting emergency SignalR connection...');

        // Force disconnect first to clean state
        await signalRService.disconnect();

        // Wait a moment then reconnect
        setTimeout(async () => {
          try {
            const connected = await signalRService.connect();
            if (connected) {
              console.log('✅ EMERGENCY CONNECTION SUCCESSFUL!');

              // Ensure global handler is working
              signalRService.ensureGlobalMatchEventHandler();
              console.log('🎯 Global handler activated');

              // Test the connection
              if (signalRService.isGloballyConnected()) {
                console.log('🏆 SIGNALR IS NOW WORKING - READY FOR EVENTS!');
              }
            } else {
              console.error('❌ Emergency connection failed');
            }
          } catch (error) {
            console.error('❌ Emergency connection error:', error);
          }
        }, 1000);
      } catch (error) {
        console.error('❌ Emergency disconnect error:', error);
      }
    };

    // Execute immediately
    emergencyConnect();

    // Also set up a safety net - retry every 10 seconds until connected
    const safetyInterval = setInterval(() => {
      if (!signalRService.isGloballyConnected()) {
        console.log('🔄 Safety net: Attempting reconnection...');
        emergencyConnect();
      } else {
        console.log('✅ Safety net: Connection is healthy');
        clearInterval(safetyInterval);
      }
    }, 10000);

    // Clean up after 5 minutes
    setTimeout(() => {
      clearInterval(safetyInterval);
      console.log('🛑 Emergency fix safety net deactivated');
    }, 300000);

    return () => {
      clearInterval(safetyInterval);
    };
  }, []);

  return null;
}

// Also export a function to manually fix SignalR from console
if (typeof window !== 'undefined') {
  (window as any).emergencySignalRFix = async () => {
    console.log('🚨 MANUAL EMERGENCY SIGNALR FIX!');

    try {
      await signalRService.disconnect();
      await new Promise((resolve) => setTimeout(resolve, 2000));
      const connected = await signalRService.connect();

      if (connected) {
        signalRService.ensureGlobalMatchEventHandler();
        console.log('✅ MANUAL FIX SUCCESSFUL!');
        return true;
      } else {
        console.error('❌ Manual fix failed');
        return false;
      }
    } catch (error) {
      console.error('❌ Manual fix error:', error);
      return false;
    }
  };
}
