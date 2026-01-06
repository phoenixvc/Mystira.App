using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Mystira.App.Application.Ports;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Application.Ports.Storage;
using Mystira.App.Application.UseCases.Media;
using Mystira.App.Contracts.Requests.Media;
using Mystira.App.Domain.Models;
using Xunit;

namespace Mystira.App.Application.Tests.UseCases.Media;

public class MediaUseCaseTests
{
    #region GetMediaUseCase Tests

    [Fact]
    public async Task GetMediaUseCase_WithValidId_ReturnsMediaAsset()
    {
        // Arrange
        var mockRepo = new Mock<IMediaAssetRepository>();
        var mockLogger = new Mock<ILogger<GetMediaUseCase>>();
        var mediaAsset = new MediaAsset
        {
            Id = "test-id",
            MediaId = "media-123",
            Url = "https://example.com/media.mp3",
            MediaType = "audio"
        };
        mockRepo.Setup(r => r.GetByMediaIdAsync("media-123")).ReturnsAsync(mediaAsset);
        var useCase = new GetMediaUseCase(mockRepo.Object, mockLogger.Object);

        // Act
        var result = await useCase.ExecuteAsync("media-123");

        // Assert
        result.Should().NotBeNull();
        result!.MediaId.Should().Be("media-123");
        result.MediaType.Should().Be("audio");
    }

    [Fact]
    public async Task GetMediaUseCase_WithNonExistentId_ReturnsNull()
    {
        // Arrange
        var mockRepo = new Mock<IMediaAssetRepository>();
        var mockLogger = new Mock<ILogger<GetMediaUseCase>>();
        mockRepo.Setup(r => r.GetByMediaIdAsync("non-existent")).ReturnsAsync((MediaAsset?)null);
        var useCase = new GetMediaUseCase(mockRepo.Object, mockLogger.Object);

        // Act
        var result = await useCase.ExecuteAsync("non-existent");

        // Assert
        result.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task GetMediaUseCase_WithEmptyId_ThrowsArgumentException(string? mediaId)
    {
        // Arrange
        var mockRepo = new Mock<IMediaAssetRepository>();
        var mockLogger = new Mock<ILogger<GetMediaUseCase>>();
        var useCase = new GetMediaUseCase(mockRepo.Object, mockLogger.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => useCase.ExecuteAsync(mediaId!));
    }

    #endregion

    #region DeleteMediaUseCase Tests

    [Fact]
    public async Task DeleteMediaUseCase_WithExistingMedia_DeletesAndReturnsTrue()
    {
        // Arrange
        var mockRepo = new Mock<IMediaAssetRepository>();
        var mockUow = new Mock<IUnitOfWork>();
        var mockBlob = new Mock<IBlobService>();
        var mockLogger = new Mock<ILogger<DeleteMediaUseCase>>();

        var mediaAsset = new MediaAsset
        {
            Id = "db-id",
            MediaId = "media-123",
            Url = "https://storage.example.com/container/file.mp3"
        };
        mockRepo.Setup(r => r.GetByMediaIdAsync("media-123")).ReturnsAsync(mediaAsset);
        mockBlob.Setup(b => b.DeleteMediaAsync("file.mp3")).ReturnsAsync(true);
        mockRepo.Setup(r => r.DeleteAsync("db-id")).Returns(Task.CompletedTask);
        mockUow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var useCase = new DeleteMediaUseCase(mockRepo.Object, mockUow.Object, mockBlob.Object, mockLogger.Object);

        // Act
        var result = await useCase.ExecuteAsync("media-123");

        // Assert
        result.Should().BeTrue();
        mockBlob.Verify(b => b.DeleteMediaAsync("file.mp3"), Times.Once);
        mockRepo.Verify(r => r.DeleteAsync("db-id"), Times.Once);
        mockUow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteMediaUseCase_WithNonExistentMedia_ReturnsFalse()
    {
        // Arrange
        var mockRepo = new Mock<IMediaAssetRepository>();
        var mockUow = new Mock<IUnitOfWork>();
        var mockBlob = new Mock<IBlobService>();
        var mockLogger = new Mock<ILogger<DeleteMediaUseCase>>();

        mockRepo.Setup(r => r.GetByMediaIdAsync("non-existent")).ReturnsAsync((MediaAsset?)null);

        var useCase = new DeleteMediaUseCase(mockRepo.Object, mockUow.Object, mockBlob.Object, mockLogger.Object);

        // Act
        var result = await useCase.ExecuteAsync("non-existent");

        // Assert
        result.Should().BeFalse();
        mockBlob.Verify(b => b.DeleteMediaAsync(It.IsAny<string>()), Times.Never);
        mockRepo.Verify(r => r.DeleteAsync(It.IsAny<string>()), Times.Never);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task DeleteMediaUseCase_WithEmptyId_ThrowsArgumentException(string? mediaId)
    {
        // Arrange
        var mockRepo = new Mock<IMediaAssetRepository>();
        var mockUow = new Mock<IUnitOfWork>();
        var mockBlob = new Mock<IBlobService>();
        var mockLogger = new Mock<ILogger<DeleteMediaUseCase>>();

        var useCase = new DeleteMediaUseCase(mockRepo.Object, mockUow.Object, mockBlob.Object, mockLogger.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => useCase.ExecuteAsync(mediaId!));
    }

    [Fact]
    public async Task DeleteMediaUseCase_WhenBlobDeletionFails_StillDeletesFromDatabase()
    {
        // Arrange
        var mockRepo = new Mock<IMediaAssetRepository>();
        var mockUow = new Mock<IUnitOfWork>();
        var mockBlob = new Mock<IBlobService>();
        var mockLogger = new Mock<ILogger<DeleteMediaUseCase>>();

        var mediaAsset = new MediaAsset
        {
            Id = "db-id",
            MediaId = "media-123",
            Url = "https://storage.example.com/container/file.mp3"
        };
        mockRepo.Setup(r => r.GetByMediaIdAsync("media-123")).ReturnsAsync(mediaAsset);
        mockBlob.Setup(b => b.DeleteMediaAsync(It.IsAny<string>())).ThrowsAsync(new Exception("Blob storage error"));
        mockRepo.Setup(r => r.DeleteAsync("db-id")).Returns(Task.CompletedTask);
        mockUow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var useCase = new DeleteMediaUseCase(mockRepo.Object, mockUow.Object, mockBlob.Object, mockLogger.Object);

        // Act
        var result = await useCase.ExecuteAsync("media-123");

        // Assert
        result.Should().BeTrue();
        mockRepo.Verify(r => r.DeleteAsync("db-id"), Times.Once);
    }

    [Fact]
    public async Task DeleteMediaUseCase_WithEmptyUrl_SkipsBlobDeletion()
    {
        // Arrange
        var mockRepo = new Mock<IMediaAssetRepository>();
        var mockUow = new Mock<IUnitOfWork>();
        var mockBlob = new Mock<IBlobService>();
        var mockLogger = new Mock<ILogger<DeleteMediaUseCase>>();

        var mediaAsset = new MediaAsset
        {
            Id = "db-id",
            MediaId = "media-123",
            Url = ""
        };
        mockRepo.Setup(r => r.GetByMediaIdAsync("media-123")).ReturnsAsync(mediaAsset);
        mockRepo.Setup(r => r.DeleteAsync("db-id")).Returns(Task.CompletedTask);
        mockUow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var useCase = new DeleteMediaUseCase(mockRepo.Object, mockUow.Object, mockBlob.Object, mockLogger.Object);

        // Act
        var result = await useCase.ExecuteAsync("media-123");

        // Assert
        result.Should().BeTrue();
        mockBlob.Verify(b => b.DeleteMediaAsync(It.IsAny<string>()), Times.Never);
    }

    #endregion

    #region ListMediaUseCase Tests

    [Fact]
    public async Task ListMediaUseCase_ReturnsPagedResults()
    {
        // Arrange
        var mockRepo = new Mock<IMediaAssetRepository>();
        var mockLogger = new Mock<ILogger<ListMediaUseCase>>();

        var mediaAssets = new List<MediaAsset>
        {
            new() { Id = "1", MediaId = "media-1", MediaType = "audio", CreatedAt = DateTime.UtcNow },
            new() { Id = "2", MediaId = "media-2", MediaType = "image", CreatedAt = DateTime.UtcNow.AddHours(-1) },
            new() { Id = "3", MediaId = "media-3", MediaType = "audio", CreatedAt = DateTime.UtcNow.AddHours(-2) }
        }.AsQueryable();

        mockRepo.Setup(r => r.GetQueryable()).Returns(mediaAssets);

        var useCase = new ListMediaUseCase(mockRepo.Object, mockLogger.Object);
        var request = new MediaQueryRequest { Page = 1, PageSize = 10 };

        // Act
        var result = await useCase.ExecuteAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.TotalCount.Should().Be(3);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(10);
    }

    [Fact]
    public async Task ListMediaUseCase_WithMediaTypeFilter_FiltersCorrectly()
    {
        // Arrange
        var mockRepo = new Mock<IMediaAssetRepository>();
        var mockLogger = new Mock<ILogger<ListMediaUseCase>>();

        var mediaAssets = new List<MediaAsset>
        {
            new() { Id = "1", MediaId = "media-1", MediaType = "audio", CreatedAt = DateTime.UtcNow },
            new() { Id = "2", MediaId = "media-2", MediaType = "image", CreatedAt = DateTime.UtcNow },
            new() { Id = "3", MediaId = "media-3", MediaType = "audio", CreatedAt = DateTime.UtcNow }
        }.AsQueryable();

        mockRepo.Setup(r => r.GetQueryable()).Returns(mediaAssets);

        var useCase = new ListMediaUseCase(mockRepo.Object, mockLogger.Object);
        var request = new MediaQueryRequest { MediaType = "audio", Page = 1, PageSize = 10 };

        // Act
        var result = await useCase.ExecuteAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.TotalCount.Should().Be(2);
        result.Media.Should().OnlyContain(m => m.MediaType == "audio");
    }

    [Fact]
    public async Task ListMediaUseCase_WithSearchFilter_SearchesCorrectly()
    {
        // Arrange
        var mockRepo = new Mock<IMediaAssetRepository>();
        var mockLogger = new Mock<ILogger<ListMediaUseCase>>();

        var mediaAssets = new List<MediaAsset>
        {
            new() { Id = "1", MediaId = "logo-image", MediaType = "image", Url = "https://example.com/logo.png", CreatedAt = DateTime.UtcNow },
            new() { Id = "2", MediaId = "background", MediaType = "image", Url = "https://example.com/bg.png", CreatedAt = DateTime.UtcNow },
            new() { Id = "3", MediaId = "icon", MediaType = "image", Url = "https://example.com/icon.png", Description = "Company logo icon", CreatedAt = DateTime.UtcNow }
        }.AsQueryable();

        mockRepo.Setup(r => r.GetQueryable()).Returns(mediaAssets);

        var useCase = new ListMediaUseCase(mockRepo.Object, mockLogger.Object);
        var request = new MediaQueryRequest { Search = "logo", Page = 1, PageSize = 10 };

        // Act
        var result = await useCase.ExecuteAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.TotalCount.Should().Be(2);
    }

    [Theory]
    [InlineData("filename", false)]
    [InlineData("mediatype", false)]
    [InlineData("filesize", true)]
    [InlineData("updatedat", true)]
    [InlineData("createdat", true)]
    [InlineData(null, true)]
    public async Task ListMediaUseCase_WithSortOptions_SortsCorrectly(string? sortBy, bool sortDescending)
    {
        // Arrange
        var mockRepo = new Mock<IMediaAssetRepository>();
        var mockLogger = new Mock<ILogger<ListMediaUseCase>>();

        var mediaAssets = new List<MediaAsset>
        {
            new() { Id = "1", MediaId = "media-1", MediaType = "audio", Url = "a.mp3", FileSizeBytes = 100, CreatedAt = DateTime.UtcNow.AddHours(-2), UpdatedAt = DateTime.UtcNow.AddHours(-1) },
            new() { Id = "2", MediaId = "media-2", MediaType = "image", Url = "b.png", FileSizeBytes = 200, CreatedAt = DateTime.UtcNow.AddHours(-1), UpdatedAt = DateTime.UtcNow }
        }.AsQueryable();

        mockRepo.Setup(r => r.GetQueryable()).Returns(mediaAssets);

        var useCase = new ListMediaUseCase(mockRepo.Object, mockLogger.Object);
        var request = new MediaQueryRequest { SortBy = sortBy, SortDescending = sortDescending, Page = 1, PageSize = 10 };

        // Act
        var result = await useCase.ExecuteAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Media.Should().HaveCount(2);
    }

    [Fact]
    public async Task ListMediaUseCase_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        var mockRepo = new Mock<IMediaAssetRepository>();
        var mockLogger = new Mock<ILogger<ListMediaUseCase>>();

        var mediaAssets = Enumerable.Range(1, 25)
            .Select(i => new MediaAsset
            {
                Id = i.ToString(),
                MediaId = $"media-{i}",
                MediaType = "audio",
                CreatedAt = DateTime.UtcNow.AddMinutes(-i)
            })
            .AsQueryable();

        mockRepo.Setup(r => r.GetQueryable()).Returns(mediaAssets);

        var useCase = new ListMediaUseCase(mockRepo.Object, mockLogger.Object);
        var request = new MediaQueryRequest { Page = 2, PageSize = 10 };

        // Act
        var result = await useCase.ExecuteAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.TotalCount.Should().Be(25);
        result.TotalPages.Should().Be(3);
        result.Page.Should().Be(2);
        result.Media.Should().HaveCount(10);
    }

    #endregion

    #region UpdateMediaMetadataUseCase Tests

    [Fact]
    public async Task UpdateMediaMetadataUseCase_WithValidData_UpdatesMedia()
    {
        // Arrange
        var mockRepo = new Mock<IMediaAssetRepository>();
        var mockUow = new Mock<IUnitOfWork>();
        var mockLogger = new Mock<ILogger<UpdateMediaMetadataUseCase>>();

        var mediaAsset = new MediaAsset
        {
            Id = "db-id",
            MediaId = "media-123",
            Description = "Original description",
            Tags = new List<string> { "tag1" },
            MediaType = "audio"
        };
        mockRepo.Setup(r => r.GetByMediaIdAsync("media-123")).ReturnsAsync(mediaAsset);
        mockRepo.Setup(r => r.UpdateAsync(It.IsAny<MediaAsset>())).Returns(Task.CompletedTask);
        mockUow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var useCase = new UpdateMediaMetadataUseCase(mockRepo.Object, mockUow.Object, mockLogger.Object);
        var updateRequest = new MediaUpdateRequest
        {
            Description = "Updated description",
            Tags = new List<string> { "tag1", "tag2" },
            MediaType = "video"
        };

        // Act
        var result = await useCase.ExecuteAsync("media-123", updateRequest);

        // Assert
        result.Should().NotBeNull();
        result.Description.Should().Be("Updated description");
        result.Tags.Should().Contain("tag2");
        result.MediaType.Should().Be("video");
        mockRepo.Verify(r => r.UpdateAsync(It.IsAny<MediaAsset>()), Times.Once);
    }

    [Fact]
    public async Task UpdateMediaMetadataUseCase_WithPartialUpdate_UpdatesOnlySpecifiedFields()
    {
        // Arrange
        var mockRepo = new Mock<IMediaAssetRepository>();
        var mockUow = new Mock<IUnitOfWork>();
        var mockLogger = new Mock<ILogger<UpdateMediaMetadataUseCase>>();

        var mediaAsset = new MediaAsset
        {
            Id = "db-id",
            MediaId = "media-123",
            Description = "Original description",
            Tags = new List<string> { "tag1" },
            MediaType = "audio"
        };
        mockRepo.Setup(r => r.GetByMediaIdAsync("media-123")).ReturnsAsync(mediaAsset);
        mockRepo.Setup(r => r.UpdateAsync(It.IsAny<MediaAsset>())).Returns(Task.CompletedTask);
        mockUow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var useCase = new UpdateMediaMetadataUseCase(mockRepo.Object, mockUow.Object, mockLogger.Object);
        var updateRequest = new MediaUpdateRequest
        {
            Description = "Updated description"
            // Tags and MediaType are null - should not be updated
        };

        // Act
        var result = await useCase.ExecuteAsync("media-123", updateRequest);

        // Assert
        result.Should().NotBeNull();
        result.Description.Should().Be("Updated description");
        result.Tags.Should().ContainSingle().Which.Should().Be("tag1");
        result.MediaType.Should().Be("audio");
    }

    [Fact]
    public async Task UpdateMediaMetadataUseCase_WithNonExistentMedia_ThrowsKeyNotFoundException()
    {
        // Arrange
        var mockRepo = new Mock<IMediaAssetRepository>();
        var mockUow = new Mock<IUnitOfWork>();
        var mockLogger = new Mock<ILogger<UpdateMediaMetadataUseCase>>();

        mockRepo.Setup(r => r.GetByMediaIdAsync("non-existent")).ReturnsAsync((MediaAsset?)null);

        var useCase = new UpdateMediaMetadataUseCase(mockRepo.Object, mockUow.Object, mockLogger.Object);
        var updateRequest = new MediaUpdateRequest { Description = "New description" };

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            useCase.ExecuteAsync("non-existent", updateRequest));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task UpdateMediaMetadataUseCase_WithEmptyId_ThrowsArgumentException(string? mediaId)
    {
        // Arrange
        var mockRepo = new Mock<IMediaAssetRepository>();
        var mockUow = new Mock<IUnitOfWork>();
        var mockLogger = new Mock<ILogger<UpdateMediaMetadataUseCase>>();

        var useCase = new UpdateMediaMetadataUseCase(mockRepo.Object, mockUow.Object, mockLogger.Object);
        var updateRequest = new MediaUpdateRequest { Description = "New description" };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            useCase.ExecuteAsync(mediaId!, updateRequest));
    }

    [Fact]
    public async Task UpdateMediaMetadataUseCase_WithNullUpdateData_ThrowsArgumentNullException()
    {
        // Arrange
        var mockRepo = new Mock<IMediaAssetRepository>();
        var mockUow = new Mock<IUnitOfWork>();
        var mockLogger = new Mock<ILogger<UpdateMediaMetadataUseCase>>();

        var useCase = new UpdateMediaMetadataUseCase(mockRepo.Object, mockUow.Object, mockLogger.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            useCase.ExecuteAsync("media-123", null!));
    }

    #endregion

    #region GetMediaByFilenameUseCase Tests

    [Fact]
    public async Task GetMediaByFilenameUseCase_WithValidFilename_ReturnsMediaAsset()
    {
        // Arrange
        var mockRepo = new Mock<IMediaAssetRepository>();
        var mockMetadataService = new Mock<IMediaMetadataService>();
        var mockLogger = new Mock<ILogger<GetMediaByFilenameUseCase>>();

        var metadataFile = new MediaMetadataFile
        {
            Entries = new List<MediaMetadataEntry>
            {
                new() { Id = "media-123", FileName = "logo.png", Title = "Logo" }
            }
        };
        var mediaAsset = new MediaAsset
        {
            Id = "db-id",
            MediaId = "media-123",
            Url = "https://example.com/logo.png"
        };

        mockMetadataService.Setup(s => s.GetMediaMetadataFileAsync()).ReturnsAsync(metadataFile);
        mockRepo.Setup(r => r.GetByMediaIdAsync("media-123")).ReturnsAsync(mediaAsset);

        var useCase = new GetMediaByFilenameUseCase(mockRepo.Object, mockMetadataService.Object, mockLogger.Object);

        // Act
        var result = await useCase.ExecuteAsync("logo.png");

        // Assert
        result.Should().NotBeNull();
        result!.MediaId.Should().Be("media-123");
    }

    [Fact]
    public async Task GetMediaByFilenameUseCase_WithNonExistentFilename_ReturnsNull()
    {
        // Arrange
        var mockRepo = new Mock<IMediaAssetRepository>();
        var mockMetadataService = new Mock<IMediaMetadataService>();
        var mockLogger = new Mock<ILogger<GetMediaByFilenameUseCase>>();

        var metadataFile = new MediaMetadataFile
        {
            Entries = new List<MediaMetadataEntry>
            {
                new() { Id = "media-123", FileName = "logo.png", Title = "Logo" }
            }
        };

        mockMetadataService.Setup(s => s.GetMediaMetadataFileAsync()).ReturnsAsync(metadataFile);

        var useCase = new GetMediaByFilenameUseCase(mockRepo.Object, mockMetadataService.Object, mockLogger.Object);

        // Act
        var result = await useCase.ExecuteAsync("nonexistent.png");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetMediaByFilenameUseCase_WithNoMetadataFile_ReturnsNull()
    {
        // Arrange
        var mockRepo = new Mock<IMediaAssetRepository>();
        var mockMetadataService = new Mock<IMediaMetadataService>();
        var mockLogger = new Mock<ILogger<GetMediaByFilenameUseCase>>();

        mockMetadataService.Setup(s => s.GetMediaMetadataFileAsync()).ReturnsAsync((MediaMetadataFile?)null);

        var useCase = new GetMediaByFilenameUseCase(mockRepo.Object, mockMetadataService.Object, mockLogger.Object);

        // Act
        var result = await useCase.ExecuteAsync("logo.png");

        // Assert
        result.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task GetMediaByFilenameUseCase_WithEmptyFilename_ThrowsArgumentException(string? fileName)
    {
        // Arrange
        var mockRepo = new Mock<IMediaAssetRepository>();
        var mockMetadataService = new Mock<IMediaMetadataService>();
        var mockLogger = new Mock<ILogger<GetMediaByFilenameUseCase>>();

        var useCase = new GetMediaByFilenameUseCase(mockRepo.Object, mockMetadataService.Object, mockLogger.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => useCase.ExecuteAsync(fileName!));
    }

    #endregion

    #region DownloadMediaUseCase Tests

    [Fact]
    public async Task DownloadMediaUseCase_WithValidMedia_ReturnsStreamAndMetadata()
    {
        // Arrange
        var mockRepo = new Mock<IMediaAssetRepository>();
        var mockHttpClientFactory = new Mock<IHttpClientFactory>();
        var mockLogger = new Mock<ILogger<DownloadMediaUseCase>>();

        var mediaAsset = new MediaAsset
        {
            Id = "db-id",
            MediaId = "media-123",
            Url = "https://example.com/audio.mp3",
            MimeType = "audio/mpeg"
        };
        mockRepo.Setup(r => r.GetByMediaIdAsync("media-123")).ReturnsAsync(mediaAsset);

        var mockHandler = new MockHttpMessageHandler(new HttpResponseMessage
        {
            StatusCode = System.Net.HttpStatusCode.OK,
            Content = new ByteArrayContent(new byte[] { 1, 2, 3, 4, 5 })
        });
        using var httpClient = new HttpClient(mockHandler);
        mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var useCase = new DownloadMediaUseCase(mockRepo.Object, mockHttpClientFactory.Object, mockLogger.Object);

        // Act
        var result = await useCase.ExecuteAsync("media-123");

        // Assert
        result.Should().NotBeNull();
        result!.Value.contentType.Should().Be("audio/mpeg");
        result.Value.fileName.Should().Be("audio.mp3");
    }

    [Fact]
    public async Task DownloadMediaUseCase_WithNonExistentMedia_ReturnsNull()
    {
        // Arrange
        var mockRepo = new Mock<IMediaAssetRepository>();
        var mockHttpClientFactory = new Mock<IHttpClientFactory>();
        var mockLogger = new Mock<ILogger<DownloadMediaUseCase>>();

        mockRepo.Setup(r => r.GetByMediaIdAsync("non-existent")).ReturnsAsync((MediaAsset?)null);

        var useCase = new DownloadMediaUseCase(mockRepo.Object, mockHttpClientFactory.Object, mockLogger.Object);

        // Act
        var result = await useCase.ExecuteAsync("non-existent");

        // Assert
        result.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task DownloadMediaUseCase_WithEmptyId_ThrowsArgumentException(string? mediaId)
    {
        // Arrange
        var mockRepo = new Mock<IMediaAssetRepository>();
        var mockHttpClientFactory = new Mock<IHttpClientFactory>();
        var mockLogger = new Mock<ILogger<DownloadMediaUseCase>>();

        var useCase = new DownloadMediaUseCase(mockRepo.Object, mockHttpClientFactory.Object, mockLogger.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => useCase.ExecuteAsync(mediaId!));
    }

    [Fact]
    public async Task DownloadMediaUseCase_WhenHttpRequestFails_ReturnsNull()
    {
        // Arrange
        var mockRepo = new Mock<IMediaAssetRepository>();
        var mockHttpClientFactory = new Mock<IHttpClientFactory>();
        var mockLogger = new Mock<ILogger<DownloadMediaUseCase>>();

        var mediaAsset = new MediaAsset
        {
            Id = "db-id",
            MediaId = "media-123",
            Url = "https://example.com/audio.mp3",
            MimeType = "audio/mpeg"
        };
        mockRepo.Setup(r => r.GetByMediaIdAsync("media-123")).ReturnsAsync(mediaAsset);

        var mockHandler = new MockHttpMessageHandler(new HttpResponseMessage
        {
            StatusCode = System.Net.HttpStatusCode.NotFound
        });
        using var httpClient = new HttpClient(mockHandler);
        mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var useCase = new DownloadMediaUseCase(mockRepo.Object, mockHttpClientFactory.Object, mockLogger.Object);

        // Act
        var result = await useCase.ExecuteAsync("media-123");

        // Assert
        result.Should().BeNull();
    }

    #endregion
}

/// <summary>
/// Helper class for mocking HttpClient responses
/// </summary>
public class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly HttpResponseMessage _response;

    public MockHttpMessageHandler(HttpResponseMessage response)
    {
        _response = response;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return Task.FromResult(_response);
    }
}
