export interface ServiceStatus {
  name: string;
  running: boolean;
  port?: number;
  url?: string;
  health?: 'healthy' | 'unhealthy' | 'unknown';
  portConflict?: boolean;
}

export interface BuildStatus {
  status: 'idle' | 'building' | 'success' | 'failed';
  progress?: number; // 0-100
  lastBuildTime?: number; // timestamp
  buildDuration?: number; // milliseconds
  message?: string;
}

export interface ServiceLog {
  service: string;
  type: 'stdout' | 'stderr';
  message: string;
  timestamp: number;
}

export interface ServiceConfig {
  name: string;
  displayName: string;
  defaultPort: number;
  isHttps: boolean;
  path: string;
  port: number;
  url: string;
  environment?: 'local' | 'dev' | 'prod';
}

export interface EnvironmentUrls {
  dev?: string;
  prod?: string;
}

export interface EnvironmentStatus {
  dev?: 'online' | 'offline' | 'checking';
  prod?: 'online' | 'offline' | 'checking';
}

export interface LogFilter {
  search: string;
  type: 'all' | 'stdout' | 'stderr';
}

