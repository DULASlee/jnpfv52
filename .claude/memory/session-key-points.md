# Session Key Points — 2026-07-18

> 本会话关键决策、发现、避坑。跨 Chat 详见 `.claude/memory/session-summaries/2026-07-18-pm-clarification-resume.md`

## 任务

PM 新流程澄清续跑：pipeline 404 第2轮答完说「继续」，应进步骤③/说明书确认，禁止回第1轮、禁止 NRE、禁止 IR-0 骨架审阅。

## 已归档位置

| 文件 | 内容 |
|------|------|
| `.cursor/CURRENT-FOCUS.md` | 当前节点 + 2026-07-18 结论 |
| `docs/progress-registry.yaml` | session_log 2026-07-18 |
| `.claude/memory/mistake-log.md` | M036–M038 + `## 2026-07-18` |
| `.cursor/rules/toolchain/episodic-memory-automation.mdc` | 会话末强制归档规则 |

## 流程缺口（用户指出）

- episodic MCP 只读，不能写回记忆
- Cursor 不跑 guard-finish Stop hook → 错题本/进度不会自动落盘
- **对策**：episodic-memory-automation.mdc 已补「会话末（写）」五文件 checklist；Agent 必须在有代码变更或调试闭环时执行

| **跨会话归档** | `.cursor/hooks/session-archive-stop.mjs` + `session-digest/latest.json` |

## 归档自动化（2026-07-18 新增）

| 层 | 机制 |
|----|------|
| 对话全文 | stop → `episodic-sync.mjs` → MCP search 可读（**不是 MCP write**） |
| 结构化进度 | stop → `session-archive-stop.mjs` → digest + followup 强制补 CURRENT-FOCUS/错题本 |
| 下会话提醒 | sessionStart 注入 `<SESSION-ARCHIVE-PENDING>` |

## 待用户验

pipeline 404：答2轮 → 继续 → 说明书确认卡片
