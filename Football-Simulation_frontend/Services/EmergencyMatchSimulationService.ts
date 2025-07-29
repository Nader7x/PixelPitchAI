/**
 * EMERGENCY MATCH SIMULATION OPTIMIZER
 * Specialized service to handle slow match simulation API calls
 */

import apiService from './ApiService';

export interface MatchSimulationOptimizer {
  simulateWithTimeout: (matchId: string, timeoutMs?: number) => Promise<any>;
  simulateWithProgress: (
    matchId: string,
    progressCallback?: (progress: string) => void
  ) => Promise<any>;
  simulateWithRetry: (matchId: string, maxRetries?: number) => Promise<any>;
  cancelSimulation: (matchId: string) => Promise<void>;
  getSimulationStatus: (matchId: string) => Promise<any>;
}

class EmergencyMatchSimulationService implements MatchSimulationOptimizer {
  private activeSimulations = new Map<string, AbortController>();
  private simulationTimeouts = new Map<string, NodeJS.Timeout>();

  /**
   * Simulate match with aggressive timeout and cancellation
   */
  async simulateWithTimeout(
    matchId: string,
    timeoutMs: number = 45000
  ): Promise<any> {
    console.log(
      `🚀 [EMERGENCY SIM] Starting optimized simulation for ${matchId} with ${timeoutMs}ms timeout`
    );

    const controller = new AbortController();
    this.activeSimulations.set(matchId, controller);

    // Set up timeout
    const timeoutId = setTimeout(() => {
      console.warn(
        `⏰ [EMERGENCY SIM] Timeout reached for ${matchId}, cancelling...`
      );
      controller.abort();
      this.cleanup(matchId);
    }, timeoutMs);

    this.simulationTimeouts.set(matchId, timeoutId);

    try {
      const startTime = performance.now();

      // Create the request with timeout and cancellation
      const response = await fetch(
        `${process.env.NEXT_PUBLIC_API_URL}/matches/simulatematch/${matchId}`,
        {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
            Authorization: `Bearer ${localStorage.getItem('accessToken') || sessionStorage.getItem('accessToken')}`,
          },
          signal: controller.signal,
          body: JSON.stringify({}),
        }
      );

      const duration = performance.now() - startTime;
      console.log(
        `⚡ [EMERGENCY SIM] Simulation completed in ${duration.toFixed(2)}ms`
      );

      if (!response.ok) {
        throw new Error(`HTTP ${response.status}: ${response.statusText}`);
      }

      const result = await response.json();
      this.cleanup(matchId);

      return result;
    } catch (error: any) {
      this.cleanup(matchId);

      if (error.name === 'AbortError') {
        console.error(
          `❌ [EMERGENCY SIM] Simulation ${matchId} was cancelled due to timeout`
        );
        throw new Error(
          `Match simulation timed out after ${timeoutMs}ms. This usually indicates heavy server load.`
        );
      }

      console.error(`❌ [EMERGENCY SIM] Simulation ${matchId} failed:`, error);
      throw error;
    }
  }
  /**
   * Simulate with progress updates
   */
  async simulateWithProgress(
    matchId: string,
    progressCallback?: (progress: string) => void
  ): Promise<any> {
    console.log(
      `📊 [EMERGENCY SIM] Starting simulation with progress tracking for ${matchId}`
    );

    let progressInterval: NodeJS.Timeout | null = null;

    if (progressCallback) {
      let dots = 0;
      progressInterval = setInterval(() => {
        dots = (dots + 1) % 4;
        const dotString = '.'.repeat(dots);
        progressCallback(
          `Simulating match${dotString} Please wait (this may take up to 45 seconds)`
        );
      }, 1000);
    }

    try {
      const result = await this.simulateWithTimeout(matchId, 45000);

      if (progressCallback) {
        progressCallback('✅ Simulation completed successfully!');
      }

      return result;
    } catch (error) {
      if (progressCallback) {
        progressCallback('❌ Simulation failed or timed out');
      }
      throw error;
    } finally {
      if (progressInterval) {
        clearInterval(progressInterval);
      }
    }
  }

  /**
   * Simulate with automatic retry logic
   */
  async simulateWithRetry(
    matchId: string,
    maxRetries: number = 2
  ): Promise<any> {
    console.log(
      `🔄 [EMERGENCY SIM] Starting simulation with retry (max ${maxRetries}) for ${matchId}`
    );

    let lastError: any;

    for (let attempt = 1; attempt <= maxRetries + 1; attempt++) {
      try {
        console.log(
          `🎯 [EMERGENCY SIM] Attempt ${attempt}/${maxRetries + 1} for ${matchId}`
        );

        // Use progressively longer timeouts for retries
        const timeout = 30000 + (attempt - 1) * 15000; // 30s, 45s, 60s

        const result = await this.simulateWithTimeout(matchId, timeout);
        console.log(`✅ [EMERGENCY SIM] Succeeded on attempt ${attempt}`);
        return result;
      } catch (error: any) {
        lastError = error;
        console.warn(
          `⚠️ [EMERGENCY SIM] Attempt ${attempt} failed:`,
          error.message
        );

        if (attempt <= maxRetries) {
          const delay = 2000 * attempt; // 2s, 4s delay between retries
          console.log(`⏳ [EMERGENCY SIM] Waiting ${delay}ms before retry...`);
          await new Promise((resolve) => setTimeout(resolve, delay));
        }
      }
    }

    console.error(`❌ [EMERGENCY SIM] All attempts failed for ${matchId}`);
    throw lastError;
  }

  /**
   * Cancel active simulation
   */
  async cancelSimulation(matchId: string): Promise<void> {
    console.log(`🛑 [EMERGENCY SIM] Cancelling simulation ${matchId}`);

    const controller = this.activeSimulations.get(matchId);
    if (controller) {
      controller.abort();
      this.cleanup(matchId);
      console.log(`✅ [EMERGENCY SIM] Simulation ${matchId} cancelled`);
    }
  }

  /**
   * Get simulation status (if backend supports it)
   */
  async getSimulationStatus(matchId: string): Promise<any> {
    try {
      const response = await apiService.get(
        `/matches/simulation-status/${matchId}`
      );
      return response;
    } catch (error) {
      console.warn(
        `⚠️ [EMERGENCY SIM] Could not get status for ${matchId}:`,
        error
      );
      return { status: 'unknown' };
    }
  }

  /**
   * Clean up simulation resources
   */
  private cleanup(matchId: string): void {
    const timeout = this.simulationTimeouts.get(matchId);
    if (timeout) {
      clearTimeout(timeout);
      this.simulationTimeouts.delete(matchId);
    }

    this.activeSimulations.delete(matchId);
  }

  /**
   * Get list of active simulations
   */
  getActiveSimulations(): string[] {
    return Array.from(this.activeSimulations.keys());
  }

  /**
   * Cancel all active simulations
   */
  cancelAllSimulations(): void {
    console.log(`🛑 [EMERGENCY SIM] Cancelling all active simulations`);

    for (const [matchId, controller] of this.activeSimulations) {
      controller.abort();
      this.cleanup(matchId);
    }

    console.log(`✅ [EMERGENCY SIM] All simulations cancelled`);
  }
}

// Create singleton instance
const emergencyMatchSimulationService = new EmergencyMatchSimulationService();

// Global access for emergency use
if (typeof window !== 'undefined') {
  (window as any).emergencyMatchSim = emergencyMatchSimulationService;

  // Emergency functions
  (window as any).cancelAllSimulations = () => {
    emergencyMatchSimulationService.cancelAllSimulations();
  };

  (window as any).getActiveSimulations = () => {
    return emergencyMatchSimulationService.getActiveSimulations();
  };
}

export default emergencyMatchSimulationService;
