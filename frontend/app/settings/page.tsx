'use client';
import { useState, useEffect } from 'react';
import { useRouter } from 'next/navigation';
import Link from 'next/link';
import {
  Settings as SettingsIcon,
  Calendar,
  Package,
  LayoutDashboardIcon,
  ClubIcon,
  Bell,
  Users,
  User,
  Search,
  Home,
  Save,
  Moon,
  Sun,
  Volume2,
  VolumeX,
  Monitor,
  Smartphone,
  Globe,
  Shield,
  Key,
  Camera,
  Mail,
  Palette,
  Zap,
  Eye,
  EyeOff,
  RefreshCw,
} from 'lucide-react';
import { SidebarLayout } from '../Components/Sidebar/Sidebar';
import { SidebarItem } from '../Components/Sidebar/SidebarItem';
import { SidebarSection } from '../Components/Sidebar/SidebarSection';
import ProtectedRoute from '@/app/Components/ProtectedRoute/ProtectedRoute';
import authService from '@/Services/AuthenticationService';
import { useSettings } from '../contexts/EnhancedSettingsContext';
import { useTranslations } from 'next-intl';
import toast from 'react-hot-toast';
import SettingsTest from '../Components/SettingsTest/SettingsTest';

// Force dynamic rendering to prevent prerender errors
export const dynamic = 'force-dynamic';

// Background Elements Component
function BackgroundElements() {
  return (
    <div className="fixed inset-0 -z-10 overflow-hidden">
      {/* Stadium background */}
      <div className="absolute inset-0 bg-gradient-to-br from-green-900 via-green-800 to-emerald-900">
        <div className="bg-[url('/images/Stadium dark.png')] absolute inset-0 bg-cover bg-center opacity-20"></div>
        <div className="absolute inset-0 bg-gradient-to-t from-black/30 to-transparent"></div>
      </div>

      {/* Floating football elements */}
      <div className="absolute top-20 left-10 h-8 w-8 animate-bounce rounded-full bg-white/10"></div>
      <div className="absolute top-40 right-20 h-6 w-6 animate-pulse rounded-full bg-green-400/20"></div>
      <div className="animate-float absolute bottom-32 left-1/4 h-10 w-10 rounded-full bg-emerald-300/15"></div>
      <div className="absolute top-60 right-1/3 h-12 w-12 animate-pulse rounded-full bg-white/5"></div>
    </div>
  );
}

export default function SettingsPage() {
  const [isAdmin, setIsAdmin] = useState(false);
  const [activeTab, setActiveTab] = useState('general');
  const router = useRouter();

  // Use the Enhanced SettingsContext and next-intl
  const {
    settings,
    updateSetting,
    saveSettings,
    resetToDefaults,
    hasChanges,
    isLoading,
    playSound,
    setTheme,
    isDarkMode,
    currentTheme,
    currentLocale,
    changeLanguage,
  } = useSettings();

  // Next-intl translations
  const t = useTranslations();

  const [saving, setSaving] = useState(false);

  useEffect(() => {
    // Check authentication
    if (!authService.isAuthenticated()) {
      router.push('/login');
      return;
    }

    checkUserRole();
  }, [router]);

  const checkUserRole = () => {
    const userInfo = authService.getCurrentUser();
    setIsAdmin(userInfo?.role === 'Admin');
  };

  const handleSaveSettings = async () => {
    setSaving(true);
    try {
      await saveSettings();
      playSound('success');
      toast.success(t('settings.saved'));
    } catch (error) {
      console.error('Error saving settings:', error);
      playSound('error');
      toast.error('Failed to save settings');
    } finally {
      setSaving(false);
    }
  };

  const handleResetToDefaults = () => {
    resetToDefaults();
    playSound('click');
    toast.success(t('settings.reset'));
  };

  const ToggleSwitch = ({
    checked,
    onChange,
    disabled = false,
  }: {
    checked: boolean;
    onChange: (checked: boolean) => void;
    disabled?: boolean;
  }) => (
    <label className="relative inline-flex cursor-pointer items-center">
      <input
        type="checkbox"
        checked={checked}
        onChange={(e) => onChange(e.target.checked)}
        disabled={disabled}
        className="peer sr-only"
      />
      <div
        className={`peer h-6 w-11 rounded-full transition-colors duration-200 ${checked ? 'bg-green-600 dark:bg-green-500' : 'bg-gray-300 dark:bg-gray-600'} ${disabled ? 'cursor-not-allowed opacity-50' : ''} peer-focus:ring-4 peer-focus:ring-green-300 peer-focus:outline-none after:absolute after:top-[2px] after:left-[2px] after:h-5 after:w-5 after:rounded-full after:border after:border-gray-300 after:bg-white after:transition-all after:content-[''] dark:peer-focus:ring-green-800 dark:after:border-gray-600 dark:after:bg-gray-200 ${checked ? 'after:translate-x-full after:border-white dark:after:border-gray-200' : ''} `}
      ></div>
    </label>
  );

  const SelectField = ({
    value,
    onChange,
    options,
    disabled = false,
  }: {
    value: string;
    onChange: (value: string) => void;
    options: { value: string; label: string }[];
    disabled?: boolean;
  }) => (
    <select
      value={value}
      onChange={(e) => onChange(e.target.value)}
      disabled={disabled}
      className={`w-full rounded-lg border border-gray-300 bg-white px-3 py-2 text-gray-700 focus:border-green-500 focus:ring-2 focus:ring-green-200 focus:outline-none dark:border-gray-600 dark:bg-gray-700 dark:text-gray-300 dark:focus:border-green-400 dark:focus:ring-green-800 ${disabled ? 'cursor-not-allowed bg-gray-100 opacity-50 dark:bg-gray-800' : ''} `}
    >
      {options.map((option) => (
        <option key={option.value} value={option.value}>
          {option.label}
        </option>
      ))}
    </select>
  );

  const ThemeSelector = () => (
    <div className="flex gap-2">
      {[
        { value: 'light', label: t('theme.light'), icon: Sun },
        { value: 'dark', label: t('theme.dark'), icon: Moon },
        { value: 'system', label: t('theme.system'), icon: Monitor },
      ].map(({ value, label, icon: Icon }) => (
        <button
          key={value}
          onClick={() => {
            setTheme(value);
            playSound('click');
          }}
          className={`flex flex-col items-center gap-2 rounded-lg border-2 p-3 transition-all ${
            currentTheme === value
              ? 'border-green-500 bg-green-50 text-green-700 dark:bg-green-900/20 dark:text-green-400'
              : 'border-gray-200 bg-white text-gray-600 hover:border-gray-300 dark:border-gray-600 dark:bg-gray-700 dark:text-gray-300'
          }`}
        >
          <Icon size={20} />
          <span className="text-xs font-medium">{label}</span>
        </button>
      ))}
    </div>
  );

  if (isLoading) {
    return (
      <ProtectedRoute allowedRoles={['User', 'Admin', 'Coach', 'Player']}>
        <BackgroundElements />
        <div className="flex min-h-screen items-center justify-center">
          <div className="text-center">
            <RefreshCw className="mx-auto h-12 w-12 animate-spin text-green-600" />
            <p className="mt-4 text-lg font-medium text-white">
              Loading settings...
            </p>
          </div>
        </div>
      </ProtectedRoute>
    );
  }

  return (
    <ProtectedRoute allowedRoles={['User', 'Admin', 'Coach', 'Player']}>
      <BackgroundElements />

      <SidebarLayout
        sidebar={
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
                    icon={<SettingsIcon size={20} />}
                    text="Admin Dashboard"
                  />
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
              <SidebarItem
                icon={<SettingsIcon size={20} />}
                text="Settings"
                active
              />
            </Link>
          </>
        }
      >
        <SettingsTest />
        <div className="min-h-screen p-6">
          <div className="mx-auto max-w-6xl">
            {/* Header */}
            <div className="mb-8">
              <div className="rounded-xl border border-white/20 bg-white/90 p-6 shadow-xl backdrop-blur-sm dark:border-gray-600/20 dark:bg-gray-800/90">
                <div className="flex items-center justify-between">
                  <div>
                    <h1 className="flex items-center gap-3 text-3xl font-bold text-gray-900 dark:text-white">
                      <SettingsIcon className="text-green-600" size={32} />
                      {t('settings.title')}
                    </h1>
                    <p className="mt-2 text-gray-600 dark:text-gray-300">
                      {t('settings.subtitle')}
                    </p>
                  </div>

                  {hasChanges && (
                    <div className="flex gap-3">
                      <button
                        onClick={handleResetToDefaults}
                        className="rounded-lg border border-gray-300 px-4 py-2 text-gray-700 transition-colors hover:bg-gray-50 dark:border-gray-600 dark:text-gray-300 dark:hover:bg-gray-700"
                      >
                        {t('common.reset')}
                      </button>
                      <button
                        onClick={handleSaveSettings}
                        disabled={saving}
                        className="flex items-center gap-2 rounded-lg bg-green-600 px-6 py-2 text-white transition-colors hover:bg-green-700 disabled:opacity-50"
                      >
                        {saving ? (
                          <RefreshCw size={16} className="animate-spin" />
                        ) : (
                          <Save size={16} />
                        )}
                        {saving ? t('common.loading') : t('common.save')}
                      </button>
                    </div>
                  )}
                </div>
              </div>
            </div>

            <div className="grid grid-cols-1 gap-6 lg:grid-cols-4">
              {/* Settings Navigation */}
              <div className="lg:col-span-1">
                <div className="sticky top-6 rounded-xl border border-white/20 bg-white/90 p-4 shadow-xl backdrop-blur-sm dark:border-gray-600/20 dark:bg-gray-800/90">
                  <nav className="space-y-2">
                    {[
                      {
                        id: 'general',
                        label: t('settings.general'),
                        icon: SettingsIcon,
                      },
                      {
                        id: 'notifications',
                        label: t('settings.notifications'),
                        icon: Bell,
                      },
                      {
                        id: 'privacy',
                        label: t('settings.privacy'),
                        icon: Shield,
                      },
                      {
                        id: 'display',
                        label: t('settings.display'),
                        icon: Monitor,
                      },
                      {
                        id: 'account',
                        label: t('settings.account'),
                        icon: Key,
                      },
                    ].map((tab) => {
                      const Icon = tab.icon;
                      return (
                        <button
                          key={tab.id}
                          onClick={() => setActiveTab(tab.id)}
                          className={`flex w-full items-center gap-3 rounded-lg px-4 py-3 text-left transition-all ${
                            activeTab === tab.id
                              ? 'bg-green-100 font-medium text-green-800 dark:bg-green-900/20 dark:text-green-400'
                              : 'text-gray-700 hover:bg-gray-100 dark:text-gray-300 dark:hover:bg-gray-700'
                          } `}
                        >
                          <Icon size={18} />
                          {tab.label}
                        </button>
                      );
                    })}
                  </nav>
                </div>
              </div>

              {/* Settings Content */}
              <div className="lg:col-span-3">
                <div className="rounded-xl border border-white/20 bg-white/90 p-6 shadow-xl backdrop-blur-sm dark:border-gray-600/20 dark:bg-gray-800/90">
                  {/* General Settings */}
                  {activeTab === 'general' && (
                    <div className="space-y-6">
                      <div>
                        <h2 className="mb-4 flex items-center gap-2 text-xl font-semibold text-gray-900 dark:text-white">
                          <SettingsIcon size={20} />
                          {t('settings.general')}
                        </h2>
                      </div>

                      {/* Theme */}
                      <div className="rounded-lg border border-gray-200 p-4 dark:border-gray-600 dark:bg-gray-800/50">
                        <div className="mb-3 flex items-center justify-between">
                          <div className="flex items-center gap-3">
                            <Palette className="text-purple-600" size={20} />
                            <div>
                              <h3 className="font-medium text-gray-900 dark:text-white">
                                {t('settings.theme')}
                              </h3>
                              <p className="text-sm text-gray-600 dark:text-gray-400">
                                Choose your preferred color scheme
                              </p>
                            </div>
                          </div>
                        </div>
                        <ThemeSelector />
                      </div>

                      {/* Language */}
                      <div className="rounded-lg border border-gray-200 p-4 dark:border-gray-600 dark:bg-gray-800/50">
                        <div className="mb-3 flex items-center justify-between">
                          <div className="flex items-center gap-3">
                            <Globe className="text-blue-600" size={20} />
                            <div>
                              <h3 className="font-medium text-gray-900 dark:text-white">
                                {t('settings.language')}
                              </h3>
                              <p className="text-sm text-gray-600 dark:text-gray-400">
                                Select your preferred language
                              </p>
                            </div>
                          </div>
                        </div>
                        <SelectField
                          value={currentLocale}
                          onChange={(value) => {
                            changeLanguage(value);
                          }}
                          options={[
                            { value: 'en', label: 'English' },
                            { value: 'es', label: 'Español' },
                            { value: 'fr', label: 'Français' },
                          ]}
                        />
                      </div>

                      {/* Timezone */}
                      <div className="rounded-lg border border-gray-200 p-4">
                        <div className="mb-3 flex items-center justify-between">
                          <div className="flex items-center gap-3">
                            <Globe className="text-green-600" size={20} />
                            <div>
                              <h3 className="font-medium text-gray-900">
                                Timezone
                              </h3>
                              <p className="text-sm text-gray-600">
                                Set your local timezone for match schedules
                              </p>
                            </div>
                          </div>
                        </div>
                        <SelectField
                          value={settings.display.timezone}
                          onChange={(value) =>
                            updateSetting('display.timezone', value)
                          }
                          options={[
                            {
                              value: 'UTC',
                              label: 'UTC (Coordinated Universal Time)',
                            },
                            {
                              value: 'America/New_York',
                              label: 'Eastern Time (US & Canada)',
                            },
                            {
                              value: 'America/Chicago',
                              label: 'Central Time (US & Canada)',
                            },
                            {
                              value: 'America/Denver',
                              label: 'Mountain Time (US & Canada)',
                            },
                            {
                              value: 'America/Los_Angeles',
                              label: 'Pacific Time (US & Canada)',
                            },
                            { value: 'Europe/London', label: 'London (GMT)' },
                            {
                              value: 'Europe/Paris',
                              label: 'Central European Time',
                            },
                            {
                              value: 'Asia/Tokyo',
                              label: 'Japan Standard Time',
                            },
                          ]}
                        />
                      </div>

                      {/* Sound */}
                      <div className="rounded-lg border border-gray-200 p-4">
                        <div className="flex items-center justify-between">
                          <div className="flex items-center gap-3">
                            {settings.display.soundEnabled ? (
                              <Volume2 className="text-blue-600" size={20} />
                            ) : (
                              <VolumeX className="text-gray-400" size={20} />
                            )}
                            <div>
                              <h3 className="font-medium text-gray-900">
                                Sound Effects
                              </h3>
                              <p className="text-sm text-gray-600">
                                Enable sound notifications and effects
                              </p>
                            </div>
                          </div>
                          <ToggleSwitch
                            checked={settings.display.soundEnabled}
                            onChange={(checked) =>
                              updateSetting('display.soundEnabled', checked)
                            }
                          />
                        </div>
                      </div>
                    </div>
                  )}

                  {/* Notification Settings */}
                  {activeTab === 'notifications' && (
                    <div className="space-y-6">
                      <div>
                        <h2 className="mb-4 flex items-center gap-2 text-xl font-semibold text-gray-900">
                          <Bell size={20} />
                          Notification Preferences
                        </h2>
                      </div>

                      {/* Match Notifications */}
                      <div className="rounded-lg border border-gray-200 p-4">
                        <h3 className="mb-4 font-medium text-gray-900">
                          Match Notifications
                        </h3>
                        <div className="space-y-4">
                          <div className="flex items-center justify-between">
                            <div>
                              <h4 className="font-medium text-gray-800">
                                Match Start
                              </h4>
                              <p className="text-sm text-gray-600">
                                Get notified when matches begin
                              </p>
                            </div>
                            <ToggleSwitch
                              checked={settings.notifications.matchStart}
                              onChange={(checked) =>
                                updateSetting(
                                  'notifications.matchStart',
                                  checked
                                )
                              }
                            />
                          </div>

                          <div className="flex items-center justify-between">
                            <div>
                              <h4 className="font-medium text-gray-800">
                                Match End
                              </h4>
                              <p className="text-sm text-gray-600">
                                Get notified when matches finish
                              </p>
                            </div>
                            <ToggleSwitch
                              checked={settings.notifications.matchEnd}
                              onChange={(checked) =>
                                updateSetting('notifications.matchEnd', checked)
                              }
                            />
                          </div>

                          <div className="flex items-center justify-between">
                            <div>
                              <h4 className="font-medium text-gray-800">
                                Auto-redirect to Simulation
                              </h4>
                              <p className="text-sm text-gray-600">
                                Automatically navigate to simulation when
                                matches start
                              </p>
                            </div>
                            <ToggleSwitch
                              checked={settings.notifications.autoRedirect}
                              onChange={(checked) =>
                                updateSetting(
                                  'notifications.autoRedirect',
                                  checked
                                )
                              }
                            />
                          </div>
                        </div>
                      </div>

                      {/* System Notifications */}
                      <div className="rounded-lg border border-gray-200 p-4">
                        <h3 className="mb-4 font-medium text-gray-900">
                          System Notifications
                        </h3>
                        <div className="space-y-4">
                          <div className="flex items-center justify-between">
                            <div>
                              <h4 className="font-medium text-gray-800">
                                System Alerts
                              </h4>
                              <p className="text-sm text-gray-600">
                                Important system announcements and maintenance
                              </p>
                            </div>
                            <ToggleSwitch
                              checked={settings.notifications.systemAlerts}
                              onChange={(checked) =>
                                updateSetting(
                                  'notifications.systemAlerts',
                                  checked
                                )
                              }
                            />
                          </div>
                        </div>
                      </div>

                      {/* Delivery Methods */}
                      <div className="rounded-lg border border-gray-200 p-4">
                        <h3 className="mb-4 font-medium text-gray-900">
                          Delivery Methods
                        </h3>
                        <div className="space-y-4">
                          <div className="flex items-center justify-between">
                            <div className="flex items-center gap-3">
                              <Mail className="text-blue-600" size={18} />
                              <div>
                                <h4 className="font-medium text-gray-800">
                                  Email Notifications
                                </h4>
                                <p className="text-sm text-gray-600">
                                  Receive notifications via email
                                </p>
                              </div>
                            </div>
                            <ToggleSwitch
                              checked={settings.notifications.email}
                              onChange={(checked) =>
                                updateSetting('notifications.email', checked)
                              }
                            />
                          </div>

                          <div className="flex items-center justify-between">
                            <div className="flex items-center gap-3">
                              <Smartphone
                                className="text-green-600"
                                size={18}
                              />
                              <div>
                                <h4 className="font-medium text-gray-800">
                                  Push Notifications
                                </h4>
                                <p className="text-sm text-gray-600">
                                  Browser push notifications
                                </p>
                              </div>
                            </div>
                            <ToggleSwitch
                              checked={settings.notifications.push}
                              onChange={(checked) =>
                                updateSetting('notifications.push', checked)
                              }
                            />
                          </div>
                        </div>
                      </div>
                    </div>
                  )}

                  {/* Privacy Settings */}
                  {activeTab === 'privacy' && (
                    <div className="space-y-6">
                      <div>
                        <h2 className="mb-4 flex items-center gap-2 text-xl font-semibold text-gray-900">
                          <Shield size={20} />
                          Privacy Settings
                        </h2>
                      </div>

                      <div className="rounded-lg border border-gray-200 p-4">
                        <h3 className="mb-4 font-medium text-gray-900">
                          Profile Visibility
                        </h3>
                        <div className="space-y-4">
                          <div className="flex items-center justify-between">
                            <div className="flex items-center gap-3">
                              <Eye className="text-blue-600" size={18} />
                              <div>
                                <h4 className="font-medium text-gray-800">
                                  Public Profile
                                </h4>
                                <p className="text-sm text-gray-600">
                                  Allow others to view your profile
                                </p>
                              </div>
                            </div>
                            <ToggleSwitch
                              checked={settings.privacy.profileVisible}
                              onChange={(checked) =>
                                updateSetting('privacy.profileVisible', checked)
                              }
                            />
                          </div>

                          <div className="flex items-center justify-between">
                            <div className="flex items-center gap-3">
                              <Zap className="text-green-600" size={18} />
                              <div>
                                <h4 className="font-medium text-gray-800">
                                  Online Status
                                </h4>
                                <p className="text-sm text-gray-600">
                                  Show when you're online
                                </p>
                              </div>
                            </div>
                            <ToggleSwitch
                              checked={settings.privacy.showOnlineStatus}
                              onChange={(checked) =>
                                updateSetting(
                                  'privacy.showOnlineStatus',
                                  checked
                                )
                              }
                            />
                          </div>

                          <div className="flex items-center justify-between">
                            <div className="flex items-center gap-3">
                              <Users className="text-purple-600" size={18} />
                              <div>
                                <h4 className="font-medium text-gray-800">
                                  Friend Requests
                                </h4>
                                <p className="text-sm text-gray-600">
                                  Allow others to send friend requests
                                </p>
                              </div>
                            </div>
                            <ToggleSwitch
                              checked={settings.privacy.allowFriendRequests}
                              onChange={(checked) =>
                                updateSetting(
                                  'privacy.allowFriendRequests',
                                  checked
                                )
                              }
                            />
                          </div>
                        </div>
                      </div>
                    </div>
                  )}

                  {/* Display Settings */}
                  {activeTab === 'display' && (
                    <div className="space-y-6">
                      <div>
                        <h2 className="mb-4 flex items-center gap-2 text-xl font-semibold text-gray-900">
                          <Monitor size={20} />
                          Display Settings
                        </h2>
                      </div>

                      <div className="rounded-lg border border-gray-200 p-4">
                        <div className="flex items-center justify-between">
                          <div className="flex items-center gap-3">
                            <Monitor className="text-blue-600" size={20} />
                            <div>
                              <h3 className="font-medium text-gray-900">
                                Compact Mode
                              </h3>
                              <p className="text-sm text-gray-600">
                                Use a more compact interface layout
                              </p>
                            </div>
                          </div>
                          <ToggleSwitch
                            checked={settings.display.compactMode}
                            onChange={(checked) =>
                              updateSetting('display.compactMode', checked)
                            }
                          />
                        </div>
                      </div>

                      <div className="rounded-lg border border-gray-200 bg-blue-50/50 p-4">
                        <div className="flex items-start gap-3">
                          <Monitor className="mt-1 text-blue-600" size={20} />
                          <div>
                            <h3 className="mb-2 font-medium text-gray-900">
                              Display Tips
                            </h3>
                            <ul className="space-y-1 text-sm text-gray-600">
                              <li>
                                • Compact mode reduces padding and spacing for
                                more content visibility
                              </li>
                              <li>
                                • Theme changes will apply to all parts of the
                                application
                              </li>
                              <li>
                                • Language changes require a page refresh to
                                take full effect
                              </li>
                            </ul>
                          </div>
                        </div>
                      </div>
                    </div>
                  )}

                  {/* Account Security */}
                  {activeTab === 'account' && (
                    <div className="space-y-6">
                      <div>
                        <h2 className="mb-4 flex items-center gap-2 text-xl font-semibold text-gray-900">
                          <Key size={20} />
                          Account Security
                        </h2>
                      </div>

                      <div className="rounded-lg border border-gray-200 p-4">
                        <div className="mb-4 flex items-center justify-between">
                          <div className="flex items-center gap-3">
                            <Shield className="text-green-600" size={20} />
                            <div>
                              <h3 className="font-medium text-gray-900">
                                Two-Factor Authentication
                              </h3>
                              <p className="text-sm text-gray-600">
                                Add an extra layer of security to your account
                              </p>
                            </div>
                          </div>
                          <ToggleSwitch
                            checked={settings.account.twoFactorEnabled}
                            onChange={(checked) =>
                              updateSetting('account.twoFactorEnabled', checked)
                            }
                          />
                        </div>

                        {settings.account.twoFactorEnabled && (
                          <div className="mt-4 rounded-lg border border-green-200 bg-green-50 p-3">
                            <p className="text-sm text-green-800">
                              ✓ Two-factor authentication is enabled. You'll be
                              prompted for a verification code when signing in.
                            </p>
                          </div>
                        )}
                      </div>

                      <div className="rounded-lg border border-gray-200 p-4">
                        <div className="mb-3 flex items-center justify-between">
                          <div className="flex items-center gap-3">
                            <RefreshCw className="text-orange-600" size={20} />
                            <div>
                              <h3 className="font-medium text-gray-900">
                                Session Timeout
                              </h3>
                              <p className="text-sm text-gray-600">
                                Automatically log out after inactivity
                              </p>
                            </div>
                          </div>
                        </div>
                        <SelectField
                          value={settings.account.sessionTimeout.toString()}
                          onChange={(value) =>
                            updateSetting(
                              'account.sessionTimeout',
                              parseInt(value)
                            )
                          }
                          options={[
                            { value: '15', label: '15 minutes' },
                            { value: '30', label: '30 minutes' },
                            { value: '60', label: '1 hour' },
                            { value: '120', label: '2 hours' },
                            { value: '480', label: '8 hours' },
                            { value: '1440', label: '24 hours' },
                          ]}
                        />
                      </div>

                      <div className="rounded-lg border border-orange-200 bg-orange-50 p-4">
                        <div className="flex items-start gap-3">
                          <Shield className="mt-1 text-orange-600" size={20} />
                          <div>
                            <h3 className="mb-2 font-medium text-orange-900">
                              Security Recommendations
                            </h3>
                            <ul className="space-y-1 text-sm text-orange-800">
                              <li>
                                • Enable two-factor authentication for better
                                security
                              </li>
                              <li>
                                • Use a strong, unique password for your account
                              </li>
                              <li>• Review your account activity regularly</li>
                              <li>• Log out from public or shared computers</li>
                            </ul>
                          </div>
                        </div>
                      </div>
                    </div>
                  )}
                </div>
              </div>
            </div>

            {/* Save Button - Fixed at bottom on mobile */}
            {hasChanges && (
              <div className="fixed right-6 bottom-6 lg:hidden">
                <button
                  onClick={handleSaveSettings}
                  disabled={saving}
                  className="flex items-center gap-2 rounded-full bg-green-600 px-6 py-3 text-white shadow-lg transition-all hover:bg-green-700 disabled:opacity-50"
                >
                  {saving ? (
                    <RefreshCw size={18} className="animate-spin" />
                  ) : (
                    <Save size={18} />
                  )}
                  {saving ? t('common.loading') : t('common.save')}
                </button>
              </div>
            )}
          </div>
        </div>
      </SidebarLayout>
    </ProtectedRoute>
  );
}
