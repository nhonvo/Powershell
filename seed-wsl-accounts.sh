#!/usr/bin/env bash
# ==============================================================================
# seed-wsl-accounts.sh
# Provisions and seeds multi-account directories and SQLite database records
# for Antigravity Switch (agysw) on Linux / WSL.
# Keeps main logic cleanly separated from ad-hoc account provisioning.
# ==============================================================================
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

echo "=================================================="
echo " 🌱 Seeding Antigravity Multi-Accounts (Linux/WSL)"
echo "=================================================="

# Account Definitions (Mapping account name -> email)
python3 - << 'PYEOF'
import os
import json
import sqlite3
import uuid
from datetime import datetime, timezone

accounts = {
    "default": "fptvttnhon2020@gmail.com",
    "fptvttnhon2020": "fptvttnhon2020@gmail.com",
    "fptvttnhon2026": "fptvttnhon2026@gmail.com",
    "nhontruongvo": "nhontruongvo@gmail.com",
    "nhontruongvo3": "nhontruongvo3@gmail.com",
    "vothuongtruongnhon2002": "vothuongtruongnhon2002@gmail.com"
}

home = os.path.expanduser("~")
repo_root = os.path.abspath(".")

db_paths = [
    os.path.join(home, ".gemini", "agytui.db"),
    os.path.join(home, ".gemini", "agytui.dev.db"),
    os.path.join(repo_root, ".gemini", "agytui.db"),
    os.path.join(repo_root, ".gemini", "agytui.dev.db")
]

subdirs = ["antigravity", "antigravity-cli", "config", "history", "antigravity-ide", "wf", "learn"]

# 1. Create directory structures and google_accounts.json / settings.json
for name, email in accounts.items():
    if name == "default":
        acc_dir = os.path.join(home, ".gemini")
    else:
        acc_dir = os.path.join(home, f".gemini_{name}")

    os.makedirs(acc_dir, exist_ok=True)
    for sub in subdirs:
        os.makedirs(os.path.join(acc_dir, sub), exist_ok=True)

    # installation_id
    inst_file = os.path.join(acc_dir, "installation_id")
    if not os.path.exists(inst_file):
        with open(inst_file, "w", encoding="utf-8") as f:
            f.write(str(uuid.uuid4()))

    # google_accounts.json
    g_obj = {
        "accounts": [{"email": email}],
        "activeAccount": email
    }
    with open(os.path.join(acc_dir, "google_accounts.json"), "w", encoding="utf-8") as f:
        json.dump(g_obj, f, indent=2)

    # settings.json
    s_obj = {
        "accountName": name,
        "userEmail": email
    }
    with open(os.path.join(acc_dir, "antigravity-cli", "settings.json"), "w", encoding="utf-8") as f:
        json.dump(s_obj, f, indent=2)

    print(f"   ✓ Provisioned directory: {acc_dir} -> {name} ({email})")

# 2. Register accounts in SQLite DBs
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

now_iso = datetime.now(timezone.utc).isoformat()

for db_path in db_paths:
    if not os.path.exists(db_path):
        continue
    try:
        conn = sqlite3.connect(db_path)
        c = conn.cursor()

        # Check if table exists
        c.execute("SELECT name FROM sqlite_master WHERE type='table' AND name='accounts';")
        if not c.fetchone():
            conn.close()
            continue

        for name, email in accounts.items():
            g_json = json.dumps({
                "accounts": [{"email": email}],
                "activeAccount": email
            }, indent=2)

            c.execute("""
                INSERT INTO accounts (account_name, email, is_active, quota_status, last_used, usage_count, request_history_json, metadata_json, google_accounts_json, updated_at)
                VALUES (?, ?, 0, 'OK', 'Never', 0, '[]', ?, ?, ?)
                ON CONFLICT(account_name) DO UPDATE SET
                    email = excluded.email,
                    google_accounts_json = excluded.google_accounts_json,
                    updated_at = excluded.updated_at;
            """, (name, email, metadata_default, g_json, now_iso))

        # Ensure default is active if no active account
        c.execute("SELECT COUNT(*) FROM accounts WHERE is_active = 1;")
        active_count = c.fetchone()[0]
        if active_count == 0:
            c.execute("UPDATE accounts SET is_active = 1 WHERE account_name = 'default';")

        conn.commit()
        conn.close()
        print(f"   ✓ Synced accounts to DB: {db_path}")
    except Exception as ex:
        print(f"   ⚠️ DB sync error ({db_path}): {ex}")
PYEOF

echo ""
echo "=================================================="
echo " ✨ Seeding Completed Successfully!"
echo "    Accounts are now available in AGYSWITCH Manager."
echo "=================================================="
