/*
 * Service Worker for Mystira PWA (development build)
 *
 * Current strategy:
 * - Caching of assets is disabled to avoid stale behaviour during development.
 * - All caches are cleared on install and activate.
 * - A CLEAR_CACHES message can be sent from the client to force cache clearing.
 *
 * NOTE:
 * - If you introduce caching here (for local offline testing), ensure that
 *   published builds (service-worker.published.js) use a compatible strategy.
 */

const LOG_PREFIX = '[Mystira ServiceWorker]';

/**
 * Clear all caches associated with this origin.
 */
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
    console.log(`${LOG_PREFIX} Install - clearing caches and skipping waiting`);

    event.waitUntil((async () => {
        await clearAllCaches();
        await self.skipWaiting();
    })());
});

self.addEventListener('activate', (event) => {
    console.log(`${LOG_PREFIX} Activate - ensuring caches are cleared and claiming clients`);

    event.waitUntil((async () => {
        await clearAllCaches();
        await self.clients.claim();
    })());
});

self.addEventListener('message', (event) => {
    if (event?.data?.type === 'CLEAR_CACHES') {
        console.log(`${LOG_PREFIX} Received CLEAR_CACHES message`);
        event.waitUntil(clearAllCaches());
    }
});

/*
 * No fetch handler is registered.
 *
 * This means:
 * - Requests go straight to the network (or browser default cache rules).
 * - The service worker is primarily used here for cache clearing and PWA lifecycle hooks.
 *
 * If you decide to add offline caching:
 * - Introduce a 'fetch' event listener.
 * - Use a minimal app-shell cache and versioned cache names.
 * - Keep the CLEAR_CACHES handler for manual invalidation.
 */
