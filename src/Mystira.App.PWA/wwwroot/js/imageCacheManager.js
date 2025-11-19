// Explicitly define on window to ensure global access
// This implementation uses IndexedDB to persist image Blobs and returns
// an Object URL that can be used directly as the <img src> value.
// Falls back to the network URL if anything goes wrong.
window.imageCacheManager = (function () {
    const DB_NAME = 'mystira-image-db';
    const DB_VERSION = 1;
    const STORE_NAME = 'images';

    /**
     * Open (or create) the IndexedDB database.
     * @returns {Promise<IDBDatabase>}
     */
    function openDb() {
        return new Promise((resolve, reject) => {
            if (!('indexedDB' in window)) {
                reject(new Error('IndexedDB not supported'));
                return;
            }
            const request = indexedDB.open(DB_NAME, DB_VERSION);
            request.onupgradeneeded = () => {
                const db = request.result;
                if (!db.objectStoreNames.contains(STORE_NAME)) {
                    const store = db.createObjectStore(STORE_NAME, { keyPath: 'mediaId' });
                    store.createIndex('timestamp', 'timestamp', { unique: false });
                }
            };
            request.onsuccess = () => resolve(request.result);
            request.onerror = () => reject(request.error || new Error('Failed to open IndexedDB'));
        });
    }

    /**
     * Get cached record by mediaId
     * @param {IDBDatabase} db
     * @param {string} mediaId
     */
    function getRecord(db, mediaId) {
        return new Promise((resolve, reject) => {
            const tx = db.transaction(STORE_NAME, 'readonly');
            const store = tx.objectStore(STORE_NAME);
            const req = store.get(mediaId);
            req.onsuccess = () => resolve(req.result || null);
            req.onerror = () => reject(req.error || new Error('Failed to read from IndexedDB'));
        });
    }

    /**
     * Put record into store
     * @param {IDBDatabase} db
     * @param {{mediaId:string, blob:Blob, contentType:string, timestamp:number}} record
     */
    function putRecord(db, record) {
        return new Promise((resolve, reject) => {
            const tx = db.transaction(STORE_NAME, 'readwrite');
            const store = tx.objectStore(STORE_NAME);
            const req = store.put(record);
            req.onsuccess = () => resolve();
            req.onerror = () => reject(req.error || new Error('Failed to write to IndexedDB'));
        });
    }

    /**
     * Clear all records
     */
    function clearStore(db) {
        return new Promise((resolve, reject) => {
            const tx = db.transaction(STORE_NAME, 'readwrite');
            const store = tx.objectStore(STORE_NAME);
            const req = store.clear();
            req.onsuccess = () => resolve();
            req.onerror = () => reject(req.error || new Error('Failed to clear IndexedDB store'));
        });
    }

    /**
     * Convert a Blob to an Object URL.
     */
    function blobToObjectUrl(blob) {
        try {
            return URL.createObjectURL(blob);
        } catch {
            return null;
        }
    }

    return {
        /**
         * Returns an object URL sourced from IndexedDB if present; otherwise fetches,
         * stores in IndexedDB, and returns the object URL. Falls back to original
         * imageUrl on failure.
         */
        async getOrCacheImage(mediaId, imageUrl) {
            if (!mediaId || !imageUrl) return '';
            try {
                const db = await openDb();
                // Try from IndexedDB
                const existing = await getRecord(db, mediaId);
                if (existing && existing.blob) {
                    const url = blobToObjectUrl(existing.blob);
                    if (url) {
                        // console.debug(`Image ${mediaId} served from IndexedDB`);
                        return url;
                    }
                }

                // Not cached or failed to create object URL — fetch from network
                const response = await fetch(imageUrl, { cache: 'no-store' });
                if (!response.ok) {
                    console.error(`Failed to fetch image ${mediaId}: ${response.status} ${response.statusText}`);
                    return imageUrl;
                }

                const blob = await response.blob();
                const contentType = response.headers.get('content-type') || blob.type || 'application/octet-stream';

                // Save to IndexedDB (best-effort)
                try {
                    await putRecord(db, {
                        mediaId,
                        blob,
                        contentType,
                        timestamp: Date.now()
                    });
                } catch (e) {
                    // Non-fatal: still return URL
                    console.warn('Failed to store image in IndexedDB', e);
                }

                const objectUrl = blobToObjectUrl(blob);
                return objectUrl || imageUrl;
            } catch (error) {
                // IndexedDB not available or some other error — fall back to original URL
                console.warn('IndexedDB image cache error, falling back to network URL:', error);
                return imageUrl;
            }
        },

        async clearCache() {
            try {
                const db = await openDb();
                await clearStore(db);
                console.log('IndexedDB image cache cleared');
            } catch (error) {
                console.error('Error clearing IndexedDB image cache:', error);
            }
        },

        // Helper to revoke object URLs created by this manager
        revokeObjectUrl(url) {
            try {
                if (typeof url === 'string' && url.startsWith('blob:')) {
                    URL.revokeObjectURL(url);
                }
            } catch (e) {
                console.debug('Failed to revoke object URL', e);
            }
        }
    };
})();

// Log to confirm the script loaded
console.log('Image cache manager (IndexedDB) initialized');