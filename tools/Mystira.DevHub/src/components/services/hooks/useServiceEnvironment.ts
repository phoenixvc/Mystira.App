import { checkEnvironmentContext, getServiceConfigs } from '../index';

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
  const switchServiceEnvironment = async (serviceName: string, environment: 'local' | 'dev' | 'prod') => {
    const serviceConfigs = getServiceConfigs(customPorts, serviceEnvironments, getEnvironmentUrls);
    const contextCheck = checkEnvironmentContext(
      serviceName,
      environment,
      serviceEnvironments,
      serviceConfigs
    );
    
    if (contextCheck.shouldWarn) {
      const confirmed = window.confirm(contextCheck.message);
      if (!confirmed) return;
    }
    
    if (environment === 'prod') {
      const confirmed = window.confirm(
        '⚠️ DANGER: PRODUCTION ENVIRONMENT ⚠️\n\n' +
        'You are about to switch to the PRODUCTION environment.\n\n' +
        'This will connect to live production services and could:\n' +
        '• Affect real user data\n' +
        '• Cause unintended side effects\n' +
        '• Impact production systems\n\n' +
        'Are you absolutely sure you want to continue?\n\n' +
        'Click OK to proceed, or Cancel to abort.'
      );
      if (!confirmed) return;
    }
    
    const status = services.find(s => s.name === serviceName);
    if (status?.running) {
      const stopConfirmed = window.confirm(
        `The ${serviceName} service is currently running. It needs to be stopped before switching environments.\n\nWould you like to stop it now?`
      );
      if (stopConfirmed) {
        await onStopService(serviceName);
      } else {
        return;
      }
    }
    
    const updated = { ...serviceEnvironments, [serviceName]: environment };
    localStorage.setItem('serviceEnvironments', JSON.stringify(updated));
    onServiceEnvironmentsChange(updated);
    
    if (environment !== 'local') {
      onCheckEnvironmentHealth(serviceName, environment);
    }
    
    const envName = environment === 'local' ? 'Local' : environment.toUpperCase();
    onAddToast(`${serviceName} switched to ${envName} environment`, 'success');
  };

  return {
    switchServiceEnvironment,
  };
}

