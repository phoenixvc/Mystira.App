// Service Worker for PWA - Enhanced Caching for Installation Support + Version checks

const LOG_PREFIX = '[Mystira ServiceWorker]';

// Cache names (bump to v2 to ensure clean slate after fixing duplicate precache entries)
const ICON_CACHE_NAME = 'pwa-icon-cache-v2';
const CORE_CACHE_NAME = 'pwa-core-cache-v2';

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

// Additional assets to cache for performance
const performanceAssets = [
    // Logo and key images (cached separately for instant rendering)
    './icons/icon-512.png',
    './icons/icon-192.png',
    './icons/favicon.png',
    './images/mystira-logo.webp',
    './images/gamesession-background.webp'
];

// Helper to normalize and de-duplicate request URLs
function uniqueUrls(urls) {
    // Normalize to absolute URLs to avoid duplicates like './icons/x.png' vs 'icons/x.png'
    const set = new Set();
    const unique = [];
    for (const u of urls) {
        const abs = new URL(u, self.location.origin).toString();
        if (!set.has(abs)) {
            set.add(abs);
            unique.push(abs);
        }
    }
    return unique;
}

// Install event - Cache essential PWA files
self.addEventListener('install', event => {
    console.log('Service Worker: Installing...');

    // Skip waiting to ensure the latest service worker activates immediately
    self.skipWaiting();

    const iconUrls = uniqueUrls(filesToCache.concat(performanceAssets));
    const coreUrls = uniqueUrls(coreFilesToCache);

    event.waitUntil(
        (async () => {
            try {
                // Cache icons
                const iconCache = await caches.open(ICON_CACHE_NAME);
                console.log('Service Worker: Caching Icon Files');
                await iconCache.addAll(iconUrls);

                // Cache core files
                const coreCache = await caches.open(CORE_CACHE_NAME);
                console.log('Service Worker: Caching Core Files');
                await coreCache.addAll(coreUrls);

                console.log('Service Worker: All Essential Files Cached');
            } catch (error) {
                console.error('Failed to cache PWA assets:', error);
                console.debug('Icon URLs attempted:', iconUrls);
                console.debug('Core URLs attempted:', coreUrls);
            }
        })()
    );
});

// Activate event - Clean up old caches
self.addEventListener('activate', event => {
    console.log('Service Worker: Activating...');

    // Take control of all clients immediately
    self.clients.claim();

    event.waitUntil((async () => {
        // Clean up old caches
        await caches.keys().then(cacheNames => {
            return Promise.all(
                cacheNames.map(cache => {
                    // Only clear caches that don't match our current cache names
                    if (cache !== ICON_CACHE_NAME && cache !== CORE_CACHE_NAME && cache !== 'mystira-version-cache') {
                        console.log('Service Worker: Clearing Old Cache:', cache);
                        return caches.delete(cache);
                    }
                })
            );
        });

        console.log('Service Worker: Cache cleanup completed');

        // On activation, check app version and notify clients if changed
        try {
            const versionResult = await checkVersionAndInvalidate();
            if (versionResult?.versionChanged) {
                await notifyClientsOfUpdate(versionResult);
            }
        } catch (err) {
            console.error(LOG_PREFIX, 'Version check on activate failed', err);
        }
    })());
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

    // Check if request is for version.json - always fetch from network
    const isVersionFile = url.pathname.endsWith('version.json');

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
    } else if (isHtmlFile || isFrameworkFile || isVersionFile) {
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

// ===== Version tracking helpers (ported from published SW) =====
async function clearAllCaches() {
    if (!self.caches) {
        return;
    }
    try {
        const cacheKeys = await caches.keys();
        // Keep the version cache, clear everything else
        const cachesToClear = cacheKeys.filter(key => key !== 'mystira-version-cache');
        if (cachesToClear.length === 0) {
            return;
        }
        await Promise.all(cachesToClear.map((key) => caches.delete(key)));
        console.log(`${LOG_PREFIX} Cleared caches:`, cachesToClear);
    } catch (error) {
        console.error(`${LOG_PREFIX} Failed to clear caches`, error);
    }
}

async function getStoredVersion() {
    try {
        const cache = await caches.open('mystira-version-cache');
        const response = await cache.match('version-key');
        if (response) {
            const data = await response.json();
            return data.version;
        }
        return null;
    } catch (error) {
        console.error(`${LOG_PREFIX} Failed to get stored version:`, error);
        return null;
    }
}

async function setStoredVersion(version) {
    try {
        const cache = await caches.open('mystira-version-cache');
        const response = new Response(JSON.stringify({ version }), {
            headers: { 'Content-Type': 'application/json' }
        });
        await cache.put('version-key', response);
    } catch (error) {
        console.error(`${LOG_PREFIX} Failed to store version:`, error);
    }
}

async function checkVersionAndInvalidate() {
    try {
        // Fetch the version file with cache-busting
        const response = await fetch(`./version.json?t=${Date.now()}`, {
            cache: 'no-store'
        });

        if (!response.ok) {
            console.log(`${LOG_PREFIX} Could not fetch version.json`);
            return { versionChanged: false };
        }

        const versionData = await response.json();
        const newVersion = versionData.version;

        // Get stored version
        const storedVersion = await getStoredVersion();

        console.log(`${LOG_PREFIX} Version check - stored: ${storedVersion}, new: ${newVersion}`);

        if (storedVersion && storedVersion !== newVersion) {
            console.log(`${LOG_PREFIX} Version changed from ${storedVersion} to ${newVersion} - clearing caches`);
            await clearAllCaches();
            await setStoredVersion(newVersion);
            return { versionChanged: true, oldVersion: storedVersion, newVersion };
        } else if (!storedVersion) {
            // First time - just store the version
            await setStoredVersion(newVersion);
            console.log(`${LOG_PREFIX} First version stored: ${newVersion}`);
        }

        return { versionChanged: false, currentVersion: newVersion };
    } catch (error) {
        console.error(`${LOG_PREFIX} Version check failed:`, error);
        return { versionChanged: false };
    }
}

async function notifyClientsOfUpdate(versionInfo) {
    const clients = await self.clients.matchAll({ type: 'window' });
    clients.forEach(client => {
        client.postMessage({
            type: 'VERSION_UPDATE',
            ...versionInfo
        });
    });
}

// Handle messages from clients
self.addEventListener('message', event => {
    if (event?.data?.type === 'CLEAR_CACHES') {
        console.log(`${LOG_PREFIX} Received CLEAR_CACHES message`);
        event.waitUntil((async () => {
            await clearAllCaches();
            // Notify the client that caches were cleared
            if (event.source) {
                event.source.postMessage({ type: 'CACHES_CLEARED' });
            }
        })());
    }

    if (event?.data?.type === 'CHECK_VERSION') {
        console.log(`${LOG_PREFIX} Received CHECK_VERSION message`);
        event.waitUntil((async () => {
            const versionResult = await checkVersionAndInvalidate();
            if (event.source) {
                event.source.postMessage({
                    type: 'VERSION_CHECK_RESULT',
                    ...versionResult
                });
            }
            if (versionResult.versionChanged) {
                await notifyClientsOfUpdate(versionResult);
            }
        })());
    }

    if (event?.data?.type === 'SKIP_WAITING') {
        console.log(`${LOG_PREFIX} Received SKIP_WAITING message`);
        self.skipWaiting();
    }
});

