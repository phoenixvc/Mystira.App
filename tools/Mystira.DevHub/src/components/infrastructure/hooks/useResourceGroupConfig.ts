import { useEffect, useState } from 'react';
import type { ResourceGroupConvention } from '../../../types';

const DEFAULT_CONFIG: ResourceGroupConvention = {
  pattern: '{env}-euw-rg-{resource}',
  defaultResourceGroup: 'dev-euw-rg-mystira-app',
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

