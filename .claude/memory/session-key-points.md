# Session Key Points — 2026-07-07

> 本会话关键决策、发现、避坑。`collect-summary.mjs` 据此收录跨会话上下文。

## 任务

创建两个项目级可 dispatch 子 agent（jnpf-tester / jnpf-debugger），分别承载 Dev Loop 测试工具链与 data-driven-debug 工具链，并把工作流水线 Phase 5 / Debug Path 的 dispatch 改线到这两个 agent。

## 关键技术决策 + 理由

1. **薄壳 agent + `skills:` frontmatter 预注入**（而非自包含巨石 / 运行时 Read）
   - 理由：Claude Code 官方文档（`code.claude.com/docs/en/sub-agents.md#preload-skills-into-subagents`）确认 `skills:` 字段在子 agent 启动时把技能全文注入上下文。jnpf-tester 预注入 `jnpf-api-cli`、jnpf-debugger 预注入 `data-driven-debug`，技能升级时 agent 自动跟进（DRY）。
   - 验证手段：claude-code-guide 子 agent 查证（[KNOWN, HIGH]）

2. **子 agent 继承项目 CLAUDE.md 层级 + git 状态**（仅 Explore/Plan 例外）
   - 理由：同上文档确认。故 agent 文件不必重写铁律、论断纪律、B0——靠继承。
   - 影响：agent 文件可保持 ~100 行薄壳

3. **两者均无 Write/Edit，输出作为 final message 返回、由主 Claude 持久化**
   - 理由：测试员/诊断员都不应改代码（铁律）。给 Write 反而诱惑 agent "顺手修"。输出（JSON / debug report）作为 final message 返回给 dispatcher 持久化，职责清晰。
   - 截图/证据由 Bash 调脚本自行落盘（visual-debug/test:api 自己写文件），不需 Write 工具

4. **命名 `jnpf-*` 不覆盖全局 test-runner**
   - 理由：用户级 `~/.claude/agents/test-runner.md` 是通用 agent，其他项目依赖。项目级同名会覆盖（priority 3 > 4）。用新名 `jnpf-tester` / `jnpf-debugger` 避免污染

5. **MCP 工具授权 `mcp__netcoredbg__*`**
   - 理由：netcoredbg 已在 `mcp.json` 配置，子 agent 默认继承主会话 MCP；显式列入 `tools:` 白名单授权运行时调试

## 发现的 Bug 及根因（即使已提交也要摘要）

### ISSUE-002：guard-write.mjs 八层守卫合并未完成（P1，pre-existing）

- **症状**：`node scripts/test-hooks.mjs` 28 用例全 FAIL，统一"期望 2 实际 1"
- **根因**：旧独立 guard（oa/sql/auth/tenant/leak）已删除，但合并到 guard-write.mjs **从未完成**——文件实际仅 L1/L2/L3/L4（L4 还是工作区隔离，非 R5）。CLAUDE.md Hooks 表"L1-L8 八层"是**虚假承诺**。test-hooks.mjs 仍引用旧文件名 → MODULE_NOT_FOUND → exit 1
- **影响**：R4/R5/R6/R8 四条 L0 红线无 hook 防护（R7 部分覆盖），仅靠 AI 自觉（L2）
- **处理**：记入 `pending-issues.md` ISSUE-002，待审批后另起任务修复
- **与本会话无关**：pre-existing。我本会话只改 .md 文档 + gitignored settings.local.json，不可能导致 hook MODULE_NOT_FOUND

## 踩过的坑 + 避免策略

1. **Claude Code 会话级 agent 注册**：`.claude/agents/*.md` 在**会话启动时**发现并注册，运行时新增**不会动态加载**。本会话创建的 jnpf-tester/jnpf-debugger 无法在本会话 dispatch 验证（"Agent type not found"），须重启 Claude Code 后新会话才能 dispatch
   - 避免：agent 文件创建后，frontmatter 静态校验（node 解析）作为本会话证据；动态 dispatch 冒烟标注"待新会话"，不伪造成功

2. **计划里的 Edit 锚点可能与实际文件有出入**：Task 6 计划假设 workflow.md 标题是 `## Phase 5 Verify — Supreme Iron Law`，实际是 `...（E2E 证据）`。Edit 前先 Grep 确认锚点存在，避免失败
   - 避免：每个 Edit 任务先 Grep 验锚点，再下 Edit

3. **`settings.local.json` 是 gitignored**：按设计 per-user 本地覆盖，不入版本控制。计划写"commit"步骤错了——强制 `git add` 反而违反 .gitignore
   - 避免：编辑 gitignored 文件后只做 JSON 合法性校验，不 commit；团队共享权限应入版本化的 `.claude/settings.json`（本项目无此文件，是缺口）

## 未写入但值得注意

- jnpf-tester/jnpf-debugger 的动态 dispatch 冒烟**未在本会话完成**（受会话级注册限制）。新会话重启后应补跑，证据存 `.claude/evidence/jnpf-{tester,debugger}-dispatch-smoke.*`（已写 fallback 说明）
