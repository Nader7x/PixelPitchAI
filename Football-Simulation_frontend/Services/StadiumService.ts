import apiService from './ApiService';

export interface Stadium {
  id: number;
  name: string;
  capacity?: number;
  city?: string;
  country?: string;
  image?: string;
  imageUrl?: string;
  surfaceType?: string;
  address?: string;
  latitude?: number;
  longitude?: number;
  description?: string;
  facilities?: string;
  builtDate?: string; // Changed to string format for date-time
}

export interface CreateStadiumDto {
  name: string;
  capacity?: number;
  city?: string;
  country?: string;
  image?: File;
  surfaceType?: string;
  address?: string;
  latitude?: number;
  longitude?: number;
  description?: string;
  facilities?: string;
  builtDate?: string; // Changed to string format for date-time
}

export interface UpdateStadiumDto {
  name?: string;
  capacity?: number;
  city?: string;
  country?: string;
  image?: File;
  surfaceType?: string;
  address?: string;
  latitude?: number;
  longitude?: number;
  description?: string;
  facilities?: string;
  builtDate?: string; // Changed to string format for date-time
}

export interface StadiumFilter {
  country?: string;
  city?: string;
}

class StadiumService {
  /**
   * Get all stadiums with optional filtering and caching
   */
  public async getStadiums(filter?: StadiumFilter): Promise<Stadium[]> {
    try {
      const cacheKey = filter
        ? `stadiums-filter-${JSON.stringify(filter)}`
        : 'stadiums-all';
      const response: any = await apiService.getWithCache<{
        succeeded: boolean;
        stadiums: Stadium[];
        error: string | null;
      }>('/stadiums', {
        params: filter,
        cacheKey,
        cacheTtl: 5 * 60 * 1000, // 5 minutes cache
      });

      if (response && response.succeeded && Array.isArray(response.stadiums)) {
        return response.stadiums;
      }

      return [];
    } catch (error) {
      console.error('Error fetching stadiums:', error);
      throw error;
    }
  }

  /**
   * Get stadium by ID with caching
   */
  public async getStadiumById(id: number): Promise<Stadium> {
    try {
      return await apiService.getWithCache<Stadium>(`/stadiums/${id}`, {
        cacheKey: `stadium-${id}`,
        cacheTtl: 10 * 60 * 1000, // 10 minutes cache
      });
    } catch (error) {
      console.error(`Error fetching stadium with ID ${id}:`, error);
      throw error;
    }
  }

  /**
   * Create a new stadium (admin only)
   * Uses FormData for image upload with retry logic
   */
  public async createStadium(stadiumData: CreateStadiumDto): Promise<Stadium> {
    try {
      const formData = new FormData();

      // Add all fields to form data
      Object.entries(stadiumData).forEach(([key, value]) => {
        if (value !== undefined) {
          if (key === 'image' && value instanceof File) {
            formData.append(key, value);
          } else if (key !== 'image') {
            formData.append(key, String(value));
          }
        }
      });

      const result = await apiService.uploadForm<Stadium>(
        '/stadiums',
        formData
      );

      // Invalidate stadiums cache after successful creation
      apiService.clearCache('^stadiums-');

      return result;
    } catch (error) {
      console.error('Error creating stadium:', error);
      throw error;
    }
  }

  /**
   * Update a stadium (admin only)
   * Uses FormData for image upload with retry logic
   */
  public async updateStadium(
    id: number,
    stadiumData: UpdateStadiumDto
  ): Promise<Stadium> {
    try {
      const formData = new FormData();

      // Add all fields to form data
      Object.entries(stadiumData).forEach(([key, value]) => {
        if (value !== undefined) {
          if (key === 'image' && value instanceof File) {
            formData.append(key, value);
          } else if (key !== 'image') {
            formData.append(key, String(value));
          }
        }
      });

      const result = await apiService.uploadForm<Stadium>(
        `/stadiums/${id}`,
        formData,
        'put'
      );

      // Invalidate stadiums cache after successful update
      apiService.clearCache('^stadiums');
      apiService.clearCache(`^stadium-${id}$`);

      return result;
    } catch (error) {
      console.error(`Error updating stadium with ID ${id}:`, error);
      throw error;
    }
  }

  /**
   * Delete a stadium (admin only) with retry logic
   */
  public async deleteStadium(id: number): Promise<void> {
    try {
      await apiService.deleteWithRetry(`/stadiums/${id}`);

      // Invalidate stadiums cache after successful deletion
      apiService.clearCache('^stadiums');
      apiService.clearCache(`^stadium-${id}$`);
    } catch (error) {
      console.error(`Error deleting stadium with ID ${id}:`, error);
      throw error;
    }
  }

  /**
   * Clear all stadiums cache
   */
  public clearStadiumsCache(): void {
    apiService.clearCache('^stadiums');
    apiService.clearCache('^stadium-');
  }
}

// Create and export a singleton instance
const stadiumService = new StadiumService();
export default stadiumService;
