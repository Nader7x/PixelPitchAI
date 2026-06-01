import createMiddleware from 'next-intl/middleware';
import type { NextRequest } from 'next/server';

const intlMiddleware = createMiddleware({
  // A list of all locales that are supported
  locales: ['en', 'es', 'fr'],

  // Used when no locale matches
  defaultLocale: 'en',

  // Don't redirect if the locale is already in the pathname
  localePrefix: 'as-needed',
});

export function proxy(request: NextRequest) {
  return intlMiddleware(request);
}

export const config = {
  // Temporarily disable proxy for all routes
  matcher: [],
};
