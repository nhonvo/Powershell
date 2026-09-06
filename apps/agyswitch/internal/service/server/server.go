package server

import (
	"encoding/json"
	"fmt"
	"net/http"
	"strings"

	"agyswitch/internal/service/store"
)

type Server struct {
	Store *store.Store
	Port  int
}

func NewServer(s *store.Store, port int) *Server {
	if port <= 0 {
		port = 8080
	}
	return &Server{Store: s, Port: port}
}

func (s *Server) Routes() http.Handler {
	mux := http.NewServeMux()
	mux.HandleFunc("/api/v1/status", s.handleStatus)
	mux.HandleFunc("/api/v1/switch", s.handleSwitch)
	mux.HandleFunc("/", s.handleIndex)
	return mux
}

func (s *Server) Start() error {
	addr := fmt.Sprintf(":%d", s.Port)
	fmt.Printf("\033[36m[agyswitch]\033[0m Starting Mobile Web Sidecar on \033[32mhttp://localhost%s\033[0m\n", addr)
	return http.ListenAndServe(addr, s.Routes())
}

func (s *Server) handleStatus(w http.ResponseWriter, r *http.Request) {
	w.Header().Set("Content-Type", "application/json")
	accs := s.Store.ListAccounts()
	active := s.Store.GetActiveAccount()

	resp := map[string]interface{}{
		"activeAccount": active,
		"accounts":      accs,
	}
	_ = json.NewEncoder(w).Encode(resp)
}

func (s *Server) handleSwitch(w http.ResponseWriter, r *http.Request) {
	if r.Method != "POST" {
		http.Error(w, "Method not allowed", http.StatusMethodNotAllowed)
		return
	}

	var req struct {
		AccountName string `json:"accountName"`
	}
	if err := json.NewDecoder(r.Body).Decode(&req); err != nil || strings.TrimSpace(req.AccountName) == "" {
		http.Error(w, "Invalid payload", http.StatusBadRequest)
		return
	}

	if err := s.Store.SetActiveAccount(req.AccountName); err != nil {
		http.Error(w, err.Error(), http.StatusInternalServerError)
		return
	}

	w.Header().Set("Content-Type", "application/json")
	_ = json.NewEncoder(w).Encode(map[string]string{"status": "success", "activeAccount": req.AccountName})
}

func (s *Server) handleIndex(w http.ResponseWriter, r *http.Request) {
	w.Header().Set("Content-Type", "text/html; charset=utf-8")
	html := `<!DOCTYPE html>
<html>
<head>
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>AGYSWITCH Mobile Control</title>
    <style>
        body { font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif; background: #0f172a; color: #f8fafc; margin: 0; padding: 16px; }
        h1 { color: #38bdf8; font-size: 1.4rem; display: flex; align-items: center; gap: 8px; }
        .card { background: #1e293b; border-radius: 12px; padding: 16px; margin-bottom: 12px; border: 1px solid #334155; }
        .acc-row { display: flex; justify-content: space-between; align-items: center; padding: 12px 0; border-bottom: 1px solid #334155; }
        .btn { background: #0284c7; color: white; border: none; padding: 8px 16px; border-radius: 8px; font-weight: bold; cursor: pointer; }
        .btn-active { background: #16a34a; }
        .badge { background: #334155; padding: 4px 8px; border-radius: 4px; font-size: 0.8rem; }
    </style>
</head>
<body>
    <h1>🛸 AGYSWITCH Mobile Control</h1>
    <div class="card" id="app">Loading accounts...</div>
    <script>
        async function loadStatus() {
            const res = await fetch('/api/v1/status');
            const data = await res.json();
            let html = '<h2>Active: ' + data.activeAccount + '</h2>';
            data.accounts.forEach(a => {
                const isActive = a.accountName === data.activeAccount;
                html += '<div class="acc-row"><div><strong>' + a.accountName + '</strong><br><span class="badge">' + a.quotaStatus + '</span></div>';
                if (isActive) {
                    html += '<button class="btn btn-active">Active</button>';
                } else {
                    html += '<button class="btn" onclick="switchAcc(\'' + a.accountName + '\')">Switch</button>';
                }
                html += '</div>';
            });
            document.getElementById('app').innerHTML = html;
        }
        async function switchAcc(name) {
            await fetch('/api/v1/switch', { method: 'POST', body: JSON.stringify({ accountName: name }) });
            loadStatus();
        }
        loadStatus();
    </script>
</body>
</html>`
	_, _ = w.Write([]byte(html))
}
