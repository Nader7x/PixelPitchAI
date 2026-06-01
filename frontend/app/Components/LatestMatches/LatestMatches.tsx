'use client';

import { useEffect, useState } from 'react';
import { useSettings } from '../../contexts/EnhancedSettingsContext';
import MatchCard from './MatchCard';
import matchService, { LatestMatch, Match } from '@/Services/MatchService';

interface StorageUser {
  userId: string;
  roles: [string];
}

export default function LatestMatches() {
  const [matches, setMatches] = useState<LatestMatch[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [isMounted, setIsMounted] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Get dark mode from settings
  const { isDarkMode } = useSettings();

  useEffect(() => {
    setIsMounted(true);
    fetchLatestMatches().then();
  }, []);

  const fetchLatestMatches = async () => {
    setIsLoading(true);
    setError(null);

    try {
      const userData = localStorage.getItem('user');
      const user: StorageUser = userData
        ? JSON.parse(userData)
        : { userId: '', roles: [''] };

      console.log('User data for latest matches:', user);
      console.log('Calling API with userId:', user.userId);

      const latestMatches = await matchService.getLatestMatches(user.userId);
      setMatches(latestMatches);
      console.log('Latest matches fetched:', latestMatches);
      console.log('Number of matches:', latestMatches.length);
    } catch (error) {
      console.error('Error fetching latest matches:', error);
      console.error('Error details:', error);
      setError('Failed to load match data. Please try again later.');
    } finally {
      setIsLoading(false);
    }
  };

  if (isLoading) {
    return (
      <div className="flex justify-center py-8">
        <div className="h-8 w-8 animate-spin rounded-full border-t-2 border-b-2 border-[#4CAF50]"></div>
      </div>
    );
  }

  if (error) {
    return (
      <div
        className={`rounded border-l-4 border-red-500 p-4 ${
          isMounted && isDarkMode
            ? 'bg-red-900/20 text-red-300'
            : 'bg-red-50 text-red-700'
        }`}
        suppressHydrationWarning
      >
        <p className="font-medium">Unable to load matches</p>
        <p className="text-sm">{error}</p>
        <button
          onClick={fetchLatestMatches}
          className="mt-2 rounded bg-red-100 px-3 py-1 text-sm text-red-700 transition-colors hover:bg-red-200"
        >
          Try Again
        </button>
      </div>
    );
  }

  if (matches.length === 0) {
    return (
      <div className="rounded border-l-4 border-blue-500 bg-blue-50 p-4 text-blue-700">
        <p className="font-medium">No matches found</p>
        <p className="text-sm">There are currently no matches to display.</p>
      </div>
    );
  }

  return (
    <div className="flex flex-col gap-4">
      {matches
        .filter((match: LatestMatch) => match.isLive === false)
        .map((match: LatestMatch) => (
          <MatchCard
            key={match.id}
            teamA={match.homeTeamName || 'Unknown Team'}
            teamB={match.awayTeamName || 'Unknown Team'}
            scoreA={match.homeTeamScore || 0}
            scoreB={match.awayTeamScore || 0}
            status={match.matchStatus || 'Scheduled'}
            HomeImage={match.homeTeamLogo || ''}
            AwayImage={match.awayTeamLogo || ''}
            date={
              match.scheduledDateTimeUtc
                ? new Date(match.scheduledDateTimeUtc)
                : new Date()
            }
            matchId={match.id}
          />
        ))}
    </div>
  );
}
