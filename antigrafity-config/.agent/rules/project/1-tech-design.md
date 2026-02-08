---
trigger: model_decision
description: Financial Project Design Standards (v2.0)
---

# 🎨 Finance Design Specs: Tech-Professional

Every UI component built for the Finance Project must strictly follow the **"Aura Express"** design system.

## 1. Color Palette (Dark Mode First)

- **Background (Canvas)**: `#0B1120` (`bg-slate-950`)
- **Surface (Card)**: `#151E32` (`bg-slate-900`)
- **Border**: `#334155` (`border-slate-700`)
- **Text Primary**: `#F8FAFC` (`text-slate-50`)
- **Accents**: Indigo-500 (Primary), Emerald-500 (Success), Rose-500 (Danger/Expense).

## 2. Visual Effects (Glassmorphism)

- **Glass Pane**: `rgba(17, 24, 39, 0.90)` background with `blur(20px)`.
- **Borders**: Sharp 1px solid `rgba(255, 255, 255, 0.05)`.
- **Rounded Corners**: Use `rounded-lg` (8px) for cards. Avoid heavily rounded/bubbly corners.

## 3. Typography & Density

- **Headings**: Inter, Medium (500), H1: 24px.
- **Numbers/Data**: JetBrains Mono for all numeric values and transaction data.
- **Layout**: High-density grid-based tile system (`react-grid-layout`).

## 4. Components: UniversalCard

Standard widgets follow the Slot Architecture:

- **Header**: Icon (rounded-2xl) + Executive Title.
- **Body**: Primary visualization (Chart/Table).
- **Footer**: Meta-data/Status.
