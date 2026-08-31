# 类级重构专家 Agent 封装手册 — CHANGELOG

> 本文档记录 [`2026-08-30-类级重构专家Agent封装实施计划.md`](./2026-08-30-类级重构专家Agent封装实施计划.md) 的所有变更。
>
> **格式**：每条变更含日期 + 版本 + 变更摘要 + 关联 baseline 决策。

---

## [2026-08-30] v1.0 — 首版发布

### Added（新增）

- ✅ 完整手册首发（891 行）
- ✅ §0 元数据 + §1 项目背景 + §2 目标与边界
- ✅ §3 设计骨架回顾（Section 1-7 决策摘要）
- ✅ §4 目录结构（Phase 1 物理分层）
- ✅ §5 实施步骤（10 步带命令）
- ✅ §6 Profile 抽离详细流程
- ✅ §7 Knowledge 资产迁移（18 个 references 全量映射）
- ✅ §8 验证清单（DoD 7 类）
- ✅ §9 增量更新约定
- ✅ §10 附录：决策索引（D-01 ~ D-30）
- ✅ §11 后续 Phase 规划（占位）
- ✅ 附录 A/B/C（命令清单 / 文档链接 / 变更记录指针）

### Source（来源）

- 上位 baseline：`docs/superpowers/specs/2026-08-30-类级重构专家Agent封装设计规格.md` v1.8
- 源 skill：`.claude/skills/generic-class-refactor-expert/` v6.0
- 设计过程：2026-08-30 brainstorming Section 1-7 全部通过

### Locked Decisions（v1.0 涵盖的锁定决策）

- **架构层（Section 2）**：L-01 ~ L-10
- **推理层（Section 3）**：CR-01 ~ CR-11 + D-15 ~ D-19
- **模式层（Section 4）**：D-20 ~ D-26
- **Profile 层（Section 5）**：P-01 ~ P-08 + D-13/D-14 + C-13 ~ C-16
- **Knowledge 层（Section 6）**：K-01 ~ K-10 + D-27 ~ D-30
- **验证层（Section 7）**：V-01 ~ V-09

### Migration Notes

- 手册 v1.0 仅覆盖 Section 1-7 的实施摘要
- 后续 Section 8-10 完成后，会在本文档追加章节：
  - Section 8 完成后追加"## 12. Qoder Implementation Mapping"
  - Section 9 完成后追加"## 13. Open Source Packaging"
  - Section 10 完成后追加"## 14. v6.0 Migration Runbook"
- v6.0 skill **不修改**，与本手册共存

---

## [2026-08-30] v2.1 — Implementation Interpretation Guard

### Added（新增）

- ✅ §5 执行前提追加 v2.1 实施约束（引用 baseline §7.5）
- ✅ Step 2 来源引用更新（§7.5.1 Governance Check + §7.5.4 Memory Boundary）

### Modified（修订）

- 修订 §5 执行前提：新增 "v2.1 实施约束" 段落，禁止以"快速 MVP"删除核心闭环
- 修订 Step 2 注释：来源增加 §7.5.1（Governance Check 内嵌）+ §7.5.4（Memory Boundary）

### Source（来源）

- 上位 baseline：`docs/superpowers/specs/2026-08-30-类级重构专家Agent封装设计规格.md` **v2.1**（Implementation Interpretation Guard）
- 设计过程：Implementation Interpretation Guard 专家审核通过

### Locked Decisions（v2.1 新增的锁定决策）

- **IRON-13**：Governance Kernel Is Active Runtime Dependency（非 Configuration）
- **IRON-14**：Capability Must Be Behaviorally Real（禁止 Fake Planner/Reflection/Evidence/Memory）
- **Phase Boundary Rule**：Phase1 仅做 Runtime 框架，禁止 Intelligence
- **MVP Definition Law**：MVP = 完整闭环的最小实现，而非最小功能
- **Memory Boundary Clarification**：Memory = Structured Engineering State，禁止 Chat History Storage
- **WORKFLOW-IRON Scope Clarification**：约束 Runtime 开发+运行+Knowledge演进+Profile演进四维度

### Implementation Notes

- Phase1 = Runtime 框架开发（骨架 + 接口 + 集成点）；禁止 Intelligence 混入
- Governance 不是配置文件：每次关键决策循环必须内嵌 Governance Check
- Fake Capability 零容忍：Planner 必须动态生成 Task Graph；Reflector 必须能发现失败
- MVP 禁止删除：状态 / Evidence / Reflection / DAG / Resume / Audit Trail

---

## 后续更新模板

```markdown
## [YYYY-MM-DD] v<MAJOR>.<MINOR>

### Added
- 新增章节 N: <标题>

### Modified
- 修订章节 N.M: <变更摘要>

### Deprecated
- 废弃章节 N: <原因>

### Migration Notes
- <如需迁移操作>
```

---

> **维护纪律**：每次更新手册主体，必须同步更新本文档，并在 commit message 中标注 `[harness-manual]` 前缀。
