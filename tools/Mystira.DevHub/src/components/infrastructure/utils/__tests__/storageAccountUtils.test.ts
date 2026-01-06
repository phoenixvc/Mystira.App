import { describe, it, expect } from 'vitest';
import {
  isValidStorageAccountName,
  extractStorageAccountName,
  parseAzureDeleteError,
} from '../storageAccountUtils';

describe('storageAccountUtils', () => {
  describe('isValidStorageAccountName', () => {
    it('should accept valid storage account names', () => {
      expect(isValidStorageAccountName('mystorageaccount')).toBe(true);
      expect(isValidStorageAccountName('storage123')).toBe(true);
      expect(isValidStorageAccountName('abc')).toBe(true); // minimum 3 chars
      expect(isValidStorageAccountName('abcdefghijklmnopqrstuvwx')).toBe(true); // max 24 chars
    });

    it('should reject names that are too short', () => {
      expect(isValidStorageAccountName('ab')).toBe(false);
      expect(isValidStorageAccountName('a')).toBe(false);
      expect(isValidStorageAccountName('')).toBe(false);
    });

    it('should reject names that are too long', () => {
      expect(isValidStorageAccountName('abcdefghijklmnopqrstuvwxy')).toBe(false); // 25 chars
      expect(isValidStorageAccountName('thisnameiswaywaytoolongforStorageaccount')).toBe(false);
    });

    it('should reject names with uppercase letters', () => {
      expect(isValidStorageAccountName('MyStorageAccount')).toBe(false);
      expect(isValidStorageAccountName('MYSTORAGEACCOUNT')).toBe(false);
    });

    it('should reject names with special characters', () => {
      expect(isValidStorageAccountName('my-storage')).toBe(false);
      expect(isValidStorageAccountName('my_storage')).toBe(false);
      expect(isValidStorageAccountName('my.storage')).toBe(false);
      expect(isValidStorageAccountName('my storage')).toBe(false);
    });
  });

  describe('extractStorageAccountName', () => {
    it('should extract from JSON format with "target" field', () => {
      const error = '{"code": "StorageAccountInAnotherResourceGroup", "target": "devsanappmystirastorage", "message": "The account is in another resource group"}';
      expect(extractStorageAccountName(error)).toBe('devsanappmystirastorage');
    });

    it('should extract from prose format', () => {
      const error = 'The account devsanappmystirastorage is already in another resource group in this subscription.';
      expect(extractStorageAccountName(error)).toBe('devsanappmystirastorage');
    });

    it('should extract from target with different formatting', () => {
      const error = 'Inner Errors: target: "mystorageacct123" message: conflict';
      expect(extractStorageAccountName(error)).toBe('mystorageacct123');
    });

    it('should handle mixed case and convert to lowercase', () => {
      const error = '{"target": "MyStorageAcct"}';
      // Note: The regex only matches lowercase, so this should be null
      // Actually, the regex has /i flag so it will match but return lowercase
      expect(extractStorageAccountName(error)).toBe('mystorageacct');
    });

    it('should return null for invalid/missing storage account name', () => {
      expect(extractStorageAccountName('Some random error message')).toBeNull();
      expect(extractStorageAccountName('')).toBeNull();
      expect(extractStorageAccountName('target: "ab"')).toBeNull(); // too short
    });

    it('should handle real Azure error format', () => {
      const realError = `{"code": "StorageAccountInAnotherResourceGroup", "target": "devsanappmystirastorage", "message": "The account devsanappmystirastorage is already in another resource group in this susbscription."}`;
      expect(extractStorageAccountName(realError)).toBe('devsanappmystirastorage');
    });
  });

  describe('parseAzureDeleteError', () => {
    it('should return default message for undefined error', () => {
      expect(parseAzureDeleteError(undefined)).toBe('Failed to delete storage account');
    });

    it('should return default message for empty error', () => {
      expect(parseAzureDeleteError('')).toBe('Failed to delete storage account');
    });

    it('should detect authorization failures', () => {
      expect(parseAzureDeleteError('AuthorizationFailed: The client does not have authorization'))
        .toContain('Permission denied');
      expect(parseAzureDeleteError('does not have authorization to perform action'))
        .toContain('Permission denied');
    });

    it('should detect resource not found errors', () => {
      const result = parseAzureDeleteError('ResourceNotFound: The storage account was not found');
      expect(result).toContain('no longer exists');
      expect(result).toContain('retry the preview');
    });

    it('should detect resource group not found errors', () => {
      expect(parseAzureDeleteError('ResourceGroupNotFound: The resource group does not exist'))
        .toContain('resource group containing this storage account no longer exists');
    });

    it('should detect lock errors', () => {
      expect(parseAzureDeleteError('ScopeLocked: The resource is locked'))
        .toContain('locked and cannot be deleted');
      expect(parseAzureDeleteError('Cannot delete because there is a lock on the resource'))
        .toContain('locked and cannot be deleted');
    });

    it('should detect login errors', () => {
      expect(parseAzureDeleteError('You are not logged in. Please run az login'))
        .toContain('Azure CLI session expired');
      expect(parseAzureDeleteError('not logged in to Azure'))
        .toContain('Azure CLI session expired');
    });

    it('should return original error for unknown patterns', () => {
      const unknownError = 'Some completely unknown error that we do not recognize';
      expect(parseAzureDeleteError(unknownError)).toBe(unknownError);
    });

    it('should be case insensitive', () => {
      expect(parseAzureDeleteError('AUTHORIZATIONFAILED'))
        .toContain('Permission denied');
      expect(parseAzureDeleteError('resourcenotfound'))
        .toContain('no longer exists');
    });
  });
});
