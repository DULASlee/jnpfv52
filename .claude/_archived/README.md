# JNPF V3.0 归档目录

> **归档时间：** 2026-06-25
> **归档原因：** V3.0 涅槃重构 — 外部 Python 状态机替代分散 Hook 和冗余规则

---

## hooks/ — 已归档的 Hook（9 个）

| 文件 | 归档原因 |
|---|---|
| `guard-workflow.mjs` | 职能上移状态机 — 阶段流转和 SP 调用检查由 `state_machine.py` 硬编码执行 |
| `post-build-verify.mjs` | 职能上移状态机 — Q3/Q4 质量门直接执行编译/测试 |
| `verify-mistake-log.mjs` | 职能上移状态机 — Phase 7 强制检查错题本 |
| `format-and-lint.mjs` | 职能上移状态机 — Q3 硬执行 `dotnet format --verify-no-changes` |
| `smart-post-hook.mjs` | 职能上移状态机 — 统一调度 eslint |
| `skill-reminder.mjs` | 职能上移状态机 — 根据任务级别自动提醒 |
| `superpowers-check.mjs` | 状态机不依赖 superpowers 插件 |
| `load-mistakes.mjs` | 职能上移状态机 — 组装 prompt 时注入 `coder-reminders.md` |
| `codegraph-auto-sync.sh` | Git hook，非 Claude hook。CodeGraph 同步由状态机 `evolution_manager` 管理 |
| `collect-summary.mjs` | 职能上移状态机 — SessionEnd 时由状态机触发 |
| `guard-oa-module.mjs` | 已吸收到 `guard-write.mjs` L4（统一八层检查） |
| `guard-sql-injection.mjs` | 已吸收到 `guard-write.mjs` L6 |
| `guard-auth.mjs` | 已吸收到 `guard-write.mjs` L7 |
| `guard-tenant-filter.mjs` | 已吸收到 `guard-write.mjs` L5 |
| `guard-frontend-leak.mjs` | 已吸收到 `guard-write.mjs` L8 |

## rules/ — 已归档的规则（6 个）

| 文件 | 归档原因 |
|---|---|
| `workflow-pipeline.md` | 合并入 `workflow.md` — 单一信源 |
| `rules-loader.md` | 职能上移状态机 — `_assemble_prompt` 硬编码加载策略 |
| `review-workflow.md` | 拆分为 `reviewer-discipline.md` + 状态机 Schema |
| `communication.md` | 软约束，长会话漂移率 ~50%，不纳入状态机 |
| `memory.md` | 跨会话记忆由状态机 `evolution_manager` 管理 |
| `codegraph-exploration.md` | Phase 2.5 动态深度引擎逻辑已整合到 `workflow.md` |

## 恢复方法

如需恢复单个文件：
```bash
cp .claude/_archived/hooks/<filename> .claude/hooks/
cp .claude/_archived/rules/<filename> .claude/rules/
```
