import { invoke } from '@tauri-apps/api/tauri';
import { useState } from 'react';
import { EnvironmentStatus, EnvironmentUrls } from '../types';

export function useEnvironmentManagement() {
  const [environmentUrls, setEnvironmentUrls] = useState<Record<string, EnvironmentUrls>>({});
  const [environmentStatus, setEnvironmentStatus] = useState<Record<string, EnvironmentStatus>>({});

  // Get environment URLs with fallback to defaults
  const getEnvironmentUrls = (serviceName: string): EnvironmentUrls => {
    if (environmentUrls[serviceName]) {
      return environmentUrls[serviceName];
    }
    
    // Fallback to hardcoded defaults
    const defaultConfigs: Record<string, EnvironmentUrls> = {
      'api': {
        dev: 'https://api-dev.mystira.app/swagger',
        prod: 'https://api.mystira.app/swagger',
      },
      'admin-api': {
        dev: 'https://admin-api-dev.mystira.app/swagger',
        prod: 'https://admin-api.mystira.app/swagger',
      },
      'pwa': {
        dev: 'https://pwa-dev.mystira.app',
        prod: 'https://mystira.app',
      },
    };
    return defaultConfigs[serviceName] || {};
  };

  // Fetch environment URLs from Azure resources
  const fetchEnvironmentUrls = async () => {
    try {
      const response = await invoke<{ success: boolean; result?: any[] }>('get_azure_resources', {});
      if (response.success && response.result) {
        const urls: Record<string, EnvironmentUrls> = {};
        
        response.result.forEach((resource: any) => {
          if (resource.type === 'Microsoft.Web/sites') {
            const name = resource.name?.toLowerCase() || '';
            const url = resource.properties?.defaultHostName 
              ? `https://${resource.properties.defaultHostName}` 
              : undefined;
            
            if (name.includes('api') && !name.includes('admin')) {
              if (name.includes('dev')) {
                urls['api'] = { ...urls['api'], dev: url ? `${url}/swagger` : undefined };
              } else if (name.includes('prod') || (!name.includes('dev') && !name.includes('staging'))) {
                urls['api'] = { ...urls['api'], prod: url ? `${url}/swagger` : undefined };
              }
            } else if (name.includes('admin')) {
              if (name.includes('dev')) {
                urls['admin-api'] = { ...urls['admin-api'], dev: url ? `${url}/swagger` : undefined };
              } else if (name.includes('prod') || (!name.includes('dev') && !name.includes('staging'))) {
                urls['admin-api'] = { ...urls['admin-api'], prod: url ? `${url}/swagger` : undefined };
              }
            } else if (name.includes('pwa') || name.includes('web')) {
              if (name.includes('dev')) {
                urls['pwa'] = { ...urls['pwa'], dev: url };
              } else if (name.includes('prod') || (!name.includes('dev') && !name.includes('staging'))) {
                urls['pwa'] = { ...urls['pwa'], prod: url };
              }
            }
          }
        });
        
        setEnvironmentUrls(urls);
      }
    } catch (error) {
      console.warn('Failed to fetch environment URLs from Azure:', error);
    }
  };

  // Check environment health
  const checkEnvironmentHealth = async (serviceName: string, environment: 'dev' | 'prod') => {
    const envUrls = getEnvironmentUrls(serviceName);
    const url = environment === 'dev' ? envUrls.dev : envUrls.prod;
    
    if (!url) {
      setEnvironmentStatus(prev => ({
        ...prev,
        [serviceName]: {
          ...prev[serviceName],
          [environment]: 'offline',
        },
      }));
      return;
    }
    
    setEnvironmentStatus(prev => ({
      ...prev,
      [serviceName]: {
        ...prev[serviceName],
        [environment]: 'checking',
      },
    }));
    
    try {
      const baseUrl = url.replace('/swagger', '').replace('/swagger/', '');
      const healthUrl = `${baseUrl}/health`;
      
      const controller = new AbortController();
      const timeoutId = setTimeout(() => controller.abort(), 5000);
      
      try {
        const response = await fetch(healthUrl, { 
          method: 'GET',
          signal: controller.signal,
          mode: 'cors',
        });
        clearTimeout(timeoutId);
        
        if (response.ok) {
          setEnvironmentStatus(prev => ({
            ...prev,
            [serviceName]: {
              ...prev[serviceName],
              [environment]: 'online',
            },
          }));
          return;
        }
      } catch (healthError) {
        clearTimeout(timeoutId);
      }
      
      const baseController = new AbortController();
      const baseTimeoutId = setTimeout(() => baseController.abort(), 5000);
      
      try {
        const baseResponse = await fetch(baseUrl, { 
          method: 'HEAD',
          signal: baseController.signal,
          mode: 'cors',
        });
        clearTimeout(baseTimeoutId);
        
        if (baseResponse.ok || baseResponse.status < 500) {
          setEnvironmentStatus(prev => ({
            ...prev,
            [serviceName]: {
              ...prev[serviceName],
              [environment]: 'online',
            },
          }));
          return;
        }
      } catch (baseError) {
        clearTimeout(baseTimeoutId);
      }
      
      setEnvironmentStatus(prev => ({
        ...prev,
        [serviceName]: {
          ...prev[serviceName],
          [environment]: 'offline',
        },
      }));
    } catch (error) {
      setEnvironmentStatus(prev => ({
        ...prev,
        [serviceName]: {
          ...prev[serviceName],
          [environment]: 'offline',
        },
      }));
    }
  };

  return {
    environmentUrls,
    environmentStatus,
    getEnvironmentUrls,
    fetchEnvironmentUrls,
    checkEnvironmentHealth,
  };
}

