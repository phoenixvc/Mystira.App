namespace Mystira.App.Infrastructure.Payments.Configuration;

/// <summary>
/// Configuration options for payment services.
/// Supports multiple providers with configuration-driven selection.
/// </summary>
public class PaymentOptions
{
    public const string SectionName = "Payments";

    /// <summary>
    /// Whether payment services are enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Use mock implementation for testing/development.
    /// </summary>
    public bool UseMockImplementation { get; set; } = true;

    /// <summary>
    /// Active payment provider when not using mock.
    /// </summary>
    public PaymentProvider Provider { get; set; } = PaymentProvider.PeachPayments;

    /// <summary>
    /// Default currency for payments.
    /// </summary>
    public string DefaultCurrency { get; set; } = "ZAR";

    /// <summary>
    /// Webhook base URL for payment callbacks.
    /// </summary>
    public string? WebhookBaseUrl { get; set; }

    /// <summary>
    /// Success redirect URL after payment.
    /// </summary>
    public string? SuccessUrl { get; set; }

    /// <summary>
    /// Cancel redirect URL if payment is cancelled.
    /// </summary>
    public string? CancelUrl { get; set; }

    /// <summary>
    /// PeachPayments specific configuration.
    /// </summary>
    public PeachPaymentsOptions PeachPayments { get; set; } = new();

    /// <summary>
    /// Retry configuration for transient failures.
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;
    public int RetryBaseDelayMs { get; set; } = 1000;
    public int TimeoutSeconds { get; set; } = 30;
}

/// <summary>
/// Supported payment providers.
/// </summary>
public enum PaymentProvider
{
    PeachPayments,
    Stripe,
    PayFast
}

/// <summary>
/// PeachPayments specific configuration.
/// </summary>
public class PeachPaymentsOptions
{
    /// <summary>
    /// PeachPayments API entity ID (merchant ID).
    /// </summary>
    public string EntityId { get; set; } = string.Empty;

    /// <summary>
    /// PeachPayments API access token/secret.
    /// </summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// PeachPayments webhook secret for signature verification.
    /// </summary>
    public string WebhookSecret { get; set; } = string.Empty;

    /// <summary>
    /// API base URL (sandbox vs production).
    /// </summary>
    public string BaseUrl { get; set; } = "https://eu-test.oppwa.com"; // Sandbox default

    /// <summary>
    /// Use 3D Secure for card payments.
    /// </summary>
    public bool Use3DSecure { get; set; } = true;

    /// <summary>
    /// Payment brands to accept (e.g., VISA, MASTERCARD).
    /// </summary>
    public List<string> PaymentBrands { get; set; } = new() { "VISA", "MASTERCARD" };

    /// <summary>
    /// Test mode flag (affects transaction processing).
    /// </summary>
    public bool TestMode { get; set; } = true;
}
