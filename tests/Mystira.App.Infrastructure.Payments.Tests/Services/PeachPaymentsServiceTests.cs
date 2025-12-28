using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using Mystira.App.Infrastructure.Payments.Configuration;
using Mystira.App.Infrastructure.Payments.Services.PeachPayments;
using Mystira.Contracts.App.Enums;
using Mystira.Contracts.App.Requests.Payments;

namespace Mystira.App.Infrastructure.Payments.Tests.Services;

public class PeachPaymentsServiceTests
{
    private readonly Mock<HttpMessageHandler> _httpHandlerMock;
    private readonly Mock<ILogger<PeachPaymentsService>> _loggerMock;
    private readonly IOptions<PaymentOptions> _options;
    private const string BaseUrl = "https://test.oppwa.com";

    // Captured request for verification
    private HttpRequestMessage? _capturedRequest;
    private string? _capturedContent;

    public PeachPaymentsServiceTests()
    {
        _httpHandlerMock = new Mock<HttpMessageHandler>();
        _loggerMock = new Mock<ILogger<PeachPaymentsService>>();

        var paymentOptions = new PaymentOptions
        {
            Provider = PaymentProvider.PeachPayments,
            TimeoutSeconds = 30,
            SuccessUrl = "https://example.com/success",
            PeachPayments = new PeachPaymentsOptions
            {
                BaseUrl = BaseUrl,
                EntityId = "test_entity_id",
                AccessToken = "test_access_token",
                WebhookSecret = "test_webhook_secret",
                TestMode = true,
                Use3DSecure = true
            }
        };
        _options = Options.Create(paymentOptions);
    }

    private PeachPaymentsService CreateService()
    {
        var httpClient = new HttpClient(_httpHandlerMock.Object);
        return new PeachPaymentsService(httpClient, _options, _loggerMock.Object);
    }

    private void SetupHttpResponse(HttpStatusCode statusCode, object? responseBody = null)
    {
        var response = new HttpResponseMessage(statusCode);
        if (responseBody != null)
        {
            response.Content = new StringContent(
                JsonSerializer.Serialize(responseBody),
                Encoding.UTF8,
                "application/json");
        }

        _httpHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>(async (req, _) =>
            {
                _capturedRequest = req;
                if (req.Content != null)
                {
                    _capturedContent = await req.Content.ReadAsStringAsync();
                }
            })
            .ReturnsAsync(response);
    }

    private void SetupHttpException(Exception exception)
    {
        _httpHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(exception);
    }

    #region CreateCheckoutAsync Tests

    [Fact]
    public async Task CreateCheckoutAsync_OnSuccess_ReturnsCreatedStatus()
    {
        // Arrange
        var request = new CheckoutRequest
        {
            AccountId = "acc-123",
            Amount = 99.99m,
            Currency = "ZAR",
            Email = "test@example.com",
            Type = CheckoutType.OneTime,
            ProductId = "product-123",
            Description = "Test checkout"
        };

        SetupHttpResponse(HttpStatusCode.OK, new
        {
            id = "checkout_12345",
            result = new { code = "000.200.100", description = "Success" }
        });

        var sut = CreateService();

        // Act
        var result = await sut.CreateCheckoutAsync(request);

        // Assert
        result.Status.Should().Be(CheckoutResultStatus.Created);
        result.CheckoutId.Should().Be("checkout_12345");
        result.RedirectUrl.Should().Contain("checkout_12345");
        result.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task CreateCheckoutAsync_OnApiError_ReturnsFailedStatus()
    {
        // Arrange
        var request = new CheckoutRequest
        {
            AccountId = "acc-123",
            Amount = 100m,
            Currency = "ZAR",
            Email = "test@example.com",
            ProductId = "product-123",
            Description = "Test checkout"
        };

        SetupHttpResponse(HttpStatusCode.BadRequest, new { error = "Invalid request" });

        var sut = CreateService();

        // Act
        var result = await sut.CreateCheckoutAsync(request);

        // Assert
        result.Status.Should().Be(CheckoutResultStatus.Failed);
        result.CheckoutId.Should().BeEmpty();
        result.ErrorMessage.Should().Contain("BadRequest");
    }

    [Fact]
    public async Task CreateCheckoutAsync_OnException_ReturnsFailedStatus()
    {
        // Arrange
        var request = new CheckoutRequest
        {
            AccountId = "acc-123",
            Amount = 100m,
            Currency = "ZAR",
            Email = "test@example.com",
            ProductId = "product-123",
            Description = "Test checkout"
        };

        SetupHttpException(new HttpRequestException("Network error"));

        var sut = CreateService();

        // Act
        var result = await sut.CreateCheckoutAsync(request);

        // Assert
        result.Status.Should().Be(CheckoutResultStatus.Failed);
        result.ErrorMessage.Should().Be("Failed to create payment checkout");
    }

    [Fact]
    public async Task CreateCheckoutAsync_ForSubscription_UsesPAPaymentType()
    {
        // Arrange
        var request = new CheckoutRequest
        {
            AccountId = "acc-123",
            Amount = 49.99m,
            Currency = "ZAR",
            Email = "test@example.com",
            Type = CheckoutType.Subscription,
            ProductId = "product-sub-123",
            Description = "Subscription checkout"
        };

        SetupHttpResponse(HttpStatusCode.OK, new { id = "checkout_sub_123" });

        var sut = CreateService();

        // Act
        await sut.CreateCheckoutAsync(request);

        // Assert
        _capturedContent.Should().NotBeNull();
        _capturedContent.Should().Contain("paymentType=PA");
    }

    [Fact]
    public async Task CreateCheckoutAsync_WithMetadata_IncludesCustomParameters()
    {
        // Arrange
        var request = new CheckoutRequest
        {
            AccountId = "acc-123",
            Amount = 100m,
            Currency = "ZAR",
            Email = "test@example.com",
            ProductId = "product-123",
            Description = "Checkout with metadata",
            Metadata = new Dictionary<string, string>
            {
                ["orderId"] = "order-456",
                ["userId"] = "user-789"
            }
        };

        SetupHttpResponse(HttpStatusCode.OK, new { id = "checkout_123" });

        var sut = CreateService();

        // Act
        await sut.CreateCheckoutAsync(request);

        // Assert
        _capturedContent.Should().NotBeNull();
        _capturedContent.Should().Contain("customParameters");
    }

    #endregion

    #region ProcessPaymentAsync Tests

    [Fact]
    public async Task ProcessPaymentAsync_OnSuccess_ReturnsSucceededStatus()
    {
        // Arrange
        var request = new PaymentRequest
        {
            AccountId = "acc-123",
            Amount = 150m,
            Currency = "ZAR",
            PaymentMethodToken = "tok_card_123"
        };

        SetupHttpResponse(HttpStatusCode.OK, new
        {
            id = "payment_12345",
            result = new { code = "000.100.110", description = "Request successfully processed" }
        });

        var sut = CreateService();

        // Act
        var result = await sut.ProcessPaymentAsync(request);

        // Assert
        result.Status.Should().Be(PaymentResultStatus.Succeeded);
        result.TransactionId.Should().Be("payment_12345");
        result.Amount.Should().Be(150m);
        result.Currency.Should().Be("ZAR");
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task ProcessPaymentAsync_OnDecline_ReturnsFailedStatus()
    {
        // Arrange
        var request = new PaymentRequest
        {
            AccountId = "acc-123",
            Amount = 100m,
            Currency = "ZAR",
            PaymentMethodToken = "tok_declined"
        };

        SetupHttpResponse(HttpStatusCode.OK, new
        {
            id = "payment_declined",
            result = new { code = "800.100.151", description = "Transaction declined" }
        });

        var sut = CreateService();

        // Act
        var result = await sut.ProcessPaymentAsync(request);

        // Assert
        result.Status.Should().Be(PaymentResultStatus.Failed);
        result.ErrorMessage.Should().Be("Transaction declined");
        result.ErrorCode.Should().Be("800.100.151");
    }

    [Fact]
    public async Task ProcessPaymentAsync_OnException_ReturnsFailedStatus()
    {
        // Arrange
        var request = new PaymentRequest
        {
            AccountId = "acc-123",
            Amount = 100m,
            Currency = "ZAR",
            PaymentMethodToken = "tok_123"
        };

        SetupHttpException(new HttpRequestException("Connection refused"));

        var sut = CreateService();

        // Act
        var result = await sut.ProcessPaymentAsync(request);

        // Assert
        result.Status.Should().Be(PaymentResultStatus.Failed);
        result.ErrorMessage.Should().Be("Payment processing failed");
    }

    #endregion

    #region VerifyWebhookAsync Tests

    [Fact]
    public async Task VerifyWebhookAsync_WithValidSignature_ReturnsWebhookEvent()
    {
        // Arrange
        var payload = JsonSerializer.Serialize(new
        {
            id = "evt_123",
            type = "DB",
            paymentId = "pay_456",
            amount = "100.00",
            currency = "ZAR",
            result = new { code = "000.100.110" },
            timestamp = DateTime.UtcNow
        });

        // Compute valid HMAC signature
        using var hmac = new System.Security.Cryptography.HMACSHA256(
            System.Text.Encoding.UTF8.GetBytes("test_webhook_secret"));
        var hash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(payload));
        var signature = Convert.ToHexString(hash).ToLowerInvariant();

        var sut = CreateService();

        // Act
        var result = await sut.VerifyWebhookAsync(payload, signature);

        // Assert
        result.EventId.Should().Be("evt_123");
        result.Type.Should().Be(WebhookEventType.PaymentSucceeded);
        result.TransactionId.Should().Be("pay_456");
        result.PaymentStatus.Should().Be(PaymentResultStatus.Succeeded);
    }

    [Fact]
    public async Task VerifyWebhookAsync_WithInvalidSignature_ThrowsException()
    {
        // Arrange
        var payload = "{\"id\": \"evt_123\"}";
        var invalidSignature = "invalid_signature";

        var sut = CreateService();

        // Act
        var act = () => sut.VerifyWebhookAsync(payload, invalidSignature);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Invalid webhook signature");
    }

    [Fact]
    public async Task VerifyWebhookAsync_ForRefund_MapsToRefundedEvent()
    {
        // Arrange
        var payload = JsonSerializer.Serialize(new
        {
            id = "evt_refund",
            type = "RF",
            paymentId = "pay_789",
            amount = "50.00",
            currency = "ZAR",
            result = new { code = "000.100.110" }
        });

        using var hmac = new System.Security.Cryptography.HMACSHA256(
            System.Text.Encoding.UTF8.GetBytes("test_webhook_secret"));
        var hash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(payload));
        var signature = Convert.ToHexString(hash).ToLowerInvariant();

        var sut = CreateService();

        // Act
        var result = await sut.VerifyWebhookAsync(payload, signature);

        // Assert
        result.Type.Should().Be(WebhookEventType.PaymentRefunded);
    }

    #endregion

    #region GetPaymentStatusAsync Tests

    [Fact]
    public async Task GetPaymentStatusAsync_ReturnsCorrectStatus()
    {
        // Arrange
        var transactionId = "pay_status_123";

        SetupHttpResponse(HttpStatusCode.OK, new
        {
            id = transactionId,
            amount = "200.00",
            currency = "ZAR",
            result = new { code = "000.100.110", description = "Success" },
            timestamp = DateTime.UtcNow
        });

        var sut = CreateService();

        // Act
        var result = await sut.GetPaymentStatusAsync(transactionId);

        // Assert
        result.TransactionId.Should().Be(transactionId);
        result.Status.Should().Be(PaymentResultStatus.Succeeded);
        result.Amount.Should().Be(200m);
        result.Currency.Should().Be("ZAR");
    }

    [Fact]
    public async Task GetPaymentStatusAsync_WithPendingCode_ReturnsPending()
    {
        // Arrange
        var transactionId = "pay_pending_123";

        SetupHttpResponse(HttpStatusCode.OK, new
        {
            id = transactionId,
            amount = "100.00",
            currency = "ZAR",
            result = new { code = "100.396.101", description = "Pending" }
        });

        var sut = CreateService();

        // Act
        var result = await sut.GetPaymentStatusAsync(transactionId);

        // Assert
        result.Status.Should().Be(PaymentResultStatus.Pending);
    }

    #endregion

    #region RefundPaymentAsync Tests

    [Fact]
    public async Task RefundPaymentAsync_OnSuccess_ReturnsSucceededStatus()
    {
        // Arrange
        var request = new RefundRequest
        {
            TransactionId = "pay_to_refund",
            Amount = 50m,
            Reason = "Customer requested refund"
        };

        SetupHttpResponse(HttpStatusCode.OK, new
        {
            id = "refund_123",
            result = new { code = "000.100.110", description = "Refund successful" }
        });

        var sut = CreateService();

        // Act
        var result = await sut.RefundPaymentAsync(request);

        // Assert
        result.Status.Should().Be(RefundStatus.Succeeded);
        result.RefundId.Should().Be("refund_123");
        result.TransactionId.Should().Be("pay_to_refund");
        result.Amount.Should().Be(50m);
    }

    [Fact]
    public async Task RefundPaymentAsync_OnFailure_ReturnsFailedStatus()
    {
        // Arrange
        var request = new RefundRequest
        {
            TransactionId = "pay_invalid",
            Amount = 100m
        };

        SetupHttpResponse(HttpStatusCode.OK, new
        {
            id = "",
            result = new { code = "700.400.200", description = "Cannot refund transaction" }
        });

        var sut = CreateService();

        // Act
        var result = await sut.RefundPaymentAsync(request);

        // Assert
        result.Status.Should().Be(RefundStatus.Failed);
        result.ErrorMessage.Should().Be("Cannot refund transaction");
    }

    [Fact]
    public async Task RefundPaymentAsync_OnException_ReturnsFailedStatus()
    {
        // Arrange
        var request = new RefundRequest
        {
            TransactionId = "pay_network_error",
            Amount = 100m
        };

        SetupHttpException(new HttpRequestException("Network error"));

        var sut = CreateService();

        // Act
        var result = await sut.RefundPaymentAsync(request);

        // Assert
        result.Status.Should().Be(RefundStatus.Failed);
        result.ErrorMessage.Should().Be("Refund processing failed");
    }

    #endregion

    #region Subscription Methods Tests

    [Fact]
    public async Task CreateSubscriptionAsync_ReturnsIncompleteStatus()
    {
        // Arrange
        var request = new SubscriptionRequest
        {
            AccountId = "acc-123",
            PlanId = "plan_monthly",
            Email = "test@example.com"
        };

        var sut = CreateService();

        // Act
        var result = await sut.CreateSubscriptionAsync(request);

        // Assert - Not yet implemented
        result.Status.Should().Be(SubscriptionResultStatus.Incomplete);
        result.ErrorMessage.Should().Contain("not yet implemented");
    }

    [Fact]
    public async Task CancelSubscriptionAsync_ReturnsFailure()
    {
        // Arrange
        var subscriptionId = "sub_123";

        var sut = CreateService();

        // Act
        var result = await sut.CancelSubscriptionAsync(subscriptionId);

        // Assert - Not yet implemented
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not yet implemented");
    }

    #endregion

    #region IsHealthyAsync Tests

    [Fact]
    public async Task IsHealthyAsync_WhenApiReachable_ReturnsTrue()
    {
        // Arrange
        SetupHttpResponse(HttpStatusCode.OK);

        var sut = CreateService();

        // Act
        var result = await sut.IsHealthyAsync();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsHealthyAsync_WhenApiReturnsNotFound_StillReturnsTrue()
    {
        // Arrange - NotFound is acceptable for health check
        SetupHttpResponse(HttpStatusCode.NotFound);

        var sut = CreateService();

        // Act
        var result = await sut.IsHealthyAsync();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsHealthyAsync_WhenApiUnreachable_ReturnsFalse()
    {
        // Arrange
        SetupHttpException(new HttpRequestException("Connection refused"));

        var sut = CreateService();

        // Act
        var result = await sut.IsHealthyAsync();

        // Assert
        result.Should().BeFalse();
    }

    #endregion
}
