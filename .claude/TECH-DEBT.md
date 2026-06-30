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
