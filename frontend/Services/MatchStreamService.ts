/**
 * High-performance Server-Sent Events (SSE) service for real-time match streaming.
 * Replaces SignalR WebSockets for unidirectional server-to-client match event broadcasts.
 */

export interface MatchStreamCallbacks {
  onEvent?: (eventData: any) => void;
  onStatistics?: (statsData: any) => void;
  onError?: (error: any) => void;
  onConnected?: () => void;
}

class MatchStreamService {
  private activeEventSource: EventSource | null = null;
  private currentMatchId: number | null = null;

  /**
   * Connect to the real-time SSE stream for a specific match.
   * Requires JWT authentication token passed as query param.
   * 
   * @param matchId The match ID to stream
   * @param token JWT authentication token
   * @param callbacks Event listeners for match events and statistics
   * @returns Cleanup function to close the stream connection
   */
  public connectToMatchStream(
    matchId: number,
    token: string,
    callbacks: MatchStreamCallbacks
  ): () => void {
    this.disconnect();
    this.currentMatchId = matchId;

    const baseUrl = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5025';
    const streamUrl = `${baseUrl}/api/matches/${matchId}/events/stream?access_token=${encodeURIComponent(token)}`;

    console.log(`[SSE] Connecting to match stream: /api/matches/${matchId}/events/stream`);
    const eventSource = new EventSource(streamUrl);
    this.activeEventSource = eventSource;

    eventSource.onopen = () => {
      console.log(`[SSE] Connected to match stream ${matchId}`);
      callbacks.onConnected?.();
    };

    eventSource.addEventListener('match_event', (event: MessageEvent) => {
      try {
        const data = JSON.parse(event.data);
        callbacks.onEvent?.(data);
      } catch (err) {
        console.error('[SSE] Failed to parse match_event JSON:', err);
      }
    });

    eventSource.addEventListener('match_statistics', (event: MessageEvent) => {
      try {
        const data = JSON.parse(event.data);
        callbacks.onStatistics?.(data);
      } catch (err) {
        console.error('[SSE] Failed to parse match_statistics JSON:', err);
      }
    });

    eventSource.onerror = (error) => {
      console.error(`[SSE] Error in match stream ${matchId}:`, error);
      callbacks.onError?.(error);
    };

    return () => this.disconnect();
  }

  /**
   * Disconnect the active SSE match stream
   */
  public disconnect(): void {
    if (this.activeEventSource) {
      console.log(`[SSE] Disconnecting match stream ${this.currentMatchId}`);
      this.activeEventSource.close();
      this.activeEventSource = null;
      this.currentMatchId = null;
    }
  }
}

export const matchStreamService = new MatchStreamService();
export default matchStreamService;
