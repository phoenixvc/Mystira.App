# Design Specifications

This document outlines the design system for the Mystira application, as reverse-engineered from the Blazor PWA implementation.

## Color Palette

### Primary Colors
- **Primary:** `#7c3aed`
- **Primary Hover:** `#6d28d9`
- **Secondary:** `#1F2937`

### Semantic Colors
- **Success:** `#10B981`
- **Danger:** `#EF4444`
- **Warning:** `#F59E0B`
- **Info:** `#3B82F6`

### Neutral Colors
- **Light:** `#F9FAFB`
- **Dark:** `#111827`

### Theme Colors (Light/Dark)
- **Background (Light):** `#f8fafc`
- **Foreground (Light):** `#111827`
- **Muted (Light):** `#6b7280`
- **Card (Light):** `rgba(255,255,255,0.85)`
- **Border (Light):** `rgba(17,24,39,0.08)`
- **Accent (Light):** `#22c55e`
- **Background (Dark):** `#0f172a`
- **Foreground (Dark):** `#f1f5f9`
- **Muted (Dark):** `#94a3b8`
- **Card (Dark):** `rgba(30, 41, 59, 0.85)`
- **Border (Dark):** `rgba(148, 163, 184, 0.1)`
- **Accent (Dark):** `#34d399`

## Typography

- **Font Family:** 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif
- **Base Font Size:** 1rem (16px)
- **Line Height:** 1.7 for paragraphs, 1.3 for headings
- **Font Weight:** 600 for headings and buttons

## Spacing

The project does not use a formal spacing system (e.g., 4px grid), but common spacing values are used throughout the application. More analysis is needed to document a consistent spacing scale.

## UI Components

The application uses a set of custom-styled UI components, including:
- Buttons
- Cards
- Modals
- Forms (inputs, selects, etc.)
- Alerts

## Design-Code Consistency Assessment

The current implementation demonstrates a consistent design language, with a well-defined color palette and typography. However, there are some areas for improvement:

- **Spacing:** The lack of a formal spacing system can lead to inconsistencies in layouts and component spacing.
- **Component Variants:** While base components are styled, there is a lack of documentation for different component variants (e.g., button sizes, alert styles).
- **Accessibility:** While some accessibility features are implemented (e.g., focus styles), a more thorough review is needed to ensure WCAG compliance.
