# 🗄️ Documentation Archive Index

> **Category**: Historical Documentation & Architectural Evolution  
> **Status**: Archived / Preserved for Audit Trail  
> **Master Gateway**: [docs/README.md](../README.md)  
> **Windows UNC Path**: `\\wsl.localhost\Ubuntu\home\truongnhon\projects\powershell-profile\docs\archive`

---

## Overview

This directory preserves historical architecture specifications, early CLI blueprints, task breakdown plans, and refactoring reports from earlier iterations of the Antigravity developer suite and PowerShell Control Center.

These files are retained to provide a complete audit trail of how the system evolved from early CLI prototypes and the monolithic C# .NET 9.0 implementation (`AgyTui`) into the modern modular Go engine suite (`agyx`, `agyswitch`, `agygit`, `agyproj`, `agydocker`, `agyterm`, `agymobile`).

---

## Archived Documents Catalog

| Document | Original Context & Topic | Historical Milestone |
| :--- | :--- | :--- |
| **[cli_clean_architecture_report.md](./cli_clean_architecture_report.md)** | Clean Architecture refactoring report for the C# console codebase | July 2026: Separation of Domain, Infrastructure, and UI layers |
| **[console_app_cli_master_mockup_blueprint.md](./console_app_cli_master_mockup_blueprint.md)** | Zero-lag ANSI mockups and interaction designs for 12 CLI subsystems | July 2026: Comprehensive UI screen blueprint for Spectre.Console |
| **[console_app_cli_feature_flow_breakdown.md](./console_app_cli_feature_flow_breakdown.md)** | Detailed task breakdown and sequence diagrams across all 12 subsystems | August 2026: Subsystem dispatch logic and event workflows |
| **[console_app_ui_test_folder_audit_report.md](./console_app_ui_test_folder_audit_report.md)** | Reorganization and verification of the 299-test UI test suite | August 2026: 1-to-1 mirroring of production UI views in test suites |
| **[console_app_ui_test_rebuild_plan.md](./console_app_ui_test_rebuild_plan.md)** | Structural plan for rebuilding and segregating UI test fixtures | August 2026: Layer isolation in test architecture |
| **[master_documentation_plan.md](./master_documentation_plan.md)** | Original documentation roadmap for the C# Control Center | July 2026: Initial documentation taxonomy |
| **[agy_switch_crud_and_sync_flows.md](./agy_switch_crud_and_sync_flows.md)** | Early CRUD and directory synchronization workflows for account switching | August 2026: Prototype context mirroring |
| **[agyswitch_cli_flow_blueprint.md](./agyswitch_cli_flow_blueprint.md)** | Specification of CLI argument parsing and account routing | September 2026: Early Go engine CLI interface |
| **[agyswitch_cli_guide.md](./agyswitch_cli_guide.md)** | Early CLI command usage guide for `agyswitch` | September 2026: Interim CLI manual |
| **[agyswitch_tui_enhancement_plan.md](./agyswitch_tui_enhancement_plan.md)** | Feature plan for terminal UI styling and layout upgrades | September 2026: Transition to raw ANSI VT100 event loop |
| **[tasks_refactor_plan.md](./tasks_refactor_plan.md)** | Sprint refactoring task list for workspace and learning modules | September 2026: Pre-migration sprint checklist |

---

## Active Documentation Reference

For current active specifications and deep-dive technical audits, consult:
- **[Documentation Gateway](../README.md)**
- **[Audit & Deep-Dive Reports](../reports/README.md)**
- **[Master System Audit & Strategic Roadmap](../../AGY_SYSTEM_AUDIT_AND_ROADMAP.md)**
