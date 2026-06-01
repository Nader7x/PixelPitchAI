'use client';
import React, { useState, useEffect } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import {
  BarChart3,
  Clock,
  Target,
  TrendingUp,
  Users,
  Eye,
  ChevronDown,
  RefreshCw,
  Info,
} from 'lucide-react';
import { SearchAnalytics } from '@/Services/SearchService';

interface SearchAnalyticsProps {
  analytics: SearchAnalytics | null;
  isLoading?: boolean;
  className?: string;
  onRefresh?: () => void;
}

interface MetricCardProps {
  title: string;
  value: string | number;
  icon: React.ReactNode;
  description?: string;
  trend?: {
    value: number;
    isPositive: boolean;
  };
  className?: string;
}

const MetricCard: React.FC<MetricCardProps> = ({
  title,
  value,
  icon,
  description,
  trend,
  className = '',
}) => (
  <motion.div
    initial={{ opacity: 0, y: 20 }}
    animate={{ opacity: 1, y: 0 }}
    transition={{ duration: 0.3 }}
    className={`rounded-xl border border-gray-200/50 bg-white/80 p-6 shadow-lg backdrop-blur-sm transition-all duration-300 hover:bg-white/90 hover:shadow-xl ${className}`}
  >
    <div className="mb-3 flex items-center justify-between">
      <div className="flex items-center space-x-3">
        <div className="rounded-lg bg-blue-100 p-2 text-blue-600">{icon}</div>
        <div>
          <h3 className="text-lg font-semibold text-gray-900">{title}</h3>
          {description && (
            <p className="text-sm text-gray-500">{description}</p>
          )}
        </div>
      </div>
      {trend && (
        <div
          className={`flex items-center space-x-1 text-sm ${
            trend.isPositive ? 'text-green-600' : 'text-red-600'
          }`}
        >
          <TrendingUp
            className={`h-4 w-4 ${trend.isPositive ? '' : 'rotate-180'}`}
          />
          <span>{Math.abs(trend.value)}%</span>
        </div>
      )}
    </div>
    <div className="text-3xl font-bold text-gray-900">{value}</div>
  </motion.div>
);

export default function SearchAnalyticsComponent({
  analytics,
  isLoading = false,
  className = '',
  onRefresh,
}: SearchAnalyticsProps) {
  const [isExpanded, setIsExpanded] = useState(false);
  const [showDetails, setShowDetails] = useState(false);

  if (!analytics && !isLoading) {
    return null;
  }

  const formatDuration = (duration: string) => {
    // Convert duration string (e.g., "00:00:00.123") to milliseconds
    const match = duration.match(/(\d+):(\d+):(\d+)\.(\d+)/);
    if (!match) return duration;

    const [, hours, minutes, seconds, milliseconds] = match;
    const totalMs =
      parseInt(hours) * 3600000 +
      parseInt(minutes) * 60000 +
      parseInt(seconds) * 1000 +
      parseInt(milliseconds);

    if (totalMs < 1000) {
      return `${totalMs}ms`;
    } else if (totalMs < 60000) {
      return `${(totalMs / 1000).toFixed(2)}s`;
    } else {
      return `${(totalMs / 60000).toFixed(2)}m`;
    }
  };

  const getStrategyIcon = (strategy: string) => {
    switch (strategy.toLowerCase()) {
      case 'auto':
        return <Target className="h-4 w-4" />;
      case 'fulltext':
        return <Eye className="h-4 w-4" />;
      case 'fuzzy':
        return <RefreshCw className="h-4 w-4" />;
      case 'hybrid':
        return <BarChart3 className="h-4 w-4" />;
      default:
        return <Info className="h-4 w-4" />;
    }
  };

  const getStrategyColor = (strategy: string) => {
    switch (strategy.toLowerCase()) {
      case 'auto':
        return 'bg-blue-100 text-blue-800 border-blue-200';
      case 'fulltext':
        return 'bg-green-100 text-green-800 border-green-200';
      case 'fuzzy':
        return 'bg-orange-100 text-orange-800 border-orange-200';
      case 'hybrid':
        return 'bg-purple-100 text-purple-800 border-purple-200';
      default:
        return 'bg-gray-100 text-gray-800 border-gray-200';
    }
  };

  if (isLoading) {
    return (
      <motion.div
        initial={{ opacity: 0 }}
        animate={{ opacity: 1 }}
        className={`rounded-xl border border-gray-200/50 bg-white/80 p-6 shadow-lg backdrop-blur-sm ${className}`}
      >
        <div className="mb-4 flex items-center justify-between">
          <h2 className="flex items-center space-x-2 text-xl font-semibold text-gray-900">
            <BarChart3 className="h-5 w-5" />
            <span>Search Analytics</span>
          </h2>
          <div className="animate-spin">
            <RefreshCw className="h-4 w-4 text-gray-400" />
          </div>
        </div>
        <div className="space-y-3">
          {[...Array(3)].map((_, i) => (
            <div key={i} className="animate-pulse">
              <div className="mb-2 h-4 w-3/4 rounded bg-gray-200"></div>
              <div className="h-8 w-1/2 rounded bg-gray-200"></div>
            </div>
          ))}
        </div>
      </motion.div>
    );
  }

  return (
    <motion.div
      initial={{ opacity: 0, y: 20 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ duration: 0.3 }}
      className={`overflow-hidden rounded-xl border border-gray-200/50 bg-white/80 shadow-lg backdrop-blur-sm ${className}`}
    >
      {/* Header */}
      <div className="border-b border-gray-200/50 p-6">
        <div className="flex items-center justify-between">
          <div className="flex items-center space-x-3">
            <div className="rounded-lg bg-gradient-to-br from-blue-100 to-purple-100 p-2">
              <BarChart3 className="h-5 w-5 text-blue-600" />
            </div>
            <div>
              <h2 className="text-xl font-semibold text-gray-900">
                Search Analytics
              </h2>
              <p className="text-sm text-gray-500">
                Query: "{analytics?.query}"
              </p>
            </div>
          </div>
          <div className="flex items-center space-x-2">
            {onRefresh && (
              <button
                onClick={onRefresh}
                className="rounded-lg p-2 text-gray-400 transition-colors hover:bg-gray-100 hover:text-gray-600"
                title="Refresh analytics"
              >
                <RefreshCw className="h-4 w-4" />
              </button>
            )}
            <button
              onClick={() => setIsExpanded(!isExpanded)}
              className="rounded-lg p-2 text-gray-400 transition-colors hover:bg-gray-100 hover:text-gray-600"
            >
              <ChevronDown
                className={`h-4 w-4 transition-transform duration-200 ${
                  isExpanded ? 'rotate-180' : ''
                }`}
              />
            </button>
          </div>
        </div>
      </div>

      {/* Quick Stats */}
      <div className="p-6">
        <div className="grid grid-cols-1 gap-4 md:grid-cols-3">
          <MetricCard
            title="Results Found"
            value={analytics?.totalResultsFound.toLocaleString() || '0'}
            icon={<Target className="h-4 w-4" />}
            description="Total matching items"
          />
          <MetricCard
            title="Search Duration"
            value={formatDuration(analytics?.searchDuration || '0')}
            icon={<Clock className="h-4 w-4" />}
            description="Time to complete search"
          />
          <MetricCard
            title="Avg. Relevance"
            value={`${(analytics?.averageRelevanceScore || 0).toFixed(1)}/10`}
            icon={<TrendingUp className="h-4 w-4" />}
            description="Average result relevance"
          />
        </div>
      </div>

      {/* Expanded Details */}
      <AnimatePresence>
        {isExpanded && (
          <motion.div
            initial={{ height: 0, opacity: 0 }}
            animate={{ height: 'auto', opacity: 1 }}
            exit={{ height: 0, opacity: 0 }}
            transition={{ duration: 0.3 }}
            className="overflow-hidden border-t border-gray-200/50"
          >
            <div className="space-y-6 p-6">
              {/* Strategy Used */}
              <div>
                <h3 className="mb-3 text-lg font-medium text-gray-900">
                  Search Strategy
                </h3>
                <div className="flex items-center space-x-3">
                  <span
                    className={`inline-flex items-center rounded-full border px-3 py-1 text-sm font-medium ${getStrategyColor(
                      analytics?.strategyUsed || ''
                    )}`}
                  >
                    {getStrategyIcon(analytics?.strategyUsed || '')}
                    <span className="ml-2 capitalize">
                      {analytics?.strategyUsed}
                    </span>
                  </span>
                  {analytics?.usedFallbackSearch && (
                    <span className="inline-flex items-center rounded-full border border-yellow-200 bg-yellow-100 px-3 py-1 text-sm font-medium text-yellow-800">
                      <Info className="mr-1 h-3 w-3" />
                      Fallback Used
                    </span>
                  )}
                </div>
                <p className="mt-2 text-sm text-gray-600">
                  {analytics?.strategyUsed === 'Auto' &&
                    'Automatically selected the best search strategy based on your query.'}
                  {analytics?.strategyUsed === 'FullText' &&
                    'Used full-text search for exact and phrase matching.'}
                  {analytics?.strategyUsed === 'Fuzzy' &&
                    'Used fuzzy search for approximate and typo-tolerant matching.'}
                  {analytics?.strategyUsed === 'Hybrid' &&
                    'Combined multiple search strategies for optimal results.'}
                </p>
              </div>

              {/* Results by Entity Type */}
              {analytics?.resultsByEntityType && (
                <div>
                  <h3 className="mb-3 text-lg font-medium text-gray-900">
                    Results by Category
                  </h3>
                  <div className="grid grid-cols-2 gap-3 md:grid-cols-5">
                    {Object.entries(analytics.resultsByEntityType).map(
                      ([type, count]) => (
                        <div
                          key={type}
                          className="rounded-lg bg-gray-50 p-3 text-center"
                        >
                          <div className="text-2xl font-bold text-gray-900">
                            {count}
                          </div>
                          <div className="text-sm text-gray-600 capitalize">
                            {type}s
                          </div>
                        </div>
                      )
                    )}
                  </div>
                </div>
              )}

              {/* Search Suggestions */}
              {analytics?.searchSuggestions &&
                analytics.searchSuggestions.length > 0 && (
                  <div>
                    <h3 className="mb-3 text-lg font-medium text-gray-900">
                      Related Suggestions
                    </h3>
                    <div className="flex flex-wrap gap-2">
                      {analytics.searchSuggestions.map((suggestion, index) => (
                        <span
                          key={index}
                          className="inline-flex cursor-pointer items-center rounded-full border border-blue-200 bg-blue-50 px-3 py-1 text-sm text-blue-700 transition-colors hover:bg-blue-100"
                        >
                          {suggestion}
                        </span>
                      ))}
                    </div>
                  </div>
                )}

              {/* Performance Insights */}
              <div className="rounded-lg bg-gradient-to-r from-blue-50 to-purple-50 p-4">
                <h4 className="mb-2 font-medium text-gray-900">
                  Performance Insights
                </h4>
                <div className="space-y-1 text-sm text-gray-600">
                  <div className="flex items-center justify-between">
                    <span>Search completed in</span>
                    <span className="font-medium">
                      {formatDuration(analytics?.searchDuration || '0')}
                    </span>
                  </div>
                  <div className="flex items-center justify-between">
                    <span>Results quality</span>
                    <span className="font-medium">
                      {analytics?.averageRelevanceScore &&
                      analytics.averageRelevanceScore > 8
                        ? 'Excellent'
                        : analytics?.averageRelevanceScore &&
                            analytics.averageRelevanceScore > 6
                          ? 'Good'
                          : analytics?.averageRelevanceScore &&
                              analytics.averageRelevanceScore > 4
                            ? 'Fair'
                            : 'Needs improvement'}
                    </span>
                  </div>
                  <div className="flex items-center justify-between">
                    <span>Total matches</span>
                    <span className="font-medium">
                      {analytics?.totalResultsFound.toLocaleString()}
                    </span>
                  </div>
                </div>
              </div>
            </div>
          </motion.div>
        )}
      </AnimatePresence>
    </motion.div>
  );
}
