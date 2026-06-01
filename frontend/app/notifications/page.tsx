'use client';

import React, { useState, useEffect, useMemo } from 'react';
import {
  Bell,
  Home,
  ClubIcon,
  Users,
  Package,
  Settings,
  User,
  Search,
  Filter,
  Archive,
  Trash2,
  CheckCircle2,
  Circle,
  Eye,
  EyeOff,
  Star,
  Clock,
  TrendingUp,
  Activity,
  Zap,
  AlertTriangle,
  Info,
  ChevronDown,
  X,
  Menu,
  Grid3X3,
  List,
  Calendar,
  SortAsc,
  SortDesc,
  RefreshCw,
} from 'lucide-react';
import ProtectedRoute from '@/app/Components/ProtectedRoute/ProtectedRoute';
import { SidebarLayout } from '@/app/Components/Sidebar/Sidebar';
import { SidebarItem } from '@/app/Components/Sidebar/SidebarItem';
import Navbar from '@/app/Components/Navbar/Navbar';
import { useSettings } from '@/app/contexts/EnhancedSettingsContext';

// Custom hooks
import { useNotifications } from './hooks/useNotifications';
import { useNotificationFilters } from './hooks/useNotificationFilters';
import { useNotificationSelection } from './hooks/useNotificationSelection';
import { NotificationType } from '@/Services/NotificationService';

// Components
import { NotificationHeader } from './components/NotificationHeader';
import { NotificationFilters } from './components/NotificationFilters';
import { NotificationActions } from './components/NotificationActions';
import { NotificationList } from './components/NotificationList';
import { LoadingState } from './components/LoadingState';

const NotificationsPage: React.FC = () => {
  const [isMounted, setIsMounted] = useState(false);
  const [viewMode, setViewMode] = useState<'grid' | 'list'>('list');
  const [sortBy, setSortBy] = useState<'date' | 'priority' | 'type'>('date');
  const [sortOrder, setSortOrder] = useState<'asc' | 'desc'>('desc');
  const [showFilters, setShowFilters] = useState(false);
  const [selectedCategory, setSelectedCategory] = useState<string>('all');

  // Get dark mode from Enhanced Settings Context
  const { isDarkMode } = useSettings();

  // Custom hooks
  const {
    notifications,
    loading,
    isLoading,
    unreadCount,
    hasMore,
    markAsRead,
    markAllAsRead,
    deleteNotification,
    deleteAllNotifications,
    handleNotificationClick,
    loadMore,
    handleBulkDelete,
  } = useNotifications();

  const {
    filteredNotifications,
    searchQuery,
    filter,
    setSearchQuery,
    setFilter,
    resetFilters,
  } = useNotificationFilters(notifications);

  const {
    selectedNotifications,
    toggleNotificationSelection,
    toggleSelectAll,
    deleteSelectedNotifications,
    isLoading: selectionLoading,
  } = useNotificationSelection();

  // Handle client-side mounting to prevent hydration errors
  useEffect(() => {
    setIsMounted(true);
  }, []);

  // Handle bulk delete with proper callbacks
  const handleDeleteSelected = async () => {
    const deletedIds = await deleteSelectedNotifications();
    if (deletedIds.length > 0) {
      handleBulkDelete(deletedIds);
    }
  };

  // Calculate notification counts by category (memoized for performance)
  const categoryStats = useMemo(() => {
    const importantCount = notifications.filter((n) =>
      [
        NotificationType.SystemAlert,
        NotificationType.Warning,
        NotificationType.Error,
      ].includes(n.type as NotificationType)
    ).length;

    const matchCount = notifications.filter((n) =>
      [
        NotificationType.MatchStart,
        NotificationType.MatchEnd,
        NotificationType.MatchUpdate,
      ].includes(n.type as NotificationType)
    ).length;

    const systemCount = notifications.filter(
      (n) => n.type === NotificationType.SystemAlert
    ).length;

    return { importantCount, matchCount, systemCount };
  }, [notifications]);

  // Notification categories for quick filtering
  const categories = [
    { id: 'all', label: 'All', icon: Bell, count: notifications.length },
    { id: 'unread', label: 'Unread', icon: Circle, count: unreadCount },
    {
      id: 'important',
      label: 'Important',
      icon: Star,
      count: categoryStats.importantCount,
    },
    {
      id: 'match',
      label: 'Matches',
      icon: Activity,
      count: categoryStats.matchCount,
    },
    {
      id: 'system',
      label: 'System',
      icon: Settings,
      count: categoryStats.systemCount,
    },
  ];

  // Calculate actual statistics from notifications (memoized for performance)
  const stats = useMemo(() => {
    const calculateThisWeekCount = () => {
      const oneWeekAgo = new Date();
      oneWeekAgo.setDate(oneWeekAgo.getDate() - 7);

      return notifications.filter((notification) => {
        const notificationDate = new Date(notification.time);
        return notificationDate >= oneWeekAgo;
      }).length;
    };

    const calculateReadRate = () => {
      if (notifications.length === 0) return '0%';
      const readCount = notifications.filter((n) => n.isRead).length;
      const readRate = Math.round((readCount / notifications.length) * 100);
      return `${readRate}%`;
    };

    const calculateTrend = (current: number, context: string) => {
      // Since we don't have historical data, we'll provide contextual information
      if (context === 'total') {
        return current > 50
          ? 'High activity'
          : current > 20
            ? 'Moderate'
            : 'Low activity';
      } else if (context === 'unread') {
        return current > 10
          ? 'Needs attention'
          : current > 0
            ? 'Some pending'
            : 'All caught up';
      } else if (context === 'week') {
        return current > 20
          ? 'Very active'
          : current > 10
            ? 'Active'
            : 'Quiet week';
      } else if (context === 'read') {
        const rate = parseInt(calculateReadRate());
        return rate > 80
          ? 'Excellent'
          : rate > 60
            ? 'Good'
            : 'Needs improvement';
      }
      return '';
    };

    const thisWeekCount = calculateThisWeekCount();
    const readRate = calculateReadRate();

    // Statistics for dashboard cards
    return [
      {
        label: 'Total Notifications',
        value: notifications.length,
        icon: Bell,
        color: 'blue',
        trend: calculateTrend(notifications.length, 'total'),
      },
      {
        label: 'Unread Messages',
        value: unreadCount,
        icon: Circle,
        color: 'red',
        trend: calculateTrend(unreadCount, 'unread'),
      },
      {
        label: 'This Week',
        value: thisWeekCount,
        icon: TrendingUp,
        color: 'green',
        trend: calculateTrend(thisWeekCount, 'week'),
      },
      {
        label: 'Read Rate',
        value: readRate,
        icon: CheckCircle2,
        color: 'purple',
        trend: calculateTrend(0, 'read'),
      },
    ];
  }, [notifications, unreadCount]);

  if (loading) {
    return (
      <ProtectedRoute allowedRoles={['User', 'Admin', 'Coach', 'Player']}>
        <div
          className={`min-h-screen w-full transition-all duration-500 ${
            // <-- FIX 1: 'flex' removed
            isMounted && isDarkMode
              ? 'bg-gradient-to-br from-gray-900 via-gray-800 to-gray-900'
              : 'bg-gradient-to-br from-gray-50 via-white to-gray-100'
          }`}
          suppressHydrationWarning
        >
          <SidebarLayout
            sidebar={
              <>
                <SidebarItem
                  icon={<Home />}
                  text="Dashboard"
                  href="/dashboard"
                />
                <SidebarItem icon={<ClubIcon />} text="Teams" href="/teams" />
                <SidebarItem icon={<Users />} text="Players" href="/players" />
                <SidebarItem
                  icon={<Package />}
                  text="Seasons"
                  href="/seasons"
                />
                <SidebarItem
                  icon={<Bell />}
                  text="Notifications"
                  href="/notifications"
                  active
                />
                <SidebarItem icon={<Search />} text="Search" href="/search" />
                <SidebarItem
                  icon={<Settings />}
                  text="Settings"
                  href="/settings"
                />
                <SidebarItem icon={<User />} text="Profile" href="/profile" />
              </>
            }
          >
            <Navbar />
            <div className="flex min-h-screen items-center justify-center">
              <div className="flex flex-col items-center space-y-4">
                <div className="animate-spin">
                  <RefreshCw className="h-12 w-12 text-blue-500" />
                </div>
                <div
                  className={`text-lg font-medium ${isDarkMode ? 'text-white' : 'text-gray-900'}`}
                >
                  Loading notifications...
                </div>
                <div
                  className={`text-sm ${isDarkMode ? 'text-gray-400' : 'text-gray-500'}`}
                >
                  Please wait while we fetch your latest updates
                </div>
              </div>
            </div>
          </SidebarLayout>
        </div>
      </ProtectedRoute>
    );
  }

  return (
    <ProtectedRoute allowedRoles={['User', 'Admin', 'Coach', 'Player']}>
      <div
        className={`min-h-screen w-full transition-all duration-500 ${
          // <-- FIX 2: 'flex' removed
          isMounted && isDarkMode
            ? 'bg-gradient-to-br from-gray-900 via-gray-800 to-gray-900'
            : 'bg-gradient-to-br from-gray-50 via-white to-gray-100'
        }`}
        suppressHydrationWarning
      >
        <SidebarLayout
          sidebar={
            <>
              <SidebarItem icon={<Home />} text="Dashboard" href="/dashboard" />
              <SidebarItem icon={<ClubIcon />} text="Teams" href="/teams" />
              <SidebarItem icon={<Users />} text="Players" href="/players" />
              <SidebarItem icon={<Package />} text="Seasons" href="/seasons" />
              <SidebarItem
                icon={<Bell />}
                text="Notifications"
                href="/notifications"
                active
              />
              <SidebarItem icon={<Search />} text="Search" href="/search" />
              <SidebarItem
                icon={<Settings />}
                text="Settings"
                href="/settings"
              />
              <SidebarItem icon={<User />} text="Profile" href="/profile" />
            </>
          }
        >
          <Navbar />

          {/* Main Content Area */}
          <div className="flex-1 overflow-hidden">
            <div className="flex h-full flex-col">
              {/* Header Section */}
              <div
                className={`border-b transition-colors duration-300 ${
                  isDarkMode
                    ? 'border-gray-700 bg-gray-800/50'
                    : 'border-gray-200 bg-white/50'
                } backdrop-blur-sm`}
              >
                <div className="px-6 py-4">
                  <div className="flex items-center justify-between">
                    <div className="flex items-center space-x-4">
                      <div
                        className={`rounded-xl p-3 ${
                          isDarkMode ? 'bg-blue-900/50' : 'bg-blue-100'
                        }`}
                      >
                        <Bell
                          className={`h-6 w-6 ${
                            isDarkMode ? 'text-blue-400' : 'text-blue-600'
                          }`}
                        />
                      </div>
                      <div>
                        <h1
                          className={`text-2xl font-bold ${
                            isDarkMode ? 'text-white' : 'text-gray-900'
                          }`}
                        >
                          Notifications
                        </h1>
                        <p
                          className={`text-sm ${
                            isDarkMode ? 'text-gray-400' : 'text-gray-600'
                          }`}
                        >
                          Stay updated with your latest activities
                        </p>
                      </div>
                    </div>

                    {/* Header Actions */}
                    <div className="flex items-center space-x-3">
                      <button
                        onClick={() => setShowFilters(!showFilters)}
                        className={`rounded-lg p-2 transition-all duration-200 ${
                          isDarkMode
                            ? 'text-gray-400 hover:bg-gray-700 hover:text-white'
                            : 'text-gray-600 hover:bg-gray-100 hover:text-gray-900'
                        } ${showFilters ? (isDarkMode ? 'bg-gray-700 text-white' : 'bg-gray-100 text-gray-900') : ''}`}
                      >
                        <Filter className="h-5 w-5" />
                      </button>

                      <div className="flex overflow-hidden rounded-lg border border-gray-300 dark:border-gray-600">
                        <button
                          onClick={() => setViewMode('list')}
                          className={`p-2 transition-colors duration-200 ${
                            viewMode === 'list'
                              ? isDarkMode
                                ? 'bg-blue-600 text-white'
                                : 'bg-blue-500 text-white'
                              : isDarkMode
                                ? 'text-gray-400 hover:bg-gray-700'
                                : 'text-gray-600 hover:bg-gray-100'
                          }`}
                        >
                          <List className="h-4 w-4" />
                        </button>
                        <button
                          onClick={() => setViewMode('grid')}
                          className={`p-2 transition-colors duration-200 ${
                            viewMode === 'grid'
                              ? isDarkMode
                                ? 'bg-blue-600 text-white'
                                : 'bg-blue-500 text-white'
                              : isDarkMode
                                ? 'text-gray-400 hover:bg-gray-700'
                                : 'text-gray-600 hover:bg-gray-100'
                          }`}
                        >
                          <Grid3X3 className="h-4 w-4" />
                        </button>
                      </div>
                    </div>
                  </div>
                </div>
              </div>

              {/* Statistics Cards */}
              <div className="px-6 py-4">
                <div className="grid grid-cols-1 gap-4 md:grid-cols-2 lg:grid-cols-4">
                  {stats.map((stat, index) => (
                    <div
                      key={index}
                      className={`rounded-xl p-4 transition-all duration-300 hover:scale-105 ${
                        isDarkMode
                          ? 'border border-gray-700 bg-gray-800/50'
                          : 'border border-gray-200 bg-white shadow-sm hover:shadow-md'
                      }`}
                    >
                      <div className="flex items-center justify-between">
                        <div>
                          <p
                            className={`text-sm font-medium ${
                              isDarkMode ? 'text-gray-400' : 'text-gray-600'
                            }`}
                          >
                            {stat.label}
                          </p>
                          <p
                            className={`text-2xl font-bold ${
                              isDarkMode ? 'text-white' : 'text-gray-900'
                            }`}
                          >
                            {stat.value}
                          </p>
                          <p
                            className={`text-xs font-medium ${isDarkMode ? 'text-gray-400' : 'text-gray-500'}`}
                          >
                            {stat.trend}
                          </p>
                        </div>
                        <div
                          className={`rounded-lg p-3 bg-${stat.color}-100 dark:bg-${stat.color}-900/50`}
                        >
                          <stat.icon
                            className={`h-6 w-6 text-${stat.color}-600 dark:text-${stat.color}-400`}
                          />
                        </div>
                      </div>
                    </div>
                  ))}
                </div>
              </div>

              {/* Filter Panel */}
              {showFilters && (
                <div
                  className={`mx-6 mb-4 rounded-xl border transition-all duration-300 ${
                    isDarkMode
                      ? 'border-gray-700 bg-gray-800/50'
                      : 'border-gray-200 bg-white shadow-sm'
                  }`}
                >
                  <div className="p-4">
                    <div className="flex flex-wrap items-center gap-4">
                      {/* Search */}
                      <div className="min-w-64 flex-1">
                        <div className="relative">
                          <Search
                            className={`absolute top-1/2 left-3 h-4 w-4 -translate-y-1/2 transform ${
                              isDarkMode ? 'text-gray-400' : 'text-gray-500'
                            }`}
                          />
                          <input
                            type="text"
                            placeholder="Search notifications..."
                            value={searchQuery}
                            onChange={(e) => setSearchQuery(e.target.value)}
                            className={`w-full rounded-lg border py-2 pr-4 pl-10 transition-colors duration-200 ${
                              isDarkMode
                                ? 'border-gray-600 bg-gray-700 text-white placeholder-gray-400 focus:border-blue-500'
                                : 'border-gray-300 bg-gray-50 text-gray-900 placeholder-gray-500 focus:border-blue-500'
                            } focus:ring-2 focus:ring-blue-500/20 focus:outline-none`}
                          />
                        </div>
                      </div>

                      {/* Sort */}
                      <div className="flex items-center space-x-2">
                        <label
                          className={`text-sm font-medium ${
                            isDarkMode ? 'text-gray-300' : 'text-gray-700'
                          }`}
                        >
                          Sort by:
                        </label>
                        <select
                          value={sortBy}
                          onChange={(e) =>
                            setSortBy(
                              e.target.value as 'date' | 'priority' | 'type'
                            )
                          }
                          className={`rounded-lg border px-3 py-1 text-sm ${
                            isDarkMode
                              ? 'border-gray-600 bg-gray-700 text-white'
                              : 'border-gray-300 bg-white text-gray-900'
                          } focus:ring-2 focus:ring-blue-500/20 focus:outline-none`}
                        >
                          <option value="date">Date</option>
                          <option value="priority">Priority</option>
                          <option value="type">Type</option>
                        </select>
                        <button
                          onClick={() =>
                            setSortOrder(sortOrder === 'asc' ? 'desc' : 'asc')
                          }
                          className={`rounded p-1 hover:bg-gray-100 dark:hover:bg-gray-700 ${
                            isDarkMode ? 'text-gray-400' : 'text-gray-600'
                          }`}
                        >
                          {sortOrder === 'asc' ? (
                            <SortAsc className="h-4 w-4" />
                          ) : (
                            <SortDesc className="h-4 w-4" />
                          )}
                        </button>
                      </div>
                    </div>
                  </div>
                </div>
              )}

              {/* Category Tabs */}
              <div className="mb-4 px-6">
                <div className="flex space-x-2 overflow-x-auto">
                  {categories.map((category) => (
                    <button
                      key={category.id}
                      onClick={() => setSelectedCategory(category.id)}
                      className={`flex items-center space-x-2 rounded-lg px-4 py-2 text-sm font-medium whitespace-nowrap transition-all duration-200 ${
                        selectedCategory === category.id
                          ? isDarkMode
                            ? 'bg-blue-600 text-white'
                            : 'bg-blue-500 text-white'
                          : isDarkMode
                            ? 'bg-gray-800 text-gray-300 hover:bg-gray-700'
                            : 'bg-gray-100 text-gray-700 hover:bg-gray-200'
                      }`}
                    >
                      <category.icon className="h-4 w-4" />
                      <span>{category.label}</span>
                      {category.count > 0 && (
                        <span
                          className={`rounded-full px-2 py-0.5 text-xs ${
                            selectedCategory === category.id
                              ? 'bg-white/20 text-white'
                              : isDarkMode
                                ? 'bg-gray-700 text-gray-300'
                                : 'bg-gray-200 text-gray-600'
                          }`}
                        >
                          {category.count}
                        </span>
                      )}
                    </button>
                  ))}
                </div>
              </div>

              {/* Action Bar */}
              <div className="mb-4 px-6">
                <div
                  className={`flex items-center justify-between rounded-lg p-3 ${
                    isDarkMode ? 'bg-gray-800/50' : 'bg-gray-50'
                  }`}
                >
                  <div className="flex items-center space-x-3">
                    <button
                      onClick={() => toggleSelectAll(filteredNotifications)}
                      className={`flex items-center space-x-2 rounded-lg px-3 py-1 text-sm transition-colors duration-200 ${
                        isDarkMode
                          ? 'text-gray-400 hover:bg-gray-700 hover:text-white'
                          : 'text-gray-600 hover:bg-gray-200 hover:text-gray-900'
                      }`}
                    >
                      <CheckCircle2 className="h-4 w-4" />
                      <span>Select All</span>
                    </button>

                    {selectedNotifications.size > 0 && (
                      <div className="flex items-center space-x-2">
                        <span
                          className={`text-sm ${
                            isDarkMode ? 'text-gray-400' : 'text-gray-600'
                          }`}
                        >
                          {selectedNotifications.size} selected
                        </span>
                        <button
                          onClick={handleDeleteSelected}
                          disabled={selectionLoading}
                          className="flex items-center space-x-1 rounded-lg bg-red-500 px-3 py-1 text-sm text-white transition-colors duration-200 hover:bg-red-600 disabled:opacity-50"
                        >
                          <Trash2 className="h-4 w-4" />
                          <span>Delete</span>
                        </button>
                      </div>
                    )}
                  </div>

                  <div className="flex items-center space-x-2">
                    <button
                      onClick={markAllAsRead}
                      className={`flex items-center space-x-2 rounded-lg px-3 py-1 text-sm transition-colors duration-200 ${
                        isDarkMode
                          ? 'text-gray-400 hover:bg-gray-700 hover:text-white'
                          : 'text-gray-600 hover:bg-gray-200 hover:text-gray-900'
                      }`}
                    >
                      <Eye className="h-4 w-4" />
                      <span>Mark All Read</span>
                    </button>

                    <button
                      onClick={resetFilters}
                      className={`flex items-center space-x-2 rounded-lg px-3 py-1 text-sm transition-colors duration-200 ${
                        isDarkMode
                          ? 'text-gray-400 hover:bg-gray-700 hover:text-white'
                          : 'text-gray-600 hover:bg-gray-200 hover:text-gray-900'
                      }`}
                    >
                      <RefreshCw className="h-4 w-4" />
                      <span>Reset</span>
                    </button>
                  </div>
                </div>
              </div>

              {/* Notifications Content */}
              <div className="notification-scrollbar flex-1 overflow-auto px-6 pb-6">
                <div
                  className={
                    viewMode === 'grid'
                      ? 'grid grid-cols-1 gap-4 md:grid-cols-2 lg:grid-cols-3'
                      : 'space-y-2'
                  }
                >
                  <NotificationList
                    notifications={notifications}
                    filteredNotifications={filteredNotifications}
                    selectedNotifications={selectedNotifications}
                    loading={loading}
                    hasMore={hasMore}
                    onToggleSelection={toggleNotificationSelection}
                    onMarkAsRead={markAsRead}
                    onDelete={deleteNotification}
                    onClick={handleNotificationClick}
                    onLoadMore={loadMore}
                    onResetFilters={resetFilters}
                    isDarkMode={isDarkMode}
                    isMounted={isMounted}
                  />
                </div>
              </div>
            </div>
          </div>
        </SidebarLayout>
      </div>
    </ProtectedRoute>
  );
};

export default NotificationsPage;
