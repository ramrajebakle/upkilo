// Cache version is auto-updated by scripts/update-sw-version.js after every Next.js build.
// The activate handler deletes all caches whose name differs from CACHE_NAME,
// ensuring users receive fresh assets on deployment.
const CACHE_NAME = 'upkilo-IXyO2TY3m19ajbnv7wZX3';

// Shell resources cached on install for offline access.
// Includes all manifest icon sizes so home-screen icons work offline.
//
// '/' is deliberately NOT here. On app.upkilo.com it answers 308 -> https://upkilo.com/,
// a CROSS-ORIGIN redirect; the resulting fetch is blocked by CORS, and because addAll() is
// atomic that single rejection failed the whole install — so the service worker never
// registered and there was no offline support at all, reported only as
// "TypeError: Failed to fetch" from this file. The locale-prefixed routes below are
// same-origin redirects, which fetch follows normally.
const SHELL_URLS = [
  '/dashboard',
  '/offline',
  '/manifest.json',
  '/icons/icon-72x72.png',
  '/icons/icon-96x96.png',
  '/icons/icon-128x128.png',
  '/icons/icon-144x144.png',
  '/icons/icon-152x152.png',
  '/icons/icon-192x192.png',
  '/icons/icon-384x384.png',
  '/icons/icon-512x512.png',
];

// Cached individually rather than with addAll().
//
// addAll() resolves only if EVERY request succeeds, so one unreachable or cross-origin-
// redirecting URL takes the entire install down with it and leaves the app with no service
// worker. Precaching is an optimisation; losing one shell entry should degrade offline
// coverage, never prevent registration. allSettled keeps the install succeeding and logs
// whatever could not be cached, so a bad entry is visible instead of fatal.
self.addEventListener('install', (event) => {
  event.waitUntil(
    caches.open(CACHE_NAME).then(async (cache) => {
      const results = await Promise.allSettled(SHELL_URLS.map((url) => cache.add(url)));
      const failed = results
        .map((r, i) => (r.status === 'rejected' ? SHELL_URLS[i] : null))
        .filter(Boolean);
      if (failed.length > 0) {
        console.warn('[sw] precache skipped:', failed.join(', '));
      }
    })
  );
  self.skipWaiting();
});

self.addEventListener('activate', (event) => {
  event.waitUntil(
    caches.keys().then((keys) =>
      Promise.all(keys.filter((k) => k !== CACHE_NAME).map((k) => caches.delete(k)))
    )
  );
  self.clients.claim();
});

// Network-first for API calls; cache-first for static assets
self.addEventListener('fetch', (event) => {
  const { request } = event;
  const url = new URL(request.url);

  // Skip non-GET and cross-origin requests
  if (request.method !== 'GET' || url.origin !== self.location.origin) return;

  // API GET calls — stale-while-revalidate for reads; skip mutations
  if (url.pathname.startsWith('/api/')) {
    if (request.method !== 'GET') return;
    event.respondWith(
      caches.match(request).then((cached) => {
        const networkFetch = fetch(request)
          .then((response) => {
            if (response.ok) {
              const clone = response.clone();
              caches.open(CACHE_NAME).then((cache) => cache.put(request, clone));
            }
            return response;
          })
          .catch(() => cached || new Response(JSON.stringify({ error: 'offline' }), {
            status: 503,
            headers: { 'Content-Type': 'application/json' },
          }));
        // Return cached immediately while revalidating in background
        return cached ? cached : networkFetch;
      })
    );
    return;
  }

  event.respondWith(
    caches.match(request).then((cached) => {
      const networkFetch = fetch(request)
        .then((response) => {
          if (response.ok) {
            const clone = response.clone();
            caches.open(CACHE_NAME).then((cache) => cache.put(request, clone));
          }
          return response;
        })
        .catch(async () => {
          if (cached) return cached;
          // For page navigation, serve the offline fallback page
          if (request.mode === 'navigate') {
            const offline = await caches.match('/offline');
            if (offline) return offline;
          }
          return new Response('Offline — no cached version available', {
            status: 503,
            statusText: 'Service Unavailable',
            headers: { 'Content-Type': 'text/plain' },
          });
        });

      // For navigation: network first so user always gets latest HTML
      return request.mode === 'navigate'
        ? networkFetch
        : cached || networkFetch;
    })
  );
});

// Push notifications
self.addEventListener('push', (event) => {
  if (!event.data) return;
  const data = event.data.json();
  event.waitUntil(
    self.registration.showNotification(data.title || 'Upkilo', {
      body: data.message || '',
      icon: '/icons/icon-192x192.png',
      badge: '/icons/icon-72x72.png',
      tag: data.tag || `upkilo-${Date.now()}-${Math.random().toString(36).slice(2)}`,
      data: { url: data.actionUrl || '/dashboard' },
      actions: data.actions || [],
    })
  );
});

self.addEventListener('notificationclick', (event) => {
  event.notification.close();
  const targetUrl = event.notification.data?.url || '/dashboard';
  event.waitUntil(
    self.clients.matchAll({ type: 'window', includeUncontrolled: true }).then((windowClients) => {
      const existing = windowClients.find((c) => c.url.includes(targetUrl) && 'focus' in c);
      return existing ? existing.focus() : self.clients.openWindow(targetUrl);
    })
  );
});
