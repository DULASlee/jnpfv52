# JNPF V3.0 技术债跟踪

> **原则：** 技术债必须显性跟踪，注明责任人和截止日期，否则会变成永久债。
> **最后更新：** 2026-06-26（V3.0 架构冻结）

---

## 待处理

### 1. `jnpf-frontend-rules` 合并 → WONTFIX
- **现状**：Rule 数量 12，设计目标 11。多出 `jnpf-frontend-rules.md`
- **决策**：暂不合并。前端规则独立维护有合理性（技术栈差异大，合并可能增加复杂度）
- **标记**：下次前端重构时评估

### 2. 真实 LLM 行为验证
- **现状**：所有规则和 soul 约束仅通过模拟测试验证，未经过真实 Claude Code 会话确认
- **方案**：R-001(C级) / R-002(B级) / R-003(埋雷) 三个任务在新会话中执行
- **责任人**：@工程师
- **截止日期**：下次会话

### 3. evolution_manager.py Markdown 解析器
- **现状**：使用正则解析 review_report.md，对格式变化敏感
- **方案**：升级为 YAML frontmatter 或 JSON 结构化解析
- **优先级**：低（当前格式已冻结，短期无变化风险）

---

## 已解决

### ✅ 5 个 CC>30 历史存量方法拆分销账（战役 D1，2026-08-24/25 完成）
- **问题**：5 个历史存量方法超圈复杂度阈值 30，均归因 commit `456e2d6b`，2026-08-24 Task 3.4 期间裁决登记基线冻结（只许下降不许上升）
- **解决**：战役 D1 三波次逐方法拆分重构（规格 `docs/superpowers/specs/架构设计规格-复杂度基线技术债拆分重构.md` · 计划 `docs/superpowers/plans/实施计划-复杂度基线技术债拆分重构.md`），D1 战役登记的 5 个基线条目全部移除（台账现存 `456e2d6b` 初始 119 条历史存量条目，非本战役范围），`/p:CI_BUILD=true` 无豁免 0 错实证，D1 Final Review 见 `.claude/evidence/d1-complexity-debt/D1-Final-Review.md`：

| 方法 | 前 CC | 拆分结构 | 销账提交 |
|------|------|---------|---------|
| `ListSuperQueryInputRewriter.Rewrite` | 84 | 门面+符号直映表+7 专项发射器 | `e84e96dd` |
| `FieldBindDefaultValueHelpers.Bind` | 82 | 门面+值对象+5 解析器+子表递归 | `be3d372e` |
| `FlowFormDataMapper.ApplyMapRules` | 37 | 门面+守卫器+3 形态发射器 | `c24c6253` |
| `ImportFirstVerifyHelpers.ValidateBatchUnique` | 35 | 门面+主字段/子表 4 子方法+错误串接器 | `717929ff` |
| `GetConditionQueryClauseAppender.Append` | 31 | 门面+直映表+3 特判器 | `bae2bf36` |

- **关闭条件**：全部销账提交可独立 revert；行为等价由 5 组特征金标准（32/19/25/13/33）+ 全套件锁定；怪异行为（Q1-Q11）保真不修（另立行为变更战役）；**子方法 ≤14 原始 DoD 5 处超限（EmitSymbolClause 17/EmitExpandedInNotInClauses 17/TryBindMainSelector 15/ResolveUserDefault 16/BindTableChildren 24）经人工批准接受为偏差，登记为后续结构优化债务（规格 §9.1/9.3）**

### ✅ 规则文件双重存在
- **问题**：`assertion-discipline.md`/`engineering-laws.md`/`workflow.md` 在 `rules/` 和 `souls/_shared/` 双重存在
- **解决**：2026-06-26 — 删除 `souls/_shared/` 副本，统一在 `rules/` 维护

### ✅ orchestrator/ 残留
- **问题**：废弃 Python 状态机代码残留 7 个 .py 文件
- **解决**：2026-06-26 — 归档到 `_archived/orchestrator/`

### ✅ workflow-state.json 残留
- **问题**：guard-workflow.mjs 状态文件未清理
- **解决**：2026-06-26 — 归档到 `_archived/`

### ✅ guard-write.mjs L3-L8 迁移
- **问题**：安全扫描逻辑在 Hook 和 Python 双份维护
- **解决**：2026-06-26 — `security_scanner.py` 维护 L3-L8，`guard-write.mjs` 维护 L1-L2 轻量检查
