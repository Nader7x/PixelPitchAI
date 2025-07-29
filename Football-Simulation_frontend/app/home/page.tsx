import Hero from '@/app/Components/Hero Section/Hero';
import SigninNavbar from '@/app/Components/Navbar/SigninNavbar';

export default function Home() {
  return (
    <div className="min-h-screen scroll-smooth">
      <SigninNavbar />
      <Hero />

      {/* Features Section */}
      <section
        id="features"
        className="scroll-mt-20 bg-gradient-to-br from-gray-50 to-green-50 py-20"
      >
        <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
          <div className="animate-fade-in mb-16 text-center">
            <h2 className="mb-4 text-4xl font-bold text-gray-900 md:text-5xl">
              Why Choose Our Football Simulator?
            </h2>
            <p className="mx-auto max-w-3xl text-xl text-gray-600">
              Experience the most advanced football simulation technology
              powered by AI
            </p>
          </div>

          <div className="grid grid-cols-1 gap-8 md:grid-cols-2 lg:grid-cols-3">
            {/* Feature 1 */}
            <div className="rounded-2xl bg-white p-8 shadow-lg transition-all duration-300 hover:scale-105 hover:shadow-xl">
              <div className="mb-6 flex h-16 w-16 items-center justify-center rounded-full bg-gradient-to-br from-green-500 to-emerald-600">
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
              </div>
              <h3 className="mb-4 text-2xl font-bold text-gray-900">
                AI-Powered Engine
              </h3>
              <p className="leading-relaxed text-gray-600">
                Our advanced AI analyzes real player data, team formations, and
                tactical strategies to create authentic match experiences.
              </p>
            </div>

            {/* Feature 2 */}
            <div className="rounded-2xl bg-white p-8 shadow-lg transition-all duration-300 hover:scale-105 hover:shadow-xl">
              <div className="mb-6 flex h-16 w-16 items-center justify-center rounded-full bg-gradient-to-br from-blue-500 to-indigo-600">
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
              </div>
              <h3 className="mb-4 text-2xl font-bold text-gray-900">
                Real-Time Statistics
              </h3>
              <p className="leading-relaxed text-gray-600">
                Track every aspect of the match with detailed real-time
                statistics, player performance metrics, and tactical analysis.
              </p>
            </div>

            {/* Feature 3 */}
            <div className="rounded-2xl bg-white p-8 shadow-lg transition-all duration-300 hover:scale-105 hover:shadow-xl">
              <div className="mb-6 flex h-16 w-16 items-center justify-center rounded-full bg-gradient-to-br from-purple-500 to-pink-600">
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
                    d="M12 6V4m0 2a2 2 0 100 4m0-4a2 2 0 110 4m-6 8a2 2 0 100-4m0 4a2 2 0 110-4m0 4v2m0-6V4m6 6v10m6-2a2 2 0 100-4m0 4a2 2 0 110-4m0 4v2m0-6V4"
                  />
                </svg>
              </div>
              <h3 className="mb-4 text-2xl font-bold text-gray-900">
                Custom Scenarios
              </h3>
              <p className="leading-relaxed text-gray-600">
                Create your own match scenarios with custom teams, weather
                conditions, and game situations for endless possibilities.
              </p>
            </div>

            {/* Feature 4 */}
            <div className="rounded-2xl bg-white p-8 shadow-lg transition-all duration-300 hover:scale-105 hover:shadow-xl">
              <div className="mb-6 flex h-16 w-16 items-center justify-center rounded-full bg-gradient-to-br from-orange-500 to-red-600">
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
                    d="M13 10V3L4 14h7v7l9-11h-7z"
                  />
                </svg>
              </div>
              <h3 className="mb-4 text-2xl font-bold text-gray-900">
                Live Match Events
              </h3>
              <p className="leading-relaxed text-gray-600">
                Experience matches as they unfold with live commentary,
                real-time events, and dynamic match progression.
              </p>
            </div>

            {/* Feature 5 */}
            <div className="rounded-2xl bg-white p-8 shadow-lg transition-all duration-300 hover:scale-105 hover:shadow-xl">
              <div className="mb-6 flex h-16 w-16 items-center justify-center rounded-full bg-gradient-to-br from-teal-500 to-cyan-600">
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
              </div>
              <h3 className="mb-4 text-2xl font-bold text-gray-900">
                Team Management
              </h3>
              <p className="leading-relaxed text-gray-600">
                Manage your favorite teams, create custom lineups, and analyze
                player performances across multiple seasons.
              </p>
            </div>

            {/* Feature 6 */}
            <div className="rounded-2xl bg-white p-8 shadow-lg transition-all duration-300 hover:scale-105 hover:shadow-xl">
              <div className="mb-6 flex h-16 w-16 items-center justify-center rounded-full bg-gradient-to-br from-yellow-500 to-orange-600">
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
                    d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z"
                  />
                </svg>
              </div>
              <h3 className="mb-4 text-2xl font-bold text-gray-900">
                Realistic Outcomes
              </h3>
              <p className="leading-relaxed text-gray-600">
                Get realistic match results based on complex algorithms that
                consider player form, team chemistry, and tactical setups.
              </p>
            </div>
          </div>
        </div>
      </section>

      {/* How It Works Section */}
      <section id="how-it-works" className="scroll-mt-20 bg-gray-900 py-20">
        <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
          <div className="mb-16 text-center">
            <h2 className="mb-4 text-4xl font-bold text-white md:text-5xl">
              How It Works
            </h2>
            <p className="mx-auto max-w-3xl text-xl text-gray-300">
              Get started with football simulation in just three simple steps
            </p>
          </div>

          <div className="grid grid-cols-1 gap-8 md:grid-cols-3">
            <div className="group text-center transition-all duration-300 hover:scale-105">
              <div className="mx-auto mb-6 flex h-20 w-20 items-center justify-center rounded-full bg-gradient-to-br from-green-500 to-emerald-600 group-hover:shadow-lg group-hover:shadow-green-500/50">
                <span className="text-3xl font-bold text-white">1</span>
              </div>
              <h3 className="mb-4 text-2xl font-bold text-white">
                Choose Teams
              </h3>
              <p className="leading-relaxed text-gray-300">
                Select your favorite teams from our extensive database of
                professional football clubs with real player data.
              </p>
            </div>

            <div className="group text-center transition-all duration-300 hover:scale-105">
              <div className="mx-auto mb-6 flex h-20 w-20 items-center justify-center rounded-full bg-gradient-to-br from-blue-500 to-indigo-600 group-hover:shadow-lg group-hover:shadow-blue-500/50">
                <span className="text-3xl font-bold text-white">2</span>
              </div>
              <h3 className="mb-4 text-2xl font-bold text-white">
                Set Parameters
              </h3>
              <p className="leading-relaxed text-gray-300">
                Customize match conditions, formations, and tactical approaches
                to create your perfect simulation scenario.
              </p>
            </div>

            <div className="group text-center transition-all duration-300 hover:scale-105">
              <div className="mx-auto mb-6 flex h-20 w-20 items-center justify-center rounded-full bg-gradient-to-br from-purple-500 to-pink-600 group-hover:shadow-lg group-hover:shadow-purple-500/50">
                <span className="text-3xl font-bold text-white">3</span>
              </div>
              <h3 className="mb-4 text-2xl font-bold text-white">
                Watch & Analyze
              </h3>
              <p className="leading-relaxed text-gray-300">
                Experience the match unfold in real-time and analyze detailed
                statistics and performance metrics.
              </p>
            </div>
          </div>
        </div>
      </section>

      {/* Technology Section */}
      <section className="bg-gradient-to-br from-green-50 to-blue-50 py-20">
        <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
          <div className="grid grid-cols-1 items-center gap-12 lg:grid-cols-2">
            <div>
              <h2 className="mb-6 text-4xl font-bold text-gray-900">
                Powered by Advanced AI Technology
              </h2>
              <p className="mb-8 text-xl leading-relaxed text-gray-600">
                Our football simulation engine uses cutting-edge artificial
                intelligence and machine learning algorithms to create the most
                realistic football experience possible.
              </p>
              <div className="space-y-6">
                <div className="flex items-start">
                  <div className="mt-1 mr-4 flex h-6 w-6 items-center justify-center rounded-full bg-green-500">
                    <svg
                      className="h-4 w-4 text-white"
                      fill="currentColor"
                      viewBox="0 0 20 20"
                    >
                      <path
                        fillRule="evenodd"
                        d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z"
                        clipRule="evenodd"
                      />
                    </svg>
                  </div>
                  <div>
                    <h4 className="mb-2 text-lg font-semibold text-gray-900">
                      Real Player Data
                    </h4>
                    <p className="text-gray-600">
                      Incorporating actual player statistics, skills, and
                      performance history
                    </p>
                  </div>
                </div>
                <div className="flex items-start">
                  <div className="mt-1 mr-4 flex h-6 w-6 items-center justify-center rounded-full bg-green-500">
                    <svg
                      className="h-4 w-4 text-white"
                      fill="currentColor"
                      viewBox="0 0 20 20"
                    >
                      <path
                        fillRule="evenodd"
                        d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z"
                        clipRule="evenodd"
                      />
                    </svg>
                  </div>
                  <div>
                    <h4 className="mb-2 text-lg font-semibold text-gray-900">
                      Dynamic Match Events
                    </h4>
                    <p className="text-gray-600">
                      AI-generated events that respond to game state and
                      tactical decisions
                    </p>
                  </div>
                </div>
                <div className="flex items-start">
                  <div className="mt-1 mr-4 flex h-6 w-6 items-center justify-center rounded-full bg-green-500">
                    <svg
                      className="h-4 w-4 text-white"
                      fill="currentColor"
                      viewBox="0 0 20 20"
                    >
                      <path
                        fillRule="evenodd"
                        d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z"
                        clipRule="evenodd"
                      />
                    </svg>
                  </div>
                  <div>
                    <h4 className="mb-2 text-lg font-semibold text-gray-900">
                      Predictive Analytics
                    </h4>
                    <p className="text-gray-600">
                      Advanced algorithms that predict realistic match outcomes
                    </p>
                  </div>
                </div>
              </div>
            </div>
            <div className="relative">
              <div className="rounded-2xl bg-gradient-to-br from-green-500 to-emerald-600 p-8 shadow-2xl">
                <div className="text-white">
                  <h3 className="mb-6 text-2xl font-bold">Simulation Stats</h3>
                  <div className="grid grid-cols-2 gap-6">
                    <div className="text-center">
                      <div className="mb-2 text-3xl font-bold">10M+</div>
                      <div className="text-green-100">Matches Simulated</div>
                    </div>
                    <div className="text-center">
                      <div className="mb-2 text-3xl font-bold">500+</div>
                      <div className="text-green-100">Teams Available</div>
                    </div>
                    <div className="text-center">
                      <div className="mb-2 text-3xl font-bold">99.2%</div>
                      <div className="text-green-100">Accuracy Rate</div>
                    </div>
                    <div className="text-center">
                      <div className="mb-2 text-3xl font-bold">24/7</div>
                      <div className="text-green-100">Simulation Available</div>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </section>

      {/* CTA Section */}
      <section
        id="get-started"
        className="scroll-mt-20 bg-gradient-to-r from-green-600 to-emerald-700 py-20"
      >
        <div className="mx-auto max-w-4xl px-4 text-center sm:px-6 lg:px-8">
          <h2 className="mb-6 text-4xl font-bold text-white md:text-5xl">
            Ready to Experience Football Like Never Before?
          </h2>
          <p className="mb-10 text-xl leading-relaxed text-green-100">
            Join thousands of football fans who are already using our advanced
            simulation platform to experience the beautiful game in a whole new
            way.
          </p>
          <div className="flex flex-col justify-center gap-4 sm:flex-row">
            <a
              href="/register"
              className="inline-block rounded-xl bg-white px-8 py-4 text-lg font-bold text-green-600 shadow-lg transition-all duration-300 hover:scale-105 hover:bg-gray-100 hover:shadow-xl"
            >
              Start Free Trial
            </a>
            <a
              href="/trial"
              className="inline-block rounded-xl border-2 border-white px-8 py-4 text-lg font-bold text-white transition-all duration-300 hover:scale-105 hover:bg-white hover:text-green-600"
            >
              Watch Demo
            </a>
          </div>
        </div>
      </section>

      {/* Data Partnership Section */}
      <section className="bg-white py-16">
        <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
          <div className="text-center">
            <h2 className="mb-8 text-3xl font-bold text-gray-900">
              Powered by Premium Football Data
            </h2>
            <p className="mx-auto mb-12 max-w-3xl text-lg text-gray-600">
              Our simulations are built using high-quality, professional
              football data to ensure the most accurate and realistic match
              experiences possible.
            </p>

            <div className="flex flex-col items-center justify-center space-y-6 md:flex-row md:space-y-0 md:space-x-12">
              <div className="flex flex-col items-center">
                <img
                  src="/logos/SB - Icon Lockup - Colour positive.png"
                  alt="StatsBomb Logo"
                  className="mb-4 h-16 w-auto object-contain"
                />
                <h3 className="mb-2 text-xl font-semibold text-gray-900">
                  StatsBomb Open Data
                </h3>
                <p className="max-w-md text-center text-gray-600">
                  Utilizing StatsBomb's comprehensive open dataset for authentic
                  player statistics, match events, and tactical analysis.
                </p>
              </div>

              <div className="flex flex-col items-center">
                <div className="mb-4 flex h-16 w-16 items-center justify-center rounded-full bg-gradient-to-br from-green-500 to-emerald-600">
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
                      d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z"
                    />
                  </svg>
                </div>
                <h3 className="mb-2 text-xl font-semibold text-gray-900">
                  Professional Quality
                </h3>
                <p className="max-w-md text-center text-gray-600">
                  Access to the same level of data used by professional football
                  clubs and analysts worldwide.
                </p>
              </div>
            </div>

            <div className="mt-8 text-sm text-gray-500">
              <p>
                Special thanks to StatsBomb for providing open access to their
                premium football data. Learn more about StatsBomb's data
                solutions at{' '}
                <a
                  href="https://statsbomb.com"
                  target="_blank"
                  rel="noopener noreferrer"
                  className="text-green-600 underline hover:text-green-700"
                >
                  statsbomb.com
                </a>
              </p>
            </div>
          </div>
        </div>
      </section>

      {/* Footer */}
      <footer className="bg-gray-900 py-12 text-white">
        <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
          <div className="grid grid-cols-1 gap-8 md:grid-cols-4">
            <div>
              <h3 className="mb-4 text-xl font-bold">Pixel Pitch AI</h3>
              <p className="leading-relaxed text-gray-400">
                The most advanced AI-powered football simulation platform for
                enthusiasts and professionals.
              </p>
            </div>
            <div>
              <h4 className="mb-4 text-lg font-semibold">Features</h4>
              <ul className="space-y-2 text-gray-400">
                <li>AI-Powered Matches</li>
                <li>Real-Time Statistics</li>
                <li>Custom Scenarios</li>
                <li>Team Management</li>
              </ul>
            </div>
            <div>
              <h4 className="mb-4 text-lg font-semibold">Support</h4>
              <ul className="space-y-2 text-gray-400">
                <li>Documentation</li>
                <li>API Reference</li>
                <li>Community Forum</li>
                <li>Contact Support</li>
              </ul>
            </div>
            <div>
              <h4 className="mb-4 text-lg font-semibold">Company</h4>
              <ul className="space-y-2 text-gray-400">
                <li>About Us</li>
                <li>Privacy Policy</li>
                <li>Terms of Service</li>
                <li>Careers</li>
              </ul>
            </div>
          </div>
          <div className="mt-8 border-t border-gray-800 pt-8 text-center text-gray-400">
            <p>&copy; 2025 Football Simulator. All rights reserved.</p>
          </div>
        </div>
      </footer>
    </div>
  );
}
