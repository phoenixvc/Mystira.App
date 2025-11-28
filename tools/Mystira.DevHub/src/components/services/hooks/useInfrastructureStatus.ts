import { invoke } from '@tauri-apps/api/tauri';
import { useState, useEffect } from 'react';

interface InfrastructureStatusState {
  dev: { exists: boolean; checking: boolean };
  prod: { exists: boolean; checking: boolean };
}

interface UseInfrastructureStatusProps {
  serviceEnvironments: Record<string, 'local' | 'dev' | 'prod'>;
}

export function useInfrastructureStatus({ serviceEnvironments }: UseInfrastructureStatusProps) {
  const [infrastructureStatus, setInfrastructureStatus] = useState<InfrastructureStatusState>({
    dev: { exists: false, checking: false },
    prod: { exists: false, checking: false },
  });

  const checkInfrastructureStatus = async (environment: 'dev' | 'prod') => {
    setInfrastructureStatus(prev => ({
      ...prev,
      [environment]: { ...prev[environment], checking: true },
    }));
    
    try {
      const response = await invoke<{ success: boolean; result?: { exists: boolean } }>(
        'check_infrastructure_exists',
        { environment, resourceGroup: null }
      );
      
      if (response.success && response.result) {
        setInfrastructureStatus(prev => ({
          ...prev,
          [environment]: { exists: response.result!.exists, checking: false },
        }));
      } else {
        setInfrastructureStatus(prev => ({
          ...prev,
          [environment]: { exists: false, checking: false },
        }));
      }
    } catch (error) {
      console.error(`Failed to check ${environment} infrastructure:`, error);
      setInfrastructureStatus(prev => ({
        ...prev,
        [environment]: { exists: false, checking: false },
      }));
    }
  };

  useEffect(() => {
    checkInfrastructureStatus('dev');
    checkInfrastructureStatus('prod');
  }, []);

  useEffect(() => {
    const hasDev = Object.values(serviceEnvironments).includes('dev');
    const hasProd = Object.values(serviceEnvironments).includes('prod');
    
    if (hasDev && !infrastructureStatus.dev.checking) {
      checkInfrastructureStatus('dev');
    }
    if (hasProd && !infrastructureStatus.prod.checking) {
      checkInfrastructureStatus('prod');
    }
  }, [serviceEnvironments]);

  return {
    infrastructureStatus,
    checkInfrastructureStatus,
  };
}

