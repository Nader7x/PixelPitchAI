'use client';

import { useState, useEffect, useMemo } from 'react';
import { Notification, NotificationType } from '@/Services/NotificationService';

export interface FilterOptions {
  type?: NotificationType;
  read?: boolean;
  dateRange?: 'today' | 'week' | 'month' | 'all';
}

interface UseNotificationFiltersReturn {
  filteredNotifications: Notification[];
  searchQuery: string;
  filter: FilterOptions;
  setSearchQuery: (query: string) => void;
  setFilter: (filter: FilterOptions) => void;
  resetFilters: () => void;
}

export const useNotificationFilters = (
  notifications: Notification[]
): UseNotificationFiltersReturn => {
  const [searchQuery, setSearchQuery] = useState('');
  const [filter, setFilter] = useState<FilterOptions>({ dateRange: 'all' });

  const filteredNotifications = useMemo(() => {
    let filtered = [...notifications];

    // Apply search filter
    if (searchQuery.trim()) {
      const query = searchQuery.toLowerCase();
      filtered = filtered.filter(
        (notification) =>
          notification.content.toLowerCase().includes(query) ||
          (notification.title &&
            notification.title.toLowerCase().includes(query))
      );
    }

    // Apply type filter
    if (filter.type) {
      filtered = filtered.filter(
        (notification) => notification.type === filter.type
      );
    }

    // Apply read status filter
    if (filter.read !== undefined) {
      filtered = filtered.filter(
        (notification) => notification.isRead === filter.read
      );
    }

    // Apply date range filter
    if (filter.dateRange && filter.dateRange !== 'all') {
      const now = new Date();
      const filterDate = new Date();

      switch (filter.dateRange) {
        case 'today':
          filterDate.setHours(0, 0, 0, 0);
          break;
        case 'week':
          filterDate.setDate(now.getDate() - 7);
          break;
        case 'month':
          filterDate.setMonth(now.getMonth() - 1);
          break;
      }

      filtered = filtered.filter(
        (notification) => new Date(notification.time) >= filterDate
      );
    }

    return filtered;
  }, [notifications, searchQuery, filter]);

  const resetFilters = () => {
    setFilter({ dateRange: 'all' });
    setSearchQuery('');
  };

  return {
    filteredNotifications,
    searchQuery,
    filter,
    setSearchQuery,
    setFilter,
    resetFilters,
  };
};
