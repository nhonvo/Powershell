# Design Specifications: Tech-Professional (v2.0)

## 🎨 Color Palette (Dark Mode First)

| Name | Hex | Tailwind Class | Usage |
| :--- | :--- | :--- | :--- |
| **Backgrounds** | | | |
| Canvas (Page) | `#0B1120` | `bg-slate-950` | Main application background (Deepest) |
| Surface (Card) | `#151E32` | `bg-slate-900` | Cards, Sidebar, Panels (Slightly lighter) |
| Surface Hover | `#1E293B` | `bg-slate-800` | Interactive elements hover state |
| Grid Line | `#334155` | `border-slate-700` | Subtle background grid pattern (opacity 0.2) |
| **Borders** | | | |
| Border Default | `#334155` | `border-slate-700` | Card borders, dividers (crisp 1px) |
| Border Highlight | `#475569` | `border-slate-600` | Active state borders |
| **Text** | | | |
| Text Primary | `#F8FAFC` | `text-slate-50` | Headings, Key data values |
| Text Secondary | `#94A3B8` | `text-slate-400` | Labels, Subtitles, Descriptions |
| Text Muted | `#64748B` | `text-slate-500` | Placeholder text, disabled states |
| **Accents** | | | |
| Primary (Indigo) | `#6366F1` | `text-indigo-500` | Main CTAs, Active Links, Branding |
| Success (Emerald) | `#10B981` | `text-emerald-500` | Positive trends, Income |
| Warning (Amber) | `#F59E0B` | `text-amber-500` | Alerts, 'Near budget' status |
| Danger (Rose) | `#F43F5E` | `text-rose-500` | Expenses, Negative trends, Errors |

## 📝 Typography

| Usage | Font Family | Weight | Size/Leading | Tracking |
| :--- | :--- | :--- | :--- | :--- |
| **Headings** | Inter | Medium (500) | H1: 24px/32px | -0.025em |
| **Body** | Inter | Regular (400) | Base: 14px/20px | Normal |
| **Data/Numbers** | JetBrains Mono | Medium (500) | 13px - 14px | -0.01em |
| **Small Caps** | Inter | SemiBold (600) | 11px | +0.05em |

## 📐 Spacing & Layout (High Density)

| Name | Value | Usage |
| :--- | :--- | :--- |
| `gap-1` | 4px | Internal icon/text gaps |
| `gap-2` | 8px | Button padding, tight lists |
| `gap-4` | 16px | Card internal padding (p-4) |
| `gap-6` | 24px | Section separation |
| `sidebar` | 64px | Collapsed width |
| `sidebar-open` | 240px | Expanded width |

## 🔲 Border Radius (Tech Feel)

| Name | Value | Usage | Visual Feel |
| :--- | :--- | :--- | :--- |
| `rounded-sm` | 4px | Inner elements, Tags, Checkboxes | Sharp, precise |
| `rounded-md` | 6px | Buttons, Inputs, Small Cards | Standard "Tech" look |
| `rounded-lg` | 8px | Main Cards, Modals | Slightly softer content references |
| *Note* | | *Avoid `rounded-xl` or `2xl` to maintain professional edge* | |

## 🌫️ Shadows & Effects

| Name | Value | Usage |
| :--- | :--- | :--- |
| `shadow-none` | none | Most elements rely on Borders |
| `shadow-sm` | `0 1px 2px 0 rgb(0 0 0 / 0.3)` | Dropdowns, Popovers |
| `glow-success` | `0 0 10px rgba(16, 185, 129, 0.2)` | Positive sparklines glow |
| `glow-primary` | `0 0 15px rgba(99, 102, 241, 0.15)` | Active state glow |

## 🖥️ Layout Components

### 1. The "Tech" Card
```css
.card-tech {
  background: var(--surface); /* slate-900 */
  border: 1px solid var(--border-default); /* slate-700 */
  border-radius: 8px; /* rounded-lg */
  /* No heavy shadow, relying on border contrast */
}
```

### 2. The Grid Background
```css
.bg-tech-grid {
  background-size: 40px 40px;
  background-image:
    linear-gradient(to right, rgba(255,255,255,0.03) 1px, transparent 1px),
    linear-gradient(to bottom, rgba(255,255,255,0.03) 1px, transparent 1px);
}
```
