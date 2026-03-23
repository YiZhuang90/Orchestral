# Orchestral Font Design Guide

## Purpose

This guide defines the baseline typography for Orchestral device panels and future OS surfaces. The goal is a clean, technical, screen-first visual language that feels modern, scientific, and consistent across hardware integration, runtime control, and data views.

## Primary Font Pair

- `Manrope`
  Use for product identity, panel titles, section headers, key metrics, and primary navigation.
- `Inter`
  Use for controls, form text, labels, status text, body copy, and dense technical information.

## Why This Pair

- `Manrope` has a geometric and slightly distinctive structure that gives Orchestral a modern, high-precision identity.
- `Inter` is optimized for screen readability and performs well at small UI sizes, which is essential for technical panels and data-heavy views.

## Usage Rules

### Use `Manrope` for:

- product or device title in the top shell
- major panel headers
- section titles that define structure
- large numeric callouts and key metrics
- primary navigation tabs

### Use `Inter` for:

- dropdowns, buttons, checkboxes, and inputs
- body text and helper text
- axis labels and plot labels
- status bars and diagnostics text
- small technical labels and dense UI copy

## Weight Guidance

- `Manrope`
  - use regular weight by default in operational UI
  - use stronger weights sparingly and only when hierarchy is unclear without them
- `Inter`
  - `400` for body text
  - avoid unnecessary bold emphasis in controls and telemetry

## Size Guidance

Orchestral should use a small layered type scale rather than many one-off sizes.

- `ShellTitle = 16 px`
  Use for the top shell title in `Manrope`
- `DisplayValue = 20 px`
  Use for the main live metric line in `Manrope`
- `SectionHeader = 16 px`
  Use for section titles and stat values in `Manrope`
- `Body = 12 px`
  Use for controls, helper text, status labels, body copy, and operational text in `Inter`
- `Micro = 12 px`
  Use for axes, dense telemetry labels, and very small utility text in `Inter`

For the first-generation device panels, use this tight three-role size system and avoid intermediate one-off values unless a proven readability issue forces a change.

## Practical Rule

If the text is about identity, hierarchy, or emphasis, prefer `Manrope`.
If the text is about operation, data, or readability under repeated use, prefer `Inter`.

## Text Case Policy

Orchestral uses sentence case for device panels by default.

### Use `sentence case` for:

- device titles when rendered inside the panel
- section headings
- tab labels
- buttons
- card titles
- helper text
- subtitles
- diagnostics text
- status descriptions
- explanatory copy

Examples:

- `Sensor type`
- `Wire count`
- `Read once`
- `Peak high`
- `No samples yet`

### Avoid broad `ALL CAPS`

Do not use all caps for normal operational UI unless a specific surface intentionally needs an instrument-label look. The default Orchestral device-panel style is calm, precise, and product-like rather than loud.

## Initial Orchestral Baseline

The PT-104 hardware integration window is the first reference implementation of this typography system. Future device panels should inherit this pairing and only diverge with a deliberate design-system decision.

## Radius System

Orchestral uses a simple two-radius system for the first generation of device panels.

- `Radius S`
  Use for buttons, dropdown containers, and other interactive controls.
- `Radius L`
  Use for cards, chart containers, side panels, stat panels, and other grouped surfaces.

### Initial Radius Tokens

- `Radius S = 8 px`
- `Radius L = 18 px`

### Practical Rule

If the element is a direct control the user clicks or edits, use `Radius S`.
If the element is a container that groups information or content, use `Radius L`.
