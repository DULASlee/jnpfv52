# Section 8 Implementation Proposal

> **本文件性质**：实现建议书（Implementation Proposal），不是代码。
>
> **目标**：将 Section 8 v1.0 设计规格转换为可执行的实施路线 + 工程任务 + 技术边界 + 验证计划。
>
> **上位文档**：[`docs/superpowers/specs/2026-08-30-Section8-Runtime-Architecture-Spec.md`](../specs/2026-08-30-Section8-Runtime-Architecture-Spec.md) **v1.0 FROZEN**
>
> **实施计划**：[`docs/superpowers/plans/2026-08-30-Section8-Runtime-Architecture-Design-Plan.md`](./2026-08-30-Section8-Runtime-Architecture-Design-Plan.md)
>
> **生效日期**：2026-08-30 · **当前状态**：待 Chief Architect 审批
>
> **约束（来自 P0~P4 全部 Constraint）**：
> - Constraint-01~09（详见设计规格 §14.3）
> - **Constraint-10** Implementation Order（新增，禁止 Intelligence 先于 Runtime）
> - **Implementation Entry Rule**（实施人员必读 Section 8 Summary + Constraints + Gate-01 + Anti-Pattern）

---

## 0. 实施入口规则（Implementation Entry Rule）

> **强制**：实施人员必须先阅读以下 4 份文件，然后才能开始编码。

### 必读清单

| # | 文件 | 阅读重点 | 阅读时长 |
|---|------|---------|---------|
| 1 | **Section 8 Summary**（§0 + §1 + §13） | Runtime 是什么 / 不是什么 | 15 min |
| 2 | **Constraints**（§14.3 9 条） | 设计纪律 | 10 min |
| 3 | **Gate-01**（§12） | 5 项证明验证标准 | 15 min |
| 4 | **Anti-Pattern List**（§11） | 6 类禁止模式 | 10 min |

**禁止**：未读完 4 份文件直接进入编码。

---

## 1. 实施阶段拆分（按 Constraint-10 顺序）

### 1.1 总体实施顺序

按 Constraint-10 强制顺序：

```
Kernel (Phase A)
 ↓
State (Phase B)
 ↓
Evidence (Phase C)
 ↓
Persistence Adapter (Phase D)
 ↓
Governance (Phase E)
 ↓
Extension Boundary (Phase F)
 ↓
Loop Execution (Phase G)
 ↓
Future Intelligence (Phase H，Phase 2+)
```

**禁止先实现**：Mode / Knowledge / Tool / LLM（这些属于 Phase H）

### 1.2 Phase 拆分（详细）

#### Phase A: Runtime Kernel（Runtime 内核）

**目标**：建立 Runtime 的核心调度能力

| Task | 输出 | 设计依赖 |
|------|------|---------|
| A-1: Lifecycle Supervisor | Session 全生命周期驱动 | §4 Layer 0 + §6 |
| A-2: State Machine Driver | 8 态状态机 + 转换矩阵 | §3 + §6 |
| A-3: Governance Interceptor | 3 个拦截点（Before/After/OnTransition） | §8 |

**完成标志**：
- Session 可被创建 / 初始化 / 启动
- 状态可转换 + 记录 StateTransitionEvidence
- 三个 Governance 拦截点已就位

#### Phase B: State & Context Layer（状态与上下文）

**目标**：建立 Session 的状态承载能力

| Task | 输出 | 设计依赖 |
|------|------|---------|
| B-1: Session Manager | CRUD Session | §5.3 |
| B-2: Execution Context Manager | Context 字段 + 快照 | §5.4 |
| B-3: Execution State Machine | 5 字段 + 转换证据 | §5.5 |

**完成标志**：
- Session CRUD 完整
- Context 字段可填充 + 快照
- State Machine 8 态转换记录 StateTransitionEvidence

#### Phase C: Evidence & Event Layer（证据与事件）

**目标**：建立可追溯性基础设施

| Task | 输出 | 设计依赖 |
|------|------|---------|
| C-1: Evidence Store | Capture/Query/Export API | §5.8 + §7.4 |
| C-2: EvidenceRecord 7 字段实现 | Source/Decision/Result/CorrelationId 等 | §5.8 |
| C-3: Event Hub | 发布订阅机制 | §5.7 |
| C-4: Audit Trail | 决策链路记录 | §3.5 |

**完成标志**：
- EvidenceRecord 可被 Capture 并持久化
- Event 可被发布与订阅
- Audit Trail 可查询决策链路

#### Phase D: Persistence Adapter（持久化适配器）

**目标**：实现 Persistence Adapter Contract

| Task | 输出 | 设计依赖 |
|------|------|---------|
| D-1: IPersistenceAdapter 接口 | 6 个方法（Save/Load/Checkpoint） | §7.3 |
| D-2: JsonPersistenceAdapter（Phase 1 实现） | JSON 序列化 + 本地文件 | §7.1 |
| D-3: Adapter 责任划分测试 | Runtime 不依赖 JSON | §7.3 责任矩阵 |

**完成标志**：
- IPersistenceAdapter 接口完整
- JsonPersistenceAdapter 可替换（未来 SQLite/Cloud）
- Runtime 不直接引用 JSON 库

#### Phase E: Governance Adapter（治理适配器）

**目标**：实现 Governance Kernel 集成

| Task | 输出 | 设计依赖 |
|------|------|---------|
| E-1: IGovernanceAdapter 接口 | 3 个方法（Before/After/OnTransition） | §8.4 |
| E-2: Governance Kernel 调用实现 | 必须非空 | §8.1 Constraint-09 |
| E-3: GovernanceBlockedException | Blocked 时的异常 | §8.4.3 |

**完成标志**：
- IGovernanceAdapter 不可为空
- Governance Blocked 后 Runtime 抛异常
- 三个拦截点都能调用 Governance

#### Phase F: Extension Boundary（扩展边界）

**目标**：建立 Port + Hook 注册机制

| Task | 输出 | 设计依赖 |
|------|------|---------|
| F-1: 5 类 Port 接口 | Mode/Profile/Knowledge/Governance/Hook | §9.2 Q1 |
| F-2: 三阶段注册机制 | Declaration/Initialization/Runtime | §9.2 Q2 |
| F-3: 7 个 Hook 点 | BeforeObserve/AfterEvaluate/BeforeAct/AfterAct/BeforeReflect/OnFailure/OnStateTransition | §9.2 Q4 |
| F-4: 空实现（NullExtension） | 除 Governance 外的 Port 空实现 | §9.2 Q1 |

**完成标志**：
- 5 个 Port 接口存在
- 7 个 Hook 点可被 Runtime 调用
- Extension 可运行时注册/注销

#### Phase G: Loop Execution（循环执行）

**目标**：建立 8 阶段 Agent Loop

| Task | 输出 | 设计依赖 |
|------|------|---------|
| G-1: Agent Loop Coordinator | 8 阶段调度（Observe→...→Continue/Complete） | §2 + §4 |
| G-2: Action Execution Framework | 调用 Extension 执行 Action | §4 Layer 1 |
| G-3: Reflection Coordinator | 汇集 Reflection 结果 | §4 Layer 1 |

**完成标志**：
- Loop 可启动并按 8 阶段执行
- Action Framework 可调用 Extension
- Reflection 结果可触发下次循环调整

#### Phase H: Future Intelligence（Phase 2+，本期不实现）

明确推迟到 Phase 2+：
- Decision Engine / Reasoning Engine / Domain Analyzer / Code Intelligence
- LLM Provider / Tool Calling / Reflect 算法
- Remote Persistence / Multi-Agent / Dynamic Model Selection

**Constraint-10 再次强调**：Phase 1 不实现 Phase H 任何组件。

---

## 2. 技术决策点（待 Chief Architect 拍板）

下列技术决策点需要 Chief Architect 在 Implementation Proposal 阶段二次拍板：

### 2.1 .NET Runtime 组织方式

| 方案 | 描述 | 优势 | 劣势 |
|------|------|------|------|
| **A. 单 Project 方案** | 全部代码在一个 .csproj | 简单 | 跨边界污染风险 |
| **B. 多 Project 分层** | Kernel/State/Evidence/Persistence/Governance/Extension/Loop 各一个 .csproj | 依赖清晰 + Constraint-08 强约束 | 复杂度上升 |
| **C. Multi-Module** | 类似现有 `zx_lowcode_netcore.sln` 的模块化 | 大型项目友好 | 初期过重 |

**推荐**：B. 多 Project 分层（与设计规格的 Layer Architecture 一致）

### 2.2 DI 边界

| 方案 | 描述 | 与 Constraint 关系 |
|------|------|------------------|
| **A. Microsoft.Extensions.DependencyInjection** | .NET 标准 DI | 主流，但需严格控制 Port 注册 |
| **B. 自建 Service Locator** | Runtime 内手写 | 简单但违反行业惯例 |
| **C. 无 DI（手动构造）** | 直接 `new` | 最简单但测试困难 |

**推荐**：A. .NET DI（必须在 Adapter 层注册 Port，业务代码不依赖具体实现）

### 2.3 Interface Assembly 划分

| 方案 | 描述 | 与 Constraint-08 关系 |
|------|------|---------------------|
| **A. 单 Contracts 程序集** | 全部 Interface 在一个程序集 | 简单但难分层 |
| **B. 按层划分** | Kernel.Abstractions / State.Abstractions / Evidence.Abstractions 各一个 | 强隔离 |
| **C. 仅 Runtime.Abstractions** | Runtime 一个程序集包含所有 | 折中 |

**推荐**：B. 按层划分（与设计规格 Layer Architecture 严格对齐）

### 2.4 Persistence Adapter 位置

| 方案 | 描述 | 与 Constraint-08 关系 |
|------|------|---------------------|
| **A. Runtime.Infra.Persistence.Json** | 在 Runtime 项目内 | 简单但绑定 |
| **B. Runtime.Infra.Persistence（独立）** | 独立 Adapter 项目 | ✅ 满足 Persistence Neutrality |
| **C. 三方库** | NuGet 引入 | 依赖外部 |

**推荐**：B. Runtime.Infra.Persistence（独立 Adapter 项目，Phase 1 实现 JSON Adapter）

### 2.5 Test Strategy

| 层 | 测试方法 | 覆盖率目标 |
|---|---------|---------|
| **Layer 0 Kernel** | xUnit 单元测试 + Mock Governance | > 90% |
| **Layer 1 Loop** | xUnit 集成测试 + 真实 Session | > 80% |
| **Layer 2 State** | xUnit 单元测试 + 内存 Adapter | > 90% |
| **Layer 3 Evidence** | xUnit + 真实 EvidenceStore | > 85% |
| **Layer 4 Extension** | xUnit + NullExtension 测试 | > 75% |
| **Gate-01 验证** | 端到端集成测试 | 100%（必须全部通过） |

### 2.6 Verification Strategy

| 验证层 | 方法 | 频率 |
|-------|------|------|
| **静态分析** | Roslyn Analyzer + 自定义 Iron Law 检测 | 每次 PR |
| **单元测试** | xUnit | 每次 PR |
| **集成测试** | xUnit + 真实 Adapter | 每次 merge |
| **Gate-01 验证** | 端到端 5 项证明 | Phase G 完成后 |
| **Anti-Pattern 扫描** | 自定义脚本（AP-01~06） | 每次 PR |

---

## 3. Gate-01 实现验证计划

Gate-01 验证分两个阶段：

### 3.1 Design Verification（已完成）

Section 8 v1.0 已通过设计层面验证：
- 12 个章节全部完成
- 9 条 Constraint 全部生效
- 3 条 LOCKED Decision 全部冻结

### 3.2 Implementation Verification（待执行）

#### G1: Agent Identity Preservation

**验证方法**：
1. **静态扫描**：执行 Roslyn Analyzer 搜索关键词
   - `switch (intent)` / `case "step_1"` 等硬编码分支
   - `if (step == N)` 模式
2. **动态验证**：执行 100 个不同 Intent，记录执行路径
3. **架构验证**：检查 Runtime 程序集是否依赖 Prompt 模板

**PASS 标准**：
- 静态扫描 0 命中
- 100 个 Intent 执行路径 ≥ 50 种不同路径
- Runtime 不含 PromptTemplate 类型

#### G2: State Preservation

**验证方法**：
1. **动态验证**：模拟完整 Suspend → 关闭 → Resume 序列
2. **状态断言**：检查 State Transition History 连续性
3. **证据断言**：检查 Resume 前后 Evidence 无重复

**PASS 标准**：
- Suspend 后关闭再 Resume，State 完整恢复
- 9 字段 Checkpoint 全部存在
- EvidenceCursor 防止重放

#### G3: Evidence Preservation

**验证方法**：
1. **静态扫描**：检查 EvidenceCapture 调用覆盖率
2. **动态验证**：执行 1000 个 Action，统计 Evidence 数量
3. **覆盖率验证**：每个 Source 都有 Evidence 生成

**PASS 标准**：
- Action 数 == Evidence 数（1:1）
- 7 类 Source 全部覆盖
- Blocked 情况也有 Evidence

#### G4: Governance Enforcement

**验证方法**：
1. **静态扫描**：搜索 `GovernanceAdapter = null` / `SkipGovernance()`
2. **动态验证**：注入 Blocked 场景，验证 Runtime 拒绝执行
3. **路径验证**：Extension 程序集不依赖 GovernanceKernel

**PASS 标准**：
- 静态扫描 0 命中
- Blocked 后 Runtime 抛 GovernanceBlockedException
- Extension 程序集无法直接调用 Governance Kernel

#### G5: Extension Preservation

**验证方法**：
1. **静态扫描**：检查 Runtime 依赖 Extension 实现细节
2. **动态验证**：运行时移除 Extension，Runtime 仍能工作（带警告）
3. **Port 验证**：每个 Port 接口存在空实现 + 真实实现

**PASS 标准**：
- Runtime 不依赖 Extension 细节
- Extension 移除后 Runtime 降级而非崩溃
- 5 个 Port 都有空 + 真实实现

### 3.3 Gate-01 通过条件

```
G1 PASS
  +
G2 PASS
  +
G3 PASS
  +
G4 PASS
  +
G5 PASS
  =
Gate-01 ✅
```

**Gate-01 失败不允许"临时豁免"**进入下一阶段。

---

## 4. 风险与缓解措施

### 4.1 已识别风险

| 风险 | 概率 | 影响 | 缓解措施 |
|------|:----:|:----:|---------|
| 实施人员跳过必读文件 | 中 | 高 | Implementation Entry Rule + PR 检查清单 |
| Phase A Kernel 实现时间超预期 | 中 | 中 | 优先核心 5 文件，Hook 留 Phase F |
| Persistence Adapter 实现复杂 | 低 | 中 | JsonPersistenceAdapter 简单实现优先 |
| Governance Kernel 调用延迟 | 低 | 中 | Mock Governance Kernel 用于测试 |
| Gate-01 验证失败 | 中 | 高 | 5 项验证分阶段跑，失败立即停 |

### 4.2 监控指标

| 指标 | 目标 |
|------|------|
| Phase A 完成时间 | ≤ 2 周 |
| Gate-01 全部通过 | 100% |
| Iron Law 静态扫描命中 | 0 |
| Anti-Pattern 命中 | 0 |
| 关键单元测试覆盖率 | ≥ 85% |

---

## 5. 项目结构（建议）

```
backend/
└── modules/
    └── mod-runtime/                          # 独立 Runtime 模块
        ├── Runtime.Core/                     # Layer 0 + Layer 1
        │   ├── Kernel/                       # Phase A
        │   ├── State/                        # Phase B
        │   ├── Evidence/                     # Phase C
        │   └── Loop/                         # Phase G
        │
        ├── Runtime.Abstractions/             # 所有 Interface（按层划分）
        │   ├── Kernel/
        │   ├── State/
        │   ├── Evidence/
        │   ├── Persistence/
        │   ├── Governance/
        │   └── Extension/
        │
        ├── Runtime.Infra.Persistence/        # Phase D（独立 Adapter）
        │   └── JsonPersistenceAdapter/
        │
        ├── Runtime.Infra.Governance/         # Phase E
        │   └── GovernanceAdapter/
        │
        ├── Runtime.Extensions/               # Phase F（空实现 + 测试）
        │   └── NullExtension/
        │
        └── Runtime.Tests/                    # 所有测试 + Gate-01 验证
            ├── UnitTests/
            ├── IntegrationTests/
            └── Gate01Verification/
```

---

## 6. 实施时间估算

| Phase | 工作量 | 累计 |
|-------|-------|------|
| A: Runtime Kernel | 1.5 周 | 1.5 周 |
| B: State & Context | 1 周 | 2.5 周 |
| C: Evidence & Event | 1 周 | 3.5 周 |
| D: Persistence Adapter | 0.5 周 | 4 周 |
| E: Governance Adapter | 0.5 周 | 4.5 周 |
| G: Loop Execution | 1.5 周 | 6 周 |
| F: Extension Boundary | 1 周 | 7 周 |
| **Gate-01 验证** | **1 周** | **8 周** |

**总估算**：约 8 周完成 Section 8 Phase 1 Runtime MVP（不含后续 Phase H Intelligence）

---

## 7. 关键决策点（待 Chief Architect 拍板）

| # | 决策点 | 推荐方案 | 等待拍板 |
|---|--------|---------|---------|
| D1 | .NET Runtime 组织方式 | B. 多 Project 分层 | ⏳ |
| D2 | DI 框架选择 | A. Microsoft.Extensions.DependencyInjection | ⏳ |
| D3 | Interface Assembly 划分 | B. 按层划分 | ⏳ |
| D4 | Persistence Adapter 位置 | B. 独立 Adapter 项目 | ⏳ |
| D5 | 是否引入第三方 LLM（即使 Phase 1 不用） | **否**（Phase 1 完全不引入） | ⏳ |
| D6 | 是否复用现有 JNPF 模块 | **否**（Runtime 独立） | ⏳ |
| D7 | 测试框架 | xUnit（与 JNPF 一致） | ⏳ |
| D8 | Mock 框架 | Moq 或 NSubstitute | ⏳ |

---

## 8. 约束与依赖

### 8.1 上位约束

| 约束来源 | 约束内容 |
|---------|---------|
| Section 8 v1.0 | 9 条 Constraint + 3 条 LOCKED Decision |
| baseline v2.1 | 14 条 IRON + HIP-01 + WORKFLOW-IRON-01 |
| .claude/rules/ | 项目级铁律 |

### 8.2 依赖关系

```
Section 9 (Mode System)
   依赖：Section 8 Extension Boundary + Port
Section 10 (Profile System)
   依赖：Section 8 Extension Boundary + Profile Loader Port
Section 11 (Knowledge System)
   依赖：Section 8 Knowledge Router Adapter Port
Section 12 (Validation & Evidence)
   依赖：Section 8 Evidence Store + Capture Framework
```

---

## 9. 后续动作

### 9.1 本阶段交付物

- [x] Section 8 Implementation Proposal（本文件）
- [x] 7 Phase 实施路线（A~G）
- [x] 8 个技术决策点
- [x] Gate-01 实现验证计划
- [x] 风险登记表

### 9.2 Chief Architect 待审批

1. **Section 8 v1.0 设计冻结**（已完成，本次复审）
2. **Implementation Proposal 批准**
3. **8 个技术决策点拍板**（D1~D8）
4. **Phase A 启动授权**

### 9.3 批准后执行顺序

```
1. Chief Architect 拍板 D1~D8
   ↓
2. 建立模块结构（backend/modules/mod-runtime/）
   ↓
3. Phase A: Runtime Kernel 开始
   ↓
4. 每个 Phase 完成后跑 Iron Law 静态检查
   ↓
5. Phase G 完成后跑 Gate-01 全部 5 项验证
   ↓
6. Gate-01 全部 PASS → Phase 1 MVP 正式完成
```

---

> **下一步动作**：等待 Chief Architect 对 Implementation Proposal 审批与 8 个技术决策点拍板。