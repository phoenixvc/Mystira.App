/**
 * Utilities for handling Azure Storage Account conflicts
 */

/**
 * Validates storage account name against Azure naming rules
 * Azure storage account names must be 3-24 lowercase alphanumeric characters
 */
export const isValidStorageAccountName = (name: string): boolean => {
  return /^[a-z0-9]{3,24}$/.test(name);
};

/**
 * Extracts storage account name from Azure error message
 * Supports multiple error message formats from Azure CLI
 */
export const extractStorageAccountName = (errorStr: string): string | null => {
  // Pattern 1: JSON format "target": "accountname"
  const jsonMatch = errorStr.match(/"target"\s*:\s*"([a-z0-9]{3,24})"/i);
  if (jsonMatch?.[1]) return jsonMatch[1].toLowerCase();

  // Pattern 2: "account <name> is already in another resource group"
  const proseMatch = errorStr.match(/account\s+([a-z0-9]{3,24})\s+is/i);
  if (proseMatch?.[1]) return proseMatch[1].toLowerCase();

  // Pattern 3: StorageAccountInAnotherResourceGroup with target
  const targetMatch = errorStr.match(/target["\s:]+([a-z0-9]{3,24})/i);
  if (targetMatch?.[1]) return targetMatch[1].toLowerCase();

  return null;
};

/**
 * Parses Azure CLI error to provide user-friendly message
 */
export const parseAzureDeleteError = (error: string | undefined): string => {
  if (!error) return 'Failed to delete storage account';

  const errorLower = error.toLowerCase();

  // Check for common error patterns
  if (errorLower.includes('authorizationfailed') || errorLower.includes('does not have authorization')) {
    return 'Permission denied: You do not have permission to delete this storage account. Contact your Azure administrator to grant you the "Storage Account Contributor" role.';
  }
  if (errorLower.includes('resourcenotfound') || errorLower.includes('was not found') || errorLower.includes('could not be found')) {
    return 'Storage account no longer exists. It may have been deleted by another user. You can retry the preview now.';
  }
  if (errorLower.includes('resourcegroupnotfound')) {
    return 'The resource group containing this storage account no longer exists.';
  }
  if (errorLower.includes('scopelocked') || (errorLower.includes('cannot delete') && errorLower.includes('lock'))) {
    return 'Storage account is locked and cannot be deleted. Remove the resource lock in Azure Portal first.';
  }
  if (errorLower.includes('not logged in') || (errorLower.includes('please run') && errorLower.includes('az login'))) {
    return 'Azure CLI session expired. Please run "az login" in your terminal and try again.';
  }

  // Return original error if no pattern matched
  return error;
};
