#!/usr/bin/env bash
# ==============================================================================
#  1-CLICK UBUNTU / WSL2 SETUP SCRIPT FOR ENHANCED POWERSHELL PROFILE REPO
#  Repository: https://github.com/nhonvo/powershell
# ==============================================================================
set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

echo "==> 1. Updating packages & installing system dependencies..."
sudo apt-get update
sudo apt-get install -y zsh fzf git curl unzip

echo "==> 2. Installing Oh My Zsh (if not installed)..."
if [ ! -d "$HOME/.oh-my-zsh" ]; then
    RUNZSH=no CHSH=no sh -c "$(curl -fsSL https://raw.githubusercontent.com/ohmyzsh/ohmyzsh/master/tools/install.sh)" "" --unattended
fi

echo "==> 3. Installing Oh My Zsh Plugins..."
ZSH_CUSTOM=${ZSH_CUSTOM:-$HOME/.oh-my-zsh/custom}
mkdir -p "$ZSH_CUSTOM/plugins" "$HOME/.local/bin" "$HOME/.config"

# zsh-autosuggestions
if [ ! -d "$ZSH_CUSTOM/plugins/zsh-autosuggestions" ]; then
    git clone https://github.com/zsh-users/zsh-autosuggestions "$ZSH_CUSTOM/plugins/zsh-autosuggestions"
fi

# zsh-syntax-highlighting
if [ ! -d "$ZSH_CUSTOM/plugins/zsh-syntax-highlighting" ]; then
    git clone https://github.com/zsh-users/zsh-syntax-highlighting.git "$ZSH_CUSTOM/plugins/zsh-syntax-highlighting"
fi

# zsh-completions
if [ ! -d "$ZSH_CUSTOM/plugins/zsh-completions" ]; then
    git clone https://github.com/zsh-users/zsh-completions "$ZSH_CUSTOM/plugins/zsh-completions"
fi

# fzf-tab (interactive popup list menu)
if [ ! -d "$ZSH_CUSTOM/plugins/fzf-tab" ]; then
    git clone https://github.com/Aloxaf/fzf-tab "$ZSH_CUSTOM/plugins/fzf-tab"
fi

echo "==> 4. Installing Oh My Posh binary..."
if [ ! -f "$HOME/.local/bin/oh-my-posh" ]; then
    curl -sL https://github.com/JanDeDobbeleer/oh-my-posh/releases/latest/download/posh-linux-amd64 -o "$HOME/.local/bin/oh-my-posh"
    chmod +x "$HOME/.local/bin/oh-my-posh"
fi

echo "==> 5. Installing eza (Terminal Icons engine)..."
if [ ! -f "$HOME/.local/bin/eza" ]; then
    curl -sL https://github.com/eza-community/eza/releases/latest/download/eza_x86_64-unknown-linux-gnu.tar.gz | tar -xz -C "$HOME/.local/bin"
    chmod +x "$HOME/.local/bin/eza"
fi

echo "==> 6. Installing .NET 9 SDK (if not installed)..."
if [ ! -d "$HOME/.dotnet" ]; then
    curl -sSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh
    bash /tmp/dotnet-install.sh --channel 9.0 --install-dir "$HOME/.dotnet"
fi

echo "==> 7. Linking centralized profile configuration..."
ln -sf "$SCRIPT_DIR/posh-profile.zsh" "$HOME/.config/posh-profile.zsh"

echo "==> 8. Configuring ~/.zshrc..."
cat << 'ZSHRC_EOF' > "$HOME/.zshrc"
# Path to oh-my-zsh
export ZSH="$HOME/.oh-my-zsh"

# Theme is managed by Oh My Posh / posh-profile.zsh
ZSH_THEME=""

# Oh My Zsh Plugins (including fzf-tab for interactive list completions)
plugins=(
    git
    fzf-tab
    zsh-autosuggestions
    zsh-syntax-highlighting
    zsh-completions
    fzf
)

# Load Oh My Zsh
[ -s "$ZSH/oh-my-zsh.sh" ] && source "$ZSH/oh-my-zsh.sh"

# .NET SDK Environment
export DOTNET_ROOT="$HOME/.dotnet"
export PATH="$HOME/.dotnet:$HOME/.dotnet/tools:$PATH"

# NVM & Environment Paths
export NVM_DIR="$HOME/.nvm"
[ -s "$NVM_DIR/nvm.sh" ] && \. "$NVM_DIR/nvm.sh"
[ -s "$NVM_DIR/bash_completion" ] && \. "$NVM_DIR/bash_completion"
export PATH="$HOME/.local/bin:$PATH"

# Load Converted Enhanced Profile (from nhonvo/powershell)
[ -s "$HOME/.config/posh-profile.zsh" ] && source "$HOME/.config/posh-profile.zsh"
ZSHRC_EOF

echo "==> 9. Setting Zsh as default login shell..."
if [ "$SHELL" != "$(which zsh)" ]; then
    sudo chsh -s "$(which zsh)" "$USER" || true
fi

echo ""
echo "=================================================================="
echo "✨ Ubuntu / WSL2 Setup Complete!"
echo "🚀 Run 'source ~/.zshrc' or open a new terminal tab to start."
echo "=================================================================="
