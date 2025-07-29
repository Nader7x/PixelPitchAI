// NotificationService - Simplified API-only service for notifications
// SignalR real-time notifications are now handled by useSignalRNotifications hook
import axios from 'axios';
import authService from './AuthenticationService';

export interface Notification {
  id: string;
  title: string;
  content: string;
  type: NotificationType;
  time: string;
  isRead: boolean;
  userId: string;
  // Additional frontend-specific fields
  matchId?: number; // For match-related notifications
  simulationId?: string; // For simulation-related notifications (from backend SimulationId)
}

export enum NotificationType {
  MatchStart = 'MatchStart',
  MatchEnd = 'MatchEnd',
  SimulationStart = 'SimulationStart',
  SimulationEnd = 'SimulationEnd',
  MatchUpdate = 'MatchUpdate',
  SimulationUpdate = 'SimulationUpdate',
  SystemAlert = 'SystemAlert',
  UserMessage = 'UserMessage',
  Info = 'Info',
  Warning = 'Warning',
  Error = 'Error',
  Success = 'Success',
}

export interface NotificationServiceConfig {
  baseUrl: string;
}

class NotificationService {
  private readonly baseUrl: string;

  constructor(config?: Partial<NotificationServiceConfig>) {
    this.baseUrl =
      config?.baseUrl ||
      process.env.NEXT_PUBLIC_API_BASE_URL ||
      'https://localhost:7082';
  }

  /**
   * Fetch user-specific notifications from API
   */
  public async getUserNotifications(
    page: number = 1,
    limit: number = 20
  ): Promise<{
    notifications: Notification[];
    total: number;
    hasMore: boolean;
  }> {
    try {
      const userId = authService.getCurrentUserId();
      if (!userId) {
        throw new Error('User not authenticated');
      }

      const response = await axios.get(
        `${this.baseUrl}/api/notifications/user/${userId}`,
        {
          params: { page, limit },
          headers: this.getAuthHeaders(),
        }
      );

      const { notifications, total, hasMore } = response.data;
      return { notifications, total, hasMore };
    } catch (error) {
      console.error('Error fetching user notifications:', error);
      throw error;
    }
  }

  /**
   * Get unread notification count for user
   */
  public async getUnreadCount(): Promise<number> {
    try {
      const userId = authService.getCurrentUserId();
      if (!userId) {
        return 0; // No user authenticated
      }

      const response = await axios.get(
        `${this.baseUrl}/api/notifications/user/${userId}/unread-count`,
        {
          headers: this.getAuthHeaders(),
        }
      );

      return response.data || 0;
    } catch (error) {
      console.error('Error fetching unread notification count:', error);
      return 0;
    }
  }

  /**
   * Mark a specific notification as read
   */
  public async markAsRead(notificationId: string): Promise<boolean> {
    try {
      await axios.post(
        `${this.baseUrl}/api/notifications/mark-as-read/${notificationId}`,
        {},
        {
          headers: this.getAuthHeaders(),
        }
      );

      return true;
    } catch (error) {
      console.error('Error marking notification as read:', error);
      return false;
    }
  }

  /**
   * Mark all user notifications as read
   */
  public async markAllAsRead(): Promise<boolean> {
    try {
      const userId = authService.getCurrentUserId();
      if (!userId) {
        throw new Error('User not authenticated');
      }

      await axios.post(
        `${this.baseUrl}/api/notifications/user/${userId}/mark-all-read`,
        {},
        {
          headers: this.getAuthHeaders(),
        }
      );

      return true;
    } catch (error) {
      console.error('Error marking all notifications as read:', error);
      return false;
    }
  }

  /**
   * Delete a specific notification
   */
  public async deleteNotification(notificationId: string): Promise<boolean> {
    try {
      await axios.delete(
        `${this.baseUrl}/api/notifications/${notificationId}`,
        {
          headers: this.getAuthHeaders(),
        }
      );

      return true;
    } catch (error) {
      console.error('Error deleting notification:', error);
      return false;
    }
  }

  /**
   * Delete all user notifications
   */
  public async deleteAllNotifications(): Promise<boolean> {
    try {
      const userId = authService.getCurrentUserId();
      if (!userId) {
        throw new Error('User not authenticated');
      }

      await axios.delete(
        `${this.baseUrl}/api/notifications/user/${userId}/all`,
        {
          headers: this.getAuthHeaders(),
        }
      );

      return true;
    } catch (error) {
      console.error('Error deleting all notifications:', error);
      return false;
    }
  }

  /**
   * Send a test notification (for development/testing)
   */
  public async sendTestNotification(
    type: NotificationType = NotificationType.Info
  ): Promise<boolean> {
    try {
      const userId = authService.getCurrentUserId();
      if (!userId) {
        throw new Error('User not authenticated');
      }

      await axios.post(
        `${this.baseUrl}/api/notifications/test`,
        {
          userId,
          type,
          message: `Test ${type} notification`,
          title: 'Test Notification',
        },
        {
          headers: this.getAuthHeaders(),
        }
      );

      return true;
    } catch (error) {
      console.error('Error sending test notification:', error);
      return false;
    }
  }

  /**
   * Set user preference for auto-redirect to match simulation
   */
  public setAutoRedirectPreference(enabled: boolean): void {
    localStorage.setItem('autoRedirectToMatch', enabled.toString());
  }

  /**
   * Get user preference for auto-redirect to match simulation
   */
  public getAutoRedirectPreference(): boolean {
    return localStorage.getItem('autoRedirectToMatch') === 'true';
  }

  /**
   * Manually trigger navigation to simulation page
   */
  public navigateToSimulation(simulationId: string): void {
    if (typeof window !== 'undefined') {
      const redirectUrl = `/simulationview/${simulationId}`;
      window.location.href = redirectUrl;
    }
  }

  /**
   * Get authorization headers for API requests
   */
  private getAuthHeaders(): Record<string, string> {
    // Use the same token retrieval logic as AuthenticationService
    const token =
      localStorage.getItem('accessToken') ||
      sessionStorage.getItem('accessToken');
    return token ? { Authorization: `Bearer ${token}` } : {};
  }

  /**
   * Get service status
   */
  public getStatus(): {
    isInitialized: boolean;
  } {
    return {
      isInitialized: true,
    };
  }
}

// Create and export singleton instance
const notificationService = new NotificationService();
export default notificationService;
