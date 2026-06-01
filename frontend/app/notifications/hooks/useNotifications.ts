'use client';

import { useState, useEffect, useCallback } from 'react';
import { useRouter } from 'next/navigation';
import toast from 'react-hot-toast';
import authService from '@/Services/AuthenticationService';
import notificationService, {
  Notification,
  NotificationType,
} from '@/Services/NotificationService';
import { NotificationData } from '@/Services/SignalRService';
import useSignalRNotifications from '@/app/hooks/useSignalRNotifications';

interface UseNotificationsReturn {
  notifications: Notification[];
  loading: boolean;
  isLoading: boolean;
  unreadCount: number;
  currentPage: number;
  total: number;
  hasMore: boolean;
  fetchNotifications: () => Promise<void>;
  fetchUnreadCount: () => Promise<void>;
  markAsRead: (notificationId: string) => Promise<void>;
  markAllAsRead: () => Promise<void>;
  deleteNotification: (notificationId: string) => Promise<void>;
  deleteAllNotifications: () => Promise<void>;
  handleNotificationClick: (notification: Notification) => void;
  loadMore: () => void;
  setCurrentPage: (page: number) => void;
  handleBulkDelete: (deletedIds: string[]) => void;
  signalRStats: {
    isConnected: boolean;
    connectionStats: any;
  };
}

const ITEMS_PER_PAGE = 20;

export const useNotifications = (): UseNotificationsReturn => {
  const router = useRouter();
  const [notifications, setNotifications] = useState<Notification[]>([]);
  const [loading, setLoading] = useState(true);
  const [isLoading, setIsLoading] = useState(false);
  const [unreadCount, setUnreadCount] = useState(0);
  const [currentPage, setCurrentPage] = useState(1);
  const [total, setTotal] = useState(0);
  const [hasMore, setHasMore] = useState(true);

  // Setup SignalR notifications with custom handling
  const { isConnected, connectionStats } = useSignalRNotifications({
    autoConnect: true,
    showToasts: true,
    autoRedirect: false,
    onNotification: (notification: NotificationData) => {
      // Handle case mismatch - the actual object has capitalized properties
      const title = (notification as any).Title || notification.title;
      const content = (notification as any).Content || notification.content;
      const time = (notification as any).Time || notification.time;

      // Convert SignalR notification to our format and add to state
      const newNotification: Notification = {
        id: notification.id,
        userId: notification.userId,
        content: content,
        title: title,
        type: notification.type as NotificationType,
        time: time,
        isRead: false,
      };

      setNotifications((prev) => [newNotification, ...prev]);
      setUnreadCount((prev) => prev + 1);
    },
    onMatchStart: (data: NotificationData, simulationId: string) => {
      const content =
        (data as any).Content || data.content || 'Match has started';
      const time = (data as any).Time || data.time;

      const newNotification: Notification = {
        id: data.id,
        userId: data.userId,
        content: content,
        title: 'Match Started',
        type: NotificationType.MatchStart,
        time: time,
        isRead: false,
        simulationId: simulationId,
      };

      setNotifications((prev) => [newNotification, ...prev]);
      setUnreadCount((prev) => prev + 1);
    },
    onMatchEnd: (data: NotificationData, simulationId: string) => {
      const newNotification: Notification = {
        id: data.id,
        userId: data.userId,
        content: data.content || 'Match has ended',
        title: 'Match Ended',
        type: NotificationType.MatchEnd,
        time: data.time,
        isRead: false,
        simulationId: simulationId,
      };

      setNotifications((prev) => [newNotification, ...prev]);
      setUnreadCount((prev) => prev + 1);
    },
    onSimulationUpdate: (data: NotificationData, simulationId: string) => {
      const newNotification: Notification = {
        id: data.id,
        userId: data.userId,
        content: data.content || 'Simulation update available',
        title: 'Simulation Update',
        type: NotificationType.SimulationUpdate,
        time: data.time,
        isRead: false,
        simulationId: simulationId,
      };

      setNotifications((prev) => [newNotification, ...prev]);
      setUnreadCount((prev) => prev + 1);
    },
  });

  const fetchNotifications = useCallback(async () => {
    try {
      setLoading(true);
      const result = await notificationService.getUserNotifications(
        currentPage,
        ITEMS_PER_PAGE
      );

      if (currentPage === 1) {
        setNotifications(result.notifications);
      } else {
        setNotifications((prev) => [...prev, ...result.notifications]);
      }

      setTotal(result.total);
      setHasMore(result.hasMore);
    } catch (error) {
      console.error('Error fetching notifications:', error);
      toast.error('Failed to load notifications');
    } finally {
      setLoading(false);
    }
  }, [currentPage]);

  const fetchUnreadCount = useCallback(async () => {
    try {
      const count = await notificationService.getUnreadCount();
      setUnreadCount(count);
    } catch (error) {
      console.error('Error fetching unread count:', error);
    }
  }, []);

  const markAsRead = useCallback(async (notificationId: string) => {
    try {
      const success = await notificationService.markAsRead(notificationId);
      if (success) {
        setNotifications((prev) =>
          prev.map((n) =>
            n.id === notificationId ? { ...n, isRead: true } : n
          )
        );
        setUnreadCount((prev) => Math.max(0, prev - 1));
        toast.success('Notification marked as read');
      }
    } catch (error) {
      console.error('Error marking notification as read:', error);
      toast.error('Failed to mark notification as read');
    }
  }, []);

  const markAllAsRead = useCallback(async () => {
    try {
      setIsLoading(true);
      const success = await notificationService.markAllAsRead();
      if (success) {
        setNotifications((prev) =>
          prev.map((notification) => ({ ...notification, isRead: true }))
        );
        setUnreadCount(0);
        toast.success('All notifications marked as read');
      }
    } catch (error) {
      console.error('Error marking all notifications as read:', error);
      toast.error('Failed to mark notifications as read');
    } finally {
      setIsLoading(false);
    }
  }, []);

  const deleteNotification = useCallback(
    async (notificationId: string) => {
      try {
        const success =
          await notificationService.deleteNotification(notificationId);
        if (success) {
          const notification = notifications.find(
            (n) => n.id === notificationId
          );
          setNotifications((prev) =>
            prev.filter((n) => n.id !== notificationId)
          );
          if (notification && !notification.isRead) {
            setUnreadCount((prev) => Math.max(0, prev - 1));
          }
          toast.success('Notification deleted');
        }
      } catch (error) {
        console.error('Error deleting notification:', error);
        toast.error('Failed to delete notification');
      }
    },
    [notifications]
  );

  const deleteAllNotifications = useCallback(async () => {
    if (
      !confirm(
        'Are you sure you want to delete ALL notifications? This action cannot be undone.'
      )
    ) {
      return;
    }

    try {
      setIsLoading(true);
      const success = await notificationService.deleteAllNotifications();
      if (success) {
        setNotifications([]);
        setUnreadCount(0);
        toast.success('All notifications cleared');
      }
    } catch (error) {
      console.error('Error clearing all notifications:', error);
      toast.error('Failed to clear notifications');
    } finally {
      setIsLoading(false);
    }
  }, []);

  const handleNotificationClick = useCallback(
    (notification: Notification) => {
      // Mark as read if not already read
      if (!notification.isRead) {
        markAsRead(notification.id);
      }

      // Handle special notification types that require navigation
      if (
        notification.type === NotificationType.MatchStart &&
        notification.simulationId
      ) {
        router.push(`/simulationview/${notification.simulationId}`);
      }
    },
    [markAsRead, router]
  );

  const loadMore = useCallback(() => {
    if (hasMore && !loading) {
      setCurrentPage((prev) => prev + 1);
    }
  }, [hasMore, loading]);

  const handleBulkDelete = useCallback(
    (deletedIds: string[]) => {
      const deletedUnreadCount = notifications.filter(
        (n) => deletedIds.includes(n.id) && !n.isRead
      ).length;

      setNotifications((prev) =>
        prev.filter((n) => !deletedIds.includes(n.id))
      );
      setUnreadCount((prev) => Math.max(0, prev - deletedUnreadCount));
    },
    [notifications]
  );

  useEffect(() => {
    if (authService.isAuthenticated()) {
      fetchNotifications();
      fetchUnreadCount();
    } else {
      router.push('/login');
    }
  }, [router, currentPage, fetchNotifications, fetchUnreadCount]);
  return {
    notifications,
    loading,
    isLoading,
    unreadCount,
    currentPage,
    total,
    hasMore,
    fetchNotifications,
    fetchUnreadCount,
    markAsRead,
    markAllAsRead,
    deleteNotification,
    deleteAllNotifications,
    handleNotificationClick,
    loadMore,
    setCurrentPage,
    handleBulkDelete,
    signalRStats: {
      isConnected,
      connectionStats,
    },
  };
};
