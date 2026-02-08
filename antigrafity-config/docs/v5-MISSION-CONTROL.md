# 🌌 Antigravity Mission Control (v5.1): System Responsibilities

This document clarifies the specific roles of each component in the `agent` directory, optimized for **Finance App** development with **Aura Express**.

---

## 🏛️ 1. Folder Responsibilities

### 📜 `agent/rules/` (The Project Constitution)

- **Responsibility**: Global constants and non-negotiable standards.
- **Active Content**:
  - `01-mission-control.md`: Identity, Security (Deny List), and Operating Protocols.
  - `project/`: Finance-specific architecture rules and categorization logic.
- **Archive**: All general coding standards are moved to `archive/rules/` and accessed via **BM25 Search**.

### 🛠️ `agent/skills/` (The Specialized Experts)

- **Responsibility**: Depth-of-knowledge modules for specific technologies and AWF system behaviors.
- **Usage**: Only loaded when a task requires that specific expertise (e.g., .NET API, Docker, CSS, or AWF Session Restore).
- **Core Skill**: `ui-ux-pro-max` is the "lead designer" for all Aura Express components.

### 🎨 `agent/skills/ui-ux-pro-max/` (Visual Designer Intelligence)

- **Responsibility**: Engineering the "WOW" factor.
- **Features**: Contains 50 styles, 21 palettes, and glassmorphism specs.
- **Integration**: My primary tool for ensuring every Finance widget looks premium and "Aura-ready."

### 🔄 `agent/workflows/` (Operational Procedures)

- **Responsibility**: Step-by-step instructions for agent tasks.
- **Lean Core**: The root contains the "Core 8" essential instructions (`/plan`, `/code`, `/verify`, etc.) in concise English for low token usage.
- **Reference Vault (`agent/reference/`)**:
  - `reference/awf-wf/`: Deep logic for standard Antigravity workflows.
  - `reference/origin-wf/`: Original detailed specs for finance operations.

---

## 🧬 2. The Auto-Pilot Flow (Recursive)

This system is designed for **Minimal Human Hand-Holding**. The Agent reads a workflow, sees a `// turbo` instruction, and executes it immediately.

### Step 1: Trigger Lean Workflow

- **User**: `/plan add-credit-card-widget`
- **Agent**: Reads `agent/workflows/plan.md`.

### Step 2: Auto-Scan (The "Brain" Trigger)

- **Instruction**: The workflow explicitly says: `IMMEDIATELY run python scripts/bm25_search.py "add-credit-card-widget"`.
- **Action**: The Agent executes this script _without_ asking for permission (Turbo Mode).
- **Result**: Unique project rules (`agent/reference/`) are injected into the context instantly.

### Step 3: Execute & Code

The agent builds the TS/React components using the retrieved "Aura Express" patterns.

### Step 4: Auto-Verify

- **User**: `/verify`
- **Agent**: Reads `agent/workflows/verify.md`.
- **Action**: Automatically runs `python scripts/bm25_indexer.py`.
- **Result**: The new code is indexed. The "Brain" is updated. 0% context loss.

---

## 🕹️ 3. Common Operational Scenarios (Workflow Combinations)

These are the standard patterns for running the Finance App project.

### 🆕 Scenario A: New Feature Development

**Trigger**: "Add a new Credit Card Widget"
**Flow**: `/plan` → `/code` → `/verify` → `/commit`

1.  **`/plan`**: Auto-scans for "Widget Patterns", defines TS interfaces and UI components.
2.  **`/code`**: Implements the logic using the retrieved context.
3.  **`/verify`**: Builds, tests, and **re-indexes** the new code into the Brain.
4.  **`/commit`**: Semantic release of the feature.

### 🐛 Scenario B: Quick Bug Fix

**Trigger**: "The date picker is crashing on iOS"
**Flow**: `/debug` → `/verify` → `/commit`

1.  **`/debug`**: Auto-scans for error logs or similar crash reports. Proposes a fix.
2.  **`/verify`**: Ensures the fix works and didn't break anything else. Updates Index.
3.  **`/commit`**: `fix(ui): resolve date picker crash on iOS`.

### 🌅 Scenario C: Start of Day (Session Resume)

**Trigger**: "Let's get back to work"
**Flow**: `/recap` → `/next`

1.  **`/recap`**: Reads `.brain/session.json` to load the last known state.
2.  **`/next`**: Checks `git status` and active plans to tell you exactly what to do next.

### 💾 Scenario D: End of Day (Context Freeze)

**Trigger**: "I'm logging off"
**Flow**: `/verify` → `/save-brain`

1.  **`/verify`**: Ensures the codebase is stable and documented before leaving.
2.  **`/save-brain`**: Dumps the current mental state to `session.json` so Scenario C works tomorrow.

### 🔧 Scenario E: Refactoring & Cleanup

**Trigger**: "Migrate all CSS to Tailwind Utility classes"
**Flow**: `/plan` → `/code` → `/verify`

1.  **`/plan`**: Searches for "CSS Standards" to ensure compliance.
2.  **`/code`**: Systematic updates.
3.  **`/verify`**: Crucial step to re-index the massive changes so the Agent knows the new style.

### 🧠 Scenario F: New Skill Creation

**Trigger**: "Create a new 'pdf-processor' skill"
**Flow**: `/build-skill`

1.  **`/build-skill`**:
    - Initializes the skill template: `python agent/skills/skill-creator/scripts/init_skill.py`.
    - Guides the editing of `SKILL.md`.
    - Packages and **Indexes** the new skill immediately.
2.  **Result**: The new skill is now discoverable via the standard `/plan` or `/code` flows.

### 🔎 Note on Skill Discovery

The "Auto-Pilot" flow automatically discovers the right skill for the job.

- When `/plan` runs `bm25_search.py`, it scans all `agent/skills/*/SKILL.md` files.
- If your task matches a skill's description (e.g., "process PDFs"), the search result will return that skill's instructions.
- The Agent then dynamically loads and uses that skill.

---

## 📈 Success Metric

- **Minimal Start Tokens**: ~90% reduction in initial "system noise."
- **Maximal Precision**: Exact patterns retrieved via lexical search from `agent/reference`.
- **Zero Drift**: Documentation, Plan, and Code are always in sync because indexing is mandatory.
