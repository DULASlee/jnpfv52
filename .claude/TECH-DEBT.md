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

### 4. Tech-Debt: CC31-Append-Refactor
- **现状**：5 个历史存量方法超圈复杂度阈值 30，均归因 commit 456e2d6b，2026-08-24 Task 3.4 期间裁决登记基线冻结（只许下降不许上升）：
  - `GetConditionQueryClauseAppender.Append`（CC31）
  - `ImportFirstVerifyHelpers.ValidateBatchUnique`（CC35）
  - `ListSuperQueryInputRewriter.Rewrite`（CC84→已拆分销账，2026-08-24 战役 D1 D1.1：门面+子方法全部 <30，基线条目已移除，无豁免通过 CI）
  - `FieldBindDefaultValueHelpers.Bind`（CC82→已拆分销账，2026-08-24 战役 D1 D1.2：门面+子方法全部 <30，基线条目已移除，无豁免通过 CI）
  - `FlowFormDataMapper.ApplyMapRules`（CC37→已拆分销账，2026-08-24 战役 D1 D1.3：门面+子方法全部 <30，基线条目已移除，无豁免通过 CI）
- **处置**：`complexity-baseline.json` 登记冻结值（每条含归因注记）；方法级 TODO 注释；CI（`dotnet build /p:CI_BUILD=true`）拦截任何上升
- **拆分重构（已立项，防遗忘硬绑定）**：设计规格 `docs/superpowers/specs/架构设计规格-复杂度基线技术债拆分重构.md` + 实施计划 `docs/superpowers/plans/实施计划-复杂度基线技术债拆分重构.md`（战役 D1，三波次：P0=Rewrite CC84→≤10 / Bind CC82→≤12，P1=ApplyMapRules / ValidateBatchUnique，P2=Append；目标：全部降到 30 以下并从基线销账）
- **启动节点**：运行时基座战役 Task 3.6 S1 门禁通过后，每波次独立节点审批
- **责任人**：待认领（后续战役承接）

---

## 已解决

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
