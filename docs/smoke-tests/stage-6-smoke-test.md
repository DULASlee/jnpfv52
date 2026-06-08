# Stage 6 Smoke Test Report

**Date:** 2026-06-07
**Stage:** 6 — HealthCheck Module + RateLimiting Module + Config Isolation + CancellationToken Coverage

---

## L1: Build Verification

| Project | Result |
|---|---|
| JNPF.API.Entry | PASS (0 errors, 1882 warnings pre-existing) |

---

## L2: Runtime Verification

| Check | Result |
|---|---|
| Port 5000 LISTENING | PASS (PID 19912) |
| Service startup | PASS (no crash) |

---

## L3: Health Check Endpoints

| Endpoint | Expected | Actual | Result |
|---|---|---|---|
| GET /health/live | "Healthy" | "Healthy" | PASS |
| GET /health | JSON with sqlserver check | `{"status":"Healthy","checks":[{"name":"sqlserver","status":"Healthy","duration":"00:00:00.0041394"}]}` | PASS |
| GET /health/ready | JSON with "db" tag filter | `{"status":"Healthy","checks":[{"name":"sqlserver","status":"Healthy","duration":"00:00:00.0012551"}]}` | PASS |

---

## L4: API Smoke Test

| Endpoint | Method | Result |
|---|---|---|
| /api/OAuth/getConfig/admin | GET | PASS (HTTP 200, returns `{"enableVerificationCode":false,"verificationCodeNumber":3}`) |
| /newapi/index.html | GET | PASS (HTTP 200, Knife4j Swagger UI loads) |

---

## Task Verification Matrix

| Task | Description | Status |
|---|---|---|
| 6.1 | HealthCheckModule — 3 endpoints consolidated from DatabaseModule + AuthenticationModule | PASS |
| 6.2 | RateLimitingModule — 3 FixedWindow policies (fixed/login/export), 429 JSON response | PASS |
| 6.3 | Config environment isolation — EventBus.json sanitized, .gitignore updated, env-variables-guide.md created | PASS |
| 6.4 | CancellationToken coverage — 15 methods in OAuthService + 4 in DeadLetterService | PASS |

---

## Module Dependency Chain

```
JsonSettingsModule
    ↓
RateLimitingModule (6.2)
    ↓
AuthenticationModule [DependsOn(JsonSettingsModule, RateLimitingModule, WeixinModule)]
    ↓
DatabaseModule
    ↓
HealthCheckModule (6.1) [DependsOn(DatabaseModule)]
```

---

## Files Changed (Stage 6)

| File | Operation | Task |
|---|---|---|
| Modules/HealthCheckModule.cs | NEW | 6.1 |
| Modules/RateLimitingModule.cs | NEW | 6.2 |
| Modules/DatabaseModule.cs | MODIFIED (removed health check registration) | 6.1 |
| Modules/AuthenticationModule.cs | MODIFIED (removed health endpoints, added RateLimitingModule dep) | 6.1, 6.2 |
| Modules/JsonSettingsModule.cs | MODIFIED (removed rate limiting code) | 6.2 |
| Configurations/EventBus.json | MODIFIED (sanitized credentials) | 6.3 |
| Configurations/EventBus.Development.json | NEW (local dev override) | 6.3 |
| .gitignore | MODIFIED (added EventBus.json) | 6.3 |
| docs/deployment/env-variables-guide.md | NEW | 6.3 |
| OAuthService.cs | MODIFIED (15 CancellationToken params) | 6.4 |
| Services/DeadLetterService.cs | MODIFIED (4 CancellationToken params) | 6.4 |

---

## Pre-existing Issues (Not Caused by Stage 6)

- MemoryCache.GetAllKeys() NullRef — known .NET 8 API change
- 1882 CA warnings — pre-existing code analysis warnings
