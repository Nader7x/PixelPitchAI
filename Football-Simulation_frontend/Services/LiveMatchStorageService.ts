/**
 * Utility service for managing live match and simulation data in localStorage
 */
class LiveMatchStorageService {
  private readonly LIVE_MATCH_ID_KEY = 'live_match_id';
  private readonly LIVE_SIMULATION_ID_KEY = 'live_simulation_id';
  private readonly LIVE_MATCH_DATA_KEY = 'live_match_data';
  private readonly REALTIME_STATISTICS_KEY = 'realtime_statistics';
  private readonly LAST_UPDATE_TIME_KEY = 'last_update_time';

  /**
   * Store live match ID
   */
  public setLiveMatchId(matchId: number): void {
    try {
      localStorage.setItem(this.LIVE_MATCH_ID_KEY, matchId.toString());
    } catch (error) {
      console.error('Failed to store live match ID:', error);
    }
  }

  /**
   * Get live match ID
   */
  public getLiveMatchId(): number | null {
    try {
      const matchId = localStorage.getItem(this.LIVE_MATCH_ID_KEY);
      return matchId ? parseInt(matchId, 10) : null;
    } catch (error) {
      console.error('Failed to get live match ID:', error);
      return null;
    }
  }

  /**
   * Store live simulation ID
   */
  public setLiveSimulationId(simulationId: string): void {
    try {
      localStorage.setItem(this.LIVE_SIMULATION_ID_KEY, simulationId);
    } catch (error) {
      console.error('Failed to store live simulation ID:', error);
    }
  }

  /**
   * Get live simulation ID
   */
  public getLiveSimulationId(): string | null {
    try {
      // return localStorage.getItem(this.LIVE_SIMULATION_ID_KEY);
            return localStorage.getItem('simulation_Id');
    } catch (error) {
      console.error('Failed to get live simulation ID:', error);
      return null;
    }
  }

  /**
   * Store basic live match data for quick access
   */
  public setLiveMatchData(data: {
    matchId: number;
    simulationId?: string;
    homeTeam: string;
    awayTeam: string;
    homeScore: number;
    awayScore: number;
    status: string;
  }): void {
    try {
      localStorage.setItem(this.LIVE_MATCH_DATA_KEY, JSON.stringify(data));
      this.setLiveMatchId(data.matchId);
      if (data.simulationId) {
        this.setLiveSimulationId(data.simulationId);
      }
    } catch (error) {
      console.error('Failed to store live match data:', error);
    }
  }

  /**
   * Get basic live match data
   */
  public getLiveMatchData(): any | null {
    try {
      const data = localStorage.getItem(this.LIVE_MATCH_DATA_KEY);
      return data ? JSON.parse(data) : null;
    } catch (error) {
      console.error('Failed to get live match data:', error);
      return null;
    }
  }

  /**
   * Clear all live match data
   */
  public clearLiveMatchData(): void {
    try {
      localStorage.removeItem(this.LIVE_MATCH_ID_KEY);
      localStorage.removeItem(this.LIVE_SIMULATION_ID_KEY);
      localStorage.removeItem(this.LIVE_MATCH_DATA_KEY);
      localStorage.removeItem(this.REALTIME_STATISTICS_KEY);
      localStorage.removeItem(this.LAST_UPDATE_TIME_KEY);
    } catch (error) {
      console.error('Failed to clear live match data:', error);
    }
  }

  /**
   * Store real-time match statistics
   */
  public setRealtimeStatistics(statistics: any, updateTime: Date): void {
    try {
      localStorage.setItem(
        this.REALTIME_STATISTICS_KEY,
        JSON.stringify(statistics)
      );
      localStorage.setItem(this.LAST_UPDATE_TIME_KEY, updateTime.toISOString());
      console.log('💾 Stored real-time statistics to localStorage:', {
        matchId: statistics.matchId,
        timeStamp: statistics.timeStamp,
        homeScore: statistics.homeTeam?.score,
        awayScore: statistics.awayTeam?.score,
        updateTime: updateTime.toISOString(),
      });
    } catch (error) {
      console.error('Failed to store real-time statistics:', error);
    }
  }

  /**
   * Get stored real-time match statistics
   */
  public getRealtimeStatistics(): {
    statistics: any | null;
    updateTime: Date | null;
  } {
    try {
      const statisticsData = localStorage.getItem(this.REALTIME_STATISTICS_KEY);
      const updateTimeData = localStorage.getItem(this.LAST_UPDATE_TIME_KEY);

      const statistics = statisticsData ? JSON.parse(statisticsData) : null;
      const updateTime = updateTimeData ? new Date(updateTimeData) : null;

      if (statistics && updateTime) {
        console.log('📁 Retrieved real-time statistics from localStorage:', {
          matchId: statistics.matchId,
          timeStamp: statistics.timeStamp,
          homeScore: statistics.homeTeam?.score,
          awayScore: statistics.awayTeam?.score,
          updateTime: updateTime.toISOString(),
          minutesAgo: Math.floor(
            (new Date().getTime() - updateTime.getTime()) / 60000
          ),
        });
      }

      return { statistics, updateTime };
    } catch (error) {
      console.error('Failed to get real-time statistics:', error);
      return { statistics: null, updateTime: null };
    }
  }

  /**
   * Check if stored statistics are for the current match and still relevant
   */
  public isStoredStatisticsValid(
    currentMatchId: number,
    maxAgeMinutes: number = 30
  ): boolean {
    try {
      const { statistics, updateTime } = this.getRealtimeStatistics();

      if (!statistics || !updateTime) {
        return false;
      }

      // Check if it's for the current match
      if (statistics.matchId !== currentMatchId) {
        console.log('⚠️ Stored statistics are for different match:', {
          storedMatchId: statistics.matchId,
          currentMatchId,
        });
        return false;
      }

      // Check if it's not too old
      const ageMinutes =
        (new Date().getTime() - updateTime.getTime()) / (1000 * 60);
      if (ageMinutes > maxAgeMinutes) {
        console.log('⚠️ Stored statistics are too old:', {
          ageMinutes: Math.floor(ageMinutes),
          maxAgeMinutes,
        });
        return false;
      }

      return true;
    } catch (error) {
      console.error('Failed to validate stored statistics:', error);
      return false;
    }
  }

  /**
   * Clear only the real-time statistics (keep basic match data)
   */
  public clearRealtimeStatistics(): void {
    try {
      localStorage.removeItem(this.REALTIME_STATISTICS_KEY);
      localStorage.removeItem(this.LAST_UPDATE_TIME_KEY);
      console.log('🧹 Cleared real-time statistics from localStorage');
    } catch (error) {
      console.error('Failed to clear real-time statistics:', error);
    }
  }

  /**
   * Check if current match ID matches stored live match ID
   */
  public isCurrentLiveMatch(matchId: number): boolean {
    const storedMatchId = this.getLiveMatchId();
    return storedMatchId === matchId;
  }

  /**
   * Get navigation URL for simulation view
   */
  public getSimulationViewUrl(): string | null {
    const simulationId = this.getLiveSimulationId();
    const matchId = this.getLiveMatchId();

    if (simulationId) {
      return `/simulationview/${simulationId}`;
    } else if (matchId) {
      // Fallback to match-based simulation URL
      return `/simulationview/${matchId}`;
    }

    return null;
  }
}

// Export singleton instance
const liveMatchStorageService = new LiveMatchStorageService();
export default liveMatchStorageService;
