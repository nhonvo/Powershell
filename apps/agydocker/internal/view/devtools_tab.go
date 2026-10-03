package view

import (
	"fmt"
	"strings"

	"agydocker/internal/model"
)

func (a *App) renderDevToolsTab(b *strings.Builder, status *model.DevToolsStackStatus, width int) {
	b.WriteString(" 🛠️  \033[1;36mCentralized Developer GUI Tools Stack (Shared pgAdmin & Mongo Express)\033[0m\033[K\r\n")
	b.WriteString("────────────────────────────────────────────────────────────────────────────────\033[K\r\n")

	if status == nil || status.RootDir == "" {
		b.WriteString(" \033[33m⚠️ Centralized dev-tools directory not located.\033[0m\033[K\r\n")
		b.WriteString("   Expected at: \033[1m/home/truongnhon/projects/dev-tools\033[0m\033[K\r\n")
		b.WriteString("   Set custom path via: \033[1magydocker tools config <path>\033[0m or export \033[1mAGY_DEV_TOOLS_PATH\033[0m\033[K\r\n")
		return
	}

	pgBadge := "\033[90m⚪ Stopped\033[0m"
	if status.PgAdminRunning {
		pgBadge = "\033[1;32m🟢 Running\033[0m \033[36m(http://localhost:5050)\033[0m"
	}

	mongoBadge := "\033[90m⚪ Stopped\033[0m"
	if status.MongoRunning {
		mongoBadge = "\033[1;32m🟢 Running\033[0m \033[36m(http://localhost:8082)\033[0m"
	}

	fmt.Fprintf(b, " \033[1mStack Root:\033[0m     %-40s\033[K\r\n", status.RootDir)
	fmt.Fprintf(b, " \033[1mpgAdmin 4:\033[0m      %s\033[K\r\n", pgBadge)
	fmt.Fprintf(b, " \033[1mMongo Express:\033[0m  %s\033[K\r\n\033[K\r\n", mongoBadge)

	b.WriteString(" 📁 \033[1mRegistered Database Connections (pgAdmin 4):\033[0m\033[K\r\n")
	b.WriteString("────────────────────────────────────────────────────────────────────────────────\033[K\r\n")

	if len(status.RegisteredServers) == 0 {
		b.WriteString("   \033[90mNo database servers registered yet.\033[0m\033[K\r\n")
		b.WriteString("   Press \033[1;32m[A]\033[0m to auto-attach current project, or \033[1;36m[S]\033[0m to sync running DB containers.\033[K\r\n\033[K\r\n")
	} else {
		// Group servers by Group name
		groups := make(map[string][]model.DevToolsServerEntry)
		var groupOrder []string
		for _, s := range status.RegisteredServers {
			grp := s.Group
			if grp == "" {
				grp = "General"
			}
			if _, exists := groups[grp]; !exists {
				groupOrder = append(groupOrder, grp)
			}
			groups[grp] = append(groups[grp], s)
		}

		itemIndex := 0
		for _, grp := range groupOrder {
			fmt.Fprintf(b, " \033[1;34m📁 %s\033[0m\033[K\r\n", grp)
			for _, s := range groups[grp] {
				prefix := "    "
				textStyle := "\033[0m"
				if itemIndex == a.SelectedIndex {
					prefix = " \033[1;32m▶ \033[0m "
					textStyle = "\033[1;32m"
				}

				hostPort := fmt.Sprintf("%s:%d", s.Host, s.Port)
				dbUser := fmt.Sprintf("%s/%s", s.MaintenanceDB, s.Username)

				fmt.Fprintf(b, "%s🐘 %s%-32s\033[0m \033[90m%-26s\033[0m %-18s\033[K\r\n",
					prefix, textStyle, truncateString(s.Name, 32), hostPort, dbUser)
				itemIndex++
			}
		}
		b.WriteString("\033[K\r\n")
	}

	b.WriteString("────────────────────────────────────────────────────────────────────────────────\033[K\r\n")
	b.WriteString(" \033[1mActions:\033[0m  \033[1;32m[U]\033[0m Start Tools  \033[1;31m[D]\033[0m Stop Tools  \033[1;36m[A]\033[0m Auto-Attach  \033[1;35m[S]\033[0m Sync Containers\033[K\r\n")
	b.WriteString("           \033[1;33m[O]\033[0m Open pgAdmin  \033[1;33m[M]\033[0m Open Mongo  \033[1;34m[R]\033[0m Reload Servers  \033[1;31m[X]\033[0m Detach DB\033[K\r\n")
}
