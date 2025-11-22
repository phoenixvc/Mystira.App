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
// NOTE: index.html and _framework/blazor.webassembly.js are NOT cached to avoid SRI issues
// These files should always be fetched from the network to ensure fresh SRI hashes
const coreFilesToCache = [
    // Root manifest and CSS
    './manifest.json',
    './css/app.css',
    // Essential JS (non-framework)
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
                    // Only clear caches that don't match our current cache names
                    if (cache !== ICON_CACHE_NAME && cache !== CORE_CACHE_NAME) {
                        console.log('Service Worker: Clearing Old Cache:', cache);
                        return caches.delete(cache);
                    }
                })
            );
        })
        .then(() => console.log('Service Worker: Cache cleanup completed'))
    );
});

// Fetch event - Handle different resource types appropriately
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

    // Check if request is for HTML files (including index.html, .html.br, .html.gz)
    // These should NOT be cached to avoid SRI integrity issues
    const isHtmlFile = url.pathname.endsWith('.html') || 
                      url.pathname.endsWith('.html.br') ||
                      url.pathname.endsWith('.html.gz') ||
                      url.pathname === '/';

    // Check if request is for framework files (.wasm, blazor.webassembly.js, etc.)
    // These should NOT be cached to ensure SRI hashes are always fresh
    const isFrameworkFile = url.pathname.includes('/_framework/') && 
                           (url.pathname.endsWith('.wasm') || 
                            url.pathname.endsWith('.wasm.br') ||
                            url.pathname.endsWith('.wasm.gz') ||
                            url.pathname.includes('blazor.webassembly.js') ||
                            url.pathname.endsWith('.js.br') ||
                            url.pathname.endsWith('.js.gz') ||
                            url.pathname.endsWith('.dll.br') ||
                            url.pathname.endsWith('.dll.gz'));

    // Check if request is for core PWA files (non-HTML, non-framework)
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
    } else if (isHtmlFile || isFrameworkFile) {
        // For HTML and framework files, use network-first strategy
        // This ensures fresh SRI hashes are always used
        event.respondWith(
            fetch(event.request, { 
                cache: 'no-store',  // Bypass HTTP cache completely
                headers: {
                    'Cache-Control': 'no-cache, no-store, must-revalidate'
                }
            })
            .then(response => {
                // Return the network response if successful
                if (!response || response.status !== 200) {
                    return response;
                }
                return response;
            })
            .catch((error) => {
                console.warn('Network request failed for', event.request.url, error);
                // If network fails, try to return cached version as fallback
                return caches.match(event.request)
                    .then(cachedResponse => {
                        if (cachedResponse) {
                            console.log('Serving from cache fallback:', event.request.url);
                            return cachedResponse;
                        }
                        // If nothing is cached and network fails, fail gracefully
                        throw new Error('Failed to fetch ' + event.request.url);
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

// Handle messages from clients
self.addEventListener('message', event => {
    if (event?.data?.type === 'CLEAR_CACHES') {
        console.log('[Mystira ServiceWorker] Received CLEAR_CACHES message - but being conservative to prevent race conditions');
        // Don't clear caches aggressively to prevent SRI race conditions
        // Cache clearing should only happen during service worker updates, not on every page load
    }
});

