namespace Mystira.App.Application.Ports.Payments;

/// <summary>
/// Port interface for payment gateway operations.
/// Abstracts payment provider implementation (PeachPayments, Stripe, etc.).
/// </summary>
public interface IPaymentService
{
    /// <summary>
    /// Initiates a checkout session for a subscription or one-time purchase.
    /// </summary>
    /// <param name="request">Checkout request details</param>
    /// <returns>Checkout result with redirect URL or payment form data</returns>
    Task<CheckoutResult> CreateCheckoutAsync(CheckoutRequest request);

    /// <summary>
    /// Processes a payment using a stored payment method or token.
    /// </summary>
    /// <param name="request">Payment request details</param>
    /// <returns>Payment result with transaction ID and status</returns>
    Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request);

    /// <summary>
    /// Verifies a payment webhook signature and parses the payload.
    /// </summary>
    /// <param name="payload">Raw webhook payload</param>
    /// <param name="signature">Webhook signature header</param>
    /// <returns>Verified webhook event</returns>
    Task<WebhookEvent> VerifyWebhookAsync(string payload, string signature);

    /// <summary>
    /// Gets the status of a payment transaction.
    /// </summary>
    /// <param name="transactionId">Payment transaction ID</param>
    /// <returns>Payment status details</returns>
    Task<PaymentStatus> GetPaymentStatusAsync(string transactionId);

    /// <summary>
    /// Refunds a payment partially or fully.
    /// </summary>
    /// <param name="request">Refund request details</param>
    /// <returns>Refund result</returns>
    Task<RefundResult> RefundPaymentAsync(RefundRequest request);

    /// <summary>
    /// Creates or updates a subscription for recurring payments.
    /// </summary>
    /// <param name="request">Subscription request details</param>
    /// <returns>Subscription result</returns>
    Task<SubscriptionResult> CreateSubscriptionAsync(SubscriptionRequest request);

    /// <summary>
    /// Cancels an active subscription.
    /// </summary>
    /// <param name="subscriptionId">Subscription ID to cancel</param>
    /// <param name="cancelImmediately">If true, cancels immediately; otherwise at end of billing period</param>
    /// <returns>Cancellation result</returns>
    Task<SubscriptionCancellationResult> CancelSubscriptionAsync(string subscriptionId, bool cancelImmediately = false);

    /// <summary>
    /// Gets subscription status and details.
    /// </summary>
    /// <param name="subscriptionId">Subscription ID</param>
    /// <returns>Subscription status</returns>
    Task<SubscriptionStatus> GetSubscriptionStatusAsync(string subscriptionId);

    /// <summary>
    /// Checks if the payment service is healthy and available.
    /// </summary>
    /// <returns>True if healthy, false otherwise</returns>
    Task<bool> IsHealthyAsync();
}

#region Request Models

/// <summary>
/// Request to create a checkout session
/// </summary>
public record CheckoutRequest
{
    public required string AccountId { get; init; }
    public required string Email { get; init; }
    public required string ProductId { get; init; }
    public required decimal Amount { get; init; }
    public required string Currency { get; init; }
    public required string Description { get; init; }
    public string? SuccessUrl { get; init; }
    public string? CancelUrl { get; init; }
    public string? WebhookUrl { get; init; }
    public Dictionary<string, string>? Metadata { get; init; }
    public CheckoutType Type { get; init; } = CheckoutType.OneTime;
}

/// <summary>
/// Request to process a direct payment
/// </summary>
public record PaymentRequest
{
    public required string AccountId { get; init; }
    public required decimal Amount { get; init; }
    public required string Currency { get; init; }
    public required string PaymentMethodToken { get; init; }
    public string? Description { get; init; }
    public Dictionary<string, string>? Metadata { get; init; }
}

/// <summary>
/// Request to refund a payment
/// </summary>
public record RefundRequest
{
    public required string TransactionId { get; init; }
    public decimal? Amount { get; init; } // null = full refund
    public string? Reason { get; init; }
}

/// <summary>
/// Request to create a subscription
/// </summary>
public record SubscriptionRequest
{
    public required string AccountId { get; init; }
    public required string Email { get; init; }
    public required string PlanId { get; init; }
    public string? PaymentMethodToken { get; init; }
    public DateTime? TrialEndDate { get; init; }
    public Dictionary<string, string>? Metadata { get; init; }
}

#endregion

#region Result Models

/// <summary>
/// Result of a checkout session creation
/// </summary>
public record CheckoutResult
{
    public required string CheckoutId { get; init; }
    public required CheckoutResultStatus Status { get; init; }
    public string? RedirectUrl { get; init; }
    public string? FormData { get; init; }
    public string? ErrorMessage { get; init; }
    public DateTime ExpiresAt { get; init; }
}

/// <summary>
/// Result of a payment transaction
/// </summary>
public record PaymentResult
{
    public required string TransactionId { get; init; }
    public required PaymentResultStatus Status { get; init; }
    public required decimal Amount { get; init; }
    public required string Currency { get; init; }
    public string? ErrorMessage { get; init; }
    public string? ErrorCode { get; init; }
    public DateTime ProcessedAt { get; init; }
    public Dictionary<string, string>? Metadata { get; init; }
}

/// <summary>
/// Payment status details
/// </summary>
public record PaymentStatus
{
    public required string TransactionId { get; init; }
    public required PaymentResultStatus Status { get; init; }
    public required decimal Amount { get; init; }
    public required string Currency { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
    public string? FailureReason { get; init; }
}

/// <summary>
/// Result of a refund operation
/// </summary>
public record RefundResult
{
    public required string RefundId { get; init; }
    public required string TransactionId { get; init; }
    public required RefundStatus Status { get; init; }
    public required decimal Amount { get; init; }
    public string? ErrorMessage { get; init; }
    public DateTime ProcessedAt { get; init; }
}

/// <summary>
/// Result of subscription creation
/// </summary>
public record SubscriptionResult
{
    public required string SubscriptionId { get; init; }
    public required SubscriptionResultStatus Status { get; init; }
    public required string PlanId { get; init; }
    public DateTime? CurrentPeriodStart { get; init; }
    public DateTime? CurrentPeriodEnd { get; init; }
    public DateTime? TrialEnd { get; init; }
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Subscription status details
/// </summary>
public record SubscriptionStatus
{
    public required string SubscriptionId { get; init; }
    public required SubscriptionResultStatus Status { get; init; }
    public required string PlanId { get; init; }
    public DateTime CurrentPeriodStart { get; init; }
    public DateTime CurrentPeriodEnd { get; init; }
    public DateTime? CancelledAt { get; init; }
    public DateTime? CancelAt { get; init; }
    public bool CancelAtPeriodEnd { get; init; }
}

/// <summary>
/// Result of subscription cancellation
/// </summary>
public record SubscriptionCancellationResult
{
    public required string SubscriptionId { get; init; }
    public required bool Success { get; init; }
    public DateTime? EffectiveDate { get; init; }
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Webhook event from payment provider
/// </summary>
public record WebhookEvent
{
    public required string EventId { get; init; }
    public required WebhookEventType Type { get; init; }
    public required string TransactionId { get; init; }
    public string? SubscriptionId { get; init; }
    public string? AccountId { get; init; }
    public decimal? Amount { get; init; }
    public string? Currency { get; init; }
    public PaymentResultStatus? PaymentStatus { get; init; }
    public DateTime Timestamp { get; init; }
    public Dictionary<string, object>? RawData { get; init; }
}

#endregion

#region Enums

public enum CheckoutType
{
    OneTime,
    Subscription
}

public enum CheckoutResultStatus
{
    Created,
    Pending,
    Failed
}

public enum PaymentResultStatus
{
    Pending,
    Processing,
    Succeeded,
    Failed,
    Cancelled,
    Refunded,
    PartiallyRefunded
}

public enum RefundStatus
{
    Pending,
    Succeeded,
    Failed
}

public enum SubscriptionResultStatus
{
    Active,
    Trialing,
    PastDue,
    Cancelled,
    Unpaid,
    Incomplete
}

public enum WebhookEventType
{
    PaymentSucceeded,
    PaymentFailed,
    PaymentRefunded,
    SubscriptionCreated,
    SubscriptionUpdated,
    SubscriptionCancelled,
    SubscriptionRenewed,
    CheckoutCompleted,
    CheckoutExpired
}

#endregion
