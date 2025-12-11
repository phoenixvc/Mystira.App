using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Mystira.App.Infrastructure.Azure.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Azure.Core;
using Xunit;

namespace Mystira.App.Api.Tests.Services;

public class AzureBlobServiceTests
{
    private readonly Mock<BlobServiceClient> _mockBlobServiceClient;
    private readonly Mock<BlobContainerClient> _mockContainerClient;
    private readonly Mock<BlobClient> _mockBlobClient;
    private readonly Mock<ILogger<AzureBlobService>> _mockLogger;
    private readonly AzureBlobService _service;

    public AzureBlobServiceTests()
    {
        _mockBlobServiceClient = new Mock<BlobServiceClient>();
        _mockContainerClient = new Mock<BlobContainerClient>();
        _mockBlobClient = new Mock<BlobClient>();
        _mockLogger = new Mock<ILogger<AzureBlobService>>();

        _mockBlobServiceClient
            .Setup(x => x.GetBlobContainerClient(It.IsAny<string>()))
            .Returns(_mockContainerClient.Object);

        _mockContainerClient
            .Setup(x => x.GetBlobClient(It.IsAny<string>()))
            .Returns(_mockBlobClient.Object);

        _service = new AzureBlobService(_mockBlobServiceClient.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task UploadMediaAsync_WithValidInput_ReturnsExpectedUrl()
    {
        // Arrange
        var fileName = "test-image.jpg";
        var contentType = "image/jpeg";
        var content = "test content";
        var fileStream = new MemoryStream(Encoding.UTF8.GetBytes(content));
    
        var mockResponse = Mock.Of<Response<BlobContentInfo>>();
        var expectedUrl = $"https://test.blob.core.windows.net/test-container/{fileName}";

        // Setup the container client
        _mockContainerClient
            .Setup(x => x.CreateIfNotExistsAsync(
                It.IsAny<PublicAccessType>(), 
                It.IsAny<Dictionary<string, string>>(), CancellationToken.None))
            .ReturnsAsync(Mock.Of<Response<BlobContainerInfo>>());

        _mockContainerClient
            .Setup(x => x.GetBlobClient(It.IsAny<string>()))
            .Returns(_mockBlobClient.Object);

        _mockBlobServiceClient
            .Setup(x => x.GetBlobContainerClient(It.IsAny<string>()))
            .Returns(_mockContainerClient.Object);

        // Setup the blob client with BlobUploadOptions parameter
        _mockBlobClient
            .Setup(x => x.UploadAsync(
                It.IsAny<Stream>(), 
                It.IsAny<BlobUploadOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockResponse);

        _mockBlobClient
            .Setup(x => x.Uri)
            .Returns(new Uri(expectedUrl));

        // Act
        var result = await _service.UploadMediaAsync(fileStream, fileName, contentType);

        // Assert
        result.Should().Be(expectedUrl);

        // Verify with the correct parameter types
        _mockBlobClient.Verify(x => x.UploadAsync(
            It.IsAny<Stream>(), 
            It.Is<BlobUploadOptions>(o => o.HttpHeaders != null && o.HttpHeaders.ContentType == contentType),
            It.IsAny<CancellationToken>()));
    }


    [Fact]
    public async Task GetMediaUrlAsync_WithValidBlobName_ReturnsCorrectUrl()
    {
        // Arrange
        var blobName = "test-file.jpg";
        var expectedUrl = $"https://test.blob.core.windows.net/test-container/{blobName}";

        _mockBlobClient
            .Setup(x => x.Uri)
            .Returns(new Uri(expectedUrl));

        // Act
        var result = await _service.GetMediaUrlAsync(blobName);

        // Assert
        result.Should().Be(expectedUrl);
        _mockContainerClient.Verify(x => x.GetBlobClient(blobName), Times.Once);
    }

    [Fact]
    public async Task DeleteMediaAsync_WithExistingBlob_ReturnsTrue()
    {
        // Arrange
        var blobName = "test-file.jpg";
        var mockResponse = Mock.Of<Response<bool>>(r => r.Value == true);

        _mockBlobClient
            .Setup(x => x.DeleteIfExistsAsync(
                It.IsAny<DeleteSnapshotsOption>(),
                It.IsAny<BlobRequestConditions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockResponse);

        // Act
        var result = await _service.DeleteMediaAsync(blobName);

        // Assert
        result.Should().BeTrue();
        _mockBlobClient.Verify(x => x.DeleteIfExistsAsync(
            DeleteSnapshotsOption.None,  // Changed from IncludeSnapshots to None
            null,
            It.IsAny<CancellationToken>()), Times.Once);
    }


    [Fact]
    public async Task DeleteMediaAsync_WithNonExistentBlob_ReturnsFalse()
    {
        // Arrange
        var blobName = "non-existent-file.jpg";
        var mockResponse = Mock.Of<Response<bool>>(r => r.Value == false);

        _mockBlobClient
            .Setup(x => x.DeleteIfExistsAsync(
                It.IsAny<DeleteSnapshotsOption>(),
                It.IsAny<BlobRequestConditions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockResponse);

        // Act
        var result = await _service.DeleteMediaAsync(blobName);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ListMediaAsync_WithPrefix_ReturnsFilteredList()
    {
        // Arrange
        var prefix = "images/";
        var blobNames = new List<string>
        {
            "images/photo1.jpg",
            "images/photo2.jpg",
            "documents/file.pdf"
        };

        // Create the expected filtered results
        var expectedResults = blobNames
            .Where(name => name.StartsWith(prefix))
            .ToList();

        // Setup mock container client
        var mockContainerClient = new Mock<BlobContainerClient>();
    
        // Setup the blob service client to return our mock container
        _mockBlobServiceClient
            .Setup(x => x.GetBlobContainerClient(It.IsAny<string>()))
            .Returns(mockContainerClient.Object);
    
        // Setup GetBlobsAsync to return only items matching the prefix
        mockContainerClient
            .Setup(x => x.GetBlobsAsync(
                It.IsAny<BlobTraits>(),
                It.IsAny<BlobStates>(),
                It.Is<string>(s => s == prefix),
                It.IsAny<CancellationToken>()))
            .Returns(() => {
                // Only return blob items that match the prefix
                var filteredItems = blobNames
                    .Where(name => name.StartsWith(prefix))
                    .Select(name => BlobsModelFactory.BlobItem(name: name));
            
                return GetAsyncBlobItems(filteredItems.ToList());
            });

        // Act
        var result = await _service.ListMediaAsync(prefix);

        // Assert
        result.Should().BeEquivalentTo(expectedResults);
    }



    // Create a mock AsyncPageable for blob items
    private static AsyncPageable<BlobItem> GetAsyncBlobItems(List<BlobItem> blobItems)
    {
        var asyncPageableMock = new Mock<AsyncPageable<BlobItem>>();
    
        asyncPageableMock
            .Setup(ap => ap.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
            .Returns(GetAsyncEnumerator(blobItems));
    
        return asyncPageableMock.Object;
    }


    // Helper method to create an async enumerator
    private static IAsyncEnumerator<BlobItem> GetAsyncEnumerator(List<BlobItem> blobItems)
    {
        var enumeratorMock = new Mock<IAsyncEnumerator<BlobItem>>();
    
        var index = -1;
        enumeratorMock
            .Setup(e => e.Current)
            .Returns(() => index >= 0 && index < blobItems.Count ? blobItems[index] : null);
    
        enumeratorMock
            .Setup(e => e.MoveNextAsync())
            .Returns(() => {
                index++;
                return new ValueTask<bool>(index < blobItems.Count);
            });
    
        return enumeratorMock.Object;
    }


    
    [Fact]
    public async Task DownloadMediaAsync_WithExistingBlob_ReturnsStream()
    {
        // Arrange
        var blobName = "test-file.jpg";
        var content = "test file content";
        var contentStream = new MemoryStream(Encoding.UTF8.GetBytes(content));
    
        // Setup ExistsAsync to return true
        _mockBlobClient
            .Setup(x => x.ExistsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(true, new MockHttpResponse(200)));
    
        // Setup DownloadStreamingAsync correctly
        var mockStreamingResponse = Mock.Of<Response<BlobDownloadStreamingResult>>(
            r => r.Value == BlobsModelFactory.BlobDownloadStreamingResult(
                contentStream, null));
    
        _mockBlobClient
            .Setup(x => x.DownloadStreamingAsync(
                It.IsAny<BlobDownloadOptions>(), CancellationToken.None))
            .ReturnsAsync(mockStreamingResponse);

        // Act
        var result = await _service.DownloadMediaAsync(blobName);

        // Assert
        result.Should().NotBeNull();
    
        // Verify content
        using var reader = new StreamReader(result!, Encoding.UTF8);
        var resultContent = await reader.ReadToEndAsync();
        resultContent.Should().Be(content);
    }


    [Fact]
    public async Task DownloadMediaAsync_WithNonExistentBlob_ReturnsNull()
    {
        // Arrange
        var blobName = "non-existent-file.jpg";

        // Mock ExistsAsync to return false
        _mockBlobClient
            .Setup(x => x.ExistsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<Response<bool>>(r => r.Value == false));

        // We don't need to setup DownloadStreamingAsync since it won't be called

        // Act
        var result = await _service.DownloadMediaAsync(blobName);

        // Assert
        result.Should().BeNull();
    }


    [Fact]
    public async Task UploadMediaAsync_WithException_ThrowsException()
    {
        // Arrange
        var fileName = "test-image.jpg";
        var contentType = "image/jpeg";
        var fileStream = new MemoryStream(Encoding.UTF8.GetBytes("test"));

        // Setup container client
        _mockContainerClient
            .Setup(x => x.CreateIfNotExistsAsync(
                It.IsAny<PublicAccessType>(), 
                It.IsAny<Dictionary<string, string>>(), CancellationToken.None))
            .ReturnsAsync(Mock.Of<Response<BlobContainerInfo>>());

        _mockContainerClient
            .Setup(x => x.GetBlobClient(It.IsAny<string>()))
            .Returns(_mockBlobClient.Object);

        _mockBlobServiceClient
            .Setup(x => x.GetBlobContainerClient(It.IsAny<string>()))
            .Returns(_mockContainerClient.Object);

        // Setup blob client to throw exception with BlobUploadOptions
        _mockBlobClient
            .Setup(x => x.UploadAsync(
                It.IsAny<Stream>(),
                It.IsAny<BlobUploadOptions>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new RequestFailedException("Upload failed"));

        // Act & Assert
        await _service.Invoking(s => s.UploadMediaAsync(fileStream, fileName, contentType))
            .Should().ThrowAsync<RequestFailedException>()
            .WithMessage("Upload failed");
    }


    private static BlobItem CreateBlobItem(string name)
    {
        // Create a mock BlobItem - this may need adjustment based on the actual BlobItem structure
        // In real Azure SDK, BlobItem is typically created internally, so this is a simplified mock
        return BlobsModelFactory.BlobItem(name: name);
    }

    private static AsyncPageable<BlobItem> CreateMockPageable(BlobItem[] items)
    {
        var mockPage = Mock.Of<Page<BlobItem>>(p => 
            p.Values == items &&
            p.ContinuationToken == null);
        
        var pages = new[] { mockPage };
        
        var mockPageable = Mock.Of<AsyncPageable<BlobItem>>();
        Mock.Get(mockPageable)
            .Setup(x => x.AsPages(It.IsAny<string>(), It.IsAny<int?>()))
            .Returns(pages.ToAsyncEnumerable());

        return mockPageable;
    }
    
    // Add this inside your test class
    private class MockHttpResponse : Response
    {
        private readonly int _status;

        public MockHttpResponse(int status)
        {
            _status = status;
        }

        public override int Status => _status;
        public override string ReasonPhrase => "OK";
        public override Stream? ContentStream { get; set; }
        public override string ClientRequestId { get => ""; set { } }
        public override void Dispose() { }
        protected override bool ContainsHeader(string name) => false;
        protected override IEnumerable<HttpHeader> EnumerateHeaders() => new List<HttpHeader>();
        protected override bool TryGetHeader(string name, out string value)
        {
            value = null;
            return false;
        }
        protected override bool TryGetHeaderValues(string name, out IEnumerable<string> values)
        {
            values = null;
            return false;
        }
    }
}