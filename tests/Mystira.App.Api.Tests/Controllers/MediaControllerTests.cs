using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Mystira.App.Api.Controllers;
using Mystira.App.Application.CQRS.MediaAssets.Queries;
using Mystira.App.Contracts.Responses.Common;
using Mystira.App.Domain.Models;
using Xunit;

namespace Mystira.App.Api.Tests.Controllers;

/// <summary>
/// Tests for MediaController - validates hexagonal architecture compliance.
/// Controller should ONLY use IMediator (CQRS pattern), no direct service dependencies.
/// </summary>
public class MediaControllerTests
{
    private static MediaController CreateController(Mock<IMediator> mediatorMock)
    {
        var logger = new Mock<ILogger<MediaController>>().Object;
        var controller = new MediaController(
            mediatorMock.Object,
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
        mediator.Setup(m => m.Send(It.IsAny<GetMediaAssetQuery>(), It.IsAny<System.Threading.CancellationToken>()))
            .ReturnsAsync(mediaAsset);
        var controller = CreateController(mediator);

        // Act
        var result = await controller.GetMediaById(mediaId);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var ok = result.Result as OkObjectResult;
        ok!.Value.Should().BeEquivalentTo(mediaAsset);
        mediator.Verify(m => m.Send(It.IsAny<GetMediaAssetQuery>(), It.IsAny<System.Threading.CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetMediaById_WhenMediaNotFound_ReturnsNotFound()
    {
        // Arrange
        var mediaId = "non-existent-media";
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<GetMediaAssetQuery>(), It.IsAny<System.Threading.CancellationToken>()))
            .ReturnsAsync((MediaAsset?)null);
        var controller = CreateController(mediator);

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
        mediator.Setup(m => m.Send(It.IsAny<GetMediaFileQuery>(), It.IsAny<System.Threading.CancellationToken>()))
            .ReturnsAsync((stream, contentType, fileName));
        var controller = CreateController(mediator);

        // Act
        var result = await controller.GetMediaFile(mediaId);

        // Assert
        result.Should().BeOfType<FileStreamResult>();
        var file = (FileStreamResult)result;
        file.ContentType.Should().Be(contentType);
        file.FileDownloadName.Should().Be(fileName);
        mediator.Verify(m => m.Send(It.IsAny<GetMediaFileQuery>(), It.IsAny<System.Threading.CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetMediaFile_WhenMediaNotFound_ReturnsNotFound()
    {
        // Arrange
        var mediaId = "non-existent-media";
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<GetMediaFileQuery>(), It.IsAny<System.Threading.CancellationToken>()))
            .ReturnsAsync(((Stream, string, string)?)null);
        var controller = CreateController(mediator);

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

    [Fact]
    public void MediaController_OnlyDependsOnIMediator()
    {
        // Arrange & Act
        var constructor = typeof(MediaController).GetConstructors()[0];
        var parameters = constructor.GetParameters();

        // Assert
        parameters.Should().HaveCount(2, "controller should only have IMediator and ILogger dependencies");
        parameters[0].ParameterType.Name.Should().Be("IMediator");
        parameters[1].ParameterType.Name.Should().Contain("ILogger");
    }
}
