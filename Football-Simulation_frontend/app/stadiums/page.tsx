'use client';
import { useState, useEffect } from 'react';
import { useRouter } from 'next/navigation';
import Link from 'next/link';
import Image from 'next/image';
import {
  Search,
  MapPin,
  Users,
  Trophy,
  Calendar,
  Star,
  Settings,
  ClubIcon,
  Bell,
  User,
  Package,
  Home,
  LayoutDashboardIcon,
  Building,
  Zap,
  Activity,
} from 'lucide-react';
import { SidebarLayout } from '../Components/Sidebar/Sidebar';
import { SidebarItem } from '../Components/Sidebar/SidebarItem';
import { SidebarSection } from '../Components/Sidebar/SidebarSection';
import ProtectedRoute from '@/app/Components/ProtectedRoute/ProtectedRoute';
import stadiumService, { Stadium } from '@/Services/StadiumService';
import authService from '@/Services/AuthenticationService';

interface StadiumStats {
  capacity: number;
  averageAttendance: number;
  matchesHosted: number;
  rating: number;
}

export default function StadiumsPage() {
  const [stadiums, setStadiums] = useState<Stadium[]>([]);
  const [filteredStadiums, setFilteredStadiums] = useState<Stadium[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [searchQuery, setSearchQuery] = useState('');
  const [isAdmin, setIsAdmin] = useState(false);
  const router = useRouter();

  useEffect(() => {
    if (!authService.isAuthenticated()) {
      router.push('/login');
      return;
    }

    setIsAdmin(authService.hasRole('Admin'));
    fetchStadiums();
  }, [router]);

  useEffect(() => {
    filterStadiums();
  }, [stadiums, searchQuery]);

  const fetchStadiums = async () => {
    try {
      setLoading(true);
      const stadiumsData = await stadiumService.getStadiums();
      setStadiums(stadiumsData);
    } catch (err) {
      console.error('Error fetching stadiums:', err);
      setError('Failed to load stadiums. Please try again.');
    } finally {
      setLoading(false);
    }
  };
  const filterStadiums = () => {
    let filtered = [...stadiums];

    // Filter by search query
    if (searchQuery.trim()) {
      filtered = filtered.filter(
        (stadium) =>
          stadium.name.toLowerCase().includes(searchQuery.toLowerCase()) ||
          stadium.city?.toLowerCase().includes(searchQuery.toLowerCase()) ||
          stadium.country?.toLowerCase().includes(searchQuery.toLowerCase()) ||
          stadium.address?.toLowerCase().includes(searchQuery.toLowerCase())
      );
    }

    setFilteredStadiums(filtered);
  };

  const getStadiumStats = (stadium: Stadium): StadiumStats => {
    // Mock stats - replace with actual API data when available
    const capacity =
      stadium.capacity || Math.floor(Math.random() * 50000) + 20000;
    return {
      capacity,
      averageAttendance: Math.floor(capacity * (0.7 + Math.random() * 0.25)), // 70-95% capacity
      matchesHosted: Math.floor(Math.random() * 100) + 50,
      rating: Math.round((4.0 + Math.random() * 1.0) * 10) / 10, // 4.0-5.0 rating
    };
  };
  const getSurfaceColor = (surface?: string) => {
    switch (surface?.toLowerCase()) {
      case 'grass':
        return 'bg-green-100 text-green-800';
      case 'artificial':
        return 'bg-blue-100 text-blue-800';
      case 'hybrid':
        return 'bg-purple-100 text-purple-800';
      default:
        return 'bg-gray-100 text-gray-800';
    }
  };

  const formatCapacity = (capacity: number) => {
    return capacity.toLocaleString();
  };

  const formatDate = (dateString?: string) => {
    if (!dateString) return 'Unknown';
    const date = new Date(dateString);
    return date.getFullYear().toString();
  };

  if (loading) {
    return (
      <ProtectedRoute allowedRoles={['User', 'Admin', 'Coach', 'Player']}>
        <SidebarLayout sidebar={<StadiumsSidebar isAdmin={isAdmin} />}>
          <div className="relative flex h-screen items-center justify-center">
            <BackgroundElements />
            <div className="relative z-10 flex flex-col items-center space-y-6">
              <div className="h-16 w-16 animate-spin rounded-full border-4 border-green-500 border-t-transparent"></div>
              <h2 className="text-2xl font-bold text-gray-800">
                Loading Stadiums...
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
        <SidebarLayout sidebar={<StadiumsSidebar isAdmin={isAdmin} />}>
          <div className="relative flex h-screen items-center justify-center">
            <BackgroundElements />
            <div className="relative z-10 text-center">
              <div className="rounded-lg border-l-4 border-red-500 bg-white p-6 text-red-700 shadow-lg">
                <p className="mb-2 text-xl font-bold">Error</p>
                <p className="mb-4">{error}</p>
                <button
                  onClick={fetchStadiums}
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
      <SidebarLayout sidebar={<StadiumsSidebar isAdmin={isAdmin} />}>
        <div className="relative min-h-screen">
          <BackgroundElements />

          <div className="relative z-10 p-6">
            {/* Header Section */}
            <div className="mb-8">
              <div className="flex items-center justify-between">
                <div>
                  <h1 className="text-3xl font-bold text-gray-900">Stadiums</h1>
                  <p className="mt-2 text-gray-600">
                    Explore football stadiums around the world
                  </p>
                </div>
                <div className="flex items-center space-x-4">
                  <div className="rounded-lg bg-white/80 p-4 shadow-lg backdrop-blur-sm">
                    <div className="flex items-center space-x-2">
                      <Building className="h-8 w-8 text-green-600" />
                      <div>
                        <p className="text-2xl font-bold text-gray-900">
                          {stadiums.length}
                        </p>
                        <p className="text-sm text-gray-600">Total Stadiums</p>
                      </div>
                    </div>
                  </div>
                </div>
              </div>
            </div>{' '}
            {/* Search and Filters */}
            <div className="mb-6 flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
              <div className="relative max-w-md flex-1">
                <input
                  type="text"
                  placeholder="Search stadiums..."
                  value={searchQuery}
                  onChange={(e) => setSearchQuery(e.target.value)}
                  className="w-full rounded-lg border border-gray-200 bg-white/80 px-10 py-3 text-gray-700 placeholder-gray-500 backdrop-blur-sm focus:border-green-500 focus:ring-1 focus:ring-green-500 focus:outline-none"
                />
                <Search className="absolute top-1/2 left-3 h-4 w-4 -translate-y-1/2 text-gray-400" />
              </div>
            </div>{' '}
            {/* Stadiums Grid */}
            <div className="grid gap-6 md:grid-cols-2 lg:grid-cols-3">
              {filteredStadiums.map((stadium) => {
                const stats = getStadiumStats(stadium);
                return (
                  <div
                    key={stadium.id}
                    className="transform transition-all duration-200"
                  >
                    <div className="rounded-xl bg-white/80 p-6 shadow-lg backdrop-blur-sm">
                      {/* Stadium Image */}
                      <div className="relative mb-4 h-48 w-full overflow-hidden rounded-lg bg-gradient-to-br from-green-400 to-blue-500">
                        {stadium.image || stadium.imageUrl ? (
                          <Image
                            src={stadium.image || stadium.imageUrl || ''}
                            alt={stadium.name}
                            fill
                            className="object-cover"
                          />
                        ) : (
                          <div className="flex h-full w-full items-center justify-center">
                            <Building className="h-16 w-16 text-white/70" />
                          </div>
                        )}
                        <div className="absolute inset-0 bg-gradient-to-t from-black/30 to-transparent"></div>
                        <div className="absolute right-3 bottom-3 left-3">
                          <h3 className="text-lg font-bold text-white">
                            {stadium.name}
                          </h3>
                        </div>
                      </div>

                      {/* Stadium Info */}
                      <div className="space-y-3">
                        <div className="flex items-center justify-between">
                          <div className="flex items-center space-x-2 text-sm text-gray-600">
                            <MapPin className="h-4 w-4" />
                            <span>
                              {stadium.city}, {stadium.country}
                            </span>
                          </div>
                          {stadium.surfaceType && (
                            <span
                              className={`inline-block rounded-full px-2 py-1 text-xs font-medium ${getSurfaceColor(stadium.surfaceType)}`}
                            >
                              {stadium.surfaceType}
                            </span>
                          )}
                        </div>

                        {/* Stadium Stats */}
                        <div className="grid grid-cols-2 gap-4 rounded-lg bg-gray-50/50 p-3">
                          <div className="text-center">
                            <p className="text-xl font-bold text-blue-600">
                              {formatCapacity(stats.capacity)}
                            </p>
                            <p className="text-xs text-gray-600">Capacity</p>
                          </div>
                          <div className="text-center">
                            <p className="text-xl font-bold text-green-600">
                              {stats.rating}
                            </p>
                            <p className="text-xs text-gray-600">Rating</p>
                          </div>
                        </div>

                        {/* Additional Info */}
                        <div className="space-y-2">
                          <div className="flex items-center space-x-2 text-sm text-gray-600">
                            <Calendar className="h-4 w-4" />
                            <span>Built: {formatDate(stadium.builtDate)}</span>
                          </div>

                          <div className="flex items-center space-x-2 text-sm text-gray-600">
                            <Activity className="h-4 w-4" />
                            <span>{stats.matchesHosted} matches hosted</span>
                          </div>

                          <div className="flex items-center space-x-2 text-sm text-gray-600">
                            <Users className="h-4 w-4" />
                            <span>
                              Avg. attendance:{' '}
                              {formatCapacity(stats.averageAttendance)}
                            </span>
                          </div>
                        </div>

                        {/* Address */}
                        {stadium.address && (
                          <div className="border-t border-gray-200 pt-3">
                            <p className="line-clamp-2 text-sm text-gray-600">
                              {stadium.address}
                            </p>
                          </div>
                        )}

                        {/* Facilities */}
                        {stadium.facilities && (
                          <div className="pt-2">
                            <div className="flex items-center space-x-2 text-sm text-gray-600">
                              <Zap className="h-4 w-4" />
                              <span className="line-clamp-1">
                                {stadium.facilities}
                              </span>
                            </div>
                          </div>
                        )}

                        {/* Rating */}
                        <div className="flex items-center justify-between pt-3">
                          <div className="flex items-center space-x-1">
                            <Star className="h-4 w-4 fill-current text-yellow-500" />
                            <span className="text-sm font-medium text-gray-700">
                              {stats.rating} / 5.0
                            </span>
                          </div>
                        </div>
                      </div>
                    </div>
                  </div>
                );
              })}
            </div>{' '}
            {/* Empty State */}
            {filteredStadiums.length === 0 && !loading && (
              <div className="py-12 text-center">
                <Building className="mx-auto mb-4 h-16 w-16 text-gray-400" />
                <h3 className="mb-2 text-lg font-medium text-gray-900">
                  No stadiums found
                </h3>
                <p className="text-gray-600">
                  {searchQuery
                    ? 'Try adjusting your search.'
                    : 'No stadiums are available at the moment.'}
                </p>
              </div>
            )}
          </div>
        </div>
      </SidebarLayout>
    </ProtectedRoute>
  );
}

function StadiumsSidebar({ isAdmin }: { isAdmin: boolean }) {
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
        <SidebarItem icon={<Users size={20} />} text="Coaches" />
      </Link>
      <Link href="/stadiums">
        <SidebarItem icon={<Home size={20} />} text="Stadiums" active />
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
