# Phase 2-C Implementation Plan

> **Phase:** Section 8 Runtime Foundation — Phase 2-C: Section 9 Mode Integration  
> **Date:** 2026-08-31  
> **Status:** IN PROGRESS

---

## 1. Implementation Order

```
1. Policy Contracts
       ↓
2. Authorization Contracts
       ↓
3. ModeContext
       ↓
4. RuntimeLifecycleController Extension
       ↓
5. Tests
       ↓
6. Integration Tests
       ↓
7. Verification
```

---

## 2. Files to Create

### 2.1 Policy Contracts

| File | Description |
|------|-------------|
| `backend/modularity/runtime/JNPF.Runtime.Core/Policy/ExecutionPolicy.cs` | ExecutionPolicy struct |
| `backend/modularity/runtime/JNPF.Runtime.Core/Policy/AuthorizationToken.cs` | Authorization token |
| `backend/modularity/runtime/JNPF.Runtime.Core/Policy/AuthorizationResult.cs` | Authorization result |

### 2.2 Admission Contracts

| File | Description |
|------|-------------|
| `backend/modularity/runtime/JNPF.Runtime.Core/Admission/AdmissionResult.cs` | Admission result |
| `backend/modularity/runtime/JNPF.Runtime.Core/Admission/IExecutionAdmission.cs` | Admission interface |

### 2.3 ModeContext

| File | Description |
|------|-------------|
| `backend/modularity/runtime/JNPF.Runtime.Core/Mode/ModeContext.cs` | Mode snapshot |

---

## 3. Files to Modify

### 3.1 IRuntimeLifecycleController

| Change | Description |
|--------|-------------|
| Add | `CreateExecution(Guid sessionId, ModeType modeType, AuthorizationToken? auth)` |
| Add | `CreateExecution(Guid sessionId, ExecutionPolicy policy, IHookRegistry? hooks)` |
| Add | `GetCurrentModeContext(Guid sessionId)` |

### 3.2 RuntimeLifecycleController

| Change | Description |
|--------|-------------|
| Add | `_modeProvider` field (IModeProvider) |
| Add | `CreateExecution` overloads with Mode support |
| Add | `GetCurrentModeContext` method |
| Modify | `ExecuteAsync` to check Admission |

### 3.3 ExecutionContext

| Change | Description |
|--------|-------------|
| Add | `ModeContext` property |

### 3.4 RuntimeSession

| Change | Description |
|--------|-------------|
| Add | `ModeContext` property |

---

## 4. Test Files to Create

| File | Coverage |
|------|----------|
| `backend/tests/JNPF.Tests.Runtime.Core/Policy/ExecutionPolicyTests.cs` | Policy creation, authorization |
| `backend/tests/JNPF.Tests.Runtime.Core/Policy/AuthorizationTests.cs` | Token validation |
| `backend/tests/JNPF.Tests.Runtime.Core/Admission/AdmissionTests.cs` | Admission scenarios |
| `backend/tests/JNPF.Tests.Runtime.Core/Mode/ModeIntegrationTests.cs` | Mode → Execution integration |
| `backend/tests/JNPF.Tests.Runtime.Core/Mode/ModeIsolationTests.cs` | Isolation verification |

---

## 5. Step-by-Step Implementation

### Step 1: Create ExecutionPolicy

```csharp
// backend/modularity/runtime/JNPF.Runtime.Core/Policy/ExecutionPolicy.cs
namespace JNPF.Runtime.Core;

public readonly struct ExecutionPolicy
{
    public bool CanRead { get; }
    public bool CanVerify { get; }
    public bool CanWrite { get; }
    public bool RequiresExplicitAuthorization { get; }
    public AuthorizationToken? AuthorizationToken { get; }
    
    public ExecutionPolicy(
        bool canRead,
        bool canVerify,
        bool canWrite,
        bool requiresExplicitAuthorization,
        AuthorizationToken? authorizationToken)
    {
        CanRead = canRead;
        CanVerify = canVerify;
        CanWrite = canWrite;
        RequiresExplicitAuthorization = requiresExplicitAuthorization;
        AuthorizationToken = authorizationToken;
    }
    
    public static ExecutionPolicy FromMode(IMode mode, AuthorizationToken? auth = null)
    {
        var caps = mode.Capabilities;
        var constraints = mode.Constraints;
        
        var canRead = caps.Allowed.Contains(Capability.Observe) 
                   || caps.Allowed.Contains(Capability.Evaluate);
        var canVerify = caps.Allowed.Contains(Capability.Build);
        var canWrite = caps.Allowed.Contains(Capability.ApplyApprovedPatch)
                    || caps.Allowed.Contains(Capability.ModifyState);
        
        return new ExecutionPolicy(
            canRead, canVerify, canWrite,
            constraints.ModeRequiresExplicitAuthorization, auth);
    }
    
    public AuthorizationResult Authorize()
    {
        if (RequiresExplicitAuthorization && AuthorizationToken == null)
            return AuthorizationResult.Rejected("Explicit authorization required");
        if (RequiresExplicitAuthorization && !AuthorizationToken!.IsValid)
            return AuthorizationResult.Rejected("Invalid authorization token");
        return AuthorizationResult.Allowed();
    }
}
```

### Step 2: Create Authorization Contracts

```csharp
// AuthorizationToken.cs
public sealed class AuthorizationToken
{
    public string Value { get; }
    public DateTime ExpiresAt { get; }
    public bool IsValid => DateTime.UtcNow < ExpiresAt;
    
    public AuthorizationToken(string value, DateTime expiresAt)
    {
        Value = value ?? throw new ArgumentNullException(nameof(value));
        ExpiresAt = expiresAt;
    }
}

// AuthorizationResult.cs
public readonly struct AuthorizationResult
{
    public bool IsAuthorized { get; }
    public string? Reason { get; }
    
    public static AuthorizationResult Allowed() => new(true, null);
    public static AuthorizationResult Rejected(string reason) => new(false, reason);
}

// AdmissionResult.cs
public readonly struct AdmissionResult
{
    public bool IsAdmitted { get; }
    public string? RejectionReason { get; }
    
    public static AdmissionResult Admitted() => new(true, null);
    public static AdmissionResult Rejected(string reason) => new(false, reason);
}
```

### Step 3: Create ModeContext

```csharp
// ModeContext.cs
public sealed class ModeContext
{
    public ModeType Type { get; }
    public ExecutionPolicy Policy { get; }
    public DateTime PolicySnapshotTime { get; init; }
    
    internal ModeContext(ModeType type, ExecutionPolicy policy)
    {
        Type = type;
        Policy = policy;
        PolicySnapshotTime = DateTime.UtcNow;
    }
}
```

### Step 4: Update IRuntimeLifecycleController

```csharp
// Add new members
public interface IRuntimeLifecycleController
{
    // ... existing ...
    
    // Mode Integration
    ExecutionContext CreateExecution(Guid sessionId, ModeType modeType, AuthorizationToken? auth = null);
    ExecutionContext CreateExecution(Guid sessionId, ExecutionPolicy policy, IHookRegistry? hooks = null);
    ModeContext? GetCurrentModeContext(Guid sessionId);
}
```

### Step 5: Update RuntimeLifecycleController

```csharp
public sealed class RuntimeLifecycleController : IRuntimeLifecycleController
{
    private readonly IModeProvider _modeProvider;
    
    // Add IModeProvider constructor parameter
    public RuntimeLifecycleController(IModeProvider modeProvider)
    {
        _modeProvider = modeProvider ?? throw new ArgumentNullException(nameof(modeProvider));
    }
    
    public ExecutionContext CreateExecution(Guid sessionId, ModeType modeType, AuthorizationToken? auth)
    {
        var session = GetSession(sessionId);
        var mode = _modeProvider.ResolveAsync(modeType, CancellationToken.None).GetAwaiter().GetResult();
        var policy = ExecutionPolicy.FromMode(mode, auth);
        var modeContext = new ModeContext(modeType, policy);
        
        var execution = ExecutionContextFactory.Create(session.SessionId, modeContext);
        return execution;
    }
    
    public ExecutionContext CreateExecution(Guid sessionId, ExecutionPolicy policy, IHookRegistry? hooks)
    {
        var session = GetSession(sessionId);
        var execution = ExecutionContextFactory.CreateWithPolicy(session.SessionId, policy, hooks);
        return execution;
    }
    
    public ModeContext? GetCurrentModeContext(Guid sessionId)
    {
        var session = GetSession(sessionId);
        return session.ModeContext;
    }
}
```

### Step 6: Update RuntimeSession

```csharp
public sealed class RuntimeSession
{
    public RuntimeSession(RuntimeContext context)
    {
        // ... existing ...
    }
    
    public ModeContext? ModeContext { get; internal set; }
}
```

### Step 7: Update ExecutionContext

```csharp
public sealed class ExecutionContext
{
    public ExecutionId Id { get; }
    public Guid SessionId { get; }
    public IHookRegistry Hooks { get; }
    public ModeContext? ModeContext { get; }
    // ...
}
```

### Step 8: Modify ExecuteAsync

```csharp
public async Task<ExecutionResult> ExecuteAsync(ExecutionContext execution, Func<ExecutionContext, Task> work, CancellationToken ct)
{
    ArgumentNullException.ThrowIfNull(execution);
    ArgumentNullException.ThrowIfNull(work);
    
    // Check Admission
    if (execution.ModeContext != null)
    {
        var authResult = execution.ModeContext.Policy.Authorize();
        if (!authResult.IsAuthorized)
        {
            return ExecutionResult.Rejected(execution.Id, authResult.Reason!);
        }
    }
    
    // Continue with existing logic...
}
```

### Step 9: Update ExecutionResult

```csharp
public static ExecutionResult Rejected(ExecutionId id, string reason) =>
    new ExecutionResult(id, ExecutionState.Rejected, reason, null, TimeSpan.Zero);
```

### Step 10: Update ExecutionState

```csharp
public enum ExecutionState
{
    Pending,
    Running,
    Completed,
    Failed,
    Cancelled,
    Rejected  // NEW
}
```

---

## 6. Test Implementation

### 6.1 Policy Tests

```csharp
[Fact]
public void FromMode_AuditMode_HasReadOnlyPermissions()
{
    var mode = new AuditMode();
    var policy = ExecutionPolicy.FromMode(mode);
    
    Assert.True(policy.CanRead);
    Assert.False(policy.CanVerify);
    Assert.False(policy.CanWrite);
    Assert.False(policy.RequiresExplicitAuthorization);
}
```

### 6.2 Authorization Tests

```csharp
[Fact]
public void Authorize_ExecuteModeWithoutAuth_ReturnsRejected()
{
    var mode = new ExecuteMode();
    var policy = ExecutionPolicy.FromMode(mode); // no auth
    
    var result = policy.Authorize();
    
    Assert.False(result.IsAuthorized);
    Assert.Equal("Explicit authorization required", result.Reason);
}
```

### 6.3 Integration Tests

```csharp
[Fact]
public async Task ExecuteAsync_WithRejectedPolicy_ReturnsRejectedResult()
{
    var controller = new RuntimeLifecycleController(new DefaultModeProvider());
    var context = RuntimeContext.Create("t", "p", "pipe", "user");
    var session = await controller.InitializeAsync(context);
    
    var execution = controller.CreateExecution(session.SessionId, ModeType.Execute); // no auth
    
    var result = await controller.ExecuteAsync(execution, ctx => Task.CompletedTask);
    
    Assert.True(result.IsRejected);
}
```

---

## 7. Build & Test Plan

### 7.1 Build

```bash
dotnet build backend/modularity/runtime/JNPF.Runtime.Core/JNPF.Runtime.Core.csproj
```

### 7.2 Tests

```bash
# Phase 2-B regression
dotnet test backend/tests/JNPF.Tests.Runtime.Core/JNPF.Tests.Runtime.Core.csproj

# Phase 2-C new tests
dotnet test backend/tests/JNPF.Tests.Runtime.Core/JNPF.Tests.Runtime.Core.csproj --filter "FullyQualifiedName~Phase2C"
```

### 7.3 Architecture Verification

```bash
# Verify no concrete Mode in Runtime.Core
Get-ChildItem -Recurse -Path backend/modularity/runtime/JNPF.Runtime.Core -Filter "*.cs" | 
    Select-String -Pattern "AuditMode|VerifyMode|ExecuteMode|IMode"
# Expected: 0 matches
```

---

## 8. Acceptance Criteria

| Criterion | Verification |
|-----------|--------------|
| Policy.FromMode creates correct policy | Test |
| Authorization works correctly | Test |
| ExecuteAsync rejects unauthorized | Test |
| ModeContext is immutable | Test |
| No concrete Mode in Runtime.Core | Architecture scan |
| No breaking changes | API diff |
| Phase 2-B tests still pass | Regression |
