'use client';

import React from 'react';
import { motion } from 'framer-motion';
import { Bell } from 'lucide-react';

interface NotificationHeaderProps {
  unreadCount: number;
  isDarkMode: boolean;
  isMounted: boolean;
}

export const NotificationHeader: React.FC<NotificationHeaderProps> = ({
  unreadCount,
  isDarkMode,
  isMounted,
}) => {
  return (
    <div className="mb-6 lg:mb-8">
      <motion.div
        initial={{ opacity: 0, y: -20 }}
        animate={{ opacity: 1, y: 0 }}
        className="flex flex-col gap-2 sm:flex-row sm:items-center sm:justify-between"
      >
        <div className="flex items-center gap-3">
          <div className="relative">
            <Bell
              className={`h-7 w-7 lg:h-8 lg:w-8 ${
                isMounted && isDarkMode ? 'text-blue-400' : 'text-blue-600'
              }`}
            />
            {unreadCount > 0 && (
              <motion.span
                initial={{ scale: 0 }}
                animate={{ scale: 1 }}
                className="absolute -top-1 -right-1 flex h-5 w-5 items-center justify-center rounded-full bg-red-500 text-xs font-bold text-white ring-2 ring-white"
              >
                {unreadCount > 99 ? '99+' : unreadCount}
              </motion.span>
            )}
          </div>
          <div>
            <h1
              className={`text-2xl font-bold lg:text-3xl ${
                isMounted && isDarkMode ? 'text-white' : 'text-gray-900'
              }`}
            >
              Notifications
            </h1>
            <p
              className={`text-sm lg:text-base ${
                isMounted && isDarkMode ? 'text-gray-300' : 'text-gray-600'
              }`}
            >
              Stay updated with your latest football activities
            </p>
          </div>
        </div>
        {unreadCount > 0 && (
          <motion.div
            initial={{ opacity: 0, scale: 0.8 }}
            animate={{ opacity: 1, scale: 1 }}
            className={`flex items-center gap-2 rounded-lg px-3 py-2 text-sm font-medium ${
              isMounted && isDarkMode
                ? 'bg-blue-900/50 text-blue-300'
                : 'bg-blue-50 text-blue-700'
            }`}
          >
            <div
              className={`h-2 w-2 animate-pulse rounded-full ${
                isMounted && isDarkMode ? 'bg-blue-400' : 'bg-blue-500'
              }`}
            ></div>
            {unreadCount} unread notification{unreadCount !== 1 ? 's' : ''}
          </motion.div>
        )}
      </motion.div>
    </div>
  );
};
