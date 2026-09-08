# ==============================================================================
# Antigravity Developer Suite - Master Makefile
# ==============================================================================

SHELL := /bin/bash
INSTALL_DIR ?= $(HOME)/.local/bin
DIST_WIN ?= ./dist/windows
APPS := agyswitch agyproj agygit agydocker agyterm agyx agymobile agyollama

.PHONY: all build install test test-v clean windows help $(APPS) mobile ollama

all: install

help:
	@echo "=================================================================="
	@echo "⚡ Antigravity Developer Suite (Go Engine - 8 Micro-Apps)"
	@echo "=================================================================="
	@echo "Usage:"
	@echo "  make build         Build all 8 Linux binaries to ./bin/"
	@echo "  make install       Build & install all binaries to $(INSTALL_DIR)"
	@echo "  make test          Run unit test suites across all 8 applications"
	@echo "  make windows       Cross-compile Windows .exe binaries to $(DIST_WIN)"
	@echo "  make <app>         Build & install a single app (e.g. make ollama)"
	@echo ""
	@echo "Available single-app targets:"
	@echo "  make switch        Build agyswitch  (Vault, Quota, Sessions)"
	@echo "  make proj          Build agyproj    (Workspaces, IDE Hub)"
	@echo "  make git           Build agygit     (Git Cockpit, Staging, Worktrees)"
	@echo "  make docker        Build agydocker  (Containers, WSL2 RAM Guard)"
	@echo "  make term          Build agyterm    (Fonts, Prompt Themes)"
	@echo "  make proxy         Build agyx       (Master Proxy & Orchestrator)"
	@echo "  make mobile        Build agymobile  (Mobile Terminal, Web PWA & Tailscale)"
	@echo "  make ollama        Build agyollama  (Local Ollama & AI Agent Cockpit)"
	@echo "=================================================================="

# Test all apps
test:
	@for app in $(APPS); do 		echo "🧪 Testing $$app..."; 		(cd apps/$$app && go test ./...) || exit 1; 	done
	@echo "✔ All test suites passed!"

test-v:
	@for app in $(APPS); do 		echo "🧪 Verbose testing $$app..."; 		(cd apps/$$app && go test -v ./...) || exit 1; 	done

# Build locally into bin/
build:
	@mkdir -p ./bin
	@for app in $(APPS); do 		echo "🔨 Building $$app -> ./bin/$$app"; 		(cd apps/$$app && go build -o ../../bin/$$app .) || exit 1; 	done
	@echo "✔ Built all binaries to ./bin/"

# Build and install directly to ~/.local/bin/
install:
	@mkdir -p $(INSTALL_DIR)
	@for app in $(APPS); do 		echo "🚀 Installing $$app -> $(INSTALL_DIR)/$$app"; 		(cd apps/$$app && go build -o $(INSTALL_DIR)/$$app .) || exit 1; 	done
	@echo "✔ All 8 apps installed to $(INSTALL_DIR)/"

# Single-app build & install targets
switch:
	@mkdir -p $(INSTALL_DIR)
	@echo "🚀 Building & installing agyswitch..."
	@(cd apps/agyswitch && go test ./... && go build -o $(INSTALL_DIR)/agyswitch .)
	@echo "✔ Installed $(INSTALL_DIR)/agyswitch"

proj:
	@mkdir -p $(INSTALL_DIR)
	@echo "🚀 Building & installing agyproj..."
	@(cd apps/agyproj && go test ./... && go build -o $(INSTALL_DIR)/agyproj .)
	@echo "✔ Installed $(INSTALL_DIR)/agyproj"

git:
	@mkdir -p $(INSTALL_DIR)
	@echo "🚀 Building & installing agygit..."
	@(cd apps/agygit && go test ./... && go build -o $(INSTALL_DIR)/agygit .)
	@echo "✔ Installed $(INSTALL_DIR)/agygit"

docker:
	@mkdir -p $(INSTALL_DIR)
	@echo "🚀 Building & installing agydocker..."
	@(cd apps/agydocker && go test ./... && go build -o $(INSTALL_DIR)/agydocker .)
	@echo "✔ Installed $(INSTALL_DIR)/agydocker"

term:
	@mkdir -p $(INSTALL_DIR)
	@echo "🚀 Building & installing agyterm..."
	@(cd apps/agyterm && go test ./... && go build -o $(INSTALL_DIR)/agyterm .)
	@echo "✔ Installed $(INSTALL_DIR)/agyterm"

proxy:
	@mkdir -p $(INSTALL_DIR)
	@echo "🚀 Building & installing agyx..."
	@(cd apps/agyx && go test ./... && go build -o $(INSTALL_DIR)/agyx .)
	@echo "✔ Installed $(INSTALL_DIR)/agyx"

agyx: proxy

mobile:
	@mkdir -p $(INSTALL_DIR)
	@echo "🚀 Building & installing agymobile..."
	@(cd apps/agymobile && go test ./... && go build -o $(INSTALL_DIR)/agymobile .)
	@echo "✔ Installed $(INSTALL_DIR)/agymobile"

ollama:
	@mkdir -p $(INSTALL_DIR)
	@echo "🚀 Building & installing agyollama..."
	@(cd apps/agyollama && go test ./... && go build -o $(INSTALL_DIR)/agyollama .)
	@echo "✔ Installed $(INSTALL_DIR)/agyollama"

# Cross-compile for Windows
windows:
	@mkdir -p $(DIST_WIN)
	@for app in $(APPS); do 		echo "🪟 Cross-compiling $$app -> $(DIST_WIN)/$$app.exe"; 		(cd apps/$$app && GOOS=windows GOARCH=amd64 go build -o ../../$(DIST_WIN)/$$app.exe .) || exit 1; 	done
	@echo "✔ All Windows .exe binaries compiled to $(DIST_WIN)/"

clean:
	@rm -rf ./bin $(DIST_WIN)
	@echo "✔ Cleaned build artifacts."
