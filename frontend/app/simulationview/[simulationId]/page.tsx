'use client';

import { useEffect, useState } from 'react';
import { Canvas } from '@react-three/fiber';
import { OrbitControls } from '@react-three/drei';
import Field from '@/components/Field';
import useSignalREventStream from '@/hooks/useSignalREventStream';
import EventPlotter from '@/components/EventPlotter';
import signalRService from '@/Services/SignalRService';
import { useRouter } from 'next/navigation';
import './simulationView.css'; // Ensure this file exists for styling

export default function SimulationView() {
  const [timer, setTimer] = useState(0);
  const [matchId, setMatchId] = useState<number>(0);
  const [matchStarted, setMatchStarted] = useState(false);
  const router = useRouter();
  // Get matchId and check if match has already started from localStorage - CLIENT SIDE ONLY
  useEffect(() => {
    if (typeof window !== 'undefined') {
      const matchIdStr = localStorage.getItem('matchId') || '';
      const parsedMatchId = matchIdStr ? parseInt(matchIdStr, 10) : 0;
      setMatchId(parsedMatchId);

      // Check if this match has already started
      const matchStartedKey = `match_started_${parsedMatchId}`;
      const hasMatchStarted = localStorage.getItem(matchStartedKey) === 'true';

      if (hasMatchStarted) {
        console.log(
          `[SimulationView] Match ${parsedMatchId} has already started - enabling navigation`
        );
        setMatchStarted(true);
      }

      console.log(
        `[SimulationView] Retrieved matchId from localStorage: "${matchIdStr}" -> ${parsedMatchId}`
      );
      console.log(`[SimulationView] matchId is valid: ${parsedMatchId > 0}`);
      console.log(`[SimulationView] Match already started: ${hasMatchStarted}`);
    }
  }, []);

  const { events, isConnected, retryCount } = useSignalREventStream(matchId);

  console.log(
    `[SimulationView] Current state - events: ${events.length}, isConnected: ${isConnected}, retries: ${retryCount}`
  ); // Debug effect to track events changes in SimulationView
  useEffect(() => {
    console.log(
      `🎪🎪🎪 [SimulationView] EVENTS ARRAY CHANGED - Count: ${events.length}`
    );
    if (events.length > 0) {
      console.log(
        `🎪 [SimulationView] Latest event in view:`,
        events[events.length - 1]
      );
      console.log(`🎪 [SimulationView] All events in view:`, events);

      // Check for match_start event
      const hasMatchStartEvent = events.some(
        (event) => event.event_type === 'match_start'
      );
      if (hasMatchStartEvent && !matchStarted) {
        console.log(
          '🎯 [SimulationView] Match started! Enabling navigation controls.'
        );
        setMatchStarted(true);

        // Store match started status in localStorage for this specific match
        if (typeof window !== 'undefined' && matchId > 0) {
          const matchStartedKey = `match_started_${matchId}`;
          localStorage.setItem(matchStartedKey, 'true');
          console.log(
            `[SimulationView] Stored match started status for match ${matchId}`
          );
        }
      }
    } else {
      console.log(`🎪 [SimulationView] No events yet in view`);
    }
  }, [events, matchStarted, matchId]);
  // Prevent browser back navigation until match starts
  useEffect(() => {
    const handlePopState = (event: PopStateEvent) => {
      if (!matchStarted) {
        // Prevent navigation by pushing current state back
        window.history.pushState(null, '', window.location.href);
        console.log(
          '🚫 [SimulationView] Navigation blocked - waiting for match to start'
        );

        // Show user feedback
        const existingNotification = document.getElementById(
          'nav-blocked-notification'
        );
        if (!existingNotification) {
          const notification = document.createElement('div');
          notification.id = 'nav-blocked-notification';
          notification.style.cssText = `
            position: fixed;
            top: 50%;
            left: 50%;
            transform: translate(-50%, -50%);
            background: rgba(220, 53, 69, 0.95);
            color: white;
            padding: 16px 24px;
            border-radius: 8px;
            font-size: 16px;
            font-weight: bold;
            z-index: 9999;
            box-shadow: 0 4px 12px rgba(0, 0, 0, 0.3);
            font-family: Arial, sans-serif;
          `;
          notification.textContent =
            '🔒 Please wait for the match to start before leaving this page';
          document.body.appendChild(notification);

          // Remove notification after 3 seconds
          setTimeout(() => {
            if (document.body.contains(notification)) {
              document.body.removeChild(notification);
            }
          }, 3000);
        }
      }
    };

    const handleBeforeUnload = (event: BeforeUnloadEvent) => {
      if (!matchStarted) {
        event.preventDefault();
        event.returnValue =
          'The match is about to start. Are you sure you want to leave?';
        return 'The match is about to start. Are you sure you want to leave?';
      }
    };

    if (!matchStarted) {
      // Push current state to prevent back navigation
      window.history.pushState(null, '', window.location.href);

      // Listen for popstate events (back/forward buttons)
      window.addEventListener('popstate', handlePopState);

      // Listen for page unload attempts
      window.addEventListener('beforeunload', handleBeforeUnload);

      console.log('🔒 [SimulationView] Navigation protection enabled');
    } else {
      console.log(
        '🔓 [SimulationView] Navigation protection disabled - match has started'
      );
    }

    return () => {
      window.removeEventListener('popstate', handlePopState);
      window.removeEventListener('beforeunload', handleBeforeUnload);
    };
  }, [matchStarted]);

  useEffect(() => {
    if (typeof window !== 'undefined') {
      // Log all localStorage items for debugging
      console.log('[SimulationView] All localStorage items:');
      for (let i = 0; i < localStorage.length; i++) {
        const key = localStorage.key(i);
        if (key) {
          const value = localStorage.getItem(key);
          console.log(`  ${key}: ${value}`);
        }
      }

      // Check if we have a valid matchId
      if (!matchId || matchId <= 0) {
        console.warn(
          '[SimulationView] No valid matchId found in localStorage. Please start a simulation first.'
        );
      } else {
        console.log(`[SimulationView] Using matchId: ${matchId}`);
      }
    }
  }, [matchId]);

  const handleReturnToDashboard = () => {
    console.log('🏠 [SimulationView] Returning to dashboard...');
    router.push('/dashboard');
  };

  // Cleanup function to remove old match started statuses (keep only last 5 matches)
  const cleanupOldMatchStates = () => {
    if (typeof window !== 'undefined') {
      const keys = Object.keys(localStorage);
      const matchStartedKeys = keys.filter((key) =>
        key.startsWith('match_started_')
      );

      if (matchStartedKeys.length > 5) {
        // Sort by match ID and keep only the 5 most recent
        const sortedKeys = matchStartedKeys.sort((a, b) => {
          const matchIdA = parseInt(a.replace('match_started_', ''), 10);
          const matchIdB = parseInt(b.replace('match_started_', ''), 10);
          return matchIdB - matchIdA; // Descending order (newest first)
        });

        // Remove older entries
        const keysToRemove = sortedKeys.slice(5);
        keysToRemove.forEach((key) => {
          localStorage.removeItem(key);
          console.log(`[SimulationView] Cleaned up old match state: ${key}`);
        });
      }
    }
  };
  // Clean up old match states when component mounts
  useEffect(() => {
    cleanupOldMatchStates();
  }, []);

  // Helper function to reset match state (for testing)
  const resetMatchState = () => {
    if (typeof window !== 'undefined' && matchId > 0) {
      const matchStartedKey = `match_started_${matchId}`;
      localStorage.removeItem(matchStartedKey);
      setMatchStarted(false);
      console.log(`[SimulationView] Reset match state for match ${matchId}`);
    }
  }; // Timer logic - synchronized across the app
  useEffect(() => {
    const interval = setInterval(() => {
      setTimer((prev) => prev + 1);
    }, 1000);
    return () => clearInterval(interval);
  }, []);

  const formatTime = (time: number) => {
    const minutes = Math.floor(time / 60);
    const seconds = time % 60;
    return `${String(minutes).padStart(2, '0')}:${String(seconds).padStart(2, '0')}`;
  };
  return (
    <div
      style={{
        width: '100vw',
        height: '100vh',
        position: 'relative',
        // Beautiful black to darker blue gradient - lighter version
        background: `
                    radial-gradient(ellipse at top, 
                        rgba(12, 20, 50, 0.5) 0%, 
                        rgba(8, 12, 30, 0.9) 40%, 
                        rgba(2, 2, 8, 1) 80%
                    ),
                    linear-gradient(135deg, 
                        #0a0a0a 0%, 
                        #0f1420 20%, 
                        #141b2a 40%, 
                        #1a2238 60%, 
                        #0a0a0a 100%
                    ),
                    linear-gradient(to bottom, 
                        rgba(5, 5, 5, 0.3) 0%, 
                        rgba(2, 2, 8, 0.7) 60%, 
                        rgba(0, 0, 0, 1) 100%
                    )
                `,
        overflow: 'hidden',
      }}
    >
      {/* Subtle atmospheric glow */}
      <div
        style={{
          position: 'absolute',
          top: 0,
          left: 0,
          right: 0,
          bottom: 0,
          background: `
                        radial-gradient(ellipse 60% 40% at 50% 20%, 
                            rgba(20, 45, 85, 0.08) 0%, 
                            transparent 70%
                        ),
                        radial-gradient(ellipse 80% 60% at 30% 80%, 
                            rgba(12, 25, 55, 0.06) 0%, 
                            transparent 60%
                        ),
                        radial-gradient(ellipse 80% 60% at 70% 80%, 
                            rgba(12, 25, 55, 0.06) 0%, 
                            transparent 60%
                        )
                    `,
          animation: 'gentle-glow 12s ease-in-out infinite alternate',
          pointerEvents: 'none',
          zIndex: 1,
        }}
      />
      {/* VERY SUBTLE scoreboard area lighting */}
      <div
        style={{
          position: 'absolute',
          top: '10%',
          left: '35%',
          width: '30%',
          height: '25%',
          background: `
                        radial-gradient(ellipse 90% 80% at 50% 70%, 
                            rgba(255, 255, 255, 0.012) 0%, 
                            rgba(255, 255, 255, 0.005) 60%, 
                            transparent 100%
                        )
                    `,
          pointerEvents: 'none',
          zIndex: 1,
        }}
      />
      {/* Background dot pattern */}
      <div
        style={{
          position: 'absolute',
          top: 0,
          left: 0,
          right: 0,
          bottom: 0,
          background: `
                        radial-gradient(1px 1px at 8% 15%, rgba(255, 255, 255, 0.3), transparent),
                        radial-gradient(2px 2px at 22% 25%, rgba(255, 255, 255, 0.2), transparent),
                        radial-gradient(1px 1px at 45% 12%, rgba(255, 255, 255, 0.25), transparent),
                        radial-gradient(1px 1px at 65% 28%, rgba(255, 255, 255, 0.2), transparent),
                        radial-gradient(2px 2px at 78% 18%, rgba(255, 255, 255, 0.15), transparent),
                        radial-gradient(1px 1px at 92% 35%, rgba(255, 255, 255, 0.3), transparent),
                        radial-gradient(1px 1px at 15% 45%, rgba(255, 255, 255, 0.2), transparent),
                        radial-gradient(2px 2px at 38% 55%, rgba(255, 255, 255, 0.25), transparent),
                        radial-gradient(1px 1px at 58% 42%, rgba(255, 255, 255, 0.2), transparent),
                        radial-gradient(1px 1px at 82% 58%, rgba(255, 255, 255, 0.15), transparent),
                        radial-gradient(2px 2px at 5% 72%, rgba(255, 255, 255, 0.3), transparent),
                        radial-gradient(1px 1px at 28% 78%, rgba(255, 255, 255, 0.2), transparent),
                        radial-gradient(1px 1px at 52% 68%, rgba(255, 255, 255, 0.25), transparent),
                        radial-gradient(2px 2px at 75% 82%, rgba(255, 255, 255, 0.15), transparent),
                        radial-gradient(1px 1px at 88% 75%, rgba(255, 255, 255, 0.2), transparent)
                    `,
          backgroundSize: '150px 150px',
          pointerEvents: 'none',
          zIndex: 1,
        }}
      />
      {/* Elegant light beams */}
      <div
        style={{
          position: 'absolute',
          top: 0,
          left: 0,
          right: 0,
          bottom: 0,
          background: `
                        linear-gradient(45deg, 
                            transparent 40%, 
                            rgba(20, 45, 85, 0.04) 50%, 
                            transparent 60%
                        ),
                        linear-gradient(-45deg, 
                            transparent 40%, 
                            rgba(12, 25, 55, 0.04) 50%, 
                            transparent 60%
                        )
                    `,
          animation: 'subtle-rays 15s ease-in-out infinite alternate',
          pointerEvents: 'none',
          zIndex: 1,
        }}
      />
      {/* Static starfield effect */}
      <div
        style={{
          position: 'absolute',
          top: 0,
          left: 0,
          right: 0,
          bottom: 0,
          background: `
                        radial-gradient(1px 1px at 20% 30%, rgba(255, 255, 255, 0.6), transparent),
                        radial-gradient(1px 1px at 40% 60%, rgba(255, 255, 255, 0.4), transparent),
                        radial-gradient(2px 2px at 60% 20%, rgba(20, 45, 85, 0.3), transparent),
                        radial-gradient(1px 1px at 80% 70%, rgba(255, 255, 255, 0.5), transparent),
                        radial-gradient(1px 1px at 10% 80%, rgba(255, 255, 255, 0.3), transparent),
                        radial-gradient(2px 2px at 90% 40%, rgba(12, 25, 55, 0.2), transparent),
                        radial-gradient(1px 1px at 70% 10%, rgba(255, 255, 255, 0.4), transparent)
                    `,
          backgroundSize:
            '300px 300px, 400px 400px, 200px 200px, 350px 350px, 450px 450px, 250px 250px, 500px 500px',
          animation: 'twinkle-stars 8s ease-in-out infinite alternate',
          pointerEvents: 'none',
          zIndex: 1,
        }}
      />
      {/* Stronger vignette for darker edges */}
      <div
        style={{
          position: 'absolute',
          top: 0,
          left: 0,
          right: 0,
          bottom: 0,
          background: `
                        radial-gradient(ellipse 80% 60% at 50% 40%, 
                            transparent 0%, 
                            transparent 40%, 
                            rgba(0, 0, 0, 0.3) 70%, 
                            rgba(0, 0, 0, 0.7) 100%
                        )
                    `,
          pointerEvents: 'none',
          zIndex: 1,
        }}
      />
      {/* Extra dark bottom overlay */}
      <div
        style={{
          position: 'absolute',
          bottom: 0,
          left: 0,
          right: 0,
          height: '40%',
          background: `
                        linear-gradient(to top, 
                            rgba(0, 0, 0, 0.7) 0%, 
                            rgba(0, 0, 0, 0.3) 50%, 
                            transparent 100%
                        )
                    `,
          pointerEvents: 'none',
          zIndex: 1,
        }}
      />
      {/* Dark side borders */}
      <div
        style={{
          position: 'absolute',
          top: 0,
          left: 0,
          width: '15%',
          height: '100%',
          background: `
                        linear-gradient(to right, 
                            rgba(0, 0, 0, 0.5) 0%, 
                            rgba(0, 0, 0, 0.2) 70%, 
                            transparent 100%
                        )
                    `,
          pointerEvents: 'none',
          zIndex: 1,
        }}
      />
      <div
        style={{
          position: 'absolute',
          top: 0,
          right: 0,
          width: '15%',
          height: '100%',
          background: `
                        linear-gradient(to left, 
                            rgba(0, 0, 0, 0.5) 0%, 
                            rgba(0, 0, 0, 0.2) 70%, 
                            transparent 100%
                        )
                    `,
          pointerEvents: 'none',
          zIndex: 1,
        }}
      />
      <Canvas
        shadows
        camera={{ position: [0, 45, 115], fov: 50, near: 0.1, far: 1000 }}
        style={{ zIndex: 2 }}
      >
        <ambientLight intensity={0.5} />
        <directionalLight position={[10, 20, 10]} intensity={1} castShadow />
        <OrbitControls />
        <Field />
        <EventPlotter events={events} timer={timer} />{' '}
        {/* ✅ Pass the SignalR events */}
      </Canvas>{' '}
      {/* Navigation Status Indicator */}
      {!matchStarted && (
        <div
          style={{
            position: 'absolute',
            top: '20px',
            left: '50%',
            transform: 'translateX(-50%)',
            padding: '10px 20px',
            fontSize: '14px',
            fontWeight: 'bold',
            color: '#ffc107',
            background: 'rgba(0, 0, 0, 0.8)',
            border: '2px solid #ffc107',
            borderRadius: '8px',
            zIndex: 15,
            textAlign: 'center',
            boxShadow: '0 4px 12px rgba(255, 193, 7, 0.3)',
            fontFamily: 'Arial, sans-serif',
            animation: 'pulse 2s infinite',
          }}
        >
          🔒 Navigation locked - Waiting for match to start...
        </div>
      )}
      {/* Return to Dashboard Button - only visible after match starts */}
      {matchStarted && (
        <button
          onClick={handleReturnToDashboard}
          style={{
            position: 'absolute',
            top: '20px',
            right: '20px',
            padding: '12px 24px',
            fontSize: '16px',
            fontWeight: 'bold',
            color: 'white',
            background: 'linear-gradient(135deg, #007acc 0%, #0056b3 100%)',
            border: 'none',
            borderRadius: '8px',
            cursor: 'pointer',
            zIndex: 15,
            boxShadow: '0 4px 12px rgba(0, 122, 204, 0.3)',
            transition: 'all 0.3s ease',
            fontFamily: 'Arial, sans-serif',
          }}
          onMouseEnter={(e) => {
            e.currentTarget.style.background =
              'linear-gradient(135deg, #0056b3 0%, #004085 100%)';
            e.currentTarget.style.transform = 'translateY(-2px)';
            e.currentTarget.style.boxShadow =
              '0 6px 16px rgba(0, 122, 204, 0.4)';
          }}
          onMouseLeave={(e) => {
            e.currentTarget.style.background =
              'linear-gradient(135deg, #007acc 0%, #0056b3 100%)';
            e.currentTarget.style.transform = 'translateY(0)';
            e.currentTarget.style.boxShadow =
              '0 4px 12px rgba(0, 122, 204, 0.3)';
          }}
        >
          ← Return to Dashboard
        </button>
      )}
      {/* Timer display */}
      <div
        style={{
          position: 'absolute',
          top: '20px',
          left: '50%',
          transform: 'translateX(-50%)',
          fontSize: '32px',
          fontWeight: 'bold',
          color: 'white',
          padding: '10px 20px',
          borderRadius: '10px',
          zIndex: 10,
        }}
      >
        {/* ⏱ {formatTime(timer)} */}
      </div>
    </div>
  );
}
