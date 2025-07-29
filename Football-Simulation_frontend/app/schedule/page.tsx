'use client';
import { useState, useEffect } from 'react';
import { useRouter } from 'next/navigation';
import Link from 'next/link';
import {
  Calendar,
  Filter,
  Clock,
  MapPin,
  Trophy,
  TrendingUp,
  Settings,
  ClubIcon,
  Bell,
  User,
  Package,
  Home,
  LayoutDashboardIcon,
  PlayCircle,
  Users,
  Search,
} from 'lucide-react';
import { SidebarLayout } from '../Components/Sidebar/Sidebar';
import { SidebarItem } from '../Components/Sidebar/SidebarItem';
import { SidebarSection } from '../Components/Sidebar/SidebarSection';
import ProtectedRoute from '@/app/Components/ProtectedRoute/ProtectedRoute';
import matchService, { Match } from '@/Services/MatchService';
import authService from '@/Services/AuthenticationService';

interface MatchFilter {
  status?: string;
  team?: string;
  date?: string;
}

export default function SchedulePage() {
  const [matches, setMatches] = useState<Match[]>([]);
  const [filteredMatches, setFilteredMatches] = useState<Match[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [isAdmin, setIsAdmin] = useState(false);
  const [activeFilter, setActiveFilter] = useState<string>('all');
  const [searchQuery, setSearchQuery] = useState('');
  const router = useRouter();

  useEffect(() => {
    if (!authService.isAuthenticated()) {
      router.push('/login');
      return;
    }

    setIsAdmin(authService.hasRole('Admin'));
    fetchMatches();
  }, [router]);

  useEffect(() => {
    filterMatches();
  }, [matches, activeFilter, searchQuery]);

  const fetchMatches = async () => {
    try {
      setLoading(true);
      const matchesData = await matchService.getMatches();
      setMatches(matchesData);
    } catch (err) {
      console.error('Error fetching matches:', err);
      setError('Failed to load matches. Please try again.');
    } finally {
      setLoading(false);
    }
  };

  const filterMatches = () => {
    let filtered = [...matches];

    // Filter by status
    if (activeFilter !== 'all') {
      filtered = filtered.filter(
        (match) => match.status?.toLowerCase() === activeFilter.toLowerCase()
      );
    }

    // Filter by search query
    if (searchQuery.trim()) {
      filtered = filtered.filter(
        (match) =>
          match.homeTeamName
            ?.toLowerCase()
            .includes(searchQuery.toLowerCase()) ||
          match.awayTeamName
            ?.toLowerCase()
            .includes(searchQuery.toLowerCase()) ||
          match.stadiumName?.toLowerCase().includes(searchQuery.toLowerCase())
      );
    }

    // Sort by date
    filtered.sort((a, b) => {
      const dateA = new Date(a.scheduledDateTimeUtc || 0).getTime();
      const dateB = new Date(b.scheduledDateTimeUtc || 0).getTime();
      return dateB - dateA; // Most recent first
    });

    setFilteredMatches(filtered);
  };

  const getStatusColor = (status?: string) => {
    switch (status?.toLowerCase()) {
      case 'live':
        return 'bg-red-500 text-white';
      case 'completed':
        return 'bg-green-500 text-white';
      case 'scheduled':
        return 'bg-blue-500 text-white';
      case 'postponed':
        return 'bg-yellow-500 text-black';
      case 'cancelled':
        return 'bg-gray-500 text-white';
      default:
        return 'bg-gray-300 text-gray-700';
    }
  };

  const formatDate = (dateString?: string) => {
    if (!dateString) return 'TBD';
    const date = new Date(dateString);
    return date.toLocaleDateString('en-US', {
      weekday: 'short',
      month: 'short',
      day: 'numeric',
      year: 'numeric',
    });
  };

  const formatTime = (timeString?: string) => {
    if (!timeString) return 'TBD';
    return timeString;
  };

  if (loading) {
    return (
      <ProtectedRoute allowedRoles={['User', 'Admin', 'Coach', 'Player']}>
        <SidebarLayout sidebar={<ScheduleSidebar isAdmin={isAdmin} />}>
          <div className="relative flex h-screen items-center justify-center">
            <BackgroundElements />
            <div className="relative z-10 flex flex-col items-center space-y-6">
              <div className="h-16 w-16 animate-spin rounded-full border-4 border-green-500 border-t-transparent"></div>
              <h2 className="text-2xl font-bold text-gray-800">
                Loading Schedule...
              </h2>
            </div>
          </div>
        </SidebarLayout>
      </ProtectedRoute>
    );
  }

  if (error) {
    return (
      <ProtectedRoute allowedRoles={['User', 'Admin', 'Coach', 'Player']}>
        <SidebarLayout sidebar={<ScheduleSidebar isAdmin={isAdmin} />}>
          <div className="relative flex h-screen items-center justify-center">
            <BackgroundElements />
            <div className="relative z-10 text-center">
              <div className="rounded-lg border-l-4 border-red-500 bg-white p-6 text-red-700 shadow-lg">
                <p className="mb-2 text-xl font-bold">Error</p>
                <p className="mb-4">{error}</p>
                <button
                  onClick={fetchMatches}
                  className="rounded-lg bg-red-500 px-4 py-2 text-white shadow transition-colors hover:bg-red-600"
                >
                  Try Again
                </button>
              </div>
            </div>
          </div>
        </SidebarLayout>
      </ProtectedRoute>
    );
  }

  return (
    <ProtectedRoute allowedRoles={['User', 'Admin', 'Coach', 'Player']}>
      <SidebarLayout sidebar={<ScheduleSidebar isAdmin={isAdmin} />}>
        <div className="relative min-h-screen">
          <BackgroundElements />

          <div className="relative z-10 p-6">
            {/* Header Section */}
            <div className="mb-8">
              <div className="flex items-center justify-between">
                <div>
                  <h1 className="text-3xl font-bold text-gray-900">
                    Match Schedule
                  </h1>
                  <p className="mt-2 text-gray-600">
                    View all upcoming and completed matches
                  </p>
                </div>
                <div className="flex items-center space-x-4">
                  <div className="rounded-lg bg-white/80 p-4 shadow-lg backdrop-blur-sm">
                    <div className="flex items-center space-x-2">
                      <Calendar className="h-8 w-8 text-green-600" />
                      <div>
                        <p className="text-2xl font-bold text-gray-900">
                          {matches.length}
                        </p>
                        <p className="text-sm text-gray-600">Total Matches</p>
                      </div>
                    </div>
                  </div>
                </div>
              </div>
            </div>

            {/* Filters and Search */}
            <div className="mb-6 flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
              <div className="flex flex-wrap gap-2">
                {['all', 'live', 'scheduled', 'completed', 'postponed'].map(
                  (filter) => (
                    <button
                      key={filter}
                      onClick={() => setActiveFilter(filter)}
                      className={`rounded-lg px-4 py-2 text-sm font-medium transition-colors ${
                        activeFilter === filter
                          ? 'bg-green-600 text-white'
                          : 'bg-white/80 text-gray-700 hover:bg-gray-100'
                      }`}
                    >
                      {filter.charAt(0).toUpperCase() + filter.slice(1)}
                    </button>
                  )
                )}
              </div>

              <div className="flex items-center space-x-4">
                <div className="relative">
                  <input
                    type="text"
                    placeholder="Search teams or stadiums..."
                    value={searchQuery}
                    onChange={(e) => setSearchQuery(e.target.value)}
                    className="rounded-lg border border-gray-200 bg-white/80 px-10 py-3 text-gray-700 placeholder-gray-500 backdrop-blur-sm focus:border-green-500 focus:ring-1 focus:ring-green-500 focus:outline-none"
                  />
                  <Search className="absolute top-1/2 left-3 h-4 w-4 -translate-y-1/2 text-gray-400" />
                </div>
                <button className="flex items-center space-x-2 rounded-lg border border-gray-200 bg-white/80 px-4 py-3 text-gray-700 backdrop-blur-sm transition-colors hover:bg-gray-50">
                  <Filter className="h-4 w-4" />
                  <span>Filters</span>
                </button>
              </div>
            </div>

            {/* Matches Grid */}
            <div className="grid gap-6">
              {filteredMatches.map((match) => (
                <Link
                  key={match.id}
                  href={`/matchdetails?matchId=${match.id}`}
                  className="group transform transition-all duration-200 hover:scale-[1.02]"
                >
                  <div className="rounded-xl bg-white/80 p-6 shadow-lg backdrop-blur-sm transition-all hover:shadow-xl">
                    <div className="flex items-center justify-between">
                      {/* Teams Section */}
                      <div className="flex flex-1 items-center space-x-8">
                        {/* Home Team */}
                        <div className="flex items-center space-x-3">
                          <div className="flex h-12 w-12 items-center justify-center rounded-full bg-gradient-to-br from-blue-400 to-blue-600 font-bold text-white">
                            {match.homeTeamName?.charAt(0) || 'H'}
                          </div>
                          <div>
                            <h3 className="font-semibold text-gray-900 transition-colors group-hover:text-green-600">
                              {match.homeTeamName || 'Home Team'}
                            </h3>
                            <p className="text-sm text-gray-600">Home</p>
                          </div>
                        </div>

                        {/* VS and Score */}
                        <div className="flex items-center space-x-4">
                          {match.status?.toLowerCase() === 'completed' && (
                            <div className="text-center">
                              <div className="text-2xl font-bold text-gray-900">
                                {match.homeTeamScore ?? 0} -{' '}
                                {match.awayTeamScore ?? 0}
                              </div>
                              <p className="text-sm text-gray-600">Final</p>
                            </div>
                          )}
                          {match.status?.toLowerCase() === 'live' && (
                            <div className="text-center">
                              <div className="text-2xl font-bold text-red-600">
                                {match.homeTeamScore ?? 0} -{' '}
                                {match.awayTeamScore ?? 0}
                              </div>
                              <div className="flex items-center justify-center space-x-1">
                                <div className="h-2 w-2 animate-pulse rounded-full bg-red-500"></div>
                                <p className="text-sm font-medium text-red-600">
                                  LIVE
                                </p>
                              </div>
                            </div>
                          )}
                          {match.status?.toLowerCase() === 'scheduled' && (
                            <div className="text-center">
                              <div className="text-lg font-bold text-gray-600">
                                VS
                              </div>
                              <div className="flex items-center justify-center space-x-1 text-sm text-gray-600">
                                <Clock className="h-3 w-3" />
                                <span>{formatTime(match.time)}</span>
                              </div>
                            </div>
                          )}
                        </div>

                        {/* Away Team */}
                        <div className="flex items-center space-x-3">
                          <div>
                            <h3 className="text-right font-semibold text-gray-900 transition-colors group-hover:text-green-600">
                              {match.awayTeamName || 'Away Team'}
                            </h3>
                            <p className="text-right text-sm text-gray-600">
                              Away
                            </p>
                          </div>
                          <div className="flex h-12 w-12 items-center justify-center rounded-full bg-gradient-to-br from-red-400 to-red-600 font-bold text-white">
                            {match.awayTeamName?.charAt(0) || 'A'}
                          </div>
                        </div>
                      </div>

                      {/* Match Info */}
                      <div className="space-y-2 text-right">
                        <div
                          className={`inline-block rounded-full px-3 py-1 text-xs font-medium ${getStatusColor(match.status)}`}
                        >
                          {match.status || 'Unknown'}
                        </div>
                        <div className="text-sm text-gray-600">
                          <div className="flex items-center space-x-1">
                            <Calendar className="h-3 w-3" />
                            <span>
                              {formatDate(match.scheduledDateTimeUtc)}
                            </span>
                          </div>
                          {match.stadiumName && (
                            <div className="mt-1 flex items-center space-x-1">
                              <MapPin className="h-3 w-3" />
                              <span>{match.stadiumName}</span>
                            </div>
                          )}
                          {match.matchWeek && (
                            <div className="mt-1 text-xs text-gray-500">
                              Week {match.matchWeek}
                            </div>
                          )}
                        </div>
                      </div>
                    </div>

                    {/* Action buttons for live matches */}
                    {match.status?.toLowerCase() === 'live' && (
                      <div className="mt-4 border-t border-gray-200 pt-4">
                        <div className="flex items-center justify-between">
                          <div className="flex items-center space-x-1 text-sm text-gray-600">
                            <PlayCircle className="h-4 w-4" />
                            <span>Match in progress</span>
                          </div>
                          <button className="flex items-center space-x-2 rounded-lg bg-red-600 px-4 py-2 text-sm font-medium text-white transition-colors hover:bg-red-700">
                            <PlayCircle className="h-4 w-4" />
                            <span>Watch Live</span>
                          </button>
                        </div>
                      </div>
                    )}
                  </div>
                </Link>
              ))}
            </div>

            {/* Empty State */}
            {filteredMatches.length === 0 && !loading && (
              <div className="py-12 text-center">
                <Calendar className="mx-auto mb-4 h-16 w-16 text-gray-400" />
                <h3 className="mb-2 text-lg font-medium text-gray-900">
                  No matches found
                </h3>
                <p className="text-gray-600">
                  {searchQuery || activeFilter !== 'all'
                    ? 'Try adjusting your search or filters.'
                    : 'No matches are scheduled at the moment.'}
                </p>
              </div>
            )}
          </div>
        </div>
      </SidebarLayout>
    </ProtectedRoute>
  );
}

function ScheduleSidebar({ isAdmin }: { isAdmin: boolean }) {
  return (
    <>
      {/* Main Navigation */}
      <Link href="/dashboard">
        <SidebarItem
          icon={<LayoutDashboardIcon size={20} />}
          text="Dashboard"
        />
      </Link>
      <Link href="/teams">
        <SidebarItem icon={<ClubIcon size={20} />} text="Teams" />
      </Link>
      <Link href="/schedule">
        <SidebarItem icon={<Calendar size={20} />} text="Schedule" active />
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
            <SidebarItem
              icon={<Settings size={20} />}
              text="Admin Dashboard"
            />{' '}
          </Link>
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
    <div className="absolute inset-0 overflow-hidden">
      {/* Gradient Background */}
      <div className="absolute inset-0 bg-gradient-to-br from-green-50 via-white to-blue-50"></div>

      {/* Floating Elements */}
      <div className="absolute top-20 left-20 h-4 w-4 animate-ping rounded-full bg-green-300/30"></div>
      <div className="absolute top-40 right-32 h-6 w-6 animate-pulse rounded-full bg-blue-300/20"></div>
      <div className="absolute bottom-32 left-40 h-5 w-5 animate-bounce rounded-full bg-green-400/25"></div>
      <div className="absolute right-20 bottom-20 h-3 w-3 animate-pulse rounded-full bg-blue-400/30"></div>

      {/* Decorative Shapes */}
      <div className="absolute top-1/4 right-10 h-32 w-32 rounded-full bg-gradient-to-br from-green-100/30 to-blue-100/30 blur-3xl"></div>
      <div className="absolute bottom-1/4 left-10 h-40 w-40 rounded-full bg-gradient-to-br from-blue-100/20 to-green-100/20 blur-3xl"></div>
    </div>
  );
}
