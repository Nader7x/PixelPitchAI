'use client';

import React from 'react';
import { motion } from 'framer-motion';
import { Bell, Filter } from 'lucide-react';

interface EmptyStateProps {
  hasNotifications: boolean;
  onResetFilters: () => void;
  isDarkMode: boolean;
  isMounted: boolean;
}

export const EmptyState: React.FC<EmptyStateProps> = ({
  hasNotifications,
  onResetFilters,
  isDarkMode,
  isMounted,
}) => {
  return (
    <div className="p-8 text-center lg:p-12">
      <motion.div
        initial={{ opacity: 0, scale: 0.8 }}
        animate={{ opacity: 1, scale: 1 }}
        transition={{ delay: 0.3 }}
        className={`mx-auto mb-6 flex h-20 w-20 items-center justify-center rounded-full ${
          isMounted && isDarkMode ? 'bg-gray-700' : 'bg-gray-100'
        }`}
      >
        <Bell
          className={`h-10 w-10 ${
            isMounted && isDarkMode ? 'text-gray-400' : 'text-gray-400'
          }`}
        />
      </motion.div>
      <motion.h3
        initial={{ opacity: 0, y: 20 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ delay: 0.4 }}
        className={`mb-2 text-lg font-semibold lg:text-xl ${
          isMounted && isDarkMode ? 'text-white' : 'text-gray-900'
        }`}
      >
        {!hasNotifications
          ? 'No notifications yet'
          : 'No notifications match your filters'}
      </motion.h3>
      <motion.p
        initial={{ opacity: 0, y: 20 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ delay: 0.5 }}
        className={`mx-auto max-w-md ${
          isMounted && isDarkMode ? 'text-gray-400' : 'text-gray-500'
        }`}
      >
        {!hasNotifications
          ? "You're all caught up! New notifications will appear here when you have football activities."
          : "Try adjusting your search or filter criteria to find the notifications you're looking for."}
      </motion.p>
      {hasNotifications && (
        <motion.button
          initial={{ opacity: 0, y: 20 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ delay: 0.6 }}
          onClick={onResetFilters}
          className={`mt-6 inline-flex items-center gap-2 rounded-lg px-4 py-2 text-sm font-medium transition-all duration-200 focus:ring-2 focus:outline-none ${
            isMounted && isDarkMode
              ? 'bg-blue-600 text-white hover:bg-blue-700 focus:ring-blue-500/50'
              : 'bg-blue-600 text-white hover:bg-blue-700 focus:ring-blue-500/50'
          }`}
        >
          <Filter size={16} />
          Clear All Filters
        </motion.button>
      )}
    </div>
  );
};
