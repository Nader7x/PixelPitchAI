'use client';

import { useState, useCallback } from 'react';
import { Notification } from '@/Services/NotificationService';
import toast from 'react-hot-toast';
import notificationService from '@/Services/NotificationService';

interface UseNotificationSelectionReturn {
  selectedNotifications: Set<string>;
  toggleNotificationSelection: (notificationId: string) => void;
  toggleSelectAll: (notifications: Notification[]) => void;
  clearSelection: () => void;
  deleteSelectedNotifications: () => Promise<string[]>;
  isLoading: boolean;
}

export const useNotificationSelection = (): UseNotificationSelectionReturn => {
  const [selectedNotifications, setSelectedNotifications] = useState<
    Set<string>
  >(new Set());
  const [isLoading, setIsLoading] = useState(false);

  const toggleNotificationSelection = useCallback((notificationId: string) => {
    setSelectedNotifications((prev) => {
      const newSet = new Set(prev);
      if (newSet.has(notificationId)) {
        newSet.delete(notificationId);
      } else {
        newSet.add(notificationId);
      }
      return newSet;
    });
  }, []);

  const toggleSelectAll = useCallback((notifications: Notification[]) => {
    setSelectedNotifications((prev) => {
      if (prev.size === notifications.length) {
        return new Set();
      } else {
        return new Set(notifications.map((n) => n.id));
      }
    });
  }, []);

  const clearSelection = useCallback(() => {
    setSelectedNotifications(new Set());
  }, []);
  const deleteSelectedNotifications = useCallback(async (): Promise<
    string[]
  > => {
    if (selectedNotifications.size === 0) return [];

    try {
      setIsLoading(true);
      const deletedIds = Array.from(selectedNotifications);
      const deletePromises = deletedIds.map((id) =>
        notificationService.deleteNotification(id)
      );

      await Promise.all(deletePromises);
      setSelectedNotifications(new Set());

      toast.success(`${deletedIds.length} notifications deleted`);

      // Return the deleted IDs so the parent can update its state
      return deletedIds;
    } catch (error) {
      console.error('Error deleting notifications:', error);
      toast.error('Failed to delete selected notifications');
      return [];
    } finally {
      setIsLoading(false);
    }
  }, [selectedNotifications]);

  return {
    selectedNotifications,
    toggleNotificationSelection,
    toggleSelectAll,
    clearSelection,
    deleteSelectedNotifications,
    isLoading,
  };
};
