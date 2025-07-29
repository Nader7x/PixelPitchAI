import createMiddleware from 'next-intl/middleware';

export default createMiddleware({
  // A list of all locales that are supported
  locales: ['en', 'es', 'fr'],

  // Used when no locale matches
  defaultLocale: 'en',

  // Don't redirect if the locale is already in the pathname
  localePrefix: 'as-needed',
});

export const config = {
  // Temporarily disable middleware for all routes
  matcher: [],
};
