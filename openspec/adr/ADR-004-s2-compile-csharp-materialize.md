# ADR-004：S2 compile 模式 + C# 直连九表物化

| 字段 | 内容 |
|------|------|
| 状态 | **已接受** |
| 日期 | 2026-07-06 |
| 决策者 | 架构师 + AI 原生链施工 |
| 关联 | `22、Skills全链路装配审计与五步推进计划.md` 第 1 步 · `4、S2预分析与SA九视图编译器施工包.md` |

## 背景

S2 原实现（agent 模式）经 `AnalystSkillService` → `sa-service` `POST /api/sa/run-async` → `SAOrchestrator.runSA` 全 LLM 九步，单次 ~9min，且在 S2 期间经 sa-service 写 `sa_*` 九表，与用户确认脱节。

2026-07-06 在 pipeline **311** 完成业务验收，确立 **compile 为主链、物化由 C# 直连主库** 的新架构。

## 决策

### 1. S2 双模式，默认 compile

| 模式 | 配置 | Analyst 路径 | 写 `sa_*` 时机 |
|------|------|--------------|----------------|
| **compile**（默认） | `SaPipeline.json` → `S2Mode: "compile"` | `SaNineViewCompiler.CompileFromSkeletonJson` | **仅**用户 `confirm-requirement-spec` 后 |
| **agent**（回归对比） | `S2Mode: "agent"` | sa-service LLM 九步 | 仍走 sa-service（**禁止**用于生产主链） |

### 2. 编译与物化职责分离

```
Skills（PM + Analyst）→ 语义 / 双审 / 02 文档
SaNineViewCompiler   → 确定性九视图 bundle → IR `SaNineViewCompiled`
用户 confirm S2      → SaMaterializationService → SaMaterializer → sa_* 九表
                     → IR `SaMaterializationCompleted` | `SaMaterializationFailed`
```

- **compile + confirm 物化不依赖 sa-service :3001**
- sa-service 仅 **agent 模式** LLM 九步回归与 Vitest 单测

### 3. 物化实现：C# `SaMaterializer` 直连 JNPF 主库

施工包曾建议 `POST /api/sa/materialize`；**落地选型为 C#**：

- `ISaMaterializer.MaterializeAsync` — SqlSugar + `Microsoft.Data.SqlClient` 批量 INSERT
- 生产库为**完整九表 schema**（`context_diagram`、`swim_lanes` 等列），非简化 migration 的 `payload_json`
- 事务内 `DISABLE/ENABLE` 九表版本触发器；`created_by`/`updated_by` = `jnpf-materialize`

**禁止**：sa-service 写 JNPF 业务库做物化（SQLEXPRESS 连库失败曾导致 HTTP 500）。

### 4. 业务验收锚点（pipeline 311）

| 项 | 标准 |
|----|------|
| 交付物 | `00` / `01` / `02` |
| IR | `AnalysisCompleted` |
| 物化 | `SaMaterializationCompleted`（scopeId/dictId/eventCount） |
| 证据 | `.claude/evidence/phase-sup-s2-e2e.json` pass=true |
| 快断言 | `E2E_PIPELINE_ID=311 pnpm test:api` |
| 九表审计 | `Migrations/scripts/sa-nine-tables-audit.sql` @PipelineId=311 |

## 后果

### 正面

- Analyst compile  wall-clock **&lt;60s**（对比 agent ~9min）
- S2 期间 **零** `sa_* 写入；语义与用户确认绑定
- Dev Loop 无需 sa-service 即可跑 S0→S2 + 物化
- 物化失败可重试 confirm，IR 留 `SaMaterializationFailed` 审计

### 负面 / 待办

- `phase-sup-s2-e2e.mjs` 尚未纳入 `materialize-wait` 标准步骤
- 22 号文档 / 部分脚本注释仍写「sa-service P0」— 须按模式区分
- agent 模式与 C# compiler golden 对齐需持续 Vitest + dotnet test

## 相关文件

| 模块 | 路径 |
|------|------|
| 配置 | `application/JNPF.API.Entry/Configurations/SaPipeline.json` |
| 选项 | `Sa/SaPipelineOptions.cs` |
| 编译器 | `Sa/SaNineViewCompiler.cs` |
| 物化 | `Sa/SaMaterializer.cs` · `Sa/SaMaterializationService.cs` |
| Analyst | `Skills/AnalystSkillService.cs` |
| 确认 API | `Skills/SkillsApiService.ConfirmRequirementSpecAsync` |
| OpenSpec | `openspec/specs/studio-s2-compile/spec.md` |
| 架构详述 | `docs/architecture/studio-s2-compile-materialize.md` |
