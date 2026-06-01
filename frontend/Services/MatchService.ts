import apiService from './ApiService';

export interface Match {
  id: number;
  seasonId: number;
  homeTeamId: number;
  awayTeamId: number;
  homeTeamName?: string;
  awayTeamName?: string;
  homeTeamLogo?: string;
  awayTeamLogo?: string;
  homeTeamScore?: number;
  awayTeamScore?: number;
  scheduledDateTimeUtc?: string;
  time?: string;
  status?: string;
  stadiumId?: number;
  stadiumName?: string;
  matchWeek?: number;
  isLive?: boolean;
}

export interface TeamSeason {
  id: number;
  name: string;
  leagueName: string;
  country: string;
  currentRound: number;
  totalRounds: number;
  isActive: boolean;
  isCompleted: boolean;
  startDate: string;
  endDate: string;
}

export interface Team {
  id: number;
  name: string;
  shortName: string;
  logo: string;
  country: string;
  city: string;
  league: string;
  primaryColor: string;
  secondaryColor: string;
  foundationDate: string;
}

export interface Stadium {
  id: number;
  name: string;
  city: string;
  country: string;
  capacity: number;
  surfaceType: string;
  address: string;
  latitude: number;
  longitude: number;
  imageUrl: string;
  description: string;
  facilities: string;
  builtDate: string;
}

export interface Coach {
  id: number;
  firstName: string;
  lastName: string;
  dateOfBirth: string;
  nationality: string;
  role: string;
  teamId: number;
  teamName: string;
  photoUrl: string;
  preferredFormation: string;
  coachingStyle: string;
  biography: string;
  yearsOfExperience: number;
  fullName: string;
}

export interface MatchDetail {
  id: number;
  homeTeamSeason: TeamSeason;
  awayTeamSeason: TeamSeason;
  homeTeam: Team;
  awayTeam: Team;
  homeTeamInMatchName: string;
  awayTeamInMatchName: string;
  scheduledDateTimeUtc: string;
  stadium: Stadium;
  matchWeek: number;
  homeCoach: Coach;
  awayCoach: Coach;
  homeTeamScore: number;
  awayTeamScore: number;
  winningTeamId: number;
  losingTeamId: number;
  isDraw: boolean;
  matchStatus: string;
  modelSimulationStartTimeUtc: string;
  homeTeamPossession: number;
  awayTeamPossession: number;
  homeTeamShots: number;
  awayTeamShots: number;
  homeTeamShotsOnTarget: number;
  awayTeamShotsOnTarget: number;
  homeTeamShotsOffTarget: number;
  awayTeamShotsOffTarget: number;
  homeTeamCorners: number;
  awayTeamCorners: number;
  homeTeamFouls: number;
  awayTeamFouls: number;
  homeTeamYellowCards: number;
  awayTeamYellowCards: number;
  homeTeamRedCards: number;
  awayTeamRedCards: number;
  homeTeamOffsides: number;
  awayTeamOffsides: number;
  homeTeamPasses: number;
  awayTeamPasses: number;
  homeTeamPassesCompleted: number;
  awayTeamPassesCompleted: number;
  homeTeamPassAccuracy: number;
  awayTeamPassAccuracy: number;
  homeTeamDribbles: number;
  awayTeamDribbles: number;
  homeTeamSaves: number;
  awayTeamSaves: number;
  homeTeamDuels: number;
  awayTeamDuels: number;
  homeTeamDuelsWon: number;
  awayTeamDuelsWon: number;
  homeTeamClearances: number;
  awayTeamClearances: number;
  homeTeamPossessionWon: number;
  awayTeamPossessionWon: number;
  homeTeamRecoveries: number;
  awayTeamRecoveries: number;
  homeTeamGoalKicks: number;
  awayTeamGoalKicks: number;
  homeLongBalls: number;
  awayLongBalls: number;
  homeAccurateLongBalls: number;
  awayAccurateLongBalls: number;
  homeTeamLongBallsAccuracy: number;
  awayTeamLongBallsAccuracy: number;
  homeTeamFreeKicks: number;
  awayTeamFreeKicks: number;
  creatorId: string;
  isLive: boolean;
}

export interface MatchDetailResponse {
  succeeded: boolean;
  notFound: boolean;
  match: MatchDetail;
  error?: string;
}

export interface MatchFilter {
  seasonId?: number;
  teamId?: number;
  status?: string;
  // Add other detailed properties as needed;
  fromDate?: string;
  toDate?: string;
  matchWeek?: number;
}

export interface CreateMatchDto {
  seasonId: number;
  homeTeamId: number;
  awayTeamId: number;
  date?: string;
  time?: string;
  stadiumId?: number;
  status?: 'Scheduled' | 'Live' | 'Completed' | 'Postponed' | 'Cancelled';
  homeTeamScore?: number;
  awayTeamScore?: number;
  matchWeek?: number;
}

export interface UpdateMatchDto {
  seasonId?: number;
  homeTeamId?: number;
  awayTeamId?: number;
  date?: string;
  time?: string;
  stadiumId?: number;
  status?: 'Scheduled' | 'Live' | 'Completed' | 'Postponed' | 'Cancelled';
  homeTeamScore?: number;
  awayTeamScore?: number;
  matchWeek?: number;
}
export interface MatchesResponse {
  matches: MatchResponse[];
  succeeded: boolean;
  error?: string;
}

export interface LatestMatchesResponse {
  matches: LatestMatch[];
  succeeded: boolean;
  error?: string;
}
export interface LatestMatch {
  id: number;
  homeTeamName: string;
  awayTeamName: string;
  homeTeamScore: number;
  awayTeamScore: number;
  scheduledDateTimeUtc: string;
  matchStatus: string;
  isLive: boolean;
  homeTeamLogo?: string;
  awayTeamLogo?: string;
}

export interface MatchResponse {
  id: number;
  seasonId: number;
  seasonName: string;
  homeTeamId: number;
  homeTeamName: string;
  awayTeamId: number;
  awayTeamName: string;
  scheduledDateTimeUtc: string;
  stadiumId: number;
  stadiumName: string;
  matchWeek: number;
  homeCoachId: number;
  awayCoachId: number;
  homeTeamScore: number;
  awayTeamScore: number;
  winningTeamId: number;
  losingTeamId: number;
  isDraw: boolean;
  matchStatus: string;
  homeTeamPossession: number;
  awayTeamPossession: number;
  homeTeamShots: number;
  awayTeamShots: number;
  homeTeamShotsOnTarget: number;
  awayTeamShotsOnTarget: number;
  homeTeamCorners: number;
  awayTeamCorners: number;
  homeTeamFouls: number;
  awayTeamFouls: number;
  homeTeamYellowCards: number;
  awayTeamYellowCards: number;
  homeTeamRedCards: number;
  awayTeamRedCards: number;
  modelSimulationStartTimeUtc: string;
}

export interface LiveMatch {
  id: number;
  isLive: boolean;
  homeTeam: Team;
  awayTeam: Team;
  homeTeamScore: number;
  awayTeamScore: number;
  scheduledDateTimeUtc: string;
  matchStatus: 'Live' | 'Scheduled' | 'Completed' | 'Postponed' | 'Cancelled';
  // Live match statistics
  homeTeamPossession: number;
  awayTeamPossession: number;
  homeTeamShots: number;
  awayTeamShots: number;
  homeTeamShotsOnTarget: number;
  awayTeamShotsOnTarget: number;
  homeTeamCorners: number;
  awayTeamCorners: number;
  homeTeamFouls: number;
  awayTeamFouls: number;
  homeTeamYellowCards: number;
  awayTeamYellowCards: number;
  homeTeamRedCards: number;
  awayTeamRedCards: number;
}

export interface LiveMatchResponse {
  succeeded: boolean;
  error?: string;
  matchId?: number;
}

export interface DirectLiveMatchResponse {
  succeeded: boolean;
  error?: string;
  hasLiveMatch: boolean;
  liveMatch?: LiveMatch;
}

class MatchService {
  /**
   * Get all matches with optional filtering and caching
   */
  public async getMatches(filter?: MatchFilter): Promise<Match[]> {
    try {
      const cacheKey = filter
        ? `matches-filter-${JSON.stringify(filter)}`
        : 'matches-all';
      const matchesResponse = await apiService.getWithCache<MatchesResponse>(
        '/matches',
        {
          params: filter,
          cacheKey,
          cacheTtl: 3 * 60 * 1000, // 3 minutes cache (shorter due to live match updates)
        }
      );
      return matchesResponse.matches;
    } catch (error) {
      console.error('Error fetching matches:', error);
      throw error;
    }
  }

  /**
   * Get match by ID with caching
   */
  public async getMatchById(id: number): Promise<Match> {
    try {
      return await apiService.getWithCache<Match>(`/matches/${id}`, {
        cacheKey: `match-${id}`,
        cacheTtl: 5 * 60 * 1000, // 5 minutes cache
      });
    } catch (error) {
      console.error(`Error fetching match with ID ${id}:`, error);
      throw error;
    }
  }

  /**
   * Get match details by ID with caching
   */
  public async getMatchDetails(id: number): Promise<MatchDetail> {
    try {
      const response = await apiService.getWithCache<MatchDetailResponse>(
        `/matches/details/${id}`,
        {
          cacheKey: `match-details-${id}`,
          cacheTtl: 5 * 60 * 1000, // 5 minutes cache
        }
      );

      if (!response.succeeded || response.notFound) {
        throw new Error(response.error || 'Match not found');
      }

      return response.match;
    } catch (error) {
      console.error(`Error fetching match details with ID ${id}:`, error);
      throw error;
    }
  }

  /**
   * Create a new match (admin, manager only) with retry logic
   */
  public async createMatch(matchData: CreateMatchDto): Promise<Match> {
    try {
      const result = await apiService.postWithRetry<Match>(
        '/matches',
        matchData
      );

      // Invalidate matches cache after successful creation
      apiService.clearCache('^matches');

      return result;
    } catch (error) {
      console.error('Error creating match:', error);
      throw error;
    }
  }

  /**
   * Update a match (admin, manager only) with retry logic
   */
  public async updateMatch(
    id: number,
    matchData: UpdateMatchDto
  ): Promise<Match> {
    try {
      const result = await apiService.putWithRetry<Match>(
        `/matches/${id}`,
        matchData
      );

      // Invalidate matches cache after successful update
      apiService.clearCache('^matches');
      apiService.clearCache(`^match-${id}$`);
      apiService.clearCache(`^match-details-${id}$`);

      return result;
    } catch (error) {
      console.error(`Error updating match with ID ${id}:`, error);
      throw error;
    }
  }

  /**
   * Delete a match (admin only) with retry logic
   */
  public async deleteMatch(id: number): Promise<void> {
    try {
      await apiService.deleteWithRetry(`/matches/${id}`);

      // Invalidate matches cache after successful deletion
      apiService.clearCache('^matches');
      apiService.clearCache(`^match-${id}$`);
      apiService.clearCache(`^match-details-${id}$`);
    } catch (error) {
      console.error(`Error deleting match with ID ${id}:`, error);
      throw error;
    }
  }

  /**
   * Get live match for a specific user
   */
  public async getLiveMatchForUser(userId: string): Promise<{
    succeeded: boolean;
    error?: string;
    hasLiveMatch: boolean;
    liveMatch?: LiveMatch;
  }> {
    try {
      const response = await apiService.getWithCache<DirectLiveMatchResponse>(
        `/matches/livematch/${userId}`,
        {
          cacheKey: `live-match-${userId}`,
          cacheTtl: 30 * 1000, // 30 seconds cache for live data
        }
      );

      console.log('Live match API response:', response);

      // Check if the request succeeded
      if (!response.succeeded) {
        return {
          succeeded: false,
          error: response.error || 'Failed to fetch live match',
          hasLiveMatch: false,
        };
      }

      // Check if there's a live match
      if (!response.hasLiveMatch || !response.liveMatch) {
        return {
          succeeded: true,
          hasLiveMatch: false,
        };
      }

      // Return the live match data directly from the response
      return {
        succeeded: true,
        hasLiveMatch: true,
        liveMatch: response.liveMatch,
      };
    } catch (error) {
      console.error(`Error fetching live match for user ${userId}:`, error);
      return {
        succeeded: false,
        error: error instanceof Error ? error.message : 'Unknown error',
        hasLiveMatch: false,
      };
    }
  }

  /**
   * Get latest matches
   */
  public async getLatestMatches(userId: string): Promise<LatestMatch[]> {
    try {
      const latestMatches =
        await apiService.getWithCache<LatestMatchesResponse>(
          `/matches/${userId}`,
          {
            cacheKey: `latest-matches-${userId}`,
            cacheTtl: 2 * 60 * 1000, // 2 minutes cache (very short for latest matches)
          }
        );
      return latestMatches.matches;
    } catch (error) {
      console.error('Error fetching latest matches:', error);
      throw error;
    }
  }

  /**
   * Clear all matches cache
   */
  public clearMatchesCache(): void {
    apiService.clearCache('^matches');
    apiService.clearCache('^match-');
    apiService.clearCache('^latest-matches');
  }
}

// Create and export a singleton instance
const matchService = new MatchService();
export default matchService;
