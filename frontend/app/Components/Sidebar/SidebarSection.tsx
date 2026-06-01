'use client';

import { useSidebarContext } from './Sidebar';

interface SidebarSectionProps {
  title: string;
  color?: string;
}

export function SidebarSection({
  title,
  color = 'text-gray-400',
}: SidebarSectionProps) {
  const { expanded, isHovered, isDarkMode } = useSidebarContext();

  // Only show section headers when sidebar is expanded or hovered
  const showContent = expanded || isHovered;

  if (!showContent) {
    return null;
  }

  // Adjust color for dark mode if using default color
  const textColor =
    color === 'text-gray-400'
      ? isDarkMode
        ? 'text-gray-500'
        : 'text-gray-400'
      : color;

  return (
    <div
      className={`mt-4 mb-2 px-4 text-xs font-semibold ${textColor} tracking-wider uppercase transition-all duration-300`}
      suppressHydrationWarning
    >
      {title}
    </div>
  );
}
