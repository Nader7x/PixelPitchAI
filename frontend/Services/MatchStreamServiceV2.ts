import authService from './AuthenticationService';
import {
  CommentaryCueV2,
  LiveTacticalOverridePayload,
  MatchLifecycleV2,
  normalizeSpatialFrame,
  PitchSpatialFrameV2,
  StructuredEventV2,
  TelemetrySnapshotV2,
} from '@/types/MatchV2';

export interface MatchStreamV2Callbacks {
  onEvent?: (event: StructuredEventV2, eventId?: string) => void;
  onSpatialFrame?: (frame: PitchSpatialFrameV2, eventId?: string) => void;
  onTelemetry?: (telemetry: TelemetrySnapshotV2, eventId?: string) => void;
  onCommentary?: (cue: CommentaryCueV2, eventId?: string) => void;
  onLifecycle?: (lifecycle: MatchLifecycleV2, eventId?: string) => void;
  onConnected?: () => void;
  onError?: (error: Event) => void;
}

function getApiBaseUrl(): string {
  const raw =
    process.env.NEXT_PUBLIC_API_BASE_URL ||
    process.env.NEXT_PUBLIC_API_URL ||
    'http://localhost:5025';
  return raw.replace(/\/api\/?$/, '');
}

function getAuthToken(explicitToken?: string): string {
  if (explicitToken) return explicitToken;
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
 * Pure SSE client for PixelPitchAI V2 live match streaming (`GET /api/v2/matches/{id}/stream`)
 * and stateless live tactical overrides (`POST /api/v2/matches/{id}/tactics`).
 */
class MatchStreamServiceV2 {
  private activeEventSource: EventSource | null = null;
  private currentMatchId: number | null = null;
  private lastEventId: string | null = null;

  public connectToMatchStream(
    matchId: number,
    callbacks: MatchStreamV2Callbacks,
    options?: { token?: string; lastEventId?: string | null }
  ): () => void {
    this.disconnect();
    this.currentMatchId = matchId;
    this.lastEventId = options?.lastEventId ?? null;

    const baseUrl = getApiBaseUrl();
    const token = getAuthToken(options?.token);
    const params = new URLSearchParams();
    if (token) params.set('access_token', token);
    if (this.lastEventId) params.set('last_event_id', this.lastEventId);

    const query = params.toString();
    const streamUrl = `${baseUrl}/api/v2/matches/${matchId}/stream${query ? `?${query}` : ''}`;

    const eventSource = new EventSource(streamUrl);
    this.activeEventSource = eventSource;

    eventSource.onopen = () => {
      callbacks.onConnected?.();
    };

    const trackId = (e: MessageEvent): string | undefined => {
      if (e.lastEventId) {
        this.lastEventId = e.lastEventId;
        return e.lastEventId;
      }
      return undefined;
    };

    eventSource.addEventListener('match_event', (e: MessageEvent) => {
      try {
        const id = trackId(e);
        const data = JSON.parse(e.data) as StructuredEventV2;
        callbacks.onEvent?.(data, id);
      } catch (err) {
        console.error('[SSE V2] Failed to parse match_event:', err);
      }
    });

    eventSource.addEventListener('spatial_frame', (e: MessageEvent) => {
      try {
        const id = trackId(e);
        const raw = JSON.parse(e.data) as Record<string, unknown>;
        callbacks.onSpatialFrame?.(normalizeSpatialFrame(raw), id);
      } catch (err) {
        console.error('[SSE V2] Failed to parse spatial_frame:', err);
      }
    });

    const handleTelemetry = (e: MessageEvent) => {
      try {
        const id = trackId(e);
        const data = JSON.parse(e.data) as TelemetrySnapshotV2;
        callbacks.onTelemetry?.(data, id);
      } catch (err) {
        console.error('[SSE V2] Failed to parse telemetry:', err);
      }
    };
    eventSource.addEventListener('telemetry', handleTelemetry);
    eventSource.addEventListener('match_telemetry', handleTelemetry);

    eventSource.addEventListener('commentary', (e: MessageEvent) => {
      try {
        const id = trackId(e);
        const data = JSON.parse(e.data) as CommentaryCueV2;
        callbacks.onCommentary?.(data, id);
      } catch (err) {
        console.error('[SSE V2] Failed to parse commentary:', err);
      }
    });

    eventSource.addEventListener('match_lifecycle', (e: MessageEvent) => {
      try {
        const id = trackId(e);
        const data = JSON.parse(e.data) as MatchLifecycleV2;
        callbacks.onLifecycle?.(data, id);
      } catch (err) {
        console.error('[SSE V2] Failed to parse match_lifecycle:', err);
      }
    });

    eventSource.onerror = (err) => {
      callbacks.onError?.(err);
    };

    return () => this.disconnect();
  }

  public getLastEventId(): string | null {
    return this.lastEventId;
  }

  public disconnect(): void {
    if (this.activeEventSource) {
      this.activeEventSource.close();
      this.activeEventSource = null;
      this.currentMatchId = null;
    }
  }

  /**
   * Stateless REST command to push an in-match tactical override (`POST /api/v2/matches/{id}/tactics`).
   */
  public async applyLiveTacticalOverride(
    matchId: number,
    payload: LiveTacticalOverridePayload
  ): Promise<{ succeeded: boolean; message?: string }> {
    const baseUrl = getApiBaseUrl();
    const token = getAuthToken();
    const res = await fetch(`${baseUrl}/api/v2/matches/${matchId}/tactics`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        ...(token ? { Authorization: `Bearer ${token}` } : {}),
      },
      body: JSON.stringify(payload),
    });

    if (!res.ok) {
      const text = await res.text().catch(() => '');
      return {
        succeeded: false,
        message: text || `HTTP ${res.status}`,
      };
    }

    const data = await res.json().catch(() => ({}));
    return { succeeded: true, message: data.message };
  }

  /**
   * Fetch persisted replay events + spatial track URL (`GET /api/v2/matches/{id}/replay`).
   */
  public async getMatchReplay(matchId: number): Promise<unknown> {
    const baseUrl = getApiBaseUrl();
    const token = getAuthToken();
    const res = await fetch(`${baseUrl}/api/v2/matches/${matchId}/replay`, {
      headers: token ? { Authorization: `Bearer ${token}` } : {},
    });
    if (!res.ok) {
      throw new Error(`Failed to fetch replay for match ${matchId}: HTTP ${res.status}`);
    }
    return res.json();
  }
}

export const matchStreamServiceV2 = new MatchStreamServiceV2();
export default matchStreamServiceV2;
