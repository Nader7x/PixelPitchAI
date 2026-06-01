'use client';

import React from 'react';
import { motion } from 'framer-motion';
import { Clock, CheckCheck, X } from 'lucide-react';
import { Notification } from '@/Services/NotificationService';
import {
  getNotificationIcon,
  getNotificationTypeColor,
} from '../utils/notificationUtils';

interface NotificationItemProps {
  notification: Notification;
  index: number;
  isSelected: boolean;
  onToggleSelection: (id: string) => void;
  onMarkAsRead: (id: string) => void;
  onDelete: (id: string) => void;
  onClick: (notification: Notification) => void;
  isDarkMode: boolean;
  isMounted: boolean;
}

export const NotificationItem: React.FC<NotificationItemProps> = ({
  notification,
  index,
  isSelected,
  onToggleSelection,
  onMarkAsRead,
  onDelete,
  onClick,
  isDarkMode,
  isMounted,
}) => {
  return (
    <motion.div
      key={notification.id}
      initial={{ opacity: 0, x: -20 }}
      animate={{ opacity: 1, x: 0 }}
      exit={{ opacity: 0, x: 20, height: 0 }}
      transition={{
        delay: index * 0.03,
        duration: 0.3,
        ease: 'easeOut',
      }}
      layout
      className={`group relative transition-all duration-200 ${
        !notification.isRead
          ? isMounted && isDarkMode
            ? 'border-l-4 border-l-blue-400 bg-gradient-to-r from-blue-900/30 to-transparent'
            : 'border-l-4 border-l-blue-500 bg-gradient-to-r from-blue-50/50 to-transparent'
          : ''
      } ${
        isSelected
          ? isMounted && isDarkMode
            ? 'bg-blue-900/30 ring-2 ring-blue-400/50'
            : 'bg-blue-50 ring-2 ring-blue-200'
          : isMounted && isDarkMode
            ? 'hover:bg-gray-700'
            : 'hover:bg-gray-50'
      }`}
    >
      <div className="p-4 lg:p-6">
        <div className="flex items-start gap-3 lg:gap-4">
          {/* Selection Checkbox */}
          <div className="flex items-center pt-1">
            <input
              type="checkbox"
              checked={isSelected}
              onChange={() => onToggleSelection(notification.id)}
              className={`h-4 w-4 rounded border transition-colors focus:ring-2 focus:ring-offset-0 ${
                isMounted && isDarkMode
                  ? 'border-gray-600 text-blue-400 focus:ring-blue-400/20'
                  : 'border-gray-300 text-blue-600 focus:ring-blue-500/20'
              }`}
            />
          </div>

          {/* Notification Icon */}
          <div className="flex-shrink-0 pt-1">
            <div
              className={`rounded-lg border p-2 shadow-sm ${
                isMounted && isDarkMode
                  ? 'border-gray-600 bg-gray-700'
                  : 'border-gray-100 bg-white'
              }`}
            >
              {getNotificationIcon(notification.type)}
            </div>
          </div>

          {/* Notification Content */}
          <div
            className="min-w-0 flex-1 cursor-pointer"
            onClick={() => onClick(notification)}
          >
            <div className="mb-3 flex items-start justify-between gap-3">
              <div className="min-w-0 flex-1">
                {notification.title && (
                  <h3
                    className={`mb-1 line-clamp-2 text-base font-semibold lg:text-lg ${
                      isMounted && isDarkMode ? 'text-white' : 'text-gray-900'
                    }`}
                  >
                    {notification.title}
                  </h3>
                )}
                <p
                  className={`line-clamp-3 text-sm leading-relaxed lg:text-base ${
                    isMounted && isDarkMode ? 'text-gray-300' : 'text-gray-700'
                  }`}
                >
                  {notification.content}
                </p>
              </div>

              {!notification.isRead && (
                <motion.div
                  initial={{ scale: 0 }}
                  animate={{ scale: 1 }}
                  className={`mt-1 h-3 w-3 flex-shrink-0 rounded-full ring-2 ${
                    isMounted && isDarkMode
                      ? 'bg-blue-400 ring-blue-400/50'
                      : 'bg-blue-500 ring-blue-200'
                  }`}
                />
              )}
            </div>

            <div className="flex items-center justify-between gap-3">
              <div className="flex flex-wrap items-center gap-3">
                <span
                  className={`inline-flex items-center rounded-full border px-2.5 py-1 text-xs font-medium ${getNotificationTypeColor(
                    notification.type,
                    isMounted && isDarkMode
                  )}`}
                >
                  {notification.type.replace(/([A-Z])/g, ' $1').trim()}
                </span>

                <div
                  className={`flex items-center gap-1 text-xs lg:text-sm ${
                    isMounted && isDarkMode ? 'text-gray-400' : 'text-gray-500'
                  }`}
                >
                  <Clock className="h-3 w-3 lg:h-4 lg:w-4" />
                  <time dateTime={notification.time}>
                    {new Date(notification.time).toLocaleString(undefined, {
                      month: 'short',
                      day: 'numeric',
                      hour: '2-digit',
                      minute: '2-digit',
                    })}
                  </time>
                </div>
              </div>

              {/* Action Buttons */}
              <div className="flex items-center gap-1 opacity-0 transition-opacity group-hover:opacity-100">
                {!notification.isRead && (
                  <button
                    onClick={(e) => {
                      e.stopPropagation();
                      onMarkAsRead(notification.id);
                    }}
                    className={`rounded-lg p-2 transition-all duration-200 focus:ring-2 focus:outline-none ${
                      isMounted && isDarkMode
                        ? 'text-blue-400 hover:bg-blue-900/50 hover:text-blue-300 focus:ring-blue-400/50'
                        : 'text-blue-600 hover:bg-blue-100 hover:text-blue-700 focus:ring-blue-500/50'
                    }`}
                    title="Mark as read"
                  >
                    <CheckCheck size={16} />
                  </button>
                )}

                <button
                  onClick={(e) => {
                    e.stopPropagation();
                    onDelete(notification.id);
                  }}
                  className={`rounded-lg p-2 transition-all duration-200 focus:ring-2 focus:outline-none ${
                    isMounted && isDarkMode
                      ? 'text-gray-400 hover:bg-red-900/50 hover:text-red-400 focus:ring-red-400/50'
                      : 'text-gray-400 hover:bg-red-100 hover:text-red-600 focus:ring-red-500/50'
                  }`}
                  title="Delete notification"
                >
                  <X size={16} />
                </button>
              </div>
            </div>
          </div>
        </div>
      </div>
    </motion.div>
  );
};
