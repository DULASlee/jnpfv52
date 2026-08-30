# UEEA Agent Runtime Implementation Iron Laws

> **Agent 实现铁律 — 防止 MVP / 重构 / 优化过程中将 Agent Runtime 退化为 Workflow / Prompt Chain**
>
> 本文档是 **HOW（实施约束）**，配套的 **WHAT（设计决策）** 见 [`docs/superpowers/specs/2026-08-30-universal-dotnet-refactor-agent-design-baseline.md`](../superpowers/specs/2026-08-30-universal-dotnet-refactor-agent-design-baseline.md) §15。
>
> **生效日期**：2026-08-30（v2.0 Major Revision 同期）
> **适用阶段**：Phase 1-5 全部 Runtime 实现、MVP 评估、代码评审、架构升级
> **约束等级**：**最高**（违反任一 = 立即停工 + Capability Impact Analysis）

---

## 0. 元数据

| 字段 | 值 |
|------|---|
| **版本** | v1.0（首发：2026-08-30）|
| **作者** | Chief Architect |
| **维护者** | UEEA Runtime 工作组 |
| **上位决策** | baseline IRON-01 ~ IRON-12（v2.0.x）|
| **配套章节** | Section 8 Runtime Architecture + Section 9 MVP Design |
| **修订方式** | 追加式（修改需提交 Change Request）|

---

## 1. 总体原则（IRON-00 隐含基础）

### Capability First, Implementation Later（能力优先，实现后置）

Agent Runtime 的实现可以**逐步简化**，但**禁止降低核心 Agent 能力模型**。

**允许降低**：

```
✅ 降低基础设施复杂度（不引入 Vector DB，用 JSON 替代）
✅ 降低并发能力（v0.1 单 Worker）
✅ 降低存储规模（v0.1 File-based，v0.2 SQLite）
✅ 降低 Adapter 数量（v0.1 仅 Qoder）
✅ 降低部署复杂度（v0.1 单进程）
```

**禁止降低**：

```
❌ 删除 Agent Loop（IRON-02）
❌ 删除状态模型（IRON-05）
❌ 删除规划能力（IRON-03）
❌ 删除 Evidence 反馈机制（IRON-07）
❌ 删除 Governance 校验（IRON-08）
❌ 删除恢复能力（IRON-06）
❌ 删除任务依赖模型（IRON-03）
```

**黄金原则**：

> **简化实现 ≠ 简化 Agent 能力**

---

## 2. IRON-01：禁止将 Agent Runtime 降级为固定流程引擎

### 原则

Agent Runtime 不允许退化为：

```
Input → 固定步骤 → LLM 调用 → Output
```

该模式属于 **LLM Workflow**，不是 **Agent Runtime**。

### 正确的 Agent Runtime 形态

```
Goal
 ↓
Mission Understanding
 ↓
Planning
 ↓
Action
 ↓
Observation
 ↓
Validation
 ↓
Reflection
 ↓
Next Action（动态决定）
```

### 判定标准

如果实现过程中发现 Runtime **无法根据执行结果调整下一步行为**，则视为 Agent 能力缺失。

### 反模式检测

```javascript
// scripts/check-iron-01.mjs
const code = readRuntimeSource();
if (code.includes('for (const step of fixedSteps)')) {
  fail('IRON-01 违反：固定循环结构，不是 Agent Runtime');
}
```

### 与现有决策关系

- 强化 **R-04** Planner 四层模型
- 强化 **Section 8.3** Agent Loop 8 步循环

---

## 3. IRON-02：Agent Loop 不允许被简化删除

### 原则

**标准 Agent Loop 6 步**：

```
Plan → Execute → Observe → Validate → Reflect → Continue
```

任何版本必须**完整保留**全部 6 个能力。

### 能力-允许-禁止矩阵

| 能力 | 是否允许删除 |
|------|------------|
| **Plan**（规划）| ❌ 禁止 |
| **Execute**（执行）| ❌ 禁止 |
| **Observe**（观察）| ❌ 禁止 |
| **Validate**（验证）| ❌ 禁止 |
| **Reflect**（反思）| ❌ 禁止 |
| **Continue**（继续）| ❌ 禁止 |

### 允许的简化

```
✅ 简化实现方式（如 Reflect 可以是简单的状态合并）
✅ 降低执行器能力（如 v0.1 无并行）
✅ 单 Worker 执行
```

### 禁止的简化

```
❌ 改成一次性 Prompt 调用（"AI: 一次性回答问题"）
❌ 改成固定脚本流程（"if-else 链"）
❌ 去除 Reflect 环节（最常见反模式）
❌ 把 Validate 移到 Loop 末尾（违反"每步验证"原则）
```

### 反模式检测

```javascript
// scripts/check-iron-02.mjs
const requiredSteps = ['plan', 'execute', 'observe', 'validate', 'reflect', 'continue'];
const runtimeSteps = extractLoopSteps(runtimeSource);
for (const step of requiredSteps) {
  if (!runtimeSteps.includes(step)) {
    fail(`IRON-02 违反：缺少 Loop 步骤 "${step}"`);
  }
}
```

### 与现有决策关系

- 强化 **Section 8.3** 8 步循环（Plan/Execute/Observe/Validate/Reflect/Continue/Complete）
- 强化 **V-04** 15 道 Gates（Validate 必须在每步）

---

## 4. IRON-03：Task Graph 不得退化为任务列表

### 原则

**正确**：

```
Mission → Task Graph → Execution DAG → Result
```

**禁止**：

```
Mission → Task Array → for loop 执行
```

### 原因

Task List 无法表达：

- ❌ 依赖关系（dependency）
- ❌ 条件执行（conditional execution）
- ❌ 失败恢复（failure recovery）
- ❌ 并行扩展（parallel extension）
- ❌ 动态调整（dynamic adjustment）

### v0.1 允许

```
Execution DAG
      ↓
Single Worker Scheduler（v0.1）
```

### v0.1 禁止

```
Execution DAG
      ↓
Linear Pipeline
      ↓
for循环
```

### 强制保留的 DAG 状态

```
✅ dependency（依赖关系）
✅ priority（优先级）
✅ execution state（执行状态）
✅ failure state（失败状态）
✅ retry state（重试状态）
```

### 反模式检测

```javascript
// scripts/check-iron-03.mjs
const schedulerCode = readSchedulerSource();
if (schedulerCode.includes('for (const task of tasks)') &&
    !schedulerCode.includes('dependency') &&
    !schedulerCode.includes('retry')) {
  fail('IRON-03 违反：Task List 不是 DAG');
}
```

### 与现有决策关系

- 强化 **R-05** Planner 四层模型
- 强化 **M-05** Planner 接口契约

---

## 5. IRON-04：简化调度不简化执行模型

### 原则

调度器实现可以简化（v0.1 串行），但**执行模型**必须完整保留。

### 调度演化路径

```
v0.1：DAG → Sequential Scheduler（可接受）
v0.2：DAG → Parallel Scheduler（异步）
v0.3：DAG → Distributed Scheduler（多进程）
```

### 必须保留的 5 个状态

每个 Task 节点必须显式包含：

| 状态 | 含义 |
|------|------|
| **dependency** | 依赖哪些 Task |
| **priority** | 优先级（用于调度排序）|
| **execution state** | pending / running / completed / failed |
| **failure state** | retry / skip / abort |
| **retry state** | retry_count / max_retries / next_retry_at |

### 禁止

```
❌ DAG → List → for 循环（即使看起来"能跑"）
❌ 合并 execution + failure 状态为单一 status
❌ 不记录 retry_count
```

### 与现有决策关系

- 强化 **IRON-03**（执行模型）
- 强化 **M-07** v0.1 串行 + 重试 3 次

---

## 6. IRON-05：Agent 状态必须显式存在

### 原则

**禁止**：

```
Prompt 上下文 = Agent 状态
```

**正确**：

Agent 必须独立维护 **6 个 State**：

```
Session State
Task State
Context State
Evidence State
Decision State
Audit State
```

### 原因

没有显式 State：

```
❌ 无法恢复（State 没有持久化点）
❌ 无法审计（State 无法被第三方验证）
❌ 无法长期运行（State 会丢失）
❌ 无法跨 Session（State 绑定单次 Prompt）
❌ 无法验证行为（State 无法对比预期）
```

### 强制要求

每个 State 必须：

```
✅ 独立数据结构（class / interface / schema）
✅ 独立持久化能力（checkpoint）
✅ 独立版本化（随 Session 演进）
✅ 独立审计（V-08 Audit Trail）
```

### 反模式检测

```javascript
// scripts/check-iron-05.mjs
const requiredStates = ['SessionState', 'TaskState', 'ContextState', 'EvidenceState', 'DecisionState', 'AuditState'];
const runtimeStates = extractStateClasses(runtimeSource);
for (const state of requiredStates) {
  if (!runtimeStates.includes(state)) {
    fail(`IRON-05 违反：缺少 State "${state}"`);
  }
}
```

### 与现有决策关系

- 强化 **R-04** Agent State Model
- 强化 **Section 8.4** 6 个状态详细定义

---

## 7. IRON-06：长任务必须可恢复

### 原则

Agent Runtime 必须支持：

```
Start → Checkpoint → Pause → Resume → Continue
```

**禁止**：

```
任务中断 → 全部重新开始
```

### Checkpoint 必含

```
✅ Session 元数据
✅ 6 个 State 完整快照
✅ 当前 Task 进度
✅ Evidence 累积
✅ 已做 Decision
✅ Audit Trail 增量
```

### Pause/Resume 要求

```
✅ Pause 命令：保存完整 State + 退出 Runtime
✅ Resume 命令：从 checkpoint 还原 6 个 State + 继续 Task
✅ Resume 必须校验 State 完整性（V-08 Audit）
✅ Resume 失败 → 提示用户 + 安全降级
```

### 应用场景

```
- 长时间分析任务（> 1 小时）
- 跨工作日任务
- LLM API rate limit 中断
- 用户临时切换 Profile
- 多 Session 协调（Phase 5）
```

### 与现有决策关系

- 强化 **R-09** Session 持久化支持跨 Session 恢复
- 强化 **M-06** Session 状态机 + Checkpoint + Resume

---

## 8. IRON-07：Evidence 必须影响 Agent 行为

### 原则

Evidence **不允许**只是最终报告附件。

**错误**：

```
Agent 执行
   ↓
生成报告
   ↓
附加 Evidence（仅作为报告参考）
```

**正确**：

```
Execute
   ↓
Evidence（参与判断）
   ↓
Validate（基于 Evidence 校验）
   ↓
Reflect（更新下一步行为）
   ↓
调整下一步规划
```

### Evidence 必须参与的决策点

| 决策点 | Evidence 作用 |
|--------|--------------|
| **Gate 校验** | Evidence 是否充分（D-31 Threshold）|
| **Risk 评估** | Evidence 决定 Risk 等级 |
| **下一步规划** | Evidence 决定是否继续 / 切换 / 停止 |
| **方案选择** | Evidence 决定最小修改 vs 大改 |

### 禁止

```
❌ Evidence 仅在报告里出现，不参与 Runtime 决策
❌ "先把任务跑完，最后整理 Evidence"
❌ Evidence 缺失不阻塞 Runtime（必须 NEED_EVIDENCE 状态）
```

### 反模式检测

```javascript
// scripts/check-iron-07.mjs
const evidenceCode = findEvidenceUsage(runtimeSource);
if (!evidenceCode.includes('evidence.validate') &&
    !evidenceCode.includes('evidence.gate')) {
  fail('IRON-07 违反：Evidence 未参与决策');
}
```

### 与现有决策关系

- 强化 **D-15** Evidence First Reasoning
- 强化 **D-31** Evidence Sufficiency Threshold
- 强化 **R-04** EvidenceState 必须影响 Reflect

---

## 9. IRON-08：Runtime 不拥有治理权

### 原则

**Runtime 负责**：

```
✅ 执行（Execute）
✅ 状态管理（State Management）
✅ 调度（Scheduling）
✅ 反馈（Feedback）
```

**Runtime 不负责**：

```
❌ 定义规则（Define Rules）
❌ 修改规则（Modify Rules）
❌ 绕过规则（Bypass Rules）
❌ 覆盖规则（Override Rules）
```

### 所有关键行为

**必须经过**：

```
Runtime
   ↓
Governance Kernel
   ↓
Decision
```

**禁止**：

```
Runtime
   ↓
直接执行（绕过 Governance）
```

### Governance 集成点（5 个）

| Runtime 动作 | Governance 调用 |
|-------------|----------------|
| 加载 Profile | D-14 Validation Gate |
| 切换 Mode | L-05 双重授权检查 |
| 加载 Knowledge | D-28 Evidence Compatibility |
| 推进 Task | D-15 Evidence First + D-31 Threshold |
| 生成 Report | V-01 ~ V-09 全部 Gates |

### 反模式检测

```javascript
// scripts/check-iron-08.mjs
const runtimeImports = extractImports(runtimeSource);
if (!runtimeImports.includes('governance-kernel')) {
  fail('IRON-08 违反：Runtime 未引用 Governance Kernel');
}
```

### 与现有决策关系

- 强化 **R-NG-04** Runtime 不替代 Governance Kernel
- 强化 **R-02** Runtime 不允许直接运行任何决策

---

## 10. IRON-09：禁止永久禁止代码修改能力

### 原则

Runtime MVP **不直接修改生产代码**（Phase 1 范围限制）。

但**架构必须预留**：

```
Audit Mode
Verify Mode
Assist Mode
Execute Mode
```

未来 Execute Mode 必须满足：

```
Change Proposal
   ↓
Evidence
   ↓
Governance Check
   ↓
Human Approval
   ↓
Controlled Execution
```

### MVP 必须预留的扩展点

```
✅ Mode 系统支持 Audit/Verify/Assist/Execute 四档
✅ Capability Matrix 含 Execute Mode 工具白名单（即使 v0.1 默认禁用）
✅ Scope Declaration 接口（含 Positive/Negative Scope）
✅ Authorization Context 接口（含 user_confirmed + scope_approved）
✅ Execute Authorization Flow 7 步骤占位
```

### 禁止

```
❌ MVP 删除 Execute Extension Point
❌ 将 Runtime 定义成"永久只读工具"
❌ 移除 Mode 切换机制
❌ 不预留 Authorization 框架
```

### 与现有决策关系

- 强化 **D-20** Agent Does Not Own Repository History
- 强化 **D-22** Explicit Change Boundary
- 强化 **D-26** Command Policy Layer

---

## 11. IRON-10：Knowledge 不允许无治理增长

### 原则

**禁止**：

```
一次经验 → 永久知识
```

**必须**：

```
Project Memory
   ↓
Validation
   ↓
Knowledge Promotion
```

### Knowledge 升级路径

```
✅ Capture（捕获）
✅ Validate（至少 2 个项目验证）
✅ Cross-Context Check（D-30）
✅ Conflict Resolution（与现有 Knowledge 不冲突）
✅ Version Management（v1.0 → v1.1 → v2.0）
✅ Promote to Knowledge Memory
```

### Runtime 必须支持的检查

```
✅ Evidence 验证（每条 Knowledge Claim 必须有 E2+ 证据）
✅ 冲突检查（与既有 Knowledge 不矛盾）
✅ 版本管理（同一 topic 多个版本可追溯）
✅ Promotion Gate（D-30 5 项全部满足）
```

### 禁止

```
❌ Runtime 静默写入 Knowledge Memory
❌ 跳过 D-30 Validation 直接晋升
❌ Knowledge 增长无版本控制
❌ Knowledge 增长无审计日志
```

### 与现有决策关系

- 强化 **R-NG-02** Runtime 不负责 Knowledge 创建
- 强化 **D-30** Knowledge Promotion Cross-Context Validation

---

## 12. IRON-11：MVP 不能成为最终架构陷阱

### 原则

任何 MVP 决策必须回答 4 个问题：

```
1. 是否保留未来扩展点？
2. 是否保持能力模型？
3. 是否可以平滑升级？
4. 是否会导致重新设计？
```

如果答案：

```
需要推翻当前设计才能升级
```

则该 MVP 方案 **禁止采用**。

### 反模式示例

```
❌ v0.1 用 Thread 跑 Runtime → v0.2 改 Process → v0.3 改 Distributed
   （每个版本都要重写）
❌ v0.1 用 JSON State → v0.2 用 SQLite → v0.3 用 PostgreSQL
   （如果 schema 不兼容就要迁移）
❌ v0.1 单 Adapter → v0.2 多 Adapter（如果 Adapter 接口不兼容就要重写）
```

### 正确做法

```
✅ v0.1 Adapter 接口预留 → v0.2 加 Adapter 实现无需改 Runtime
✅ v0.1 State 抽象层预留 → v0.2 切换后端无需改 Session Manager
✅ v0.1 单 Worker Scheduler 接口预留 → v0.2 切换 Parallel 无需改 Planner
```

### MVP 决策检查清单

```markdown
## MVP Decision Review

### 决策内容
<简述 MVP 决策>

### 4 项检查
- [ ] 未来扩展点是否保留？
- [ ] 能力模型是否完整？
- [ ] 是否可平滑升级？
- [ ] 是否会导致重新设计？

### 升级路径
<描述从 v0.1 到 v0.2 到 v0.3 的平滑过渡方案>

### 风险评估
<可能的架构陷阱>

### 批准
- [ ] 架构师签字
- [ ] Lead Engineer 签字
- [ ] Architecture Change Record 已创建
```

### 与现有决策关系

- 强化 **PV-3** Phase 0-5 Exit Gate 强制
- 强化 **Section 9.1** MVP Scope Boundary

---

## 13. IRON-12：禁止未经批准的能力削减

### 原则

AI 工程师在实现过程中 **禁止自行**：

```
❌ 删除接口（即使"看起来没用"）
❌ 合并状态（即使"看起来重复"）
❌ 简化数据模型（即使"看起来冗余"）
❌ 移除验证流程（即使"看起来低效"）
❌ 替换 DAG 为列表（即使"v0.1 简单点"）
❌ 删除 Audit（即使"占空间"）
❌ 删除 Evidence（即使"暂时用不上"）
```

### 必须提交

任何修改必须提交：

```
Capability Impact Analysis

包含：
1. 删除什么能力？
2. 为什么必要？
3. 是否影响未来扩展？
4. 是否需要 Architecture Change Record？
```

### Capability Impact Analysis 模板

```markdown
## Capability Impact Analysis

### 删除的能力
<例如：删除 Retry State>

### 删除原因
<例如：v0.1 简化实现>

### 影响分析
- 当前用户：<列出受影响场景>
- 未来扩展：<列出受影响升级路径>
- 治理约束：<列出违反的铁律>

### 替代方案
<例如：v0.2 重新加入>

### 风险评估
<例如：v0.2 升级时需要重构 Scheduler>

### 批准
- [ ] 架构师签字
- [ ] Lead Engineer 签字
- [ ] Architecture Change Record ID: <CR-XXX>
```

### 审批流程

```
1. AI 工程师提交 Capability Impact Analysis
2. 架构师 Review（必须明确批准 / 拒绝）
3. 若批准 → 创建 Architecture Change Record
4. CR 进入 baseline 决策表（追加 / 修改）
5. 实施 + 验证
```

### 禁止

```
❌ AI 工程师自行决定"简化"任何能力
❌ 在 PR 中无说明删除接口
❌ "等发现有问题再加回来"的侥幸心理
❌ 不写 Capability Impact Analysis 直接重构
```

### 与现有决策关系

- 强化 **IRON-00** 简化实现 ≠ 简化能力
- 强化 **PV-3** Phase Exit Gate 强制

---

## 14. 验收标准

### 14.1 Iron Laws 自检清单（每个 MVP 版本必跑）

```bash
# IRON-01 ~ IRON-12 自检
bash scripts/check-iron-laws.sh

# 期望输出：
# ✅ IRON-01: PASS（无固定循环结构）
# ✅ IRON-02: PASS（Loop 6 步完整）
# ✅ IRON-03: PASS（DAG 5 状态完整）
# ✅ IRON-04: PASS（执行模型完整）
# ✅ IRON-05: PASS（6 个 State 显式）
# ✅ IRON-06: PASS（Checkpoint/Resume 支持）
# ✅ IRON-07: PASS（Evidence 参与决策）
# ✅ IRON-08: PASS（Runtime 引用 Governance）
# ✅ IRON-09: PASS（Execute Extension Point 预留）
# ✅ IRON-10: PASS（Knowledge 治理流程）
# ✅ IRON-11: PASS（MVP 升级路径明确）
# ✅ IRON-12: PASS（Capability Impact Analysis 流程）
```

### 14.2 Phase 1 MVP DoD

在 §9.9 v0.1 Acceptance Criteria 基础上，**额外必须满足**：

```
✅ IRON-01 ~ IRON-12 全部 PASS
✅ Capability Impact Analysis 流程已建立
✅ Iron Laws 自检脚本已集成到 CI
✅ Architecture Change Record 流程已建立
```

### 14.3 评审检查点

| 评审场景 | 必须审查的 Iron Law |
|---------|-------------------|
| Code Review（任何 PR）| IRON-02 / IRON-05 / IRON-08 |
| 架构评审（任何 Decision）| IRON-01 / IRON-03 / IRON-09 / IRON-11 |
| MVP 验收 | IRON-01 ~ IRON-12 全部 |
| 性能优化 | IRON-04（不允许砍 DAG 状态）|
| 安全审查 | IRON-08 / IRON-10 |

---

## 15. 修订流程

### 15.1 修订类型

| 类型 | 含义 | 流程 |
|------|------|------|
| **追加** | 新增 Iron Law（如未来 IRON-13）| 提交 Change Request → 架构师批准 |
| **细化** | 现有 Iron Law 增加细则 | 提交 Change Request → 架构师批准 |
| **修订** | 修改现有 Iron Law 内容 | 提交 Change Request → **必须 3 人评审** |
| **废弃** | 删除现有 Iron Law | **不允许**（Iron Law 是约束，永不废弃） |

### 15.2 Change Request 模板

```markdown
## Iron Law Change Request

### 变更类型
<追加 / 细化 / 修订>

### 目标 IRON
<IRON-XX 或 "新增 IRON-NN">

### 当前内容
<引用现有>

### 变更内容
<描述变更>

### 变更理由
<业务 / 架构 / 安全>

### 影响分析
- 受影响的 Section：<列出>
- 受影响的决策：<列出>
- 受影响的代码：<列出>

### 批准
- [ ] 架构师签字
- [ ] Lead Engineer 签字
- [ ] （修订时）3 人评审记录
```

---

## 16. 与其他文档的关系

### 16.1 上位文档

```
docs/superpowers/specs/2026-08-30-universal-dotnet-refactor-agent-design-baseline.md
├── §15 Implementation Iron Laws（决策索引）
├── IRON-01 ~ IRON-12（12 条决策）
└── Section 8-12（Runtime 设计与 MVP）
```

### 16.2 配套文档

```
docs/harness/类级重构专家Agent封装手册.md
├── §0 元数据（v2.0.x）
├── §5 实施步骤（必须遵循 Iron Laws）
└── §8 DoD（包含 Iron Law 自检）

docs/harness/UEEA-Runtime-MVP-实施手册.md（未来）
├── 完整 Runtime MVP 实施细节
└── 必须引用 Iron Laws 作为最高约束
```

### 16.3 引用约定

任何 Runtime 实现 / 评审 / 优化文档必须显式声明：

```
本文档遵循 IRON-01 ~ IRON-12。
如需突破任何 Iron Law，必须提交 Capability Impact Analysis + Architecture Change Record。
```

---

## 附录 A：Iron Laws 速查表

| IRON | 一句话核心 | 反模式 |
|------|----------|--------|
| IRON-01 | 不降级为 Workflow | 固定步骤 + LLM 调用 |
| IRON-02 | Loop 6 步不删除 | 一次性 Prompt |
| IRON-03 | DAG 不是 List | Task Array + for |
| IRON-04 | 调度简化 ≠ 执行模型简化 | 砍 DAG 状态 |
| IRON-05 | 6 State 显式存在 | Prompt 上下文 = State |
| IRON-06 | 长任务可恢复 | 中断重头开始 |
| IRON-07 | Evidence 参与决策 | Evidence 仅报告附件 |
| IRON-08 | Runtime 无治理权 | Runtime 绕过 Governance |
| IRON-09 | 预留 Execute Extension | 删除 Execute 扩展点 |
| IRON-10 | Knowledge 治理增长 | 一次经验 = 永久知识 |
| IRON-11 | MVP 不成陷阱 | 升级需推翻重设计 |
| IRON-12 | 禁止擅自削减能力 | 简化掉不写 CIA |

---

## 附录 B：完整 Capability Impact Analysis 模板

参见 §13 IRON-12 详细字段。完整 .md 模板另行维护。

---

## 附录 C：检查脚本（伪代码）

```bash
#!/bin/bash
# scripts/check-iron-laws.sh

echo "=== IRON Laws 自检 ==="

bash scripts/check-iron-01-loop-structure.sh
bash scripts/check-iron-02-loop-steps.sh
bash scripts/check-iron-03-dag-shape.sh
bash scripts/check-iron-04-execution-model.sh
bash scripts/check-iron-05-state-explicit.sh
bash scripts/check-iron-06-recovery-support.sh
bash scripts/check-iron-07-evidence-in-decision.sh
bash scripts/check-iron-08-governance-import.sh
bash scripts/check-iron-09-execute-extensionpoint.sh
bash scripts/check-iron-10-knowledge-governance.sh
bash scripts/check-iron-11-mvp-upgrade-path.sh
bash scripts/check-iron-12-capability-impact.sh

echo "=== 全部 PASS ==="
```

---

> **下次更新触发**：新增 Iron Law（如 IRON-13+）/ Phase 2 实施发现需细化 / Phase 3 评审需修订。

> **维护纪律**：每次更新本文档主体，必须同步更新 `docs/superpowers/specs/2026-08-30-universal-dotnet-refactor-agent-design-baseline.md` §15 决策索引。