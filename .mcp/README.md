# Model Context Protocol (MCP) Configuration

This directory contains the Model Context Protocol (MCP) configuration for the Mystira.App repository. MCP is a standard that allows AI assistants to access tools, resources, and context about the repository.

## What is MCP?

Model Context Protocol (MCP) is an open protocol that enables AI assistants to:
- Access repository files and directories
- Execute build, test, and development commands
- Query GitHub issues and pull requests
- Get structured context about the project architecture

## Configuration Files

### `config.json`

The main MCP configuration file that defines:

- **MCP Servers**: Pre-configured servers for filesystem, GitHub, and Git operations
- **Resources**: Key documentation files and their locations
- **Prompts**: Reusable prompt templates for common tasks
- **Tools**: Command-line tools for building, testing, and running the application
- **Context**: Project metadata and architectural principles

### `.github/mcp.json`

The GitHub Copilot coding agent MCP configuration. This file is automatically used by GitHub Copilot's coding agent when working with this repository.

## Using MCP with AI Assistants

### GitHub Copilot Coding Agent

The `.github/mcp.json` file is automatically picked up by GitHub Copilot's coding agent. No additional configuration is needed.

**Adding MCP Secrets:**

For servers that require authentication (like the GitHub server), add secrets in your repository settings:
1. Go to Settings → Secrets and variables → Copilot
2. Add secrets prefixed with `COPILOT_MCP_` (e.g., `COPILOT_MCP_GITHUB_TOKEN`)

## Available Tools

The MCP configuration provides these development tools:

- **build**: Build the entire solution
- **test**: Run all tests
- **format**: Format code according to project standards
- **run-api**: Start the public API
- **run-admin-api**: Start the admin API
- **run-pwa**: Start the Blazor PWA

## Key Resources

The configuration exposes these important resources:

- **documentation**: `/docs` directory with comprehensive project documentation
- **readme**: Main project overview
- **architecture-rules**: Strict architectural rules for the codebase
- **best-practices**: Development standards and guidelines
- **contributing**: Contribution guidelines
- **solution**: Visual Studio solution file

## Prompts

Pre-defined prompts for common development tasks:

### `architecture-check`
Verify that code follows Hexagonal/Clean Architecture principles.

**Usage**: "Check if this code follows our architecture rules: [paste code]"

### `create-api-endpoint`
Generate a new API endpoint following project patterns.

**Usage**: "Create an API endpoint for [entity] with [action] action"

### `create-use-case`
Generate a new use case in the Application layer.

**Usage**: "Create a use case for [name] that [description]"

## Project Context

The MCP configuration includes metadata about the project:

- **Project Type**: Monorepo
- **Architecture**: Hexagonal/Clean Architecture
- **Primary Language**: C#
- **Framework**: .NET 9
- **Frontend**: Blazor WebAssembly
- **Database**: Azure Cosmos DB
- **Cloud Platform**: Azure

### Key Principles

1. Strict layer separation (Domain, Application, Infrastructure, API)
2. No business logic in controllers
3. Dependency injection throughout
4. Async/await for all I/O operations
5. Security-first approach
6. PII awareness and protection

## Troubleshooting

### MCP Servers Not Loading

- Ensure Node.js and npm are installed
- Check that the file paths in the configuration are correct
- Verify that your GitHub token (if using GitHub server) has the correct permissions

### Commands Not Working

- Ensure .NET 9 SDK is installed
- Verify you're in the correct working directory
- Check that all project dependencies are restored

## Further Reading

- [MCP Specification](https://modelcontextprotocol.io/)
- [GitHub Copilot Instructions](../.github/copilot-instructions.md)
- [Project README](../README.md)
- [Architecture Rules](../docs/architecture/ARCHITECTURAL_RULES.md)
