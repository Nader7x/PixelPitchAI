'use client';

import React from 'react';
import { motion } from 'framer-motion';
import { CheckCheck, Trash2, X } from 'lucide-react';
import { Notification } from '@/Services/NotificationService';

interface NotificationActionsProps {
  selectedNotifications: Set<string>;
  filteredNotifications: Notification[];
  unreadCount: number;
  isLoading: boolean;
  onToggleSelectAll: () => void;
  onMarkAllAsRead: () => void;
  onDeleteSelected: () => void;
  onClearAll: () => void;
  isDarkMode: boolean;
  isMounted: boolean;
}

export const NotificationActions: React.FC<NotificationActionsProps> = ({
  selectedNotifications,
  filteredNotifications,
  unreadCount,
  isLoading,
  onToggleSelectAll,
  onMarkAllAsRead,
  onDeleteSelected,
  onClearAll,
  isDarkMode,
  isMounted,
}) => {
  return (
    <motion.div
      initial={{ opacity: 0, y: 20 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ delay: 0.1 }}
      className={`mb-4 rounded-xl border p-4 shadow-sm lg:mb-6 ${
        isMounted && isDarkMode
          ? 'border-gray-700 bg-gray-800'
          : 'border-gray-200 bg-white'
      }`}
    >
      <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <div className="flex items-center gap-4">
          <label className="flex cursor-pointer items-center gap-2 select-none">
            <input
              type="checkbox"
              checked={
                selectedNotifications.size === filteredNotifications.length &&
                filteredNotifications.length > 0
              }
              onChange={onToggleSelectAll}
              className={`h-4 w-4 rounded border transition-colors focus:ring-2 focus:ring-offset-0 ${
                isMounted && isDarkMode
                  ? 'border-gray-600 text-blue-400 focus:ring-blue-400/20'
                  : 'border-gray-300 text-blue-600 focus:ring-blue-500/20'
              }`}
            />
            <span
              className={`text-sm font-medium ${
                isMounted && isDarkMode ? 'text-gray-300' : 'text-gray-700'
              }`}
            >
              Select All
            </span>
          </label>

          {filteredNotifications.length > 0 && (
            <span
              className={`text-sm ${
                isMounted && isDarkMode ? 'text-gray-400' : 'text-gray-500'
              }`}
            >
              ({filteredNotifications.length} total)
            </span>
          )}

          {selectedNotifications.size > 0 && (
            <motion.span
              initial={{ opacity: 0, scale: 0.8 }}
              animate={{ opacity: 1, scale: 1 }}
              className={`rounded-full px-2 py-1 text-xs font-medium ${
                isMounted && isDarkMode
                  ? 'bg-blue-900/50 text-blue-300'
                  : 'bg-blue-100 text-blue-700'
              }`}
            >
              {selectedNotifications.size} selected
            </motion.span>
          )}
        </div>

        <div className="flex flex-wrap items-center gap-2">
          {unreadCount > 0 && (
            <motion.button
              initial={{ opacity: 0, scale: 0.8 }}
              animate={{ opacity: 1, scale: 1 }}
              onClick={onMarkAllAsRead}
              disabled={isLoading}
              className={`inline-flex items-center gap-2 rounded-lg px-3 py-2 text-sm font-medium transition-all duration-200 focus:ring-2 focus:outline-none disabled:cursor-not-allowed disabled:opacity-50 ${
                isMounted && isDarkMode
                  ? 'bg-blue-600 text-white hover:bg-blue-700 focus:ring-blue-500/50'
                  : 'bg-blue-600 text-white hover:bg-blue-700 focus:ring-blue-500/50'
              }`}
            >
              <CheckCheck size={16} />
              Mark All Read
            </motion.button>
          )}

          {selectedNotifications.size > 0 && (
            <motion.button
              initial={{ opacity: 0, scale: 0.8 }}
              animate={{ opacity: 1, scale: 1 }}
              onClick={onDeleteSelected}
              disabled={isLoading}
              className={`inline-flex items-center gap-2 rounded-lg px-3 py-2 text-sm font-medium transition-all duration-200 focus:ring-2 focus:outline-none disabled:cursor-not-allowed disabled:opacity-50 ${
                isMounted && isDarkMode
                  ? 'bg-red-600 text-white hover:bg-red-700 focus:ring-red-500/50'
                  : 'bg-red-600 text-white hover:bg-red-700 focus:ring-red-500/50'
              }`}
            >
              <Trash2 size={16} />
              Delete ({selectedNotifications.size})
            </motion.button>
          )}

          {filteredNotifications.length > 0 && (
            <button
              onClick={onClearAll}
              disabled={isLoading}
              className={`inline-flex items-center gap-2 rounded-lg border px-3 py-2 text-sm font-medium transition-all duration-200 focus:ring-2 focus:outline-none disabled:cursor-not-allowed disabled:opacity-50 ${
                isMounted && isDarkMode
                  ? 'border-gray-600 bg-gray-800 text-gray-300 hover:border-gray-500 hover:bg-gray-700 focus:ring-gray-500/20'
                  : 'border-gray-300 bg-white text-gray-700 hover:border-gray-400 hover:bg-gray-50 focus:ring-gray-500/20'
              }`}
            >
              <X size={16} />
              Clear All
            </button>
          )}
        </div>
      </div>
    </motion.div>
  );
};
