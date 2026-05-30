# CLAUDE.md

> **Runtime:** `claude.ai/code` — This file is scoped for Claude Code CLI and web environment.
> Other AI runtimes (e.g., Copilot, Cursor) should reference their own configuration files.

## Workspace

Monorepo: `backend/` + three frontends + `docs/`. Daily work here only; `d:\liu202505v2` is archive.

## Build & Run

```bash
# Backend (.NET 6, global.json in backend/)
cd d:\JNPF-v52\backend && dotnet build
cd d:\JNPF-v52\backend && dotnet run --project application/JNPF.API.Entry/JNPF.API.Entry.csproj
cd d:\JNPF-v52\backend && dotnet run --project application/JNPF.OA.API.Entry/JNPF.OA.API.Entry.csproj

# Frontends (pnpm)
cd d:\JNPF-v52\jnpf-web-vue3 && pnpm run dev        # PC admin UI → :3100
cd d:\JNPF-v52\jnpf-web-datascreen && pnpm run dev    # DataV → :8100/DataV/
cd d:\JNPF-v52\jnpf-app-vue3 && python scripts/proxy_server.py  # Mobile H5
```

## Architecture

Low-code platform (JNPF) on a custom .NET framework. Backend tiers under `backend/`:

- **`framework/`** — Core: `DynamicApiController`, DI, `ConfigurableOptions`, Swagger/Knife4jUI, SqlSugar, Dapper, Mapster, JWT, Serilog
- **`infrastructure/`** — Cross-cutting: event bus, OAuth, WebSockets, third-party integrations
- **`modularity/`** — Business modules: `system`, `oauth`, `workflow`, `visualdev`, `engine`, `codegen`, `message`, `taskscheduler`, `app`, `extend`, `common`, `visualdata`, `inteAssistant`, `zxdev`, `subdev`
- **`application/`** — Hosts: `JNPF.API.Entry` (main API), `JNPF.OA.API.Entry` (OA API). Velocity templates under `wwwroot/Template/`

> **注意：** OA 模块（JNPF.OA.API.Entry）在本项目中未启用。如需激活，请先经架构师评估与现有 IoT/MES 模块的集成影响。

- **`web/`** — SQL init (`jnpf_sundial_init.sql`) + static assets

### 项目模块层级总览

#### 层级说明

| 层级 | 职责 | 目录约定 | DynamicApiController 映射 |
|---|---|---|---|
| **API.Entry** | 应用入口、中间件注册、启动配置 | `JNPF.{Module}.API.Entry` | 无（Entry 不暴露 API） |
| **API.Controller** | 路由定义（动态生成） | `JNPF.{Module}.API` | Service 方法 → 自动映射 |
| **Application.Service** | 业务逻辑编排 | `JNPF.{Module}.Application` | 方法签名决定 API 端点 |
| **Domain** | 领域模型、实体定义 | `JNPF.{Module}.Domain` | 不直接暴露 |
| **Infrastructure** | 数据访问、外部集成 | `JNPF.{Module}.Infrastructure` | 不直接暴露 |

#### 当前启用模块

| 模块 | API.Entry | Application | Domain | Infrastructure | 状态 |
|---|---|---|---|---|---|
| Base（基础） | ✅ | ✅ | ✅ | ✅ | 启用 |
| Message（消息） | ✅ | ✅ | ✅ | ✅ | 启用 |
| WorkFlow（工作流） | ✅ | ✅ | ✅ | ✅ | 启用 |
| DataVisualization（数据大屏） | ✅ | ✅ | ✅ | ✅ | 启用 |
| OA | ❌ | — | — | — | 未启用 |
| **IoT.Device**（规划中） | 待建 | 待建 | 待建 | 待建 | 规划 |
| **MES.Production**（规划中） | 待建 | 待建 | 待建 | 待建 | 规划 |

> **标注：** IoT 和 MES 模块为本项目核心业务模块，需由架构师设计 Module 边界后建立。

Frontends (repo root):

| Directory | Role |
|-----------|------|
| `jnpf-web-vue3` | PC admin UI (Vue 3 + Vite + Ant Design) |
| `jnpf-web-datascreen` | Avue DataV designer |
| `jnpf-app-vue3` | UniApp mobile |

## Key Patterns

- **Dynamic API**: Controllers are auto-generated from `IDynamicApiController` services — reference `*Service` classes in docs, not manual controllers. **禁止手动创建 Controller。**
- **Unified Response**: `RESTfulResult<T>` (`framework/JNPF/UnifyResult/`) auto-wraps all API returns. Exceptions use `Oops.Oh()` (`FriendlyException`), not raw `throw`.
- **Connection strings**: `backend/application/JNPF.API.Entry/Configurations/ConnectionStrings.json` (gitignored; template: `ConnectionStrings.example.json`)
- **Codegen**: Apache Velocity `.vm` templates
- **EventBus**: Channel in-process (`framework/JNPF/EventBus/`) for lightweight decoupling; RabbitMQ (`infrastructure/JNPF.Extras.EventBus.RabbitMQ/`) for durable cross-process messaging
- **Real-time**: SignalR (`framework/JNPF/InstantMessaging/`, `[MapHub]` auto-scan); WebSocket (`infrastructure/JNPF.Extras.WebSockets/`)
- **Multi-tenant**: SqlSugar-level `ITenantFilter` / `TenantOptions` (`framework/JNPF.Extras.DatabaseAccessor.SqlSugar/`)

## Code Analysis

Roslynator + StyleCop enforced via `backend/dotnet.ruleset`, `backend/stylecop.json`, `backend/.editorconfig` (with `root = true`). All projects target `net6.0` with nullable enabled. SDK version locked in `backend/global.json` (`latestPatch`, no prerelease).

### 代码风格（预留，暂不强制执行）

> 本项目计划引入 Roslynator + StyleCop 进行静态分析。当前阶段仅做预留，
> 不强制执行。正式启用日期待架构师通知。
>
> 预计启用条件：
> - 核心模块（IoT/MES）架构稳定
> - 模块层级归属文档完成
> - CI/CD 流水线就绪

## Database

- SqlSugar (SQL Server) + Dapper
- Init SQL: `backend/web/jnpf_sundial_init.sql`
- Table naming: `{MODULE_PREFIX}_{ENTITY}` uppercase (e.g. `BASE_USER`, `EXT_EMPLOYEE`, `FLOW_TASK`)
- New module prefixes: `IOT_` / `MES_` (awaiting modeling review)

## Conventions

- Naming: [`docs/conventions/naming.md`](docs/conventions/naming.md)
- Error response: [`docs/conventions/error-response.md`](docs/conventions/error-response.md) — `RESTfulResult<T>` format, code 600 = JWT expired
- Logging: [`docs/conventions/logging.md`](docs/conventions/logging.md) — Serilog levels, prod ≥ Warning
- Git workflow: [`docs/conventions/git-workflow.md`](docs/conventions/git-workflow.md) — Conventional Commits, branch strategy
- IoT/MES rules: [`.cursor/rules/iot-mes-conventions.mdc`](.cursor/rules/iot-mes-conventions.mdc) — telemetry off SqlSugar, device auth separation

## Architecture Documentation

Follow [`docs/architecture/ARCHITECTURE_DOC_RULES.md`](docs/architecture/ARCHITECTURE_DOC_RULES.md) when writing architecture docs. Five mandatory rules: **penetration** (file path + class + method), **data anchoring** (core tables per module), **diagrams** (Mermaid/ASCII), **verifiable** (searchable in source), **no vagueness** (no generic phrases).

## Agent Toolchain

See [`.cursor/rules/toolchain-division.mdc`](.cursor/rules/toolchain-division.mdc) for full rules.

| Tool | Role | 可执行编码？ | 可执行运维？ | 可执行架构决策？ |
|------|------|---|---|---|
| **superpowers 技能集** | 日常开发、代码生成、调试 | ✅ 是 | ❌ 否 | ❌ 否 |
| **OpenSpec** | 知识库（`openspec/specs/`） | ❌ 否 | ❌ 否 | ❌ 否 |
| **Serena** | C# symbol-level changes in `backend/modularity/` and `backend/framework/` | ✅ 是 | ❌ 否 | ❌ 否 |
| **episodic-memory** | Cross-session WHY context (auto sync + auto search) | ❌ 否 | ❌ 否 | ❌ 否 |

### 明确约束

> **🔴 /opsx:apply 严禁用于编码操作。**
> /opsx:apply 仅用于基础设施和运维操作。使用 /opsx:apply 修改业务代码将导致
> 变更无法追溯、无法 Code Review，属于违规操作。

> **🟢 日常开发必须使用 superpowers 技能集。**
> 所有业务代码的创建、修改、调试必须通过 superpowers 技能集执行。

Episodic project ID: `D--JNPF-v52` (from `.cursor/toolchain.manifest.json`).

## 文档索引

### V5.2 架构文档

| 文档 | 路径 | 最后更新 | 适用版本 | 说明 |
|---|---|---|---|---|
| 整体架构说明 | `docs/architecture/overview.md` | YYYY-MM-DD | ≥ V5.2 | 系统整体架构 |
| 模块设计文档 | `docs/architecture/modules.md` | YYYY-MM-DD | ≥ V5.2 | 各模块详细设计 |
| 工具链设置 | `docs/toolchain/SETUP.md` | YYYY-MM-DD | ≥ V5.2 | 开发环境配置 |
| DynamicApiController 说明 | `docs/patterns/dynamic-api.md` | YYYY-MM-DD | ≥ V5.2 | API 自动生成机制 |
| Demo 手册 | `docs/v52-demo-manual.md` | YYYY-MM-DD | ≥ V5.2 | 演示操作手册 |

### 业务域文档（规划中）

| 文档 | 路径 | 状态 | 说明 |
|---|---|---|---|
| IoT 设备管理 | `docs/domain/iot-device.md` | 待创建 | 智能手环/家居/更衣柜/工地 |
| MES 生产管理 | `docs/domain/mes-production.md` | 待创建 | 企业制造执行系统 |
| 设备协议集成 | `docs/domain/device-protocol.md` | 待创建 | MQTT/CoAP/HTTP 等协议规范 |

> 所有文档必须标注日期和适用版本。AI 引用时应检查版本兼容性。
>
> 日期格式：YYYY-MM-DD。文档首次建立时填写实际日期，后续更新时同步修改。
