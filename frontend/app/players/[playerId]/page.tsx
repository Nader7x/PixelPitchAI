'use client';
import { useState, useEffect } from 'react';
import { useRouter, useParams } from 'next/navigation';
import Link from 'next/link';
import Image from 'next/image';
import {
  User,
  Calendar,
  MapPin,
  TrendingUp,
  Trophy,
  Target,
  Users,
  ArrowLeft,
  Star,
  ClubIcon,
  Bell,
  Package,
  Home,
  LayoutDashboardIcon,
  Search,
  Settings,
  Activity,
  Award,
  Flag,
} from 'lucide-react';
import { SidebarLayout } from '../../Components/Sidebar/Sidebar';
import { SidebarItem } from '../../Components/Sidebar/SidebarItem';
import { SidebarSection } from '../../Components/Sidebar/SidebarSection';
import ProtectedRoute from '@/app/Components/ProtectedRoute/ProtectedRoute';
import playerService, { Player } from '@/Services/PlayerService';
import authService from '@/Services/AuthenticationService';
import teamService from '@/Services/TeamService';

interface PlayerStats {
  appearances: number;
  goals: number;
  assists: number;
  yellowCards: number;
  redCards: number;
  rating: number;
  minutesPlayed: number;
  saves?: number; // For goalkeepers
  cleanSheets?: number; // For goalkeepers
}

interface PlayerProfile extends Player {
  stats: PlayerStats;
  biography?: string;
  marketValue?: number;
  contract?: {
    startDate: string;
    endDate: string;
    salary: number;
  };
  achievements?: string[];
}

export default function PlayerDetailPage() {
  const [player, setPlayer] = useState<PlayerProfile | null>(null);
  const [playerteam, setPlayerTeam] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [isAdmin, setIsAdmin] = useState(false);
  const router = useRouter();
  const params = useParams();
  const playerId = params.playerId as string;

  useEffect(() => {
    if (!authService.isAuthenticated()) {
      router.push('/login');
      return;
    }

    setIsAdmin(authService.hasRole('Admin'));
    if (playerId) {
      fetchPlayerDetails();
    }
  }, [router, playerId]);

  const fetchPlayerDetails = async () => {
    try {
      setLoading(true);
      setError(null);

      // Fetch basic player data
      const playerData = await playerService.getPlayerById(parseInt(playerId));
      if (!playerData) {
        throw new Error('Player not found');
      }
      const teamData = await teamService.getTeamById(playerData.id);
      if (teamData) {
        setPlayerTeam(teamData.name);
      }

      // Mock additional data - replace with actual API calls
      const mockStats: PlayerStats = {
        appearances: Math.floor(Math.random() * 50) + 10,
        goals: Math.floor(Math.random() * 25),
        assists: Math.floor(Math.random() * 15),
        yellowCards: Math.floor(Math.random() * 8),
        redCards: Math.floor(Math.random() * 2),
        rating: Math.random() * 4 + 6, // 6-10 rating
        minutesPlayed: Math.floor(Math.random() * 3000) + 500,
        ...(playerData.position === 'Goalkeeper' && {
          saves: Math.floor(Math.random() * 100) + 20,
          cleanSheets: Math.floor(Math.random() * 15) + 5,
        }),
      };

      const playerProfile: PlayerProfile = {
        ...playerData,
        stats: mockStats,
        biography: `${playerData.knownName} is a talented ${playerData.position.toLowerCase()} known for exceptional skills and dedication to the sport.`,
        marketValue: Math.floor(Math.random() * 50000000) + 1000000,
        contract: {
          startDate: '2023-07-01',
          endDate: '2026-06-30',
          salary: Math.floor(Math.random() * 200000) + 50000,
        },
        achievements: [
          'Player of the Month (3x)',
          'Top Scorer Award',
          'Best Young Player',
        ],
      };

      setPlayer(playerProfile);
    } catch (err) {
      console.error('Error fetching player details:', err);
      setError('Failed to load player details. Please try again.');
    } finally {
      setLoading(false);
    }
  };

  if (loading) {
    return (
      <ProtectedRoute allowedRoles={['User', 'Admin', 'Coach', 'Player']}>
        <SidebarLayout sidebar={<PlayerDetailSidebar isAdmin={isAdmin} />}>
          <div className="relative flex h-screen items-center justify-center">
            <BackgroundElements />
            <div className="relative z-10 flex flex-col items-center space-y-6">
              <div className="h-16 w-16 animate-spin rounded-full border-4 border-green-500 border-t-transparent"></div>
              <h2 className="text-2xl font-bold text-gray-800">
                Loading Player...
              </h2>
            </div>
          </div>
        </SidebarLayout>
      </ProtectedRoute>
    );
  }

  if (error || !player) {
    return (
      <ProtectedRoute allowedRoles={['User', 'Admin', 'Coach', 'Player']}>
        <SidebarLayout sidebar={<PlayerDetailSidebar isAdmin={isAdmin} />}>
          <div className="relative flex h-screen items-center justify-center">
            <BackgroundElements />
            <div className="relative z-10 text-center">
              <div className="rounded-lg border-l-4 border-red-500 bg-white p-6 text-red-700 shadow-lg">
                <p className="mb-2 text-xl font-bold">Error</p>
                <p className="mb-4">{error || 'Player not found'}</p>
                <div className="space-x-4">
                  <button
                    onClick={fetchPlayerDetails}
                    className="rounded-lg bg-red-500 px-4 py-2 text-white shadow transition-colors hover:bg-red-600"
                  >
                    Try Again
                  </button>
                  <Link
                    href="/players"
                    className="rounded-lg bg-gray-500 px-4 py-2 text-white shadow transition-colors hover:bg-gray-600"
                  >
                    Back to Players
                  </Link>
                </div>
              </div>
            </div>
          </div>
        </SidebarLayout>
      </ProtectedRoute>
    );
  }

  return (
    <ProtectedRoute allowedRoles={['User', 'Admin', 'Coach', 'Player']}>
      <SidebarLayout sidebar={<PlayerDetailSidebar isAdmin={isAdmin} />}>
        <div className="relative min-h-screen">
          <BackgroundElements />

          <div className="relative z-10 p-6">
            {/* Back Button */}
            <div className="mb-6">
              <Link
                href="/players"
                className="inline-flex items-center space-x-2 text-gray-600 transition-colors hover:text-gray-900"
              >
                <ArrowLeft className="h-4 w-4" />
                <span>Back to Players</span>
              </Link>
            </div>

            {/* Player Header */}
            <div className="mb-8 rounded-xl bg-white/80 p-6 shadow-lg backdrop-blur-sm">
              <div className="flex flex-col items-start space-y-4 lg:flex-row lg:items-center lg:space-y-0 lg:space-x-6">
                <div className="relative">
                  <div className="h-32 w-32 rounded-full bg-gradient-to-br from-green-400 to-green-600 p-2">
                    <div className="flex h-full w-full items-center justify-center rounded-full bg-white">
                      {player.photoUrl ? (
                        <Image
                          src={player.photoUrl}
                          alt={`${player.knownName} photo`}
                          width={112}
                          height={112}
                          className="rounded-full object-cover"
                        />
                      ) : (
                        <User className="h-16 w-16 text-green-600" />
                      )}
                    </div>
                  </div>
                  <div className="absolute -right-2 -bottom-2 rounded-full bg-green-500 p-2">
                    <span className="text-sm font-bold text-white">
                      #{player.shirtNumber || 'N/A'}
                    </span>
                  </div>
                </div>

                <div className="flex-1">
                  <div className="flex items-start justify-between">
                    <div>
                      <h1 className="text-3xl font-bold text-gray-900">
                        {player.knownName}
                      </h1>
                      <div className="mt-2 flex items-center space-x-4">
                        <span className="inline-flex items-center rounded-full bg-green-100 px-3 py-1 text-sm font-medium text-green-800">
                          {player.position}
                        </span>
                        <div className="flex items-center space-x-1 text-gray-600">
                          <Flag className="h-4 w-4" />
                          <span>{player.nationality || 'Unknown'}</span>
                        </div>
                      </div>
                      <div className="mt-2 flex items-center space-x-2">
                        <MapPin className="h-4 w-4 text-gray-500" />
                        <span className="text-gray-600">
                          Team: {playerteam || 'Free Agent'}
                        </span>
                      </div>
                    </div>

                    <div className="text-right">
                      <div className="flex items-center space-x-1">
                        <Star className="h-5 w-5 fill-current text-yellow-500" />
                        <span className="text-2xl font-bold text-gray-900">
                          {player.stats.rating.toFixed(1)}
                        </span>
                      </div>
                      <p className="text-sm text-gray-600">Overall Rating</p>
                      {player.marketValue && (
                        <p className="mt-2 text-lg font-semibold text-green-600">
                          €{(player.marketValue / 1000000).toFixed(1)}M
                        </p>
                      )}
                    </div>
                  </div>
                </div>
              </div>
            </div>

            {/* Stats Overview */}
            <div className="mb-8 grid gap-6 md:grid-cols-2 lg:grid-cols-4">
              <div className="rounded-xl bg-white/80 p-6 shadow-lg backdrop-blur-sm">
                <div className="flex items-center justify-between">
                  <div>
                    <p className="text-2xl font-bold text-blue-600">
                      {player.stats.appearances}
                    </p>
                    <p className="text-sm text-gray-600">Appearances</p>
                  </div>
                  <Activity className="h-8 w-8 text-blue-600" />
                </div>
              </div>

              <div className="rounded-xl bg-white/80 p-6 shadow-lg backdrop-blur-sm">
                <div className="flex items-center justify-between">
                  <div>
                    <p className="text-2xl font-bold text-green-600">
                      {player.stats.goals}
                    </p>
                    <p className="text-sm text-gray-600">Goals</p>
                  </div>
                  <Target className="h-8 w-8 text-green-600" />
                </div>
              </div>

              <div className="rounded-xl bg-white/80 p-6 shadow-lg backdrop-blur-sm">
                <div className="flex items-center justify-between">
                  <div>
                    <p className="text-2xl font-bold text-purple-600">
                      {player.stats.assists}
                    </p>
                    <p className="text-sm text-gray-600">Assists</p>
                  </div>
                  <TrendingUp className="h-8 w-8 text-purple-600" />
                </div>
              </div>

              <div className="rounded-xl bg-white/80 p-6 shadow-lg backdrop-blur-sm">
                <div className="flex items-center justify-between">
                  <div>
                    <p className="text-2xl font-bold text-amber-600">
                      {Math.floor(player.stats.minutesPlayed / 90)}
                    </p>
                    <p className="text-sm text-gray-600">Games Played</p>
                  </div>
                  <Award className="h-8 w-8 text-amber-600" />
                </div>
              </div>
            </div>

            {/* Detailed Information */}
            <div className="grid gap-6 lg:grid-cols-2">
              {/* Performance Stats */}
              <div className="rounded-xl bg-white/80 p-6 shadow-lg backdrop-blur-sm">
                <h3 className="mb-4 text-xl font-bold text-gray-900">
                  Performance Statistics
                </h3>
                <div className="space-y-4">
                  <div className="flex items-center justify-between">
                    <span className="text-gray-600">Minutes Played</span>
                    <span className="font-semibold">
                      {player.stats.minutesPlayed.toLocaleString()}
                    </span>
                  </div>
                  <div className="flex items-center justify-between">
                    <span className="text-gray-600">Yellow Cards</span>
                    <span className="font-semibold text-yellow-600">
                      {player.stats.yellowCards}
                    </span>
                  </div>
                  <div className="flex items-center justify-between">
                    <span className="text-gray-600">Red Cards</span>
                    <span className="font-semibold text-red-600">
                      {player.stats.redCards}
                    </span>
                  </div>
                  {player.position === 'Goalkeeper' && (
                    <>
                      <div className="flex items-center justify-between">
                        <span className="text-gray-600">Saves</span>
                        <span className="font-semibold text-blue-600">
                          {player.stats.saves}
                        </span>
                      </div>
                      <div className="flex items-center justify-between">
                        <span className="text-gray-600">Clean Sheets</span>
                        <span className="font-semibold text-green-600">
                          {player.stats.cleanSheets}
                        </span>
                      </div>
                    </>
                  )}
                </div>
              </div>

              {/* Player Information */}
              <div className="rounded-xl bg-white/80 p-6 shadow-lg backdrop-blur-sm">
                <h3 className="mb-4 text-xl font-bold text-gray-900">
                  Player Information
                </h3>
                <div className="space-y-4">
                  <div>
                    <h4 className="mb-2 font-semibold text-gray-700">
                      Biography
                    </h4>
                    <p className="text-gray-600">{player.biography}</p>
                  </div>

                  {player.contract && (
                    <div>
                      <h4 className="mb-2 font-semibold text-gray-700">
                        Contract
                      </h4>
                      <div className="space-y-1 text-sm text-gray-600">
                        <p>
                          Contract Period:{' '}
                          {new Date(player.contract.startDate).getFullYear()} -{' '}
                          {new Date(player.contract.endDate).getFullYear()}
                        </p>
                        <p>
                          Annual Salary: €
                          {player.contract.salary.toLocaleString()}
                        </p>
                      </div>
                    </div>
                  )}

                  {player.achievements && player.achievements.length > 0 && (
                    <div>
                      <h4 className="mb-2 font-semibold text-gray-700">
                        Achievements
                      </h4>
                      <ul className="space-y-1 text-sm text-gray-600">
                        {player.achievements.map((achievement, index) => (
                          <li
                            key={index}
                            className="flex items-center space-x-2"
                          >
                            <Trophy className="h-3 w-3 text-amber-500" />
                            <span>{achievement}</span>
                          </li>
                        ))}
                      </ul>
                    </div>
                  )}
                </div>
              </div>
            </div>
          </div>
        </div>
      </SidebarLayout>
    </ProtectedRoute>
  );
}

function PlayerDetailSidebar({ isAdmin }: { isAdmin: boolean }) {
  return (
    <>
      {/* Main Navigation */}
      <Link href="/dashboard">
        <SidebarItem
          icon={<LayoutDashboardIcon size={20} />}
          text="Dashboard"
        />
      </Link>{' '}
      <Link href="/teams">
        <SidebarItem icon={<ClubIcon size={20} />} text="Teams" />
      </Link>
      <Link href="/players">
        <SidebarItem icon={<User size={20} />} text="Players" active />
      </Link>
      <Link href="/coaches">
        <SidebarItem icon={<Users size={20} />} text="Coaches" />
      </Link>
      <Link href="/stadiums">
        <SidebarItem icon={<Home size={20} />} text="Stadiums" />
      </Link>
      {/* Admin Section */}
      {isAdmin && (
        <>
          <SidebarSection title="Admin" color="text-amber-600" />
          <Link href="/admin">
            <SidebarItem icon={<Settings size={20} />} text="Admin Dashboard" />
          </Link>
        </>
      )}{' '}
      {/* Notifications */}
      <Link href="/notifications">
        <SidebarItem icon={<Bell size={20} />} text="Notifications" />
      </Link>
      {/* Search & Settings */}
      <SidebarSection title="Other" />
      <Link href="/search">
        <SidebarItem icon={<Search size={20} />} text="Search" />
      </Link>
      <Link href="/settings">
        <SidebarItem icon={<Settings size={20} />} text="Settings" />
      </Link>
    </>
  );
}

function BackgroundElements() {
  return (
    <div className="fixed inset-0 z-0">
      {/* Base Gradient */}
      <div className="absolute inset-0 bg-gradient-to-br from-emerald-50 via-green-50 to-teal-50"></div>

      {/* Stadium Silhouette Background */}
      <div className="absolute inset-0 opacity-[0.03]">
        <Image
          src="/images/Stadium dark.png"
          alt="Stadium Background"
          fill
          className="object-cover object-center"
        />
      </div>

      {/* Animated Floating Elements */}
      <div className="absolute inset-0 overflow-hidden">
        <div className="animate-float absolute top-20 left-20 h-2 w-2 rounded-full bg-green-400/20"></div>
        <div className="animate-float-delayed absolute top-40 right-32 h-3 w-3 rounded-full bg-emerald-400/15"></div>
        <div className="animate-float-slow absolute bottom-32 left-40 h-1 w-1 rounded-full bg-teal-400/25"></div>
        <div className="animate-float-delayed absolute right-20 bottom-20 h-2 w-2 rounded-full bg-green-300/20"></div>
      </div>
    </div>
  );
}
