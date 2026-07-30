# Domain Bounded Contexts Audit & Reference Status Report

This document presents a complete audit of all domain model classes, records, and enums located in `csapp/AgyTui/Domain/`. It details all fields, properties, constructors, domain methods, and explicit reference status flags (**Production & Tests**, **Production Only**, **Test Only**, or **Unreferenced**).

---

## 1. Account Context (`Domain/AccountContext/`)

### 1.1 `AccountAggregate`
- **File Path**: `csapp/AgyTui/Domain/AccountContext/AccountAggregate.cs`
- **Class Type**: `public class AccountAggregate`
- **Reference Flag**: `[Production & Tests]`
- **Constructors**:
  - `AccountAggregate(string accountName, string? email = null, bool isActive = false, string quotaStatus = "OK", string? lastUsed = null, int usageCount = 0, IEnumerable<string>? requestHistory = null)`
- **Fields / Properties**:
  - `public string AccountName { get; private set; }`
  - `public string? Email { get; private set; }`
  - `public bool IsActive { get; private set; }`
  - `public string QuotaStatus { get; private set; }`
  - `public string LastUsed { get; private set; }`
  - `public int UsageCount { get; private set; }`
  - `public List<string> RequestHistory { get; private set; }`
- **Domain Methods**:
  - `MarkActive()`: Sets `IsActive = true`.
  - `MarkInactive()`: Sets `IsActive = false`.
  - `SetQuotaExceeded(bool exceeded)`: Sets `QuotaStatus` to `"Exceeded"` or `"OK"`.
  - `RecordUsage(string timestamp)`: Increments `UsageCount`, updates `LastUsed`, appends to `RequestHistory`.
  - `ToMetadata()`: Converts aggregate to `AccountMetadata`.
  - `FromMetadata(string accountName, AccountMetadata metadata, string? email, bool isActive)`: Factory method creating aggregate from `AccountMetadata`.
- **References**:
  - **Production**: `AgyAccountStore.cs` (`GetAccountAggregate`, `SaveAccountAggregate`, `UpdateAccountMetadata`, `SetAccountQuotaExceeded`, `SetActiveAccount`), `IAgyAccountStore.cs`.
  - **Tests**: `DomainContextsTests.cs`.

---

### 1.2 `EncryptedToken`
- **File Path**: `csapp/AgyTui/Domain/AccountContext/EncryptedToken.cs`
- **Class Type**: `public sealed record EncryptedToken(string AccountName, string CipherText, DateTime CreatedAtUtc)`
- **Reference Flag**: `[Production & Tests]`
- **Constructors**:
  - Positional record constructor: `EncryptedToken(string AccountName, string CipherText, DateTime CreatedAtUtc)`
- **Fields / Properties**:
  - `public string AccountName { get; init; }`
  - `public string CipherText { get; init; }`
  - `public DateTime CreatedAtUtc { get; init; }`
- **References**:
  - **Production**: `AgyVault.cs` (`CreateEncryptedToken`), `IAgyVault.cs`.
  - **Tests**: `DomainContextsTests.cs`.

---

### 1.3 `QuotaMetrics`
- **File Path**: `csapp/AgyTui/Domain/AccountContext/QuotaMetrics.cs`
- **Class Type**: `public sealed record QuotaMetrics(double RemainingWeekly, double Remaining5H, string TimeWeekly, string Time5H, int CountWeekly, int Count5H, string ExhaustionWeekly, string Exhaustion5H)`
- **Reference Flag**: `[Production & Tests]`
- **Constructors**:
  - Positional record constructor with 8 parameters.
- **Fields / Properties**:
  - `public double RemainingWeekly { get; init; }`
  - `public double Remaining5H { get; init; }`
  - `public string TimeWeekly { get; init; }`
  - `public string Time5H { get; init; }`
  - `public int CountWeekly { get; init; }`
  - `public int Count5H { get; init; }`
  - `public string ExhaustionWeekly { get; init; }`
  - `public string Exhaustion5H { get; init; }`
- **References**:
  - **Production**: `AgyQuotaEngine.cs`, `IAgyQuotaEngine.cs`.
  - **Tests**: `QuotaMetricsTests.cs`.

---

## 2. AI Agent Context (`Domain/AiAgentContext/`)

### 2.1 `AgentInvocationLog`
- **File Path**: `csapp/AgyTui/Domain/AiAgentContext/AgentInvocationLog.cs`
- **Class Type**: `public class AgentInvocationLog`
- **Reference Flag**: `[Production & Tests]`
- **Constructors**:
  - `AgentInvocationLog(string alias, long durationMs, bool success, string activeAccount, ProviderMode mode = ProviderMode.Auto)`
- **Fields / Properties**:
  - `public Guid Id { get; private set; }`
  - `public string Alias { get; private set; }`
  - `public DateTime TimestampUtc { get; private set; }`
  - `public long DurationMs { get; private set; }`
  - `public bool Success { get; private set; }`
  - `public string ActiveAccount { get; private set; }`
  - `public ProviderMode Mode { get; private set; }`
- **References**:
  - **Production**: `CommandLoggingMiddleware.cs` (instantiates and logs invocation records).
  - **Tests**: `DomainContextsTests.cs`.

---

### 2.2 `ProviderMode`
- **File Path**: `csapp/AgyTui/Domain/AiAgentContext/ProviderMode.cs`
- **Class Type**: `public enum ProviderMode`
- **Reference Flag**: `[Production & Tests]`
- **Enum Values**:
  - `Auto`
  - `CloudDirect`
  - `LocalOllama`
- **References**:
  - **Production**: `AiDashboardView.cs`, `CommandLoggingMiddleware.cs`, `Config.cs`, `CommandRegistry.cs`.
  - **Tests**: `AiClientTests.cs`, `AiModeCheckTests.cs`, `ShowAiDashboardTests.cs`, `SqlitePersistenceTests.cs`, `DomainContextsTests.cs`.

---

## 3. Learn Context (`Domain/LearnContext/`)

### 3.1 `FlashcardDeck`
- **File Path**: `csapp/AgyTui/Domain/LearnContext/FlashcardDeck.cs`
- **Class Type**: `public class FlashcardDeck`
- **Reference Flag**: `[Production & Tests]`
- **Constructors**:
  - `FlashcardDeck(string topic, int cardsCount = 0, double averageEaseFactor = 2.5, DateTime? lastReviewedUtc = null)`
- **Fields / Properties**:
  - `public string Topic { get; private set; }`
  - `public int CardsCount { get; private set; }`
  - `public double AverageEaseFactor { get; private set; }`
  - `public DateTime LastReviewedUtc { get; private set; }`
- **Domain Methods**:
  - `UpdateStats(int cardsCount, double averageEaseFactor)`: Updates cards count, ease factor, and `LastReviewedUtc`.
- **References**:
  - **Production**: `IStudyRepository.cs` (`LoadDeck`, `SaveDeck`), `JsonStudyRepository.cs`.
  - **Tests**: `DomainContextsTests.cs`.

---

## 4. Workspace Context (`Domain/WorkspaceContext/`)

### 4.1 `ProjectPath`
- **File Path**: `csapp/AgyTui/Domain/WorkspaceContext/ProjectPath.cs`
- **Class Type**: `public sealed record ProjectPath`
- **Reference Flag**: `[Production & Tests]` (via `WorkspaceAggregate`)
- **Constructors**:
  - `ProjectPath(string path)`
- **Fields / Properties**:
  - `public string Value { get; }`
  - `public bool Exists => Directory.Exists(Value) || File.Exists(Value);`
- **Domain Methods**:
  - `ToString()`: Returns normalized path `Value`.
- **References**:
  - **Production**: Consumed by `WorkspaceAggregate.cs` -> `WorkspaceRegistry.cs`.
  - **Tests**: `DomainContextsTests.cs`.

---

### 4.2 `WorkspaceAggregate`
- **File Path**: `csapp/AgyTui/Domain/WorkspaceContext/WorkspaceAggregate.cs`
- **Class Type**: `public class WorkspaceAggregate`
- **Reference Flag**: `[Production & Tests]`
- **Constructors**:
  - `WorkspaceAggregate(string name, string workspacePath, string corpusName, bool isActive = false, string? gitBranch = null, string? alias = null, IEnumerable<string>? tags = null)`
- **Fields / Properties**:
  - `public string Name { get; private set; }`
  - `public ProjectPath WorkspacePath { get; private set; }`
  - `public string CorpusName { get; private set; }`
  - `public bool IsActive { get; private set; }`
  - `public string? GitBranch { get; private set; }`
  - `public string? Alias { get; private set; }`
  - `public string[] Tags { get; private set; }`
- **Domain Methods**:
  - `Activate()`: Sets `IsActive = true`.
  - `Deactivate()`: Sets `IsActive = false`.
  - `SetBranch(string? branch)`: Updates `GitBranch`.
  - `ToEntry()`: Converts aggregate to `WorkspaceEntry`.
  - `FromEntry(WorkspaceEntry entry, bool isActive, string? gitBranch)`: Factory method creating aggregate from `WorkspaceEntry`.
- **References**:
  - **Production**: `WorkspaceRegistry.cs` (`GetWorkspaceAggregates`).
  - **Tests**: `DomainContextsTests.cs`.

---

## 5. Summary Reference Matrix

| Class Name | Bounded Context | Reference Status Flag | Production Reference Site(s) | Test Reference Site(s) |
| :--- | :--- | :---: | :--- | :--- |
| `AccountAggregate` | `AccountContext` | **Production & Tests** | `AgyAccountStore.cs`, `IAgyAccountStore.cs` | `DomainContextsTests.cs` |
| `EncryptedToken` | `AccountContext` | **Production & Tests** | `AgyVault.cs`, `IAgyVault.cs` | `DomainContextsTests.cs` |
| `QuotaMetrics` | `AccountContext` | **Production & Tests** | `AgyQuotaEngine.cs`, `IAgyQuotaEngine.cs` | `QuotaMetricsTests.cs` |
| `AgentInvocationLog` | `AiAgentContext` | **Production & Tests** | `CommandLoggingMiddleware.cs` | `DomainContextsTests.cs` |
| `ProviderMode` | `AiAgentContext` | **Production & Tests** | `AiDashboardView.cs`, `CommandLoggingMiddleware.cs`, `Config.cs` | `AiClientTests.cs`, `DomainContextsTests.cs` |
| `FlashcardDeck` | `LearnContext` | **Production & Tests** | `IStudyRepository.cs`, `JsonStudyRepository.cs` | `DomainContextsTests.cs` |
| `ProjectPath` | `WorkspaceContext` | **Production & Tests** | `WorkspaceAggregate.cs`, `WorkspaceRegistry.cs` | `DomainContextsTests.cs` |
| `WorkspaceAggregate` | `WorkspaceContext` | **Production & Tests** | `WorkspaceRegistry.cs` | `DomainContextsTests.cs` |
