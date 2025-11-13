using Microsoft.JSInterop;

namespace Mystira.App.PWA.Services
{
    public interface IImageCacheService
    {
        ValueTask<string> GetOrCacheImageAsync(string mediaId, string imageUrl);
        ValueTask ClearCacheAsync();
    }

    public class ImageCacheService : IImageCacheService
    {
        private readonly IJSRuntime _jsRuntime;
        private readonly ILogger<ImageCacheService> _logger;

        public ImageCacheService(IJSRuntime jsRuntime, ILogger<ImageCacheService> logger)
        {
            _jsRuntime = jsRuntime;
            _logger = logger;
        }

        public async ValueTask<string> GetOrCacheImageAsync(string mediaId, string imageUrl)
        {
            if (string.IsNullOrEmpty(mediaId) || string.IsNullOrEmpty(imageUrl))
                return string.Empty;

            try
            {
                // First check if the function exists
                var exists = await _jsRuntime.InvokeAsync<bool>("eval", 
                    "typeof window.imageCacheManager !== 'undefined' && typeof window.imageCacheManager.getOrCacheImage === 'function'");
                
                if (!exists)
                {
                    _logger.LogWarning("Image cache manager not initialized, falling back to uncached image");
                    return imageUrl;
                }
                
                // Call JavaScript to get/cache the image
                return await _jsRuntime.InvokeAsync<string>("imageCacheManager.getOrCacheImage", mediaId, imageUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error caching image for mediaId: {MediaId}", mediaId);
                // Return original URL as fallback
                return imageUrl;
            }
        }

        public async ValueTask ClearCacheAsync()
        {
            try
            {
                var exists = await _jsRuntime.InvokeAsync<bool>("eval", 
                    "typeof window.imageCacheManager !== 'undefined' && typeof window.imageCacheManager.clearCache === 'function'");
                
                if (exists)
                {
                    await _jsRuntime.InvokeVoidAsync("imageCacheManager.clearCache");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing image cache");
            }
        }
    }
}