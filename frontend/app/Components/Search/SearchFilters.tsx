'use client';
import React, { useState, useEffect } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import {
  Filter,
  X,
  ChevronDown,
  Calendar,
  Star,
  MapPin,
  Trophy,
  Users,
  RotateCcw,
  Search,
} from 'lucide-react';
import { AdvancedSearchFilters } from '@/Services/SearchService';

interface SearchFiltersProps {
  filters: AdvancedSearchFilters;
  onFiltersChange: (filters: AdvancedSearchFilters) => void;
  onApplyFilters: () => void;
  onResetFilters: () => void;
  isVisible: boolean;
  onToggle: () => void;
  className?: string;
}

interface FilterSection {
  title: string;
  icon: React.ReactNode;
  content: React.ReactNode;
  isOpen: boolean;
  onToggle: () => void;
}

export default ({
  filters,
  onFiltersChange,
  onApplyFilters,
  onResetFilters,
  isVisible,
  onToggle,
  className = '',
}: SearchFiltersProps) => {
  const [openSections, setOpenSections] = useState<Record<string, boolean>>({
    entityTypes: true,
    location: false,
    competition: false,
    dateRange: false,
    rating: false,
    advanced: false,
  });

  const toggleSection = (section: string) => {
    setOpenSections((prev) => ({
      ...prev,
      [section]: !prev[section],
    }));
  };

  const handleFilterChange = (key: keyof AdvancedSearchFilters, value: any) => {
    onFiltersChange({
      ...filters,
      [key]: value,
    });
  };

  const handleEntityTypeChange = (type: string, checked: boolean) => {
    const currentTypes = filters.entityTypes || [];
    let newTypes: string[];

    if (checked) {
      newTypes = [...currentTypes, type];
    } else {
      newTypes = currentTypes.filter((t) => t !== type);
    }

    handleFilterChange('entityTypes', newTypes);
  };

  const hasActiveFilters = () => {
    return (
      (filters.entityTypes && filters.entityTypes.length > 0) ||
      filters.country ||
      filters.league ||
      filters.position ||
      filters.role ||
      filters.fromDate ||
      filters.toDate ||
      filters.minCapacity ||
      filters.maxCapacity ||
      filters.strategy !== 'Auto'
    );
  };

  const entityTypes = [
    { value: 'Team', label: 'Teams', icon: <Trophy className="h-4 w-4" /> },
    { value: 'Player', label: 'Players', icon: <Users className="h-4 w-4" /> },
    { value: 'Coach', label: 'Coaches', icon: <Users className="h-4 w-4" /> },
    {
      value: 'Stadium',
      label: 'Stadiums',
      icon: <MapPin className="h-4 w-4" />,
    },
    {
      value: 'Match',
      label: 'Matches',
      icon: <Calendar className="h-4 w-4" />,
    },
  ];

  const searchStrategies = [
    { value: 'Auto', label: 'Auto (Recommended)' },
    { value: 'FullText', label: 'Full Text Search' },
    { value: 'Fuzzy', label: 'Fuzzy Search' },
    { value: 'Hybrid', label: 'Hybrid Search' },
  ];

  const FilterSection: React.FC<FilterSection> = ({
    title,
    icon,
    content,
    isOpen,
    onToggle,
  }) => (
    <div className="border-b border-gray-200 last:border-b-0">
      <button
        onClick={onToggle}
        className="flex w-full items-center justify-between p-4 text-left transition-colors hover:bg-gray-50"
      >
        <div className="flex items-center space-x-2">
          <div className="text-gray-600">{icon}</div>
          <span className="font-medium text-gray-900">{title}</span>
        </div>
        <ChevronDown
          className={`h-4 w-4 text-gray-400 transition-transform duration-200 ${
            isOpen ? 'rotate-180' : ''
          }`}
        />
      </button>
      <AnimatePresence>
        {isOpen && (
          <motion.div
            initial={{ height: 0, opacity: 0 }}
            animate={{ height: 'auto', opacity: 1 }}
            exit={{ height: 0, opacity: 0 }}
            transition={{ duration: 0.2 }}
            className="overflow-hidden"
          >
            <div className="px-4 pb-4">{content}</div>
          </motion.div>
        )}
      </AnimatePresence>
    </div>
  );

  return (
    <>
      {/* Filter Toggle Button */}
      <button
        onClick={onToggle}
        className={`flex items-center space-x-2 rounded-lg border px-4 py-2 transition-all duration-200 ${
          hasActiveFilters()
            ? 'border-blue-500 bg-blue-50 text-blue-700'
            : 'border-gray-300 bg-white text-gray-700 hover:bg-gray-50'
        }`}
      >
        <Filter className="h-4 w-4" />
        <span>Filters</span>
        {hasActiveFilters() && (
          <span className="flex h-5 w-5 items-center justify-center rounded-full bg-blue-500 text-xs text-white">
            {[
              filters.entityTypes?.length || 0,
              filters.country ? 1 : 0,
              filters.league ? 1 : 0,
              filters.position ? 1 : 0,
              filters.role ? 1 : 0,
              filters.fromDate || filters.toDate ? 1 : 0,
              filters.minCapacity || filters.maxCapacity ? 1 : 0,
            ].reduce((a, b) => a + b, 0)}
          </span>
        )}
      </button>

      {/* Filter Panel */}
      <AnimatePresence>
        {isVisible && (
          <motion.div
            initial={{ opacity: 0, y: -20, scale: 0.95 }}
            animate={{ opacity: 1, y: 0, scale: 1 }}
            exit={{ opacity: 0, y: -20, scale: 0.95 }}
            transition={{ duration: 0.2 }}
            className={`absolute top-full right-0 left-0 z-50 mt-2 rounded-xl border border-gray-200 bg-white shadow-xl ${className}`}
          >
            {/* Header */}
            <div className="flex items-center justify-between border-b border-gray-200 p-4">
              <div className="flex items-center space-x-2">
                <Filter className="h-5 w-5 text-gray-600" />
                <h3 className="text-lg font-semibold text-gray-900">
                  Search Filters
                </h3>
              </div>
              <div className="flex items-center space-x-2">
                {hasActiveFilters() && (
                  <button
                    onClick={onResetFilters}
                    className="flex items-center space-x-1 rounded-lg px-3 py-1.5 text-sm text-gray-600 transition-colors hover:bg-gray-100"
                  >
                    <RotateCcw className="h-4 w-4" />
                    <span>Reset</span>
                  </button>
                )}
                <button
                  onClick={onToggle}
                  className="rounded-lg p-1.5 text-gray-400 transition-colors hover:bg-gray-100 hover:text-gray-600"
                >
                  <X className="h-4 w-4" />
                </button>
              </div>
            </div>

            {/* Filter Sections */}
            <div className="max-h-96 overflow-y-auto">
              {/* Entity Types */}
              <FilterSection
                title="Entity Types"
                icon={<Trophy className="h-4 w-4" />}
                isOpen={openSections.entityTypes}
                onToggle={() => toggleSection('entityTypes')}
                content={
                  <div className="grid grid-cols-2 gap-3">
                    {entityTypes.map((type) => (
                      <label
                        key={type.value}
                        className="flex cursor-pointer items-center space-x-2 rounded-lg p-2 transition-colors hover:bg-gray-50"
                      >
                        <input
                          type="checkbox"
                          checked={
                            filters.entityTypes?.includes(type.value) || false
                          }
                          onChange={(e) =>
                            handleEntityTypeChange(type.value, e.target.checked)
                          }
                          className="rounded border-gray-300 text-blue-600 focus:ring-blue-500"
                        />
                        <div className="flex items-center space-x-1">
                          {type.icon}
                          <span className="text-sm">{type.label}</span>
                        </div>
                      </label>
                    ))}
                  </div>
                }
              />

              {/* Location Filters */}
              <FilterSection
                title="Location & Competition"
                icon={<MapPin className="h-4 w-4" />}
                isOpen={openSections.location}
                onToggle={() => toggleSection('location')}
                content={
                  <div className="space-y-4">
                    <div>
                      <label className="mb-1 block text-sm font-medium text-gray-700">
                        Country
                      </label>
                      <input
                        type="text"
                        value={filters.country || ''}
                        onChange={(e) =>
                          handleFilterChange('country', e.target.value)
                        }
                        placeholder="e.g., England, Spain, Germany"
                        className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:border-blue-500 focus:ring-blue-500"
                      />
                    </div>
                    <div>
                      <label className="mb-1 block text-sm font-medium text-gray-700">
                        League/Competition
                      </label>
                      <input
                        type="text"
                        value={filters.league || ''}
                        onChange={(e) =>
                          handleFilterChange('league', e.target.value)
                        }
                        placeholder="e.g., Premier League, La Liga"
                        className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:border-blue-500 focus:ring-blue-500"
                      />
                    </div>
                  </div>
                }
              />

              {/* Player/Coach Specific */}
              <FilterSection
                title="Player & Coach Details"
                icon={<Users className="h-4 w-4" />}
                isOpen={openSections.competition}
                onToggle={() => toggleSection('competition')}
                content={
                  <div className="space-y-4">
                    <div>
                      <label className="mb-1 block text-sm font-medium text-gray-700">
                        Position (Players)
                      </label>
                      <input
                        type="text"
                        value={filters.position || ''}
                        onChange={(e) =>
                          handleFilterChange('position', e.target.value)
                        }
                        placeholder="e.g., Midfielder, Striker, Goalkeeper"
                        className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:border-blue-500 focus:ring-blue-500"
                      />
                    </div>
                    <div>
                      <label className="mb-1 block text-sm font-medium text-gray-700">
                        Role (Coaches)
                      </label>
                      <input
                        type="text"
                        value={filters.role || ''}
                        onChange={(e) =>
                          handleFilterChange('role', e.target.value)
                        }
                        placeholder="e.g., Head Coach, Assistant Coach"
                        className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:border-blue-500 focus:ring-blue-500"
                      />
                    </div>
                  </div>
                }
              />

              {/* Date Range */}
              <FilterSection
                title="Date Range"
                icon={<Calendar className="h-4 w-4" />}
                isOpen={openSections.dateRange}
                onToggle={() => toggleSection('dateRange')}
                content={
                  <div className="grid grid-cols-2 gap-4">
                    <div>
                      <label className="mb-1 block text-sm font-medium text-gray-700">
                        From Date
                      </label>
                      <input
                        type="date"
                        value={filters.fromDate || ''}
                        onChange={(e) =>
                          handleFilterChange('fromDate', e.target.value)
                        }
                        className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:border-blue-500 focus:ring-blue-500"
                      />
                    </div>
                    <div>
                      <label className="mb-1 block text-sm font-medium text-gray-700">
                        To Date
                      </label>
                      <input
                        type="date"
                        value={filters.toDate || ''}
                        onChange={(e) =>
                          handleFilterChange('toDate', e.target.value)
                        }
                        className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:border-blue-500 focus:ring-blue-500"
                      />
                    </div>
                  </div>
                }
              />

              {/* Stadium Capacity */}
              <FilterSection
                title="Stadium Capacity"
                icon={<MapPin className="h-4 w-4" />}
                isOpen={openSections.rating}
                onToggle={() => toggleSection('rating')}
                content={
                  <div className="grid grid-cols-2 gap-4">
                    <div>
                      <label className="mb-1 block text-sm font-medium text-gray-700">
                        Min Capacity
                      </label>
                      <input
                        type="number"
                        value={filters.minCapacity || ''}
                        onChange={(e) =>
                          handleFilterChange(
                            'minCapacity',
                            parseInt(e.target.value) || undefined
                          )
                        }
                        placeholder="e.g., 20000"
                        className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:border-blue-500 focus:ring-blue-500"
                      />
                    </div>
                    <div>
                      <label className="mb-1 block text-sm font-medium text-gray-700">
                        Max Capacity
                      </label>
                      <input
                        type="number"
                        value={filters.maxCapacity || ''}
                        onChange={(e) =>
                          handleFilterChange(
                            'maxCapacity',
                            parseInt(e.target.value) || undefined
                          )
                        }
                        placeholder="e.g., 100000"
                        className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:border-blue-500 focus:ring-blue-500"
                      />
                    </div>
                  </div>
                }
              />

              {/* Advanced Settings */}
              <FilterSection
                title="Advanced Search Settings"
                icon={<Search className="h-4 w-4" />}
                isOpen={openSections.advanced}
                onToggle={() => toggleSection('advanced')}
                content={
                  <div className="space-y-4">
                    <div>
                      <label className="mb-1 block text-sm font-medium text-gray-700">
                        Search Strategy
                      </label>
                      <select
                        value={filters.strategy || 'Auto'}
                        onChange={(e) =>
                          handleFilterChange('strategy', e.target.value as any)
                        }
                        className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:border-blue-500 focus:ring-blue-500"
                      >
                        {searchStrategies.map((strategy) => (
                          <option key={strategy.value} value={strategy.value}>
                            {strategy.label}
                          </option>
                        ))}
                      </select>
                    </div>
                    <div className="flex items-center space-x-2">
                      <input
                        type="checkbox"
                        id="enableFuzzySearch"
                        checked={filters.enableFuzzySearch || false}
                        onChange={(e) =>
                          handleFilterChange(
                            'enableFuzzySearch',
                            e.target.checked
                          )
                        }
                        className="rounded border-gray-300 text-blue-600 focus:ring-blue-500"
                      />
                      <label
                        htmlFor="enableFuzzySearch"
                        className="cursor-pointer text-sm text-gray-700"
                      >
                        Enable fuzzy/approximate matching
                      </label>
                    </div>
                  </div>
                }
              />
            </div>

            {/* Footer */}
            <div className="flex items-center justify-between border-t border-gray-200 p-4">
              <span className="text-sm text-gray-500">
                {hasActiveFilters() ? 'Filters applied' : 'No filters applied'}
              </span>
              <div className="flex space-x-2">
                <button
                  onClick={onToggle}
                  className="rounded-lg border border-gray-300 px-4 py-2 text-sm text-gray-700 transition-colors hover:bg-gray-50"
                >
                  Cancel
                </button>
                <button
                  onClick={() => {
                    onApplyFilters();
                    onToggle();
                  }}
                  className="rounded-lg bg-blue-600 px-4 py-2 text-sm text-white transition-colors hover:bg-blue-700"
                >
                  Apply Filters
                </button>
              </div>
            </div>
          </motion.div>
        )}
      </AnimatePresence>
    </>
  );
};
