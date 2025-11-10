/**
 * Auth0 Post-Login Action for Account Linking
 * 
 * This action automatically links accounts that share a verified email address
 * to prevent duplicate accounts when users sign in with different methods.
 * 
 * Features:
 * - Links accounts with verified emails
 * - Preserves primary account attributes
 * - Handles linking conflicts gracefully
 * - Logs linking attempts for security monitoring
 * 
 * To enable:
 * 1. Go to Auth0 Dashboard -> Actions -> Library
 * 2. Create new Action -> "Build from scratch"
 * 3. Copy this code
 * 4. Add to Post-Login flow
 * 5. Deploy and test
 */

/**
 * Handler that will be called during the execution of a PostLogin flow.
 *
 * @param {Event} event - Details about the user and the context in which they are logging in.
 * @param {PostLoginAPI} api - Interface whose methods can be used to change the behavior of the login.
 */
exports.onExecutePostLogin = async (event, api) => {
  // Configuration - adjust these values as needed
  const ENABLE_AUTO_LINKING = true; // Set to false to disable auto-linking
  const PRESERVE_PRIMARY_PROFILE = true; // Keep primary account's profile data
  const LOG_LINKING_ATTEMPTS = true; // Log for security monitoring
  
  // Skip if auto-linking is disabled
  if (!ENABLE_AUTO_LINKING) {
    return;
  }
  
  // Only proceed if user has a verified email
  if (!event.user.email || !event.user.email_verified) {
    if (LOG_LINKING_ATTEMPTS) {
      console.log('Skipping account linking - no verified email found');
    }
    return;
  }
  
  try {
    // Search for existing accounts with the same verified email
    const users = await api.users.getUsersByEmail(event.user.email);
    
    // Filter out the current user and only include verified accounts
    const otherVerifiedUsers = users.filter(user => 
      user.user_id !== event.user.user_id && 
      user.email === event.user.email && 
      user.email_verified === true
    );
    
    if (otherVerifiedUsers.length === 0) {
      if (LOG_LINKING_ATTEMPTS) {
        console.log(`No other verified accounts found for email: ${event.user.email}`);
      }
      return;
    }
    
    // Get the primary account (first found, or you could implement more sophisticated logic)
    const primaryUser = otherVerifiedUsers[0];
    const currentUser = event.user;
    
    // Determine which account should be primary
    const shouldKeepCurrentUserAsPrimary = determinePrimaryAccount(currentUser, primaryUser);
    
    const primaryAccountId = shouldKeepCurrentUserAsPrimary ? currentUser.user_id : primaryUser.user_id;
    const secondaryAccountId = shouldKeepCurrentUserAsPrimary ? primaryUser.user_id : currentUser.user_id;
    
    if (LOG_LINKING_ATTEMPTS) {
      console.log(`Linking accounts: primary=${primaryAccountId}, secondary=${secondaryAccountId}`);
    }
    
    // Link the accounts
    await api.user.linkUser(primaryAccountId, {
      user_id: secondaryAccountId.split('|')[1], // Remove connection prefix
      provider: secondaryAccountId.split('|')[0] // Get connection name
    });
    
    // Set the primary user as the current user
    if (!shouldKeepCurrentUserAsPrimary) {
      api.user.setUserMetadata(primaryUser.user_metadata || {});
      api.user.setAppMetadata(primaryUser.app_metadata || {});
    }
    
    // Add claim to indicate accounts were linked
    api.accessToken.setCustomClaim('accounts_linked', true);
    api.accessToken.setCustomClaim('primary_account', primaryAccountId);
    
    if (LOG_LINKING_ATTEMPTS) {
      console.log(`Successfully linked accounts for email: ${event.user.email}`);
    }
    
  } catch (error) {
    // Log error but don't fail the login
    console.error('Error during account linking:', error);
    
    // Add claim to indicate linking failed
    api.accessToken.setCustomClaim('account_linking_failed', true);
    api.accessToken.setCustomClaim('account_linking_error', error.message);
  }
};

/**
 * Determines which account should be the primary account
 * You can customize this logic based on your business requirements
 */
function determinePrimaryAccount(currentUser, otherUser) {
  // Simple logic: prefer the account that was created most recently
  const currentUserCreated = new Date(currentUser.created_at);
  const otherUserCreated = new Date(otherUser.created_at);
  
  // You could also consider:
  // - Prefer certain connection types (e.g., email over social)
  // - Prefer accounts with more complete profiles
  // - Prefer accounts with certain app_metadata
  
  if (PRESERVE_PRIMARY_PROFILE) {
    // Keep the most recently created account as primary
    return currentUserCreated > otherUserCreated;
  }
  
  // Default: keep current user as primary
  return true;
}

/**
 * Additional helper functions you might want to add:
 */

// Function to check if a connection is considered "primary"
function isPrimaryConnection(connection) {
  const primaryConnections = ['email', 'google-oauth2', 'apple'];
  return primaryConnections.includes(connection);
}

// Function to merge user metadata intelligently
function mergeUserMetadata(primary, secondary) {
  const merged = { ...primary };
  
  // Merge specific fields you want to preserve
  const fieldsToMerge = ['display_name', 'picture', 'locale', 'timezone'];
  
  fieldsToMerge.forEach(field => {
    if (secondary[field] && !merged[field]) {
      merged[field] = secondary[field];
    }
  });
  
  return merged;
}

/**
 * Security considerations:
 * 
 * 1. Only link accounts with verified emails
 * 2. Log all linking attempts for monitoring
 * 3. Handle linking errors gracefully
 * 4. Consider rate limiting to prevent abuse
 * 5. Provide user controls to unlink accounts if needed
 * 6. Consider adding MFA requirements before linking
 */