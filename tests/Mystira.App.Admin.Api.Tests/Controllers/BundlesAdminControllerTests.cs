using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Mystira.App.Admin.Api.Controllers;
using Mystira.App.Admin.Api.Services;
using Mystira.App.Domain.Models;
using Xunit;

namespace Mystira.App.Admin.Api.Tests.Controllers;

public class BundlesAdminControllerTests
{
    private static BundlesAdminController CreateController(Mock<IContentBundleAdminService> serviceMock)
    {
        var logger = new Mock<ILogger<BundlesAdminController>>().Object;
        var controller = new BundlesAdminController(serviceMock.Object, logger)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
        return controller;
    }

    [Fact]
    public async Task GetAll_ReturnsOk_WithBundles()
    {
        var bundles = new List<ContentBundle>
        {
            new() { Id = "b1", Title = "Bundle 1", AgeGroup = "6-9" },
            new() { Id = "b2", Title = "Bundle 2", AgeGroup = "10-12" }
        };
        var service = new Mock<IContentBundleAdminService>();
        service.Setup(s => s.GetAllAsync()).ReturnsAsync(bundles);
        var controller = CreateController(service);

        var result = await controller.GetAll();

        result.Result.Should().BeOfType<OkObjectResult>();
        var ok = result.Result as OkObjectResult;
        ok!.Value.Should().BeEquivalentTo(bundles);
        service.Verify(s => s.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task GetAll_WhenServiceThrows_Returns500_WithTraceId()
    {
        var service = new Mock<IContentBundleAdminService>();
        service.Setup(s => s.GetAllAsync()).ThrowsAsync(new System.Exception("boom"));
        var controller = CreateController(service);

        var result = await controller.GetAll();

        result.Result.Should().BeOfType<ObjectResult>();
        var obj = result.Result as ObjectResult;
        obj!.StatusCode.Should().Be(500);
        obj.Value!.ToString().Should().Contain("TraceId");
    }

    [Fact]
    public async Task GetById_WhenFound_ReturnsOk_WithBundle()
    {
        var bundle = new ContentBundle { Id = "b1", Title = "Bundle 1" };
        var service = new Mock<IContentBundleAdminService>();
        service.Setup(s => s.GetByIdAsync("b1")).ReturnsAsync(bundle);
        var controller = CreateController(service);

        var result = await controller.GetById("b1");

        result.Result.Should().BeOfType<OkObjectResult>();
        var ok = result.Result as OkObjectResult;
        ok!.Value.Should().BeEquivalentTo(bundle);
    }

    [Fact]
    public async Task GetById_WhenNotFound_Returns404()
    {
        var service = new Mock<IContentBundleAdminService>();
        service.Setup(s => s.GetByIdAsync("missing")).ReturnsAsync((ContentBundle?)null);
        var controller = CreateController(service);

        var result = await controller.GetById("missing");

        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetById_WhenServiceThrows_Returns500_WithTraceId()
    {
        var service = new Mock<IContentBundleAdminService>();
        service.Setup(s => s.GetByIdAsync("b1")).ThrowsAsync(new System.Exception("err"));
        var controller = CreateController(service);

        var result = await controller.GetById("b1");

        result.Result.Should().BeOfType<ObjectResult>();
        var obj = result.Result as ObjectResult;
        obj!.StatusCode.Should().Be(500);
        obj.Value!.ToString().Should().Contain("TraceId");
    }

    [Fact]
    public async Task Create_ReturnsCreated_WithBundle()
    {
        var input = new ContentBundle { Title = "New", AgeGroup = "6-9" };
        var created = new ContentBundle { Id = "newid", Title = "New", AgeGroup = "6-9" };
        var service = new Mock<IContentBundleAdminService>();
        service.Setup(s => s.CreateAsync(It.IsAny<ContentBundle>())).ReturnsAsync(created);
        var controller = CreateController(service);

        var result = await controller.Create(input);

        result.Result.Should().BeOfType<CreatedAtActionResult>();
        var createdResult = result.Result as CreatedAtActionResult;
        createdResult!.ActionName.Should().Be(nameof(BundlesAdminController.GetById));
        createdResult.RouteValues!["id"].Should().Be("newid");
        createdResult.Value.Should().BeEquivalentTo(created);
    }

    [Fact]
    public async Task Create_WhenServiceThrows_Returns500_WithTraceId()
    {
        var input = new ContentBundle { Title = "New" };
        var service = new Mock<IContentBundleAdminService>();
        service.Setup(s => s.CreateAsync(It.IsAny<ContentBundle>())).ThrowsAsync(new System.Exception("err"));
        var controller = CreateController(service);

        var result = await controller.Create(input);

        result.Result.Should().BeOfType<ObjectResult>();
        var obj = result.Result as ObjectResult;
        obj!.StatusCode.Should().Be(500);
        obj.Value!.ToString().Should().Contain("TraceId");
    }

    [Fact]
    public async Task Update_WhenFound_ReturnsOk_WithUpdatedBundle()
    {
        var input = new ContentBundle { Title = "Updated", AgeGroup = "10-12" };
        var updated = new ContentBundle { Id = "b1", Title = "Updated", AgeGroup = "10-12" };
        var service = new Mock<IContentBundleAdminService>();
        service.Setup(s => s.UpdateAsync("b1", It.IsAny<ContentBundle>())).ReturnsAsync(updated);
        var controller = CreateController(service);

        var result = await controller.Update("b1", input);

        result.Result.Should().BeOfType<OkObjectResult>();
        var ok = result.Result as OkObjectResult;
        ok!.Value.Should().BeEquivalentTo(updated);
    }

    [Fact]
    public async Task Update_WhenNotFound_Returns404()
    {
        var input = new ContentBundle { Title = "Updated" };
        var service = new Mock<IContentBundleAdminService>();
        service.Setup(s => s.UpdateAsync("missing", It.IsAny<ContentBundle>())).ReturnsAsync((ContentBundle?)null);
        var controller = CreateController(service);

        var result = await controller.Update("missing", input);

        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Update_WhenServiceThrows_Returns500_WithTraceId()
    {
        var input = new ContentBundle { Title = "Updated" };
        var service = new Mock<IContentBundleAdminService>();
        service.Setup(s => s.UpdateAsync("b1", It.IsAny<ContentBundle>())).ThrowsAsync(new System.Exception("err"));
        var controller = CreateController(service);

        var result = await controller.Update("b1", input);

        result.Result.Should().BeOfType<ObjectResult>();
        var obj = result.Result as ObjectResult;
        obj!.StatusCode.Should().Be(500);
        obj.Value!.ToString().Should().Contain("TraceId");
    }
}
