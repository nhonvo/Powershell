# 🐳 Docker Command Architecture & Dual-Tier Enhancement Pattern

## 1. Design Blueprint & Dual-Tier Standard

Container operations are split into:
1. **Native CLI Tier**: Direct execution of standard `docker` and `docker compose` CLI commands (`dk`, `dimg`, `dlogs`, `dcup`, `dcdown`).
2. **Custom TUI Tier (`✨`)**: Interactive Spectre.Console container dashboards, log tailers, prune assistants, and compose stack managers (`dku`, `dimgu`, `dlogsu`, `docker-health`).

---

## 2. Naming & Routing Conventions

- **Native CLI Commands**:
  - `dk` $\rightarrow$ Standard native `docker ps @args`.
  - `dimg` $\rightarrow$ Standard native `docker images @args`.
  - `dlogs` $\rightarrow$ Standard native `docker logs @args`.
  - `dcup` $\rightarrow$ Standard native `docker compose up -d @args`.
  - `dcdown` $\rightarrow$ Standard native `docker compose down @args`.

- **Custom TUI Commands (`✨`)**:
  - **`dku`** / **`dki`** $\rightarrow$ `✨ Docker Container Dashboard (Custom Spectre TUI Table)`
  - **`dimgu`** $\rightarrow$ `✨ Docker Image Manager (Custom Spectre TUI Table & Cleaner)`
  - **`dlogsu`** $\rightarrow$ `✨ Container Log Tailer (Custom Spectre Pager)`
  - **`docker-health`** $\rightarrow$ `✨ Docker Health Check (Daemon Connectivity Inspector)`
  - **`dkcl`** $\rightarrow$ `✨ Clean Docker Containers (Interactive Prune Assistant)`

---

## 3. Docker Command Alignment Matrix

| Native Command (CLI) | Execution Action | Custom TUI Command (`✨`) | TUI Feature & Behavior |
| :--- | :--- | :--- | :--- |
| **`dk`** | `docker ps @args` | **`dku`** / **`dki`** | **`✨ Docker Container Dashboard`**: Live color-coded Spectre table of running containers, ports, and status. |
| **`dimg`** | `docker images @args` | **`dimgu`** | **`✨ Docker Image Manager`**: Interactive table listing local images, sizes, tags, and dangling image cleanup. |
| **`dlogs`** | `docker logs @args` | **`dlogsu`** | **`✨ Container Log Tailer`**: Scrollable Spectre Pager view for streaming container stdout/stderr logs. |
| **`dcup`** | `docker compose up -d` | **`dcupu`** | **`✨ Docker Compose Launcher`**: Multi-container stack launcher with service health verification. |
| **`dcdown`** | `docker compose down` | **`dcdownu`** | **`✨ Docker Compose Stopper`**: Container teardown wizard with volume cleanup prompts. |
| **`dkcl`** | `docker container prune -f` | **`dkcl`** | **`✨ Clean Docker Containers`**: Interactive container & dangling volume cleanup wizard. |
| N/A | Native CLI | **`docker-health`** | **`✨ Docker Health Check`**: Daemon connection inspector verifying engine pipe (`npipe://`). |

---

## 4. TUI Menu Tree Folder Mapping

All Docker container tools are grouped under **`📂 Docker Tools`** in [CommandRegistry.cs](file:///C:/Users/TruongNhon/Documents/Powershell/csapp/AgyTui/UI/Core/Registries/CommandRegistry.cs):

```text
─ [-] 📂 Docker Tools
     ├── 🐳 /dk — Docker Container List (Native)
     ├── 🐳 /dku — ✨ Docker Container Dashboard (Custom TUI Table)
     ├── 🐳 /dimg — Docker Images List (Native)
     ├── 🐳 /dimgu — ✨ Docker Image Manager (Custom TUI)
     ├── 🐳 /dlogs — Docker Container Logs (Native)
     ├── 🐳 /dlogsu — ✨ Container Log Tailer (Custom TUI Pager)
     ├── 🐳 /dcup — Start Docker Compose (Native)
     ├── 🐳 /dcdown — Stop Docker Compose (Native)
     ├── 🐳 /dkcl — ✨ Clean Docker Containers
     └── 🐳 /docker-health — ✨ Docker Health Check
```
