# 22、Skills 全链路装配审计与五步推进计划

> ⚠️ **R12 三元组适配声明（2026-07-07 追加）：** 本推进计划涉及的所有 Skill / IR / 物化层 MUST 携带三元组 `(tenantId, projectId, pipelineId)`。第 1 步 pipeline 311 的 greenfield 自锚定（projectId = pipelineId）为 WorkMode 默认值，**不代表 projectId 维度可省略**。详见 `.cursor/rules/triple-key-iron-law.mdc`。

> 状态：审计定稿 · **第 1 步 S0→S2 已落地（2026-07-06，pipeline 311）** · 创建：2026-07-05  
> 上游：`19、全链条补充开发详细任务计划.md` · `20、skills的初稿.md` · `21、CognitiveSkill统一模具施工包.md`  
> 用途：保存 2026-07-05 全链路 Skills 装配审计结论，作为后续会话的**唯一上下文锚点**；五步推进按业务可验收顺序执行。

---

## 1. 业务锚点（开工前三问）

| # | 问题 | 本链答案 |
|---|------|----------|
| Q1 | 用户做什么操作？ | 提交需求页对话 → 门控 → 确认各阶段 → 下载说明书与源码包 |
| Q2 | 完成后拿到什么？ | `00`~`08` deliverables + IR 事件 + 沙箱预览链接 + 源码 ZIP |
| Q3 | 哪条 E2E 验收？ | 见 §6 各步验收命令（禁止仅用 `dotnet build` 声称完成） |

---

## 2. 全链路环节与装配总览

| 阶段 | 环节 | Skill/组件 ID | 创建 | 装配 | 缺口 |
|------|------|---------------|------|------|------|
| S0 | SA 门控 | `GatePipeline` + `RequirementGateService` | ✅ | ✅ | 🟡 非 Skill 形态 |
| S1 | 产品骨架 | `pm-skill` | ✅ | ✅ | 🟢 |
| S2 | 需求九步 | `analyst-skill` + `SaNineViewCompiler` | ✅ | ✅ | 🟢 compile 主链不依赖 sa-service；agent 模式回归才需 :3001 |
| S3 | 架构设计 | `architect-skill` | ✅ | ✅ | 🟢 |
| S4 | DB/UI/总体 | `db-design-skill` / `ui-design-skill` / `system-design-skill` | ✅ | ✅ | 🟢 |
| S5 | 代码生成 | `developer-skill` | ✅ | ✅ | 🟡 交付物索引待补 |
| S5 | 测试生成 | `tester-skill` | ✅ | ✅ | 🟡 无 LLM MVP |
| S6 | 部署交付 | `deploy-skill` | ✅ | ✅ | ✅ `DeploySkillService` + `PipelineDeliveryCoordinator` + `StageConfirmSkillTrigger.ScheduleDelivery` |
| 旁路 | Bug 修复 | `bugfix-skill` | ✅ | ✅ | 不在 happy path |

**注册机制**：`IBaseSkill + ITransient` 由 JNPF DI 自动扫描；`SkillRegistry.GetServices<IBaseSkill>()` 聚合。

---

## 3. 逐步骤详细审计

### 3.1 S0 — 用户提交 + SA 门控

| 维度 | 状态 | 代码路径 |
|------|------|----------|
| 组件 | ✅ | `Gates/GatePipeline.cs` · `Gates/RequirementGateService.cs` |
| API | ✅ | `POST /api/studio/pipeline/execute/{id}/sa-gate`（`AIDevelopmentPipelineService.ExecuteGateAsync`） |
| 提示词 | ✅ | `SemanticFitnessValidator.BuildSystemPrompt` |
| 知识库 | 🟡 | `kg.search-seeds` → `BASE_AI_SEED_TEMPLATE` |
| LLM | ✅ | `ILlmGatewayService` |
| 产出物 | ✅ | `00-gate-report.json` · `00-merged-requirement.md` |
| 落库 | ✅ | `PipelineDeliverableService.SaveGateDeliverablesAsync` → `INTE_ASSISTANT_DELIVERABLE` |

**缺口**：门控与 PM 之间的「多轮对话」= SSE 聊天 + 门控评估，非独立 Skill；结构化骨架须触发 `pm-skill`。

### 3.2 S1 — `pm-skill`（产品 Skill）

| 维度 | 状态 | 说明 |
|------|------|------|
| 类 | ✅ | `Skills/PmSkillService.cs` — `CognitiveSkill` R1 |
| 提示词 | ✅ | 内嵌 IR-0 JSON system prompt |
| MCP | ✅ | `kg.search-seeds` · `kg.score-candidate` ToT Top-1 |
| LLM | ✅ | `TreeSearchAsync` 3 分支；Budget `MaxLlmCalls=3` |
| IR 事件 | ✅ | `SkeletonCreated` → `IR0_Skeleton` |
| 产出校验 | ✅ | `ValidateOutputAsync` 必须有 `businessEvents` |
| 交付物 | ✅ | `01-skeleton.md`（`SkillDeliverableCoordinator`） |
| API | ✅ | `POST .../skills/pm/{id}/run` · `confirm-skeleton` |
| 前端 | ✅ | `usePmSkill.ts` |

门控通过可自动触发 PM（`GatePipelineOptions.AutoRunPmSkillOnGatePass`）。

### 3.3 S2 — `analyst-skill`（compile 主链 + 需求分析说明书）

> **架构变更（2026-07-06）：** ADR-004 · `openspec/specs/studio-s2-compile/spec.md` · `docs/architecture/studio-s2-compile-materialize.md`

| 维度 | compile 模式（**默认**） | agent 模式（回归对比） |
|------|------------------------|------------------------|
| 配置 | `SaPipeline.json` → `S2Mode: "compile"` | `S2Mode: "agent"` |
| 九步来源 | `SaNineViewCompiler.CompileFromSkeletonJson`（纯 C#） | sa-service `SAOrchestrator.runSA` LLM 九步 |
| 类 | ✅ `AnalystSkillService.cs` | 同左 |
| LLM | ❌ Compiler 无 LLM | 🟡 LLM 在 sa-service |
| S2 写 `sa_*` | **禁止** | legacy 同步写库（禁止主链） |
| 物化 | 用户 `confirm-requirement-spec` → `SaMaterializer`（C# 直连主库） | 不适用主链 |
| IR 事件 | `SaNineViewCompiled` · `AnalysisCompleted` · `SaMaterializationCompleted` | 同左（物化仍走 C#） |
| 交付物 | ✅ `02-requirement-spec.md` | 同左 |
| 需要 sa-service | **否** | 是 |

**第 1 步验收（pipeline 311）：** deliverables 00–02 · `AnalysisCompleted` · `SaMaterializationCompleted` · `phase-sup-s2-e2e.json` pass=true。

### 3.4 S3 — `architect-skill`

| 维度 | 状态 |
|------|------|
| 类/LLM/事件/交付物 | ✅ `ArchitectureDecisionRecorded` → `03-architecture.md` |
| 触发 | 需求阶段确认 → `StageConfirmSkillTrigger` 调度 |

### 3.5 S4 — 设计四 Skill

| Skill | LLM | 事件 | 交付物 |
|-------|-----|------|--------|
| `db-design-skill` | ✅ | `DDLStabilized` | `05-ddl.sql` |
| `ui-design-skill` | ✅ | `UIDesignStabilized` | `06-formpage-ir.json` |
| `system-design-skill` | ❌ 约束引擎 | `SystemDesignLocked` | `04-system-design.md` |
| 编排 | `DesignSkillOrchestrator` 3 并行 + 1 串行 | | |

API：`DesignSkillsApiService` · 前端 `useDesignSkills.ts` ✅

### 3.6 S5 — `developer-skill` + `tester-skill`

| 维度 | developer | tester |
|------|-----------|--------|
| LLM | ❌ `.vm` 模板 | ❌ 确定性推导 |
| 编排 | `DeveloperSkillOrchestrator`：codegen → sandbox → arch-guard → tester | |
| 交付物落盘 | 🟡→✅ `07-codegen-manifest.json` | 🟡→✅ `08-testsuite.json` |
| 前端 | 🟡→✅ `useDeveloperSkill.ts` | |

### 3.7 S6 — 部署交付

| 维度 | 原状 | 本计划 |
|------|------|--------|
| `deploy-skill` | ❌ 仅常量 | ✅ `DeploySkillService` |
| 协调器 | `PipelineDeliveryCoordinator` | 收编进 deploy-skill |
| IR 事件 | 无写入 | `DeploymentVerified` / `DeploymentFailed` |
| 预览凭据 | 未硬编码 | 壳工程 seed `admin/admin123`（配置项） |
| 触发 | 开发阶段确认 | `StageConfirmSkillTrigger` → `deploy-skill` |

---

## 4. 横向基础设施

| 能力 | 路径 |
|------|------|
| 调度.harness | `Skills/SkillHarness.cs` |
| LLM 网关 | `LlmGatewayService` + `SkillLlmBudgetGuard` |
| MCP | `Configurations/McpTools.json` |
| IR 事件库 | `Ir/IrEventStoreService.cs` |
| 经验回流 | `Skills/Cognitive/IExperienceRecorder.cs` |
| 阶段确认编排 | `Pipeline/StageConfirmSkillTrigger.cs` |
| 交付物服务 | `Studio/PipelineDeliverableService.cs` · `Studio/SkillDeliverableCoordinator.cs` |

---

## 5. 装配成熟度六维矩阵

| Skill | 提示词 | 知识库/MCP | 事件库 | LLM | 产出校验 | 交付物落库 |
|-------|--------|-----------|--------|-----|----------|-----------|
| S0 门控 | ✅ | 🟡 | 🟡 | ✅ | ✅ | ✅ 00 |
| pm-skill | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ 01 |
| analyst-skill | 🟡 | ✅ | ✅ | 🟡 | ✅ | ✅ 02 |
| architect-skill | ✅ | 🟡 | ✅ | ✅ | ✅ | ✅ 03 |
| db/ui-design | ✅ | 🟡 | ✅ | ✅ | ✅ | ✅ 05/06 |
| system-design | ❌ | ✅ | ✅ | ❌ | ✅ | ✅ 04 |
| developer-skill | ❌ | ❌ | ✅ | ❌ | ✅ | ✅ 07 |
| tester-skill | ❌ | ❌ | ✅ | ❌ | ✅ | ✅ 08 |
| deploy-skill | ❌ | ❌ | ✅ | ❌ | ✅ | ✅ 09 |

---

## 6. 五步业务可验收推进计划

### 第 1 步 — S0→S2 需求链闭环 ✅（2026-07-06 业务通过，pipeline 311）

**用户路径**：创建 pipeline → SA 门控 → PM 骨架 → 确认骨架 → Analyst **compile** → 下载 `02-requirement-spec.md` → **确认需求分析说明书** → 九表物化

**验收命令**：

```powershell
# 前置：start-dev.ps1（:5000 API）；compile 模式不需要 sa-service

# ① 日常快断言（~10s — 首选，禁止仅用慢速 mjs）
E2E_PIPELINE_ID=311 pnpm test:api

# ② 阶段 evidence / 长链（按需）
node scripts/phase-sup-s2-e2e.mjs verify --pipeline-id 311

# ③ 九表审计
# Migrations/scripts/sa-nine-tables-audit.sql @PipelineId=311
```

**工具选型：** `openspec/specs/studio-e2e-toolchain/spec.md` · `.cursor/rules/testing-toolchain.mdc`

**通过标准**：deliverables 含 `00`/`01`/`02`；IR 含 `AnalysisCompleted` + `SaMaterializationCompleted`；证据 `.claude/evidence/phase-sup-s2-e2e.json`。

**收口待办**：`materialize-wait` 纳入 `phase-sup-s2-e2e.mjs` 标准步骤（施工包 §9.3）。

---

### 第 2 步 — S3→S4 设计四 Skill 闭环

**用户路径**：需求阶段确认 → architect → 架构确认 → db/ui/system-design → deliverables `03`~`06`

**验收命令**（2026-07 工具链 — **禁止**默认跑 `phase-sup-s34-e2e.mjs`）：

```powershell
# ① 快断言（日常 ~10s，已有 pipeline 且设计 Skill 已跑完）
E2E_PIPELINE_ID=311 pnpm test:api

# ② 手工驱动（REST Client 点 stage confirm / 看 runs）
pnpm sync:http-env
# → api-tests/http/studio-s34-chain.http

# ③ 自动驱动（分钟级 LLM，Rare — Vitest 非 mjs）
E2E_PIPELINE_ID=311 E2E_DRIVE_S34=1 pnpm test:api
```

**通过标准**：IR-2 四片段 stable/locked；deliverables 含 `03-architecture.md` ~ `06-formpage-ir.json`。

**废弃**：`node scripts/phase-sup-s34-e2e.mjs`（exit 1，仅提示迁移路径）

---

### 第 3 步 — S5 开发测试链

**用户路径**：设计阶段确认 → developer orchestrator → codegen stable → tester → IR-3

**验收命令**：

```powershell
node scripts/phase4-green-path.mjs
```

**通过标准**：`CodeGeneratedStablePromoted` + `TestSuiteGenerated`；workspace 有生成物。

**前端**：`useDeveloperSkill.ts` + `developerSkills.ts` 接入提交需求页。

---

### 第 4 步 — DeploySkill + 全链 E2E

**交付**：`DeploySkillService` · `DeploySkillsApiService` · IR `DeploymentVerified`

**验收命令**：

```powershell
node scripts/phase5-fullchain-e2e.mjs
```

**通过标准**：previewUrl + downloadUrl 非空；`DeploymentVerified` 事件存在。

---

### 第 5 步 — 交付物索引补齐

**范围**：developer/tester/deploy 完成后写入 `07-codegen-manifest.json`、`08-testsuite.json`、`09-deployment-report.json` 至 deliverables 索引。

**验收**：各步 E2E 脚本内 `assertDeliverables()` 覆盖 07~09。

---

## 7. 关键代码路径索引

| 模块 | 路径 |
|------|------|
| Skill 注册 | `Skills/SkillRegistry.cs` |
| Skill 调度 | `Skills/SkillHarness.cs` |
| 交付物协调 | `Studio/SkillDeliverableCoordinator.cs` |
| 门控 API | `AIDevelopmentPipelineService.ExecuteGateAsync` |
| 阶段确认 | `POST .../stage/{pipelineId}/confirm` |
| MCP 配置 | `application/JNPF.API.Entry/Configurations/McpTools.json` |
| 前端提交页 | `jnpf-web-vue3/src/views/studio/views/ai/submit-requirement.vue` |

---

## 8. 本节核心表清单

- **BASE_AI_PIPELINE** — 流水线主表
- **BASE_AI_IR_EVENT** — IR 事件溯源
- **INTE_ASSISTANT_DELIVERABLE** — 交付物索引
- **BASE_AI_GENERATED_PROJECT** — 已生成系统（previewUrl / sourceZipUrl）
- **BASE_AI_SKILL_RUN** — Skill 运行记录
- **BASE_AI_CALL_LOG** — LLM 审计

---

## 9. 待办与阻塞（2026-07-07 更新）

| 优先级 | 项 | 负责步骤 |
|--------|-----|----------|
| ~~P0~~ | ~~`sa-service :3001` 常驻~~ | **已降级**：compile 主链不需要；仅 agent 回归 / promptfoo |
| ~~P0~~ | ~~**第 2 步** — Vitest studio-s34 + architect 03 落盘~~ | ✅ 已完成（pipeline 311 验证） |
| ~~P0~~ | ~~**第 4 步** — DeploySkill + 全链 E2E~~ | ✅ 已完成（2026-07-07，pipeline 311 DeploymentVerified） |
| ~~P2~~ | ~~预览凭据 admin/admin123 写入壳工程 seed~~ | ✅ 已完成（`DeploySkillService` defaultCredentials） |
| P1 | `materialize-wait` 纳入 S2 标准 E2E | 第 1 步收口 |
| P1 | `phase5-fullchain-e2e.mjs` 全链运行验证（脚本已实现，需在完整 LLM 链路下跑一次） | 第 4 步 |

### 9.1 E2E 脚本索引（本次施工新增）

| 步骤 | 脚本 | 证据 |
|------|------|------|
| 1 | `node scripts/phase-sup-s2-e2e.mjs [--skip-analyst]` | `.claude/evidence/phase-sup-s2-e2e.json` |
| 2 | **`E2E_PIPELINE_ID=<id> pnpm test:api`**（`studio-s34.test.mjs`）+ `studio-s34-chain.http` | Vitest 报告 |
| 3 | `node scripts/phase4-green-path.mjs` | `.claude/evidence/phase4-d14-green-path.json` |
| 4+5 | `node scripts/phase5-fullchain-e2e.mjs [--fast]` | `.claude/evidence/phase5-fullchain-e2e.json` |

---

---

## 11. E2E 验收铁律 — 全脚本静默 + 组合锚定（2026 业界调研结论）

> **用户约束：** ① 禁止为每个生成 API 写死 URL 清单；② **必须全脚本 headless 静默跑**，几十秒级反馈，不靠手点 DevTools。  
> DevTools 仅作开发期对照；**CI/Agent 闭环 = Playwright 静默脚本自动抓 Network/SSE + sqlcmd 验表**。

### 11.1 2026 业界分层（调研摘要）

| 层级 | 代表方案 | 核心手段 | 是否写死 API | 典型耗时 |
|------|----------|----------|--------------|----------|
| **L0 开源基座** | [Playwright](https://playwright.dev/docs/network) | `headless` + `page.on('response')` + `waitForResponse('**/api/**')` + Trace | ❌ glob/_predicate | 布局 ~30s |
| **L0 HAR/VCR** | [routeFromHAR](https://playwright.dev/docs/mock) · [test-proxy-recorder](https://github.com/asmyshlyaev177/test-proxy-recorder) | 录一次真流量 → CI 离线回放（含 SSR/WS） | ❌ 录什么回放什么 | **~几十秒** |
| **L0 API 发现** | [PlayCapture](https://github.com/BankkRoll/PlayCapture) · [TabAPI](https://github.com/Lay4U/tabapi) | 点页面 → 从流量推断 OpenAPI/MCP | ❌ 自动聚类 endpoint | 交互录制 |
| **L0 契约覆盖** | [playswag](https://github.com/MichalFidor/playswag) | 测试流量 vs `/newapi` OpenAPI 覆盖率 | ❌ 对照 spec | 随用例 |
| **L1 商业 AI** | Doksi · TestSprite · Thunders · testRigor | 自然语言用例 + 静默抓 network/log/state + 自愈 selector | ❌ 意图驱动 | 云沙箱分钟级 |
| **L2 数据锚定** | **sqlcmd** | 验业务表行，不看 HTTP 200 | N/A | 秒级 |

**JNPF 选型结论：** L0 Playwright + HAR/VCR + 全流量 NetworkTap + sqlcmd；L0 playswag 对 Studio `/newapi`；**不引入** per-API 硬编码脚本。

### 11.2 静默脚本标准形态（替代手点 DevTools）

```
pnpm e2e:studio:silent-smoke   # headless, ~30–90s（HAR 回放模式）
  ├─ beforeEach: attach NetworkTap（抓 **/api/** 全部 XHR/Fetch）
  ├─ 页面控件：POM 点发送/确认（与现 spec 一致）
  ├─ SSE：page.evaluate 注入 EventSource 监听器 → 回传 event 名（禁止 response.body 读 SSE，Playwright 已知乱码 bug）
  ├─ 断言：UI web-first expect + NetworkTap 导出 JSON + sqlcmd 查表
  └─ afterEach: 写 .claude/evidence/network-{runId}.json + playwright-report.json
```

**SSE 最佳实践（2026）：**
- **CI 快路径：** `page.route()` mock `text/event-stream` 或 HAR 回放 → 确定性、几十秒 [Playwright mock 文档](https://playwright.dev/docs/mock)
- **集成路径：** 浏览器内 `EventSource` 监听器经 `page.exposeFunction` 回传 event（长 LLM 流不可用 `route.fetch()` 阻塞）
- **禁止：** 对 SSE 调 `response.body()`（[Playwright #39812](https://github.com/microsoft/playwright/issues/39812) 乱码）

**不写死 API 的关键：**
```typescript
// ✅ glob — 抓所有 API，事后从 JSON 里查，不维护 URL 列表
page.on('response', async (res) => {
  if (!res.url().includes('/api/')) return;
  tap.push({ method: res.request().method(), url: redactToken(res.url()), status: res.status(), body: await safeJson(res) });
});
await page.waitForResponse(r => r.url().includes('/api/') && r.status() === 200); // 等「任意」关键响应

// ✅ HAR — 录一次真跑，CI 回放
await page.routeFromHAR('e2e/recordings/studio-gate.har', { url: '**/api/**', update: !!process.env.RECORD_HAR });
```

### 11.3 四层组合（静默版）

| 层 | 工具 | 验什么 |
|----|------|--------|
| **L1 静默 UI** | Playwright headless + POM | 控件事件、门控文案、交付物按钮 |
| **L1b 自动 Network** | NetworkTap fixture（非 DevTools） | 全部 `/api/**` URL/status/body 落盘 |
| **L1c SSE** | `page.evaluate` EventSource hook 或 HAR mock | gate_passed / skill_progress 事件 |
| **L2 数据** | sqlcmd | `inte_assistant_deliverable` / `ai_ir_event` 有预期行 |
| **L3 临时 curl** | 从 NetworkTap JSON **复制** URL，人工/debug 一次性复现 |
| **L4 契约** | playswag + `/newapi` | 生成模块 API 是否被 E2E 触达 |

### 11.4 脚本定位（修正）

| 脚本/命令 | 定位 | 扩展策略 |
|-----------|------|----------|
| `pnpm e2e:studio:layout` | 控件 smoke ~30s | 新页面加 POM，不加 API 清单 |
| `pnpm e2e:studio:gate` | 真 LLM 门控 ~2–4min | 预发/夜间；日常用 HAR 回放版 |
| `pnpm e2e:studio:silent-*`（待建） | **HAR 回放 + NetworkTap**，几十秒 | 录一次 `RECORD_HAR=1` 更新 har |
| `phase-sup-*.mjs` | Studio 主干 HTTP smoke（Dev Loop） | 仅 Studio 契约变更时改 |
| **生成代码** | PlayCapture/TabAPI 录流量 → OpenAPI → playswag 覆盖 | **零 hardcode** |

### 11.5 sqlcmd 锚定（不变）

```powershell
sqlcmd -S "(local)\SQLEXPRESS" -U sa -P "***" -d ZXAF_V1_DevTest1 -Q ^
  "SELECT F_FileName FROM inte_assistant_deliverable WHERE F_PipelineId=@id AND F_DeleteMark=0"
```

### 11.6 生成代码验收模板（可复用，零 API 清单）

```
Playwright silent spec（headless）
  → POM 点页面控件
  → NetworkTap 自动记录全部 /api/**
  → SSE hook 记录 event 类型
  → sqlcmd 断言业务表
  → evidence: network.json + screenshot + sql 输出
（可选）playswag 报告 OpenAPI 覆盖率
```

### 11.7 待落地施工项（下一迭代）

1. `e2e/helpers/networkTap.ts` — 全 API 静默采集 + token 脱敏 + evidence 落盘  
2. `e2e/helpers/sseTap.ts` — EventSource inject（绕 Playwright SSE body bug）  
3. `e2e/studio/05-silent-gate-har.spec.ts` — HAR 回放模式，目标 **<90s**  
4. `scripts/sqlcmd-verify.mjs` — 读 `E2E_PIPELINE_ID` 查 deliverable/IR 表  
5. 评估引入 `test-proxy-recorder` 或纯 `routeFromHAR` 做 Studio VCR  
6. 评估 `playswag` 对接 `http://localhost:5000/newapi` 做生成模块契约覆盖  

---

## 12. 变更记录（续）

| 2026-07-05 | DDL 已执行：`inte_assistant_deliverable` + `IX_deliverable_pipeline`（库 ZXAF_V1_DevTest1） |
| 2026-07-05 | §11 组合式 E2E 铁律：curl/sqlcmd/页面/浏览器，禁止生成 API 清单脚本 |
| 2026-07-06 | **ADR-004** S2 compile + C# `SaMaterializer` 物化；第 1 步 pipeline 311 通过；sa-service 从 P0 降为 agent 回归专用 |
| 2026-07-06 | 项目规则：R11 · `.cursor/rules/studio-s2-compile.mdc` · AGENTS/CLAUDE 同步 |
| 2026-07-06 | **E2E 分层：** `openspec/specs/studio-e2e-toolchain/spec.md` · Vitest `pnpm test:api` 日常默认 · mjs 降为 watch/evidence |

---

## 13. 五步推进现状（2026-07-07 更新）

| 步 | 状态 | 锚点 |
|----|------|------|
| 1 S0→S2 | ✅ 业务通过 | pipeline **311** · `phase-sup-s2-e2e.json` |
| 2 S3→S4 | ✅ 业务通过 | Vitest S34 + pipeline **311** 03~06 |
| 3 S5 | ✅ 业务通过 | pipeline **311** · `phase4-d14-green-path.json` · Vitest `studio-s5.test.mjs` |
| 4 Deploy+全链 | ✅ 代码+数据通过 | `DeploySkillService` + `DeploySkillsApiService` + `PipelineDeliveryCoordinator` + `phase5-fullchain-e2e.mjs` 全部实现；pipeline 311 IR 含 `DeploymentVerified`（2 次）；`09-deployment-report.md` 已落盘 |
| 5 07~09 | ✅ 代码+数据通过 | `SkillDeliverableCoordinator` switch 全覆盖 developer/tester/deploy；pipeline 311 deliverables 含 `07-codegen-manifest.json` · `08-testsuite.json` · `09-deployment-report.md` |

**2026-07-07 排查结论：** 五步推进计划全部实现。pipeline 311 数据库验证：
- deliverables 表：`00`~`09` 全部 12 个文件齐全
- IR 事件：`DeploymentVerified`(2) · `DeploymentFailed`(1) · `CodeGeneratedStablePromoted`(3) · `TestSuiteGenerated`(3) · `SystemDesignLocked`
- `StageConfirmSkillTrigger.ScheduleDelivery` 自动调度 deploy-skill（第 244-288 行）
- 修复：`DeploySkillsApiService.ResolveProjectAsync` 三元组 projectId 解析（原返回 pipelineId，与 IrObservabilityApiService P1-A 同类 bug）
