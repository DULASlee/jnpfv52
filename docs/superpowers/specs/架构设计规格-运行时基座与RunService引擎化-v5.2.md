# 架构设计规格 — 运行时基座与 RunService 引擎化（v5.2 模板修订版）

- **模板**：AI 可编程设计规格模板 v5.2（终版）
- **日期**：2026-08-21（修订：替换初版；任务原子化 ≤4h）
- **状态**：Phase 1 ✅ 已确认；Phase 2-4 已经用户审查反馈 v2 修订（13 项缺陷全部整改：豁免废除/隔离纠正/终审拆分/TenantId/跨文件分页/schema 核验/TDD 顺序/超时语义/指标降级/文件面补全/工时纠偏/范围界定/字段映射/503语义/自治去重/行数依据），待统一再确认
- **上游**：`2026-08-20-runservice-engine-refactor-design.md`（A+C 子规格）· `runservice-refactor-master-plan.md` v3 · `runtime-infrastructure-gap-analysis.md` v3
- **关联 CR**：CR-20260820-01
- **节奏豁免**：用户已授权 Phase 3 任务一次性批量输出后统一确认（模板默认逐个确认）

---

# Phase 1：需求挑战与全局基线对齐（已确认，存档）

## §0 场景速写

| 维度 | 值 |
|------|-----|
| 场景类型 | 系统重构（RunService 4157 行上帝类引擎化）+ 功能迭代（四特性降级版混入） |
| 变更范围 | 跨模块（visualdev / common / inteAssistant / EventBus.Outbox / API.Entry / tests / analyzers） |
| 是否多轨并行 | 是——重构轨（纯移动+抽象）与特性轨（行为变更特性），文件面零交集 |

## §1.1 需求降维分析

**本质业务价值**：借 RunService 引擎化的开膛窗口，一次性把平台副作用（DB/事务/出站/异常/日志）挂进统一漏斗，使「租户饿死、LLM 挂起拖死线程池、事故不可查」三类结构性风险在架构上不可能发生；同时消除基座债务被推入 backlog 后永不偿付的风险。

**伪需求/过度设计判定（前轮已实证）**：全局幂等键中间件=平台级幻想（痛点不成立）；RFC 9457 全量统一=涉前端联动的越界项；告警 as-code=依赖不存在的 Grafana 基建。

## §1.2 复杂度砍刀（仅针对已提出需求及其隐含影响）

| 已提出需求点 | 砍/降级决定 | 来源 |
|-------------|------------|------|
| 异常边界（P0-2） | **降级**：Json 字段内结构化，不动表 schema（加列依赖 2.12 版本化迁移，当前缺失） | 隐含影响：schema 变更无迁移能力承载 |
| 幂等（P1-2/P2-1） | P1-2 Outbox Sweeper **保留**；P2-1 全局幂等键**整体砍除**（前端防抖+Outbox 幂等表兜底已覆盖） | 用户 Phase 1 拍板 |
| 韧性管线（P0-3） | **降级**：仅 LLM/MCP 热路径两个命名客户端，非全客户端铺开（全量留 backlog 2.3） | 隐含影响：附件下载手工重试不收敛，登记遗留 |
| 可查询日志（P1-1） | **降级**：OTel 规范文件 JSON+内置查询 API；采集端点/看板移出本期（无部署能力） | 用户 Phase 1 拍板（Conway 约束） |
| [由可查询日志隐含] PII 脱敏 | **确认纳入**——日志面扩大必然带出合规面扩大，必须同批交付 | 隐含影响识别（非构造） |
| [由混入式隐含] 特性开关基建 | **确认纳入**——行为变更混入纯移动重构必然带出定点回滚需求 | 隐含影响识别（非构造） |

## §1.3 范围边界表

| 纳入范围 | 明确排除（附排除原因） |
|---------|----------------------|
| RunService 引擎化 A+C（S0-S5：六组件拆分+IRuntimeDataStore+IRunService 17→7） | CC 降级（CC140 保持——降 CC=行为变更，违 JNPF009 基线铁律，二期 2.10） |
| F0 特性开关四布尔位 | RFC 9457 ProblemDetails——涉前端错误负载解析联动，非本期边界内 |
| F1 可查询日志降级版（文件 JSON+请求日志+脱敏+查询 API） | Seq/LGTM 部署栈——无 DevOps 角色（Conway），采集端/看板待运维就绪（2.5 余留） |
| F2 Outbox Sweeper + DB 互斥锁 | Redis 分布式锁——不可假设 Redis 在场（Cache.json memory/redis 二选一） |
| F3 出站韧性（仅 LLM/MCP） | 全客户端韧性铺开 / 附件下载手工重试收敛——面扩大，留 2.3 |
| F4 异常边界（非 HTTP 入口+Json 结构化） | 异常表加列（type/code/innerChain）——待 2.12 版本化迁移后升格 |
| 结构挂靠点声明+引擎构造白名单架构测试 | Quartz 专项边界（2.6 余留）/ 就绪探针补全（2.7）/ 缓存防击穿（2.2）/ 分区限流（2.4）——与本次文件面无交，独立 CR |

## §1.4 全局基线与组织约束确认

| 维度 | 结论 |
|------|------|
| 成本与 ROI | 以 Phase 4 §4.1 任务级实测为唯一口径：重构轨 79h + 特性轨 46h = 125h ≈ 15.6 人日（含测试与证据采集）；早期天级粗估「特性净 13 人日」已废弃（粗估含缓冲，任务级实测更准）；License 零新增（Polly v8 MIT）；云资源零新增（降级后无部署组件） |
| 安全合规基线 | PIPL：F1 脱敏与日志面扩大同批交付（已纳入）；F4 异常上下文禁入栈变量值。OWASP：IRuntimeDataStore 全参数化（L0 硬门控既有）；F1 查询 API 权限点+租户过滤+路径白名单 |
| Conway 团队约束 | 单团队无专职 DevOps——一切需部署侧介入的形态（Seq/LGTM/Grafana）已排除；F2 用 DB 锁不引入 Redis 依赖 |

## §1.5 多轨隔离声明

| 轨道 | 目标 | 文件面 |
|------|------|--------|
| 重构轨 | RunService 绞杀者式引擎化，路由契约零差异 | `backend/modularity/visualdev/JNPF.VisualDev/{RunService.cs,Runtime/}` · `JNPF.VisualDev.Interfaces/IRunService.cs` · `backend/modularity/common/Common.CodeGen/.../ExportImportDataHelper.cs`（仅 T-R5-2，CR 门禁） · `backend/tools/JNPF.Analyzers/complexity-baseline.json` · `backend/tests/JNPF.Tests.VisualDev/` · `backend/tests/JNPF.Tests.Architecture/RunEngineSqlSugarBoundaryTests.cs` |
| 特性轨 | 四特性降级版（开关门控） | F1：`application/JNPF.API.Entry/{Infrastructure/,Services/}` · F2：`infrastructure/JNPF.Extras.EventBus.Outbox/` · F3：`modularity/inteAssistant/JNPF.InteAssistant/` · F4：`modularity/common/JNPF.Common.Core/{ExceptionBoundary/,Filter/}` · F0：`framework/JNPF/`+`Configurations/App.json` · **特性轨测试载体**：`JNPF.Tests.Common`（F0/F1/F4）· `JNPF.Tests.Stage5`（F2）· `JNPF.Tests.Phase6`（F3） |

**隔离纪律**：两轨文件面禁止交叉（实证零交集）。共享文件声明：`Program.cs`/启动链（F1 请求日志注册）与 `App.json`（F0）为特性轨独有写入点，重构轨禁触；`JNPF.Tests.Architecture/` 白名单断言扩展归重构轨。

## §1.6 向用户提问

**无新问题**——模板禁止已知答案的问题。前轮三问已拍板：①部署能力→降级确认 ②开关→四特性级粒度批准 ③幂等→整体砍除只留 Sweeper。

**🔒 Phase 1 检查点：已通过（用户 2026-08-21 确认）。**

---

# Phase 2：核心取舍博弈（决策快照）

> 博弈过程已于本轮对话前段完成并拍板；按 A6 纪律此处仅存决策快照，任务级 ADR 引用编号。

## §2.1 决策难点 1：混入形态（已拍板 = 方案 C）

| 方案 | 结论 | 一句话理由 |
|------|------|-----------|
| A 严格串行 | 否决 | 违背③混入决策初衷 |
| B 双轨全并行 | 否决 | 共享冒烟基线导致归因污染 |
| **C 单轨穿插+阶段内双门禁分段裁决** | **选定** | 文件面零交集（实证）支撑有序穿插；**特性门禁红不阻塞重构门禁**为红线（下称 ADR-C） |

## §2.2 决策难点 2：回滚轴分工（已拍板 = 方案 B）

| 对象 | 回滚轴 | 一句话理由 |
|------|--------|-----------|
| 重构（纯移动、行为等价） | 阶段级 git revert | 运行时开关对行为等价代码语义为空；双路并存=拆分作废（下称 ADR-R） |
| 四特性（真实行为变更） | App.json 四布尔开关，特性级粒度，默认 false | 精确熔断，只关出问题的单特性（下称 ADR-F） |

## §2.3 核心架构图（重构场景：前后对比 + 组件交互）

**前（现状）**：

```
WorkFlow + 5 注入点 ──► RunService（4157 行，42 方法）
                          ├── 直接持有 SqlSugarScope _sqlSugarClient（唯一可变状态）
                          ├── _visualDevRepository.AsSugarClient() ×49 + _sqlSugarClient ×8
                          └── 编译/执行/列表/视图/DB路由 六职责糅合
```

**后（目标）**：

```
WorkFlow(IRunService 7方法) ─► RunService 门面(<400行, ITransient)
5 注入点 ──────────────────────┘    │ 委托
                    ┌───────────────┼───────────────┐
                    ▼               ▼               ▼
          RunListQueryService  RunDataViewService  RunDataEngine   （均 ITransient）
                    └───────────────┼───────────────┘
                                    ▼ 构造注入（白名单硬门控）
              RunSqlCompiler(ISingleton,纯函数) + IRuntimeDataStore(接口)
                                                      ▲
                                SqlSugarRuntimeDataStore（唯一 SqlSugar 绑定点）

特性轨（零交集文件面，开关门控，虚线=依赖的既有面）：
F0 App.json 四布尔 ─┬─ F1 Serilog app-sink+请求日志+脱敏+LogQuery API（API.Entry）
                    ├─ F2 OutboxSweeperService+DB锁（EventBus.Outbox）
                    ├─ F3 Polly v8 标准管线（InteAssistant LLM/MCP 命名客户端）
                    └─ F4 IExceptionBoundary 非HTTP入口 + LogExceptionHandler 结构化
```

标注：全部为进程内同步依赖注入（异步仅 Outbox 轮询与 BackgroundService 定时）；数据流单向门面→引擎→抽象→实现，无环。

---

# Phase 3：精简设计规格与任务拆分

## §0 全局架构约束（跨模块+多轨，强制填写）

| 约束 | 唯一入口 | 禁止绕过方式 | 验证手段 |
|------|---------|-------------|---------|
| 引擎 DB 副作用唯一漏斗 | `IRuntimeDataStore` | 引擎构造注入白名单外任何 DB 类型（SqlSugar/IDataBaseManager/ISqlSugarRepository/Dapper） | 架构测试构造白名单断言（T-R2-1） |
| SqlSugar 唯一绑定点 | `SqlSugarRuntimeDataStore` | 引擎引用 SqlSugar 命名空间类型 | 架构测试 SqlSugar 引用扫描（T-R2-1） |
| 路由契约零差异 | harness `--mode routes --filter "api/visualdev"` 快照 | 任何阶段快照 diff 非空即停 | 每任务收尾 Compare-Object（T-R0-1 基线） |
| JNPF009 只随迁 | `complexity-baseline.json` file/symbol 路径更新 | 值上调或新增条目 | `dotnet build /p:CI_BUILD=true` 0 新增 |
| 特性行为变更唯一开关 | `RuntimeFoundationOptions` 四布尔位 | 特性轨未门控的行为变更直接生效 | T-F0-1 单测 + S5 翻牌序列 |
| 多轨文件面隔离 | §1.5 声明 | 重构轨触特性面 / 特性轨触 RunService·Runtime·IRunService | 每任务 §3 矩阵 🚫 栏 + commit 前 `git status` 核对 |

---

## S0 安全网（重构轨）

### Task T-R0-1：路由快照基线落盘

预估工时：2h ｜ 依赖：无

**§1 验收契约**：做什么：经 harness 反射枚举全量 ActionDescriptor，过滤 `api/visualdev` 落盘为重构期唯一路由契约基线。纯后台型；监控=N/A——本任务产出物本身即验证载体（无运行时行为）。
DoD：
- [ ] 基线文件 `.claude/evidence/cr-20260820-01/s0-routes-visualdev-baseline.txt` 存在且末行含 `[METRIC] route_matched>0`
- [ ] 同法落盘 `api/permission/users` 防误伤基线一份

**§2 代码契约骨架**：本任务无新增代码契约，仅涉及 harness 既有 `--mode routes` 执行——四条豁免条件全满足（无签名/结构/存储/复杂逻辑变更）。

**§3 文件变更矩阵**：

| 类型 | 路径 | 说明 |
|------|------|------|
| ➕ 新增 | `.claude/evidence/cr-20260820-01/s0-routes-visualdev-baseline.txt` | 路由基线 |
| 🚫 禁触 | 特性轨全部文件面（§1.5） | 多轨隔离 |

**§4 五件套**：4.1 ADR：沿用 ADR-C（纯移动纪律的验证前提）。4.2 安全：N/A——只读反射枚举无敏感面。4.3 灰度：N/A——证据文件无发布面。4.4 风险：已知风险=harness inproc 下 DatabaseModule 注册失败致路由缺失（performance-baseline 已登记 F2 缺陷）；回滚=重跑并核对 route_total 与上次基线量级。4.5 测试：断言 `route_matched>0` 且含 OnlineDev/Base/ShortLink 三委托方代表路由各 ≥1 条；SLO=N/A（无运行时行为）。

**§5 六腿**：全 N/A——纯证据采集任务（运行既有 harness 命令），无设计取舍。

**§6 提问**：无待确认项。

### Task T-R0-2：IRunService 契约守护测试

预估工时：3h ｜ 依赖：无

**§1 验收契约**：做什么：反射断言 IRunService 17 成员签名冻结 + WorkFlow 消费 7 方法存在。纯后台型；监控=N/A——测试即载体。
DoD：
- [ ] `RunServiceContractTests` 全绿：成员数=17、签名字符串集合与开工回填基线一致、7 个 nameof 成员存在
- [ ] 回填基线经 `typeof(IRunService).GetMethods()` 实际输出固化（禁止手写猜测）

**§2 代码契约骨架**（新增测试类，含签名）：

```csharp
public class RunServiceContractTests
{
    [Fact] public void IRunService_MemberCount_IsSeventeen();
    [Fact] public void IRunService_MemberSignatures_AreFrozen(); // 期望集=开工回填的 ExpectedSignatures 常量数组
    [Theory] [MemberData(nameof(WorkFlowConsumed))]
    public void WorkFlowConsumed_Method_Exists(string methodName); // 7 方法：SaveFlowFormData/GetFlowFormDataDetails/SaveDataToDataByFId/GetDbLink/GetVisualDevModelDataConfig/GetCreateSqlByTemplate/GetUpdateSqlByTemplate
}
```

**§3 矩阵**：➕ `backend/tests/JNPF.Tests.VisualDev/RunServiceContractTests.cs` ｜ 🚫 特性轨文件面。

**§4 五件套**：4.1 ADR：沿用 ADR-C。4.2 安全：N/A——反射只读。4.3 灰度：N/A。4.4 风险：nameof 与接口实际成员不符→处置：以接口为准修正测试，禁止改接口。4.5 测试：本身即测试任务；SLO=N/A。

**§5 六腿**：全 N/A——守护测试无性能/内存取舍。

**§6 提问**：无待确认项。

### Task T-R0-3：委托方路由归属契约测试

预估工时：2h ｜ 依赖：无

**§1 验收契约**：做什么：反射断言三委托方（VisualDevModelDataService/VisualDevService/VisualdevShortLinkService）类存在、ApiDescriptionSettings Name 值与 Route 模板冻结。
DoD：
- [ ] `VisualDevRouteOwnerTests` 全绿，三组（类名,Name,Route模板）期望值经开工反射读取固化

**§2 代码契约骨架**：

```csharp
public class VisualDevRouteOwnerTests
{
    [Theory] [MemberData(nameof(Owners))] // (typeName, expectedName, expectedRouteTemplate) 三元组，开工回填
    public void DelegateOwner_KeepsNameAndRoute(string typeName, string expectedName, string expectedRouteTemplate);
}
```

**§3 矩阵**：➕ `backend/tests/JNPF.Tests.VisualDev/VisualDevRouteOwnerTests.cs` ｜ 🚫 特性轨文件面。

**§4 五件套**：4.1 ADR：沿用 ADR-C。4.2 安全：N/A。4.3 灰度：N/A。4.4 风险：委托方实际 Route 模板与 CR 登记不符→以代码为准回填并在证据注明。4.5 测试：本身即测试；SLO=N/A。

**§5 六腿**：全 N/A——守护测试。

**§6 提问**：无待确认项。

### Task T-F0-1：RuntimeFoundationOptions 开关基建

预估工时：3h ｜ 依赖：无（特性轨）

**§1 验收契约**：做什么：App.json 增 RuntimeFoundation 节（四布尔位默认 false）+ Options 类绑定 + 默认值守护测试。纯后台型；监控=N/A——配置件，依赖消费方特性各自指标。
DoD：
- [ ] `RuntimeFoundationOptionsTests` 2 用例全绿（默认全 false / 配置绑定）
- [ ] 缺配置节时 Get 结果仍全 false（兜底断言）

**§2 代码契约骨架**：

```csharp
public class RuntimeFoundationOptions
{
    public const string Section = "RuntimeFoundation";
    public bool QueryableLogging { get; set; }
    public bool OutboxSweeper { get; set; }
    public bool OutboundResilience { get; set; }
    public bool ExceptionBoundary { get; set; }
}
```

**§3 矩阵**：➕ `backend/framework/JNPF/Options/RuntimeFoundationOptions.cs`（开工按 Options 类现行归属目录对齐）｜ ✏️ `backend/application/JNPF.API.Entry/Configurations/App.json`（追加 RuntimeFoundation 节）｜ ➕ `backend/tests/JNPF.Tests.Common/RuntimeFoundationOptionsTests.cs` ｜ 🚫 重构轨文件面（§1.5）。

**§4 五件套**：4.1 ADR：沿用 ADR-F（特性级粒度拍板）。4.2 安全：N/A——配置节无敏感数据。4.3 灰度：纯新增配置节向后兼容（缺节=全 false=现状行为）。4.4 风险：N/A——错误配置由默认 false 兜底。4.5 测试：上述 2+1 用例；SLO=N/A。

**§5 六腿**：全 N/A——纯配置绑定件（模板铁律5：全 N/A 已说明任务类型）。

**§6 提问**：无待确认项。

### Task T-F0-2：日志磁盘基线采集

预估工时：1h ｜ 依赖：无

**§1 验收契约**：做什么：采集当前 logs/ 目录体积与文件数，供 F1 磁盘风险对照（F1 灰度回滚条件依据）。
DoD：
- [ ] `f0-log-baseline.txt` 含目录总字节/文件数/error·warning 单文件最大值

**§2 代码契约骨架**：本任务无新增代码契约，仅涉及一条 PowerShell 采集命令——四条豁免条件全满足。

**§3 矩阵**：➕ `.claude/evidence/cr-20260820-01/f0-log-baseline.txt` ｜ 🚫 重构轨文件面。

**§4 五件套**：4.1 ADR：N/A——无决策（度量采集）。4.2 安全：N/A。4.3 灰度：N/A。4.4 风险：N/A。4.5 测试：DoD 即可读性断言（文件存在且三值非空）；SLO=N/A。

**§5 六腿**：全 N/A——证据采集任务。

**§6 提问**：无待确认项。

---

## S1 编译层（重构轨）

### Task T-R1-1：RunSqlCompiler 骨架创建

预估工时：1h ｜ 依赖：T-R0-2

**§1 验收契约**：做什么：建 Runtime/ 目录与空骨架类（ISingleton 标记），构建通过。
DoD：
- [ ] `dotnet build backend` 0 错误；骨架类含类级注释（职责/DI 裁定/来源）

**§2 代码契约骨架**：

```csharp
namespace JNPF.VisualDev.Runtime;
/// <summary>运行态 SQL 编译层：纯函数、零 DB 依赖（DI 约束表：Singleton）.</summary>
public class RunSqlCompiler : ISingleton { }
```

**§3 矩阵**：➕ `backend/modularity/visualdev/JNPF.VisualDev/Runtime/RunSqlCompiler.cs` ｜ 🚫 特性轨文件面。

**§4 五件套**：4.1 ADR：沿用 A+C spec §3.1 组件裁定。4.2 安全：N/A。4.3 灰度：N/A——纯移动纪律下骨架无行为面。4.4 风险：N/A。4.5 测试：构建即验证；SLO=N/A。

**§5 六腿**：全 N/A——空骨架。

**§6 提问**：无待确认项。

### Task T-R1-2：编译层七方法纯移动

预估工时：4h ｜ 依赖：T-R1-1

**§1 验收契约**：做什么：移动 GetListQuerySql/GetInfoQuerySql/GetQueryJson/GetSuperQueryJson/GetSuperQueryInput/GetIConditionalModelListByTableName/GetVisualDevModelDataConfig 及其专属私有辅助至 RunSqlCompiler；RunService 调用点改 `_compiler.X`（IRunService 成员保留委托转发）。方法体逐字不改。
DoD：
- [ ] 7 方法+专属辅助全部迁出，RunService 无残留声明
- [ ] 共享辅助归属经 Find References 逐一裁定（共享者留 RunService，结果记入 commit message）
- [ ] `dotnet build backend` 0 错误

**§2 代码契约骨架**：本任务无新增代码契约，仅涉及既有方法位置迁移（签名/结构/存储/逻辑四不变）——豁免条件全满足。

**§3 矩阵**：✏️ `RunService.cs`（移出 7 方法+调用点改委托）｜ ✏️ `Runtime/RunSqlCompiler.cs`（接收方法体）｜ 🚫 特性轨文件面 ｜ 🚫 `IRunService.cs`（本任务不改接口）。

**§4 五件套**：4.1 ADR：沿用 ADR-C 纯移动纪律。4.2 安全：N/A——无新数据面。4.3 灰度：N/A——行为等价纯移动（ADR-R：回滚轴=git revert）。4.4 风险：共享辅助误迁导致其他方法编译失败→构建即暴露，revert 本 commit。4.5 测试：构建+既有 23 个 Helpers 测试全绿；SLO=N/A（行为等价，无性能语义变化）。

**§5 六腿**：全 N/A——纯位置移动，无取舍。

**§6 提问**：无待确认项。

### Task T-R1-3：编译层 DB 依赖参数化剥离

预估工时：4h ｜ 依赖：T-R1-2

**§1 验收契约**：做什么：Compiler 方法体内若存在 `_visualDevRepository`/`_sqlSugarClient` 调用，将该 DB 调用留在 RunService 侧取数，结果经参数传入 Compiler——编译层零 DB 硬约束的唯一允许签名调整（参数化重排，SQL 拼装逻辑逐字不动）。
DoD：
- [ ] `grep -r "SqlSugar\|AsSugarClient" Runtime/RunSqlCompiler.cs` 0 匹配
- [ ] 构建 0 错误 + Helpers 测试全绿

**§2 代码契约骨架**（签名变更示例——豁免不适用，必须给出）：

```csharp
// 变更前（若存在 DB 内联）：private string GetXxx(QueryInput input) { var meta = _visualDevRepository...; ... }
// 变更后：public string GetXxx(QueryInput input, List<FieldMeta> preloadedMeta) { ... } // 取数留在调用方
```
实际剥离清单开工时以 grep 实测为准；若实测 Compiler 七方法零 DB 调用，本任务降级为断言任务（仅补 grep 断言并记录）。

**§3 矩阵**：✏️ `Runtime/RunSqlCompiler.cs`（签名参数化）｜ ✏️ `RunService.cs`（调用点补取数传参）｜ 🚫 特性轨文件面。

**§4 五件套**：4.1 ADR：任务特有——剥离边界=「取数留调用方」而非「Compiler 注入仓储」，否决后者理由：编译层可单测面依赖零 DI。4.2 安全：N/A——参数化不改 SQL 构造。4.3 灰度：N/A——行为等价。4.4 风险：参数语义错位致 SQL 生成差异→T-R1-4 特征测试兜底。4.5 测试：Helpers 测试+构建；SLO=N/A。

**§5 六腿**：5.3 组件化—— Compiler 成为零依赖纯函数组件，是后续特征单测与（backlog 2.13）热路径基准的唯一可测面；防腐体现：DB 取数结果以 POCO 传入，Compiler 不见 ORM 类型。其余五腿 N/A。

**§6 提问**：无待确认项。

### Task T-R1-4：编译层 JNPF009 基线随迁

预估工时：2h ｜ 依赖：T-R1-3

**§1 验收契约**：做什么：complexity-baseline.json 中属 Compiler 的条目（GetListQuerySql CC140/GetSuperQueryInput/GetQueryJson 等）file/symbol 改指 RunSqlCompiler，值不变。
DoD：
- [ ] `dotnet build /p:CI_BUILD=true` 0 新增 JNPF009
- [ ] baseline 中 RunService→RunSqlCompiler 迁移条目数 = 实际迁出的超限方法数

**§2 代码契约骨架**：本任务无新增代码契约，仅涉及基线 JSON 的路径字段更新——豁免条件全满足。

**§3 矩阵**：✏️ `backend/tools/JNPF.Analyzers/complexity-baseline.json` ｜ 🚫 特性轨文件面。

**§4 五件套**：4.1 ADR：沿用 §0 全局约束（只随迁）。4.2 安全：N/A。4.3 灰度：N/A。4.4 风险：漏迁条目致 CI 红→构建即暴露，补齐。4.5 测试：CI_BUILD 构建即门禁；SLO=N/A。

**§5 六腿**：全 N/A——基线路径维护。

**§6 提问**：无待确认项。

### Task T-R1-5：编译层特征单测

预估工时：4h ｜ 依赖：T-R1-3

**§1 验收契约**：做什么：7 编译方法各 ≥2 特征用例（正常+边界），期望值由现状运行输出固化（禁止手写）。
DoD：
- [ ] `RunSqlCompilerTests` ≥14 用例全绿
- [ ] 覆盖 JOIN/过滤/子查询/分页/超级查询五类路径（A+C spec §8 门禁）

**§2 代码契约骨架**：

```csharp
public class RunSqlCompilerTests
{
    private readonly RunSqlCompiler _compiler = new();
    [Fact] public void GetListQuerySql_PaginationFilter_Characterization();
    [Fact] public void GetSuperQueryInput_EmptyCondition_Characterization();
    // 其余 5 方法 ×2 同模式；特征期望常量以 #region characterization 集中存放
}
```

**§3 矩阵**：➕ `backend/tests/JNPF.Tests.VisualDev/RunSqlCompilerTests.cs` ｜ 🚫 特性轨文件面。

**§4 五件套**：4.1 ADR：沿用 spec §8 单测门禁。4.2 安全：N/A。4.3 灰度：N/A。4.4 风险：特征输入构造依赖方法签名理解偏差→用例先跑现状输出再固化，天然防错。4.5 测试：本任务即测试；SLO=N/A。

**§5 六腿**：全 N/A——特征测试。

**§6 提问**：无待确认项。

### Task T-R1-6：S1 快照门禁验证

预估工时：1h ｜ 依赖：T-R1-4, T-R1-5

**§1 验收契约**：做什么：重跑路由快照与基线比对 + VisualDev 测试全量，产出 S1 节点审批材料。
DoD：
- [ ] `Compare-Object` 快照零输出；`s1-routes.txt` 落盘
- [ ] `dotnet test JNPF.Tests.VisualDev` 全绿（含新增 ≥14+2+3 用例）

**§2 代码契约骨架**：无新增代码契约——四条豁免条件全满足。

**§3 矩阵**：➕ `.claude/evidence/cr-20260820-01/s1-routes.txt` ｜ 🚫 特性轨文件面。

**§4 五件套**：4.1 ADR：沿用 ADR-C。4.2-4.4：N/A（验证任务）。4.5 测试：本任务即门禁执行；SLO=N/A。

**§5 六腿**：全 N/A——门禁验证任务。

**§6 提问**：无待确认项。

---

## S2 数据访问抽象（重构轨）+ F1 可查询日志（特性轨）

### Task T-R2-1：引擎边界架构测试（SqlSugar 扫描+构造白名单）

预估工时：3h ｜ 依赖：T-R1-6

**§1 验收契约**：做什么：新建架构测试两类断言——①引擎类字段/构造/方法签名零 SqlSugar 类型；②引擎构造参数类型 ∈ 白名单 `{RunSqlCompiler, IRuntimeDataStore, ILogger<>, IOptions<>, ICacheManager}`。S2 阶段 RunService 门面暂列豁免位（S5 恢复）。
DoD：
- [ ] 测试对 RunSqlCompiler 即刻绿；对尚未创建的引擎类以类型名清单守护（缺失时报失败信息明确）
- [ ] 白名单外注入一个测试桩类时断言确实红（反向用例）

**§2 代码契约骨架**：

```csharp
public class RunEngineSqlSugarBoundaryTests
{
    private static readonly string[] EngineTypes = { "RunSqlCompiler","RunDataEngine","RunListQueryService","RunDataViewService","RunService" };
    private static readonly HashSet<string> CtorWhitelist = new() { "RunSqlCompiler","IRuntimeDataStore","ILogger","IOptions","ICacheManager" };
    [Fact] public void EngineTypes_DoNotReferenceSqlSugar();      // 字段/ctor/方法签名 FullName 前缀扫描
    [Fact] public void EngineCtors_OnlyInjectWhitelistTypes();     // S2 期 RunService 豁免，TODO-S5 恢复
}
```

**§3 矩阵**：➕ `backend/tests/JNPF.Tests.Architecture/RunEngineSqlSugarBoundaryTests.cs` ｜ 🚫 特性轨文件面。

**§4 五件套**：4.1 ADR：任务特有——白名单泛型按开放泛型名匹配（ILogger<T> 的 T 即引擎类名，全等匹配会误杀），闭合泛型逐参校验。4.2 安全：N/A。4.3 灰度：N/A。4.4 风险：豁免位遗忘恢复→T-R5-3 显式恢复并列入 DoD。4.5 测试：正反用例各 ≥1；SLO=N/A。

**§5 六腿**：5.3 组件化——该测试是「结构挂靠点声明」的可执行化，未来 2.3 韧性装饰器的唯一前提是漏斗不被绕道破坏。其余 N/A。

**§6 提问**：无待确认项。

### Task T-R2-2：IRuntimeDataStore 契约与 RuntimeDbLink

预估工时：2h ｜ 依赖：T-R2-1

**§1 验收契约**：做什么：落 provider 中立抽象接口与中立 DTO（签名逐字采用 A+C spec §4）。
DoD：
- [ ] 接口 7 成员与 spec §4 逐字一致；构建 0 错误

**§2 代码契约骨架**：

```csharp
public interface IRuntimeDataStore
{
    string Dialect { get; }
    Task<object?> ExecuteScalarAsync(string sql, DbParameter[]? pars = null, string? tenantId = null);
    Task<int> ExecuteCommandAsync(string sql, DbParameter[]? pars = null, string? tenantId = null);
    Task<List<Dictionary<string, object>>> SqlQueryAsync(string sql, DbParameter[]? pars = null, string? tenantId = null);
    Task<DataTable> GetDataTableAsync(string sql, DbParameter[]? pars = null, string? tenantId = null);
    Task<bool> AnyAsync(string sql, DbParameter[]? pars = null, string? tenantId = null);
    Task RunInTransactionAsync(Func<Task> action, string? tenantId = null);
    RuntimeDbLink? ResolveDbLink(string linkId, string? tenantId = null);
}
public record RuntimeDbLink(string Id, string DbType, string ConnectionString);
```

**§3 矩阵**：➕ `Runtime/IRuntimeDataStore.cs` ➕ `Runtime/RuntimeDbLink.cs` ｜ 🚫 特性轨文件面。

**§4 五件套**：4.1 ADR：沿用 A+C spec §4（YAGNI：仅 49+8 现有调用面能力）。4.2 安全：契约只提供 `DbParameter[]` 参数化入口、不提供字符串拼参重载——OWASP 注入底线的契约级落点。4.3 灰度：N/A——接口声明无行为。4.4 风险：N/A。4.5 测试：编译即契约；SLO=N/A。

**§5 六腿**：5.6 对标——形态对标 Dapper 最小执行面 + EF Core ExecuteSqlRaw 参数模型；取舍：保留 DataTable 返回（现网 Utilities 12 处消费）而非强推 IEnumerable，规避全消费面改造。其余 N/A。

**§6 提问**：无待确认项。

### Task T-R2-3：SqlSugarRuntimeDataStore 实现与状态迁移

预估工时：4h ｜ 依赖：T-R2-2

**§1 验收契约**：做什么：实现 IRuntimeDataStore；RunService 的 `_sqlSugarClient` 获取/初始化/Dispose/AsTenant 语义原样迁入（ITransient+IDisposable）。
DoD：
- [ ] RunService 中 `_sqlSugarClient` 字段声明删除，改注入 `IRuntimeDataStore`（旧调用点暂留，T-R2-4/5 收敛）
- [ ] 构建 0 错误；Dispose 语义等价（原字段置空行为保留）

**§2 代码契约骨架**：

```csharp
public class SqlSugarRuntimeDataStore : IRuntimeDataStore, ITransient, IDisposable
{
    private readonly SqlSugarScope _client; // 初始化代码自 RunService 构造函数原样迁入
    public string Dialect => _client.CurrentConnectionConfig.DbType.ToString().ToLowerInvariant();
    public void Dispose() { /* 原 RunService.Dispose 语义原样迁入 */ }
    // 各方法实现体=自 RunService 对应调用点剪切（禁改写）
}
```

**§3 矩阵**：➕ `Runtime/SqlSugarRuntimeDataStore.cs` ｜ ✏️ `RunService.cs`（删字段+构造改注入）｜ 🚫 特性轨文件面。

**§4 五件套**：4.1 ADR：沿用 A+C spec §4（唯一绑定点）。4.2 安全：AsTenant×4 收敛本层，tenantId 参数缺省走当前请求上下文，与原行为一致（多租户过滤语义不脱离上下文）。4.3 灰度：N/A——等价迁移（ADR-R revert 兜底）。4.4 风险：外部数据源动态连接切换等价性→T-R2-6 活体冒烟专项。4.5 测试：构建+Helpers；SLO：外部数据源链路冒烟 200（业务口径：「配置外部数据源的在线开发功能正常出数」）。

**§5 六腿**：5.2 内存——SqlSugarScope 生命周期经 Transient+IDisposable 承接，与原 RunService 同语义（每请求图创建/释放），并发语义不变。其余 N/A。

**§6 提问**：无待确认项。

### Task T-R2-4：调用收敛（非 Queryable 类 36 处）

预估工时：⚠ 探索型 6h（输出物：收敛台账+编译全绿；逐处定位型工作不可压缩）｜ 依赖：T-R2-3

**§1 验收契约**：做什么：收敛 Utilities×12 / SqlQueryable×7 / CurrentConnectionConfig×3 / AsTenant×4 / Ado 执行查询类至 IRuntimeDataStore。范围界定（防重叠）：本任务的 SqlQueryable×7 = **已是 SqlQueryable 形态、仅需执行入口改经 IRuntimeDataStore**（台账编号 L1-L36）；LINQ Queryable→SqlQueryable 的**改写**归 T-R2-5（编号 Q1-Q27），两者不重叠。
DoD：
- [ ] 台账 `s2-convergence-ledger.md` 36 处逐行（编号 L1-L36：行号/类别/去向）全勾
- [ ] RunService 中上述类别调用清零（grep 佐证）；构建 0 错误

**§2 代码契约骨架**：无新增代码契约，仅既有调用点改经既有接口方法——豁免条件满足。

**§3 矩阵**：✏️ `RunService.cs` ｜ ✏️ `Runtime/SqlSugarRuntimeDataStore.cs` ｜ ➕ `.claude/evidence/cr-20260820-01/s2-convergence-ledger.md` ｜ 🚫 特性轨文件面。

**§4 五件套**：4.1 ADR：任务特有——Utilities 12 处拆分准则=「是否触碰 SqlSugar 类型」二值判定（provider 相关下沉 / 纯工具上移），否决主观分类。4.2 安全：全参数化迁移（L0 钩子兜底拦截插值）。4.3 灰度：N/A。4.4 风险：CurrentConnectionConfig 3 处以 Dialect/ResolveDbLink 替代后取值口径偏差→台账逐处记替代映射，冒烟核对。4.5 测试：构建+Helpers；SLO：快照零 diff（T-R2-6 统一验证）。

**§5 六腿**：全 N/A——等价收敛，判定准则已在 ADR 固化。

**§6 提问**：无待确认项。

### Task T-R2-5：Queryable 收敛与 SQL 等价比对（27 处）

预估工时：⚠ 探索型 6h（输出物：27 处比对记录+扩展成员清单；行为敏感逐处 ToSql 比对不可压缩）｜ 依赖：T-R2-4

**§1 验收契约**：做什么：运行时业务表 Queryable 改 SqlQueryable（编号 Q1-Q27，与 L 系列不重叠）；元数据实体查询按 D1 边界不改写 LINQ 形态，但其**执行入口同样收敛**（见下方元数据处置）。每处改写前后 ToSql 比对。**豁免路径已废除（v2 修订）**：不允许「保留原用法」——T-R2-3 已删 `_sqlSugarClient` 字段且白名单断言禁 SqlSugar 引用，保留原用法既无法编译也会使 S5 架构测试变红。
DoD：
- [ ] 台账追加 27 行（编号 Q1-Q27：SQL 等价判定）；**全部完成改写，无豁免残留**
- [ ] 元数据实体（VisualDevEntity 等平台表）查询处置：实体型 Queryable 语义封装为 `IRuntimeDataStore` 扩展成员（接口加成员+SqlSugarRuntimeDataStore 实现，按 T-R2-2 契约流程，扩展清单记台账 M 系列）——RunService 对 SqlSugar 零引用
- [ ] 无法等价改写为 SqlQueryable 的业务表查询：同样经 IRuntimeDataStore 扩展成员承载（而非保留 SqlSugar 直调）

**§2 代码契约骨架**：无新增代码契约主体——LINQ→SqlQueryable 等价改写；若触发 IRuntimeDataStore 扩展，扩展签名在此登记（示例）：

```csharp
// 扩展成员（仅当 M 系列台账触发时新增，逐条登记）：
Task<List<Dictionary<string, object>>> QueryByConditionAsync(string tableName, IEnumerable<IConditionalModel> conditions, string? tenantId = null);
```
核心伪代码（触发条件③：等价判定口径自然语言存在两种理解）：

```
for each queryable调用点:
    sqlBefore = query.ToSqlString()             // 改写前抓取
    改写为 SqlQueryable(同语义SQL, 参数)
    sqlAfter  = newQuery.ToSqlString()          // 改写后抓取
    if Normalize(sqlBefore) == Normalize(sqlAfter): 记等价   // Normalize=去空白+参数占位符归一
    else: 回滚该处改写，改经 IRuntimeDataStore 扩展成员承载（台账记 M 系列）——禁止保留 SqlSugar 直调
```

**§3 矩阵**：✏️ `RunService.cs` ｜ ✏️ 台账 ｜ ✏️（若触发）`Runtime/IRuntimeDataStore.cs`+`Runtime/SqlSugarRuntimeDataStore.cs`（扩展成员）｜ 🚫 特性轨文件面 ｜ 🚫 元数据实体 LINQ 表达式形态（D1 边界：不改写表达式树，仅封装执行入口）。

**§4 五件套**：4.1 ADR：沿用 spec 风险7 + 任务特有——豁免废除后的兜底路径=IRuntimeDataStore 扩展成员（接口可控、绑定点唯一），否决「恢复临时字段」理由：状态收敛单点被破坏即架构目标作废。4.2 安全：SqlQueryable 与扩展成员同样参数化。4.3 灰度：N/A。4.4 风险：LINQ 表达式树隐含排序/别名在 SQL 化后差异→Normalize 比对外加 T-R2-6 活体冒烟；别名不一致致 500 是既有踩坑（多表 Where/OrderBy 别名），逐处核对。4.5 测试：台账全勾+快照；SLO：列表出数冒烟 200。

**§5 六腿**：5.1 性能——SqlQueryable 与原 LINQ 同一 SQL 生成路径，无新增开销；设计选择仅为等价性验证成本。其余 N/A。

**§6 提问**：无待确认项。

### Task T-R2-6：S2 重构门禁验证与外部链路冒烟

预估工时：2h ｜ 依赖：T-R2-5

**§1 验收契约**：做什么：架构测试（引擎类全查）+ 快照零 diff + 外部数据源活体冒烟 + test:api。
DoD：
- [ ] `RunEngineSqlSugarBoundaryTests` 绿；快照零输出（`s2-routes.txt`）
- [ ] 外部数据源端点冒烟 200（无可用外部源→台账登记+默认库 SqlQueryable 路径冒烟替代）
- [ ] `E2E_PIPELINE_ID=311 pnpm test:api` 全绿

**§2 代码契约骨架**：无新增代码契约——豁免条件全满足。

**§3 矩阵**：➕ `s2-routes.txt` ➕ `s2-live-smoke.txt` ｜ 🚫 特性轨文件面。

**§4 五件套**：4.1 ADR：沿用 ADR-C。4.2-4.3：N/A。4.4 风险：冒烟暴露收敛遗漏→台账定位回修（可定位到具体处）。4.5 测试：本任务即门禁；SLO：快照零 diff + 冒烟 200。

**§5 六腿**：全 N/A——门禁任务（S2 重构段收尾）。

**§6 提问**：无待确认项。

### Task T-F1-1：PII 脱敏策略

预估工时：3h ｜ 依赖：T-F0-1（特性轨，S2 重构段门禁绿后开工）

**§1 验收契约**：做什么：Serilog Destructuring 策略——手机号保留前 3 后 4、身份证保留前 4 后 4，中位 `***`；属性名 ∈ {password, secret, token, apikey}（精确词表）整体 `***`。纯后台型；监控=N/A——策略组件，随 F1 整体指标。
DoD：
- [ ] `PiiDestructuringPolicyTests` 五用例全绿（手机/身份证/密码属性/无关属性不误伤/嵌套对象穿透）

**§2 代码契约骨架**：

```csharp
public class PiiDestructuringPolicy : IDestructuringPolicy
{
    public bool TryDestructure(object value, ILogEventPropertyValueFactory factory,
        [NotNullWhen(true)] out LogEventPropertyValue? result);
}
```

**§3 矩阵**：➕ `backend/application/JNPF.API.Entry/Infrastructure/PiiDestructuringPolicy.cs` ➕ `backend/tests/JNPF.Tests.Common/PiiDestructuringPolicyTests.cs` ｜ 🚫 重构轨文件面。

**§4 五件套**：4.1 ADR：任务特有——属性名匹配用精确词表，否决子串模糊匹配理由：业务字段名含 token 子串会被误伤。4.2 安全：本任务即 PIPL 落地点——脱敏规则字段级明确（见 §1 规则）；正则与词表双通道判定。4.3 灰度：挂载由 F1 开关统一门控（T-F1-2）。4.4 风险：未知类型放行不抛（边界防御）。4.5 测试：五用例；SLO=N/A（组件级）。

**§5 六腿**：5.4 健壮性——正则静态编译（避免每日志事件重编译）；未知类型直接放行。其余 N/A。

**§6 提问**：无待确认项。

### Task T-F1-2：全级别 OTel 文件 sink 与请求日志

预估工时：3h ｜ 依赖：T-F1-1

**§1 验收契约**：做什么：QueryableLogging=true 时追加 `app-.json` 全级别 sink（字段：Timestamp/Level/TraceId/SpanId/**TenantId/UserId**/SourceContext/Message/Exception——TenantId/UserId 经 TraceIdMiddleware 既有三元注入 LogContext，租户过滤的字段来源，缺失即 F1 验收不过）+ `UseSerilogRequestLogging`（TraceIdMiddleware 之后）。纯后台型；监控指标：**日志可查率**（抽 10 条请求经 TraceId 在 app 文件命中率）=100%；告警=N/A——降级形态无采集端，部署栈就绪后由 2.5 余留承接（诚实登记）。
DoD：
- [ ] 开关 true：app-{date}.json 生成且含 TraceId **与 TenantId** 字段；请求日志行含方法/路径/状态码/耗时
- [ ] 开关 false：不生成 app 文件、无请求日志行

**§2 代码契约骨架**：

```csharp
// SerilogBootstrap.Configure（开关经 IConfiguration 读——Serilog 配置期 DI 未就绪）
if (cfg.GetSection(RuntimeFoundationOptions.Section)
       .GetValue<bool>(nameof(RuntimeFoundationOptions.QueryableLogging)))
{
    loggerConfig = loggerConfig
        .WriteTo.File(new OtelJsonFormatter(), Path.Combine(logDir, "app-.json"),
            rollingInterval: RollingInterval.Day, retainedFileCountLimit: 14,
            fileSizeLimitBytes: 50 * 1024 * 1024)
        .Destructure.With<PiiDestructuringPolicy>();
}
// 启动链：if (options.QueryableLogging) app.UseSerilogRequestLogging();
```
**字段映射（v2 修订）**：CompactJsonFormatter 默认输出 `@t/@l/@x/@mt` 缩写键，查询 API 无法按名解析——改用自研 `OtelJsonFormatter : ITextFormatter`（新增类，同目录），输出固定键名 Timestamp/Level/TraceId/SpanId/TenantId/UserId/SourceContext/Message/Exception（LogContext 属性取不到时写空串）；单文件超 50MB 由 Serilog 自动分片（app-{date}_NNN.json，查询侧按前缀枚举）。

**§3 矩阵**：✏️ `backend/application/JNPF.API.Entry/Infrastructure/SerilogBootstrap.cs` ｜ ➕ `backend/application/JNPF.API.Entry/Infrastructure/OtelJsonFormatter.cs` ｜ ✏️ 启动链（请求日志门控行）｜ 🚫 重构轨文件面。

**§4 五件套**：4.1 ADR：沿用初版 T-F1 ADR（无部署能力→文件信号）。4.2 安全：脱敏与 sink 同一 if 块挂载——禁止「只开 sink 不开脱敏」中间态（代码结构保证）。4.3 灰度：开关 false=现状字节级；单机部署无分流面，翻牌即全量，观察载体=S5 冒烟。4.4 风险：磁盘占用上升→回滚条件：日均增量 > T-F0-2 基线 3 倍→翻回 false；LogDiskGuardService Error-only 降级为第二道兜底。4.5 测试：开关双态断言+TraceId 字段断言；SLO：可回溯 14 天（业务口径：事故排查窗口）。

**§5 六腿**：5.1 性能——sink 异步缓冲写，不阻塞请求路径；5.2 内存——文件句柄随滚动释放。5.3 组件化——OtelJsonFormatter 独立类，未来 2.5 桥接 OTel Logs 时字段契约直接复用。其余 N/A。

**§6 提问**：无待确认项。

### Task T-F1-3：LogQueryService 内置查询 API

预估工时：4h ｜ 依赖：T-F1-2

**§1 验收契约**：做什么：只读动态 API `GET api/system/LogQuery`——参数 startTime/endTime/level/traceId/keyword/**page/pageSize（默认 20，上限 200）**；跨文件扫描规则：按时间窗枚举 `app-{yyyyMMdd}*.json`（含分片，按文件名日期过滤，最多扫 14 个=保留期上限），按日倒序逐文件流式读（新→旧），命中达 pageSize 即停（无需全局排序）；租户过滤；路径白名单。纯后台型；监控指标：查询命中率（有结果/总请求）观察口径；告警=N/A——新 API 无历史基线，阈值待 2.5 承接。
DoD：
- [ ] traceId 精确命中；时间窗+级别过滤正确；**跨日时间窗查询覆盖多文件**（用例跨两天）；损坏 JSON 行跳过不抛
- [ ] 权限点拦截非授权访问；跨租户日志不可见（按行内 TenantId 字段过滤；管理员无租户上下文时放行——开工核对 UserManager 语义后固化）
- [ ] 分页行为：page/pageSize 参数边界（pageSize>200 截断为 200）

**§2 代码契约骨架**：

```csharp
[ApiDescriptionSettings(Tag = "System", Name = "LogQuery", Order = 219)]
[Route("api/system/[controller]")]
public class LogQueryService : IDynamicApiController, ITransient
{
    [HttpGet] [SecurityDefine]   // 权限点名称开工按 Systems 模块惯例回填
    public Task<RESTfulResult<PageResult<LogQueryItem>>> Get(LogQueryInput input);
}
public class LogQueryInput { public DateTime? StartTime; public DateTime? EndTime;
    public string? Level; public string? TraceId; public string? Keyword;
    public int Page = 1; public int PageSize = 20; /* 上限 200，超限截断 */ }
```

**§3 矩阵**：➕ `backend/application/JNPF.API.Entry/Services/LogQueryService.cs` ｜ 🚫 重构轨文件面 ｜ 🚫 `api/visualdev` 路由域（新路由不得落入该过滤域——快照死线）。

**§4 五件套**：4.1 ADR：任务特有——选文件流式读，否决 SQLite 索引中间层理由：引入存储即引入迁移面，违背降级初衷。4.2 安全：三落点——[SecurityDefine] 权限拦截；TenantId 过滤（管理员无租户上下文时放行——开工核对 UserManager 语义后固化）；路径仅 LogDir 内（GetFullPath 前缀校验防穿越）。4.3 灰度：开关 false 时 API 返回 `RESTfulResult` 业务失败（code=404，msg="日志查询功能未启用"；不用 503——避免前端误判服务降级触发重试风暴）；路由存在性不随开关抖动。4.4 风险：大文件扫描慢→命中上限 200 提前终止+单文件 50MB 上界。4.5 测试：六用例（命中/过滤/权限/租户隔离/跨日多文件/分页边界）；SLO：P95 < 500ms（单文件 10 万行内；跨 14 文件扫描上限下 < 2s），超限记 P2 观察项（工单制，无 Oncall 载体）。

**§5 六腿**：5.1 性能——流式 StreamReader+yield 提前终止；禁止 ReadAllLines。5.5 运行时——async 文件 IO 不占请求线程。其余 N/A。

**§6 提问**：无待确认项。

### Task T-F1-4：F1 验证与三跳贯通证据

预估工时：2h ｜ 依赖：T-F1-3

**§1 验收契约**：做什么：开关双态回归 + 三跳贯通（请求头 TraceId→app 文件→查询 API 命中）+ 快照零 diff 复核。
DoD：
- [ ] `f1-traceid-chain.txt` 记录三跳证据链
- [ ] api/visualdev 快照零 diff 复核（F1 新增路由不在该域）

**§2 代码契约骨架**：无新增代码契约——豁免条件全满足。

**§3 矩阵**：➕ `f1-traceid-chain.txt` ｜ 🚫 重构轨文件面。

**§4 五件套**：4.1 ADR：沿用 ADR-C（特性门禁独立跑）。4.2-4.3：N/A。4.4 风险：三跳断裂→定位 enricher/中间件顺序修复。4.5 测试：本任务即特性门禁；SLO：可查率 100%。

**§5 六腿**：全 N/A——验证任务（F1 特性段收尾）。

**§6 提问**：无待确认项。

---

## S3 执行层（重构轨）+ F2 Outbox 可靠性（特性轨）

### Task T-R3-1：RunDataEngine 骨架创建

预估工时：1h ｜ 依赖：T-R2-6

**§1 验收契约**：做什么：建执行层骨架（ITransient，构造注入 RunSqlCompiler+IRuntimeDataStore）。
DoD：
- [ ] 构建 0 错误；白名单架构断言对本类即刻生效且绿

**§2 代码契约骨架**：

```csharp
public class RunDataEngine : ITransient
{
    private readonly RunSqlCompiler _compiler;
    private readonly IRuntimeDataStore _dataStore;
    public RunDataEngine(RunSqlCompiler compiler, IRuntimeDataStore dataStore);
}
```

**§3 矩阵**：➕ `Runtime/RunDataEngine.cs` ｜ 🚫 特性轨文件面。

**§4 五件套**：4.1 ADR：沿用 A+C spec §3.1。4.2 安全：构造白名单即结构落点（无第二 DB 通道）。4.3 灰度：N/A。4.4 风险：N/A。4.5 测试：架构测试即验证；SLO=N/A。

**§5 六腿**：全 N/A——空骨架。

**§6 提问**：无待确认项。

### Task T-R3-2：执行层二十方法纯移动

预估工时：⚠ 探索型 6h（输出物：20 方法+专属辅助迁完+构建全绿；逐方法 Find References 裁定不可压缩）｜ 依赖：T-R3-1

**§1 验收契约**：做什么：纯移动 spec §3.1 清单 20 方法（Create/Update/BatchUpdate/SaveFlowFormData/GetFlowFormDataDetails/SaveDataToDataByFId/OptimisticLocking/DataTransferVerify/UniqueVerify 等）+专属私有辅助；IRunService 成员在 RunService 保留一行委托。
DoD：
- [ ] 20 方法迁出，方法体逐字未改（git diff 仅位置与委托变化）
- [ ] 构建 0 错误 + Helpers 全绿

**§2 代码契约骨架**：无新增代码契约——纯位置迁移，豁免条件全满足。

**§3 矩阵**：✏️ `RunService.cs` ｜ ✏️ `Runtime/RunDataEngine.cs` ｜ ➕（若需）`Runtime/RuntimeSharedHelpers.cs`（多引擎共享辅助，台账登记）｜ 🚫 特性轨文件面 ｜ 🚫 `IRunService.cs`。

**§4 五件套**：4.1 ADR：沿用 ADR-C。4.2 安全：迁移中遇裸 throw new 原样保留并记入 F4 台账**作为存量基线**（T-F4-2 基线断言以此为基准守护零新增；存量治理登记技术债，本任务不扩面）。4.3 灰度：N/A（等价迁移）。4.4 风险：共享辅助归属误判→Find References 双引擎交叉核对。4.5 测试：构建+Helpers+快照（T-R3-3）；SLO=N/A。

**§5 六腿**：全 N/A——纯移动。

**§6 提问**：无待确认项。

### Task T-R3-3：执行层基线随迁与 CRUD 冒烟

预估工时：3h ｜ 依赖：T-R3-2

**§1 验收契约**：做什么：baseline 随迁 4 条目（SaveDataToDataByFId CC90/GenerateFeilds CC81/FieldBindDefaultValue CC82/DataTransferVerify CC74）+ CRUD 全链路冒烟 + 快照零 diff。
DoD：
- [ ] CI_BUILD 0 新增；`s3-routes.txt` 零 diff
- [ ] CRUD 冒烟（建→查→改→删）四步 200，证据 `s3-crud-smoke.txt`

**§2 代码契约骨架**：无新增代码契约——豁免条件全满足。

**§3 矩阵**：✏️ `complexity-baseline.json` ｜ ➕ 证据两份 ｜ 🚫 特性轨文件面。

**§4 五件套**：4.1 ADR：沿用 §0 全局约束。4.2-4.3：N/A。4.4 风险：CRUD 冒烟暴露 S2 收敛遗漏→台账定位回修。4.5 测试：冒烟即业务断言；SLO：CRUD 四步全 200（业务口径：「在线开发表单数据增删改查可用」）。

**§5 六腿**：全 N/A——验证任务（S3 重构段收尾）。

**§6 提问**：无待确认项。

### Task T-F2-1：Outbox DB 互斥锁

预估工时：3h ｜ 依赖：T-F0-1（特性轨，S3 重构段门禁绿后开工）

**§1 验收契约**：做什么：**前置核验**——核对 EventOutboxMessage 现有字段（Status 状态机含 Pending/Processing、RetryCount、MaxRetry、DeadLetter 标识）与 Outbox 表现行建表机制（证据：master-plan 核验表已确认 RetryCount/MaxRetry=3/DeadLetter 存在，本步落实体级再核）；字段缺失→停手上报，F2 降级或并入 2.12 前置，**禁止私自加列**（无迁移能力）；核验通过后建 EVENT_OUTBOX_LOCK 单行锁表 + IOutboxLock/DbOutboxLock（心跳 60s 过期可抢）。纯后台型；监控指标：**锁连续抢占失败轮次**（>10 轮说明实例心跳异常）观察口径；告警=N/A——指标经 OTel metrics 产生，告警规则属已砍除的 2.16（诚实登记）。
DoD：
- [ ] `f2-outbox-schema-check.txt`：字段核对结果 + 建表机制结论（缺字段即停手上报记录在案）
- [ ] 锁表建表方式与 Outbox 现行建表机制同源集成（定位 Outbox 表现行初始化代码，同一处登记新表；若无自动建表机制→降级为建表 SQL 脚本随仓+部署清单登记，不假设 CodeFirst 可用）
- [ ] 三用例绿：空闲获取成功 / 他人持锁获取失败 / 心跳过期（>60s）抢锁成功

**§2 代码契约骨架**：

```csharp
public interface IOutboxLock
{
    Task<bool> TryAcquireAsync(string instanceId, CancellationToken ct = default);
    Task ReleaseAsync(string instanceId, CancellationToken ct = default);
}
public class EventOutboxLock { [SugarColumn(IsPrimaryKey = true)] public string LockKey { get; set; } = "SWEEPER";
    public string InstanceId { get; set; } = ""; public DateTime Heartbeat { get; set; } }
```
核心伪代码（触发条件②：并发/时序依赖）：

```
TryAcquire(instanceId):
    row = 读锁行(无则插入)
    if row.InstanceId == instanceId 或 now - row.Heartbeat > 60s:
        affected = 条件更新(Heartbeat=now, InstanceId=instanceId, WHERE 旧值匹配)  // 乐观并发
        return affected == 1
    return false
```

**§3 矩阵**：➕ `backend/infrastructure/JNPF.Extras.EventBus.Outbox/IOutboxLock.cs` ➕ `DbOutboxLock.cs` ➕ 锁表实体 ｜ ✏️（若机制同源）Outbox 建表初始化处（登记锁表）｜ ➕ Stage5 测试用例 ｜ ➕ `f2-outbox-schema-check.txt` ｜ 🚫 重构轨文件面 ｜ 🚫 EventOutboxMessage 实体（禁止加列）。

**§4 五件套**：4.1 ADR：沿用初版 T-F2 ADR（DB 锁 vs Redis 锁：不假设 Redis 在场）+ 任务特有——schema 缺失时的降级路径（停手上报）优先于私自加列，否决加列理由：2.12 版本化迁移缺失现状下裸 ALTER 违反自身纪律（同 F4 降级逻辑）。4.2 安全：锁 SQL 全参数化；锁表无敏感数据。4.3 灰度：锁表建表按核验结论执行；消费方开关门控在 T-F2-2。4.4 风险：条件更新竞态→WHERE 旧值匹配乐观并发，失败放弃本轮（30s 后重试，业务可容忍）。4.5 测试：三用例；SLO=N/A（组件级）。

**§5 六腿**：5.4 健壮性——心跳过期自愈无死锁残留；抢锁幂等（仅影响行数语义）。其余 N/A。

**§6 提问**：无待确认项。

### Task T-F2-2：OutboxSweeperService 回收器

预估工时：4h ｜ 依赖：T-F2-1

**§1 验收契约**：做什么：BackgroundService 30s 轮询——Processing 超 10 分钟回置 Pending+RetryCount+1；超 MaxRetry 转 DeadLetter（复用现有死信路径）；批量上限 100；开关 false 不注册服务。纯后台型；监控指标：**消息卡死数**（Processing 超 10 分钟存量）——业务口径「用户提交的事件最长 10 分 30 秒内必被重试或入死信可查」；告警：卡死数 > 0 持续 2 轮（1 分钟）记 P2 观察项（工单制，无 Oncall 载体——2.16 砍除所致）。
DoD：
- [ ] 四用例绿：超时回收 / MaxRetry 升死信 / 持锁跳过 / 双实例并发仅一方回收

**§2 代码契约骨架**：

```csharp
public class OutboxSweeperService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken);
    // 30s 循环：TryAcquire → 扫描超时批 → 回置/升死信 → Release
}
// 注册（开关门控）：if (options.OutboxSweeper) services.AddHostedService<OutboxSweeperService>();
```

**§3 矩阵**：➕ `OutboxSweeperService.cs` ｜ ✏️ Outbox 模块注册处 ｜ ➕ Stage5 四用例 ｜ 🚫 重构轨文件面。

**§4 五件套**：4.1 ADR：沿用初版 T-F2 ADR。4.2 安全：回收 SQL 全参数化。4.3 灰度：开关 false=服务不存在（行为=现状）；翻牌=S5 统一窗口。4.4 风险：误回收慢处理中的真消息→10 分钟阈值 >> 现有最长退避链（约 60s 级），余量 10 倍；幂等消费表兜底重复消费。4.5 测试：四用例（含并发模拟）；SLO：卡死滞留 P99 < 10 分 30 秒（业务指标：提交的事件不会无声消失）。

**§5 六腿**：5.4 健壮性——ExecuteAsync 全包 try-catch（后台服务异常裸奔正是 F4 治理对象，本服务不得自犯）；单轮异常仅记日志不断循环。其余 N/A。

**§6 提问**：无待确认项。

### Task T-F2-3：F2 验证与并发证据

预估工时：2h ｜ 依赖：T-F2-2

**§1 验收契约**：做什么：Stage5 全量绿 + 双实例并发回收证据 + 快照零 diff 复核。
DoD：
- [ ] `f2-sweeper-concurrency.txt` 记录双实例断言结果；快照复核零 diff

**§2 代码契约骨架**：无新增代码契约——豁免条件全满足。

**§3 矩阵**：➕ `f2-sweeper-concurrency.txt` ｜ 🚫 重构轨文件面。

**§4 五件套**：4.1 ADR：沿用 ADR-C。4.2-4.3：N/A。4.4 风险：并发用例时序偶发→固定虚拟时钟不用真实等待。4.5 测试：本任务即特性门禁；SLO 同 T-F2-2。

**§5 六腿**：全 N/A——验证任务（F2 特性段收尾）。

**§6 提问**：无待确认项。

---

## S4 列表/视图层（重构轨）+ F3 出站韧性（特性轨）

### Task T-R4-1：RunListQueryService 纯移动

预估工时：⚠ 探索型 6h（输出物：5 方法+专属辅助迁完+构建全绿；CC85 大类逐块移动不可压缩）｜ 依赖：T-R3-3

**§1 验收契约**：做什么：纯移动 GetListResult(CC85)/GetRelationFormList/GetHaveTableInfo/GetHaveTableInfoDetails/GetListChildTable + 专属辅助至 RunListQueryService（ITransient，构造同 RunDataEngine）。
DoD：
- [ ] 5 方法迁出，方法体逐字未改；构建 0 错误 + Helpers 全绿（含既有 List*Helpers 测试）

**§2 代码契约骨架**：无新增代码契约——纯位置迁移，豁免条件全满足。

**§3 矩阵**：➕ `Runtime/RunListQueryService.cs` ｜ ✏️ `RunService.cs` ｜ ✏️ `complexity-baseline.json`（GetListResult 条目随迁，值不变）｜ 🚫 特性轨文件面 ｜ 🚫 `IRunService.cs`。

**§4 五件套**：4.1 ADR：沿用 ADR-C。4.2 安全：N/A（无新数据面）。4.3 灰度：N/A。4.4 风险：列表装配依赖的私有辅助与视图层共享→Find References 裁定，共享者入 RuntimeSharedHelpers。4.5 测试：构建+Helpers+快照（T-R4-2）；SLO=N/A。

**§5 六腿**：全 N/A——纯移动。

**§6 提问**：无待确认项。

### Task T-R4-2：RunDataViewService 纯移动

预估工时：3h ｜ 依赖：T-R4-1

**§1 验收契约**：做什么：纯移动 GetDataViewResults/GetDataViewQuery/AddDataViewId/GetPageToDataTable 至 RunDataViewService。
DoD：
- [ ] 4 方法迁出；构建 0 错误 + Helpers 全绿

**§2 代码契约骨架**：无新增代码契约——豁免条件全满足。

**§3 矩阵**：➕ `Runtime/RunDataViewService.cs` ｜ ✏️ `RunService.cs` ｜ 🚫 特性轨文件面。

**§4 五件套**：4.1 ADR：沿用 ADR-C。4.2-4.3：N/A。4.4 风险：视图层对列表层私有辅助的隐性依赖→编译即暴露。4.5 测试：构建+Helpers；SLO=N/A。

**§5 六腿**：全 N/A——纯移动。

**§6 提问**：无待确认项。

### Task T-R4-3：S4 门禁验证与列表视图冒烟

预估工时：2h ｜ 依赖：T-R4-2

**§1 验收契约**：做什么：快照零 diff + 列表分页冒烟（首页/翻页/条件过滤）+ 数据视图查询冒烟。
DoD：
- [ ] `s4-routes.txt` 零 diff；列表三形态 200；视图端点 200；证据 `s4-smoke.txt`

**§2 代码契约骨架**：无新增代码契约——豁免条件全满足。

**§3 矩阵**：➕ 证据两份 ｜ 🚫 特性轨文件面。

**§4 五件套**：4.1 ADR：沿用 ADR-C。4.2-4.3：N/A。4.4 风险：分页参数形态差异→与 S3 冒烟响应形态对比。4.5 测试：冒烟即业务断言；SLO：列表分页/视图查询 200（业务口径：「在线开发列表与数据视图可用」）。

**§5 六腿**：全 N/A——验证任务（S4 重构段收尾）。

**§6 提问**：无待确认项。

### Task T-F3-1：引入 Http.Resilience 依赖与流式端点核验

预估工时：2h ｜ 依赖：T-F0-1（特性轨，S4 重构段门禁绿后开工）

**§1 验收契约**：做什么：InteAssistant.csproj 增 `Microsoft.Extensions.Http.Resilience` 包引用 + restore；核验 LLM 客户端是否混用流式/非流式端点，混用则拆独立命名客户端（零重试配置）。纯后台型；监控=N/A——依赖件。
DoD：
- [ ] `dotnet restore` 成功；流式/非流式核验结论落盘 `f3-endpoint-inventory.md`（含拆分动作若触发）

**§2 代码契约骨架**：

```xml
<PackageReference Include="Microsoft.Extensions.Http.Resilience" Version="8.*" />
```
（版本以华为镜像可得的 8.x 最新稳定版为准；流式客户端拆分无新签名，仅 AddHttpClient 命名新增。）

**§3 矩阵**：✏️ `backend/modularity/inteAssistant/JNPF.InteAssistant/JNPF.InteAssistant.csproj` ｜ ✏️（若触发）`PipelineSchedulingModule.cs` 命名客户端拆分 ｜ ➕ `f3-endpoint-inventory.md` ｜ 🚫 重构轨文件面。

**§4 五件套**：4.1 ADR：任务特有——流式端点禁重试是硬约束（重试流式响应=重复输出/配额浪费），拆分与否取决于核验结果而非预设。4.2 安全：包源限华为镜像（nuget.config 既有）；新依赖无已知高危 CVE（restore 后 CI 依赖扫描佐证）。4.3 灰度：仅包引用无行为变化。4.4 风险：包版本与 net8.0 兼容→8.x 线官方支持 net8。4.5 测试：构建即验证；SLO=N/A。

**§5 六腿**：5.6 对标——即引入业界 GA 标准（Microsoft.Extensions.Http.Resilience）本身；取舍：接受固定五段管线不自定义段（YAGNI）。其余 N/A。

**§6 提问**：无待确认项。

### Task T-F3-2：韧性管线测试（MockHttpHandler）

预估工时：3h ｜ 依赖：T-F3-1

**§1 验收契约**：做什么：先写失败测试——**测试内独立构造 ResiliencePipeline 实例**（参数与 T-F3-3 生产配置同源常量，不依赖 DI 接线即可编译先红），注入失败序列断言重试/熔断行为（TDD 顺序：本任务先于 T-F3-3 配置落码）。纯后台型；监控=N/A——测试件。
DoD：
- [ ] 两用例先红：前 2 次 503 第 3 次 200→最终成功且请求计数=3；5 连败→熔断打开（后续请求不经 handler）
- [ ] 管线参数抽为共享常量类（ResilienceParams），T-F3-3 生产配置引用同一常量（防测试/生产参数漂移）

**§2 代码契约骨架**：

```csharp
public class OutboundResilienceTests
{
    [Fact] public Task Pipeline_RetriesTransientFailures_ThenSucceeds();  // MockHttpHandler 失败序列 + 虚拟时间
    [Fact] public Task Pipeline_OpensCircuit_AfterConsecutiveFailures();
}
```

**§3 矩阵**：➕ `backend/tests/JNPF.Tests.Phase6/OutboundResilienceTests.cs`（或 InteAssistant 现行测试归属项目，开工核对）｜ ➕（若需）`ResilienceParams.cs` 共享常量（InteAssistant 模块内）｜ 🚫 重构轨文件面。

**§4 五件套**：4.1 ADR：沿用初版 T-F3 ADR（Polly v8 vs 自研）。4.2 安全：N/A（测试无敏感面）。4.3 灰度：N/A。4.4 风险：虚拟时间工具与 Polly v8 版本适配→官方 testing 包同源。4.5 测试：本任务即测试；SLO=N/A。

**§5 六腿**：全 N/A——测试先行任务。

**§6 提问**：无待确认项。

### Task T-F3-3：LLM/MCP 命名客户端管线配置

预估工时：3h ｜ 依赖：T-F3-2

**§1 验收契约**：做什么：OutboundResilience=true 时为 LLM/MCP 命名客户端挂 AddStandardResilienceHandler（**单次尝试超时 60s / 重试 2 次（共 3 次尝试）指数退避 / 总超时 200s=60s×3+余量 / 熔断采样 30s**——v2 修订：TotalRequestTimeout 是全尝试总超时，原 120s 方案一次慢尝试即耗尽总超时使重试失效，单次/总超时必须分离）；false 时不挂（行为=现状）。
DoD：
- [ ] T-F3-2 两用例转绿（引用同一 ResilienceParams）；开关 false 回归：管线不生效（请求计数断言）

**§2 代码契约骨架**：

```csharp
var builder = services.AddHttpClient("LlmGateway", client => { /* 现有配置保留 */ });
if (options.OutboundResilience)
{
    builder.AddStandardResilienceHandler(o =>
    {
        o.AttemptTimeout.Timeout = TimeSpan.FromSeconds(ResilienceParams.AttemptTimeoutSeconds);      // 单次尝试 60s
        o.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(ResilienceParams.TotalTimeoutSeconds);   // 总超时 200s=单次×3+余量
        o.Retry.MaxRetryAttempts = ResilienceParams.MaxRetryAttempts;   // 2（共 3 次尝试），配额放大防线
        o.Retry.BackoffType = DelayBackoffType.Exponential;
        o.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
    });
}
```

**§3 矩阵**：✏️ `backend/modularity/inteAssistant/JNPF.InteAssistant/PipelineSchedulingModule.cs` ｜ 🚫 重构轨文件面 ｜ 🚫 附件下载手工重试处（登记遗留，本期不收敛——§1.2 砍刀）。

**§4 五件套**：4.1 ADR：沿用初版 T-F3 ADR + 任务特有——单次/总超时分离（Polly 标准管线 TotalRequestTimeout 覆盖全部尝试，不分离则重试形同虚设）。4.2 安全：重试不重放一次性凭证（LLM 推理为只读语义，可重试——已在 Phase 1 确认）；单次超时 60s 防线程池拖死（P0-3 原始痛点）。4.3 灰度：开关 false=现状；翻牌=S5 窗口。4.4 风险：重试放大 LLM 配额消耗→仅 2 次+指数退避+流式禁重试（T-F3-1 拆分）；熔断误开→阈值 5 连败+30s 半开探测+开关兜底。4.5 测试：两用例转绿+false 回归；SLO：出站调用 P99 总耗时 < 200s（此前无上界——本 SLO 即建立基线）。

**§5 六腿**：5.4 健壮性——本任务即容错三件套落点（重试/断路器/超时，隔舱含于标准组）；边界防御：熔断打开时快速失败而非挂起。其余 N/A。

**§6 提问**：无待确认项。

### Task T-F3-4：F3 验证与活体冒烟

预估工时：2h ｜ 依赖：T-F3-3

**§1 验收契约**：做什么：开关 true 下真实 LLM 端点调用 200 + 重试指标产生性验证（**v2 修订：无采集端/看板，指标「可见」降级为「已产生并注册」——MeterListener 单测捕获断言计数>0；展示层待 2.16**）+ 快照零 diff 复核。纯后台型；监控指标：**LLM 调用最终成功率**（含重试后成功）——业务口径「AI 功能可用」；告警=N/A——指标先产生，告警规则属 2.16。
DoD：
- [ ] `f3-resilience-live.txt` 含实调 200 + 重试事件结构化日志记录；MeterListener 单测断言重试计数仪表已注册且可捕获；快照复核零 diff

**§2 代码契约骨架**：无新增代码契约——豁免条件全满足。

**§3 矩阵**：➕ `f3-resilience-live.txt` ｜ 🚫 重构轨文件面。

**§4 五件套**：4.1 ADR：沿用 ADR-C。4.2-4.3：N/A。4.4 风险：实调依赖 LLM 提供商可用性→失败时以 MockHttpHandler 集成证据替代并登记。4.5 测试：本任务即特性门禁；SLO 同 T-F3-3。

**§5 六腿**：全 N/A——验证任务（F3 特性段收尾）。

**§6 提问**：无待确认项。

---

## S5 收尾（重构轨）+ F4 异常边界（特性轨）+ 终审翻牌

### Task T-R5-1：CodeGen 注入点切换 CR 起草与审批

预估工时：2h ｜ 依赖：T-R4-3

**§1 验收契约**：做什么：起草 Common.CodeGen/ExportImportDataHelper 注入点切换 CR（切换点/目标注入类型/回滚方式），提交用户审批；批准后 workflow-state 标 cr-approved。无审批不动代码。
DoD：
- [ ] CR 文件落盘 `.claude/change-requests/`；用户审批记录在案

**§2 代码契约骨架**：本任务无新增代码契约，仅涉及 CR 文档——豁免条件全满足。

**§3 矩阵**：➕ CR 文档 ｜ 🚫 `ExportImportDataHelper.cs`（未批禁触）。

**§4 五件套**：4.1 ADR：沿用 spec 风险4（跨模块切换单独 CR）。4.2-4.3：N/A。4.4 风险：审批等待阻塞 S5→可先并行 T-F4-1。4.5 测试：N/A（审批流程任务）；SLO=N/A。

**§5 六腿**：全 N/A——流程任务。

**§6 提问**：无待确认项。

### Task T-R5-2：CodeGen 注入点切换执行

预估工时：3h ｜ 依赖：T-R5-1（cr-approved）

**§1 验收契约**：做什么：按 CR 批准口径切换 ExportImportDataHelper 注入类型（消费单一引擎→直注引擎类；混合消费→保留门面）；单 commit 可 revert。
DoD：
- [ ] 切换后构建 0 错误 + 快照零 diff（`s5a-routes.txt`）+ test:api 全绿

**§2 代码契约骨架**：无新增代码契约——注入类型替换，豁免条件满足（无新签名/结构/存储）。

**§3 矩阵**：✏️ `backend/modularity/common/Common.CodeGen/.../ExportImportDataHelper.cs`（构造注入类型+调用点对齐）｜ 🚫 特性轨文件面。

**§4 五件套**：4.1 ADR：沿用已批 CR。4.2 安全：N/A（模块内消费面不变）。4.3 灰度：N/A（行为等价）。4.4 风险：消费方法已迁引擎后签名微差→编译即暴露，按引擎 public 方法对齐。4.5 测试：快照+test:api；SLO=N/A。

**§5 六腿**：全 N/A——注入切换。

**§6 提问**：无待确认项。

### Task T-R5-3：IRunService 瘦身与门面缩壳

预估工时：4h ｜ 依赖：T-R5-2

**§1 验收契约**：做什么：前置核验 WorkFlow 消费面（grep FlowTaskManager/FlowFormService/FlowTaskOtherUtil 全落 7 方法内，发现第 8 个即停手上报）→ IRunService 17→7 → RunService 缩壳 <400 行（仅 7 方法委托+基础设施方法委托）→ 模块内 4 注入点按消费面切换 → 架构测试豁免位恢复（RunService 纳入白名单断言）→ 契约测试改严（成员数断言 7+瘦身后签名冻结基线）。
DoD：
- [ ] WorkFlow 模块编译通过（零遗漏消费）；RunService <400 行（**行数上限依据（v2 修订）**：7 方法委托×约 5 行 + 基础设施方法委托 + 构造函数与字段；开工先落 `s5-shell-shrink-estimate.txt` 记录残留方法清单×行数测算，超限即审计未迁残留而非放宽指标）
- [ ] `RunServiceContractTests` 改严后全绿；白名单断言含 RunService 且绿

**§2 代码契约骨架**（接口修改——禁止豁免）：

```csharp
public interface IRunService   // 17→7，保留成员（签名不变）：
{   // SaveFlowFormData / GetFlowFormDataDetails / SaveDataToDataByFId /
    // GetDbLink / GetVisualDevModelDataConfig / GetCreateSqlByTemplate / GetUpdateSqlByTemplate
}   // 被删成员的实现已在引擎类 public（S1-S4 保证）；被删成员的前消费方仅 WorkFlow（已核验）
```

**§3 矩阵**：✏️ `IRunService.cs`（删 10 成员）｜ ✏️ `RunService.cs`（缩壳）｜ ✏️ 模块内 4 注入点（VisualDevService/VisualDevModelDataService/VisualdevShortLinkService/VisualdevModelAppService）｜ ✏️ `RunServiceContractTests.cs`（改严）｜ ✏️ `RunEngineSqlSugarBoundaryTests.cs`（豁免位恢复）｜ 🚫 特性轨文件面 ｜ 🚫 WorkFlow 模块（只编译验证不改码）。

**§4 五件套**：4.1 ADR：任务特有——被删成员不标 [Obsolete] 过渡（接口消费方全在仓内且已核验，直接删除编译即验证）；否决保留废弃成员理由：死成员残留即幽灵耦合。4.2 安全：接口删除不改权限面（API 契约在委托方，快照守护）。4.3 灰度：N/A（行为等价纯移动收尾，ADR-R）。4.4 风险：消费面核验遗漏（第 8 个方法）→停手上报机制已在 DoD；注入点切换逐个构建。4.5 测试：契约测试改严+sln 构建；SLO=N/A。

**§5 六腿**：5.3 组件化——ISP 落地：WorkFlow 只见 7 方法窄接口，跨模块依赖面（WorkFlow→VisualDev）从 17 方法收窄至 7（依赖矩阵硬化收益）。其余 N/A。

**§6 提问**：无待确认项。

### Task T-F4-1：非 HTTP 异常入口台账与边界契约

预估工时：3h ｜ 依赖：T-F0-1（特性轨，S5 重构段开工后并行）

**§1 验收契约**：做什么：grep 全仓非 HTTP 入口清单（BackgroundService/IHostedService/IEventHandlerExecutor/SSE/WebSocket 管道）落台账（**OutboxSweeperService 标注自治：已内置 try-catch，不重复接线**）；定义 IExceptionBoundary 契约 + 先写失败测试。纯后台型；监控指标（随 T-F4-2 生效）：**边界捕获次数**（按入口类型）——业务口径「后台故障有记录可查而非无声崩溃」；**可观测性降级（v2 修订）：无采集端，指标验证口径=「已产生并注册」（MeterListener 单测捕获），展示待 2.16**；告警=N/A（2.16 砍除）。
DoD：
- [ ] `f4-entry-inventory.md` 台账落盘；`ExceptionBoundaryTests` 两用例先红（CapturesStructuredJson / DoesNotSwallowSilently）

**§2 代码契约骨架**：

```csharp
public interface IExceptionBoundary
{
    Task HandleAsync(string entry, Exception ex, IReadOnlyDictionary<string, string?>? context = null);
    // 行为：记 Error 日志 + metric(entry) + 结构化入库（开关门控）+ 按入口语义处置，自身零抛异常
}
public record StructuredException(string Type, string? Code, string Message,
    IReadOnlyList<InnerFrame> InnerChain /* 深度上限 5 */, IReadOnlyDictionary<string, string?> Context);
```

**§3 矩阵**：➕ `backend/modularity/common/JNPF.Common.Core/ExceptionBoundary/IExceptionBoundary.cs` ➕ `StructuredException.cs` ｜ ➕ `backend/tests/JNPF.Tests.Common/ExceptionBoundaryTests.cs` ｜ ➕ `f4-entry-inventory.md` ｜ 🚫 重构轨文件面。

**§4 五件套**：4.1 ADR：沿用初版 T-F4 ADR（Json 内结构化不动 schema）。4.2 安全：context 仅含业务标识（FormId/FlowId/TenantId/TraceId），**禁止入栈变量值**（PIPL——防敏感数据进日志库）。4.3 灰度：契约件无行为；门控在 T-F4-2。4.4 风险：入口台账遗漏→覆盖面=台账全覆盖作为 DoD，缺一即漏。4.5 测试：两用例先红；SLO=N/A（组件级）。

**§5 六腿**：5.4 健壮性——innerChain 深度上限 5 防异常链序列化爆内存；边界自身零抛异常（内部全包）。其余 N/A。

**§6 提问**：无待确认项。

### Task T-F4-2：边界接线与异常记录结构化

预估工时：4h ｜ 依赖：T-F4-1

**§1 验收契约**：做什么：台账入口逐一接线 IExceptionBoundary（后台=记录后吞掉防进程崩；事件=交 Outbox 重试路径；自治入口（如 Sweeper）不重复接线，仅登记）；LogExceptionHandler 双格式（ExceptionBoundary=true 写结构化 JSON；false 维持 `Message+"\n"+StackTrace` 字节级兼容）；**引擎抛出面基线断言（v2 修订）：存量裸 throw new 由 T-R3-2 台账登记为基线，断言=「裸 throw 点位与基线完全一致、零新增」——方法体不改则存量不可能清零，「零裸 throw」口径与纯移动纪律必然冲突，改按基线守护；存量治理登记技术债不阻塞本期**；断言归特性轨测试载体（禁触重构轨文件）。纯后台型；监控指标同 T-F4-1（含可观测性降级口径）；告警=N/A。
DoD：
- [ ] 台账入口全接线（自治入口登记在案）；双格式测试绿（含查询侧首字符 `{` 兼容读）
- [ ] 引擎抛出面基线断言绿（点位与基线一致、零新增；断言类在 JNPF.Tests.Common，不触重构轨）

**§2 代码契约骨架**：

```csharp
// LogExceptionHandler.OnExceptionAsync 内（开关门控双格式）
entity.Json = options.ExceptionBoundary
    ? JsonSerializer.Serialize(new StructuredException(ex.GetType().FullName, (ex as FriendlyException)?.Code,
        ex.Message, BuildInnerChain(ex, depth: 5), context))
    : context.Exception.Message + "\n" + context.Exception.StackTrace;  // 原格式字节级保留
```

**§3 矩阵**：➕ `ExceptionBoundary.cs`（实现）｜ ✏️ 台账列出的各入口文件（薄接线，特性轨文件面内）｜ ✏️ `LogExceptionHandler.cs`（双格式）｜ ➕ `backend/tests/JNPF.Tests.Common/EngineThrowSiteBaselineTests.cs`（特性轨测试载体——**v2 修订：原方案误列重构轨 RunEngineSqlSugarBoundaryTests.cs，违反 ADR-C 隔离，已纠正**）｜ ➕ 测试用例 ｜ 🚫 重构轨全部文件（含引擎方法体与架构测试）。

**§4 五件套**：4.1 ADR：沿用初版 T-F4 ADR（Json 内结构化不动 schema）+ 任务特有——抛出面守护选「基线断言」而非「零裸 throw」，否决后者理由：存量点位在纯移动纪律下不可改写（改写方法体=行为变更风险），基线断言既防新增又不动摇存量；存量治理入技术债登记。4.2 安全：入库沿用现有 `Log:CreateExLog` 事件路径，权限面不变；结构化字段白名单（见 T-F4-1 4.2）。4.3 灰度：开关 false=旧格式字节级；翻牌后新旧格式共存，兼容读规则=首字符 `{` 判定。4.4 风险：吞异常掩盖故障→吞前必记 Error 日志+metric（DoesNotSwallowSilently 用例守护）。4.5 测试：双格式+接线冒烟；SLO：非 HTTP 未捕获异常导致的进程崩溃次数=0（当前无基线，本期建立）。

**§5 六腿**：5.6 对标——取 .NET 8 IExceptionHandler（HTTP 层已有 FriendlyExceptionFilter 等价物）的非 HTTP 延伸语义 + Aspire host 级边界思想；砍其 Dashboard 依赖。其余 N/A。

**§6 提问**：无待确认项。

### Task T-F4-3：F4 验证与后台异常冒烟

预估工时：2h ｜ 依赖：T-F4-2

**§1 验收契约**：做什么：人为触发一次后台作业异常（开关 true）——断言进程存活+入库结构化；快照零 diff 复核。
DoD：
- [ ] `f4-boundary-live.txt` 含触发记录+入库 JSON 样本+进程存活佐证；快照复核零 diff

**§2 代码契约骨架**：无新增代码契约——豁免条件全满足。

**§3 矩阵**：➕ `f4-boundary-live.txt` ｜ 🚫 重构轨文件面。

**§4 五件套**：4.1 ADR：沿用 ADR-C。4.2-4.3：N/A。4.4 风险：触发手段影响环境→用测试租户/测试作业，事后清理。4.5 测试：本任务即特性门禁；SLO 同 T-F4-2。

**§5 六腿**：全 N/A——验证任务（F4 特性段收尾）。

**§6 提问**：无待确认项。

### Task T-R5-4：重构轨终审（六门禁）

预估工时：3h ｜ 依赖：**仅 T-R5-3**（v2 修订：解除对 T-F4-3 的依赖——重构终审被特性门禁阻塞直接违反 ADR-C 红线；重构/特性终审拆为两个独立节点）

**§1 验收契约**：做什么：重构轨六门禁全量（sln Debug/Release、CI JNPF009、VisualDev 测试、架构测试、快照零 diff、test:api）+ 活体冒烟（登录/CurrentUser/OnlineDev 三类端点，开关仍全 false=重构纯净态）。纯后台型；监控=N/A——验证任务。
DoD：
- [ ] 六门禁逐项证据落盘 `s5-final-*.txt`；重构轨交付结论独立出具（不等特性轨）

**§2 代码契约骨架**：无新增代码契约——豁免条件全满足。

**§3 矩阵**：➕ 证据 ｜ 🚫 全部源码文件（终审只验不改）。

**§4 五件套**：4.1 ADR：沿用 ADR-C（红线纪律的可执行化：重构终审依赖集不含任何特性任务）。4.2 安全：冒烟含权限面（登录/CurrentUser）。4.3 灰度：N/A。4.4 风险：六门禁任一红→定位到具体阶段 revert（ADR-R）。4.5 测试：全回归即终验；SLO：既有业务冒烟全 200。

**§5 六腿**：全 N/A——终审任务。

**§6 提问**：无待确认项。

### Task T-F4-4：特性轨终审与按序翻牌

预估工时：2h ｜ 依赖：T-R5-3, T-F4-3（特性轨独立终审，红不阻塞重构轨已交付结论）

**§1 验收契约**：做什么：翻牌序列（QueryableLogging→OutboxSweeper→OutboundResilience→ExceptionBoundary，每翻一个跑一轮冒烟，红即翻回登记缺陷**不阻塞其余翻牌与重构轨交付**）+ 四开关全 true 活体冒烟（登录/CurrentUser/OnlineDev/日志查询/Sweeper 状态）+ 四特性指标产生性汇总（MeterListener 断言口径）。纯后台型；监控：四特性指标均已注册即为验收口径（展示待 2.16）。
DoD：
- [ ] 翻牌记录 `f4-final-flip.txt`（含每次冒烟结果）；全 true 冒烟 200（或红项翻回+缺陷登记）；特性轨终审结论独立出具

**§2 代码契约骨架**：无新增代码契约——豁免条件全满足。

**§3 矩阵**：✏️ `Configurations/App.json`（翻牌，唯一允许的开关变更窗口）｜ ➕ 证据 ｜ 🚫 其余全部源码文件。

**§4 五件套**：4.1 ADR：沿用 ADR-C/ADR-F（双轨终审分离+翻牌序列）。4.2 安全：翻牌后冒烟含权限面。4.3 灰度：本任务即灰度终点（翻牌=放量；放量依据=前序门禁全绿，回滚条件=任一冒烟红即翻回）。4.4 风险：某特性翻牌红→翻回+登记修复任务（红线纪律：不影响重构轨交付结论）。4.5 测试：翻牌冒烟即验证；SLO：全 true 态既有业务冒烟全 200。

**§5 六腿**：全 N/A——终审任务。

**§6 提问**：无待确认项。

---

# Phase 4：持续校准与自检闭环

## §4.1 任务依赖图与工时

```
重构轨：T-R0-1/2/3 → T-R1-1→R1-2→R1-3→(R1-4,R1-5)→R1-6 → T-R2-1→R2-2→R2-3→R2-4→R2-5→R2-6
        → T-R3-1→R3-2→R3-3 → T-R4-1→R4-2→R4-3 → T-R5-1→R5-2→R5-3→R5-4（重构终审，仅依赖 R5-3）
特性轨：T-F0-1/2 → T-F1-1→F1-2→F1-3→F1-4（挂 S2）；T-F2-1→F2-2→F2-3（挂 S3）；
        T-F3-1→F3-2→F3-3→F3-4（挂 S4）；T-F4-1→F4-2→F4-3（挂 S5，与 R5-1 并行）→ T-F4-4（特性终审，依赖 R5-3+F4-3）
```

| 段 | 任务 | 工时 | 累计 |
|------|------|------|------|
| S0 | T-R0-1/2/3 + T-F0-1/2 | 2+3+2+3+1 = 11h | 11h |
| S1 | T-R1-1…6 | 1+4+4+2+4+1 = 16h | 27h |
| S2 | T-R2-1…6 | 3+2+4+6⚠+6⚠+2 = 23h | 50h |
| S2-F1 | T-F1-1…4 | 3+3+4+2 = 12h | 62h |
| S3 | T-R3-1/2/3 | 1+6⚠+3 = 10h | 72h |
| S3-F2 | T-F2-1/2/3 | 3+4+2 = 9h | 81h |
| S4 | T-R4-1/2/3 | 6⚠+3+2 = 11h | 92h |
| S4-F3 | T-F3-1…4 | 2+3+3+2 = 10h | 102h |
| S5 | T-R5-1/2/3 + T-F4-1/2/3/4 + T-R5-4 | 2+3+4+3+4+2+2+3 = 23h | 125h |

**总计：125h ≈ 15.6 人日（33 任务，其中 4 个标 ⚠ 探索型 6h；v2 修订：原 124h/32 任务为拆分前口径，特性轨实为 46h 非 31h——算术错误已纠正）**。单轨穿插=阶段窗口内串行，交付历时 ≈ 125h（F4-1 与 R5-1 小段并行可省约 2h）；重构轨 79h / 特性轨 46h（含测试与证据采集）。

## §4.2 实施校准钩子

| 校准项 | 验证方式 | 触发时机 |
|--------|---------|---------|
| Sweeper 并发正确性（锁不失效） | 双实例模拟断言同批消息仅回收一次（T-F2-2 用例） | T-F2-3 |
| 韧性管线真实生效 | MockHttpHandler 失败序列断言重试/熔断（T-F3-2）+ 实调 200 | T-F3-4 |
| 挂靠点未被破坏 | 白名单架构测试绿 + grep 引擎层零 IDataBaseManager/ISqlSugarRepository 引用 + T-R2-5 豁免废除后 RunService 零 SqlSugar 引用复核 | T-R5-3/T-R5-4 |
| 日志可查闭环 | TraceId 三跳贯通（头→文件→查询 API） | T-F1-4 |
| Queryable 等价性 | 27 处 ToSql 比对台账 + 列表冒烟 | T-R2-5/T-R2-6 |
| **决策回溯触发条件** | 实施中遇问题必须回溯 §2.1/2.2 ADR：①S4 韧性致 LLM 配额放大 >30%→重评重试参数；②F1 磁盘占用翻倍→重评全级别落盘；③快照非零 diff 且定位到纯移动段→重评该移动归属裁定 | 全程 |

## §4.3 自检清单（逐项实操验证结果，非仅打勾）

| # | 检查项 | 验证方式 | 结果 |
|---|--------|---------|------|
| 1 | 原子性 | 全文搜索任务标题：无「和/以及/同时/及」连接词 | ✅ |
| 2 | 工时约束 | 逐任务核对：29 个 ≤4h；4 个 6h 均标 ⚠ 探索型且注明输出物与不可压缩理由 | ✅ |
| 3 | 契约完整性 | 新增/修改 public 面任务（T-R2-2/F0-1/F1-1/F1-3/F2-1/F3-1/T-F4-1/R5-3）均有骨架；纯移动任务按四条豁免 | ✅ |
| 4 | 豁免合理性 | 逐豁免任务核对四条：纯移动/验证/证据任务无新签名/结构/存储/复杂逻辑 | ✅ |
| 5 | 矩阵完整性 | 每任务 §3 至少一条 ➕/✏️ | ✅ |
| 6 | 矩阵隔离性 | 每任务 🚫 栏已列另一轨文件面；关键保护面（IRunService/元数据 LINQ 形态/api-visualdev 域/EventOutboxMessage 实体）额外单列；**v2 修订：T-F4-2 误列重构轨文件已纠正** | ✅ |
| 7 | 伪代码合规 | 仅 T-R2-5（歧义）与 T-F2-1（并发）含伪代码，均 ≤30 行；其余任务无 | ✅ |
| 8 | 无模糊词 | 全文搜索：适当/酌情/根据需要 = 0 命中；「合理」仅出现于任务名「快照基线落盘」等无涉（已核查） | ✅ |
| 9 | 边界连续 | 依赖对返回/入参对齐：IRuntimeDataStore（T-R2-2）→引擎构造（T-R3-1）；StructuredException（T-F4-1）→双格式写入（T-F4-2）；ResilienceParams（T-F3-2）→生产配置（T-F3-3）；裸 throw 基线台账（T-R3-2）→基线断言（T-F4-2） | ✅ |
| 10 | 可测试性 | 每个 DoD 可拟测试名（如 IRunService_MemberCount_IsSeven / Sweeper_RecoversTimedOutProcessing） | ✅ |
| 11 | 五件套底线 | 每任务 ≥2 项实质（验证类任务=ADR+风险或 ADR+测试两项实质，其余 N/A+理由）；无凑数空话 | ✅ |
| 12 | 六腿底线 | 每任务 ≥1 实质或「全 N/A+任务类型说明」（纯移动/验证/骨架任务诚实标注） | ✅ |
| 13 | ADR 无重复 | 全局决策（ADR-C/ADR-R/ADR-F）仅引用编号；任务特有决策（白名单泛型匹配/词表匹配/入口分策处置等）才展开 | ✅ |
| 14 | 隐形开发防护 | 纯后台任务均有业务监控指标（可查率/卡死数/LLM 成功率/边界捕获数）或告警=N/A+理由（2.16 砍除诚实登记） | ✅ |
| 15 | 砍刀真实性 | §1.2 全部针对已提出需求（四特性）及隐含影响（PII/开关），无凭空构造再砍除 | ✅ |
| 16 | 监控业务性 | 告警/指标均业务口径（事件不无声消失/AI 功能可用/后台故障可查），无 CPU/内存类 | ✅ |
| 17 | 跳过合理性 | 无「本场景不适用」章节（多轨+跨模块，全部章节适用） | ✅ |
| 18 | 检查点完整 | Phase 1 ✅ 已确认；Phase 2-4 本轮输出待统一确认（用户授权的批量模式） | ✅ |

## §4.4 决策回溯机制

实施中任任务受阻，第一动作=回溯 Phase 2 两项 ADR（ADR-C 混入形态 / ADR-R+F 回滚轴）与 §4.2 触发条件表，评估是否需「决策回滚」；禁止绕过 ADR 就地打补丁。红线纪律（特性门禁红不阻塞重构门禁）在任何回溯中不可豁免。

---

## 🔒 Phase 3 检查点（批量模式，逐项自检）

- [x] 名称无连接词，工时 ≤4h 或 ⚠ 探索型标注（33 任务）
- [x] 代码契约已填写或按四条豁免；接口签名未以「下沉 OpenAPI」为由跳过
- [x] 伪代码仅两处（歧义/并发触发），均 ≤30 行
- [x] 文件矩阵完整，多轨 🚫 栏全列
- [x] 五件套每任务 ≥2 实质；六腿每任务 ≥1 实质或诚实全 N/A+类型说明
- [x] ADR 未重复 Phase 2（全局决策仅引用 ADR-C/R/F 编号）
- [x] 纯后台任务均含业务监控指标或告警 N/A+理由
- [x] §0 全局约束已填（跨模块+多轨强制项）
- [ ] **用户统一确认** ← 等待
