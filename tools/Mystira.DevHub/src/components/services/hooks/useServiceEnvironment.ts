import { useState } from 'react';
import { checkEnvironmentContext, getServiceConfigs } from '../index';

export interface EnvironmentConfirmation {
  type: 'context' | 'production' | 'stopService';
  serviceName: string;
  environment: 'local' | 'dev' | 'prod';
  message: string;
}

interface UseServiceEnvironmentProps {
  customPorts: Record<string, number>;
  serviceEnvironments: Record<string, 'local' | 'dev' | 'prod'>;
  getEnvironmentUrls: (serviceName: string) => { dev?: string; prod?: string };
  services: Array<{ name: string; running: boolean }>;
  onStopService: (serviceName: string) => Promise<void>;
  onServiceEnvironmentsChange: (environments: Record<string, 'local' | 'dev' | 'prod'>) => void;
  onCheckEnvironmentHealth: (serviceName: string, environment: 'dev' | 'prod') => void;
  onAddToast: (message: string, type: 'info' | 'success' | 'error' | 'warning', duration?: number) => void;
}


export function useServiceEnvironment({
  customPorts,
  serviceEnvironments,
  getEnvironmentUrls,
  services,
  onStopService,
  onServiceEnvironmentsChange,
  onCheckEnvironmentHealth,
  onAddToast,
}: UseServiceEnvironmentProps) {
  // State for pending confirmation dialogs
  const [pendingConfirmation, setPendingConfirmation] = useState<EnvironmentConfirmation | null>(null);

  // Perform the actual environment switch (called after all confirmations)
  const performEnvironmentSwitch = async (serviceName: string, environment: 'local' | 'dev' | 'prod', needsStop: boolean) => {
    if (needsStop) {
      await onStopService(serviceName);
    }

    const updated = { ...serviceEnvironments, [serviceName]: environment };
    localStorage.setItem('serviceEnvironments', JSON.stringify(updated));
    onServiceEnvironmentsChange(updated);

    if (environment !== 'local') {
      onCheckEnvironmentHealth(serviceName, environment);
    }

    const envName = environment === 'local' ? 'Local' : environment.toUpperCase();
    onAddToast(`${serviceName} switched to ${envName} environment`, 'success');
    setPendingConfirmation(null);
  };

  // Handle confirmation dialog result
  const handleConfirmation = async (confirmed: boolean) => {
    if (!pendingConfirmation || !confirmed) {
      setPendingConfirmation(null);
      return;
    }

    const { type, serviceName, environment } = pendingConfirmation;
    const status = services.find(s => s.name === serviceName);

    if (type === 'context') {
      // Context warning confirmed, check for production warning next
      if (environment === 'prod') {
        setPendingConfirmation({
          type: 'production',
          serviceName,
          environment,
          message: 'You are about to switch to the PRODUCTION environment. This will connect to live production services and could affect real user data, cause unintended side effects, and impact production systems.',
        });
      } else if (status?.running) {
        setPendingConfirmation({
          type: 'stopService',
          serviceName,
          environment,
          message: `The ${serviceName} service is currently running. It needs to be stopped before switching environments. Would you like to stop it now?`,
        });
      } else {
        await performEnvironmentSwitch(serviceName, environment, false);
      }
    } else if (type === 'production') {
      // Production warning confirmed, check if service needs to be stopped
      if (status?.running) {
        setPendingConfirmation({
          type: 'stopService',
          serviceName,
          environment,
          message: `The ${serviceName} service is currently running. It needs to be stopped before switching environments. Would you like to stop it now?`,
        });
      } else {
        await performEnvironmentSwitch(serviceName, environment, false);
      }
    } else if (type === 'stopService') {
      // Stop service confirmed, perform the switch
      await performEnvironmentSwitch(serviceName, environment, true);
    }
  };

  const switchServiceEnvironment = async (serviceName: string, environment: 'local' | 'dev' | 'prod') => {
    const serviceConfigs = getServiceConfigs(customPorts, serviceEnvironments, getEnvironmentUrls);
    const contextCheck = checkEnvironmentContext(
      serviceName,
      environment,
      serviceEnvironments,
      serviceConfigs
    );

    const status = services.find(s => s.name === serviceName);

    // Check if context warning is needed
    if (contextCheck.shouldWarn) {
      setPendingConfirmation({
        type: 'context',
        serviceName,
        environment,
        message: contextCheck.message,
      });
      return;
    }

    // Check if production warning is needed
    if (environment === 'prod') {
      setPendingConfirmation({
        type: 'production',
        serviceName,
        environment,
        message: 'You are about to switch to the PRODUCTION environment. This will connect to live production services and could affect real user data, cause unintended side effects, and impact production systems.',
      });
      return;
    }

    // Check if service needs to be stopped
    if (status?.running) {
      setPendingConfirmation({
        type: 'stopService',
        serviceName,
        environment,
        message: `The ${serviceName} service is currently running. It needs to be stopped before switching environments. Would you like to stop it now?`,
      });
      return;
    }

    // No confirmations needed, perform switch immediately
    await performEnvironmentSwitch(serviceName, environment, false);
  };

  return {
    switchServiceEnvironment,
    pendingConfirmation,
    handleConfirmation,
    cancelConfirmation: () => setPendingConfirmation(null),
  };
}

