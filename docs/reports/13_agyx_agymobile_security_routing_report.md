# ⚡ Security & Proxy Routing Audit Report: `agyx` and `agymobile`

> **Module**: Antigravity Developer Suite Core (`apps/agyx` & `apps/agymobile`)  
> **Date**: September 8, 2026  
> **Status**: Completed & Verified Cleanly  
> **Windows UNC Path**: `\\wsl.localhost\Ubuntu\home\truongnhon\projects\powershell-profile\AGYX_AGYMOBILE_SECURITY_ROUTING_REPORT.md`

---

## 1. Executive Summary

This engineering task resolved critical security vulnerabilities, proxy routing gaps, child process exit code handling, and mobile station pairing features across `apps/agyx` and `apps/agymobile`:
1. **Critical Network Security Lockdown**: Eliminated `0.0.0.0` wildcard interface binding in `agymobile`, binding strictly to Tailscale mesh IPv4 or loopback (`127.0.0.1`). Implemented bearer token authorization for destructive/sensitive endpoints (`/api/action/flush-ram` and `/api/action/docker-stop-all`), allowing unauthenticated requests strictly from loopback.
2. **Seamless Suite Orchestration in `agyx`**: Preserved child process exit codes via `*exec.ExitError`, integrated `agymobile` into `GetRegisteredTools()` with aliases (`m`, `remote`, `phone`, `pwa`), expanded the Master Cockpit TUI to 6 tabs (adding `[6] 📱 Mobile`), and added `renderMobileSummary()`.
3. **Mobile Station Pairing & QR Generator**: Created [qr.go](./apps/agymobile/internal/service/tailscaleops/qr.go) generating ASCII/ANSI QR codes and connection helpers for instant smartphone terminal pairing (SSH) and Mobile Web Cockpit (PWA).
4. **Zero Impact on `agyswitch`**: Adhered strictly to the constraint: `agyswitch` account management, token swapping, vault credentials, and account files remained 100% untouched.

---

## 2. Detailed Breakdown of Modifications

### 2.1 `apps/agyx` Proxy & Cockpit Enhancements

| File | Changes Made |
| :--- | :--- |
| [apps/agyx/main.go](./apps/agyx/main.go) | • Imported `os/exec`<br>• In lines 48–52, preserved child exit codes when `proxy.Execute` returns `*exec.ExitError`<br>• Added `agyx mobile` to help text with aliases `m`, `remote`, `phone`, `pwa`<br>• Added `alias agy-mobile="agyx mobile"` and `alias agym="agyx mobile"` to shell init generator |
| [apps/agyx/internal/proxy/proxy.go](./apps/agyx/internal/proxy/proxy.go) | • Registered `mobile` tool definition in `GetRegisteredTools()` referencing `agymobile` binary with aliases `m`, `remote`, `phone`, `pwa` |
| [apps/agyx/internal/proxy/proxy_test.go](./apps/agyx/internal/proxy/proxy_test.go) | • Updated expected tool count from 5 to 6<br>• Added test cases for `mobile`, `m`, `remote`, `phone`, `pwa` resolution |
| [apps/agyx/internal/view/cockpit.go](./apps/agyx/internal/view/cockpit.go) | • Expanded tabs from 5 to 6 tabs including `[6] 📱 Mobile`<br>• Updated arrow, Tab, and numeric hotkeys (`1`–`6`) modulo 6<br>• Updated `getActiveToolBinary()` to include `agymobile`<br>• Added `renderMobileSummary()` preview for Tab 5 |
| [apps/agyx/internal/view/view_test.go](./apps/agyx/internal/view/view_test.go) | • Added test assertion rendering Tab 5 (`app.ActiveTab = 5`) |

### 2.2 `apps/agymobile` Security Lockdown & Pairing Station

| File | Changes Made |
| :--- | :--- |
| [apps/agymobile/internal/web/server.go](./apps/agymobile/internal/web/server.go) | • **CRITICAL FIX**: Replaced `0.0.0.0:%d` with strictly bound Tailscale IPv4 (`tsInfo.IPv4`) or `127.0.0.1`<br>• Added `isLoopback(r)` detecting loopback (`127.0.0.1`, `::1`)<br>• Added `isAuthorized(r)` checking `Authorization: Bearer <token>` or `?token=<token>`<br>• Secured `handleFlushRAM` and `handleDockerStopAll` with HTTP 401 for unauthorized calls<br>• Enhanced frontend JavaScript in `embeddedHTML` to extract, persist in `sessionStorage`, and attach bearer token to all API calls |
| [apps/agymobile/internal/service/tailscaleops/qr.go](./apps/agymobile/internal/service/tailscaleops/qr.go) | • **NEW**: Persistent token management at `~/.config/antigravity/mobile_token.secret`<br>• Implemented `EnsureAuthToken()`, `GetAuthToken()`, `GetPairingInfo(port)`, `GenerateQRCodeString(content)`, and `PrintPairingInfo(w, port)`<br>• Renders ANSI/Unicode half-block QR codes via `github.com/skip2/go-qrcode` |
| [apps/agymobile/main.go](./apps/agymobile/main.go) | • Added `qr` and `pair` subcommands calling `tailscaleops.PrintPairingInfo(os.Stdout, port)`<br>• Updated help documentation |
| [apps/agymobile/internal/service/tailscaleops/qr_test.go](./apps/agymobile/internal/service/tailscaleops/qr_test.go) | • **NEW**: Unit tests for token generation, idempotency, QR code string rendering, pairing info resolution, and terminal output |
| [apps/agymobile/internal/web/server_test.go](./apps/agymobile/internal/web/server_test.go) | • **NEW**: Unit tests for `isLoopback`, `isAuthorized`, `handleFlushRAM` auth enforcement (401 vs authorized), `handleDockerStopAll` auth enforcement, `/api/stats`, and `/` HTML |

---

## 3. Test Verification & Results

### 3.1 `apps/agyx` Test Results
```
=== RUN   TestProxy_GetRegisteredTools
--- PASS: TestProxy_GetRegisteredTools (0.00s)
=== RUN   TestProxy_ResolveTool
--- PASS: TestProxy_ResolveTool (0.00s)
PASS
ok  	agyx/internal/proxy	0.002s
=== RUN   TestCockpit_PrintStatus
--- PASS: TestCockpit_PrintStatus (0.00s)
=== RUN   TestCockpit_Render
--- PASS: TestCockpit_Render (0.00s)
PASS
ok  	agyx/internal/view	0.002s
```

### 3.2 `apps/agymobile` Test Results
```
=== RUN   TestAuthToken_EnsureAndGet
--- PASS: TestAuthToken_EnsureAndGet (0.00s)
=== RUN   TestGenerateQRCodeString
--- PASS: TestGenerateQRCodeString (0.00s)
=== RUN   TestGetPairingInfo
--- PASS: TestGetPairingInfo (0.52s)
=== RUN   TestPrintPairingInfo
--- PASS: TestPrintPairingInfo (0.62s)
PASS
ok  	agymobile/internal/service/tailscaleops	1.142s
=== RUN   TestApp_PrintStatus
--- PASS: TestApp_PrintStatus (0.62s)
=== RUN   TestApp_Render
--- PASS: TestApp_Render (0.00s)
=== RUN   TestTruncate
--- PASS: TestTruncate (0.00s)
PASS
ok  	agymobile/internal/view	0.623s
=== RUN   TestIsLoopback
--- PASS: TestIsLoopback (0.00s)
=== RUN   TestIsAuthorized
--- PASS: TestIsAuthorized (0.00s)
=== RUN   TestHandleFlushRAM_AuthEnforcement
--- PASS: TestHandleFlushRAM_AuthEnforcement (0.26s)
=== RUN   TestHandleDockerStopAll_AuthEnforcement
--- PASS: TestHandleDockerStopAll_AuthEnforcement (0.10s)
=== RUN   TestHandleStats
--- PASS: TestHandleStats (0.59s)
=== RUN   TestHandleIndex
--- PASS: TestHandleIndex (0.00s)
PASS
ok  	agymobile/internal/web	0.958s
```

---

## 4. Operational Commands Reference

- **Launch Unified Master Cockpit**: `agyx`
- **Proxy directly to Mobile Station TUI**: `agyx mobile` or `agym`
- **Display Connection String & QR Code**: `agymobile qr` or `agyx mobile qr`
- **Start Mobile Web Dashboard (Port 7890)**: `agymobile serve` or `agyx mobile serve`
- **Reclaim WSL2 RAM**: `agymobile flush` or `agyx mobile flush`
