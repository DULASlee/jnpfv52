# FSPM MCP 完整系统实施计划 V6.1

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 从基线 `968a27d8` 出发，按 P5→P15 连续施工，交付 AI 可通过 MCP 真正操作 FSPM 的可执行系统并封存。

**Architecture:** 三 Tool 共用执行管线（Validate→Context→Workspace→Gateway→Project→Evidence→Response），经唯一网关边界调用 Core/Compiler 能力；MCP 自身不实现语义、变更、验证中的任何一项。

**Tech Stack:** .NET 8（`net8.0`，SDK 由 `backend/global.json` 锁定 8.0.424）/ MCP C# SDK 2.2.0 / xUnit 2.6.2 + Microsoft.NET.Test.Sdk 17.8.0 / PowerShell 执行构建验证。

**配套规格：** `docs/superpowers/spec/FSPM-MCP完整系统实施设计规格V6.1.md`（节点编号与本计划一一对应，含 V6.1-01~08 修正）。

**工程纪律（本项目覆盖 skill 默认）：** 禁用流程性 TDD，采用"先实现 → xUnit 覆盖核心产出物"，每节点 Code+Test+Evidence 三件套齐全方算完成；`dotnet build`/`dotnet test` 全程在 Windows PowerShell 执行。

---

## 0. 文件结构图（本计划锁定的分解）

### 0.1 新建（MCP 工程）

```text
backend/modularity/Foundry.FSPM.Mcp/
├── Execution/McpExecutionContext.cs ............. P6（MCP-06-01）
├── Execution/McpExecutionContextFactory.cs ...... P6（MCP-06-01）
├── Execution/McpExecutionPipeline.cs ............ P6（MCP-06-07）
├── Execution/McpOperationResult.cs .............. P6（MCP-06-03）
├── Validation/IMcpRequestValidator.cs ........... P6（MCP-06-02）
├── Validation/McpRequestValidator.cs ............ P6（MCP-06-02）
├── Errors/IMcpExceptionMapper.cs ................ P6（MCP-06-04）
├── Errors/McpExceptionMapper.cs ................. P6（MCP-06-04）
├── Workspace/IMcpWorkspaceResolver.cs ........... P6（MCP-06-05）
├── Workspace/McpWorkspaceResolver.cs ............ P6/P7（MCP-06-05/07-01）
├── Workspace/ISolutionResolver.cs ............... P7（MCP-07-02）
├── Workspace/IProjectResolver.cs ................ P7（MCP-07-03）
├── Workspace/CompilationIdentity.cs ............. P7（MCP-07-05）
├── Gateways/ISemanticGateway.cs ................. P6（MCP-06-06）
├── Gateways/IConstructionGateway.cs ............. P6（MCP-06-06）
├── Gateways/IVerificationGateway.cs ............. P6（MCP-06-06）
├── Gateways/ICompilationProvider.cs ............. P7（MCP-07-04）
├── Tools/Requests/UnderstandRequest.cs .......... P8（MCP-08-01）
├── Tools/Requests/ConstructRequest.cs ........... P9（MCP-09-01）
├── Tools/Requests/VerifyRequest.cs .............. P11（MCP-11-01）
├── Mapping/SemanticQueryParser.cs ............... P8（MCP-08-02）
├── Mapping/SemanticProjectionMapper.cs .......... P8（MCP-08-05/08-06）
├── Mapping/ConstructionProjectionMapper.cs ...... P9（MCP-09-05）
├── Mapping/VerificationProjectionMapper.cs ...... P11（MCP-11-03~11-06）
├── Evidence/McpEvidenceAdapter.cs ............... P12（MCP-12-01~12-07）
└── Hosting/McpServerHost.cs ..................... P6 备选（仅当 Program.cs 超 120 行时拆分）
```

### 0.2 修改（MCP 工程）

```text
backend/modularity/Foundry.FSPM.Mcp/Program.cs ................ P5 已干净，仅 P6 接管线时动
backend/modularity/Foundry.FSPM.Mcp/Mcp/FspmUnderstandTool.cs . P5（MCP-05-01）+ P6（MCP-06-07）+ P8
backend/modularity/Foundry.FSPM.Mcp/Mcp/FspmConstructTool.cs .. P5（MCP-05-01）+ P6（MCP-06-07）+ P9/P10
backend/modularity/Foundry.FSPM.Mcp/Mcp/FspmVerifyTool.cs ..... P5（MCP-05-01）+ P6（MCP-06-07）+ P11
```

### 0.3 测试工程

```text
backend/tests/Foundry.FSPM.Mcp.Tests/
├── Infrastructure/McpTestServerFactory.cs ... P5（MCP-05-02，新建）
├── Infrastructure/McpClientFixture.cs ........ P5（MCP-05-02，新建）
├── Infrastructure/McpResponseAssertions.cs ... P5（MCP-05-02，新建）
├── Discovery/McpServerLifecycleTests.cs ...... P5（MCP-05-02，新建）
├── Discovery/McpDiscoveryTests.cs ............ P5（MCP-05-02，新建）
├── Contract/ToolContractTests.cs ............. P5（由 AwaitingContractTests.cs 演进）
├── Semantic/UnderstandE2ETests.cs ............ P8（MCP-08-07/08-08）
├── Construction/ConstructE2ETests.cs ......... P10
├── Verification/VerifyE2ETests.cs ............ P11
├── Evidence/EvidenceE2ETests.cs .............. P12
├── Hardening/*.cs（6 文件）................... P14
└── Full/FullVerticalSliceTests.cs ............ P13
backend/tests/fixtures/ConstructionFixture/ ... P10（MCP-10-01，新建夹具工程）
```

### 0.4 文档与证据

```text
docs/FSPM/MCP_CORE_API_GAP.md ................. P7 起维护（缺口表）
docs/FSPM/MCP_COMPILATION_API_GAP.md .......... P7 条件（仅 P7.4 BLOCKED 时）
docs/FSPM/MCP-FINAL-VERIFICATION.md ........... P15（封存）
.fspm/evidence/p6-* / p7-* / ... / p13-* ..... 每节点一目录（见各任务）
```

---

## 1. P5 任务（MCP 基础工程闭环）

### Task P5-1：MCP-05-01 修复 3 个 AwaitingContractTests

**Files:**
- Modify: `backend/modularity/Foundry.FSPM.Mcp/Mcp/FspmUnderstandTool.cs`
- Modify: `backend/modularity/Foundry.FSPM.Mcp/Mcp/FspmConstructTool.cs`
- Modify: `backend/modularity/Foundry.FSPM.Mcp/Mcp/FspmVerifyTool.cs`
- Evidence: `.fspm/evidence/step-5/awaiting-contract.json`, `.fspm/evidence/step-5/step5-test.json`

- [ ] **Step 1: 读契约，确认三态语义**
  读 `docs/superpowers/specs/2026-09-03-fspm-mcp-stdio-adapter-design.md` §3.2 与 `docs/FSPM/INTERFACE_LOCKDOWN.md` §1.3，确认：非法参数→`INVALID_REQUEST` 信封；有效请求但上游缺失→`AWAITING_COMPILER` 信封；内部异常→`FAILED` 信封。
- [ ] **Step 2: 改造三 Tool 的校验路径**
  将三 Tool 内的 `throw new ArgumentException(...)` 改为返回结构化信封（STATUS=`INVALID_REQUEST`，含 `field` 与 `message`），有效请求保持 `AWAITING_COMPILER` 信封。不得改测试断言。
- [ ] **Step 3: 跑测试验证 9/9**
  Run: `dotnet test backend/tests/Foundry.FSPM.Mcp.Tests/Foundry.FSPM.Mcp.Tests.csproj -c Debug --nologo --verbosity=normal`
  Expected: `通过数 9，失败数 0，跳过数 0`，exit 0。
- [ ] **Step 4: 落证据并提交**
  将测试输出存 `.fspm/evidence/step-5/awaiting-contract.json`，汇总存 `step5-test.json`。
  ```bash
  git add backend/modularity/Foundry.FSPM.Mcp/Mcp/ .fspm/evidence/step-5/
  git commit -m "fix(fspm-mcp): MCP-05-01 参数校验信封化，9/9 PASS"
  ```

### Task P5-2：MCP-05-02 测试基座

**Files:**
- Create: `backend/tests/Foundry.FSPM.Mcp.Tests/Infrastructure/McpTestServerFactory.cs`
- Create: `backend/tests/Foundry.FSPM.Mcp.Tests/Infrastructure/McpClientFixture.cs`
- Create: `backend/tests/Foundry.FSPM.Mcp.Tests/Infrastructure/McpResponseAssertions.cs`
- Create: `backend/tests/Foundry.FSPM.Mcp.Tests/Discovery/McpServerLifecycleTests.cs`
- Create: `backend/tests/Foundry.FSPM.Mcp.Tests/Discovery/McpDiscoveryTests.cs`
- Modify: `backend/tests/Foundry.FSPM.Mcp.Tests/McpBoundaryTests.cs`, `AwaitingContractTests.cs`

- [ ] **Step 1: 实现 McpTestServerFactory**（统一拉起真实 stdio 子进程、抓 stdout/stderr、关闭；禁止 in-process 冒充）
- [ ] **Step 2: 实现 McpClientFixture + McpResponseAssertions**（统一建连；统一断言 IsError/信封字段）
- [ ] **Step 3: 迁移旧测试到新基座**（McpBoundaryTests + AwaitingContractTests 改用 Fixture，行为不变）
- [ ] **Step 4: 新增 Lifecycle + Discovery 测试**（Start/Discover/Invoke/Shutdown；`registeredTools.Count==3` 且名称精确）
- [ ] **Step 5: 全量跑测试**
  Run: `dotnet test backend/tests/Foundry.FSPM.Mcp.Tests/Foundry.FSPM.Mcp.Tests.csproj -c Debug --nologo --verbosity=normal`
  Expected: 总数 ≥ 9，0 FAIL。
- [ ] **Step 6: Commit**
  ```bash
  git add backend/tests/Foundry.FSPM.Mcp.Tests/
  git commit -m "feat(fspm-mcp): MCP-05-02 测试基座 + Lifecycle/Discovery"
  ```

### P5 Gate（必须全 PASS 方可进 P6）

```text
dotnet build backend/modularity/Foundry.FSPM.Mcp/Foundry.FSPM.Mcp.csproj -c Debug --nologo → 0w0e
dotnet test backend/tests/Foundry.FSPM.Mcp.Tests/Foundry.FSPM.Mcp.Tests.csproj -c Debug --nologo → 9/9（基座扩展后只多不减）
```

---

## 2. P6 任务（统一执行框架）

### Task P6-1：MCP-06-01 ExecutionContext

**Files:** Create `Execution/McpExecutionContext.cs`, `Execution/McpExecutionContextFactory.cs`; Test `McpExecutionContextTests.cs`; Evidence `.fspm/evidence/p6-execution-context/`
- [ ] **Step 1: 实现**（字段：ExecutionId/CorrelationId/StartedAt/Workspace/Request 快照；工厂为唯一生成点）
- [ ] **Step 2: 单测**（唯一性：同请求多次 Create 仅一 Context；ExecutionId 全局唯一）
- [ ] **Step 3: Build+Test+Commit**（命令同 P5 Gate；commit `feat(fspm-mcp): MCP-06-01 ExecutionContext`）

### Task P6-2：MCP-06-02 RequestValidator

**Files:** Create `Validation/IMcpRequestValidator.cs`, `Validation/McpRequestValidator.cs`; Test `McpRequestValidatorTests.cs`; Evidence `.fspm/evidence/p6-validator/`
- [ ] **Step 1: 实现**（null/empty/必填字段/路径存在性/operation-target 格式）
- [ ] **Step 2: 三 Tool 接入**（删散装 ArgumentException，经 Validator 后置 INVALID_REQUEST 信封）
- [ ] **Step 3: 单测**（每类非法输入一例）**Step 4: Build+全量测试+Commit**

### Task P6-3：MCP-06-03 ResponseEnvelope

**Files:** Create `Execution/McpOperationResult.cs`; Test 序列化单测；Evidence `.fspm/evidence/p6-envelope/`
- [ ] **Step 1: 先走公共契约所有权阶梯（V6.1-02）**（① 查 Spec v2/LOCKDOWN 是否已存在所需公共类型 → ② 已存在则一字不改直接用 → ③ 不存在则只建内部 Adapter Model，且不得自行冻结为公共契约；需新公共模型时 STOP 上报，不擅自发明）
- [ ] **Step 2: 实现 + 四 Status 往返单测**（SUCCESS/INVALID_REQUEST/AWAITING_COMPILER/FAILED）
- [ ] **Step 3: Build+Test+Commit**

### Task P6-4：MCP-06-04 ExceptionMapper

**Files:** Create `Errors/IMcpExceptionMapper.cs`, `Errors/McpExceptionMapper.cs`; Test 五类异常映射；Evidence `.fspm/evidence/p6-exceptions/`
- [ ] **Step 1: 实现**（Validation/Resolution/Construction/Verification/Infrastructure → FAILED 信封，不泄露内部栈）
- [ ] **Step 2: 三 Tool 收口 Step 3: 单测 Step 4: Build+Test+Commit**（禁止 catch→Success）

### Task P6-5：MCP-06-05 WorkspaceResolver

**Files:** Create `Workspace/IMcpWorkspaceResolver.cs`, `Workspace/McpWorkspaceResolver.cs`; Test；Evidence `.fspm/evidence/p6-workspace/`
- [ ] **Step 1: 实现**（仅文件系统定位 RootPath/SolutionPath/ProjectPath；零语义解析）
- [ ] **Step 2: 单测**（有效/不存在路径）**Step 3: Build+Test+Commit**

### Task P6-6：MCP-06-06 三网关边界

**Files:** Create `Gateways/` 下 3 接口 + 3 初始实现（AWAITING_COMPILER）；Test 网关契约；Evidence `.fspm/evidence/p6-gateways/`
- [ ] **Step 1: 实现**（网关为唯一可引用 Core 命名空间之处；初版返回等待态）
- [ ] **Step 2: 审计**（`grep -r "Foundry.FSPM.Core" backend/modularity/Foundry.FSPM.Mcp/ --include=*.cs` 除 Gateways 外零命中；且在 evidence 中分别记录 `Adapter Contract` 与 `Implementation` 状态——Stub 只算前者 PASS，不得冒充后者，V6.1-01）
- [ ] **Step 3: Build+Test+Commit**

### Task P6-7：MCP-06-07 共享管线定型

**Files:** Create `Execution/McpExecutionPipeline.cs`; Modify 三 Tool（瘦身为参数声明+管线调用）；Test 管线 E2E；Evidence `.fspm/evidence/p6-pipeline/`
- [ ] **Step 1: 实现管线**（Request→Validate→CreateContext→ResolveWorkspace→Gateway→ProjectResult→PersistEvidence→Response）
- [ ] **Step 2: 三 Tool 改管线调用 Step 3: 9/9 回归 Step 4: Build+Test+Commit**

### P6 Gate

```text
新增 8 源文件 Build 0w0e；单测+集成全 PASS；9/9 基座回归 PASS
```

---

## 3. P7 任务（工作区与编译适配）

### Task P7-1~P7-3：Workspace/Solution/Project 三级定位

**Files:** `Workspace/McpWorkspaceResolver.cs`（增强）, `Workspace/ISolutionResolver.cs`, `Workspace/IProjectResolver.cs`；Tests；Evidence `.fspm/evidence/p7-workspace|solution|project/`
- [ ] **Step 1: 实现**（workspace 规范化；sln 0/多/单三种确定行为；project→ProjectId+编译目标）
- [ ] **Step 2: 单测**（每级有效/无效各一例）**Step 3: Build+Test+Commit**（`feat(fspm-mcp): MCP-07 workspace/solution/project`）

### Task P7-4：MCP-07-04 Compilation Gateway

**Files:** Create `Gateways/ICompilationProvider.cs`（+实现或 BLOCKED 占位）；Evidence `.fspm/evidence/p7-compilation/`
- [ ] **Step 1: 查上游**（Compiler/Core 是否交付 Compilation；查 `MCP_CORE_API_GAP.md`）
- [ ] **Step 2a（就绪）: 实现转发**（禁自建 Compiler）**Step 2b（未就绪）: 标 BLOCKED + 写 `docs/FSPM/MCP_COMPILATION_API_GAP.md`**，禁伪造
- [ ] **Step 3: Build+Test+Commit**

### Task P7-5：MCP-07-05 CompilationIdentity

**Files:** Create `Workspace/CompilationIdentity.cs`；Test；Evidence `.fspm/evidence/p7-compilation-identity/`（仅非 BLOCKED 时执行）
- [ ] **Step 1: 实现**（Project/AssemblyName/CompilationId/Timestamp）**Step 2: 单测 Step 3: Commit**

### P7 Gate

```text
Workspace/Solution/Project/Compilation/Identity 全 resolved；
或 P7.1–P7.3 COMPLETE + P7.4 BLOCKED + MCP_COMPILATION_API_GAP.md
```

---

## 4. P8 任务（Understand 真实化）

### Task P8-1：请求冻结 + Target 解析（MCP-08-01/08-02）

**Files:** Create `Tools/Requests/UnderstandRequest.cs`, `Mapping/SemanticQueryParser.cs`；Tests；Evidence `.fspm/evidence/p8-request|target/`
- [ ] **Step 1: 实现**（1 段=Type，2 段=Member；非法格式结构化失败）
- [ ] **Step 2: 四场景单测**（User/User.UserName/User.Password/User.Login）**Step 3: Build+Test+Commit**

### Task P8-2：Resolver 接入 + 真实 Symbol（MCP-08-03/08-04）

**Files:** Modify `Gateways/ISemanticGateway.cs` 真实分支；Tests；Evidence `.fspm/evidence/p8-resolver|symbol/`
- [ ] **Step 1: 查上游**（ISemanticResolver FSPM-07/08；未交付则 BLOCKED，禁 `new SemanticType` 冒充）
- [ ] **Step 2: 实现（V6.1-03）**（SemanticQuery→ISemanticGateway→Core Resolver→SemanticRef/真实三类 Symbol；禁止 MCP 调用 `Compilation.GetTypeByMetadataName` 自行二次解析，禁止 `new SemanticType` 冒充）
- [ ] **Step 3: 集成测试 Step 4: Commit**

### Task P8-3：投影 + 位置 + 金色测试 + E2E（MCP-08-05~08-08）

**Files:** Create `Mapping/SemanticProjectionMapper.cs`；Create `Semantic/UnderstandE2ETests.cs`（4 Fact）；Evidence `.fspm/evidence/p8-projection|location|golden|e2e/`
- [ ] **Step 1: 实现投影**（SemanticKind/QualifiedName/SymbolIdentity/SourceLocation/ContainingSymbol；无位置 Symbol 显式缺失态）
- [ ] **Step 2: 四场景金色测试 Step 3: stdio E2E（复用 P5 基座）Step 4: Build+Test+Commit**

### P8 Gate

```text
User/User.UserName/User.Password/User.Login PASS；
Real Compilation/ISymbol/SourceLocation PASS → 第一个真实能力成立
```

---

## 5. P9 任务（Construct 规划适配）

### Task P9-1：请求 + Intent 透传 + Target 解析（MCP-09-01~09-03）

**Files:** Create `Tools/Requests/ConstructRequest.cs`；Modify `Gateways/IConstructionGateway.cs`（透传+调 SemanticGateway）；Tests；Evidence `.fspm/evidence/p9-request|intent|target/`
- [ ] **Step 1: 实现**（MCP 只 deserialize/validate/pass-through；IntentParser 归 Core，代码审计零解析逻辑）
- [ ] **Step 2: Target 经 P8 链得 SemanticRef**（未解析出直接失败）**Step 3: Build+Test+Commit**

### Task P9-2：ConstructionService 接入 + PLANNED 投影（MCP-09-04/09-05）

**Files:** Create `Mapping/ConstructionProjectionMapper.cs`；Tests；Evidence `.fspm/evidence/p9-gateway|projection/`
- [ ] **Step 1: 查上游**（ConstructionService FSPM-13/14；未交付 BLOCKED）
- [ ] **Step 2: 实现**（plan→status/target/planId；断言 PLANNED≠CONSTRUCTED）**Step 3: Build+Test+Commit**

### P9 Gate

```text
request → Target resolution → ConstructionService → real construction plan
```

---

## 6. P10 任务（变更/重编/重绑）

### Task P10-1：夹具 + 指纹（MCP-10-01/10-02）

**Files:** Create `backend/tests/fixtures/ConstructionFixture/`（小工程+还原脚本）；指纹工具；Evidence `.fspm/evidence/p10-fixture|before/`
- [ ] **Step 1: 建夹具**（可独立 build + 一键还原）**Step 2: SHA256 指纹稳定单测 Step 3: Commit**

### Task P10-2：Mutation + 变更验证 + Build（MCP-10-03~10-05）

**Files:** Modify ConstructionGateway；Tests；Evidence `.fspm/evidence/p10-mutation|changed|build/`
- [ ] **Step 1: 查上游**（SourceMutationEngine；未交付 BLOCKED）
- [ ] **Step 2: 实现（V6.1-04）**（plan→Mutate→changedFiles/diffSummary/writerTransactionId；before≠after 断言；Build 真正执行者为 Core Build Pipeline——经 VerificationGateway 调用；Core 未提供则本步 BLOCKED；禁止 `Process.Start("dotnet")` 自建构建）**Step 3: Build+Test+Commit**

### Task P10-3：Rebind + Evidence（MCP-10-06/10-07）

**Files:** Tests `Construction/ConstructE2ETests.cs`；Evidence `.fspm/evidence/p10-rebind|evidence/`
- [ ] **Step 1: 实现（V6.1-04）**（重取 Compilation→新 Identity→由 Core SemanticResolver 执行重解析得新 SemanticRef；old≠new；MCP 禁自行重解析；九字段 Evidence 经 McpEvidenceAdapter 落盘）
- [ ] **Step 2: E2E Step 3: Commit**

### P10 Gate

```text
source changed → build passed → new compilation → re-resolve → evidence → CONSTRUCTED
```

---

## 7. P11 任务（Verify 真实化）

### Task P11-1：请求 + Orchestrator 接入（MCP-11-01/11-02）

**Files:** Create `Tools/Requests/VerifyRequest.cs`；Modify VerificationGateway；Evidence `.fspm/evidence/p11-request|gateway/`
- [ ] **Step 1: 查上游**（VerificationOrchestrator FSPM-04..12/17/18；未交付 BLOCKED）
- [ ] **Step 2: 实现 Step 3: Build+Test+Commit**

### Task P11-2：四段投影 + HardGate + Response（MCP-11-03~11-08）

**Files:** Create `Mapping/VerificationProjectionMapper.cs`；Tests `Verification/VerifyE2ETests.cs`；Evidence `.fspm/evidence/p11-analysis|build|test|runtime|hardgate|response/`
- [ ] **Step 1: 实现**（Analysis: RuleId/Diagnostic/Severity/Evidence/Status；Build: status/Errors/Warnings；Test: count/Passed/Failed/Skipped/Duration；Runtime: 有则真调、无则 NOT_ESTABLISHED；Analysis=FAIL⇒后段 NOT_RUN 短路）
- [ ] **Step 2: 五段 Response E2E Step 3: Build+Test+Commit**

### P11 Gate

```text
request → Orchestrator → Analysis → Build → Test → Evidence
```

---

## 8. P12 任务（证据集成）

### Task P12-1：McpEvidenceAdapter + 七类文件 + 反查（MCP-12-01~12-07）

**Files:** Create `Evidence/McpEvidenceAdapter.cs`；Tests `Evidence/EvidenceE2ETests.cs`；Evidence `.fspm/evidence/p12-*/`

> **冻结（V6.1-05）：Evidence Authority = Core；MCP = Persistence/Correlation Adapter。** Adapter 只 persist/expose/correlate，不判定 PASS、不生成核心事实。
- [ ] **Step 1: 实现目录规范**（`.fspm/evidence/<execution-id>/`）
- [ ] **Step 2: 实现六类文件写**（request/semantic/construction/analysis+build+test+runtime/result，字段清单见规格 §10）
- [ ] **Step 3: Response 补 evidencePath + 反查测试 Step 4: Build+Test+Commit**

### P12 Gate

```text
任一 understand/construct/verify 调用均可反查到完整 Evidence
```

---

## 9. P13 任务（全链路垂直切片）

### Task P13-1：FullVerticalSliceTests（MCP-13-01~13-09）

**Files:** Create `Full/FullVerticalSliceTests.cs`；Evidence `.fspm/evidence/p13-smoke/`
- [ ] **Step 1: 编排**（Start→Discover→Understand 四场景→Construct→Build→Rebind→Verify→Evidence，每步断言，失败停并保留现场，fixture 还原）
- [ ] **Step 2: 跑 smoke**（`dotnet test --filter FullVerticalSlice` 全绿）**Step 3: Commit**

### P13 Gate

```text
Understand/Construct/Build/Rebind/Verify/Evidence 全 PASS → Vertical Slice COMPLETE
```

---

## 10. P14 任务（生产加固，可与 P13 收尾并行）

**Files:** `Hardening/` 下 6 测试文件；`p14-perf-baseline.json`；Evidence `.fspm/evidence/p14-*/`
- [ ] **Step 1: 输入/异常/取消/并发/恢复/幂等矩阵测试**（每类一文件，逐文件 Build+Test+Commit）
- [ ] **Step 2: 性能基线落盘**（启动/发现/三 Tool 耗时 `p14-perf-baseline.json`）

---

## 11. P15 任务（正式封存）

### Task P15-1：MCP-15-01 封存

**Files:** Create `docs/FSPM/MCP-FINAL-VERIFICATION.md`（Architecture/Tool Contracts/Build/Tests/E2E/Evidence/Known Limitations/Upstream Dependencies 八节）
- [ ] **Step 1: 写八节 Step 2: 附最终全量测试报告 Step 3: Commit**（冻结后只收缺陷修复）

---

## 12. 全局门禁速查（每 Phase 出口必跑）

```powershell
dotnet build backend/modularity/Foundry.FSPM.Mcp/Foundry.FSPM.Mcp.csproj -c Debug --nologo
dotnet build backend/tests/Foundry.FSPM.Mcp.Tests/Foundry.FSPM.Mcp.Tests.csproj -c Debug --nologo
dotnet test backend/tests/Foundry.FSPM.Mcp.Tests/Foundry.FSPM.Mcp.Tests.csproj -c Debug --nologo --verbosity=normal
```

Expected：Build 均为 `0 个警告 0 个错误`；Test 0 FAIL（P5 起 9/9，此后只增不减）。

---

## 13. 节点状态机（V6.1-07，执行期必填）

每节点 evidence 目录必须含 `node-status.json`（字段：node / adapterContract / implementation / updatedAt / blocker），五态：READY → IN_PROGRESS → PASS，FAILED 回炉，BLOCKED 仅冻 Implementation。上游依赖节点（P7-4/P8-2/P9-2/P10-2/P11-1 的"查上游"步）必须双行记录 Adapter Contract 与 Implementation 状态。

## 14. 并行施工调度（V6.1-06）

MCP 先行：P5 → P6 → P7 → Adapter Harness，不等 Compiler；Compiler 每交付一 API，按 §15 立即接入对应 Phase（P8/P9-P10/P11/P12），不从头盘点。P14 与 P13 收尾可并行。

## 15. 上游 API 自动解 Block（V6.1-08）

`BLOCKED → READY → IN_PROGRESS → 验收 → PASS/FAILED`，同步更新 `MCP_CORE_API_GAP.md`；仅当交付签名与 LOCKDOWN 冻结面冲突时 STOP 上报。

## 16. 自检（Self-Review，已执行）

**1. 规格覆盖度：** 规格 §3–§13 共 50 个节点（MCP-05-01 至 MCP-15-01）→ 本计划 Task P5-1 至 P15-1 逐一覆盖，无遗漏；规格 §15 的 F01–F15 映射到 P5/P7/P8/P9/P10/P11/P12 任务；规格 §16 缺口表对应 P7-4/P8-2/P9-2/P10-2/P11-1 的"查上游"步骤；规格 §17 并行调度/§19 状态机/§20 自动解 Block 对应本计划 §14/§13/§15；类型名与 Status 取值两处逐字一致（含 V6.1-01~08 新增条款）。

**2. 占位符扫描：** 全文无 TBD/TODO/"后续补充"/"类似 Task N"；P7-4 等上游依赖写明 BLOCKED 条件与缺口文档产物，非占位；凡涉及代码均给出确切文件路径与命令。

**3. 类型一致性：** `McpExecutionContext/McpOperationResult/ISemanticGateway/IConstructionGateway/IVerificationGateway/McpEvidenceAdapter/CompilationIdentity/UnderstandRequest/ConstructRequest/VerifyRequest` 命名在规格与计划中逐字一致；Status 取值（SUCCESS/INVALID_REQUEST/AWAITING_COMPILER/FAILED）两处一致。

---

## 17. 执行交接

**Plan complete and saved to `docs/superpowers/plan/FSPM-MCP完整系统实施计划V6.1.md`. Two execution options:**

**1. Subagent-Driven（推荐）** — 每个 Task 派 fresh subagent，Task 间 review，快速迭代。REQUIRED SUB-SKILL: superpowers:subagent-driven-development。

**2. Inline Execution** — 本会话内按 Task 批量执行，设检查点。REQUIRED SUB-SKILL: superpowers:executing-plans。

**Which approach?（待你审核规格与计划后，先不执行，等你选执行方式。）**
