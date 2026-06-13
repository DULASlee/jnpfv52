# Default Workflow — Step 1-7 详细说明

> **触发条件**：执行 S 级或 A 级任务时，在对应 Step 加载本文件中的详细说明。主文件中的 Step 标题 + 一句话摘要提供全局视角，执行到具体步骤时按需 Read 本文件获取详细检查清单。
> **主文件引用**：CLAUDE.md § Default Workflow（Step 标题 + 摘要）

---

## Step 1: Understand（理解）

- 重述任务，确认理解正确
- 评估任务分级（S / A / B）
- 与用户确认范围和预期结果
- **不确定就问，不要猜**

---

## Step 2: Scout（侦察）

- Grep/Read 扫描影响面：哪些文件、哪些方法会被影响
- 找到同代码库中类似的**正常工作的**代码作为参考
- 检查近期 git 变更，了解上下文

---

## Step 3: Plan（设计 + 计划）

### S 级任务 — 头脑风暴（硬性要求）

1. 探索项目上下文（文件、文档、近期提交）
2. 逐个提问澄清需求（一次一个问题，优先多选）
3. 提出 2-3 种实现路径 + 推荐方案
4. 分段展示设计，每段获得用户确认
5. 写设计文档 → `docs/superpowers/specs/YYYY-MM-DD-<topic>-design.md`
6. 自检：占位符扫描、内部一致性、范围检查、歧义检查
7. 用户审核设计文档后，进入计划编写

### S 级和 A 级任务 — 编写实施计划

1. 文件结构映射：哪些文件创建、哪些修改、各自职责
2. 任务分解为可独立执行的小步骤（每步 2-5 分钟）
3. 每步包含：精确文件路径、完整代码、精确命令、预期输出
4. 计划写入 → `docs/superpowers/plans/YYYY-MM-DD-<feature-name>.md`
5. 自检：需求覆盖、占位符扫描、类型一致性
6. 与用户确认后进入实施

**B 级任务：** 跳过设计文档和实施计划，在脑中形成方案后直接进入 Step 4。

---

## Step 4: Implement（实施）

### 选择执行方式

```
S 级任务（3+ tasks）→ 子代理驱动（推荐）
  - 每个 Task 派一个子代理
  - 两阶段审查：spec 合规 → 代码质量
  - 连续执行，不暂停确认

S 级任务（<3 tasks）或 A 级任务 → 本会话内执行
  - 按计划步骤逐个执行
  - 每个 Task 标记 in_progress → 完成 → completed

3+ 个独立任务 → 并行子代理
  - 每个子代理独立上下文
  - 完成后检查冲突，运行全量测试
```

### 执行铁律

- 标记 `in_progress` 后再开始，完成后立即标记 `completed`
- 严格按计划步骤执行，不"顺手"改计划外的东西
- 子代理不信任报告：完成后必须独立检查 VCS diff + 验证变更
- 子代理 BLOCKED → 分析原因（缺上下文？能力不足？计划有误？），不盲目重试

---

## Step 5: Test（测试）

- 输出 `🧪 Testing Protocol 启动` 声明
- 运行 `dotnet build`（后端）或 `vue-tsc --noEmit`（前端）
- 运行实际服务或测试命令
- 读完整输出，确认 0 errors
- Bug 修复：复现原始症状 → 确认消失
- **S 级任务：自动触发 test-runner 子代理**
- **A 级任务：代码变更完成后触发 test-runner 子代理（非可选）**
- **B 级任务：至少执行 dotnet build 或 vue-tsc --noEmit**
- Gate Function 全部打勾后才能声称通过

> 详细测试规则见 `.claude/rules/testing-discipline.md`
> 子代理编排规则见 `.claude/rules/review-workflow.md`

---

## Step 6: Self-review（自查）

- `git status` + `git diff` 审查变更
- 对照需求/计划逐项检查完成度
- 架构合规性检查（R1-R5）
- **S 级任务：自动触发 code-reviewer 子代理**
- FAIL → 修复 → 重审（最多 3 轮）
- 3 轮后仍有 FAIL → 报告给用户，请求介入

---

## Step 7: Report（报告）

```
## 完成报告

**变更摘要：** [一句话]

**文件变更：**
| 文件 | 操作 | 行数 |
|---|---|---|

**测试结果：** PASS / FAIL（含证据）

**已知问题：** 无 / [列出]

**剩余工作：** 无 / [列出]
```

重要变更写入 `.claude/memory/decisions.md`。
