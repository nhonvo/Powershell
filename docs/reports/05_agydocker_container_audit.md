# 🐳 Deep Technical Audit: `agydocker` Container Fleet & WSL2 RAM Guard (Go Engine)

> **Category**: Developer Suite Core Audit  
> **Target Subsystem**: `apps/agydocker`  
> **Source Files**: [apps/agydocker/main.go](../../apps/agydocker/main.go) · [apps/agydocker/internal/service/dockerops/dockerops.go](../../apps/agydocker/internal/service/dockerops/dockerops.go) · [apps/agydocker/internal/view/app.go](../../apps/agydocker/internal/view/app.go)  
> **Comparison Baseline**: C# `DockerClient.cs` (`AgyTui`)  
> **Windows UNC Reference**: `\\wsl.localhost\Ubuntu\home\truongnhon\projects\powershell-profile\apps\agydocker`

---

## 1. Executive Summary & Architecture

`agydocker` manages Docker container fleets, Compose project stacks, and monitors WSL2 host memory headroom to prevent Windows workstation freezing:
1. **Container Lifecycle & Batch Stacks**: Start, stop, restart individual containers (`s`, `r`) or batch-toggle entire Compose project stacks (`a`).
2. **WSL2 Kernel RAM Guard**: Parses `/proc/meminfo` directly to render live UTF-8 bar gauges (`████░░░░`) for Linux RAM and Swap usage.
3. **Interactive Container Shell Exec**: Attaches directly to running containers (`e` launches `docker exec -it <id> sh`).
4. **Volume & System Pruning**: Reclaims disk and memory cache via `docker system prune`.

```
apps/agydocker/
├── go.mod                                   # Go 1.25.0, requires only x/term and x/sys
├── main.go                                  # CLI command router & interactive TUI dispatcher
└── internal/
    ├── model/
    │   └── docker.go                        # ContainerInfo, MemInfo, VolumeInfo schemas
    ├── service/
    │   └── dockerops/
    │       ├── dockerops.go                 # Subprocess exec wrappers (200 LOC)
    │       └── dockerops_test.go            # Unit tests
    └── view/
        ├── app.go                           # Custom ANSI TUI engine & event loop
        ├── poll_linux.go                    # unix.Poll(fd, 150) non-blocking key polling
        └── poll_windows.go                  # Windows fallback polling
```

---

## 2. Code Defects & Implementation Gaps

### 2.1 Incorrect Volume Pruning Flag
- **Location**: [apps/agydocker/internal/view/app.go lines 412–429](../../apps/agydocker/internal/view/app.go#L412-L429)
```go
case 'P', 'p': // Volume Tab Prune
    ...
    out, err := dockerops.PruneSystem() // ⚠️ Calls `docker system prune -f`
```
- **Defect**: In Tab 2 (Volumes), pressing `P` triggers `PruneSystem()`, which executes `docker system prune -f`.
- **Impact**: In Docker, `docker system prune -f` prunes stopped containers, networks, and build cache, but **leaves anonymous and unused volumes untouched** unless `--volumes` is passed. As a result, unused volumes are never deleted when the user requests volume pruning!
- **Remediation**: Execute `docker volume prune -f` when pruning from the Volumes Tab.

### 2.2 Discarded Docker Daemon Offline Error
- **Location**: [apps/agydocker/internal/view/app.go line 117](../../apps/agydocker/internal/view/app.go#L117)
```go
a.cachedContainers, _ = dockerops.ListContainers() // ⚠️ Error explicitly discarded!
```
- **Defect**: When the Docker daemon is offline or `/var/run/docker.sock` is unreachable, `ListContainers()` returns an error, but `app.go` discards it with `_`.
- **Impact**: The UI displays `No Docker containers found on this host.` rather than alerting the developer that the Docker daemon is stopped.
- **Remediation**: Store the connection error and render a prominent red alert banner: `⚠️ Docker daemon is offline. Run 'sudo service docker start' or start Docker Desktop.`.

### 2.3 Absence of Per-Container Resource Metrics
- **Current State**: Displays container status (`Up`, `Exited`) and port bindings, but does not monitor per-container memory RSS or CPU consumption.
- **Comparison with C#**: C# `DockerClient.cs` executed `docker stats --no-stream --format "{{json .}}"` to display live container CPU%, memory usage, and network I/O in the health dashboard.
- **Remediation**: Add a background poller for `docker stats --no-stream` to display memory consumption per container in the table.

### 2.4 Missing Container Operations: Kill, Delete & Image Management
- **No Force Kill (`kill`)**: Only graceful `docker stop` is available. If a container hangs on SIGTERM, the developer cannot terminate it.
- **No Container Deletion (`rm`)**: Cannot remove stopped containers from the TUI.
- **No Image Management**: Lacks an Images tab to inspect image sizes, repository tags, and prune dangling layers (`docker image prune -af`).

---

## 3. Comparison with Legacy C# System (`DockerClient.cs`)

| Feature | Legacy C# `DockerClient.cs` | Go `agydocker` | Parity Status & Verdict |
| :--- | :--- | :--- | :--- |
| **Interface Style** | Modal Spectre tables & menus. | Persistent Interactive 3-Tab TUI. | 🟢 **Go Wins**: Live cursor navigation, continuous refresh. |
| **WSL2 Host RAM / Swap Meters** | None. | Live `/proc/meminfo` bar gauges. | 🟢 **Go Wins**: Immediate awareness of host memory pressure. |
| **Container CPU / RAM Stats** | `docker stats --no-stream` with live table. | Omitted in table. | ❌ **C# Advantage**: C# provided container-level resource breakdown. |
| **Image Management** | List images, delete image, image prune. | None. | ❌ **Missing in Go**: No image inspection or pruning. |
| **Volume Pruning** | `volume prune -f`. | Buggy `system prune -f`. | ⚠️ **C# Advantage**: C# correctly targeted volume pruning. |
| **Interactive Container Shell** | None. | `docker exec -it <id> sh` on `e`. | 🟢 **Go Wins**: 1-tap shell access. |

---

## 4. Prioritized Action Plan & Next Steps

1. **Fix Volume Pruning**:
   - Add `dockerops.PruneVolumes()` running `docker volume prune -f` and invoke it in Tab 2.
2. **Daemon Offline Sentinel**:
   - Catch errors from `ListContainers()` and display a recovery banner in the TUI header.
3. **Add Per-Container CPU/RAM Stats**:
   - Ingest `docker stats --no-stream` asynchronously and add `CPU %` and `MEM` columns to Tab 0.
4. **Add Container Force-Kill (`k`) and Deletion (`d`)**:
   - Key `k`: `docker kill <id>`.
   - Key `d`: `docker rm <id>` (guarded with confirmation).
5. **Add Docker Images Tab**:
   - Tab 3: List images (`docker images --format {{json .}}`) with size, tag, and dangling image prune (`docker image prune -af`).
