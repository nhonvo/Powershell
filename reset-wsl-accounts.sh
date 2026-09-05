#!/usr/bin/env bash
# ==============================================================================
# reset-wsl-accounts.sh
# Safely nukes and reseeds Antigravity accounts to clean contexts in WSL2.
# Completely isolated to WSL2 (~/.gemini*) - DOES NOT TOUCH Windows Host (/mnt/c)
# Aligned with nuke-recreate-accounts.ps1 for complete parity.
# ==============================================================================
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

# Ensure dotnet is in PATH if installed in ~/.dotnet
export PATH="$HOME/.dotnet:$HOME/.dotnet/tools:$HOME/.local/bin:$PATH"
export DOTNET_ROOT="$HOME/.dotnet"

echo -e "\033[31m==================================================\033[0m"
echo -e "\033[31m 💣 NUKING & RE-CREATING ALL AGY ACCOUNTS (WSL2) \033[0m"
echo -e "\033[31m==================================================\033[0m"

# 1. Stop any background AgyTui / agy / antigravity processes in WSL
echo -e "\033[33m🛑 Stopping running agy/AgyTui/antigravity processes in WSL...\033[0m"
pkill -f "AgyTui" || true
pkill -f "agy" || true
pkill -f "antigravity" || true

# 1b. Ensure agyswitch Go binary is compiled and installed to ~/.local/bin/
mkdir -p "$HOME/.local/bin"
if [ -d "$REPO_ROOT/agyswitch-go" ]; then
    (cd "$REPO_ROOT/agyswitch-go" && go build -ldflags="-s -w" -o agyswitch)
    cp "$REPO_ROOT/agyswitch-go/agyswitch" "$HOME/.local/bin/agyswitch"
    chmod +x "$HOME/.local/bin/agyswitch"
    ln -sf "$HOME/.local/bin/agyswitch" "$HOME/.local/bin/agysw"
    ln -sf "$HOME/.local/bin/agyswitch" "$HOME/.local/bin/agyx"
    echo -e "   ✨ Compiled and installed unified agyswitch Go binary to $HOME/.local/bin/agyswitch"
fi

# 2. Complete Regex Removal, Database Nuke & Multi-Account Reseeding (WSL2 Isolated)
echo -e "\033[33m💣 Purging accounts, regex matching directories, and reseeding...\033[0m"

python3 - "$REPO_ROOT" << 'PYEOF'
import os
import sys
import re
import json
import sqlite3
import uuid
import shutil
from datetime import datetime, timezone

repo_root = os.path.abspath(sys.argv[1]) if len(sys.argv) > 1 else os.getcwd()
wsl_home = os.path.expanduser("~")

search_roots = [wsl_home]

accounts = {
    "fptvttnhon2020": "fptvttnhon2020@gmail.com",
    "fptvttnhon2026": "fptvttnhon2026@gmail.com",
    "nhontruongvo": "nhontruongvo@gmail.com",
    "nhontruongvo3": "nhontruongvo3@gmail.com",
    "vothuongtruongnhon2002": "vothuongtruongnhon2002@gmail.com"
}
active_acc = "vothuongtruongnhon2002"

auth_rel_paths = [
    "keyring_token.txt", "oauth_creds.json", "state.json", "session.json", "auth.json",
    "antigravity-oauth-token", "antigravity-cli/antigravity-oauth-token", "antigravity-cli/keyring_token.txt",
    ".keyring"
]

pattern = re.compile(r"^\.gemini(_.*)?$")
subdirs = ["antigravity", "antigravity-cli", "config", "history", "antigravity-ide", "wf", "learn"]

for target_root in search_roots:
    if not os.path.exists(target_root):
        continue

    # Step A: Regex Match to Completely Remove all .gemini_* Directories & Purge Auth Files
    for item in os.listdir(target_root):
        if pattern.match(item):
            target_dir = os.path.join(target_root, item)
            if os.path.isdir(target_dir):
                for rel in auth_rel_paths:
                    p = os.path.join(target_dir, rel)
                    if os.path.isfile(p):
                        try: os.remove(p)
                        except Exception: pass
                    elif os.path.isdir(p):
                        try: shutil.rmtree(p, ignore_errors=True)
                        except Exception: pass

                if item != ".gemini" and item.startswith(".gemini_"):
                    try:
                        shutil.rmtree(target_dir, ignore_errors=True)
                        print(f"   💣 Removed directory via regex: {target_dir}")
                    except Exception as ex:
                        print(f"   ⚠️ Could not remove {target_dir}: {ex}")

    # Step B: Re-create Clean Account Directories (Reseeding - Logged Out State)
    for name, email in accounts.items():
        acc_dir = os.path.join(target_root, f".gemini_{name}")
        os.makedirs(acc_dir, exist_ok=True)
        for sub in subdirs:
            os.makedirs(os.path.join(acc_dir, sub), exist_ok=True)

        with open(os.path.join(acc_dir, "installation_id"), "w", encoding="utf-8") as f:
            f.write(str(uuid.uuid4()))

        g_obj = {
            "accounts": [{"email": email}],
            "activeAccount": email
        }
        with open(os.path.join(acc_dir, "google_accounts.json"), "w", encoding="utf-8") as f:
            json.dump(g_obj, f, indent=2)

        s_obj = {
            "accountName": name,
            "userEmail": email
        }
        with open(os.path.join(acc_dir, "antigravity-cli", "settings.json"), "w", encoding="utf-8") as f:
            json.dump(s_obj, f, indent=2)

        print(f"   ✨ Re-created clean account context: '{name}' -> ({email}) in {target_root}")

    # Step C: Set Active Account Context to vothuongtruongnhon2002
    active_src = os.path.join(target_root, f".gemini_{active_acc}")
    primary_dir = os.path.join(target_root, ".gemini")

    os.makedirs(primary_dir, exist_ok=True)
    if os.path.exists(active_src):
        for item in os.listdir(active_src):
            s = os.path.join(active_src, item)
            d = os.path.join(primary_dir, item)
            if os.path.isdir(s):
                if os.path.exists(d):
                    try: shutil.rmtree(d, ignore_errors=True)
                    except Exception: pass
                try: shutil.copytree(s, d)
                except Exception: pass
            else:
                try: shutil.copy2(s, d)
                except Exception: pass

    with open(os.path.join(primary_dir, "active_account.txt"), "w", encoding="utf-8") as f:
        f.write(active_acc)

    print(f"   👉 Set active account context to: '{active_acc}' in {target_root}")

# Step D: Database Paths in WSL
db_paths = [
    os.path.join(wsl_home, ".gemini", "agytui.db"),
    os.path.join(wsl_home, ".gemini", "agytui.dev.db"),
    os.path.join(repo_root, ".gemini", "agytui.db"),
    os.path.join(repo_root, ".gemini", "agytui.dev.db"),
    os.path.join(repo_root, "csapp", "AgyTui", ".gemini", "agytui.db"),
    os.path.join(repo_root, "csapp", "AgyTui", ".gemini", "agytui.dev.db"),
    os.path.join(repo_root, "csapp", "AgyTui", "accounts.db")
]

now_iso = datetime.now(timezone.utc).isoformat()
metadata_default = json.dumps({
    "LastUsed": "Never",
    "UsageCount": 0,
    "QuotaStatus": "OK",
    "RequestHistory": [],
    "RemainingWeekly": None,
    "Remaining5H": None,
    "TimeWeekly": None,
    "Time5H": None
}, indent=2)

for db_path in db_paths:
    db_dir = os.path.dirname(db_path)
    os.makedirs(db_dir, exist_ok=True)
    try:
        conn = sqlite3.connect(db_path)
        c = conn.cursor()

        c.execute("""
            CREATE TABLE IF NOT EXISTS accounts (
                account_name TEXT PRIMARY KEY,
                email TEXT,
                keyring_token TEXT,
                google_accounts_json TEXT,
                oauth_creds_json TEXT,
                state_json TEXT,
                quota_status TEXT DEFAULT 'OK',
                last_used TEXT DEFAULT 'Never',
                usage_count INTEGER DEFAULT 0,
                request_history_json TEXT DEFAULT '[]',
                metadata_json TEXT DEFAULT '{}',
                is_active INTEGER DEFAULT 0,
                updated_at TEXT
            );
        """)

        c.execute("DELETE FROM accounts;")
        try:
            c.execute("DELETE FROM system_state WHERE state_key LIKE '%token%' OR state_key LIKE '%auth%' OR state_key LIKE '%account%';")
        except Exception:
            pass

        for name, email in accounts.items():
            g_json = json.dumps({
                "accounts": [{"email": email}],
                "activeAccount": email
            }, indent=2)
            is_act = 1 if name == active_acc else 0

            c.execute("""
                INSERT INTO accounts (account_name, email, keyring_token, google_accounts_json, oauth_creds_json, state_json, is_active, quota_status, last_used, usage_count, request_history_json, metadata_json, updated_at)
                VALUES (?, ?, NULL, ?, NULL, NULL, ?, 'OK', 'Never', 0, '[]', ?, ?)
            """, (name, email, g_json, is_act, metadata_default, now_iso))

        c.execute("""
            INSERT OR REPLACE INTO accounts (account_name, email, keyring_token, google_accounts_json, oauth_creds_json, state_json, is_active, quota_status, last_used, usage_count, request_history_json, metadata_json, updated_at)
            VALUES ('default', ?, NULL, ?, NULL, NULL, 0, 'OK', 'Never', 0, '[]', ?, ?)
        """, (accounts[active_acc], json.dumps({"accounts": [{"email": accounts[active_acc]}], "activeAccount": accounts[active_acc]}, indent=2), metadata_default, now_iso))

        conn.commit()
        conn.close()
        print(f"   🧹 Nuked & reseeded DB: {db_path}")
    except Exception as ex:
        print(f"   ⚠️ DB sync error ({db_path}): {ex}")
PYEOF

echo -e "\033[36m==================================================\033[0m"
echo -e "\033[32m ✔ All 5 Accounts Cleanly Re-created in WSL2!\033[0m"
echo -e "\033[32m   Logged out state forced (Key: None).\033[0m"
echo -e "\033[32m   Active account set to: 'vothuongtruongnhon2002'\033[0m"
echo -e "\033[36m==================================================\033[0m"




