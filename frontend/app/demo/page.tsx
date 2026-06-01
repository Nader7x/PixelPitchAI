'use client';

import { motion } from 'framer-motion';
import Image from 'next/image';
import Link from 'next/link';
import { useState } from 'react';
import SigninNavbar from '@/app/Components/Navbar/SigninNavbar';

export default function Demo() {
  const [currentDemo, setCurrentDemo] = useState<
    'overview' | 'simulation' | 'analytics'
  >('overview');

  const demos = {
    overview: {
      title: 'Platform Overview',
      description:
        'Get a comprehensive look at the PixelPitchAI platform and its capabilities.',
      features: [
        'AI-powered match engine in action',
        'Real-time statistics and analytics',
        'Team management interface',
        'Match customization options',
      ],
    },
    simulation: {
      title: 'Live Match Simulation',
      description:
        'Watch as our AI engine simulates a complete football match with realistic events.',
      features: [
        'Dynamic player movements and decisions',
        'Realistic match events and outcomes',
        'Live commentary and statistics',
        'Tactical analysis and insights',
      ],
    },
    analytics: {
      title: 'Advanced Analytics',
      description:
        'Explore the depth of our statistical analysis and predictive modeling.',
      features: [
        'Comprehensive match statistics',
        'Player performance metrics',
        'Tactical heat maps',
        'Predictive match outcomes',
      ],
    },
  } as const;

  return (
    <div className="min-h-screen bg-gradient-to-br from-slate-50 via-white to-green-50">
      <SigninNavbar />

      {/* Hero Section */}
      <section className="relative overflow-hidden bg-gradient-to-br from-slate-900 via-blue-900 to-indigo-900 pt-20 pb-20">
        {/* Background Pattern */}
        <div className="bg-[url('/images/Stadium dark.png')] absolute inset-0 bg-cover bg-center opacity-30"></div>
        <div className="absolute inset-0 bg-gradient-to-t from-black/70 via-transparent to-black/40"></div>

        <div className="relative mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
          <div className="text-center">
            <motion.div
              initial={{ opacity: 0, scale: 0.8 }}
              animate={{ opacity: 1, scale: 1 }}
              transition={{ duration: 0.8 }}
              className="mx-auto mb-8 flex h-24 w-24 items-center justify-center rounded-full bg-gradient-to-br from-green-500 to-emerald-600 shadow-2xl"
            >
              <svg
                className="h-12 w-12 text-white"
                fill="none"
                stroke="currentColor"
                viewBox="0 0 24 24"
              >
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth={2}
                  d="M14.828 14.828a4 4 0 01-5.656 0M9 10h1m4 0h1M9 16v-2a2 2 0 012-2h2a2 2 0 012 2v2M12 3v1m6.364 1.636l-.707.707M21 12h-1M4 12H3m3.343-5.657l-.707-.707m2.828 9.9a5 5 0 117.072 0l-.548.547A3.374 3.374 0 0014 18.469V19a2 2 0 11-4 0v-.531c0-.895-.356-1.754-.988-2.386l-.548-.547z"
                />
              </svg>
            </motion.div>

            <motion.h1
              initial={{ opacity: 0, y: 30 }}
              animate={{ opacity: 1, y: 0 }}
              transition={{ duration: 0.8, delay: 0.2 }}
              className="mb-6 text-5xl font-bold text-white md:text-6xl lg:text-7xl"
            >
              Experience{' '}
              <span className="bg-gradient-to-r from-green-400 via-emerald-400 to-blue-500 bg-clip-text text-transparent">
                PixelPitchAI
              </span>
            </motion.h1>

            <motion.p
              initial={{ opacity: 0, y: 30 }}
              animate={{ opacity: 1, y: 0 }}
              transition={{ duration: 0.8, delay: 0.4 }}
              className="mx-auto max-w-4xl text-xl leading-relaxed text-blue-100 md:text-2xl"
            >
              See our AI-powered football simulation in action. Watch live
              demos, explore features, and discover what makes PixelPitchAI
              revolutionary.
            </motion.p>

            <motion.div
              initial={{ opacity: 0, y: 30 }}
              animate={{ opacity: 1, y: 0 }}
              transition={{ duration: 0.8, delay: 0.6 }}
              className="mt-10"
            >
              <Link
                href="/register"
                className="inline-flex items-center gap-3 rounded-xl bg-gradient-to-r from-green-500 to-emerald-600 px-8 py-4 text-lg font-bold text-white shadow-2xl transition-all duration-300 hover:scale-105 hover:shadow-green-500/50"
              >
                <svg
                  className="h-6 w-6"
                  fill="none"
                  stroke="currentColor"
                  viewBox="0 0 24 24"
                >
                  <path
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    strokeWidth={2}
                    d="M14.828 14.828a4 4 0 01-5.656 0M9 10h1m4 0h1M9 16v-2a2 2 0 012-2h2a2 2 0 012 2v2M12 3v1m6.364 1.636l-.707.707M21 12h-1M4 12H3m3.343-5.657l-.707-.707m2.828 9.9a5 5 0 117.072 0l-.548.547A3.374 3.374 0 0014 18.469V19a2 2 0 11-4 0v-.531c0-.895-.356-1.754-.988-2.386l-.548-.547z"
                  />
                </svg>
                Try It Free Now
              </Link>
            </motion.div>
          </div>
        </div>
      </section>

      {/* Demo Selection */}
      <section className="py-20">
        <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
          <motion.div
            initial={{ opacity: 0, y: 30 }}
            whileInView={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.8 }}
            viewport={{ once: true }}
            className="mb-16 text-center"
          >
            <h2 className="mb-4 text-4xl font-bold text-gray-900 md:text-5xl">
              Choose Your Demo Experience
            </h2>
            <p className="mx-auto max-w-3xl text-xl text-gray-600">
              Explore different aspects of our platform through interactive
              demonstrations
            </p>
          </motion.div>{' '}
          {/* Demo Navigation */}
          <div className="mb-12 flex flex-wrap justify-center gap-4">
            {(
              Object.entries(demos) as Array<
                [keyof typeof demos, (typeof demos)[keyof typeof demos]]
              >
            ).map(([key, demo]) => (
              <motion.button
                key={key}
                onClick={() => setCurrentDemo(key)}
                className={`rounded-xl px-6 py-3 font-semibold transition-all duration-300 ${
                  currentDemo === key
                    ? 'scale-105 bg-gradient-to-r from-green-500 to-emerald-600 text-white shadow-lg'
                    : 'bg-white text-gray-700 hover:bg-gray-50 hover:shadow-md'
                }`}
                whileHover={{ scale: currentDemo === key ? 1.05 : 1.02 }}
                whileTap={{ scale: 0.98 }}
              >
                {demo.title}
              </motion.button>
            ))}
          </div>
          {/* Demo Content */}
          <div className="grid grid-cols-1 gap-12 lg:grid-cols-2 lg:items-center">
            <motion.div
              key={currentDemo}
              initial={{ opacity: 0, x: -50 }}
              animate={{ opacity: 1, x: 0 }}
              transition={{ duration: 0.6 }}
            >
              <h3 className="mb-6 text-3xl font-bold text-gray-900">
                {demos[currentDemo].title}
              </h3>
              <p className="mb-8 text-lg leading-relaxed text-gray-600">
                {demos[currentDemo].description}
              </p>

              <div className="space-y-4">
                <h4 className="text-xl font-semibold text-gray-900">
                  What you'll see:
                </h4>
                <ul className="space-y-3">
                  {demos[currentDemo].features.map((feature, index) => (
                    <motion.li
                      key={feature}
                      initial={{ opacity: 0, x: -20 }}
                      animate={{ opacity: 1, x: 0 }}
                      transition={{ duration: 0.4, delay: index * 0.1 }}
                      className="flex items-start space-x-3"
                    >
                      <div className="flex h-6 w-6 items-center justify-center rounded-full bg-gradient-to-br from-green-500 to-emerald-600">
                        <svg
                          className="h-3 w-3 text-white"
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
                      </div>
                      <span className="text-gray-700">{feature}</span>
                    </motion.li>
                  ))}
                </ul>
              </div>
            </motion.div>

            <motion.div
              key={`video-${currentDemo}`}
              initial={{ opacity: 0, x: 50 }}
              animate={{ opacity: 1, x: 0 }}
              transition={{ duration: 0.6 }}
              className="relative"
            >
              {/* Demo Video Placeholder */}
              <div className="aspect-video overflow-hidden rounded-2xl bg-gradient-to-br from-slate-800 to-slate-900 shadow-2xl">
                <div className="flex h-full items-center justify-center">
                  <div className="text-center">
                    <motion.div
                      className="mx-auto mb-6 flex h-20 w-20 items-center justify-center rounded-full bg-white/10 backdrop-blur-sm"
                      whileHover={{ scale: 1.1 }}
                      whileTap={{ scale: 0.9 }}
                    >
                      <svg
                        className="h-8 w-8 text-white"
                        fill="currentColor"
                        viewBox="0 0 24 24"
                      >
                        <path d="M8 5v14l11-7z" />
                      </svg>
                    </motion.div>
                    <h4 className="mb-2 text-xl font-bold text-white">
                      {demos[currentDemo].title} Demo
                    </h4>
                    <p className="text-gray-300">
                      Click to watch the interactive demo
                    </p>
                  </div>
                </div>

                {/* Floating Elements */}
                <div className="absolute top-4 right-4">
                  <div className="rounded-lg bg-green-500/20 px-3 py-1 text-sm font-medium text-green-300 backdrop-blur-sm">
                    LIVE DEMO
                  </div>
                </div>
              </div>

              {/* Demo Stats */}
              <motion.div
                initial={{ opacity: 0, y: 20 }}
                animate={{ opacity: 1, y: 0 }}
                transition={{ duration: 0.6, delay: 0.3 }}
                className="mt-6 grid grid-cols-3 gap-4"
              >
                {[
                  { label: 'Duration', value: '5 min' },
                  { label: 'Interactive', value: 'Yes' },
                  { label: 'HD Quality', value: '1080p' },
                ].map((stat, index) => (
                  <div
                    key={stat.label}
                    className="rounded-lg bg-white p-4 text-center shadow-md"
                  >
                    <div className="text-lg font-bold text-gray-900">
                      {stat.value}
                    </div>
                    <div className="text-sm text-gray-600">{stat.label}</div>
                  </div>
                ))}
              </motion.div>
            </motion.div>
          </div>
        </div>
      </section>

      {/* Live Demo Features */}
      <section className="bg-gray-50 py-20">
        <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
          <motion.div
            initial={{ opacity: 0, y: 30 }}
            whileInView={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.8 }}
            viewport={{ once: true }}
            className="mb-16 text-center"
          >
            <h2 className="mb-4 text-4xl font-bold text-gray-900 md:text-5xl">
              Interactive Demo Features
            </h2>
            <p className="mx-auto max-w-3xl text-xl text-gray-600">
              Experience the full power of PixelPitchAI through our interactive
              demos
            </p>
          </motion.div>

          <div className="grid grid-cols-1 gap-8 md:grid-cols-2 lg:grid-cols-3">
            {[
              {
                title: 'Real-Time Simulation',
                description:
                  'Watch matches unfold in real-time with our advanced AI engine.',
                icon: (
                  <svg
                    className="h-8 w-8 text-white"
                    fill="none"
                    stroke="currentColor"
                    viewBox="0 0 24 24"
                  >
                    <path
                      strokeLinecap="round"
                      strokeLinejoin="round"
                      strokeWidth={2}
                      d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z"
                    />
                  </svg>
                ),
                gradient: 'from-blue-500 to-indigo-600',
              },
              {
                title: 'Interactive Controls',
                description:
                  'Pause, rewind, and analyze specific moments in the match.',
                icon: (
                  <svg
                    className="h-8 w-8 text-white"
                    fill="none"
                    stroke="currentColor"
                    viewBox="0 0 24 24"
                  >
                    <path
                      strokeLinecap="round"
                      strokeLinejoin="round"
                      strokeWidth={2}
                      d="M12 6V4m0 2a2 2 0 100 4m0-4a2 2 0 110 4m-6 8a2 2 0 100-4m0 4a2 2 0 100 4m0-4v2m0-6V4m6 6v10m6-2a2 2 0 100-4m0 4a2 2 0 100 4m0-4v2m0-6V4"
                    />
                  </svg>
                ),
                gradient: 'from-green-500 to-emerald-600',
              },
              {
                title: 'Live Analytics',
                description:
                  'See real-time statistics and analytics as the match progresses.',
                icon: (
                  <svg
                    className="h-8 w-8 text-white"
                    fill="none"
                    stroke="currentColor"
                    viewBox="0 0 24 24"
                  >
                    <path
                      strokeLinecap="round"
                      strokeLinejoin="round"
                      strokeWidth={2}
                      d="M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z"
                    />
                  </svg>
                ),
                gradient: 'from-purple-500 to-pink-600',
              },
              {
                title: 'Team Customization',
                description:
                  'Modify team lineups, formations, and tactics in real-time.',
                icon: (
                  <svg
                    className="h-8 w-8 text-white"
                    fill="none"
                    stroke="currentColor"
                    viewBox="0 0 24 24"
                  >
                    <path
                      strokeLinecap="round"
                      strokeLinejoin="round"
                      strokeWidth={2}
                      d="M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0zm6 3a2 2 0 11-4 0 2 2 0 014 0zM7 10a2 2 0 11-4 0 2 2 0 014 0z"
                    />
                  </svg>
                ),
                gradient: 'from-orange-500 to-red-600',
              },
              {
                title: 'Match Insights',
                description:
                  'Get detailed insights into player performance and team dynamics.',
                icon: (
                  <svg
                    className="h-8 w-8 text-white"
                    fill="none"
                    stroke="currentColor"
                    viewBox="0 0 24 24"
                  >
                    <path
                      strokeLinecap="round"
                      strokeLinejoin="round"
                      strokeWidth={2}
                      d="M9.663 17h4.673M12 3v1m6.364 1.636l-.707.707M21 12h-1M4 12H3m3.343-5.657l-.707-.707m2.828 9.9a5 5 0 117.072 0l-.548.547A3.374 3.374 0 0014 18.469V19a2 2 0 11-4 0v-.531c0-.895-.356-1.754-.988-2.386l-.548-.547z"
                    />
                  </svg>
                ),
                gradient: 'from-teal-500 to-cyan-600',
              },
              {
                title: 'Export & Share',
                description:
                  'Export match highlights and share your simulations with others.',
                icon: (
                  <svg
                    className="h-8 w-8 text-white"
                    fill="none"
                    stroke="currentColor"
                    viewBox="0 0 24 24"
                  >
                    <path
                      strokeLinecap="round"
                      strokeLinejoin="round"
                      strokeWidth={2}
                      d="M8.684 13.342C8.886 12.938 9 12.482 9 12c0-.482-.114-.938-.316-1.342m0 2.684a3 3 0 110-2.684m0 2.684l6.632 3.316m-6.632-6l6.632-3.316m0 0a3 3 0 105.367-2.684 3 3 0 00-5.367 2.684zm0 9.316a3 3 0 105.368 2.684 3 3 0 00-5.368-2.684z"
                    />
                  </svg>
                ),
                gradient: 'from-rose-500 to-pink-600',
              },
            ].map((feature, index) => (
              <motion.div
                key={feature.title}
                initial={{ opacity: 0, y: 30 }}
                whileInView={{ opacity: 1, y: 0 }}
                transition={{ duration: 0.8, delay: index * 0.1 }}
                viewport={{ once: true }}
                className="rounded-2xl bg-white p-8 shadow-lg transition-all duration-300 hover:scale-105 hover:shadow-xl"
              >
                <div
                  className={`mb-6 flex h-16 w-16 items-center justify-center rounded-full bg-gradient-to-br ${feature.gradient}`}
                >
                  {feature.icon}
                </div>
                <h3 className="mb-4 text-2xl font-bold text-gray-900">
                  {feature.title}
                </h3>
                <p className="leading-relaxed text-gray-600">
                  {feature.description}
                </p>
              </motion.div>
            ))}
          </div>
        </div>
      </section>

      {/* Testimonials */}
      <section className="py-20">
        <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
          <motion.div
            initial={{ opacity: 0, y: 30 }}
            whileInView={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.8 }}
            viewport={{ once: true }}
            className="mb-16 text-center"
          >
            <h2 className="mb-4 text-4xl font-bold text-gray-900 md:text-5xl">
              What Users Say About Our Demos
            </h2>
            <p className="mx-auto max-w-3xl text-xl text-gray-600">
              Hear from football enthusiasts who've experienced PixelPitchAI
              firsthand
            </p>
          </motion.div>

          <div className="grid grid-cols-1 gap-8 md:grid-cols-3">
            {[
              {
                quote:
                  "The demo blew my mind! The AI's understanding of football tactics is incredible. I could see my favorite team's playing style perfectly replicated.",
                author: 'Sarah Chen',
                role: 'Football Analyst',
                rating: 5,
              },
              {
                quote:
                  'As a coach, I was skeptical at first. But after seeing the demo, I realized this could revolutionize how we analyze and prepare for matches.',
                author: 'Marco Rodriguez',
                role: 'Youth Coach',
                rating: 5,
              },
              {
                quote:
                  'The level of detail is amazing. Every player behaves differently, formations matter, and the outcomes feel realistic. This is the future of football simulation!',
                author: 'David Thompson',
                role: 'Football Fan',
                rating: 5,
              },
            ].map((testimonial, index) => (
              <motion.div
                key={testimonial.author}
                initial={{ opacity: 0, y: 30 }}
                whileInView={{ opacity: 1, y: 0 }}
                transition={{ duration: 0.8, delay: index * 0.2 }}
                viewport={{ once: true }}
                className="rounded-2xl bg-white p-8 shadow-lg transition-all duration-300 hover:shadow-xl"
              >
                <div className="mb-4 flex text-yellow-400">
                  {[...Array(testimonial.rating)].map((_, i) => (
                    <svg
                      key={i}
                      className="h-5 w-5 fill-current"
                      viewBox="0 0 20 20"
                    >
                      <path d="M9.049 2.927c.3-.921 1.603-.921 1.902 0l1.07 3.292a1 1 0 00.95.69h3.462c.969 0 1.371 1.24.588 1.81l-2.8 2.034a1 1 0 00-.364 1.118l1.07 3.292c.3.921-.755 1.688-1.54 1.118l-2.8-2.034a1 1 0 00-1.175 0l-2.8 2.034c-.784.57-1.838-.197-1.539-1.118l1.07-3.292a1 1 0 00-.364-1.118L2.98 8.72c-.783-.57-.38-1.81.588-1.81h3.461a1 1 0 00.951-.69l1.07-3.292z" />
                    </svg>
                  ))}
                </div>
                <blockquote className="mb-6 text-lg leading-relaxed text-gray-700">
                  "{testimonial.quote}"
                </blockquote>
                <div>
                  <div className="font-bold text-gray-900">
                    {testimonial.author}
                  </div>
                  <div className="text-gray-600">{testimonial.role}</div>
                </div>
              </motion.div>
            ))}
          </div>
        </div>
      </section>

      {/* CTA Section */}
      <section className="bg-gradient-to-r from-green-600 to-emerald-700 py-20">
        <div className="mx-auto max-w-4xl px-4 text-center sm:px-6 lg:px-8">
          <motion.div
            initial={{ opacity: 0, y: 30 }}
            whileInView={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.8 }}
            viewport={{ once: true }}
          >
            <h2 className="mb-6 text-4xl font-bold text-white md:text-5xl">
              Ready to Experience PixelPitchAI?
            </h2>
            <p className="mb-10 text-xl leading-relaxed text-green-100">
              Don't just watch the demo – experience the full power of our
              AI-driven football simulation platform yourself.
            </p>
            <div className="flex flex-col justify-center gap-4 sm:flex-row">
              <Link
                href="/register"
                className="inline-block rounded-xl bg-white px-8 py-4 text-lg font-bold text-green-600 shadow-lg transition-all duration-300 hover:scale-105 hover:bg-gray-100 hover:shadow-xl"
              >
                Start Free Trial
              </Link>
              <Link
                href="/login"
                className="inline-block rounded-xl border-2 border-white px-8 py-4 text-lg font-bold text-white transition-all duration-300 hover:scale-105 hover:bg-white hover:text-green-600"
              >
                Sign In Now
              </Link>
            </div>
          </motion.div>
        </div>
      </section>

      {/* Footer */}
      <footer className="bg-gray-900 py-12">
        <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
          <div className="grid grid-cols-1 gap-8 md:grid-cols-4">
            <div className="col-span-1 md:col-span-2">
              <div className="mb-4 flex items-center">
                <div className="mr-3 h-10 w-10">
                  <Image
                    src="/logos/PixelPitch.png"
                    alt="PixelPitch Logo"
                    width={40}
                    height={40}
                    className="rounded-full object-cover"
                  />
                </div>
                <div>
                  <h3 className="text-xl font-bold text-white">PixelPitchAI</h3>
                  <p className="text-gray-400">
                    AI-Powered Football Simulation
                  </p>
                </div>
              </div>
              <p className="text-gray-400">
                Experience the future of football simulation with cutting-edge
                AI technology.
              </p>
            </div>
            <div>
              <h4 className="mb-4 text-lg font-semibold text-white">Demos</h4>
              <ul className="space-y-2 text-gray-400">
                <li>
                  <button
                    onClick={() => setCurrentDemo('overview')}
                    className="transition-colors hover:text-white"
                  >
                    Platform Overview
                  </button>
                </li>
                <li>
                  <button
                    onClick={() => setCurrentDemo('simulation')}
                    className="transition-colors hover:text-white"
                  >
                    Live Simulation
                  </button>
                </li>
                <li>
                  <button
                    onClick={() => setCurrentDemo('analytics')}
                    className="transition-colors hover:text-white"
                  >
                    Advanced Analytics
                  </button>
                </li>
                <li>
                  <Link
                    href="/register"
                    className="transition-colors hover:text-white"
                  >
                    Try Now
                  </Link>
                </li>
              </ul>
            </div>
            <div>
              <h4 className="mb-4 text-lg font-semibold text-white">
                Resources
              </h4>
              <ul className="space-y-2 text-gray-400">
                <li>
                  <Link
                    href="/about"
                    className="transition-colors hover:text-white"
                  >
                    About Us
                  </Link>
                </li>
                <li>
                  <Link
                    href="/features"
                    className="transition-colors hover:text-white"
                  >
                    Features
                  </Link>
                </li>
                <li>
                  <Link
                    href="/contact"
                    className="transition-colors hover:text-white"
                  >
                    Contact
                  </Link>
                </li>
                <li>
                  <Link
                    href="/support"
                    className="transition-colors hover:text-white"
                  >
                    Support
                  </Link>
                </li>
              </ul>
            </div>
          </div>
          <div className="mt-8 border-t border-gray-800 pt-8 text-center text-gray-400">
            <p>&copy; 2025 PixelPitchAI. All rights reserved.</p>
          </div>
        </div>
      </footer>
    </div>
  );
}
