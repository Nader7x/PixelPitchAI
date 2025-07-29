'use client';
import { useState, useEffect } from 'react';
import { useRouter, useParams } from 'next/navigation';
import Link from 'next/link';
import Image from 'next/image';
import {
  ArrowLeft,
  Calendar,
  MapPin,
  Users,
  Star,
  Clock,
  Wifi,
  ParkingCircle,
  Car,
  Coffee,
  Shield,
  Home,
  LayoutDashboardIcon,
  Bell,
  User,
  Package,
  Settings,
  Search,
  ClubIcon,
  Building,
  TreePine,
  Activity,
} from 'lucide-react';
import { SidebarLayout } from '../../Components/Sidebar/Sidebar';
import { SidebarItem } from '../../Components/Sidebar/SidebarItem';
import { SidebarSection } from '../../Components/Sidebar/SidebarSection';
import ProtectedRoute from '@/app/Components/ProtectedRoute/ProtectedRoute';
import stadiumService, { Stadium } from '@/Services/StadiumService';
import authService from '@/Services/AuthenticationService';

interface StadiumStats {
  capacity: number;
  averageAttendance: number;
  matchesHosted: number;
  lastRenovation: string;
  surfaceType: string;
  rating: number;
}

interface Facility {
  id: string;
  name: string;
  description: string;
  icon: React.ReactNode;
  available: boolean;
}

interface Event {
  id: string;
  title: string;
  date: string;
  type: 'match' | 'concert' | 'event';
  attendance: number;
}

export default function StadiumDetailPage() {
  const params = useParams();
  const stadiumId = params.stadiumId as string;
  const [stadium, setStadium] = useState<Stadium | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [isAdmin, setIsAdmin] = useState(false);
  const router = useRouter();

  useEffect(() => {
    if (!authService.isAuthenticated()) {
      router.push('/login');
      return;
    }

    setIsAdmin(authService.hasRole('Admin'));
    fetchStadiumDetails();
  }, [router, stadiumId]);

  const fetchStadiumDetails = async () => {
    try {
      setLoading(true);
      setError(null);
      const stadiumData = await stadiumService.getStadiumById(
        parseInt(stadiumId, 10)
      );
      setStadium(stadiumData);
    } catch (err) {
      console.error('Error fetching stadium details:', err);
      setError('Failed to load stadium details. Please try again.');
    } finally {
      setLoading(false);
    }
  };

  const getStadiumStats = (stadium: Stadium): StadiumStats => {
    return {
      capacity: stadium.capacity || 50000,
      averageAttendance: Math.floor((stadium.capacity || 50000) * 0.85),
      matchesHosted: Math.floor(Math.random() * 200) + 50,
      lastRenovation: '2020',
      surfaceType: stadium.surfaceType || 'Natural Grass',
      rating: stadium.latitude || Math.random() * 2 + 3,
    };
  };

  const getFacilities = (): Facility[] => {
    return [
      {
        id: '1',
        name: 'VIP Lounges',
        description: 'Premium hospitality areas',
        icon: <Star className="h-5 w-5" />,
        available: true,
      },
      {
        id: '2',
        name: 'Parking',
        description: '5,000 parking spaces',
        icon: <ParkingCircle className="h-5 w-5" />,
        available: true,
      },
      {
        id: '3',
        name: 'Wi-Fi',
        description: 'Free high-speed internet',
        icon: <Wifi className="h-5 w-5" />,
        available: true,
      },
      {
        id: '4',
        name: 'Concessions',
        description: 'Food and beverage outlets',
        icon: <Coffee className="h-5 w-5" />,
        available: true,
      },
      {
        id: '5',
        name: 'Security',
        description: '24/7 security and surveillance',
        icon: <Shield className="h-5 w-5" />,
        available: true,
      },
      {
        id: '6',
        name: 'Accessibility',
        description: 'Wheelchair accessible areas',
        icon: <Users className="h-5 w-5" />,
        available: true,
      },
    ];
  };

  const getRecentEvents = (): Event[] => {
    return [
      {
        id: '1',
        title: 'Championship Final',
        date: '2024-06-15',
        type: 'match',
        attendance: 48500,
      },
      {
        id: '2',
        title: 'International Friendly',
        date: '2024-05-20',
        type: 'match',
        attendance: 52000,
      },
      {
        id: '3',
        title: 'Summer Concert Series',
        date: '2024-07-10',
        type: 'concert',
        attendance: 55000,
      },
    ];
  };

  if (loading) {
    return (
      <ProtectedRoute allowedRoles={['User', 'Admin', 'Coach', 'Player']}>
        <SidebarLayout sidebar={<StadiumDetailSidebar isAdmin={isAdmin} />}>
          <div className="relative flex h-screen items-center justify-center">
            <BackgroundElements />
            <div className="relative z-10 flex flex-col items-center space-y-6">
              <div className="h-16 w-16 animate-spin rounded-full border-4 border-green-500 border-t-transparent"></div>
              <h2 className="text-2xl font-bold text-gray-800">
                Loading Stadium Details...
              </h2>
            </div>
          </div>
        </SidebarLayout>
      </ProtectedRoute>
    );
  }

  if (error || !stadium) {
    return (
      <ProtectedRoute allowedRoles={['User', 'Admin', 'Coach', 'Player']}>
        <SidebarLayout sidebar={<StadiumDetailSidebar isAdmin={isAdmin} />}>
          <div className="relative flex h-screen items-center justify-center">
            <BackgroundElements />
            <div className="relative z-10 text-center">
              <div className="rounded-lg border-l-4 border-red-500 bg-white p-6 text-red-700 shadow-lg">
                <p className="mb-2 text-xl font-bold">Error</p>
                <p className="mb-4">{error || 'Stadium not found'}</p>
                <div className="space-x-4">
                  <button
                    onClick={fetchStadiumDetails}
                    className="rounded-lg bg-red-500 px-4 py-2 text-white shadow transition-colors hover:bg-red-600"
                  >
                    Try Again
                  </button>
                  <Link
                    href="/stadiums"
                    className="rounded-lg bg-gray-500 px-4 py-2 text-white shadow transition-colors hover:bg-gray-600"
                  >
                    Back to Stadiums
                  </Link>
                </div>
              </div>
            </div>
          </div>
        </SidebarLayout>
      </ProtectedRoute>
    );
  }

  const stats = getStadiumStats(stadium);
  const facilities = getFacilities();
  const recentEvents = getRecentEvents();

  return (
    <ProtectedRoute allowedRoles={['User', 'Admin', 'Coach', 'Player']}>
      <SidebarLayout sidebar={<StadiumDetailSidebar isAdmin={isAdmin} />}>
        <div className="relative min-h-screen">
          <BackgroundElements />

          <div className="relative z-10 p-6">
            {/* Header with Back Button */}
            <div className="mb-6">
              <Link
                href="/stadiums"
                className="inline-flex items-center space-x-2 text-gray-600 transition-colors hover:text-gray-800"
              >
                <ArrowLeft className="h-5 w-5" />
                <span>Back to Stadiums</span>
              </Link>
            </div>

            {/* Stadium Header */}
            <div className="mb-8 rounded-xl bg-white/80 p-8 shadow-lg backdrop-blur-sm">
              <div className="flex flex-col gap-6 lg:flex-row">
                <div className="flex-shrink-0">
                  <div className="relative h-48 w-80 overflow-hidden rounded-lg bg-gradient-to-br from-green-400 to-green-600">
                    {stadium.imageUrl ? (
                      <Image
                        src={stadium.imageUrl}
                        alt={stadium.name}
                        fill
                        className="object-cover"
                      />
                    ) : (
                      <div className="flex h-full w-full items-center justify-center">
                        <Building className="h-20 w-20 text-white" />
                      </div>
                    )}
                  </div>
                </div>

                <div className="flex-1 space-y-4">
                  <div>
                    <h1 className="text-3xl font-bold text-gray-900">
                      {stadium.name}
                    </h1>
                    <div className="mt-2 flex flex-wrap gap-4">
                      <span className="inline-flex items-center rounded-full bg-green-100 px-3 py-1 text-sm font-medium text-green-800">
                        <TreePine className="mr-1 h-4 w-4" />
                        {stats.surfaceType}
                      </span>
                      <span className="inline-flex items-center rounded-full bg-blue-100 px-3 py-1 text-sm font-medium text-blue-800">
                        <Users className="mr-1 h-4 w-4" />
                        {stats.capacity.toLocaleString()} capacity
                      </span>
                      <div className="flex items-center">
                        <Star className="mr-1 h-4 w-4 text-yellow-500" />
                        <span className="text-sm font-medium">
                          {stats.rating.toFixed(1)}
                        </span>
                      </div>
                    </div>
                  </div>

                  <div className="grid grid-cols-1 gap-4 md:grid-cols-2">
                    <div className="flex items-center space-x-2 text-gray-600">
                      <MapPin className="h-5 w-5 text-gray-400" />
                      <span>{stadium.city || 'Location not specified'}</span>
                    </div>
                    <div className="flex items-center space-x-2 text-gray-600">
                      <Calendar className="h-5 w-5 text-gray-400" />
                      <span>Built {stadium.builtDate || 'Unknown'}</span>
                    </div>
                    <div className="flex items-center space-x-2 text-gray-600">
                      <Activity className="h-5 w-5 text-gray-400" />
                      <span>Last renovated {stats.lastRenovation}</span>
                    </div>
                    <div className="flex items-center space-x-2 text-gray-600">
                      <Users className="h-5 w-5 text-gray-400" />
                      <span>
                        Avg. attendance:{' '}
                        {stats.averageAttendance.toLocaleString()}
                      </span>
                    </div>
                  </div>
                </div>
              </div>
            </div>

            {/* Stats Grid */}
            <div className="mb-8 grid gap-6 md:grid-cols-2 lg:grid-cols-4">
              <div className="rounded-xl bg-white/80 p-6 shadow-lg backdrop-blur-sm">
                <div className="flex items-center space-x-3">
                  <div className="rounded-full bg-blue-100 p-3">
                    <Users className="h-6 w-6 text-blue-600" />
                  </div>
                  <div>
                    <p className="text-2xl font-bold text-gray-900">
                      {stats.capacity.toLocaleString()}
                    </p>
                    <p className="text-sm text-gray-600">Total Capacity</p>
                  </div>
                </div>
              </div>

              <div className="rounded-xl bg-white/80 p-6 shadow-lg backdrop-blur-sm">
                <div className="flex items-center space-x-3">
                  <div className="rounded-full bg-green-100 p-3">
                    <Activity className="h-6 w-6 text-green-600" />
                  </div>
                  <div>
                    <p className="text-2xl font-bold text-gray-900">
                      {stats.matchesHosted}
                    </p>
                    <p className="text-sm text-gray-600">Matches Hosted</p>
                  </div>
                </div>
              </div>

              <div className="rounded-xl bg-white/80 p-6 shadow-lg backdrop-blur-sm">
                <div className="flex items-center space-x-3">
                  <div className="rounded-full bg-yellow-100 p-3">
                    <Star className="h-6 w-6 text-yellow-600" />
                  </div>
                  <div>
                    <p className="text-2xl font-bold text-gray-900">
                      {stats.rating.toFixed(1)}
                    </p>
                    <p className="text-sm text-gray-600">Average Rating</p>
                  </div>
                </div>
              </div>

              <div className="rounded-xl bg-white/80 p-6 shadow-lg backdrop-blur-sm">
                <div className="flex items-center space-x-3">
                  <div className="rounded-full bg-purple-100 p-3">
                    <Users className="h-6 w-6 text-purple-600" />
                  </div>
                  <div>
                    <p className="text-2xl font-bold text-gray-900">
                      {Math.round(
                        (stats.averageAttendance / stats.capacity) * 100
                      )}
                      %
                    </p>
                    <p className="text-sm text-gray-600">Fill Rate</p>
                  </div>
                </div>
              </div>
            </div>

            {/* Content Grid */}
            <div className="grid gap-8 lg:grid-cols-2">
              {/* Stadium Information */}
              <div className="rounded-xl bg-white/80 p-6 shadow-lg backdrop-blur-sm">
                <h2 className="mb-4 text-xl font-bold text-gray-900">
                  Stadium Information
                </h2>
                <div className="space-y-4 text-gray-700">
                  <p>
                    {stadium.name} is a premier football stadium known for its
                    excellent facilities and atmosphere. With a capacity of{' '}
                    {stats.capacity.toLocaleString()}, it serves as a home
                    ground for major football events and concerts.
                  </p>
                  <p>
                    The stadium features state-of-the-art technology and has
                    undergone recent renovations to enhance the fan experience
                    while maintaining its historic character and charm.
                  </p>
                  <div className="grid grid-cols-2 gap-4 border-t pt-4">
                    <div>
                      <p className="text-sm font-medium text-gray-500">
                        Surface Type
                      </p>
                      <p className="text-gray-900">{stats.surfaceType}</p>
                    </div>
                    <div>
                      <p className="text-sm font-medium text-gray-500">
                        Opened
                      </p>
                      <p className="text-gray-900">
                        {stadium.builtDate || 'Unknown'}
                      </p>
                    </div>
                  </div>
                </div>
              </div>

              {/* Facilities */}
              <div className="rounded-xl bg-white/80 p-6 shadow-lg backdrop-blur-sm">
                <h2 className="mb-4 text-xl font-bold text-gray-900">
                  Facilities & Amenities
                </h2>
                <div className="grid grid-cols-1 gap-4">
                  {facilities.map((facility) => (
                    <div
                      key={facility.id}
                      className="flex items-center space-x-3 rounded-lg bg-gray-50/50 p-3"
                    >
                      <div
                        className={`rounded-full p-2 ${facility.available ? 'bg-green-100 text-green-600' : 'bg-gray-100 text-gray-400'}`}
                      >
                        {facility.icon}
                      </div>
                      <div className="flex-1">
                        <h3 className="font-medium text-gray-900">
                          {facility.name}
                        </h3>
                        <p className="text-sm text-gray-600">
                          {facility.description}
                        </p>
                      </div>
                      <div
                        className={`rounded-full px-2 py-1 text-xs font-medium ${
                          facility.available
                            ? 'bg-green-100 text-green-800'
                            : 'bg-gray-100 text-gray-600'
                        }`}
                      >
                        {facility.available ? 'Available' : 'N/A'}
                      </div>
                    </div>
                  ))}
                </div>
              </div>

              {/* Recent Events */}
              <div className="rounded-xl bg-white/80 p-6 shadow-lg backdrop-blur-sm lg:col-span-2">
                <h2 className="mb-4 text-xl font-bold text-gray-900">
                  Recent Events
                </h2>
                <div className="grid gap-4 md:grid-cols-3">
                  {recentEvents.map((event) => (
                    <div
                      key={event.id}
                      className="rounded-lg border border-gray-200 p-4"
                    >
                      <div className="mb-2 flex items-center justify-between">
                        <span
                          className={`rounded-full px-2 py-1 text-xs font-medium ${
                            event.type === 'match'
                              ? 'bg-green-100 text-green-800'
                              : event.type === 'concert'
                                ? 'bg-purple-100 text-purple-800'
                                : 'bg-blue-100 text-blue-800'
                          }`}
                        >
                          {event.type.charAt(0).toUpperCase() +
                            event.type.slice(1)}
                        </span>
                        <span className="text-sm text-gray-500">
                          {new Date(event.date).toLocaleDateString()}
                        </span>
                      </div>
                      <h3 className="mb-1 font-medium text-gray-900">
                        {event.title}
                      </h3>
                      <div className="flex items-center space-x-2 text-sm text-gray-600">
                        <Users className="h-4 w-4" />
                        <span>
                          {event.attendance.toLocaleString()} attendees
                        </span>
                      </div>
                    </div>
                  ))}
                </div>
              </div>
            </div>
          </div>
        </div>
      </SidebarLayout>
    </ProtectedRoute>
  );
}

function StadiumDetailSidebar({ isAdmin }: { isAdmin: boolean }) {
  return (
    <>
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
        <SidebarItem icon={<User size={20} />} text="Players" />
      </Link>
      <Link href="/coaches">
        <SidebarItem icon={<Users size={20} />} text="Coaches" />
      </Link>
      <Link href="/stadiums">
        <SidebarItem icon={<Home size={20} />} text="Stadiums" active />
      </Link>
      {isAdmin && (
        <>
          <SidebarSection title="Admin" color="text-amber-600" />
          <Link href="/admin">
            <SidebarItem icon={<Settings size={20} />} text="Admin Dashboard" />
          </Link>{' '}
        </>
      )}
      <Link href="/notifications">
        <SidebarItem icon={<Bell size={20} />} text="Notifications" />
      </Link>
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
      <div className="absolute inset-0 bg-gradient-to-br from-green-50 via-blue-50 to-indigo-50"></div>

      <div className="absolute inset-0 opacity-[0.03]">
        <Image
          src="/images/Stadium dark.png"
          alt="Stadium Background"
          fill
          className="object-cover object-center"
        />
      </div>

      <div className="absolute inset-0 overflow-hidden">
        <div className="animate-float absolute top-20 left-20 h-2 w-2 rounded-full bg-green-400/20"></div>
        <div className="animate-float-delayed absolute top-40 right-32 h-3 w-3 rounded-full bg-blue-400/15"></div>
        <div className="animate-float-slow absolute bottom-32 left-40 h-1 w-1 rounded-full bg-indigo-400/25"></div>
        <div className="animate-float-delayed absolute right-20 bottom-20 h-2 w-2 rounded-full bg-green-300/20"></div>
      </div>
    </div>
  );
}
