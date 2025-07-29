'use client';

import { ReactNode } from 'react';
import { useSidebarContext } from './Sidebar';
import Link from 'next/link';

interface SidebarItemProps {
  icon: ReactNode;
  text: string;
  active?: boolean;
  alert?: boolean;
  onClick?: () => void;
  href?: string;
}

export function SidebarItem({
  icon,
  text,
  active,
  alert,
  onClick,
  href,
}: SidebarItemProps) {
  const { expanded, isCompactMode, isHovered, isDarkMode } =
    useSidebarContext();

  // Determine if content should be shown (expanded or hovered)
  const showContent = expanded || isHovered;

  // Dynamic classes based on sidebar state
  const getItemClasses = () => {
    const baseClasses =
      'relative flex items-center font-medium rounded-md cursor-pointer transition-all duration-300 group overflow-hidden';

    // Spacing based on mode and state
    let spacingClasses = '';
    if (isCompactMode && !showContent) {
      spacingClasses = 'py-2 px-2 my-1 justify-center w-8 h-8 mx-auto'; // Ultra compact with fixed size
    } else if (!showContent) {
      spacingClasses = 'py-2.5 px-2.5 my-1 justify-center w-10 h-10 mx-auto'; // Normal compact with fixed size
    } else {
      spacingClasses = 'py-2.5 px-3 my-1 w-full'; // Expanded
    }

    // Active/inactive styling
    const styleClasses = active
      ? isDarkMode
        ? 'bg-gradient-to-tr from-green-500/30 to-green-600/20 text-green-300 shadow-sm border border-green-400/30'
        : 'bg-gradient-to-tr from-green-500/20 to-green-600/10 text-green-700 shadow-sm border border-green-200/50'
      : isDarkMode
        ? 'hover:bg-green-800/20 text-gray-300 hover:text-green-300 hover:scale-[1.02] hover:shadow-sm'
        : 'hover:bg-green-50 text-gray-600 hover:text-green-700 hover:scale-[1.02] hover:shadow-sm';

    return `${baseClasses} ${spacingClasses} ${styleClasses}`;
  };

  // Icon size based on mode
  const getIconSize = () => {
    if (isCompactMode && !showContent) return 'text-base'; // 16px
    if (!showContent) return 'text-lg'; // 18px
    return 'text-xl'; // 20px
  };

  // Inner content of the sidebar item
  const content = (
    <>
      {/* Icon */}
      <span
        className={`inline-block flex-shrink-0 transition-all duration-300 ${getIconSize()}`}
      >
        {icon}
      </span>

      {/* Text - only show when expanded */}
      <span
        className={`overflow-hidden whitespace-nowrap transition-all duration-300 ${
          showContent
            ? 'ml-3 w-auto max-w-none opacity-100'
            : 'ml-0 w-0 max-w-0 opacity-0'
        }`}
      >
        {text}
      </span>

      {/* Alert indicator */}
      {alert && (
        <div
          className={`absolute h-2 w-2 rounded-full bg-red-500 transition-all duration-300 ${
            showContent ? 'top-1/2 right-2 -translate-y-1/2' : 'top-1 right-1'
          }`}
        />
      )}

      {/* Tooltip for collapsed state */}
      {!showContent && (
        <div
          className={`invisible absolute left-full z-50 ml-2 rounded-lg px-3 py-2 text-sm whitespace-nowrap shadow-lg backdrop-blur-sm transition-all duration-200 group-hover:visible group-hover:ml-3 group-hover:opacity-100 ${
            isDarkMode
              ? 'bg-gray-800/95 text-gray-200'
              : 'bg-gray-900/95 text-white'
          } opacity-0`}
        >
          {text}
          {/* Tooltip arrow */}
          <div
            className={`absolute top-1/2 left-0 h-2 w-2 -translate-x-1 -translate-y-1/2 rotate-45 ${
              isDarkMode ? 'bg-gray-800/95' : 'bg-gray-900/95'
            }`}
          ></div>
        </div>
      )}
    </>
  );

  // Return Link component if href is provided, otherwise a div with onClick
  if (href) {
    return (
      <li>
        <Link href={href} className={getItemClasses()}>
          {content}
        </Link>
      </li>
    );
  }

  return (
    <li className={getItemClasses()} onClick={onClick}>
      {content}
    </li>
  );
}
