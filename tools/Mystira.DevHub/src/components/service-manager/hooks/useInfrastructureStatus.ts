import { invoke } from '@tauri-apps/api/tauri';
import { useEffect, useState } from 'react';

interface InfrastructureStatus {
  dev: { exists: boolean; checking: boolean };
  prod: { exists: boolean; checking: boolean };
}

export function useInfrastructureStatus(serviceEnvironments: Record<string, 'local' | 'dev' | 'prod'>) {
  const [status, setStatus] = useState<InfrastructureStatus>({
    dev: { exists: false, checking: false },
    prod: { exists: false, checking: false },
  });

  const checkStatus = async (environment: 'dev' | 'prod') => {
    setStatus(prev => ({
      ...prev,
      [environment]: { ...prev[environment], checking: true },
    }));

    try {
      const response = await invoke<{ success: boolean; result?: { exists: boolean } }>(
        'check_infrastructure_exists',
        { environment, resourceGroup: null }
      );

      if (response.success && response.result) {
        setStatus(prev => ({
          ...prev,
          [environment]: { exists: response.result!.exists, checking: false },
        }));
      } else {
        setStatus(prev => ({
          ...prev,
          [environment]: { exists: false, checking: false },
        }));
      }
    } catch (error) {
      console.error(`Failed to check ${environment} infrastructure:`, error);
      setStatus(prev => ({
        ...prev,
        [environment]: { exists: false, checking: false },
      }));
    }
  };

  useEffect(() => {
    const hasDev = Object.values(serviceEnvironments).includes('dev');
    const hasProd = Object.values(serviceEnvironments).includes('prod');

    if (hasDev && !status.dev.checking) {
      checkStatus('dev');
    }
    if (hasProd && !status.prod.checking) {
      checkStatus('prod');
    }
  }, [serviceEnvironments]);

  return { status, checkStatus };
}

