# Stage 2 Implementation Plan

> Date: 2026-06-07
> Tasks: 2.1 JnpfModule, 2.2 AppServiceCollectionExtensions, 2.3 TenantContext, 2.4 Non-HTTP Interceptors

## Directory & Namespace Mapping

| Task | Actual Path | Namespace |
|---|---|---|
| 2.1 Module System | `framework/JNPF/Modules/` | `JNPF.Modules` |
| 2.2 Extensions | `framework/JNPF/App/Extensions/AppServiceCollectionExtensions.cs` | `Microsoft.Extensions.DependencyInjection` |
| 2.3 TenantContext | `framework/JNPF.Extras.DatabaseAccessor.SqlSugar/TenantContext/` | `JNPF.Extras.DatabaseAccessor.SqlSugar.TenantContext` |
| 2.4 EventBus | `modularity/common/JNPF.Common.Core/EventBus/TenantPropagationExecutor.cs` | `JNPF.EventHandler` |
| 2.4 Schedule | `modularity/common/JNPF.Common.Core/Schedule/TenantPropagationJobExecutor.cs` | `JNPF.Schedule` |

## Sealing Status

| File | Sealed? | Can Modify? |
|---|---|---|
| SqlSugarConfigureExtensions.cs | Stage 1 | NO |
| Program.cs | Stage 1 | NO |
| JwtHandler.cs | Stage 1 | NO |
| AppServiceCollectionExtensions.cs | **Stage 2** | YES (this stage) |
| Startup.cs | Stage 3 | YES (this stage) |
| SqlSugarRepository.cs | Stage 4 | YES (later) |

## Task 2.1: JnpfModule Module System

**6 files** in `framework/JNPF/Modules/`

| File | Purpose |
|---|---|
| `JnpfModule.cs` | Abstract base class with ConfigureServices/OnApplicationInitialization virtual methods |
| `DependsOnAttribute.cs` | `[DependsOn(typeof(A), typeof(B))]` for dependency declaration |
| `ModuleDescriptor.cs` | Metadata wrapper: Type, Name, Dependencies |
| `ModuleGraphBuilder.cs` | Kahn algorithm topological sort + cycle detection |
| `ModuleLoadException.cs` | Custom exception with CircularPath property |
| `LegacyModule.cs` | Bridge module that calls old AddStartups() logic, always first in order |

**Unit Tests** (5 cases in `backend/tests/verifications/ModuleVerification/`):
1. Linear A→B→C → [C, B, A]
2. Diamond DAG → valid topological order
3. Cycle → ModuleLoadException with path
4. No deps → registration order
5. LegacyModule always first

## Task 2.2: Extend AppServiceCollectionExtensions.cs

**Modify** `framework/JNPF/App/Extensions/AppServiceCollectionExtensions.cs`:

1. Add `AddJnpfModules()` — scan, sort, instantiate, ConfigureServices
2. Add `UseJnpfModules(app, modules)` — call OnApplicationInitialization
3. Mark `AddStartups()` as `[Obsolete]`
4. Update `AddApp()` to call both (legacy + new)

## Task 2.3: TenantContext Service Family

**12 files + 1 middleware** in `framework/JNPF.Extras.DatabaseAccessor.SqlSugar/TenantContext/`

| File | Purpose |
|---|---|
| `ITenantContext.cs` | Interface: TenantId, SystemId, UserId, ConnectionInfo, SetFromHttpContext, SetExplicit, BeginScope, ClearScope |
| `TenantContext.cs` | Impl with static AsyncLocal<TenantInfo> Current; IHttpContextAccessor + ITenantResolver[] injection |
| `TenantInfo.cs` | Value object: TenantId, SystemId, UserId, ConnectionInfo |
| `ITenantResolver.cs` | Strategy interface: ResolveTenantId(HttpContext) |
| `ClaimTenantResolver.cs` | JWT Claims extraction |
| `FallbackTenantResolver.cs` | 4-level: JWT → Header → Query → default |
| `CacheTenantResolver.cs` | ICacheManager lookup (jnpf:global:tenant) |
| `TenantConnectionInfo.cs` | ConfigId, ConnectionString, DatabaseType, IsolationType |
| `ITenantIsolationStrategy.cs` | ApplyQueryFilter, ApplyWriteProtection, ConfigureConnection |
| `ColumnIsolationStrategy.cs` | QueryFilter.AddTableFilter for ITenantFilter |
| `SchemaIsolationStrategy.cs` | Schema switching via AsTenant() |
| `DisposableAction.cs` | IDisposable helper for BeginScope |

**Middleware**: `TenantMiddleware.cs` — try/finally with ClearScope()

**DI Registration**: Via new `TenantModule.cs` (JnpfModule subclass) — NOT in sealed SqlSugarConfigureExtensions.cs

## Task 2.4: Non-HTTP Context Propagation

### EventBus: `TenantPropagationExecutor.cs`

Composite pattern — wraps existing RetryEventHandlerExecutor:
1. Extract TenantId from IEventSource.Payload (reflection: check for "TenantId" property)
2. Set TenantContext.Current via static method
3. Delegate to inner RetryEventHandlerExecutor
4. finally: TenantContext.ClearCurrent()

**Registration**: Change Startup.cs line 260 from `AddExecutor<RetryEventHandlerExecutor>()` to `AddExecutor<TenantPropagationExecutor>()`. Startup.cs is NOT sealed until Stage 3.

### Schedule: `TenantPropagationJobExecutor.cs`

Same pattern — extract tenant from JobDetail.Properties JSON:
1. Parse Properties JSON, extract "tenantId"
2. Set TenantContext.Current
3. Execute job
4. finally: ClearCurrent()

**Registration**: Via new module or Startup.cs modification (AddExecutor<TenantPropagationJobExecutor>() in AddSchedule options).

## Execution Order

```
Task 2.1 (Day 1-3) → Task 2.2 (Day 4) → Task 2.4 (Day 5-6)
Task 2.3 (Day 1-4, parallel with 2.1)
```

## Key Constraints

- All new files use existing project namespaces and conventions
- sealed files (SqlSugarConfigureExtensions.cs, Program.cs, JwtHandler.cs) are NOT modified
- AppServiceCollectionExtensions.cs modified once, then sealed
- Startup.cs modified for executor registration (sealed in Stage 3)
- Unit tests for all new infrastructure
