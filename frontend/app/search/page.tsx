'use client';
import { useState, useEffect, useCallback, Suspense } from 'react';
import { useRouter, useSearchParams } from 'next/navigation';
import Link from 'next/link';
import Image from 'next/image';
import { motion, AnimatePresence } from 'framer-motion';
import {
  Search as SearchIcon,
  Filter,
  Clock,
  TrendingUp,
  Users,
  Home,
  User,
  Calendar,
  Package,
  Star,
  MapPin,
  Trophy,
  ClubIcon,
  LayoutDashboardIcon,
  Bell,
  Settings,
  X,
  ChevronDown,
  SortAsc,
  SortDesc,
  Grid3x3,
  List,
  BarChart3,
  Loader2,
  AlertCircle,
} from 'lucide-react';
import { SidebarLayout } from '../Components/Sidebar/Sidebar';
import { SidebarItem } from '../Components/Sidebar/SidebarItem';
import { SidebarSection } from '../Components/Sidebar/SidebarSection';
import ProtectedRoute from '@/app/Components/ProtectedRoute/ProtectedRoute';
import {
  SearchAutocomplete,
  SearchResultCard,
  SearchFilters,
  SearchAnalytics,
} from '@/app/Components/Search';
import searchService, {
  GlobalSearchResponse,
  SearchResultItem,
  AdvancedSearchFilters,
  SearchAnalytics as SearchAnalyticsType,
} from '@/Services/SearchService';
import authService from '@/Services/AuthenticationService';
import { useSettings } from '../contexts/EnhancedSettingsContext';
import toast from 'react-hot-toast';

interface SearchState {
  query: string;
  results: SearchResultItem[];
  loading: boolean;
  error: string | null;
  currentPage: number;
  totalPages: number;
  totalResults: number;
  analytics: SearchAnalyticsType | null;
}

// SearchContent component that uses useSearchParams
function SearchContent() {
  const searchParams = useSearchParams();
  const router = useRouter();
  const [isAdmin, setIsAdmin] = useState(false);
  const [isMounted, setIsMounted] = useState(false);
  const [viewMode, setViewMode] = useState<'grid' | 'list'>('grid');
  const [sortBy, setSortBy] = useState<'relevance' | 'name' | 'date'>(
    'relevance'
  );
  const [sortOrder, setSortOrder] = useState<'asc' | 'desc'>('desc');
  const [showFilters, setShowFilters] = useState(false);
  const [showAnalytics, setShowAnalytics] = useState(false);
  const [recentSearches, setRecentSearches] = useState<string[]>([]);

  // Get dark mode from settings
  const { isDarkMode, playSound } = useSettings();

  // Handle client-side mounting
  useEffect(() => {
    setIsMounted(true);
  }, []);

  // Search state
  const [searchState, setSearchState] = useState<SearchState>({
    query: searchParams.get('q') || '',
    results: [],
    loading: false,
    error: null,
    currentPage: 1,
    totalPages: 0,
    totalResults: 0,
    analytics: null,
  });

  // Advanced filters
  const [filters, setFilters] = useState<AdvancedSearchFilters>({
    query: searchParams.get('q') || '',
    entityTypes: [],
    strategy: 'Auto',
    enableFuzzySearch: false,
    page: 1,
    pageSize: 12,
    sortBy: 'relevance',
    sortDescending: true,
  });

  useEffect(() => {
    if (!authService.isAuthenticated()) {
      router.push('/login');
      return;
    }

    setIsAdmin(authService.hasRole('Admin'));
    loadRecentSearches();

    // Perform initial search if query exists
    if (searchState.query) {
      performSearch(searchState.query);
    }
  }, [router]);

  // Update filters when search params change
  useEffect(() => {
    const queryParam = searchParams.get('q');
    if (queryParam && queryParam !== searchState.query) {
      setSearchState((prev) => ({ ...prev, query: queryParam }));
      setFilters((prev) => ({ ...prev, query: queryParam }));
      performSearch(queryParam);
    }
  }, [searchParams]);

  const loadRecentSearches = () => {
    const saved = localStorage.getItem('recentSearches');
    if (saved) {
      setRecentSearches(JSON.parse(saved));
    }
  };

  const saveSearch = (query: string) => {
    if (!query.trim()) return;

    const updated = [query, ...recentSearches.filter((s) => s !== query)].slice(
      0,
      10
    );
    setRecentSearches(updated);
    localStorage.setItem('recentSearches', JSON.stringify(updated));
  };

  const clearRecentSearches = () => {
    setRecentSearches([]);
    localStorage.removeItem('recentSearches');
  };

  const performSearch = useCallback(
    async (query: string, page: number = 1, useFilters: boolean = false) => {
      if (!query.trim()) {
        setSearchState((prev) => ({
          ...prev,
          results: [],
          totalResults: 0,
          totalPages: 0,
          analytics: null,
        }));
        return;
      }

      setSearchState((prev) => ({ ...prev, loading: true, error: null }));

      try {
        saveSearch(query);

        let response: GlobalSearchResponse;
        let analytics: SearchAnalyticsType | null = null;

        if (useFilters) {
          // Use advanced search with filters
          const advancedFilters: AdvancedSearchFilters = {
            ...filters,
            query,
            page,
            sortBy: sortBy === 'relevance' ? undefined : sortBy,
            sortDescending: sortOrder === 'desc',
          };
          response = await searchService.advancedSearch(advancedFilters);
        } else {
          // Use global search
          response = await searchService.globalSearch(
            query,
            page,
            filters.pageSize
          );
        }

        // Get analytics if available
        try {
          analytics = await searchService.getSearchAnalytics(query);
        } catch (error) {
          console.warn('Failed to load search analytics:', error);
        }

        setSearchState((prev) => ({
          ...prev,
          query,
          results: response.items,
          currentPage: response.currentPage,
          totalPages: response.totalPages,
          totalResults: response.totalResults,
          analytics,
          loading: false,
        }));

        // Update URL
        router.replace(`/search?q=${encodeURIComponent(query)}`);
      } catch (error) {
        console.error('Search error:', error);
        setSearchState((prev) => ({
          ...prev,
          loading: false,
          error: error instanceof Error ? error.message : 'Search failed',
        }));
        toast.error('Search failed. Please try again.');
      }
    },
    [filters, sortBy, sortOrder, router]
  );

  const handleSearch = (query: string) => {
    setSearchState((prev) => ({ ...prev, query }));
    setFilters((prev) => ({ ...prev, query }));
    performSearch(query, 1, showFilters);
  };

  const handleFiltersChange = (newFilters: AdvancedSearchFilters) => {
    setFilters(newFilters);
  };

  const handleApplyFilters = () => {
    performSearch(searchState.query, 1, true);
  };

  const handleResetFilters = () => {
    const resetFilters: AdvancedSearchFilters = {
      query: searchState.query,
      entityTypes: [],
      strategy: 'Auto',
      enableFuzzySearch: false,
      page: 1,
      pageSize: 12,
      sortBy: 'relevance',
      sortDescending: true,
    };
    setFilters(resetFilters);
    setSortBy('relevance');
    setSortOrder('desc');
    performSearch(searchState.query, 1, false);
  };

  const handlePageChange = (page: number) => {
    performSearch(searchState.query, page, showFilters);
  };

  const handleSortChange = (newSortBy: typeof sortBy) => {
    if (newSortBy === sortBy) {
      setSortOrder(sortOrder === 'asc' ? 'desc' : 'asc');
    } else {
      setSortBy(newSortBy);
      setSortOrder('desc');
    }
  };

  const getTrendingSearches = () => [
    'Real Madrid',
    'Cristiano Ronaldo',
    'LaLiga',
    'Football boots',
    'Santiago Bernabéu',
    'Lionel Messi',
    'Kylian Mbappé',
    'Barcelona',
  ];

  const renderPagination = () => {
    if (searchState.totalPages <= 1) return null;

    const pages = [];
    const currentPage = searchState.currentPage;
    const totalPages = searchState.totalPages;

    // Always show first page
    if (currentPage > 3) {
      pages.push(1);
      if (currentPage > 4) pages.push('...');
    }

    // Show pages around current page
    for (
      let i = Math.max(1, currentPage - 2);
      i <= Math.min(totalPages, currentPage + 2);
      i++
    ) {
      pages.push(i);
    }

    // Always show last page
    if (currentPage < totalPages - 2) {
      if (currentPage < totalPages - 3) pages.push('...');
      pages.push(totalPages);
    }

    return (
      <div className="mt-8 flex items-center justify-center space-x-2">
        <button
          onClick={() => handlePageChange(currentPage - 1)}
          disabled={currentPage === 1 || searchState.loading}
          className="rounded-lg border border-gray-300 bg-white px-3 py-2 text-sm font-medium text-gray-700 hover:bg-gray-50 disabled:cursor-not-allowed disabled:opacity-50"
        >
          Previous
        </button>
        {pages.map((page, index) => (
          <button
            key={index}
            onClick={() => typeof page === 'number' && handlePageChange(page)}
            disabled={page === '...' || searchState.loading}
            className={`rounded-lg px-3 py-2 text-sm font-medium ${
              page === currentPage
                ? 'bg-blue-600 text-white'
                : 'border border-gray-300 bg-white text-gray-700 hover:bg-gray-50'
            } ${page === '...' ? 'cursor-default' : ''}`}
          >
            {page}
          </button>
        ))}
        <button
          onClick={() => handlePageChange(currentPage + 1)}
          disabled={currentPage === totalPages || searchState.loading}
          className="rounded-lg border border-gray-300 bg-white px-3 py-2 text-sm font-medium text-gray-700 hover:bg-gray-50 disabled:cursor-not-allowed disabled:opacity-50"
        >
          Next
        </button>
      </div>
    );
  };

  return (
    <ProtectedRoute allowedRoles={['User', 'Admin', 'Coach', 'Player']}>
      <SidebarLayout sidebar={<SearchSidebar isAdmin={isAdmin} />}>
        <div
          className={`relative min-h-screen transition-colors duration-300 ${
            isMounted && isDarkMode ? 'bg-gray-900' : 'bg-white'
          }`}
          suppressHydrationWarning
        >
          <BackgroundElements />

          <div className="relative z-10 p-6">
            {/* Header */}
            <div className="mb-8">
              <motion.div
                initial={{ opacity: 0, y: -20 }}
                animate={{ opacity: 1, y: 0 }}
                transition={{ duration: 0.5 }}
                className="mb-6"
              >
                <h1
                  className={`mb-2 text-4xl font-bold transition-colors duration-300 ${
                    isMounted && isDarkMode ? 'text-white' : 'text-gray-900'
                  }`}
                  suppressHydrationWarning
                >
                  Advanced Search
                </h1>
                <p
                  className={`text-lg transition-colors duration-300 ${
                    isMounted && isDarkMode ? 'text-gray-300' : 'text-gray-600'
                  }`}
                  suppressHydrationWarning
                >
                  Find players, teams, stadiums, coaches, and more with powerful
                  search capabilities
                </p>
              </motion.div>

              {/* Search Bar */}
              <motion.div
                initial={{ opacity: 0, y: 20 }}
                animate={{ opacity: 1, y: 0 }}
                transition={{ duration: 0.5, delay: 0.1 }}
                className="mb-6"
              >
                <div className="flex items-center space-x-4">
                  <div className="flex-1">
                    <SearchAutocomplete
                      value={searchState.query}
                      onSearch={handleSearch}
                      placeholder="Search for players, teams, stadiums, coaches..."
                      className="w-full"
                      showHistory={true}
                      showTrending={true}
                      maxSuggestions={8}
                    />
                  </div>

                  {/* Filter Button */}
                  <div className="relative">
                    <SearchFilters
                      filters={filters}
                      onFiltersChange={handleFiltersChange}
                      onApplyFilters={handleApplyFilters}
                      onResetFilters={handleResetFilters}
                      isVisible={showFilters}
                      onToggle={() => setShowFilters(!showFilters)}
                    />
                  </div>
                </div>
              </motion.div>

              {/* Search Controls */}
              {searchState.results.length > 0 && (
                <motion.div
                  initial={{ opacity: 0, y: 20 }}
                  animate={{ opacity: 1, y: 0 }}
                  transition={{ duration: 0.5, delay: 0.2 }}
                  className="mb-6 flex items-center justify-between"
                >
                  <div className="flex items-center space-x-4">
                    <h2 className="text-xl font-semibold text-gray-900">
                      {searchState.totalResults.toLocaleString()} results for "
                      {searchState.query}"
                    </h2>
                    {searchState.analytics && (
                      <button
                        onClick={() => setShowAnalytics(!showAnalytics)}
                        className="flex items-center space-x-1 rounded-lg border border-gray-300 px-3 py-1 text-sm text-gray-600 hover:bg-gray-50"
                      >
                        <BarChart3 className="h-4 w-4" />
                        <span>Analytics</span>
                      </button>
                    )}
                  </div>

                  <div className="flex items-center space-x-4">
                    {/* Sort Controls */}
                    <div className="flex items-center space-x-2">
                      <span className="text-sm text-gray-600">Sort by:</span>
                      <button
                        onClick={() => handleSortChange('relevance')}
                        className={`rounded-lg px-3 py-1 text-sm ${
                          sortBy === 'relevance'
                            ? 'bg-blue-100 text-blue-700'
                            : 'text-gray-600 hover:bg-gray-100'
                        }`}
                      >
                        Relevance
                        {sortBy === 'relevance' &&
                          (sortOrder === 'desc' ? (
                            <SortDesc className="ml-1 inline h-3 w-3" />
                          ) : (
                            <SortAsc className="ml-1 inline h-3 w-3" />
                          ))}
                      </button>
                      <button
                        onClick={() => handleSortChange('name')}
                        className={`rounded-lg px-3 py-1 text-sm ${
                          sortBy === 'name'
                            ? 'bg-blue-100 text-blue-700'
                            : 'text-gray-600 hover:bg-gray-100'
                        }`}
                      >
                        Name
                        {sortBy === 'name' &&
                          (sortOrder === 'desc' ? (
                            <SortDesc className="ml-1 inline h-3 w-3" />
                          ) : (
                            <SortAsc className="ml-1 inline h-3 w-3" />
                          ))}
                      </button>
                    </div>

                    {/* View Mode Toggle */}
                    <div className="flex items-center rounded-lg border border-gray-300 p-1">
                      <button
                        onClick={() => setViewMode('grid')}
                        className={`rounded p-1 ${
                          viewMode === 'grid'
                            ? 'bg-blue-100 text-blue-600'
                            : 'text-gray-600'
                        }`}
                      >
                        <Grid3x3 className="h-4 w-4" />
                      </button>
                      <button
                        onClick={() => setViewMode('list')}
                        className={`rounded p-1 ${
                          viewMode === 'list'
                            ? 'bg-blue-100 text-blue-600'
                            : 'text-gray-600'
                        }`}
                      >
                        <List className="h-4 w-4" />
                      </button>
                    </div>
                  </div>
                </motion.div>
              )}
            </div>

            {/* Analytics Panel */}
            <AnimatePresence>
              {showAnalytics && searchState.analytics && (
                <motion.div
                  initial={{ opacity: 0, height: 0 }}
                  animate={{ opacity: 1, height: 'auto' }}
                  exit={{ opacity: 0, height: 0 }}
                  transition={{ duration: 0.3 }}
                  className="mb-8"
                >
                  <SearchAnalytics analytics={searchState.analytics} />
                </motion.div>
              )}
            </AnimatePresence>

            {/* Main Content */}
            {searchState.loading ? (
              <div className="flex items-center justify-center py-16">
                <div className="text-center">
                  <Loader2 className="mx-auto mb-4 h-8 w-8 animate-spin text-blue-600" />
                  <p className="text-gray-600">Searching...</p>
                </div>
              </div>
            ) : searchState.error ? (
              <div className="flex items-center justify-center py-16">
                <div className="text-center">
                  <AlertCircle className="mx-auto mb-4 h-8 w-8 text-red-500" />
                  <p className="mb-2 text-red-600">Search failed</p>
                  <p className="text-gray-600">{searchState.error}</p>
                  <button
                    onClick={() => performSearch(searchState.query)}
                    className="mt-4 rounded-lg bg-blue-600 px-4 py-2 text-white hover:bg-blue-700"
                  >
                    Try Again
                  </button>
                </div>
              </div>
            ) : searchState.query && searchState.results.length > 0 ? (
              <div>
                {/* Search Results */}
                <motion.div
                  initial={{ opacity: 0 }}
                  animate={{ opacity: 1 }}
                  transition={{ duration: 0.5, delay: 0.3 }}
                  className={
                    viewMode === 'grid'
                      ? 'grid gap-6 md:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4'
                      : 'space-y-4'
                  }
                >
                  {searchState.results.map((result, index) => (
                    <SearchResultCard
                      key={result.id}
                      result={result}
                      index={index}
                      showMetadata={true}
                      showDescription={true}
                      className={viewMode === 'list' ? 'w-full' : ''}
                    />
                  ))}
                </motion.div>

                {/* Pagination */}
                {renderPagination()}
              </div>
            ) : searchState.query && !searchState.loading ? (
              <div className="py-16 text-center">
                <SearchIcon className="mx-auto mb-4 h-16 w-16 text-gray-400" />
                <h3 className="mb-2 text-xl font-semibold text-gray-900">
                  No results found
                </h3>
                <p className="mb-4 text-gray-600">
                  Try adjusting your search terms or filters
                </p>
                <button
                  onClick={handleResetFilters}
                  className="rounded-lg bg-blue-600 px-4 py-2 text-white hover:bg-blue-700"
                >
                  Clear Filters
                </button>
              </div>
            ) : (
              <DefaultSearchContent
                recentSearches={recentSearches}
                onSearch={handleSearch}
                onClearRecent={clearRecentSearches}
                trendingSearches={getTrendingSearches()}
              />
            )}
          </div>
        </div>
      </SidebarLayout>
    </ProtectedRoute>
  );
}

// Default Search Content Component
function DefaultSearchContent({
  recentSearches,
  onSearch,
  onClearRecent,
  trendingSearches,
}: {
  recentSearches: string[];
  onSearch: (query: string) => void;
  onClearRecent: () => void;
  trendingSearches: string[];
}) {
  return (
    <div className="space-y-8">
      {/* Recent Searches */}
      {recentSearches.length > 0 && (
        <motion.div
          initial={{ opacity: 0, y: 20 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ duration: 0.5, delay: 0.1 }}
          className="rounded-xl border border-gray-200/50 bg-white/80 p-6 shadow-lg backdrop-blur-sm"
        >
          <div className="mb-4 flex items-center justify-between">
            <h2 className="flex items-center text-xl font-semibold text-gray-900">
              <Clock className="mr-2 h-5 w-5 text-gray-600" />
              Recent Searches
            </h2>
            <button
              onClick={onClearRecent}
              className="text-sm text-gray-500 transition-colors hover:text-gray-700"
            >
              Clear All
            </button>
          </div>
          <div className="flex flex-wrap gap-2">
            {recentSearches.map((search, index) => (
              <motion.button
                key={index}
                initial={{ opacity: 0, scale: 0.9 }}
                animate={{ opacity: 1, scale: 1 }}
                transition={{ duration: 0.3, delay: index * 0.1 }}
                onClick={() => onSearch(search)}
                className="rounded-full bg-gray-100 px-4 py-2 text-sm text-gray-700 transition-all hover:scale-105 hover:bg-gray-200"
              >
                {search}
              </motion.button>
            ))}
          </div>
        </motion.div>
      )}

      {/* Trending Searches */}
      <motion.div
        initial={{ opacity: 0, y: 20 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ duration: 0.5, delay: 0.2 }}
        className="rounded-xl border border-gray-200/50 bg-white/80 p-6 shadow-lg backdrop-blur-sm"
      >
        <h2 className="mb-4 flex items-center text-xl font-semibold text-gray-900">
          <TrendingUp className="mr-2 h-5 w-5 text-green-600" />
          Trending Searches
        </h2>
        <div className="flex flex-wrap gap-2">
          {trendingSearches.map((trend, index) => (
            <motion.button
              key={index}
              initial={{ opacity: 0, scale: 0.9 }}
              animate={{ opacity: 1, scale: 1 }}
              transition={{ duration: 0.3, delay: index * 0.1 }}
              onClick={() => onSearch(trend)}
              className="rounded-full bg-gradient-to-r from-blue-100 to-purple-100 px-4 py-2 text-sm text-blue-700 transition-all hover:scale-105 hover:from-blue-200 hover:to-purple-200"
            >
              {trend}
            </motion.button>
          ))}
        </div>
      </motion.div>

      {/* Quick Access Categories */}
      <motion.div
        initial={{ opacity: 0, y: 20 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ duration: 0.5, delay: 0.3 }}
        className="rounded-xl border border-gray-200/50 bg-white/80 p-6 shadow-lg backdrop-blur-sm"
      >
        <h2 className="mb-4 text-xl font-semibold text-gray-900">
          Quick Access
        </h2>
        <div className="grid grid-cols-2 gap-4 md:grid-cols-4">
          <Link
            href="/players"
            className="group flex flex-col items-center rounded-lg p-4 transition-all hover:scale-105 hover:bg-gray-50"
          >
            <div className="mb-2 rounded-lg bg-blue-100 p-3 transition-colors group-hover:bg-blue-200">
              <User className="h-8 w-8 text-blue-600" />
            </div>
            <span className="text-sm font-medium text-gray-900">Players</span>
            <span className="text-xs text-gray-500">Browse all players</span>
          </Link>
          <Link
            href="/teams"
            className="group flex flex-col items-center rounded-lg p-4 transition-all hover:scale-105 hover:bg-gray-50"
          >
            <div className="mb-2 rounded-lg bg-purple-100 p-3 transition-colors group-hover:bg-purple-200">
              <Trophy className="h-8 w-8 text-purple-600" />
            </div>
            <span className="text-sm font-medium text-gray-900">Teams</span>
            <span className="text-xs text-gray-500">Explore teams</span>
          </Link>
          <Link
            href="/stadiums"
            className="group flex flex-col items-center rounded-lg p-4 transition-all hover:scale-105 hover:bg-gray-50"
          >
            <div className="mb-2 rounded-lg bg-orange-100 p-3 transition-colors group-hover:bg-orange-200">
              <Home className="h-8 w-8 text-orange-600" />
            </div>
            <span className="text-sm font-medium text-gray-900">Stadiums</span>
            <span className="text-xs text-gray-500">Find stadiums</span>
          </Link>
          <Link
            href="/coaches"
            className="group flex flex-col items-center rounded-lg p-4 transition-all hover:scale-105 hover:bg-gray-50"
          >
            <div className="mb-2 rounded-lg bg-green-100 p-3 transition-colors group-hover:bg-green-200">
              <Users className="h-8 w-8 text-green-600" />
            </div>
            <span className="text-sm font-medium text-gray-900">Coaches</span>
            <span className="text-xs text-gray-500">View coaches</span>
          </Link>
        </div>
      </motion.div>

      {/* Search Tips */}
      <motion.div
        initial={{ opacity: 0, y: 20 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ duration: 0.5, delay: 0.4 }}
        className="rounded-xl border border-blue-200/50 bg-gradient-to-r from-blue-50 to-purple-50 p-6"
      >
        <h3 className="mb-3 text-lg font-semibold text-gray-900">
          Search Tips
        </h3>
        <div className="grid grid-cols-1 gap-4 text-sm text-gray-600 md:grid-cols-2">
          <div>
            <h4 className="mb-1 font-medium text-gray-800">General Search</h4>
            <ul className="space-y-1">
              <li>• Use quotes for exact phrases: "Manchester United"</li>
              <li>• Search by position: midfielder, goalkeeper</li>
              <li>• Search by nationality: English players</li>
            </ul>
          </div>
          <div>
            <h4 className="mb-1 font-medium text-gray-800">
              Advanced Features
            </h4>
            <ul className="space-y-1">
              <li>• Use filters to narrow down results</li>
              <li>• Sort by relevance, name, or date</li>
              <li>• Enable fuzzy search for typo tolerance</li>
            </ul>
          </div>
        </div>
      </motion.div>
    </div>
  );
}

// Search Sidebar Component
function SearchSidebar({ isAdmin }: { isAdmin: boolean }) {
  return (
    <>
      <Link href="/dashboard">
        <SidebarItem
          icon={<LayoutDashboardIcon size={20} />}
          text="Dashboard"
        />
      </Link>{' '}
      <Link href="/teams">
        <SidebarItem icon={<Trophy size={20} />} text="Teams" />
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
      {isAdmin && (
        <>
          <SidebarSection title="Admin" color="text-amber-600" />{' '}
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
        <SidebarItem icon={<SearchIcon size={20} />} text="Search" active />
      </Link>
      <Link href="/settings">
        <SidebarItem icon={<Settings size={20} />} text="Settings" />
      </Link>
    </>
  );
}

// Background Elements Component
function BackgroundElements() {
  return (
    <div className="fixed inset-0 z-0">
      {/* Gradient Background */}
      <div className="absolute inset-0 bg-gradient-to-br from-blue-50 via-indigo-50 to-purple-50"></div>
      {/* Stadium Background Image */}
      <div className="absolute inset-0 opacity-[0.02]">
        <Image
          src="/images/Stadium dark.png"
          alt="Stadium Background"
          fill
          className="object-cover object-center"
          priority={false}
        />
      </div>
      {/* Animated Floating Elements */}
      <div className="absolute inset-0 overflow-hidden">
        <motion.div
          animate={{
            y: [-20, 20, -20],
            rotate: [0, 360],
          }}
          transition={{
            duration: 20,
            repeat: Infinity,
            ease: 'linear',
          }}
          className="absolute top-20 left-20 h-2 w-2 rounded-full bg-blue-400/20"
        />
        <motion.div
          animate={{
            y: [20, -20, 20],
            rotate: [360, 0],
          }}
          transition={{
            duration: 25,
            repeat: Infinity,
            ease: 'linear',
            delay: 5,
          }}
          className="absolute top-40 right-32 h-3 w-3 rounded-full bg-indigo-400/15"
        />
        <motion.div
          animate={{
            y: [-10, 30, -10],
            x: [-10, 10, -10],
          }}
          transition={{
            duration: 15,
            repeat: Infinity,
            ease: 'easeInOut',
            delay: 10,
          }}
          className="absolute bottom-32 left-40 h-1 w-1 rounded-full bg-purple-400/25"
        />
        <motion.div
          animate={{
            y: [30, -10, 30],
            x: [10, -10, 10],
          }}
          transition={{
            duration: 18,
            repeat: Infinity,
            ease: 'easeInOut',
            delay: 3,
          }}
          className="absolute right-20 bottom-20 h-2 w-2 rounded-full bg-blue-300/20"
        />
      </div>
      {/* Subtle Mesh Pattern */}{' '}
      <div className="absolute inset-0 opacity-[0.01]">
        <div
          className="h-full w-full"
          style={{
            backgroundImage: `url("data:image/svg+xml,%3Csvg width='60' height='60' viewBox='0 0 60 60' xmlns='http://www.w3.org/2000/svg'%3E%3Cg fill='none' fill-rule='evenodd'%3E%3Cg fill='%234f46e5' fill-opacity='0.1'%3E%3Ccircle cx='10' cy='10' r='2'/%3E%3C/g%3E%3C/g%3E%3C/svg%3E")`,
          }}
        />
      </div>
    </div>
  );
}

// Loading component for Suspense fallback
function SearchLoading() {
  return (
    <div className="flex min-h-screen items-center justify-center bg-gradient-to-br from-slate-900 via-purple-900 to-slate-900 text-white">
      <div className="text-center">
        <div className="mx-auto mb-4 h-32 w-32 animate-spin rounded-full border-b-2 border-white"></div>
        <p className="text-xl">Loading search...</p>
      </div>
    </div>
  );
}

// Main SearchPage component with Suspense boundary
export default function SearchPage() {
  return (
    <Suspense fallback={<SearchLoading />}>
      <SearchContent />
    </Suspense>
  );
}
