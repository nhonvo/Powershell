package shellgen

import (
	"fmt"
	"strings"
)

// GenerateZsh produces the complete shell initialization script for Zsh / Bash.
func GenerateZsh() string {
	var b strings.Builder

	b.WriteString("# =============================================================================\n")
	b.WriteString("#  AGYX UNIFIED DEVELOPER SUITE — SHELL INITIALIZATION (ZSH / BASH)\n")
	b.WriteString("#  Auto-generated from Go Single Source of Truth (catalog.go)\n")
	b.WriteString("# =============================================================================\n\n")

	// Ensure ~/.local/bin is in PATH
	b.WriteString(`if [ -d "$HOME/.local/bin" ] && [[ ":$PATH:" != *":$HOME/.local/bin:"* ]]; then
    export PATH="$HOME/.local/bin:$PATH"
fi

`)

	// Clean collisions before declaring functions
	b.WriteString("unalias proj dclean clh sln-add dlogsu dkstac dkrmac gcmt gmergeu open-file view-file head-file tail-file find-file grep-file ip-info theme 2>/dev/null || true\n\n")

	// Suite Native Binaries
	b.WriteString("# --- Native AGYX Suite Binaries ---\n")
	for _, app := range GetSuiteApps() {
		b.WriteString(fmt.Sprintf("alias %s=\"$HOME/.local/bin/%s\"\n", app, app))
	}
	b.WriteString("\n")

	// Catalog Aliases
	b.WriteString("# --- Catalog Aliases ---\n")
	catalog := GetCatalog()
	for _, item := range catalog {
		switch item.Type {
		case SuiteApp:
			if len(item.Args) > 0 {
				b.WriteString(fmt.Sprintf("alias %s=\"$HOME/.local/bin/%s %s\"\n", item.Name, item.Target, strings.Join(item.Args, " ")))
			} else {
				b.WriteString(fmt.Sprintf("alias %s=\"$HOME/.local/bin/%s\"\n", item.Name, item.Target))
			}
		case ExternalCli:
			if len(item.Args) > 0 {
				b.WriteString(fmt.Sprintf("alias %s=\"%s %s\"\n", item.Name, item.Target, strings.Join(item.Args, " ")))
			} else {
				b.WriteString(fmt.Sprintf("alias %s=\"%s\"\n", item.Name, item.Target))
			}
		case AliasMapping:
			b.WriteString(fmt.Sprintf("alias %s=\"%s\"\n", item.Name, item.Target))
		}
	}
	b.WriteString("\n")

	// Functions (Shell State Mutators)
	b.WriteString(`# --- Shell Functions & State Mutators ---
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
                    echo -e "  \033[33m[$i]\033[0m \033[32m$(basename "$d")\033[0m \033[90m($d)\033[0m"
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
        git commit -m "$*"
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

dclean() {
    find . -type d \( -name "bin" -o -name "obj" \) -prune -exec rm -rf {} + 2>/dev/null
    echo -e "\033[32m🧹 Cleaned all bin/ and obj/ folders.\033[0m"
}

sln-add() {
    local projs=($(find . -name "*.csproj" -not -path "*/bin/*" -not -path "*/obj/*" 2>/dev/null))
    if [ ${#projs[@]} -gt 0 ]; then
        dotnet sln add "${projs[@]}"
    else
        echo "No .csproj projects found."
    fi
}

clh() {
    if [ -n "$HISTFILE" ] && [ -f "$HISTFILE" ]; then
        > "$HISTFILE"
    fi
    history -c 2>/dev/null || true
    clear
    echo -e "\033[33m🧹 All command history has been cleared.\033[0m"
}

alias dkstac='docker stop $(docker ps -q) 2>/dev/null || echo "No running containers."'
alias dkrmac='docker rm -f $(docker ps -aq) 2>/dev/null || echo "No containers to remove."'
alias view-file="view"
alias find-file="ff"
alias grep-file="gf"
alias open-folder="f"
`)

	return b.String()
}

// GeneratePowerShell produces the complete shell initialization script for PowerShell on Windows.
func GeneratePowerShell() string {
	var b strings.Builder

	b.WriteString("# =============================================================================\n")
	b.WriteString("#  AGYX UNIFIED DEVELOPER SUITE — SHELL INITIALIZATION (POWERSHELL)\n")
	b.WriteString("#  Auto-generated from Go Single Source of Truth (catalog.go)\n")
	b.WriteString("# =============================================================================\n\n")

	// Suite Native Binaries Registration
	b.WriteString(`$suiteAppNames = @("agyswitch", "agyproj", "agygit", "agydocker", "agyterm", "agyx", "agymobile", "agyollama")
foreach ($appName in $suiteAppNames) {
    $binPath = Join-Path $HOME ".local\bin\$appName.exe"
    if (-not (Test-Path $binPath) -and $Global:ProfileRepoRoot) {
        $binPath = Join-Path $Global:ProfileRepoRoot "dist\windows\$appName.exe"
    }
    if (Test-Path $binPath) {
        Set-Item -Path "Function:$appName" -Value ([ScriptBlock]::Create("& '$binPath' @args")) -Force
    } else {
        Set-Item -Path "Function:$appName" -Value ([ScriptBlock]::Create("wsl $appName @args")) -Force
    }
}

`)

	// Emit Catalog
	b.WriteString("# --- Catalog Aliases & Functions ---\n")
	catalog := GetCatalog()
	for _, item := range catalog {
		switch item.Type {
		case SuiteApp:
			if len(item.Args) > 0 {
				argStr := strings.Join(item.Args, " ")
				b.WriteString(fmt.Sprintf("function %s { %s %s @args }\n", item.Name, item.Target, argStr))
			} else {
				b.WriteString(fmt.Sprintf("Set-Alias -Name '%s' -Value '%s' -Force\n", item.Name, item.Target))
			}
		case ExternalCli:
			if len(item.Args) > 0 {
				argStr := strings.Join(item.Args, " ")
				b.WriteString(fmt.Sprintf("function %s { %s %s @args }\n", item.Name, item.Target, argStr))
			} else {
				b.WriteString(fmt.Sprintf("Set-Alias -Name '%s' -Value '%s' -Force\n", item.Name, item.Target))
			}
		case AliasMapping:
			b.WriteString(fmt.Sprintf("Set-Alias -Name '%s' -Value '%s' -Force\n", item.Name, item.Target))
		}
	}
	b.WriteString("\n")

	// PowerShell Functions
	b.WriteString(`# --- Shell Functions & State Mutators ---
function proj {
    param([string]$Query)
    if (-not $Query) {
        $agyProj = Join-Path $HOME ".local\bin\agyproj.exe"
        if (-not (Test-Path $agyProj) -and $Global:ProfileRepoRoot) { $agyProj = Join-Path $Global:ProfileRepoRoot "dist\windows\agyproj.exe" }
        if (Test-Path $agyProj) { & $agyProj }
        elseif (Get-Command agyproj -ErrorAction SilentlyContinue) { agyproj }
        else { wsl agyproj }

        $agyHome = if ($env:GEMINI_HOME) { $env:GEMINI_HOME } else { Join-Path $env:USERPROFILE ".gemini" }
        $projFile = Join-Path $agyHome "selected_project.txt"
        if (Test-Path -LiteralPath $projFile) {
            $targetProj = (Get-Content -LiteralPath $projFile -Raw).Trim()
            Remove-Item -LiteralPath $projFile -Force -ErrorAction SilentlyContinue
            if ($targetProj -and (Test-Path -LiteralPath $targetProj)) {
                Set-Location -LiteralPath $targetProj
                Write-Host "📂 Switched workspace directory to: $targetProj" -ForegroundColor Green
            }
        }
        return
    }

    $targetDir = $null
    if (Get-Command agyproj -ErrorAction SilentlyContinue) {
        $targetDir = (agyproj cd $Query 2>$null)
    } elseif (Get-Command wsl -ErrorAction SilentlyContinue) {
        $targetDir = (wsl agyproj cd $Query 2>$null)
    }
    if ($targetDir -and (Test-Path -LiteralPath $targetDir)) {
        Set-Location -LiteralPath $targetDir
        Write-Host "📂 Switched to: $(Get-Location)" -ForegroundColor Green
        return
    }
    Write-Host "❌ No workspace matching: $Query" -ForegroundColor Red
}

function dclean {
    Get-ChildItem -Path . -Include bin,obj -Recurse -Directory -Force -ErrorAction SilentlyContinue | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
    Write-Host "🧹 Cleaned all bin/ and obj/ folders." -ForegroundColor Green
}

function sln-add {
    $projs = Get-ChildItem -Path . -Filter *.csproj -Recurse | Where-Object { $_.FullName -notmatch '[\\/](bin|obj)[\\/]' } | Select-Object -ExpandProperty FullName
    if ($projs) { dotnet sln add $projs } else { Write-Host "No .csproj projects found." -ForegroundColor Yellow }
}

function dlogsu {
    if ($args.Count -gt 0) { agydocker logs $args[0] } else { agydocker }
}

function gcmt {
    if ($args.Count -gt 0) { agygit commit ($args -join " ") } else { agygit commit }
}

function gmergeu {
    if ($args.Count -gt 0) { agygit merge $args[0] } else { agygit }
}

function dkstac {
    $running = docker ps -q 2>$null
    if ($running) { docker stop $running } else { Write-Host "No running containers." -ForegroundColor Green }
}

function dkrmac {
    $allCont = docker ps -aq 2>$null
    if ($allCont) { docker rm -f $allCont } else { Write-Host "No containers to remove." -ForegroundColor Green }
}
`)

	return b.String()
}
