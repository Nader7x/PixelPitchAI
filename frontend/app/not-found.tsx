'use client';

import { useState, useEffect } from 'react';
import Link from 'next/link';
import { useRouter } from 'next/navigation';
import { motion } from 'framer-motion';
import {
  Home,
  ArrowLeft,
  Search,
  Users,
  Calendar,
  Trophy,
  MapPin,
  Settings,
  Mail,
  RotateCcw,
  TrendingUp,
  Clock,
  AlertTriangle,
  Zap,
} from 'lucide-react';
import { useSettings } from './contexts/EnhancedSettingsContext';

export default function NotFoundPage() {
  const router = useRouter();
  const [isMounted, setIsMounted] = useState(false);
  const [currentUrl, setCurrentUrl] = useState('');
  const [showErrorReport, setShowErrorReport] = useState(false);
  const [reportSent, setReportSent] = useState(false);

  // Get dark mode from settings
  const { isDarkMode, playSound } = useSettings();

  useEffect(() => {
    setIsMounted(true);
    setCurrentUrl(window.location.pathname);
  }, []);

  // Popular pages for quick navigation
  const popularPages = [
    {
      name: 'Dashboard',
      href: '/dashboard',
      icon: Home,
      description: 'Your main control center',
    },
    {
      name: 'Teams',
      href: '/teams',
      icon: Users,
      description: 'Browse all football teams',
    },
    {
      name: 'Players',
      href: '/players',
      icon: Users,
      description: 'Discover player profiles',
    },
    {
      name: 'Schedule',
      href: '/schedule',
      icon: Calendar,
      description: 'Upcoming matches',
    },
    {
      name: 'Stadiums',
      href: '/stadiums',
      icon: MapPin,
      description: 'Explore stadiums worldwide',
    },
    {
      name: 'Match Simulation',
      href: '/matchsimulation',
      icon: Zap,
      description: 'Live match experience',
    },
  ];

  // Suggested actions based on common 404 scenarios
  const suggestedActions = [
    {
      title: 'Search for Content',
      description: 'Find players, teams, or matches',
      icon: Search,
      action: () => router.push('/search'),
      color: 'blue',
    },
    {
      title: 'Go Back',
      description: 'Return to the previous page',
      icon: ArrowLeft,
      action: () => router.back(),
      color: 'gray',
    },
    {
      title: 'Home Page',
      description: 'Start from the beginning',
      icon: Home,
      action: () => router.push('/'),
      color: 'green',
    },
    {
      title: 'Try Again',
      description: 'Refresh the current page',
      icon: RotateCcw,
      action: () => window.location.reload(),
      color: 'orange',
    },
  ];

  const handleErrorReport = () => {
    // Here you could integrate with your error reporting service
    setReportSent(true);
    playSound?.('success');
    setTimeout(() => {
      setShowErrorReport(false);
      setReportSent(false);
    }, 2000);
  };

  const getSmartSuggestions = () => {
    const url = currentUrl.toLowerCase();
    const suggestions = [];

    if (url.includes('player')) {
      suggestions.push({ text: 'Browse all players', href: '/players' });
    }
    if (url.includes('team')) {
      suggestions.push({ text: 'Explore teams', href: '/teams' });
    }
    if (url.includes('match')) {
      suggestions.push({ text: 'View match schedule', href: '/schedule' });
      suggestions.push({
        text: 'Start match simulation',
        href: '/matchsimulation',
      });
    }
    if (url.includes('stadium')) {
      suggestions.push({ text: 'Discover stadiums', href: '/stadiums' });
    }

    return suggestions;
  };

  const smartSuggestions = getSmartSuggestions();

  return (
    <div
      className={`min-h-screen transition-colors duration-300 ${
        isMounted && isDarkMode
          ? 'bg-gradient-to-br from-gray-900 via-gray-800 to-gray-900'
          : 'bg-gradient-to-br from-green-50 via-white to-blue-50'
      }`}
      suppressHydrationWarning
    >
      {/* Background Elements */}
      <div className="absolute inset-0 overflow-hidden">
        {/* Stadium silhouette */}
        <div
          className={`absolute right-0 bottom-0 left-0 h-32 opacity-10 ${
            isMounted && isDarkMode ? 'bg-gray-700' : 'bg-green-900'
          }`}
          style={{
            clipPath:
              'polygon(0 100%, 10% 80%, 20% 85%, 30% 75%, 40% 80%, 50% 70%, 60% 80%, 70% 75%, 80% 85%, 90% 80%, 100% 100%)',
          }}
        ></div>

        {/* Floating football elements */}
        <motion.div
          animate={{ y: [-10, 10, -10], rotate: [0, 360] }}
          transition={{ duration: 8, repeat: Infinity, ease: 'easeInOut' }}
          className={`absolute top-20 right-20 h-16 w-16 rounded-full ${
            isMounted && isDarkMode ? 'bg-gray-700' : 'bg-green-100'
          } opacity-20`}
        />
        <motion.div
          animate={{ y: [10, -10, 10], rotate: [360, 0] }}
          transition={{ duration: 10, repeat: Infinity, ease: 'easeInOut' }}
          className={`absolute top-40 left-32 h-12 w-12 rounded-full ${
            isMounted && isDarkMode ? 'bg-gray-600' : 'bg-blue-100'
          } opacity-30`}
        />
      </div>

      <div className="relative z-10 flex min-h-screen flex-col items-center justify-center px-4 py-16">
        <div className="w-full max-w-4xl text-center">
          {/* Main 404 Content */}
          <motion.div
            initial={{ opacity: 0, y: -50 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.6 }}
            className="mb-12"
          >
            {/* 404 Number */}
            <div className="mb-6">
              <span
                className={`bg-gradient-to-r bg-clip-text text-8xl font-bold text-transparent md:text-9xl ${
                  isMounted && isDarkMode
                    ? 'from-red-400 via-orange-400 to-yellow-400'
                    : 'from-red-500 via-orange-500 to-yellow-500'
                }`}
                suppressHydrationWarning
              >
                404
              </span>
            </div>

            {/* Title and Description */}
            <h1
              className={`mb-4 text-3xl font-bold md:text-4xl ${
                isMounted && isDarkMode ? 'text-white' : 'text-gray-900'
              }`}
              suppressHydrationWarning
            >
              Oops! You've Kicked the Ball Out of Bounds
            </h1>
            <p
              className={`mb-2 text-lg md:text-xl ${
                isMounted && isDarkMode ? 'text-gray-300' : 'text-gray-600'
              }`}
              suppressHydrationWarning
            >
              The page you're looking for has been transferred to another
              league.
            </p>
            {currentUrl && (
              <p
                className={`font-mono text-sm ${
                  isMounted && isDarkMode ? 'text-gray-500' : 'text-gray-400'
                }`}
                suppressHydrationWarning
              >
                Path: <span className="font-semibold">{currentUrl}</span>
              </p>
            )}
          </motion.div>

          {/* Quick Actions */}
          <motion.div
            initial={{ opacity: 0, y: 50 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.6, delay: 0.2 }}
            className="mb-12"
          >
            <h2
              className={`mb-6 text-xl font-semibold ${
                isMounted && isDarkMode ? 'text-gray-200' : 'text-gray-800'
              }`}
              suppressHydrationWarning
            >
              What would you like to do?
            </h2>
            <div className="mb-8 grid grid-cols-1 gap-4 md:grid-cols-2 lg:grid-cols-4">
              {suggestedActions.map((action, index) => (
                <motion.button
                  key={action.title}
                  onClick={() => {
                    playSound?.('click');
                    action.action();
                  }}
                  whileHover={{ scale: 1.05 }}
                  whileTap={{ scale: 0.95 }}
                  className={`rounded-xl border-2 p-6 transition-all duration-200 ${
                    isMounted && isDarkMode
                      ? 'hover:bg-gray-750 border-gray-700 bg-gray-800 hover:border-gray-600'
                      : 'border-gray-200 bg-white hover:border-gray-300 hover:shadow-md'
                  }`}
                  suppressHydrationWarning
                >
                  <action.icon
                    className={`mx-auto mb-3 h-8 w-8 ${
                      action.color === 'blue'
                        ? 'text-blue-500'
                        : action.color === 'green'
                          ? 'text-green-500'
                          : action.color === 'orange'
                            ? 'text-orange-500'
                            : 'text-gray-500'
                    }`}
                  />
                  <h3
                    className={`mb-2 font-semibold ${
                      isMounted && isDarkMode ? 'text-white' : 'text-gray-900'
                    }`}
                    suppressHydrationWarning
                  >
                    {action.title}
                  </h3>
                  <p
                    className={`text-sm ${
                      isMounted && isDarkMode
                        ? 'text-gray-400'
                        : 'text-gray-600'
                    }`}
                    suppressHydrationWarning
                  >
                    {action.description}
                  </p>
                </motion.button>
              ))}
            </div>
          </motion.div>

          {/* Popular Pages */}
          <motion.div
            initial={{ opacity: 0, y: 50 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.6, delay: 0.4 }}
            className="mb-12"
          >
            <h2
              className={`mb-6 text-xl font-semibold ${
                isMounted && isDarkMode ? 'text-gray-200' : 'text-gray-800'
              }`}
              suppressHydrationWarning
            >
              Popular Destinations
            </h2>
            <div className="grid grid-cols-1 gap-4 md:grid-cols-2 lg:grid-cols-3">
              {popularPages.map((page, index) => (
                <Link
                  key={page.name}
                  href={page.href}
                  onClick={() => playSound?.('click')}
                  className={`group rounded-lg border p-4 transition-all duration-200 ${
                    isMounted && isDarkMode
                      ? 'border-gray-700 bg-gray-800 hover:border-green-500'
                      : 'border-gray-200 bg-white hover:border-green-400 hover:shadow-sm'
                  }`}
                  suppressHydrationWarning
                >
                  <div className="flex items-center space-x-3">
                    <page.icon className="h-6 w-6 text-green-500 transition-transform group-hover:scale-110" />
                    <div className="text-left">
                      <h3
                        className={`font-semibold ${
                          isMounted && isDarkMode
                            ? 'text-white group-hover:text-green-400'
                            : 'text-gray-900 group-hover:text-green-600'
                        }`}
                        suppressHydrationWarning
                      >
                        {page.name}
                      </h3>
                      <p
                        className={`text-sm ${
                          isMounted && isDarkMode
                            ? 'text-gray-400'
                            : 'text-gray-600'
                        }`}
                        suppressHydrationWarning
                      >
                        {page.description}
                      </p>
                    </div>
                  </div>
                </Link>
              ))}
            </div>
          </motion.div>

          {/* Smart Suggestions */}
          {smartSuggestions.length > 0 && (
            <motion.div
              initial={{ opacity: 0, y: 50 }}
              animate={{ opacity: 1, y: 0 }}
              transition={{ duration: 0.6, delay: 0.6 }}
              className="mb-12"
            >
              <h2
                className={`mb-6 text-xl font-semibold ${
                  isMounted && isDarkMode ? 'text-gray-200' : 'text-gray-800'
                }`}
                suppressHydrationWarning
              >
                Based on your URL, you might be looking for:
              </h2>
              <div className="flex flex-wrap justify-center gap-3">
                {smartSuggestions.map((suggestion, index) => (
                  <Link
                    key={index}
                    href={suggestion.href}
                    onClick={() => playSound?.('click')}
                    className={`rounded-full border-2 px-4 py-2 transition-all duration-200 ${
                      isMounted && isDarkMode
                        ? 'border-blue-500 bg-blue-900/30 text-blue-300 hover:bg-blue-800/50'
                        : 'border-blue-200 bg-blue-50 text-blue-700 hover:bg-blue-100'
                    }`}
                    suppressHydrationWarning
                  >
                    <TrendingUp className="mr-2 inline h-4 w-4" />
                    {suggestion.text}
                  </Link>
                ))}
              </div>
            </motion.div>
          )}

          {/* Error Reporting */}
          <motion.div
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            transition={{ duration: 0.6, delay: 0.8 }}
            className="text-center"
          >
            {!showErrorReport ? (
              <button
                onClick={() => setShowErrorReport(true)}
                className={`inline-flex items-center px-4 py-2 text-sm transition-colors duration-200 ${
                  isMounted && isDarkMode
                    ? 'text-gray-400 hover:text-gray-300'
                    : 'text-gray-500 hover:text-gray-700'
                }`}
                suppressHydrationWarning
              >
                <AlertTriangle className="mr-2 h-4 w-4" />
                Report this broken link
              </button>
            ) : (
              <div
                className={`inline-block rounded-lg border p-4 ${
                  isMounted && isDarkMode
                    ? 'border-gray-700 bg-gray-800'
                    : 'border-gray-200 bg-white'
                }`}
                suppressHydrationWarning
              >
                {!reportSent ? (
                  <>
                    <p
                      className={`mb-3 text-sm ${
                        isMounted && isDarkMode
                          ? 'text-gray-300'
                          : 'text-gray-600'
                      }`}
                      suppressHydrationWarning
                    >
                      Help us improve by reporting this broken link
                    </p>
                    <div className="flex gap-2">
                      <button
                        onClick={handleErrorReport}
                        className="rounded-lg bg-red-500 px-4 py-2 text-white transition-colors hover:bg-red-600"
                      >
                        Report Issue
                      </button>
                      <button
                        onClick={() => setShowErrorReport(false)}
                        className={`rounded-lg px-4 py-2 transition-colors ${
                          isMounted && isDarkMode
                            ? 'bg-gray-700 text-gray-300 hover:bg-gray-600'
                            : 'bg-gray-200 text-gray-700 hover:bg-gray-300'
                        }`}
                        suppressHydrationWarning
                      >
                        Cancel
                      </button>
                    </div>
                  </>
                ) : (
                  <div className="text-green-500">
                    <p className="text-sm">✓ Thank you for the feedback!</p>
                  </div>
                )}
              </div>
            )}
          </motion.div>
        </div>
      </div>
    </div>
  );
}
