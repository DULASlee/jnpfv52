# Review Workflow — 子代理编排规则

> 补充 Default Workflow 中 Step 5 (Test) 和 Step 6 (Self-review) 的自动化细节。

---

## ⚡ 强制触发（AI 每次代码变更后 MUST 自检）

```
写完代码 ≠ 完成。以下任一条件命中，MUST spawn test-runner subagent:

[ ] Write/Edit 累积计数器 ≥ 2（见 CLAUDE.md Review Gate）
[ ] 修改了 2+ 个文件
[ ] 新增/修改了 20+ 行逻辑代码
[ ] 修改了 .cs 文件（后端）
[ ] 修改了 .vue/.ts/.tsx 文件（前端）
[ ] 创建/删除了文件

命中 → 立即 spawn test-runner, 不等用户指示。
全部未命中 → 至少执行 dotnet build + vue-tsc --noEmit。
```

---

## 触发条件

当以下任一条件满足时，MUST 执行完整的三阶段 Review Workflow：

| 条件 | 说明 |
|---|---|
| 任务涉及 2+ 文件修改 | 影响面大，需要系统化验证 |
| 任务涉及 20+ 行逻辑代码 | 复杂度高，需要审查 |
| 用户明确要求 "review" / "审查" / "跑一下测试" | 人工触发 |
| PR 创建前 | 最终把关 |
| 调用 `/full-review` slash command | 一键触发 |

**跳过条件（仅跑 build 验证，不跑子代理）：**
- 单文件 ≤5 行修改（纯样式调整、错字修正）
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

> **占位符（由调用方注入）：**
> - `[文件列表]`：本次变更的文件清单（来自 git diff --name-only）
> - `[diff stat]`：本次变更的统计信息（来自 git diff --stat）

```
你是 JNPF 项目的测试验证代理。你的任务是验证当前代码变更不会引入回归。

**本次变更文件列表：**
[文件列表]

**变更统计：**
[diff stat]

**验证步骤（按顺序执行）：**

1. 后端编译验证
   cd backend && dotnet build application/JNPF.API.Entry/JNPF.API.Entry.csproj
   预期：0 errors
   如果失败：报告具体编译错误，不要继续

2. 前端类型检查（如有前端变更）
   cd jnpf-web-vue3 && npx vue-tsc --noEmit
   预期：0 errors
   如果失败：报告类型错误

3. 变更影响面检查
   对 [文件列表] 中每个 .cs 文件，grep 检查是否有其他文件引用了被修改的方法名
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

> **占位符（由调用方注入）：**
> - `[文件列表]`：本次变更的文件清单
> - `[diff 内容]`：每个变更文件的 diff（用 git diff 获取）

```
你是 JNPF 项目的代码审查代理。你的任务是对当前代码变更进行严格审查。

**本次变更文件列表：**
[文件列表]

**变更内容：**
[diff 内容]

**审查维度：**

1. 架构合规性（对照 `.claude/rules/architecture-redlines.md`）
   - R1: 是否手动创建了 Controller？（应由 IDynamicApiController 自动生成）
   - R2: 是否手动包装 RESTfulResult？（框架自动包装）
   - R4: 新 SqlSugar 查询是否包含 ITenantFilter？（防跨租户泄露）
   - R5: 是否修改了禁用模块（OA）或未创建模块（IoT/MES）？
   - R9: 是否有未申报的架构师指令偏离？
   - R10: 是否发现 BUG 但未执行 Bug Discovery Protocol？

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

5. 错题本纪律（对照 `.claude/rules/workflow.md` Step 6）
   - 本次变更是否包含 fix/bug/错误修复性质的改动？
   - 如有，是否已在 `.claude/memory/mistake-log.md` 追加对应条目？
   - 未追加 → 报告为严重问题（违反 Step 6 错题本强制检查）

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
