'use client';
import { useState, useEffect } from 'react';
import { useRouter } from 'next/navigation';
import Link from 'next/link';
import Image from 'next/image';
import {
  Search,
  Filter,
  Users,
  Trophy,
  Calendar,
  MapPin,
  Star,
  Settings,
  ClubIcon,
  Bell,
  User,
  Package,
  Home,
  LayoutDashboardIcon,
  Briefcase,
  Award,
  Target,
} from 'lucide-react';
import { SidebarLayout } from '../Components/Sidebar/Sidebar';
import { SidebarItem } from '../Components/Sidebar/SidebarItem';
import { SidebarSection } from '../Components/Sidebar/SidebarSection';
import ProtectedRoute from '@/app/Components/ProtectedRoute/ProtectedRoute';
import coachService, { Coach } from '@/Services/CoachService';
import authService from '@/Services/AuthenticationService';

interface CoachStats {
  experience: number;
  winRate: number;
  currentTeam: string | null;
  achievements: number;
}

export default function CoachesPage() {
  const [coaches, setCoaches] = useState<Coach[]>([]);
  const [filteredCoaches, setFilteredCoaches] = useState<Coach[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [searchQuery, setSearchQuery] = useState('');
  const [nationalityFilter, setNationalityFilter] = useState('');
  const [isAdmin, setIsAdmin] = useState(false);
  const router = useRouter();

  useEffect(() => {
    if (!authService.isAuthenticated()) {
      router.push('/login');
      return;
    }

    setIsAdmin(authService.hasRole('Admin'));
    fetchCoaches();
  }, [router]);

  useEffect(() => {
    filterCoaches();
  }, [coaches, searchQuery, nationalityFilter]);

  const fetchCoaches = async () => {
    try {
      setLoading(true);
      const coachesData = await coachService.getCoaches();
      setCoaches(coachesData);
    } catch (err) {
      console.error('Error fetching coaches:', err);
      setError('Failed to load coaches. Please try again.');
    } finally {
      setLoading(false);
    }
  };

  const filterCoaches = () => {
    let filtered = [...coaches];

    // Filter by search query
    if (searchQuery.trim()) {
      filtered = filtered.filter(
        (coach) =>
          `${coach.firstName} ${coach.lastName}`
            .toLowerCase()
            .includes(searchQuery.toLowerCase()) ||
          coach.nationality
            ?.toLowerCase()
            .includes(searchQuery.toLowerCase()) ||
          coach.role?.toLowerCase().includes(searchQuery.toLowerCase()) ||
          coach.coachingStyle?.toLowerCase().includes(searchQuery.toLowerCase())
      );
    }

    // Filter by nationality
    if (nationalityFilter) {
      filtered = filtered.filter(
        (coach) =>
          coach.nationality?.toLowerCase() === nationalityFilter.toLowerCase()
      );
    }

    setFilteredCoaches(filtered);
  };

  const getCoachStats = (coach: Coach): CoachStats => {
    // Mock stats - replace with actual API data when available
    return {
      experience: coach.yearsOfExperience || Math.floor(Math.random() * 20) + 5,
      winRate: Math.floor(Math.random() * 40) + 50, // 50-90%
      currentTeam: coach.teamId ? `Team ${coach.teamId}` : null,
      achievements: Math.floor(Math.random() * 10) + 1,
    };
  };

  const getUniqueNationalities = () => {
    const nationalities = coaches
      .map((coach) => coach.nationality)
      .filter(Boolean)
      .filter((value, index, self) => self.indexOf(value) === index)
      .sort();
    return nationalities;
  };

  const getRoleColor = (role?: string) => {
    switch (role?.toLowerCase()) {
      case 'head coach':
        return 'bg-blue-100 text-blue-800';
      case 'assistant coach':
        return 'bg-green-100 text-green-800';
      case 'goalkeeper coach':
        return 'bg-yellow-100 text-yellow-800';
      case 'fitness coach':
        return 'bg-purple-100 text-purple-800';
      default:
        return 'bg-gray-100 text-gray-800';
    }
  };

  if (loading) {
    return (
      <ProtectedRoute allowedRoles={['User', 'Admin', 'Coach', 'Player']}>
        <SidebarLayout sidebar={<CoachesSidebar isAdmin={isAdmin} />}>
          <div className="relative flex h-screen items-center justify-center">
            <BackgroundElements />
            <div className="relative z-10 flex flex-col items-center space-y-6">
              <div className="h-16 w-16 animate-spin rounded-full border-4 border-green-500 border-t-transparent"></div>
              <h2 className="text-2xl font-bold text-gray-800">
                Loading Coaches...
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
        <SidebarLayout sidebar={<CoachesSidebar isAdmin={isAdmin} />}>
          <div className="relative flex h-screen items-center justify-center">
            <BackgroundElements />
            <div className="relative z-10 text-center">
              <div className="rounded-lg border-l-4 border-red-500 bg-white p-6 text-red-700 shadow-lg">
                <p className="mb-2 text-xl font-bold">Error</p>
                <p className="mb-4">{error}</p>
                <button
                  onClick={fetchCoaches}
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
      <SidebarLayout sidebar={<CoachesSidebar isAdmin={isAdmin} />}>
        <div className="relative min-h-screen">
          <BackgroundElements />

          <div className="relative z-10 p-6">
            {/* Header Section */}
            <div className="mb-8">
              <div className="flex items-center justify-between">
                <div>
                  <h1 className="text-3xl font-bold text-gray-900">Coaches</h1>
                  <p className="mt-2 text-gray-600">
                    Explore all coaches and their expertise
                  </p>
                </div>
                <div className="flex items-center space-x-4">
                  <div className="rounded-lg bg-white/80 p-4 shadow-lg backdrop-blur-sm">
                    <div className="flex items-center space-x-2">
                      <Users className="h-8 w-8 text-green-600" />
                      <div>
                        <p className="text-2xl font-bold text-gray-900">
                          {coaches.length}
                        </p>
                        <p className="text-sm text-gray-600">Total Coaches</p>
                      </div>
                    </div>
                  </div>
                </div>
              </div>
            </div>
            {/* Search and Filters */}
            <div className="mb-6 flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
              <div className="relative max-w-md flex-1">
                <input
                  type="text"
                  placeholder="Search coaches..."
                  value={searchQuery}
                  onChange={(e) => setSearchQuery(e.target.value)}
                  className="w-full rounded-lg border border-gray-200 bg-white/80 px-10 py-3 text-gray-700 placeholder-gray-500 backdrop-blur-sm focus:border-green-500 focus:ring-1 focus:ring-green-500 focus:outline-none"
                />
                <Search className="absolute top-1/2 left-3 h-4 w-4 -translate-y-1/2 text-gray-400" />
              </div>

              <div className="flex items-center space-x-4">
                <select
                  value={nationalityFilter}
                  onChange={(e) => setNationalityFilter(e.target.value)}
                  className="rounded-lg border border-gray-200 bg-white/80 px-4 py-3 text-gray-700 backdrop-blur-sm focus:border-green-500 focus:ring-1 focus:ring-green-500 focus:outline-none"
                >
                  <option value="">All Nationalities</option>
                  {getUniqueNationalities().map((nationality) => (
                    <option key={nationality} value={nationality}>
                      {nationality}
                    </option>
                  ))}
                </select>{' '}
                <div className="flex items-center space-x-2 px-4 py-3 text-gray-700">
                  <Filter className="h-4 w-4" />
                  <span>Filters</span>
                </div>
              </div>
            </div>{' '}
            {/* Coaches Grid */}
            <div className="grid gap-6 md:grid-cols-2 lg:grid-cols-3">
              {filteredCoaches.map((coach) => {
                const stats = getCoachStats(coach);
                return (
                  <div
                    key={coach.id}
                    className="transform transition-all duration-200"
                  >
                    <div className="rounded-xl bg-white/80 p-6 shadow-lg backdrop-blur-sm">
                      {/* Coach Header */}
                      <div className="mb-4 flex items-center space-x-4">
                        <div className="relative h-16 w-16 rounded-full bg-gradient-to-br from-blue-400 to-blue-600 p-0.5">
                          <div className="flex h-full w-full items-center justify-center rounded-full bg-white">
                            {coach.Photo || coach.photoUrl ? (
                              <Image
                                src={coach.Photo || coach.photoUrl || ''}
                                alt={`${coach.firstName} ${coach.lastName}`}
                                width={56}
                                height={56}
                                className="rounded-full object-cover"
                              />
                            ) : (
                              <Users className="h-8 w-8 text-blue-600" />
                            )}
                          </div>
                        </div>
                        <div className="flex-1">
                          <h3 className="font-bold text-gray-900">
                            {coach.firstName} {coach.lastName}
                          </h3>
                          {coach.role && (
                            <span
                              className={`inline-block rounded-full px-2 py-1 text-xs font-medium ${getRoleColor(coach.role)}`}
                            >
                              {coach.role}
                            </span>
                          )}
                          {coach.nationality && (
                            <div className="mt-1 flex items-center space-x-2 text-sm text-gray-500">
                              <MapPin className="h-3 w-3" />
                              <span>{coach.nationality}</span>
                            </div>
                          )}
                        </div>
                      </div>

                      {/* Coach Stats */}
                      <div className="mb-4 grid grid-cols-2 gap-4 rounded-lg bg-gray-50/50 p-4">
                        <div className="text-center">
                          <p className="text-2xl font-bold text-blue-600">
                            {stats.experience}
                          </p>
                          <p className="text-xs text-gray-600">Years Exp.</p>
                        </div>
                        <div className="text-center">
                          <p className="text-2xl font-bold text-green-600">
                            {stats.winRate}%
                          </p>
                          <p className="text-xs text-gray-600">Win Rate</p>
                        </div>
                      </div>

                      {/* Coach Details */}
                      <div className="space-y-3">
                        {coach.preferredFormation && (
                          <div className="flex items-center space-x-2 text-sm text-gray-600">
                            <Target className="h-4 w-4" />
                            <span>Formation: {coach.preferredFormation}</span>
                          </div>
                        )}

                        {coach.coachingStyle && (
                          <div className="flex items-center space-x-2 text-sm text-gray-600">
                            <Briefcase className="h-4 w-4" />
                            <span>Style: {coach.coachingStyle}</span>
                          </div>
                        )}

                        {stats.currentTeam && (
                          <div className="flex items-center space-x-2 text-sm text-gray-600">
                            <ClubIcon className="h-4 w-4" />
                            <span>Current Team: {stats.currentTeam}</span>
                          </div>
                        )}
                      </div>

                      {/* Achievements */}
                      <div className="mt-4 flex items-center justify-between">
                        <div className="flex items-center space-x-1">
                          <Award className="h-4 w-4 text-yellow-500" />
                          <span className="text-sm font-medium text-gray-700">
                            {stats.achievements} Achievements
                          </span>
                        </div>
                      </div>

                      {/* Biography Preview */}
                      {coach.biography && (
                        <div className="mt-4 border-t border-gray-200 pt-4">
                          <p className="line-clamp-2 text-sm text-gray-600">
                            {coach.biography}
                          </p>
                        </div>
                      )}
                    </div>
                  </div>
                );
              })}
            </div>
            {/* Empty State */}
            {filteredCoaches.length === 0 && !loading && (
              <div className="py-12 text-center">
                <Users className="mx-auto mb-4 h-16 w-16 text-gray-400" />
                <h3 className="mb-2 text-lg font-medium text-gray-900">
                  No coaches found
                </h3>
                <p className="text-gray-600">
                  {searchQuery || nationalityFilter
                    ? 'Try adjusting your search or filters.'
                    : 'No coaches are available at the moment.'}
                </p>
              </div>
            )}
          </div>
        </div>
      </SidebarLayout>
    </ProtectedRoute>
  );
}

function CoachesSidebar({ isAdmin }: { isAdmin: boolean }) {
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
      <Link href="/players">
        <SidebarItem icon={<User size={20} />} text="Players" />
      </Link>
      <Link href="/coaches">
        <SidebarItem icon={<Users size={20} />} text="Coaches" active />
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
    <div className="absolute inset-0 overflow-hidden">
      {/* Gradient Background */}
      <div className="absolute inset-0 bg-gradient-to-br from-blue-50 via-white to-green-50"></div>

      {/* Floating Elements */}
      <div className="absolute top-20 left-20 h-4 w-4 animate-ping rounded-full bg-blue-300/30"></div>
      <div className="absolute top-40 right-32 h-6 w-6 animate-pulse rounded-full bg-green-300/20"></div>
      <div className="absolute bottom-32 left-40 h-5 w-5 animate-bounce rounded-full bg-blue-400/25"></div>
      <div className="absolute right-20 bottom-20 h-3 w-3 animate-pulse rounded-full bg-green-400/30"></div>

      {/* Decorative Shapes */}
      <div className="absolute top-1/4 right-10 h-32 w-32 rounded-full bg-gradient-to-br from-blue-100/30 to-green-100/30 blur-3xl"></div>
      <div className="absolute bottom-1/4 left-10 h-40 w-40 rounded-full bg-gradient-to-br from-green-100/20 to-blue-100/20 blur-3xl"></div>
    </div>
  );
}
