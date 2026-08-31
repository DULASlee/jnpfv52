# Implementation Master Plan v2.1 — 落盘 + STOP 保持（2026-08-31）

## 来源

Chief Architect 2026-08-31 final directive（"以后不会让 AI 工程师自己跑 Schema 修复"）

## 落盘路径

docs/superpowers/plans/2026-08-31-JNPF-Table-Refactoring-Master-Plan-v2.1.md

## Plan 内容

- 0. 总体执行目标（Table Schema Refactoring CLOSED）
- 1. 全局执行铁律（10 Iron Laws 重申）
- 2. 当前状态严格 STOP
- 3. Phase 30：Batch 30+ Gap Review Gate（Tasks 30.1-30.7）
- 4. Phase 31：Migration Specification
- 5. Phase 32：Human Gate
- 6. Phase 33：Migration Execution
- 7. Phase 34：Runtime Validation
- 8. Phase 35：Performance Validation
- 9. Phase 36：Batch Closure
- 10. Phase 37：全局 Gap Closure
- 11. Phase 38：Final Acceptance
- 12. 最终关闭条件
- 13. AI 工程师执行协议
- 14. 汇报格式
- 15. 当前唯一允许启动的 Task Bundle
- 16. 当前项目终点定义

## 关键变化（vs 之前 plan）

之前 Plan 假设"发现问题 → 立即修复"。
Plan v2.1 强制插入 **完整 Decision Gate**：
1. 真实 Schema 查询（不是历史报告）
2. Target Contract 检查
3. Risk 分类
4. 5 种状态彻底分开：NO_CHANGE / DEFERRED / EXCLUDED / BLOCKED / MIGRATION_REQUIRED
5. 不允许 "TODO / TBD / Later" 等无操作性结果
6. Dynamic / Hybrid 强制 Human Gate

## 当前唯一允许动作（明确）

BATCH 30+ GAP REVIEW BUNDLE（Tasks 30.1-30.7）

## 当前禁止动作

ALTER TABLE / CREATE INDEX / DROP / Constraint Change / Column Change / ORM Mapping / Entity Change / Production Data Migration

## AI 工程师契约

- 不主动推进任何工作
- 不创建新 artifact（除了本 plan 已落盘 + handoff）
- 不修改源文件
- 不扩张项目范围
- 等待 Chief Architect 触发 BATCH 30+ GAP REVIEW BUNDLE
- 触发后：完整执行 30.1 → 30.7

## 与之前 STOP 锁定的关系

Plan v2.1 = STOP 锁定 + 详细执行协议
下次解锁 = Chief Architect 触发 Batch 30+ Gap Review Bundle
