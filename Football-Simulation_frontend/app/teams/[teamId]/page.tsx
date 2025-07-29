'use client';
import { useState, useEffect } from 'react';
import { useRouter, useParams } from 'next/navigation';
import Link from 'next/link';
import Image from 'next/image';
import {
  ArrowLeft,
  Users,
  Trophy,
  Calendar,
  MapPin,
  Star,
  TrendingUp,
  Settings,
  ClubIcon,
  Bell,
  User,
  Package,
  Home,
  LayoutDashboardIcon,
  Edit,
  Mail,
  Phone,
  Globe,
  Award,
  Search,
} from 'lucide-react';
import { SidebarLayout } from '../../Components/Sidebar/Sidebar';
import { SidebarItem } from '../../Components/Sidebar/SidebarItem';
import { SidebarSection } from '../../Components/Sidebar/SidebarSection';
import ProtectedRoute from '@/app/Components/ProtectedRoute/ProtectedRoute';
import teamService, { Team } from '@/Services/TeamService';
import playerService, { Player } from '@/Services/PlayerService';
import authService from '@/Services/AuthenticationService';

interface TeamStats {
  wins: number;
  losses: number;
  draws: number;
  goalsFor: number;
  goalsAgainst: number;
  points: number;
  position: number;
  matchesPlayed: number;
}

export default function TeamDetailPage() {
  const [team, setTeam] = useState<Team | null>(null);
  const [players, setPlayers] = useState<Player[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [isAdmin, setIsAdmin] = useState(false);
  const router = useRouter();
  const params = useParams();
  const teamId = params.teamId as string;

  useEffect(() => {
    if (!authService.isAuthenticated()) {
      router.push('/login');
      return;
    }

    setIsAdmin(authService.hasRole('Admin'));

    if (teamId) {
      fetchTeamDetails();
    }
  }, [router, teamId]);

  const fetchTeamDetails = async () => {
    try {
      setLoading(true);
      setError(null);

      const [teamData, playersData] = await Promise.all([
        teamService.getTeamById(parseInt(teamId)),
        playerService.getPlayers({ teamId: parseInt(teamId) }),
      ]);

      setTeam(teamData);
      setPlayers(playersData);
    } catch (err) {
      console.error('Error fetching team details:', err);
      setError('Failed to load team details. Please try again.');
    } finally {
      setLoading(false);
    }
  };

  const getTeamStats = (): TeamStats => {
    // Mock stats - replace with actual data from your API
    return {
      wins: 15,
      losses: 3,
      draws: 6,
      goalsFor: 42,
      goalsAgainst: 18,
      points: 51,
      position: 3,
      matchesPlayed: 24,
    };
  };

  if (loading) {
    return (
      <ProtectedRoute allowedRoles={['User', 'Admin', 'Coach', 'Player']}>
        <SidebarLayout sidebar={<TeamDetailSidebar isAdmin={isAdmin} />}>
          <div className="relative flex h-screen items-center justify-center">
            <BackgroundElements />
            <div className="relative z-10 flex flex-col items-center space-y-6">
              <div className="h-16 w-16 animate-spin rounded-full border-4 border-green-500 border-t-transparent"></div>
              <h2 className="text-2xl font-bold text-gray-800">
                Loading Team Details...
              </h2>
            </div>
          </div>
        </SidebarLayout>
      </ProtectedRoute>
    );
  }

  if (error || !team) {
    return (
      <ProtectedRoute allowedRoles={['User', 'Admin', 'Coach', 'Player']}>
        <SidebarLayout sidebar={<TeamDetailSidebar isAdmin={isAdmin} />}>
          <div className="relative flex h-screen items-center justify-center">
            <BackgroundElements />
            <div className="relative z-10 text-center">
              <div className="rounded-lg border-l-4 border-red-500 bg-white p-6 text-red-700 shadow-lg">
                <p className="mb-2 text-xl font-bold">Error</p>
                <p className="mb-4">{error || 'Team not found'}</p>
                <div className="space-x-4">
                  <button
                    onClick={fetchTeamDetails}
                    className="rounded-lg bg-red-500 px-4 py-2 text-white shadow transition-colors hover:bg-red-600"
                  >
                    Try Again
                  </button>
                  <Link
                    href="/teams"
                    className="rounded-lg bg-gray-500 px-4 py-2 text-white shadow transition-colors hover:bg-gray-600"
                  >
                    Back to Teams
                  </Link>
                </div>
              </div>
            </div>
          </div>
        </SidebarLayout>
      </ProtectedRoute>
    );
  }

  const stats = getTeamStats();

  return (
    <ProtectedRoute allowedRoles={['User', 'Admin', 'Coach', 'Player']}>
      <SidebarLayout sidebar={<TeamDetailSidebar isAdmin={isAdmin} />}>
        <div className="relative min-h-screen">
          <BackgroundElements />

          <div className="relative z-10 p-6">
            {/* Header Section */}
            <div className="mb-8">
              <div className="mb-6 flex items-center space-x-4">
                <Link
                  href="/teams"
                  className="flex items-center space-x-2 text-gray-600 transition-colors hover:text-gray-900"
                >
                  <ArrowLeft className="h-5 w-5" />
                  <span>Back to Teams</span>
                </Link>
              </div>

              <div className="rounded-xl bg-white/80 p-8 shadow-lg backdrop-blur-sm">
                <div className="flex flex-col items-start space-y-6 lg:flex-row lg:items-center lg:space-y-0 lg:space-x-8">
                  {/* Team Logo */}
                  <div className="relative h-32 w-32 rounded-full bg-gradient-to-br from-green-400 to-green-600 p-2">
                    <div className="flex h-full w-full items-center justify-center rounded-full bg-white">
                      {team.logo ? (
                        <Image
                          src={team.logo}
                          alt={`${team.name} logo`}
                          width={100}
                          height={100}
                          className="rounded-full object-cover"
                        />
                      ) : (
                        <ClubIcon className="h-16 w-16 text-green-600" />
                      )}
                    </div>
                  </div>

                  {/* Team Info */}
                  <div className="flex-1">
                    <div className="flex items-start justify-between">
                      <div>
                        <h1 className="mb-2 text-4xl font-bold text-gray-900">
                          {team.name}
                        </h1>
                        <div className="mb-4 flex flex-wrap items-center gap-4 text-gray-600">
                          <div className="flex items-center space-x-2">
                            <MapPin className="h-5 w-5" />
                            <span>{team.city || 'City not specified'}</span>
                          </div>
                          {team.league && (
                            <div className="flex items-center space-x-2">
                              <Trophy className="h-5 w-5" />
                              <span>{team.league}</span>
                            </div>
                          )}
                          <div className="flex items-center space-x-2">
                            <Users className="h-5 w-5" />
                            <span>{players.length} Players</span>
                          </div>
                        </div>
                      </div>
                      {isAdmin && (
                        <button className="flex items-center space-x-2 rounded-lg bg-green-600 px-4 py-2 text-white transition-colors hover:bg-green-700">
                          <Edit className="h-4 w-4" />
                          <span>Edit Team</span>
                        </button>
                      )}
                    </div>
                  </div>
                </div>
              </div>
            </div>

            {/* Stats Section */}
            <div className="mb-8 grid gap-6 md:grid-cols-2 lg:grid-cols-4">
              <div className="rounded-xl bg-white/80 p-6 shadow-lg backdrop-blur-sm">
                <div className="flex items-center justify-between">
                  <div>
                    <p className="text-3xl font-bold text-green-600">
                      #{stats.position}
                    </p>
                    <p className="text-gray-600">League Position</p>
                  </div>
                  <Award className="h-8 w-8 text-green-600" />
                </div>
              </div>

              <div className="rounded-xl bg-white/80 p-6 shadow-lg backdrop-blur-sm">
                <div className="flex items-center justify-between">
                  <div>
                    <p className="text-3xl font-bold text-blue-600">
                      {stats.points}
                    </p>
                    <p className="text-gray-600">Points</p>
                  </div>
                  <Star className="h-8 w-8 text-blue-600" />
                </div>
              </div>

              <div className="rounded-xl bg-white/80 p-6 shadow-lg backdrop-blur-sm">
                <div className="flex items-center justify-between">
                  <div>
                    <p className="text-3xl font-bold text-purple-600">
                      {stats.wins}
                    </p>
                    <p className="text-gray-600">Wins</p>
                  </div>
                  <Trophy className="h-8 w-8 text-purple-600" />
                </div>
              </div>

              <div className="rounded-xl bg-white/80 p-6 shadow-lg backdrop-blur-sm">
                <div className="flex items-center justify-between">
                  <div>
                    <p className="text-3xl font-bold text-orange-600">
                      {stats.goalsFor}
                    </p>
                    <p className="text-gray-600">Goals Scored</p>
                  </div>
                  <TrendingUp className="h-8 w-8 text-orange-600" />
                </div>
              </div>
            </div>

            {/* Detailed Stats */}
            <div className="mb-8 grid gap-6 lg:grid-cols-2">
              <div className="rounded-xl bg-white/80 p-6 shadow-lg backdrop-blur-sm">
                <h2 className="mb-4 text-xl font-bold text-gray-900">
                  Season Statistics
                </h2>
                <div className="space-y-4">
                  <div className="flex items-center justify-between">
                    <span className="text-gray-600">Matches Played</span>
                    <span className="font-bold text-gray-900">
                      {stats.matchesPlayed}
                    </span>
                  </div>
                  <div className="flex items-center justify-between">
                    <span className="text-gray-600">Wins</span>
                    <span className="font-bold text-green-600">
                      {stats.wins}
                    </span>
                  </div>
                  <div className="flex items-center justify-between">
                    <span className="text-gray-600">Draws</span>
                    <span className="font-bold text-yellow-600">
                      {stats.draws}
                    </span>
                  </div>
                  <div className="flex items-center justify-between">
                    <span className="text-gray-600">Losses</span>
                    <span className="font-bold text-red-600">
                      {stats.losses}
                    </span>
                  </div>
                  <div className="flex items-center justify-between">
                    <span className="text-gray-600">Goals For</span>
                    <span className="font-bold text-blue-600">
                      {stats.goalsFor}
                    </span>
                  </div>
                  <div className="flex items-center justify-between">
                    <span className="text-gray-600">Goals Against</span>
                    <span className="font-bold text-red-600">
                      {stats.goalsAgainst}
                    </span>
                  </div>
                  <div className="flex items-center justify-between">
                    <span className="text-gray-600">Goal Difference</span>
                    <span
                      className={`font-bold ${stats.goalsFor - stats.goalsAgainst >= 0 ? 'text-green-600' : 'text-red-600'}`}
                    >
                      {stats.goalsFor - stats.goalsAgainst > 0 ? '+' : ''}
                      {stats.goalsFor - stats.goalsAgainst}
                    </span>
                  </div>
                </div>
              </div>

              <div className="rounded-xl bg-white/80 p-6 shadow-lg backdrop-blur-sm">
                <h2 className="mb-4 text-xl font-bold text-gray-900">
                  Team Information
                </h2>
                <div className="space-y-4">
                  <div className="flex items-center space-x-3">
                    <MapPin className="h-5 w-5 text-gray-400" />
                    <span className="text-gray-700">
                      {team.city || 'City not specified'}
                    </span>
                  </div>
                  {team.league && (
                    <div className="flex items-center space-x-3">
                      <Trophy className="h-5 w-5 text-gray-400" />
                      <span className="text-gray-700">{team.league}</span>
                    </div>
                  )}
                  <div className="flex items-center space-x-3">
                    <Users className="h-5 w-5 text-gray-400" />
                    <span className="text-gray-700">
                      {players.length} Players
                    </span>
                  </div>
                  <div className="flex items-center space-x-3">
                    <Calendar className="h-5 w-5 text-gray-400" />
                    <span className="text-gray-700">
                      Founded: {team.foundationDate || 'Unknown'}
                    </span>
                  </div>
                </div>
              </div>
            </div>

            {/* Players Section */}
            <div className="rounded-xl bg-white/80 p-6 shadow-lg backdrop-blur-sm">
              <div className="mb-6 flex items-center justify-between">
                <h2 className="text-xl font-bold text-gray-900">Squad</h2>
                <Link
                  href={`/teams/${teamId}/players`}
                  className="text-green-600 transition-colors hover:text-green-700"
                >
                  View All Players
                </Link>
              </div>

              {players.length > 0 ? (
                <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
                  {players.slice(0, 6).map((player) => (
                    <Link
                      key={player.id}
                      href={`/players/${player.id}`}
                      className="group flex items-center space-x-3 rounded-lg bg-gray-50/50 p-4 transition-all hover:bg-gray-100/50 hover:shadow-md"
                    >
                      <div className="relative h-12 w-12 rounded-full bg-gradient-to-br from-green-400 to-green-600 p-0.5">
                        <div className="flex h-full w-full items-center justify-center rounded-full bg-white">
                          {player.image ? (
                            <Image
                              src={player.image}
                              alt={`${player.fullName} profile`}
                              width={40}
                              height={40}
                              className="rounded-full object-cover"
                            />
                          ) : (
                            <User className="h-6 w-6 text-green-600" />
                          )}
                        </div>
                      </div>
                      <div className="flex-1">
                        <p className="font-medium text-gray-900 transition-colors group-hover:text-green-600">
                          {player.knownName}
                        </p>
                        <p className="text-sm text-gray-600">
                          {player.position}
                        </p>
                      </div>
                    </Link>
                  ))}
                </div>
              ) : (
                <div className="py-8 text-center">
                  <Users className="mx-auto mb-3 h-12 w-12 text-gray-400" />
                  <p className="text-gray-600">
                    No players found for this team.
                  </p>
                </div>
              )}
            </div>
          </div>
        </div>
      </SidebarLayout>
    </ProtectedRoute>
  );
}

function TeamDetailSidebar({ isAdmin }: { isAdmin: boolean }) {
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
        <SidebarItem icon={<ClubIcon size={20} />} text="Teams" active />
      </Link>
      <Link href="/players">
        <SidebarItem icon={<User size={20} />} text="Players" />
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
          </Link>{' '}
        </>
      )}
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
