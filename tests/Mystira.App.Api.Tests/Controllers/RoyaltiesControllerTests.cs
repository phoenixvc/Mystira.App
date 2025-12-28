using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Mystira.App.Api.Controllers;
using Mystira.App.Api.Models;
using Mystira.App.Application.CQRS.Royalties.Commands;
using Mystira.App.Application.CQRS.Royalties.Queries;
using Mystira.App.Domain.Models;
using Mystira.Contracts.App.Requests.Royalties;
using Wolverine;

namespace Mystira.App.Api.Tests.Controllers;

public class RoyaltiesControllerTests
{
    private readonly Mock<IMessageBus> _mockBus;
    private readonly Mock<ILogger<RoyaltiesController>> _mockLogger;
    private readonly RoyaltiesController _controller;

    public RoyaltiesControllerTests()
    {
        _mockBus = new Mock<IMessageBus>();
        _mockLogger = new Mock<ILogger<RoyaltiesController>>();
        _controller = new RoyaltiesController(_mockBus.Object, _mockLogger.Object);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    #region GetClaimableRoyalties Tests

    [Fact]
    public async Task GetClaimableRoyalties_WithValidIpAssetId_ReturnsOk()
    {
        // Arrange
        var ipAssetId = "ip-asset-123";
        var balance = new RoyaltyBalance
        {
            IpAssetId = ipAssetId,
            ClaimableAmount = 1000m,
            Currency = "ETH"
        };

        _mockBus
            .Setup(x => x.InvokeAsync<RoyaltyBalance>(
                It.IsAny<GetClaimableRoyaltiesQuery>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ReturnsAsync(balance);

        // Act
        var result = await _controller.GetClaimableRoyalties(ipAssetId);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedBalance = okResult.Value.Should().BeOfType<RoyaltyBalance>().Subject;
        returnedBalance.IpAssetId.Should().Be(ipAssetId);
        returnedBalance.ClaimableAmount.Should().Be(1000m);
    }

    [Fact]
    public async Task GetClaimableRoyalties_WithEmptyIpAssetId_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.GetClaimableRoyalties("");

        // Assert
        var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var errorResponse = badRequestResult.Value.Should().BeOfType<ErrorResponse>().Subject;
        errorResponse.Message.Should().Contain("IP Asset ID is required");
    }

    [Fact]
    public async Task GetClaimableRoyalties_WithWhitespaceIpAssetId_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.GetClaimableRoyalties("   ");

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetClaimableRoyalties_WithArgumentException_ReturnsBadRequest()
    {
        // Arrange
        _mockBus
            .Setup(x => x.InvokeAsync<RoyaltyBalance>(
                It.IsAny<GetClaimableRoyaltiesQuery>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ThrowsAsync(new ArgumentException("Invalid IP Asset"));

        // Act
        var result = await _controller.GetClaimableRoyalties("invalid-id");

        // Assert
        var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var errorResponse = badRequestResult.Value.Should().BeOfType<ErrorResponse>().Subject;
        errorResponse.Message.Should().Contain("Invalid IP Asset ID");
    }

    [Fact]
    public async Task GetClaimableRoyalties_WithInvalidOperationException_ReturnsInternalServerError()
    {
        // Arrange
        _mockBus
            .Setup(x => x.InvokeAsync<RoyaltyBalance>(
                It.IsAny<GetClaimableRoyaltiesQuery>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ThrowsAsync(new InvalidOperationException("Operation failed"));

        // Act
        var result = await _controller.GetClaimableRoyalties("ip-asset-123");

        // Assert
        var statusResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task GetClaimableRoyalties_WithException_ReturnsInternalServerError()
    {
        // Arrange
        _mockBus
            .Setup(x => x.InvokeAsync<RoyaltyBalance>(
                It.IsAny<GetClaimableRoyaltiesQuery>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetClaimableRoyalties("ip-asset-123");

        // Assert
        var statusResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(500);
    }

    #endregion

    #region PayRoyalty Tests

    [Fact]
    public async Task PayRoyalty_WithValidRequest_ReturnsOk()
    {
        // Arrange
        var ipAssetId = "ip-asset-123";
        var request = new PayRoyaltyRequest
        {
            Amount = 100m,
            PayerReference = "payer-wallet-123"
        };

        var paymentResult = new RoyaltyPaymentResult
        {
            Success = true,
            TransactionHash = "tx-hash-456",
            Amount = 100m
        };

        _mockBus
            .Setup(x => x.InvokeAsync<RoyaltyPaymentResult>(
                It.IsAny<PayRoyaltyCommand>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ReturnsAsync(paymentResult);

        // Act
        var result = await _controller.PayRoyalty(ipAssetId, request);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedResult = okResult.Value.Should().BeOfType<RoyaltyPaymentResult>().Subject;
        returnedResult.Success.Should().BeTrue();
        returnedResult.TransactionHash.Should().Be("tx-hash-456");
    }

    [Fact]
    public async Task PayRoyalty_WithEmptyIpAssetId_ReturnsBadRequest()
    {
        // Arrange
        var request = new PayRoyaltyRequest { Amount = 100m };

        // Act
        var result = await _controller.PayRoyalty("", request);

        // Assert
        var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var errorResponse = badRequestResult.Value.Should().BeOfType<ErrorResponse>().Subject;
        errorResponse.Message.Should().Contain("IP Asset ID is required");
    }

    [Fact]
    public async Task PayRoyalty_WithZeroAmount_ReturnsBadRequest()
    {
        // Arrange
        var request = new PayRoyaltyRequest { Amount = 0 };

        // Act
        var result = await _controller.PayRoyalty("ip-asset-123", request);

        // Assert
        var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var errorResponse = badRequestResult.Value.Should().BeOfType<ErrorResponse>().Subject;
        errorResponse.Message.Should().Contain("Amount must be greater than zero");
    }

    [Fact]
    public async Task PayRoyalty_WithNegativeAmount_ReturnsBadRequest()
    {
        // Arrange
        var request = new PayRoyaltyRequest { Amount = -50m };

        // Act
        var result = await _controller.PayRoyalty("ip-asset-123", request);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task PayRoyalty_WhenPaymentFails_ReturnsBadRequest()
    {
        // Arrange
        var request = new PayRoyaltyRequest { Amount = 100m };
        var paymentResult = new RoyaltyPaymentResult
        {
            Success = false,
            ErrorMessage = "Insufficient funds"
        };

        _mockBus
            .Setup(x => x.InvokeAsync<RoyaltyPaymentResult>(
                It.IsAny<PayRoyaltyCommand>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ReturnsAsync(paymentResult);

        // Act
        var result = await _controller.PayRoyalty("ip-asset-123", request);

        // Assert
        var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var errorResponse = badRequestResult.Value.Should().BeOfType<ErrorResponse>().Subject;
        errorResponse.Message.Should().Contain("Insufficient funds");
    }

    [Fact]
    public async Task PayRoyalty_WithArgumentException_ReturnsBadRequest()
    {
        // Arrange
        var request = new PayRoyaltyRequest { Amount = 100m };

        _mockBus
            .Setup(x => x.InvokeAsync<RoyaltyPaymentResult>(
                It.IsAny<PayRoyaltyCommand>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ThrowsAsync(new ArgumentException("Invalid payment request"));

        // Act
        var result = await _controller.PayRoyalty("ip-asset-123", request);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region ClaimRoyalties Tests

    [Fact]
    public async Task ClaimRoyalties_WithValidRequest_ReturnsOk()
    {
        // Arrange
        var ipAssetId = "ip-asset-123";
        var request = new ClaimRoyaltiesRequest
        {
            ContributorWallet = "0x1234567890abcdef"
        };

        _mockBus
            .Setup(x => x.InvokeAsync<string>(
                It.IsAny<ClaimRoyaltiesCommand>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ReturnsAsync("tx-hash-789");

        // Act
        var result = await _controller.ClaimRoyalties(ipAssetId, request);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ClaimRoyaltiesResponse>().Subject;
        response.IpAssetId.Should().Be(ipAssetId);
        response.ContributorWallet.Should().Be("0x1234567890abcdef");
        response.TransactionHash.Should().Be("tx-hash-789");
    }

    [Fact]
    public async Task ClaimRoyalties_WithEmptyIpAssetId_ReturnsBadRequest()
    {
        // Arrange
        var request = new ClaimRoyaltiesRequest { ContributorWallet = "0x1234567890abcdef" };

        // Act
        var result = await _controller.ClaimRoyalties("", request);

        // Assert
        var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var errorResponse = badRequestResult.Value.Should().BeOfType<ErrorResponse>().Subject;
        errorResponse.Message.Should().Contain("IP Asset ID is required");
    }

    [Fact]
    public async Task ClaimRoyalties_WithEmptyContributorWallet_ReturnsBadRequest()
    {
        // Arrange
        var request = new ClaimRoyaltiesRequest { ContributorWallet = "" };

        // Act
        var result = await _controller.ClaimRoyalties("ip-asset-123", request);

        // Assert
        var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var errorResponse = badRequestResult.Value.Should().BeOfType<ErrorResponse>().Subject;
        errorResponse.Message.Should().Contain("Contributor wallet address is required");
    }

    [Fact]
    public async Task ClaimRoyalties_WithWhitespaceContributorWallet_ReturnsBadRequest()
    {
        // Arrange
        var request = new ClaimRoyaltiesRequest { ContributorWallet = "   " };

        // Act
        var result = await _controller.ClaimRoyalties("ip-asset-123", request);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task ClaimRoyalties_WithArgumentException_ReturnsBadRequest()
    {
        // Arrange
        var request = new ClaimRoyaltiesRequest { ContributorWallet = "invalid-wallet" };

        _mockBus
            .Setup(x => x.InvokeAsync<string>(
                It.IsAny<ClaimRoyaltiesCommand>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ThrowsAsync(new ArgumentException("Invalid wallet address"));

        // Act
        var result = await _controller.ClaimRoyalties("ip-asset-123", request);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task ClaimRoyalties_WithException_ReturnsInternalServerError()
    {
        // Arrange
        var request = new ClaimRoyaltiesRequest { ContributorWallet = "0x1234567890abcdef" };

        _mockBus
            .Setup(x => x.InvokeAsync<string>(
                It.IsAny<ClaimRoyaltiesCommand>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ThrowsAsync(new Exception("Blockchain error"));

        // Act
        var result = await _controller.ClaimRoyalties("ip-asset-123", request);

        // Assert
        var statusResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(500);
    }

    #endregion
}
