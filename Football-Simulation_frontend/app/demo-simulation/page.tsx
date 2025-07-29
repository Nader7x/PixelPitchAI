'use client';

import { useState, useEffect } from 'react';
import { useRouter } from 'next/navigation';
import { Canvas } from '@react-three/fiber';
import { OrbitControls } from '@react-three/drei';
import { Play, Pause, RotateCcw, ArrowLeft } from 'lucide-react';
import useMockEventStream from '@/hooks/useMockEventStream';
import { Event } from '@/types/Event';
import DemoEventPlotter from '@/components/DemoEventPlotter';

// Demo match data that doesn't use localStorage
const DEMO_MATCH_DATA = {
  homeTeam: 'Barcelona_2016',
  awayTeam: 'Real_Madrid_2021',
  homeTeamDisplay: 'Barcelona 2016',
  awayTeamDisplay: 'Real Madrid 2021',
};

export default function DemoSimulationPage() {
  const router = useRouter();
  const [isStarted, setIsStarted] = useState(false);
  const [isPaused, setIsPaused] = useState(false);
  const [demoEvents, setDemoEvents] = useState<Event[]>([]);
  const [timer, setTimer] = useState(0);

  // Use the mock event stream
  const streamedEvents = useMockEventStream();

  useEffect(() => {
    if (isStarted && !isPaused) {
      setDemoEvents(streamedEvents);
    }
  }, [streamedEvents, isStarted, isPaused]);

  // Timer logic - synchronized with demo
  useEffect(() => {
    let interval: NodeJS.Timeout;
    if (isStarted && !isPaused) {
      interval = setInterval(() => {
        setTimer((prev) => prev + 1);
      }, 1000);
    }
    return () => clearInterval(interval);
  }, [isStarted, isPaused]);

  const startDemo = () => {
    setIsStarted(true);
    setIsPaused(false);
    setDemoEvents([]);
    setTimer(0);
  };

  const pauseDemo = () => {
    setIsPaused(!isPaused);
  };

  const resetDemo = () => {
    setIsStarted(false);
    setIsPaused(false);
    setDemoEvents([]);
    setTimer(0);
    // Force a page refresh to restart the mock stream
    window.location.reload();
  };
  const goBack = () => {
    router.back();
  };

  return (
    <>
      {/* Add the CSS animations inline */}
      <style jsx>{`
        @keyframes gentle-glow {
          0% {
            opacity: 0.4;
            transform: scale(1) rotate(0deg);
          }
          50% {
            opacity: 0.7;
            transform: scale(1.05) rotate(2deg);
          }
          100% {
            opacity: 0.5;
            transform: scale(1.02) rotate(-1deg);
          }
        }

        @keyframes subtle-rays {
          0% {
            opacity: 0.2;
            transform: rotate(0deg) scale(1);
          }
          50% {
            opacity: 0.4;
            transform: rotate(1deg) scale(1.02);
          }
          100% {
            opacity: 0.3;
            transform: rotate(-0.5deg) scale(1.01);
          }
        }

        @keyframes twinkle-stars {
          0% {
            opacity: 0.3;
            transform: scale(1);
          }
          50% {
            opacity: 0.8;
            transform: scale(1.1);
          }
          100% {
            opacity: 0.4;
            transform: scale(1.05);
          }
        }
      `}</style>

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

        {/* Control Panel - Floating at top */}
        <div className="absolute top-4 left-1/2 z-10 -translate-x-1/2 transform">
          <div className="flex items-center gap-4 rounded-lg border border-gray-700 bg-gray-900/80 px-6 py-3 backdrop-blur-sm">
            <button
              onClick={goBack}
              className="rounded-lg bg-gray-800 p-2 text-white transition-colors hover:bg-gray-700"
            >
              <ArrowLeft size={20} />
            </button>

            <div className="font-medium text-white">Demo Simulation</div>

            {!isStarted ? (
              <button
                onClick={startDemo}
                className="flex items-center gap-2 rounded-lg bg-green-600 px-4 py-2 font-medium text-white transition-colors hover:bg-green-700"
              >
                <Play size={16} />
                Start
              </button>
            ) : (
              <>
                <button
                  onClick={pauseDemo}
                  className={`flex items-center gap-2 rounded-lg px-4 py-2 font-medium transition-colors ${
                    isPaused
                      ? 'bg-green-600 text-white hover:bg-green-700'
                      : 'bg-yellow-600 text-white hover:bg-yellow-700'
                  }`}
                >
                  {isPaused ? <Play size={16} /> : <Pause size={16} />}
                  {isPaused ? 'Resume' : 'Pause'}
                </button>
                <button
                  onClick={resetDemo}
                  className="flex items-center gap-2 rounded-lg bg-red-600 px-4 py-2 font-medium text-white transition-colors hover:bg-red-700"
                >
                  <RotateCcw size={16} />
                  Reset
                </button>
              </>
            )}
          </div>
        </div>

        {/* 3D Canvas - Full Screen */}
        <Canvas
          shadows
          camera={{ position: [0, 45, 115], fov: 50, near: 0.1, far: 1000 }}
          style={{ zIndex: 2 }}
        >
          <ambientLight intensity={0.5} />
          <directionalLight
            position={[10, 20, 10]}
            intensity={1}
            castShadow
          />{' '}
          <OrbitControls />
          <DemoEventPlotter
            events={demoEvents}
            timer={timer}
            homeTeam={DEMO_MATCH_DATA.homeTeam}
            awayTeam={DEMO_MATCH_DATA.awayTeam}
          />{' '}
        </Canvas>
      </div>
    </>
  );
}
