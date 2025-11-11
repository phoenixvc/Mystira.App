// Service worker for Mystira PWA (published build)
// Caching has been fully disabled to prevent stale assets across deployments.

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

self.addEventListener('message', (event) => {
    if (event?.data?.type === 'CLEAR_CACHES') {
        console.log(`${LOG_PREFIX} Received CLEAR_CACHES message`);
        event.waitUntil(clearAllCaches());
    }
});
