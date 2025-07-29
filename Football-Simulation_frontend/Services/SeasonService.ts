import apiService from './ApiService';

export interface Season {
  id: number;
  name: string;
  leagueName?: string;
  country?: string;
  startDate?: string;
  endDate?: string;
  isActive?: boolean;
  currentRound?: number;
  totalRounds?: number;
  isCompleted?: boolean;
  matchCount?: number;
  teamStandings?: any;
}

export interface CreateSeasonDto {
  name: string;
  leagueName?: string;
  country?: string;
  startDate?: string;
  endDate?: string;
  isActive?: boolean;
}

export interface UpdateSeasonDto {
  name?: string;
  leagueName?: string;
  country?: string;
  startDate?: string;
  endDate?: string;
  isActive?: boolean;
}

export interface SeasonFilter {
  leagueName?: string;
  country?: string;
  isActive?: boolean;
}

interface SeasonResponse {
  succeeded: boolean;
  seasons: Season[];
  error: string | null;
}

class SeasonService {
  /**
   * Get all seasons with optional filtering and caching
   */
  public async getSeasons(filter?: SeasonFilter): Promise<Season[]> {
    try {
      const cacheKey = filter
        ? `seasons-filter-${JSON.stringify(filter)}`
        : 'seasons-all';
      const response = await apiService.getWithCache<SeasonResponse>(
        '/seasons',
        {
          params: filter,
          cacheKey,
          cacheTtl: 5 * 60 * 1000, // 5 minutes cache
        }
      );
      if (!response.succeeded) {
        throw new Error(response.error || 'Failed to fetch seasons');
      }
      return response.seasons;
    } catch (error) {
      console.error('Error fetching seasons:', error);
      throw error;
    }
  }

  /**
   * Get season by ID with caching
   */
  public async getSeasonById(id: number): Promise<Season> {
    try {
      const response = await apiService.getWithCache<{
        succeeded: boolean;
        season: Season;
        error: string | null;
      }>(`/seasons/${id}`, {
        cacheKey: `season-${id}`,
        cacheTtl: 10 * 60 * 1000, // 10 minutes cache
      });
      if (!response.succeeded) {
        throw new Error(
          response.error || `Failed to fetch season with ID ${id}`
        );
      }
      return response.season;
    } catch (error) {
      console.error(`Error fetching season with ID ${id}:`, error);
      throw error;
    }
  }

  /**
   * Create a new season (admin only) with retry logic
   */
  public async createSeason(seasonData: CreateSeasonDto): Promise<Season> {
    try {
      const response = await apiService.postWithRetry<{
        succeeded: boolean;
        season: Season;
        error: string | null;
      }>('/seasons', seasonData);

      if (!response.succeeded) {
        throw new Error(response.error || 'Failed to create season');
      }

      // Invalidate seasons cache after successful creation
      apiService.clearCache('^seasons');

      return response.season;
    } catch (error) {
      console.error('Error creating season:', error);
      throw error;
    }
  }

  /**
   * Update a season (admin only) with retry logic
   */
  public async updateSeason(
    id: number,
    seasonData: UpdateSeasonDto
  ): Promise<Season> {
    try {
      const response = await apiService.putWithRetry<{
        succeeded: boolean;
        season: Season;
        error: string | null;
      }>(`/seasons/${id}`, seasonData);

      if (!response.succeeded) {
        throw new Error(
          response.error || `Failed to update season with ID ${id}`
        );
      }

      // Invalidate seasons cache after successful update
      apiService.clearCache('^seasons');
      apiService.clearCache(`^season-${id}$`);

      return response.season;
    } catch (error) {
      console.error(`Error updating season with ID ${id}:`, error);
      throw error;
    }
  }

  /**
   * Delete a season (admin only) with retry logic
   */
  public async deleteSeason(id: number): Promise<void> {
    try {
      const response = await apiService.deleteWithRetry<{
        succeeded: boolean;
        error: string | null;
      }>(`/seasons/${id}`);

      if (!response.succeeded) {
        throw new Error(
          response.error || `Failed to delete season with ID ${id}`
        );
      }

      // Invalidate seasons cache after successful deletion
      apiService.clearCache('^seasons');
      apiService.clearCache(`^season-${id}$`);
    } catch (error) {
      console.error(`Error deleting season with ID ${id}:`, error);
      throw error;
    }
  }

  /**
   * Clear all seasons cache
   */
  public clearSeasonsCache(): void {
    apiService.clearCache('^seasons');
    apiService.clearCache('^season-');
  }
}

// Create and export a singleton instance
const seasonService = new SeasonService();
export default seasonService;
