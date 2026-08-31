# ADR-019: Table Refactoring Expert Skill v1.0 冻结决策

**状态:** Final
**日期:** 2026-08-30
**阶段:** Phase 8 / P8-E Final Closure

---

## 背景

JNPF 后端 289 张表数据库历史上积累了大量 schema 漂移（列名大小写混用、nvarchar(MAX) 限制、VIEW/TABLE 误判等）、命名不一致、索引缺失等问题。传统 DBA 治理面临三大矛盾：

1. **规模化矛盾** — 289 张表 vs DBA 时间有限
2. **可追溯矛盾** — 重要决策缺乏证据
3. **核心保护矛盾** — base_user 等核心表不能误修改

Phase 8 设计了 Table Refactoring Expert Skill，通过 7 维度评估、Hard Gate 矩阵、Schema 漂移检测、Triple-Key Iron Law 等机制，建立"AI 驱动的数据库治理生产体系"。

执行结果：
- 17 批次（6 P8-B + 11 P8-C）连续执行，0 事故
- 93 张表治理（88 唯一 + 1 视图 + 4 边缘）
- 190 个索引优化
- R1 人工治理 5/5 PASS + R2-COMP 独立验证 10/10 PASS

---

## 决策内容

**Table Refactoring Expert Skill v1.0 已冻结（FROZEN @ 2026-08-30）。**

```
冻结范围（v1.0 保护以下内容的不变性）：
  - 风险分级框架（R0/R1/R2/R3+）
  - 决策模式（REFACTORED/NO-CHANGE/DEDUPLICATED/DEFERRED）
  - Schema 漂移检测规则（5 类漂移 + 自动修复）
  - Triple-Key Iron Law（AI/IR/SA 表强制）
  - Hard Gate 矩阵（HG#1~HG#6）
  - 4 层 Safety Gates 阈值
  - 7 维度评估矩阵
```

Skill 的工程化使用文档：`docs/构建AI软件工程agent闭环体系/table-refactoring-expert-skill-v1.md`

---

## 理由

### 1. 已达到生产验证标准

```
R1 人工治理：5/5 PASS
R2-COMP 独立 AI 验证：10/10 EXACT/EQUIV 一致
Safety Gates：4/4 PASS（HG FN=0, P0/P1=0, Scope=0, Closure=0）
生产事故：0
Schema 漂移自动检测：16+ 处
```

### 2. Phase 8 已确立完整的工程证据

- 95+ 个 Evidence 文件
- 4 层资产（战略/管理/技术/机器）
- 17 个 Batch Closure 记录
- 与未来 Aspire 微服务化的资产复用映射

### 3. 治理成熟度达到企业级

Skill 已能区分"什么时候动 / 什么时候不动"（22 张 NO-CHANGE 表），具备"知道什么时候不动"的 AI 治理核心能力。

### 4. v1.0 是 Aspire 微服务化的关键输入

Phase 8 资产已建立清晰的资产复用映射：
- Domain Boundary → Registry CSV Module + RiskLevel
- Repository 设计 → Change Catalog Schema 漂移修正记录
- CQRS 模型 → 索引对应查询路径的业务价值翻译

继续扩展 v1.x 会推迟 Aspire 衔接。

---

## 备选方案

| 方案 | 优点 | 缺点 | 为何不选 |
|---|---|---|---|
| 持续扩展 v1.x | 覆盖更多场景 | 推迟 Aspire 衔接；范围蔓延；变更未受控 | ✅ 战略时机不对 |
| 不冻结，继续无版本管理 | 灵活 | 未来 Aspire 无法引用稳定基线 | 不可持续 |
| **v1.0 冻结 + v2.0 演进（已选）** | 稳定基线 + 后续演进；Aspire 可立即衔接 | 升级需走 CR 流程 | ✅ 选择此项 |

---

## 后果

### 正面

- **Aspire 衔接** — Aspire 微服务化可立即基于 v1.0 设计
- **可复用性** — v1.0 可直接迁移至其他 SQL Server 项目
- **不变性保证** — Phase 8 已执行的 93 张表重新评估应产生一致结果
- **CR 升级路径** — v2.0 演进需经 Chief Architect 审批 + 回归测试

### 负面

- **新场景覆盖延迟** — 跨表外键等场景需等 v2.0
- **升级成本** — v2.0 必须走 CR + 回溯测试
- **Skill 限制需透明披露** — 9 项限制需在使用文档明确列出

### 风险缓解

- 9 项 Skill 限制已在 harness 文档中透明披露
- Triple-Key Iron Law 已强制 AI/IR/SA 表
- 16+ 处 schema 漂移检测案例已沉淀
- v2.0 候选方向已识别（跨表重构、Repository 模板、多方言支持）

---

## 验证结果

```
R2-COMP Cross-Round Cumulative：
  - Round 1: 5/5 PASS (1 RUBRIC DIFFERENCE, non-blocking)
  - Round 2: 5/5 PASS (perfect alignment)
  - Combined: 10/10 EXACT/EQUIV 一致
  - Stop Rule: TRIGGERED (5 criteria 全部满足)
  - 详见 p8-a/r2/CROSS-ROUND-CUMULATIVE-AND-GATE-DECISION.md

R1 人工盲审：
  - 5/5 文件签字 (LJY)
  - 详见 p8-a/shadow/real-human-blind-review/

生产执行：
  - P8-B: 6 批次 / 30 表 / 70 索引
  - P8-C: 11 批次 / 64 表 / 120 索引（含 5 张跨批次再触碰）
  - 17/17 全部 CLOSED, 0 incidents
  - 详见 docs/universal/Phase-8/Production-Progress-Ledger.md
```

---

## Skill v1.0 关键能力指标

| 指标 | 数值 |
|------|------|
| 风险判断一致率 | 100% (10/10 EXACT) |
| 动作建议一致率 | 100% (10/10 EQUIV/EXACT) |
| Hard Gate FN | 0 |
| P0/P1 误判 | 0 |
| Scope 错误 | 0 |
| Closure 错误 | 0 |
| Schema 漂移自动检测 | 16+ 处 |
| 生产事故 | 0 |

---

## v2.0 候选演进方向（不在 v1.0 范围）

| 方向 | 优先级 | 触发条件 |
|------|-------|---------|
| 跨表外键重构 | P1 | Aspire 阶段需要跨表优化时 |
| Repository 模板自动生成 | P1 | Aspire Repository 重构启动 |
| 多数据库方言支持 | P2 | 新项目需要 MySQL/PostgreSQL |
| 性能基准测试自动化 | P2 | 需要量化索引收益时 |
| LLM 辅助索引推荐 | P3 | 高级场景需要 |

---

## 相关 ADR

- ADR-020: R2-COMP 独立 AI 验证作为主要验证机制（驱动 Skill v1.0 冻结）
- ADR-021: Triple-Key Iron Law（Skill v1.0 核心规则之一）
- ADR-022: NO-CHANGE 主动判断原则（Skill v1.0 治理文化）
- ADR-023: Schema 漂移检测执行前强制规则（Skill v1.0 核心能力）

## 相关资产

- `docs/构建AI软件工程agent闭环体系/table-refactoring-expert-skill-v1.md` — Skill 工程化使用文档
- `docs/universal/Phase-8/Phase-8-最终关闭报告.md` — Phase 8 关闭报告
- `docs/universal/Phase-8/JNPF-AI-数据库治理-转型报告.md` — 战略叙事
- `docs/universal/Phase-8/p8-a/r2/CROSS-ROUND-CUMULATIVE-AND-GATE-DECISION.md` — R2-COMP 验证证据


