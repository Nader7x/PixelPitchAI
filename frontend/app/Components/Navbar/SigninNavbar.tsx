'use client';

import Image from 'next/image';
import Link from 'next/link';
import authService from '@/Services/AuthenticationService';
import { useState, useEffect } from 'react';
import { motion, AnimatePresence } from 'framer-motion';

export default function SigninNavbar() {
  const [isScrolled, setIsScrolled] = useState(false);
  const [isMenuOpen, setIsMenuOpen] = useState(false);
  const [isAuthenticated, setIsAuthenticated] = useState(false);

  useEffect(() => {
    // Check authentication status
    setIsAuthenticated(authService.isAuthenticated());

    // Add scroll event listener
    const handleScroll = () => {
      if (window.scrollY > 10) {
        setIsScrolled(true);
      } else {
        setIsScrolled(false);
      }
    };

    window.addEventListener('scroll', handleScroll);
    return () => window.removeEventListener('scroll', handleScroll);
  }, []);

  const toggleMenu = () => {
    setIsMenuOpen(!isMenuOpen);
  };

  return (
    <motion.nav
      className={`z-50 w-full ${isScrolled ? 'bg-black/80 shadow-lg backdrop-blur-md' : 'bg-transparent'} fixed top-0 left-0 transition-all duration-300`}
      initial={{ y: -100 }}
      animate={{ y: 0 }}
      transition={{ duration: 0.5 }}
    >
      <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
        <div className="flex h-16 items-center justify-between md:h-20">
          {/* Logo and Brand */}
          <div className="flex items-center">
            <Link href="/" className="flex items-center">
              <div className="relative mr-3 h-10 w-10">
                <Image
                  src="/logos/PixelPitch.png"
                  fill
                  className="object-contain"
                  alt="Logo"
                />
              </div>
              <div>
                <h1 className="text-xl font-bold tracking-tight text-white">
                  PixelPitchAI
                </h1>
                <p className="-mt-1 text-xs text-gray-300">
                  AI-Powered Match Engine
                </p>
              </div>
            </Link>
          </div>

          {/* Desktop Navigation Links */}
          <div className="hidden items-center space-x-8 md:flex">
            <Link
              href="/"
              className="text-gray-300 transition-colors duration-200 hover:text-white"
            >
              Home
            </Link>

            <Link
              href="/about"
              className="text-gray-300 transition-colors duration-200 hover:text-white"
            >
              About
            </Link>

            {/* Auth Button */}
            <Link href={isAuthenticated ? '/dashboard' : '/login'}>
              <motion.button
                whileHover={{ scale: 1.05 }}
                whileTap={{ scale: 0.95 }}
                className={`${
                  isAuthenticated
                    ? 'bg-green-600 hover:bg-green-700'
                    : 'bg-blue-600 hover:bg-blue-700'
                } flex items-center gap-2 rounded-full px-6 py-2 text-sm font-medium text-white shadow-md`}
              >
                {isAuthenticated ? (
                  <>
                    <svg
                      xmlns="http://www.w3.org/2000/svg"
                      className="h-4 w-4"
                      fill="none"
                      viewBox="0 0 24 24"
                      stroke="currentColor"
                    >
                      <path
                        strokeLinecap="round"
                        strokeLinejoin="round"
                        strokeWidth={2}
                        d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z"
                      />
                    </svg>
                    Dashboard
                  </>
                ) : (
                  <>
                    <svg
                      xmlns="http://www.w3.org/2000/svg"
                      className="h-4 w-4"
                      fill="none"
                      viewBox="0 0 24 24"
                      stroke="currentColor"
                    >
                      <path
                        strokeLinecap="round"
                        strokeLinejoin="round"
                        strokeWidth={2}
                        d="M11 16l-4-4m0 0l4-4m-4 4h14m-5 4v1a3 3 0 01-3 3H6a3 3 0 01-3-3V7a3 3 0 013-3h7a3 3 0 013 3v1"
                      />
                    </svg>
                    Sign In
                  </>
                )}
              </motion.button>
            </Link>
          </div>

          {/* Mobile menu button */}
          <div className="md:hidden">
            <button
              onClick={toggleMenu}
              className="text-gray-300 hover:text-white focus:outline-none"
            >
              {isMenuOpen ? (
                <svg
                  className="h-6 w-6"
                  fill="none"
                  stroke="currentColor"
                  viewBox="0 0 24 24"
                >
                  <path
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    strokeWidth="2"
                    d="M6 18L18 6M6 6l12 12"
                  />
                </svg>
              ) : (
                <svg
                  className="h-6 w-6"
                  fill="none"
                  stroke="currentColor"
                  viewBox="0 0 24 24"
                >
                  <path
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    strokeWidth="2"
                    d="M4 6h16M4 12h16M4 18h16"
                  />
                </svg>
              )}
            </button>
          </div>
        </div>
      </div>

      {/* Mobile menu */}
      <AnimatePresence>
        {isMenuOpen && (
          <motion.div
            className="md:hidden"
            initial={{ opacity: 0, height: 0 }}
            animate={{ opacity: 1, height: 'auto' }}
            exit={{ opacity: 0, height: 0 }}
            transition={{ duration: 0.3 }}
          >
            <div className="space-y-3 bg-black/90 px-2 pt-2 pb-4 backdrop-blur-md">
              <Link
                href="/"
                onClick={() => setIsMenuOpen(false)}
                className="block rounded-lg px-4 py-2 text-gray-200 hover:bg-gray-800"
              >
                Home
              </Link>
              <Link
                href="/features"
                onClick={() => setIsMenuOpen(false)}
                className="block rounded-lg px-4 py-2 text-gray-200 hover:bg-gray-800"
              >
                Features
              </Link>
              <Link
                href="/teams"
                onClick={() => setIsMenuOpen(false)}
                className="block rounded-lg px-4 py-2 text-gray-200 hover:bg-gray-800"
              >
                Teams
              </Link>
              <Link
                href="/about"
                onClick={() => setIsMenuOpen(false)}
                className="block rounded-lg px-4 py-2 text-gray-200 hover:bg-gray-800"
              >
                About
              </Link>
              <div className="pt-2">
                <Link
                  href={isAuthenticated ? '/dashboard' : '/login'}
                  onClick={() => setIsMenuOpen(false)}
                  className={`block w-full rounded-lg px-4 py-2 text-center font-medium ${
                    isAuthenticated
                      ? 'bg-green-600 text-white'
                      : 'bg-blue-600 text-white'
                  }`}
                >
                  {isAuthenticated ? 'Dashboard' : 'Sign In'}
                </Link>
              </div>
            </div>
          </motion.div>
        )}
      </AnimatePresence>
    </motion.nav>
  );
}
