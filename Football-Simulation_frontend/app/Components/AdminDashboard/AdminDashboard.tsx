'use client';

import React, { useState, useEffect } from 'react';
import TeamManagement from './EntityManagement/TeamManagement';
import StadiumManagement from './EntityManagement/StadiumManagement';
import PlayerManagement from './EntityManagement/PlayerManagement';
import SeasonManagement from './EntityManagement/SeasonManagement';
import CoachManagement from '@/app/Components/AdminDashboard/EntityManagement/CoachManagement';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { motion, AnimatePresence } from 'framer-motion';
import { ArrowLeft, LayoutDashboard } from 'lucide-react';
import { useRouter } from 'next/navigation';

const AdminDashboard = () => {
  const [activeTab, setActiveTab] = useState('teams');
  const [mounted, setMounted] = useState(false);
  const router = useRouter();

  useEffect(() => {
    setMounted(true);
  }, []);

  const handleTabChange = (value: string) => {
    // Ensure we don't set the same tab (prevents unnecessary re-rendering)
    if (activeTab !== value) {
      setActiveTab(value);
    }
  };

  // Prevent clicks inside tab content from bubbling up and causing issues
  const handleContentClick = (e: React.MouseEvent) => {
    e.stopPropagation();
  };

  const handleBackToDashboard = () => {
    router.push('/dashboard');
  };

  if (!mounted) return null;

  return (
    <div className="min-h-screen bg-gradient-to-br from-gray-900 via-gray-800 to-gray-900 text-white">
      <div className="relative container mx-auto p-6">
        <motion.div
          initial={{ opacity: 0, y: -20 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ duration: 0.5 }}
          className="mb-8 flex items-center justify-between"
        >
          <div>
            <h1 className="mb-2 bg-gradient-to-r from-green-400 to-blue-500 bg-clip-text text-4xl font-bold text-transparent">
              Admin Dashboard
            </h1>
            <p className="text-lg text-gray-300">
              Manage your football data with ease
            </p>
          </div>

          {/* Back to Dashboard Button */}
          <motion.button
            initial={{ opacity: 0, x: 20 }}
            animate={{ opacity: 1, x: 0 }}
            transition={{ delay: 0.3, duration: 0.5 }}
            onClick={handleBackToDashboard}
            className="group flex items-center gap-2 rounded-lg bg-gradient-to-r from-green-500 to-blue-500 px-4 py-2 text-white shadow-lg transition-all duration-300 hover:scale-105 hover:from-green-600 hover:to-blue-600 hover:shadow-xl"
          >
            <ArrowLeft
              size={18}
              className="transition-transform duration-300 group-hover:-translate-x-1"
            />
            <LayoutDashboard size={18} />
            <span className="font-medium">Back to Dashboard</span>
          </motion.button>
        </motion.div>

        <motion.div
          initial={{ opacity: 0 }}
          animate={{ opacity: 1 }}
          transition={{ delay: 0.2, duration: 0.5 }}
          className="relative z-20 overflow-hidden rounded-xl border border-gray-700 bg-gray-800 shadow-2xl"
        >
          <Tabs
            defaultValue="teams"
            value={activeTab}
            className="w-full"
            onValueChange={handleTabChange}
          >
            <div className="px-6 pt-6">
              <TabsList className="grid w-full grid-cols-5 rounded-lg bg-gray-700 p-1">
                <TabsTrigger
                  value="teams"
                  className="relative z-30 transition-all duration-300 data-[state=active]:bg-gradient-to-r data-[state=active]:from-green-500 data-[state=active]:to-blue-500 data-[state=active]:text-white"
                  onClick={() => handleTabChange('teams')}
                >
                  Teams
                </TabsTrigger>
                <TabsTrigger
                  value="stadiums"
                  className="relative z-30 transition-all duration-300 data-[state=active]:bg-gradient-to-r data-[state=active]:from-green-500 data-[state=active]:to-blue-500 data-[state=active]:text-white"
                  onClick={() => handleTabChange('stadiums')}
                >
                  Stadiums
                </TabsTrigger>
                <TabsTrigger
                  value="players"
                  className="relative z-30 transition-all duration-300 data-[state=active]:bg-gradient-to-r data-[state=active]:from-green-500 data-[state=active]:to-blue-500 data-[state=active]:text-white"
                  onClick={() => handleTabChange('players')}
                >
                  Players
                </TabsTrigger>
                <TabsTrigger
                  value="seasons"
                  className="relative z-30 transition-all duration-300 data-[state=active]:bg-gradient-to-r data-[state=active]:from-green-500 data-[state=active]:to-blue-500 data-[state=active]:text-white"
                  onClick={() => handleTabChange('seasons')}
                >
                  Seasons
                </TabsTrigger>
                <TabsTrigger
                  value="coaches"
                  className="relative z-30 transition-all duration-300 data-[state=active]:bg-gradient-to-r data-[state=active]:from-green-500 data-[state=active]:to-blue-500 data-[state=active]:text-white"
                  onClick={() => handleTabChange('coaches')}
                >
                  Coaches
                </TabsTrigger>
              </TabsList>
            </div>

            <AnimatePresence mode="wait">
              <motion.div
                key={activeTab}
                initial={{ opacity: 0, y: 10 }}
                animate={{ opacity: 1, y: 0 }}
                exit={{ opacity: 0, y: -10 }}
                transition={{ duration: 0.3 }}
                className="relative z-10 p-6"
                onClick={handleContentClick}
              >
                <TabsContent value="teams" className="relative z-10 mt-0">
                  <TeamManagement />
                </TabsContent>

                <TabsContent value="stadiums" className="relative z-10 mt-0">
                  <StadiumManagement />
                </TabsContent>

                <TabsContent value="players" className="relative z-10 mt-0">
                  <PlayerManagement />
                </TabsContent>

                <TabsContent value="seasons" className="relative z-10 mt-0">
                  <SeasonManagement />
                </TabsContent>

                <TabsContent value="coaches" className="relative z-10 mt-0">
                  <CoachManagement />
                </TabsContent>
              </motion.div>
            </AnimatePresence>
          </Tabs>
        </motion.div>

        {/* Background decorative elements */}
        <div className="pointer-events-none absolute top-40 right-10 h-64 w-64 animate-pulse rounded-full bg-blue-500 opacity-10 blur-3xl filter"></div>
        <div
          className="pointer-events-none absolute bottom-20 left-10 h-80 w-80 animate-pulse rounded-full bg-green-500 opacity-10 blur-3xl filter"
          style={{ animationDelay: '2s' }}
        ></div>
      </div>
    </div>
  );
};
export default AdminDashboard;
