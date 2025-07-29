'use client';
import { SidebarLayout } from '../Components/Sidebar/Sidebar';
import { SidebarItem } from '../Components/Sidebar/SidebarItem';
import { SidebarSection } from '../Components/Sidebar/SidebarSection';
import DashboardContent from '../Components/DashboardContent/DashboardContent';
import {
  Settings,
  Calendar,
  Package,
  LayoutDashboardIcon,
  LogOutIcon,
  ClubIcon,
  Bell,
  Users,
  User,
  Search,
  Home,
  BarChart3,
} from 'lucide-react';
import Link from 'next/link';
import { useEffect, useState } from 'react';
import { useRouter } from 'next/navigation';
import authService from '@/Services/AuthenticationService';
import Image from 'next/image';
import ProtectedRoute from '@/app/Components/ProtectedRoute/ProtectedRoute';
import { useSettings } from '../contexts/EnhancedSettingsContext';

// Force dynamic rendering to prevent prerender errors
export const dynamic = 'force-dynamic';

export default function Dashboard() {
  const [isAdmin, setIsAdmin] = useState(false);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [currentUserId, setCurrentUserId] = useState<string | null>(null);
  const [isMounted, setIsMounted] = useState(false);
  const router = useRouter();

  // Use settings context for theme and other preferences
  const { isDarkMode, playSound } = useSettings();

  // Handle client-side mounting to prevent hydration errors
  useEffect(() => {
    setIsMounted(true);
  }, []);

  useEffect(() => {
    // Check if user is authenticated
    if (!authService.isAuthenticated()) {
      router.push('/login');
      return;
    }

    // Get current user ID
    const userId = authService.getCurrentUserId();
    setCurrentUserId(userId);

    // Fetch initial data
    fetchDashboardData().then(() => {});

    // Set up token refresh interval
    const refreshInterval = setInterval(
      async () => {
        await authService.checkAndRefreshToken();
      },
      5 * 60 * 1000
    ); // Check every 5 minutes

    return () => clearInterval(refreshInterval);
  }, [router]);

  const fetchDashboardData = async () => {
    setIsLoading(true);
    setError(null);

    try {
      checkUserRole();
    } catch (err) {
      console.error('Error fetching dashboard data:', err);
      setError('Failed to load dashboard data. Please try again.');
    } finally {
      setIsLoading(false);
    }
  };

  const checkUserRole = () => {
    // Use the auth service to check if user has admin role
    setIsAdmin(authService.hasRole('Admin'));
  };

  const handleLogout = () => {
    authService.logout();
    router.push('/login');
  };

  if (isLoading) {
    return (
      <div
        className={`relative flex h-screen items-center justify-center overflow-hidden ${
          isMounted && isDarkMode
            ? 'bg-gradient-to-br from-gray-900 via-gray-800 to-slate-900'
            : 'bg-gradient-to-br from-emerald-900 via-green-800 to-teal-900'
        }`}
        suppressHydrationWarning
      >
        {/* Animated Background Elements */}
        <div className="absolute inset-0">
          {/* Floating Football Elements */}
          <div className="absolute top-10 left-10 h-4 w-4 animate-ping rounded-full bg-white/20"></div>
          <div className="absolute top-32 right-20 h-6 w-6 animate-pulse rounded-full bg-white/15 delay-300"></div>
          <div className="absolute bottom-20 left-32 h-3 w-3 animate-bounce rounded-full bg-white/25 delay-500"></div>
          <div className="absolute right-16 bottom-40 h-5 w-5 animate-ping rounded-full bg-white/10 delay-700"></div>

          {/* Animated Lines */}
          <div className="absolute top-0 left-1/4 h-full w-px animate-pulse bg-gradient-to-b from-transparent via-white/10 to-transparent"></div>
          <div className="absolute top-0 right-1/3 h-full w-px animate-pulse bg-gradient-to-b from-transparent via-white/5 to-transparent delay-1000"></div>
        </div>

        {/* Main Loading Content */}
        <div className="relative z-10 flex flex-col items-center space-y-6">
          {/* Football Icon with Enhanced Animation */}
          <div className="relative">
            <div className="absolute inset-0 animate-ping rounded-full bg-white/20"></div>
            <div className="relative rounded-full bg-white/10 p-8 backdrop-blur-sm">
              <div className="animate-spin-slow">
                <Image
                  src="/images/football-pattern.png"
                  alt="Loading"
                  width={100}
                  height={100}
                  className="animate-pulse"
                />
              </div>
            </div>
          </div>

          {/* Loading Text */}
          <div className="text-center">
            <h2 className="mb-2 animate-pulse text-2xl font-bold text-white">
              Loading Dashboard
            </h2>
            <div className="flex items-center space-x-1">
              <div className="h-2 w-2 animate-bounce rounded-full bg-white"></div>
              <div className="h-2 w-2 animate-bounce rounded-full bg-white delay-100"></div>
              <div className="h-2 w-2 animate-bounce rounded-full bg-white delay-200"></div>
            </div>
          </div>

          {/* Progress Bar */}
          <div className="h-1 w-64 overflow-hidden rounded-full bg-white/20">
            <div className="h-full animate-pulse rounded-full bg-gradient-to-r from-white/60 to-white/80"></div>
          </div>
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div
        className={`flex h-screen items-center justify-center ${
          isMounted && isDarkMode
            ? 'bg-gradient-to-br from-gray-900 to-gray-800'
            : 'bg-gradient-to-br from-green-800 to-green-900'
        }`}
        suppressHydrationWarning
      >
        <div
          className={`rounded-lg border-l-4 border-red-500 p-6 shadow-lg ${
            isMounted && isDarkMode
              ? 'bg-gray-800 text-red-400'
              : 'bg-white text-red-700'
          }`}
          suppressHydrationWarning
        >
          <p className="mb-2 text-xl font-bold">Error</p>
          <p className="mb-4">{error}</p>
          <button
            onClick={fetchDashboardData}
            className="rounded-lg bg-red-500 px-4 py-2 text-white shadow transition-colors hover:bg-red-600"
          >
            Try Again
          </button>
        </div>
      </div>
    );
  }

  return (
    <ProtectedRoute allowedRoles={['User', 'Admin', 'Coach', 'Player']}>
      <SidebarLayout
        sidebar={
          <>
            {/* Main Navigation */}
            <Link href="/dashboard">
              <SidebarItem
                icon={<LayoutDashboardIcon size={20} />}
                text="Dashboard"
                active
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
            </Link>{' '}
            <Link href="/stadiums">
              <SidebarItem icon={<Home size={20} />} text="Stadiums" />
            </Link>
            {/* Admin Section - only show if user is admin */}
            {isAdmin && (
              <>
                <SidebarSection title="Admin" color="text-amber-600" />
                <Link href="/admin">
                  <SidebarItem
                    icon={<Settings size={20} />}
                    text="Admin Dashboard"
                  />
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
        }
      >
        {/* Enhanced Creative Background */}
        <div className="fixed inset-0 z-0">
          {/* Base Gradient */}
          <div
            className={`absolute inset-0 ${
              isDarkMode
                ? 'bg-gradient-to-br from-gray-900 via-gray-800 to-slate-900'
                : 'bg-gradient-to-br from-emerald-50 via-green-50 to-teal-50'
            }`}
          ></div>

          {/* Stadium Silhouette Background */}
          <div className="absolute inset-0 opacity-[0.03]">
            <Image
              src="/images/Stadium dark.png"
              alt="Stadium Background"
              fill
              className="object-cover object-center"
            />
          </div>

          {/* Geometric Pattern Overlay */}
          <div className="absolute inset-0 opacity-[0.02]">
            <div
              className="h-full w-full"
              style={{
                backgroundImage: `
              radial-gradient(circle at 25% 25%, rgba(34, 197, 94, 0.1) 0%, transparent 50%),
              radial-gradient(circle at 75% 75%, rgba(16, 185, 129, 0.1) 0%, transparent 50%),
              linear-gradient(45deg, transparent 40%, rgba(34, 197, 94, 0.05) 50%, transparent 60%)
            `,
              }}
            ></div>
          </div>

          {/* Subtle Football Pattern */}
          <div className="absolute inset-0 opacity-[0.015]">
            <Image
              src="/images/football-pattern.png"
              alt="Football Pattern"
              fill
              className="object-cover"
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

        {/* Main content that can now access sidebar context */}
        <DashboardContent />
      </SidebarLayout>
    </ProtectedRoute>
  );
}
