# AI Assistant Configuration Guide

This repository includes comprehensive configurations to enhance AI-assisted development through GitHub Copilot and the Model Context Protocol (MCP).

## Overview

We provide two complementary AI assistance configurations:

1. **GitHub Copilot Instructions** - Context-aware code suggestions
2. **Model Context Protocol (MCP) Configuration** - Repository tools and resources access

## GitHub Copilot Instructions

### Location

`.github/copilot-instructions.md`

### What It Does

This file provides GitHub Copilot with comprehensive context about:

- **Repository architecture** (Hexagonal/Clean Architecture)
- **Technology stack** (.NET 9, Blazor, Cosmos DB, Azure)
- **Coding standards** and best practices
- **Project structure** and dependency rules
- **Common development patterns**
- **Security requirements** and PII handling

### How It Works

GitHub Copilot automatically reads this file when:
- Making code suggestions
- Generating new code
- Answering questions about the repository
- Providing explanations

No setup required if you have GitHub Copilot installed in VS Code, Visual Studio, or other supported IDEs.

### Benefits

- **Contextual Suggestions**: Code suggestions follow project architecture patterns
- **Style Consistency**: Generated code matches existing code style
- **Layer Compliance**: Suggestions respect architectural boundaries
- **Security Awareness**: Copilot understands security requirements and PII concerns

## Model Context Protocol (MCP)

### Location

`.mcp/config.json` and `.mcp/README.md`

### What It Is

MCP is an open protocol that allows AI assistants to:
- Access repository files and structure
- Execute development commands (build, test, run)
- Query GitHub issues and pull requests
- Get structured project context

### What It Provides

1. **MCP Servers**
   - **Filesystem**: Browse and read repository files
   - **GitHub**: Query issues, PRs, and repository metadata
   - **Git**: Execute git operations

2. **Resources**
   - Quick access to key documentation files
   - Architecture rules and best practices
   - Contributing guidelines

3. **Tools**
   - `build`: Build the entire solution
   - `test`: Run all tests
   - `format`: Format code
   - `run-api`: Start the public API
   - `run-admin-api`: Start the admin API
   - `run-pwa`: Start the Blazor PWA

4. **Prompts**
   - Architecture compliance checking
   - API endpoint generation
   - Use case creation

### Setup

#### For Claude Desktop

1. Open Claude Desktop configuration:
   - **macOS**: `~/Library/Application Support/Claude/claude_desktop_config.json`
   - **Windows**: `%APPDATA%\Claude\claude_desktop_config.json`
   - **Linux**: `~/.config/Claude/claude_desktop_config.json`

2. Add the Mystira MCP servers:

```json
{
  "mcpServers": {
    "mystira-filesystem": {
      "command": "npx",
      "args": [
        "-y",
        "@modelcontextprotocol/server-filesystem",
        "/absolute/path/to/Mystira.App"
      ]
    },
    "mystira-github": {
      "command": "npx",
      "args": ["-y", "@modelcontextprotocol/server-github"],
      "env": {
        "GITHUB_PERSONAL_ACCESS_TOKEN": "<your-github-token>"
      }
    },
    "mystira-git": {
      "command": "npx",
      "args": ["-y", "@modelcontextprotocol/server-git"],
      "cwd": "/absolute/path/to/Mystira.App"
    }
  }
}
```

3. Replace placeholders:
   - `/absolute/path/to/Mystira.App` → Your local repository path
   - `<your-github-token>` → Your GitHub Personal Access Token

4. Restart Claude Desktop

#### For Other MCP-Compatible AI Assistants

Refer to your AI assistant's documentation for loading MCP configurations. The `config.json` follows the standard MCP schema.

### Benefits

- **Efficient Navigation**: Quickly access any file or documentation
- **Command Execution**: Build, test, and run commands directly
- **Contextual Understanding**: AI has deep knowledge of project structure
- **Consistent Patterns**: AI can suggest code following project templates

## Usage Examples

### With GitHub Copilot

**Example 1: Creating an API Controller**

```
// Type: "Create a controller for managing user badges"
// Copilot will suggest code that:
// - Uses correct routing pattern (/api/[controller])
// - Delegates to use cases (no business logic in controller)
// - Follows async/await patterns
// - Includes proper authorization attributes
```

**Example 2: Creating a Use Case**

```
// Type: "Create a use case for creating a new game session"
// Copilot will suggest:
// - Interface in the correct namespace
// - Implementation with repository injection
// - Proper async patterns
// - Input/output models
```

### With MCP (Claude Desktop)

**Example 1: Understanding Architecture**

```
User: "Show me how game sessions are created in this codebase"

Claude (using MCP):
- Reads architecture documentation
- Finds GameSession controller
- Locates CreateGameSession use case
- Explains the flow with actual code examples
```

**Example 2: Running Tests**

```
User: "Run the tests for the Admin API"

Claude (using MCP):
- Executes: dotnet test tests/Mystira.App.Admin.Api.Tests/
- Shows results
- If failures, reads test output and suggests fixes
```

**Example 3: Checking Architecture Compliance**

```
User: "Is this controller following our architecture rules?" [paste code]

Claude (using MCP):
- Reads ARCHITECTURAL_RULES.md
- Analyzes the code
- Points out any violations
- Suggests corrections
```

## Best Practices

### For Developers

1. **Keep Instructions Updated**: When architecture changes, update `.github/copilot-instructions.md`
2. **Review Suggestions**: Always review AI-generated code for correctness
3. **Use Prompts**: Leverage predefined MCP prompts for common tasks
4. **Test Generated Code**: Run tests to verify AI suggestions

### For Maintainers

1. **Document New Patterns**: Add new patterns to copilot-instructions.md
2. **Update MCP Resources**: Keep resource URIs current as files move
3. **Add Useful Tools**: Extend MCP config with new development commands
4. **Version Control**: Commit changes to AI configurations alongside code

## Troubleshooting

### GitHub Copilot Not Using Instructions

- Ensure you're using a recent version of GitHub Copilot
- Check that `.github/copilot-instructions.md` is committed to the repository
- Try reloading the IDE window

### MCP Servers Not Loading

- Verify Node.js and npm are installed
- Check file paths are absolute and correct
- Ensure GitHub token has necessary permissions
- Check Claude Desktop logs for errors

### Commands Not Working in MCP

- Ensure .NET 9 SDK is installed
- Verify you're in the correct working directory
- Run `dotnet restore` to restore dependencies
- Check that paths in MCP config are correct

## Further Reading

- [GitHub Copilot Documentation](https://docs.github.com/en/copilot)
- [Model Context Protocol Specification](https://modelcontextprotocol.io/)
- [Project Architecture Rules](docs/architecture/ARCHITECTURAL_RULES.md)
- [Development Best Practices](docs/best-practices.md)
- [Contributing Guide](CONTRIBUTING.md)

## Feedback

If you have suggestions for improving these AI configurations:

1. Open an issue describing the improvement
2. Submit a PR with your proposed changes
3. Update documentation to reflect new capabilities

The goal is to make AI assistance as helpful as possible while maintaining code quality and architectural integrity.
