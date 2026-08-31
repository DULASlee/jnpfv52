# L1 — Project Rules（项目级规则）

> **层级：** 项目特有的编码规范和最佳实践
> 
> **执行层：** L1 警告（L2 约定）
> 
> **来源：** 所有 L1 规则的内容存储在 `GOVERNANCE-INDEX.md` 引用的源文件中

---

## 1. 架构设计

### L1-01: ADF 三先行

**来源：** `.claude/rules/architecture-design-interface-first.md`

**规则：** 架构 → 设计模式 → 接口契约 → 实现

**强制要求：**
- P0: Business First Q1-Q3
- P1: 层边界、唯一源、三元组、≥2 方案+failure_boundary
- P2: 模式映射 SkillHarness/Gate/IR/IDynamicApiController
- P3: 签名/DTO/事件契约，禁止方法体
- P4: 实现 + 节点审批

---

### L1-02: 架构红线

**来源：** `.claude/rules/architecture-redlines.md`

**规则：** R1-R12 架构约束清单

**关键红线：**
- R1: Never write Controllers
- R2: Unified Response with RESTfulResult
- R3: Codegen boundary - bugs in generated code → fix .vm template
- R4: Multi-tenant isolation
- R5: Module Boundary (OA disabled, IoT/MES not exist)
- R6: Frontend Memory Safety
- R7: SQL Injection Defense
- R8: API Permission Declaration
- R9-R12: 架构完整性约束

---

### L1-03: 断言纪律

**来源：** `.claude/rules/assertion-discipline.md`

**规则：** Tag claims with [KNOWN]/[COMPUTED]/[INFERRED]/[GUESS] + confidence

**强制要求：**
- [KNOWN] - 已验证的事实
- [COMPUTED] - 计算得出的结论
- [INFERRED] - 推断得出的结论
- [GUESS] - 猜测（需标注置信度）

---

## 2. 工作流程

### L1-10: WORKFLOW-IRON-01

**来源：** `.claude/rules/workflow-iron-law.md`

**规则：** 自主工程闭环 4 环节强制执行

**强制要求：**
- Superpowers 驱动
- 自主连续执行
- 阶段闭环验证
- 质量审查
- 交付汇报

---

### L1-11: HIP-01

**来源：** `.claude/rules/agent-runtime-iron-laws.md`

**规则：** Human Interrupt Policy

**强制要求：**
- 默认连续，仅 4 类情况停
- 关键节点不可被 HIP-01 绕过

---

### L1-12: 工作汇报规范

**来源：** `.claude/rules/ai-work-report-iron-law.md`

**规则：** 六维结构化汇报

**强制要求：**
1. 做了什么（事实）
2. 发现了什么（洞察）
3. 意味着什么（判断）
4. 建议什么（建议）
5. 证据在哪（证据）
6. 风险在哪（风险）

---

### L1-13: 节点审批门禁

**来源：** `.claude/rules/implementation-integrity-iron-law.md`

**规则：** 每个功能节点完成后必须暂停等待审批

**强制要求：**
- 业务实现说明
- 代码质量自检
- 业务功能证据
- 验收标准对照
- 未经审批不得进入下一节点

---

## 3. 需求分析

### L1-20: 需求分析铁律

**来源：** `.claude/rules/req-analysis-iron-law.md`

**规则：** 阶段 A-B-C 为唯一施工依据

**强制要求：**
- 禁止新增 .mjs 脚本
- 数据一致性：IR=Write Model
- 逐阶段推进
- 以阶段 A-B-C 为总纲

---

### L1-21: 交互式澄清

**来源：** `.claude/rules/studio-clarification.md`

**规则：** 结构化选择题让用户细化需求

**强制要求：**
- 每轮 3-5 题
- Required 问题必须回答才能推进
- ClarificationRequested/ClarificationAnswered 事件可审计

---

## 4. 测试与验证

### L1-30: 测试工具链

**来源：** `.claude/rules/testing-toolchain.md`

**规则：** E2E 分层工具链

**分层：**
- Vitest 快断言（首选）
- Playwright UI 测试
- 长链 mjs（evidence）

---

### L1-31: 测试纪律

**来源：** `.claude/rules/testing.md`

**规则：** Phase 1 验证测试标准

---

### L1-32: Review 工作流

**来源：** `.claude/rules/review-workflow.md`

**规则：** 代码审查流程

---

## 5. 调试与修复

### L1-40: 系统调试

**来源：** `.claude/rules/debugging.md`

**规则：** 系统化调试流程

**流程：**
1. Root Cause Investigation
2. Pattern Analysis
3. Hypothesis and Testing
4. Implementation

---

### L1-41: Reviewer 纪律

**来源：** `.claude/rules/reviewer-discipline.md`

**规则：** 独立审查员行为规范

---

## 6. Studio 特定

### L1-50: S2 Compile 主链

**来源：** `.claude/rules/studio-s2-compile.md`

**规则：** compile 模式 vs agent 模式边界

**强制要求：**
- 生产主链默认 compile
- S2 期间禁止写 sa_*
- sa_* 九表由 SaMaterializer 物化

---

### L1-51: Eval Pipeline

**来源：** `.claude/rules/studio-eval-pipeline.md`

**规则：** 四层评估管线

**分层：**
- L1 组件 / L2 轨迹 / L3 任务（确定性）
- L4 业务（LLM fast tier）

---

### L1-52: 阶段验收测试

**来源：** `.claude/rules/fullchain-sprint-iron-law.md`

**规则：** F1-F4 全链条冲刺铁律

---

## 关联文档

- `GOVERNANCE-INDEX.md` — 完整规则映射表
- `.claude/rules/` — 规则源文件目录
