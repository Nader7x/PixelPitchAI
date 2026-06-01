'use client';

import React from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import { Notification } from '@/Services/NotificationService';
import { NotificationItem } from './NotificationItem';
import { EmptyState } from './EmptyState';

interface NotificationListProps {
  notifications: Notification[];
  filteredNotifications: Notification[];
  selectedNotifications: Set<string>;
  loading: boolean;
  hasMore: boolean;
  onToggleSelection: (id: string) => void;
  onMarkAsRead: (id: string) => void;
  onDelete: (id: string) => void;
  onClick: (notification: Notification) => void;
  onLoadMore: () => void;
  onResetFilters: () => void;
  isDarkMode: boolean;
  isMounted: boolean;
}

export const NotificationList: React.FC<NotificationListProps> = ({
  notifications,
  filteredNotifications,
  selectedNotifications,
  loading,
  hasMore,
  onToggleSelection,
  onMarkAsRead,
  onDelete,
  onClick,
  onLoadMore,
  onResetFilters,
  isDarkMode,
  isMounted,
}) => {
  return (
    <motion.div
      initial={{ opacity: 0, y: 20 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ delay: 0.2 }}
      className={`overflow-hidden rounded-xl border shadow-sm ${
        isMounted && isDarkMode
          ? 'border-gray-700 bg-gray-800'
          : 'border-gray-200 bg-white'
      }`}
    >
      {filteredNotifications.length === 0 ? (
        <EmptyState
          hasNotifications={notifications.length > 0}
          onResetFilters={onResetFilters}
          isDarkMode={isDarkMode}
          isMounted={isMounted}
        />
      ) : (
        <div
          className={`divide-y ${
            isMounted && isDarkMode ? 'divide-gray-700' : 'divide-gray-100'
          }`}
        >
          <AnimatePresence mode="popLayout">
            {filteredNotifications.map((notification, index) => (
              <NotificationItem
                key={notification.id}
                notification={notification}
                index={index}
                isSelected={selectedNotifications.has(notification.id)}
                onToggleSelection={onToggleSelection}
                onMarkAsRead={onMarkAsRead}
                onDelete={onDelete}
                onClick={onClick}
                isDarkMode={isDarkMode}
                isMounted={isMounted}
              />
            ))}
          </AnimatePresence>
        </div>
      )}

      {/* Load More Button */}
      {hasMore && filteredNotifications.length > 0 && (
        <div
          className={`border-t p-4 text-center lg:p-6 ${
            isMounted && isDarkMode
              ? 'border-gray-700 bg-gray-700/50'
              : 'border-gray-100 bg-gray-50/50'
          }`}
        >
          <motion.button
            initial={{ opacity: 0, y: 20 }}
            animate={{ opacity: 1, y: 0 }}
            onClick={onLoadMore}
            disabled={loading}
            className={`inline-flex items-center gap-2 rounded-lg border px-6 py-3 text-sm font-medium transition-all duration-200 focus:ring-2 focus:outline-none disabled:cursor-not-allowed disabled:opacity-50 ${
              isMounted && isDarkMode
                ? 'border-gray-600 bg-gray-800 text-gray-300 hover:border-gray-500 hover:bg-gray-700 focus:ring-gray-500/20'
                : 'border-gray-300 bg-white text-gray-700 hover:border-gray-400 hover:bg-gray-50 focus:ring-gray-500/20'
            }`}
          >
            {loading ? (
              <>
                <div
                  className={`h-4 w-4 animate-spin rounded-full border-2 border-t-transparent ${
                    isMounted && isDarkMode
                      ? 'border-gray-400'
                      : 'border-gray-400'
                  }`}
                ></div>
                Loading more...
              </>
            ) : (
              'Load More Notifications'
            )}
          </motion.button>
        </div>
      )}
    </motion.div>
  );
};
