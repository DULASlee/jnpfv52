# Review Workflow — JNPF 项目级（覆盖全局）

> 本文件补充全局 `~/.claude/rules/review-workflow.md`，把三阶段审查的 subagent_type 指向 JNPF 专属 agent。
> 全局文件保留通用 prompt 模板；本项目优先用本文件的 dispatch 路由。

## 三阶段审查（JNPF 专属 dispatch）

### Stage 1: jnpf-tester 子 agent（替换通用 test-runner）

**Agent:** `subagent_type: "jnpf-tester"`
**何时跳过：** 仅改 `.md`/`.json`/配置文件 → 跳过（build-only 验证即可）。
**Prompt 模板：**

```
验证当前代码变更不引入回归。变更类型：{后端 .cs / 前端 .vue.ts / API·Skill·IR / Bug 回归}。
变更文件：{git diff --name-only 输出}。
按你的决策矩阵跑 Dev Loop，返回 fugu/test-report-v1 JSON。
```

**回退：** `verdict: FAIL` → 主 Claude 据 `failed_checks[].suggested_fix` 决定回退 Coder 或 dispatch jnpf-debugger。

### Stage 2: code-reviewer 子 agent（保持通用）

**Agent:** `subagent_type: "code-reviewer"`（通用，未替换）
架构/安全/质量审查，按全局模板 + `.claude/rules/reviewer-discipline.md` 维度。

### Stage 3: 失败回退 → jnpf-debugger（替换 general-purpose）

当 Stage 1 反复 FAIL 或 Stage 2 发现需运行时定位的问题：

**Agent:** `subagent_type: "jnpf-debugger"`
**Prompt 模板：**

```
诊断以下问题，抓运行时数据定位根因，返回 debug report：
- 症状：{具体描述}
- 已尝试：{修复历史}
- 触发来源：{Stage 1 FAIL / Stage 2 发现 / 用户手动}
```

**返回：** debug report → 主 Claude 持久化到 `workspace/debug_report.md` → 交还 Coder 执行修复。

## 触发条件

执行完整三阶段审查当 ANY of：3+ 文件修改 / 50+ 行逻辑代码 / 用户要求 review / 提 PR 前 / `/full-review`。

**跳过条件（build-only，无 subagent）：** 单文件 ≤10 行 / 仅 `.md`/`.json`/配置。
