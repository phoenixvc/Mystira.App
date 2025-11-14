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
    console.log(`${LOG_PREFIX} Install - skipping waiting and clearing caches`);

    event.waitUntil((async () => {
        await clearAllCaches();
        await self.skipWaiting();
    })());
});

self.addEventListener('activate', (event) => {
    console.log(`${LOG_PREFIX} Activate - ensuring caches are cleared`);

    event.waitUntil((async () => {
        await clearAllCaches();
        await self.clients.claim();
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
            fetch(event.request)
                .catch(() => {
                    // Network failed, try cache as fallback
                    return caches.match(event.request)
                        .then(response => response || new Response('Network error', { status: 503 }));
                })
        );
    }
});

self.addEventListener('message', (event) => {
    if (event?.data?.type === 'CLEAR_CACHES') {
        console.log(`${LOG_PREFIX} Received CLEAR_CACHES message`);
        event.waitUntil(clearAllCaches());
    }
});
