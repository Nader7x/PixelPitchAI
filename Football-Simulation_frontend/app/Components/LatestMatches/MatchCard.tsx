'use client';

import Link from 'next/link';
import { useSettings } from '../../contexts/EnhancedSettingsContext';

type MatchCardProps = {
  teamA: string;
  teamB: string;
  scoreA: number;
  scoreB: number;
  status?: string;
  HomeImage?: string;
  AwayImage?: string;
  date?: Date;
  matchId?: number;
};

export default function MatchCard({
  teamA,
  teamB,
  scoreA,
  scoreB,
  status,
  date,
  matchId,
  HomeImage = '', // Default to empty string if no image URL is provided
  AwayImage = '', // Default to empty string if no image URL is provided
}: MatchCardProps) {
  // Get dark mode from settings
  const { isDarkMode } = useSettings();
  // Format date if available
  const formattedDate = date
    ? new Intl.DateTimeFormat('en-US', {
        month: 'short',
        day: 'numeric',
        hour: '2-digit',
        minute: '2-digit',
      }).format(date)
    : '';

  // Determine status badge style
  const getStatusBadge = () => {
    switch (status?.toLowerCase()) {
      case 'live':
        return (
          <span className="badge badge-sm badge-error animate-pulse">LIVE</span>
        );
      case 'scheduled':
        return <span className="badge badge-sm badge-info">UPCOMING</span>;
      case 'cancelled':
        return <span className="badge badge-sm badge-warning">CANCELLED</span>;
      case 'completed':
      default:
        return <span className="badge badge-sm badge-success">FINAL</span>;
    }
  };

  return (
    <Link
      href={matchId ? `/matchdetails?matchId=${matchId}` : '/matchdetails'}
      className="block"
    >
      <div
        className={`list rounded-box shadow-md transition-all hover:shadow-lg ${
          isDarkMode
            ? 'bg-gray-800 hover:bg-gray-700'
            : 'bg-base-100 hover:bg-white'
        }`}
      >
        <div className="list-row items-center justify-between gap-4 p-4">
          {/* Team A */}
          <div className="flex items-center gap-2 px-8">
            <img
              className="rounded-box size-10"
              src={HomeImage || '/logos/barcelona.png'}
              alt={teamA}
            />
            <div>
              <div
                className={`font-semibold ${
                  isDarkMode ? 'text-gray-200' : 'text-gray-900'
                }`}
              >
                {teamA}
              </div>
              <div
                className={`text-xl font-bold ${
                  isDarkMode ? 'text-white' : 'text-gray-900'
                }`}
              >
                {scoreA}
              </div>
            </div>
          </div>

          {/* Center - Status and Time */}
          <div className="flex flex-col items-center">
            {getStatusBadge()}
            <div
              className={`mt-1 text-center text-lg font-bold ${
                isDarkMode ? 'text-gray-300' : 'text-gray-900'
              }`}
            >
              VS
            </div>
            {formattedDate && (
              <div
                className={`mt-1 text-xs ${
                  isDarkMode ? 'text-gray-400' : 'text-gray-500'
                }`}
              >
                {formattedDate}
              </div>
            )}
          </div>

          {/* Team B */}
          <div className="flex items-center gap-2 px-10">
            <div className="text-right">
              <div
                className={`font-semibold ${
                  isDarkMode ? 'text-gray-200' : 'text-gray-900'
                }`}
              >
                {teamB}
              </div>
              <div
                className={`text-xl font-bold ${
                  isDarkMode ? 'text-white' : 'text-gray-900'
                }`}
              >
                {scoreB}
              </div>
            </div>
            <img
              className="rounded-box size-10"
              src={AwayImage || '/logos/real madrid.png'}
              alt={teamB}
            />
          </div>
        </div>
      </div>
    </Link>
  );
}
