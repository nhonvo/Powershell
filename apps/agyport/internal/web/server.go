package web

import (
	"encoding/json"
	"fmt"
	"net/http"
	"os/exec"
	"runtime"
	"strconv"
	"time"

	"agyport/internal/model"
	"agyport/internal/service/portops"
	"agyport/internal/service/ramops"
)

// StartServer launches the Web UI dashboard on the specified port
func StartServer(port int, autoOpen bool) error {
	addr := fmt.Sprintf("127.0.0.1:%d", port)
	mux := http.NewServeMux()

	mux.HandleFunc("/", handleIndex)
	mux.HandleFunc("/api/stats", handleStats)
	mux.HandleFunc("/api/kill", handleKill)
	mux.HandleFunc("/api/kill-pid", handleKillPID)
	mux.HandleFunc("/api/kill-all", handleKillAll)
	mux.HandleFunc("/api/reclaim", handleReclaim)

	url := fmt.Sprintf("http://127.0.0.1:%d", port)
	fmt.Printf("\r\n🌐 \033[1;36mAGYPORT Web UI Dashboard\033[0m listening on:\r\n")
	fmt.Printf("   • Dashboard URL: \033[1;32m%s\033[0m\r\n", url)
	fmt.Printf("   • Auto-refresh:  \033[33mActive (every 2.5s)\033[0m\r\n")
	fmt.Printf("   • Press \033[1mCtrl+C\033[0m to stop server.\r\n\r\n")

	if autoOpen {
		go func() {
			time.Sleep(300 * time.Millisecond)
			OpenBrowser(url)
		}()
	}

	return http.ListenAndServe(addr, mux)
}

// OpenBrowser attempts to open the default system web browser
func OpenBrowser(url string) {
	if runtime.GOOS == "windows" {
		_ = exec.Command("cmd.exe", "/c", "start", url).Start()
		return
	}

	// Check WSL2
	if _, err := exec.LookPath("cmd.exe"); err == nil {
		_ = exec.Command("cmd.exe", "/c", "start", url).Start()
		return
	}
	if _, err := exec.LookPath("wslview"); err == nil {
		_ = exec.Command("wslview", url).Start()
		return
	}
	if _, err := exec.LookPath("xdg-open"); err == nil {
		_ = exec.Command("xdg-open", url).Start()
		return
	}
}

func handleIndex(w http.ResponseWriter, r *http.Request) {
	if r.URL.Path != "/" {
		http.NotFound(w, r)
		return
	}
	w.Header().Set("Content-Type", "text/html; charset=utf-8")
	w.Write([]byte(dashboardHTML))
}

type StatsResponse struct {
	Ports       []model.PortInfo       `json:"ports"`
	Memory      *model.MemorySummary   `json:"memory"`
	Suggestions []model.RAMSuggestion  `json:"suggestions"`
	TopProcs    []model.ProcessMemInfo `json:"top_procs"`
	Timestamp   string                 `json:"timestamp"`
}

func handleStats(w http.ResponseWriter, r *http.Request) {
	ports, _ := portops.ListPorts()
	mem, _ := ramops.GetMemorySummary()
	procs, _ := ramops.GetTopMemoryProcesses(20)
	suggestions := ramops.GenerateRAMSuggestions(ports, procs, mem)

	resp := StatsResponse{
		Ports:       ports,
		Memory:      mem,
		Suggestions: suggestions,
		TopProcs:    procs,
		Timestamp:   time.Now().Format("15:04:05"),
	}

	w.Header().Set("Content-Type", "application/json")
	_ = json.NewEncoder(w).Encode(resp)
}

func handleKill(w http.ResponseWriter, r *http.Request) {
	portStr := r.URL.Query().Get("port")
	if portStr == "" {
		http.Error(w, "missing port parameter", http.StatusBadRequest)
		return
	}
	port, err := strconv.Atoi(portStr)
	if err != nil {
		http.Error(w, "invalid port parameter", http.StatusBadRequest)
		return
	}

	force := r.URL.Query().Get("force") == "true"
	res, err := portops.KillPort(port, force)
	w.Header().Set("Content-Type", "application/json")
	if err != nil {
		w.WriteHeader(http.StatusInternalServerError)
		_ = json.NewEncoder(w).Encode(map[string]any{"success": false, "error": err.Error(), "port": port})
		return
	}
	_ = json.NewEncoder(w).Encode(res)
}

func handleKillPID(w http.ResponseWriter, r *http.Request) {
	pidStr := r.URL.Query().Get("pid")
	if pidStr == "" {
		http.Error(w, "missing pid parameter", http.StatusBadRequest)
		return
	}
	pid, err := strconv.Atoi(pidStr)
	if err != nil {
		http.Error(w, "invalid pid parameter", http.StatusBadRequest)
		return
	}

	force := r.URL.Query().Get("force") == "true"
	err = portops.KillPID(pid, force)
	w.Header().Set("Content-Type", "application/json")
	if err != nil {
		w.WriteHeader(http.StatusInternalServerError)
		_ = json.NewEncoder(w).Encode(map[string]any{"success": false, "error": err.Error(), "pid": pid})
		return
	}
	_ = json.NewEncoder(w).Encode(map[string]any{"success": true, "pid": pid})
}

func handleKillAll(w http.ResponseWriter, r *http.Request) {
	force := r.URL.Query().Get("force") == "true"
	results, err := portops.KillAllDevPorts(force)
	w.Header().Set("Content-Type", "application/json")
	if err != nil {
		w.WriteHeader(http.StatusInternalServerError)
		_ = json.NewEncoder(w).Encode(map[string]any{"success": false, "error": err.Error()})
		return
	}
	_ = json.NewEncoder(w).Encode(results)
}

func handleReclaim(w http.ResponseWriter, r *http.Request) {
	dryRun := r.URL.Query().Get("dry_run") == "true"
	res, err := ramops.ReclaimDevRAM(dryRun)
	w.Header().Set("Content-Type", "application/json")
	if err != nil {
		w.WriteHeader(http.StatusInternalServerError)
		_ = json.NewEncoder(w).Encode(map[string]any{"success": false, "error": err.Error()})
		return
	}
	_ = json.NewEncoder(w).Encode(res)
}

const dashboardHTML = `<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="UTF-8">
  <meta name="viewport" content="width=device-width, initial-scale=1.0">
  <title>AGYPORT — Port & RAM Master Dashboard</title>
  <style>
    :root {
      --bg: #0d1117;
      --card-bg: #161b22;
      --card-border: #30363d;
      --text-main: #c9d1d9;
      --text-bright: #f0f6fc;
      --text-muted: #8b949e;
      --primary: #58a6ff;
      --success: #3fb950;
      --warning: #d29922;
      --danger: #f85149;
      --purple: #bc8cff;
      --font: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, Helvetica, Arial, sans-serif;
    }
    * { box-sizing: border-box; margin: 0; padding: 0; }
    body {
      background-color: var(--bg);
      color: var(--text-main);
      font-family: var(--font);
      line-height: 1.5;
      padding: 24px;
    }
    .container { max-width: 1200px; margin: 0 auto; }
    header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      flex-wrap: wrap;
      gap: 16px;
      margin-bottom: 24px;
      padding-bottom: 16px;
      border-bottom: 1px solid var(--card-border);
    }
    .logo-group h1 {
      font-size: 24px;
      font-weight: 700;
      color: var(--text-bright);
      display: flex;
      align-items: center;
      gap: 8px;
    }
    .logo-badge {
      background: linear-gradient(135deg, #1f6feb, #238636);
      color: #fff;
      font-size: 11px;
      padding: 2px 8px;
      border-radius: 12px;
      font-weight: 600;
      letter-spacing: 0.5px;
    }
    .header-actions { display: flex; align-items: center; gap: 10px; flex-wrap: wrap; }
    button {
      cursor: pointer;
      font-family: inherit;
      font-size: 13px;
      font-weight: 600;
      padding: 8px 14px;
      border-radius: 6px;
      border: 1px solid transparent;
      transition: all 0.15s ease-in-out;
      display: inline-flex;
      align-items: center;
      gap: 6px;
    }
    .btn-primary { background: #238636; color: #fff; border-color: rgba(240,246,252,0.1); }
    .btn-primary:hover { background: #2ea043; }
    .btn-danger { background: #da3633; color: #fff; border-color: rgba(240,246,252,0.1); }
    .btn-danger:hover { background: #f85149; }
    .btn-outline { background: #21262d; color: var(--text-main); border-color: var(--card-border); }
    .btn-outline:hover { background: #30363d; color: var(--text-bright); }
    .metrics-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(260px, 1fr));
      gap: 16px;
      margin-bottom: 24px;
    }
    .card {
      background: var(--card-bg);
      border: 1px solid var(--card-border);
      border-radius: 8px;
      padding: 16px 20px;
    }
    .card-title {
      font-size: 13px;
      font-weight: 600;
      color: var(--text-muted);
      text-transform: uppercase;
      letter-spacing: 0.5px;
      margin-bottom: 8px;
      display: flex;
      justify-content: space-between;
    }
    .metric-value { font-size: 24px; font-weight: 700; color: var(--text-bright); }
    .metric-sub { font-size: 12px; color: var(--text-muted); margin-top: 4px; }
    .progress-bar-bg {
      height: 8px;
      background: #21262d;
      border-radius: 4px;
      overflow: hidden;
      margin-top: 10px;
    }
    .progress-bar-fill {
      height: 100%;
      background: var(--primary);
      border-radius: 4px;
      transition: width 0.4s ease;
    }
    .progress-bar-fill.warning { background: var(--warning); }
    .progress-bar-fill.danger { background: var(--danger); }
    .suggestion-box {
      background: rgba(88, 166, 255, 0.08);
      border: 1px solid rgba(88, 166, 255, 0.3);
      border-radius: 8px;
      padding: 16px 20px;
      margin-bottom: 24px;
      display: none;
    }
    .suggestion-box.active { display: block; }
    .suggestion-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 10px;
    }
    .suggestion-header h3 { font-size: 15px; color: var(--primary); font-weight: 600; }
    .suggestion-item {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding: 10px 0;
      border-top: 1px solid rgba(88, 166, 255, 0.15);
      gap: 12px;
      flex-wrap: wrap;
    }
    .table-container {
      background: var(--card-bg);
      border: 1px solid var(--card-border);
      border-radius: 8px;
      overflow: hidden;
      margin-bottom: 24px;
    }
    .table-header-row {
      padding: 14px 20px;
      display: flex;
      justify-content: space-between;
      align-items: center;
      gap: 16px;
      border-bottom: 1px solid var(--card-border);
      flex-wrap: wrap;
    }
    .table-header-row h2 { font-size: 16px; font-weight: 600; color: var(--text-bright); }
    .search-box {
      background: #0d1117;
      border: 1px solid var(--card-border);
      border-radius: 6px;
      padding: 6px 12px;
      color: var(--text-bright);
      font-size: 13px;
      width: 260px;
      outline: none;
    }
    .search-box:focus { border-color: var(--primary); }
    table { width: 100%; border-collapse: collapse; text-align: left; font-size: 13px; }
    th {
      background: #1c2128;
      color: var(--text-muted);
      font-weight: 600;
      padding: 12px 16px;
      border-bottom: 1px solid var(--card-border);
    }
    td {
      padding: 12px 16px;
      border-bottom: 1px solid var(--card-border);
      vertical-align: middle;
    }
    tr:last-child td { border-bottom: none; }
    tr:hover { background: rgba(110, 118, 129, 0.05); }
    .badge {
      display: inline-block;
      padding: 2px 8px;
      border-radius: 12px;
      font-size: 11px;
      font-weight: 600;
    }
    .badge-port { background: rgba(88,166,255,0.15); color: var(--primary); font-family: monospace; font-size: 12px; }
    .badge-frontend { background: rgba(56,189,248,0.15); color: #38bdf8; }
    .badge-backend { background: rgba(52,211,153,0.15); color: #34d399; }
    .badge-database { background: rgba(251,191,36,0.15); color: #fbbf24; }
    .badge-ai { background: rgba(192,132,252,0.15); color: #c084fc; }
    .badge-system { background: rgba(148,163,184,0.15); color: #94a3b8; }
    .badge-pid { background: #21262d; color: var(--text-muted); font-family: monospace; }
    .btn-sm { padding: 4px 10px; font-size: 12px; }
    .toast {
      position: fixed;
      bottom: 24px;
      right: 24px;
      background: #1f6feb;
      color: #fff;
      padding: 12px 20px;
      border-radius: 6px;
      box-shadow: 0 8px 24px rgba(0,0,0,0.4);
      display: none;
      z-index: 1000;
      font-size: 14px;
      font-weight: 500;
    }
    .toast.active { display: block; animation: fadeIn 0.2s ease; }
    @keyframes fadeIn { from { opacity: 0; transform: translateY(10px); } to { opacity: 1; transform: translateY(0); } }
  </style>
</head>
<body>
  <div class="container">
    <header>
      <div class="logo-group">
        <h1>⚡ AGYPORT <span class="logo-badge">PORT & RAM MASTER</span></h1>
      </div>
      <div class="header-actions">
        <button class="btn-primary" onclick="reclaimDevRAM()">🧹 Reclaim Dev RAM</button>
        <button class="btn-danger" onclick="killAllDevPorts()">⚡ Kill All Dev Ports</button>
        <button class="btn-outline" onclick="fetchData()">🔄 Refresh</button>
        <label style="font-size: 12px; color: var(--text-muted); display: flex; align-items: center; gap: 4px;">
          <input type="checkbox" id="autoRefreshToggle" checked> Live Polling
        </label>
      </div>
    </header>

    <div class="metrics-grid">
      <div class="card">
        <div class="card-title">Host & WSL2 RAM <span id="ramPct">0%</span></div>
        <div class="metric-value" id="ramUsed">-</div>
        <div class="metric-sub" id="ramSub">Available: -</div>
        <div class="progress-bar-bg"><div class="progress-bar-fill" id="ramBar" style="width: 0%"></div></div>
      </div>
      <div class="card">
        <div class="card-title">WSL2 Swap <span id="swapPct">0%</span></div>
        <div class="metric-value" id="swapUsed">-</div>
        <div class="metric-sub" id="swapSub">Total Swap: -</div>
        <div class="progress-bar-bg"><div class="progress-bar-fill" id="swapBar" style="width: 0%"></div></div>
      </div>
      <div class="card">
        <div class="card-title">Listening Ports</div>
        <div class="metric-value" id="activePortsCount">-</div>
        <div class="metric-sub" id="devPortsCount">Developer servers active</div>
      </div>
      <div class="card">
        <div class="card-title">Reclaimable Dev RAM</div>
        <div class="metric-value" id="reclaimableRAM" style="color: var(--success);">-</div>
        <div class="metric-sub" id="reclaimableSub">No dead dev servers</div>
      </div>
    </div>

    <div class="suggestion-box" id="suggestionBox">
      <div class="suggestion-header">
        <h3>💡 Smart RAM Leverage Suggestions</h3>
        <span style="font-size: 12px; color: var(--primary);" id="suggestionCount">0 suggestions</span>
      </div>
      <div id="suggestionList"></div>
    </div>

    <div class="table-container">
      <div class="table-header-row">
        <h2>🌐 Active Listening Ports (<span id="tablePortCount">0</span>)</h2>
        <input type="text" id="searchInput" class="search-box" placeholder="🔍 Search port, process, framework..." oninput="filterTable()">
      </div>
      <table>
        <thead>
          <tr>
            <th>PORT</th>
            <th>PROTO</th>
            <th>PID</th>
            <th>PROCESS</th>
            <th>FRAMEWORK</th>
            <th>RAM USAGE</th>
            <th>USER & BIND</th>
            <th style="text-align: right;">ACTION</th>
          </tr>
        </thead>
        <tbody id="portsTableBody">
          <tr><td colspan="8" style="text-align: center; color: var(--text-muted); padding: 24px;">Loading ports...</td></tr>
        </tbody>
      </table>
    </div>

    <div class="table-container">
      <div class="table-header-row">
        <h2>🔥 Top Memory Consuming Processes</h2>
      </div>
      <table>
        <thead>
          <tr>
            <th>PID</th>
            <th>PROCESS</th>
            <th>FRAMEWORK</th>
            <th>RAM (RSS)</th>
            <th>% OF RAM</th>
            <th>PORTS</th>
            <th>USER</th>
            <th style="text-align: right;">ACTION</th>
          </tr>
        </thead>
        <tbody id="topProcsTableBody">
          <tr><td colspan="8" style="text-align: center; color: var(--text-muted); padding: 24px;">Loading processes...</td></tr>
        </tbody>
      </table>
    </div>
  </div>

  <div class="toast" id="toastMsg"></div>

  <script>
    var globalData = null;

    function fetchData() {
      fetch('/api/stats')
        .then(function(res) { return res.json(); })
        .then(function(data) {
          globalData = data;
          renderDashboard(data);
        })
        .catch(function(err) {
          console.error("Failed to fetch stats:", err);
        });
    }

    function renderDashboard(data) {
      if (data.memory) {
        var m = data.memory;
        document.getElementById('ramUsed').innerText = m.used_formatted;
        document.getElementById('ramPct').innerText = m.used_percent.toFixed(1) + '%';
        document.getElementById('ramSub').innerText = 'Available: ' + m.available_formatted + ' / ' + m.total_formatted;
        var ramBar = document.getElementById('ramBar');
        ramBar.style.width = Math.min(m.used_percent, 100) + '%';
        ramBar.className = 'progress-bar-fill' + (m.used_percent > 85 ? ' danger' : m.used_percent > 70 ? ' warning' : '');

        if (m.swap_total_bytes > 0) {
          document.getElementById('swapUsed').innerText = m.swap_used_formatted;
          document.getElementById('swapPct').innerText = m.swap_used_percent.toFixed(1) + '%';
          document.getElementById('swapSub').innerText = 'Total Swap: ' + (m.swap_total_bytes / (1024*1024*1024)).toFixed(1) + ' GB';
          document.getElementById('swapBar').style.width = Math.min(m.swap_used_percent, 100) + '%';
        }
      }

      var ports = data.ports || [];
      document.getElementById('activePortsCount').innerText = ports.length;
      document.getElementById('tablePortCount').innerText = ports.length;

      var devServers = 0;
      var reclaimBytes = 0;
      ports.forEach(function(p) {
        if (!p.is_system_port && p.pid > 0) {
          devServers++;
          reclaimBytes += p.memory_bytes;
        }
      });
      document.getElementById('devPortsCount').innerText = devServers + ' dev processes running';
      document.getElementById('reclaimableRAM').innerText = formatBytes(reclaimBytes);
      document.getElementById('reclaimableSub').innerText = devServers + ' dev processes reclaimable';

      // Suggestions
      var suggestions = data.suggestions || [];
      var sugBox = document.getElementById('suggestionBox');
      if (suggestions.length > 0) {
        sugBox.classList.add('active');
        document.getElementById('suggestionCount').innerText = suggestions.length + ' suggestions';
        var sugHtml = '';
        suggestions.forEach(function(s) {
          sugHtml += '<div class="suggestion-item">' +
            '<div><strong>' + s.title + '</strong> <span style="color: var(--success); font-weight:600;">(+' + s.reclaimable_formatted + ' reclaimable)</span>' +
            '<div style="font-size: 12px; color: var(--text-muted);">' + s.description + '</div></div>' +
            '<button class="btn-primary btn-sm" onclick="applySuggestion(\'' + s.action_type + '\')">Reclaim Now</button>' +
            '</div>';
        });
        document.getElementById('suggestionList').innerHTML = sugHtml;
      } else {
        sugBox.classList.remove('active');
      }

      renderPortsTable(ports);
      renderTopProcsTable(data.top_procs || []);
    }

    function renderPortsTable(ports) {
      var q = document.getElementById('searchInput').value.toLowerCase().trim();
      var filtered = ports.filter(function(p) {
        if (!q) return true;
        return p.port.toString().indexOf(q) !== -1 ||
               (p.process_name && p.process_name.toLowerCase().indexOf(q) !== -1) ||
               (p.framework && p.framework.toLowerCase().indexOf(q) !== -1) ||
               (p.user && p.user.toLowerCase().indexOf(q) !== -1);
      });

      var tbody = document.getElementById('portsTableBody');
      if (filtered.length === 0) {
        tbody.innerHTML = '<tr><td colspan="8" style="text-align: center; color: var(--text-muted); padding: 24px;">No matching ports found.</td></tr>';
        return;
      }

      var html = '';
      filtered.forEach(function(p) {
        var fwBadgeClass = getBadgeClass(p.dev_category);
        var memStr = p.memory_bytes > 0 ? (p.memory_formatted + ' (' + p.memory_percent.toFixed(1) + '%)') : '-';
        var killBtn = p.is_system_port 
          ? '<span style="color: var(--text-muted); font-size: 11px;">Protected</span>' 
          : '<button class="btn-danger btn-sm" onclick="killPort(' + p.port + ')">Kill :' + p.port + '</button>';

        html += '<tr>' +
          '<td><span class="badge badge-port">' + p.port + '</span></td>' +
          '<td><span style="font-size:11px; font-weight:600;">' + p.protocol.toUpperCase() + '</span></td>' +
          '<td><span class="badge badge-pid">' + (p.pid > 0 ? p.pid : '-') + '</span></td>' +
          '<td><strong>' + (p.process_name || '-') + '</strong></td>' +
          '<td><span class="badge ' + fwBadgeClass + '">' + (p.framework || '-') + '</span></td>' +
          '<td>' + memStr + '</td>' +
          '<td style="color: var(--text-muted);">' + (p.user || '-') + ' @ ' + p.bind_address + '</td>' +
          '<td style="text-align: right;">' + killBtn + '</td>' +
          '</tr>';
      });
      tbody.innerHTML = html;
    }

    function renderTopProcsTable(procs) {
      var tbody = document.getElementById('topProcsTableBody');
      if (procs.length === 0) {
        tbody.innerHTML = '<tr><td colspan="8" style="text-align: center; color: var(--text-muted); padding: 24px;">No active processes.</td></tr>';
        return;
      }

      var html = '';
      procs.slice(0, 8).forEach(function(p) {
        var portsStr = p.ports && p.ports.length > 0 ? p.ports.join(', ') : '-';
        var devBadge = p.is_dev_server ? '<span class="badge badge-backend">Dev Server</span>' : '<span style="color: var(--text-muted);">No</span>';
        var killBtn = p.pid > 1 ? '<button class="btn-danger btn-sm" onclick="killPID(' + p.pid + ')">Kill PID</button>' : '';

        html += '<tr>' +
          '<td><span class="badge badge-pid">' + p.pid + '</span></td>' +
          '<td><strong>' + p.name + '</strong></td>' +
          '<td>' + devBadge + '</td>' +
          '<td style="color: var(--danger); font-weight: 600;">' + p.rss_formatted + '</td>' +
          '<td>' + p.rss_percent.toFixed(1) + '%</td>' +
          '<td><span class="badge badge-port">' + portsStr + '</span></td>' +
          '<td style="color: var(--text-muted);">' + p.user + '</td>' +
          '<td style="text-align: right;">' + killBtn + '</td>' +
          '</tr>';
      });
      tbody.innerHTML = html;
    }

    function filterTable() {
      if (globalData && globalData.ports) {
        renderPortsTable(globalData.ports);
      }
    }

    function killPort(port) {
      if (!confirm('Are you sure you want to terminate process on port ' + port + '?')) return;
      fetch('/api/kill?port=' + port, { method: 'POST' })
        .then(function(res) { return res.json(); })
        .then(function(data) {
          if (data.success) {
            showToast('✔ Successfully terminated port ' + port);
            fetchData();
          } else {
            showToast('✖ Failed: ' + (data.error || 'unknown error'));
          }
        })
        .catch(function(err) {
          showToast('✖ Network error: ' + err);
        });
    }

    function killPID(pid) {
      if (!confirm('Are you sure you want to terminate PID ' + pid + '?')) return;
      fetch('/api/kill-pid?pid=' + pid, { method: 'POST' })
        .then(function(res) { return res.json(); })
        .then(function(data) {
          if (data.success) {
            showToast('✔ Successfully terminated PID ' + pid);
            fetchData();
          } else {
            showToast('✖ Failed: ' + (data.error || 'unknown error'));
          }
        })
        .catch(function(err) {
          showToast('✖ Network error: ' + err);
        });
    }

    function killAllDevPorts() {
      if (!confirm('⚠️ Are you sure you want to terminate ALL developer server ports?')) return;
      fetch('/api/kill-all', { method: 'POST' })
        .then(function(res) { return res.json(); })
        .then(function(data) {
          showToast('✔ Terminated ' + (data.length || 0) + ' developer ports');
          fetchData();
        })
        .catch(function(err) {
          showToast('✖ Failed to kill dev ports: ' + err);
        });
    }

    function reclaimDevRAM() {
      fetch('/api/reclaim', { method: 'POST' })
        .then(function(res) { return res.json(); })
        .then(function(data) {
          showToast('✔ Reclaimed ' + data.freed_formatted + ' RAM across ' + (data.killed_pids ? data.killed_pids.length : 0) + ' dev processes');
          fetchData();
        })
        .catch(function(err) {
          showToast('✖ Reclaim failed: ' + err);
        });
    }

    function applySuggestion(actionType) {
      reclaimDevRAM();
    }

    function getBadgeClass(cat) {
      switch(cat) {
        case 'Frontend': return 'badge-frontend';
        case 'Backend':
        case 'Web Server': return 'badge-backend';
        case 'Database': return 'badge-database';
        case 'AI/ML': return 'badge-ai';
        default: return 'badge-system';
      }
    }

    function formatBytes(bytes) {
      if (!bytes || bytes === 0) return '0 B';
      var k = 1024;
      var sizes = ['B', 'KB', 'MB', 'GB'];
      var i = Math.floor(Math.log(bytes) / Math.log(k));
      return (bytes / Math.pow(k, i)).toFixed(1) + ' ' + sizes[i];
    }

    function showToast(msg) {
      var toast = document.getElementById('toastMsg');
      toast.innerText = msg;
      toast.classList.add('active');
      setTimeout(function() { toast.classList.remove('active'); }, 3500);
    }

    // Initial Load & Live Polling
    fetchData();
    setInterval(function() {
      if (document.getElementById('autoRefreshToggle').checked) {
        fetchData();
      }
    }, 2500);
  </script>
</body>
</html>`
