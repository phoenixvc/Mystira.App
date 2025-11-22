
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
            {
                return string.Empty;
            }

            try
            {
                // Call JavaScript directly - it will handle the error if not initialized
                return await _jsRuntime.InvokeAsync<string>("imageCacheManager.getOrCacheImage", mediaId, imageUrl);
            }
            catch (JSException jsEx)
            {
                _logger.LogWarning(jsEx, "Image cache manager not available, falling back to uncached image");
                return imageUrl;
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
                await _jsRuntime.InvokeVoidAsync("imageCacheManager.clearCache");
            }
            catch (JSException jsEx)
            {
                _logger.LogWarning(jsEx, "Image cache manager not available for clearing");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing image cache");
            }
        }
    }
}
