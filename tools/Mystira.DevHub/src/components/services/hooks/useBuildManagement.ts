import { invoke } from '@tauri-apps/api/tauri';
import { useState } from 'react';
import { BuildStatus } from '../types';

export function useBuildManagement() {
  const [buildStatus, setBuildStatus] = useState<Record<string, BuildStatus>>({});

  const prebuildService = async (
    serviceName: string,
    repoRoot: string,
    onViewModeChange: (serviceName: string, mode: 'logs') => void,
    onShowLogs: (serviceName: string, show: boolean) => void
  ) => {
    const startTime = Date.now();
    
    onShowLogs(serviceName, true);
    onViewModeChange(serviceName, 'logs');
    
    setBuildStatus(prev => ({
      ...prev,
      [serviceName]: {
        status: 'building',
        progress: 0,
        message: 'Initializing build...',
      },
    }));
    
    const progressInterval = setInterval(() => {
      const elapsed = Date.now() - startTime;
      const estimatedProgress = Math.min(90, 10 + (elapsed / 45000) * 80);
      setBuildStatus(prev => ({
        ...prev,
        [serviceName]: {
          ...prev[serviceName],
          progress: Math.floor(estimatedProgress),
          message: `Building... (${Math.floor(estimatedProgress)}%)`,
        },
      }));
    }, 500);
    
    try {
      await invoke('prebuild_service', {
        serviceName,
        repoRoot,
      });
      
      clearInterval(progressInterval);
      const duration = Date.now() - startTime;
      setBuildStatus(prev => ({
        ...prev,
        [serviceName]: {
          status: 'success',
          progress: 100,
          lastBuildTime: Date.now(),
          buildDuration: duration,
          message: `Built in ${(duration / 1000).toFixed(1)}s`,
        },
      }));
    } catch (error: any) {
      clearInterval(progressInterval);
      const duration = Date.now() - startTime;
      onShowLogs(serviceName, true);
      onViewModeChange(serviceName, 'logs');
      setBuildStatus(prev => ({
        ...prev,
        [serviceName]: {
          status: 'failed',
          progress: 0,
          lastBuildTime: Date.now(),
          buildDuration: duration,
          message: `Build failed: ${error?.message || error}`,
        },
      }));
      console.error(`Prebuild failed for ${serviceName}:`, error);
    }
  };

  const prebuildAllServices = async (
    repoRoot: string,
    serviceConfigs: Array<{ name: string }>,
    useCurrentBranch: boolean,
    currentBranch: string,
    onViewModeChange: (serviceName: string, mode: 'logs') => void,
    onShowLogs: (serviceName: string, show: boolean) => void
  ) => {
    // Validate repoRoot is not empty
    if (!repoRoot || repoRoot.trim() === '') {
      console.error('Repository root is empty. Cannot prebuild services.');
      return;
    }
    
    const rootToUse = useCurrentBranch && currentBranch 
      ? `${repoRoot}\\..\\Mystira.App-${currentBranch}`
      : repoRoot;
    
    const prebuildPromises = serviceConfigs.map((config) =>
      prebuildService(config.name, rootToUse, onViewModeChange, onShowLogs).catch(err => {
        console.error(`Failed to prebuild ${config.name}:`, err);
      })
    );
    
    await Promise.allSettled(prebuildPromises);
  };

  return {
    buildStatus,
    prebuildService,
    prebuildAllServices,
  };
}

