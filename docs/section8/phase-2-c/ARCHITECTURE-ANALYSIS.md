# Phase 2-C Architecture Analysis

> **Phase:** Section 8 Runtime Foundation — Phase 2-C: Section 9 Mode Integration  
> **Date:** 2026-08-31  
> **Status:** IN PROGRESS

---

## 1. Ownership Model

### 1.1 Runtime Layer owns

```
RuntimeState
RuntimeContext
RuntimeSession
ExecutionContext
ExecutionState
ExecutionResult
ExecutionAdmission
Hook Registry
Event Publisher
```

### 1.2 Mode Layer owns

```
IMode
ModeType
Capability
ModeCapabilitySet
ConstraintSet
IModeProvider
ModeDescriptor (metadata)
```

### 1.3 Ownership Boundary

| Ownership | Runtime | Mode |
|-----------|---------|------|
| Execution Lifecycle | ✅ | ❌ |
| Execution Policy | ❌ | ✅ |
| Policy Resolution | ✅ (via Provider) | ❌ |
| Policy Immutable | ✅ (snapshot) | ✅ |
| Admission Decision | ✅ | ❌ |
| State Machine | ✅ | ❌ |
| Capability Definition | ❌ | ✅ |
| Constraint Definition | ❌ | ✅ |

---

## 2. Dependency Direction

### 2.1 Allowed Dependencies

```
Runtime.Core
    │
    ├──► IModeProvider (Port)
    │        │
    │        └──► IMode (Section 9)
    │
    └──► ExecutionPolicy (NEW - minimal contract)
             │
             └──► Mode owns policy semantics

Section 9 Mode
    │
    └──► IModeProvider
             │
             └──► IMode implementations
```

### 2.2 Forbidden Dependencies

```
❌ Runtime.Core → IMode concrete implementation
❌ Runtime.Core → AuditMode / VerifyMode / ExecuteMode
❌ IMode → Runtime.Core types
❌ ExecutionPolicy → Mode-specific enum values
❌ Mode → ExecutionContext
```

---

## 3. Integration Points

### 3.1 Runtime → Mode Integration

```
IRuntimeLifecycleController
    │
    ├── CurrentSession
    │       │
    │       └── RuntimeSession
    │               │
    │               ├── SessionId
    │               ├── RuntimeContext (triple-key)
    │               ├── RuntimeState
    │               └── [NEW] ModeContext
    │
    └── CreateExecution(sessionId, modeType?)
            │
            ├── Resolve Mode via IModeProvider
            ├── Create ExecutionPolicy (snapshot)
            ├── Create ExecutionContext + Policy
            └── Return ExecutionContext
```

### 3.2 ModeContext (NEW)

```csharp
public sealed class ModeContext
{
    public ModeType Type { get; }
    public ExecutionPolicy Policy { get; }
    public DateTime PolicySnapshotTime { get; }
    
    // Immutable after creation
}
```

### 3.3 ExecutionPolicy (NEW - minimal contract)

```csharp
// Minimal contract - Runtime doesn't know Mode specifics
public readonly struct ExecutionPolicy
{
    public bool CanRead { get; }
    public bool CanVerify { get; }
    public bool CanWrite { get; }
    public bool RequiresExplicitAuthorization { get; }
    
    // Authorization result
    public AuthorizationResult Authorize();
}
```

---

## 4. Authorization Model

### 4.1 Mode-Based Authorization Matrix

| Mode | Read | Verify | Write | Requires Auth |
|------|------|--------|-------|--------------|
| Audit | ✅ | ❌ | ❌ | ❌ |
| Verify | ✅ | ✅ | ❌ | ❌ |
| Execute | ✅ | ✅ | ✅ | ✅ |
| Assist | Profile | Profile | Profile | Profile |

### 4.2 Authorization Flow

```
Execution Request
      ↓
Check ModeContext.Policy
      ↓
  ├── ModeRequiresExplicitAuthorization = true?
  │       ├── YES → Check explicit authorization
  │       │           ├── Authorized → ALLOW
  │       │           └── NOT Authorized → REJECT
  │       └── NO → ALLOW
      ↓
Execution continues
```

---

## 5. Lifecycle Coordination

### 5.1 Two-Level Lifecycle

```
RuntimeSession (Level 1)
    │
    ├── RuntimeState (Initializing → Running → Disposed)
    │
    └── Execution (Level 2) [Multiple per Session]
            │
            ├── ExecutionState (Pending → Running → Completed/Failed/Cancelled)
            │
            └── ModeContext (immutable once created)
                    │
                    ├── ModeType
                    └── ExecutionPolicy
```

### 5.2 State Transition Authority

| Transition | Authority | Notes |
|-----------|-----------|-------|
| RuntimeSession state | Runtime | RuntimeStateMachine |
| Execution state | Runtime | ExecutionContext lifecycle |
| ModeContext create | Runtime | Via IModeProvider |
| Policy immutable | Runtime | Snapshot at creation |

---

## 6. Isolation Verification

### 6.1 Runtime.Core Isolation Requirements

```
Runtime.Core must NOT contain:
    ❌ AuditMode / VerifyMode / ExecuteMode
    ❌ Capability enum
    ❌ ModeCapabilitySet
    ❌ ConstraintSet
    ❌ IMode concrete implementations
    ❌ Prompt / LLM / Workflow / Intelligence
```

### 6.2 Dependency Check

```csharp
// Dependency direction (verified via build):
JNPF.Runtime.Core
    └──► JNPF.Runtime.Capability (IModeProvider port only)

JNPF.Runtime.Capability
    └──► (no Runtime.Core dependencies)
```

---

## 7. Event Metadata

### 7.1 Allowed in Events

```csharp
public record ExecutionStartedEvent
{
    public ExecutionId ExecutionId { get; init; }
    public Guid SessionId { get; init; }
    public ModeType? CurrentMode { get; init; }  // Allowed
    // ...
}
```

### 7.2 Forbidden in Events

```
❌ Full Mode description
❌ Capability set details
❌ Profile content
❌ Knowledge content
❌ Prompt / LLM state
❌ Internal Mode state
```

---

## 8. Architecture Analysis Summary

| Dimension | Status |
|-----------|--------|
| Ownership Model | ✅ Clear separation |
| Dependency Direction | ✅ Runtime → Mode only |
| Lifecycle Coordination | ✅ Two-level model |
| Authorization | ✅ Mode-based, Admission only |
| Isolation | ✅ No concrete Mode in Runtime |
| Events | ✅ Minimal Mode metadata |
| Policy Immutability | ✅ Snapshot at Admission |

**Conclusion:** Architecture supports Mode → Execution integration without Runtime knowing Mode implementation.
