'use client';

import { ReactNode, useState, useEffect } from 'react';
import { useSettings } from '../../contexts/EnhancedSettingsContext';

interface RightPanelProps {
  children: ReactNode;
  title?: string;
}

export default function RightPanel({
  children,
  title = 'Updates',
}: RightPanelProps) {
  const [isMounted, setIsMounted] = useState(false);

  // Get dark mode from settings
  const { isDarkMode } = useSettings();

  useEffect(() => {
    setIsMounted(true);
  }, []);

  return (
    <aside className="h-screen">
      <nav
        className={`flex h-full flex-col shadow-sm transition-colors duration-300 ${
          isMounted && isDarkMode
            ? 'border-gray-700 bg-gray-800'
            : 'border-gray-200 bg-white'
        }`}
        suppressHydrationWarning
      >
        <div className="flex items-center justify-between p-4 pb-2">
          <div className="flex items-center">
            <h1
              className={`px-3 text-xl font-bold transition-colors duration-300 ${
                isMounted && isDarkMode ? 'text-white' : 'text-gray-900'
              }`}
              suppressHydrationWarning
            >
              {title}
            </h1>
          </div>
        </div>

        <ul className="flex-1 space-y-1 px-3 py-5">{children}</ul>
      </nav>
    </aside>
  );
}
