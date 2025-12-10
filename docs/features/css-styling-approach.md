# CSS Styling Approach for Mystira Blazor Application

## Overview

This document outlines the recommended CSS styling approach for the Mystira Blazor WebAssembly application. After evaluating different styling methodologies, we recommend using **Blazor Scoped CSS** as the primary approach for component styling.

## Recommendation: Use Blazor Scoped CSS

### Why Scoped CSS?

Blazor Scoped CSS is the recommended approach for styling Blazor components for the following reasons:

1. **Native Blazor Support**: Scoped CSS is built into Blazor with zero configuration required
2. **Already in Use**: The project already uses scoped CSS (e.g., `DiceRoller.razor.css`)
3. **Automatic Scope Isolation**: Styles are automatically scoped to components, preventing CSS conflicts
4. **Build-time Processing**: CSS is processed at build time and bundled efficiently
5. **No Additional Dependencies**: Works out-of-the-box with no extra packages or tooling

### Why NOT CSS Modules?

CSS Modules are **NOT recommended** for Blazor applications because:

1. **JavaScript Framework Pattern**: CSS Modules are designed for JavaScript frameworks (React, Vue, Angular)
2. **No Native Blazor Support**: Requires additional tooling and configuration to work with Blazor
3. **Unnecessary Complexity**: Adds build complexity without providing benefits over Scoped CSS
4. **Non-standard for Blazor**: Goes against Blazor community best practices

## Implementation Guide

### Scoped CSS File Structure

For each Blazor component, create a corresponding scoped CSS file:

```
Components/
├── MyComponent.razor
└── MyComponent.razor.css
```

### Example: Creating a Scoped CSS File

**Component File: `Components/BundleCard.razor`**
```razor
<div class="bundle-card">
    <h3 class="bundle-title">@Title</h3>
    <p class="bundle-description">@Description</p>
</div>

@code {
    [Parameter] public string Title { get; set; }
    [Parameter] public string Description { get; set; }
}
```

**Scoped CSS File: `Components/BundleCard.razor.css`**
```css
/* These styles are automatically scoped to BundleCard component only */
.bundle-card {
    border: 1px solid #ddd;
    border-radius: 8px;
    padding: 16px;
    transition: transform 0.2s;
}

.bundle-card:hover {
    transform: translateY(-4px);
    box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
}

.bundle-title {
    color: #8B5CF6;
    font-size: 1.5rem;
    margin-bottom: 8px;
}

.bundle-description {
    color: #6B7280;
    line-height: 1.6;
}
```

### How Scoped CSS Works

At build time, Blazor:
1. Processes each `.razor.css` file
2. Generates unique scope identifiers (e.g., `b-abc123`)
3. Appends scope identifiers to CSS selectors
4. Applies scope attributes to component HTML elements
5. Bundles all scoped CSS into `{ProjectName}.styles.css`

**Generated HTML:**
```html
<div class="bundle-card" b-abc123>
    <h3 class="bundle-title" b-abc123>Adventure Title</h3>
    <p class="bundle-description" b-abc123>Description text</p>
</div>
```

**Generated CSS:**
```css
.bundle-card[b-abc123] {
    border: 1px solid #ddd;
    /* ... */
}
```

## Current Approach in Mystira

### Global CSS
Location: `src/Mystira.App.PWA/wwwroot/css/app.css`

Global CSS is used for:
- Design system foundations (colors, typography, spacing)
- Bootstrap customizations and overrides
- Utility classes used across multiple components
- Layout and grid systems
- Animation keyframes

### Scoped CSS
Currently Used: `src/Mystira.App.PWA/Components/DiceRoller.razor.css`

Scoped CSS should be used for:
- Component-specific styles
- Styles that should not leak to other components
- Component state variations (hover, active, disabled)
- Complex component styling that benefits from isolation

### Migration Strategy

The current approach uses global CSS for all components. For better maintainability, consider extracting component-specific styles:

**Current Approach (Global CSS):**
```css
/* In app.css - applies globally */
.bundle-card {
    border: 1px solid #ddd;
}
```

**Future Approach (Scoped CSS):**
```css
/* In BundleCard.razor.css - scoped to component */
.bundle-card {
    border: 1px solid #ddd;
}
```

This migration can be done gradually without breaking existing functionality.

## Best Practices

### 1. Use Scoped CSS for Component-Specific Styles
```css
/* BundleCard.razor.css */
.card-container {
    /* Component-specific styles */
}
```

### 2. Use Global CSS for Shared Utilities
```css
/* app.css */
.text-primary-custom {
    color: #8B5CF6;
}
```

### 3. Leverage CSS Variables for Theming
```css
/* app.css - Define theme variables globally */
:root {
    --primary-color: #8B5CF6;
    --secondary-color: #10B981;
    --text-muted: #6B7280;
}

/* Component.razor.css - Use variables in scoped styles */
.component-title {
    color: var(--primary-color);
}
```

### 4. Avoid Deep Selectors (::deep)
```css
/* ❌ Avoid - breaks scoping */
.parent ::deep .child {
    color: red;
}

/* ✅ Prefer - use parameters to pass styling props */
.parent {
    /* Style parent only */
}
```

### 5. Use CSS Classes, Not Inline Styles
```razor
<!-- ❌ Avoid inline styles -->
<div style="color: red; padding: 16px;">Content</div>

<!-- ✅ Use CSS classes -->
<div class="error-message">Content</div>
```

## Accessibility Considerations

When styling components, ensure accessibility:

```css
/* Keyboard focus indicators */
.button:focus-visible {
    outline: 2px solid var(--primary-color);
    outline-offset: 2px;
}

/* Sufficient color contrast (WCAG AA) */
.text-primary {
    color: #8B5CF6; /* Ensure contrast ratio > 4.5:1 */
}

/* Touch-friendly sizes (minimum 44x44px) */
.touch-target {
    min-width: 44px;
    min-height: 44px;
}
```

## Performance Optimization

### CSS Bundling
Blazor automatically bundles all scoped CSS files into a single file:
```
_content/{ProjectName}/{ProjectName}.styles.css
```

### Critical CSS
Keep global `app.css` lean to improve initial load:
- Move component-specific styles to scoped CSS files
- Use CSS minification in production builds
- Leverage browser caching with proper cache headers

### Avoid Expensive Selectors
```css
/* ❌ Expensive - descendant selector */
.container div span a {
    color: blue;
}

/* ✅ Efficient - single class */
.link {
    color: blue;
}
```

## Example: Extracting Styles from Global to Scoped

### Before (Global CSS in app.css)
```css
.hero-section {
    background: linear-gradient(135deg, #f5f3ff 0%, #faf5ff 100%);
    padding: 3rem 1rem;
}

.hero-badge {
    background: linear-gradient(135deg, #fbbf24 0%, #f59e0b 100%);
    color: white;
}
```

### After (Scoped CSS in HeroSection.razor.css)
```css
/* HeroSection.razor.css */
.hero-section {
    background: linear-gradient(135deg, #f5f3ff 0%, #faf5ff 100%);
    padding: 3rem 1rem;
}

.hero-badge {
    background: linear-gradient(135deg, #fbbf24 0%, #f59e0b 100%);
    color: white;
}
```

**Benefits:**
- Styles are isolated to HeroSection component
- No risk of naming conflicts with other components
- Easier to maintain and refactor
- Component and its styles can be moved/deleted together

## IDE Support

Visual Studio and Visual Studio Code provide excellent support for Scoped CSS:

- **IntelliSense**: CSS autocomplete and suggestions
- **Go to Definition**: Navigate from HTML class to CSS definition
- **Refactoring**: Rename classes across Razor and CSS files
- **Error Detection**: CSS syntax validation

## Testing Scoped Styles

### Manual Testing
1. Inspect element in browser DevTools
2. Verify scope attribute is applied (e.g., `b-abc123`)
3. Check computed styles are correct
4. Test in different browsers

### Automated Testing
Use Playwright or similar tools to test rendered styles:
```csharp
[Fact]
public async Task BundleCard_AppliesScopedStyles()
{
    await Page.GotoAsync("/");
    var card = await Page.QuerySelectorAsync(".bundle-card");
    var color = await card.EvaluateAsync<string>("el => getComputedStyle(el).borderColor");
    Assert.Equal("rgb(221, 221, 221)", color);
}
```

## Troubleshooting

### Issue: Scoped Styles Not Applied
**Solution:** Ensure the `.razor.css` file is in the same directory and has the same base name as the `.razor` file.

### Issue: Styles Affecting Other Components
**Solution:** Check for `::deep` selectors or global styles in `app.css` that might be overriding scoped styles.

### Issue: Build Not Generating Scoped CSS Bundle
**Solution:** Clean and rebuild the project:
```bash
dotnet clean
dotnet build
```

## References

- [Blazor CSS Isolation Documentation](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/css-isolation)
- [CSS Scoping Module Level 1 Spec](https://www.w3.org/TR/css-scoping-1/)
- [Mystira Best Practices Guide](../best-practices.md)

## Conclusion

**Use Blazor Scoped CSS** for component styling in the Mystira application. It provides the best balance of:
- **Simplicity**: No additional configuration needed
- **Maintainability**: Component styles co-located with components
- **Performance**: Efficient bundling and caching
- **Standards**: Follows Blazor community best practices

Avoid CSS Modules as they are designed for JavaScript frameworks and add unnecessary complexity to Blazor projects. The current approach of global CSS in `app.css` works well, but gradually migrating component-specific styles to scoped CSS files will improve long-term maintainability.
