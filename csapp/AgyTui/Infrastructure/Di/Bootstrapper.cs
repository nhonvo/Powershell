using AgyTui.Infrastructure.Integrations.Ai.Abstractions;
using AgyTui.Infrastructure.Integrations.Ai.Providers;
using AgyTui.Infrastructure.Integrations.Ai.Services;
using AgyTui.Infrastructure.Logging;
using AgyTui.Infrastructure.Persistence;
using AgyTui.UI.Core.Commands;
using AgyTui.UI.Core.State;
using Microsoft.Extensions.DependencyInjection;

using AgyTui.Infrastructure.Persistence.Seeding;
using AgyTui.UI.Core.Common;
using AgyTui.UI.Core.Interfaces;
using AgyTui.UI.Core.Layouts;
using AgyTui.UI.Core.Layouts.Interfaces;
using AgyTui.UI.Core.Navigation;
using AgyTui.UI.Core.Navigation.Interfaces;
using AgyTui.UI.Screens;
using AgyTui.UI.Screens.Interfaces;
using AgyTui.UI.Screens.Services;
using AgyTui.UI.Screens.Workspace;

namespace AgyTui.Infrastructure.Di;

public static class Bootstrapper
{
    private static IServiceProvider? _serviceProvider;

    public static IServiceProvider ServiceProvider => _serviceProvider ??= BuildServiceProvider();

    public static IServiceProvider BuildServiceProvider(IServiceCollection? customServices = null)
    {
        var services = customServices ?? new ServiceCollection();

        // Core Services & Reactive UI State
        services.AddSingleton<IAppPathManager, AppPathManager>();
        services.AddSingleton<IFileSystem, PhysicalFileSystem>();
        services.AddSingleton<IConfigService, ConfigService>();
        services.AddSingleton<IUiStateStore, UiStateStore>();
        services.AddSingleton<IUiCommandDispatcher, UiCommandDispatcher>();
        services.AddSingleton<IHttpClientProvider, HttpClientProvider>();
        services.AddSingleton<IProcessRunner, ProcessRunner>();
        services.AddSingleton<ISystemHelper, SystemHelper>();
        services.AddSingleton<IAgyUiComponents, AgyUiComponentsService>();
        services.AddSingleton<IIcons, IconsService>();
        services.AddSingleton<IScrollableListView, ScrollableListViewService>();
        services.AddSingleton<ISpectreMenu, SpectreMenuService>();
        services.AddSingleton<ISpectrePanel, SpectrePanelService>();
        services.AddSingleton<ISpectreTable, SpectreTableService>();
        services.AddSingleton<ISpectreProgress, SpectreProgressService>();
        services.AddSingleton<IStatusWidgetRegistry, StatusWidgetRegistryService>();
        services.AddSingleton<IScreenChrome, ScreenChromeService>();
        services.AddSingleton<IProfileHelp, ProfileHelpService>();
        services.AddSingleton<IMenuNodeBuilder, MenuNodeBuilderService>();
        services.AddSingleton<ICcNavigator, CcNavigatorService>();
        services.AddSingleton<ICommandPalette, CommandPaletteService>();
        services.AddSingleton<ISubPageNavigator, SubPageNavigatorService>();
        services.AddSingleton<ICareerSuite, CareerSuiteService>();
        services.AddSingleton<IGitNexusSuite, GitNexusSuiteService>();
        services.AddSingleton<IIdeSuite, IdeSuiteService>();
        services.AddSingleton<ILearnSuite, LearnSuiteService>();

        // AI Services
        services.AddSingleton<IAiProcessRunner, AiProcessRunner>();
        services.AddSingleton<IOllamaClient, OllamaClient>();
        services.AddSingleton<IClaudeClient, ClaudeProvider>();
        services.AddSingleton<IHermesClient, HermesProvider>();
        services.AddSingleton<IOpenClawClient, OpenClawProvider>();
        services.AddSingleton<IAiProjectScanner, AiProjectScanner>();
        services.AddSingleton<IAiCommitGenerator, AiCommitGenerator>();
        services.AddSingleton<IAiLearningGenerator, AiLearningGenerator>();

        // SQLite Persistence & Seeding Pipeline
        services.AddSingleton<ISqliteDatabase, SqliteDatabase>();
        services.AddSingleton<SqliteMigrationEngine>();
        services.AddSingleton<ILearningDataSeeder, LearningDataSeeder>();
        services.AddSingleton<IConfigRepository, SqliteConfigRepository>();
        services.AddSingleton<IAgyAccountRepository, SqliteAgyAccountRepository>();
        services.AddSingleton<IWorkspaceRepository, SqliteWorkspaceRepository>();

        services.AddSingleton<ISeeder, AccountSeeder>();
        services.AddSingleton<ISeeder, WorkspaceSeeder>();
        services.AddSingleton<ISeeder, LearningSeeder>();
        services.AddSingleton<ISeeder, ThemeSeeder>();
        services.AddSingleton<ISeeder, ResourceSeeder>();
        services.AddSingleton<ISeeder, SkillSeeder>();
        services.AddSingleton<IMasterSeeder, MasterSeeder>();

        // Tool Integration Services
        services.AddSingleton<IAwsClient, AwsClient>();
        services.AddSingleton<IDockerClient, DockerClient>();
        services.AddSingleton<IDotNetClient, DotNetClient>();
        services.AddSingleton<IGitClient, GitClient>();
        services.AddSingleton<IAgyAccountStore, AgyAccountStore>();
        services.AddSingleton<IAgyQuotaEngine, AgyQuotaEngine>();
        services.AddSingleton<IAgyVault, AgyVault>();
        services.AddSingleton<IStudyRepository, JsonStudyRepository>();
        services.AddSingleton<IEditorResolver, EditorResolver>();
        services.AddSingleton<IProjectScaffolder, ProjectScaffolder>();
        services.AddSingleton<IThemeManager, ThemeManager>();
        services.AddSingleton<IObsidianBridge, ObsidianBridge>();

        // Navigation, Renderers & Routers with Logging Middleware
        services.AddSingleton<ThreePaneRenderer>();
        services.AddSingleton<FlatTreeRenderer>();
        services.AddSingleton<IStatusWidget, DiskSpaceWidget>();
        services.AddSingleton<IStatusWidget, PublicIpWidget>();
        services.AddSingleton<IStatusWidget, SshInfoWidget>();
        services.AddSingleton<IStatusWidget, AccountTreeWidget>();
        services.AddSingleton<IStatusWidget, QuotaChartWidget>();
        services.AddSingleton<IStatusWidget, LiveDashboardWidget>();
        services.AddSingleton<IStatusWidget, OllamaStatusWidget>();
        services.AddSingleton<IScreenView, AccountScreen>();
        services.AddSingleton<IScreenView, ProjectScreen>();
        services.AddSingleton<IScreenView, ThemeScreen>();
        services.AddSingleton<IScreenView, TopicScreen>();
        services.AddSingleton<IUiNavigationHandler, UiNavigationHandler>();
        services.AddSingleton<AgyTui.UI.Core.Commands.CommandRegistry>();
        services.AddSingleton<AgyTui.UI.Core.Navigation.Routers.GitCommandRouter>();
        services.AddSingleton<AgyTui.UI.Core.Navigation.Routers.AiCommandRouter>();
        services.AddSingleton<AgyTui.UI.Core.Navigation.Routers.LearnCommandRouter>();
        services.AddSingleton<AgyTui.UI.Core.Navigation.Routers.SystemCommandRouter>();
        services.AddSingleton<IErrorLogger, FileErrorLogger>();
        services.AddSingleton<CommandRouter>();
        services.AddSingleton<ICommandRouter>(sp => new CommandLoggingMiddleware(sp.GetRequiredService<CommandRouter>(), sp.GetRequiredService<IErrorLogger>()));

        _serviceProvider = services.BuildServiceProvider();
        return _serviceProvider;
    }
}

