import { useEffect, useState } from 'react';
import type { TemplateConfig } from '../../templates/TemplateSelector';

const DEFAULT_TEMPLATES: TemplateConfig[] = [
  {
    id: 'storage',
    name: 'Storage Account',
    file: 'storage.bicep',
    description: 'Azure Storage Account with blob services and containers',
    selected: true,
    resourceGroup: '',
    parameters: { sku: 'Standard_LRS' },
  },
  {
    id: 'cosmos',
    name: 'Cosmos DB',
    file: 'cosmos-db.bicep',
    description: 'Azure Cosmos DB account with database and containers',
    selected: true,
    resourceGroup: '',
    parameters: { databaseName: 'MystiraAppDb' },
  },
  {
    id: 'appservice',
    name: 'App Service',
    file: 'app-service.bicep',
    description: 'Azure App Service with Linux runtime',
    selected: true,
    resourceGroup: '',
    parameters: { sku: 'B1' },
  },
  {
    id: 'keyvault',
    name: 'Key Vault',
    file: 'key-vault.bicep',
    description: 'Azure Key Vault for secrets management',
    selected: false,
    resourceGroup: '',
    parameters: {},
  },
];

export function useTemplates(environment: string) {
  const [templates, setTemplates] = useState<TemplateConfig[]>(DEFAULT_TEMPLATES);

  useEffect(() => {
    const saved = localStorage.getItem(`templates_${environment}`);
    if (saved) {
      try {
        const parsed = JSON.parse(saved);
        setTemplates(parsed);
      } catch (e) {
        console.error('Failed to parse saved templates:', e);
      }
    }
  }, [environment]);

  useEffect(() => {
    localStorage.setItem(`templates_${environment}`, JSON.stringify(templates));
  }, [templates, environment]);

  return { templates, setTemplates };
}

