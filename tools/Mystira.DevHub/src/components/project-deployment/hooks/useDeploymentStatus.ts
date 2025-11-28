import { useState, useEffect } from 'react';
import { invoke } from '@tauri-apps/api/tauri';
import type { CommandResponse, ResourceGroupConvention, ProjectInfo } from '../../../types';
import type { InfrastructureStatus } from '../../InfrastructureStatus';
import type { DeploymentStatus } from '../types';

interface UseDeploymentStatusProps {
  environment: string;
  resourceGroupConfig: ResourceGroupConvention;
  projects: ProjectInfo[];
}

export function useDeploymentStatus({
  environment,
  resourceGroupConfig,
  projects,
}: UseDeploymentStatusProps) {
  const [deploymentStatus, setDeploymentStatus] = useState<Record<string, DeploymentStatus>>({});
  const [loadingStatus, setLoadingStatus] = useState(false);
  const [lastRefreshTime, setLastRefreshTime] = useState<number | null>(null);
  const [error, setError] = useState<string | null>(null);

  const loadDeploymentStatus = async () => {
    setLoadingStatus(true);
    setError(null);
    try {
      const resourceGroup = resourceGroupConfig.defaultResourceGroup || `dev-san-rg-mystira-app`;
      
      const response: CommandResponse<InfrastructureStatus> = await invoke('check_infrastructure_status', {
        environment,
        resourceGroup,
      });

      if (response.success && response.result) {
        const infrastructureStatus = response.result;
        const status: Record<string, DeploymentStatus> = {};
        const refreshTime = Date.now();

        projects.forEach(project => {
          const storageDeployed = project.infrastructure.storage 
            ? (infrastructureStatus.resources.storage?.exists || false)
            : true;
          
          const cosmosDeployed = project.infrastructure.cosmos 
            ? (infrastructureStatus.resources.cosmos?.exists || false)
            : true;
          
          const appServiceDeployed = project.infrastructure.appService 
            ? (infrastructureStatus.resources.appService?.exists || false)
            : true;
          
          const keyVaultDeployed = project.infrastructure.keyVault 
            ? (infrastructureStatus.resources.keyVault?.exists || false)
            : true;

          const allRequiredDeployed = 
            (!project.infrastructure.storage || storageDeployed) &&
            (!project.infrastructure.cosmos || cosmosDeployed) &&
            (!project.infrastructure.appService || appServiceDeployed) &&
            (!project.infrastructure.keyVault || keyVaultDeployed);

          const projectStatus: DeploymentStatus = {
            projectId: project.id,
            resources: {
              storage: { 
                deployed: storageDeployed, 
                name: infrastructureStatus.resources.storage?.name 
              },
              cosmos: { 
                deployed: cosmosDeployed, 
                name: infrastructureStatus.resources.cosmos?.name 
              },
              appService: { 
                deployed: appServiceDeployed, 
                name: infrastructureStatus.resources.appService?.name 
              },
              keyVault: { 
                deployed: keyVaultDeployed, 
                name: infrastructureStatus.resources.keyVault?.name 
              },
            },
            allRequiredDeployed,
            lastChecked: refreshTime,
          };

          status[project.id] = projectStatus;
        });

        setDeploymentStatus(status);
        setLastRefreshTime(refreshTime);
      } else {
        const errorMsg = response.error || 'Failed to check infrastructure status';
        setError(errorMsg);
        console.error('Failed to load deployment status:', errorMsg);
      }
    } catch (error) {
      const errorMsg = `Error checking infrastructure status: ${error}`;
      setError(errorMsg);
      console.error('Failed to load deployment status:', error);
    } finally {
      setLoadingStatus(false);
    }
  };

  useEffect(() => {
    loadDeploymentStatus();
  }, [environment, resourceGroupConfig.defaultResourceGroup]);

  return {
    deploymentStatus,
    loadingStatus,
    lastRefreshTime,
    error,
    loadDeploymentStatus,
  };
}

