# Capability: studio-e2e-toolchain

> **状态：** 已落地（2026-07-06）  
> **规则唯一信源：** `.cursor/rules/testing-toolchain.mdc`（`alwaysApply: true`）  
> **详表：** `api-tests/README.md` · `e2e/README.md`

## 概述

2026-07-06 起，Studio/API 业务验收从「**单一慢速 `.mjs` 长链**」改为 **分层工具组合**：

| 层级 | 工具 | 耗时 | 用途 |
|------|------|------|------|
| **L0 编译** | dotnet / pnpm type-check | 秒–分 | 语法 |
| **L1 快 API** | **Vitest** `pnpm test:api` | ~10s | 交付物 / IR / 物化 **结构化断言**（**日常默认**） |
| **L1b 探针** | **REST Client** `.http` | 秒 | 手工调 API、看 Response |
| **L2 长链** | `phase-sup-*.mjs` **分步** | 分–十分 | Skill **watch**、新建 pipeline、**evidence JSON** |
| **L3 UI** | Playwright | 分 | 页面 / 门控 / 阶段截图 |
| **L4 LLM** | promptfoo | 分 | SA Agent 提示词回归 |
| **L5 数据** | sqlcmd / 九表 audit SQL | 秒 | `sa_*` 行数与 JSON 校验 |

**铁律：** 已有 pipeline 的断言 **MUST 先跑 L1 Vitest**；**禁止**用慢速 mjs `verify` 替代快测。

## 决策树（Agent 必查）

```
改 Studio/API/Skill 后需要验收？
├─ 仅断言交付物 / IR / 物化状态（已有 pipelineId）
│  └─ ✅ E2E_PIPELINE_ID=<id> pnpm test:api          （~10s，首选）
├─ 手工探某个 API、看 body
│  └─ ✅ pnpm sync:http-env → api-tests/http/*.http
├─ 需要等 Skill 跑完（gate/pm/analyst 分钟级）或从零 create pipeline
│  └─ ✅ node scripts/phase-sup-s2-e2e.mjs <分步>   （禁止 blind all）
├─ 阶段交付 / guard-finish 需 evidence JSON
│  └─ ✅ phase-sup 分步 + verify → .claude/evidence/
├─ 改前端 UI / 门控文案
│  └─ ✅ pnpm e2e:studio:*
├─ 改 SA Agent 提示词
│  └─ ✅ promptfoo eval
└─ 验九表数据
   └─ ✅ sa-nine-tables-audit.sql @PipelineId
```

## 共享鉴权

所有工具 **MUST** 复用 `scripts/lib/jnpf-auth.mjs`（MD5+AES → `POST /api/oauth/Login`）。

- Vitest：`tests/api/studio-s2.test.mjs` import 同上
- REST Client：`pnpm sync:http-env` → `api-tests/http/http-client.env.json`（gitignore）
- mjs：`phase-sup-api.mjs` 共享业务 API

## 文件布局

| 路径 | 角色 |
|------|------|
| `tests/api/studio-s2.test.mjs` | Vitest 结构化断言 |
| `tests/api/vitest.config.mjs` | Vitest 配置 |
| `api-tests/http/studio-s2-chain.http` | REST Client 快测 |
| `scripts/sync-http-env.mjs` | Token → `.http` 环境 |
| `scripts/lib/jnpf-auth.mjs` | 唯一登录协议 |
| `scripts/lib/phase-sup-api.mjs` | Vitest + mjs 共享 |
| `scripts/phase-sup-s2-e2e.mjs` | 长链分步 + evidence |

根 `package.json`：`pnpm test:api` · `pnpm test:api:watch` · `pnpm sync:http-env`

## 日常 Dev Loop（强制顺序）

```powershell
dotnet build
node scripts/jnpf-api.mjs GET /api/oauth/CurrentUser
$env:E2E_PIPELINE_ID="311"
pnpm test:api                    # L1 快断言 — 优先
# 仅当 L1 不足：phase-sup 分步 / Playwright / promptfoo
```

## mjs 保留场景（不可删）

| 场景 | 原因 |
|------|------|
| Skill 分钟级 watch | Vitest 无内置 heartbeat 轮询 |
| 从零 create → gate → pm → analyst | 需分步状态机 |
| `.claude/evidence/*.json` 阶段交付 | 22 号五步计划 evidence 路径 |

## 禁止

- ❌ 为同一断言新建 `.mjs`（用 Vitest 或 `.http`）
- ❌ 日常 Dev Loop **仅**跑 `phase-sup-s2-e2e.mjs verify` 而跳过 `pnpm test:api`
- ❌ `node scripts/phase2-skills-e2e.mjs` / `scripts/studio-e2e.mjs` 全链
- ❌ `.http` 替代 Skill 等待（无轮询）
- ❌ 各工具手写登录（非 `jnpf-auth.mjs`）

## 业务验收示例（S2 · pipeline 311）

```powershell
E2E_PIPELINE_ID=311 pnpm test:api
node scripts/phase-sup-s2-e2e.mjs verify --pipeline-id 311   # evidence 可选
```

## 本节关键路径索引

- `.cursor/rules/testing-toolchain.mdc`
- `.cursor/rules/auto-test-fix-loop.mdc`
- `api-tests/README.md`
- `openspec/specs/studio-s2-compile/spec.md`（S2 业务锚点）
