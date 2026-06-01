'use client';

import { useSettings } from '@/app/contexts/EnhancedSettingsContext';
import { useTranslations } from 'next-intl';
import { Moon, Sun, Monitor, Globe } from 'lucide-react';

export default function SettingsTest() {
  const { isDarkMode, currentTheme, currentLocale, settings } = useSettings();
  const t = useTranslations();

  const getThemeIcon = () => {
    switch (currentTheme) {
      case 'light':
        return <Sun className="h-5 w-5 text-yellow-500" />;
      case 'dark':
        return <Moon className="h-5 w-5 text-blue-500" />;
      case 'system':
        return <Monitor className="h-5 w-5 text-gray-500" />;
      default:
        return <Monitor className="h-5 w-5 text-gray-500" />;
    }
  };

  return (
    <div className="fixed top-4 right-4 z-50 rounded-lg border border-gray-200 bg-white p-4 shadow-lg dark:border-gray-600 dark:bg-gray-800">
      <h3 className="mb-3 font-medium text-gray-900 dark:text-white">
        Settings Status
      </h3>

      <div className="space-y-2 text-sm">
        <div className="flex items-center gap-2">
          {getThemeIcon()}
          <span className="text-gray-600 dark:text-gray-300">
            Theme: {currentTheme} {isDarkMode ? '(Dark)' : '(Light)'}
          </span>
        </div>

        <div className="flex items-center gap-2">
          <Globe className="h-5 w-5 text-green-500" />
          <span className="text-gray-600 dark:text-gray-300">
            Language: {currentLocale}
          </span>
        </div>

        <div className="text-gray-600 dark:text-gray-300">
          Sound: {settings.display.soundEnabled ? 'On' : 'Off'}
        </div>

        <div className="text-gray-600 dark:text-gray-300">
          Translation Test: {t('common.save')}
        </div>
      </div>
    </div>
  );
}
