import React from 'react';
import Image from 'next/image';
import { LogoBackground } from '@/app/Components/Logo3d/logo3d';

interface MatchStatsProps {
  teamA: {
    name: string;
    stats: (string | number)[];
    color: string;
    logoUrl: string;
  };
  teamB: {
    name: string;
    stats: (string | number)[];
    color: string;
    logoUrl: string;
  };
  labels: string[];
}

export const MatchStats: React.FC<MatchStatsProps> = ({
  teamA,
  teamB,
  labels,
}) => {
  // Football stat icons mapping
  const getStatIcon = (label: string): string => {
    const iconMap: { [key: string]: string } = {
      Shots: '⚽',
      'Shots on target': '🎯',
      Possession: '🏃‍♂️',
      Passes: '🦵',
      'Pass accuracy': '✅',
      Fouls: '🟨',
      'Yellow cards': '🟨',
      'Red cards': '🟥',
      Offsides: '🚫',
      Corners: '📐',
    };
    return iconMap[label] || '⚽';
  };

  // Determine which team performed better for each stat
  const getStatComparison = (
    statLabel: string,
    teamAValue: string | number,
    teamBValue: string | number
  ) => {
    const getNumericValue = (val: string | number): number => {
      if (typeof val === 'string') {
        const numeric = parseFloat(val.replace(/[^0-9.]/g, ''));
        return isNaN(numeric) ? 0 : numeric;
      }
      return val;
    };

    const aNum = getNumericValue(teamAValue);
    const bNum = getNumericValue(teamBValue);

    // Rules for determining which is better
    const betterWhenHigher = [
      'Shots',
      'Shots on target',
      'Possession',
      'Passes',
      'Pass accuracy',
      'Corners',
    ];
    const betterWhenLower = ['Fouls', 'Yellow cards', 'Red cards', 'Offsides'];

    let teamABetter = false;
    let teamBBetter = false;

    if (betterWhenHigher.includes(statLabel)) {
      teamABetter = aNum > bNum;
      teamBBetter = bNum > aNum;
    } else if (betterWhenLower.includes(statLabel)) {
      teamABetter = aNum < bNum;
      teamBBetter = bNum < aNum;
    }

    return { teamABetter, teamBBetter };
  };

  return (
    <div className="mx-auto w-full">
      {/* Enhanced Stats Container */}
      <div className="relative rounded-2xl border border-white/20 bg-white/10 p-8 shadow-2xl backdrop-blur-sm">
        {/* Decorative background elements */}
        <div className="absolute inset-0 rounded-2xl bg-gradient-to-br from-white/5 via-transparent to-white/5"></div>
        <div className="absolute top-4 right-4 h-2 w-2 animate-pulse rounded-full bg-white/30"></div>
        <div className="absolute bottom-4 left-4 h-1.5 w-1.5 animate-pulse rounded-full bg-white/20 delay-300"></div>

        {/* Team Headers */}
        <div className="relative z-10 mb-8 grid grid-cols-3 items-center gap-6">
          {/* Team A */}
          <div className="flex items-center gap-4">
            <div className="group relative">
              <div className="absolute inset-0 animate-pulse rounded-full bg-gradient-to-br from-blue-400/30 to-purple-400/30 blur"></div>
              <Image
                src={teamA.logoUrl}
                alt={teamA.name}
                width={56}
                height={56}
                className="relative rounded-full border-2 border-white/30 shadow-xl transition-transform duration-300 group-hover:scale-110"
              />
            </div>
            <div>
              <h3 className="text-lg font-bold text-white">{teamA.name}</h3>
              <div className="h-1 w-16 rounded-full bg-gradient-to-r from-blue-500 to-purple-500"></div>
            </div>
          </div>

          {/* VS Separator */}
          <div className="text-center">
            <div className="relative">
              <div className="absolute inset-0 rounded-full bg-gradient-to-r from-blue-400/20 via-purple-400/20 to-pink-400/20 blur-lg"></div>
              <div className="relative rounded-full border border-white/30 bg-white/20 px-4 py-2 backdrop-blur-sm">
                <span className="text-lg font-bold text-white">VS</span>
              </div>
            </div>
          </div>

          {/* Team B */}
          <div className="flex items-center justify-end gap-4">
            <div className="text-right">
              <h3 className="text-lg font-bold text-white">{teamB.name}</h3>
              <div className="ml-auto h-1 w-16 rounded-full bg-gradient-to-r from-red-500 to-pink-500"></div>
            </div>
            <div className="group relative">
              <div className="absolute inset-0 animate-pulse rounded-full bg-gradient-to-br from-red-400/30 to-pink-400/30 blur"></div>
              <Image
                src={teamB.logoUrl}
                alt={teamB.name}
                width={56}
                height={56}
                className="relative rounded-full border-2 border-white/30 shadow-xl transition-transform duration-300 group-hover:scale-110"
              />
            </div>
          </div>
        </div>

        {/* Enhanced Stats Grid */}
        <div className="relative z-10 space-y-4">
          {labels.map((label, index) => {
            const teamAValue = teamA.stats[index];
            const teamBValue = teamB.stats[index];

            // Calculate percentages for progress bars
            const getNumericValue = (val: string | number): number => {
              if (typeof val === 'string') {
                const numeric = parseFloat(val.replace(/[^0-9.]/g, ''));
                return isNaN(numeric) ? 0 : numeric;
              }
              return val;
            };

            const aNum = getNumericValue(teamAValue);
            const bNum = getNumericValue(teamBValue);
            const total = aNum + bNum || 1;
            const aPercent = (aNum / total) * 100;
            const bPercent = (bNum / total) * 100;

            // Get stat comparison
            const { teamABetter, teamBBetter } = getStatComparison(
              label,
              teamAValue,
              teamBValue
            );

            // Determine colors based on performance
            const getStatColor = (isTeamA: boolean, isBetter: boolean) => {
              if (isBetter) {
                return isTeamA
                  ? 'linear-gradient(135deg, #10b981, #059669)'
                  : 'linear-gradient(135deg, #10b981, #059669)'; // Green for better
              } else if (
                (isTeamA && teamBBetter) ||
                (!isTeamA && teamABetter)
              ) {
                return isTeamA
                  ? 'linear-gradient(135deg, #ef4444, #dc2626)'
                  : 'linear-gradient(135deg, #ef4444, #dc2626)'; // Red for worse
              } else {
                return isTeamA
                  ? `linear-gradient(135deg, ${teamA.color}dd, ${teamA.color})`
                  : `linear-gradient(135deg, ${teamB.color}dd, ${teamB.color})`;
              }
            };

            const getProgressBarColor = (
              isTeamA: boolean,
              isBetter: boolean
            ) => {
              if (isBetter) {
                return 'linear-gradient(90deg, #10b981, #059669)'; // Green for better
              } else if (
                (isTeamA && teamBBetter) ||
                (!isTeamA && teamABetter)
              ) {
                return 'linear-gradient(90deg, #ef4444, #dc2626)'; // Red for worse
              } else {
                return isTeamA
                  ? `linear-gradient(90deg, ${teamA.color}, ${teamA.color}cc)`
                  : `linear-gradient(90deg, ${teamB.color}cc, ${teamB.color})`;
              }
            };

            return (
              <div
                key={label}
                className="group rounded-xl border border-white/10 bg-white/5 p-4 backdrop-blur-sm transition-all duration-300 hover:border-white/20 hover:bg-white/10"
              >
                <div className="grid grid-cols-5 items-center gap-4">
                  {/* Team A Stat */}
                  <div className="text-center">
                    <div
                      className="inline-block rounded-lg px-3 py-2 text-sm font-bold text-white shadow-lg transition-all duration-300 hover:scale-105"
                      style={{
                        background: getStatColor(true, teamABetter),
                        boxShadow: teamABetter
                          ? '0 4px 15px rgba(16, 185, 129, 0.4)'
                          : teamBBetter
                            ? '0 4px 15px rgba(239, 68, 68, 0.4)'
                            : `0 4px 15px ${teamA.color}40`,
                      }}
                    >
                      {teamABetter && '🏆'} {teamAValue}
                    </div>
                  </div>

                  {/* Progress Bar */}
                  <div className="col-span-1">
                    <div className="h-3 w-full overflow-hidden rounded-full bg-white/20">
                      <div
                        className="h-full rounded-full transition-all duration-1000 ease-out"
                        style={{
                          width: `${aPercent}%`,
                          background: getProgressBarColor(true, teamABetter),
                          boxShadow: teamABetter
                            ? '0 0 10px rgba(16, 185, 129, 0.5)'
                            : teamBBetter
                              ? '0 0 10px rgba(239, 68, 68, 0.5)'
                              : 'none',
                        }}
                      />
                    </div>
                  </div>

                  {/* Label with Icon */}
                  <div className="text-center">
                    <div className="flex flex-col items-center gap-1">
                      <span className="text-xl">{getStatIcon(label)}</span>
                      <span className="text-sm font-medium text-white/90 transition-colors duration-300 group-hover:text-white">
                        {label}
                      </span>
                    </div>
                  </div>

                  {/* Progress Bar */}
                  <div className="col-span-1">
                    <div className="h-3 w-full overflow-hidden rounded-full bg-white/20">
                      <div
                        className="ml-auto h-full rounded-full transition-all duration-1000 ease-out"
                        style={{
                          width: `${bPercent}%`,
                          background: getProgressBarColor(false, teamBBetter),
                          boxShadow: teamBBetter
                            ? '0 0 10px rgba(16, 185, 129, 0.5)'
                            : teamABetter
                              ? '0 0 10px rgba(239, 68, 68, 0.5)'
                              : 'none',
                        }}
                      />
                    </div>
                  </div>

                  {/* Team B Stat */}
                  <div className="text-center">
                    <div
                      className="inline-block rounded-lg px-3 py-2 text-sm font-bold text-white shadow-lg transition-all duration-300 hover:scale-105"
                      style={{
                        background: getStatColor(false, teamBBetter),
                        boxShadow: teamBBetter
                          ? '0 4px 15px rgba(16, 185, 129, 0.4)'
                          : teamABetter
                            ? '0 4px 15px rgba(239, 68, 68, 0.4)'
                            : `0 4px 15px ${teamB.color}40`,
                      }}
                    >
                      {teamBBetter && '🏆'} {teamBValue}
                    </div>
                  </div>
                </div>
              </div>
            );
          })}
        </div>

        {/* Enhanced Stats Summary */}
        <div className="mt-8 border-t border-white/20 pt-6">
          <div className="mb-4 text-center">
            <h4 className="text-lg font-semibold text-white">
              📊 Performance Overview
            </h4>
            <p className="text-sm text-white/70">
              🏆 Green indicates better performance • 🔴 Red indicates areas for
              improvement
            </p>
          </div>
          <div className="grid grid-cols-3 gap-4 text-center">
            <div className="rounded-lg border border-white/10 bg-gradient-to-br from-emerald-500/20 to-green-500/20 p-3 backdrop-blur-sm">
              <div className="text-xs text-white/70">🏆 Stats Categories</div>
              <div className="text-lg font-bold text-emerald-400">
                {labels.length}
              </div>
            </div>
            <div className="rounded-lg border border-white/10 bg-gradient-to-br from-blue-500/20 to-purple-500/20 p-3 backdrop-blur-sm">
              <div className="text-xs text-white/70">⚡ Match Status</div>
              <div className="text-sm font-semibold text-blue-400">
                Live Analysis
              </div>
            </div>
            <div className="rounded-lg border border-white/10 bg-gradient-to-br from-purple-500/20 to-pink-500/20 p-3 backdrop-blur-sm">
              <div className="text-xs text-white/70">📈 Data Quality</div>
              <div className="text-sm font-semibold text-purple-400">
                Real-time
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};
