# Mystira Admin Desktop Application - Architecture Guide

## Overview

This document outlines the architecture for a standalone React/Tauri desktop application that provides a user-friendly GUI for managing Mystira infrastructure, data migrations, and administrative tasks.

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────┐
│                    Mystira Admin Desktop App                 │
│                     (React + Tauri)                          │
├─────────────────────────────────────────────────────────────┤
│  Frontend (React)              │  Backend (Tauri/Rust)       │
│  ├─ Dashboard                  │  ├─ CLI Process Manager     │
│  ├─ Infrastructure Manager     │  ├─ Auth Token Storage      │
│  ├─ Migration Manager          │  ├─ Secure Config Storage   │
│  ├─ Scenario Manager           │  └─ File System Access      │
│  └─ Settings                   │                             │
└─────────────────┬───────────────────────────────────────────┘
                  │
                  │ HTTP/REST API
                  ▼
┌─────────────────────────────────────────────────────────────┐
│              Mystira Admin API (Azure App Service)           │
│              https://dev-euw-app-mystora-admin-api           │
├─────────────────────────────────────────────────────────────┤
│  ├─ Authentication (JWT)                                     │
│  ├─ Infrastructure Management                                │
│  ├─ Data Migration Operations                                │
│  ├─ Scenario Management                                      │
│  └─ System Configuration                                     │
└─────────────────────────────────────────────────────────────┘
```

## Technology Stack

### Frontend
- **React 18** - UI framework
- **TypeScript** - Type safety
- **TanStack Query (React Query)** - API state management
- **Zustand** - Client-side state management
- **Tailwind CSS** - Styling
- **Shadcn/ui** - Component library
- **React Router** - Navigation

### Backend (Tauri)
- **Tauri 1.5+** - Desktop app framework
- **Rust** - Backend runtime for Tauri
- **tokio** - Async runtime
- **reqwest** - HTTP client for API calls

### Build & Development
- **Vite** - Build tool and dev server
- **pnpm** - Package manager
- **ESLint & Prettier** - Code quality

## Project Structure

```
tools/mystira-admin-desktop/
├── src/                          # React frontend source
│   ├── components/               # Reusable UI components
│   │   ├── ui/                   # Base UI components (shadcn)
│   │   ├── infrastructure/       # Infrastructure management components
│   │   ├── migration/            # Data migration components
│   │   └── scenarios/            # Scenario management components
│   ├── pages/                    # Page components
│   │   ├── Dashboard.tsx
│   │   ├── Infrastructure.tsx
│   │   ├── Migration.tsx
│   │   ├── Scenarios.tsx
│   │   └── Settings.tsx
│   ├── services/                 # API services
│   │   ├── api.ts                # API client configuration
│   │   ├── auth.ts               # Authentication service
│   │   ├── infrastructure.ts     # Infrastructure operations
│   │   └── migration.ts          # Migration operations
│   ├── stores/                   # State management
│   │   ├── authStore.ts          # Auth state
│   │   └── configStore.ts        # App configuration
│   ├── types/                    # TypeScript types
│   │   ├── api.ts
│   │   └── models.ts
│   ├── utils/                    # Utility functions
│   │   ├── constants.ts
│   │   └── helpers.ts
│   ├── App.tsx                   # Root component
│   └── main.tsx                  # React entry point
├── src-tauri/                    # Tauri backend
│   ├── src/
│   │   ├── main.rs               # Tauri app entry
│   │   ├── commands.rs           # Tauri commands (IPC)
│   │   ├── auth.rs               # Secure auth token storage
│   │   └── config.rs             # Configuration management
│   ├── Cargo.toml                # Rust dependencies
│   └── tauri.conf.json           # Tauri configuration
├── public/                       # Static assets
├── package.json
├── tsconfig.json
├── vite.config.ts
├── tailwind.config.js
└── README.md
```

## Authentication Flow

### JWT-Based Authentication

```typescript
// 1. User Login
POST /adminapi/auth/login
{
  "email": "admin@mystira.app",
  "password": "********"
}

// Response
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "...",
  "expiresAt": "2024-01-01T12:00:00Z",
  "user": {
    "id": "user-123",
    "email": "admin@mystira.app",
    "role": "Admin"
  }
}

// 2. Tauri Command - Store Token Securely
await invoke('store_auth_token', { token: '...' });

// 3. Subsequent API Calls - Include Token
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...

// 4. Token Refresh (when expired)
POST /adminapi/auth/refresh
{
  "refreshToken": "..."
}
```

### Secure Token Storage (Tauri)

```rust
// src-tauri/src/auth.rs
use keyring::Entry;
use tauri::command;

#[command]
pub fn store_auth_token(token: String) -> Result<(), String> {
    let entry = Entry::new("mystira-admin", "auth-token")
        .map_err(|e| e.to_string())?;
    entry.set_password(&token)
        .map_err(|e| e.to_string())
}

#[command]
pub fn get_auth_token() -> Result<String, String> {
    let entry = Entry::new("mystira-admin", "auth-token")
        .map_err(|e| e.to_string())?;
    entry.get_password()
        .map_err(|e| e.to_string())
}

#[command]
pub fn delete_auth_token() -> Result<(), String> {
    let entry = Entry::new("mystira-admin", "auth-token")
        .map_err(|e| e.to_string())?;
    entry.delete_password()
        .map_err(|e| e.to_string())
}
```

## API Integration

### API Service Configuration

```typescript
// src/services/api.ts
import axios, { AxiosInstance } from 'axios';
import { invoke } from '@tauri-apps/api/tauri';

const API_BASE_URL = 'https://dev-euw-app-mystora-admin-api.azurewebsites.net';  // Note: "mystora" is the actual Azure resource name

class ApiClient {
  private client: AxiosInstance;

  constructor() {
    this.client = axios.create({
      baseURL: API_BASE_URL,
      timeout: 30000,
      headers: {
        'Content-Type': 'application/json',
      },
    });

    // Request interceptor - Add auth token
    this.client.interceptors.request.use(async (config) => {
      try {
        const token = await invoke<string>('get_auth_token');
        if (token) {
          config.headers.Authorization = `Bearer ${token}`;
        }
      } catch (error) {
        console.error('Failed to get auth token:', error);
      }
      return config;
    });

    // Response interceptor - Handle errors
    this.client.interceptors.response.use(
      (response) => response,
      async (error) => {
        if (error.response?.status === 401) {
          // Token expired - redirect to login
          await invoke('delete_auth_token');
          window.location.href = '/login';
        }
        return Promise.reject(error);
      }
    );
  }

  public getClient(): AxiosInstance {
    return this.client;
  }
}

export const apiClient = new ApiClient().getClient();
```

### Infrastructure Operations Service

```typescript
// src/services/infrastructure.ts
import { apiClient } from './api';
import { invoke } from '@tauri-apps/api/tauri';

export interface InfrastructureValidationResult {
  isValid: boolean;
  errors: string[];
  warnings: string[];
}

export interface DeploymentStatus {
  id: string;
  status: 'pending' | 'running' | 'succeeded' | 'failed';
  startedAt: string;
  completedAt?: string;
  logs: string[];
}

export const infrastructureService = {
  // Validate Bicep templates
  async validate(): Promise<InfrastructureValidationResult> {
    const { data } = await apiClient.post('/adminapi/infrastructure/validate');
    return data;
  },

  // Preview infrastructure changes (what-if)
  async preview(): Promise<any> {
    const { data } = await apiClient.post('/adminapi/infrastructure/preview');
    return data;
  },

  // Deploy infrastructure
  async deploy(): Promise<DeploymentStatus> {
    const { data } = await apiClient.post('/adminapi/infrastructure/deploy');
    return data;
  },

  // Get deployment status
  async getDeploymentStatus(deploymentId: string): Promise<DeploymentStatus> {
    const { data } = await apiClient.get(`/adminapi/infrastructure/deployments/${deploymentId}`);
    return data;
  },

  // Alternatively: Use GitHub CLI via Tauri command
  async triggerGitHubWorkflow(action: 'validate' | 'preview' | 'deploy'): Promise<string> {
    return await invoke('trigger_github_workflow', { action });
  },
};
```

### Migration Operations Service

```typescript
// src/services/migration.ts
import { apiClient } from './api';

export interface MigrationProgress {
  type: 'scenarios' | 'bundles' | 'media' | 'blobs';
  total: number;
  completed: number;
  failed: number;
  status: 'running' | 'completed' | 'failed';
}

export const migrationService = {
  // Start migration
  async startMigration(
    types: string[],
    sourceConfig: any,
    destConfig: any
  ): Promise<string> {
    const { data } = await apiClient.post('/adminapi/migrations/start', {
      types,
      sourceConfig,
      destConfig,
    });
    return data.migrationId;
  },

  // Get migration progress
  async getMigrationProgress(migrationId: string): Promise<MigrationProgress[]> {
    const { data } = await apiClient.get(`/adminapi/migrations/${migrationId}/progress`);
    return data;
  },

  // Get migration logs
  async getMigrationLogs(migrationId: string): Promise<string[]> {
    const { data } = await apiClient.get(`/adminapi/migrations/${migrationId}/logs`);
    return data;
  },
};
```

## UI Components

### Dashboard Page

```typescript
// src/pages/Dashboard.tsx
import React from 'react';
import { useQuery } from '@tanstack/react-query';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { apiClient } from '@/services/api';

export const Dashboard: React.FC = () => {
  const { data: stats } = useQuery({
    queryKey: ['dashboard-stats'],
    queryFn: async () => {
      const { data } = await apiClient.get('/adminapi/dashboard/stats');
      return data;
    },
  });

  return (
    <div className="p-6 space-y-6">
      <h1 className="text-3xl font-bold">Admin Dashboard</h1>
      
      <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
        <Card>
          <CardHeader>
            <CardTitle>Scenarios</CardTitle>
          </CardHeader>
          <CardContent>
            <p className="text-3xl font-bold">{stats?.scenariosCount || 0}</p>
            <p className="text-sm text-muted-foreground">Total scenarios</p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>Content Bundles</CardTitle>
          </CardHeader>
          <CardContent>
            <p className="text-3xl font-bold">{stats?.bundlesCount || 0}</p>
            <p className="text-sm text-muted-foreground">Total bundles</p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>Media Assets</CardTitle>
          </CardHeader>
          <CardContent>
            <p className="text-3xl font-bold">{stats?.mediaCount || 0}</p>
            <p className="text-sm text-muted-foreground">Total media files</p>
          </CardContent>
        </Card>
      </div>
    </div>
  );
};
```

### Infrastructure Management Page

```typescript
// src/pages/Infrastructure.tsx
import React, { useState } from 'react';
import { useMutation } from '@tanstack/react-query';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Alert, AlertDescription } from '@/components/ui/alert';
import { infrastructureService } from '@/services/infrastructure';
import { Loader2, CheckCircle, XCircle } from 'lucide-react';

export const Infrastructure: React.FC = () => {
  const [validationResult, setValidationResult] = useState<any>(null);

  const validateMutation = useMutation({
    mutationFn: infrastructureService.validate,
    onSuccess: (data) => {
      setValidationResult(data);
    },
  });

  const deployMutation = useMutation({
    mutationFn: infrastructureService.deploy,
  });

  return (
    <div className="p-6 space-y-6">
      <h1 className="text-3xl font-bold">Infrastructure Management</h1>

      <Card>
        <CardHeader>
          <CardTitle>Bicep Template Validation</CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <Button
            onClick={() => validateMutation.mutate()}
            disabled={validateMutation.isPending}
          >
            {validateMutation.isPending && (
              <Loader2 className="mr-2 h-4 w-4 animate-spin" />
            )}
            Validate Templates
          </Button>

          {validationResult && (
            <Alert>
              <AlertDescription>
                {validationResult.isValid ? (
                  <div className="flex items-center gap-2 text-green-600">
                    <CheckCircle className="h-5 w-5" />
                    <span>Templates are valid!</span>
                  </div>
                ) : (
                  <div className="space-y-2">
                    <div className="flex items-center gap-2 text-red-600">
                      <XCircle className="h-5 w-5" />
                      <span>Validation errors found:</span>
                    </div>
                    <ul className="list-disc pl-6">
                      {validationResult.errors.map((error: string, i: number) => (
                        <li key={i}>{error}</li>
                      ))}
                    </ul>
                  </div>
                )}
              </AlertDescription>
            </Alert>
          )}
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>Deployment Actions</CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="flex gap-4">
            <Button variant="secondary">Preview Changes</Button>
            <Button
              onClick={() => deployMutation.mutate()}
              disabled={deployMutation.isPending || !validationResult?.isValid}
            >
              {deployMutation.isPending && (
                <Loader2 className="mr-2 h-4 w-4 animate-spin" />
              )}
              Deploy Infrastructure
            </Button>
          </div>
        </CardContent>
      </Card>
    </div>
  );
};
```

### Migration Management Page

```typescript
// src/pages/Migration.tsx
import React, { useState } from 'react';
import { useMutation, useQuery } from '@tanstack/react-query';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Progress } from '@/components/ui/progress';
import { migrationService } from '@/services/migration';
import { Checkbox } from '@/components/ui/checkbox';

export const Migration: React.FC = () => {
  const [migrationId, setMigrationId] = useState<string | null>(null);
  const [selectedTypes, setSelectedTypes] = useState<string[]>([
    'scenarios',
    'bundles',
    'media',
    'blobs',
  ]);

  const startMigrationMutation = useMutation({
    mutationFn: (types: string[]) =>
      migrationService.startMigration(types, {}, {}),
    onSuccess: (id) => {
      setMigrationId(id);
    },
  });

  const { data: progress } = useQuery({
    queryKey: ['migration-progress', migrationId],
    queryFn: () => migrationService.getMigrationProgress(migrationId!),
    enabled: !!migrationId,
    refetchInterval: 2000, // Poll every 2 seconds
  });

  return (
    <div className="p-6 space-y-6">
      <h1 className="text-3xl font-bold">Data Migration</h1>

      <Card>
        <CardHeader>
          <CardTitle>Select Resources to Migrate</CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          {['scenarios', 'bundles', 'media', 'blobs'].map((type) => (
            <div key={type} className="flex items-center space-x-2">
              <Checkbox
                id={type}
                checked={selectedTypes.includes(type)}
                onCheckedChange={(checked) => {
                  setSelectedTypes(
                    checked
                      ? [...selectedTypes, type]
                      : selectedTypes.filter((t) => t !== type)
                  );
                }}
              />
              <label htmlFor={type} className="capitalize">
                {type}
              </label>
            </div>
          ))}

          <Button
            onClick={() => startMigrationMutation.mutate(selectedTypes)}
            disabled={startMigrationMutation.isPending || selectedTypes.length === 0}
          >
            Start Migration
          </Button>
        </CardContent>
      </Card>

      {progress && (
        <Card>
          <CardHeader>
            <CardTitle>Migration Progress</CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            {progress.map((item) => (
              <div key={item.type} className="space-y-2">
                <div className="flex justify-between">
                  <span className="capitalize">{item.type}</span>
                  <span>
                    {item.completed} / {item.total}
                  </span>
                </div>
                <Progress value={(item.completed / item.total) * 100} />
              </div>
            ))}
          </CardContent>
        </Card>
      )}
    </div>
  );
};
```

## Configuration

### Tauri Configuration

```json
// src-tauri/tauri.conf.json
{
  "build": {
    "beforeDevCommand": "pnpm dev",
    "beforeBuildCommand": "pnpm build",
    "devPath": "http://localhost:5173",
    "distDir": "../dist"
  },
  "package": {
    "productName": "Mystira Admin",
    "version": "0.1.0"
  },
  "tauri": {
    "allowlist": {
      "all": false,
      "shell": {
        "all": false,
        "execute": true,
        "sidecar": false,
        "open": false
      },
      "http": {
        "all": false,
        "request": true,
        "scope": [
          "https://dev-euw-app-mystora-admin-api.azurewebsites.net/**",
          "https://prod-wus-app-mystira-api.azurewebsites.net/**"
        ]
      },
      "fs": {
        "all": false,
        "readFile": true,
        "writeFile": true,
        "scope": ["$APPCONFIG/*"]
      }
    },
    "bundle": {
      "active": true,
      "identifier": "com.mystira.admin",
      "icon": [
        "icons/32x32.png",
        "icons/128x128.png",
        "icons/128x128@2x.png",
        "icons/icon.icns",
        "icons/icon.ico"
      ],
      "resources": [],
      "externalBin": [],
      "copyright": "",
      "category": "DeveloperTool",
      "shortDescription": "Mystira Admin Desktop Application",
      "longDescription": "Desktop application for managing Mystira infrastructure and data",
      "windows": {
        "certificateThumbprint": null,
        "digestAlgorithm": "sha256",
        "timestampUrl": ""
      }
    },
    "security": {
      "csp": "default-src 'self'; connect-src 'self' https://dev-euw-app-mystora-admin-api.azurewebsites.net https://prod-wus-app-mystira-api.azurewebsites.net; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'"
    },
    "updater": {
      "active": false
    },
    "windows": [
      {
        "title": "Mystira Admin",
        "width": 1200,
        "height": 800,
        "resizable": true,
        "fullscreen": false
      }
    ]
  }
}
```

## Deployment

### Building the Desktop Application

```bash
# Install dependencies
pnpm install

# Development mode
pnpm tauri dev

# Build for production
pnpm tauri build

# Output locations:
# - Windows: src-tauri/target/release/bundle/msi/
# - macOS: src-tauri/target/release/bundle/dmg/
# - Linux: src-tauri/target/release/bundle/appimage/
```

### Distribution

1. **Windows**: Distribute `.msi` installer
2. **macOS**: Distribute `.dmg` or publish to App Store
3. **Linux**: Distribute `.AppImage` or `.deb` package

## Security Considerations

### Authentication Security
- JWT tokens stored in OS keychain (Windows Credential Manager, macOS Keychain, Linux Secret Service)
- Never store tokens in localStorage or plain text files
- Implement automatic token refresh
- Clear tokens on logout

### API Communication
- All API calls over HTTPS only
- Validate SSL certificates
- Implement request timeout and retry logic
- Sanitize all user inputs

### Desktop Security
- Content Security Policy (CSP) restricts inline scripts
- HTTP scope limits API calls to approved domains
- File system access restricted to app config directory
- No shell command execution except whitelisted operations

## Future Enhancements

1. **Auto-update mechanism** using Tauri updater
2. **Multi-environment support** (dev, staging, prod)
3. **Offline mode** with local caching
4. **Real-time notifications** using WebSockets
5. **Telemetry and crash reporting**
6. **User preferences** and customization
7. **CLI integration** for power users
8. **Backup and restore** functionality

## Development Setup

### Prerequisites
- Node.js 18+
- pnpm 8+
- Rust 1.70+
- Tauri CLI

### Getting Started

```bash
# Clone repository
git clone https://github.com/phoenixvc/Mystira.App.git
cd Mystira.App/tools/mystira-admin-desktop

# Install dependencies
pnpm install

# Install Tauri CLI
cargo install tauri-cli

# Run in development mode
pnpm tauri dev

# Build for production
pnpm tauri build
```

## API Requirements

The Admin API must expose the following endpoints for the desktop app to function:

### Authentication
- `POST /adminapi/auth/login` - User login
- `POST /adminapi/auth/refresh` - Token refresh
- `POST /adminapi/auth/logout` - User logout

### Infrastructure
- `POST /adminapi/infrastructure/validate` - Validate templates
- `POST /adminapi/infrastructure/preview` - What-if analysis
- `POST /adminapi/infrastructure/deploy` - Deploy infrastructure
- `GET /adminapi/infrastructure/deployments/{id}` - Get deployment status

### Migration
- `POST /adminapi/migrations/start` - Start migration
- `GET /adminapi/migrations/{id}/progress` - Get progress
- `GET /adminapi/migrations/{id}/logs` - Get logs

### Dashboard
- `GET /adminapi/dashboard/stats` - Get statistics

### Scenarios
- `GET /adminapi/scenarios` - List scenarios
- `POST /adminapi/scenarios` - Create scenario
- `PUT /adminapi/scenarios/{id}` - Update scenario
- `DELETE /adminapi/scenarios/{id}` - Delete scenario

## Conclusion

This architecture provides a robust foundation for a desktop administration tool that integrates seamlessly with the Mystira Admin API. The Tauri framework offers native performance with web technologies, secure token storage, and cross-platform compatibility while maintaining a small application footprint.
