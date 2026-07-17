---
name: coder-mode
description: 进入 Coder 角色（写/改后端 .cs 或前端 .vue/.ts 代码时）。活性加载 souls/coder/soul.md 角色定义，按 Phase 4 Build 约束、sql-safety/frontend-memory-leak 红线、Review Gate 计数器行动。
---

# Coder Mode — 活性加载 souls/coder/soul.md

调用此 skill 即进入 **Coder** 角色。立即 Read 以下文件并按其约束行动：

1. `D:\JNPF-v52\.claude\souls\coder\soul.md` — 角色定义（身份/约束/Phase 4 明细 §7/Review Gate §8/闭环引用 §9/输入输出/禁止/回退）
2. 编码前按 soul「输入格式」列出的 Rule 文件：
   - 写 `.cs` → `.claude/rules/sql-safety.md` + `.claude/rules/jnpf-expert-traps.md`
   - 写 SSE/timer/EventSource/WebSocket → `.claude/rules/frontend-memory-leak.md`

## 触发场景

- 写或改后端 C# 代码（.cs）
- 写或改前端代码（.vue / .ts）
- 任何 Phase 4 Build 实施动作

## 退出条件

代码变更完成 → 按 soul §9 自动测试闭环走 Dev Loop：
`dotnet build → node scripts/jnpf-api.mjs GET /api/oauth/CurrentUser → E2E_PIPELINE_ID=311 pnpm test:api`
三步全绿 → 交还 Orchestrator 进入 Phase 5 Verify（dispatch `jnpf-tester`）。

## 硬约束（来自 soul）

- **ADF 三先行（S/A）：** 无 P1–P3 用户批准不得写业务实现；见 `.cursor/rules/architecture-design-interface-first.mdc`
- **Review Gate 计数器**（soul §8）：Write/Edit 后 +1，≥2 触发 code-reviewer 子代理；Step 7 重置
- **todo 强制含** `🔍 代码审查(子代理)` + `📝 错题本追加`，code-reviewer PASS 前保持 pending
- **禁止**吞异常 / TODO/FIXME / 无根因改动 / `.ToListAsync()` 用于 >100 条查询 / IDynamicApiController 方法带 Async 后缀
- **Trap 自查**（soul §3）：Mapster 审计字段 / N+1 / Updateable 租户 / public=API / 分页
- **零占位符：** hooks L11 + pre-commit 硬拦；例外 `// placeholder-ok: <理由>`