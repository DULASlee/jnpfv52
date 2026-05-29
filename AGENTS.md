# AGENTS.md

This file provides guidance to Codex (Codex.ai/code) when working with code in **`d:\JNPF-v52`** (v5.2 clean workspace).

## Workspace layout

```
d:\JNPF-v52\
├── backend\              # .NET solution (zx_lowcode_netcore.sln)
├── jnpf-web-vue3\       # PC frontend → :3100
├── jnpf-web-datascreen\ # Data screen → :8100/DataV/
├── jnpf-app-vue3\       # UniApp mobile → :3800 (H5 + proxy)
└── docs\                # Demo manual, architecture, toolchain
```

Archive: `d:\liu202505v2` — do not use for daily development.

## Build & Run

```bash
# Backend (.NET 6, global.json in backend/)
cd d:\JNPF-v52\backend
dotnet build
dotnet build -c Release
dotnet run --project application/JNPF.API.Entry/JNPF.API.Entry.csproj
dotnet run --project application/JNPF.OA.API.Entry/JNPF.OA.API.Entry.csproj
docker build -f application/JNPF.API.Entry/Dockerfile -t jnpf-api .

# PC frontend
cd d:\JNPF-v52\jnpf-web-vue3
pnpm run dev

# Data screen
cd d:\JNPF-v52\jnpf-web-datascreen
pnpm run dev

# Mobile H5 (after HBuilderX build + proxy)
cd d:\JNPF-v52\jnpf-app-vue3
python scripts/proxy_server.py
```

## Architecture

Low-code platform (JNPF) on a custom .NET framework. Backend tiers live under **`backend/`**:

### `backend/framework/` — Core Framework

- `DynamicApiController`, DI, `ConfigurableOptions`, Swagger/Knife4jUI, etc.
- Extensions: SqlSugar, Dapper, Mapster, JWT, Serilog

### `backend/infrastructure/` — Cross-cutting

Event bus, OAuth, WebSockets, third-party integrations

### `backend/modularity/` — Business modules

`system`, `oauth`, `workflow`, `visualdev`, `engine`, `codegen`, `message`, `taskscheduler`, `app`, `extend`, `common`, `visualdata`, `inteAssistant`, `zxdev`, `subdev`

### `backend/application/` — Hosts

- **`JNPF.API.Entry`** — main API (`Serve.Run()`, Velocity templates under `wwwroot/Template/`)
- **`JNPF.OA.API.Entry`** — OA API

### `backend/web/` — SQL init & static assets

e.g. `jnpf_sundial_init.sql`

### Frontends (repo root, not under backend)

| Directory | Role |
|-----------|------|
| `jnpf-web-vue3` | PC admin UI |
| `jnpf-web-datascreen` | Avue DataV designer |
| `jnpf-app-vue3` | UniApp mobile |

## Key patterns

- Dynamic API from `IDynamicApiService` services
- Connection strings: `backend/application/JNPF.API.Entry/Configurations/ConnectionStrings.json` (gitignored)
- Codegen: Apache Velocity `.vm` templates

## Analysis rules

- Roslynator + StyleCop; rules in `backend/dotnet.ruleset`, `backend/stylecop.json`, `backend/.editorconfig`

## Database

- SqlSugar (SQL Server) + Dapper
- Init SQL: `backend/web/jnpf_sundial_init.sql`

## Architecture documentation

Follow [`docs/architecture/ARCHITECTURE_DOC_RULES.md`](docs/architecture/ARCHITECTURE_DOC_RULES.md) and `.cursor/rules/architecture-doc-standards.mdc`.

## Agent toolchain

See [`.cursor/rules/toolchain-division.mdc`](.cursor/rules/toolchain-division.mdc) and [TOOLCHAIN.md](TOOLCHAIN.md). Episodic project: **`D--JNPF-v52`**.

- OpenSpec — knowledge base only
- Superpowers — development execution
- Serena — C# under `backend/modularity/` / `backend/framework/`
- Do **not** use `/opsx:apply` or `/opsx:explore` for day-to-day coding
