# AGENTS.md

Compact instruction file for automated coding agents working in this repository.

## Project

JNPF v5.2 low-code platform — .NET 8 backend + Vue 3 frontends. Full architecture/rules in [CLAUDE.md](./CLAUDE.md); this file covers what an agent would otherwise guess wrong.

## Dev Environment Startup

**Only** use the unified script — never `npm run dev` or `dotnet run` directly:

```powershell
powershell -ExecutionPolicy Bypass -File D:\JNPF-v52\start-dev.ps1
```

Kills stale dotnet/node processes, frees ports 3100+5000, then launches frontend (`:3100`) and backend (`:5000` with hot-reload).

## Key Commands

| What | Command | Working Dir |
|------|---------|-------------|
| Backend build | `dotnet build` | `backend/` |
| Backend Release build | `dotnet build -c Release` | `backend/` |
| Backend CI build (with analyzers) | `dotnet build /p:CI_BUILD=true` | `backend/` |
| Backend tests | `dotnet test backend/zx_lowcode_netcore.sln` | repo root |
| Frontend lint | `pnpm lint` | `jnpf-web-vue3/` |
| Frontend type-check | `pnpm type-check` | `jnpf-web-vue3/` |
| Frontend unit tests | `pnpm test:unit` | `jnpf-web-vue3/` |
| Frontend build | `pnpm build` | `jnpf-web-vue3/` |
| Toolchain verify | `node scripts/verify-toolchain.mjs` | repo root |
| Git hooks enable (after clone) | `git config core.hooksPath .githooks` | repo root |

**CI gate order:** `lint → type-check → test:unit → build`

## Monorepo Layout

```
backend/              .NET 8 solution (zx_lowcode_netcore.sln)
  framework/          Core: DynamicApiController, DI, SqlSugar, JWT, Serilog
  infrastructure/     Cross-cutting: event bus, OAuth, WebSockets
  modularity/         15 business modules (system, workflow, visualdev, etc.)
  application/        Hosts: JNPF.API.Entry (main), JNPF.OA.API.Entry (OA — disabled)
  tests/              Integration test projects (Gate, Phase6, Stage5, ADR012)
  tools/              JNPF.Analyzers (custom Roslyn analyzer)
jnpf-web-vue3/        PC admin frontend → :3100 (pnpm, Vite, Ant Design Vue, WindiCSS)
jnpf-web-datascreen/  Data screen frontend → :8100/DataV/ (pnpm, Element Plus)
jnpf-app-vue3/        UniApp mobile H5 → :3800 (requires proxy_server.py)
```

## Architecture Rules (violation = broken system)

- **Never write Controllers.** All APIs auto-map from Service classes implementing `IDynamicApiController`.
- **Unified response:** `RESTfulResult<T>` wraps automatically. Throw `Oops.Oh()` (system) / `Oops.Bah()` (business) — never raw `Exception`. HTTP code 600 = JWT expired.
- **Codegen boundary:** Bugs in generated code → fix `.vm` template source. Never edit template output files directly.
- **Multi-tenant:** Every SqlSugar query MUST verify `ITenantFilter` is active. Missing filter = cross-tenant data leak.
- **OA module is disabled** — never modify. IoT/MES modules don't exist — never scaffold.
- **Database:** SqlSugar (SQL Server) + Dapper. Table names: `UPPER_SNAKE_CASE` with module prefix (`BASE_USER`, `FLOW_TASK`). C# code: PascalCase.

## Frontend SSE/Timer Rules (memory leak prevention)

Every `setTimeout`/`setInterval`/`EventSource`/`WebSocket` must follow these or leaks result:

1. **Save** timer return values to variables — never fire-and-forget.
2. **Clear** all timers in `onUnmounted`.
3. **EventSource reconnect must have a retry cap** (e.g., `MAX_RETRIES = 5`), not infinite.
4. **Never call `connect()` directly in `onerror`** — always via `setTimeout` + counter (synchronous error → busy loop).
5. **SSE URL must use `buildEventSourceUrl()`** from `/@/utils/http/sseUrl` — dev proxy requires `/dev` prefix, not raw `/api`.
6. **EventSource must pass JWT via `?token=`** — cannot set Authorization header. `buildEventSourceUrl()` handles this.

## Secrets / Config (gitignored)

- `backend/application/JNPF.API.Entry/Configurations/ConnectionStrings.json` — must create locally.
- `backend/application/JNPF.API.Entry/Configurations/JWT.json`
- `.env.local`, `.env.*.local`, `.env.toolchain` — never commit.

## Package Manager / Registry

- **Frontends:** pnpm (8.x). Registry pre-configured in root `.npmrc` → `registry.npmmirror.com`.
- **Backend:** NuGet with Huawei Cloud mirror (`backend/nuget.config`).
- Node.js 18+, .NET SDK 8.0 (pinned in `backend/global.json`).

## Default Credentials

admin / 123456 (seed data). Backend API docs: `http://localhost:5000/newapi`.

## Docker

```bash
# Production
docker compose -f docker-compose.production.yml --env-file .env.production up -d

# Backend image only
docker build -f backend/application/JNPF.API.Entry/Dockerfile -t jnpf-api backend/
```
