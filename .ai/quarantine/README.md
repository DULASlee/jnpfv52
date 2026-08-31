# Quarantine — NOT LOADED / NOT AUTHORITATIVE / NOT WATCHED

> **Purpose:** 隔离 Legacy / Unknown / Experimental Harness — 不删除，可逆，零权威。  
> **Date:** 2026-09-01 · **Gate:** Phase 0.5  
> **Policy:** 任何位于此目录下的文件 **不会** 被 Harness Resolver 加载，不参与 Governance，不被 Hook 监听。

---

## 1. What is here

| Item | Original Location | Reason | Status |
|------|-------------------|--------|--------|
| `backups/CLAUDE.md.bak.20260707` | `D:\JNPF-v52\CLAUDE.md.bak.20260707` | 备份冗余 | QUARANTINED — moved |
| `backups/archive-banner-stop.mjs.bak-20260808-simplify` | `.cursor/hooks/archive-banner-stop.mjs.bak-20260808-simplify` | Hook 简化备份 | QUARANTINED — moved |
| `backups/episodic-session-start.mjs.bak-20260808-simplify` | `.cursor/hooks/episodic-session-start.mjs.bak-20260808-simplify` | Hook 备份 | QUARANTINED — moved |
| `backups/session-archive-lib.mjs.bak-20260808-simplify` | `.cursor/hooks/session-archive-lib.mjs.bak-20260808-simplify` | Hook 备份 | QUARANTINED — moved |
| `backups/session-end.mjs.bak-20260808-simplify` | `.cursor/hooks/session-end.mjs.bak-20260808-simplify` | Hook 备份 | QUARANTINED — moved |
| `backups/episodic-memory-automation.mdc.bak-20260808-simplify` | `.cursor/rules/toolchain/episodic-memory-automation.mdc.bak-20260808-simplify` | Rule 备份 | QUARANTINED — moved |
| `_archived-manifest/MANIFEST.txt` | `.claude/_archived/**` (48 entries) | 历史归档 — 不参与加载，已记录 manifest | QUARANTINED — manifest only (original retained as cold archive) |
| `superpowers-brainstorm/MANIFEST.txt` | `.superpowers/brainstorm/**` | 临时 brainstorm 状态 | QUARANTINED — manifest only |

### Cold archive (retained but NOT LOADED)

- `.claude/_archived/**` — 48 entries (hooks/rules/orchestrator/verification) — **NOT LOADED** by any hook or resolver. Kept for audit, never auto-loaded.
- `.superpowers/brainstorm/**` — temp brainstorm server state — **NOT LOADED**.

### Disabled plugins (logically quarantined)

- `episodic-memory@superpowers-marketplace: false` (settings.json `enabledPlugins`)
- `double-shot-latte@superpowers-marketplace: false`
- `serena` global disabled (`disabledMcpjsonServers: ["serena"]` — project override via opencode.json is intentional Capability, not Governance)

---

## 2. Guarantees

```
NOT LOADED          — No hook, no resolver, no skill loader references this dir
NOT AUTHORITATIVE   — Cannot govern, cannot override Control Plane
NOT WATCHED         — No file watcher, no auto-sync (codegraph --no-watch)
REVERSIBLE          — Move back if needed, no deletion
```

Verified by:
- `hooks/guard-write.mjs` — does not scan `.ai/quarantine`
- `hooks/guard-skill-load.mjs` — does not load from `.ai/quarantine`
- `HARNESS-RESOLUTION.yaml` — quarantine.root excluded from resolution.steps
- `.gitignore` — `.ai/quarantine` is ignored (no commit)

---

## 3. How to restore (if needed)

```powershell
Move-Item -LiteralPath "D:\JNPF-v52\.ai\quarantine\backups\CLAUDE.md.bak.20260707" -Destination "D:\JNPF-v52\CLAUDE.md.bak.20260707"
```

But prefer **Migrate** over restore — extract principle, not file.

---

## 4. Next: Archive (after Migrate)

When a quarantined item's valuable principle has been migrated to Control Plane:

```
.ai/quarantine/<item> → .ai/archive/<item> (with MIGRATION-NOTE.md)
```

Archive is permanent cold storage, never loaded.

