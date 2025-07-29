import { getRequestConfig } from 'next-intl/server';
import { notFound } from 'next/navigation';

// Can be imported from a shared config
const locales = ['en', 'es', 'fr'];

export default getRequestConfig(async ({ locale }) => {
  // Validate that the incoming `locale` parameter is valid
  // If not valid, default to 'en' instead of throwing notFound()
  const validLocale = locales.includes(locale as any) ? locale : 'en';

  return {
    locale: validLocale!,
    messages: (await import(`./messages/${validLocale}.json`)).default,
    timeZone: 'UTC', // Global default timezone to prevent markup mismatches
  };
});
