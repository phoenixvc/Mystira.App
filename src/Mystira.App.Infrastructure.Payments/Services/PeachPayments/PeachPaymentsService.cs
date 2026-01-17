using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mystira.App.Application.Ports.Payments;
using Mystira.App.Infrastructure.Payments.Configuration;
using Mystira.Contracts.App.Enums;
using Mystira.Contracts.App.Requests.Payments;
using Mystira.Contracts.App.Responses.Payments;

namespace Mystira.App.Infrastructure.Payments.Services.PeachPayments;

/// <summary>
/// PeachPayments implementation of IPaymentService.
/// Integrates with PeachPayments API for card payments in South Africa.
/// </summary>
public class PeachPaymentsService : IPaymentService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PeachPaymentsService> _logger;
    private readonly PaymentOptions _options;
    private readonly PeachPaymentsOptions _peachOptions;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public PeachPaymentsService(
        HttpClient httpClient,
        IOptions<PaymentOptions> options,
        ILogger<PeachPaymentsService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _options = options.Value;
        _peachOptions = _options.PeachPayments;

        ConfigureHttpClient();
    }

    private void ConfigureHttpClient()
    {
        _httpClient.BaseAddress = new Uri(_peachOptions.BaseUrl);
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _peachOptions.AccessToken);
        _httpClient.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);
    }

    public async Task<CheckoutResult> CreateCheckoutAsync(CheckoutRequest request)
    {
        try
        {
            _logger.LogInformation(
                "Creating PeachPayments checkout for account {AccountId}, amount {Amount} {Currency}",
                request.AccountId, request.Amount, request.Currency);

            // Build checkout request parameters
            var formData = new Dictionary<string, string>
            {
                ["entityId"] = _peachOptions.EntityId,
                ["amount"] = request.Amount.ToString("F2"),
                ["currency"] = request.Currency,
                ["paymentType"] = request.Type == CheckoutType.Subscription ? "PA" : "DB", // PA=Pre-auth, DB=Debit
                ["merchantTransactionId"] = $"{request.AccountId}_{DateTime.UtcNow:yyyyMMddHHmmss}",
                ["customer.email"] = request.Email,
                ["shopperResultUrl"] = request.SuccessUrl ?? _options.SuccessUrl ?? string.Empty,
                ["testMode"] = _peachOptions.TestMode ? "EXTERNAL" : "LIVE"
            };

            // Add 3D Secure if enabled
            if (_peachOptions.Use3DSecure)
            {
                formData["threeDSecure.eci"] = "05";
            }

            // Add metadata
            if (request.Metadata != null)
            {
                foreach (var (key, value) in request.Metadata)
                {
                    formData[$"customParameters[{key}]"] = value;
                }
            }

            // Create the checkout session
            var content = new FormUrlEncodedContent(formData);

            var response = await _httpClient.PostAsync("/v1/checkouts", content);
            var responseBody = await response.Content.ReadAsStringAsync();

            _logger.LogDebug("PeachPayments checkout response: {Response}", responseBody);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("PeachPayments checkout failed: {StatusCode} - {Response}",
                    response.StatusCode, responseBody);

                return new CheckoutResult
                {
                    CheckoutId = string.Empty,
                    Status = CheckoutResultStatus.Failed,
                    ErrorMessage = $"Payment provider error: {response.StatusCode}"
                };
            }

            var result = JsonSerializer.Deserialize<PeachPaymentsCheckoutResponse>(responseBody, JsonOptions);

            return new CheckoutResult
            {
                CheckoutId = result?.Id ?? string.Empty,
                Status = CheckoutResultStatus.Created,
                RedirectUrl = BuildCheckoutUrl(result?.Id ?? string.Empty),
                ExpiresAt = DateTime.UtcNow.AddMinutes(30)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating PeachPayments checkout for account {AccountId}", request.AccountId);

            return new CheckoutResult
            {
                CheckoutId = string.Empty,
                Status = CheckoutResultStatus.Failed,
                ErrorMessage = "Failed to create payment checkout"
            };
        }
    }

    public async Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request)
    {
        try
        {
            _logger.LogInformation(
                "Processing PeachPayments payment for account {AccountId}, amount {Amount} {Currency}",
                request.AccountId, request.Amount, request.Currency);

            var formData = new Dictionary<string, string>
            {
                ["entityId"] = _peachOptions.EntityId,
                ["amount"] = request.Amount.ToString("F2"),
                ["currency"] = request.Currency,
                ["paymentType"] = "DB", // Debit
                ["paymentBrand"] = "VISA", // Will be determined from token
                ["card.token"] = request.PaymentMethodToken
            };

            var content = new FormUrlEncodedContent(formData);

            var response = await _httpClient.PostAsync("/v1/payments", content);
            var responseBody = await response.Content.ReadAsStringAsync();

            _logger.LogDebug("PeachPayments payment response: {Response}", responseBody);

            var result = JsonSerializer.Deserialize<PeachPaymentsPaymentResponse>(responseBody, JsonOptions);
            var isSuccess = IsSuccessfulResult(result?.Result?.Code);

            return new PaymentResult
            {
                TransactionId = result?.Id ?? string.Empty,
                Status = isSuccess ? PaymentResultStatus.Succeeded : PaymentResultStatus.Failed,
                Amount = request.Amount,
                Currency = request.Currency,
                ProcessedAt = DateTime.UtcNow,
                ErrorMessage = isSuccess ? null : result?.Result?.Description,
                ErrorCode = isSuccess ? null : result?.Result?.Code
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing PeachPayments payment for account {AccountId}", request.AccountId);

            return new PaymentResult
            {
                TransactionId = string.Empty,
                Status = PaymentResultStatus.Failed,
                Amount = request.Amount,
                Currency = request.Currency,
                ProcessedAt = DateTime.UtcNow,
                ErrorMessage = "Payment processing failed"
            };
        }
    }

    public Task<WebhookEvent> VerifyWebhookAsync(string payload, string signature)
    {
        try
        {
            // Verify webhook signature using HMAC-SHA256
            var computedSignature = ComputeHmacSha256(payload, _peachOptions.WebhookSecret);

            if (!string.Equals(computedSignature, signature, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Invalid PeachPayments webhook signature");
                throw new InvalidOperationException("Invalid webhook signature");
            }

            var webhookData = JsonSerializer.Deserialize<PeachPaymentsWebhook>(payload, JsonOptions);

            if (webhookData == null)
            {
                throw new InvalidOperationException("Failed to parse webhook payload");
            }

            var eventType = MapWebhookEventType(webhookData.Type);
            var paymentStatus = MapPaymentStatus(webhookData.Result?.Code);

            return Task.FromResult(new WebhookEvent
            {
                EventId = webhookData.Id ?? Guid.NewGuid().ToString(),
                Type = eventType,
                TransactionId = webhookData.PaymentId ?? string.Empty,
                Amount = decimal.TryParse(webhookData.Amount, out var amount) ? amount : null,
                Currency = webhookData.Currency,
                PaymentStatus = paymentStatus,
                Timestamp = webhookData.Timestamp ?? DateTime.UtcNow,
                RawData = JsonSerializer.Deserialize<Dictionary<string, object>>(payload)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying PeachPayments webhook");
            throw;
        }
    }

    public async Task<PaymentStatus> GetPaymentStatusAsync(string transactionId)
    {
        try
        {
            _logger.LogInformation("Getting payment status for transaction {TransactionId}", transactionId);

            var request = new HttpRequestMessage(HttpMethod.Get, $"/v1/payments/{transactionId}?entityId={_peachOptions.EntityId}");
            request.Headers.Add("Authorization", $"Bearer {_peachOptions.AccessToken}");

            var response = await _httpClient.SendAsync(request);
            var responseBody = await response.Content.ReadAsStringAsync();

            var result = JsonSerializer.Deserialize<PeachPaymentsPaymentResponse>(responseBody, JsonOptions);
            var status = MapPaymentStatus(result?.Result?.Code);

            return new PaymentStatus
            {
                TransactionId = transactionId,
                Status = status ?? PaymentResultStatus.Pending,
                Amount = decimal.TryParse(result?.Amount, out var amount) ? amount : 0,
                Currency = result?.Currency ?? "ZAR",
                CreatedAt = result?.Timestamp ?? DateTime.UtcNow,
                CompletedAt = status == PaymentResultStatus.Succeeded ? result?.Timestamp : null,
                FailureReason = status == PaymentResultStatus.Failed ? result?.Result?.Description : null
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payment status for transaction {TransactionId}", transactionId);
            throw;
        }
    }

    public async Task<RefundResult> RefundPaymentAsync(RefundRequest request)
    {
        try
        {
            _logger.LogInformation("Processing refund for transaction {TransactionId}", request.TransactionId);

            var formData = new Dictionary<string, string>
            {
                ["entityId"] = _peachOptions.EntityId,
                ["paymentType"] = "RF" // Refund
            };

            if (request.Amount.HasValue)
            {
                formData["amount"] = request.Amount.Value.ToString("F2");
            }

            var content = new FormUrlEncodedContent(formData);

            var response = await _httpClient.PostAsync($"/v1/payments/{request.TransactionId}", content);
            var responseBody = await response.Content.ReadAsStringAsync();

            var result = JsonSerializer.Deserialize<PeachPaymentsPaymentResponse>(responseBody, JsonOptions);
            var isSuccess = IsSuccessfulResult(result?.Result?.Code);

            return new RefundResult
            {
                RefundId = result?.Id ?? string.Empty,
                TransactionId = request.TransactionId,
                Status = isSuccess ? RefundStatus.Succeeded : RefundStatus.Failed,
                Amount = request.Amount ?? 0,
                ProcessedAt = DateTime.UtcNow,
                ErrorMessage = isSuccess ? null : result?.Result?.Description
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing refund for transaction {TransactionId}", request.TransactionId);

            return new RefundResult
            {
                RefundId = string.Empty,
                TransactionId = request.TransactionId,
                Status = RefundStatus.Failed,
                Amount = request.Amount ?? 0,
                ProcessedAt = DateTime.UtcNow,
                ErrorMessage = "Refund processing failed"
            };
        }
    }

    public Task<SubscriptionResult> CreateSubscriptionAsync(SubscriptionRequest request)
    {
        // PeachPayments uses recurring payments via COF (Card on File)
        // This would need to be implemented with recurring payment registration
        _logger.LogWarning("PeachPayments subscription creation not yet implemented - use checkout flow with COF");

        return Task.FromResult(new SubscriptionResult
        {
            SubscriptionId = string.Empty,
            Status = SubscriptionResultStatus.Incomplete,
            PlanId = request.PlanId,
            ErrorMessage = "Subscription creation via direct API not yet implemented. Use checkout flow."
        });
    }

    public Task<SubscriptionCancellationResult> CancelSubscriptionAsync(string subscriptionId, bool cancelImmediately = false)
    {
        // Would need to be implemented with recurring payment management
        _logger.LogWarning("PeachPayments subscription cancellation not yet implemented");

        return Task.FromResult(new SubscriptionCancellationResult
        {
            SubscriptionId = subscriptionId,
            Success = false,
            ErrorMessage = "Subscription cancellation not yet implemented"
        });
    }

    public Task<SubscriptionStatus> GetSubscriptionStatusAsync(string subscriptionId)
    {
        _logger.LogWarning("PeachPayments subscription status not yet implemented");

        return Task.FromResult(new SubscriptionStatus
        {
            SubscriptionId = subscriptionId,
            Status = SubscriptionResultStatus.Active,
            PlanId = "unknown",
            CurrentPeriodStart = DateTime.UtcNow,
            CurrentPeriodEnd = DateTime.UtcNow.AddMonths(1)
        });
    }

    public async Task<bool> IsHealthyAsync()
    {
        try
        {
            // Simple health check - verify we can reach the API
            var request = new HttpRequestMessage(HttpMethod.Get, "/v1/");
            request.Headers.Add("Authorization", $"Bearer {_peachOptions.AccessToken}");

            var response = await _httpClient.SendAsync(request);
            return response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.NotFound;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "PeachPayments health check failed");
            return false;
        }
    }

    #region Private Helpers

    private string BuildCheckoutUrl(string checkoutId)
    {
        // Build the widget script URL for PeachPayments hosted payment form
        return $"{_peachOptions.BaseUrl}/v1/paymentWidgets.js?checkoutId={checkoutId}";
    }

    private static bool IsSuccessfulResult(string? resultCode)
    {
        // PeachPayments result codes: 000.xxx.xxx are successful
        return resultCode != null && resultCode.StartsWith("000.");
    }

    private static string ComputeHmacSha256(string payload, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static WebhookEventType MapWebhookEventType(string? type)
    {
        return type?.ToUpperInvariant() switch
        {
            "PA" or "DB" => WebhookEventType.PaymentSucceeded,
            "RF" => WebhookEventType.PaymentRefunded,
            "RV" => WebhookEventType.PaymentFailed,
            _ => WebhookEventType.PaymentSucceeded
        };
    }

    private static PaymentResultStatus? MapPaymentStatus(string? resultCode)
    {
        if (string.IsNullOrEmpty(resultCode)) return null;

        return resultCode switch
        {
            _ when resultCode.StartsWith("000.") => PaymentResultStatus.Succeeded,
            _ when resultCode.StartsWith("100.") => PaymentResultStatus.Pending,
            _ => PaymentResultStatus.Failed
        };
    }

    #endregion
}

#region PeachPayments API Response Models

internal class PeachPaymentsCheckoutResponse
{
    public string? Id { get; set; }
    public PeachPaymentsResult? Result { get; set; }
}

internal class PeachPaymentsPaymentResponse
{
    public string? Id { get; set; }
    public string? PaymentType { get; set; }
    public string? PaymentBrand { get; set; }
    public string? Amount { get; set; }
    public string? Currency { get; set; }
    public PeachPaymentsResult? Result { get; set; }
    public DateTime? Timestamp { get; set; }
}

internal class PeachPaymentsResult
{
    public string? Code { get; set; }
    public string? Description { get; set; }
}

internal class PeachPaymentsWebhook
{
    public string? Id { get; set; }
    public string? Type { get; set; }
    public string? PaymentId { get; set; }
    public string? Amount { get; set; }
    public string? Currency { get; set; }
    public PeachPaymentsResult? Result { get; set; }
    public DateTime? Timestamp { get; set; }
}

#endregion
