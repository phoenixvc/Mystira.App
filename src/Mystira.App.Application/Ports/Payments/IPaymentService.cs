using Mystira.Contracts.App.Enums;
using Mystira.Contracts.App.Requests.Payments;
using Mystira.Contracts.App.Responses.Payments;

namespace Mystira.App.Application.Ports.Payments;

/// <summary>
/// Port interface for payment gateway operations.
/// Abstracts payment provider implementation (PeachPayments, Stripe, etc.).
/// Types are defined in Mystira.Contracts for cross-project sharing.
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
