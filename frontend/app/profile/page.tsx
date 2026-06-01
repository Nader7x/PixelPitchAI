'use client';

import { ProfileCard } from '@/app/Components/ProfileCard/ProfileCard';
import Sidebar from '@/app/Components/Sidebar/Sidebar';
import { SidebarItem } from '@/app/Components/Sidebar/SidebarItem';
import {
  Calendar,
  ClubIcon,
  LayoutDashboardIcon,
  LogOutIcon,
  Package,
  Settings,
} from 'lucide-react';
import Navbar from '@/app/Components/Navbar/Navbar';
import { useEffect, useState } from 'react';
import { useRouter } from 'next/navigation';
import { motion, AnimatePresence } from 'framer-motion';
import authService, {
  UserProfile,
  UpdateUserRequest,
} from '@/Services/AuthenticationService';
import ProtectedRoute from '@/app/Components/ProtectedRoute/ProtectedRoute';
import { useSettings } from '@/app/contexts/EnhancedSettingsContext';
import './profile.css';

interface UserStorage {
  userId: string;
  username: string;
  email: string;
  accessToken: string;
  refreshToken: string;
}

export default function ProfilePage() {
  const [profile, setProfile] = useState<UserProfile | null>(null);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');
  const [isMounted, setIsMounted] = useState(false);
  const router = useRouter();

  // Get dark mode from Enhanced Settings Context
  const { isDarkMode } = useSettings();

  // Handle client-side mounting to prevent hydration errors
  useEffect(() => {
    setIsMounted(true);
  }, []);

  useEffect(() => {
    const fetchProfile = async () => {
      try {
        if (!authService.isAuthenticated()) {
          console.log('User not authenticated, redirecting to login');
          router.push('/login');
          return;
        }
        const user: UserStorage =
          typeof window !== 'undefined' && localStorage.getItem('user')
            ? JSON.parse(localStorage.getItem('user') || '')
            : null;
        if (!user?.userId) {
          console.log('User ID not found, redirecting to login');
          router.push('/login');
          return;
        }
        const userProfile = await authService.getUserProfile(user.userId);
        setProfile(userProfile);
      } catch (err) {
        setError('Failed to load profile.');
        console.error('Profile fetch error:', err);
      } finally {
        setLoading(false);
      }
    };
    fetchProfile().then();
  }, [router]);

  const handleLogout = () => {
    authService.logout();
    router.push('/login');
  };

  const handleProfileSave = async (updatedProfile: any, avatarFile?: File) => {
    if (!profile) return { success: false, message: 'No profile loaded' };

    try {
      setSaving(true);

      // First upload avatar if provided
      if (avatarFile) {
        try {
          const avatarUpdateData: UpdateUserRequest = {
            Id: profile.userId,
            Image: avatarFile,
          };
          const uploadResult =
            await authService.updateProfile(avatarUpdateData);

          // Update the profile with the new image URL if upload was successful
          if (uploadResult && uploadResult.imageUrl) {
            // Update local state with the new image URL from Azure storage
            setProfile((prev) =>
              prev
                ? {
                    ...prev,
                    imageUrl: uploadResult.imageUrl,
                  }
                : null
            );
          }
        } catch (error) {
          console.error('Avatar upload error:', error);
          return { success: false, message: 'Failed to upload avatar image.' };
        }
      }

      // Then update other profile fields if changed
      const updateData = {
        username:
          updatedProfile.username !== profile.username
            ? updatedProfile.username
            : undefined,
        email:
          updatedProfile.email !== profile.email
            ? updatedProfile.email
            : undefined,
        favoriteTeamId:
          updatedProfile.favoriteTeamId !== profile.favoriteTeamId
            ? updatedProfile.favoriteTeamId
            : undefined,
        age:
          updatedProfile.age !== profile.age ? updatedProfile.age : undefined,
        gender:
          updatedProfile.gender !== profile.gender
            ? updatedProfile.gender
            : undefined,
      };
      console.log(profile.userId, 'profile.userId');
      // Only include fields that have changed
      const filteredUpdateData: UpdateUserRequest = {
        Id: profile.userId,
        ...Object.fromEntries(
          Object.entries(updateData).filter(([_, v]) => v !== undefined)
        ),
      };

      // Only send update request if there are changes
      if (Object.keys(filteredUpdateData).length > 1) {
        console.log('Updating profile with data:', filteredUpdateData);

        await authService.updateProfile(filteredUpdateData);

        // Refresh profile data after update
        if (profile.userId) {
          const refreshedProfile = await authService.getUserProfile(
            profile.userId
          );
          setProfile(refreshedProfile);
        }
      }

      return { success: true };
    } catch (error: any) {
      console.error('Profile update error:', error);
      return {
        success: false,
        message: error.message || 'Failed to update profile.',
      };
    } finally {
      setSaving(false);
    }
  };

  return (
    <ProtectedRoute allowedRoles={['User', 'Admin', 'Coach', 'Player']}>
      <div
        className={`profile-page-container relative min-h-screen overflow-hidden transition-colors duration-300 ${
          isMounted && isDarkMode ? 'bg-gray-900' : ''
        }`}
        suppressHydrationWarning
      >
        {/* Replace existing background with football field */}
        <div
          className={`profile-background fixed inset-0 z-0 ${
            isMounted && isDarkMode ? 'bg-gray-900' : ''
          }`}
        >
          {/* Overlay to soften the background */}
          <div
            className={`absolute inset-0 transition-colors duration-300 ${
              isMounted && isDarkMode
                ? 'bg-opacity-70 bg-gray-900'
                : 'bg-opacity-40 bg-white'
            }`}
          ></div>
        </div>

        <div className="relative z-10 flex min-h-screen">
          <AnimatePresence>
            <motion.div
              initial={{ x: -100, opacity: 0 }}
              animate={{ x: 0, opacity: 1 }}
              transition={{ duration: 0.5, ease: 'easeOut' }}
              className="fixed top-0 left-0 z-20 h-full w-16"
            >
              <Sidebar>
                {/* Fixed nested anchor issue by using div wrappers with onClick */}
                <div onClick={() => router.push('/dashboard')}>
                  <SidebarItem
                    icon={<LayoutDashboardIcon size={20} />}
                    text="Dashboard"
                  />
                </div>{' '}
                <div onClick={() => router.push('/teams')}>
                  <SidebarItem icon={<ClubIcon size={20} />} text="Teams" />
                </div>
                <div onClick={() => router.push('/products')}>
                  <SidebarItem
                    icon={<Package size={20} />}
                    text="Products"
                    alert
                  />
                </div>
                <div onClick={() => router.push('/settings')}>
                  <SidebarItem icon={<Settings size={20} />} text="Settings" />
                </div>
                <div onClick={handleLogout}>
                  <SidebarItem icon={<LogOutIcon size={20} />} text="Logout" />
                </div>
              </Sidebar>
            </motion.div>
          </AnimatePresence>

          <div className="ml-16 flex-1 transition-all duration-300 ease-in-out">
            <AnimatePresence>
              <motion.div
                initial={{ y: -20, opacity: 0 }}
                animate={{ y: 0, opacity: 1 }}
                transition={{ duration: 0.3, delay: 0.1 }}
                className="w-full"
              >
                <Navbar />
              </motion.div>
            </AnimatePresence>

            <AnimatePresence>
              {loading ? (
                <motion.div
                  className="flex h-[80vh] items-center justify-center"
                  initial={{ opacity: 0 }}
                  animate={{ opacity: 1 }}
                  exit={{ opacity: 0 }}
                >
                  <div
                    className={`loader ${isMounted && isDarkMode ? 'dark-mode' : ''}`}
                  >
                    <svg viewBox="0 0 80 80">
                      <circle
                        cx="40"
                        cy="40"
                        r="32"
                        className="loader-circle-bg"
                      ></circle>
                      <circle
                        cx="40"
                        cy="40"
                        r="32"
                        className="loader-circle"
                      ></circle>
                    </svg>
                  </div>
                </motion.div>
              ) : error ? (
                <motion.div
                  className="flex h-[80vh] flex-col items-center justify-center px-4"
                  initial={{ scale: 0.9, opacity: 0 }}
                  animate={{ scale: 1, opacity: 1 }}
                  transition={{ duration: 0.4 }}
                >
                  <div className="error-icon mb-4">❌</div>
                  <div
                    className={`mb-4 text-center font-medium ${
                      isMounted && isDarkMode ? 'text-red-400' : 'text-red-500'
                    }`}
                  >
                    {error}
                  </div>
                  <motion.button
                    onClick={() => router.push('/login')}
                    className="rounded-lg bg-gradient-to-r from-amber-500 to-amber-600 px-6 py-3 text-white shadow-md transition-all hover:from-amber-600 hover:to-amber-700 hover:shadow-lg"
                    whileHover={{ scale: 1.05 }}
                    whileTap={{ scale: 0.95 }}
                  >
                    Return to Login
                  </motion.button>
                </motion.div>
              ) : profile ? (
                <motion.div
                  className="my-4 flex flex-col items-center justify-center p-4 md:my-8"
                  initial={{ y: 20, opacity: 0 }}
                  animate={{ y: 0, opacity: 1 }}
                  transition={{ duration: 0.5, delay: 0.2 }}
                >
                  <div className="profile-card-container">
                    <ProfileCard
                      initialProfile={{
                        avatarUrl: profile.imageUrl || '/default-avatar.jpeg',
                        username: profile.username,
                        email: profile.email,
                        age: profile.age,
                        gender: profile.gender,
                        favoriteTeam: profile.favoriteTeamName,
                        favoriteTeamId: profile.favoriteTeamId,
                      }}
                      onProfileSave={handleProfileSave}
                      isLoading={saving}
                      isDarkMode={isMounted && isDarkMode}
                    />
                  </div>

                  <motion.div
                    className="mt-8 mb-8"
                    initial={{ opacity: 0 }}
                    animate={{ opacity: 1 }}
                    transition={{ delay: 0.5 }}
                  >
                    <div onClick={() => router.push('/dashboard')}>
                      <motion.button
                        className="flex items-center space-x-2 rounded-lg bg-gradient-to-r from-amber-500 to-amber-600 px-6 py-3 text-white shadow-md transition-all hover:shadow-lg"
                        whileHover={{ scale: 1.05 }}
                        whileTap={{ scale: 0.95 }}
                      >
                        <LayoutDashboardIcon size={18} />
                        <span>Back to Dashboard</span>
                      </motion.button>
                    </div>
                  </motion.div>
                </motion.div>
              ) : null}
            </AnimatePresence>
          </div>
        </div>
      </div>
    </ProtectedRoute>
  );
}
