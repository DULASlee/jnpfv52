# E2E — 按页测、按需跑

**原则：** 测哪个页面就只跑那个 spec，不跑全链、不等 Analyst（除非你自己加）。

前置：`start-dev.ps1`（:3100 + :5000）

## Playwright（页面 + 控件）

| 命令 | 测什么 | 大约耗时 |
|------|--------|----------|
| `pnpm e2e:login` | 仅登录 | ~15s |
| `pnpm e2e:studio:layout` | 提交需求页三栏/输入框/发送钮 | ~30s |
| `pnpm e2e:studio:gate` | 发送需求 → 等「需求材料评估通过」 | ~2–4min |
| `pnpm e2e:studio:deliverables` | **已有** pipeline 的 S0 交付物按钮 | ~1min |

交付物检测需要已有 pipeline（跑过门控）：

```powershell
$env:E2E_PIPELINE_ID="294"
pnpm e2e:studio:deliverables
```

指定需求文案：

```powershell
$env:E2E_REQUIREMENT="你的需求…"
pnpm e2e:studio:gate
```

失败截图：Playwright 自动；报告：`.claude/evidence/playwright-report.json`

## dotnet API 快测（单接口，秒级）

```powershell
# MCP 网关是否 200
dotnet test backend/tests/JNPF.Tests.Gate --filter "FullyQualifiedName~McpTools"

# 某 pipeline 的 deliverables 列表
$env:E2E_PIPELINE_ID="294"
dotnet test backend/tests/JNPF.Tests.Gate --filter "FullyQualifiedName~Deliverables_List"
```

## 不要用的

- `scripts/studio-e2e.mjs` 全链编排（已废弃，耗时长且硬编码）
- `POST /api/studio/ir/{id}/simulate` 产品验收禁用
- **为每个生成 API 写 curl 清单脚本** — 见 22 号文档 §11 组合调试

## 组合调试（生成代码 / 不可枚举 API）— 2026 静默脚本方案

**原则：全 headless 静默跑，几十秒反馈；DevTools 仅开发对照，不进 CI。**

| 层 | 工具 | 用途 |
|----|------|------|
| 静默 UI | Playwright headless + POM | 点控件，不等人手 |
| 自动 Network | `page.on('response')` + glob `**/api/**` | **脚本内**抓全部 API，写 evidence JSON |
| SSE | HAR mock 或 `page.evaluate` EventSource hook | 不用 `response.body()`（SSE 乱码 bug） |
| HAR/VCR | `routeFromHAR` / test-proxy-recorder | 录一次 → CI 离线回放 ~几十秒 |
| 数据 | sqlcmd | 验表，真业务完成 |
| 契约 | playswag + `/newapi` | 生成 API 覆盖率，零 hardcode |

```powershell
# 快路径：HAR 回放（日常 Dev Loop，~几十秒）
RECORD_HAR=0 pnpm e2e:studio:silent-gate   # 待建

# 录 HAR（预发/本地一次）
RECORD_HAR=1 pnpm e2e:studio:silent-gate

# sqlcmd 锚定
sqlcmd ... -Q "SELECT F_FileName FROM inte_assistant_deliverable WHERE F_PipelineId='297'"
```

详见 **22 号文档 §11**（含 Doksi/TestSprite/PlayCapture 等业界调研）。

平台主干 `phase-sup-*.mjs` 仅 Dev Loop；**生成模块不走 API 清单**。

## API 快测（Vitest + REST Client）— 2026 新增

替代「为每个断言手写 `.mjs`」：

| 场景 | 工具 | 命令 |
|------|------|------|
| 结构化断言（交付物/IR） | Vitest | `E2E_PIPELINE_ID=311 pnpm test:api` |
| 手工点 API、看 Response | REST Client | `pnpm sync:http-env` → `api-tests/http/studio-s2-chain.http` |

详见 **`api-tests/README.md`**（分层对照表 + 与 promptfoo/Playwright/dotnet 的分工）。
