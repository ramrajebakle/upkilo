const createNextIntlPlugin = require('next-intl/plugin');
const { withSentryConfig } = require('@sentry/nextjs');
const withNextIntl = createNextIntlPlugin('./i18n/request.ts');

/** @type {import('next').NextConfig} */
const nextConfig = {
  output: 'standalone',
  reactStrictMode: true,
  transpilePackages: ['recharts'],
  images: {
    remotePatterns: [
      { protocol: 'https', hostname: 'upkilo.com' },
      { protocol: 'https', hostname: 'storage.upkilo.com' },
      { protocol: 'https', hostname: '*.blob.core.windows.net' },
      { protocol: 'https', hostname: 'upload.wikimedia.org' },
      { protocol: 'https', hostname: 'lh3.googleusercontent.com' },
      { protocol: 'http',  hostname: 'localhost' },
      // User-uploaded logos from any CDN — use unoptimized prop on those components
    ],
  },
  env: {
    NEXT_PUBLIC_API_URL:  process.env.NEXT_PUBLIC_API_URL  || 'http://localhost:5000',
    NEXT_PUBLIC_SITE_URL: process.env.NEXT_PUBLIC_SITE_URL || 'https://upkilo.com',
  },
  async rewrites() {
    return [
      {
        // Proxy only backend routes — /api/v1/* exclusively.
        // Do NOT use /api/* here: it would swallow /api/auth/* (NextAuth) and
        // proxy them to the .NET backend, which returns 500 for unknown routes.
        source: '/api/v1/:path*',
        destination: `${process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000'}/api/v1/:path*`,
      },
    ];
  },

  async headers() {
    return [
      {
        // Apple App Site Association must be served as application/json without the .json extension
        source: '/.well-known/apple-app-site-association',
        headers: [
          { key: 'Content-Type', value: 'application/json' },
          { key: 'Cache-Control', value: 'public, max-age=3600' },
        ],
      },
      {
        source: '/.well-known/assetlinks.json',
        headers: [
          { key: 'Content-Type', value: 'application/json' },
          { key: 'Cache-Control', value: 'public, max-age=3600' },
        ],
      },
    ];
  },
};

module.exports = withSentryConfig(withNextIntl(nextConfig), {
  // Sentry org/project — set SENTRY_ORG and SENTRY_PROJECT env vars in CI.
  silent: true,
  // Upload source maps to Sentry on every production build so stack traces
  // show original TypeScript file/line rather than minified bundle positions.
  widenClientFileUpload: true,
  hideSourceMaps: true,
  disableLogger: true,
});
