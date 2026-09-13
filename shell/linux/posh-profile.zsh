# ==============================================================================
#  ENHANCED UBUNTU / LINUX SHELL ENVIRONMENT (Converted from PowerShell Profile)
#  Repository: https://github.com/nhonvo/powershell
# ==============================================================================

# --- 1. DYNAMIC REPO & THEME RESOLUTION ---
if [ -n "$ZSH_VERSION" ]; then
    _CURRENT_SCRIPT="${(%):-%N}"
    [ -z "$_CURRENT_SCRIPT" ] && _CURRENT_SCRIPT="$0"
    CURRENT_SCRIPT_DIR="${_CURRENT_SCRIPT:A:h}"
elif [ -n "$BASH_VERSION" ]; then
    CURRENT_SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
fi

if [ -d "$CURRENT_SCRIPT_DIR/../../apps" ]; then
    export REPO_ROOT="$(cd "$CURRENT_SCRIPT_DIR/../.." && pwd)"
elif [ -d "$CURRENT_SCRIPT_DIR/../apps" ]; then
    export REPO_ROOT="$(cd "$CURRENT_SCRIPT_DIR/.." && pwd)"
else
    export REPO_ROOT="$(cd "$CURRENT_SCRIPT_DIR/../.." 2>/dev/null || cd "$CURRENT_SCRIPT_DIR/.." && pwd)"
fi

export POSH_THEMES_PATH="$REPO_ROOT/shell/assets/powershell-themes"
if [ ! -d "$POSH_THEMES_PATH" ]; then
    export POSH_THEMES_PATH="$REPO_ROOT/psapp/asset/powershell-themes"
fi
export PATH="$HOME/.local/bin:$HOME/.dotnet:$HOME/.dotnet/tools:$PATH"
export DOTNET_ROOT="$HOME/.dotnet"

# Load saved theme or fallback to neko
if [ -f "$HOME/.config/selected_posh_theme.txt" ]; then
    POSH_THEME=$(cat "$HOME/.config/selected_posh_theme.txt" | tr -d '[:space:]')
else
    POSH_THEME="neko"
fi

if command -v oh-my-posh >/dev/null 2>&1; then
    THEME_FILE="$POSH_THEMES_PATH/${POSH_THEME}.omp.json"
    if [ -f "$THEME_FILE" ]; then
        eval "$(oh-my-posh init zsh --config "$THEME_FILE")"
    else
        eval "$(oh-my-posh init zsh)"
    fi
fi

# Interactive Theme Switcher Function
theme() {
    local theme_dir="$POSH_THEMES_PATH"
    if [ -z "$1" ]; then
        echo -e "\033[36m🎨 Current Theme:\033[0m \033[32m${POSH_THEME:-neko}\033[0m"
        echo -e "\033[33mUsage:\033[0m theme <theme-name>"
        echo -e "\033[36mAvailable themes in your repository (80+):\033[0m"
        if [ -d "$theme_dir" ]; then
            ls "$theme_dir" | grep '\.omp\.json$' | sed 's/\.omp\.json$//' | column -c 80
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
    eval "$(oh-my-posh init zsh --config "$config_file")"
    echo -e "\033[32m✨ Applied Oh My Posh theme: $target_theme\033[0m"
}

# --- 2. PSREADLINE-EQUIVALENT HISTORY & PREDICTIONS ---
HISTSIZE=50000
SAVEHIST=50000
HISTFILE="$HOME/.zsh_history"
setopt EXTENDED_HISTORY
setopt HIST_EXPIRE_DUPS_FIRST
setopt HIST_IGNORE_DUPS
setopt HIST_IGNORE_ALL_DUPS
setopt HIST_FIND_NO_DUPS
setopt HIST_IGNORE_SPACE
setopt HIST_SAVE_NO_DUPS
setopt SHARE_HISTORY

# Ghost-text autosuggestions styled in PSReadLine teal color (#70A99F)
export ZSH_AUTOSUGGEST_HIGHLIGHT_STYLE="fg=#70A99F"
export ZSH_AUTOSUGGEST_STRATEGY=(history completion)

# Keybindings: Up/Down arrow search prefix, Ctrl+Space / Right Arrow to accept
bindkey '^[[A' history-beginning-search-backward
bindkey '^[[B' history-beginning-search-forward
bindkey '^ ' autosuggest-accept
bindkey '^E' autosuggest-accept

# --- 3. KEYBOARD FIXES (Ctrl+Backspace, Ctrl+Left/Right, Word Navigation) ---
autoload -U select-word-style 2>/dev/null || true
select-word-style bash 2>/dev/null || true

# Ctrl+Backspace to delete whole word (like Windows / PowerShell)
bindkey '^H' backward-kill-word
bindkey '^?' backward-delete-char
bindkey '^W' backward-kill-word
bindkey '\e\b' backward-kill-word
bindkey '\e[127;5u' backward-kill-word
bindkey '^[[3;5~' kill-word

# Ctrl + Left/Right Arrow jumps words
bindkey '^[[1;5D' backward-word
bindkey '^[[1;5C' forward-word
bindkey '^[[5D' backward-word
bindkey '^[[5C' forward-word

# Remove potential alias collisions before defining functions
unalias cc ccd gf ff view head-file tail-file open-file clh proj gcommit dclean ls l ll la lt 2>/dev/null || true

# --- 4. TERMINAL ICONS (eza / Terminal-Icons equivalent) ---
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

sync_active_agy_environment() {
    local active_file="$HOME/.gemini/active_account.txt"
    if [ -f "$active_file" ]; then
        local acc_name="$(tr -d '[:space:]' < "$active_file")"
        if [ -n "$acc_name" ] && [ "$acc_name" != "default" ]; then
            local target_home="$HOME/.gemini_$acc_name"
            if [ -d "$target_home" ]; then
                export GEMINI_HOME="$target_home"
            fi
        elif [ "$acc_name" = "default" ]; then
            export GEMINI_HOME="$HOME/.gemini"
        fi
    fi
}

# --- 5. CONTROL CENTER & AGYX COCKPIT (Go Suite) ---
cc() {
    local agyx_bin="$HOME/.local/bin/agyx"
    if [ ! -f "$agyx_bin" ] && [ -n "$REPO_ROOT" ]; then
        agyx_bin="$REPO_ROOT/dist/linux/agyx"
    fi
    if [ -f "$agyx_bin" ]; then
        "$agyx_bin" "$@"
    elif command -v agyx >/dev/null 2>&1; then
        agyx "$@"
    else
        echo -e "\033[31m❌ agyx binary not found in ~/.local/bin or PATH\033[0m"
    fi
    sync_active_agy_environment
}

ccd() {
    cc "$@"
}

# Sync active environment on startup
sync_active_agy_environment

# --- AGYX DEVELOPER SUITE (Go Single Source of Truth Hook) ---
if [ -x "$HOME/.local/bin/agyx" ]; then
    eval "$("$HOME/.local/bin/agyx" init zsh)"
elif command -v agyx >/dev/null 2>&1; then
    eval "$(agyx init zsh)"
fi

# --- 6. CUSTOM FUNCTIONS ---

# Docker & Git Helpers (AGYX Integrated)
dlogsu() {
    if [ $# -gt 0 ]; then
        agydocker logs "$1"
    else
        agydocker
    fi
}

gcmt() {
    if [ -x "$HOME/.local/bin/agygit" ]; then
        if [ $# -gt 0 ]; then
            "$HOME/.local/bin/agygit" commit "$*"
        else
            "$HOME/.local/bin/agygit" commit
        fi
    else
        gcommit "$@"
    fi
}

gmergeu() {
    if [ -x "$HOME/.local/bin/agygit" ]; then
        if [ $# -gt 0 ]; then
            "$HOME/.local/bin/agygit" merge "$1"
        else
            "$HOME/.local/bin/agygit"
        fi
    else
        git merge "$@"
    fi
}

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
_ff_func() {
    local pattern="${1:-*}"
    find . -maxdepth 6 -type f \( -name "$pattern" -o -name "*$pattern*" \) \
        -not -path '*/.git/*' \
        -not -path '*/bin/*' \
        -not -path '*/obj/*' \
        -not -path '*/node_modules/*' 2>/dev/null | head -n 50 | while IFS= read -r file; do
            echo -e "\033[36m📄 $file\033[0m"
        done
}
alias ff="noglob _ff_func"

# gf: Fast recursive text search / grep
_gf_func() {
    local pattern="$1"
    if [ -z "$pattern" ]; then
        echo -e "\033[33mUsage: gf <pattern>\033[0m"
        return 1
    fi
    grep -rnI --color=always \
        --exclude-dir={.git,bin,obj,node_modules} \
        "$pattern" . 2>/dev/null | head -n 100
}
alias gf="noglob _gf_func"

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

# proj: Workspace hopper & Navigator (Integrated with agyproj)
proj() {
    if [ $# -eq 0 ]; then
        if [ -x "$HOME/.local/bin/agyproj" ]; then
            "$HOME/.local/bin/agyproj"
            local sel_proj="$HOME/.gemini/selected_project.txt"
            if [ -f "$sel_proj" ]; then
                local target_dir="$(tr -d '\r\n' < "$sel_proj")"
                rm -f "$sel_proj"
                if [ -n "$target_dir" ] && [ -d "$target_dir" ]; then
                    cd "$target_dir"
                    echo -e "\033[32m📂 Switched workspace directory to:\033[0m \033[36m$target_dir\033[0m"
                fi
            fi
        else
            local base_dir="$HOME/projects"
            echo -e "\033[36m📂 Workspaces in $base_dir:\033[0m"
            local i=1
            for d in "$base_dir"/*; do
                if [ -d "$d" ]; then
                    local bname=$(basename "$d")
                    echo -e "  \033[33m[$i]\033[0m \033[32m$bname\033[0m \033[90m($d)\033[0m"
                    ((i++))
                fi
            done
        fi
        return 0
    fi

    local query="$1"
    if [ -x "$HOME/.local/bin/agyproj" ]; then
        local target_dir="$("$HOME/.local/bin/agyproj" cd "$query" 2>/dev/null)"
        if [ -n "$target_dir" ] && [ -d "$target_dir" ]; then
            cd "$target_dir"
            echo -e "\033[32m📂 Switched to:\033[0m \033[36m$(pwd)\033[0m"
            return 0
        fi
    fi

    local base_dir="$HOME/projects"
    local matches=()
    for d in "$base_dir"/*; do
        if [ -d "$d" ]; then
            local bname=$(basename "$d")
            if [[ "${bname:l}" =~ "${query:l}" ]]; then
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

# --- 7. ALIASES ---

# Git Aliases (Integrated with agygit & matching PowerShell profile)
alias gs="git status"
alias gsu="agygit"
alias gsi="git status"
alias gd="git diff"
alias glo="git log --graph --oneline --decorate"
alias glg="agygit graph"
alias glog="agygit log"
alias gb="git branch"
alias gbr="agygit"
alias gbu="agygit"
alias co="git checkout"
alias cob="git checkout -b"
alias gbd="git branch -d"
alias ga="git add ."
alias gunstage="git restore --staged ."
alias gca="git commit --amend"
alias gundo="agygit undo"
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

# Docker Aliases (Integrated with agydocker & matching PowerShell profile)
alias dk="docker ps"
alias dps="docker ps"
alias containers="docker ps"
alias dku="agydocker"
alias dki="agydocker"
alias dkcl="agydocker"
alias dimg="docker images"
alias dimgu="agydocker"
alias dlogs="docker logs"
alias dkcpu="docker compose up"
alias dcup="docker compose up"
alias dkcpub="docker compose up --build"
alias dkcpd="docker compose down"
alias dcdown="agydocker down"
alias dkprune="agydocker prune"
alias fix-volume="agydocker prune"
alias fix-image="agydocker prune"
alias docker-health="agydocker ram"
alias dkstac='docker stop $(docker ps -q) 2>/dev/null || echo "No running containers."'
alias dkrmac='docker rm -f $(docker ps -aq) 2>/dev/null || echo "No containers to remove."'

# .NET SDK Aliases (Matching PowerShell profile)
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

# Navigation & Utilities (Matching PowerShell profile)
alias cls="clear"
alias ..="cd .."
alias ...="cd ../.."
alias ip-info="hostname -I 2>/dev/null || ip -br a"
alias cat-file="view"
alias open="open-file"

# Port & RAM Management (Integrated with agyport)
alias ports="agyport ls"
alias killport="agyport kill"
alias kp="agyport kill"
alias killallports="agyport kill-all"
alias killdev="agyport kill-all"
alias reclaim-ram="agyport reclaim"
alias ram-hogs="agyport top"
alias mem-status="agyport ram"


echo -e "\033[32m🛸 Enhanced Ubuntu Profile Loaded (Oh My Posh: ${POSH_THEME:-neko})\033[0m"

