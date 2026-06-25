# 14 号 · Sprint 0 前置阶段（已完成 · ARCH-004）

> **状态**：✅ Sprint 0-A / 0-B **已完成**（2026-06-13 前）  
> **验收记录**：[`docs/phase-1-retrospective.md`](../../../phase-1-retrospective.md) · [`docs/progress-registry.yaml`](../../../progress-registry.yaml)  
> **禁止**：作为当前施工入口；工程师从 [`36、全栈开发总计划`](../36、V7.0 AI原生低代码平台全栈开发总计划.md) 看**下一步**。

---

## 一、本阶段是什么

**Sprint 0** = 10 号总计划中 **「阶段零（F-0~F-4）」之后、「阶段一（F-5）」之前** 的工程闭合 Sprint，不是「阶段一~六」里的某一阶段。

| 环节 | 内容 | 状态 |
|------|------|------|
| **Sprint 0-A** | CI、Schema 回归、Outbox/JwtHandler、ADR-017/018 | ✅ `v5.2-gate-p0-complete` |
| **Sprint 0-B** | LlmGateway、ir-to-schema、AI 表、Studio 骨架 | ✅ `v5.2-ai-infrastructure-m0` |
| **Sprint 0-C** | V7.0 六项 POC（AgentIR、Qdrant、SchemaGovernor…） | ⚠️ **未在 Sprint 0 内完成** → 见 36 号 §2.3 **待办** |
| **Three.js PoC** | 阶段二启动前门禁 | 见 `poc/threejs-benchmark/` · 阶段一回顾 |

原 14 号全文（逐日任务表）因仓库误删且 git 副本编码损坏，**不再恢复长文**；逐日验收以 `phase-1-retrospective.md` §3 交付物清单为准。

---

## 二、已完成的关键交付（摘要）

详见 `phase-1-retrospective.md` §3。

**后端**：`LlmGatewayService` · `AiCallLogService` · `KnowledgeGraphStore` · `FounderGuardMiddleware` · AI 相关 Entity/表  
**前端**：`ir-to-schema.ts` · `types.ts` · Vue3/Dashboard 编译器 · Studio 骨架 · 158 tests  
**文档**：`coverage-gap-report.md` · `infrastructure-debt-registry.yaml` · ADR-017/018  

---

## 三、与当前进度的关系

```
✅ 阶段零 F-0~F-4
✅ Sprint 0-A / 0-B
✅ 阶段一部分 F-5、F-6a（见 phase-1-retrospective）
⏳ Sprint 0-C（V7.1 POC-A~F）— 36 号 §2.3
⏳ 阶段二 F-6b — 36 号 §6.2
⏳ 数据轨 DS-0~6 — 35 号
```

**工程师现在该读**：`33 宪法` → `36 路线图（看「当前状态」）` → `10~12 分卷` → `35 数据轨`。
