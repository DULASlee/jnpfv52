# jnpf-debugger dispatch 冒烟证据

**日期**：2026-07-06
**Agent 文件**：`.claude/agents/jnpf-debugger.md`
**Commit**：`6eb42a5c`

## 静态校验（本会话已完成）

- frontmatter 解析：PASS
- 字段核验：`name: jnpf-debugger`、`description:`、`tools: Bash, Read, Grep, Glob, mcp__netcoredbg__*`、`skills: data-driven-debug` 全部存在
- 校验命令：`node -e "...jnpf-debugger.md frontmatter match..."` → `PASS: frontmatter valid`

## 动态 dispatch（本会话失败，符合预期 fallback）

- 方法：Agent 工具，`subagent_type: jnpf-debugger`
- 结果：**FAIL — agent not loaded in current session**
- 错误：`Agent type 'jnpf-debugger' not found. Available agents: claude, claude-code-guide, code-reviewer, episodic-memory:search-conversations, Explore, general-purpose, Plan, security-scanner, statusline-setup, test-runner`

## 根因 [KNOWN]

Claude Code 在**会话启动时**发现 `.claude/agents/*.md` 并注册为可 dispatch subagent。本会话启动时该文件尚不存在（创建于本会话中），故未注册。运行时新增 agent 文件**不会被动态加载**——这是 Claude Code 的确定行为（非 bug），与本计划 Task 3 预告的"已知不确定性"一致。

## Fallback

- **静态**：frontmatter 校验 PASS（本会话已完成，证明 agent 文件定义合法）
- **动态**：重启 Claude Code 后，新会话启动时会发现 `.claude/agents/jnpf-debugger.md` 并注册。届时重跑 dispatch 冒烟即可验证：
  ```
  Agent 工具 → subagent_type: jnpf-debugger
  prompt: "检查 backend/.claude/diagnostics/ 下 session-*.jsonl，用 jq 提取最近 3 条 error，按 debug report 格式返回"
  预期：返回合法 debug report，含"数据链路追踪"表头
  ```

## 结论

agent 定义就绪 + 静态合法。动态 dispatch 验证待新会话——这是 Claude Code 会话级 agent 注册机制限制，非 agent 文件本身问题。

## 不确定性标注

- `[KNOWN]` Claude Code 会话级 agent 注册（运行时返回的 available 列表为实证）
- `[INFERRED]` 新会话启动后将注册成功——基于 Claude Code 官方文档"项目级 .claude/agents/ 会被发现" + 本会话观察到的注册机制，置信度 HIGH（但未在当前上下文实际执行新会话验证）
