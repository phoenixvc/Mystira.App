# Development Launch Scripts

PowerShell scripts to launch the Mystira.App development environment.

## Quick Start

From the repository root, run:
```powershell
.\start.ps1
```

Or use the full path:
```powershell
.\scripts\start-all.ps1
```

## Individual Scripts

### Start Services

- **`start-api.ps1`** - Starts the main API
  - HTTPS: https://localhost:7096
  - HTTP: http://localhost:5260
  - Swagger: https://localhost:7096/swagger

- **`start-admin-api.ps1`** - Starts the Admin API
  - HTTPS: https://localhost:7096
  - HTTP: http://localhost:5260
  - Admin UI: https://localhost:7096/admin

- **`start-pwa.ps1`** - Starts the PWA frontend
  - HTTP: http://localhost:7000
  - HTTPS: https://localhost:7000

### Open Browsers

- **`open-swagger.ps1`** - Opens Swagger UI in default browser
- **`open-admin-ui.ps1`** - Opens Admin UI in default browser
- **`open-pwa.ps1`** - Opens PWA in default browser

### Master Script

- **`start-all.ps1`** - Starts all services and opens browsers automatically

## Usage Examples

### Start all services (recommended)
```powershell
# From repository root
.\start.ps1

# Or from scripts directory
.\scripts\start-all.ps1
```

### Start individual services
```powershell
# Terminal 1 - API
.\scripts\start-api.ps1

# Terminal 2 - Admin API
.\scripts\start-admin-api.ps1

# Terminal 3 - PWA
.\scripts\start-pwa.ps1
```

### Open browsers after services are running
```powershell
.\scripts\open-swagger.ps1
.\scripts\open-admin-ui.ps1
.\scripts\open-pwa.ps1
```

## Ports Summary

| Service | HTTP Port | HTTPS Port | UI/Swagger |
|---------|-----------|------------|------------|
| API | 5260 | 7096 | /swagger |
| Admin API | 5260 | 7096 | /admin, /swagger |
| PWA | 7000 | 7000 | / |

**Note:** API and Admin API use the same ports and cannot run simultaneously. Run only one at a time, or configure different ports in their respective `launchSettings.json` files.

## Notes

- Make sure you have the .NET SDK installed
- The scripts assume you're running from the repository root
- Services may take a few seconds to start up
- If ports are already in use, you'll need to stop the existing processes first
- Each service runs in its own minimized PowerShell window

