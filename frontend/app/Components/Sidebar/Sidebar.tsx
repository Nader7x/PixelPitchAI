'use client';

import {
  createContext,
  useContext,
  useState,
  ReactNode,
  useEffect,
} from 'react';
import { ChevronFirst, ChevronLast, LogOut } from 'lucide-react';
import Image from 'next/image';
import Link from 'next/link';
import { useRouter } from 'next/navigation';
import authService from '@/Services/AuthenticationService';
import { useSettings } from '../../contexts/EnhancedSettingsContext';

interface SidebarProps {
  children: ReactNode;
}

interface SidebarLayoutProps {
  sidebar: ReactNode;
  children: ReactNode;
}

interface SidebarContextType {
  expanded: boolean;
  isHovered: boolean;
  isCompactMode: boolean;
  isDarkMode: boolean;
  isMounted: boolean;
}

const SidebarContext = createContext<SidebarContextType | undefined>(undefined);

// New SidebarLayout component that provides context to both sidebar and main content
export function SidebarLayout({ sidebar, children }: SidebarLayoutProps) {
  const [expanded, setExpanded] = useState(false); // Start collapsed
  const [isHovered, setIsHovered] = useState(false);
  const [isCompactMode, setIsCompactMode] = useState(true); // New compact mode state
  const [isMounted, setIsMounted] = useState(false);

  // Get dark mode from settings context
  const { isDarkMode } = useSettings();

  // Handle client-side mounting to prevent hydration errors
  useEffect(() => {
    setIsMounted(true);
  }, []);

  // Keyboard shortcuts for sidebar
  useEffect(() => {
    const handleKeyPress = (e: KeyboardEvent) => {
      // Ctrl/Cmd + B to toggle sidebar
      if ((e.ctrlKey || e.metaKey) && e.key === 'b') {
        e.preventDefault();
        setExpanded((prev) => !prev);
      }
      // Ctrl/Cmd + Shift + B to toggle compact mode
      if ((e.ctrlKey || e.metaKey) && e.shiftKey && e.key === 'B') {
        e.preventDefault();
        setIsCompactMode((prev) => !prev);
      }
    };

    window.addEventListener('keydown', handleKeyPress);
    return () => window.removeEventListener('keydown', handleKeyPress);
  }, []);

  return (
    <SidebarContext.Provider
      value={{
        expanded: expanded || isHovered,
        isHovered,
        isCompactMode,
        isDarkMode: isMounted && isDarkMode,
        isMounted,
      }}
    >
      <div className="relative flex min-h-screen">
        <SidebarComponent
          expanded={expanded}
          setExpanded={setExpanded}
          isHovered={isHovered}
          setIsHovered={setIsHovered}
          isCompactMode={isCompactMode}
          setIsCompactMode={setIsCompactMode}
          isDarkMode={isMounted && isDarkMode}
          isMounted={isMounted}
        >
          {sidebar}
        </SidebarComponent>
        <div
          className={`hidden flex-1 transition-all duration-300 ease-in-out md:block ${
            expanded || isHovered ? 'ml-64' : isCompactMode ? 'ml-14' : 'ml-16'
          }`}
        >
          {children}
        </div>
        {/* Mobile: full width when sidebar is hidden */}
        <div className="flex-1 md:hidden">{children}</div>
      </div>
    </SidebarContext.Provider>
  );
}

// Original Sidebar component, now renamed to SidebarComponent for internal use
interface SidebarComponentProps {
  children: ReactNode;
  expanded: boolean;
  setExpanded: (value: boolean | ((prev: boolean) => boolean)) => void;
  isHovered: boolean;
  setIsHovered: (value: boolean) => void;
  isCompactMode: boolean;
  setIsCompactMode: (value: boolean | ((prev: boolean) => boolean)) => void;
  isDarkMode: boolean;
  isMounted: boolean;
}

function SidebarComponent({
  children,
  expanded,
  setExpanded,
  isHovered,
  setIsHovered,
  isCompactMode,
  setIsCompactMode,
  isDarkMode,
  isMounted,
}: SidebarComponentProps) {
  const router = useRouter();

  const handleLogout = () => {
    authService.logout();
    router.push('/login');
  };

  const showContent = expanded || isHovered;
  const sidebarWidth = showContent ? 'w-64' : isCompactMode ? 'w-14' : 'w-16';

  return (
    <aside
      className="group fixed top-0 left-0 z-50 hidden h-full transition-all duration-300 ease-in-out md:block"
      onMouseEnter={() => setIsHovered(true)}
      onMouseLeave={() => setIsHovered(false)}
    >
      <nav
        className={`flex h-full flex-col transition-all duration-300 ease-in-out ${
          showContent ? 'w-64 shadow-2xl' : sidebarWidth + ' shadow-xl'
        } ${
          isDarkMode
            ? 'border-gray-700/50 bg-gray-900/95 backdrop-blur-sm'
            : 'border-gray-200/50 bg-white/95 backdrop-blur-sm'
        }`}
        suppressHydrationWarning
      >
        {/* Sidebar Header */}
        <div
          className={`flex items-center justify-between p-3 ${
            isDarkMode ? 'border-gray-700/50' : 'border-gray-200/50'
          } border-b`}
          suppressHydrationWarning
        >
          <div className="flex min-w-0 items-center gap-3 overflow-hidden">
            <div
              className={`flex flex-shrink-0 items-center justify-center rounded-xl border bg-white/10 shadow-sm backdrop-blur-sm transition-all duration-300 hover:scale-110 hover:bg-white/20 hover:shadow-md ${
                isDarkMode
                  ? 'border-gray-600/30 hover:border-gray-500/50'
                  : 'border-gray-300/30 hover:border-gray-400/50'
              } ${
                showContent
                  ? 'h-10 w-10'
                  : isCompactMode
                    ? 'h-8 w-8'
                    : 'h-9 w-9'
              }`}
            >
              <Link href={'/home'}>
                <Image
                  src="/logos/PixelPitch.png"
                  width={showContent ? 40 : isCompactMode ? 30 : 35}
                  height={showContent ? 40 : isCompactMode ? 30 : 35}
                  className="rounded-lg transition-transform duration-300 hover:rotate-12"
                  alt="Logo"
                />
              </Link>
            </div>
            <h1
              className={`bg-gradient-to-r bg-clip-text text-lg font-bold whitespace-nowrap text-transparent transition-all duration-300 ${
                isDarkMode
                  ? 'from-green-400 to-emerald-400'
                  : 'from-green-600 to-emerald-600'
              } ${
                showContent
                  ? 'w-auto translate-x-0 opacity-100'
                  : 'w-0 -translate-x-4 overflow-hidden opacity-0'
              }`}
              suppressHydrationWarning
            >
              PixelPitchAI
            </h1>
          </div>

          {showContent && (
            <div className="flex flex-shrink-0 items-center gap-1">
              {/* Compact Mode Toggle */}
              <button
                onClick={() => setIsCompactMode(!isCompactMode)}
                className={`rounded-lg p-1.5 transition-all duration-200 hover:scale-110 ${
                  isDarkMode
                    ? 'bg-gray-800/50 hover:bg-gray-700/70'
                    : 'bg-gray-100/50 hover:bg-gray-200/70'
                }`}
                title={
                  isCompactMode
                    ? 'Switch to Normal Mode'
                    : 'Switch to Compact Mode'
                }
                suppressHydrationWarning
              >
                <div
                  className={`h-3 w-3 rounded-sm transition-all duration-200 ${
                    isCompactMode ? 'bg-green-500' : 'bg-gray-400'
                  }`}
                ></div>
              </button>

              <button
                onClick={() => setExpanded((prev) => !prev)}
                className={`rounded-lg p-1.5 transition-all duration-200 hover:scale-110 ${
                  isDarkMode
                    ? 'bg-gray-800/50 hover:bg-gray-700/70'
                    : 'bg-gray-100/50 hover:bg-gray-200/70'
                }`}
                suppressHydrationWarning
              >
                {expanded ? (
                  <ChevronFirst
                    className={`h-4 w-4 ${isDarkMode ? 'text-gray-300' : 'text-gray-600'}`}
                  />
                ) : (
                  <ChevronLast
                    className={`h-4 w-4 ${isDarkMode ? 'text-gray-300' : 'text-gray-600'}`}
                  />
                )}
              </button>
            </div>
          )}
        </div>

        {/* Main Menu Section */}
        <div className="flex flex-1 flex-col overflow-hidden">
          <div
            className={`px-2 py-3 transition-all duration-300 ${isCompactMode && !showContent ? 'px-1 py-2' : ''}`}
          >
            {showContent && (
              <p
                className={`mb-3 flex items-center justify-between px-3 text-xs font-semibold tracking-wider whitespace-nowrap uppercase ${
                  isDarkMode ? 'text-gray-400' : 'text-gray-500'
                }`}
                suppressHydrationWarning
              >
                <span>Main Menu</span>
                <span
                  className={`rounded-full px-1.5 py-0.5 text-xs ${
                    isCompactMode
                      ? 'bg-green-100 text-green-700'
                      : 'bg-blue-100 text-blue-700'
                  }`}
                >
                  {isCompactMode ? 'Compact' : 'Normal'}
                </span>
              </p>
            )}
            <ul className="space-y-1">{children}</ul>
          </div>
        </div>

        {/* Hover Indicator - Enhanced */}
        {!showContent && (
          <>
            <div className="absolute top-1/2 right-0 h-16 w-1 -translate-y-1/2 rounded-l-full bg-gradient-to-b from-green-400 to-emerald-500 opacity-30"></div>
            <div className="absolute top-1/2 right-0 h-8 w-0.5 -translate-y-1/2 animate-pulse rounded-l-full bg-gradient-to-b from-green-300 to-emerald-400 opacity-50"></div>

            {/* Quick expand button for compact mode */}
            {isCompactMode && (
              <button
                onClick={() => setExpanded(true)}
                className="absolute top-1/2 -right-2 h-6 w-6 -translate-y-1/2 rounded-full bg-green-500 text-white opacity-0 shadow-lg transition-all duration-300 group-hover:opacity-100 hover:scale-110 hover:bg-green-600"
              >
                <ChevronLast className="ml-0.5 h-3 w-3" />
              </button>
            )}
          </>
        )}
      </nav>
    </aside>
  );
}

// Main Sidebar component for backward compatibility
export default function Sidebar({ children }: SidebarProps) {
  return (
    <SidebarLayout sidebar={children}>
      {/* Default empty content for backward compatibility */}
      <div className="flex-1"></div>
    </SidebarLayout>
  );
}

export function useSidebarContext() {
  const context = useContext(SidebarContext);
  if (!context)
    throw new Error('useSidebarContext must be used inside <SidebarLayout>');
  return context;
}
