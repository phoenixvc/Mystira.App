// Service Worker for PWA - Enhanced Caching for Installation Support

// Cache names
const ICON_CACHE_NAME = 'pwa-icon-cache-v1';
const CORE_CACHE_NAME = 'pwa-core-cache-v1';

// Files to cache (essential PWA files)
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

// Core PWA files to cache for offline functionality
const coreFilesToCache = [
    // Core application files
    './',
    './index.html',
    './manifest.json',
    // Framework files
    './_framework/blazor.webassembly.js',
    // CSS
    './css/app.css',
    // Essential JS
    './js/pwaInstall.js',
    './js/imageCacheManager.js',
    './js/audioPlayer.js',
    './dice.js'
];

// Install event - Cache essential PWA files
self.addEventListener('install', event => {
    console.log('Service Worker: Installing...');

    // Skip waiting to ensure the latest service worker activates immediately
    self.skipWaiting();

    event.waitUntil(
        Promise.all([
            // Cache icons
            caches.open(ICON_CACHE_NAME)
                .then(cache => {
                    console.log('Service Worker: Caching Icon Files');
                    return cache.addAll(filesToCache);
                }),
            // Cache core files
            caches.open(CORE_CACHE_NAME)
                .then(cache => {
                    console.log('Service Worker: Caching Core Files');
                    return cache.addAll(coreFilesToCache);
                })
        ])
        .then(() => console.log('Service Worker: All Essential Files Cached'))
        .catch(error => console.error('Failed to cache PWA assets:', error))
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
                    if (cache !== ICON_CACHE_NAME && cache !== CORE_CACHE_NAME) {
                        console.log('Service Worker: Clearing Old Cache:', cache);
                        return caches.delete(cache);
                    }
                })
            );
        })
    );

    return self.clients.claim();
});

// Fetch event - Cache-first for core files, icons from cache, network fallback
self.addEventListener('fetch', event => {
    // Skip cross-origin requests
    if (!event.request.url.startsWith(self.location.origin)) {
        return;
    }

    // Skip non-GET requests
    if (event.request.method !== 'GET') {
        return;
    }

    const url = new URL(event.request.url);
    
    // Check if the request is for an icon file
    const isIcon = url.pathname.match(/\.(ico|png)$/i) &&
        (url.pathname.includes('/icons/') || url.pathname.includes('/favicon.ico'));

    // Check if request is for core PWA files
    const isCoreFile = coreFilesToCache.some(file => {
        const coreUrl = new URL(file, self.location.origin);
        return url.pathname === coreUrl.pathname;
    });

    if (isIcon) {
        // For icons, use cache-first strategy
        event.respondWith(
            caches.match(event.request)
                .then(cachedResponse => {
                    if (cachedResponse) {
                        return cachedResponse;
                    }

                    // If not in cache, fetch from network and cache it
                    return fetch(event.request)
                        .then(response => {
                            if (!response || response.status !== 200) {
                                return response;
                            }

                            const responseToCache = response.clone();
                            caches.open(ICON_CACHE_NAME)
                                .then(cache => cache.put(event.request, responseToCache));

                            return response;
                        });
                })
        );
    } else if (isCoreFile) {
        // For core files, use cache-first strategy with network fallback
        event.respondWith(
            caches.match(event.request)
                .then(cachedResponse => {
                    if (cachedResponse) {
                        return cachedResponse;
                    }

                    // If not in cache, fetch from network and cache it
                    return fetch(event.request)
                        .then(response => {
                            if (!response || response.status !== 200) {
                                return response;
                            }

                            const responseToCache = response.clone();
                            caches.open(CORE_CACHE_NAME)
                                .then(cache => cache.put(event.request, responseToCache));

                            return response;
                        })
                        .catch(() => {
                            // If network fails and it's the index.html, serve cached version
                            if (url.pathname === '/' || url.pathname.endsWith('index.html')) {
                                return caches.match('/index.html');
                            }
                        });
                })
        );
    } else {
        // For all other requests, use network-only strategy
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