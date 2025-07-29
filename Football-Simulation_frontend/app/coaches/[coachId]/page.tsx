'use client';
import { useState, useEffect } from 'react';
import { useRouter, useParams } from 'next/navigation';
import Link from 'next/link';
import Image from 'next/image';
import {
  ArrowLeft,
  Calendar,
  Trophy,
  Users,
  Star,
  Clock,
  Target,
  TrendingUp,
  Award,
  ClubIcon,
  Bell,
  User,
  Package,
  Home,
  LayoutDashboardIcon,
  Settings,
  Search,
  Flag,
  Briefcase,
} from 'lucide-react';
import { SidebarLayout } from '../../Components/Sidebar/Sidebar';
import { SidebarItem } from '../../Components/Sidebar/SidebarItem';
import { SidebarSection } from '../../Components/Sidebar/SidebarSection';
import ProtectedRoute from '@/app/Components/ProtectedRoute/ProtectedRoute';
import coachService, { Coach } from '@/Services/CoachService';
import authService from '@/Services/AuthenticationService';

interface CoachStats {
  matchesCoached: number;
  winRate: number;
  trophiesWon: number;
  playersCoached: number;
  averageRating: number;
  experienceYears: number;
}

interface Achievement {
  id: string;
  title: string;
  description: string;
  date: string;
  type: 'trophy' | 'milestone' | 'award';
}

export default function CoachDetailPage() {
  const params = useParams();
  const coachId = params.coachId as string;
  const [coach, setCoach] = useState<Coach | null>(null);
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
    fetchCoachDetails();
  }, [router, coachId]);

  const fetchCoachDetails = async () => {
    try {
      setLoading(true);
      setError(null);
      const coachData = await coachService.getCoachById(parseInt(coachId));
      setCoach(coachData);
    } catch (err) {
      console.error('Error fetching coach details:', err);
      setError('Failed to load coach details. Please try again.');
    } finally {
      setLoading(false);
    }
  };

  const getCoachStats = (coach: Coach): CoachStats => {
    // Mock enhanced stats - replace with actual data from your API
    return {
      matchesCoached: Math.floor(Math.random() * 200) + 50,
      winRate: Math.floor(Math.random() * 40) + 50,
      trophiesWon: Math.floor(Math.random() * 10) + 1,
      playersCoached: Math.floor(Math.random() * 100) + 20,
      averageRating: Math.random() * 2 + 3,
      experienceYears:
        coach.yearsOfExperience || Math.floor(Math.random() * 20) + 5,
    };
  };

  const getAchievements = (): Achievement[] => {
    return [
      {
        id: '1',
        title: 'League Champion',
        description: 'Won the National League Championship',
        date: '2024',
        type: 'trophy',
      },
      {
        id: '2',
        title: '100 Matches Milestone',
        description: 'Coached 100 professional matches',
        date: '2023',
        type: 'milestone',
      },
      {
        id: '3',
        title: 'Coach of the Year',
        description: 'Named Coach of the Year by Football Association',
        date: '2023',
        type: 'award',
      },
    ];
  };

  if (loading) {
    return (
      <ProtectedRoute allowedRoles={['User', 'Admin', 'Coach', 'Player']}>
        <SidebarLayout sidebar={<CoachDetailSidebar isAdmin={isAdmin} />}>
          <div className="relative flex h-screen items-center justify-center">
            <BackgroundElements />
            <div className="relative z-10 flex flex-col items-center space-y-6">
              <div className="h-16 w-16 animate-spin rounded-full border-4 border-green-500 border-t-transparent"></div>
              <h2 className="text-2xl font-bold text-gray-800">
                Loading Coach Details...
              </h2>
            </div>
          </div>
        </SidebarLayout>
      </ProtectedRoute>
    );
  }

  if (error || !coach) {
    return (
      <ProtectedRoute allowedRoles={['User', 'Admin', 'Coach', 'Player']}>
        <SidebarLayout sidebar={<CoachDetailSidebar isAdmin={isAdmin} />}>
          <div className="relative flex h-screen items-center justify-center">
            <BackgroundElements />
            <div className="relative z-10 text-center">
              <div className="rounded-lg border-l-4 border-red-500 bg-white p-6 text-red-700 shadow-lg">
                <p className="mb-2 text-xl font-bold">Error</p>
                <p className="mb-4">{error || 'Coach not found'}</p>
                <div className="space-x-4">
                  <button
                    onClick={fetchCoachDetails}
                    className="rounded-lg bg-red-500 px-4 py-2 text-white shadow transition-colors hover:bg-red-600"
                  >
                    Try Again
                  </button>
                  <Link
                    href="/coaches"
                    className="rounded-lg bg-gray-500 px-4 py-2 text-white shadow transition-colors hover:bg-gray-600"
                  >
                    Back to Coaches
                  </Link>
                </div>
              </div>
            </div>
          </div>
        </SidebarLayout>
      </ProtectedRoute>
    );
  }

  const stats = getCoachStats(coach);
  const achievements = getAchievements();

  return (
    <ProtectedRoute allowedRoles={['User', 'Admin', 'Coach', 'Player']}>
      <SidebarLayout sidebar={<CoachDetailSidebar isAdmin={isAdmin} />}>
        <div className="relative min-h-screen">
          <BackgroundElements />

          <div className="relative z-10 p-6">
            {/* Header with Back Button */}
            <div className="mb-6">
              <Link
                href="/coaches"
                className="inline-flex items-center space-x-2 text-gray-600 transition-colors hover:text-gray-800"
              >
                <ArrowLeft className="h-5 w-5" />
                <span>Back to Coaches</span>
              </Link>
            </div>

            {/* Coach Profile Header */}
            <div className="mb-8 rounded-xl bg-white/80 p-8 shadow-lg backdrop-blur-sm">
              <div className="flex flex-col gap-6 lg:flex-row">
                <div className="flex-shrink-0">
                  <div className="relative h-32 w-32 rounded-full bg-gradient-to-br from-blue-400 to-blue-600 p-1">
                    <div className="flex h-full w-full items-center justify-center rounded-full bg-white">
                      {coach.photoUrl ? (
                        <Image
                          src={coach.photoUrl}
                          alt={`${coach.firstName} ${coach.lastName}`}
                          width={120}
                          height={120}
                          className="rounded-full object-cover"
                        />
                      ) : (
                        <Users className="h-16 w-16 text-blue-600" />
                      )}
                    </div>
                  </div>
                </div>

                <div className="flex-1 space-y-4">
                  <div>
                    <h1 className="text-3xl font-bold text-gray-900">
                      {coach.firstName} {coach.lastName}
                    </h1>
                    <div className="mt-2 flex flex-wrap gap-4">
                      <span className="inline-flex items-center rounded-full bg-blue-100 px-3 py-1 text-sm font-medium text-blue-800">
                        <Briefcase className="mr-1 h-4 w-4" />
                        {coach.role || 'Head Coach'}
                      </span>
                      <span className="inline-flex items-center rounded-full bg-green-100 px-3 py-1 text-sm font-medium text-green-800">
                        <Clock className="mr-1 h-4 w-4" />
                        {stats.experienceYears} years experience
                      </span>
                    </div>
                  </div>

                  <div className="grid grid-cols-1 gap-4 md:grid-cols-3">
                    <div className="flex items-center space-x-2 text-gray-600">
                      <Flag className="h-5 w-5 text-gray-400" />
                      <span>{coach.nationality || 'Not specified'}</span>
                    </div>
                    <div className="flex items-center space-x-2 text-gray-600">
                      <Calendar className="h-5 w-5 text-gray-400" />
                      <span>
                        Age{' '}
                        {coach.dateOfBirth
                          ? `${new Date().getFullYear() - new Date(coach.dateOfBirth).getFullYear()} years`
                          : 'Not specified'}
                      </span>
                    </div>
                    <div className="flex items-center space-x-2 text-gray-600">
                      <Star className="h-5 w-5 text-yellow-500" />
                      <span>{stats.averageRating.toFixed(1)} Rating</span>
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
                    <Target className="h-6 w-6 text-blue-600" />
                  </div>
                  <div>
                    <p className="text-2xl font-bold text-gray-900">
                      {stats.matchesCoached}
                    </p>
                    <p className="text-sm text-gray-600">Matches Coached</p>
                  </div>
                </div>
              </div>

              <div className="rounded-xl bg-white/80 p-6 shadow-lg backdrop-blur-sm">
                <div className="flex items-center space-x-3">
                  <div className="rounded-full bg-green-100 p-3">
                    <TrendingUp className="h-6 w-6 text-green-600" />
                  </div>
                  <div>
                    <p className="text-2xl font-bold text-gray-900">
                      {stats.winRate}%
                    </p>
                    <p className="text-sm text-gray-600">Win Rate</p>
                  </div>
                </div>
              </div>

              <div className="rounded-xl bg-white/80 p-6 shadow-lg backdrop-blur-sm">
                <div className="flex items-center space-x-3">
                  <div className="rounded-full bg-yellow-100 p-3">
                    <Trophy className="h-6 w-6 text-yellow-600" />
                  </div>
                  <div>
                    <p className="text-2xl font-bold text-gray-900">
                      {stats.trophiesWon}
                    </p>
                    <p className="text-sm text-gray-600">Trophies Won</p>
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
                      {stats.playersCoached}
                    </p>
                    <p className="text-sm text-gray-600">Players Coached</p>
                  </div>
                </div>
              </div>
            </div>

            {/* Content Grid */}
            <div className="grid gap-8 lg:grid-cols-2">
              {/* Biography */}
              <div className="rounded-xl bg-white/80 p-6 shadow-lg backdrop-blur-sm">
                <h2 className="mb-4 text-xl font-bold text-gray-900">
                  Biography
                </h2>
                <div className="space-y-4 text-gray-700">
                  <p>
                    {coach.firstName} {coach.lastName} is a seasoned football
                    coach with {stats.experienceYears} years of experience in
                    professional football management. Known for their tactical
                    innovation and player development skills.
                  </p>
                  <p>
                    Throughout their career, they have demonstrated exceptional
                    leadership qualities and have been instrumental in
                    developing young talent while maintaining competitive team
                    performance.
                  </p>
                  <div className="grid grid-cols-2 gap-4 border-t pt-4">
                    <div>
                      <p className="text-sm font-medium text-gray-500">
                        Coaching Style
                      </p>
                      <p className="text-gray-900">
                        {coach.coachingStyle || 'Tactical & Adaptive'}
                      </p>
                    </div>
                    <div>
                      <p className="text-sm font-medium text-gray-500">
                        Specialization
                      </p>
                      <p className="text-gray-900">
                        {coach.role || 'Player Development'}
                      </p>
                    </div>
                  </div>
                </div>
              </div>

              {/* Achievements */}
              <div className="rounded-xl bg-white/80 p-6 shadow-lg backdrop-blur-sm">
                <h2 className="mb-4 text-xl font-bold text-gray-900">
                  Achievements
                </h2>
                <div className="space-y-4">
                  {achievements.map((achievement) => (
                    <div
                      key={achievement.id}
                      className="flex items-start space-x-3"
                    >
                      <div className="rounded-full bg-yellow-100 p-2">
                        {achievement.type === 'trophy' && (
                          <Trophy className="h-4 w-4 text-yellow-600" />
                        )}
                        {achievement.type === 'milestone' && (
                          <Target className="h-4 w-4 text-blue-600" />
                        )}
                        {achievement.type === 'award' && (
                          <Award className="h-4 w-4 text-purple-600" />
                        )}
                      </div>
                      <div className="flex-1">
                        <h3 className="font-medium text-gray-900">
                          {achievement.title}
                        </h3>
                        <p className="text-sm text-gray-600">
                          {achievement.description}
                        </p>
                        <p className="mt-1 text-xs text-gray-500">
                          {achievement.date}
                        </p>
                      </div>
                    </div>
                  ))}
                </div>
              </div>

              {/* Career Timeline */}
              <div className="rounded-xl bg-white/80 p-6 shadow-lg backdrop-blur-sm lg:col-span-2">
                <h2 className="mb-4 text-xl font-bold text-gray-900">
                  Career Timeline
                </h2>
                <div className="space-y-6">
                  <div className="flex items-start space-x-4">
                    <div className="rounded-full bg-green-100 p-2">
                      <ClubIcon className="h-4 w-4 text-green-600" />
                    </div>
                    <div className="flex-1">
                      <h3 className="font-medium text-gray-900">
                        Current Position
                      </h3>
                      <p className="text-sm text-gray-600">
                        {coach.role || 'Head Coach'} - Present
                      </p>
                      <p className="mt-1 text-xs text-gray-500">
                        Leading the team with innovative tactics and player
                        development focus
                      </p>
                    </div>
                  </div>

                  <div className="flex items-start space-x-4">
                    <div className="rounded-full bg-blue-100 p-2">
                      <Award className="h-4 w-4 text-blue-600" />
                    </div>
                    <div className="flex-1">
                      <h3 className="font-medium text-gray-900">
                        Professional Coaching Career
                      </h3>
                      <p className="text-sm text-gray-600">
                        Started{' '}
                        {new Date().getFullYear() - stats.experienceYears}
                      </p>
                      <p className="mt-1 text-xs text-gray-500">
                        Began professional coaching career after successful
                        playing experience
                      </p>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </SidebarLayout>
    </ProtectedRoute>
  );
}

function CoachDetailSidebar({ isAdmin }: { isAdmin: boolean }) {
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
        <SidebarItem icon={<Users size={20} />} text="Coaches" active />
      </Link>
      <Link href="/stadiums">
        <SidebarItem icon={<Home size={20} />} text="Stadiums" />
      </Link>
      {isAdmin && (
        <>
          <SidebarSection title="Admin" color="text-amber-600" />
          <Link href="/admin">
            <SidebarItem icon={<Settings size={20} />} text="Admin Dashboard" />
          </Link>
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
      <div className="absolute inset-0 bg-gradient-to-br from-blue-50 via-indigo-50 to-purple-50"></div>

      <div className="absolute inset-0 opacity-[0.03]">
        <Image
          src="/images/Stadium dark.png"
          alt="Stadium Background"
          fill
          className="object-cover object-center"
        />
      </div>

      <div className="absolute inset-0 overflow-hidden">
        <div className="animate-float absolute top-20 left-20 h-2 w-2 rounded-full bg-blue-400/20"></div>
        <div className="animate-float-delayed absolute top-40 right-32 h-3 w-3 rounded-full bg-indigo-400/15"></div>
        <div className="animate-float-slow absolute bottom-32 left-40 h-1 w-1 rounded-full bg-purple-400/25"></div>
        <div className="animate-float-delayed absolute right-20 bottom-20 h-2 w-2 rounded-full bg-blue-300/20"></div>
      </div>
    </div>
  );
}
