import authService from './AuthenticationService';
import {
  ExperimentProgressEvent,
  MonteCarloExperimentRequest,
  MonteCarloExperimentResponse,
} from '@/types/MatchV2';

export interface ExperimentStreamCallbacks {
  onProgress?: (progress: ExperimentProgressEvent) => void;
  onCompleted?: (result: MonteCarloExperimentResponse) => void;
  onError?: (error: Event | Error) => void;
  onConnected?: () => void;
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
 * V2 Counterfactual Monte Carlo Experimentation REST & SSE service.
 */
class ExperimentServiceV2 {
  public async runExperiment(
    request: MonteCarloExperimentRequest
  ): Promise<MonteCarloExperimentResponse> {
    const baseUrl = getApiBaseUrl();
    const token = getAuthToken();

    const res = await fetch(`${baseUrl}/api/v2/experiments`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        ...(token ? { Authorization: `Bearer ${token}` } : {}),
      },
      body: JSON.stringify(request),
    });

    if (!res.ok) {
      const text = await res.text().catch(() => '');
      throw new Error(text || `Experiment request failed (${res.status})`);
    }

    return (await res.json()) as MonteCarloExperimentResponse;
  }

  public async getExperiment(
    experimentId: string
  ): Promise<MonteCarloExperimentResponse> {
    const baseUrl = getApiBaseUrl();
    const token = getAuthToken();
    const res = await fetch(`${baseUrl}/api/v2/experiments/${encodeURIComponent(experimentId)}`, {
      headers: token ? { Authorization: `Bearer ${token}` } : {},
    });
    if (!res.ok) {
      throw new Error(`Failed to fetch experiment ${experimentId}: ${res.status}`);
    }
    return (await res.json()) as MonteCarloExperimentResponse;
  }

  public async getUserExperiments(
    userId: string
  ): Promise<MonteCarloExperimentResponse[]> {
    const baseUrl = getApiBaseUrl();
    const token = getAuthToken();
    const res = await fetch(
      `${baseUrl}/api/v2/experiments/user/${encodeURIComponent(userId)}`,
      {
        headers: token ? { Authorization: `Bearer ${token}` } : {},
      }
    );
    if (!res.ok) {
      throw new Error(`Failed to fetch user experiments: ${res.status}`);
    }
    return (await res.json()) as MonteCarloExperimentResponse[];
  }

  /**
   * Launch a deterministic 3D visual replay for a representative Monte Carlo seed.
   */
  public async replaySeedIn3D(
    experimentId: string,
    seed: number,
    armId?: string
  ): Promise<{ matchId: number; simulationId?: string }> {
    const baseUrl = getApiBaseUrl();
    const token = getAuthToken();
    const params = armId ? `?armId=${encodeURIComponent(armId)}` : '';
    const res = await fetch(
      `${baseUrl}/api/v2/experiments/${encodeURIComponent(experimentId)}/replay-seed/${seed}${params}`,
      {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          ...(token ? { Authorization: `Bearer ${token}` } : {}),
        },
      }
    );
    if (!res.ok) {
      const text = await res.text().catch(() => '');
      throw new Error(text || `Failed to launch seed replay (${res.status})`);
    }
    return (await res.json()) as { matchId: number; simulationId?: string };
  }

  /**
   * Connect to SSE progress stream `GET /api/v2/experiments/{id}/stream`.
   */
  public streamExperimentProgress(
    experimentId: string,
    callbacks: ExperimentStreamCallbacks
  ): () => void {
    const baseUrl = getApiBaseUrl();
    const token = getAuthToken();
    const query = token ? `?access_token=${encodeURIComponent(token)}` : '';
    const url = `${baseUrl}/api/v2/experiments/${encodeURIComponent(experimentId)}/stream${query}`;

    const es = new EventSource(url);

    es.onopen = () => {
      callbacks.onConnected?.();
    };

    es.addEventListener('experiment_progress', (e: MessageEvent) => {
      try {
        callbacks.onProgress?.(JSON.parse(e.data) as ExperimentProgressEvent);
      } catch (err) {
        console.error('[SSE Experiment] Failed to parse progress:', err);
      }
    });

    es.addEventListener('experiment_completed', (e: MessageEvent) => {
      try {
        callbacks.onCompleted?.(
          JSON.parse(e.data) as MonteCarloExperimentResponse
        );
        es.close();
      } catch (err) {
        console.error('[SSE Experiment] Failed to parse completion:', err);
      }
    });

    es.onerror = (err) => {
      callbacks.onError?.(err);
    };

    return () => {
      es.close();
    };
  }
}

export const experimentServiceV2 = new ExperimentServiceV2();
export default experimentServiceV2;
