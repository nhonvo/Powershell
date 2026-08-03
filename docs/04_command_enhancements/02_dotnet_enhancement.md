# ⚙️ .NET Command Architecture & Dual-Tier Enhancement Pattern

## 1. Design Blueprint & Dual-Tier Standard

Following our established Dual-Tier Architecture pattern, **.NET Developer Tools** are split into two execution paths:
1. **Native CLI Tier**: Standard `dotnet` commands running raw CLI tools with direct output piping (`dbld`, `dr`, `dtst`, `drestore`, `dpublish`).
2. **Custom TUI Tier (`✨`)**: Interactive Spectre.Console build status screens, test result tables, dependency clean prompts, and EF Core migration assistants (`dbldu`, `dru`, `dtstu`, `dcleanu`).

---

## 2. Naming & Routing Conventions

- **Native CLI Commands**:
  - `dbld` / `db` $\rightarrow$ Standard native `dotnet build @args`.
  - `dr` $\rightarrow$ Standard native `dotnet run @args`.
  - `dtst` / `dt` $\rightarrow$ Standard native `dotnet test @args`.
  - `drestore` / `dres` $\rightarrow$ Standard native `dotnet restore @args`.
  - `dpublish` / `dpub` $\rightarrow$ Standard native `dotnet publish @args`.

- **Custom TUI Commands (`✨`)**:
  - **`dbldu`** $\rightarrow$ `✨ [.NET] Build Project (Custom TUI Progress & Summary)`
  - **`dru`** $\rightarrow$ `✨ [.NET] Run Project (Custom TUI Process Runner & Log Pager)`
  - **`dtstu`** $\rightarrow$ `✨ [.NET] Test Project (Custom TUI Test Runner & Results Table)`
  - **`clean-build`** $\rightarrow$ `✨ Clean & Rebuild Binary (Targeted obj/bin Purge)`
  - **`rebuild-tui`** $\rightarrow$ `✨ Rebuild AgyTui Executable (In-place Single-File Publish)`

---

## 3. .NET Command Alignment Matrix

| Native Command (CLI) | Execution Action | Custom TUI Command (`✨`) | TUI Feature & Behavior |
| :--- | :--- | :--- | :--- |
| **`dbld`** / **`db`** | `dotnet build @args` | **`dbldu`** | **`✨ [.NET] Build Project`**: Custom Spectre build progress spinner and formatted warning/error summary. |
| **`dr`** | `dotnet run @args` | **`dru`** | **`✨ [.NET] Run Project`**: Custom Spectre process launcher with interactive environment variable picker. |
| **`dtst`** / **`dt`** | `dotnet test @args` | **`dtstu`** | **`✨ [.NET] Test Project`**: Interactive Spectre test runner rendering pass/fail tables and duration breakdowns. |
| **`drestore`** / **`dres`**| `dotnet restore @args` | **`drestoreu`** | **`✨ [.NET] Restore Dependencies`**: Multi-project NuGet restore progress viewer. |
| **`dpublish`** / **`dpub`**| `dotnet publish @args` | **`rebuild-tui`** | **`✨ Rebuild AgyTui Executable`**: Single-file self-contained publish script compiling `AgyTui.exe` in-place. |
| **`dclean`** | `dotnet clean @args` | **`clean-build`** | **`✨ Clean & Rebuild`**: Targeted `bin`/`obj` filesystem purge with lock-file handle checks. |
| **`dotnet-info`** | `dotnet --info` | **`dotnet-infou`** | **`✨ [.NET] System & SDK Info`**: Spectre table breakdown of installed SDKs, runtimes, and system environment. |

---

## 4. TUI Menu Tree Folder Mapping

All .NET developer tools are grouped under **`📂 .NET Project Tools`** in [CommandRegistry.cs](file:///C:/Users/TruongNhon/Documents/Powershell/csapp/AgyTui/UI/Core/Registries/CommandRegistry.cs):

```text
─ [-] 📂 .NET Project Tools
     ├── ⚙️ /dbld — [.NET] Build Project (Native)
     ├── ⚙️ /dbldu — ✨ [.NET] Build Project (Custom TUI Progress)
     ├── ⚙️ /dr — [.NET] Run Project (Native)
     ├── ⚙️ /dru — ✨ [.NET] Run Project (Custom TUI Runner)
     ├── ⚙️ /dtst — [.NET] Test Project (Native)
     ├── ⚙️ /dtstu — ✨ [.NET] Test Project (Custom TUI Results Table)
     ├── ⚙️ /drestore — [.NET] Restore Dependencies (Native)
     ├── ⚙️ /dpublish — [.NET] Publish Single-File Binary (Native)
     ├── ⚙️ /clean-build — ✨ Clean & Rebuild Binary
     ├── ⚙️ /rebuild-tui — ✨ Rebuild AgyTui Executable
     └── ⚙️ /dotnet-info — [.NET] System & SDK Info
```
