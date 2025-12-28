using Microsoft.Extensions.Logging;
using Moq;
using Mystira.App.Infrastructure.Payments.Services.Mock;
using Mystira.Contracts.App.Enums;
using Mystira.Contracts.App.Requests.Payments;

namespace Mystira.App.Infrastructure.Payments.Tests.Services;

public class MockPaymentServiceTests
{
    private readonly MockPaymentService _sut;
    private readonly Mock<ILogger<MockPaymentService>> _loggerMock;

    public MockPaymentServiceTests()
    {
        _loggerMock = new Mock<ILogger<MockPaymentService>>();
        _sut = new MockPaymentService(_loggerMock.Object);
    }

    #region CreateCheckoutAsync Tests

    [Fact]
    public async Task CreateCheckoutAsync_ReturnsCreatedStatus()
    {
        // Arrange
        var request = new CheckoutRequest
        {
            AccountId = "acc-123",
            Amount = 99.99m,
            Currency = "ZAR",
            Email = "test@example.com",
            ProductId = "product-123",
            Description = "Test checkout"
        };

        // Act
        var result = await _sut.CreateCheckoutAsync(request);

        // Assert
        result.Status.Should().Be(CheckoutResultStatus.Created);
        result.CheckoutId.Should().StartWith("mock_checkout_");
        result.RedirectUrl.Should().Contain(result.CheckoutId);
        result.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task CreateCheckoutAsync_StoresTransactionAsPending()
    {
        // Arrange
        var request = new CheckoutRequest
        {
            AccountId = "acc-123",
            Amount = 50m,
            Currency = "ZAR",
            Email = "test@example.com",
            ProductId = "product-123",
            Description = "Test checkout"
        };

        // Act
        var checkoutResult = await _sut.CreateCheckoutAsync(request);
        var status = await _sut.GetPaymentStatusAsync(checkoutResult.CheckoutId);

        // Assert
        status.Status.Should().Be(PaymentResultStatus.Pending);
        status.Amount.Should().Be(50m);
        status.Currency.Should().Be("ZAR");
    }

    #endregion

    #region ProcessPaymentAsync Tests

    [Fact]
    public async Task ProcessPaymentAsync_ReturnsTransactionId()
    {
        // Arrange
        var request = new PaymentRequest
        {
            AccountId = "acc-123",
            Amount = 100m,
            Currency = "ZAR",
            PaymentMethodToken = "tok_123"
        };

        // Act
        var result = await _sut.ProcessPaymentAsync(request);

        // Assert
        result.TransactionId.Should().StartWith("mock_txn_");
        result.Amount.Should().Be(100m);
        result.Currency.Should().Be("ZAR");
        result.ProcessedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task ProcessPaymentAsync_StoresTransaction()
    {
        // Arrange
        var request = new PaymentRequest
        {
            AccountId = "acc-123",
            Amount = 75m,
            Currency = "USD",
            PaymentMethodToken = "tok_456"
        };

        // Act
        var paymentResult = await _sut.ProcessPaymentAsync(request);
        var status = await _sut.GetPaymentStatusAsync(paymentResult.TransactionId);

        // Assert
        status.TransactionId.Should().Be(paymentResult.TransactionId);
        status.Amount.Should().Be(75m);
        status.Currency.Should().Be("USD");
    }

    #endregion

    #region VerifyWebhookAsync Tests

    [Fact]
    public async Task VerifyWebhookAsync_ReturnsSuccessfulPaymentEvent()
    {
        // Arrange
        var payload = "{}";
        var signature = "test_signature";

        // Act
        var result = await _sut.VerifyWebhookAsync(payload, signature);

        // Assert
        result.EventId.Should().StartWith("mock_evt_");
        result.Type.Should().Be(WebhookEventType.PaymentSucceeded);
        result.PaymentStatus.Should().Be(PaymentResultStatus.Succeeded);
        result.Currency.Should().Be("ZAR");
    }

    #endregion

    #region GetPaymentStatusAsync Tests

    [Fact]
    public async Task GetPaymentStatusAsync_WhenTransactionNotFound_ReturnsDefaultStatus()
    {
        // Arrange
        var unknownId = "unknown_txn_123";

        // Act
        var result = await _sut.GetPaymentStatusAsync(unknownId);

        // Assert
        result.TransactionId.Should().Be(unknownId);
        result.Status.Should().Be(PaymentResultStatus.Succeeded);
    }

    #endregion

    #region RefundPaymentAsync Tests

    [Fact]
    public async Task RefundPaymentAsync_ReturnsSuccessfulRefund()
    {
        // Arrange
        var request = new RefundRequest
        {
            TransactionId = "txn_123",
            Amount = 50m,
            Reason = "Customer request"
        };

        // Act
        var result = await _sut.RefundPaymentAsync(request);

        // Assert
        result.RefundId.Should().StartWith("mock_refund_");
        result.TransactionId.Should().Be("txn_123");
        result.Status.Should().Be(RefundStatus.Succeeded);
        result.Amount.Should().Be(50m);
    }

    [Fact]
    public async Task RefundPaymentAsync_FullRefund_MarksTransactionAsRefunded()
    {
        // Arrange
        var paymentRequest = new PaymentRequest
        {
            AccountId = "acc-123",
            Amount = 100m,
            Currency = "ZAR",
            PaymentMethodToken = "tok_123"
        };
        var paymentResult = await _sut.ProcessPaymentAsync(paymentRequest);

        var refundRequest = new RefundRequest
        {
            TransactionId = paymentResult.TransactionId,
            Amount = null // Full refund
        };

        // Act
        await _sut.RefundPaymentAsync(refundRequest);
        var status = await _sut.GetPaymentStatusAsync(paymentResult.TransactionId);

        // Assert
        status.Status.Should().Be(PaymentResultStatus.Refunded);
    }

    [Fact]
    public async Task RefundPaymentAsync_PartialRefund_MarksTransactionAsPartiallyRefunded()
    {
        // Arrange
        var paymentRequest = new PaymentRequest
        {
            AccountId = "acc-123",
            Amount = 100m,
            Currency = "ZAR",
            PaymentMethodToken = "tok_123"
        };
        var paymentResult = await _sut.ProcessPaymentAsync(paymentRequest);

        var refundRequest = new RefundRequest
        {
            TransactionId = paymentResult.TransactionId,
            Amount = 25m // Partial refund
        };

        // Act
        await _sut.RefundPaymentAsync(refundRequest);
        var status = await _sut.GetPaymentStatusAsync(paymentResult.TransactionId);

        // Assert
        status.Status.Should().Be(PaymentResultStatus.PartiallyRefunded);
    }

    #endregion

    #region Subscription Tests

    [Fact]
    public async Task CreateSubscriptionAsync_ReturnsActiveSubscription()
    {
        // Arrange
        var request = new SubscriptionRequest
        {
            AccountId = "acc-123",
            PlanId = "plan_monthly",
            Email = "test@example.com"
        };

        // Act
        var result = await _sut.CreateSubscriptionAsync(request);

        // Assert
        result.SubscriptionId.Should().StartWith("mock_sub_");
        result.Status.Should().Be(SubscriptionResultStatus.Active);
        result.PlanId.Should().Be("plan_monthly");
        result.CurrentPeriodEnd.Should().BeAfter(result.CurrentPeriodStart);
    }

    [Fact]
    public async Task CreateSubscriptionAsync_WithTrial_ReturnsTrialingStatus()
    {
        // Arrange
        var request = new SubscriptionRequest
        {
            AccountId = "acc-123",
            PlanId = "plan_monthly",
            Email = "test@example.com",
            TrialEndDate = DateTime.UtcNow.AddDays(14)
        };

        // Act
        var result = await _sut.CreateSubscriptionAsync(request);

        // Assert
        result.Status.Should().Be(SubscriptionResultStatus.Trialing);
        result.TrialEnd.Should().NotBeNull();
    }

    [Fact]
    public async Task CancelSubscriptionAsync_ReturnsSuccess()
    {
        // Arrange
        var subscriptionId = "sub_123";

        // Act
        var result = await _sut.CancelSubscriptionAsync(subscriptionId, cancelImmediately: false);

        // Assert
        result.Success.Should().BeTrue();
        result.SubscriptionId.Should().Be(subscriptionId);
        result.EffectiveDate.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task CancelSubscriptionAsync_Immediately_SetsEffectiveDateToNow()
    {
        // Arrange
        var subscriptionId = "sub_123";

        // Act
        var result = await _sut.CancelSubscriptionAsync(subscriptionId, cancelImmediately: true);

        // Assert
        result.Success.Should().BeTrue();
        result.EffectiveDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task GetSubscriptionStatusAsync_WhenNotFound_ReturnsDefaultActiveStatus()
    {
        // Arrange
        var unknownId = "unknown_sub_123";

        // Act
        var result = await _sut.GetSubscriptionStatusAsync(unknownId);

        // Assert
        result.SubscriptionId.Should().Be(unknownId);
        result.Status.Should().Be(SubscriptionResultStatus.Active);
        result.PlanId.Should().Be("mock_plan");
    }

    #endregion

    #region Health Check Tests

    [Fact]
    public async Task IsHealthyAsync_AlwaysReturnsTrue()
    {
        // Act
        var result = await _sut.IsHealthyAsync();

        // Assert
        result.Should().BeTrue();
    }

    #endregion
}
