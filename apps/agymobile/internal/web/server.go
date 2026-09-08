package web

import (
	"encoding/json"
	"fmt"
	"net"
	"net/http"
	"os/exec"
	"strings"

	"agymobile/internal/model"
	"agymobile/internal/service/agentops"
	"agymobile/internal/service/hostops"
	"agymobile/internal/service/tailscaleops"
)

// StartServer launches the mobile web dashboard
func StartServer(port int) error {
	tsInfo := tailscaleops.GetTailscaleInfo()
	bindIP := tsInfo.IPv4
	if bindIP == "" || bindIP == "127.0.0.1" {
		bindIP = "127.0.0.1"
	}
	addr := fmt.Sprintf("%s:%d", bindIP, port)

	mux := http.NewServeMux()
	mux.HandleFunc("/", handleIndex)
	mux.HandleFunc("/api/stats", handleStats)
	mux.HandleFunc("/api/action/flush-ram", handleFlushRAM)
	mux.HandleFunc("/api/action/docker-stop-all", handleDockerStopAll)

	token := tailscaleops.GetAuthToken()
	fmt.Printf("\r\n📱 \033[1;36mAGYMOBILE Web Cockpit\033[0m listening on:\r\n")
	if bindIP == "127.0.0.1" {
		fmt.Printf("   • Local:     \033[32mhttp://127.0.0.1:%d\033[0m\r\n", port)
		if token != "" {
			fmt.Printf("   • Auth URL:  \033[1;36mhttp://127.0.0.1:%d/?token=%s\033[0m\r\n", port, token)
		}
	} else {
		fmt.Printf("   • Tailscale: \033[1;33mhttp://%s:%d\033[0m\r\n", bindIP, port)
		if token != "" {
			fmt.Printf("   • Auth URL:  \033[1;36mhttp://%s:%d/?token=%s\033[0m\r\n", bindIP, port, token)
		}
		if tsInfo.MagicDNS != "" {
			fmt.Printf("   • MagicDNS:  \033[1;35mhttp://%s:%d\033[0m\r\n", tsInfo.MagicDNS, port)
		}
	}
	fmt.Printf("\r\n \033[37mTip: Open this URL on your phone browser and tap 'Add to Home Screen'\033[0m\r\n")

	return http.ListenAndServe(addr, mux)
}

func isLoopback(r *http.Request) bool {
	host, _, err := net.SplitHostPort(r.RemoteAddr)
	if err != nil {
		host = r.RemoteAddr
	}
	host = strings.Trim(host, "[]")
	if host == "127.0.0.1" || host == "::1" || host == "localhost" {
		return true
	}
	ip := net.ParseIP(host)
	if ip != nil && ip.IsLoopback() {
		return true
	}
	return false
}

func isAuthorized(r *http.Request) bool {
	if isLoopback(r) {
		return true
	}

	expectedToken := tailscaleops.GetAuthToken()
	if expectedToken == "" {
		return false
	}

	// 1. Check Authorization: Bearer <token>
	authHeader := r.Header.Get("Authorization")
	if authHeader != "" {
		parts := strings.SplitN(authHeader, " ", 2)
		if len(parts) == 2 && strings.EqualFold(parts[0], "Bearer") {
			if strings.TrimSpace(parts[1]) == expectedToken {
				return true
			}
		}
	}

	// 2. Check query parameter: ?token=<token>
	if r.URL.Query().Get("token") == expectedToken {
		return true
	}

	return false
}

func handleStats(w http.ResponseWriter, r *http.Request) {
	w.Header().Set("Content-Type", "application/json")
	w.Header().Set("Access-Control-Allow-Origin", "*")

	dash := getDashboardData()
	_ = json.NewEncoder(w).Encode(dash)
}

func handleFlushRAM(w http.ResponseWriter, r *http.Request) {
	w.Header().Set("Content-Type", "application/json")
	if !isAuthorized(r) {
		w.WriteHeader(http.StatusUnauthorized)
		_ = json.NewEncoder(w).Encode(map[string]string{"status": "error", "message": "Unauthorized: missing or invalid authentication token"})
		return
	}

	if r.Method != http.MethodPost {
		w.WriteHeader(http.StatusMethodNotAllowed)
		_ = json.NewEncoder(w).Encode(map[string]string{"status": "error", "message": "Method not allowed"})
		return
	}

	err := hostops.DropCaches()
	if err != nil {
		w.WriteHeader(http.StatusInternalServerError)
		_ = json.NewEncoder(w).Encode(map[string]string{"status": "error", "message": err.Error()})
		return
	}
	_ = json.NewEncoder(w).Encode(map[string]string{"status": "ok", "message": "WSL2 cache cleared successfully"})
}

func handleDockerStopAll(w http.ResponseWriter, r *http.Request) {
	w.Header().Set("Content-Type", "application/json")
	if !isAuthorized(r) {
		w.WriteHeader(http.StatusUnauthorized)
		_ = json.NewEncoder(w).Encode(map[string]string{"status": "error", "message": "Unauthorized: missing or invalid authentication token"})
		return
	}

	if r.Method != http.MethodPost {
		w.WriteHeader(http.StatusMethodNotAllowed)
		_ = json.NewEncoder(w).Encode(map[string]string{"status": "error", "message": "Method not allowed"})
		return
	}

	out, _ := exec.Command("docker", "ps", "-q").Output()
	ids := strings.Fields(string(out))
	if len(ids) > 0 {
		args := append([]string{"stop"}, ids...)
		_ = exec.Command("docker", args...).Run()
	}
	_ = json.NewEncoder(w).Encode(map[string]interface{}{"status": "ok", "stopped_count": len(ids)})
}

func getDashboardData() model.MobileDashboard {
	host := hostops.GetHostStats()
	ts := tailscaleops.GetTailscaleInfo()
	ag := agentops.GetAgentSummary()

	return model.MobileDashboard{
		Host:             host,
		Tailscale:        ts,
		ActiveAIAccount:  ag.ActiveAccount,
		AITokenQuotaUSD:  ag.QuotaUSD,
		AITokenCount:     ag.TokenCount,
		RunningContainers: ag.ContainersUp,
		TotalContainers:  ag.ContainersTotal,
		ActiveAgents:     ag.ActiveCount,
		RecentAgentStep:  ag.RecentStep,
	}
}

func handleIndex(w http.ResponseWriter, r *http.Request) {
	if r.URL.Path != "/" {
		http.NotFound(w, r)
		return
	}

	w.Header().Set("Content-Type", "text/html; charset=utf-8")
	w.Write([]byte(embeddedHTML))
}

const embeddedHTML = `<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="UTF-8">
  <meta name="viewport" content="width=device-width, initial-scale=1.0, maximum-scale=1.0, user-scalable=no">
  <meta name="theme-color" content="#090d16">
  <title>AGYMOBILE · Remote Cockpit</title>
  <style>
    * { box-sizing: border-box; margin: 0; padding: 0; -webkit-tap-highlight-color: transparent; }
    body {
      font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, Helvetica, Arial, sans-serif;
      background: #090d16;
      color: #e2e8f0;
      padding: 16px;
      display: flex;
      flex-direction: column;
      gap: 14px;
      min-height: 100vh;
    }
    header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding-bottom: 12px;
      border-bottom: 1px solid #1e293b;
    }
    .brand { display: flex; align-items: center; gap: 8px; font-weight: 700; font-size: 1.15rem; color: #38bdf8; }
    .status-pill {
      font-size: 0.75rem;
      padding: 4px 10px;
      border-radius: 999px;
      background: rgba(16, 185, 129, 0.15);
      color: #10b981;
      border: 1px solid rgba(16, 185, 129, 0.3);
      font-weight: 600;
    }
    .grid { display: grid; grid-template-columns: repeat(2, 1fr); gap: 10px; }
    .card {
      background: #0f172a;
      border: 1px solid #1e293b;
      border-radius: 12px;
      padding: 12px;
      display: flex;
      flex-direction: column;
      gap: 6px;
    }
    .card.full { grid-column: span 2; }
    .card-title { font-size: 0.75rem; color: #94a3b8; text-transform: uppercase; letter-spacing: 0.5px; font-weight: 600; }
    .card-val { font-size: 1.25rem; font-weight: 700; color: #f8fafc; }
    .card-sub { font-size: 0.75rem; color: #64748b; }
    
    .progress-bar {
      width: 100%;
      height: 6px;
      background: #1e293b;
      border-radius: 3px;
      overflow: hidden;
      margin-top: 4px;
    }
    .progress-fill { height: 100%; background: #38bdf8; width: 0%; transition: width 0.3s ease; }
    .progress-fill.warn { background: #f59e0b; }
    .progress-fill.danger { background: #ef4444; }

    .btn {
      background: #1e293b;
      color: #f8fafc;
      border: 1px solid #334155;
      padding: 12px;
      border-radius: 10px;
      font-weight: 600;
      font-size: 0.9rem;
      display: flex;
      align-items: center;
      justify-content: center;
      gap: 8px;
      cursor: pointer;
      transition: all 0.15s ease;
      touch-action: manipulation;
    }
    .btn:active { transform: scale(0.97); background: #334155; }
    .btn-primary { background: #0284c7; border-color: #38bdf8; }
    .btn-primary:active { background: #0369a1; }
    .btn-danger { background: rgba(239, 68, 68, 0.15); border-color: rgba(239, 68, 68, 0.4); color: #fca5a5; }
    .btn-danger:active { background: rgba(239, 68, 68, 0.3); }

    .log-box {
      background: #050811;
      border: 1px solid #1e293b;
      border-radius: 8px;
      padding: 10px;
      font-family: monospace;
      font-size: 0.78rem;
      color: #cbd5e1;
      min-height: 48px;
      word-break: break-all;
    }
    .toast {
      position: fixed;
      bottom: 20px;
      left: 50%;
      transform: translateX(-50%);
      background: #10b981;
      color: #042f2e;
      padding: 8px 16px;
      border-radius: 20px;
      font-weight: 700;
      font-size: 0.85rem;
      opacity: 0;
      transition: opacity 0.3s;
      pointer-events: none;
    }
    .toast.show { opacity: 1; }
  </style>
</head>
<body>
  <header>
    <div class="brand">📱 AGYMOBILE</div>
    <div id="ts-status" class="status-pill">Tailscale Online</div>
  </header>

  <div class="grid">
    <div class="card">
      <div class="card-title">WSL2 RAM Usage</div>
      <div id="ram-val" class="card-val">-- / -- GB</div>
      <div class="progress-bar"><div id="ram-bar" class="progress-fill"></div></div>
    </div>

    <div class="card">
      <div class="card-title">Docker Containers</div>
      <div id="docker-val" class="card-val">-- / -- Up</div>
      <div id="docker-sub" class="card-sub">Grouped Projects</div>
    </div>

    <div class="card">
      <div class="card-title">Active AI Profile</div>
      <div id="ai-val" class="card-val">--</div>
      <div id="ai-sub" class="card-sub">$0.00 · 0 tokens</div>
    </div>

    <div class="card">
      <div class="card-title">Tailscale IP</div>
      <div id="ip-val" class="card-val">100.x.x.x</div>
      <div id="magic-val" class="card-sub">MagicDNS</div>
    </div>

    <div class="card full">
      <div class="card-title">Live Antigravity Agent State</div>
      <div id="agent-box" class="log-box">Waiting for agent activity...</div>
    </div>
  </div>

  <div style="display: flex; flex-direction: column; gap: 10px; margin-top: 6px;">
    <button class="btn btn-primary" onclick="flushRAM()">🚀 Reclaim WSL2 RAM (Drop Cache)</button>
    <button class="btn btn-danger" onclick="stopDocker()">🛑 Stop All Running Containers</button>
  </div>

  <div id="toast" class="toast">Action completed</div>

  <script>
    const urlParams = new URLSearchParams(window.location.search);
    let token = urlParams.get('token') || '';
    if (token) {
      try { sessionStorage.setItem('agymobile_token', token); } catch(e){}
    } else {
      try { token = sessionStorage.getItem('agymobile_token') || ''; } catch(e){}
    }

    function authHeaders() {
      const h = { 'Content-Type': 'application/json' };
      if (token) {
        h['Authorization'] = 'Bearer ' + token;
      }
      return h;
    }

    function authUrl(endpoint) {
      if (!token) return endpoint;
      const sep = endpoint.includes('?') ? '&' : '?';
      return endpoint + sep + 'token=' + encodeURIComponent(token);
    }

    function showToast(msg) {
      const t = document.getElementById('toast');
      t.innerText = msg;
      t.classList.add('show');
      setTimeout(() => t.classList.remove('show'), 2000);
      if (navigator.vibrate) navigator.vibrate(50);
    }

    async function fetchStats() {
      try {
        const res = await fetch(authUrl('/api/stats'), { headers: authHeaders() });
        const d = await res.json();
        
        // RAM
        const ramUsedGB = (d.host.used_ram_mb / 1024).toFixed(1);
        const ramTotalGB = (d.host.total_ram_mb / 1024).toFixed(1);
        const ramPct = Math.round(d.host.ram_used_pct);
        document.getElementById('ram-val').innerText = ramUsedGB + ' / ' + ramTotalGB + ' GB';
        const bar = document.getElementById('ram-bar');
        bar.style.width = ramPct + '%';
        bar.className = 'progress-fill' + (ramPct > 85 ? ' danger' : (ramPct > 70 ? ' warn' : ''));

        // Docker
        document.getElementById('docker-val').innerText = d.running_containers + ' / ' + d.total_containers + ' Up';

        // AI Account
        document.getElementById('ai-val').innerText = d.active_ai_account || 'Default';
        document.getElementById('ai-sub').innerText = '$' + (d.ai_token_quota_usd || 0).toFixed(2) + ' · ' + (d.ai_token_count || 0).toLocaleString() + ' tok';

        // Tailscale
        document.getElementById('ip-val').innerText = d.tailscale.ipv4;
        document.getElementById('magic-val').innerText = d.tailscale.magic_dns || d.tailscale.hostname;

        // Agent
        if (d.recent_agent_step) {
          document.getElementById('agent-box').innerText = d.recent_agent_step;
        }
      } catch (e) {
        console.error('Stats poll error', e);
      }
    }

    async function flushRAM() {
      try {
        const res = await fetch(authUrl('/api/action/flush-ram'), {
          method: 'POST',
          headers: authHeaders()
        });
        const d = await res.json();
        if (res.status === 401) {
          showToast('Unauthorized: Check token');
          return;
        }
        showToast(d.message || 'RAM Reclaimed');
        fetchStats();
      } catch (e) {
        showToast('Failed to flush RAM');
      }
    }

    async function stopDocker() {
      if (!confirm('Stop all running Docker containers?')) return;
      try {
        const res = await fetch(authUrl('/api/action/docker-stop-all'), {
          method: 'POST',
          headers: authHeaders()
        });
        const d = await res.json();
        if (res.status === 401) {
          showToast('Unauthorized: Check token');
          return;
        }
        showToast('Stopped ' + (d.stopped_count !== undefined ? d.stopped_count : 0) + ' containers');
        fetchStats();
      } catch (e) {
        showToast('Failed to stop containers');
      }
    }

    fetchStats();
    setInterval(fetchStats, 3000);
  </script>
</body>
</html>
`
