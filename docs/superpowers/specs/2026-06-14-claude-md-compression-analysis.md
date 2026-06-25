# CLAUDE.md 压缩精简分析报告

> **提交给：** 首席架构师审批
> **日期：** 2026-06-14
> **分析对象：** `D:\JNPF-v52\CLAUDE.md`（562 行，17 个主章节）

---

## 一、问题诊断

### 1.1 核心矛盾

CLAUDE.md 的目标是让 AI 遵循规则，但其**长度本身就与可靠性成反比**：

- **562 行** → 在长会话中会被上下文压缩截断
- 关键指令（如"触发 test-runner"）埋在第 437 行，压缩后最先丢失
- 大量内容（Debugging Discipline 105 行、Testing Discipline 70 行、Default Workflow 143 行）**每次会话都加载**，但实际只在特定场景使用

### 1.2 当前结构问题

```
CLAUDE.md 562 行
├── ⚡ SESSION ACTIVATION        13 行  ← 新增，正确
├── Core Identity                 4 行  ← 正确
├── Architecture Redlines         7 行  ← 正确
├── Engineering Iron Laws        57 行  ← 🔴 应提取
├── Debugging Discipline        105 行  ← 🔴 应提取
├── Testing Discipline           70 行  ← 🔴 应提取
├── Proactive Behavior           10 行  ← 保留
├── Communication & Refusal       5 行  ← 保留
├── Build & Run                  11 行  ← 保留
├── Architecture                  8 行  ← 保留
├── Database                      5 行  ← 保留
├── Agent Toolchain              13 行  ← 🟡 可精简
├── Default Workflow            143 行  ← 🔴 应提取
├── On-Demand Rules              17 行  ← ✅ 正确的索引模式
├── Technical Preferences         3 行  ← 保留
└── 增量规则                      45 行  ← 🟡 部分重复
```

**结论：** 562 行中有 **375 行（67%）属于"特定场景触发"内容，不应常驻主文件。**

---

## 二、逐章节分析

### 2.1 Engineering Iron Laws（57 行）— 建议提取

| 子节 | 行数 | 触发频率 | 建议 |
|---|---|---|---|
| Law 1: No Escalation | 3 | 每次任务 | ✅ 保留 1 句摘要 |
| Law 2: Gate Function 5 步 | 10 | 声称完成时 | 🔴 提取到 `.claude/rules/engineering-iron-laws.md` |
| Law 2: 验证要求表 | 9 | 声称完成时 | 🔴 同上 |
| Law 2: 红旗词清单 | 6 | 声称完成时 | 🔴 同上 |
| Law 2: 合理化借口表 | 8 | 声称完成时 | 🔴 同上 |
| Law 3: Honest Reporting | 2 | 偶尔 | ✅ 保留 1 句 |
| Law 4: No Shortcuts | 2 | 每次 | ✅ 保留 1 句 |

**保留在主文件：**
```
## Engineering Iron Laws (详见 .claude/rules/engineering-iron-laws.md)
1. No Escalation: Fix ALL errors immediately, NEVER deflect
2. Verification is Completion: NO completion claims without fresh verification evidence → Gate Function 5 步
3. Honest Reporting: If uncertain, say so — don't fabricate
4. No Shortcuts: NEVER TODO, pseudo-implement, swallow exceptions, or skip boundary cases
```

### 2.2 Debugging Discipline（105 行）— 建议完整提取

这是最典型的 on-demand 内容。已有触发条件：

> WHEN 遇到 bug / 测试失败 / 异常行为 / 编译错误 → 回到上方 Debugging Discipline

**问题：** 105 行内容每次会话都加载，但只在 bug 出现时使用（约 10-20% 的会话）。

**建议：** 完整提取到 `.claude/rules/debugging-discipline.md`

保留在主文件的只有：
```
## Debugging (详见 .claude/rules/debugging-discipline.md)
铁律: NO FIXES WITHOUT ROOT CAUSE INVESTIGATION FIRST
遇到 bug → Phase 1 根因调查 → Phase 2 模式分析 → Phase 3 假设测试 → Phase 4 修复
3 次修复失败 → 停下来质疑架构，讨论后再继续
```

### 2.3 Testing Discipline（70 行）— 建议完整提取

同 Debugging Discipline，已有触发条件：

> WHEN 准备声称"完成"/"通过"/"修复" → 回到上方 Law 2 Gate Function

**建议：** 提取到 `.claude/rules/testing-discipline.md`

保留在主文件的只有：
```
## Testing (详见 .claude/rules/testing-discipline.md)
铁律: NO TASK IS COMPLETE WITHOUT RUNNING THE ACTUAL TEST COMMAND
完成前 Gate Function 5 项自检 → 全部打勾才能声称完成
```

### 2.4 Default Workflow（143 行）— 建议拆分

这是最大的章节。Step 1-7 的详细说明 + 执行路径 + 子代理策略。

| 子节 | 行数 | 每次都需要？ | 建议 |
|---|---|---|---|
| 任务分级表 | 6 | ✅ 每次 | 保留 |
| 强制声明模板 | 6 | ✅ 每次 | 保留 |
| Step 1-4 详细说明 | 64 | ❌ 按需 | 提取 |
| Step 5-7 详细说明 | 30 | ❌ 按需 | 提取 |
| 执行路径速查 | 10 | ✅ 每次 | 保留 |
| 子代理执行策略 | 27 | ❌ S 级任务 | 提取 |

**建议：** Step 1-7 详细说明提取到 `.claude/rules/default-workflow.md`

保留在主文件的只有任务分级 + 执行路径 + 关键触发规则。

### 2.5 增量规则（45 行）— 去重 + 合并

| 子节 | 行数 | 问题 | 建议 |
|---|---|---|---|
| 跨会话记忆规范 | 8 | 与 CLAUDE.md Step 7 重复 | 合并 |
| 禁止推脱补充 | 6 | 与 Law 1 语义重叠 | 合并到 Law 1 |
| 项目健康验证 | 8 | 与 Testing 重叠 | 合并到 Testing |
| 安全知识库 | 3 | 独立 | ✅ 保留 |
| 前端 UI 品味 | 10 | 已通过 `jnpf-ui-enhance` 管理 | ✅ 保留 |

### 2.6 Agent Toolchain（13 行）— 可精简

当前是表格 + 说明。4 个工具中只有 2 个活跃使用（superpowers, Serena），可简化。

### 2.7 应保留在主文件的内容（已正确）

这些章节简短且每次都需要，无需改动：

- Core Identity（4 行）
- Architecture Redlines R1-R5（7 行）— 安全红线，不可移除
- Proactive Behavior（10 行）— 行为速查表
- Communication & Refusal（5 行）
- Build & Run（11 行）
- Architecture（8 行）
- Database（5 行）
- On-Demand Rules（17 行）— 正确的索引模式 ✅
- Technical Preferences（3 行）

---

## 三、压缩方案

### 3.1 目标结构

```
CLAUDE.md  ~180 行（从 562 行压缩 68%）
├── ⚡ SESSION ACTIVATION            13 行
├── Core Identity                     4 行
├── Architecture Redlines R1-R5       7 行
├── Iron Laws (1 sentence each)      10 行
├── Proactive Behavior               10 行
├── Communication & Refusal           5 行
├── Build & Run                      11 行
├── Architecture                      8 行
├── Database                          5 行
├── Agent Toolchain (精简)            8 行
├── Workflow (任务分级 + 执行路径)    25 行
├── Debugging (1 段摘要)              8 行
├── Testing (1 段摘要)                8 行
├── On-Demand Rules INDEX            20 行
└── Technical Preferences + 增量      15 行
```

### 3.2 提取目标文件

| 新文件 | 来源 | 预计行数 | On-Demand 触发条件 |
|---|---|---|---|
| `.claude/rules/engineering-iron-laws.md` | Law 2 详细内容 | ~50 | WHEN 声称完成 / 准备提交 |
| `.claude/rules/debugging-discipline.md` | Debugging 全文 | ~105 | WHEN 遇到 bug / 异常 |
| `.claude/rules/testing-discipline.md` | Testing 全文 | ~70 | WHEN 声称完成 |
| `.claude/rules/default-workflow.md` | Step 1-7 详细 | ~120 | WHEN S级/A级任务 |
| `.claude/rules/incremental-rules.md` | 增量规则 | ~40 | WHEN 会话结束 / 安全任务 |

这 5 个文件**已经存在于 On-Demand Rules 的触发体系中**，只是内容还嵌在 CLAUDE.md 里。

### 3.3 关键设计决策

**原则：CLAUDE.md = AI 每次必读的最小集。On-Demand Rules = 按场景触发读取。**

| 内容 | 保留在 CLAUDE.md | 提取到 rules/ |
|---|---|---|
| "遇到 bug 怎么办" | 1 句摘要 + 文件引用 | 完整 4 阶段流程 |
| "如何验证完成" | Gate Function 5 步名称 | 详细表格、红旗清单 |
| "任务怎么分级" | 分级表 + 执行路径 | Step 1-7 详细说明 |
| "禁止行为" | 4 条铁律（各 1 句） | 详细案例、反例 |

---

## 四、实施风险与缓解

| 风险 | 影响 | 缓解 |
|---|---|---|
| AI 不主动读 on-demand 文件 | 规则未加载，行为退化 | On-Demand Rules 已有触发体系 + SESSION ACTIVATION 强迫检查 |
| 提取后文件被忽略 | 规则形同虚设 | 主文件保留 1 句摘要 + 明确引用路径，触发条件用 WHEN 格式 |
| 上下文压缩仍截断主文件 | 180 行也可能被截 | SESSION ACTIVATION 块放在最顶部，用代码块包裹（更难压缩） |
| 规则文件版本漂移 | 主文件和子文件不一致 | 单一来源原则：规则只存在于一个文件中 |

---

## 五、建议审批事项

1. **是否同意将 CLAUDE.md 从 562 行压缩到 ~180 行？**
2. **是否同意提取 5 个 on-demand 规则文件？**
3. **是否同意 Engineering Iron Laws 只保留 4 句话在主文件，详细内容提取？**
4. **Debugging/Testing Discipline 的详细流程是否完整提取？**
5. **Default Workflow Step 1-7 详细说明是否提取？**

---

## 六、附录：当前 CLAUDE.md 章节占比

```
SESSION ACTIVATION    13  ██
Core Identity          4  █
Architecture Redlines   7  █
Engineering Iron Laws  57  ██████████
Debugging Discipline  105  ██████████████████
Testing Discipline     70  ████████████
Proactive Behavior     10  ██
Communication           5  █
Build & Run            11  ██
Architecture            8  █
Database                5  █
Agent Toolchain        13  ██
Default Workflow      143  █████████████████████████
On-Demand Rules        17  ███
Tech Preferences        3  █
增量规则                45  ████████
                      ---
                      562  (67% 属于 on-demand 内容)
```
