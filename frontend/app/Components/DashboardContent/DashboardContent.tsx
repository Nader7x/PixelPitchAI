'use client';

import { useSidebarContext } from '../Sidebar/Sidebar';
import { useSettings } from '../../contexts/EnhancedSettingsContext';
import SpaceEfficiencyPanel from '../SpaceEfficiencyPanel/SpaceEfficiencyPanel';
import Navbar from '@/app/Components/Navbar/Navbar';
import DashboardImage from '@/app/Components/DashboardImage/DashboardImage';
import LatestMatches from '@/app/Components/LatestMatches/LatestMatches';
import RightPanel from '@/app/Components/RightPanel/RightPanel';
import LiveMatchPanel from '@/app/Components/RightPanel/LiveMatchPanel';
import { Settings, Play } from 'lucide-react';
import Link from 'next/link';
import { useState, useEffect } from 'react';

export default function DashboardContent() {
  const [showSpacePanel, setShowSpacePanel] = useState(false);
  const [isMounted, setIsMounted] = useState(false);
  const { expanded, isCompactMode } = useSidebarContext();
  const { isDarkMode } = useSettings();

  useEffect(() => {
    setIsMounted(true);
  }, []);

  return (
    <>
      {/* Content container - Remove redundant margins since SidebarLayout already handles them */}
      <div
        className={`relative z-10 w-full transition-colors duration-300 ${
          isMounted && isDarkMode ? 'bg-gray-900' : 'bg-white'
        }`}
        suppressHydrationWarning
      >
        <Navbar />

        {/* Space Efficiency Toggle */}
        <div className="px-4 py-2">
          <button
            onClick={() => setShowSpacePanel(!showSpacePanel)}
            className={`flex items-center gap-2 rounded-lg px-3 py-1 text-sm transition-all ${
              isMounted && isDarkMode
                ? 'bg-green-500/20 text-green-400 hover:bg-green-500/30'
                : 'bg-green-600/10 text-green-600 hover:bg-green-600/20'
            }`}
            suppressHydrationWarning
          >
            <Settings size={14} />
            {showSpacePanel ? 'Hide' : 'Show'} Space Metrics
          </button>
        </div>

        {/* Space Efficiency Panel */}
        {showSpacePanel && (
          <div className="mx-4 mb-4">
            <SpaceEfficiencyPanel
              isCompactMode={isCompactMode}
              sidebarExpanded={expanded}
            />
          </div>
        )}

        <div className="flex flex-1 flex-col p-4 sm:p-6 lg:flex-row">
          {/* Central Content - Enhanced Layout */}
          <div className="flex-1 overflow-y-auto">
            {/* Dashboard Image */}
            <div className="mb-6">
              <DashboardImage />
            </div>
            {/* Hero Section with Match Simulation CTA */}
            <div className="relative mb-6 overflow-hidden rounded-2xl bg-gradient-to-br from-green-600 via-green-700 to-emerald-800 shadow-2xl">
              <div className="absolute inset-0 bg-black/10"></div>
              <div className="relative z-10 p-8">
                <div className="flex flex-col items-center text-center lg:flex-row lg:text-left">
                  <div className="flex-1">
                    <h1 className="mb-4 text-4xl font-bold text-white">
                      Welcome to Match Simulation
                    </h1>
                    <p className="mb-6 text-lg text-green-100">
                      Experience the thrill of live football matches with our
                      advanced simulation engine
                    </p>
                    <div className="flex flex-col gap-4 sm:flex-row">
                      <Link href="/matchsimulation">
                        <button className="group relative overflow-hidden rounded-xl bg-white px-8 py-4 text-lg font-semibold text-green-700 shadow-lg transition-all duration-300 hover:scale-105 hover:shadow-xl">
                          <span className="relative z-10 flex items-center gap-2">
                            <Play size={20} />
                            Start Simulation
                          </span>
                          <div className="absolute inset-0 bg-gradient-to-r from-green-50 to-emerald-50 opacity-0 transition-opacity duration-300 group-hover:opacity-100"></div>
                        </button>
                      </Link>
                    </div>
                  </div>
                  <div className="mt-6 lg:mt-0 lg:ml-8">
                    <div className="relative">
                      <div className="absolute inset-0 animate-pulse rounded-full bg-white/20"></div>
                      <div className="relative rounded-full bg-white/10 p-6 backdrop-blur-sm">
                        <Play size={60} className="animate-bounce text-white" />
                      </div>
                    </div>
                  </div>
                </div>
              </div>
              {/* Animated background elements */}
              <div className="animate-float absolute top-4 right-4 h-20 w-20 rounded-full bg-white/5"></div>
              <div className="animate-float-delayed absolute bottom-4 left-4 h-16 w-16 rounded-full bg-white/5"></div>
            </div>

            {/* Latest Matches Section */}
            <div className="mb-6">
              <LatestMatches />
            </div>
          </div>{' '}
          {/* Right Panel */}
          <div className="w-full lg:ml-6 lg:w-80">
            <RightPanel>
              <LiveMatchPanel />
            </RightPanel>
          </div>
        </div>
      </div>
    </>
  );
}
