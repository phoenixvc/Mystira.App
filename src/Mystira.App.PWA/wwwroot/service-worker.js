// Service Worker for PWA - Limited Caching (Icons Only)

// Cache names
const ICON_CACHE_NAME = 'pwa-icon-cache-v1';

// Files to cache (icons only)
const filesToCache = [
    // PWA Icons
    './icons/apple-icon-180.png',
    './icons/icon-192.png',
    './icons/icon-192-maskable.png',
    './icons/icon-384.png',
    './icons/icon-512.png',
    './icons/icon-512-maskable.png',
    './icons/icon-1024.png',
    './icons/favicon.png'
];

// Install event - Cache icon assets only
self.addEventListener('install', event => {
    console.log('Service Worker: Installing...');

    // Skip waiting to ensure the latest service worker activates immediately
    self.skipWaiting();

    event.waitUntil(
        caches.open(ICON_CACHE_NAME)
            .then(cache => {
                console.log('Service Worker: Caching Icon Files');
                return cache.addAll(filesToCache);
            })
            .then(() => console.log('Service Worker: All Icon Files Cached'))
            .catch(error => console.error('Failed to cache icon assets:', error))
    );
});

// Activate event - Clean up old caches
self.addEventListener('activate', event => {
    console.log('Service Worker: Activating...');

    // Take control of all clients immediately
    self.clients.claim();

    event.waitUntil(
        caches.keys().then(cacheNames => {
            return Promise.all(
                cacheNames.map(cache => {
                    if (cache !== ICON_CACHE_NAME) {
                        console.log('Service Worker: Clearing Old Cache:', cache);
                        return caches.delete(cache);
                    }
                })
            );
        })
    );

    return self.clients.claim();
});

// Fetch event - Serve only icon files from cache, everything else from network
self.addEventListener('fetch', event => {
    // Skip cross-origin requests
    if (!event.request.url.startsWith(self.location.origin)) {
        return;
    }

    // Skip non-GET requests
    if (event.request.method !== 'GET') {
        return;
    }

    // Check if the request is for an icon file
    const url = new URL(event.request.url);
    const isIcon = url.pathname.match(/\.(ico|png)$/i) &&
        (url.pathname.includes('/icons/') || url.pathname.includes('/favicon.ico'));

    if (isIcon) {
        // For icons, use cache-first strategy
        event.respondWith(
            caches.match(event.request)
                .then(cachedResponse => {
                    if (cachedResponse) {
                        // Return cached icon
                        return cachedResponse;
                    }

                    // If not in cache, fetch from network and cache it
                    return fetch(event.request)
                        .then(response => {
                            if (!response || response.status !== 200) {
                                return response;
                            }

                            // Clone the response
                            const responseToCache = response.clone();

                            // Add the icon to the cache
                            caches.open(ICON_CACHE_NAME)
                                .then(cache => {
                                    cache.put(event.request, responseToCache);
                                });

                            return response;
                        });
                })
        );
    } else {
        // For non-icon requests, just use the network
        // No caching for other resources
        return;
    }
});

// Optional: handle push notifications
self.addEventListener('push', event => {
    const title = 'Push Notification';
    const options = {
        body: event.data?.text() || 'Notification from the app',
        icon: './icons/icon-192.png'
    };

    event.waitUntil(
        self.registration.showNotification(title, options)
    );
});