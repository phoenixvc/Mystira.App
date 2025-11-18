// Explicitly define on window to ensure global access
window.imageCacheManager = {
    cacheName: 'mystira-image-cache',

    async getOrCacheImage(mediaId, imageUrl) {
        try {
            // Try to get from cache first
            const cache = await caches.open(this.cacheName);
            const cachedResponse = await cache.match(imageUrl);

            if (cachedResponse && cachedResponse.ok) {
                console.log(`Image ${mediaId} found in cache`);
                return imageUrl; // Return the original URL (browser will use cache)
            }

            // Not in cache, fetch and store
            console.log(`Caching image ${mediaId} from ${imageUrl}`);
            const response = await fetch(imageUrl, { cache: 'no-store' });

            if (response.ok) {
                // Clone the response before putting it in the cache
                await cache.put(imageUrl, response.clone());

                // Return the original URL - browser will use the now-cached version
                return imageUrl;
            } else {
                console.error(`Failed to fetch image: ${response.status} ${response.statusText}`);
                return imageUrl; // Return original URL as fallback
            }
        } catch (error) {
            console.error('Error in image caching:', error);
            return imageUrl; // Return original URL as fallback
        }
    },

    async clearCache() {
        try {
            await caches.delete(this.cacheName);
            console.log('Image cache cleared');
        } catch (error) {
            console.error('Error clearing image cache:', error);
        }
    }
};

// Log to confirm the script loaded
console.log('Image cache manager initialized');