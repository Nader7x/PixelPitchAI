// lib/teamStorage.ts
/**
 * Utility functions for managing team names in localStorage
 * Used to persist team information across page reloads during simulations
 */

export const STORAGE_KEYS = {
  HOME_TEAM: 'homeTeamName',
  AWAY_TEAM: 'awayTeamName',
  HOME_SCORE: 'homeScore',
  AWAY_SCORE: 'awayScore',
  MATCH_TIME: 'matchTime',
  MATCH_ID: 'matchId',
} as const;

/**
 * Store team names in localStorage
 */
export const storeTeamNames = (homeTeam: string, awayTeam: string): void => {
  if (typeof window === 'undefined') return;

  try {
    localStorage.setItem(STORAGE_KEYS.HOME_TEAM, homeTeam);
    localStorage.setItem(STORAGE_KEYS.AWAY_TEAM, awayTeam);
    console.log(
      `[TeamStorage] Stored team names - Home: ${homeTeam}, Away: ${awayTeam}`
    );
  } catch (error) {
    console.warn('[TeamStorage] Failed to store team names:', error);
  }
};

/**
 * Store scores in localStorage
 */
export const storeScores = (homeScore: number, awayScore: number): void => {
  if (typeof window === 'undefined') return;

  try {
    localStorage.setItem(STORAGE_KEYS.HOME_SCORE, homeScore.toString());
    localStorage.setItem(STORAGE_KEYS.AWAY_SCORE, awayScore.toString());
    console.log(
      `[TeamStorage] Stored scores - Home: ${homeScore}, Away: ${awayScore}`
    );
  } catch (error) {
    console.warn('[TeamStorage] Failed to store scores:', error);
  }
};

/**
 * Store match ID in localStorage
 */
export const storeMatchId = (matchId: string): void => {
  if (typeof window === 'undefined') return;

  try {
    localStorage.setItem(STORAGE_KEYS.MATCH_ID, matchId);
    console.log(`[TeamStorage] Stored match ID: ${matchId}`);
  } catch (error) {
    console.warn('[TeamStorage] Failed to store match ID:', error);
  }
};

/**
 * Retrieve team names from localStorage
 */
export const getStoredTeamNames = (): {
  homeTeam: string | null;
  awayTeam: string | null;
} => {
  if (typeof window === 'undefined') {
    return { homeTeam: null, awayTeam: null };
  }

  try {
    const homeTeam = localStorage.getItem(STORAGE_KEYS.HOME_TEAM);
    const awayTeam = localStorage.getItem(STORAGE_KEYS.AWAY_TEAM);
    return { homeTeam, awayTeam };
  } catch (error) {
    console.warn('[TeamStorage] Failed to retrieve team names:', error);
    return { homeTeam: null, awayTeam: null };
  }
};

/**
 * Retrieve scores from localStorage
 */
export const getStoredScores = (): { homeScore: number; awayScore: number } => {
  if (typeof window === 'undefined') {
    return { homeScore: 0, awayScore: 0 };
  }

  try {
    const homeScore = localStorage.getItem(STORAGE_KEYS.HOME_SCORE);
    const awayScore = localStorage.getItem(STORAGE_KEYS.AWAY_SCORE);
    return {
      homeScore: homeScore ? parseInt(homeScore, 10) : 0,
      awayScore: awayScore ? parseInt(awayScore, 10) : 0,
    };
  } catch (error) {
    console.warn('[TeamStorage] Failed to retrieve scores:', error);
    return { homeScore: 0, awayScore: 0 };
  }
};

/**
 * Retrieve match ID from localStorage
 */
export const getStoredMatchId = (): string | null => {
  if (typeof window === 'undefined') {
    return null;
  }

  try {
    return localStorage.getItem(STORAGE_KEYS.MATCH_ID);
  } catch (error) {
    console.warn('[TeamStorage] Failed to retrieve match ID:', error);
    return null;
  }
};

/**
 * Store match time in localStorage
 */
export const storeMatchTime = (matchTime: number): void => {
  if (typeof window === 'undefined') return;

  try {
    localStorage.setItem(STORAGE_KEYS.MATCH_TIME, matchTime.toString());
  } catch (error) {
    console.warn('[TeamStorage] Failed to store match time:', error);
  }
};

/**
 * Retrieve match time from localStorage
 */
export const getStoredMatchTime = (): number => {
  if (typeof window === 'undefined') {
    return 0;
  }

  try {
    const matchTime = localStorage.getItem(STORAGE_KEYS.MATCH_TIME);
    return matchTime ? parseInt(matchTime, 10) : 0;
  } catch (error) {
    console.warn('[TeamStorage] Failed to retrieve match time:', error);
    return 0;
  }
};

/**
 * Clear team names from localStorage
 */
export const clearStoredTeamNames = (): void => {
  if (typeof window === 'undefined') return;

  try {
    localStorage.removeItem(STORAGE_KEYS.HOME_TEAM);
    localStorage.removeItem(STORAGE_KEYS.AWAY_TEAM);
    console.log('[TeamStorage] Cleared stored team names');
  } catch (error) {
    console.warn('[TeamStorage] Failed to clear team names:', error);
  }
};

/**
 * Clear scores from localStorage
 */
export const clearStoredScores = (): void => {
  if (typeof window === 'undefined') return;

  try {
    localStorage.removeItem(STORAGE_KEYS.HOME_SCORE);
    localStorage.removeItem(STORAGE_KEYS.AWAY_SCORE);
    console.log('[TeamStorage] Cleared stored scores');
  } catch (error) {
    console.warn('[TeamStorage] Failed to clear scores:', error);
  }
};

/**
 * Clear match time from localStorage
 */
export const clearStoredMatchTime = (): void => {
  if (typeof window === 'undefined') return;

  try {
    localStorage.removeItem(STORAGE_KEYS.MATCH_TIME);
    console.log('[TeamStorage] Cleared stored match time');
  } catch (error) {
    console.warn('[TeamStorage] Failed to clear match time:', error);
  }
};

/**
 * Clear match ID from localStorage
 */
export const clearStoredMatchId = (): void => {
  if (typeof window === 'undefined') return;

  try {
    localStorage.removeItem(STORAGE_KEYS.MATCH_ID);
    console.log('[TeamStorage] Cleared stored match ID');
  } catch (error) {
    console.warn('[TeamStorage] Failed to clear match ID:', error);
  }
};

/**
 * Clear all stored match data (teams, scores, match time, and match ID)
 */
export const clearAllMatchData = (): void => {
  clearStoredTeamNames();
  clearStoredScores();
  clearStoredMatchTime();
  clearStoredMatchId();
};

/**
 * Get team name with fallback logic
 */
export const getTeamNameWithFallback = (
  teamFromEvent: string | undefined,
  storageKey: string,
  defaultName: string
): string => {
  if (teamFromEvent) return teamFromEvent;

  if (typeof window !== 'undefined') {
    try {
      const storedTeam = localStorage.getItem(storageKey);
      if (storedTeam) return storedTeam;
    } catch (error) {
      console.warn(
        `[TeamStorage] Failed to read ${storageKey} from localStorage:`,
        error
      );
    }
  }

  return defaultName;
};
