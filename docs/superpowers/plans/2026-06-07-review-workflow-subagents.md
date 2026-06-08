# Review Workflow 子代理编排 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 在 JNPF 开发环境中建立 test-runner + code-reviewer 子代理自动编排体系，实现"代码完成后自动验证 + 自动审查 + 自动修复"的闭环。

**Architecture:** 采用 CLAUDE.md 规则驱动（方式 2）+ Slash Command 手动触发（方式 4）的组合方案。CLAUDE.md 中的规则定义触发条件和子代理 prompt，slash command 提供关键节点的一键入口。不使用 Hook 强制触发，避免死循环风险。

**Tech Stack:** Claude Code Agent tool、CLAUDE.md rules、`.claude/commands/` slash commands、`.claude/rules/` rule files

---

## File Structure

| 文件 | 职责 | 操作 |
|---|---|---|
| `.claude/rules/review-workflow.md` | 子代理编排规则：何时触发、prompt 模板、结果处理 | **Create** |
| `.claude/commands/full-review.md` | `/full-review` slash command：一键跑完三阶段 | **Create** |
| `CLAUDE.md` | 在 Default Workflow 和 On-Demand Rules 中引用新规则 | **Modify** |

---

### Task 1: 创建 `.claude/rules/review-workflow.md`

**Files:**
- Create: `.claude/rules/review-workflow.md`

这是核心规则文件。它定义：
1. 何时自动触发子代理
2. test-runner 子代理的 prompt 模板
3. code-reviewer 子代理的 prompt 模板
4. 主 Claude 如何处理 review 反馈

- [ ] **Step 1: 创建规则文件**

```bash
cat > D:/JNPF-v52/.claude/rules/review-workflow.md << 'RULES_EOF'
# Review Workflow — 子代理编排规则

> 补充 Default Workflow 中 Step 5 (Test) 和 Step 6 (Self-review) 的自动化细节。

---

## 触发条件

当以下任一条件满足时，MUST 执行完整的三阶段 Review Workflow：

| 条件 | 说明 |
|---|---|
| 任务涉及 3+ 文件修改 | 影响面大，需要系统化验证 |
| 任务涉及 50+ 行逻辑代码 | 复杂度高，需要审查 |
| 用户明确要求 "review" / "审查" / "跑一下测试" | 人工触发 |
| PR 创建前 | 最终把关 |
| 调用 `/full-review` slash command | 一键触发 |

**跳过条件（仅跑 build 验证，不跑子代理）：**
- 单文件 ≤10 行修改（bug fix、样式调整、文档更新）
- 仅修改 `.md`、`.json`、配置文件

---

## 阶段 4: test-runner 子代理

**触发方式：** 在 Default Workflow Step 5 之后自动触发，或由 `/full-review` 手动触发。

**Agent 配置：**
```
subagent_type: "general-purpose"
run_in_background: false  （需要结果才能继续）
```

**Prompt 模板：**

```
你是 JNPF 项目的测试验证代理。你的任务是验证当前代码变更不会引入回归。

**验证步骤（按顺序执行）：**

1. 后端编译验证
   cd D:/JNPF-v52/backend && dotnet build application/JNPF.API.Entry/JNPF.API.Entry.csproj
   预期：0 errors
   如果失败：报告具体编译错误，不要继续

2. 前端类型检查（如有前端变更）
   cd D:/JNPF-v52/jnpf-web-vue3 && npx vue-tsc --noEmit
   预期：0 errors
   如果失败：报告类型错误

3. 变更影响面检查
   运行 git diff --name-only 查看变更文件
   对每个变更的 .cs 文件，grep 检查是否有其他文件引用了被修改的方法名
   报告潜在的调用方影响

**输出格式（严格遵守）：**

## 测试报告

| 验证项 | 结果 | 详情 |
|---|---|---|
| 后端编译 | PASS/FAIL | 错误信息 |
| 前端类型检查 | PASS/FAIL/SKIP | 错误信息 |
| 影响面分析 | PASS/WARNING | 受影响文件列表 |

**结论：** PASS（可继续）/ FAIL（必须修复）

**关键发现：** 列出所有发现的问题，按严重程度排序
```

---

## 阶段 5: code-reviewer 子代理

**触发方式：** test-runner 通过后自动触发，或由 `/full-review` 手动触发。

**Agent 配置：**
```
subagent_type: "general-purpose"
run_in_background: false
```

**Prompt 模板：**

```
你是 JNPF 项目的代码审查代理。你的任务是对当前代码变更进行严格审查。

**审查维度：**

1. 架构合规性（对照 CLAUDE.md Architecture Redlines）
   - R1: 是否手动创建了 Controller？（应由 IDynamicApiController 自动生成）
   - R2: 是否手动包装 RESTfulResult？（框架自动包装）
   - R4: 新 SqlSugar 查询是否包含 ITenantFilter？（防跨租户泄露）
   - R5: 是否修改了禁用模块（OA）或未创建模块（IoT/MES）？

2. 工程铁律合规性（对照 Engineering Iron Laws）
   - 是否有 TODO / TBD / "fix later" 注释？
   - 是否有吞没异常的 try-catch？
   - 是否有未验证的假设（"应该可以"、"理论上"）？

3. JNPF 专家陷阱检查（对照 .claude/rules/jnpf-expert-traps.md）
   - Trap 2: Mapster Adapt() 是否覆盖了审计字段？
   - Trap 3: 列表查询是否有 N+1 风险（导航属性未 eager load）？
   - Trap 4: Oops.Bah vs Oops.Oh 使用是否正确？
   - Trap 6: 方法名是否有 Async 后缀？（不应有）
   - Trap 8: Updateable/Deleteable 是否显式指定了 TenantId？
   - Trap 9: 公共方法是否都是 intended API endpoints？

4. 代码质量
   - 方法是否超过 50 行？（应拆分）
   - 是否有重复代码？（DRY）
   - 命名是否符合 PascalCase（C#）/ camelCase（字段）？
   - 是否有硬编码的魔法数字/字符串？

**审查范围：** 只审查本次变更的文件，不审查未修改的代码。

**输出格式（严格遵守）：**

## 代码审查报告

### 严重问题（必须修复）
| # | 文件:行号 | 问题 | 违反规则 | 建议修复 |
|---|---|---|---|---|
| 1 | path/file.cs:42 | 描述 | R4/Trap7 | 具体修复代码 |

### 潜在问题（建议修复）
| # | 文件:行号 | 问题 | 风险等级 | 建议 |
|---|---|---|---|---|

### 优点
- 列出做得好的地方（如有）

**结论：** PASS（0 严重问题）/ FAIL（有严重问题，必须修复）
```

---

## 阶段 6: 主 Claude 处理 Review 反馈

**这不是子代理，而是主 Claude 的行为规则。**

当 code-reviewer 返回 FAIL 时，主 Claude MUST：

1. **立即修复所有严重问题** — 不等用户指示
2. **修复后重新触发 test-runner** — 验证修复没有引入新问题
3. **重新触发 code-reviewer** — 确认严重问题已清零
4. **最多循环 3 次** — 如果 3 次后仍有严重问题，报告给用户并停止

**循环终止条件：**
- code-reviewer 返回 PASS → 进入报告阶段
- 循环 3 次仍有 FAIL → 报告剩余问题，请求用户介入
- test-runner 返回 FAIL → 先修编译错误，再继续

**最终报告模板：**

```
## Review 完成报告

**变更摘要：** [一句话描述本次变更]

**文件变更：**
| 文件 | 操作 | 行数 |
|---|---|---|
| path/file.cs | Modified | +15 -3 |

**验证结果：**
- 后端编译：PASS
- 前端类型检查：PASS/SKIP
- 代码审查：PASS（经 N 轮修复）

**修复记录：**
1. [问题描述] → [修复方式]

**剩余风险：** 无 / [列出未修复的潜在问题]
```

---

## 子代理 Prompt 模板使用方式

主 Claude 在触发子代理时，使用 Agent tool 的 prompt 参数传入上述模板，并追加：

```
**本次变更文件列表：**
[从 git diff --name-only 获取]

**变更内容摘要：**
[从 git diff 获取关键变更]
```

这样子代理就有足够上下文进行针对性验证/审查。
RULES_EOF
```

- [ ] **Step 2: 验证文件已创建**

```bash
cat D:/JNPF-v52/.claude/rules/review-workflow.md | head -5
```

Expected: 文件前 5 行包含 "# Review Workflow"

- [ ] **Step 3: Commit**

```bash
cd D:/JNPF-v52 && git add .claude/rules/review-workflow.md
git commit -m "feat(rules): add review-workflow sub-agent orchestration rules"
```

---

### Task 2: 创建 `/full-review` Slash Command

**Files:**
- Create: `.claude/commands/full-review.md`

Slash command 提供一键触发完整三阶段流程的入口。

- [ ] **Step 1: 创建 commands 目录和文件**

```bash
mkdir -p D:/JNPF-v52/.claude/commands
cat > D:/JNPF-v52/.claude/commands/full-review.md << 'CMD_EOF'
# /full-review — 完整三阶段代码审查

执行完整的 Review Workflow：验证 → 审查 → 修复循环。

## 执行步骤

### Step 1: 收集变更上下文

运行以下命令收集本次变更信息：
```bash
git diff --name-only HEAD~1 HEAD
git diff --stat HEAD~1 HEAD
```

如果有未提交的变更，也纳入审查范围：
```bash
git diff --name-only
git diff --stat
```

### Step 2: 阶段 4 — test-runner

使用 Agent tool 启动 test-runner 子代理：

```
Agent({
  description: "test-runner: 后端编译 + 前端类型检查 + 影响面分析",
  prompt: `[Review Workflow 规则中的 test-runner prompt 模板]

**本次变更文件列表：**
[Step 1 获取的文件列表]

**变更内容摘要：**
[Step 1 获取的 diff stat]`
})
```

如果 test-runner 返回 FAIL → 立即修复编译错误 → 重新运行 test-runner
如果 test-runner 返回 PASS → 继续 Step 3

### Step 3: 阶段 5 — code-reviewer

使用 Agent tool 启动 code-reviewer 子代理：

```
Agent({
  description: "code-reviewer: 架构合规 + 铁律检查 + 陷阱扫描",
  prompt: `[Review Workflow 规则中的 code-reviewer prompt 模板]

**本次变更文件列表：**
[Step 1 获取的文件列表]

**变更内容：**
[读取每个变更文件的 diff 内容，提供给 reviewer]`
})
```

### Step 4: 阶段 6 — 处理反馈

如果 code-reviewer 返回 FAIL：
1. 立即修复所有严重问题
2. 重新运行 test-runner（Step 2）
3. 重新运行 code-reviewer（Step 3）
4. 最多循环 3 次

如果 code-reviewer 返回 PASS → 继续 Step 5

### Step 5: 输出最终报告

使用 Review Workflow 规则中的最终报告模板，汇总：
- 变更摘要
- 文件变更列表
- 验证结果
- 修复记录
- 剩余风险

将报告保存到 `.claude/memory/decisions.md`（如果是重要变更）。
CMD_EOF
```

- [ ] **Step 2: 验证文件已创建**

```bash
cat D:/JNPF-v52/.claude/commands/full-review.md | head -5
```

Expected: 文件前 5 行包含 "# /full-review"

- [ ] **Step 3: Commit**

```bash
cd D:/JNPF-v52 && git add .claude/commands/full-review.md
git commit -m "feat(commands): add /full-review slash command for 3-phase review"
```

---

### Task 3: 更新 CLAUDE.md — 集成 Review Workflow

**Files:**
- Modify: `CLAUDE.md` — Default Workflow Step 5/6 增强 + On-Demand Rules 引用

- [ ] **Step 1: 在 Default Workflow 中增强 Step 5 和 Step 6**

在 CLAUDE.md 的 `## Default Workflow` 部分，将现有的单行描述：

```
1. Understand (restate & confirm) → 2. Scout (Grep/Read to map impact surface) → 3. Plan (confirm before implementing) → 4. Implement (small steps, frequent verification) → 5. Test (run tests + run service) → 6. Self-review (git status + code review) → 7. Report (what done / changed / results / remaining)
```

替换为：

```
1. Understand (restate & confirm)
2. Scout (Grep/Read to map impact surface)
3. Plan (confirm before implementing)
4. Implement (small steps, frequent verification)
5. Test (run tests + run service) → **复杂任务自动触发 test-runner 子代理**
6. Self-review (git status + code review) → **复杂任务自动触发 code-reviewer 子代理，FAIL 时自动修复循环（最多 3 轮）**
7. Report (what done / changed / results / remaining)

> 子代理编排详细规则见 `.claude/rules/review-workflow.md`
> 手动触发完整审查：使用 `/full-review` slash command
```

- [ ] **Step 2: 在 On-Demand Rules 中添加 Review Workflow 引用**

在 CLAUDE.md 的 `## On-Demand Rules` 部分，在现有规则之后追加：

```
WHEN 完成涉及 3+ 文件或 50+ 行代码的变更 => Read `.claude/rules/review-workflow.md` 并执行三阶段审查
WHEN 用户要求 "review" / "审查" / "跑测试" => 执行 `.claude/rules/review-workflow.md` 中的完整流程
```

- [ ] **Step 3: 验证 CLAUDE.md 修改正确**

```bash
grep -n "review-workflow" D:/JNPF-v52/CLAUDE.md
```

Expected: 至少 2 行匹配（Default Workflow 引用 + On-Demand Rules 引用）

- [ ] **Step 4: Commit**

```bash
cd D:/JNPF-v52 && git add CLAUDE.md
git commit -m "docs(CLAUDE.md): integrate review-workflow into Default Workflow and On-Demand Rules"
```

---

### Task 4: 端到端验证 — 用 CancellationToken 变更测试完整流程

**Files:**
- 无新文件，使用已有变更验证流程

用本次会话中已完成的 CancellationToken 变更作为测试案例，验证 review-workflow 的三个阶段。

- [ ] **Step 1: 收集变更上下文**

```bash
cd D:/JNPF-v52 && git diff --name-only HEAD~3 HEAD
git diff --stat HEAD~3 HEAD
```

Expected: 包含 IUsersService.cs, UsersService.cs, DepartmentService.cs, RoleService.cs 等文件

- [ ] **Step 2: 手动触发 test-runner 子代理**

使用 Agent tool，prompt 按 `.claude/rules/review-workflow.md` 中的 test-runner 模板构造。

验证点：
- 后端编译 PASS（0 errors）
- 影响面分析列出所有调用了变更方法的文件

- [ ] **Step 3: 手动触发 code-reviewer 子代理**

使用 Agent tool，prompt 按 `.claude/rules/review-workflow.md` 中的 code-reviewer 模板构造。

验证点：
- 检查 CancellationToken 是否正确添加（无遗漏、无多余）
- 检查接口与实现是否同步
- 检查是否有 Trap 6（Async 后缀）违规

- [ ] **Step 4: 处理 review 反馈（如有）**

如果 code-reviewer 返回严重问题，修复后重新触发 test-runner + code-reviewer。

- [ ] **Step 5: 输出最终报告**

按 Review Workflow 的最终报告模板输出，确认流程跑通。

---

## Self-Review Checklist

**1. Spec coverage:**
- [x] 阶段 4 (test-runner) → Task 1 prompt 模板 + Task 2 Step 2
- [x] 阶段 5 (code-reviewer) → Task 1 prompt 模板 + Task 2 Step 3
- [x] 阶段 6 (处理反馈) → Task 1 循环规则 + Task 2 Step 4
- [x] 自动触发条件 → Task 1 触发条件表
- [x] 手动触发入口 → Task 2 slash command
- [x] 集成到现有 workflow → Task 3 CLAUDE.md 修改

**2. Placeholder scan:**
- [x] 无 TBD/TODO
- [x] 所有 prompt 模板都是完整可用的
- [x] 所有文件路径都是精确的

**3. Type consistency:**
- [x] test-runner prompt 中的命令与 CLAUDE.md Build & Run 一致
- [x] code-reviewer 检查项与 jnpf-expert-traps.md 一致
- [x] 触发条件与增量规则中的"复杂任务规划增强"一致
