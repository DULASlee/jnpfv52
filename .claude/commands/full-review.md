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
  prompt: `你是 JNPF 项目的测试验证代理。你的任务是验证当前代码变更不会引入回归。

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

**本次变更文件列表：**
[从 Step 1 获取的文件列表]

**变更内容摘要：**
[从 Step 1 获取的 diff stat]`
})
```

如果 test-runner 返回 FAIL → 立即修复编译错误 → 重新运行 test-runner
如果 test-runner 返回 PASS → 继续 Step 3

### Step 3: 阶段 5 — code-reviewer

使用 Agent tool 启动 code-reviewer 子代理：

```
Agent({
  description: "code-reviewer: 架构合规 + 铁律检查 + 陷阱扫描",
  prompt: `你是 JNPF 项目的代码审查代理。你的任务是对当前代码变更进行严格审查。

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

**本次变更文件列表：**
[从 Step 1 获取的文件列表]

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

汇总以下信息：
- 变更摘要
- 文件变更列表
- 验证结果
- 修复记录
- 剩余风险

将报告保存到 `.claude/memory/decisions.md`（如果是重要变更）。
