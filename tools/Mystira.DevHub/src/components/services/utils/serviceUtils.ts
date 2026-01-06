import { ServiceConfig } from '../types';

export function formatTimeSince(timestamp?: number): string | null {
  if (!timestamp) return null;
  const seconds = Math.floor((Date.now() - timestamp) / 1000);
  if (seconds < 60) return `${seconds}s ago`;
  const minutes = Math.floor(seconds / 60);
  if (minutes < 60) return `${minutes}m ago`;
  const hours = Math.floor(minutes / 60);
  if (hours < 24) return `${hours}h ago`;
  const days = Math.floor(hours / 24);
  return `${days}d ago`;
}

export function getHealthIndicator(health?: 'healthy' | 'unhealthy' | 'unknown'): string {
  if (health === 'healthy') {
    return '🟢';
  } else if (health === 'unhealthy') {
    return '🔴';
  }
  return '⚪';
}

export function getServiceConfigs(
  customPorts: Record<string, number>,
  serviceEnvironments: Record<string, 'local' | 'dev' | 'prod'>,
  getEnvironmentUrls: (serviceName: string) => { dev?: string; prod?: string }
): ServiceConfig[] {
  const baseConfigs = [
    { name: 'api', displayName: 'API', defaultPort: 7096, isHttps: true, path: '/swagger' },
    { name: 'admin-api', displayName: 'Admin API', defaultPort: 7097, isHttps: true, path: '/swagger' },
    { name: 'pwa', displayName: 'PWA', defaultPort: 7000, isHttps: false, path: '' },
  ];
  
  return baseConfigs.map(config => {
    const environment = serviceEnvironments[config.name] || 'local';
    const envUrls = getEnvironmentUrls(config.name);
    
    let url: string;
    if (environment === 'dev' && envUrls.dev) {
      url = envUrls.dev;
    } else if (environment === 'prod' && envUrls.prod) {
      url = envUrls.prod;
    } else {
      // Local environment
      const port = customPorts[config.name] || config.defaultPort;
      const protocol = config.isHttps ? 'https' : 'http';
      url = `${protocol}://localhost:${port}${config.path}`;
    }
    
    return {
      ...config,
      port: customPorts[config.name] || config.defaultPort,
      url,
      environment,
    };
  });
}

