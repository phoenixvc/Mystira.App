// Service Worker for Mystira PWA
// Provides offline functionality and caching

const CACHE_NAME = 'mystira-pwa-v1.0.0';
const DATA_CACHE_NAME = 'mystira-data-v1.0.0';

// Files to cache for offline functionality
const FILES_TO_CACHE = [
    '/',
    '/index.html',
    '/manifest.json',
    '/css/app.css',
    '/icon-192.png',
    '/icon-512.png'
];

// Install event - cache essential files
self.addEventListener('install', (evt) => {
    console.log('[ServiceWorker] Install');
    
    evt.waitUntil(
        caches.open(CACHE_NAME).then((cache) => {
            console.log('[ServiceWorker] Pre-caching offline page');
            return cache.addAll(FILES_TO_CACHE);
        })
    );
    
    self.skipWaiting();
});

// Activate event - clean up old caches
self.addEventListener('activate', (evt) => {
    console.log('[ServiceWorker] Activate');
    
    evt.waitUntil(
        caches.keys().then((keyList) => {
            return Promise.all(keyList.map((key) => {
                if (key !== CACHE_NAME && key !== DATA_CACHE_NAME) {
                    console.log('[ServiceWorker] Removing old cache', key);
                    return caches.delete(key);
                }
            }));
        })
    );
    
    self.clients.claim();
});

// Fetch event - serve from cache when possible
self.addEventListener('fetch', (evt) => {
    // Handle API calls differently from static assets
    if (evt.request.url.includes('/api/')) {
        console.log('[ServiceWorker] Fetch (data)', evt.request.url);
        
        evt.respondWith(
            caches.open(DATA_CACHE_NAME).then((cache) => {
                return fetch(evt.request)
                    .then((response) => {
                        // If the request was successful, clone the response and store it in the cache
                        if (response.status === 200) {
                            cache.put(evt.request.url, response.clone());
                        }
                        return response;
                    })
                    .catch((err) => {
                        // Network request failed, try to get it from the cache
                        console.log('[ServiceWorker] Network request failed, trying cache', err);
                        return cache.match(evt.request);
                    });
            })
        );
    } else {
        // Handle static assets
        console.log('[ServiceWorker] Fetch (static)', evt.request.url);
        
        evt.respondWith(
            caches.match(evt.request).then((response) => {
                if (response) {
                    return response;
                }
                
                return fetch(evt.request).then((response) => {
                    // Check if we received a valid response
                    if (!response || response.status !== 200 || response.type !== 'basic') {
                        return response;
                    }
                    
                    // Clone the response
                    const responseToCache = response.clone();
                    
                    caches.open(CACHE_NAME).then((cache) => {
                        cache.put(evt.request, responseToCache);
                    });
                    
                    return response;
                });
            })
        );
    }
});

// Background sync for offline actions
self.addEventListener('sync', (evt) => {
    console.log('[ServiceWorker] Background Sync', evt.tag);
    
    if (evt.tag === 'background-sync') {
        evt.waitUntil(doBackgroundSync());
    }
});

function doBackgroundSync() {
    // Handle background sync logic here
    console.log('[ServiceWorker] Performing background sync');
    return Promise.resolve();
}

// Push notifications (if needed in future)
self.addEventListener('push', (evt) => {
    console.log('[ServiceWorker] Push Received');
    
    const data = evt.data ? evt.data.json() : {};
    const title = data.title || 'Mystira Notification';
    const options = {
        body: data.body || 'New content available!',
        icon: '/icon-192.png',
        badge: '/icon-96.png',
        vibrate: [100, 50, 100],
        data: data.url || '/'
    };
    
    evt.waitUntil(self.registration.showNotification(title, options));
});

// Notification click handling
self.addEventListener('notificationclick', (evt) => {
    console.log('[ServiceWorker] Notification click Received');
    
    evt.notification.close();
    
    evt.waitUntil(
        clients.openWindow(evt.notification.data)
    );
});
