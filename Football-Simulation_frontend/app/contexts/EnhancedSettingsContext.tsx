'use client';

import React, {
  createContext,
  useContext,
  useEffect,
  useState,
  ReactNode,
} from 'react';
import { useTheme } from 'next-themes';
import { useTranslations, useLocale } from 'next-intl';
import { useRouter, usePathname } from 'next/navigation';
import notificationService from '@/Services/NotificationService';

export interface UserSettings {
  notifications: {
    matchStart: boolean;
    matchEnd: boolean;
    systemAlerts: boolean;
    email: boolean;
    push: boolean;
    autoRedirect: boolean;
  };
  privacy: {
    profileVisible: boolean;
    showOnlineStatus: boolean;
    allowFriendRequests: boolean;
  };
  display: {
    timezone: string;
    compactMode: boolean;
    soundEnabled: boolean;
  };
  account: {
    twoFactorEnabled: boolean;
    sessionTimeout: number;
  };
}

const defaultSettings: UserSettings = {
  notifications: {
    matchStart: true,
    matchEnd: true,
    systemAlerts: true,
    email: true,
    push: true,
    autoRedirect: true,
  },
  privacy: {
    profileVisible: true,
    showOnlineStatus: true,
    allowFriendRequests: true,
  },
  display: {
    timezone: 'UTC',
    compactMode: false,
    soundEnabled: true,
  },
  account: {
    twoFactorEnabled: false,
    sessionTimeout: 30,
  },
};

interface SettingsContextType {
  settings: UserSettings;
  updateSetting: (path: string, value: any) => void;
  saveSettings: () => Promise<void>;
  resetToDefaults: () => void;
  hasChanges: boolean;
  isLoading: boolean;
  playSound: (
    soundType: 'notification' | 'success' | 'error' | 'click'
  ) => void;
  // Theme functions using next-themes
  setTheme: (theme: string) => void;
  isDarkMode: boolean;
  currentTheme: string | undefined;
  // Language functions using next-intl
  currentLocale: string;
  changeLanguage: (locale: string) => void;
}

const SettingsContext = createContext<SettingsContextType | undefined>(
  undefined
);

// Sound effects URLs
const soundEffects = {
  notification: '/sounds/success.mp3', // Using success sound as fallback
  success: '/sounds/success.mp3',
  error: '/sounds/success.mp3', // Using success sound as fallback
  click: '/sounds/click.mp3',
};

export function SettingsProvider({ children }: { children: ReactNode }) {
  const [settings, setSettings] = useState<UserSettings>(defaultSettings);
  const [hasChanges, setHasChanges] = useState(false);
  const [isLoading, setIsLoading] = useState(true);

  // Next-themes integration
  const { theme, setTheme, resolvedTheme } = useTheme();
  // Next-intl integration with fallback
  let locale = 'en';
  let router: any = null;
  let pathname = '';

  try {
    locale = useLocale();
    router = useRouter();
    pathname = usePathname();
  } catch (error) {
    // Intl context not available, use defaults
    console.log('Intl context not available, using default locale');
  }

  const isDarkMode = resolvedTheme === 'dark';

  // Load settings on mount
  useEffect(() => {
    loadSettings();
  }, []);

  const loadSettings = () => {
    try {
      const savedSettings = localStorage.getItem('userSettings');
      if (savedSettings) {
        const parsed = JSON.parse(savedSettings);
        setSettings({ ...defaultSettings, ...parsed });
      }

      // Load notification settings from NotificationService
      const autoRedirect = notificationService.getAutoRedirectPreference();
      setSettings((prev) => ({
        ...prev,
        notifications: {
          ...prev.notifications,
          autoRedirect,
        },
      }));
    } catch (error) {
      console.error('Error loading settings:', error);
    } finally {
      setIsLoading(false);
    }
  };
  const updateSetting = (path: string, value: any) => {
    setSettings((prev) => {
      const newSettings = { ...prev };
      const keys = path.split('.');
      let current: any = newSettings;

      // Ensure all intermediate objects exist
      for (let i = 0; i < keys.length - 1; i++) {
        const key = keys[i];
        if (!current[key] || typeof current[key] !== 'object') {
          current[key] = {};
        }
        current = current[key];
      }

      // Set the final value
      current[keys[keys.length - 1]] = value;
      return newSettings;
    });
    setHasChanges(true);
  };
  const saveSettings = async () => {
    try {
      // Save to localStorage
      localStorage.setItem('userSettings', JSON.stringify(settings));

      // Update notification service preferences
      notificationService.setAutoRedirectPreference(
        settings.notifications.autoRedirect
      );

      // Dispatch custom event to notify other components of settings changes
      if (typeof window !== 'undefined') {
        window.dispatchEvent(new CustomEvent('settingsUpdated'));
      }

      // In a real app, you would also save to the backend
      // await authService.updateUserSettings(settings);

      setHasChanges(false);

      // Play success sound if enabled
      if (settings.display.soundEnabled) {
        playSound('success');
      }

      return Promise.resolve();
    } catch (error) {
      console.error('Error saving settings:', error);
      throw error;
    }
  };

  const resetToDefaults = () => {
    setSettings(defaultSettings);
    setHasChanges(true);
    // Also reset theme to system
    setTheme('system');
  };

  // Sound playback function
  const playSound = (
    soundType: 'notification' | 'success' | 'error' | 'click'
  ) => {
    if (!settings.display.soundEnabled) return;

    try {
      const audio = new Audio(soundEffects[soundType]);
      audio.volume = 0.5;
      audio.play().catch(() => {
        // Silently fail if sound can't be played
      });
    } catch (error) {
      // Silently fail if audio creation fails
    }
  }; // Language change function
  const changeLanguage = (newLocale: string) => {
    try {
      // Store language preference in localStorage
      localStorage.setItem('preferredLanguage', newLocale);

      // Dispatch custom event to notify providers of language change
      if (typeof window !== 'undefined') {
        window.dispatchEvent(new CustomEvent('settingsUpdated'));
      }

      // Show success message
      console.log(`Language changed to: ${newLocale}`);

      // Play sound
      playSound('click');

      // For now, still use reload but with better error handling
      // In the future, this could be replaced with dynamic message loading
      setTimeout(() => {
        try {
          window.location.reload();
        } catch (error) {
          console.error('Error reloading page:', error);
        }
      }, 150);
    } catch (error) {
      console.error('Error changing language:', error);
    }
  };

  const value: SettingsContextType = {
    settings,
    updateSetting,
    saveSettings,
    resetToDefaults,
    hasChanges,
    isLoading,
    playSound,
    // Theme functions
    setTheme,
    isDarkMode,
    currentTheme: theme,
    // Language functions
    currentLocale: locale,
    changeLanguage,
  };

  return (
    <SettingsContext.Provider value={value}>
      {children}
    </SettingsContext.Provider>
  );
}

export const useSettings = () => {
  const context = useContext(SettingsContext);
  if (context === undefined) {
    throw new Error('useSettings must be used within a SettingsProvider');
  }
  return context;
};

// Hook for easy theme detection
export const useSettingsTheme = () => {
  const { isDarkMode, currentTheme, setTheme } = useSettings();

  const toggleTheme = () => {
    const newTheme = isDarkMode ? 'light' : 'dark';
    setTheme(newTheme);
  };

  return {
    isDarkMode,
    theme: currentTheme,
    toggleTheme,
    setTheme,
  };
};

// Hook for easy language management
export const useSettingsLanguage = () => {
  const { currentLocale, changeLanguage } = useSettings();

  return {
    currentLanguage: currentLocale,
    setLanguage: changeLanguage,
  };
};
