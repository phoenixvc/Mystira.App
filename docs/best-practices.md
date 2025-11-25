# Mystira Development Best Practices

This document outlines the key best practices and standards for developing the Mystira application. Following these guidelines helps us maintain a high-quality, secure, and maintainable codebase.

## 1. Architecture & Code Organization

- **Separation of Concerns (SoC):** We use a multi-layered architecture. Adhere to the established project structure:
  - `Mystira.App.Domain`: Core business models and logic. Should have no dependencies on other layers.
  - `Mystira.App.Api`: The backend API. Contains controllers, services, and data access logic.
  - `Mystira.App.PWA`: The Blazor WebAssembly frontend. Contains UI components, pages, and client-side logic.
  - `Mystira.App.Infrastructure.Azure`: Services for interacting with Azure (e.g., Blob Storage, Email).
- **Dependency Injection (DI):** Use DI for all services. Register services in `Program.cs` with the appropriate lifetime (`Singleton`, `Scoped`, `Transient`).
- **Configuration:** All secrets and environment-specific settings must be loaded from configuration (`appsettings.json`, environment variables, or a secure store like Azure Key Vault). **Never hardcode secrets.**

## 2. Security

- **Input Validation:** Validate all incoming data at the API controller level. Use data annotations and FluentValidation where appropriate.
- **Authentication & Authorization:** Secure all sensitive API endpoints with `[Authorize]` attributes. Use the provided JWT-based authentication.
- **CORS:** The Cross-Origin Resource Sharing (CORS) policy must be a strict whitelist of allowed origins. Do not use wildcard subdomains.
- **Dependency Management:** Regularly check for and update outdated or vulnerable NuGet packages.
- **Cryptographically Secure Randomness:** For any security-sensitive operations like generating verification codes, always use `System.Security.Cryptography.RandomNumberGenerator`.

## 3. Performance & Scalability

- **Efficient Database Queries:** Use asynchronous EF Core queries (`...Async()`) to avoid blocking threads. Write efficient queries to minimize data transfer and avoid N+1 problems.
- **Blazor Performance:**
  - Minimize component re-renders. Use `@key` for lists and override `ShouldRender` for complex components where appropriate.
  - Use lazy loading for large assets or components that are not immediately needed.
- **API Responsiveness:** Ensure all API endpoints are responsive and return data in a timely manner. Use caching for data that doesn't change often.

## 4. Testing

- **Unit Tests:** All business logic in the `Domain` project should be covered by unit tests.
- **Integration Tests:** The `Api` project should have integration tests that cover the main API endpoints and their interactions with the database and other services.
- **Test Coverage:** Aim for high test coverage, especially for critical business logic and security-sensitive code.

## 5. Frontend (PWA & UI/UX)

- **Component-Based Design:** Build reusable Blazor components for UI elements.
- **Design System:** Adhere to the established design system (colors, typography, spacing) defined in `wwwroot/css/app.css`.
- **CSS Styling:** Use Blazor Scoped CSS for component-specific styles. See [CSS Styling Approach](features/CSS_STYLING_APPROACH.md) for detailed guidance.
  - Create `.razor.css` files alongside components for scoped styles
  - Use global CSS (`app.css`) only for shared utilities and design system foundations
  - Avoid CSS Modules (designed for JavaScript frameworks, not Blazor)
- **Accessibility (A11y):** Ensure all UI components are accessible and meet WCAG 2.1 AA standards. This includes:
  - Using semantic HTML.
  - Providing alternative text for images.
  - Ensuring sufficient color contrast.
  - Making all interactive elements keyboard-navigable with clear focus indicators.

## 6. Documentation

- **Code Comments:** Add comments to explain complex or non-obvious parts of the code.
- **READMEs:** Keep the main `README.md` and any project-specific READMEs up-to-date with the latest information.
- **Commit Messages:** Write clear and descriptive commit messages following the [Conventional Commits](https) specification.
