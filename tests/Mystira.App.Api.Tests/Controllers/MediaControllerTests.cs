using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using MediatR;
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
        Mock<IMediator> mediatorMock,
        Mock<IMediaApiService> mediaServiceMock)
    {
        var logger = new Mock<ILogger<MediaController>>().Object;
        var controller = new MediaController(
            mediatorMock.Object,
            mediaServiceMock.Object,
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
        var mediator = new Mock<IMediator>();
        var mediaService = new Mock<IMediaApiService>();
        mediaService.Setup(s => s.GetMediaByIdAsync(mediaId)).ReturnsAsync(mediaAsset);
        var controller = CreateController(mediator, mediaService);

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
        var mediator = new Mock<IMediator>();
        var mediaService = new Mock<IMediaApiService>();
        mediaService.Setup(s => s.GetMediaByIdAsync(mediaId)).ReturnsAsync((MediaAsset?)null);
        var controller = CreateController(mediator, mediaService);

        // Act
        var result = await controller.GetMediaById(mediaId);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
        var notFound = (NotFoundObjectResult)result.Result!;
        notFound.Value.Should().BeOfType<ErrorResponse>();
        var error = (ErrorResponse)notFound.Value!;
        error.Message.Should().Contain(mediaId);
    }

    [Fact]
    public async Task GetMediaFile_WhenMediaExists_ReturnsFile()
    {
        // Arrange
        var mediaId = "image-final-logo-fe3f75db";
        var stream = new MemoryStream(new byte[] { 1, 2, 3, 4 });
        var contentType = "image/png";
        var fileName = "logo.png";
        var mediator = new Mock<IMediator>();
        var mediaService = new Mock<IMediaApiService>();
        mediaService.Setup(s => s.GetMediaFileAsync(mediaId))
            .ReturnsAsync((stream, contentType, fileName));
        var controller = CreateController(mediator, mediaService);

        // Act
        var result = await controller.GetMediaFile(mediaId);

        // Assert
        result.Should().BeOfType<FileStreamResult>();
        var file = (FileStreamResult)result;
        file.ContentType.Should().Be(contentType);
        file.FileDownloadName.Should().Be(fileName);
        mediaService.Verify(s => s.GetMediaFileAsync(mediaId), Times.Once);
    }

    [Fact]
    public async Task GetMediaFile_WhenMediaNotFound_ReturnsNotFound()
    {
        // Arrange
        var mediaId = "non-existent-media";
        var mediator = new Mock<IMediator>();
        var mediaService = new Mock<IMediaApiService>();
        mediaService.Setup(s => s.GetMediaFileAsync(mediaId))
            .ReturnsAsync(((Stream, string, string)?)null);
        var controller = CreateController(mediator, mediaService);

        // Act
        var result = await controller.GetMediaFile(mediaId);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        var notFound = (NotFoundObjectResult)result;
        notFound.Value.Should().BeOfType<ErrorResponse>();
        var error = (ErrorResponse)notFound.Value!;
        error.Message.Should().Contain(mediaId);
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
