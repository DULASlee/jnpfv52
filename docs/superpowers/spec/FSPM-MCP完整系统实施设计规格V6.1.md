# FSPM MCP 完整系统实施设计规格 V6.1

> **性质：** 设计规格（Spec）——锁定"做什么、做到什么程度算完成"，不规定逐行代码。
> **配套实施计划：** `docs/superpowers/plan/FSPM-MCP完整系统实施计划V6.1.md`
> **执行总纲来源：** 首席架构师《FSPM MCP AI 工程师完整系统实施施工包 V6.0》+ 首席架构师 V6.1 工程修正裁决（8 项）
> **状态：** V6.1 工程修正版（8 项修正已落实），已获准进入连续施工

| 项 | 值 |
|---|---|
| 基线分支 | `feature/fspm-mcp-stdio-adapter` |
| 基线 Commit | `968a27d8`（`docs(fspm-mcp): add STEP 5 gate closure report`） |
| 基线状态 | MCP Build PASS / Tests Build PASS / Tests 6/9 PASS / Startup PASS / 3 Tools PASS / stdout Boundary PASS |
| 权威冻结文档 | `docs/superpowers/specs/2026-09-03-fspm-mcp-stdio-adapter-design.md`（Spec v2） |
| 权威冻结文档 | `docs/FSPM/INTERFACE_LOCKDOWN.md`（Compiler↔MCP 共享契约） |
| 上游缺口基线 | `.fspm/evidence/baseline/MCP_UPSTREAM_GAP.md`、`MCP_CORE_BINDING_CONTRACT.md` |
| 日期 | 2026-09-03 |

---

## §0.0 V6.0→V6.1 修订记录（首席架构师裁决，8 项）

| 编号 | 修正点 | 落点 |
|---|---|---|
| V6.1-01 | Gateway Stub 仅编译期接口占位 + 测试替身，不得计入真实功能完成；`Adapter Contract Ready ≠ Core Integration Ready` | MCP-06-06、§19 |
| V6.1-02 | 公共契约所有权阶梯：先查冻结→已存在则用→不存在则仅内部 Adapter Model，禁止 MCP 冻结新公共模型 | MCP-06-03 |
| V6.1-03 | P8.4 删除 `Compilation.GetTypeByMetadataName` 二次解析，改由 Core Resolver 返回真实 Symbol | MCP-08-04 |
| V6.1-04 | P10 Build 执行者为 Core；缺失则 BLOCKED，禁 `Process.Start("dotnet")`；Rebind 由 Core Resolver 完成 | MCP-10-05、MCP-10-06 |
| V6.1-05 | Evidence Authority = Core；MCP = Persistence/Correlation Adapter | §10 卷首冻结 |
| V6.1-06 | §17 改并行施工调度（MCP 先行 + 上游到达即接入），替代纯串行 | §17 |
| V6.1-07 | 全节点状态机 READY / IN_PROGRESS / PASS / FAILED / BLOCKED（含 Adapter/Implementation 双态） | §19 |
| V6.1-08 | 上游 API 到达自动解 Block 并接入的执行规则 | §20 |

---

## §0 基线事实与全局铁律

### 0.1 已有资产（不得删除，不得回滚）

```text
Spec v2（705 行，已冻结）
INTERFACE_LOCKDOWN.md（Compiler↔MCP 共享契约，已冻结）
backend/modularity/Foundry.FSPM.Mcp/（Program.cs 45 行干净入口 + 3 Tool stub）
backend/tests/Foundry.FSPM.Mcp.Tests/（McpBoundaryTests 6 Fact + AwaitingContractTests 3 Fact）
.fspm/evidence/（baseline / step-5 / env-context-compare 证据链）
```

### 0.2 已知阻塞（本规格必须正面处理，不得绕过）

```text
阻塞 1：3 个 AwaitingContractTests FAIL（ArgumentException → MCP error response → IsError=null）
阻塞 2：Foundry.FSPM.Core 当前不存在（仅空壳），Semantic/Construction/Verification 真实 API 尚未形成
```

### 0.3 全局铁律（所有 Phase 通用，违反任一条即停工）

```text
铁律 1：stdout ONLY MCP JSON-RPC；stderr ONLY 日志诊断。Console.WriteLine 写 stdout 永久禁止。
铁律 2：Tool 数量恒为 3（fspm_understand / fspm_construct / fspm_verify），不得增删改名。
铁律 3：MCP = Adapter。禁止 MCP 自己解析 C#、构造 SemanticRef、写 Source Mutation、跑 Analyzer、生成 Verification、制造 Evidence。
铁律 4：每个开发节点必须产出三件套：Code + Test + Evidence，缺一即节点未完成。
铁律 5：唯一允许修改的源码范围是 Foundry.FSPM.Mcp / Foundry.FSPM.Mcp.Tests；触碰 Compiler/Core/Analyzer/Login.Mvp/其他 Worktree 即越界。
铁律 6：本项目禁用流程性 TDD（先红后绿），采用"先实现 → xUnit 覆盖核心产出物"，Phase Gate 强制覆盖。
```

---

## §1 最终系统目标

### 1.1 最终真实调用链

```text
AI Client
    │ MCP / stdio（JSON-RPC，stdout 纯净）
    ▼
FspmMcpServer（Program.cs 正式入口）
    │
    ├── fspm_understand → SemanticGateway → Compilation → SemanticResolver → ISymbol → SemanticResponse
    ├── fspm_construct → ConstructionGateway → SemanticResolver → ConstructionPlanner
    │                     → SourceMutationEngine → Build → Rebind → ConstructionEvidence
    └── fspm_verify → VerificationGateway → VerificationOrchestrator
                      → Analyze / Build / Test / Runtime → Evidence
```

### 1.2 最终业务闭环

```text
AI → MCP → Understand → Construct → Build → Verify → Evidence
```

### 1.3 完成定义（必须同时满足）

```text
MCP Server PASS / 3 Tools PASS / stdio PASS /
Understand PASS / Construct PASS / Build PASS / Rebind PASS /
Verify PASS / Evidence PASS / Full Smoke PASS / Regression PASS
```

### 1.4 反作弊清单（任一命中即判定失败）

```text
无重复 Semantic Core、无伪造 Symbol、无伪造 Construction、
无伪造 Verification、无伪造 Evidence、无隐藏第四 Tool、无 stdout 污染
```

---

## §2 阶段总表

| Phase | 名称 | 核心结果 | 依赖上游 |
|---|---|---|---|
| P5 | MCP 基础工程闭环 | 9/9 PASS，测试基座稳定 | 否 |
| P6 | MCP 统一执行框架 | 三 Tool 同一管线，网关边界建立 | 否 |
| P7 | 工作区与编译适配 | 真实工程定位；Compilation 按需 BLOCKED | 部分（Compilation） |
| P8 | Semantic Understand 真实化 | 真实 Type/Property/Method + SourceLocation | 是（SemanticResolver） |
| P9 | Construction 请求与规划适配 | 真实 construction plan（PLANNED） | 是（ConstructionService） |
| P10 | 源码变更与重编重绑 | 真实变更 + Build + Rebind（CONSTRUCTED） | 是（MutationEngine） |
| P11 | Verification 真实化 | Analysis/Build/Test/Runtime 全投影 | 是（Orchestrator+Analyzer） |
| P12 | 证据集成 | 每次调用可追溯 evidence 目录 | 是（Evidence 收集器） |
| P13 | 全链路垂直切片 | Understand→Construct→Verify 全链路冒烟 | 是（P8–P12 全部） |
| P14 | 生产加固 | 异常/并发/取消/恢复/幂等/性能基线 | 否 |
| P15 | 正式封存 | MCP-FINAL-VERIFICATION + 冻结 | 否 |

---

## §3 P5 — MCP 基础工程闭环

### 节点 MCP-05-01：修复 3 个 AwaitingContractTests

- **开发目标：** 消除 `ArgumentException → MCP error response → IsError=null` 链条，使 9/9 Fact 通过。
- **前置条件：** 基线 `968a27d8`；已读 Spec v2 §3.2 与 INTERFACE_LOCKDOWN §1.3，明确非法参数 / 有效请求 / 上游缺失三种状态的契约含义。
- **逻辑路径：**
  ```text
  CallToolAsync → Tool 参数校验（抛 ArgumentException）
      → MCP Transport 捕获 → error response（IsError=null，非业务信封）
      → Assert.False(result.IsError) 失败
  ```
- **实现方法：** 在三个 Tool 内将参数校验异常映射为结构化业务信封返回（携带 `INVALID_REQUEST` 状态与字段说明），不再让校验异常穿透到 Transport 层；有效请求保持返回 `AWAITING_COMPILER` 信封；不改测试断言，不降低测试数量。
- **产物：**
  ```text
  backend/modularity/Foundry.FSPM.Mcp/Mcp/FspmUnderstandTool.cs（修改）
  backend/modularity/Foundry.FSPM.Mcp/Mcp/FspmConstructTool.cs（修改）
  backend/modularity/Foundry.FSPM.Mcp/Mcp/FspmVerifyTool.cs（修改）
  .fspm/evidence/step-5/awaiting-contract.json（9/9 原始输出）
  .fspm/evidence/step-5/step5-test.json（汇总）
  ```
- **验收标准：** `dotnet test` 输出 `通过数 9 / 失败数 0 / 跳过数 0`，exit code 0；证据 JSON 落盘。

### 节点 MCP-05-02：测试基座（Fixture / Lifecycle / Discovery）

- **开发目标：** 消灭"每个测试自己拉起 Server"的重复模式，建立可复用的测试基础设施。
- **前置条件：** MCP-05-01 完成（9/9 PASS）。
- **逻辑路径：**
  ```text
  现有 McpStdioServerFixture（每个 Fixture 拉一个进程）
      → 抽取 McpTestServerFactory（统一拉起/关闭/抓 stdout/stderr）
      → McpClientFixture（统一建连）/ McpResponseAssertions（统一断言）
      → 旧测试迁移到新基座 → 全量重跑
  ```
- **实现方法：** 在 Tests 工程新增 `Infrastructure/` 目录，实现上述四个类；`McpBoundaryTests` 与 `AwaitingContractTests` 改为复用新基座；Server 拉起仍走真实 stdio 子进程（禁止 in-process 冒充，沿用 STEP 5 架构评审结论）。
- **产物：**
  ```text
  backend/tests/Foundry.FSPM.Mcp.Tests/Infrastructure/McpTestServerFactory.cs（新建）
  backend/tests/Foundry.FSPM.Mcp.Tests/Infrastructure/McpClientFixture.cs（新建）
  backend/tests/Foundry.FSPM.Mcp.Tests/Infrastructure/McpResponseAssertions.cs（新建）
  backend/tests/Foundry.FSPM.Mcp.Tests/McpBoundaryTests.cs（修改：迁移基座）
  backend/tests/Foundry.FSPM.Mcp.Tests/AwaitingContractTests.cs（修改：迁移基座）
  McpServerLifecycleTests.cs（新建：Start/Discover/Invoke/Shutdown）
  McpDiscoveryTests.cs（新建：registeredTools.Count==3，名称精确匹配）
  ```
- **验收标准：**
  ```text
  McpServerLifecycleTests = PASS（startup exception=0，unexpected stdout=0）
  Tool Discovery 经真实 MCP Tool Registry 验证通过（禁止源码 grep 充数）
  全量测试数 ≥ 9 且 0 FAIL
  ```

### P5 Gate

```text
[PASS] MCP Build（0 warning 0 error）
[PASS] Tests Build（0 warning 0 error）
[PASS] Tests（9/9，0 FAIL，0 SKIP）
[PASS] Server Startup（真实 stdio 子进程）
[PASS] Exactly 3 Tools（真实 Registry）
[PASS] stdout Boundary（协议纯净）
```

---

## §4 P6 — MCP 统一执行框架

### 节点 MCP-06-01：McpExecutionContext

- **开发目标：** 一次 MCP 调用有唯一的、可追踪的运行上下文。
- **前置条件：** P5 Gate 通过。
- **逻辑路径：** `Tool 收到请求 → contextFactory.Create() → 后续全链路携带 ExecutionId/CorrelationId`。
- **实现方法：** 新建 `Execution/McpExecutionContext.cs`（字段：ExecutionId、CorrelationId、StartedAt、Workspace、Request 快照）与 `McpExecutionContextFactory.cs`（唯一生成点）；禁止 Tool 自行生成多个 ExecutionId。
- **产物：** 上述 2 个源文件 + `McpExecutionContextTests.cs` + `.fspm/evidence/p6-execution-context/`。
- **验收标准：** 同一请求多次调用工厂仅产生一个 Context；ExecutionId 全局唯一（xUnit 断言）；Build 0w0e。

### 节点 MCP-06-02：IMcpRequestValidator / McpRequestValidator

- **开发目标：** 三个 Tool 共用一套请求校验（null / empty / 必填字段 / 路径存在性 / operation-target 格式）。
- **前置条件：** MCP-06-01 完成。
- **逻辑路径：** `Tool → Validator.Validate() → 通过则建 Context，不过则返回 INVALID_REQUEST 信封`。
- **实现方法：** 新建 `Validation/` 目录与上述接口+实现；三 Tool 接入统一 Validator，删除各自的散装 `ArgumentException` 校验。
- **产物：** 2 个源文件 + `McpRequestValidatorTests.cs`（覆盖 null/empty/缺字段/非法路径）+ `.fspm/evidence/p6-validator/`。
- **验收标准：** 三 Tool 均经统一 Validator；非法输入返回结构化 INVALID_REQUEST 而非 Transport 异常。

### 节点 MCP-06-03：McpOperationResult〈T〉响应信封

- **开发目标：** 统一成功/失败/等待上游的响应形状，与冻结契约一致。
- **前置条件：** MCP-06-02 完成；公共契约所有权阶梯（V6.1-02）必须先执行：① 检查 Spec v2 / INTERFACE_LOCKDOWN 是否已存在所需公共类型 → ② 已存在则直接使用，一字不改 → ③ 不存在则 MCP 只允许建立内部 Adapter Model，且不得自行冻结为公共契约（需首席架构师批准）。禁止 MCP 工程师因"实现方便"发明 `McpOperationResult<T>` 之类的新公共模型，避免未来形成 Core Contract / MCP Contract / 自创模型三套并存。
- **逻辑路径：** `Gateway 结果 → 信封（Status/ExecutionId/Result/Error/Evidence）→ 序列化 → MCP Response`。
- **实现方法：** 新建 `Execution/McpOperationResult.cs`；Status 取值与协议冻结定义一致（SUCCESS / INVALID_REQUEST / AWAITING_COMPILER / FAILED，最终以 Spec 为准）。
- **产物：** 1 个源文件 + 序列化单测 + `.fspm/evidence/p6-envelope/`。
- **验收标准：** 四种 Status 均可序列化往返；与冻结契约字段名逐字一致。

### 节点 MCP-06-04：IMcpExceptionMapper / McpExceptionMapper

- **开发目标：** Core 异常变为稳定 MCP Response，永不裸抛。
- **前置条件：** MCP-06-03 完成。
- **逻辑路径：** `ValidationException / ResolutionException / ConstructionException / VerificationException / InfrastructureException → 映射 → FAILED 信封（含可诊断信息，不泄露内部栈）`。
- **实现方法：** 新建 `Errors/` 目录与映射器；三 Tool 统一经映射器收口。
- **产物：** 2 个源文件 + 映射单测（每种异常一例）+ `.fspm/evidence/p6-exceptions/`。
- **验收标准：** 五类异常各有确定映射；禁止 `catch(Exception) → return Success`。

### 节点 MCP-06-05：IMcpWorkspaceResolver / McpWorkspaceResolver

- **开发目标：** 只定位工作区（RootPath / SolutionPath / ProjectPath），不解析 C#。
- **前置条件：** MCP-06-04 完成。
- **逻辑路径：** `输入（workspace/solution/project）→ 路径存在性校验 → ResolvedWorkspace`。
- **实现方法：** 新建 `Workspace/` 目录与上述接口+实现；此处只做文件系统级定位，任何语义解析都属越界。
- **产物：** 2 个源文件 + 定位单测（含不存在路径的失败例）+ `.fspm/evidence/p6-workspace/`。
- **验收标准：** 有效输入返回三路径；无效输入返回结构化失败。

### 节点 MCP-06-06：三网关边界（ISemanticGateway / IConstructionGateway / IVerificationGateway）

- **开发目标：** 建立 MCP 与 Core 之间的唯一内部边界：MCP 请求 ↔ Core 请求的双向转换。
- **前置条件：** MCP-06-05 完成；已读 MCP_CORE_BINDING_CONTRACT（调用方向冻结）。
- **逻辑路径：** `MCP request → Gateway.Translate → Core request（上游缺失时返回 AWAITING_COMPILER）→ Core result → Gateway.Project → MCP 投影`。
- **实现方法：** 新建 `Gateways/` 目录与三接口 + 三个初始实现（初始实现返回 AWAITING_COMPILER，等待 P8/P9/P11 接入真实 Core）；网关是唯一允许引用 Core 命名空间的地方。
- **防假完成条款（V6.1-01）：** Gateway Stub 只能作为编译期接口占位和测试替身，不得被计入真实功能完成；节点验收必须区分 `Adapter Contract Ready`（接口形状冻结、可编译、可测）与 `Core Integration Ready`（真实 Core 调用贯通），两者状态独立记录（见 §19），前者 PASS 不得冒充后者。
- **产物：** 6 个源文件 + 网关契约测试 + `.fspm/evidence/p6-gateways/`。
- **验收标准：** 三 Tool 均经网关调用 Core（即使当前返回等待态）；MCP 其他位置无 Core 引用（grep 审计）。

### 节点 MCP-06-07：共享执行管线定型

- **开发目标：** 三 Tool 收敛为同一管线：Request → Validate → CreateContext → ResolveWorkspace → Gateway → ProjectResult → PersistEvidence → Response。
- **前置条件：** MCP-06-01 ~ MCP-06-06 全部完成。
- **逻辑路径：** 将三 Tool 方法体重写为管线调用；各阶段异常经 ExceptionMapper 收口。
- **实现方法：** 新建 `Execution/McpExecutionPipeline.cs`；三 Tool 瘦身为"参数声明 + 管线调用"。
- **产物：** 1 个源文件 + 三 Tool 修改 + 端到端管线测试 + `.fspm/evidence/p6-pipeline/`。
- **验收标准：** 三 Tool 代码结构对称；任一阶段失败均产生结构化信封；9/9 基座测试仍 PASS。

### P6 Gate

```text
新增源文件 8 个（§4 各节点）全部 Build 0w0e
单测 + 集成测试全部 PASS
9/9 基座测试回归 PASS
```

---

## §5 P7 — 工作区与编译适配

### 节点 MCP-07-01：Workspace Loading

- **开发目标：** 由 `{"workspace": "D:\\..."}` 输入得到 `ResolvedWorkspace`。
- **前置条件：** P6 Gate 通过。
- **逻辑路径：** 输入反序列化 → 路径规范化 → 存在性校验 → ResolvedWorkspace。
- **实现方法：** 充实 `McpWorkspaceResolver` 的 workspace 分支。
- **产物：** WorkspaceResolver 增强 + 单测 + `.fspm/evidence/p7-workspace/`。
- **验收标准：** 真实存在的 workspace 解析成功；不存在的返回结构化失败。

### 节点 MCP-07-02：Solution Resolution（ISolutionResolver）

- **开发目标：** 找到 `.sln`、校验路径、加载 solution 标识。
- **前置条件：** MCP-07-01 完成。
- **逻辑路径：** workspace root 枚举 `*.sln` → 唯一性判定（0 个/多个均为失败态）→ Solution 标识。
- **实现方法：** 新建 `Workspace/ISolutionResolver.cs` + 实现。
- **产物：** 2 个源文件 + 单测 + `.fspm/evidence/p7-solution/`。
- **验收标准：** 单解多解零解三种情形均有确定行为。

### 节点 MCP-07-03：Project Resolution（IProjectResolver）

- **开发目标：** 由 project name/path 得到 ProjectId 与编译目标。
- **前置条件：** MCP-07-02 完成。
- **逻辑路径：** solution 内匹配 project → ProjectId → 编译目标（TargetFramework）。
- **实现方法：** 新建 `Workspace/IProjectResolver.cs` + 实现。
- **产物：** 2 个源文件 + 单测 + `.fspm/evidence/p7-project/`。
- **验收标准：** 真实 solution fixture 下解析成功。

### 节点 MCP-07-04：Compilation Gateway（ICompilationProvider）

- **开发目标：** Workspace → Solution → Project → Compilation 全链打通。
- **前置条件：** MCP-07-03 完成 + Compiler/Core 提供 Compilation 能力。
- **逻辑路径：** ResolvedWorkspace → Solution → Project → `ICompilationProvider.GetCompilationAsync()`。
- **实现方法：** 新建 `Gateways/ICompilationProvider.cs`；仅转发 Core/Compiler 的 Compilation，MCP 禁止自建第二套 Compiler。
- **产物：** 接口 + 实现（或 BLOCKED 占位）+ `.fspm/evidence/p7-compilation/`。
- **验收标准：** 若上游未交付：P7.1–P7.3 标 COMPLETE，P7.4 标 BLOCKED，并输出 `MCP_COMPILATION_API_GAP.md`（不得伪造 Compilation 充数）。

### 节点 MCP-07-05：Compilation Identity

- **开发目标：** 保存 Project / AssemblyName / CompilationId / Timestamp，供 P10 Rebind 比对。
- **前置条件：** MCP-07-04 非 BLOCKED。
- **逻辑路径：** 每次获取 Compilation 即快照身份。
- **实现方法：** 新建 `Workspace/CompilationIdentity.cs` 记录类型。
- **产物：** 1 个源文件 + 单测 + `.fspm/evidence/p7-compilation-identity/`。
- **验收标准：** 两次获取身份可比对；时间戳单调。

### P7 Gate

```text
Workspace resolved / Solution resolved / Project resolved / Compilation acquired / Identity available
或：P7.1–P7.3 COMPLETE + P7.4 BLOCKED + MCP_COMPILATION_API_GAP.md
```

---

## §6 P8 — FSPM_UNDERSTAND 真实化（第一个真实能力）

### 节点 MCP-08-01：UnderstandRequest 冻结

- **开发目标：** 冻结请求形状（Workspace / Project / Target）。
- **前置条件：** P7 非全 BLOCKED；已读 INTERFACE_LOCKDOWN 相关节。
- **逻辑路径：** MCP Tool 参数 → UnderstandRequest（经 P6 Validator）。
- **实现方法：** 新建 `Tools/Requests/UnderstandRequest.cs`。
- **产物：** 1 个源文件 + 反序列化单测 + `.fspm/evidence/p8-request/`。
- **验收标准：** 缺字段/空字段均被 Validator 拦截。

### 节点 MCP-08-02：Target Parser（SemanticQuery）

- **开发目标：** 解析 `User` / `User.UserName` / `User.Password` / `User.Login` 为 SemanticQuery。
- **前置条件：** MCP-08-01 完成。
- **逻辑路径：** 点分切分 → 段数判定（1=Type，2=Member）→ SemanticQuery{TypeName, MemberName}。
- **实现方法：** 新建 `Mapping/SemanticQueryParser.cs`；非法格式返回结构化失败。
- **产物：** 1 个源文件 + 四场景单测 + `.fspm/evidence/p8-target/`。
- **验收标准：** 四种 Target 解析正确；非法 Target 有确定失败。

### 节点 MCP-08-03：Semantic Resolver Gateway 接入

- **开发目标：** `ISemanticGateway.ResolveAsync(...)` 真实调用 Core `SemanticResolver`。
- **前置条件：** MCP-08-02 完成 + 上游交付 `ISemanticResolver`（FSPM-07/08）；否则本节点 BLOCKED。
- **逻辑路径：** SemanticQuery → Compilation → Core SemanticResolver → SemanticRef。
- **实现方法：** 充实 `SemanticGateway` 真实分支，保留 AWAITING_COMPILER 降级路径（上游缺失时）。
- **产物：** Gateway 增强 + 集成测试 + `.fspm/evidence/p8-resolver/`。
- **验收标准：** 上游就绪则真实解析；未就绪则 BLOCKED（禁伪造）。

### 节点 MCP-08-04：真实 ISymbol 获取

- **开发目标：** 拿到真实 `INamedTypeSymbol` / `IPropertySymbol` / `IMethodSymbol`。
- **前置条件：** MCP-08-03 非 BLOCKED。
- **逻辑路径（V6.1-03）：** SemanticQuery → ISemanticGateway → Core SemanticResolver → SemanticRef / 真实 ISymbol → MCP Projection。
- **实现方法：** 由 Core SemanticResolver 返回真实 Symbol / Bound Semantic Result，MCP 仅进行 Projection；禁止 MCP 在拿到 SemanticRef 后调用 `Compilation.GetTypeByMetadataName` 自行二次解析（否则 MCP 长成第二个 Semantic Resolver，违反"MCP = Adapter"冻结）；禁止 `new SemanticType(...)` 冒充。
- **产物：** 集成测试（断言 Symbol 非 null 且 Kind 正确）+ `.fspm/evidence/p8-symbol/`。
- **验收标准：** 三类 Symbol 真实可达。

### 节点 MCP-08-05：SemanticProjectionMapper

- **开发目标：** ISymbol → SemanticResponse（含 SemanticKind / QualifiedName / SymbolIdentity / SourceLocation / ContainingSymbol）。
- **前置条件：** MCP-08-04 完成。
- **逻辑路径：** Symbol.Kind 分派 → 字段抽取 → Response 组装。
- **实现方法：** 新建 `Mapping/SemanticProjectionMapper.cs`。
- **产物：** 1 个源文件 + 映射单测 + `.fspm/evidence/p8-projection/`。
- **验收标准：** 五字段齐全；QualifiedName 与源码一致。

### 节点 MCP-08-06：SourceLocation 投影

- **开发目标：** FilePath / StartLine / StartColumn / EndLine / EndColumn 真实投影。
- **前置条件：** MCP-08-05 完成。
- **逻辑路径：** Symbol.Locations → LineSpan → 五字段。
- **实现方法：** 在 ProjectionMapper 内实现；无位置的 Symbol（如 ErrorType）返回显式缺失态而非伪造行号。
- **产物：** 映射增强 + 单测 + `.fspm/evidence/p8-location/`。
- **验收标准：** 行列号与 IDE 打开位置一致。

### 节点 MCP-08-07：Understand Golden Test（四场景）

- **开发目标：** `User` / `User.UserName` / `User.Password` / `User.Login` 四场景金色测试。
- **前置条件：** MCP-08-06 完成。
- **逻辑路径：** 真实 fixture 工程 → 四次调用 → 断言 QualifiedName + Kind + Location。
- **实现方法：** 新建 `Semantic/UnderstandE2ETests.cs`（4 Fact）。
- **产物：** 测试文件 + `.fspm/evidence/p8-golden/`。
- **验收标准：** 4/4 PASS。

### 节点 MCP-08-08：Understand E2E 全链

- **开发目标：** MCP → Workspace → Project → Compilation → SemanticResolver → ISymbol → Projection → MCP Response 全链贯通。
- **前置条件：** MCP-08-07 完成。
- **逻辑路径：** 真实 MCP Client 经 stdio 调用 `fspm_understand` → 全链 → 结构化响应。
- **实现方法：** E2E 测试复用 P5 基座。
- **产物：** E2E 测试 + `.fspm/evidence/p8-e2e/`。
- **验收标准：** 全链 0 伪造；响应含真实 SourceLocation。

### P8 Gate

```text
User PASS / User.UserName PASS / User.Password PASS / User.Login PASS /
Real Compilation PASS / Real ISymbol PASS / Real SourceLocation PASS
```

**P8 完成后：FSPM MCP 第一个真实能力成立。**

---

## §7 P9 — FSPM_CONSTRUCT 请求与规划适配

### 节点 MCP-09-01：ConstructRequest 冻结

- **开发目标：** 冻结请求形状（workspace / target / operation / intent / parameters）。
- **前置条件：** P8 Gate 通过。
- **逻辑路径：** Tool 参数 → ConstructRequest（经 P6 Validator）。
- **实现方法：** 新建 `Tools/Requests/ConstructRequest.cs`。
- **产物：** 1 个源文件 + 单测 + `.fspm/evidence/p9-request/`。
- **验收标准：** 形状冻结，缺字段可拦截。

### 节点 MCP-09-02：Intent Adapter（透传）

- **开发目标：** MCP 只做 deserialize + validate + pass-through；IntentParser 归 Core。
- **前置条件：** MCP-09-01 完成。
- **逻辑路径：** intent 原文 → 校验非空 → 原样转发 Core。
- **实现方法：** 在 ConstructionGateway 内实现透传分支。
- **产物：** Gateway 增强 + 单测 + `.fspm/evidence/p9-intent/`。
- **验收标准：** MCP 侧无任何意图解析逻辑（代码审计）。

### 节点 MCP-09-03：Semantic Target Resolution

- **开发目标：** 经 SemanticResolver 获得 SemanticRef（复用 P8 能力）。
- **前置条件：** MCP-09-02 完成 + P8 真实化完成。
- **逻辑路径：** ConstructRequest.target → P8 TargetParser → SemanticResolver → SemanticRef。
- **实现方法：** ConstructionGateway 调用 SemanticGateway。
- **产物：** 集成测试 + `.fspm/evidence/p9-target/`。
- **验收标准：** 未解析出的 Target 直接失败，不进入规划。

### 节点 MCP-09-04：Construction Gateway 接入 ConstructionService

- **开发目标：** 调用 Core `ConstructionService` 获得真实 construction plan。MCP 不实现 Planning。
- **前置条件：** MCP-09-03 完成 + 上游交付 ConstructionService（FSPM-13/14）；否则 BLOCKED。
- **逻辑路径：** SemanticRef + intent → ConstructionService.PlanAsync → plan。
- **实现方法：** 充实 ConstructionGateway 真实分支。
- **产物：** Gateway 增强 + 集成测试 + `.fspm/evidence/p9-gateway/`。
- **验收标准：** 真实 plan 返回；未就绪则 BLOCKED。

### 节点 MCP-09-05：Construction Result Projection（PLANNED）

- **开发目标：** 投影 status / target / planId；明确 `PLANNED != CONSTRUCTED`。
- **前置条件：** MCP-09-04 非 BLOCKED。
- **逻辑路径：** plan → 投影（此时尚未改源码，状态只能是 PLANNED）。
- **实现方法：** 新建 `Mapping/ConstructionProjectionMapper.cs`。
- **产物：** 1 个源文件 + 单测 + `.fspm/evidence/p9-projection/`。
- **验收标准：** PLANNED 态无 changedFiles 断言；状态机审计通过。

### P9 Gate

```text
Construction request → Target resolution → ConstructionService → real construction plan
```

---

## §8 P10 — 源码变更与重编重绑（CONSTRUCTED）

### 节点 MCP-10-01：ConstructionFixture 夹具工程

- **开发目标：** small / deterministic / recoverable 的专用 fixture 工程。
- **前置条件：** P9 Gate 通过。
- **逻辑路径：** 选最小可编译 fixture → 纳入版本控制 → 快照基线。
- **实现方法：** 新建 `backend/tests/fixtures/ConstructionFixture/`（独立小工程 + 还原脚本）。
- **产物：** 夹具工程 + 还原说明 + `.fspm/evidence/p10-fixture/`。
- **验收标准：** 夹具可独立 build；可一键还原。

### 节点 MCP-10-02：Before Fingerprint

- **开发目标：** 记录变更前源码 hash。
- **前置条件：** MCP-10-01 完成。
- **逻辑路径：** 枚举 fixture 源码文件 → SHA256 → beforeFingerprint。
- **实现方法：** 在 ConstructionGateway 调用前计算（复用 Evidence SHA256 协议语义）。
- **产物：** 指纹工具 + 单测 + `.fspm/evidence/p10-before/`。
- **验收标准：** 同一源码多次计算指纹稳定一致。

### 节点 MCP-10-03：Source Mutation（SourceMutationEngine）

- **开发目标：** MCP 调用 Core `SourceMutationEngine`，获得 changedFiles / diffSummary / writerTransactionId。
- **前置条件：** MCP-10-02 完成 + 上游交付 MutationEngine；否则 BLOCKED。
- **逻辑路径：** plan → MutationEngine.MutateAsync → 变更结果。
- **实现方法：** ConstructionGateway 真实分支；MCP 不直接写文件（原子写归 Engine）。
- **产物：** Gateway 增强 + 集成测试 + `.fspm/evidence/p10-mutation/`。
- **验收标准：** 返回三要素齐全。

### 节点 MCP-10-04：Verify Source Changed

- **开发目标：** beforeFingerprint != afterFingerprint（预期变更场景）。
- **前置条件：** MCP-10-03 非 BLOCKED。
- **逻辑路径：** 变更后重算指纹 → 比对。
- **实现方法：** 测试断言 + 网关内校验；预期无修改场景须符合 Contract 显式声明。
- **产物：** 断言测试 + `.fspm/evidence/p10-changed/`。
- **验收标准：** 变更场景指纹必变；未变则 FAIL（禁静默通过）。

### 节点 MCP-10-05：Build（Core Verification/Build Pipeline）

- **开发目标：** 获得真实 BuildResult；MCP 禁止自研 Build Framework。
- **前置条件：** MCP-10-04 完成。
- **逻辑路径：** 变更后工程 → Core Build 管线 → BuildResult。
- **实现方法：** 经 VerificationGateway 调用 Core Build Pipeline 的真实 Build 能力，Build 的真正执行者是 Core（V6.1-04）；若 Core 尚未提供 Build Pipeline 则本节点为 BLOCKED，禁止 MCP 以 `Process.Start("dotnet", ...)` 自建构建充数。
- **产物：** 集成测试 + `.fspm/evidence/p10-build/`。
- **验收标准：** Build 成功/失败均如实投影。

### 节点 MCP-10-06：Rebind

- **开发目标：** 重新获取 Compilation 并重解析 SemanticRef；证明 old != new。
- **前置条件：** MCP-10-05 Build PASS。
- **逻辑路径（V6.1-04）：** Mutation → Build → 重建 Compilation → Core SemanticResolver 重新执行 → 新 SemanticRef → 身份比对。重绑必须由 Core Resolver 完成，MCP 禁止自行重解析。
- **实现方法：** 复用 P7 身份机制 + P8 解析链。
- **产物：** 集成测试 + `.fspm/evidence/p10-rebind/`。
- **验收标准：** 新旧 Compilation 身份不同；Target 可重解析。

### 节点 MCP-10-07：ConstructionEvidence 组装

- **开发目标：** target / changedFiles / beforeFingerprint / afterFingerprint / diffSummary / writerTransactionId / timestamp / status / reason 九字段齐全。
- **前置条件：** MCP-10-06 完成。
- **逻辑路径：** 各阶段产物 → Evidence 组装 → 落盘。
- **实现方法：** 经 McpEvidenceAdapter（见 P12）落盘。
- **产物：** Evidence 样例 + `.fspm/evidence/p10-evidence/`。
- **验收标准：** 九字段缺一即 FAIL。

### P10 Gate

```text
source changed → build passed → new compilation exists →
target can re-resolve → construction evidence exists → CONSTRUCTED
```

---

## §9 P11 — FSPM_VERIFY 真实化

### 节点 MCP-11-01：VerifyRequest 冻结

- **开发目标：** 冻结 workspace / scope / target / verificationLevel。
- **前置条件：** P10 Gate 通过。
- **逻辑路径：** Tool 参数 → VerifyRequest（经 P6 Validator）。
- **实现方法：** 新建 `Tools/Requests/VerifyRequest.cs`。
- **产物：** 1 个源文件 + 单测 + `.fspm/evidence/p11-request/`。
- **验收标准：** 形状冻结。

### 节点 MCP-11-02：Verification Gateway 接入 VerificationOrchestrator

- **开发目标：** 调用 Core `VerificationOrchestrator`；上游未交付（FSPM-04..12/17/18）则 BLOCKED。
- **前置条件：** MCP-11-01 完成。
- **逻辑路径：** VerifyRequest → Orchestrator → 分阶段执行。
- **实现方法：** 充实 VerificationGateway 真实分支。
- **产物：** Gateway 增强 + 集成测试 + `.fspm/evidence/p11-gateway/`。
- **验收标准：** 真实编排；未就绪 BLOCKED。

### 节点 MCP-11-03：Analysis Gate 投影

- **开发目标：** 投影 RuleId / Diagnostic / Severity / Evidence / Status。
- **前置条件：** MCP-11-02 非 BLOCKED。
- **逻辑路径：** Analyzer 结果 → 投影。
- **实现方法：** 新建 `Mapping/VerificationProjectionMapper.cs`（Analysis 部分）。
- **产物：** 映射 + 单测 + `.fspm/evidence/p11-analysis/`。
- **验收标准：** 规则命中如实投影。

### 节点 MCP-11-04：Build Gate 投影

- **开发目标：** 投影 Build status / Errors / Warnings / Evidence。
- **前置条件：** MCP-11-03 完成。
- **逻辑路径：** Build 结果 → 投影。
- **实现方法：** 同上（Build 部分）。
- **产物：** 映射增强 + 单测 + `.fspm/evidence/p11-build/`。
- **验收标准：** 错误警告如实投影。

### 节点 MCP-11-05：Test Gate 投影

- **开发目标：** 投影 Test count / Passed / Failed / Skipped / Duration。
- **前置条件：** MCP-11-04 完成。
- **逻辑路径：** 测试运行结果 → 投影。
- **实现方法：** 同上（Test 部分）。
- **产物：** 映射增强 + 单测 + `.fspm/evidence/p11-test/`。
- **验收标准：** 五字段齐全。

### 节点 MCP-11-06：Runtime Gate（条件）

- **开发目标：** 若存在 Runtime/HTTP/Login MVP 则真实调用，否则显式 NOT_ESTABLISHED。
- **前置条件：** MCP-11-05 完成。
- **逻辑路径：** 上游 Runtime 存在性判定 → 真实调用或 NOT_ESTABLISHED。
- **实现方法：** 条件分支 + 显式状态（禁伪造 Runtime 通过）。
- **产物：** 映射增强 + `.fspm/evidence/p11-runtime/`。
- **验收标准：** 无 Runtime 时状态为 NOT_ESTABLISHED 而非 PASS。

### 节点 MCP-11-07：Hard Gate 语义

- **开发目标：** Analysis=FAIL ⇒ Build/Test=NOT_RUN。
- **前置条件：** MCP-11-06 完成。
- **逻辑路径：** 阶段失败 → 后续阶段置 NOT_RUN（非 SKIP/FAIL）。
- **实现方法：** Orchestrator 调用侧强制短路 + 测试覆盖。
- **产物：** 短路逻辑 + 单测 + `.fspm/evidence/p11-hardgate/`。
- **验收标准：** 失败短路行为可测试复现。

### 节点 MCP-11-08：Verification Response 组装

- **开发目标：** Analysis / Build / Test / Runtime / Final 五段齐全。
- **前置条件：** MCP-11-07 完成。
- **逻辑路径：** 各段投影 → Final 判定 → Response。
- **实现方法：** Response 组装 + E2E。
- **产物：** E2E 测试 + `.fspm/evidence/p11-response/`。
- **验收标准：** 五段缺一即 FAIL。

### P11 Gate

```text
Verify request → Orchestrator → Analysis → Build → Test → Evidence
```

---

## §10 P12 — 证据集成

> **冻结（V6.1-05）：Evidence Authority = Core；Evidence Transport / Persistence / Correlation = MCP。**
> MCP 不得判定 Analysis / Construction / Verification 通过与否，不得生成任何核心 Evidence 事实；
> Core 产出 Authoritative Evidence，MCP 的 Evidence Adapter 只负责 persist / expose / correlate。

### 节点 MCP-12-01：Execution Folder 规范

- **开发目标：** 统一 `.fspm/evidence/<execution-id>/` 目录规范。
- **前置条件：** P11 Gate 通过。
- **逻辑路径：** ExecutionId → 目录创建 → 后续文件写入。
- **实现方法：** 新建 `Evidence/McpEvidenceAdapter.cs`（目录管理部分）。
- **产物：** 1 个源文件 + 单测 + `.fspm/evidence/p12-folder/`。
- **验收标准：** 目录名与 ExecutionId 一一对应。

### 节点 MCP-12-02 ~ MCP-12-06：六类证据文件

- **开发目标：** request.json / semantic.json / construction.json / analysis.json+build.json+test.json+runtime.json / result.json 逐一落地。
- **前置条件：** MCP-12-01 完成。
- **逻辑路径：** 各阶段产物 → 对应 JSON → 落盘。
- **实现方法：** McpEvidenceAdapter 分阶段写文件方法。
- **产物：** Adapter 增强 + 样例文件 + `.fspm/evidence/p12-files/`。
- **验收标准：** 字段清单见总纲 §10（P12.2–P12.6），缺字段即 FAIL。

### 节点 MCP-12-07：Evidence Correlation

- **开发目标：** ExecutionId ↔ Evidence 目录 ↔ MCP Response 可反向关联。
- **前置条件：** MCP-12-06 完成。
- **逻辑路径：** Response 携带 executionId + evidencePath → 反查。
- **实现方法：** Response 信封补 evidencePath 字段 + 反查测试。
- **产物：** 信封增强 + 反查测试 + `.fspm/evidence/p12-correlation/`。
- **验收标准：** 任一 Response 可反查到完整 evidence 目录。

### P12 Gate

```text
任何 understand / construct / verify 调用均可找到对应 Evidence
```

---

## §11 P13 — 全链路垂直切片（终极功能验收）

### 节点 MCP-13-01：Start Server

- **开发目标 / 前置条件 / 逻辑路径 / 实现方法：** 复用 P5 基座拉起真实 MCP Server。
- **产物：** smoke 测试基座。
- **验收标准：** Server 启动 0 异常。

### 节点 MCP-13-02：Discover

- **开发目标：** 真实 Registry 得到 3 Tools。
- **产物：** 断言（名称精确）。
- **验收标准：** 数量与名称精确匹配。

### 节点 MCP-13-03：Understand（四场景）

- **开发目标：** `User` / `User.UserName` / `User.Password` / `User.Login` 真实解析。
- **前置条件：** P8 完成。
- **产物：** smoke 断言。
- **验收标准：** 四场景全部真实 Symbol。

### 节点 MCP-13-04 ~ MCP-13-06：Construct → Build → Rebind

- **开发目标：** 在 fixture 上执行真实变更、重编、重绑。
- **前置条件：** P10 完成。
- **产物：** smoke 断言 + fixture 还原。
- **验收标准：** CONSTRUCTED 达成且 fixture 可还原。

### 节点 MCP-13-07：Verify

- **开发目标：** 调用 `fspm_verify` 得全段响应。
- **前置条件：** P11 完成。
- **产物：** smoke 断言。
- **验收标准：** 五段齐全。

### 节点 MCP-13-08：Evidence

- **开发目标：** execution-id 下九类文件齐全（request/semantic/construction/analysis/build/test/runtime/result）。
- **前置条件：** P12 完成。
- **产物：** 文件存在性断言。
- **验收标准：** 缺一即 FAIL。

### 节点 MCP-13-09：Full Smoke 自动化

- **开发目标：** 一条测试跑完全链：Start → Discover → Understand → Construct → Build → Rebind → Verify → Evidence。
- **前置条件：** MCP-13-01 ~ MCP-13-08 完成。
- **逻辑路径：** 顺序编排 + 每步断言 + 失败即停并保留现场。
- **实现方法：** 新建 `Full/FullVerticalSliceTests.cs`。
- **产物：** 1 个测试文件 + `.fspm/evidence/p13-smoke/`。
- **验收标准：** 全绿；任一步失败则整体 FAIL。

### P13 Gate

```text
Understand PASS / Construct PASS / Build PASS /
Rebind PASS / Verify PASS / Evidence PASS → MCP Vertical Slice COMPLETE
```

---

## §12 P14 — 生产加固（不阻塞主线，可与 P13 并行收尾）

| 节点 | 开发目标 | 前置条件 | 实现方法 | 产物 | 验收标准 |
|---|---|---|---|---|---|
| MCP-14-01 输入健壮性 | null/empty/非法/未知 target-project 全覆盖 | P13 进行中 | 异常输入矩阵测试 | `Hardening/InputTests.cs` + evidence | 每类输入有确定行为 |
| MCP-14-02 异常映射 | Core/IO/编译/变更/构建/测试失败全验证 | MCP-14-01 | 故障注入测试 | `Hardening/ExceptionTests.cs` + evidence | 无裸抛、无假成功 |
| MCP-14-03 取消语义 | CancellationToken 取消不留半写 | MCP-14-02 | 取消测试 | `Hardening/CancellationTests.cs` + evidence | 取消后工作区可恢复 |
| MCP-14-04 并发 | 同工作区/异工作区/并行 understand-verify | MCP-14-03 | 并行测试 | `Hardening/ConcurrencyTests.cs` + evidence | 无竞态污染 |
| MCP-14-05 恢复 | Build/Mutation FAIL 后工作区可恢复 | MCP-14-04 | 恢复测试 | `Hardening/RecoveryTests.cs` + evidence | 失败后状态已知可恢复 |
| MCP-14-06 幂等 | understand/verify/construct 重复执行符合 Contract | MCP-14-05 | 重复执行测试 | `Hardening/IdempotencyTests.cs` + evidence | 重复结果符合契约 |
| MCP-14-07 性能基线 | 启动/发现/understand/construct/verify 耗时基线 | MCP-14-06 | 计时记录 | `p14-perf-baseline.json` + evidence | 基线落盘，可回归比对 |

---

## §13 P15 — 正式封存

### 节点 MCP-15-01：MCP-FINAL-VERIFICATION 与冻结

- **开发目标：** 形成封存文档并冻结工作面。
- **前置条件：** P13 Gate 通过（P14 至少完成 14-01~14-03）。
- **逻辑路径：** 汇总架构/契约/构建/测试/E2E/证据/已知限制/上游依赖 → 评审 → 冻结。
- **实现方法：** 编写 `docs/FSPM/MCP-FINAL-VERIFICATION.md`，含 Architecture / Tool Contracts / Build / Tests / E2E / Evidence / Known Limitations / Upstream Dependencies 八节。
- **产物：** 封存文档 + 最终全量测试报告。
- **验收标准：** 八节齐全；冻结后 MCP 侧只接受缺陷修复，不再加功能。

---

## §14 目标代码与测试结构

### 14.1 MCP 工程目标结构（按 Phase 渐进创建，禁止一次建空类）

```text
Foundry.FSPM.Mcp/
├── Program.cs
├── Hosting/McpServerHost.cs（P6，如需从 Program.cs 拆分时）
├── Tools/
│   ├── FspmUnderstandTool.cs / FspmConstructTool.cs / FspmVerifyTool.cs
│   └── Requests/UnderstandRequest.cs / ConstructRequest.cs / VerifyRequest.cs（P8/P9/P11）
├── Execution/McpExecutionContext.cs / McpExecutionContextFactory.cs / McpExecutionPipeline.cs / McpOperationResult.cs（P6）
├── Validation/IMcpRequestValidator.cs / McpRequestValidator.cs（P6）
├── Workspace/IMcpWorkspaceResolver.cs / McpWorkspaceResolver.cs / ISolutionResolver.cs / IProjectResolver.cs / CompilationIdentity.cs（P6/P7）
├── Gateways/ISemanticGateway.cs / IConstructionGateway.cs / IVerificationGateway.cs / ICompilationProvider.cs（P6/P7）
├── Mapping/SemanticQueryParser.cs / SemanticProjectionMapper.cs / ConstructionProjectionMapper.cs / VerificationProjectionMapper.cs（P8/P9/P11）
├── Errors/IMcpExceptionMapper.cs / McpExceptionMapper.cs（P6）
└── Evidence/McpEvidenceAdapter.cs（P12）
```

### 14.2 测试工程目标结构

```text
Foundry.FSPM.Mcp.Tests/
├── Infrastructure/McpTestServerFactory.cs / McpClientFixture.cs / McpResponseAssertions.cs（P5）
├── Discovery/McpDiscoveryTests.cs + McpServerLifecycleTests.cs（P5）
├── Contract/ToolContractTests.cs（P5，由 AwaitingContractTests 演进）
├── Semantic/UnderstandE2ETests.cs（P8）
├── Construction/ConstructE2ETests.cs（P10）
├── Verification/VerifyE2ETests.cs（P11）
├── Evidence/EvidenceE2ETests.cs（P12）
├── Hardening/InputTests.cs / ExceptionTests.cs / CancellationTests.cs / ConcurrencyTests.cs / RecoveryTests.cs / IdempotencyTests.cs（P14）
└── Full/FullVerticalSliceTests.cs（P13）
```

---

## §15 最终 15 事实映射表

| 事实 | 含义 | 验收 Phase |
|---|---|---|
| F01 | 真实 JNPF Compilation 可达 | P7 |
| F02 | 真实 Type Symbol | P8 |
| F03 | 真实 Property Symbol | P8 |
| F04 | 真实 Method Symbol | P8 |
| F05 | SemanticRef ↔ Symbol 对应 | P8 |
| F06 | AI Intent ↔ Symbol 对应 | P9 |
| F07 | Source Span 真实 | P8 |
| F08 | 真实 Source Mutation | P10 |
| F09 | 重编成功 | P10 |
| F10 | 重绑成功 | P10 |
| F11 | Analyzer 真实执行 | P11 |
| F12 | Test 真实执行 | P11 |
| F13 | Login.Mvp Runtime 真实（或显式 NOT_ESTABLISHED） | P11 |
| F14 | Evidence 全链可追溯 | P12 |
| F15 | MCP stdio 入口真实 | P5 |

---

## §16 上游缺口管理（MCP_CORE_API_GAP）

MCP 工程师必须维护 `docs/FSPM/MCP_CORE_API_GAP.md`，每缺口记录 Capability / Required API / Input / Output / Current State / Owner / Blocking Phase：

| Capability | Required API | Owner | Blocking |
|---|---|---|---|
| Compilation | ICompilationProvider | Compiler/Core | P7 |
| Resolve Type | ISemanticResolver | Compiler/Core | P8 |
| Resolve Property | ResolvePropertyAsync | Compiler/Core | P8 |
| Resolve Method | ResolveMethodAsync | Compiler/Core | P8 |
| Construct | IConstructionService | Core | P9 |
| Mutation | ISourceMutationEngine | Core | P10 |
| Verify 编排 | IVerificationOrchestrator | Core | P11 |
| Analyzer | 4 Analyzer + FspmRuleIds | Core/Analyzer | P11 |
| Evidence | IEvidenceStore | Core | P12 |

Compiler 每交付一个 API，MCP 立即集成，无需重新设计。

---

## §17 并行施工调度（V6.1-06，替代纯串行）

MCP 与 Compiler 并行施工；MCP 先行完成全部不依赖上游的工作（P5/P6/P7 + 网关契约 + 测试基座），上游 API 到达即按 §20 规则接入对应 Phase。

```text
                 ┌──────── MCP P5（Foundation：9/9 + 基座）
                 │
                 ├──────── MCP P6（执行框架 + 网关契约占位）
                 │
                 ├──────── MCP P7（工作区定位；Compilation 缺则 BLOCKED+缺口文档）
                 │
                 └──────── MCP Adapter Harness 就绪
                         │
Compiler ────────────────┤
   ↓                     │
Compilation API ─────────┤
   ↓                     ▼
Semantic API ─────────→ MCP P8（Understand 真实化）
                          │
Construction API ───────→ MCP P9/P10（规划 + 变更/重编/重绑）
                          │
Verification API ───────→ MCP P11（Verify 真实化）
                          │
Evidence API ───────────→ MCP P12（证据集成）
                          │
                          ▼
                        MCP P13（Full Smoke）
```

调度铁律：MCP-06、MCP-07、测试基础设施、Gateway Contract 完全可以先干完，不等待 Compiler；
Compiler 一旦交付某 API，MCP 立即按 §20 接入对应 Phase，不从头盘点。

今日优先级（同图从上到下）：TODAY-P0 P5 → TODAY-P1 P6 → TODAY-P2 P7 → 上游到达即 TODAY-P3 P8 → P9/P10 → P11 → P12 → P13。

---

## §18 总体原则（四条）

```text
① 先跑通 ② 纵向打通 ③ 局部缺陷连续修 ④ 真正上游阻塞才停
```

禁止：为完美暂停整体推进、为测试而测试、为抽象而抽象、为 MCP 重造 FSPM Core。

最终交付：**一个 AI 可通过 MCP 真正操作 FSPM 的可执行软件系统。**

---

## §19 节点状态机（V6.1-07）

每个开发节点除"前置/产物/验收"外，必须显式标注以下五态之一：

```text
READY（前置齐备，可开工）→ IN_PROGRESS（施工中）→ PASS（验收通过）
↘ FAILED（验收失败，就地修复后回 IN_PROGRESS）
↘ BLOCKED（前置中的上游依赖缺失，见下）
```

### 上游依赖节点的双态记录

凡前置条件含上游 API 的节点（MCP-07-04、MCP-08-03、MCP-09-04、MCP-10-03、MCP-11-02 及同类），必须分开记录两行状态：

```text
Adapter Contract（接口形状/占位/测试替身）：READY / PASS
Implementation（真实 Core 调用贯通）：READY / BLOCKED / PASS
```

`Adapter Contract = PASS` 而 `Implementation = BLOCKED` 是合法的中间态，表示"MCP 侧已就绪，只等上游"。
BLOCKED 只冻结本节点的 Implementation 部分，不得被理解为"整个 Phase / 整个 MCP 停工"——MCP 先行工作（§17）继续推进。

### 状态存放

节点状态记录在对应 evidence 目录的 `node-status.json`（字段：node / adapterContract / implementation / updatedAt / blocker），随每次 Gate 更新。

---

## §20 上游 API 到达自动解 Block 规则（V6.1-08）

当 Compiler/Core 交付某节点所需的上游 API 时，无需新的架构师裁决，执行：

```text
Implementation: BLOCKED → READY → IN_PROGRESS（接入真实调用，替换占位）
    → 跑本节点验收 → PASS（或 FAILED→就地修复）
```

同时更新 `docs/FSPM/MCP_CORE_API_GAP.md` 对应行的 Current State，并补 evidence。
若交付的 API 签名与 INTERFACE_LOCKDOWN 冻结面冲突，STOP 并上报（此为唯一的停止条件，其余一律连续施工）。
