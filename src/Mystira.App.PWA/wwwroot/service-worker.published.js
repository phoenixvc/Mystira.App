// Blazor PWA default published service worker

self.importScripts('./service-worker-assets.js');

const CACHE_NAME = 'offline-cache-v1';
const ASSETS = self.assetsManifest
    ? self.assetsManifest.assets.map(a => new URL(a.url, self.location).toString())
    : [];

self.addEventListener('install', event => {
    event.waitUntil((async () => {
        const cache = await caches.open(CACHE_NAME);
        await cache.addAll(['./', ...ASSETS]);
        await self.skipWaiting();
    })());
});

self.addEventListener('activate', event => {
    event.waitUntil((async () => {
        // delete old caches
        const keys = await caches.keys();
        await Promise.all(keys.filter(k => k !== CACHE_NAME).map(k => caches.delete(k)));
        await self.clients.claim();
    })());
});

self.addEventListener('fetch', event => {
    const { request } = event;

    // Never cache auth or SW-related endpoints
    const noCache = ['/authentication/', '/callback', '/signin-', '/signout-'];
    if (noCache.some(p => request.url.includes(p))) return;

    event.respondWith((async () => {
        // Network-first for navigation/HTML, cache-first for static assets
        if (request.mode === 'navigate' || (request.headers.get('Accept') || '').includes('text/html')) {
            try {
                const networkResponse = await fetch(request);
                const cache = await caches.open(CACHE_NAME);
                cache.put(request, networkResponse.clone());
                return networkResponse;
            } catch {
                const cache = await caches.open(CACHE_NAME);
                return (await cache.match(request)) || (await cache.match('./'));
            }
        } else {
            const cache = await caches.open(CACHE_NAME);
            const cached = await cache.match(request);
            if (cached) return cached;
            const networkResponse = await fetch(request);
            // Only cache OK, same-origin static resources
            if (networkResponse.ok && new URL(request.url).origin === location.origin) {
                cache.put(request, networkResponse.clone());
            }
            return networkResponse;
        }
    })());
});
