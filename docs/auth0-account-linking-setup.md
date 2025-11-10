# Auth0 Post-Login Action Setup Guide

This guide explains how to set up the account linking Post-Login Action for the Mystira PWA.

## Overview

The Post-Login Action automatically links user accounts that share the same verified email address. This prevents duplicate accounts when users sign in with different methods (e.g., email magic link + Google + Apple).

## Setup Instructions

### 1. Navigate to Auth0 Actions

1. Go to your [Auth0 Dashboard](https://manage.auth0.com/)
2. Navigate to **Actions** → **Library**
3. Click **Build Custom**

### 2. Create the Action

1. **Name**: `Account Linking`
2. **Trigger**: `Post Login`
3. **Runtime**: `Node 18`
4. Click **Create**

### 3. Add the Code

1. Copy the contents from `docs/auth0-post-login-action.js`
2. Paste it into the Action editor
3. Review and adjust the configuration constants at the top:

```javascript
// Configuration - adjust these values as needed
const ENABLE_AUTO_LINKING = true; // Set to false to disable auto-linking
const PRESERVE_PRIMARY_PROFILE = true; // Keep primary account's profile data
const LOG_LINKING_ATTEMPTS = true; // Log for security monitoring
```

### 4. Add to Flow

1. Click **Add to flow**
2. Select **Post Login**
3. Drag the Action into the flow
4. Position it **before** any other Actions
5. Click **Apply**

### 5. Deploy and Test

1. Click **Deploy** to deploy the Action
2. Test with different login methods using the same email:
   - Sign up with email magic link
   - Sign out
   - Sign in with Google using the same email
   - Verify accounts are linked automatically

## Configuration Options

### Auto-Linking Control

```javascript
const ENABLE_AUTO_LINKING = true; // Enable/disable automatic linking
```

- `true`: Automatically link accounts with verified emails
- `false`: Skip linking (useful for testing)

### Primary Account Selection

```javascript
const PRESERVE_PRIMARY_PROFILE = true; // Profile preservation logic
```

- `true`: Keep most recently created account as primary
- `false`: Use custom logic (modify `determinePrimaryAccount` function)

### Logging

```javascript
const LOG_LINKING_ATTEMPTS = true; // Security logging
```

- `true`: Log all linking attempts to Auth0 logs
- `false`: Minimal logging

## Customization

### Primary Account Logic

Modify the `determinePrimaryAccount` function to implement custom logic:

```javascript
function determinePrimaryAccount(currentUser, otherUser) {
  // Example: prefer certain connection types
  const preferredOrder = ['email', 'google-oauth2', 'apple', 'windowslive'];
  
  const currentUserConnection = currentUser.user_id.split('|')[0];
  const otherUserConnection = otherUser.user_id.split('|')[0];
  
  const currentUserPriority = preferredOrder.indexOf(currentUserConnection);
  const otherUserPriority = preferredOrder.indexOf(otherUserConnection);
  
  // Prefer connection with higher priority (lower index)
  if (currentUserPriority !== otherUserPriority) {
    return currentUserPriority < otherUserPriority;
  }
  
  // If same priority, use creation date
  return new Date(currentUser.created_at) > new Date(otherUser.created_at);
}
```

### Metadata Merging

Add custom metadata merging logic:

```javascript
function mergeUserMetadata(primary, secondary) {
  const merged = { ...primary };
  
  // Merge specific fields
  if (secondary.display_name && !merged.display_name) {
    merged.display_name = secondary.display_name;
  }
  
  if (secondary.picture && !merged.picture) {
    merged.picture = secondary.picture;
  }
  
  return merged;
}
```

## Security Considerations

### 1. Email Verification

The Action only links accounts with **verified emails**. This prevents:

- Email spoofing attacks
- Accidental linking of unverified accounts
- Account takeover through email changes

### 2. Rate Limiting

Consider adding rate limiting to prevent abuse:

```javascript
// Add at the beginning of onExecutePostLogin
const RATE_LIMIT_WINDOW = 60000; // 1 minute in ms
const MAX_LINKING_ATTEMPTS = 5;

// You would need to implement a storage mechanism for tracking attempts
```

### 3. Monitoring

Enable logging and monitor for:

- Frequent linking failures
- Unusual linking patterns
- Multiple accounts being linked to the same email

### 4. User Controls

Consider providing user controls to:

- View linked accounts
- Unlink accounts manually
- Choose primary account

## Testing

### Test Scenarios

1. **New User Signup**
   - Sign up with email magic link
   - Verify account is created normally

2. **Social Linking**
   - Sign up with email
   - Sign out
   - Sign in with Google (same email)
   - Verify accounts are linked

3. **Multiple Social Accounts**
   - Link email + Google + Apple
   - Verify all three are linked to one primary account

4. **Error Handling**
   - Try linking with unverified email
   - Verify linking is skipped gracefully

### Debugging

1. Check Auth0 logs for Action execution
2. Look for custom claims in the access token:
   - `accounts_linked`: true if linking occurred
   - `account_linking_failed`: true if linking failed
3. Use the Auth0 Dashboard to view linked accounts

## Troubleshooting

### Common Issues

#### Action Not Executing
- Verify the Action is added to the Post Login flow
- Check the Action is deployed (not just saved)
- Ensure the flow is properly configured

#### Linking Not Working
- Verify both accounts have verified emails
- Check for JavaScript errors in the Action
- Review the Auth0 logs for error messages

#### Wrong Primary Account
- Adjust the `determinePrimaryAccount` function
- Review the `PRESERVE_PRIMARY_PROFILE` setting
- Test with different account creation orders

#### Metadata Not Preserved
- Check the metadata merging logic
- Verify the `api.user.setUserMetadata` calls
- Review the Action's permissions

### Getting Help

1. Check the [Auth0 Actions Documentation](https://auth0.com/docs/customize/actions)
2. Review the [Account Linking Guide](https://auth0.com/docs/manage-users/user-accounts/account-linking)
3. Enable detailed logging and review the logs
4. Test in a development tenant first

## Production Deployment

### Checklist

- [ ] Test thoroughly in development environment
- [ ] Review and customize configuration constants
- [ ] Enable comprehensive logging
- [ ] Set up monitoring for Action failures
- [ ] Document your customizations
- [ ] Test with all supported identity providers
- [ ] Verify user experience with linked accounts

### Rollback Plan

1. Keep the Action disabled initially
2. Enable for a small percentage of users
3. Monitor for issues
4. Roll back by disabling the Action if problems occur

## Related Documentation

- [Auth0 Actions Overview](https://auth0.com/docs/customize/actions)
- [Post Login Actions](https://auth0.com/docs/customize/actions/flows-and-triggers/post-login)
- [Account Linking](https://auth0.com/docs/manage-users/user-accounts/account-linking)
- [User Metadata](https://auth0.com/docs/manage-users/user-accounts/metadata)