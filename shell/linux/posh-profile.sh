# ==============================================================================
#  ENHANCED LINUX SHELL ENVIRONMENT (Universal Bash & Zsh Profile)
#  Converted from Windows PowerShell Profile: https://github.com/nhonvo/powershell
# ==============================================================================

# --- 1. DYNAMIC REPO & PATH RESOLUTION ---
if [ -n "$ZSH_VERSION" ]; then
    _CURRENT_SCRIPT="${(%):-%N}"
    [ -z "$_CURRENT_SCRIPT" ] && _CURRENT_SCRIPT="$0"
    CURRENT_SCRIPT_DIR="${_CURRENT_SCRIPT:A:h}"
elif [ -n "$BASH_VERSION" ]; then
    CURRENT_SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" 2>/dev/null && pwd)"
else
    CURRENT_SCRIPT_DIR="$HOME/projects/Powershell/shell/linux"
fi

if [ -d "$CURRENT_SCRIPT_DIR/../../apps" ]; then
    export REPO_ROOT="$(cd "$CURRENT_SCRIPT_DIR/../.." && pwd)"
elif [ -d "$CURRENT_SCRIPT_DIR/../apps" ]; then
    export REPO_ROOT="$(cd "$CURRENT_SCRIPT_DIR/.." && pwd)"
else
    export REPO_ROOT="$HOME/projects/Powershell"
fi

export POSH_THEMES_PATH="$REPO_ROOT/shell/assets/powershell-themes"
export PATH="$HOME/.local/bin:$HOME/.dotnet:$HOME/.dotnet/tools:$PATH"
export DOTNET_ROOT="$HOME/.dotnet"

# --- 2. PROMPT & THEME (Oh My Posh with fallback to Starship) ---
if [ -f "$HOME/.config/selected_posh_theme.txt" ]; then
    POSH_THEME=$(tr -d '[:space:]' < "$HOME/.config/selected_posh_theme.txt")
else
    POSH_THEME="neko"
fi

# Interactive Theme Switcher Function
theme() {
    local theme_dir="$POSH_THEMES_PATH"
    if [ -z "$1" ]; then
        echo -e "\033[36m🎨 Current Theme:\033[0m \033[32m${POSH_THEME:-neko}\033[0m"
        echo -e "\033[33mUsage:\033[0m theme <theme-name>"
        echo -e "\033[36mAvailable themes in your repository (80+):\033[0m"
        if [ -d "$theme_dir" ]; then
            command ls "$theme_dir" | grep '\.omp\.json$' | sed 's/\.omp\.json$//' | column -c 80
        fi
        return 0
    fi

    local target_theme="$1"
    local config_file="$theme_dir/${target_theme}.omp.json"
    if [ ! -f "$config_file" ]; then
        echo -e "\033[31m❌ Theme not found: $target_theme\033[0m"
        return 1
    fi

    mkdir -p "$HOME/.config"
    echo "$target_theme" > "$HOME/.config/selected_posh_theme.txt"
    export POSH_THEME="$target_theme"
    if command -v oh-my-posh >/dev/null 2>&1; then
        if [ -n "$ZSH_VERSION" ]; then
            eval "$(oh-my-posh init zsh --config "$config_file")"
        else
            eval "$(oh-my-posh init bash --config "$config_file")"
        fi
    fi
    echo -e "\033[32m✨ Applied Oh My Posh theme: $target_theme\033[0m"
}

# --- 3. READLINE / KEYBOARD NAVIGATION ---
if [ -n "$BASH_VERSION" ]; then
    HISTSIZE=50000
    HISTFILESIZE=50000
    shopt -s histappend 2>/dev/null || true
    # Arrow up/down prefix search
    bind '"\e[A": history-search-backward' 2>/dev/null || true
    bind '"\e[B": history-search-forward' 2>/dev/null || true
    # Ctrl+Backspace to delete word
    bind '"\C-h": backward-kill-word' 2>/dev/null || true
    bind '"\C-w": backward-kill-word' 2>/dev/null || true
elif [ -n "$ZSH_VERSION" ]; then
    HISTSIZE=50000
    SAVEHIST=50000
    HISTFILE="$HOME/.zsh_history"
    setopt EXTENDED_HISTORY HIST_IGNORE_DUPS HIST_IGNORE_SPACE SHARE_HISTORY 2>/dev/null || true
    bindkey '^[[A' history-beginning-search-backward 2>/dev/null || true
    bindkey '^[[B' history-beginning-search-forward 2>/dev/null || true
    bindkey '^H' backward-kill-word 2>/dev/null || true
    bindkey '^W' backward-kill-word 2>/dev/null || true
fi

# --- 4. TERMINAL ICONS & MODERN LS (eza) ---
if command -v eza >/dev/null 2>&1; then
    alias ls="eza --icons --group-directories-first"
    alias l="eza -la --icons --git --group-directories-first"
    alias ll="eza -l --icons --git --group-directories-first"
    alias la="eza -a --icons --group-directories-first"
    alias lt="eza --tree --level=2 --icons"
else
    alias ls="ls --color=auto"
    alias ll="ls -lah --color=auto"
    alias la="ls -A --color=auto"
fi

# --- 5. CUSTOM PRODUCTIVITY FUNCTIONS (Matching PowerShell Profile) ---

# Fast commit with automatic message concatenation
gcommit() {
    if [ $# -gt 0 ]; then
        git commit -m "$*"
    else
        git commit
    fi
}

# Clean bin & obj directories recursively (.NET)
dclean() {
    find . -type d \( -name "bin" -o -name "obj" \) -prune -exec rm -rf {} + 2>/dev/null
    echo -e "\033[32m🧹 Cleaned all bin/ and obj/ folders.\033[0m"
}

# view / cat-file: Formatted file viewer with line numbers & metadata
view() {
    local target="$1"
    local max_lines="${2:-100}"
    if [ -z "$target" ]; then
        echo -e "\033[33mUsage: view <filepath> [maxLines]\033[0m"
        return 1
    fi
    if [ ! -f "$target" ]; then
        echo -e "\033[31mFile not found: $target\033[0m"
        return 1
    fi
    local total_lines=$(wc -l < "$target" 2>/dev/null || echo 0)
    local size_kb=$(command du -k "$target" 2>/dev/null | cut -f1 || echo 0)
    echo -e "\033[36m📄 $target ($total_lines lines · ${size_kb} KB)\033[0m"
    echo -e "\033[90m────────────────────────────────────────────────────────────────────────────────\033[0m"
    nl -ba -w3 -s' │ ' "$target" | head -n "$max_lines"
    if [ "$total_lines" -gt "$max_lines" ]; then
        echo -e "\033[90m... (showing $max_lines of $total_lines lines)\033[0m"
    fi
    echo -e "\033[90m────────────────────────────────────────────────────────────────────────────────\033[0m"
}

# head / tail formatted
head-file() {
    local target="$1"
    local n="${2:-20}"
    if [ -z "$target" ] || [ ! -f "$target" ]; then
        echo -e "\033[33mUsage: head-file <filepath> [n]\033[0m"
        return 1
    fi
    nl -ba -w3 -s' │ ' "$target" | head -n "$n"
}

tail-file() {
    local target="$1"
    local n="${2:-20}"
    if [ -z "$target" ] || [ ! -f "$target" ]; then
        echo -e "\033[33mUsage: tail-file <filepath> [n]\033[0m"
        return 1
    fi
    tail -n "$n" "$target" | sed 's/^/│ /'
}

# ff: Fast recursive file finder (excludes git, bin, obj, node_modules)
ff() {
    local pattern="${1:-*}"
    find . -maxdepth 6 -type f \( -name "$pattern" -o -name "*$pattern*" \) \
        -not -path '*/.git/*' \
        -not -path '*/bin/*' \
        -not -path '*/obj/*' \
        -not -path '*/node_modules/*' 2>/dev/null | head -n 50 | while IFS= read -r file; do
            echo -e "\033[36m📄 $file\033[0m"
        done
}

# gf: Fast recursive text search / grep
gf() {
    local pattern="$1"
    if [ -z "$pattern" ]; then
        echo -e "\033[33mUsage: gf <pattern>\033[0m"
        return 1
    fi
    grep -rnI --color=always \
        --exclude-dir={.git,bin,obj,node_modules} \
        "$pattern" . 2>/dev/null | head -n 100
}

# Smart opener
open-file() {
    local target="${1:-.}"
    if [[ "$target" =~ ^https?:// ]]; then
        if command -v xdg-open >/dev/null 2>&1; then xdg-open "$target"; fi
    elif [ -d "$target" ]; then
        cd "$target"
    elif [ -f "$target" ]; then
        ${EDITOR:-nano} "$target"
    fi
}

# Clear Shell History
clh() {
    if [ -n "$HISTFILE" ] && [ -f "$HISTFILE" ]; then
        > "$HISTFILE"
    fi
    history -c 2>/dev/null || true
    clear
    echo -e "\033[33m🧹 All command history has been cleared.\033[0m"
}

# proj: Workspace hopper & Navigator
proj() {
    local base_dir="$HOME/projects"
    if [ $# -eq 0 ]; then
        echo -e "\033[36m📂 Workspaces in $base_dir:\033[0m"
        local i=1
        for d in "$base_dir"/*; do
            if [ -d "$d" ]; then
                local bname=$(basename "$d")
                echo -e "  \033[33m[$i]\033[0m \033[32m$bname\033[0m \033[90m($d)\033[0m"
                ((i++))
            fi
        done
        return 0
    fi

    local query="$1"
    local query_lower=$(echo "$query" | tr '[:upper:]' '[:lower:]')
    local matches=()
    for d in "$base_dir"/*; do
        if [ -d "$d" ]; then
            local bname=$(basename "$d")
            local bname_lower=$(echo "$bname" | tr '[:upper:]' '[:lower:]')
            if [[ "$bname_lower" == *"$query_lower"* ]]; then
                matches+=("$d")
            fi
        fi
    done

    if [ ${#matches[@]} -eq 0 ]; then
        echo -e "\033[31m❌ No workspace matching: $query\033[0m"
        return 1
    elif [ ${#matches[@]} -eq 1 ]; then
        cd "${matches[0]}"
        echo -e "\033[32m📂 Switched to:\033[0m \033[36m$(pwd)\033[0m"
    else
        echo -e "\033[33mMultiple workspaces match '$query':\033[0m"
        for m in "${matches[@]}"; do
            echo -e "  • $(basename "$m")"
        done
        cd "${matches[0]}"
        echo -e "\033[32m📂 Jumped to first match:\033[0m \033[36m$(pwd)\033[0m"
    fi
}

# Network Ports Management
ports() {
    echo -e "\033[36m🔌 Active Listening Ports:\033[0m"
    ss -tulpn 2>/dev/null | grep -E "LISTEN" || netstat -tulpn 2>/dev/null
}

killport() {
    local port="$1"
    if [ -z "$port" ]; then
        echo -e "\033[33mUsage: killport <port>\033[0m"
        return 1
    fi
    fuser -k -n tcp "$port" 2>/dev/null || true
    echo -e "\033[32m✓ Terminated process on port $port\033[0m"
}

# --- 6. ALIASES (Matching PowerShell Profile) ---

# Git Aliases
alias gs="git status"
alias gsi="git status"
alias gd="git diff"
alias glo="git log --graph --oneline --decorate"
alias gb="git branch"
alias co="git checkout"
alias cob="git checkout -b"
alias gbd="git branch -d"
alias ga="git add ."
alias gunstage="git restore --staged ."
alias gca="git commit --amend"
alias git-undo="git reset --soft HEAD~1"
alias gr="git reset --soft HEAD~1"
alias grh="git reset --hard"
alias gfetch="git fetch"
alias gpu="git push"
alias gpush="git push"
alias gpull="git pull"
alias guf="git push --force-with-lease"
alias gclone="git clone"
alias gremote="git remote -v"
alias grt="git remote -v"
alias gco-remote="git checkout -t"
alias cor="git checkout -t"
alias gmerge="git merge"
alias gm="git merge"
alias gstash="git stash"
alias gst="git stash"
alias grebase="git rebase"
alias grb="git rebase"

# Docker Aliases
alias dk="docker ps"
alias dps="docker ps"
alias containers="docker ps"
alias dimg="docker images"
alias dlogs="docker logs"
alias dkcpu="docker compose up"
alias dcup="docker compose up"
alias dkcpub="docker compose up --build"
alias dkcpd="docker compose down"
alias dcdown="docker compose down"
alias dkstac='docker stop $(docker ps -q) 2>/dev/null || echo "No running containers."'
alias dkrmac='docker rm -f $(docker ps -aq) 2>/dev/null || echo "No containers to remove."'

# .NET SDK Aliases
alias dr="dotnet run"
alias dw="dotnet watch"
alias dwatch="dotnet watch"
alias db="dotnet build"
alias dbld="dotnet build"
alias rebuild="dotnet build"
alias dfmt="dotnet format"
alias dt="dotnet test"
alias dtst="dotnet test"
alias dwt="dotnet watch test"
alias dcl="dotnet clean"
alias dres="dotnet restore"
alias drestore="dotnet restore"
alias sln="dotnet new sln"
alias console="dotnet new console"
alias webapi="dotnet new webapi"
alias update-db="dotnet ef database update"
alias add-migration="dotnet ef migrations add"
alias clean-build="dclean"

# Navigation & Utilities
alias cls="clear"
alias ..="cd .."
alias ...="cd ../.."
alias ip-info="hostname -I 2>/dev/null || ip -br a"
alias cat-file="view"
alias open="open-file"
alias kp="killport"

# --- 7. LOAD NOTIFICATION ---
# echo -e "\033[32m🛸 Enhanced Linux Profile Loaded (Repository: nhonvo/powershell)\033[0m"
