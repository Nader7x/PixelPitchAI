'use client';

import React, {
  createContext,
  useContext,
  useEffect,
  useState,
  ReactNode,
} from 'react';
import notificationService from '@/Services/NotificationService';

export interface UserSettings {
  theme: 'light' | 'dark' | 'system';
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
    language: string;
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
  theme: 'system',
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
    language: 'en',
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
  isDarkMode: boolean;
  currentLanguage: string;
  t: (key: string, fallback?: string) => string;
  playSound: (
    soundType: 'notification' | 'success' | 'error' | 'click'
  ) => void;
}

const SettingsContext = createContext<SettingsContextType | undefined>(
  undefined
);

// Language translations
const translations: Record<string, Record<string, string>> = {
  en: {
    // Navigation
    'nav.dashboard': 'Dashboard',
    'nav.teams': 'Teams',
    'nav.schedule': 'Schedule',
    'nav.players': 'Players',
    'nav.coaches': 'Coaches',
    'nav.stadiums': 'Stadiums',
    'nav.settings': 'Settings',
    'nav.notifications': 'Notifications',
    'nav.search': 'Search',
    'nav.shop': 'Shop',
    'nav.admin': 'Admin Dashboard',

    // Common
    'common.save': 'Save',
    'common.cancel': 'Cancel',
    'common.loading': 'Loading...',
    'common.error': 'Error',
    'common.success': 'Success',
    'common.confirm': 'Confirm',
    'common.delete': 'Delete',
    'common.edit': 'Edit',
    'common.view': 'View',
    'common.close': 'Close',

    // Settings
    'settings.title': 'Settings & Preferences',
    'settings.subtitle': 'Customize your Football Simulation experience',
    'settings.general': 'General Settings',
    'settings.notifications': 'Notification Preferences',
    'settings.privacy': 'Privacy Settings',
    'settings.display': 'Display Settings',
    'settings.account': 'Account Security',
    'settings.theme': 'Theme',
    'settings.language': 'Language',
    'settings.timezone': 'Timezone',
    'settings.sound': 'Sound Effects',
    'settings.saved': 'Settings saved successfully!',
    'settings.reset': 'Settings reset to defaults',

    // Themes
    'theme.light': 'Light',
    'theme.dark': 'Dark',
    'theme.system': 'System Default',

    // Dashboard
    'dashboard.welcome': 'Welcome to PixelPitchAI',
    'dashboard.simulate': 'Simulate New Matches',
    'dashboard.viewDetails': 'View Match Details',
    'dashboard.liveMatches': 'Live Matches',
    'dashboard.latestMatches': 'Latest Matches',
  },
  es: {
    // Navigation
    'nav.dashboard': 'Panel de Control',
    'nav.teams': 'Equipos',
    'nav.schedule': 'Calendario',
    'nav.players': 'Jugadores',
    'nav.coaches': 'Entrenadores',
    'nav.stadiums': 'Estadios',
    'nav.settings': 'Configuración',
    'nav.notifications': 'Notificaciones',
    'nav.search': 'Buscar',
    'nav.shop': 'Tienda',
    'nav.admin': 'Panel de Administrador',

    // Common
    'common.save': 'Guardar',
    'common.cancel': 'Cancelar',
    'common.loading': 'Cargando...',
    'common.error': 'Error',
    'common.success': 'Éxito',
    'common.confirm': 'Confirmar',
    'common.delete': 'Eliminar',
    'common.edit': 'Editar',
    'common.view': 'Ver',
    'common.close': 'Cerrar',

    // Settings
    'settings.title': 'Configuración y Preferencias',
    'settings.subtitle': 'Personaliza tu experiencia de Simulación de Fútbol',
    'settings.general': 'Configuración General',
    'settings.notifications': 'Preferencias de Notificación',
    'settings.privacy': 'Configuración de Privacidad',
    'settings.display': 'Configuración de Pantalla',
    'settings.account': 'Seguridad de la Cuenta',
    'settings.theme': 'Tema',
    'settings.language': 'Idioma',
    'settings.timezone': 'Zona Horaria',
    'settings.sound': 'Efectos de Sonido',
    'settings.saved': '¡Configuración guardada exitosamente!',
    'settings.reset': 'Configuración restablecida a valores predeterminados',

    // Themes
    'theme.light': 'Claro',
    'theme.dark': 'Oscuro',
    'theme.system': 'Predeterminado del Sistema',

    // Dashboard
    'dashboard.welcome': 'Bienvenido a PixelPitchAI',
    'dashboard.simulate': 'Simular Nuevos Partidos',
    'dashboard.viewDetails': 'Ver Detalles del Partido',
    'dashboard.liveMatches': 'Partidos en Vivo',
    'dashboard.latestMatches': 'Últimos Partidos',
  },
  fr: {
    // Navigation
    'nav.dashboard': 'Tableau de Bord',
    'nav.teams': 'Équipes',
    'nav.schedule': 'Calendrier',
    'nav.players': 'Joueurs',
    'nav.coaches': 'Entraîneurs',
    'nav.stadiums': 'Stades',
    'nav.settings': 'Paramètres',
    'nav.notifications': 'Notifications',
    'nav.search': 'Rechercher',
    'nav.shop': 'Boutique',
    'nav.admin': 'Tableau de Bord Admin',

    // Common
    'common.save': 'Sauvegarder',
    'common.cancel': 'Annuler',
    'common.loading': 'Chargement...',
    'common.error': 'Erreur',
    'common.success': 'Succès',
    'common.confirm': 'Confirmer',
    'common.delete': 'Supprimer',
    'common.edit': 'Modifier',
    'common.view': 'Voir',
    'common.close': 'Fermer',

    // Settings
    'settings.title': 'Paramètres et Préférences',
    'settings.subtitle':
      'Personnalisez votre expérience de Simulation de Football',
    'settings.general': 'Paramètres Généraux',
    'settings.notifications': 'Préférences de Notification',
    'settings.privacy': 'Paramètres de Confidentialité',
    'settings.display': "Paramètres d'Affichage",
    'settings.account': 'Sécurité du Compte',
    'settings.theme': 'Thème',
    'settings.language': 'Langue',
    'settings.timezone': 'Fuseau Horaire',
    'settings.sound': 'Effets Sonores',
    'settings.saved': 'Paramètres sauvegardés avec succès !',
    'settings.reset': 'Paramètres réinitialisés aux valeurs par défaut',

    // Themes
    'theme.light': 'Clair',
    'theme.dark': 'Sombre',
    'theme.system': 'Défaut du Système',

    // Dashboard
    'dashboard.welcome': 'Bienvenue sur PixelPitchAI',
    'dashboard.simulate': 'Simuler de Nouveaux Matchs',
    'dashboard.viewDetails': 'Voir les Détails du Match',
    'dashboard.liveMatches': 'Matchs en Direct',
    'dashboard.latestMatches': 'Derniers Matchs',
  },
};

// Sound effects URLs
const soundEffects = {
  notification: '/sounds/notification.mp3',
  success: '/sounds/success.mp3',
  error: '/sounds/error.mp3',
  click: '/sounds/click.mp3',
};

export function SettingsProvider({ children }: { children: ReactNode }) {
  const [settings, setSettings] = useState<UserSettings>(defaultSettings);
  const [hasChanges, setHasChanges] = useState(false);
  const [isLoading, setIsLoading] = useState(true);
  const [isDarkMode, setIsDarkMode] = useState(false);

  // Load settings on mount
  useEffect(() => {
    loadSettings();
  }, []);

  // Apply theme changes
  useEffect(() => {
    applyTheme();
  }, [settings.theme]);

  // Apply language changes
  useEffect(() => {
    applyLanguage();
  }, [settings.display.language]);

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

  const applyTheme = () => {
    const root = document.documentElement;

    if (settings.theme === 'dark') {
      root.classList.add('dark');
      setIsDarkMode(true);
    } else if (settings.theme === 'light') {
      root.classList.remove('dark');
      setIsDarkMode(false);
    } else {
      // System theme
      const prefersDark = window.matchMedia(
        '(prefers-color-scheme: dark)'
      ).matches;
      if (prefersDark) {
        root.classList.add('dark');
        setIsDarkMode(true);
      } else {
        root.classList.remove('dark');
        setIsDarkMode(false);
      }
    }
  };

  const applyLanguage = () => {
    // Set document language
    document.documentElement.lang = settings.display.language;

    // You could also update meta tags here for SEO
    const metaLang = document.querySelector('meta[name="language"]');
    if (metaLang) {
      metaLang.setAttribute('content', settings.display.language);
    }
  };

  const updateSetting = (path: string, value: any) => {
    setSettings((prev) => {
      const newSettings = { ...prev };
      const keys = path.split('.');
      let current: any = newSettings;

      for (let i = 0; i < keys.length - 1; i++) {
        current = current[keys[i]];
      }

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
  };

  // Translation function
  const t = (key: string, fallback?: string): string => {
    const langTranslations =
      translations[settings.display.language] || translations.en;
    return langTranslations[key] || fallback || key;
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
  };

  const value: SettingsContextType = {
    settings,
    updateSetting,
    saveSettings,
    resetToDefaults,
    hasChanges,
    isLoading,
    isDarkMode,
    currentLanguage: settings.display.language,
    t,
    playSound,
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
export const useTheme = () => {
  const { isDarkMode, settings, updateSetting } = useSettings();

  const toggleTheme = () => {
    const newTheme = isDarkMode ? 'light' : 'dark';
    updateSetting('theme', newTheme);
  };

  return {
    isDarkMode,
    theme: settings.theme,
    toggleTheme,
    setTheme: (theme: 'light' | 'dark' | 'system') =>
      updateSetting('theme', theme),
  };
};

// Hook for easy language management
export const useLanguage = () => {
  const { currentLanguage, t, updateSetting } = useSettings();

  const setLanguage = (language: string) => {
    updateSetting('display.language', language);
  };

  return {
    currentLanguage,
    setLanguage,
    t,
  };
};
