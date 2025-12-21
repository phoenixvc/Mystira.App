# Best Practices Benchmark

## Framework & Patterns
- **Hexagonal Architecture:** Strictly decouple Domain from Infrastructure using Ports (Interfaces).
- **CQRS:** Enforce separation of Read (Queries) and Write (Commands) models.
- **Dependency Injection:** Use appropriate lifetimes (Singleton for caches, Scoped for HTTP clients).

## Security
- **COPPA:** Minimal PII collection; strictly no child-identifiable tracking.
- **OWASP:** Protection against Injection, Broken Auth, and XSS.
- **Sensitive Data:** Never log PII; use Key Vault for all connection strings.

## Performance
- **PWA:** Efficient caching of static and binary assets; lazy load large modules.
- **Cosmos DB:** Optimize RU usage via careful partition key selection and indexing.
- **Blazor:** Optimize JS interop calls; avoid unnecessary re-renders.

## Testing
- **Coverage:** Target >80% for Domain and Application layers.
- **Strategies:** Use Red-Green-Refactor; ensure regression tests for every bug fix.
- **Negative cases:** Explicitly test boundary conditions and error paths.

## Error Handling & Observability
- **Policies:** Uniform use of Polly for network resilience.
- **Logging:** Structural logging with correlation IDs; separate Audit logs from Debug logs.
- **UX:** Clear user-facing error messages; no stack traces in the UI.
