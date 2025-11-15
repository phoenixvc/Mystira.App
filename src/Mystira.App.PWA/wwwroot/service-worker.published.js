// Service worker for Mystira PWA (published build)
// Uses network-first strategy for HTML and framework files to prevent SRI issues
// Clears caches on install/activate to ensure fresh assets

const LOG_PREFIX = '[Mystira ServiceWorker]';

async function clearAllCaches() {
    if (!self.caches) {
        return;
    }

    try {
        const cacheKeys = await caches.keys();
        if (cacheKeys.length === 0) {
            return;
        }

        await Promise.all(cacheKeys.map((key) => caches.delete(key)));
        console.log(`${LOG_PREFIX} Cleared caches:`, cacheKeys);
    } catch (error) {
        console.error(`${LOG_PREFIX} Failed to clear caches`, error);
    }
}

self.addEventListener('install', (event) => {
    console.log(`${LOG_PREFIX} Install - skipping waiting`);

    event.waitUntil((async () => {
        // Don't clear all caches on install to prevent race conditions
        // Only clear when explicitly needed
        await self.skipWaiting();
    })());
});

self.addEventListener('activate', (event) => {
    console.log(`${LOG_PREFIX} Activate - taking control and cleaning old caches`);

    event.waitUntil((async () => {
        // Clear all caches to ensure fresh assets and prevent SRI issues
        // This is safe during activate since the new service worker is already in control
        await clearAllCaches();
        await self.clients.claim();
        console.log(`${LOG_PREFIX} Activation complete - caches cleared and clients claimed`);
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

    // For HTML and framework files, use network-first strategy to ensure SRI hashes are fresh
    if (isHtmlFile || isFrameworkFile) {
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
        console.log(`${LOG_PREFIX} Received CLEAR_CACHES message - but being conservative to prevent race conditions`);
        // Don't clear caches aggressively to prevent SRI race conditions
        // Cache clearing should only happen during service worker updates, not on every page load
        event.waitUntil(Promise.resolve());
    }
});
