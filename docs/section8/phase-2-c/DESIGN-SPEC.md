# Phase 2-C Design Specification

> **Phase:** Section 8 Runtime Foundation — Phase 2-C: Section 9 Mode Integration  
> **Date:** 2026-08-31  
> **Status:** IN PROGRESS

---

## 1. Design Principles

### 1.1 Core Principles

1. **Runtime owns Execution** — Mode does not control execution lifecycle
2. **Mode owns Policy** — Policy semantics defined by Mode layer
3. **Minimal Contract** — Runtime knows only ExecutionPolicy, not Mode specifics
4. **Policy Immutable** — Snapshot at Admission, never mutated
5. **Admission Only** — Authorization checked once at admission, not per Hook

### 1.2 Anti-Patterns

```
❌ Runtime knowing Audit/Verify/Execute enum values
❌ Mode controlling ExecutionState
❌ Policy mutation during Execution
❌ Capability checking in Hooks
❌ Full Mode object in ExecutionContext
```

---

## 2. New Contracts

### 2.1 ExecutionPolicy

```csharp
namespace JNPF.Runtime.Core;

public readonly struct ExecutionPolicy
{
    public bool CanRead { get; }
    public bool CanVerify { get; }
    public bool CanWrite { get; }
    public bool RequiresExplicitAuthorization { get; }
    public AuthorizationToken? AuthorizationToken { get; }
    
    private ExecutionPolicy(
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
        var capabilities = mode.Capabilities;
        var constraints = mode.Constraints;
        
        var canRead = capabilities.Allowed.Contains(Capability.Observe) 
                   || capabilities.Allowed.Contains(Capability.Evaluate);
        var canVerify = capabilities.Allowed.Contains(Capability.Build);
        var canWrite = capabilities.Allowed.Contains(Capability.ApplyApprovedPatch)
                    || capabilities.Allowed.Contains(Capability.ModifyState);
        
        return new ExecutionPolicy(
            canRead,
            canVerify,
            canWrite,
            constraints.ModeRequiresExplicitAuthorization,
            auth);
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

### 2.2 AuthorizationToken

```csharp
namespace JNPF.Runtime.Core;

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
```

### 2.3 AuthorizationResult

```csharp
namespace JNPF.Runtime.Core;

public readonly struct AuthorizationResult
{
    public bool IsAuthorized { get; }
    public string? Reason { get; }
    
    private AuthorizationResult(bool isAuthorized, string? reason)
    {
        IsAuthorized = isAuthorized;
        Reason = reason;
    }
    
    public static AuthorizationResult Allowed() => new(true, null);
    public static AuthorizationResult Rejected(string reason) => new(false, reason);
}
```

### 2.4 ModeContext

```csharp
namespace JNPF.Runtime.Core;

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

---

## 3. Runtime.Core Extensions

### 3.1 IRuntimeLifecycleController Changes

```csharp
public interface IRuntimeLifecycleController
{
    // ... existing methods ...
    
    // === Mode Integration ===
    
    /// <summary>
    /// Creates an Execution with Mode policy.
    /// </summary>
    ExecutionContext CreateExecution(Guid sessionId, ModeType modeType, AuthorizationToken? auth = null);
    
    /// <summary>
    /// Creates an Execution with custom policy and hooks.
    /// </summary>
    ExecutionContext CreateExecution(Guid sessionId, ExecutionPolicy policy, IHookRegistry? hooks = null);
    
    /// <summary>
    /// Gets the ModeContext for the current session.
    /// </summary>
    ModeContext? GetCurrentModeContext(Guid sessionId);
}
```

### 3.2 IExecutionAdmission (NEW)

```csharp
namespace JNPF.Runtime.Core;

public interface IExecutionAdmission
{
    AdmissionResult Evaluate(ExecutionContext context, ExecutionPolicy policy);
}
```

### 3.3 AdmissionResult

```csharp
namespace JNPF.Runtime.Core;

public readonly struct AdmissionResult
{
    public bool IsAdmitted { get; }
    public string? RejectionReason { get; }
    
    private AdmissionResult(bool isAdmitted, string? rejectionReason)
    {
        IsAdmitted = isAdmitted;
        RejectionReason = rejectionReason;
    }
    
    public static AdmissionResult Admitted() => new(true, null);
    public static AdmissionResult Rejected(string reason) => new(false, reason);
}
```

---

## 4. Execution Lifecycle with Mode

### 4.1 Execution Creation Flow

```
1. CreateExecution(sessionId, modeType, auth?)
         ↓
2. Resolve Mode via IModeProvider
         ↓
3. Create ExecutionPolicy from Mode
         ↓
4. Authorize (check explicit auth if required)
         ↓
5. Create ModeContext (immutable snapshot)
         ↓
6. Create ExecutionContext + ModeContext
         ↓
7. Return ExecutionContext
```

### 4.2 Execution Flow with Admission

```
ExecuteAsync(execution, work)
      ↓
1. Get ModeContext from execution
      ↓
2. Check ModeContext.Policy
      ↓
3. Policy.Authorize()
      ↓
   ├── ALLOWED → Continue
   └── REJECTED → Return FailureResult (rejected)
      ↓
4. Continue with existing Hook Pipeline
```

---

## 5. API Changes Summary

### 5.1 New Public APIs in Runtime.Core

| Type | Visibility | Reason |
|------|-----------|--------|
| ExecutionPolicy | public struct | Policy contract |
| AuthorizationToken | public sealed class | Auth token |
| AuthorizationResult | public readonly struct | Auth result |
| ModeContext | public sealed class | Mode snapshot |
| AdmissionResult | public readonly struct | Admission result |
| IExecutionAdmission | public interface | Extensibility |

### 5.2 New Interface Members

| Interface | New Member | Rationale |
|-----------|-----------|-----------|
| IRuntimeLifecycleController | CreateExecution(sessionId, modeType, auth) | Mode-aware creation |
| IRuntimeLifecycleController | CreateExecution(sessionId, policy, hooks) | Direct policy creation |
| IRuntimeLifecycleController | GetCurrentModeContext(sessionId) | Mode query |

### 5.3 Breaking Change Assessment

```
Assessment: ADDITIVE ONLY

✅ New methods are additive (optional)
✅ Existing behavior unchanged
✅ No interface member removal
✅ No signature changes to existing methods

Result: No Breaking Change
```

---

## 6. File Structure

```
Runtime.Core/
├── Execution/
│   ├── ExecutionId.cs
│   ├── ExecutionState.cs
│   ├── ExecutionResult.cs
│   ├── ExecutionContext.cs
│   ├── ExecutionDescriptor.cs
│   ├── ExecutionContextFactory.cs
│   └── [NEW] ModeContext.cs
├── Policy/
│   └── [NEW] ExecutionPolicy.cs
├── Admission/
│   ├── [NEW] AuthorizationToken.cs
│   ├── [NEW] AuthorizationResult.cs
│   ├── [NEW] AdmissionResult.cs
│   └── [NEW] IExecutionAdmission.cs
├── Lifecycle/
│   ├── IRuntimeLifecycleController.cs
│   └── RuntimeLifecycleController.cs
└── Hooks/
    ├── IExecutionHook.cs
    ├── ExecutionHookRegistry.cs
    └── ...
```

---

## 7. Test Scenarios

### 7.1 Policy Tests

```
✅ ExecutionPolicy.FromMode(AuditMode) → CanRead=true, CanVerify=false, CanWrite=false
✅ ExecutionPolicy.FromMode(VerifyMode) → CanRead=true, CanVerify=true, CanWrite=false
✅ ExecutionPolicy.FromMode(ExecuteMode, auth) → CanRead=true, CanVerify=true, CanWrite=true
✅ ExecutionPolicy.FromMode(ExecuteMode) → RequiresExplicitAuthorization=true
```

### 7.2 Authorization Tests

```
✅ AuditMode policy.Authorize() → Allowed
✅ ExecuteMode without auth → Rejected("Explicit authorization required")
✅ ExecuteMode with valid auth → Allowed
✅ ExecuteMode with expired auth → Rejected("Invalid authorization token")
```

### 7.3 Lifecycle Tests

```
✅ CreateExecution with Audit → ExecutionContext with Audit Policy
✅ CreateExecution with Execute + auth → ExecutionContext with Execute Policy
✅ CreateExecution with Execute, no auth → ExecutionContext with Rejected policy
✅ Execution with Rejected policy → ExecuteAsync returns Failure immediately
```

### 7.4 Isolation Tests

```
✅ Runtime.Core has no AuditMode/VerifyMode/ExecuteMode reference
✅ ExecutionPolicy has no Mode-specific enum values
✅ ModeContext is immutable after creation
✅ Policy cannot be modified after snapshot
```

---

## 8. Design Summary

| Component | Design Decision |
|-----------|-----------------|
| Mode belongs to | Session |
| Policy scope | Execution (immutable snapshot) |
| Authorization check | Admission (once) |
| Runtime knows | ExecutionPolicy (minimal contract) |
| Runtime不知道 | Audit/Verify/Execute specific values |
| Policy creation | Via IModeProvider → ExecutionPolicy |
| Hook role | Observation only, no policy check |
