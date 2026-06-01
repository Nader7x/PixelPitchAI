import type { NextConfig } from 'next';
import withNextIntl from 'next-intl/plugin';

const withNextIntlConfig = withNextIntl('./i18n.ts');

const nextConfig: NextConfig = {
  // Docker support - enables standalone output for container deployment
  output: process.env.NODE_ENV === 'production' ? 'standalone' : undefined,

  // Network configuration for Docker
  ...(process.env.NODE_ENV === 'production' && {
    experimental: {
      outputFileTracingRoot: process.cwd(),
    },
  }),

  // Security headers for production
  async headers() {
    if (process.env.NODE_ENV === 'production') {
      return [
        {
          source: '/(.*)',
          headers: [
            {
              key: 'X-Frame-Options',
              value: 'SAMEORIGIN',
            },
            {
              key: 'X-Content-Type-Options',
              value: 'nosniff',
            },
            {
              key: 'Referrer-Policy',
              value: 'strict-origin-when-cross-origin',
            },
            {
              key: 'Permissions-Policy',
              value: 'camera=(), microphone=(), geolocation=()',
            },
          ],
        },
      ];
    }
    return [];
  },

  images: {
    remotePatterns: [
      {
        protocol: 'https',
        hostname: 't4.ftcdn.net',
      },
      {
        protocol: 'https',
        hostname: 'img.freepik.com',
      },
      {
        protocol: 'https',
        hostname: 'encrypted-tbn0.gstatic.com',
      },
    ],
    // Docker network support
    unoptimized: process.env.NODE_ENV === 'development',
  },
  eslint: {
    // Warning: This allows production builds to successfully complete even if
    // your project has ESLint errors.
    ignoreDuringBuilds: true,
  },
  // Turbopack configuration (moved from experimental.turbo)
  turbopack: {
    rules: {
      '*.wasm': {
        loaders: ['@vercel/webpack-loader-wasm'],
        as: '*.wasm',
      },
    },
  },
  experimental: {
    // Other experimental features can be added here if needed
  },
  // Optimize bundle splitting
  webpack: (config, { dev, isServer }) => {
    // Optimize Three.js and related packages
    if (!isServer) {
      config.resolve.alias = {
        ...config.resolve.alias,
        'three/examples/jsm': 'three/examples/jsm',
      };

      // Split vendor chunks for better caching
      config.optimization.splitChunks = {
        ...config.optimization.splitChunks,
        cacheGroups: {
          ...config.optimization.splitChunks.cacheGroups,
          three: {
            name: 'three',
            chunks: 'all',
            test: /[\\/]node_modules[\\/](three|@react-three)[\\/]/,
            priority: 20,
            reuseExistingChunk: true,
          },
          vendor: {
            name: 'vendor',
            chunks: 'all',
            test: /[\\/]node_modules[\\/]/,
            priority: 10,
            reuseExistingChunk: true,
          },
        },
      };
    }

    return config;
  },
  // Performance optimizations
  poweredByHeader: false,
  compress: true,
};

export default withNextIntlConfig(nextConfig);
