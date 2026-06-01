'use client';
import React, { useState, useEffect, useRef, useCallback } from 'react';
import { Search, Clock, TrendingUp, X, ArrowUpDown } from 'lucide-react';
import { motion, AnimatePresence } from 'framer-motion';
import { useSettings } from '../../contexts/EnhancedSettingsContext';
import searchService, { SearchSuggestion } from '@/Services/SearchService';
import { useRouter } from 'next/navigation';

interface SearchAutocompleteProps {
  onSearch: (query: string) => void;
  placeholder?: string;
  className?: string;
  showHistory?: boolean;
  showTrending?: boolean;
  maxSuggestions?: number;
  value?: string;
  onChange?: (value: string) => void;
}

export default function SearchAutocomplete({
  onSearch,
  placeholder = 'Search for players, teams, stadiums...',
  className = '',
  showHistory = true,
  showTrending = true,
  maxSuggestions = 5,
  value,
  onChange,
}: SearchAutocompleteProps) {
  const [query, setQuery] = useState(value || '');
  const [suggestions, setSuggestions] = useState<SearchSuggestion[]>([]);
  const [isOpen, setIsOpen] = useState(false);
  const [isMounted, setIsMounted] = useState(false);
  const [isLoading, setIsLoading] = useState(false);
  const [selectedIndex, setSelectedIndex] = useState(-1);
  const [recentSearches, setRecentSearches] = useState<string[]>([]);
  const [trendingSearches, setTrendingSearches] = useState<string[]>([]);

  const inputRef = useRef<HTMLInputElement>(null);
  const dropdownRef = useRef<HTMLDivElement>(null);
  const router = useRouter();

  // Get dark mode from settings
  const { isDarkMode } = useSettings();

  // Handle client-side mounting
  useEffect(() => {
    setIsMounted(true);
  }, []);

  // Load search history and trending searches
  useEffect(() => {
    loadRecentSearches();
    loadTrendingSearches();
  }, []);

  // Debounced search suggestions
  useEffect(() => {
    if (query.length < 2) {
      setSuggestions([]);
      return;
    }

    const timeoutId = setTimeout(() => {
      fetchSuggestions(query);
    }, 300);

    return () => clearTimeout(timeoutId);
  }, [query]);

  // Handle external value changes
  useEffect(() => {
    if (value !== undefined) {
      setQuery(value);
    }
  }, [value]);

  const loadRecentSearches = () => {
    const saved = localStorage.getItem('recentSearches');
    if (saved) {
      setRecentSearches(JSON.parse(saved));
    }
  };

  const loadTrendingSearches = () => {
    // Mock trending searches - replace with actual API call
    setTrendingSearches([
      'Manchester United',
      'Cristiano Ronaldo',
      'Premier League',
      'Football boots',
      'Wembley Stadium',
    ]);
  };

  const fetchSuggestions = async (searchQuery: string) => {
    if (!searchQuery.trim()) return;

    setIsLoading(true);
    try {
      const suggestions = await searchService.getSuggestions(
        searchQuery,
        maxSuggestions
      );
      setSuggestions(suggestions);
    } catch (error) {
      console.error('Error fetching suggestions:', error);
      setSuggestions([]);
    } finally {
      setIsLoading(false);
    }
  };

  const saveSearch = (searchQuery: string) => {
    if (!searchQuery.trim()) return;

    const updated = [
      searchQuery,
      ...recentSearches.filter((s) => s !== searchQuery),
    ].slice(0, 10);
    setRecentSearches(updated);
    localStorage.setItem('recentSearches', JSON.stringify(updated));
  };

  const clearRecentSearches = () => {
    setRecentSearches([]);
    localStorage.removeItem('recentSearches');
  };

  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const newValue = e.target.value;
    setQuery(newValue);
    onChange?.(newValue);
    setSelectedIndex(-1);
    setIsOpen(true);
  };

  const handleSearch = useCallback(
    (searchQuery: string) => {
      if (!searchQuery.trim()) return;

      saveSearch(searchQuery);
      setQuery(searchQuery);
      setIsOpen(false);
      onSearch(searchQuery);
      inputRef.current?.blur();
    },
    [onSearch]
  );

  const handleKeyDown = (e: React.KeyboardEvent) => {
    const totalItems = getSuggestionItems().length;

    switch (e.key) {
      case 'ArrowDown':
        e.preventDefault();
        setSelectedIndex((prev) => (prev < totalItems - 1 ? prev + 1 : -1));
        break;
      case 'ArrowUp':
        e.preventDefault();
        setSelectedIndex((prev) => (prev > -1 ? prev - 1 : totalItems - 1));
        break;
      case 'Enter':
        e.preventDefault();
        if (selectedIndex >= 0) {
          const items = getSuggestionItems();
          const selectedItem = items[selectedIndex];
          if (selectedItem) {
            handleSearch(selectedItem.text);
          }
        } else {
          handleSearch(query);
        }
        break;
      case 'Escape':
        setIsOpen(false);
        setSelectedIndex(-1);
        inputRef.current?.blur();
        break;
    }
  };

  const getSuggestionItems = () => {
    const items: Array<{
      text: string;
      type: 'suggestion' | 'recent' | 'trending';
      icon?: React.ReactNode;
      description?: string;
    }> = [];

    // Add API suggestions
    suggestions.forEach((suggestion) => {
      items.push({
        text: suggestion.text,
        type: 'suggestion',
        description: suggestion.description,
      });
    });

    // Add recent searches if no query or no suggestions
    if (showHistory && (!query || suggestions.length === 0)) {
      recentSearches.slice(0, 3).forEach((search) => {
        items.push({
          text: search,
          type: 'recent',
          icon: (
            <Clock
              className={`h-4 w-4 ${
                isMounted && isDarkMode ? 'text-gray-500' : 'text-gray-400'
              }`}
            />
          ),
        });
      });
    }

    // Add trending searches if no query
    if (showTrending && !query) {
      trendingSearches.slice(0, 3).forEach((trend) => {
        items.push({
          text: trend,
          type: 'trending',
          icon: <TrendingUp className="h-4 w-4 text-green-500" />,
        });
      });
    }

    return items;
  };

  // Click outside handler
  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (
        dropdownRef.current &&
        !dropdownRef.current.contains(event.target as Node) &&
        !inputRef.current?.contains(event.target as Node)
      ) {
        setIsOpen(false);
      }
    };

    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, []);

  const suggestionItems = getSuggestionItems();

  return (
    <div className={`relative ${className}`}>
      <div className="relative">
        <div className="pointer-events-none absolute inset-y-0 left-0 flex items-center pl-3">
          <Search
            className={`h-5 w-5 ${
              isMounted && isDarkMode ? 'text-gray-400' : 'text-gray-400'
            }`}
          />
        </div>
        <input
          ref={inputRef}
          type="text"
          value={query}
          onChange={handleInputChange}
          onKeyDown={handleKeyDown}
          onFocus={() => setIsOpen(true)}
          placeholder={placeholder}
          className={`w-full rounded-lg border py-3 pr-4 pl-12 text-sm transition-all duration-200 focus:outline-none ${
            isMounted && isDarkMode
              ? 'border-gray-600 bg-gray-700 text-white placeholder-gray-400 focus:border-blue-400 focus:ring-2 focus:ring-blue-400/20'
              : 'border-gray-300 bg-white text-gray-900 placeholder-gray-500 focus:border-blue-500 focus:ring-2 focus:ring-blue-500/20'
          } ${
            isOpen ? 'rounded-b-none border-blue-500 dark:border-blue-400' : ''
          }`}
          suppressHydrationWarning
        />
        {isLoading && (
          <div className="pointer-events-none absolute inset-y-0 right-0 flex items-center pr-3">
            <div className="h-4 w-4 animate-spin rounded-full border-2 border-blue-500 border-t-transparent"></div>
          </div>
        )}
      </div>

      <AnimatePresence>
        {isOpen && suggestionItems.length > 0 && (
          <motion.div
            ref={dropdownRef}
            initial={{ opacity: 0, y: -10 }}
            animate={{ opacity: 1, y: 0 }}
            exit={{ opacity: 0, y: -10 }}
            transition={{ duration: 0.2 }}
            className={`absolute top-full right-0 left-0 z-50 max-h-96 overflow-y-auto rounded-b-lg border border-t-0 shadow-lg ${
              isMounted && isDarkMode
                ? 'border-gray-600 bg-gray-700'
                : 'border-gray-300 bg-white'
            }`}
            suppressHydrationWarning
          >
            {/* Recent searches header */}
            {showHistory && recentSearches.length > 0 && !query && (
              <div
                className={`flex items-center justify-between border-b px-4 py-2 ${
                  isMounted && isDarkMode
                    ? 'border-gray-600'
                    : 'border-gray-100'
                }`}
                suppressHydrationWarning
              >
                <span
                  className={`text-xs font-medium tracking-wide uppercase ${
                    isMounted && isDarkMode ? 'text-gray-400' : 'text-gray-500'
                  }`}
                  suppressHydrationWarning
                >
                  Recent Searches
                </span>
                <button
                  onClick={clearRecentSearches}
                  className={`text-xs transition-colors ${
                    isMounted && isDarkMode
                      ? 'text-gray-500 hover:text-gray-300'
                      : 'text-gray-400 hover:text-gray-600'
                  }`}
                  suppressHydrationWarning
                >
                  Clear
                </button>
              </div>
            )}

            {suggestionItems.map((item, index) => (
              <button
                key={`${item.type}-${item.text}-${index}`}
                onClick={() => handleSearch(item.text)}
                className={`w-full px-4 py-3 text-left transition-colors ${
                  selectedIndex === index
                    ? isMounted && isDarkMode
                      ? 'bg-blue-900/50 text-blue-300'
                      : 'bg-blue-50 text-blue-700'
                    : isMounted && isDarkMode
                      ? 'hover:bg-gray-600'
                      : 'hover:bg-gray-50'
                } ${
                  item.type === 'trending' && !query
                    ? isMounted && isDarkMode
                      ? 'border-t border-gray-600'
                      : 'border-t border-gray-100'
                    : ''
                }`}
                suppressHydrationWarning
              >
                <div className="flex items-center space-x-3">
                  {item.icon && (
                    <div className="flex-shrink-0">{item.icon}</div>
                  )}
                  <div className="min-w-0 flex-1">
                    <div
                      className={`text-sm font-medium ${
                        isMounted && isDarkMode
                          ? 'text-gray-200'
                          : 'text-gray-900'
                      }`}
                      suppressHydrationWarning
                    >
                      {item.text}
                    </div>
                    {item.description && (
                      <div
                        className={`truncate text-xs ${
                          isMounted && isDarkMode
                            ? 'text-gray-400'
                            : 'text-gray-500'
                        }`}
                        suppressHydrationWarning
                      >
                        {item.description}
                      </div>
                    )}
                  </div>
                  {item.type === 'suggestion' && (
                    <ArrowUpDown className="h-3 w-3 rotate-45 text-gray-400" />
                  )}
                </div>
              </button>
            ))}

            {/* Trending searches header */}
            {showTrending && !query && trendingSearches.length > 0 && (
              <div className="border-t border-gray-100 px-4 py-2">
                <span className="text-xs font-medium tracking-wide text-gray-500 uppercase">
                  Trending Searches
                </span>
              </div>
            )}
          </motion.div>
        )}
      </AnimatePresence>
    </div>
  );
}
