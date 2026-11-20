'use client';

import { useState, useEffect, ReactNode } from 'react';
import { NextIntlClientProvider } from 'next-intl';
import { ThemeProvider } from 'next-themes';
import { Toaster } from 'react-hot-toast';
import { SettingsProvider } from './contexts/EnhancedSettingsContext';

// Import messages
import enMessages from '../messages/en.json';
import esMessages from '../messages/es.json';
import frMessages from '../messages/fr.json';

// Get locale data from localStorage
function getClientSideLocaleData() {
  let selectedLocale = 'en';
  let userTimeZone = 'UTC';

  try {
    if (typeof window !== 'undefined') {
      selectedLocale = localStorage.getItem('preferredLanguage') || 'en';

      // Try to get timezone from user settings or browser
      try {
        const userSettings = localStorage.getItem('userSettings');
        if (userSettings) {
          const settings = JSON.parse(userSettings);
          userTimeZone = settings?.display?.timezone || 'UTC';
        } else {
          // Fallback to browser timezone
          userTimeZone =
            Intl.DateTimeFormat().resolvedOptions().timeZone || 'UTC';
        }
      } catch (error) {
        console.warn('Error reading timezone from settings:', error);
        userTimeZone = 'UTC';
      }
    }
  } catch (error) {
    console.warn('Error reading locale data:', error);
    selectedLocale = 'en';
    userTimeZone = 'UTC';
  }

  const messages = {
    en: enMessages,
    es: esMessages,
    fr: frMessages,
  };

  // Ensure we have a valid locale
  const validLocale = ['en', 'es', 'fr'].includes(selectedLocale)
    ? selectedLocale
    : 'en';

  return {
    locale: validLocale,
    messages: messages[validLocale as keyof typeof messages] || enMessages,
    timeZone: userTimeZone,
  };
}

export function ClientProviders({ children }: { children: ReactNode }) {
  const [localeData, setLocaleData] = useState(() => ({
    locale: 'en',
    messages: enMessages,
    timeZone: 'UTC',
  }));
  useEffect(() => {
    // Update locale data on client side
    const clientData = getClientSideLocaleData();
    setLocaleData(clientData); // Listen for changes to user settings (timezone, language)
    const handleStorageChange = (e: StorageEvent) => {
      try {
        if (e.key === 'userSettings' || e.key === 'preferredLanguage') {
          const updatedData = getClientSideLocaleData();
          setLocaleData(updatedData);
        }
      } catch (error) {
        console.warn('Error handling storage change:', error);
      }
    };

    // Listen for settings changes in the same tab
    const handleSettingsUpdate = () => {
      try {
        // Add a small delay to ensure localStorage is updated
        setTimeout(() => {
          const finalData = getClientSideLocaleData();
          setLocaleData(finalData);
        }, 50);
      } catch (error) {
        console.warn('Error handling settings update:', error);
      }
    };

    // Listen for storage changes from other tabs
    window.addEventListener('storage', handleStorageChange);

    // Custom event for same-tab settings changes
    window.addEventListener('settingsUpdated', handleSettingsUpdate);

    return () => {
      window.removeEventListener('storage', handleStorageChange);
      window.removeEventListener('settingsUpdated', handleSettingsUpdate);
    };
  }, []);
  return (
    <NextIntlClientProvider
      locale={localeData.locale}
      messages={localeData.messages}
      timeZone={localeData.timeZone}
    >
      <ThemeProvider
        attribute="class"
        defaultTheme="system"
        enableSystem
        disableTransitionOnChange={false}
      >
        <SettingsProvider>
          {children}
          <Toaster
            position="top-right"
            toastOptions={{
              duration: 4000,
              className: '',
              style: {},
            }}
          />
        </SettingsProvider>
      </ThemeProvider>
    </NextIntlClientProvider>
  );
}
