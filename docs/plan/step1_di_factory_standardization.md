# Detailed Plan - Step 1: Solution DI Audit, Prohibition of `new ServiceObject()`, and Test Fixtures

## 1. Executive Summary
This document defines the strict solution-wide Dependency Injection (DI) audit rules for `csapp/AgyTui` and `csapp/AgyTui.Tests`. **Direct instantiation of service objects using `new ServiceObject()` across classes is strictly prohibited.** Every service consumed by another class MUST be requested via constructor DI or top-level `Func<T>` factory delegates, backed by clean interfaces (`I<ServiceName>`) registered in `Bootstrapper.cs`.

---

## 2. Prohibition of `new ServiceObject()` Audit & Resolution Rules

### 2.1 The Anti-Pattern vs Standardized Solution

| Anti-Pattern (`new ServiceObject()`) | Standardized DI Solution |
| :--- | :--- |
| `new AiProcessRunner().RunInteractive(...)` | `var runner = _processRunnerFactory(); runner.RunInteractive(...)` |
| `private static readonly IAgyVault _vault = new AgyVault();` | `private static readonly Func<IAgyVault> _vaultFactory = () => Bootstrapper.ServiceProvider.GetRequiredService<IAgyVault>();` |
| `public AgyAccountStore() : this(new SqliteAgyAccountRepository(new SqliteDatabase()))` | Primary constructor with DI fallback `Bootstrapper.ServiceProvider.GetRequiredService<IAgyAccountRepository>()` |
| `public AgyQuotaEngine() : this(new AgyAccountStore())` | Primary constructor with `Func<IAgyAccountStore>` fallback |
| `new AccountTreeWidget()`, `new QuotaChartWidget()` | Instantiated via DI `IServiceProvider` in `StatusWidgetRegistry` |
| `var store = Bootstrapper.ServiceProvider.GetRequiredService<IAgyAccountStore>()` inside method bodies | Top-level `private static readonly Func<IAgyAccountStore> AccountStoreFactory = ...` |

---

## 3. Complete C# Solution DI Scan & Interface Registry

The following table lists every service component in `csapp/AgyTui`, its interface, registration lifetime, and injection pattern:

| Domain / Layer | Class Name | Required Interface | DI Lifetime | Injection Pattern |
| :--- | :--- | :--- | :--- | :--- |
| **Account Management** | `AgyAccountStore` | `IAgyAccountStore` | `Singleton` | Constructor `IAgyAccountRepository`, `Func<IAgyQuotaEngine>`, `Func<IAgyVault>` |
| **Account Management** | `AgyQuotaEngine` | `IAgyQuotaEngine` | `Singleton` | Constructor `IAgyAccountStore` |
| **Account Management** | `AgyVault` | `IAgyVault` | `Singleton` | Constructor `IAgyAccountStore` |
| **AI Integration** | `ClaudeProvider` | `IClaudeClient` | `Singleton` | Constructor `IAiProcessRunner`, `Func<IAgyAccountStore>` |
| **AI Integration** | `HermesProvider` | `IHermesClient` | `Singleton` | Constructor `IAiProcessRunner` |
| **AI Integration** | `OpenClawProvider` | `IOpenClawClient` | `Singleton` | Constructor `IAiProcessRunner` |
| **AI Integration** | `OllamaClient` | `IOllamaClient` | `Singleton` | Constructor `HttpClientProvider` |
| **AI Integration** | `AiProcessRunner` | `IAiProcessRunner` | `Singleton` | Constructor `Func<IAgyAccountStore>` |
| **AI Integration** | `AiProjectScanner` | `IAiProjectScanner` | `Singleton` | Constructor `IAiProcessRunner` |
| **AI Integration** | `AiCommitGenerator` | `IAiCommitGenerator` | `Singleton` | Constructor `IAiProcessRunner` |
| **AI Integration** | `AiLearningGenerator`| `IAiLearningGenerator`| `Singleton` | Constructor `IAiProcessRunner`, `Func<IClaudeClient>` |
| **Tool Integration** | `AwsClient` | `IAwsClient` | `Singleton` | Constructor `Helpers.ProcessRunner` |
| **Tool Integration** | `DockerClient` | `IDockerClient` | `Singleton` | Constructor `Helpers.ProcessRunner` |
| **Tool Integration** | `DotNetClient` | `IDotNetClient` | `Singleton` | Constructor `Helpers.ProcessRunner` |
| **Tool Integration** | `GitClient` | `IGitClient` | `Singleton` | Constructor `Helpers.ProcessRunner` |
| **Persistence** | `SqliteDatabase` | `ISqliteDatabase` | `Singleton` | Constructor `IOptions<DatabaseOptions>` |
| **Persistence** | `SqliteAgyAccountRepository` | `IAgyAccountRepository` | `Singleton` | Constructor `ISqliteDatabase` |
| **Persistence** | `SqliteConfigRepository` | `IConfigRepository` | `Singleton` | Constructor `ISqliteDatabase` |
| **Persistence** | `JsonStudyRepository` | `IStudyRepository` | `Singleton` | Constructor `IAppPathManager` |
| **Core Services** | `AppPathManager` | `IAppPathManager` | `Singleton` | Constructor `IOptions<AppPathOptions>` |

---

## 4. Unit Test Injection Fixture Pattern (`ServiceTestFixture`)

To support fast, isolated unit testing without depending on global state or live filesystems, `csapp/AgyTui.Tests` includes a dedicated test fixture builder:

```csharp
namespace AgyTui.Tests.Fixtures;

public class ServiceTestFixture : IDisposable
{
    public IServiceCollection Services { get; } = new ServiceCollection();
    public IServiceProvider ServiceProvider => Services.BuildServiceProvider();

    public ServiceTestFixture()
    {
        // Register Mock / Fake Defaults for Unit Testing
        Services.AddSingleton<ISqliteDatabase, FakeSqliteDatabase>();
        Services.AddSingleton<IAgyAccountRepository, InMemoryAgyAccountRepository>();
        Services.AddSingleton<IAppPathManager, TestAppPathManager>();
        Services.AddSingleton<IAgyQuotaEngine, TestAgyQuotaEngine>();
        Services.AddSingleton<IAgyVault, TestAgyVault>();
        Services.AddSingleton<IAgyAccountStore, AgyAccountStore>();
    }

    public ServiceTestFixture WithMock<TService>(TService mockInstance) where TService : class
    {
        Services.RemoveAll(typeof(TService));
        Services.AddSingleton(mockInstance);
        return this;
    }

    public void Dispose() { }
}
```

---

## 5. Implementation Checklist

- [x] Identify and catalog all `new ServiceObject()` instantiations across `csapp/AgyTui`.
- [x] Prohibit direct service instantiations in favor of `Func<T>` factories or constructor injection.
- [ ] Refactor fallback constructors in `AgyAccountStore`, `AgyQuotaEngine`, `AgyVault` to use DI resolving.
- [ ] Refactor static helper call sites (`AiProcessRunner.RunInteractiveStatic`, `AgySecretVault`, `TokenVault`, `StatusWidgets`, `ScreenChrome`, `SubPageNavigator`, `AiDashboardView`, `LearnDataPaths`) to use top-level `Func<T>` factories.
- [ ] Create `ServiceTestFixture.cs` in `csapp/AgyTui.Tests/Fixtures/`.
- [ ] Migrate `AccountServiceTests.cs`, `AccountStatsTests.cs`, and `QuotaCentralizationTests.cs` to consume `ServiceTestFixture`.
- [x] Verify 100% test pass rate across `dotnet test`.
