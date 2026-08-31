# Runtime.Core v0.1 — Approved Public API Surface Baseline

> **Status:** APPROVED — Phase 2-A.1 Contract Hardening Gate
> **Date:** 2026-08-31
> **Purpose:** Structural contract lock for API Freeze verification

> **⚠️ IMPORTANT — Baseline Origin:**
> 此 baseline **不是**从当前代码自动生成的。它是基于 **Chief Architect 审批的 ADR-006** 决策 + **Phase 2-A 实际实现** 人工审查后确定的 approved contract。
> Baseline 的每个 type/member 都对应一个 architectural decision，不是"当前有什么就记录什么"。
> 验证工具 `RuntimeApiSurfaceCheck` 使用此 baseline 与当前 assembly 差分，确保没有意外的 API surface 漂移。

---

## Baseline Source & Authority

| 依据 | 内容 |
|------|------|
| **ADR-006** | Runtime.Core v0.1 Architecture Decision Record |
| **Chief Architect Review** | 2026-08-31 Phase 2-A.1 审查反馈 |
| **Phase 2-A Contract** | 6 public types, internal RuntimeSession constructor |
| **Verification Tool** | `backend/tools/RuntimeApiSurfaceCheck` |

---

## Public Types

| # | Namespace | Type | Kind | File |
|---|-----------|------|------|------|
| 1 | `JNPF.Runtime.Core` | `RuntimeContext` | class (sealed) | `RuntimeContext.cs` |
| 2 | `JNPF.Runtime.Core` | `RuntimeSession` | class (sealed) | `RuntimeSession.cs` |
| 3 | `JNPF.Runtime.Core` | `RuntimeState` | enum | `RuntimeState.cs` |
| 4 | `JNPF.Runtime.Core` | `IRuntimeLifecycleController` | interface | `IRuntimeLifecycleController.cs` |
| 5 | `JNPF.Runtime.Core` | `RuntimeLifecycleController` | class (sealed) | `RuntimeLifecycleController.cs` |
| 6 | `JNPF.Runtime.Core` | `RuntimeStateMachine` | static class | `RuntimeStateMachine.cs` |

**Total: 6 public types**

---

## RuntimeContext — Public API

| # | Member | Kind | Signature |
|---|--------|------|-----------|
| 1 | `.TenantId` | property | `public string TenantId { get; }` |
| 2 | `.ProjectId` | property | `public string ProjectId { get; }` |
| 3 | `.PipelineId` | property | `public string PipelineId { get; }` |
| 4 | `.CreatedAtUtc` | property | `public DateTime CreatedAtUtc { get; }` |
| 5 | `.CreatorUserId` | property | `public string CreatorUserId { get; }` |
| 6 | `.Metadata` | property | `public IReadOnlyDictionary<string, string> Metadata { get; }` |
| 7 | `.Create(...)` | static method | `public static RuntimeContext Create(string tenantId, string projectId, string pipelineId, string creatorUserId)` |
| 8 | `.WithMetadata(...)` | instance method | `public RuntimeContext WithMetadata(string key, string value)` |

**Constructor:** private (use `Create` factory)

---

## RuntimeSession — Public API

| # | Member | Kind | Signature |
|---|--------|------|-----------|
| 1 | `.SessionId` | property | `public Guid SessionId { get; }` |
| 2 | `.Context` | property | `public RuntimeContext Context { get; }` |
| 3 | `.State` | property | `public RuntimeState State { get; private set; }` |
| 4 | `.StateChangedAtUtc` | property | `public DateTime StateChangedAtUtc { get; private set; }` |
| 5 | `.StateReason` | property | `public string? StateReason { get; private set; }` |

**Constructor:** `internal RuntimeSession(RuntimeContext context)` — NOT public

---

## RuntimeState — Public API

| # | Member | Kind | Value |
|---|--------|------|-------|
| 1 | `Created` | enum member | `= 0` |
| 2 | `Initialized` | enum member | `= 1` |
| 3 | `Running` | enum member | `= 2` |
| 4 | `Paused` | enum member | `= 3` |
| 5 | `Completed` | enum member | `= 4` |
| 6 | `Failed` | enum member | `= 5` |
| 7 | `Disposed` | enum member | `= 6` |

---

## IRuntimeLifecycleController — Public API

| # | Member | Kind | Signature |
|---|--------|------|-----------|
| 1 | `.CurrentSession` | property | `RuntimeSession? CurrentSession { get; }` |
| 2 | `.InitializeAsync(...)` | method | `Task<RuntimeSession> InitializeAsync(RuntimeContext context, CancellationToken cancellationToken = default)` |
| 3 | `.StartAsync(...)` | method | `Task StartAsync(Guid sessionId, CancellationToken cancellationToken = default)` |
| 4 | `.PauseAsync(...)` | method | `Task PauseAsync(Guid sessionId, CancellationToken cancellationToken = default)` |
| 5 | `.ResumeAsync(...)` | method | `Task ResumeAsync(Guid sessionId, CancellationToken cancellationToken = default)` |
| 6 | `.CompleteAsync(...)` | method | `Task CompleteAsync(Guid sessionId, CancellationToken cancellationToken = default)` |
| 7 | `.FailAsync(...)` | method | `Task FailAsync(Guid sessionId, string reason, CancellationToken cancellationToken = default)` |
| 8 | `.DisposeAsync(...)` | method | `Task DisposeAsync(Guid sessionId, CancellationToken cancellationToken = default)` |

---

## RuntimeLifecycleController — Public API

| # | Member | Kind | Signature |
|---|--------|------|-----------|
| 1 | (inherits all from IRuntimeLifecycleController) | — | — |

**Additional:** implements `IRuntimeLifecycleController`

---

## RuntimeStateMachine — Public API

| # | Member | Kind | Signature |
|---|--------|------|-----------|
| 1 | `.CanTransition(...)` | static method | `public static bool CanTransition(RuntimeState current, RuntimeState target)` |
| 2 | `.Transition(...)` | static method | `public static void Transition(RuntimeSession session, RuntimeState target, string? reason = null)` |

---

## API Freeze Constraints

### 禁止在 v0.2 前添加

| 类别 | 禁止内容 |
|------|----------|
| **New public types** | 任何新增 `public` class/interface/enum/struct |
| **New public members** | 任何新增 `public` method/property/event/field |
| **Public constructor** | `RuntimeSession` 构造函数必须保持 `internal` |
| **Intelligence types** | Agent / Capability / Memory / Tool / Model / Prompt / Skill / Plan / Workflow |
| **Profile types** | Profile / ProfileProvider / ProfileRegistry |
| **Knowledge types** | Knowledge / Memory / VectorStore |
| **LLM types** | Llm / Reasoner / Planner / Prompt / Step / DAG |

### 允许的变更（v0.x）

- 添加 `internal` 类型和成员
- 修改 `private` 实现细节
- 添加 `protected internal` 或 `private protected` 成员
- 添加 XML doc comments
- 扩展 enum 值（需 CR 审批）

---

## Verification

**Baseline hash:** `sha256:RUNTIME-CORE-V0.1-20260831`

**Verification method:**
```bash
dotnet run --project tools/RuntimeApiSurfaceCheck
# Expected output: PASS — No unexpected public surface diff
```

---

## Change Log

| Date | Change | CR | Status |
|------|--------|-----|--------|
| 2026-08-31 | Initial baseline | — | APPROVED |
