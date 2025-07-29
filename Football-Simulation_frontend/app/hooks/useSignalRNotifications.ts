'use client';

import { useEffect, useCallback, useState, useRef } from 'react';
import { useRouter } from 'next/navigation';
import toast from 'react-hot-toast';
import signalRService, {
  NotificationData,
  NotificationType,
} from '@/Services/SignalRService';
import notificationService, {
  Notification,
} from '@/Services/NotificationService';
import authService from '@/Services/AuthenticationService';

interface UseSignalRNotificationsOptions {
  autoConnect?: boolean;
  autoRedirectOnMatchStart?: boolean;
  showToasts?: boolean;
  autoRedirect?: boolean;
  onNotificationReceived?: (notification: NotificationData) => void;
  onNotification?: (notification: NotificationData) => void;
  onMatchStart?: (notification: NotificationData, simulationId: string) => void;
  onMatchEnd?: (notification: NotificationData, simulationId: string) => void;
  onSimulationUpdate?: (
    notification: NotificationData,
    simulationId: string
  ) => void;
}

export const useSignalRNotifications = (
  options: UseSignalRNotificationsOptions = {}
) => {
  const {
    autoConnect = false,
    autoRedirectOnMatchStart = false,
    autoRedirect = false,
    showToasts = true,
    onNotificationReceived,
    onNotification,
    onMatchStart,
    onMatchEnd,
    onSimulationUpdate,
  } = options;

  const router = useRouter();
  const [isConnected, setIsConnected] = useState(false);
  const [connectionStats, setConnectionStats] = useState<any>(null);

  // Use refs to store the latest callback functions to avoid re-initializing SignalR
  const callbackRefs = useRef({
    onNotificationReceived,
    onNotification,
    onMatchStart,
    onMatchEnd,
    onSimulationUpdate,
  });

  // Update refs when callbacks change
  useEffect(() => {
    callbackRefs.current = {
      onNotificationReceived,
      onNotification,
      onMatchStart,
      onMatchEnd,
      onSimulationUpdate,
    };
  }, [
    onNotificationReceived,
    onNotification,
    onMatchStart,
    onMatchEnd,
    onSimulationUpdate,
  ]);

  // Initialize SignalR connection
  const initializeConnection = useCallback(async () => {
    if (!authService.isAuthenticated()) {
      console.warn(
        'User not authenticated, cannot establish SignalR connections'
      );
      return false;
    }

    try {
      const connected = await signalRService.ensureConnection();
      setIsConnected(connected);

      if (connected) {
        setConnectionStats(signalRService.getConnectionStats());
        console.log('SignalR notifications initialized successfully');
      }

      return connected;
    } catch (error) {
      console.error('Failed to initialize SignalR connection:', error);
      setIsConnected(false);
      return false;
    }
  }, []); // Handle generic notifications
  const handleNotification = useCallback(
    (notification: NotificationData) => {
      console.log('Received notification:', notification);
      if (showToasts) {
        // Handle case mismatch - the actual object has capitalized properties
        const title = (notification as any).Title || notification.title;
        const content = (notification as any).Content || notification.content;

        const toastMessage = title ? `${title}: ${content}` : content;

        console.log(
          '🍞 Showing toast:',
          toastMessage,
          'Type:',
          notification.type,
          'Raw notification:',
          notification
        );

        switch (notification.type) {
          case NotificationType.Error:
            toast.error(toastMessage);
            break;
          case NotificationType.Warning:
            toast(toastMessage, { icon: '⚠️' });
            break;
          case NotificationType.Success:
            toast.success(toastMessage);
            break;
          case NotificationType.Info:
            toast(toastMessage, { icon: 'ℹ️' });
            break;
          default:
            toast(toastMessage);
        }
        console.log('🍞 Toast call completed');
      } else {
        console.log('🍞 Toast disabled, showToasts:', showToasts);
      }

      // Use the latest callback from ref
      callbackRefs.current.onNotificationReceived?.(notification);
      callbackRefs.current.onNotification?.(notification);
    },
    [showToasts] // Remove callback dependencies
  );
  // Handle match start notifications
  const handleMatchStart = useCallback(
    (notification: NotificationData, simulationId: string) => {
      console.log(
        'Match started notification:',
        notification,
        'Simulation ID:',
        simulationId
      );
      if (showToasts) {
        // Handle case mismatch - the actual object has capitalized properties
        const content = (notification as any).Content || notification.content;

        toast.success(`⚽ Match Started! ${content}`, {
          duration: 6000,
        });

        if (!autoRedirectOnMatchStart && !autoRedirect) {
          console.log(
            'Match started - to view match, navigate to:',
            `/simulationview/${simulationId}`
          );
        }
      }

      if (autoRedirectOnMatchStart) {
        setTimeout(() => {
          router.push(`/simulationview/${simulationId}`);
        }, 2000);
      }

      // Use the latest callback from ref
      callbackRefs.current.onMatchStart?.(notification, simulationId);
    },
    [showToasts, autoRedirectOnMatchStart, router] // Remove callback dependency
  );
  // Handle match end notifications
  const handleMatchEnd = useCallback(
    (notification: NotificationData, simulationId: string) => {
      console.log(
        'Match ended notification:',
        notification,
        'Simulation ID:',
        simulationId
      );
      if (showToasts) {
        // Handle case mismatch - the actual object has capitalized properties
        const content = (notification as any).Content || notification.content;

        toast.success(`🎉 Match Completed! ${content}`, {
          duration: 8000,
        });

        console.log(
          'Match completed - to view results, navigate to:',
          `/simulationview/${simulationId}`
        );
      }

      // Use the latest callback from ref
      callbackRefs.current.onMatchEnd?.(notification, simulationId);
    },
    [showToasts] // Remove callback and router dependencies
  );
  // Handle match update notifications
  const handleMatchUpdate = useCallback(
    (notification: NotificationData, simulationId: string) => {
      console.log(
        'Match update notification:',
        notification,
        'Simulation ID:',
        simulationId
      );
      if (showToasts) {
        // Handle case mismatch - the actual object has capitalized properties
        const content = (notification as any).Content || notification.content;

        toast(`⚽ ${content}`, {
          duration: 4000,
          icon: '🏃‍♂️',
        });
      }

      // Use the latest callback from ref
      callbackRefs.current.onSimulationUpdate?.(notification, simulationId);
    },
    [showToasts] // Remove callback dependency
  );

  // Handle simulation update notifications
  const handleSimulationUpdate = useCallback(
    (notification: NotificationData, simulationId: string) => {
      console.log(
        'Simulation update notification:',
        notification,
        'Simulation ID:',
        simulationId
      );
      if (showToasts) {
        // Handle case mismatch - the actual object has capitalized properties
        const content = (notification as any).Content || notification.content;

        toast(`📊 Simulation Update: ${content}`, {
          duration: 3000,
        });
      }

      // Use the latest callback from ref
      callbackRefs.current.onSimulationUpdate?.(notification, simulationId);
    },
    [showToasts] // Remove callback dependency
  );

  // Handle welcome messages
  const handleMessage = useCallback(
    (message: string) => {
      console.log('Received message:', message);

      if (showToasts && message) {
        toast.success(message, {
          duration: 3000,
          icon: '👋',
        });
      }
    },
    [showToasts]
  );
  // Setup all SignalR event listeners
  useEffect(() => {
    if (!authService.isAuthenticated()) return;

    let mounted = true;

    const setupListeners = async () => {
      const connected = await initializeConnection();

      if (!connected || !mounted) return;

      // Set up all notification listeners
      signalRService.onNotification(handleNotification);
      signalRService.onMatchStartNotificationAsync(handleMatchStart);
      signalRService.onMatchEndNotificationAsync(handleMatchEnd);
      signalRService.onMatchUpdateNotificationAsync(handleMatchUpdate);
      signalRService.onSimulationStartNotification(handleSimulationUpdate);
      signalRService.onMessageAsync(handleMessage);

      console.log('SignalR notification listeners setup complete');
    };

    setupListeners();

    return () => {
      mounted = false;
      // Don't remove listeners here as they might be used by other components
      // The service handles listener cleanup properly
    };
  }, [
    // Only include initializeConnection to prevent infinite re-runs
    // The callbacks are memoized with useCallback and will be stable
    initializeConnection,
  ]);

  // Cleanup on unmount
  useEffect(() => {
    return () => {
      // Only cleanup if this is the last component using the hook
      // In a real app, you might want to implement reference counting
    };
  }, []);

  // Refresh connection stats periodically
  useEffect(() => {
    if (!isConnected) return;

    const interval = setInterval(() => {
      setConnectionStats(signalRService.getConnectionStats());
    }, 5000);

    return () => clearInterval(interval);
  }, [isConnected]);

  return {
    isConnected,
    connectionStats,
    initializeConnection,
    // Expose methods for manual control
    reconnect: signalRService.resetConnection.bind(signalRService),
    disconnect: signalRService.disconnect.bind(signalRService),
    getConnectionState: () => ({
      matchSimulation: signalRService.getMatchSimulationConnectionState(),
      notification: signalRService.getNotificationConnectionState(),
    }),
  };
};

export default useSignalRNotifications;
