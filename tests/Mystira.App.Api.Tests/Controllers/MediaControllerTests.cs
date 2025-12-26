using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Mystira.App.Api.Controllers;
using Mystira.App.Application.CQRS.MediaAssets.Queries;
using Mystira.Contracts.App.Responses.Common;
using Mystira.App.Domain.Models;
using Wolverine;
using Xunit;

namespace Mystira.App.Api.Tests.Controllers;

/// <summary>
/// Tests for MediaController - validates hexagonal architecture compliance.
/// Controller should ONLY use IMessageBus (CQRS pattern), no direct service dependencies.
/// </summary>
public class MediaControllerTests
{
    private static MediaController CreateController(Mock<IMessageBus> busMock)
    {
        var logger = new Mock<ILogger<MediaController>>().Object;
        var controller = new MediaController(
            busMock.Object,
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
        var bus = new Mock<IMessageBus>();
        bus.Setup(m => m.InvokeAsync<MediaAsset?>(It.IsAny<GetMediaAssetQuery>(), It.IsAny<CancellationToken>(), It.IsAny<TimeSpan?>()))
            .ReturnsAsync(mediaAsset);
        var controller = CreateController(bus);

        // Act
        var result = await controller.GetMediaById(mediaId);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var ok = result.Result as OkObjectResult;
        ok!.Value.Should().BeEquivalentTo(mediaAsset);
        bus.Verify(m => m.InvokeAsync<MediaAsset?>(It.IsAny<GetMediaAssetQuery>(), It.IsAny<CancellationToken>(), It.IsAny<TimeSpan?>()), Times.Once);
    }

    [Fact]
    public async Task GetMediaById_WhenMediaNotFound_ReturnsNotFound()
    {
        // Arrange
        var mediaId = "non-existent-media";
        var bus = new Mock<IMessageBus>();
        bus.Setup(m => m.InvokeAsync<MediaAsset?>(It.IsAny<GetMediaAssetQuery>(), It.IsAny<CancellationToken>(), It.IsAny<TimeSpan?>()))
            .ReturnsAsync((MediaAsset?)null);
        var controller = CreateController(bus);

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
        var bus = new Mock<IMessageBus>();
        bus.Setup(m => m.InvokeAsync<(Stream, string, string)?>(It.IsAny<GetMediaFileQuery>(), It.IsAny<CancellationToken>(), It.IsAny<TimeSpan?>()))
            .ReturnsAsync((stream, contentType, fileName));
        var controller = CreateController(bus);

        // Act
        var result = await controller.GetMediaFile(mediaId);

        // Assert
        result.Should().BeOfType<FileStreamResult>();
        var file = (FileStreamResult)result;
        file.ContentType.Should().Be(contentType);
        file.FileDownloadName.Should().Be(fileName);
        bus.Verify(m => m.InvokeAsync<(Stream, string, string)?>(It.IsAny<GetMediaFileQuery>(), It.IsAny<CancellationToken>(), It.IsAny<TimeSpan?>()), Times.Once);
    }

    [Fact]
    public async Task GetMediaFile_WhenMediaNotFound_ReturnsNotFound()
    {
        // Arrange
        var mediaId = "non-existent-media";
        var bus = new Mock<IMessageBus>();
        bus.Setup(m => m.InvokeAsync<(Stream, string, string)?>(It.IsAny<GetMediaFileQuery>(), It.IsAny<CancellationToken>(), It.IsAny<TimeSpan?>()))
            .ReturnsAsync(((Stream, string, string)?)null);
        var controller = CreateController(bus);

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
        var attributes = method!.GetCustomAttributes(typeof(AllowAnonymousAttribute), false);

        // Assert
        attributes.Should().NotBeEmpty("GetMediaById should have [AllowAnonymous] attribute for landing page access");
    }

    [Fact]
    public void MediaController_HasAllowAnonymousOnGetMediaFile()
    {
        // Arrange
        var method = typeof(MediaController).GetMethod("GetMediaFile");

        // Act
        var attributes = method!.GetCustomAttributes(typeof(AllowAnonymousAttribute), false);

        // Assert
        attributes.Should().NotBeEmpty("GetMediaFile should have [AllowAnonymous] attribute for landing page access");
    }

    [Fact]
    public void MediaController_GetMediaFile_HasResponseCacheAttribute()
    {
        // Arrange
        var method = typeof(MediaController).GetMethod("GetMediaFile");

        // Act
        var attribute = (ResponseCacheAttribute?)method!.GetCustomAttributes(typeof(ResponseCacheAttribute), false)[0];

        // Assert
        attribute.Should().NotBeNull();
        attribute!.Duration.Should().Be(31536000);
        attribute.Location.Should().Be(ResponseCacheLocation.Any);
    }

    [Fact]
    public void MediaController_OnlyDependsOnIMessageBus()
    {
        // Arrange & Act
        var constructor = typeof(MediaController).GetConstructors()[0];
        var parameters = constructor.GetParameters();

        // Assert
        parameters.Should().HaveCount(2, "controller should only have IMessageBus and ILogger dependencies");
        parameters[0].ParameterType.Name.Should().Be("IMessageBus");
        parameters[1].ParameterType.Name.Should().Contain("ILogger");
    }
}
