# Archive — Retired after Migrate (Cold Storage)

> **Purpose:** 已完成 Migrate 的 Harness 项 — 保留审计，不再加载。  
> **Status:** EMPTY (Phase 0.5 — no migrations retired yet)

When a quarantined item is migrated:

```
.ai/quarantine/<item> → .ai/archive/<item>/MIGRATION-NOTE.md
```

MIGRATION-NOTE must record:
- Source principle
- Target Control Plane file
- Date + Reviewer

No files here yet — next migrations: `verification-before-completion` principle → Control Plane Verification Policy.
