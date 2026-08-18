# 业务优先铁律（Business First Iron Law）

> **定位：** JNPF v5.2 项目**宪法级**铁律，凌驾于架构红线 R1–R10、Superpowers 流程、CognitiveSkill/基础设施迭代之上。
> **Cursor 常驻规则：** `.cursor/rules/business-first-iron-law.mdc`（`alwaysApply: true`）
> **生效日期：** 2026-07-05 · **永久生效**

---

## 核心宣言

**一切的架构设计、功能实现、测试、性能优化，都必须围绕具体业务功能和客户操作为目标来实现。**

**一切不以业务功能为目标的工作，都等于是瞎折腾。**

---

## 执行层级

| 层级 | 说明 |
|---|---|
| **宪法级** | 本铁律。定「做什么」——无业务锚定不得开工、不得声称完成。 |
| L0 架构红线 | R1–R10。定「怎么做不出事」。 |
| L1 证据铁律 | Supreme Iron Law / auto-test-fix-loop。定「怎么证明做过」。 |

**三者缺一不可。** 仅有 L0+L1、无用户可感知业务产物 = **未完成**。

---

## 开工前三问（Mandatory Gate）

1. **用户是谁？在界面上做什么操作？**
2. **完成后用户能拿到/看到什么业务产物？**
3. **哪条 E2E/脚本模拟真实用户路径验收？**

答不出任一问 → **停止编码**，向用户确认业务目标。

---

## 有效工作 vs 瞎折腾

| ✅ 有效 | ❌ 瞎折腾 |
|---|---|
| SA 门控 + 附件 annex 全链 E2E | 仅 GatePipeline 单元测试 |
| `02-requirement-spec.md` 供用户确认 | 仅 CognitiveSkill 模具迁移 |
| PM 万字附件 → `01-skeleton.md` | ToT metadata 无用户文档 |
| 业务 HTTP E2E（用户路径） | 纯内存 `cognitive-r0` 后宣称阶段完成 |

---

## Agent 强制行为

- **编码前**：回复中写出 Q1–Q3（业务目标 + 操作路径 + 验收命令）。
- **编码中**：P0 业务缺口优先；基建/重构须标注服务的用户路径。
- **测试**：业务 E2E（`jnpf-api.mjs` / 领域脚本 / Playwright）**不可被**单元测试替代。
- **完成声称**：附业务 E2E exit 0 或 E1/E2/E3 用户路径证据。
- **Infrastructure-only**：标注「基建债」，不得替代业务验收。

---

## AI 原生链业务锚点

| 阶段 | 用户可感知产物 | 验收 |
|---|---|---|
| S0 门控 | 拦截/通过、`00-merged-requirement.md`、附件可下载 | sa-gate E2E |
| S1 PM | `01-skeleton.md`、IR-0 确认 | 长文档 + confirm |
| S2 Analyst | `02-requirement-spec.md`；confirm 后 sa_* 九表（C# SaMaterializer） | **`E2E_PIPELINE_ID=311 pnpm test:api`（首选）** · phase-sup-s2 verify（evidence） |
| S3+ | 详细设计、代码生成 | 19 号计划 |

详见 `docs/AI原生开发/1、多用户多任务并行/19、全链条补充开发详细任务计划.md`。

---

## 关联文档

- `CLAUDE.md` — §Business First Iron Law
- `.cursorrules` — 架构红线第 0 条
- `AGENTS.md` — Business First 摘要
- `.claude/rules/architecture-redlines.md` — 序言 B0
