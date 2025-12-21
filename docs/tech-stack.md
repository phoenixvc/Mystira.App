# Technology Stack Overview

## Frontend
- **Framework:** Blazor WebAssembly (.NET 9.0)
- **State Management:** Scoped services, IndexedDB (via JS Interop)
- **Offline:** Service Workers, `imageCacheManager.js`
- **Resilience:** Polly (Retry, Circuit Breaker, Timeout)
- **UI:** CSS Grid/Flexbox, Custom Violet theme

## Backend
- **Framework:** ASP.NET Core (.NET 9.0)
- **Messaging:** MediatR v12.4.1 (CQRS)
- **Validation:** FluentValidation
- **Mapping:** AutoMapper (or similar inferred from DTO patterns)

## Data & Storage
- **Primary DB:** Azure Cosmos DB (NoSQL)
- **ORM:** Entity Framework Core 9.0 (Cosmos Provider)
- **Blob Storage:** Azure Blob Storage for media assets
- **Consistency:** ValueConverters for complex nested types

## Infrastructure & DevOps
- **Hosting:** Azure App Service (API), Azure Static Web Apps (PWA)
- **Secrets:** Azure Key Vault, User Secrets
- **Hooks:** Husky.Net for pre-commit formatting
- **CI/CD:** GitHub Actions (inferred from `README.md`)

## Testing
- **Unit:** xUnit
- **Assertions:** FluentAssertions
- **Mocking:** Moq
- **Scope:** Domain logic, Application Queries/Commands, JS Interop mocks
