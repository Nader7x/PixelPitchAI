'use client';

import { useEffect, useRef, useState } from 'react';
import { useRouter } from 'next/navigation';
import toast from 'react-hot-toast';
import authService from '@/Services/AuthenticationService';
import notificationService, {
  NotificationData,
  NotificationType,
} from '@/Services/NotificationService';

export interface UseNotificationsSseOptions {
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
  onExperimentCompleted?: (payload: Record<string, unknown>) => void;
}

function getApiBaseUrl(): string {
  const raw =
    process.env.NEXT_PUBLIC_API_BASE_URL ||
    process.env.NEXT_PUBLIC_API_URL ||
    'http://localhost:5025';
  return raw.replace(/\/api\/?$/, '');
}

function getAuthToken(): string {
  if (typeof window === 'undefined') return '';
  return (
    (authService as unknown as { getToken?: () => string }).getToken?.() ||
    localStorage.getItem('accessToken') ||
    localStorage.getItem('token') ||
    sessionStorage.getItem('accessToken') ||
    sessionStorage.getItem('token') ||
    ''
  );
}

/**
 * Pure SSE notifications hook (`GET /api/v2/notifications/stream`) replacing SignalR `/Notify`.
 */
export const useNotificationsSse = (
  options: UseNotificationsSseOptions = {}
) => {
  const {
    autoConnect = true,
    autoRedirectOnMatchStart = false,
    autoRedirect = false,
    showToasts = true,
    onNotificationReceived,
    onNotification,
    onMatchStart,
    onMatchEnd,
    onSimulationUpdate,
    onExperimentCompleted,
  } = options;

  const router = useRouter();
  const [isConnected, setIsConnected] = useState(false);
  const [lastEventId, setLastEventId] = useState<string | null>(null);

  const callbackRefs = useRef({
    onNotificationReceived,
    onNotification,
    onMatchStart,
    onMatchEnd,
    onSimulationUpdate,
    onExperimentCompleted,
  });

  useEffect(() => {
    callbackRefs.current = {
      onNotificationReceived,
      onNotification,
      onMatchStart,
      onMatchEnd,
      onSimulationUpdate,
      onExperimentCompleted,
    };
  }, [
    onNotificationReceived,
    onNotification,
    onMatchStart,
    onMatchEnd,
    onSimulationUpdate,
    onExperimentCompleted,
  ]);

  useEffect(() => {
    if (!autoConnect) return;
    if (!authService.isAuthenticated()) return;

    const token = getAuthToken();
    const baseUrl = getApiBaseUrl();
    const params = new URLSearchParams();
    if (token) params.set('access_token', token);

    const url = `${baseUrl}/api/v2/notifications/stream${params.toString() ? `?${params.toString()}` : ''}`;
    const eventSource = new EventSource(url);

    eventSource.onopen = () => {
      setIsConnected(true);
    };

    const normalizeNotification = (raw: Record<string, any>): NotificationData => ({
      id: String(raw.id ?? raw.Id ?? crypto.randomUUID?.() ?? Date.now()),
      title: String(raw.title ?? raw.Title ?? 'Notification'),
      content: String(raw.content ?? raw.Content ?? raw.message ?? ''),
      type: (raw.type ?? raw.Type ?? NotificationType.Info) as NotificationType,
      time: String(raw.time ?? raw.Time ?? new Date().toISOString()),
      isRead: Boolean(raw.isRead ?? raw.IsRead ?? false),
      userId: String(raw.userId ?? raw.UserId ?? ''),
      simulationId: raw.simulationId ?? raw.SimulationId,
      matchId: raw.matchId ?? raw.MatchId,
    });

    eventSource.addEventListener('notification', (e: MessageEvent) => {
      if (e.lastEventId) setLastEventId(e.lastEventId);
      try {
        const parsed = normalizeNotification(JSON.parse(e.data));
        if (showToasts) {
          toast(parsed.content || parsed.title);
        }
        callbackRefs.current.onNotificationReceived?.(parsed);
        callbackRefs.current.onNotification?.(parsed);
      } catch (err) {
        console.error('[SSE Notifications] Failed to parse notification:', err);
      }
    });

    const handleSimStarted = (e: MessageEvent) => {
      if (e.lastEventId) setLastEventId(e.lastEventId);
      try {
        const raw = JSON.parse(e.data);
        const parsed = normalizeNotification(raw);
        const simId = String(
          raw.simulationId ?? raw.SimulationId ?? raw.matchId ?? ''
        );
        if (showToasts) {
          toast.success(parsed.content || 'Match simulation started');
        }
        callbackRefs.current.onMatchStart?.(parsed, simId);
        if (
          (autoRedirectOnMatchStart || autoRedirect) &&
          notificationService.getAutoRedirectPreference() &&
          simId
        ) {
          router.push(`/simulationview/${simId}`);
        }
      } catch (err) {
        console.error('[SSE Notifications] Failed to parse simulation_started:', err);
      }
    };
    eventSource.addEventListener('simulation_started', handleSimStarted);
    eventSource.addEventListener('match_start', handleSimStarted);

    const handleMatchDone = (e: MessageEvent) => {
      if (e.lastEventId) setLastEventId(e.lastEventId);
      try {
        const raw = JSON.parse(e.data);
        const parsed = normalizeNotification(raw);
        const simId = String(
          raw.simulationId ?? raw.SimulationId ?? raw.matchId ?? ''
        );
        if (showToasts) {
          toast.success(parsed.content || 'Match completed');
        }
        callbackRefs.current.onMatchEnd?.(parsed, simId);
      } catch (err) {
        console.error('[SSE Notifications] Failed to parse match_completed:', err);
      }
    };
    eventSource.addEventListener('match_completed', handleMatchDone);
    eventSource.addEventListener('match_end', handleMatchDone);

    eventSource.addEventListener('experiment_completed', (e: MessageEvent) => {
      if (e.lastEventId) setLastEventId(e.lastEventId);
      try {
        const raw = JSON.parse(e.data);
        if (showToasts) {
          toast.success('Counterfactual Monte Carlo experiment completed');
        }
        callbackRefs.current.onExperimentCompleted?.(raw);
      } catch (err) {
        console.error('[SSE Notifications] Failed to parse experiment_completed:', err);
      }
    });

    eventSource.onerror = () => {
      setIsConnected(false);
    };

    return () => {
      eventSource.close();
      setIsConnected(false);
    };
  }, [autoConnect, autoRedirect, autoRedirectOnMatchStart, router, showToasts]);

  return {
    isConnected,
    lastEventId,
    connectionStats: {
      transport: 'SSE',
      isConnected,
      lastEventId,
    },
  };
};

export default useNotificationsSse;
