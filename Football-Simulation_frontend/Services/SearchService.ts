import apiService from './ApiService';
import { Team } from './TeamService';
import { Match } from './MatchService';
import { Player } from './PlayerService';
import { Coach } from './CoachService';

// API Response Interfaces based on search-docs.md
export interface SearchResultItem {
  id: string;
  type: 'Team' | 'Player' | 'Coach' | 'Stadium' | 'Match';
  name: string;
  description: string;
  thumbnailUrl?: string;
  url: string;
  additionalData: { [key: string]: any };
}

export interface GlobalSearchResponse {
  totalResults: number;
  currentPage: number;
  totalPages: number;
  pageSize: number;
  items: SearchResultItem[];
}

export interface SearchSuggestion {
  text: string;
  type: string;
  description: string;
  relevance: number;
  thumbnailUrl?: string;
  additionalData: { [key: string]: any };
}

export interface SearchAnalytics {
  query: string;
  strategyUsed: string;
  searchDuration: string;
  totalResultsFound: number;
  resultsByEntityType: { [key: string]: number };
  averageRelevanceScore: number;
  usedFallbackSearch: boolean;
  searchSuggestions: string[];
}

export interface AdvancedSearchFilters {
  query: string;
  entityTypes?: string[];
  country?: string;
  league?: string;
  position?: string;
  role?: string;
  fromDate?: string;
  toDate?: string;
  minCapacity?: number;
  maxCapacity?: number;
  page?: number;
  pageSize?: number;
  sortBy?: string;
  sortDescending?: boolean;
  enableFuzzySearch?: boolean;
  strategy?: 'Auto' | 'FullText' | 'Fuzzy' | 'Hybrid';
}

// Legacy interface for backward compatibility
export interface SearchResult {
  teams: Team[];
  matches: Match[];
  players: Player[];
  coaches: Coach[];
  totalResults: number;
  currentPage: number;
  totalPages: number;
}

export interface SearchParams {
  query: string;
  page?: number;
  pageSize?: number;
}

class SearchService {
  private readonly baseUrl = '/search';
  private readonly retryAttempts = 3;
  private readonly retryDelay = 1000; // 1 second
  private searchCache = new Map<string, { data: any; expiry: number }>();
  private readonly cacheExpiry = 5 * 60 * 1000; // 5 minutes

  /**
   * Cache management for search results
   */
  private getCachedResult<T>(key: string): T | null {
    const cached = this.searchCache.get(key);
    if (cached && Date.now() < cached.expiry) {
      return cached.data as T;
    }
    if (cached) {
      this.searchCache.delete(key);
    }
    return null;
  }

  private setCachedResult<T>(key: string, data: T): void {
    this.searchCache.set(key, {
      data,
      expiry: Date.now() + this.cacheExpiry,
    });
  }

  /**
   * Clear expired cache entries
   */
  public clearExpiredCache(): void {
    const now = Date.now();
    for (const [key, value] of this.searchCache.entries()) {
      if (now >= value.expiry) {
        this.searchCache.delete(key);
      }
    }
  }

  /**
   * Retry helper function for failed API calls
   */
  private async retryOperation<T>(
    operation: () => Promise<T>,
    attempts: number = this.retryAttempts
  ): Promise<T> {
    try {
      return await operation();
    } catch (error) {
      if (attempts > 1) {
        await new Promise((resolve) => setTimeout(resolve, this.retryDelay));
        return this.retryOperation(operation, attempts - 1);
      }
      throw error;
    }
  }

  /**
   * Fallback search when primary search fails
   */
  private async fallbackSearch(
    query: string,
    page: number = 1,
    pageSize: number = 10
  ): Promise<GlobalSearchResponse> {
    // Simple fallback search without advanced features
    return {
      totalResults: 0,
      currentPage: page,
      totalPages: 0,
      pageSize,
      items: [],
    };
  }

  /**
   * Global search across all entities with ranking and relevance
   * @param query Search term (minimum 2 characters)
   * @param page Page number (default: 1)
   * @param pageSize Number of results per page (default: 10, max: 50)
   */
  public async globalSearch(
    query: string,
    page: number = 1,
    pageSize: number = 10
  ): Promise<GlobalSearchResponse> {
    try {
      if (!query || query.length < 2) {
        throw new Error('Search query must be at least 2 characters long');
      }

      const cacheKey = `global-search-${query}-${page}-${pageSize}`;
      const cachedResult = this.getCachedResult<GlobalSearchResponse>(cacheKey);
      if (cachedResult) {
        return cachedResult;
      }

      return await this.retryOperation(async () => {
        const response = await apiService.getWithCache<GlobalSearchResponse>(
          this.baseUrl,
          {
            params: { query, page, pageSize },
            cacheKey,
            cacheTtl: 2 * 60 * 1000, // 2 minutes cache
          }
        );

        // Cache the result
        this.setCachedResult(cacheKey, response);

        return response;
      });
    } catch (error) {
      console.error('Error performing global search:', error);
      // Return fallback search results
      try {
        return await this.fallbackSearch(query, page, pageSize);
      } catch (fallbackError) {
        console.error('Fallback search also failed:', fallbackError);
        throw error;
      }
    }
  }

  /**
   * Advanced search with strategy selection
   * @param query Search term
   * @param strategy Search strategy (Auto, FullText, Fuzzy, Hybrid)
   * @param page Page number
   * @param pageSize Results per page
   */
  public async searchWithStrategy(
    query: string,
    strategy: 'Auto' | 'FullText' | 'Fuzzy' | 'Hybrid' = 'Auto',
    page: number = 1,
    pageSize: number = 10
  ): Promise<GlobalSearchResponse> {
    try {
      if (!query || query.length < 2) {
        throw new Error('Search query must be at least 2 characters long');
      }

      const cacheKey = `strategy-search-${query}-${strategy}-${page}-${pageSize}`;
      const cachedResult = this.getCachedResult<GlobalSearchResponse>(cacheKey);
      if (cachedResult) {
        return cachedResult;
      }

      return await apiService.getWithCache<GlobalSearchResponse>(
        `${this.baseUrl}/strategy`,
        {
          params: { query, strategy, page, pageSize },
          cacheKey,
          cacheTtl: 2 * 60 * 1000,
        }
      );
    } catch (error) {
      console.error('Error performing strategy search:', error);
      throw error;
    }
  }

  /**
   * Advanced search with filters and sorting
   * @param filters Advanced search filters
   */
  public async advancedSearch(
    filters: AdvancedSearchFilters
  ): Promise<GlobalSearchResponse> {
    try {
      if (!filters.query || filters.query.length < 2) {
        throw new Error('Search query must be at least 2 characters long');
      }

      return await apiService.post<GlobalSearchResponse>(
        `${this.baseUrl}/filtered`,
        filters
      );
    } catch (error) {
      console.error('Error performing advanced search:', error);
      throw error;
    }
  }

  /**
   * Unified search across selected entity types
   * @param query Search term
   * @param entityTypes Array of entity types to search
   * @param page Page number
   * @param pageSize Results per page
   */
  public async unifiedSearch(
    query: string,
    entityTypes?: string[],
    page: number = 1,
    pageSize: number = 10
  ): Promise<GlobalSearchResponse> {
    try {
      if (!query || query.length < 2) {
        throw new Error('Search query must be at least 2 characters long');
      }

      const params: any = { query, page, pageSize };
      if (entityTypes && entityTypes.length > 0) {
        params.entityTypes = entityTypes.join(',');
      }

      const cacheKey = `unified-search-${query}-${entityTypes?.join(',') || 'all'}-${page}-${pageSize}`;
      const cachedResult = this.getCachedResult<GlobalSearchResponse>(cacheKey);
      if (cachedResult) {
        return cachedResult;
      }

      return await apiService.getWithCache<GlobalSearchResponse>(
        `${this.baseUrl}/unified`,
        {
          params,
          cacheKey,
          cacheTtl: 2 * 60 * 1000,
        }
      );
    } catch (error) {
      console.error('Error performing unified search:', error);
      throw error;
    }
  }

  /**
   * Search all entities with optional fuzzy matching
   * @param query Search term
   * @param enableFuzzySearch Enable fuzzy/approximate matching
   * @param page Page number
   * @param pageSize Results per page
   */
  public async searchAll(
    query: string,
    enableFuzzySearch: boolean = false,
    page: number = 1,
    pageSize: number = 10
  ): Promise<GlobalSearchResponse> {
    try {
      if (!query || query.length < 2) {
        throw new Error('Search query must be at least 2 characters long');
      }

      const cacheKey = `search-all-${query}-${enableFuzzySearch}-${page}-${pageSize}`;
      const cachedResult = this.getCachedResult<GlobalSearchResponse>(cacheKey);
      if (cachedResult) {
        return cachedResult;
      }

      return await apiService.getWithCache<GlobalSearchResponse>(
        `${this.baseUrl}/all`,
        {
          params: { query, enableFuzzySearch, page, pageSize },
          cacheKey,
          cacheTtl: 2 * 60 * 1000,
        }
      );
    } catch (error) {
      console.error('Error performing search all:', error);
      throw error;
    }
  }

  /**
   * Search teams with advanced ranking
   * @param query Search term
   * @param limit Max results (default: 10, max: 50)
   * @param enableFuzzySearch Enable fuzzy search
   * @param advanced Use advanced ranking
   */
  public async searchTeams(
    query: string,
    limit: number = 10,
    enableFuzzySearch: boolean = false,
    advanced: boolean = true
  ): Promise<SearchResultItem[]> {
    try {
      if (!query || query.length < 2) {
        throw new Error('Search query must be at least 2 characters long');
      }

      const cacheKey = `search-teams-${query}-${limit}-${enableFuzzySearch}-${advanced}`;
      const cachedResult = this.getCachedResult<SearchResultItem[]>(cacheKey);
      if (cachedResult) {
        return cachedResult;
      }

      return await apiService.getWithCache<SearchResultItem[]>(
        `${this.baseUrl}/teams`,
        {
          params: { query, limit, enableFuzzySearch, advanced },
          cacheKey,
          cacheTtl: 2 * 60 * 1000,
        }
      );
    } catch (error) {
      console.error('Error searching teams:', error);
      throw error;
    }
  }

  /**
   * Search players with advanced ranking
   * @param query Search term
   * @param limit Max results (default: 10, max: 50)
   * @param enableFuzzySearch Enable fuzzy search
   * @param advanced Use advanced ranking
   */
  public async searchPlayers(
    query: string,
    limit: number = 10,
    enableFuzzySearch: boolean = false,
    advanced: boolean = true
  ): Promise<SearchResultItem[]> {
    try {
      if (!query || query.length < 2) {
        throw new Error('Search query must be at least 2 characters long');
      }

      const cacheKey = `search-players-${query}-${limit}-${enableFuzzySearch}-${advanced}`;
      const cachedResult = this.getCachedResult<SearchResultItem[]>(cacheKey);
      if (cachedResult) {
        return cachedResult;
      }

      return await apiService.getWithCache<SearchResultItem[]>(
        `${this.baseUrl}/players`,
        {
          params: { query, limit, enableFuzzySearch, advanced },
          cacheKey,
          cacheTtl: 2 * 60 * 1000,
        }
      );
    } catch (error) {
      console.error('Error searching players:', error);
      throw error;
    }
  }

  /**
   * Get search suggestions for autocomplete
   * @param query Partial search term
   * @param limit Max suggestions (default: 5)
   */
  public async getSuggestions(
    query: string,
    limit: number = 5
  ): Promise<SearchSuggestion[]> {
    try {
      if (!query || query.length < 1) {
        return [];
      }

      const cacheKey = `suggestions-${query}-${limit}`;

      return await apiService.getWithCache<SearchSuggestion[]>(
        `${this.baseUrl}/suggestions`,
        {
          params: { query, limit },
          cacheKey,
          cacheTtl: 30 * 1000, // 30 seconds cache for suggestions
        }
      );
    } catch (error) {
      console.error('Error getting search suggestions:', error);
      return [];
    }
  }

  /**
   * Get search analytics and statistics
   * @param query Search term to analyze
   */
  public async getSearchAnalytics(query: string): Promise<SearchAnalytics> {
    try {
      if (!query || query.length < 2) {
        throw new Error('Search query must be at least 2 characters long');
      }

      return await apiService.get<SearchAnalytics>(
        `${this.baseUrl}/analytics`,
        {
          params: { query },
        }
      );
    } catch (error) {
      console.error('Error getting search analytics:', error);
      throw error;
    }
  }

  /**
   * Legacy search method for backward compatibility
   * Converts new API response to legacy format
   */
  public async search(params: SearchParams): Promise<SearchResult> {
    try {
      const response = await this.globalSearch(
        params.query,
        params.page,
        params.pageSize
      );

      // Convert new format to legacy format
      const legacyResult: SearchResult = {
        teams: response.items
          .filter((item) => item.type === 'Team')
          .map((item) => ({
            id: parseInt(item.id),
            name: item.name,
            description: item.description,
            imageUrl: item.thumbnailUrl,
            ...item.additionalData,
          })) as Team[],
        players: response.items
          .filter((item) => item.type === 'Player')
          .map((item) => ({
            id: parseInt(item.id),
            fullName: item.name, // Map name to fullName as required by Player interface
            position: item.additionalData?.position || '', // Required field in Player interface
            photoUrl: item.thumbnailUrl || '', // Required field in Player interface
            ...item.additionalData,
          })) as Player[],
        coaches: response.items
          .filter((item) => item.type === 'Coach')
          .map((item) => {
            // Split name into firstName and lastName for Coach interface
            const nameParts = item.name.split(' ');
            const firstName = nameParts[0] || '';
            const lastName = nameParts.slice(1).join(' ') || '';

            return {
              id: parseInt(item.id),
              firstName: firstName,
              lastName: lastName,
              name: item.name,
              photoUrl: item.thumbnailUrl,
              ...item.additionalData,
            };
          }) as Coach[],
        matches: response.items
          .filter((item) => item.type === 'Match')
          .map((item) => ({
            id: parseInt(item.id),
            name: item.name,
            description: item.description,
            seasonId: item.additionalData?.seasonId || 0,
            homeTeamId: item.additionalData?.homeTeamId || 0,
            awayTeamId: item.additionalData?.awayTeamId || 0,
            ...item.additionalData,
          })) as Match[],
        totalResults: response.totalResults,
        currentPage: response.currentPage,
        totalPages: response.totalPages,
      };

      return legacyResult;
    } catch (error) {
      console.error('Error performing legacy search:', error);
      throw error;
    }
  }

  /**
   * Quick search with minimal caching for autocomplete/suggestions
   * @param query Quick search query
   */
  public async quickSearch(query: string): Promise<SearchResult> {
    try {
      if (!query || query.length < 1) {
        return {
          teams: [],
          matches: [],
          players: [],
          coaches: [],
          totalResults: 0,
          currentPage: 1,
          totalPages: 0,
        };
      }

      return await this.search({
        query,
        page: 1,
        pageSize: 5, // Smaller page size for quick search
      });
    } catch (error) {
      console.error('Error performing quick search:', error);
      throw error;
    }
  }

  /**
   * Clear search cache
   */
  public clearSearchCache(): void {
    apiService.clearCache(
      '^(global-search|strategy-search|unified-search|search-all|search-teams|search-players|suggestions)-'
    );
  }

  /**
   * Get popular/trending searches
   */
  public async getTrendingSearches(limit: number = 10): Promise<string[]> {
    try {
      const cacheKey = `trending-searches-${limit}`;
      const cached = this.getCachedResult<string[]>(cacheKey);
      if (cached) {
        return cached;
      }

      const trending = await apiService.get<string[]>(
        `${this.baseUrl}/trending`,
        {
          params: { limit },
        }
      );

      this.setCachedResult(cacheKey, trending);
      return trending;
    } catch (error) {
      console.error('Error getting trending searches:', error);
      // Return fallback trending searches
      return [
        'Barcelona',
        'Real Madrid',
        'Manchester United',
        'Liverpool',
        'PSG',
      ];
    }
  }

  /**
   * Get recent searches for user
   */
  public getRecentSearches(): string[] {
    try {
      const recent = localStorage.getItem('recent-searches');
      return recent ? JSON.parse(recent) : [];
    } catch (error) {
      console.error('Error getting recent searches:', error);
      return [];
    }
  }

  /**
   * Add search to recent searches
   */
  public addToRecentSearches(query: string): void {
    try {
      if (!query.trim()) return;

      const recent = this.getRecentSearches();
      const updated = [query, ...recent.filter((q) => q !== query)].slice(
        0,
        10
      );
      localStorage.setItem('recent-searches', JSON.stringify(updated));
    } catch (error) {
      console.error('Error adding to recent searches:', error);
    }
  }

  /**
   * Clear recent searches
   */
  public clearRecentSearches(): void {
    try {
      localStorage.removeItem('recent-searches');
    } catch (error) {
      console.error('Error clearing recent searches:', error);
    }
  }
}

// Create and export a singleton instance
const searchService = new SearchService();
export default searchService;
