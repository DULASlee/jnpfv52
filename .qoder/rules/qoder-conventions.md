# Qoder 项目规则（Always Apply）

> **本文件性质**：Qoder IDE 项目级入口规则（alwaysApply），不替代 `.claude/rules/` 详规，而是入口导航 + 硬约束摘要。
>
> **优先级**：当与项目内其他 rules（`.claude/rules/`、`.cursor/rules/`）冲突时，**本文件优先**（Qoder 官方约定）。
>
> **字符上限**：100,000 字符（Qoder 限制） — 本文件保持 < 5,000 字符，详细规则通过智能/globs 拉取。

---

## 1. Qoder 启动时必须读取的资源

### 1.1 Rules 详规（按需拉取）

| 规则 | 路径 | 用途 |
|------|------|------|
| 宪法摘要 | `.claude/rules/00-constitution.md` | 四支柱 + ADF 写入锁 |
| 工程闭环铁律 | `.claude/rules/workflow-iron-law.md` | 4 环节自主闭环（必读）|
| 针式搜索铁律 | `.claude/rules/needle-search.md` | 禁止全仓 Grep/Grep 拖网 |
| MCP 代码搜索 | `.claude/rules/mcp-code-search.md` | Serena/codegraph/KG 用法 |
| Agent Runtime 铁律 | `.claude/rules/agent-runtime-iron-laws.md` | HIP-01 人类中断政策 |
| 需求分析子链铁律 | `.claude/rules/req-analysis-iron-law.md` | 阶段 A-B-C 强制 |
| 实现完整性铁律 | `.claude/rules/implementation-integrity-iron-law.md` | 五禁令 |
| 业务优先铁律 | `.claude/rules/business-first-iron-law.md` | B0 业务优先 |
| 三元组铁律 | `.claude/rules/triple-key-iron-law.md` | tenant/project/pipeline 三元组 |
| ADF 先行 | `.claude/rules/architecture-design-interface-first.md` | P0-P4 阶段纪律 |
| 全链条冲刺铁律 | `.claude/rules/fullchain-sprint-iron-law.md` | F1-F4 |
| 断言纪律 | `.claude/rules/assertion-discipline.md` | KNOWN/COMPUTED/INFERRED 标签 |
| 重构纪律 | `.claude/rules/reviewer-discipline.md` | Reviewer 模拟 |
| 工作汇报标准 | `.claude/rules/ai-work-report-iron-law.md` | 六要素汇报 |
| 低代码原则 | `.claude/rules/low-code-principles.md` | JNPF 平台特有约束 |
| JNPF 专家陷阱 | `.claude/rules/jnpf-expert-traps.md` | 编码陷阱汇总 |
| SQL 安全 | `.claude/rules/sql-safety.md` | 参数化查询 |
| 测试工具链 | `.claude/rules/testing-toolchain.md` | pnpm test:api 等 |
| 前端 SSE/Timer | `.claude/rules/frontend-memory-leak.md` | 内存泄漏防御 |
| Review 工作流 | `.claude/rules/review-workflow.md` | PR 提交流程 |

### 1.2 项目级 Skills（按 Skill 工具调用）

| 目录 | 数量 | 调用方式 |
|------|:----:|----------|
| `.agents/skills/` | 14 | `Skill tool` 直接调用系统级同名 skill |
| `.cursor/skills/` | 26 | `Skill tool` 直接调用系统级同名 skill |
| `.trae/skills/` | 7 | `Skill tool` 直接调用系统级同名 skill |

**Qoder 与 Skill tool 的关系**：系统级 skill（`using-superpowers` / `verification-before-completion` / `brainstorming` / `writing-plans` / `subagent-driven-development` / `test-driven-development` / `systematic-debugging` / `dispatching-parallel-agents` / `requesting-code-review` / `receiving-code-review` / `executing-plans` / `finishing-a-development-branch` / `using-git-worktrees` 等）通过 `Skill` 工具调用即可。  
**项目级同名 skill**（如 `.cursor/skills/dotnet-patterns`、`jnpf-api-cli`、`production-audit` 等）需用 `Read` 工具读取其 `SKILL.md` 后遵循其约束执行。

### 1.3 MCP Servers（项目级 + 用户级）

| Server | 范围 | 用途 |
|--------|------|------|
| **sequential-thinking** | Qoder 用户级 | 思维链推理（多次思考） |
| **interactive-feedback-mcp** | Qoder 用户级 | 交互反馈请求 |
| **browser-use** | Qoder 用户级 | 浏览器自动化（click/drag/fill 等） |
| **chrome-devtools** | Qoder 用户级 + 项目级 `.mcp.json` | Chrome DevTools 桥接（29 个 tools） |
| **genui** | Qoder 用户级 | UI Widget 渲染 |
| **playwright** | Qoder 用户级 + 项目级 `.mcp.json` | Playwright 浏览器自动化 |
| **serena** | Qoder 用户级（默认 disabled）+ 项目级 | C# 符号级精确查询（find_symbol / find_referencing_symbols / find_implementations / get_symbols_overview）|

**注意**：项目配置的 `mcp.json`（无点）和 `opencode.json` 是 Claude Code / OpenCode 的，不被 Qoder 读取。Qoder 读取 `.qoder/settings.json` 和 `.mcp.json`（点开头）。

---

## 2. 硬约束（不可绕过）

### 2.1 针式搜索铁律（必须遵守）

| 你知道什么 | ✅ 正确动作 | ❌ 禁止 |
|---|---|---|
| 已知路径 / 模块文件名 | 直接 Read（大文件带 offset/limit） | 先全仓 Glob/Grep |
| 只知关键词、不知路径 | **一次**精准 Grep（必须带 path 和/或 glob） | 无 path/glob 的全仓扫 |
| C# 类/方法/接口名 | **Serena `find_symbol`**（如可用）/ file-scoped Grep + Read | Shell find；广域 Grep 扫符号 |
| 只知文件名模式 | **窄** Glob（如 `**/PmSkill*.cs`） | `**/*`、`**/*.cs` 拖网 |
| 「这个文件在哪」单点问题 | 窄 Glob 或一次 Grep | 派 explore / Task 子 Agent |

**卡住信号**：单次工具 >15 秒无结果 → 停止盲重试 → 收窄 path/glob 或换 Serena/直接 Read。

### 2.2 工程闭环铁律（WORKFLOW-IRON-01）

**任何工作轮次不得仅以"完成任务步骤"为结束条件，必须以"通过完整工程闭环"为结束条件。**

```
Implementation → Self Evaluation → Self Test → Self Repair → Reviewer Review → Final Report
```

**Superpowers 绑定规则**：

| 工作环节 | 必须调用能力 |
|---------|------------|
| 方案设计 | Architecture / Design Superpower |
| 实现修改 | Implementation / Coding Superpower |
| 自动评估 | Review / Analysis Superpower |
| 自动测试 | Testing / Validation Superpower |
| 自动修复 | Debug / Refactoring Superpower |
| Reviewer 审查 | Independent Review Superpower |

**缺少任一环节：状态不得标记为完成。**

### 2.3 ADF 写入锁（00-constitution.mdc）

`.claude/workflow-state.json` → `adfPhase`：

| adfPhase | 可否写业务 `.cs/.vue` |
|---|---|
| `null`（日常）| 可 |
| `P0`–`P3` | **不可**（仅文档/模板/state/claim）|
| `P4` / `exempt` | 可 |

机器门：`guard-adf-write` L12。

### 2.4 四支柱硬门（00-constitution.mdc）

提交可审批前写 `.claude/pillar-claim-current.json`，执行 `node .claude/hooks/pillar-claim-check.mjs --force`。

四支柱 = 业务功能本体（30§0.11）— **禁止** 纠偏 / 文案 / 单测绿 顶替。

### 2.5 三元组铁律（triple-key-iron-law）

AI 原生开发一切数据/IR/路径/SkillContext MUST 携带 `(tenantId, projectId, pipelineId)` 三元组。  
关系：`1 tenant → N projects → M pipelines`。

---

## 3. AGENTS.md / CLAUDE.md 兼容性

Qoder 自动读取项目根的 `AGENTS.md`、`CLAUDE.md`、`IRON_RULES.md`、`TOOLCHAIN.md`，与本文件**深度合并**。冲突时本文件优先。

---

## 4. Qoder 命令速查

| 命令 | 用途 |
|------|------|
| `/mcp reload` | 重载 MCP 配置（修改 `.mcp.json` 后调用） |
| `/tools` | 查看当前可用 tools |
| `/settings` | 打开设置面板 |
| `/create-skill` | 创建新 Skill |
| `/create-plugin` | 创建 Qoder 插件 |
| `/vercel-deploy` | Vercel 一键部署 |

---

> **维护纪律**：本文件变更需同步更新 `.claude/rules/00-constitution.md` + `.cursor/rules/00-constitution.mdc`。字符上限 100k，**禁止** 长篇复制详规 — 用链接和摘要引导 Qoder 按需拉取。