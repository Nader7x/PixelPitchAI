'use client';

import React from 'react';
import { motion } from 'framer-motion';
import {
  Bell,
  Home,
  ClubIcon,
  Users,
  Package,
  Settings,
  User,
  Search,
} from 'lucide-react';

interface LoadingStateProps {
  isDarkMode: boolean;
  isMounted: boolean;
}

export const LoadingState: React.FC<LoadingStateProps> = ({
  isDarkMode,
  isMounted,
}) => {
  return (
    <div className="flex flex-1 items-center justify-center p-4">
      <motion.div
        initial={{ opacity: 0, scale: 0.8 }}
        animate={{ opacity: 1, scale: 1 }}
        className="text-center"
      >
        <div
          className={`mx-auto mb-6 flex h-16 w-16 items-center justify-center rounded-full shadow-lg ${
            isMounted && isDarkMode ? 'bg-gray-800' : 'bg-white'
          }`}
        >
          <div
            className={`h-8 w-8 animate-spin rounded-full border-3 border-t-transparent ${
              isMounted && isDarkMode ? 'border-blue-400' : 'border-blue-500'
            }`}
          ></div>
        </div>
        <h2
          className={`mb-3 text-2xl font-bold ${
            isMounted && isDarkMode ? 'text-white' : 'text-gray-800'
          }`}
        >
          Loading Notifications
        </h2>
        <p
          className={`max-w-md ${
            isMounted && isDarkMode ? 'text-gray-300' : 'text-gray-600'
          }`}
        >
          Please wait while we fetch your latest notifications and updates...
        </p>
        <motion.div
          initial={{ width: 0 }}
          animate={{ width: '100%' }}
          transition={{ duration: 2, ease: 'easeInOut' }}
          className={`mx-auto mt-4 h-1 max-w-xs rounded-full ${
            isMounted && isDarkMode ? 'bg-blue-400' : 'bg-blue-500'
          }`}
        />
      </motion.div>
    </div>
  );
};
