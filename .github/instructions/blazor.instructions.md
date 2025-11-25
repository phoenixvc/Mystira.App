---
applyTo: "**/*.razor"
---

# Blazor Component Guidelines

## Component Structure

- Use `@code` blocks for component logic
- Inject services using `@inject` directive
- Keep components focused and single-purpose

## Styling

- Use **Blazor Scoped CSS** (`.razor.css` files)
- DO NOT use CSS Modules
- Global styles go in `wwwroot/css/app.css`
- Use CSS custom properties for theming

## Performance

- Use `@key` directive for list rendering
- Implement `ShouldRender()` to prevent unnecessary re-renders
- Lazy load non-critical components
- Consider offline/PWA functionality

## Accessibility (WCAG 2.1 AA)

- Use semantic HTML elements
- Include ARIA labels for interactive elements
- Ensure keyboard navigation works
- Maintain sufficient color contrast

## Error Handling

- Handle exceptions gracefully
- Show user-friendly error messages
- Log errors with context (anonymize PII)
