# Default Workflow — JNPF 专属约束

> 七阶段流水线见 `CLAUDE.md` Workflow Pipeline。本文件仅保留 JNPF 项目的专属输出格式和约束。

---

## 任务分级

| 级别 | 条件 | 流水线 |
|---|---|---|
| **S 级（复杂）** | 3+ 文件 / 50+ 行 / 架构决策 / 新模块 | 完整 7 Phase + brainstorming 独立阶段 + 子代理 |
| **A 级（标准）** | 2 文件 / 10-50 行 / 功能增强 | 完整 7 Phase |
| **B 级（简单）** | 单文件 ≤10 行 / bug fix / 样式 / 文档 | 跳过 Phase 3 Plan，不跳过 Phase 2 Brainstorm |

**B 级绝不跳过 Brainstorm（S1 铁律）和 Verify（Law 2）。**

## 强制声明 — 开始任何任务前，输出：

```
🔄 Workflow 启动
- 任务分级：S / A / B
- 理由：[为什么是这个级别]
```

---

## Phase 2 Brainstorm 后 → Phase 3 Plan 时的需求提取清单

**A 级及以上任务，编码前 MUST 输出：**

```
📋 需求提取清单
| # | 需求原文（来自架构师/用户指令） | 实现映射 | 歧义/风险 |
|---|---|---|---|
| 1 | [逐条引用原文] | [映射到哪个文件/函数] | 无 / [具体歧义] |
```

**清单为空 → 不得开始编码。**
**有条目标注"歧义" → 必须先提问澄清，获准后才能编码。**

编码完成后，Phase 6 Review MUST 对照此清单逐条标注：
```
✅已实现 / ⚠️偏离(附理由及审批记录) / ❌未实现(附阻塞原因)
```
偏离或未实现若无事前审批记录 → 流程违规，MUST 退回补救。

---

## Phase 4 Build 执行铁律

- 标记 `in_progress` 后再开始，完成后立即标记 `completed`
- 严格按计划步骤执行，不"顺手"改计划外的东西
- **审查项强制注入：** 每次开始编码时，todo_write 中 MUST 含 `🔍 代码审查 (子代理)` 条目（status: pending）。该条目在 code-reviewer 返回 PASS 之前 MUST NOT 标记 completed。无此条目 → 流程阻塞。
- **错题本强制注入：** todo_write 中 MUST 含 `📝 错题本追加` 条目。Phase 6 Review 时检查。Phase 7 报告前该条目仍为 pending → 流程阻塞。
- 子代理不信任报告：完成后必须独立检查 VCS diff + 验证变更
- 子代理 BLOCKED → 分析原因（缺上下文？能力不足？计划有误？），不盲目重试

---

## Phase 5 Verify — Supreme Iron Law（E2E 证据）

- **⬛ 浏览器端到端操作是唯一验收标准**
- 使用 `playwright` 技能打开浏览器
- 产出截图至 `.claude/evidence/`（E1 证据）
- 记录操作路径（E2 证据）→ Phase 7 报告中输出
- 描述实际 UI 状态（E3 证据）→ Phase 7 报告中输出
- 无 E1/E2/E3 → `guard-finish.mjs` BLOCK

---

## Phase 6 Review — 错题本强制检查

- 本次 session 是否有 `fix:` / `bug:` / 错误修复性质的改动？
- **判断方法：** `git log --oneline --since="<session-start>"` 检查 commit message 前缀
- **有 → MUST 追加到 `.claude/memory/mistake-log.md`**
- **未追加 → 流程阻塞，MUST NOT 声称 Phase 7 完成**
- 此项检查不因任务级别（S/A/B）豁免

---

## Phase 7 Report（报告模板）

```
## 完成报告

**变更摘要：** [一句话]

**文件变更：**
| 文件 | 操作 | 行数 |
|---|---|---|

**测试结果：** PASS / FAIL（含证据）

**⬛ E2E 验证证据（Supreme Iron Law）：**
- E1 截图：[路径]
- E2 操作路径：[打开页面 → 操作步骤 → 观察结果]
- E3 实际输出：[浏览器中实际看到的 UI 状态]

**🟠 错题本：** 本次新增 N 条（Mxxx-Myyy）/ 无需新增

**已知问题：** 无 / [列出]

**剩余工作：** 无 / [列出]
```

重要变更写入 `.claude/memory/decisions.md`。

---

## 执行路径速查

```
收到任务
  │
  ├─ 简单？(B级) → Phase 1→2→4→5→6→7 (skip Phase 3 Plan)
  │
  ├─ 标准？(A级) → Phase 1→2→3→4→5→6→7
  │
  └─ 复杂？(S级) → Phase 1→2→3→4(subagent)→5(test-runner)→6(code-reviewer)→7
```

> 手动触发完整审查：SP `requesting-code-review`

---

## 关联规则索引

| 阶段 | 规则文件 |
|---|---|
| Phase 1-2 | `architecture-redlines.md`, `jnpf-expert-traps.md` |
| Phase 4 | `sql-safety.md`, `frontend-memory-leak.md` |
| Phase 5 | `testing.md`, `engineering-laws.md` |
| Phase 6 | `review-workflow.md`, `architecture-redlines.md` |
| Debug | `debugging.md`, `engineering-laws.md` |
