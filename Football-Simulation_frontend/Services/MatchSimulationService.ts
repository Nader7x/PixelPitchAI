import apiService from './ApiService';

export interface TeamSeason {
  seasonId: number;
  seasonName: string;
}

export interface TeamSeasonsResponse {
  succeeded: boolean;
  teamId: number;
  teamName: string;
  seasons: TeamSeason[];
  error: string;
}

export interface SimulateMatchRequest {
  homeTeamId: number;
  awayTeamId: number;
  homeTeamName: string;
  awayTeamName: string;
  homeTeamSeason: string;
  awayTeamSeason: string;
  homeSeasonId: number;
  awaySeasonId: number;
}

export interface MatchEvent {
  timestamp: string;
  time_seconds: number;
  minute: number;
  second: number;
  team: string;
  player: string;
  action: string;
  position: [number, number];
  outcome: string;
  height: string;
  card: string | null;
  pass_target: [number, number];
  shot_target: [number, number];
  body_part: string;
  event_type: string;
  type: string;
  event_index: number;
  match_id: string;
  home_team: string;
  away_team: string;
  long_pass: boolean;
  pass_length: number;
  Score: {
    Home: number;
    Away: number;
  };
}

export interface SimulationApiResponse {
  match_id: number;
  home_team_name: string;
  away_team_name: string;
  home_team_season: string;
  away_team_season: string;
  events_count: number;
  execution_time: number;
  preview: string;
  simulation_id: string;
  status: string;
}

export interface SimulateMatchResponse {
  succeeded: boolean;
  id: number;
  homeTeamName: string;
  awayTeamName: string;
  error: string;
  apiResponse: SimulationApiResponse;
}

export interface SimulationTrackResponse {
  succeeded: boolean;
  simulationId: string;
  status: string;
  progress: number;
  isComplete: boolean;
  error: string;
}

export interface SimulationResultResponse {
  succeeded: boolean;
  simulationId: string;
  matchResult: any;
  events: MatchEvent[];
  finalScore: {
    home: number;
    away: number;
  };
  error: string;
}

class MatchSimulationService {
  /**
   * Get team seasons by team ID with caching
   */
  public async getTeamSeasons(teamId: number): Promise<TeamSeasonsResponse> {
    try {
      return await apiService.getWithCache<TeamSeasonsResponse>(
        `/teams/seasons/${teamId}`,
        {
          cacheKey: `team-seasons-${teamId}`,
          cacheTtl: 10 * 60 * 1000, // 10 minutes cache
        }
      );
    } catch (error) {
      console.error(`Error fetching seasons for team ${teamId}:`, error);
      throw error;
    }
  }

  /**
   * Start match simulation with retry logic
   */
  public async simulateMatch(
    userId: string,
    matchData: SimulateMatchRequest
  ): Promise<SimulateMatchResponse> {
    try {
      const response = await apiService.postWithRetry<SimulateMatchResponse>(
        `matches/simulatematch/${userId}`,
        matchData
      );

      if (!response.succeeded) {
        throw new Error(response.error || 'Failed to start match simulation');
      }

      return response;
    } catch (error) {
      console.error('Error starting match simulation:', error);
      throw error;
    }
  }

  /**
   * Track simulation progress with caching (short TTL for real-time updates)
   */
  public async trackSimulation(
    simulationId: string
  ): Promise<SimulationTrackResponse> {
    try {
      return await apiService.getWithCache<SimulationTrackResponse>(
        `/matches/simulation/track/${simulationId}`,
        {
          cacheKey: `simulation-track-${simulationId}`,
          cacheTtl: 2 * 1000, // 2 seconds cache for real-time tracking
        }
      );
    } catch (error) {
      console.error(`Error tracking simulation ${simulationId}:`, error);
      throw error;
    }
  }

  /**
   * Get simulation result with caching
   */
  public async getSimulationResult(
    simulationId: string
  ): Promise<SimulationResultResponse> {
    try {
      return await apiService.getWithCache<SimulationResultResponse>(
        `/matches/simulation/result/${simulationId}`,
        {
          cacheKey: `simulation-result-${simulationId}`,
          cacheTtl: 30 * 60 * 1000, // 30 minutes cache for completed results
        }
      );
    } catch (error) {
      console.error(`Error getting simulation result ${simulationId}:`, error);
      throw error;
    }
  }

  /**
   * Clear simulation cache for real-time updates
   */
  public clearSimulationCache(simulationId?: string): void {
    if (simulationId) {
      apiService.clearCache(`^simulation-track-${simulationId}$`);
      apiService.clearCache(`^simulation-result-${simulationId}$`);
    } else {
      apiService.clearCache('^simulation-');
    }
  }

  /**
   * Clear team seasons cache
   */
  public clearTeamSeasonsCache(): void {
    apiService.clearCache('^team-seasons-');
  }
}

// Create and export a singleton instance
const matchSimulationService = new MatchSimulationService();
export default matchSimulationService;
