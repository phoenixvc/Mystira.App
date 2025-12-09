import { ServiceConfig } from '../types';

export function checkEnvironmentContext(
  serviceName: string,
  newEnvironment: 'local' | 'dev' | 'prod',
  allServiceEnvironments: Record<string, 'local' | 'dev' | 'prod'>,
  serviceConfigs: ServiceConfig[]
): { shouldWarn: boolean; message: string; severity: 'warning' | 'danger' } {
  const otherEnvironments = Object.entries(allServiceEnvironments)
    .filter(([name]) => name !== serviceName)
    .map(([, env]) => env);
  
  const hasProd = otherEnvironments.includes('prod');
  const hasDev = otherEnvironments.includes('dev');
  const hasLocal = otherEnvironments.includes('local');
  
  // Warning: Switching to Prod while others are on Dev/Local
  if (newEnvironment === 'prod' && (hasDev || hasLocal)) {
    const mixedServices = Object.entries(allServiceEnvironments)
      .filter(([name, env]) => name !== serviceName && env !== 'prod')
      .map(([name]) => {
        const config = serviceConfigs.find(c => c.name === name);
        return config?.displayName || name;
      });
    
    return {
      shouldWarn: true,
      severity: 'danger',
      message: `⚠️ DANGER: You are switching ${serviceName} to PRODUCTION while other services are on different environments:\n\n${mixedServices.join(', ')}\n\nThis can cause data inconsistencies and unexpected behavior. Are you sure you want to continue?`
    };
  }
  
  // Warning: Mixing Dev and Local
  if (newEnvironment === 'dev' && hasLocal && !hasProd) {
    return {
      shouldWarn: true,
      severity: 'warning',
      message: `⚠️ WARNING: You are switching ${serviceName} to DEV while other services are on LOCAL.\n\nThis may cause API mismatches. Consider switching all services to the same environment.`
    };
  }
  
  // Warning: Switching to Local while others are on Dev/Prod
  if (newEnvironment === 'local' && (hasDev || hasProd)) {
    const deployedServices = Object.entries(allServiceEnvironments)
      .filter(([name, env]) => name !== serviceName && env !== 'local')
      .map(([name, env]) => {
        const config = serviceConfigs.find(c => c.name === name);
        return `${config?.displayName || name} (${env.toUpperCase()})`;
      });
    
    return {
      shouldWarn: true,
      severity: 'warning',
      message: `⚠️ WARNING: You are switching ${serviceName} to LOCAL while other services are on deployed environments:\n\n${deployedServices.join(', ')}\n\nThis may cause API mismatches. Consider switching all services to the same environment.`
    };
  }
  
  return { shouldWarn: false, message: '', severity: 'warning' };
}

