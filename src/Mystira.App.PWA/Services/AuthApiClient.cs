using System.Net.Http.Json;
using Mystira.App.PWA.Models;

namespace Mystira.App.PWA.Services;

/// <summary>
/// API client for authentication-related operations
/// </summary>
public class AuthApiClient : BaseApiClient, IAuthApiClient
{
    public AuthApiClient(HttpClient httpClient, ILogger<AuthApiClient> logger, ITokenProvider tokenProvider)
        : base(httpClient, logger, tokenProvider)
    {
    }

    public async Task<PasswordlessSignupResponse?> RequestPasswordlessSignupAsync(string email, string displayName)
    {
        try
        {
            Logger.LogInformation("Requesting passwordless signup for email: {Email}", email);

            var request = new { email, displayName };
            var response = await HttpClient.PostAsJsonAsync("api/auth/passwordless/signup", request, JsonOptions);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<PasswordlessSignupResponse>(JsonOptions);
                Logger.LogInformation("Passwordless signup request successful for: {Email}", email);
                return result;
            }
            else
            {
                Logger.LogWarning("Passwordless signup request failed with status: {StatusCode} for email: {Email}",
                    response.StatusCode, email);

                // Try to read error response from the API
                try
                {
                    var errorResult = await response.Content.ReadFromJsonAsync<PasswordlessSignupResponse>(JsonOptions);
                    if (errorResult != null)
                    {
                        return errorResult;
                    }
                }
                catch (System.Text.Json.JsonException jsonEx)
                {
                    Logger.LogWarning(jsonEx, "Failed to parse error response for passwordless signup for email: {Email}", email);
                    // If we can't parse the error response, return a generic error
                }

                return new PasswordlessSignupResponse
                {
                    Success = false,
                    Message = $"Request failed with status {(int)response.StatusCode}. Please try again."
                };
            }
        }
        catch (HttpRequestException ex)
        {
            Logger.LogError(ex, "Network error requesting passwordless signup for email: {Email}", email);
            return new PasswordlessSignupResponse
            {
                Success = false,
                Message = "Unable to connect to the server. Please check your internet connection and try again."
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error requesting passwordless signup for email: {Email}", email);
            return new PasswordlessSignupResponse
            {
                Success = false,
                Message = "An unexpected error occurred. Please try again."
            };
        }
    }

    public async Task<PasswordlessVerifyResponse?> VerifyPasswordlessSignupAsync(string email, string code)
    {
        try
        {
            Logger.LogInformation("Verifying passwordless signup for email: {Email}", email);

            var request = new { email, code };
            var response = await HttpClient.PostAsJsonAsync("api/auth/passwordless/verify", request, JsonOptions);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<PasswordlessVerifyResponse>(JsonOptions);
                Logger.LogInformation("Passwordless signup verification successful for: {Email}", email);
                return result;
            }
            else
            {
                Logger.LogWarning("Passwordless signup verification failed with status: {StatusCode} for email: {Email}",
                    response.StatusCode, email);
                return null;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error verifying passwordless signup for email: {Email}", email);
            return null;
        }
    }

    public async Task<PasswordlessSigninResponse?> RequestPasswordlessSigninAsync(string email)
    {
        try
        {
            Logger.LogInformation("Requesting passwordless signin for email: {Email}", email);

            var request = new { email };
            var response = await HttpClient.PostAsJsonAsync("api/auth/passwordless/signin", request, JsonOptions);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<PasswordlessSigninResponse>(JsonOptions);
                Logger.LogInformation("Passwordless signin request successful for: {Email}", email);
                return result;
            }
            else
            {
                Logger.LogWarning("Passwordless signin request failed with status: {StatusCode} for email: {Email}",
                    response.StatusCode, email);

                // Try to read error response from the API
                try
                {
                    var errorResult = await response.Content.ReadFromJsonAsync<PasswordlessSigninResponse>(JsonOptions);
                    if (errorResult != null)
                    {
                        return errorResult;
                    }
                }
                catch (System.Text.Json.JsonException jsonEx)
                {
                    Logger.LogWarning(jsonEx, "Failed to parse error response for passwordless signin for email: {Email}", email);
                    // If we can't parse the error response, return a generic error
                }

                return new PasswordlessSigninResponse
                {
                    Success = false,
                    Message = $"Request failed with status {(int)response.StatusCode}. Please try again."
                };
            }
        }
        catch (HttpRequestException ex)
        {
            Logger.LogError(ex, "Network error requesting passwordless signin for email: {Email}", email);
            return new PasswordlessSigninResponse
            {
                Success = false,
                Message = "Unable to connect to the server. Please check your internet connection and try again."
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error requesting passwordless signin for email: {Email}", email);
            return new PasswordlessSigninResponse
            {
                Success = false,
                Message = "An unexpected error occurred. Please try again."
            };
        }
    }

    public async Task<PasswordlessVerifyResponse?> VerifyPasswordlessSigninAsync(string email, string code)
    {
        try
        {
            Logger.LogInformation("Verifying passwordless signin for email: {Email}", email);

            var request = new { email, code };
            var response = await HttpClient.PostAsJsonAsync("api/auth/passwordless/signin/verify", request, JsonOptions);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<PasswordlessVerifyResponse>(JsonOptions);
                Logger.LogInformation("Passwordless signin verification successful for: {Email}", email);
                return result;
            }
            else
            {
                Logger.LogWarning("Passwordless signin verification failed with status: {StatusCode} for email: {Email}",
                    response.StatusCode, email);
                return null;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error verifying passwordless signin for email: {Email}", email);
            return null;
        }
    }

    public async Task<RefreshTokenResponse?> RefreshTokenAsync(string token, string refreshToken)
    {
        try
        {
            Logger.LogInformation("Refreshing token");

            var request = new { token, refreshToken };
            var response = await HttpClient.PostAsJsonAsync("api/auth/refresh", request, JsonOptions);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<RefreshTokenResponse>(JsonOptions);
                Logger.LogInformation("Token refresh successful");
                return result;
            }
            else
            {
                Logger.LogWarning("Token refresh failed with status: {StatusCode}", response.StatusCode);
                return null;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error refreshing token");
            return null;
        }
    }

    public async Task<Account?> GetAccountByEmailAsync(string email)
    {
        try
        {
            Logger.LogInformation("Fetching account for email: {Email}", email);

            var encodedEmail = Uri.EscapeDataString(email);
            var response = await HttpClient.GetAsync($"api/accounts/email/{encodedEmail}");

            if (response.IsSuccessStatusCode)
            {
                var account = await response.Content.ReadFromJsonAsync<Account>(JsonOptions);
                Logger.LogInformation("Successfully fetched account for email: {Email}", email);
                return account;
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                Logger.LogWarning("Account not found for email: {Email}", email);
                return null;
            }
            else
            {
                Logger.LogWarning("API request failed with status: {StatusCode} for email: {Email}",
                    response.StatusCode, email);
                return null;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error fetching account for email: {Email}", email);
            return null;
        }
    }
}

