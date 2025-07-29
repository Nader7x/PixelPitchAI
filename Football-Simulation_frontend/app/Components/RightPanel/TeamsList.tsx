'use client';

import { ChevronRight } from 'lucide-react';
import Link from 'next/link';
import { Team } from '@/Services/TeamService';

interface TeamsListProps {
  teams: Team[];
  onSelect?: (team: Team) => void;
}

export default function TeamsList({ teams, onSelect }: TeamsListProps) {
  // If teams is empty, show placeholder
  if (!teams || teams.length === 0) {
    return (
      <div className="space-y-2 p-4">
        <div className="p-4 text-center text-gray-500">No teams available</div>
      </div>
    );
  }

  return (
    <div className="space-y-2 p-4">
      {teams.map((team) => (
        <Link
          href={`/teams/${team.id}`}
          key={team.id}
          onClick={(e) => {
            if (onSelect) {
              e.preventDefault();
              onSelect(team);
            }
          }}
          className="flex cursor-pointer items-center justify-between rounded-lg bg-white px-3 py-2 transition-colors hover:bg-gray-100"
        >
          <div className="flex items-center space-x-3">
            {/* Team Logo or Initial */}
            <div
              className="flex h-8 w-8 items-center justify-center overflow-hidden rounded-full"
              style={{
                backgroundColor: team.primaryColor
                  ? `${team.primaryColor.toLowerCase()}`
                  : '#4CAF50',
                color: team.secondaryColor
                  ? `${team.secondaryColor.toLowerCase()}`
                  : 'white',
              }}
            >
              {team.logo ? (
                <img
                  src={
                    team.shortName === 'BAR'
                      ? '/logos/barcelona.png'
                      : team.shortName === 'RMA'
                        ? '/logos/real madrid.png'
                        : `/logos/${team.name.toLowerCase().replace(/\s+/g, '-')}.png`
                  }
                  alt={team.name}
                  className="h-8 w-8 object-cover"
                />
              ) : (
                <span className="text-sm font-bold">
                  {team.shortName || team.name.charAt(0)}
                </span>
              )}
            </div>

            {/* Team Info */}
            <div className="flex flex-col">
              <span className="text-sm font-medium">{team.name}</span>
              <span className="text-xs text-gray-500">{team.league}</span>
            </div>
          </div>
          <ChevronRight className="text-gray-400" size={18} />
        </Link>
      ))}
    </div>
  );
}
