import apiService from './ApiService';

export interface Player {
  id: number;
  fullName: string;
  knownName?: string;
  position: string;
  shirtNumber?: number;
  nationality?: string;
  image?: string;
  preferredFoot?: 'Left' | 'Right' | 'Both';
  teamId?: number;
  photoUrl: string;
}

export interface CreatePlayerDto {
  fullName: string;
  knownName?: string;
  position: string;
  shirtNumber?: number;
  nationality?: string;
  preferredFoot?: 'Left' | 'Right' | 'Both';
  teamId?: number | null;
  photo?: File;
}

export interface UpdatePlayerDto {
  fullName?: string;
  knownName?: string;
  position?: string;
  shirtNumber?: number;
  nationality?: string;
  preferredFoot?: 'Left' | 'Right' | 'Both';
  teamId?: number | null;
  photo?: File;
}

export interface PlayerFilter {
  nationality?: string;
  preferredFoot?: 'Left' | 'Right' | 'Both';
  teamId?: number;
  pageNumber?: number;
  pageSize?: number;
}

export interface PaginatedPlayerResponse {
  succeeded: boolean;
  players: Player[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
  error: string | null;
}

interface PlayerResponse {
  succeeded: boolean;
  players: Player[];
  error: string | null;
}

class PlayerService {
  /**
   * Get all players with optional filtering and caching
   */
  public async getPlayers(filter?: PlayerFilter): Promise<Player[]> {
    try {
      const cacheKey = filter
        ? `players-filter-${JSON.stringify(filter)}`
        : 'players-all';
      const response = await apiService.getWithCache<PlayerResponse>(
        '/players',
        {
          params: filter,
          cacheKey,
          cacheTtl: 5 * 60 * 1000, // 5 minutes cache
        }
      );
      if (!response.succeeded) {
        throw new Error(response.error || 'Failed to fetch players');
      }
      return response.players;
    } catch (error) {
      console.error('Error fetching players:', error);
      throw error;
    }
  }

  /**
   * Get paginated players with optional filtering
   */
  public async getPaginatedPlayers(
    filter?: PlayerFilter
  ): Promise<PaginatedPlayerResponse> {
    try {
      const params = {
        pageNumber: filter?.pageNumber || 1,
        pageSize: filter?.pageSize || 25,
        nationality: filter?.nationality,
        preferredFoot: filter?.preferredFoot,
        teamId: filter?.teamId,
      };

      console.log(
        'PlayerService - Making paginated API call with params:',
        params
      );

      const cacheKey = `players-paginated-${JSON.stringify(params)}`;
      const response = await apiService.getWithCache<PaginatedPlayerResponse>(
        '/players', // Try specific pagination endpoint first
        {
          params,
          cacheKey,
          cacheTtl: 5 * 60 * 1000, // 5 minutes cache
        }
      );

      console.log('PlayerService - Raw API response:', response);
      console.log('PlayerService - Response keys:', Object.keys(response));
      console.log('PlayerService - Response type:', typeof response);

      // More detailed response analysis
      console.log('=== DETAILED API RESPONSE ANALYSIS ===');
      console.log('Response object:', JSON.stringify(response, null, 2));
      console.log('Response succeeded:', response.succeeded);
      console.log('Response players type:', typeof response.players);
      console.log(
        'Response players is array:',
        Array.isArray(response.players)
      );
      console.log('Response totalCount type:', typeof response.totalCount);
      console.log('Response totalPages type:', typeof response.totalPages);
      console.log('=============================================');

      // Check if the response has the expected structure
      if (response && typeof response === 'object') {
        console.log(
          'PlayerService - Players array length:',
          response.players?.length || 'undefined'
        );
        console.log('PlayerService - TotalCount:', response.totalCount);
        console.log('PlayerService - Succeeded:', response.succeeded);

        // Check if response is wrapped in a data property
        let actualResponse = response;
        const responseAsAny = response as any;
        if (responseAsAny.data && typeof responseAsAny.data === 'object') {
          console.log(
            'PlayerService - Response has data wrapper, unwrapping...'
          );
          actualResponse = responseAsAny.data;
          console.log('PlayerService - Unwrapped response:', actualResponse);
        }

        // If the API returns players but no pagination metadata, calculate it
        if (actualResponse.players && Array.isArray(actualResponse.players)) {
          const pageNumber = params.pageNumber || 1;
          const pageSize = params.pageSize || 25;
          const totalPlayers = actualResponse.players.length;

          // Check if we have proper pagination metadata
          const hasValidPagination =
            actualResponse.totalCount > 0 && actualResponse.totalPages > 0;

          if (!hasValidPagination) {
            console.log(
              'PlayerService - Invalid or missing pagination metadata, calculating...'
            );

            // CRITICAL FIX: If we're getting a full dataset (like 300 players),
            // this is likely the entire dataset, not a paginated subset
            let finalTotalCount = totalPlayers;

            // If the API returned a totalCount of 0 but we have players,
            // use the actual number of players returned as the total
            if (actualResponse.totalCount === 0 && totalPlayers > 0) {
              console.log(
                'PlayerService - API returned totalCount:0 but has players, using player count as total'
              );
              finalTotalCount = totalPlayers;
            }

            // If we got more players than our page size, this might be the full dataset
            if (totalPlayers > pageSize) {
              console.log(
                'PlayerService - Got more players than page size, treating as full dataset'
              );

              // For client-side pagination, slice the data for the current page
              const startIndex = (pageNumber - 1) * pageSize;
              const endIndex = startIndex + pageSize;
              const paginatedPlayers = actualResponse.players.slice(
                startIndex,
                endIndex
              );

              const enhancedResponse: PaginatedPlayerResponse = {
                succeeded: true,
                players: paginatedPlayers, // Return only the current page
                totalCount: finalTotalCount,
                pageNumber: pageNumber,
                pageSize: pageSize,
                totalPages: Math.ceil(finalTotalCount / pageSize),
                hasPreviousPage: pageNumber > 1,
                hasNextPage: pageNumber < Math.ceil(finalTotalCount / pageSize),
                error: null,
              };

              console.log(
                'PlayerService - Client-side pagination response:',
                enhancedResponse
              );
              return enhancedResponse;
            } else {
              // Server-side pagination case or small dataset
              const enhancedResponse: PaginatedPlayerResponse = {
                succeeded: true,
                players: actualResponse.players,
                totalCount: finalTotalCount,
                pageNumber: pageNumber,
                pageSize: pageSize,
                totalPages: Math.ceil(finalTotalCount / pageSize),
                hasPreviousPage: pageNumber > 1,
                hasNextPage: pageNumber < Math.ceil(finalTotalCount / pageSize),
                error: null,
              };

              console.log(
                'PlayerService - Server-side pagination response:',
                enhancedResponse
              );
              return enhancedResponse;
            }
          } else {
            console.log('PlayerService - Valid pagination metadata found');
            return actualResponse as PaginatedPlayerResponse;
          }
        }
      }

      if (!response.succeeded) {
        throw new Error(response.error || 'Failed to fetch players');
      }

      return response;
    } catch (error) {
      console.error('Error fetching paginated players:', error);

      // Try the regular endpoint with pagination parameters as fallback
      try {
        console.log(
          'PlayerService - Trying regular endpoint with pagination params'
        );
        const params = {
          pageNumber: filter?.pageNumber || 1,
          pageSize: filter?.pageSize || 25,
          nationality: filter?.nationality,
          preferredFoot: filter?.preferredFoot,
          teamId: filter?.teamId,
        };

        const response = await apiService.getWithCache<PaginatedPlayerResponse>(
          '/players',
          {
            params,
            cacheKey: `players-regular-paginated-${JSON.stringify(params)}`,
            cacheTtl: 5 * 60 * 1000,
          }
        );

        console.log('PlayerService - Fallback response:', response);
        console.log(
          'PlayerService - Fallback response keys:',
          Object.keys(response)
        );

        if (response.succeeded) {
          // Check if we need to enhance the response structure
          let actualResponse = response;
          const responseAsAny = response as any;
          if (responseAsAny.data && typeof responseAsAny.data === 'object') {
            console.log(
              'PlayerService - Fallback: Response has data wrapper, unwrapping...'
            );
            actualResponse = responseAsAny.data;
          }

          if (actualResponse.players && Array.isArray(actualResponse.players)) {
            const hasValidPagination =
              actualResponse.totalCount > 0 && actualResponse.totalPages > 0;

            if (!hasValidPagination) {
              console.log(
                'PlayerService - Fallback: No pagination metadata, calculating...'
              );
              const pageNumber = params.pageNumber || 1;
              const pageSize = params.pageSize || 25;
              const totalPlayers = actualResponse.players.length;

              // Apply the same logic as main endpoint
              let finalTotalCount = totalPlayers;

              if (actualResponse.totalCount === 0 && totalPlayers > 0) {
                console.log(
                  'PlayerService - Fallback: API returned totalCount:0 but has players'
                );
                finalTotalCount = totalPlayers;
              }

              if (totalPlayers > pageSize) {
                console.log(
                  'PlayerService - Fallback: Client-side pagination for full dataset'
                );
                const startIndex = (pageNumber - 1) * pageSize;
                const endIndex = startIndex + pageSize;
                const paginatedPlayers = actualResponse.players.slice(
                  startIndex,
                  endIndex
                );

                const enhancedResponse: PaginatedPlayerResponse = {
                  succeeded: true,
                  players: paginatedPlayers,
                  totalCount: finalTotalCount,
                  pageNumber: pageNumber,
                  pageSize: pageSize,
                  totalPages: Math.ceil(finalTotalCount / pageSize),
                  hasPreviousPage: pageNumber > 1,
                  hasNextPage:
                    pageNumber < Math.ceil(finalTotalCount / pageSize),
                  error: null,
                };

                console.log(
                  'PlayerService - Fallback client-side pagination response:',
                  enhancedResponse
                );
                return enhancedResponse;
              } else {
                const enhancedResponse: PaginatedPlayerResponse = {
                  succeeded: true,
                  players: actualResponse.players,
                  totalCount: finalTotalCount,
                  pageNumber: pageNumber,
                  pageSize: pageSize,
                  totalPages: Math.ceil(finalTotalCount / pageSize),
                  hasPreviousPage: pageNumber > 1,
                  hasNextPage:
                    pageNumber < Math.ceil(finalTotalCount / pageSize),
                  error: null,
                };

                console.log(
                  'PlayerService - Fallback enhanced response:',
                  enhancedResponse
                );
                return enhancedResponse;
              }
            }
          }

          console.log(
            'PlayerService - Regular endpoint with pagination worked:',
            response
          );
          return response;
        }
      } catch (fallbackError) {
        console.error(
          'PlayerService - Both pagination endpoints failed:',
          fallbackError
        );
      }

      throw error;
    }
  }

  /**
   * Get player by ID with caching
   */
  public async getPlayerById(id: number): Promise<Player> {
    try {
      const response = await apiService.getWithCache<{
        succeeded: boolean;
        player: Player;
        error: string | null;
      }>(`/players/${id}`, {
        cacheKey: `player-${id}`,
        cacheTtl: 10 * 60 * 1000, // 10 minutes cache
      });
      if (!response.succeeded) {
        throw new Error(
          response.error || `Failed to fetch player with ID ${id}`
        );
      }
      return response.player;
    } catch (error) {
      console.error(`Error fetching player with ID ${id}:`, error);
      throw error;
    }
  }

  /**
   * Create a new player (admin only)
   * Uses FormData for image upload with retry logic
   */
  public async createPlayer(playerData: CreatePlayerDto): Promise<Player> {
    try {
      const formData = new FormData();

      // Add all fields to form data
      Object.entries(playerData).forEach(([key, value]) => {
        if (value !== undefined) {
          if (key === 'photo' && value instanceof File) {
            formData.append(key, value);
          } else if (key !== 'photo') {
            // Handle null value for teamId explicitly
            if (key === 'teamId' && value === null) {
              formData.append(key, '');
            } else {
              formData.append(key, String(value));
            }
          }
        }
      });

      const response = await apiService.uploadForm<{
        succeeded: boolean;
        player: Player;
        error: string | null;
      }>('/players', formData);

      if (!response.succeeded) {
        throw new Error(response.error || 'Failed to create player');
      }

      // Invalidate players cache after successful creation
      apiService.clearCache('^players');
      apiService.clearCache('^players-paginated');

      return response.player;
    } catch (error) {
      console.error('Error creating player:', error);
      throw error;
    }
  }

  /**
   * Update a player (admin only)
   * Uses FormData for image upload with retry logic
   */
  public async updatePlayer(
    id: number,
    playerData: UpdatePlayerDto
  ): Promise<Player> {
    try {
      const formData = new FormData();

      // Add all fields to form data
      Object.entries(playerData).forEach(([key, value]) => {
        if (value !== undefined) {
          if (key === 'photo' && value instanceof File) {
            formData.append(key, value);
          } else if (key !== 'photo') {
            // Handle null value for teamId explicitly
            if (key === 'teamId' && value === null) {
              formData.append(key, '');
            } else {
              formData.append(key, String(value));
            }
          }
        }
      });

      const response = await apiService.uploadForm<{
        succeeded: boolean;
        player: Player;
        error: string | null;
      }>(`/players/${id}`, formData, 'put');

      if (!response.succeeded) {
        throw new Error(
          response.error || `Failed to update player with ID ${id}`
        );
      }

      // Invalidate players cache after successful update
      apiService.clearCache('^players');
      apiService.clearCache('^players-paginated');
      apiService.clearCache(`^player-${id}$`);

      return response.player;
    } catch (error) {
      console.error(`Error updating player with ID ${id}:`, error);
      throw error;
    }
  }

  /**
   * Delete a player (admin only) with retry logic
   */
  public async deletePlayer(id: number): Promise<void> {
    try {
      const response = await apiService.deleteWithRetry<{
        succeeded: boolean;
        error: string | null;
      }>(`/players/${id}`);

      if (!response.succeeded) {
        throw new Error(
          response.error || `Failed to delete player with ID ${id}`
        );
      }

      // Invalidate players cache after successful deletion
      apiService.clearCache('^players');
      apiService.clearCache('^players-paginated');
      apiService.clearCache(`^player-${id}$`);
    } catch (error) {
      console.error(`Error deleting player with ID ${id}:`, error);
      throw error;
    }
  }

  /**
   * Clear all players cache
   */
  public clearPlayersCache(): void {
    apiService.clearCache('^players');
    apiService.clearCache('^players-paginated');
    apiService.clearCache('^player-');
  }
}

// Create and export a singleton instance
const playerService = new PlayerService();
export default playerService;
