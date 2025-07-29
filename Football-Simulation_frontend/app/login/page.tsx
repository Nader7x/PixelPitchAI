'use client';

import dynamic from 'next/dynamic';
import Link from 'next/link';
import { useRouter } from 'next/navigation';
import React, { useState } from 'react';
import authService from '../../Services/AuthenticationService';
import { motion } from 'framer-motion';
import Image from 'next/image';

const ModelViewer = dynamic(() => import('../Components/ModelViewer'), {
  ssr: false,
});

export default function SignIn() {
  const router = useRouter();
  const [error, setError] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [rememberMe, setRememberMe] = useState(false);
  const [showPassword, setShowPassword] = useState(false);

  const handleSignIn = async (event?: React.MouseEvent<HTMLButtonElement>) => {
    // Prevent double-clicking and ensure we're not already loading
    if (isLoading) {
      console.log('Already loading, ignoring click');
      return;
    }

    // Prevent default form submission if this was triggered by a form
    if (event) {
      event.preventDefault();
      event.stopPropagation();
    }

    const email =
      (
        document.getElementById(
          'LoggingEmailAddress'
        ) as HTMLInputElement | null
      )?.value || '';
    const password =
      (document.getElementById('loggingPassword') as HTMLInputElement | null)
        ?.value || '';

    if (!email || !password) {
      setError('Please enter both email and password');
      return;
    }

    setIsLoading(true);
    setError(null);

    try {
      await authService.login({
        email,
        password,
      });

      // Success animation before redirect
      setTimeout(() => {
        router.push('/dashboard');
      }, 500);
    } catch (error: any) {
      console.error('Error during login:', error);
      // Handle different error scenarios
      if (error.response) {
        // The request was made and the server responded with a status code
        // that falls out of the range of 2xx
        if (error.response.status === 401) {
          setError('Invalid email or password');
        } else if (error.response.data && error.response.data.message) {
          setError(error.response.data.message);
        } else {
          setError('Login failed. Please try again.');
        }
      } else if (error.request) {
        // The request was made but no response was received
        setError('No response from server. Please check your connection.');
      } else {
        // Something happened in setting up the request that triggered an Error
        setError('Incorrect Email or Password. Please try again.');
      }
    } finally {
      setIsLoading(false);
    }
  };

  // Handle Enter key press
  const handleKeyDown = (event: React.KeyboardEvent) => {
    if (event.key === 'Enter' && !isLoading) {
      event.preventDefault();
      handleSignIn();
    }
  };

  // Toggle password visibility
  const togglePasswordVisibility = () => {
    setShowPassword(!showPassword);
  };

  return (
    <div className="relative flex min-h-screen items-center justify-center overflow-hidden bg-gradient-to-br from-slate-900 via-blue-900 to-indigo-900">
      {/* Animated background layers */}
      <div className="bg-[url('/images/Stadium dark.png')] absolute inset-0 bg-cover bg-center opacity-50"></div>
      <div className="absolute inset-0 bg-gradient-to-t from-black/70 via-transparent to-black/40"></div>

      {/* Animated football pattern */}
      <motion.div
        className="absolute inset-0 bg-[url('/images/football-pattern.png')] bg-repeat opacity-15"
        animate={{
          backgroundPosition: ['0px 0px', '120px 120px'],
        }}
        transition={{
          duration: 25,
          repeat: Infinity,
          ease: 'linear',
        }}
      ></motion.div>

      {/* Enhanced floating gradient orbs */}
      <motion.div
        className="absolute top-20 left-20 h-80 w-80 rounded-full bg-gradient-to-r from-blue-500/25 to-purple-600/20 blur-3xl"
        animate={{
          x: [0, 60, 0],
          y: [0, -40, 0],
          scale: [1, 1.2, 1],
        }}
        transition={{
          duration: 10,
          repeat: Infinity,
          ease: 'easeInOut',
        }}
      />
      <motion.div
        className="absolute right-20 bottom-20 h-96 w-96 rounded-full bg-gradient-to-r from-purple-500/20 to-indigo-600/25 blur-3xl"
        animate={{
          x: [0, -50, 0],
          y: [0, 50, 0],
          scale: [1, 0.8, 1],
        }}
        transition={{
          duration: 12,
          repeat: Infinity,
          ease: 'easeInOut',
          delay: 2,
        }}
      />
      {/* Additional floating orb */}
      <motion.div
        className="absolute top-1/2 left-1/2 h-64 w-64 -translate-x-1/2 -translate-y-1/2 rounded-full bg-gradient-to-r from-amber-400/15 to-orange-500/20 blur-3xl"
        animate={{
          x: [0, 30, -30, 0],
          y: [0, -25, 25, 0],
          scale: [1, 1.1, 0.9, 1],
        }}
        transition={{
          duration: 15,
          repeat: Infinity,
          ease: 'easeInOut',
          delay: 1,
        }}
      />

      <motion.div
        initial={{ opacity: 0, y: 30, scale: 0.95 }}
        animate={{ opacity: 1, y: 0, scale: 1 }}
        transition={{ duration: 0.8, ease: 'easeOut' }}
        className="relative mx-auto flex w-full max-w-sm overflow-hidden rounded-3xl border border-white/20 bg-white/95 shadow-2xl backdrop-blur-lg lg:max-w-6xl dark:bg-gray-900/95"
        style={{
          boxShadow:
            '0 25px 50px -12px rgba(0, 0, 0, 0.5), 0 0 0 1px rgba(255, 255, 255, 0.1), 0 0 100px rgba(59, 130, 246, 0.15)',
        }}
      >
        {/* Left Side 3D Player with enhanced styling */}
        <div className="relative h-full w-0 overflow-hidden rounded-l-3xl lg:w-3/5">
          {/* Enhanced gradient overlay */}
          <div className="absolute inset-0 z-10 bg-gradient-to-t from-slate-900/90 via-blue-800/60 to-transparent"></div>
          <div className="absolute inset-0 z-10 bg-gradient-to-r from-indigo-900/40 to-transparent"></div>

          <div className="relative h-full w-full">
            <ModelViewer />
          </div>

          {/* Enhanced branding overlay */}
          <motion.div
            className="absolute bottom-12 left-12 z-20 text-white"
            initial={{ opacity: 0, x: -30 }}
            animate={{ opacity: 1, x: 0 }}
            transition={{ delay: 0.5, duration: 0.8 }}
          >
            <motion.h2
              className="mb-4 text-5xl font-black tracking-tight"
              style={{
                background:
                  'linear-gradient(45deg, #ffffff, #fbbf24, #f59e0b, #d97706)',
                WebkitBackgroundClip: 'text',
                WebkitTextFillColor: 'transparent',
                backgroundClip: 'text',
                textShadow: '2px 2px 4px rgba(0,0,0,0.5)',
                backgroundSize: '200% 200%',
              }}
              animate={{
                backgroundPosition: ['0% 50%', '100% 50%', '0% 50%'],
              }}
              transition={{
                duration: 4,
                repeat: Infinity,
                ease: 'easeInOut',
              }}
            >
              PIXELPITCHAI
            </motion.h2>
            <motion.p
              className="max-w-sm text-lg font-semibold text-blue-100/90"
              initial={{ opacity: 0, y: 20 }}
              animate={{ opacity: 1, y: 0 }}
              transition={{ delay: 0.7, duration: 0.6 }}
            >
              Experience the beautiful game like never before with cutting-edge
              simulation technology.
            </motion.p>

            {/* Animated stats */}
            <motion.div
              className="mt-6 flex space-x-6"
              initial={{ opacity: 0 }}
              animate={{ opacity: 1 }}
              transition={{ delay: 1, duration: 0.6 }}
            >
              <div className="text-center">
                <div className="text-2xl font-bold text-white">300+</div>
                <div className="text-xs text-blue-200">Players</div>
              </div>
              <div className="text-center">
                <div className="text-2xl font-bold text-white">50+</div>
                <div className="text-xs text-blue-200">Teams</div>
              </div>
              <div className="text-center">
                <div className="text-2xl font-bold text-white">1000+</div>
                <div className="text-xs text-blue-200">Matches</div>
              </div>
            </motion.div>
          </motion.div>

          {/* Enhanced floating footballs animation */}
          <motion.div
            className="absolute top-20 right-20 z-10 h-24 w-24"
            animate={{
              y: [0, -20, 0],
              rotate: [0, 15, -15, 0],
              scale: [1, 1.1, 1],
            }}
            transition={{
              duration: 4,
              repeat: Infinity,
              ease: 'easeInOut',
            }}
          >
            <div className="flex h-24 w-24 items-center justify-center rounded-full border border-white/20 bg-gradient-to-br from-white/30 to-blue-200/30 shadow-xl backdrop-blur-sm">
              <motion.div
                animate={{ rotate: [0, 360] }}
                transition={{ duration: 8, repeat: Infinity, ease: 'linear' }}
              >
                ⚽
              </motion.div>
            </div>
          </motion.div>

          {/* Additional floating element */}
          <motion.div
            className="absolute top-40 right-32 z-10 h-16 w-16"
            animate={{
              y: [0, -15, 0],
              x: [0, 10, 0],
              rotate: [0, -10, 0],
            }}
            transition={{
              duration: 3,
              repeat: Infinity,
              ease: 'easeInOut',
              delay: 1,
            }}
          >
            <div className="flex h-16 w-16 items-center justify-center rounded-full border border-purple-200/30 bg-gradient-to-br from-purple-300/20 to-white/20 backdrop-blur-sm">
              <motion.div
                animate={{
                  scale: [1, 1.2, 1],
                  rotate: [0, 180, 360],
                }}
                transition={{
                  duration: 4,
                  repeat: Infinity,
                  ease: 'easeInOut',
                }}
              >
                🏆
              </motion.div>
            </div>
          </motion.div>

          {/* Animated particles */}
          {[...Array(8)].map((_, i) => (
            <motion.div
              key={i}
              className="absolute h-2 w-2 rounded-full bg-gradient-to-r from-blue-300/60 to-purple-300/60"
              style={{
                top: `${20 + i * 8}%`,
                right: `${15 + (i % 3) * 5}%`,
              }}
              animate={{
                y: [0, -25, 0],
                opacity: [0.4, 1, 0.4],
                scale: [1, 1.8, 1],
              }}
              transition={{
                duration: 2.5 + i * 0.4,
                repeat: Infinity,
                ease: 'easeInOut',
                delay: i * 0.2,
              }}
            />
          ))}
        </div>

        {/* Right Side Form with enhanced football styling */}
        <div className="relative w-full px-10 py-12 md:px-12 lg:w-2/5">
          {/* Enhanced background pattern with subtle football texture */}
          <div className="pointer-events-none absolute inset-0 rounded-r-3xl bg-gradient-to-br from-blue-50/40 via-white/30 to-purple-50/30"></div>
          <div
            className="pointer-events-none absolute inset-0 rounded-r-3xl opacity-5"
            style={{
              backgroundImage: `url("data:image/svg+xml,%3Csvg width='60' height='60' viewBox='0 0 60 60' xmlns='http://www.w3.org/2000/svg'%3E%3Cg fill='none' fill-rule='evenodd'%3E%3Cg fill='%23000000' fill-opacity='0.1'%3E%3Ccircle cx='30' cy='30' r='4'/%3E%3C/g%3E%3C/g%3E%3C/svg%3E")`,
              backgroundSize: '30px 30px',
            }}
          ></div>{' '}
          <motion.div
            initial={{ scale: 0.8, opacity: 0 }}
            animate={{ scale: 1, opacity: 1 }}
            transition={{ delay: 0.3, duration: 0.6, ease: 'easeOut' }}
            className="relative mx-auto flex justify-center"
          >
            {/* Enhanced football logo - Made clickable */}
            <Link href="/" className="cursor-pointer">
              <motion.div
                className="relative h-28 w-28"
                whileHover={{ scale: 1.1, rotate: 360 }}
                transition={{ duration: 0.6 }}
              >
                <div className="flex h-28 w-28 items-center justify-center rounded-full border-4 border-white/50 bg-gradient-to-br from-blue-500 to-purple-600 shadow-xl">
                  <Image
                    src="/logos/PixelPitch.png"
                    alt="PixelPitch Logo"
                    width={124}
                    height={124}
                    className="rounded-full object-cover"
                  />
                </div>
                {/* Animated ring */}
                <motion.div
                  className="absolute inset-0 rounded-full border-2 border-blue-400/50"
                  animate={{ scale: [1, 1.3, 1], opacity: [0.5, 0, 0.5] }}
                  transition={{ duration: 2, repeat: Infinity }}
                />
              </motion.div>
            </Link>
          </motion.div>
          <motion.h1
            initial={{ opacity: 0, y: 20 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ delay: 0.4, duration: 0.6 }}
            className="mt-6 bg-gradient-to-r from-blue-600 to-purple-600 bg-clip-text text-center text-3xl font-bold text-transparent dark:from-blue-400 dark:to-purple-400"
          >
            Welcome Back!
          </motion.h1>
          <motion.p
            initial={{ opacity: 0, y: 10 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ delay: 0.5, duration: 0.5 }}
            className="mt-2 text-center text-sm text-gray-600 dark:text-gray-400"
          >
            Enter the arena and lead your team to victory
          </motion.p>
          <motion.p
            initial={{ opacity: 0, y: 20 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ delay: 0.5, duration: 0.6 }}
            className="mt-3 text-center text-base text-gray-600 dark:text-gray-300"
          >
            Sign in to continue your football journey
          </motion.p>
          {/* Enhanced error message with animation */}
          {error && (
            <motion.div
              initial={{ opacity: 0, y: -20, scale: 0.9 }}
              animate={{ opacity: 1, y: 0, scale: 1 }}
              exit={{ opacity: 0, y: -20, scale: 0.9 }}
              className="mt-6 rounded-xl border-l-4 border-red-500 bg-gradient-to-r from-red-50 to-red-100/50 p-4 text-sm text-red-700 shadow-lg backdrop-blur-sm"
            >
              <div className="flex items-center">
                <motion.svg
                  xmlns="http://www.w3.org/2000/svg"
                  className="mr-3 h-6 w-6"
                  viewBox="0 0 20 20"
                  fill="currentColor"
                  animate={{ rotate: [0, 10, -10, 0] }}
                  transition={{ duration: 0.5 }}
                >
                  <path
                    fillRule="evenodd"
                    d="M8.257 3.099c.765-1.36 2.722-1.36 3.486 0l5.58 9.92c.75 1.334-.213 2.98-1.742 2.98H4.42c-1.53 0-2.493-1.646-1.743-2.98l5.58-9.92zM11 13a1 1 0 11-2 0 1 1 0 012 0zm-1-8a1 1 0 00-1 1v3a1 1 0 002 0V6a1 1 0 00-1-1z"
                    clipRule="evenodd"
                  />
                </motion.svg>
                <span className="font-medium">{error}</span>
              </div>
            </motion.div>
          )}
          {/* Login Form */}
          <form
            onSubmit={(e) => {
              e.preventDefault();
              handleSignIn();
            }}
          >
            {/* Enhanced Email Input with floating label and icons */}
            <motion.div
              className="relative mt-8"
              initial={{ opacity: 0, x: -20 }}
              animate={{ opacity: 1, x: 0 }}
              transition={{ delay: 0.6, duration: 0.6 }}
            >
              <div className="relative">
                <motion.div
                  className="absolute top-1/2 left-4 -translate-y-1/2 transform text-blue-500"
                  whileHover={{ scale: 1.1 }}
                >
                  <svg
                    className="h-5 w-5"
                    fill="none"
                    stroke="currentColor"
                    viewBox="0 0 24 24"
                  >
                    <path
                      strokeLinecap="round"
                      strokeLinejoin="round"
                      strokeWidth={2}
                      d="M16 12a4 4 0 10-8 0 4 4 0 008 0zm0 0v1.5a2.5 2.5 0 005 0V12a9 9 0 10-9 9m4.5-1.206a8.959 8.959 0 01-4.5 1.207"
                    />
                  </svg>
                </motion.div>
                <input
                  id="LoggingEmailAddress"
                  className="peer w-full rounded-2xl border-2 border-gray-200 bg-white/90 py-4 pr-4 pl-12 text-gray-700 placeholder-transparent backdrop-blur-sm transition-all duration-300 hover:border-blue-300 hover:shadow-lg hover:shadow-blue-500/10 focus:border-blue-500 focus:bg-white focus:ring-4 focus:ring-blue-500/20 focus:outline-none dark:border-gray-600 dark:bg-gray-700/80 dark:text-gray-200 dark:focus:border-blue-400"
                  type="email"
                  onKeyDown={handleKeyDown}
                  placeholder="Email Address"
                />
                <label
                  htmlFor="LoggingEmailAddress"
                  className="absolute -top-3 left-10 bg-white px-2 text-sm font-medium text-blue-600 transition-all peer-placeholder-shown:top-4 peer-placeholder-shown:left-12 peer-placeholder-shown:text-base peer-placeholder-shown:text-gray-400 peer-focus:-top-3 peer-focus:left-10 peer-focus:text-sm peer-focus:text-blue-600 dark:bg-gray-800 dark:text-blue-400 dark:peer-focus:text-blue-400"
                >
                  Email Address
                </label>
              </div>
            </motion.div>

            {/* Enhanced Password Input with floating label and toggle visibility */}
            <motion.div
              className="relative mt-6"
              initial={{ opacity: 0, x: -20 }}
              animate={{ opacity: 1, x: 0 }}
              transition={{ delay: 0.7, duration: 0.6 }}
            >
              <div className="relative">
                <motion.div
                  className="absolute top-1/2 left-4 -translate-y-1/2 transform text-blue-500"
                  whileHover={{ scale: 1.1 }}
                >
                  <svg
                    className="h-5 w-5"
                    fill="none"
                    stroke="currentColor"
                    viewBox="0 0 24 24"
                  >
                    <path
                      strokeLinecap="round"
                      strokeLinejoin="round"
                      strokeWidth={2}
                      d="M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z"
                    />
                  </svg>
                </motion.div>
                <input
                  id="loggingPassword"
                  className="peer w-full rounded-2xl border-2 border-gray-200 bg-white/90 py-4 pr-14 pl-12 text-gray-700 placeholder-transparent backdrop-blur-sm transition-all duration-300 hover:border-blue-300 hover:shadow-lg hover:shadow-blue-500/10 focus:border-blue-500 focus:bg-white focus:ring-4 focus:ring-blue-500/20 focus:outline-none dark:border-gray-600 dark:bg-gray-700/80 dark:text-gray-200 dark:focus:border-blue-400"
                  type={showPassword ? 'text' : 'password'}
                  onKeyDown={handleKeyDown}
                  placeholder="Password"
                />
                <label
                  htmlFor="loggingPassword"
                  className="absolute -top-3 left-10 bg-white px-2 text-sm font-medium text-blue-600 transition-all peer-placeholder-shown:top-4 peer-placeholder-shown:left-12 peer-placeholder-shown:text-base peer-placeholder-shown:text-gray-400 peer-focus:-top-3 peer-focus:left-10 peer-focus:text-sm peer-focus:text-blue-600 dark:bg-gray-800 dark:text-blue-400 dark:peer-focus:text-blue-400"
                >
                  Password
                </label>
                <motion.button
                  type="button"
                  onClick={togglePasswordVisibility}
                  className="absolute top-1/2 right-4 -translate-y-1/2 transform text-gray-500 transition-colors hover:text-blue-500"
                  whileHover={{ scale: 1.1 }}
                  whileTap={{ scale: 0.9 }}
                >
                  <motion.div
                    animate={{ rotate: showPassword ? 180 : 0 }}
                    transition={{ duration: 0.3 }}
                  >
                    {showPassword ? (
                      <svg
                        className="h-5 w-5"
                        fill="none"
                        stroke="currentColor"
                        viewBox="0 0 24 24"
                      >
                        <path
                          strokeLinecap="round"
                          strokeLinejoin="round"
                          strokeWidth={2}
                          d="M13.875 18.825A10.05 10.05 0 0112 19c-4.478 0-8.268-2.943-9.543-7a9.97 9.97 0 011.563-3.029m5.858.908a3 3 0 114.243 4.243M9.878 9.878l4.242 4.242M9.878 9.878L3 3m6.878 6.878L21 21"
                        />
                      </svg>
                    ) : (
                      <svg
                        className="h-5 w-5"
                        fill="none"
                        stroke="currentColor"
                        viewBox="0 0 24 24"
                      >
                        <path
                          strokeLinecap="round"
                          strokeLinejoin="round"
                          strokeWidth={2}
                          d="M15 12a3 3 0 11-6 0 3 3 0 016 0z"
                        />
                        <path
                          strokeLinecap="round"
                          strokeLinejoin="round"
                          strokeWidth={2}
                          d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z"
                        />
                      </svg>
                    )}
                  </motion.div>
                </motion.button>
              </div>
            </motion.div>

            {/* Remember me checkbox and forgot password */}
            <motion.div
              className="mt-4 flex items-center justify-between"
              initial={{ opacity: 0, y: 10 }}
              animate={{ opacity: 1, y: 0 }}
              transition={{ delay: 0.8, duration: 0.5 }}
            >
              <div className="flex items-center space-x-3">
                <div className="relative flex items-center">
                  <input
                    id="remember-me"
                    name="remember-me"
                    type="checkbox"
                    className="peer relative h-5 w-5 cursor-pointer appearance-none rounded border-2 border-gray-300 bg-white transition-all duration-200 checked:border-blue-500 checked:bg-blue-500 hover:border-blue-400 focus:ring-2 focus:ring-blue-500 focus:ring-offset-1 dark:border-gray-600 dark:bg-gray-700 dark:checked:border-blue-400 dark:checked:bg-blue-400"
                    checked={rememberMe}
                    onChange={(e) => setRememberMe(e.target.checked)}
                  />
                  <motion.div
                    className="pointer-events-none absolute inset-0 flex items-center justify-center text-white"
                    initial={false}
                    animate={{
                      scale: rememberMe ? 1 : 0,
                      opacity: rememberMe ? 1 : 0,
                    }}
                    transition={{ duration: 0.2 }}
                  >
                    <svg
                      className="h-3 w-3"
                      fill="none"
                      stroke="currentColor"
                      viewBox="0 0 24 24"
                    >
                      <path
                        strokeLinecap="round"
                        strokeLinejoin="round"
                        strokeWidth={3}
                        d="M5 13l4 4L19 7"
                      />
                    </svg>
                  </motion.div>
                </div>
                <label
                  htmlFor="remember-me"
                  className="cursor-pointer text-sm font-medium text-gray-700 transition-colors select-none hover:text-blue-600 dark:text-gray-300 dark:hover:text-blue-400"
                >
                  Remember me
                </label>
              </div>
              <Link
                href="/forgot-password"
                className="text-sm text-blue-600 transition-colors hover:text-blue-700 hover:underline dark:text-blue-400"
              >
                Forgot password?
              </Link>
            </motion.div>

            {/* Sign In Button with football theme */}
            <div className="mt-6">
              <motion.button
                className={`relative w-full rounded-2xl px-6 py-4 text-sm font-medium text-white transition-all duration-200 focus:ring-4 focus:ring-blue-500/50 focus:outline-none ${
                  isLoading
                    ? 'cursor-not-allowed bg-gray-600 opacity-80'
                    : 'bg-gradient-to-r from-blue-600 to-purple-600 shadow-lg hover:from-blue-700 hover:to-purple-700 hover:shadow-xl hover:shadow-blue-500/30'
                }`}
                onClick={handleSignIn}
                disabled={isLoading}
                whileHover={!isLoading ? { scale: 1.02 } : {}}
                whileTap={!isLoading ? { scale: 0.98 } : {}}
                style={{ pointerEvents: isLoading ? 'none' : 'auto' }}
              >
                {isLoading ? (
                  <div className="flex items-center justify-center">
                    <svg
                      className="mr-3 -ml-1 h-5 w-5 animate-spin text-white"
                      xmlns="http://www.w3.org/2000/svg"
                      fill="none"
                      viewBox="0 0 24 24"
                    >
                      <circle
                        className="opacity-25"
                        cx="12"
                        cy="12"
                        r="10"
                        stroke="currentColor"
                        strokeWidth="4"
                      ></circle>
                      <path
                        className="opacity-75"
                        fill="currentColor"
                        d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"
                      ></path>
                    </svg>
                    Signing In...
                  </div>
                ) : (
                  'Sign In'
                )}
              </motion.button>
            </div>
          </form>{' '}
          {/* Sign Up Link with animation */}
          <motion.div
            className="relative z-50 mt-8 text-center"
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            transition={{ delay: 0.6, duration: 0.5 }}
          >
            <span className="text-sm text-gray-600 dark:text-gray-300">
              Don't have an account?{' '}
            </span>
            <Link
              href="/register"
              className="relative z-50 text-sm font-medium text-blue-600 transition-colors hover:text-blue-700 hover:underline dark:text-blue-400"
            >
              Create an account
            </Link>
          </motion.div>
          {/* Back to Home Button */}
          <motion.div
            className="relative z-50 mt-4 text-center"
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            transition={{ delay: 0.7, duration: 0.5 }}
          >
            <Link
              href="/"
              className="inline-flex items-center text-sm font-medium text-gray-600 transition-colors hover:text-blue-600 dark:text-gray-400 dark:hover:text-blue-400"
            >
              <motion.svg
                className="mr-2 h-4 w-4"
                fill="none"
                stroke="currentColor"
                viewBox="0 0 24 24"
                whileHover={{ x: -2 }}
                transition={{ duration: 0.2 }}
              >
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth={2}
                  d="M10 19l-7-7m0 0l7-7m-7 7h18"
                />
              </motion.svg>
              Back to Home
            </Link>
          </motion.div>
        </div>
      </motion.div>
    </div>
  );
}
