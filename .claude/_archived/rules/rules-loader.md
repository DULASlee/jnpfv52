# 规则加载协议 — 分层加载 + 去重 + 预算控制

> **设计原则：** 规则越多，AI 上下文自噬越严重。通过分层加载和语义去重，将 always-load 控制在 6,000 tokens 以内，释放上下文给真正的代码工作。
>
> **当前状态：** 18 个规则文件，always-load ~22,000 tokens（占总 context 11%）。目标：压缩至 6,000 tokens（2.9%）。

---

## 加载层级

| 层级 | 内容 | Token预算 | 加载时机 | 包含文件 |
|---|---|---|---|---|
| **L0: Core** | 状态机定义 + 安全红线摘要 + 论断纪律 | <2,000t | SessionStart（始终加载） | CLAUDE.md (精简后) + assertion-discipline.md 摘要 |
| **L1: Workflow** | 阶段流转规则 + GATE 阻断条件 + 任务分级 | <3,000t | SessionStart（始终加载） | workflow.md (合并后) |
| **L2: Domain** | JNPF 特定规则（低代码原则、专家陷阱、架构红线） | 按需 | Phase 触发 | low-code-principles.md, jnpf-expert-traps.md, architecture-redlines.md |
| **L3: Tool** | MCP 调用规范、CodeGraph 查询语法、Tool Search 路由 | 按需 | 工具调用前 | codegraph-exploration.md (路由表部分), tool-search 内嵌 |

---

## 按需加载触发表

| 触发条件 | 加载层级 | 具体文件 |
|---|---|---|
| Phase 1 Align | L2 | architecture-redlines.md |
| Phase 2 Brainstorm → Phase 2.5 Explore | L3 | codegraph-exploration.md |
| Phase 3 Plan (S/A 级) | L2 | low-code-principles.md |
| Phase 4 Build — 写 C# 代码 | L2 | jnpf-expert-traps.md + sql-safety.md |
| Phase 4 Build — 写 Vue 代码 | L2 | jnpf-frontend-rules.md + frontend-memory-leak.md |
| Phase 5 Verify | L2 | testing.md |
| Phase 6 Review | L2 | review-workflow.md |
| Debug Path | L2 | debugging.md |
| 会话 Start/Stop | L2 | memory.md |

---

## 去重策略

### 已知重复块（从 always-load 中移除的重复内容）

| 重复内容 | 单一信源位置 | 从以下文件移除 |
|---|---|---|
| Gate Function (IDENTIFY→RUN→READ→VERIFY→CLAIM) | `engineering-laws.md` Law 2 | `testing.md`（改为引用链接） |
| 红旗词清单 | `engineering-laws.md` Law 2 | `testing.md`（改为引用链接） |
| 任务分级 (S/A/B) | `workflow.md`（合并后） | `workflow-pipeline.md`（已合并） |
| Phase 流水线映射 | `workflow.md`（合并后） | `CLAUDE.md`（精简为引用） |
| 架构红线 R1-R10 摘要 | `architecture-redlines.md` | `CLAUDE.md`（仅保留速查表引用） |
| Trap 1/6/9/4/5/7/8/12 | `architecture-redlines.md` | `jnpf-expert-traps.md`（标注"详见红线 R1-R8"） |

---

## Always-Load 预算检查

SessionStart 加载完毕后，AI MUST 自行检查：

```
📊 规则加载预算报告
  L0 Core:      [实际] tokens / [2000] 预算
  L1 Workflow:  [实际] tokens / [3000] 预算
  Always-load:  [实际] tokens / [6000] 预算  ← 硬上限
  状态: ✅ 通过 / ⚠️ 超出（需手工检查）
```

---

## 关联文件

- 工作流流水线（合并后）→ `workflow.md`
- 工程铁律（Gate Function 单一信源）→ `engineering-laws.md`
- 架构铁律（R1-R10 唯一信源）→ `architecture-redlines.md`
