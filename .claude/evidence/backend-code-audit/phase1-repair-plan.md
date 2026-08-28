# Phase 1: Critical Security Closure - Repair Plan

## Scope Constraint
**ONLY** process findings already confirmed in Trusted Baseline:
- J1: SQL injection risk (11 instances)
- J2: Hardcoded secrets (1 instance)
- J5: Unsafe deserialization (13 instances)
- N2: SQL injection (1 instance)

**TOTAL: 26 Critical Security Findings**

**PROHIBITED**: Do NOT expand audit scope to I2/C1/E4/J4/E1.

---

## Finding Group 1: J5 - Unsafe Deserialization (13 instances)

### Evidence Collection (Minimal Required)
1. Confirm deserialization entry point
2. Confirm input source (trusted/untrusted)
3. Identify current deserialization method

### Repair Strategy
- Replace `JsonConvert.DeserializeObject` with `JsonSerializer.Deserialize` (System.Text.Json)
- Or add `[JsonSerializerSettings]` with `TypeNameHandling.None`
- Validate input before deserialization

### Files to Fix
- `DataInterfaceService.cs` (814, 1970, 1972, + 10 more)
- Other files TBD from phase1-critical-security.json

### Acceptance Criteria
- [ ] No `JsonConvert.DeserializeObject` without type validation
- [ ] All deserialization uses safe serializer settings
- [ ] Unit test: malicious payload rejected
- [ ] Build passes: `dotnet build`

---

## Finding Group 2: J1 - SQL Injection Risk (11 instances)

### Evidence Collection (Minimal Required)
1. Confirm SQL concatenation point
2. Confirm data flow (user input → SQL)
3. Identify parameterization opportunity

### Repair Strategy
- Replace string concatenation with `SqlParameter[]`
- Use SqlSugar's `ISugarQueryable` with expression trees
- Or use `SqlSugarClient.Ado.ExecuteCommand` with parameters

### Files to Fix
- `BatchDeleteSqlPlanner.cs` (33)
- `FieldBindDefaultValueHelpers.cs` (109, 144)
- Other files TBD from phase1-critical-security.json

### Acceptance Criteria
- [ ] No SQL string concatenation with user input
- [ ] All dynamic SQL uses parameterized queries
- [ ] Unit test: SQL injection attempt blocked
- [ ] Build passes: `dotnet build`

---

## Finding Group 3: N2 - SQL Injection R7 (1 instance)

### Evidence Collection (Minimal Required)
1. Confirm `$"SELECT ..."` or `string.Format` SQL
2. Confirm table/column name source

### Repair Strategy
- Validate table/column name against whitelist
- Use `SqlSugarClient.Queryable<T>().Where()` instead of raw SQL

### File to Fix
- `ConfigController.cs` (293)

### Acceptance Criteria
- [ ] No interpolated SQL with dynamic table names
- [ ] Table name validated against whitelist
- [ ] Build passes: `dotnet build`

---

## Finding Group 4: J2 - Hardcoded Secrets (1 instance)

### Evidence Collection (Minimal Required)
1. Confirm hardcoded credential
2. Identify configuration alternative

### Repair Strategy
- Move to `appsettings.json` or Environment Variable
- Use `IConfiguration` to read at runtime

### File to Fix
- `WechatMiniProgramService.cs` (71)

### Acceptance Criteria
- [ ] No hardcoded secrets in source code
- [ ] Credential read from configuration
- [ ] Build passes: `dotnet build`

---

## Execution Order

1. **J5** (13 instances) - Highest count, systematic fix
2. **J1** (11 instances) - Critical security
3. **N2** (1 instance) - Critical security
4. **J2** (1 instance) - Credential exposure

## Verification Loop (Per Finding)

```
Confirm issue → Minimal evidence → Fix → Targeted test → Build → Next
```

**NO** expanded analysis permitted during Phase 1.

---

## Deliverables

1. Code changes per finding group
2. Unit tests for each fix
3. Full build verification: `dotnet build`
4. Re-scan to confirm findings suppressed