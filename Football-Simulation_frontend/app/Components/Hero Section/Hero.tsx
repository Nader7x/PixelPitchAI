'use client';

import { useRouter } from 'next/navigation';
import { motion } from 'framer-motion';
import { useState, useEffect } from 'react';

export default function Hero() {
  const router = useRouter();
  const [isLoaded, setIsLoaded] = useState(false);

  useEffect(() => {
    setIsLoaded(true);
  }, []);

  const handleGetStarted = () => {
    router.push('/register');
  };

  const containerVariants = {
    hidden: { opacity: 0 },
    visible: {
      opacity: 1,
      transition: {
        staggerChildren: 0.3,
        delayChildren: 0.2,
      },
    },
  };

  const itemVariants = {
    hidden: { y: 20, opacity: 0 },
    visible: {
      y: 0,
      opacity: 1,
      transition: {
        type: 'spring' as const,
        stiffness: 100,
        damping: 10,
      },
    },
  };

  return (
    <section
      className="relative flex min-h-screen w-full items-center justify-center bg-cover bg-center bg-no-repeat pt-16 md:pt-20"
      style={{
        backgroundImage: "url('/images/Stadium dark.png')",
      }}
    >
      {/* Gradient Overlay */}
      <div className="absolute inset-0 bg-gradient-to-b from-black/71 via-black/50 to-black/70"></div>

      {/* Animated pattern */}
      <div className="absolute inset-0 bg-[url('/images/football-pattern.png')] bg-repeat opacity-5"></div>

      {/* Hero Content */}
      <div className="relative z-10 mx-auto w-full max-w-7xl px-4 py-20 md:px-6">
        <motion.div
          className="text-center"
          initial="hidden"
          animate={isLoaded ? 'visible' : 'hidden'}
          variants={containerVariants}
        >
          <motion.div variants={itemVariants}>
            <h1 className="mb-6 text-5xl leading-tight font-extrabold text-white md:text-6xl lg:text-8xl">
              Experience Football Like{' '}
              <span className="animate-pulse bg-gradient-to-r from-green-400 via-emerald-400 to-blue-500 bg-clip-text text-transparent">
                Never Before
              </span>
            </h1>
          </motion.div>

          <motion.div variants={itemVariants}>
            <p className="mx-auto mb-8 max-w-3xl text-xl leading-relaxed text-gray-200 md:text-2xl">
              Dive into the most realistic football simulations powered by
              cutting-edge AI technology. Generate authentic match events,
              comprehensive statistics, and tactical insights — experience the
              beautiful game like never before.
            </p>
          </motion.div>

          <motion.div
            variants={itemVariants}
            className="flex flex-col justify-center gap-4 sm:flex-row sm:gap-6"
          >
            <button
              className="group relative transform overflow-hidden rounded-xl bg-gradient-to-r from-green-500 to-emerald-600 px-8 py-4 text-lg font-semibold text-white shadow-lg transition-all duration-300 hover:scale-105 hover:from-green-600 hover:to-emerald-700 hover:shadow-2xl active:scale-95"
              onClick={handleGetStarted}
            >
              <span className="relative z-10">Sign Up</span>
              <div className="absolute inset-0 bg-gradient-to-r from-white/20 to-transparent opacity-0 transition-opacity duration-300 group-hover:opacity-100"></div>
            </button>
          </motion.div>

          {/* Feature highlights */}
          <motion.div
            variants={itemVariants}
            className="mt-16 grid grid-cols-1 gap-6 text-center md:grid-cols-3"
          >
            <div className="rounded-xl bg-white/10 p-6 backdrop-blur-sm transition-all hover:bg-white/20">
              <div className="bg-primary/20 text-primary mx-auto mb-4 flex h-12 w-12 items-center justify-center rounded-full">
                <svg
                  xmlns="http://www.w3.org/2000/svg"
                  className="h-6 w-6"
                  fill="none"
                  viewBox="0 0 24 24"
                  stroke="currentColor"
                >
                  <path
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    strokeWidth={2}
                    d="M9.663 17h4.673M12 3v1m6.364 1.636l-.707.707M21 12h-1M4 12H3m3.343-5.657l-.707-.707m2.828 9.9a5 5 0 117.072 0l-.548.547A3.374 3.374 0 0014 18.469V19a2 2 0 11-4 0v-.531c0-.895-.356-1.754-.988-2.386l-.548-.547z"
                  />
                </svg>
              </div>
              <h3 className="mb-2 text-xl font-bold text-white">
                AI-Powered Matches
              </h3>
              <p className="text-gray-300">
                State-of-the-art AI generates realistic match simulations based
                on real player data.
              </p>
            </div>

            <div className="rounded-xl bg-white/10 p-6 backdrop-blur-sm transition-all hover:bg-white/20">
              <div className="bg-primary/20 text-primary mx-auto mb-4 flex h-12 w-12 items-center justify-center rounded-full">
                <svg
                  xmlns="http://www.w3.org/2000/svg"
                  className="h-6 w-6"
                  fill="none"
                  viewBox="0 0 24 24"
                  stroke="currentColor"
                >
                  <path
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    strokeWidth={2}
                    d="M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z"
                  />
                </svg>
              </div>
              <h3 className="mb-2 text-xl font-bold text-white">
                Detailed Statistics
              </h3>
              <p className="text-gray-300">
                Access comprehensive match statistics, player performance data,
                and tactical analysis.
              </p>
            </div>

            <div className="rounded-xl bg-white/10 p-6 backdrop-blur-sm transition-all hover:bg-white/20">
              <div className="bg-primary/20 text-primary mx-auto mb-4 flex h-12 w-12 items-center justify-center rounded-full">
                <svg
                  xmlns="http://www.w3.org/2000/svg"
                  className="h-6 w-6"
                  fill="none"
                  viewBox="0 0 24 24"
                  stroke="currentColor"
                >
                  <path
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    strokeWidth={2}
                    d="M12 6V4m0 2a2 2 0 100 4m0-4a2 2 0 110 4m-6 8a2 2 0 100-4m0 4a2 2 0 110-4m0 4v2m0-6V4m6 6v10m6-2a2 2 0 100-4m0 4a2 2 0 110-4m0 4v2m0-6V4"
                  />
                </svg>
              </div>
              <h3 className="mb-2 text-xl font-bold text-white">
                Customizable Matches
              </h3>
              <p className="text-gray-300">
                Create your own match scenarios with custom teams, players, and
                game conditions.
              </p>
            </div>
          </motion.div>
        </motion.div>
      </div>

      {/* Bottom floating element */}
      <div className="absolute right-0 bottom-5 left-0 z-10 flex justify-center opacity-80">
        <a
          href="#features"
          className="group flex cursor-pointer items-center gap-2 text-sm text-white transition-colors duration-300 hover:text-green-300"
        >
          <span>Scroll to explore</span>
          <svg
            xmlns="http://www.w3.org/2000/svg"
            className="h-4 w-4 animate-bounce group-hover:animate-pulse"
            fill="none"
            viewBox="0 0 24 24"
            stroke="currentColor"
          >
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              strokeWidth={2}
              d="M19 14l-7 7m0 0l-7-7m7 7V3"
            />
          </svg>
        </a>
      </div>
    </section>
  );
}
