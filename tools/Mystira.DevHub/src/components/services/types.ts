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
  isManual?: boolean; // true if manually triggered, false if automatic/initial
}

export interface LogEntry {
  service: string;
  type: 'stdout' | 'stderr';
  source?: 'build' | 'run'; // Distinguish between build logs and runtime logs
  message: string;
  timestamp: number;
}

/** @deprecated Use LogEntry instead - kept for backward compatibility */
export type ServiceLog = LogEntry;

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
  source?: 'all' | 'build' | 'run'; // Filter by log source
  severity?: 'all' | 'errors' | 'warnings' | 'info'; // Filter by log severity (legacy - kept for backward compatibility)
  severityEnabled?: {
    errors: boolean;
    warnings: boolean;
    info: boolean;
  }; // Checkbox states for severity filters - if all false or undefined, show all
  timestampFormat?: 'time' | 'full' | 'relative'; // Timestamp display format
}

export interface LogViewerSettings {
  autoScroll: boolean;
  autoScrollToErrors: boolean;
  showLineNumbers: boolean;
  collapseSimilar: boolean;
  maxLogs: number; // Log retention limit
}

