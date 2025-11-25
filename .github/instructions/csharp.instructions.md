---
applyTo: "**/*.cs"
---

# C# Code Guidelines

## Architecture Compliance

Follow **Hexagonal/Clean Architecture** strictly:

1. **Domain Layer** (`src/Mystira.App.Domain/`)
   - Pure business logic and domain models
   - No external dependencies
   - Rich domain models with behavior

2. **Application Layer** (`src/Mystira.App.Application/`)
   - Use cases only (one per business action)
   - Define repository interfaces (ports)
   - No infrastructure dependencies

3. **Infrastructure Layer** (`src/Mystira.App.Infrastructure.*/`)
   - Repository implementations
   - External service adapters
   - Third-party integrations

4. **API Layer** (`src/Mystira.App.Api/`, `src/Mystira.App.Admin.Api/`)
   - Controllers ONLY - NO business logic
   - Route to use cases, not services directly

## Code Style

- Use `async/await` for all I/O operations
- Private fields use `_camelCase` naming
- Use nullable reference types
- Validate input at API boundaries
- Never hardcode secrets or connection strings

## Security

- Always use `[Authorize]` for protected endpoints
- Admin endpoints require `[Authorize(Roles = "Admin")]`
- Consider COPPA compliance for child data operations
- Use `System.Security.Cryptography.RandomNumberGenerator` for crypto

## Testing

- Add unit tests for all use cases
- Use xUnit with Moq for mocking
- Follow Arrange-Act-Assert pattern
