export function getModuleFromResourceType(resourceType: string): 'storage' | 'cosmos' | 'appservice' | null {
  const normalized = resourceType.toLowerCase().trim();

  const storageTypes = [
    'microsoft.storage/storageaccounts',
    'microsoft.storage/storageaccounts/blobservices',
    'microsoft.storage/storageaccounts/blobservices/containers',
  ];

  const cosmosTypes = [
    'microsoft.documentdb/databaseaccounts',
    'microsoft.documentdb/databaseaccounts/sqldatabases',
    'microsoft.documentdb/databaseaccounts/sqldatabases/containers',
    'microsoft.documentdb/databaseaccounts/sqlroleassignments',
  ];

  const appServiceTypes = [
    'microsoft.web/sites',
    'microsoft.web/serverfarms',
    'microsoft.web/sites/config',
  ];

  if (storageTypes.some(type => normalized === type || normalized.startsWith(type + '/'))) {
    return 'storage';
  }
  if (cosmosTypes.some(type => normalized === type || normalized.startsWith(type + '/'))) {
    return 'cosmos';
  }
  if (appServiceTypes.some(type => normalized === type || normalized.startsWith(type + '/'))) {
    return 'appservice';
  }

  if (normalized.includes('storage') && normalized.includes('account')) {
    return 'storage';
  }
  if (normalized.includes('documentdb') || normalized.includes('cosmos')) {
    return 'cosmos';
  }
  if (normalized.includes('web/sites') || normalized.includes('web/serverfarms')) {
    return 'appservice';
  }

  return null;
}

