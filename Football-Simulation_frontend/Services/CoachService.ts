import apiService from './ApiService';

export interface Coach {
  id: number;
  firstName: string;
  lastName: string;
  name?: string; // Keep for backward compatibility
  nationality?: string;
  dateOfBirth?: string;
  Photo?: string;
  photoUrl?: string;
  teamId?: number;
  preferredFormation?: string;
  coachingStyle?: string;
  role?: string;
  yearsOfExperience?: number;
  biography?: string;
}

export interface CreateCoachDto {
  firstName: string;
  lastName: string;
  nationality?: string;
  dateOfBirth?: string;
  teamId?: number;
  Photo?: File;
  preferredFormation?: string;
  coachingStyle?: string;
  role?: string;
  yearsOfExperience?: number;
  biography?: string;
}

export interface UpdateCoachDto {
  firstName?: string;
  lastName?: string;
  nationality?: string;
  dateOfBirth?: string;
  teamId?: number;
  Photo?: File;
  preferredFormation?: string;
  coachingStyle?: string;
  role?: string;
  yearsOfExperience?: number;
  biography?: string;
}

export interface CoachFilter {
  nationality?: string;
  teamId?: number;
  role?: string;
}

class CoachService {
  /**
   * Get all coaches with optional filtering
   * Utilizes caching for performance optimization
   */
  public async getCoaches(filter?: CoachFilter): Promise<Coach[]> {
    try {
      const cacheKey = filter
        ? `coaches-filter-${JSON.stringify(filter)}`
        : 'coaches-all';
      const response: any = await apiService.getWithCache<{
        succeeded: boolean;
        coaches: Coach[];
        error: string | null;
      }>('/coaches/filter', {
        params: filter,
        cacheKey,
        cacheTtl: 5 * 60 * 1000, // 5 minutes cache
      });

      if (response && response.succeeded && Array.isArray(response.coaches)) {
        return response.coaches;
      }

      return [];
    } catch (error) {
      console.error('Error fetching coaches:', error);
      throw error;
    }
  }

  /**
   * Get coach by ID with caching
   */
  public async getCoachById(id: number): Promise<Coach> {
    try {
      return await apiService.getWithCache<Coach>(`/coaches/${id}`, {
        cacheKey: `coach-${id}`,
        cacheTtl: 10 * 60 * 1000, // 10 minutes cache
      });
    } catch (error) {
      console.error(`Error fetching coach with ID ${id}:`, error);
      throw error;
    }
  }

  /**
   * Create a new coach (admin only)
   * Uses FormData for image upload with retry logic
   */
  public async createCoach(coachData: CreateCoachDto): Promise<Coach> {
    try {
      const formData = new FormData();

      // Add all fields to form data
      Object.entries(coachData).forEach(([key, value]) => {
        if (value !== undefined) {
          if (key === 'Photo' && value instanceof File) {
            formData.append('Photo', value); // Changed to 'Photo' to match API
          } else if (key !== 'Photo') {
            formData.append(key, String(value));
          }
        }
      });

      const result = await apiService.uploadForm<Coach>('/coaches', formData);

      // Invalidate coaches cache after successful creation
      apiService.clearCache('^coaches');

      return result;
    } catch (error) {
      console.error('Error creating coach:', error);
      throw error;
    }
  }

  /**
   * Update a coach (admin only)
   * Uses FormData for image upload with retry logic
   */
  public async updateCoach(
    id: number,
    coachData: UpdateCoachDto
  ): Promise<Coach> {
    try {
      const formData = new FormData();

      // Add all fields to form data
      Object.entries(coachData).forEach(([key, value]) => {
        if (value !== undefined) {
          if (key === 'Photo' && value instanceof File) {
            formData.append('Photo', value); // Changed to 'Photo' to match API
          } else if (key !== 'Photo') {
            formData.append(key, String(value));
          }
        }
      });

      const result = await apiService.uploadForm<Coach>(
        `/coaches/${id}`,
        formData,
        'put'
      );

      // Invalidate coaches cache after successful update
      apiService.clearCache('^coaches');
      apiService.clearCache(`^coach-${id}$`);

      return result;
    } catch (error) {
      console.error(`Error updating coach with ID ${id}:`, error);
      throw error;
    }
  }

  /**
   * Delete a coach (admin only) with retry logic
   */
  public async deleteCoach(id: number): Promise<void> {
    try {
      await apiService.deleteWithRetry(`/coaches/${id}`);

      // Invalidate coaches cache after successful deletion
      apiService.clearCache('^coaches');
      apiService.clearCache(`^coach-${id}$`);
    } catch (error) {
      console.error(`Error deleting coach with ID ${id}:`, error);
      throw error;
    }
  }

  /**
   * Clear all coaches cache
   */
  public clearCoachesCache(): void {
    apiService.clearCache('^coaches');
    apiService.clearCache('^coach-');
  }
}

// Create and export a singleton instance
const coachService = new CoachService();
export default coachService;
