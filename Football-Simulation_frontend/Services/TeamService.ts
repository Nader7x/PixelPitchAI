import apiService from './ApiService';
import { Stadium } from '@/Services/StadiumService';
import { Coach } from '@/Services/CoachService';

export interface Team {
  id: number;
  name: string;
  shortName?: string;
  logo?: string;
  primaryColor?: string;
  secondaryColor?: string;
  city?: string;
  country?: string;
  stadiumId?: number;
  stadium?: Stadium;
  coachId?: number;
  coach?: Coach;
  league?: string;
  foundationDate?: string;
}

export interface CreateTeamRequest {
  name: string;
  shortName?: string;
  logo?: string;
  primaryColor?: string;
  secondaryColor?: string;
  city?: string;
  country?: string;
  FoundationDate?: string; // Date-time format
  stadiumId?: number;
  coachId?: number;
  league?: string;
}

export interface UpdateTeamRequest {
  name?: string;
  shortName?: string;
  logo?: string;
  primaryColor?: string;
  secondaryColor?: string;
  city?: string;
  country?: string;
  FoundationDate?: string; // Date-time format
  stadiumId?: number;
  coachId?: number;
  league?: string;
}

class TeamService {
  /**
   * Get all teams with caching
   */
  public async getAllTeams(): Promise<Team[]> {
    try {
      const response: any = await apiService.getWithCache<Team[]>('/teams', {
        cacheKey: 'all-teams',
        cacheTtl: 5 * 60 * 1000, // Cache for 5 minutes
      });
      return response.teams;
    } catch (error) {
      console.error('Error fetching all teams:', error);
      throw error;
    }
  }

  /**
   * Get team by ID with caching
   */
  public async getTeamById(id: number): Promise<Team> {
    try {
      return await apiService.getWithCache<Team>(`/teams/${id}`, {
        cacheKey: `team-${id}`,
        cacheTtl: 10 * 60 * 1000, // Cache for 10 minutes
      });
    } catch (error) {
      console.error(`Error fetching team with ID ${id}:`, error);
      throw error;
    }
  }

  /**
   * Create a new team with retry logic (admin only)
   */
  public async createTeam(teamData: CreateTeamRequest): Promise<Team> {
    try {
      const result = await apiService.postWithRetry<Team>('/teams', teamData);

      // Clear teams cache after successful creation
      apiService.clearCache('all-teams|teams');

      return result;
    } catch (error) {
      console.error('Error creating team:', error);
      throw error;
    }
  }

  /**
   * Create a new team with image upload and retry logic (multipart/form-data)
   */
  public async createTeamWithImage(formData: FormData): Promise<Team> {
    try {
      const result = await apiService.uploadForm<Team>(
        '/teams',
        formData,
        'post'
      );

      // Clear teams cache after successful creation
      apiService.clearCache('all-teams|teams');

      return result;
    } catch (error) {
      console.error('Error creating team with image:', error);
      throw error;
    }
  }

  /**
   * Update a team with retry logic (admin only)
   */
  public async updateTeam(
    id: number,
    teamData: UpdateTeamRequest
  ): Promise<Team> {
    try {
      const result = await apiService.putWithRetry<Team>(
        `/teams/${id}`,
        teamData
      );

      // Clear specific team cache and teams list cache
      apiService.clearCache(`team-${id}|all-teams|teams`);

      return result;
    } catch (error) {
      console.error(`Error updating team with ID ${id}:`, error);
      throw error;
    }
  }

  /**
   * Update a team with image upload and retry logic (multipart/form-data)
   */
  public async updateTeamWithImage(
    id: number,
    formData: FormData
  ): Promise<Team> {
    try {
      const result = await apiService.uploadForm<Team>(
        `/teams/${id}`,
        formData,
        'put'
      );

      // Clear specific team cache and teams list cache
      apiService.clearCache(`team-${id}|all-teams|teams`);

      return result;
    } catch (error) {
      console.error(`Error updating team with ID ${id} with image:`, error);
      throw error;
    }
  }

  /**
   * Delete a team with retry logic (admin only)
   */
  public async deleteTeam(id: number): Promise<void> {
    try {
      await apiService.deleteWithRetry(`/teams/${id}`);

      // Clear specific team cache and teams list cache
      apiService.clearCache(`team-${id}|all-teams|teams`);
    } catch (error) {
      console.error(`Error deleting team with ID ${id}:`, error);
      throw error;
    }
  }

  /**
   * Clear all team-related cache
   */
  public clearTeamsCache(): void {
    apiService.clearCache('team|all-teams');
  }
}

// Create and export a singleton instance
const teamService = new TeamService();
export default teamService;
