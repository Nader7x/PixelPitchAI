'use client';

import { useEffect, useState } from 'react';
import experimentServiceV2 from '@/Services/ExperimentServiceV2';
import {
  ExperimentProgressEvent,
  MonteCarloExperimentResponse,
} from '@/types/MatchV2';

export function useExperimentStreamV2(experimentId: string | null) {
  const [progress, setProgress] = useState<ExperimentProgressEvent | null>(null);
  const [result, setResult] = useState<MonteCarloExperimentResponse | null>(
    null
  );
  const [isConnected, setIsConnected] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!experimentId) return;

    setError(null);
    const disconnect = experimentServiceV2.streamExperimentProgress(
      experimentId,
      {
        onConnected: () => setIsConnected(true),
        onProgress: (p) => setProgress(p),
        onCompleted: (res) => {
          setResult(res);
          setIsConnected(false);
        },
        onError: () => {
          setIsConnected(false);
          setError('Experiment stream disconnected');
        },
      }
    );

    return () => {
      disconnect();
      setIsConnected(false);
    };
  }, [experimentId]);

  return { progress, result, isConnected, error };
}

export default useExperimentStreamV2;
