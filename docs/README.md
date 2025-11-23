# Mystira.App Documentation

Welcome to the Mystira.App documentation. This directory contains comprehensive guides for developers, administrators, and contributors.

## Quick Links

- **[Main README](../README.md)** - Project overview and getting started
- **[Setup Guides](setup/)** - Installation and configuration guides
- **[Feature Documentation](features/)** - Detailed feature documentation

## Setup Guides

### Email & Authentication

- **[Email Setup](setup/EMAIL_SETUP.md)** - Complete guide for email integration with Azure Communication Services
  - Quick start (no Azure required)
  - Azure Communication Services setup
  - Email template customization
  - Troubleshooting

## Feature Documentation

### Authentication

- **[Passwordless Signup](features/PASSWORDLESS_SIGNUP.md)** - Technical implementation of passwordless authentication
  - Architecture and flow
  - Backend implementation
  - Frontend integration
  - Security features

### Architecture

- **[Admin API Separation](features/ADMIN_API_SEPARATION.md)** - Admin and client API architecture
  - Project structure
  - API separation rationale
  - Deployment strategies
  - Endpoint documentation

## Project Structure

``` text
docs/
├── README.md                        # This file
├── setup/                           # Setup and configuration guides
│   └── EMAIL_SETUP.md              # Email integration setup
└── features/                        # Feature documentation
    ├── PASSWORDLESS_SIGNUP.md      # Passwordless authentication
    └── ADMIN_API_SEPARATION.md     # Admin API architecture
```

## API Documentation

- **[Client API](../src/Mystira.App.Api/README.md)** - Main client-facing API documentation
- **[Admin API](../src/Mystira.App.Admin.Api/README.md)** - Administrative API documentation
- **[Cosmos Console](../Mystira.App.CosmosConsole/README.md)** - Database reporting tool

## Getting Started

1. **New to Mystira?** Start with the [Main README](../README.md)
2. **Setting up development?** Check [Email Setup](setup/EMAIL_SETUP.md) for local development
3. **Understanding the architecture?** Read [Admin API Separation](features/ADMIN_API_SEPARATION.md)
4. **Implementing authentication?** See [Passwordless Signup](features/PASSWORDLESS_SIGNUP.md)

## Contributing

When adding new documentation:

1. Place setup/configuration guides in `docs/setup/`
2. Place feature documentation in `docs/features/`
3. Update this README.md with links to new documentation
4. Keep documentation focused and avoid redundancy

## Documentation Standards

- Use clear, descriptive titles
- Include code examples where applicable
- Provide both quick start and detailed sections
- Link to related documentation
- Keep examples up-to-date with latest code

## Support

For questions or issues:

- Check the relevant documentation first
- Review [Main README](../README.md) for general information
- Open an issue on GitHub for bugs or feature requests
- Consult API documentation for endpoint details
