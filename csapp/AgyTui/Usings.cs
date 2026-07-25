global using System;
global using System.IO;
global using System.Linq;
global using System.Collections.Generic;
global using System.Text;
global using Spectre.Console;

global using AgyTui.Core.Interfaces;
global using AgyTui.Infrastructure;
global using AgyTui.Infrastructure.Common;
global using AgyTui.Infrastructure.Persistence;
global using AgyTui.Infrastructure.Persistence.Accounts;
global using AgyTui.Infrastructure.Persistence.Learning;
global using AgyTui.Infrastructure.Integrations;
global using AgyTui.Core.Models;
global using AgyTui.Core.Registries;
global using AgyTui.Infrastructure.Integrations.Obsidian;
global using AgyTui.Infrastructure.Integrations.Sys;
global using AgyTui.Infrastructure.Integrations.Aws;
global using AgyTui.Infrastructure.Integrations.Docker;
global using AgyTui.Infrastructure.Integrations.DotNet;
global using AgyTui.Infrastructure.Integrations.Git;
global using AgyTui.Infrastructure.Integrations.Ai;

global using AgyTui.UI.Core.Navigation;
global using AgyTui.UI.Core.Layouts;
global using AgyTui.UI.Core.Common;
global using AgyTui.UI.Screens.SysNet;
global using AgyTui.UI.Screens.Account;
global using AgyTui.UI.Screens.Learn;
global using AgyTui.UI.Screens.Git;
global using AgyTui.UI.Screens.Ide;
global using AgyTui.UI.Screens.Quizzes;
global using AgyTui.UI.Screens.Career;

global using Helpers = AgyTui.Infrastructure.Common;
global using SystemHelper = AgyTui.UI.Screens.SysNet.SystemConsoleView;
global using SshHelper = AgyTui.UI.Screens.SysNet.SshConsoleView;
