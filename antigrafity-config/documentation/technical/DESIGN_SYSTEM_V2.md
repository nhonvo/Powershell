# 🎨 UI/UX Design System V2: "Aura Express" (Deep Dive)

> **Status**: Reference Document for Phase 44
> **Design Philosophy**: Clarity through depth, delight through motion, and structure through widgets.

---

## 🕶️ The "Glassmorphism" Style Guide

Based on `public/images/auth/dashboard-preview.png`, the system defines a multi-layered glass effect.

### 1. Layer Hierarchy (Z-Index Strategy)

1. **Level 0 (Atmospheric)**: `bg-linear-to-br` with blurred SVGs or decorative orbs.
2. **Level 1 (The Pane)**: The semi-transparent card surface.
3. **Level 2 (The Content)**: High-contrast text and vibrant interactive elements.

### 2. Glass Specifications

| Attribute   | Light Mode                             | Dark Mode                              |
| :---------- | :------------------------------------- | :------------------------------------- |
| **Surface** | `rgba(255, 255, 255, 0.90)`            | `rgba(17, 24, 39, 0.90)`               |
| **Blur**    | `blur(12px)`                           | `blur(20px)`                           |
| **Border**  | `1px solid rgba(255, 255, 255, 0.2)`   | `1px solid rgba(255, 255, 255, 0.05)`  |
| **Shadow**  | `0 25px 50px -12px rgba(0, 0, 0, 0.1)` | `0 25px 50px -12px rgba(0, 0, 0, 0.5)` |

---

## 🧩 Widget Organization & Layout

The dashboard implements a **Modular Tile System** using `react-grid-layout`.

### 1. Widget Anatomy

Standard widgets (`UniversalCard`) follow a 3-section slot architecture:

- **Slot: Header**: Contains an Icon (rounded-2xl background), Title (executive font), and an Action (usually a Context Menu or "Expand").
- **Slot: Body**: The primary data visualization (Chart, Table, or Summary).
- **Slot: Footer**: Secondary metadata or a "Live Status" indicator.

### 2. Layout Grid (12-Column System)

- **Large Widgets (6-8 units)**: Primary Charts (Net Worth History, Portfolio Performance).
- **Medium Widgets (4-6 units)**: Data Tables (Recent Transactions), Distribution Charts (Asset Allocation).
- **Small Widgets (2-3 units)**: Stat Cards (Total Assets, Crypto Holdings).

---

## 🏗️ Incremental Implementation Plan

To ensure zero regressions, Phase 44 will follow a **"Parallel Style"** strategy.

### 1. Parallel Configuration (`src/app/global-v1.css`)

We will **NOT** modify the main `globals.css` or `tailwind.config.ts` initially. Instead:

- Create `src/app/global-v1.css` to host the V2 variables and specialized Aura utilities.
- Use standard CSS `@layer` directives to avoid conflicts with Tailwind v3/v4 defaults.

### 2. Style Injection

```css
/* src/app/global-v1.css */
@layer base {
  :root {
    --aura-glass-bg: rgba(255, 255, 255, 0.9);
    --aura-glass-border: rgba(255, 255, 255, 0.2);
    --aura-accent-blue: #3b82f6;
  }
  .dark {
    --aura-glass-bg: rgba(17, 24, 39, 0.9);
    --aura-glass-border: rgba(255, 255, 255, 0.05);
  }
}

@layer components {
  .glass-pane {
    @apply bg-[var(--aura-glass-bg)] backdrop-blur-xl border-[var(--aura-glass-border)] shadow-2xl;
  }
}
```

### 3. Progressive Refactor Cycle

1. **Pilot**: Apply `global-v1.css` styles to the **Sign In** and **Sign Up** forms (completed in previous step).
2. **Phase A**: Refactor the **Dashboard Layout** container but keep individual widgets on V1 styles.
3. **Phase B**: Incrementally migrate widgets (`UniversalCard`) to the new CSS classes one by one.
4. **Final**: Once all pages use V2, merge `global-v1.css` into the main stylesheet.
