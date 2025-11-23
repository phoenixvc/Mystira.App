using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Mystira.App.Api.Controllers;
using Mystira.App.Api.Services;
using Mystira.App.Contracts.Responses.Common;
using Mystira.App.Domain.Models;
using Xunit;

namespace Mystira.App.Api.Tests.Controllers;

public class MediaControllerTests
{
    private static MediaController CreateController(
        Mock<IMediaApiService> mediaServiceMock,
        Mock<IMediaMetadataService> metadataServiceMock)
    {
        var logger = new Mock<ILogger<MediaController>>().Object;
        var controller = new MediaController(
            mediaServiceMock.Object,
            metadataServiceMock.Object,
            logger)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
        return controller;
    }

    [Fact]
    public async Task GetMediaById_WhenMediaExists_ReturnsOk()
    {
        // Arrange
        var mediaId = "image-final-logo-fe3f75db";
        var mediaAsset = new MediaAsset
        {
            Id = mediaId,
            MediaId = mediaId,
            MediaType = "image",
            MimeType = "image/png",
            Url = "https://example.com/logo.png"
        };
        var mediaService = new Mock<IMediaApiService>();
        mediaService.Setup(s => s.GetMediaByIdAsync(mediaId)).ReturnsAsync(mediaAsset);
        var metadataService = new Mock<IMediaMetadataService>();
        var controller = CreateController(mediaService, metadataService);

        // Act
        var result = await controller.GetMediaById(mediaId);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var ok = result.Result as OkObjectResult;
        ok!.Value.Should().BeEquivalentTo(mediaAsset);
        mediaService.Verify(s => s.GetMediaByIdAsync(mediaId), Times.Once);
    }

    [Fact]
    public async Task GetMediaById_WhenMediaNotFound_ReturnsNotFound()
    {
        // Arrange
        var mediaId = "non-existent-media";
        var mediaService = new Mock<IMediaApiService>();
        mediaService.Setup(s => s.GetMediaByIdAsync(mediaId)).ReturnsAsync((MediaAsset?)null);
        var metadataService = new Mock<IMediaMetadataService>();
        var controller = CreateController(mediaService, metadataService);

        // Act
        var result = await controller.GetMediaById(mediaId);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
        var notFound = result.Result as NotFoundObjectResult;
        notFound!.Value.Should().BeOfType<ErrorResponse>();
        var error = notFound.Value as ErrorResponse;
        error!.Message.Should().Contain(mediaId);
    }

    [Fact]
    public async Task GetMediaFile_WhenMediaExists_ReturnsFile()
    {
        // Arrange
        var mediaId = "image-final-logo-fe3f75db";
        var stream = new MemoryStream(new byte[] { 1, 2, 3, 4 });
        var contentType = "image/png";
        var fileName = "logo.png";
        var mediaService = new Mock<IMediaApiService>();
        mediaService.Setup(s => s.GetMediaFileAsync(mediaId))
            .ReturnsAsync((stream, contentType, fileName));
        var metadataService = new Mock<IMediaMetadataService>();
        var controller = CreateController(mediaService, metadataService);

        // Act
        var result = await controller.GetMediaFile(mediaId);

        // Assert
        result.Should().BeOfType<FileStreamResult>();
        var file = result as FileStreamResult;
        file!.ContentType.Should().Be(contentType);
        file.FileDownloadName.Should().Be(fileName);
        mediaService.Verify(s => s.GetMediaFileAsync(mediaId), Times.Once);
    }

    [Fact]
    public async Task GetMediaFile_WhenMediaNotFound_ReturnsNotFound()
    {
        // Arrange
        var mediaId = "non-existent-media";
        var mediaService = new Mock<IMediaApiService>();
        mediaService.Setup(s => s.GetMediaFileAsync(mediaId))
            .ReturnsAsync(((Stream, string, string)?)null);
        var metadataService = new Mock<IMediaMetadataService>();
        var controller = CreateController(mediaService, metadataService);

        // Act
        var result = await controller.GetMediaFile(mediaId);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        var notFound = result as NotFoundObjectResult;
        notFound!.Value.Should().BeOfType<ErrorResponse>();
        var error = notFound.Value as ErrorResponse;
        error!.Message.Should().Contain(mediaId);
    }

    [Fact]
    public void MediaController_HasAllowAnonymousOnGetMediaById()
    {
        // Arrange
        var method = typeof(MediaController).GetMethod("GetMediaById");

        // Act
        var attributes = method!.GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute), false);

        // Assert
        attributes.Should().NotBeEmpty("GetMediaById should have [AllowAnonymous] attribute for landing page access");
    }

    [Fact]
    public void MediaController_HasAllowAnonymousOnGetMediaFile()
    {
        // Arrange
        var method = typeof(MediaController).GetMethod("GetMediaFile");

        // Act
        var attributes = method!.GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute), false);

        // Assert
        attributes.Should().NotBeEmpty("GetMediaFile should have [AllowAnonymous] attribute for landing page access");
    }
}
