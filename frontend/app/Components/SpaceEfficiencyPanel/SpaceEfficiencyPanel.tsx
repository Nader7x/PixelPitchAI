'use client';

import { useState, useEffect } from 'react';
import { Monitor, Maximize2, Minimize2 } from 'lucide-react';

interface SpaceEfficiencyPanelProps {
  isCompactMode: boolean;
  sidebarExpanded: boolean;
}

export default function SpaceEfficiencyPanel({
  isCompactMode,
  sidebarExpanded,
}: SpaceEfficiencyPanelProps) {
  const [screenWidth, setScreenWidth] = useState(0);
  const [contentWidth, setContentWidth] = useState(0);

  useEffect(() => {
    const updateDimensions = () => {
      const width = window.innerWidth;
      setScreenWidth(width); // Calculate content width based on sidebar state (matching actual sidebar widths)
      const sidebarWidth = sidebarExpanded ? 256 : isCompactMode ? 56 : 64;
      setContentWidth(width - sidebarWidth);
    };

    updateDimensions();
    window.addEventListener('resize', updateDimensions);
    return () => window.removeEventListener('resize', updateDimensions);
  }, [isCompactMode, sidebarExpanded]);

  const spaceEfficiency =
    screenWidth > 0 ? ((contentWidth / screenWidth) * 100).toFixed(1) : 0;
  const spaceSaved = isCompactMode ? 8 : 0; // pixels saved in compact mode (64px - 56px = 8px)

  return (
    <div className="fixed right-4 bottom-4 z-40 min-w-[200px] rounded-lg border border-gray-200/50 bg-white/90 p-3 shadow-lg backdrop-blur-sm">
      <div className="mb-2 flex items-center gap-2">
        <Monitor size={16} className="text-blue-600" />
        <span className="text-sm font-semibold text-gray-700">
          Space Efficiency
        </span>
      </div>

      <div className="space-y-2 text-xs">
        <div className="flex justify-between">
          <span className="text-gray-600">Content Area:</span>
          <span className="font-mono font-medium">{spaceEfficiency}%</span>
        </div>

        <div className="flex justify-between">
          <span className="text-gray-600">Mode:</span>
          <div className="flex items-center gap-1">
            {isCompactMode ? (
              <Minimize2 size={12} className="text-green-600" />
            ) : (
              <Maximize2 size={12} className="text-blue-600" />
            )}
            <span
              className={`font-medium ${
                isCompactMode ? 'text-green-600' : 'text-blue-600'
              }`}
            >
              {isCompactMode ? 'Compact' : 'Normal'}
            </span>
          </div>
        </div>

        {spaceSaved > 0 && (
          <div className="flex justify-between">
            <span className="text-gray-600">Space Saved:</span>
            <span className="font-mono font-medium text-green-600">
              +{spaceSaved}px
            </span>
          </div>
        )}

        <div className="mt-2 border-t border-gray-200 pt-2">
          <div className="text-xs text-gray-500">
            <kbd className="rounded bg-gray-100 px-1 py-0.5 text-xs">
              Ctrl+B
            </kbd>{' '}
            Toggle
          </div>
          <div className="mt-1 text-xs text-gray-500">
            <kbd className="rounded bg-gray-100 px-1 py-0.5 text-xs">
              Ctrl+Shift+B
            </kbd>{' '}
            Compact
          </div>
        </div>
      </div>
    </div>
  );
}
