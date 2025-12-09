// Service worker for Mystira PWA (published build)
// Uses network-first strategy for HTML and framework files to prevent SRI issues
// Version-based cache invalidation for automatic updates

const LOG_PREFIX = '[Mystira ServiceWorker]';

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

// Simple key-value storage using a dedicated cache for version tracking
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

// Notify all clients about the version change
async function notifyClientsOfUpdate(versionInfo) {
    const clients = await self.clients.matchAll({ type: 'window' });
    clients.forEach(client => {
        client.postMessage({
            type: 'VERSION_UPDATE',
            ...versionInfo
        });
    });
}

self.addEventListener('install', (event) => {
    console.log(`${LOG_PREFIX} Install - skipping waiting`);

    event.waitUntil((async () => {
        await self.skipWaiting();
    })());
});

self.addEventListener('activate', (event) => {
    console.log(`${LOG_PREFIX} Activate - taking control and checking version`);

    event.waitUntil((async () => {
        // Check version and clear caches if needed
        const versionResult = await checkVersionAndInvalidate();

        if (versionResult.versionChanged) {
            // Notify clients that a new version is available
            await notifyClientsOfUpdate(versionResult);
        }

        await self.clients.claim();
        console.log(`${LOG_PREFIX} Activation complete - clients claimed`);
    })());
});

self.addEventListener('fetch', (event) => {
    // Skip cross-origin requests and non-GET requests
    if (!event.request.url.startsWith(self.location.origin) ||
        event.request.method !== 'GET') {
        return;
    }

    const url = new URL(event.request.url);

    // Check if request is for HTML files
    const isHtmlFile = url.pathname.endsWith('.html') ||
                      url.pathname.endsWith('.html.br') ||
                      url.pathname.endsWith('.html.gz') ||
                      url.pathname === '/';

    // Check if request is for framework files
    const isFrameworkFile = url.pathname.includes('/_framework/') &&
                           (url.pathname.endsWith('.wasm') ||
                            url.pathname.endsWith('.wasm.br') ||
                            url.pathname.endsWith('.wasm.gz') ||
                            url.pathname.includes('blazor.webassembly.js') ||
                            url.pathname.endsWith('.js.br') ||
                            url.pathname.endsWith('.js.gz') ||
                            url.pathname.endsWith('.dll.br') ||
                            url.pathname.endsWith('.dll.gz'));

    // Check if request is for version.json - always fetch from network
    const isVersionFile = url.pathname.endsWith('version.json');

    // For HTML, framework files, and version.json - use network-first strategy
    if (isHtmlFile || isFrameworkFile || isVersionFile) {
        event.respondWith(
            fetch(event.request, {
                cache: 'no-store',  // Bypass HTTP cache completely
                headers: {
                    'Cache-Control': 'no-cache, no-store, must-revalidate'
                }
            })
            .then(response => {
                if (!response || response.status !== 200) {
                    return response;
                }
                return response;
            })
            .catch((error) => {
                console.warn(`${LOG_PREFIX} Network request failed for`, event.request.url, error);
                // Network failed, try cache as fallback
                return caches.match(event.request)
                    .then(response => {
                        if (response) {
                            console.log(`${LOG_PREFIX} Serving from cache fallback:`, event.request.url);
                            return response;
                        }
                        return new Response('Network error', { status: 503 });
                    });
            })
        );
    }
});

self.addEventListener('message', (event) => {
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
