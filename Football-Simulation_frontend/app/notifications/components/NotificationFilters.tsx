'use client';

import React from 'react';
import { motion } from 'framer-motion';
import { Search } from 'lucide-react';
import { NotificationType } from '@/Services/NotificationService';
import { FilterOptions } from '../hooks/useNotificationFilters';

interface NotificationFiltersProps {
  searchQuery: string;
  filter: FilterOptions;
  onSearchChange: (query: string) => void;
  onFilterChange: (filter: FilterOptions) => void;
  onResetFilters: () => void;
  isDarkMode: boolean;
  isMounted: boolean;
}

export const NotificationFilters: React.FC<NotificationFiltersProps> = ({
  searchQuery,
  filter,
  onSearchChange,
  onFilterChange,
  onResetFilters,
  isDarkMode,
  isMounted,
}) => {
  return (
    <motion.div
      initial={{ opacity: 0, y: 20 }}
      animate={{ opacity: 1, y: 0 }}
      className={`mb-4 rounded-xl border p-4 shadow-sm lg:mb-6 lg:p-6 ${
        isMounted && isDarkMode
          ? 'border-gray-700 bg-gray-800'
          : 'border-gray-200 bg-white'
      }`}
    >
      <div className="flex flex-col gap-4">
        {/* Search */}
        <div className="w-full">
          <div className="relative">
            <Search
              className={`absolute top-1/2 left-3 h-4 w-4 -translate-y-1/2 transform ${
                isMounted && isDarkMode ? 'text-gray-400' : 'text-gray-400'
              }`}
            />
            <input
              type="text"
              placeholder="Search notifications..."
              value={searchQuery}
              onChange={(e) => onSearchChange(e.target.value)}
              className={`w-full rounded-lg border py-2.5 pr-4 pl-10 text-sm transition-all duration-200 placeholder:text-gray-400 focus:ring-2 focus:outline-none ${
                isMounted && isDarkMode
                  ? 'border-gray-600 bg-gray-700 text-white focus:border-blue-400 focus:ring-blue-400/20'
                  : 'border-gray-300 bg-white text-gray-900 focus:border-blue-500 focus:ring-blue-500/20'
              }`}
            />
          </div>
        </div>

        {/* Filters Row */}
        <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-4">
          {/* Type Filter */}
          <div>
            <label
              className={`mb-1 block text-xs font-medium ${
                isMounted && isDarkMode ? 'text-gray-300' : 'text-gray-700'
              }`}
            >
              Type
            </label>
            <select
              value={filter.type || ''}
              onChange={(e) =>
                onFilterChange({
                  ...filter,
                  type: (e.target.value as NotificationType) || undefined,
                })
              }
              className={`w-full rounded-lg border px-3 py-2 text-sm transition-all duration-200 focus:ring-2 focus:outline-none ${
                isMounted && isDarkMode
                  ? 'border-gray-600 bg-gray-700 text-white focus:border-blue-400 focus:ring-blue-400/20'
                  : 'border-gray-300 bg-white text-gray-900 focus:border-blue-500 focus:ring-blue-500/20'
              }`}
            >
              <option value="">All Types</option>
              {Object.values(NotificationType).map((type) => (
                <option key={type} value={type}>
                  {type.replace(/([A-Z])/g, ' $1').trim()}
                </option>
              ))}
            </select>
          </div>

          {/* Read Status Filter */}
          <div>
            <label
              className={`mb-1 block text-xs font-medium ${
                isMounted && isDarkMode ? 'text-gray-300' : 'text-gray-700'
              }`}
            >
              Status
            </label>
            <select
              value={filter.read === undefined ? '' : filter.read.toString()}
              onChange={(e) =>
                onFilterChange({
                  ...filter,
                  read:
                    e.target.value === ''
                      ? undefined
                      : e.target.value === 'true',
                })
              }
              className={`w-full rounded-lg border px-3 py-2 text-sm transition-all duration-200 focus:ring-2 focus:outline-none ${
                isMounted && isDarkMode
                  ? 'border-gray-600 bg-gray-700 text-white focus:border-blue-400 focus:ring-blue-400/20'
                  : 'border-gray-300 bg-white text-gray-900 focus:border-blue-500 focus:ring-blue-500/20'
              }`}
            >
              <option value="">All Status</option>
              <option value="false">Unread</option>
              <option value="true">Read</option>
            </select>
          </div>

          {/* Date Range Filter */}
          <div>
            <label
              className={`mb-1 block text-xs font-medium ${
                isMounted && isDarkMode ? 'text-gray-300' : 'text-gray-700'
              }`}
            >
              Time Period
            </label>
            <select
              value={filter.dateRange || 'all'}
              onChange={(e) =>
                onFilterChange({
                  ...filter,
                  dateRange: e.target.value as any,
                })
              }
              className={`w-full rounded-lg border px-3 py-2 text-sm transition-all duration-200 focus:ring-2 focus:outline-none ${
                isMounted && isDarkMode
                  ? 'border-gray-600 bg-gray-700 text-white focus:border-blue-400 focus:ring-blue-400/20'
                  : 'border-gray-300 bg-white text-gray-900 focus:border-blue-500 focus:ring-blue-500/20'
              }`}
            >
              <option value="all">All Time</option>
              <option value="today">Today</option>
              <option value="week">This Week</option>
              <option value="month">This Month</option>
            </select>
          </div>

          {/* Reset Filters */}
          <div className="flex items-end">
            <button
              onClick={onResetFilters}
              className={`w-full rounded-lg border px-4 py-2 text-sm font-medium transition-all duration-200 focus:ring-2 focus:outline-none ${
                isMounted && isDarkMode
                  ? 'border-gray-600 text-gray-300 hover:border-gray-500 hover:bg-gray-700 focus:ring-gray-500/20'
                  : 'border-gray-300 text-gray-700 hover:border-gray-400 hover:bg-gray-50 focus:ring-gray-500/20'
              }`}
            >
              Reset Filters
            </button>
          </div>
        </div>
      </div>
    </motion.div>
  );
};
