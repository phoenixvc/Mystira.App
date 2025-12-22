# Entra External ID Troubleshooting & FAQ

**Date**: December 22, 2025  
**Author**: Manus AI

---

## 1. Common Issues

### 1.1. Google Error: `redirect_uri_mismatch`

**Error Message**: `Error 400: redirect_uri_mismatch`

**Cause**: The redirect URI that Entra External ID is sending to Google is not registered in your Google Cloud Console project.

**Solution**:

1.  Go to the [Google Cloud Console Credentials page](https://console.cloud.google.com/apis/credentials).
2.  Select your OAuth 2.0 Client ID.
3.  In the **Authorized redirect URIs** section, add all 7 of the following URIs:

    ```
    https://login.microsoftonline.com
    https://login.microsoftonline.com/te/a816d461-fbf8-4477-83a6-a62ad74ff28f/oauth2/authresp
    https://login.microsoftonline.com/te/mystira.onmicrosoft.com/oauth2/authresp
    https://a816d461-fbf8-4477-83a6-a62ad74ff28f.ciamlogin.com/a816d461-fbf8-4477-83a6-a62ad74ff28f/federation/oidc/accounts.google.com
    https://a816d461-fbf8-4477-83a6-a62ad74ff28f.ciamlogin.com/mystira.onmicrosoft.com/federation/oidc/accounts.google.com
    https://mystira.ciamlogin.com/a816d461-fbf8-4477-83a6-a62ad74ff28f/federation/oauth2
    https://mystira.ciamlogin.com/mystira.onmicrosoft.com/federation/oauth2
    ```

### 1.2. Entra Sign-in Page Still Shows with `domain_hint`

**Issue**: After implementing `domain_hint=google.com`, the Entra External ID sign-in page is still displayed instead of redirecting directly to Google.

**Cause**: This is a known limitation of Microsoft Entra External ID (CIAM). The `domain_hint` parameter is not always honored, especially in customer-facing scenarios.

**Solution**: This is the expected behavior with the current version of Entra External ID. The user will need to click the "Sign in with Google" button on the Entra page. For a true direct-to-provider experience, a custom implementation using the Google Sign-In JavaScript library would be required.

### 1.3. 404 Error on Redirect

**Issue**: A 404 Not Found error occurs when redirecting to Entra External ID.

**Cause**: The `Authority` URL in `appsettings.json` is likely incorrect.

**Solution**:

Ensure that the `Authority` URL does **not** contain a `/v2.0` suffix. It should be in the format `https://<tenant_name>.ciamlogin.com/<tenant_id>`.

**Correct**:
```json
"Authority": "https://mystira.ciamlogin.com/a816d461-fbf8-4477-83a6-a62ad74ff28f"
```

**Incorrect**:
```json
"Authority": "https://mystira.ciamlogin.com/a816d461-fbf8-4477-83a6-a62ad74ff28f/v2.0"
```

### 1.4. API Returns 401 Unauthorized

**Issue**: The backend API returns a 401 Unauthorized error, even after a successful login.

**Cause**: The API is not correctly validating the JWT.

**Solution**:

1.  **Check the Audience**: Ensure that the `Audience` in the API's `appsettings.json` matches the **Public API Client ID** from your Terraform output.

    ```json
    "JwtSettings": {
      "Audience": "<YOUR_PUBLIC_API_CLIENT_ID>"
    }
    ```

2.  **Check the Issuer**: The `Issuer` should have a `/v2.0` suffix.

    ```json
    "JwtSettings": {
      "Issuer": "https://mystira.ciamlogin.com/a816d461-fbf8-4477-83a6-a62ad74ff28f/v2.0"
    }
    ```

3.  **Check the JWKS Endpoint**: Ensure the `JwksEndpoint` is correct.

    ```json
    "JwtSettings": {
      "JwksEndpoint": "https://mystira.ciamlogin.com/a816d461-fbf8-4477-83a6-a62ad74ff28f/discovery/v2.0/keys"
    }
    ```

---

## 2. Debugging the Authentication Flow

### 2.1. Browser Developer Tools

-   **Network Tab**: Use the Network tab (F12) to inspect the redirect URLs. Check for the presence of `domain_hint`, `state`, and `nonce` parameters. Verify that the `redirect_uri` is correct.
-   **Console Tab**: Look for any JavaScript errors related to authentication.
-   **Application Tab**: Check `localStorage` to see if the `mystira_entra_token` and `mystira_entra_account` are being set after a successful login.

### 2.2. Decoding JWTs

Use a tool like [jwt.ms](https://jwt.ms) to decode the ID token and access token. This allows you to inspect the claims and verify that the `iss` (issuer), `aud` (audience), and `exp` (expiry) are correct.

---

## 3. Frequently Asked Questions (FAQ)

**Q: Why are we using Entra External ID instead of Azure AD B2C?**

A: Azure AD B2C is being deprecated for new tenants as of May 1, 2025. Microsoft Entra External ID is the next-generation customer identity and access management (CIAM) solution.

**Q: Can we implement a true popup login instead of a redirect?**

A: Yes, but it requires significant changes. The current `IAuthService` interface is not designed for the asynchronous nature of popup authentication. This is documented as technical debt and can be addressed in a future iteration by using the `Microsoft.Authentication.WebAssembly.Msal` library with `LoginMode = "popup"`.

**Q: How do we add other social logins, like Facebook or Discord?**

A: You can add other identity providers in the Entra admin center under **External Identities** > **All identity providers**. Each provider will have its own setup process for creating an application and obtaining a client ID and secret.

**Q: What is the difference between the ID token and the access token?**

-   **ID Token**: Proves the identity of the user. It is for the PWA (client) to use.
-   **Access Token**: Grants access to a protected resource (the API). It is for the backend API to validate.
