import { useEffect, useState } from 'react';
import type { ResourceGroupConvention } from '../../../types';

// Naming convention: [org]-[env]-[project]-rg-[region]
const DEFAULT_CONFIG: ResourceGroupConvention = {
  pattern: 'mys-{env}-mystira-rg-san',
  defaultResourceGroup: 'mys-dev-mystira-rg-san',
  resourceTypeMappings: {},
};

export function useResourceGroupConfig(environment: string) {
  const [config, setConfig] = useState<ResourceGroupConvention>(DEFAULT_CONFIG);

  useEffect(() => {
    const saved = localStorage.getItem(`resourceGroupConfig_${environment}`);
    if (saved) {
      try {
        const parsed = JSON.parse(saved);
        setConfig(parsed);
      } catch (e) {
        console.error('Failed to parse saved resource group config:', e);
      }
    }
  }, [environment]);

  const saveConfig = (newConfig: ResourceGroupConvention) => {
    setConfig(newConfig);
    localStorage.setItem(`resourceGroupConfig_${environment}`, JSON.stringify(newConfig));
  };

  return { config, setConfig: saveConfig };
}

