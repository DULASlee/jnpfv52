# 架构设计规格 — 运行时基座与 RunService 引擎化（模板 v6.0 版）

- **模板**：设计规格生成模板 v6.0（`docs/AI编程范式工程/1、设计规格文档编写模板`）
- **日期**：2026-08-21（v5.2 实质内容按 v6.0 认知闭环结构重组；v5.2 的 13 项审查修订全部保留）
- **状态**：待专家委员会审核
- **上游**：`2026-08-20-runservice-engine-refactor-design.md`（A+C 子规格）· `runservice-refactor-master-plan.md` v3 · `runtime-infrastructure-gap-analysis.md` v3
- **关联 CR**：CR-20260820-01
- **节奏豁免**：用户已授权批量输出后统一确认（模板默认逐模块确认）

---

# 第一部分：行为纪律声明

遵循模板 §1.1/§1.2 全部纪律；本项目附加两条实证纪律：Evidence Over Assumption（猜 3 次不行即停手抓运行时数据）、JNPF 实现完整性铁律（节点审批门禁）。本文档中 `[待确认]` 共 2 处（F1 租户过滤的管理员放行语义、F2 锁表建表机制），均已标注阻塞的开工动作，不作已确认事实使用。

---

# 第二部分：Phase 1 — 需求挑战与全局基线对齐（已确认存档）

## §0 场景速写

| 维度 | 值 |
|------|-----|
| 场景类型 | 系统重构（RunService 4157 行上帝类引擎化）+ 功能迭代（四特性降级版混入） |
| 变更范围 | 跨模块（visualdev / common / inteAssistant / EventBus.Outbox / API.Entry / tests / analyzers） |
| 是否多轨并行 | 是——重构轨（纯移动+抽象）与特性轨（行为变更特性），文件面零交集 |

## §1.1 需求降维分析

- **表面需求**：拆分 RunService 上帝类 + 补齐运行时底座四特性（异常边界/幂等/韧性/可查询日志）。
- **真实痛点**：平台副作用（DB/事务/出站/异常/日志）无统一挂靠点——单租户可饿死全局、LLM 挂起拖死线程池、事故后日志不可查；且基座债务反复被推入 backlog 后永不偿付。
- **不解决的后果**：RunService 继续以 CC140 巨兽形态承接所有运行时需求，每次改动都是全仓风险；多租户商业化承诺（租户隔离/可用性）无结构保障。
- **80% 替代方案核验**：仅拆 RunService 不建基座 = 结构整洁但泄漏依旧；仅建基座不拆分 = 漏斗存在但最大泄漏源（49+8 处直调）仍在漏斗外。二者必须同窗施工——这正是③混入式拍板的依据。
- **伪需求判定（已实证）**：全局幂等键中间件=平台级幻想（痛点不成立）；RFC 9457 全量统一=涉前端联动的越界项；告警 as-code=依赖不存在的 Grafana 基建。

## §1.2 复杂度砍刀（仅针对已提出需求及其隐含影响）

| 已提出需求点 | 砍/降级决定 | 来源 |
|-------------|------------|------|
| 异常边界（P0-2） | **降级**：Json 字段内结构化，不动表 schema（加列依赖 2.12 版本化迁移，当前缺失） | 隐含影响：schema 变更无迁移能力承载 |
| 幂等（P1-2/P2-1） | P1-2 Outbox Sweeper **保留**；P2-1 全局幂等键**整体砍除**（前端防抖+Outbox 幂等表兜底已覆盖） | 用户拍板 |
| 韧性管线（P0-3） | **降级**：仅 LLM/MCP 热路径两个命名客户端（全量留 backlog 2.3）；附件下载手工重试不收敛，登记遗留 | 隐含影响登记 |
| 可查询日志（P1-1） | **降级**：OTel 规范文件 JSON+内置查询 API；采集端点/看板移出本期（无部署能力） | 用户拍板（Conway 约束） |
| [由可查询日志隐含] PII 脱敏 | **确认纳入**——日志面扩大必然带出合规面扩大，同批交付 | 隐含影响识别（非构造） |
| [由混入式隐含] 特性开关基建 | **确认纳入**——行为变更混入纯移动重构必然带出定点回滚需求 | 隐含影响识别（非构造） |

## §1.3 范围边界表

| 纳入范围 | 明确排除（附排除原因） |
|---------|----------------------|
| RunService 引擎化 A+C（M1-M6：六组件拆分+IRuntimeDataStore+IRunService 17→7） | CC 降级（CC140 保持——降 CC=行为变更，违 JNPF009 基线铁律，二期 2.10） |
| F0 特性开关四布尔位 | RFC 9457 ProblemDetails——涉前端错误负载解析联动 |
| F1 可查询日志降级版（M7） | Seq/LGTM 部署栈——无 DevOps 角色（Conway），待运维就绪（2.5 余留） |
| F2 Outbox Sweeper + DB 互斥锁（M8） | Redis 分布式锁——不可假设 Redis 在场（Cache.json memory/redis 二选一） |
| F3 出站韧性仅 LLM/MCP（M9） | 全客户端韧性铺开 / 附件下载手工重试收敛——面扩大，留 2.3 |
| F4 异常边界非 HTTP 入口+Json 结构化（M10） | 异常表加列（type/code/innerChain）——待 2.12 后升格 |
| 结构挂靠点声明+引擎构造白名单架构测试 | Quartz 专项边界（2.6）/ 就绪探针（2.7）/ 缓存防击穿（2.2）/ 分区限流（2.4）——与本次文件面无交，独立 CR |

## §1.4 全局基线与组织约束确认

| 维度 | 结论 |
|------|------|
| 成本与 ROI | 以 Phase 5 §5.1 任务级实测为唯一口径：重构轨 79h + 特性轨 46h = 125h ≈ 15.6 人日（含测试与证据采集）；早期天级粗估「特性净 13 人日」已废弃（粗估含缓冲）；License 零新增（Polly v8 MIT）；云资源零新增 |
| 安全合规基线 | PIPL：M7 脱敏与日志面扩大同批交付；M10 异常上下文禁入栈变量值。OWASP：IRuntimeDataStore 全参数化（L0 硬门控既有）；M7 查询 API 权限点+租户过滤+路径白名单 |
| Conway 团队约束 | 单团队无专职 DevOps——一切需部署侧介入的形态（Seq/LGTM/Grafana）已排除；M8 用 DB 锁不引入 Redis 依赖 |

## §1.5 多轨隔离声明

| 轨道 | 目标 | 文件面 |
|------|------|--------|
| 重构轨 | RunService 绞杀者式引擎化，路由契约零差异 | `backend/modularity/visualdev/JNPF.VisualDev/{RunService.cs,Runtime/}` · `JNPF.VisualDev.Interfaces/IRunService.cs` · `backend/modularity/common/Common.CodeGen/.../ExportImportDataHelper.cs`（仅 Task 6.7，CR 门禁） · `backend/tools/JNPF.Analyzers/complexity-baseline.json` · `backend/tests/JNPF.Tests.VisualDev/` · `backend/tests/JNPF.Tests.Architecture/RunEngineSqlSugarBoundaryTests.cs` |
| 特性轨 | 四特性降级版（开关门控） | M7：`application/JNPF.API.Entry/{Infrastructure/,Services/}` · M8：`infrastructure/JNPF.Extras.EventBus.Outbox/` · M9：`modularity/inteAssistant/JNPF.InteAssistant/` · M10：`modularity/common/JNPF.Common.Core/{ExceptionBoundary/,Filter/}` · M11：`framework/JNPF/`+`Configurations/App.json` · **特性轨测试载体**：`JNPF.Tests.Common`（M7/M10/M11）· `JNPF.Tests.Stage5`（M8）· `JNPF.Tests.Phase6`（M9） |

**隔离纪律**：两轨文件面禁止交叉（实证零交集）。共享文件声明：`Program.cs`/启动链（M7 请求日志注册）与 `App.json`（M11）为特性轨独有写入点，重构轨禁触；`JNPF.Tests.Architecture/` 白名单断言扩展归重构轨。

## §1.6 向用户提问

**无新问题**——模板禁止已知答案的问题。前轮三问已拍板：①部署能力→降级确认 ②开关→四特性级粒度批准 ③幂等→整体砍除只留 Sweeper。

**🔒 Phase 1 检查点：已通过（用户 2026-08-21 确认）。**

---

# 第三部分：Phase 2 — 核心取舍博弈（决策快照，已拍板）

> 博弈过程已于对话中完成；Phase 3 各模块选型结论引用本节编号（ADR-C/ADR-R/ADR-F），不重复展开。

## §2.1 决策难点 1：混入形态

**决策背景**：③混入式的死穴是验证链污染——路由快照零 diff 只能验证行为不变的纯移动，四特性全是行为变更；一旦混流，冒烟红无法归因。实证关键事实：四特性文件面与重构文件面零交集。

| 方案 | 优点 | 缺点 | 代价 | 失效条件 |
|------|------|------|------|---------|
| A 严格串行 | 验证链绝对纯净 | 违背③决策；特性偿付再入「下一期」黑洞 | 周期 +100% | 用户拒绝长周期 |
| B 双轨全并行 | 理论周期最短 | 共享冒烟基线致归因污染；单团队并行=上下文切换幻觉 | 门禁矩阵复杂化 | 冒烟红时无法定位责任轨 |
| **C 单轨穿插+双门禁分段裁决** | 文件面零交集支撑有序穿插；归因保全 | 阶段窗口拉长 ~30% | 单阶段审批材料双段 | **特性文件面与重构面出现交集时失效**（需重评隔离） |

**推理链与拍板**：A 违背决策初衷；B 的致命伤是共享冒烟基线（验证手段本身被污染）；C 是唯一同时满足「③混入/验证链不污染/单团队可执行」的形态。**拍板=C**。红线纪律：**特性门禁红不阻塞重构门禁**（下称 ADR-C）。

## §2.2 决策难点 2：回滚轴分工

**决策背景**：纯移动重构行为等价，运行时开关两侧代码行为相同——开关语义为空；若做双路并存则 4157 行旧类全量保留，拆分目标当场作废。

| 方案 | 优点 | 缺点 | 代价 | 失效条件 |
|------|------|------|------|---------|
| A EnableNewRunServiceEngine 双路开关 | 运行时可切 | 旧类全保留=拆分白拆；双路测试面翻倍 | 维护黑洞 | 开关移除日永不到来 |
| **B 回滚轴分工** | 重构 revert 彻底（连代码存在性回退）；特性开关精确熔断 | 两种机制并存需纪律 | 0.5d 开关基建 | git 历史被 rebase 破坏时 revert 轴失效 |

**推理链与拍板**：问「决策错了回退成本多大」——重构每阶段独立 commit+快照零 diff，revert 单 commit 即完整回退，比运行时开关更彻底；特性是真实行为变更，必须有运行时熔断且粒度到特性级（一损俱损的总开关被否决）。**拍板=B**：重构=阶段级 git revert（ADR-R）；特性=App.json 四布尔位默认 false（ADR-F）。

## §2.3 核心架构图（重构场景：前后对比 + 组件交互）

```
【前】 WorkFlow + 5 注入点 ─► RunService（4157行，42方法，持 SqlSugarScope _sqlSugarClient 唯一可变状态）
                                ├── _visualDevRepository.AsSugarClient()×49 + _sqlSugarClient×8（直调）
                                └── 编译/执行/列表/视图/DB路由 六职责糅合

【后】 WorkFlow(IRunService 7方法) ─► RunService 门面(<400行, ITransient)
       5 注入点 ──────────────────────┘  │ 委托（同步/进程内 DI）
                        ┌────────────────┼────────────────┐
                        ▼                ▼                ▼
              RunListQueryService  RunDataViewService  RunDataEngine   （均 ITransient）
                        └────────────────┼────────────────┘
                                         ▼ 构造注入（白名单硬门控）
                   RunSqlCompiler(ISingleton,纯函数) + IRuntimeDataStore(接口)
                                                            ▲
                                      SqlSugarRuntimeDataStore（唯一 SqlSugar 绑定点）

【特性轨（零交集文件面，开关门控）】
M11 App.json 四布尔 ─┬─ M7 Serilog app-sink+请求日志+脱敏+LogQuery API（API.Entry）
                     ├─ M8 OutboxSweeperService+DB锁（EventBus.Outbox，异步30s轮询）
                     ├─ M9 Polly v8 标准管线（InteAssistant LLM/MCP 命名客户端，出站HTTP）
                     └─ M10 IExceptionBoundary 非HTTP入口 + LogExceptionHandler 结构化
```

**🔒 Phase 2 检查点：已通过（两项拍板在案：C / B）。**

---

# 第四部分：Phase 3 — 功能模块设计（认知闭环驱动）

## §0 全局架构约束（跨模块+多轨，强制）

| 约束 | 唯一入口 | 禁止绕过方式 | 验证手段 |
|------|---------|-------------|---------|
| 引擎 DB 副作用唯一漏斗 | `IRuntimeDataStore` | 引擎构造注入白名单外任何 DB 类型（SqlSugar/IDataBaseManager/ISqlSugarRepository/Dapper） | 架构测试构造白名单断言（M2） |
| SqlSugar 唯一绑定点 | `SqlSugarRuntimeDataStore` | 引擎引用 SqlSugar 命名空间类型 | 架构测试 SqlSugar 引用扫描（M2） |
| 路由契约零差异 | harness `--mode routes --filter "api/visualdev"` 快照 | 任何阶段快照 diff 非空即停 | 每模块收尾 Compare-Object（M1 基线） |
| JNPF009 只随迁 | `complexity-baseline.json` file/symbol 路径更新 | 值上调或新增条目 | `dotnet build /p:CI_BUILD=true` 0 新增 |
| 特性行为变更唯一开关 | `RuntimeFoundationOptions` 四布尔位 | 特性轨未门控的行为变更直接生效 | M11 单测 + M10 翻牌序列 |
| 多轨文件面隔离 | §1.5 声明 | 重构轨触特性面 / 特性轨触重构面 | 每模块 Stage 4 矩阵 🚫 栏 + commit 前 git status 核对 |

## 模块索引与挂载表（S0-S5 双轨编排）

| 模块 | 名称 | 轨道 | 挂载阶段 | 工时 | 依赖 |
|------|------|------|---------|------|------|
| M1 | 安全网 | 重构 | S0 | 7h | 无 |
| M2 | 数据访问抽象 | 重构 | S2 | 23h | M3 |
| M3 | 编译层 | 重构 | S1 | 16h | M1 |
| M4 | 执行层 | 重构 | S3 | 10h | M2 |
| M5 | 列表层 | 重构 | S4 | 8h | M4 |
| M6 | 视图层与收尾 | 重构 | S4-S5 | 15h | M5 |
| M7 | 可查询日志 | 特性 | S2 | 12h | M11 |
| M8 | Outbox 可靠性 | 特性 | S3 | 9h | M11 |
| M9 | 出站韧性 | 特性 | S4 | 10h | M11 |
| M10 | 异常边界 | 特性 | S5 | 11h | M11 |
| M11 | 特性开关基建 | 特性 | S0 | 4h | 无 |

> M6 命名含「与」字面说明：「视图层与收尾」为 S4b+S5 的**阶段聚合**而非职责合并，内部任务严格原子（命名铁律在任务级执行）。若委员会认为违规模糊，拆分为 M6a/M6b 两模块重编号即可（结构不受影响）。

---

## Module M11：特性开关基建（先行，最先施工）

预估总工时：4h ｜ 依赖：无

### Stage 0：侦察

**0.1 存量事实清单**

| 事实 | 精确来源 | 约束的后续阶段 |
|------|---------|---------------|
| F1 App.json 为 JNPF 配置约定文件，IConfiguration 自动绑定 | `backend/application/JNPF.API.Entry/Configurations/App.json` | → Stage 2.3 配置节方案 |
| F2 Serilog 配置期 DI 未就绪（Log.Logger 在 Host 构建前创建） | `Infrastructure/SerilogBootstrap.cs:19`（Configure(IConfiguration) 签名） | → Stage 2.5：M7 开关读取必须走 IConfiguration 而非 IOptions |
| F3 仓库既有 Options 类归属惯例待核 | [待确认:Options 类现行归属目录 framework/JNPF 或 Common.Core] | 阻塞 Task 11.1 落位（开工 10 分钟内 grep 即解，不阻塞设计） |

**0.2 输入输出边界**

| 方向 | 实体 | 协议/方式 | 来源/去向 |
|------|------|----------|----------|
| 输入 | RuntimeFoundation 配置节 | 配置文件绑定 | App.json |
| 输出 | 四布尔开关值 | IOptions/RuntimeFoundationOptions | M7/M8/M9/M10 门控判断点 |

### Stage 1：业务分析

**1.1 功能实现目标**

| 维度 | 内容 |
|------|------|
| 核心业务问题 | 让每个新基座能力可以「单独关电闸」——出问题时只关掉它，不株连其他能力，也不回滚重构成果 |
| 使用角色与场景 | 开发/实施人员在冒烟异常或现场故障时修改配置重启，频率≈每特性一次（翻牌窗口）+应急 |
| 业务成功标准 | 任一特性异常时，改一个布尔位+重启即恢复原状行为，其余三特性与重构成果不受影响 |
| 链路位置 | 上游：App.json 配置 → 本模块 → 下游：M7/M8/M9/M10 的门控分支 |
| 不解决的后果 | ③混入式失去定点回滚能力，任一特性翻车即整体回滚（ADR-F 的反面） |

**1.2 核心业务流程**

主流程：读取 App.json 配置 → 绑定 RuntimeFoundationOptions → 供消费方判断 → 到达终态：配置生效。
关键分支：配置节缺失 → 四值默认 false（现状行为，兜底终态）。
业务异常流程：配置值写错（非 bool）→ 绑定失败抛配置异常 → 启动失败即暴露（配置错误不允许静默）。
终态枚举：`全 false`（现状，默认）/`单项 true`（翻牌中间态）/`全 true`（S5 终审目标态）。

**1.3 业务逻辑边界**：负责=开关定义与默认值兜底；不负责=开关的消费逻辑（归各特性模块）与翻牌操作（归 M10 Task 10.4）。数据所有权：App.json 配置节由 M11 定义，M7-M10 只读。

**1.4 业务规则清单**

| 编号 | 规则 | 判断条件（精确） | 动作 | 来源 | 优先级 |
|------|------|-----------------|------|------|--------|
| BR-1 | 默认全关 | 配置节缺失或值缺省 | 四值=false | ADR-F 拍板 | 高 |
| BR-2 | 翻牌唯一窗口 | 修改 App.json 开关值 | 仅允许 M10 Task 10.4 终审窗口 | ADR-F/ADR-C | 高 |
| BR-3 | 配置错误不静默 | 值类型≠bool | 启动失败 | 模板纪律「宁可说待确认不编造」同源 | 中 |

规则冲突声明：无冲突。

**1.5 约束传递**

| 业务分析产出 | 约束的六维维度 | 约束内容 |
|-------------|---------------|---------|
| F2（Serilog 期 DI 未就绪）+ 1.2 兜底终态 | 2.5 运行时 | 开关读取必须兼容 DI 未就绪场景（IConfiguration 直读） |
| 1.3 数据所有权（M7-M10 只读） | 2.3 组件化 | 只暴露 Options POCO，不提供写接口 |

### Stage 2：六维深度设计

- **2.1 算法**：业务锚点=1.2 主流程；无计算路径——配置绑定为框架既有能力。**N/A 判断依据**：无匹配/排序/聚合计算，复杂度讨论无对象。
- **2.2 内存**：Options POCO 进程级单例（IOptions 语义），4 个布尔位，无生命周期问题。**N/A+依据**：无资源创建/释放/池化。
- **2.3 组件化**：业务锚点=1.3。选定=独立 `RuntimeFoundationOptions` POCO + `Section` 常量；否决=每特性各自读配置键（四处字符串键漂移风险）；失效条件：特性数 >8 时考虑分组配置节。契约见 2.7。
- **2.4 健壮性**：边界防御=配置节缺失→Get 返回默认实例全 false（BR-1）；类型错误→抛出（BR-3，不吞）。无外部依赖。
- **2.5 运行时**：承接 F2——SerilogBootstrap 内 `cfg.GetSection(Section).GetValue<bool>(...)` 直读；DI 就绪后的消费方用 IOptions<RuntimeFoundationOptions>。两通道读同一配置源，无可见性问题（配置不可变）。
- **2.6 对标**：.NET FeatureManagement 包（Microsoft.FeatureManagement）可承载，但引入包=引入依赖面，四个布尔位的 POCO 自研 <20 行——选自研；切换触发条件：需要百分比放量/用户分组灰度时引入 FeatureManagement。

**2.7 代码契约骨架**（来源 2.3）：

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

### Stage 3：红队自检

- **Q1 删除-引用矛盾**：本模块无删除/替换操作。结论：无矛盾。
- **Q2 参数自洽**：无数值参数。结论：不适用。
- **Q3 DoD 可达**：Task 11.1 的 DoD「2 用例绿」前置=Options 类+配置节落盘，必然可达。结论：可达。
- **Q4 BR 覆盖**：BR-1→2.4 默认值断言用例；BR-2→M10 Task 10.4 承接（跨模块引用成立）；BR-3→2.4 抛出语义+绑定用例。结论：全覆盖。

### Stage 4：手术刀拆分

**4.1 精确修改点**：新建文件为主（Options 类+测试），App.json 追加配置节（追加式，无行号依赖）；编译预演：Options 类先于测试编译（同批提交）。

**4.2 变更影响声明**：App.json 追加节对现有消费方零影响（新增键）。

**4.3 验收契约（DoD）**：

- [ ] `RuntimeFoundationOptionsTests` 2 用例绿（默认全 false / 配置绑定）→ 若失败回滚：revert 本任务 commit
- [ ] 缺配置节兜底断言绿 → 若失败回滚：同上

监控与可观测性：N/A——配置件，依赖消费方特性各自指标（铁律4 满足其一：本模块日志载体=配置绑定失败启动异常本身）。

**4.4 文件变更矩阵**

| 类型 | 路径 | 说明 | 迁移策略 |
|------|------|------|---------|
| ➕ | `backend/framework/JNPF/Options/RuntimeFoundationOptions.cs`（归属按 F3 开工核对） | Options 类 | — |
| ✏️ | `Configurations/App.json` | 追加 RuntimeFoundation 节（全 false） | 配置追加零迁移 |
| ➕ | `backend/tests/JNPF.Tests.Common/RuntimeFoundationOptionsTests.cs` | 2 用例 | — |
| 🚫 | 重构轨全部文件面（§1.5） | 多轨隔离 | — |

**4.5 任务拆分**：Task 11.1 开关基建落盘（4h，依赖无：含 Options 类+配置节+测试+日志基线采集 `f0-log-baseline.txt`——基线采集随本任务顺带，供 M7 Stage 3 磁盘风险对照）。

**4.6 向用户提问**：无待确认项。

---

## Module M1：安全网

预估总工时：7h ｜ 依赖：无

### Stage 0：侦察

| 事实 | 精确来源 | 约束的后续阶段 |
|------|---------|---------------|
| F1 RunService 4157 行/42 方法/JNPF009 占 8 条（CC140 全仓最高） | `backend/modularity/visualdev/JNPF.VisualDev/RunService.cs` + `complexity-baseline.json` | → M3-M6 全部拆分边界；→ Stage 1 成功标准 |
| F2 RunService 非 IDynamicApiController，API 面在三委托方（OnlineDev/Base/ShortLink） | CR-20260820-01 排查实证 | → Stage 1.3：路由契约由委托方间接承载 |
| F3 IRunService 17 方法，WorkFlow 实际消费 7 个（调用点计数实证） | `IRunService.cs` + WorkFlow 三消费类 grep | → M6 瘦身目标 17→7 |
| F4 harness `--mode routes` 反射枚举 ActionDescriptor 能力现成 | `backend/tools/JNPF.Startup.Benchmarks/Program.cs` RunRoutes | → Stage 2.6 复用（不自研） |
| F5 inproc 下 DatabaseModule 注册失败（F2 缺陷，performance-baseline 登记） | `performance-baseline.md` §已知偏差 | → Stage 3 Q2 快照量级风险 |
| F6 JNPF.Tests.VisualDev 已有 23 个 Helpers 测试 | 目录实证 | → 每模块回归基线 |

**0.2 输入输出边界**：输入=程序集反射元数据（进程内）；输出=路由基线文件+契约测试（消费方：M2-M6 每模块收尾门禁）。

### Stage 1：业务分析

**1.1**：核心业务问题=「给 4157 行的开膛手术装上全程心电图」——任何一刀切坏了契约，立刻报警。使用角色=施工者每模块收尾跑一次。业务成功标准=任一后续模块若破坏路由/接口契约，本模块产出的门禁必然变红。链路位置=上游：现状代码 → 本模块 → 下游：M2-M6 门禁。不解决的后果=纯移动重构失去「零差异」验证手段，等同裸奔。

**1.2**：主流程：枚举全量路由 → 过滤 api/visualdev → 落盘基线 → 反射固化 IRunService 17 签名与三委托方归属 → 终态：基线在案。异常流程：route_matched=0（过滤串错）→ 停手排查重跑。终态枚举：基线成立 / 基线采集失败（阻塞全部后续）。

**1.3**：负责=契约基线采集与守护测试；不负责=契约破坏的修复（归各模块）。数据所有权：基线文件由 M1 创建，后续模块只读比对禁改。

**1.4 业务规则**：

| 编号 | 规则 | 判断条件 | 动作 | 来源 | 优先级 |
|------|------|---------|------|------|--------|
| BR-1 | 快照零差异 | Compare-Object 输出非空 | 该模块停手定位回滚 | A+C spec 死线 | 高 |
| BR-2 | 契约回填禁止手写 | 签名期望集 | 必须由 typeof 反射输出固化 | 防编造纪律 | 高 |
| BR-3 | 防误伤域同采 | api/permission/users 基线 | 与 visualdev 同批落盘 | CR-01 先例 | 中 |

**1.5 约束传递**：F5（快照量级风险）→ Stage 3 Q2 数学验证项；F3（17/7 实证）→ M6 的 BR 依据；BR-1 → 全局约束表第 3 行（验证手段=本模块基线）。

### Stage 2：六维深度设计

- **2.1 算法**：反射枚举+字符串排序比对，O(n log n)，n≈路由总数（千级）——无瓶颈。**N/A+依据**：一次性采集，量级无优化空间需求。
- **2.2 内存**：N/A——一次性进程，无资源生命周期。
- **2.3 组件化**：业务锚点=F4。选定=复用 harness RunRoutes，测试类用反射+属性名字符串匹配（CR-01 模式，不引 MVC 程序集依赖）；否决=起真实 HTTP 服务采 Swagger（慢且依赖环境）；失效条件：harness 注册链与真实启动链漂移时需改用真实进程采集。
- **2.4 健壮性**：依赖故障=harness inproc 失败（F5）→ 核对 route_total 量级与上次一致；契约测试对现状必然绿，不绿=回填错误（自我验证）。
- **2.5 运行时**：N/A——离线工具执行。
- **2.6 对标**：契约快照模式对标 ApprovalTests/快照测试流派；取舍=只取「基线+diff」语义，不引入其文件管理机制（evidence 目录已有约定）。

**2.7 契约骨架**（来源 2.3）：

```csharp
public class RunServiceContractTests
{
    [Fact] public void IRunService_MemberCount_IsSeventeen();
    [Fact] public void IRunService_MemberSignatures_AreFrozen(); // 期望集=反射回填常量
    [Theory] [MemberData(nameof(WorkFlowConsumed))]
    public void WorkFlowConsumed_Method_Exists(string methodName); // F3 七方法
}
public class VisualDevRouteOwnerTests
{
    [Theory] [MemberData(nameof(Owners))] // (类名, Name, Route模板) 三元组反射回填
    public void DelegateOwner_KeepsNameAndRoute(string typeName, string expectedName, string expectedRouteTemplate);
}
```

### Stage 3：红队自检

- **Q1**：无删除/替换操作。无矛盾。
- **Q2 参数自洽**：唯一量级参数=route_matched，验证：F2 三委托方在案 → matched>0 必然成立；F5 风险登记为观察项（route_total 量级核对）。✅
- **Q3 DoD 可达**：基线落盘 DoD 前置=harness 可运行，F5 已给兜底路径。可达。
- **Q4 BR 覆盖**：BR-1→基线文件（比对载体）；BR-2→回填流程入 DoD；BR-3→同采步骤。全覆盖。

### Stage 4：手术刀拆分

**4.3 验收契约（DoD）**：
- [ ] `s0-routes-visualdev-baseline.txt` 存在且 `[METRIC] route_matched>0` → 失败回滚：重跑并核对 route_total 量级
- [ ] `api/permission/users` 防误伤基线落盘 → 失败回滚：同上
- [ ] 两契约测试类全绿（回填基线固化）→ 失败回滚：修正回填（以代码为准），禁改接口

**4.4 矩阵**：➕ 两个基线文件（evidence）➕ 两个测试类 ｜ 🚫 特性轨文件面。

**4.5 任务**：Task 1.1 路由快照基线（2h）｜ Task 1.2 IRunService 契约测试（3h）｜ Task 1.3 委托方归属测试（2h）。

**4.6 提问**：无待确认项。

---

## Module M3：编译层

预估总工时：16h ｜ 依赖：M1

### Stage 0：侦察

| 事实 | 精确来源 | 约束的后续阶段 |
|------|---------|---------------|
| F1 编译类 7 方法：GetListQuerySql(2302,CC140)/GetInfoQuerySql(2907)/GetQueryJson(2967,CC72)/GetSuperQueryJson(3517)/GetSuperQueryInput(2189)/GetIConditionalModelListByTableName(2896)/GetVisualDevModelDataConfig(2016,CC71) | `RunService.cs` 行号实测（A+C spec §3.1） | → Stage 4 移动清单 |
| F2 JNPF009 基线含上述条目 | `complexity-baseline.json` | → Task 3.4 随迁 |
| F3 直接针对 RunService 的测试=0；Helpers 测试 23 个覆盖部分编译辅助 | JNPF.Tests.VisualDev 目录 | → Stage 1 成功标准（首个可单测面） |
| F4 DI 约束表裁定 Compiler=Singleton（零状态纯函数） | `runservice-refactor-di-constraints.md` §2 | → Stage 2.3 生命周期 |

**0.2 边界**：输入=查询条件 JSON/模型元数据（方法参数）；输出=SQL 文本/IConditionalModel 列表（消费方：M4/M5/M6 引擎）。

### Stage 1：业务分析

**1.1**：核心业务问题=把「把可视化模型翻译成 SQL」这门手艺从 4157 行大杂烩里独立出来，变成可以单独考试的手艺人。使用角色=下游引擎（每次列表/详情查询触发，高频）。业务成功标准=翻译手艺逐字不变（特征单测固化现状输出），且从此可以单独考试（零 DB 依赖单测）。链路位置=上游：M4/M5/M6 传参 → 本模块 → 下游：SQL/IConditionalModel 返回。不解决的后果=CC140 巨兽继续不可测，任何 SQL 生成改动全仓裸奔。

**1.2**：主流程：接收查询输入 → 拼装条件/JOIN/分页/超级查询 SQL → 返回。分支：超级查询 vs 普通查询（走不同拼装路径）。业务异常流程：输入条件为空 → 返回基础 SQL（不抛错，存量语义）。终态：SQL 文本产出。

**1.3**：负责=SQL/IConditionalModel 生成；不负责=取数（归 M2/调用方——编译层零 DB 是硬约束）、执行。职责分界线=是否触碰 SqlSugar 类型（二值判定）。数据所有权：Compiler 无状态不持数据。

**1.4 业务规则**：

| 编号 | 规则 | 判断条件 | 动作 | 来源 | 优先级 |
|------|------|---------|------|------|--------|
| BR-1 | 编译层零 DB | 方法体出现仓储/SqlSugar 调用 | 取数留调用方，结果经参数传入 | A+C spec 硬约束 | 高 |
| BR-2 | 方法体逐字不改 | git diff 出现逻辑变更 | 拒绝合入 | 绞杀者纪律 | 高 |
| BR-3 | CC 值随迁不变 | baseline 值变更 | 拒绝合入 | JNPF009 铁律 | 高 |

**1.5 约束传递**：BR-1 → 2.3 组件化（零 DI 依赖设计）+ Task 3.3 剥离任务存在性；F1 → Stage 4 移动清单逐行对应；F3 → 1.1 成功标准（特征单测）。

### Stage 2：六维深度设计

- **2.1 算法**：业务锚点=1.2 拼装流程。本模块是既有算法的搬家，不改写（BR-2）——复杂度现状 CC140 保持（BR-3，降 CC 属二期 2.10）。设计决策：选定=纯移动；否决=顺手优化拼装逻辑（否决理由：行为变更混入纯移动，验证链污染）；失效条件：二期降 CC 立项时重开。
- **2.2 内存**：无状态纯函数，无资源持有（F4）。**N/A+依据**：无对象生命周期管理对象。
- **2.3 组件化**：业务锚点=BR-1。选定=`RunSqlCompiler : ISingleton` 零构造依赖；否决=注入仓储「顺手取数」（否决理由：可单测面依赖零 DI；DI 约束表禁注入）；失效条件：出现必须跨方法缓存的编译中间产物时重评（当前无）。契约见 2.7。
- **2.4 健壮性**：边界防御=空条件输入按存量语义返回基础 SQL（不改写不新增校验——BR-2）。异常语义原样保留（裸 throw 登记入 M10 基线台账）。
- **2.5 运行时**：Singleton 纯函数天然并发安全（无共享可变状态）。无异步边界变更。
- **2.6 对标**：对标 Mendix/OutSystems 的 QueryCompiler 分层（编译与执行分离）；取舍=本期只分离不重写，编译器内部结构维持原样（绞杀者纪律）。

**2.7 契约骨架**（来源 2.3）：

```csharp
public class RunSqlCompiler : ISingleton { }
// 7 方法签名自 RunService 原样迁入（F1 清单）；若 BR-1 剥离触发，受影响方法签名增加预取参数：
// 例：public string GetXxx(QueryInput input, List<FieldMeta> preloadedMeta)
```

### Stage 3：红队自检

- **Q1 删除-引用矛盾**：删除=RunService 内 7 方法声明。引用核查：RunService 调用点改 `_compiler.X` 委托（Task 3.2 DoD 覆盖）；IRunService 成员保留委托转发。无矛盾。
- **Q2 参数自洽**：无数值参数。不适用。
- **Q3 DoD 可达**：Task 3.2 的「grep RunSqlCompiler 零 SqlSugar」前置=Task 3.3 剥离完成——依赖顺序 3.2→3.3 成立（先移动后剥离，剥离针对移动后代码）。可达。
- **Q4 BR 覆盖**：BR-1→Task 3.3+2.3；BR-2→Task 3.2 DoD（git diff 断言）；BR-3→Task 3.4。全覆盖。

### Stage 4：手术刀拆分

**4.1 精确修改点**：F1 清单 7 方法 `RunService.cs:2302/2907/2967/3517/2189/2896/2016` → `Runtime/RunSqlCompiler.cs`（整方法移动，行号以开工实测为准）；编译预演：先骨架后移动，每方法移动后构建一次。

**4.2 变更影响**：7 方法由 RunService 迁出，模块内调用点全在 visualdev（编译即验证）；IRunService 成员不动（本模块禁触）。

**4.3 验收契约（DoD）**：
- [ ] 7 方法迁出+方法体逐字未改 → 失败回滚：revert Task 3.2 commit
- [ ] `grep SqlSugar Runtime/RunSqlCompiler.cs` 0 匹配 → 失败回滚：回 Task 3.3 补剥离
- [ ] `RunSqlCompilerTests` ≥14 用例全绿（特征值现状运行固化）→ 失败回滚：定位移动误改
- [ ] CI_BUILD 0 新增 JNPF009 → 失败回滚：补 baseline 随迁条目

监控与可观测性：N/A——纯移动无运行时行为变更（日志载体=既有日志不变）。

**4.4 矩阵**：➕ `Runtime/RunSqlCompiler.cs` ➕ `RunSqlCompilerTests.cs` ｜ ✏️ `RunService.cs` ✏️ `complexity-baseline.json` ｜ 🚫 `IRunService.cs` 🚫 特性轨文件面。

**4.5 任务**：Task 3.1 骨架（1h）｜ Task 3.2 七方法纯移动（4h）｜ Task 3.3 DB 依赖参数化剥离（4h）｜ Task 3.4 基线随迁（2h）｜ Task 3.5 特征单测（4h）｜ Task 3.6 S1 快照门禁（1h，证据 `s1-routes.txt`）。

**4.6 提问**：无待确认项。

---

## Module M2：数据访问抽象

预估总工时：23h ｜ 依赖：M3（S1 完成后开工）

### Stage 0：侦察

| 事实 | 精确来源 | 约束的后续阶段 |
|------|---------|---------------|
| F1 `_visualDevRepository.AsSugarClient()` 49 处：Queryable×27/Utilities×12/SqlQueryable×7/CurrentConnectionConfig×3 | `RunService.cs` grep 实测（A+C spec §2） | → Stage 4 台账 L/Q 编号分工 |
| F2 `_sqlSugarClient` 直调 8 处：AsTenant×4/SqlQueryable×4；字段非 readonly+IDisposable | `RunService.cs` 字段声明+grep | → Stage 2.2 状态迁移；→ Stage 3 Q1 删除引用核查 |
| F3 IRuntimeDataStore 契约已在 A+C spec §4 定稿（7 成员） | `2026-08-20-...design.md` §4 | → Stage 2.3 契约逐字采用 |
| F4 DI 约束表：DataStore=Transient（承接原 RunService 状态与 Dispose） | `runservice-refactor-di-constraints.md` §2 | → Stage 2.5 生命周期 |
| F5 多表查询 Where/OrderBy 别名不一致曾致运行时 500（既有踩坑） | 项目 common_pitfalls 记忆 + 历史事故 | → Stage 2.4 等价比对风险 |
| F6 元数据实体（VisualDevEntity 等平台表）Queryable 属 D1 修订边界：不改写表达式树 | A+C spec §3.2 | → Stage 1.3 职责边界；→ Task 2.5 M 系列处置 |

**0.2 边界**：输入=引擎层方法调用（SQL+参数/条件模型）；输出=查询结果集/执行行数/事务边界/DbLink 路由（消费方：M4/M5/M6 引擎）。

### Stage 1：业务分析

**1.1**：核心业务问题=让运行时引擎与「用哪家数据库」解耦——今天 SQL Server，明天 PostgreSQL/时序库，引擎一行不改。使用角色=M4/M5/M6 引擎（每次数据操作，最高频路径）。业务成功标准=引擎类代码里找不到任何 SqlSugar 字样（架构测试断言），且现有 49+8 处调用的行为逐一等价。链路位置=上游：三引擎 → 本模块 → 下游：SqlSugarScope（唯一绑定点）。不解决的后果=PG/时序兼容永远是口号，每次换库讨论都从 4157 行里重新考古。

**1.2**：主流程：引擎提交 SQL/条件 → DataStore 参数化执行（含租户切换）→ 返回结果集。关键分支：外部数据源调用 → 先 ResolveDbLink 路由再执行。业务异常流程：SQL 执行失败 → 原样抛出（异常语义不吞不改，治理归 M10）。终态：结果集返回 / 异常抛出。

**1.3**：负责=DB 副作用唯一漏斗（执行/查询/事务/租户/路由）；不负责=SQL 生成（归 M3 编译层）、业务编排（归三引擎）、元数据实体的 LINQ 表达式树形态（F6 边界，仅封装其执行入口）。职责分界线=是否触碰 SqlSugar 类型。数据所有权：SqlSugarRuntimeDataStore 持有 SqlSugarScope（承接 F2，进程内每请求图）。

**1.4 业务规则**：

| 编号 | 规则 | 判断条件 | 动作 | 来源 | 优先级 |
|------|------|---------|------|------|--------|
| BR-1 | 全参数化 | SQL 出现字符串插值 | L0 钩子拦截+拒绝合入 | OWASP/项目铁律 | 高 |
| BR-2 | 豁免废除 | Queryable 改写「无法等价」 | 经 IRuntimeDataStore 扩展成员承载，禁保留 SqlSugar 直调 | v5.2 审查修订#1 | 高 |
| BR-3 | 台账编号不重叠 | L1-L36（收敛）vs Q1-Q27（改写）vs M 系列（扩展成员） | 三类分账，行号唯一键 | v5.2 审查修订#13 | 中 |
| BR-4 | 元数据表达式树不改写 | VisualDevEntity 等 LINQ 形态 | 仅封装执行入口入 M 系列 | F6/D1 边界 | 高 |
| BR-5 | 租户语义不脱离上下文 | tenantId 参数缺省 | 走当前请求上下文（与原 AsTenant 行为一致） | 多租户铁律 | 高 |

规则冲突声明：BR-2 与「纯移动不改写」张力→裁定：改写限于执行通道（SQL 等价比对守护），方法体业务逻辑不动。

**1.5 约束传递**：F1/F2 → 2.3 契约能力面（只含现有能力，YAGNI）；BR-2 → 2.4 扩展成员兜底路径；F5 → 2.4 别名核对项；BR-5 → 2.4 租户参数语义；F2 → 2.2 状态迁移设计。

### Stage 2：六维深度设计

- **2.1 算法**：业务锚点=1.2 主流程+BR-2。核心问题=27 处 LINQ Queryable 的 SQL 化等价判定。选定=逐处 ToSql 前后抓取+Normalize 比对（去空白+占位符归一）；否决=「信任 SqlSugar 编译器不比对」（否决理由：F5 别名踩坑证明生成 SQL 存在隐性差异面）；失效条件：Normalize 无法覆盖的语义差异（排序/类型转换）出现时，该处转 M 系列扩展成员。
- **2.2 内存**：业务锚点=F2。SqlSugarScope 生命周期经 `Transient+IDisposable` 承接——初始化代码自 RunService 构造函数原样迁入，Dispose 置空语义保留；每请求 DI 图创建/释放，与原语义一致（原 RunService 亦 Transient）。泄漏防御：构造初始化失败不得留半初始化状态（失败即抛）。
- **2.3 组件化**：业务锚点=1.1/1.3。选定=接口+单实现（SqlSugarRuntimeDataStore），契约逐字 F3；扩展成员按需新增（BR-2 触发时，逐条登记 M 系列）；否决=抽象工厂多 provider 预建（否决理由：PG/时序 provider 属 backlog 2.8/2.9，预建=YAGNI）；失效条件：第二个 provider 实装时（接口稳定性届时接受真实检验）。契约见 2.7。DI=Transient（F4）。
- **2.4 健壮性**：依赖故障=DB 不可用→原样抛出（不引入新降级——行为等价纪律）；边界防御=BR-1 参数化是唯一入口（契约不提供拼参重载）；BR-5 租户缺省语义；别名风险=F5 逐处核对（Where/OrderBy 别名与 Join 一致性）。重试/数学验证=N/A——本模块不做出站调用，执行语义单次即终（重试属 M9 域）。
- **2.5 运行时**：Transient 生命周期与 DI 图一致；异步语义承接原调用点（已全异步，`.Result/.Wait()=0` 实证）；无新并发面。
- **2.6 对标**：形态对标 Dapper 最小执行面 + EF Core `Database.ExecuteSqlRaw` 参数模型；取舍=保留 DataTable 返回（现网 Utilities 12 处消费）而非强推 IEnumerable（规避全消费面改造）；不引入 Dapper 本身（SqlSugar 执行能力已覆盖，引第二 ORM=绑定面扩大）。切换触发条件：PG provider 实装（2.8）时评估。

**2.7 契约骨架**（来源 2.3）：

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

public class SqlSugarRuntimeDataStore : IRuntimeDataStore, ITransient, IDisposable
{
    private readonly SqlSugarScope _client; // 初始化代码自 RunService 构造函数原样迁入
    public string Dialect => _client.CurrentConnectionConfig.DbType.ToString().ToLowerInvariant();
    public void Dispose() { /* 原 RunService.Dispose 语义原样迁入 */ }
}

// 扩展成员（仅当 M 系列台账触发时新增，逐条登记；示例）：
// Task<List<Dictionary<string, object>>> QueryByConditionAsync(string tableName, IEnumerable<IConditionalModel> conditions, string? tenantId = null);
```

伪代码（触发条件③：等价判定口径自然语言存在两种理解）：

```
for each queryable调用点 (Q1-Q27):
    sqlBefore = query.ToSqlString()
    改写为 SqlQueryable(同语义SQL, 参数)
    sqlAfter  = newQuery.ToSqlString()
    if Normalize(sqlBefore) == Normalize(sqlAfter): 记等价
    else: 回滚该处改写，改经 IRuntimeDataStore 扩展成员承载（M 系列）——禁止保留 SqlSugar 直调
```

### Stage 3：红队自检

- **Q1 删除-引用矛盾**：删除=`RunService._sqlSugarClient` 字段（Task 2.3）。引用核查：8 处直调（F2）必须在 Task 2.4/2.5 收敛完成后字段删除才可编译——**顺序约束已入任务依赖（2.3 删声明时旧调用点暂留为参数过渡，2.4/2.5 清零）**；AsTenant×4 收敛至 DataStore 内部后无残留引用。结论：矛盾已识别并以任务顺序消解。
- **Q2 参数自洽**：台账总量验证：36（L）+27（Q）=63 vs 实证 49+8=57——差异来源=同一调用点跨类别计数。**修正：台账以行号为唯一键去重，开工首步产出对账行（L/Q/M 合计=实测唯一行号数）**。✅已修正。
- **Q3 DoD 可达**：Task 2.5「全部完成改写无豁免残留」前置=扩展成员机制可用（2.7 契约含扩展示例）+比对流程定义（伪代码）。可达。
- **Q4 BR 覆盖**：BR-1→2.4 契约设计；BR-2→Task 2.5 DoD；BR-3→Q2 修正后的行号唯一键；BR-4→Task 2.5 M 系列；BR-5→2.4 租户语义。全覆盖。

### Stage 4：手术刀拆分

**4.1 精确修改点**：`RunService.cs` `_sqlSugarClient` 字段声明处（开工实测行号）删除+构造改注入；49+8 处调用点逐行入台账（行号键）；新增 `Runtime/` 三文件。编译预演：契约（Task 2.2）→实现（2.3）→收敛（2.4/2.5）顺序，每任务构建一次。

**4.2 变更影响**：`_sqlSugarClient` 状态迁移对外零可见（RunService 行为等价）；IRuntimeDataStore 扩展成员若触发=接口新增（向后兼容）。

**4.3 验收契约（DoD）**：
- [ ] 架构测试双断言绿（SqlSugar 扫描+构造白名单，RunSqlCompiler 即刻生效；反向用例红验证）→ 失败回滚：revert Task 2.1
- [ ] 台账 L/Q/M 对账行=实测唯一行号全勾 → 失败回滚：逐处定位补收敛
- [ ] `grep -E "SqlSugar|AsSugarClient" RunService.cs` 收敛后 0 匹配 → 失败回滚：回 Task 2.4/2.5
- [ ] 快照零 diff（`s2-routes.txt`）+ 外部数据源冒烟 200 + test:api 全绿 → 失败回滚：台账定位具体处 revert

监控与可观测性：N/A——行为等价迁移无新运行时行为（日志载体=既有日志不变）。

**4.4 矩阵**：➕ `Runtime/{IRuntimeDataStore,RuntimeDbLink,SqlSugarRuntimeDataStore}.cs` ➕ `RunEngineSqlSugarBoundaryTests.cs` ➕ 台账 ｜ ✏️ `RunService.cs`（字段删除+收敛；扩展成员若触发则 ✏️ 接口与实现）｜ 🚫 元数据实体 LINQ 表达式形态（BR-4）🚫 特性轨文件面。

**4.5 任务**：Task 2.1 边界架构测试（3h）｜ Task 2.2 契约与 RuntimeDbLink（2h）｜ Task 2.3 实现与状态迁移（4h）｜ Task 2.4 收敛 L 系列 36 处（⚠探索型 6h）｜ Task 2.5 改写 Q 系列 27 处（⚠探索型 6h）｜ Task 2.6 S2 门禁与外部链路冒烟（2h）。

**4.6 提问**：无待确认项。

---

## Module M4：执行层

预估总工时：10h ｜ 依赖：M2

### Stage 0：侦察

| 事实 | 精确来源 | 约束的后续阶段 |
|------|---------|---------------|
| F1 执行类 20 方法清单（Create 615/Update 878/BatchUpdate 937/SaveFlowFormData 1250/GetFlowFormDataDetails 1316/SaveDataToDataByFId 1362,CC90/OptimisticLocking 3808/DataTransferVerify 3864/UniqueVerify 2201/GenerateFeilds 1748,CC81/FieldBindDefaultValue 1995,CC82 等，全清单见 A+C spec §3.1） | `RunService.cs` 行号实测 | → Stage 4 移动清单 |
| F2 JNPF009 相关 4 条：SaveDataToDataByFId CC90/GenerateFeilds CC81/FieldBindDefaultValue CC82/DataTransferVerify CC74 | `complexity-baseline.json` | → Task 4.3 随迁 |
| F3 流程表单方法被 WorkFlow 经 IRunService 消费（SaveFlowFormData×4/GetFlowFormDataDetails×11 等调用点实证） | WorkFlow grep | → BR-2 委托转发必须保留 |
| F4 方法体内存在裸 throw new（精确数量开工 grep 登记） | [待确认:精确数量，开工登记入 M10 基线台账] | 不阻塞设计；阻塞 M10 基线断言数值 |

**0.2 边界**：输入=门面委托/引擎间调用（表单数据/流程数据）；输出=CRUD 结果/校验结论（消费方：门面→委托方 API→前端；WorkFlow 经 IRunService）。

### Stage 1：业务分析

**1.1**：核心业务问题=把「写数据」的全部手艺（建/改/删/流程表单存取/唯一校验/乐观锁）集中到一个专职工匠手里。使用角色=在线开发表单操作者（增删改查，日常高频）+ WorkFlow 流程引擎（流程表单存取，流程触发频率）。业务成功标准=表单数据增删改查与流程表单存取的对外行为逐字不变（CRUD 冒烟四步全 200）。链路位置=上游：门面/WorkFlow → 本模块 → 下游：M2 数据访问。不解决的后果=写逻辑散在 4157 行中，乐观锁/唯一校验等关键一致性逻辑无法被独立审视。

**1.2**：主流程（以 SaveFlowFormData 为例）：接收流程表单数据 → 字段生成/默认值绑定 → 唯一校验 → 事务写入 → 返回。关键分支：有表/无表（CreateHaveTableSql 路径）；批量/单条。业务异常流程：唯一校验失败 → Oops.Bah 业务异常 → 用户看到重复提示（存量语义不改）。终态：写入成功 / 业务异常抛出。

**1.3**：负责=CRUD+流程表单+校验+乐观锁；不负责=SQL 编译（归 M3）、列表装配（归 M5）、数据视图（归 M6）。职责分界线=写路径 vs 读编排。数据所有权：运行时业务表数据由本模块经 M2 写入，读方为 M5/M6。

**1.4 业务规则**：

| 编号 | 规则 | 判断条件 | 动作 | 来源 | 优先级 |
|------|------|---------|------|------|--------|
| BR-1 | 方法体逐字不改 | git diff 逻辑变更 | 拒绝合入 | 绞杀者纪律 | 高 |
| BR-2 | IRunService 成员委托保留 | SaveFlowFormData 等 F3 成员 | RunService 一行委托 | WorkFlow 契约 | 高 |
| BR-3 | 裸 throw 登记基线 | 迁移遇裸 throw new | 原样保留+登记 M10 台账 | v5.2 修订#3 | 中 |
| BR-4 | CC 随迁 | 4 条 baseline 条目 | file/symbol 改指 RunDataEngine 值不变 | JNPF009 | 高 |

**1.5 约束传递**：F3 → BR-2；BR-1 → Stage 2 全维度（纯移动无设计改写空间，六维以承接与守护为主）；BR-3 → M10 基线断言输入。

### Stage 2：六维深度设计

- **2.1 算法**：纯移动不改写（BR-1）——CC90/81/82/74 保持。**N/A+依据**：无新计算路径设计。
- **2.2 内存**：引擎无自有资源（DB 状态已在 M2 收敛）；Transient 每请求图。**N/A+依据**：无资源持有。
- **2.3 组件化**：选定=`RunDataEngine : ITransient`，构造注入 `(RunSqlCompiler, IRuntimeDataStore)`——白名单内；否决=引擎直注 SqlSugar（白名单断言拦截）；失效条件：出现白名单外合理依赖时须先修架构约束表再修代码（防既成事实）。共享辅助归属：Find References 双引擎交叉核对，共享者入 `RuntimeSharedHelpers`。
- **2.4 健壮性**：异常语义原样（BR-3 登记不治理）；事务语义承接（经 M2 RunInTransactionAsync）；乐观锁行为不变。无新容错设计（行为等价纪律）。
- **2.5 运行时**：全异步承接（原 `.Result/.Wait()=0`）；Transient 无并发状态。
- **2.6 对标**：对标低代码平台 Runtime DataService 分层（执行层独立于编译层）；取舍=只分层不重写。

**2.7 契约骨架**（来源 2.3）：

```csharp
public class RunDataEngine : ITransient
{
    public RunDataEngine(RunSqlCompiler compiler, IRuntimeDataStore dataStore);
    // 20 方法 public 签名自 RunService 原样迁入（F1 清单）
}
```

### Stage 3：红队自检

- **Q1**：删除=RunService 内 20 方法声明。引用核查：IRunService 成员（F3）保留门面委托（BR-2）；模块内直调点改 `_dataEngine.X`（编译验证）。无矛盾。
- **Q2**：无数值参数。不适用。
- **Q3**：「20 方法迁出+逐字未改」前置=移动执行+git diff 审查——可达（纯机械操作）。
- **Q4**：BR-1→DoD git diff 断言；BR-2→DoD 委托保留；BR-3→台账登记步骤；BR-4→Task 4.3。全覆盖。

### Stage 4：手术刀拆分

**4.1 精确修改点**：F1 清单 20 方法 `RunService.cs:615/670/677/878/937/1026/1032/1250/1316/1362/1495/1593/1637/1727/1748/1995/2178/2201/3808/3864` → `Runtime/RunDataEngine.cs`（开工实测行号）。编译预演：骨架→移动→委托改写分步构建。

**4.2 变更影响**：WorkFlow 经 IRunService 消费零感知（委托保留）；模块内调用点编译即验证。

**4.3 验收契约（DoD）**：
- [ ] 20 方法迁出+逐字未改（git diff 仅位置与委托）→ 失败回滚：revert Task 4.2
- [ ] baseline 4 条随迁+CI_BUILD 0 新增 → 失败回滚：补随迁条目
- [ ] CRUD 冒烟四步（建→查→改→删）全 200（`s3-crud-smoke.txt`）+ 快照零 diff → 失败回滚：M2 台账定位回修

监控与可观测性：N/A——行为等价迁移（日志载体=既有日志不变）。

**4.4 矩阵**：➕ `Runtime/RunDataEngine.cs` ➕（若需）`Runtime/RuntimeSharedHelpers.cs` ｜ ✏️ `RunService.cs` ✏️ `complexity-baseline.json` ｜ 🚫 `IRunService.cs` 🚫 特性轨文件面。

**4.5 任务**：Task 4.1 骨架（1h）｜ Task 4.2 二十方法纯移动（⚠探索型 6h）｜ Task 4.3 基线随迁与 CRUD 冒烟（3h）。

**4.6 提问**：无待确认项。

---

## Module M5：列表层

预估总工时：8h ｜ 依赖：M4

### Stage 0：侦察

| 事实 | 精确来源 | 约束的后续阶段 |
|------|---------|---------------|
| F1 列表类 5 方法：GetListResult(168,CC85)/GetRelationFormList(312)/GetHaveTableInfo(418)/GetHaveTableInfoDetails(509)/GetListChildTable(3577) | `RunService.cs` 行号实测 | → Stage 4 移动清单 |
| F2 JNPF.Tests.VisualDev 已有 List*Helpers 测试 10+ 个（列表辅助已预拆分过） | 目录实证 | → BR-3 回归基线（不得破坏） |
| F3 GetListQuerySql(CC140) 已在 M3 迁出，GetListResult 消费其产出 | F1+M3 清单 | → 2.3 依赖方向 M5→M3 |

**0.2 边界**：输入=列表查询请求（分页/过滤/关联）；输出=分页结果集/关联表单列表（消费方：门面→OnlineDev API→前端）。

### Stage 1：业务分析

**1.1**：核心业务问题=把「列表编排」手艺（分页/子表/关联表装配）独立成专职工匠。使用角色=在线开发列表页使用者（分页浏览，最高频读路径）。业务成功标准=列表首页/翻页/条件过滤三形态对外行为不变（冒烟 200）。链路位置=上游：门面 → 本模块 → 下游：M3 编译 SQL + M2 执行。不解决的后果=CC85 列表巨兽继续与写逻辑同居，读写互相牵连。

**1.2**：主流程：接收列表请求 → 编译 SQL（调 M3）→ 执行查询（调 M2）→ 子表/关联表装配 → 分页返回。关键分支：有无子表（GetListChildTable 路径）；关联表单（GetRelationFormList）。业务异常流程：空结果 → 返回空分页结构（存量语义）。终态：分页结果返回。

**1.3**：负责=列表编排装配；不负责=SQL 生成（M3）、数据写入（M4）、数据视图（M6）。分界线=读编排 vs 写路径 / 视图引擎。

**1.4 业务规则**：BR-1 方法体逐字不改（绞杀者）；BR-2 baseline GetListResult 条目随迁值不变；BR-3 既有 List*Helpers 测试全绿（F2，辅助类归属移动时同步 using 引用面）。

**1.5 约束传递**：F2 → 2.3 辅助归属裁决输入；F3 → 2.3 依赖方向（M5→M3→M2 单向无环）。

### Stage 2：六维深度设计

- **2.1 算法**：纯移动不改写。**N/A+依据**：无新计算设计（CC85 保持）。
- **2.2 内存**：Transient 无自有资源。**N/A+依据**：同 M4。
- **2.3 组件化**：选定=`RunListQueryService : ITransient`，构造同 M4 模式（Compiler+DataStore）；辅助归属裁决=仅列表消费的辅助随迁、M4/M5 共享者留 RuntimeSharedHelpers（F2 既有测试引用面为裁决输入）；否决=辅助全量随迁（否决理由：破坏 M4 已定位的共享辅助）。
- **2.4 健壮性**：异常语义原样；空结果边界=存量语义不动。
- **2.5 运行时**：全异步承接；Transient。
- **2.6 对标**：同 M4（Runtime QueryService 分层惯例）。

**2.7 契约骨架**（来源 2.3）：

```csharp
public class RunListQueryService : ITransient
{
    public RunListQueryService(RunSqlCompiler compiler, IRuntimeDataStore dataStore);
    // 5 方法 public 签名原样迁入（F1）
}
```

### Stage 3：红队自检

- **Q1**：删除=RunService 内 5 方法。引用核查：门面委托改写+List*Helpers 测试 using 同步（F2）。无矛盾。
- **Q2**：无数值参数。不适用。
- **Q3**：DoD「三形态冒烟 200」前置=移动完成——可达。
- **Q4**：BR-1→git diff 断言；BR-2→Task 5.1 随迁；BR-3→DoD Helpers 全绿。全覆盖。

### Stage 4：手术刀拆分

**4.3 验收契约（DoD）**：
- [ ] 5 方法迁出+逐字未改；Helpers 测试全绿 → 失败回滚：revert Task 5.1
- [ ] 快照零 diff + 列表三形态冒烟 200（`s4-routes.txt`/`s4-smoke.txt`）→ 失败回滚：M2 台账定位

监控与可观测性：N/A——行为等价迁移。

**4.4 矩阵**：➕ `Runtime/RunListQueryService.cs` ｜ ✏️ `RunService.cs` ✏️ `complexity-baseline.json` ｜ 🚫 `IRunService.cs` 🚫 特性轨文件面。

**4.5 任务**：Task 5.1 列表层纯移动（⚠探索型 6h）｜ Task 5.2 S4 门禁与冒烟（2h）。

**4.6 提问**：无待确认项。

---

## Module M6：视图层与收尾（S4b+S5 阶段聚合）

预估总工时：15h ｜ 依赖：M5

> 命名说明：本模块为 S4b+S5 的**阶段聚合**而非职责合并，内部任务严格原子（命名铁律在任务级执行）；若委员会认定违规模糊，拆为 M6a/M6b 重编号即可（结构不受影响）。

### Stage 0：侦察

| 事实 | 精确来源 | 约束的后续阶段 |
|------|---------|---------------|
| F1 视图类 4 方法：GetDataViewResults(3873)/GetDataViewQuery(4038)/AddDataViewId(4015)/GetPageToDataTable(3998) | `RunService.cs` 行号实测 | → Task 6.1 |
| F2 IRunService 17 成员中 WorkFlow 消费 7 个（M1 F3 实证）；被删 10 成员消费方核验路径已定 | `IRunService.cs`+WorkFlow grep | → Task 6.4 瘦身前置核验 |
| F3 模块内 4 注入点（VisualDevService/VisualDevModelDataService/VisualdevShortLinkService/VisualdevModelAppService）+ 跨模块 ExportImportDataHelper（Common.CodeGen） | CR-20260820-01 §2 | → Task 6.7 CR 门禁 |
| F4 Common.CodeGen 注入点切换属关键业务方法修改面 → 铁律六 CR 审批必经 | 需求分析子链铁律 | → Task 6.7 审批前置 |

**0.2 边界**：输入=数据视图请求/收尾期全模块状态；输出=视图结果/最终门面+瘦接口（消费方：OnlineDev API、WorkFlow）。

### Stage 1：业务分析

**1.1**：核心业务问题=数据视图引擎独立 + 整个重构的收口：把 4157 行大宅改成「小门面+四间专业工坊」并交付验收。使用角色=数据视图使用者（视图查询）+ 重构验收者。业务成功标准=视图查询行为不变；门面 <400 行；IRunService 17→7 且 WorkFlow 零感知；六门禁全绿。链路位置=上游：M1-M5 全部产出 → 本模块 → 下游：交付。不解决的后果=重构半途而废，门面仍是第二个上帝类。

**1.2**：主流程：视图方法迁出 → CodeGen CR 审批与切换 → 消费面核验（第 8 个方法即停手）→ 接口瘦身 17→7 → 门面缩壳 → 豁免位恢复+契约改严 → 六门禁终审。业务异常流程：消费面发现第 8 方法 → 停手上报重评瘦身目标。终态：重构轨交付（独立结论，不等特性轨）。

**1.3**：负责=视图迁移+接口瘦身+门面缩壳+注入点切换+重构终审；不负责=特性翻牌终审（M10 Task 10.4）——v5.2 修订#4：重构/特性终审分离（ADR-C 红线）。

**1.4 业务规则**：

| 编号 | 规则 | 判断条件 | 动作 | 来源 | 优先级 |
|------|------|---------|------|------|--------|
| BR-1 | 消费面核验优先 | grep 发现第 8 个消费方法 | 停手上报，禁自行扩 7 方法清单 | spec §8 契约门禁 | 高 |
| BR-2 | 被删成员不留废弃残骸 | IRunService 删 10 成员 | 直接删除不加 [Obsolete]（消费方全在仓内已核验） | v5.2 设计决策 | 中 |
| BR-3 | 门面行数上限有依据 | <400 行 | 先落测算证据 s5-shell-shrink-estimate.txt，超限审计残留而非放宽指标 | v5.2 修订 | 中 |
| BR-4 | CodeGen 切换无 CR 不动 | cr-approved 标记 | 未批禁触 ExportImportDataHelper | 铁律六/F4 | 高 |
| BR-5 | 重构终审不依赖特性任务 | Task 6.8 依赖集 | 仅 Task 6.4 | v5.2 修订#4 | 高 |

**1.5 约束传递**：BR-1 → Task 6.4 前置步骤；BR-4 → Task 6.7 依赖；BR-5 → Stage 4 任务依赖图；F3 → 2.3 注入点切换策略。

### Stage 2：六维深度设计

- **2.1 算法**：纯移动（视图 4 方法）+无新计算。**N/A+依据**。
- **2.2 内存**：门面缩壳后 RunService 仍 Transient 无自有状态（状态全在 M2）。**N/A+依据**。
- **2.3 组件化**：业务锚点=F3/1.3。选定=门面仅委托（7 IRunService 成员+基础设施方法委托引擎/DataStore）；注入点按消费面切换（单一引擎消费→直注引擎类；混合→保留门面）；ISP 落地=WorkFlow→VisualDev 依赖面 17→7；否决=门面保留业务逻辑「以防万一」（否决理由：缩壳目标是消灭第二个上帝类萌芽）；失效条件：发现基础设施方法无法下沉时单独评审去向。
- **2.4 健壮性**：瘦身后异常语义不变（委托透传）；契约测试改严防签名漂移。
- **2.5 运行时**：DI 语义不变（IRunService 现注册维持 Transient）。
- **2.6 对标**：Facade 模式+ISP 惯例；无独立选型。

**2.7 契约骨架**（来源 2.3；接口修改禁止豁免）：

```csharp
public interface IRunService   // 17→7，保留成员签名不变：
{   // SaveFlowFormData / GetFlowFormDataDetails / SaveDataToDataByFId /
    // GetDbLink / GetVisualDevModelDataConfig / GetCreateSqlByTemplate / GetUpdateSqlByTemplate
}   // 被删成员实现已在引擎类 public（M3-M5 保证）；消费方核验=BR-1
```

### Stage 3：红队自检

- **Q1 删除-引用矛盾**：删除=IRunService 10 成员+RunService 全部迁出方法。引用核查：BR-1 前置核验（第 8 方法停手机制）+编译即验证（WorkFlow 模块编译为 DoD）。无矛盾。
- **Q2 参数自洽**：门面行数上限 400=7 委托×约 5 行+基础设施委托+构造/字段，测算证据 BR-3 落盘。✅
- **Q3 DoD 可达**：「白名单断言含 RunService 且绿」前置=门面零 SqlSugar（状态已在 M2 迁走，委托不引 DB 类型）——可达。
- **Q4 BR 覆盖**：BR-1→Task 6.4 前置；BR-2→Task 6.4 ADR；BR-3→DoD 测算证据；BR-4→Task 6.7 依赖；BR-5→Task 6.8 依赖集。全覆盖。

### Stage 4：手术刀拆分

**4.3 验收契约（DoD）**：
- [ ] 视图 4 方法迁出+视图冒烟 200（s4b 证据）→ 失败回滚：revert Task 6.1
- [ ] CodeGen 切换后快照零 diff+test:api 绿 → 失败回滚：revert Task 6.7（单 commit）
- [ ] 消费面核验记录+IRunService=7 成员+RunService<400 行（测算证据在案）→ 失败回滚：停手上报（BR-1）或审计残留（BR-3）
- [ ] 契约测试改严绿+白名单断言含 RunService 绿 → 失败回滚：定位豁免位/签名问题
- [ ] 重构终审六门禁全绿（sln Debug/Release、CI JNPF009、VisualDev 测试、架构测试、快照零 diff、test:api）+ 开关全 false 冒烟（`s5-final-*.txt`）→ 失败回滚：定位阶段 revert（ADR-R）

监控与可观测性：N/A——行为等价收尾（终审只验不改）。

**4.4 矩阵**：➕ `Runtime/RunDataViewService.cs` ➕ 证据 ｜ ✏️ `RunService.cs` `IRunService.cs` 4 个模块内注入点 `ExportImportDataHelper.cs`（仅 cr-approved 后）契约/架构测试 ｜ 🚫 WorkFlow 模块（只编译验证不改码）🚫 特性轨文件面。

**4.5 任务**：Task 6.1 视图层纯移动（3h）｜ Task 6.2 S4b 门禁与视图冒烟（2h）｜ Task 6.3 CodeGen CR 起草审批（2h）｜ Task 6.7 CodeGen 切换执行（3h，依赖 6.3 批准）｜ Task 6.4 IRunService 瘦身与门面缩壳（4h，依赖 6.7）｜ Task 6.8 重构终审六门禁（3h，**仅依赖 6.4**——BR-5）。

**4.6 提问**：无待确认项。

---

## Module M7：可查询日志

预估总工时：12h ｜ 依赖：M11（挂 S2 窗口，重构段门禁绿后开工）

### Stage 0：侦察

| 事实 | 精确来源 | 约束的后续阶段 |
|------|---------|---------------|
| F1 SerilogBootstrap 文件 sink 仅 error/warning 两路，info 不落盘（仅 Console） | `SerilogBootstrap.cs:36-54` 实证 | → Stage 1 核心缺口定义 |
| F2 TraceIdMiddleware 已注入 TraceId/UserId/TenantId 三元至日志上下文（LogContext） | `TraceIdMiddleware.cs` 实证（专家评估资产表核实） | → Stage 2.1 字段来源；→ TenantId 过滤可行性 |
| F3 UseSerilogRequestLogging 未注册（全仓 0 匹配） | grep 实证 | → Task 7.2 |
| F4 Logging.json Seq.Enabled=false（部署栈不在场） | `Configurations/Logging.json` | → §1.2 砍刀：采集端移出本期 |
| F5 LogDiskGuardService 已有磁盘守卫（5GB 报警/1GB Error-only 降级，5min 轮询） | `Services/LogDiskGuardService.cs:16-78` | → Stage 2.4 磁盘风险第二道兜底 |
| F6 LogHealthCheckService 独立 API 存在但未入 ready 管道 | `Services/LogHealthCheckService.cs` | → 不属本模块（2.7 backlog），不触碰 |
| F7 日志基线体积已采集（f0-log-baseline.txt，M11 产出） | evidence | → Stage 3 Q2 磁盘增量验证 |

**0.2 边界**：输入=全平台日志事件（Serilog 管道）+查询请求（HTTP）；输出=app-*.json 文件+查询响应（消费方：管理员排障）。

### Stage 1：业务分析

**1.1**：核心业务问题=事故之后能查——拿着一个 TraceId，能把这次请求的日志找出来。使用角色=管理员/实施人员（事故排障，低频但关键）。业务成功标准=抽 10 条请求，凭 TraceId 都能查到对应日志（可查率 100%）；跨租户日志互不可见。链路位置=上游：全平台日志事件 → 本模块 → 下游：查询 API→排障人员。不解决的后果=事故只能盯控制台/翻文件碰运气，多租户下还有跨租户泄漏风险。

**1.2**：主流程（写入侧）：日志事件产生 → PII 脱敏 → 全级别写 app-{date}.json（含 TenantId/TraceId）。主流程（查询侧）：查询请求到达 → 权限拦截 → 按时间窗枚举文件（含分片，≤4个=保留期上限）→ 日倒序流式扫描 → 租户过滤+条件过滤 → 命中达 pageSize 即停 → 返回。业务异常流程：功能未开启（开关 false）→ 返回 RESTfulResult 业务失败 code=404 msg=「日志查询功能未启用」（不用 503——避免前端误判服务降级触发重试风暴）。终态：查询命中返回 / 空结果返回。

**1.3**：负责=日志落盘格式化+查询 API；不负责=采集端/看板（F4，运维就绪后 2.5 余留）、磁盘守卫（F5 既有不动）、ready 探针（F6）。数据所有权：app-*.json 由本模块写入，查询侧只读；日志行内 TenantId 为隔离依据。

**1.4 业务规则**：

| 编号 | 规则 | 判断条件 | 动作 | 来源 | 优先级 |
|------|------|---------|------|------|--------|
| BR-1 | 脱敏与 sink 同开关 | QueryableLogging=true | 同一 if 块挂载，禁「只开 sink 不开脱敏」中间态 | PIPL 合规 | 高 |
| BR-2 | 日志行含 TenantId | app-*.json 每行 | OtelJsonFormatter 从 LogContext 取三元（F2），取不到写空串 | v5.2 修订#5 | 高 |
| BR-3 | 查询按 TenantId 过滤 | 非管理员请求 | 行内 TenantId != 当前租户则不可见 | 多租户铁律 | 高 |
| BR-4 | 管理员放行规则 | 无租户上下文 | [待确认:管理员放行语义开工核对 UserManager 后固化] | 阻塞 Task 7.3 实现，不阻塞设计 | 高 |
| BR-5 | 分页上限 | pageSize | 默认 20，>200 截断为 200 | v5.2 修订#6 | 中 |
| BR-6 | 扫描上限 | 文件枚举 | 最多 14 个（保留期上限），命中 pageSize 即停 | v5.2 修订#6 | 中 |
| BR-7 | 磁盘护栏 | 日均增量 > 基线（F7）3 倍 | 翻回开关 false；LogDiskGuard 自动 Error-only 为第二道兜底 | Stage 2.4 风险对策 | 高 |

规则冲突声明：BR-3 vs BR-4：租户过滤与管理员放行冲突时，BR-4 固化规则优先（开工后回填具体语义）。

**1.5 约束传递**：F1 → 1.1 核心缺口；F2 → BR-2/BR-3 可行性；BR-1 → 2.4 代码结构约束；BR-6/BR-5 → 2.1 扫描算法；BR-7 → 2.4 风险对策；F5 → 2.4 兜底链。

### Stage 2：六维深度设计

- **2.1 算法**：业务锚点=1.2 查询侧主流程+BR-5/BR-6。选定=按日倒序逐文件流式扫描（StreamReader+yield，命中 pageSize 即停，无需全局排序——倒序天然满足「新优先」）；否决=全文件载入内存排序（否决理由：50MB×14 文件，内存不可接受）；复杂度=O(扫描行数)，上限受 BR-6 封顶。失效条件：日志量增长致 14 文件扫描普遍 >2s → 重评索引方案（2.5 余留）。
- **2.2 内存**：流式读禁止 ReadAllLines；sink 异步缓冲（Serilog 默认）不阻塞请求线程；文件句柄随滚动释放。单行 JSON 解析对象短生命周期无 LOH 风险（单行远小于 85KB）。
- **2.3 组件化**：选定=PiiDestructuringPolicy（独立类，未来 2.5 桥接 OTel Logs 复用）+ OtelJsonFormatter（独立类，字段契约：Timestamp/Level/TraceId/SpanId/TenantId/UserId/SourceContext/Message/Exception，LogContext 取不到写空串）+ LogQueryService（IDynamicApiController）；否决 CompactJsonFormatter（默认 @t/@l 缩写键，查询侧无法按名解析——v5.2 修订）。ACL：查询侧对文件布局（app-{date}_NNN 分片）的依赖收敛在枚举逻辑单处。
- **2.4 健壮性**：依赖故障=磁盘满→F5 自动降级 Error-only + BR-7 翻回开关；边界防御=损坏 JSON 行跳过不抛；空时间窗→仅扫当天；路径穿越防御=GetFullPath 后前缀校验仅限 LogDir；权限=[SecurityDefine] 拦截（权限点名称开工按 Systems 模块惯例回填）。
- **2.5 运行时**：sink 异步缓冲写（Serilog 默认）；查询 async 文件 IO 不占请求线程；Serilog 配置期 DI 未就绪→开关经 IConfiguration 直读（M11 2.5 同源结论）。
- **2.6 对标**：查询模型取 Seq（过滤表达式+时间窗）的极简子集；砍表达式引擎只留三过滤器（level/traceId/keyword），规避自建查询引擎维护黑洞；切换触发条件：部署栈就绪（2.5）时直接换 Seq，文件格式已对齐 OTel 可平滑导出。

**2.7 契约骨架**（来源 2.3）：

```csharp
public class PiiDestructuringPolicy : IDestructuringPolicy
{
    public bool TryDestructure(object value, ILogEventPropertyValueFactory factory,
        [NotNullWhen(true)] out LogEventPropertyValue? result);
    // 规则：手机号保留前3后4、身份证保留前4后4 中位 ***；
    // 属性名 ∈ {password, secret, token, apikey}（精确词表，禁子串模糊）整体 ***
}

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

// SerilogBootstrap 扩展（开关经 IConfiguration 读——Serilog 配置期 DI 未就绪）：
// if (QueryableLogging) loggerConfig.WriteTo.File(new OtelJsonFormatter(), app-.json, 日滚动, 保留 14, 50MB)
//                                       .Destructure.With<PiiDestructuringPolicy>();
// 启动链：if (QueryableLogging) app.UseSerilogRequestLogging();（TraceIdMiddleware 之后）
```

### Stage 3：红队自检

- **Q1 删除-引用矛盾**：无删除/替换（纯新增 sink+API；原 error/warning 双路不动）。无矛盾。
- **Q2 参数自洽**：磁盘增量验证：app 全级别 vs error/warning 双路，保守估计增量 ≤5 倍；BR-7 回滚阈值 3 倍→存在触发可能，但 F5 自动降级兼兜底，且回滚动作（翻 false）即时生效。✅可接受（已设双兜底）；单文件 50MB×14=700MB 上限封顶。查询 SLO：单文件 10 万行内 P95<500ms；14 文件扫描上限下 <2s。
- **Q3 DoD 可达**：三跳贯通（头→文件→API）前置=F2 三元注入存在（已实证）+formatter 输出 TenantId/TraceId。可达。
- **Q4 BR 覆盖**：BR-1→2.4/代码块同 if；BR-2→2.3 formatter；BR-3→Task 7.3 用例；BR-4→[待确认]阻塞标注；BR-5/BR-6→2.1；BR-7→2.4。全覆盖（BR-4 待确认项不充当已确认事实）。

### Stage 4：手术刀拆分

**4.3 验收契约（DoD）**：
- [ ] 五用例绿（手机/身份证/密码属性/不误伤/嵌套穿透）→ 失败回滚：revert Task 7.1
- [ ] 开关 true：app-{date}.json 含 TraceId 与 TenantId；false：无 app 文件无请求日志行 → 失败回滚：revert Task 7.2
- [ ] 六用例绿（命中/过滤/权限/租户隔离/跨日多文件/分页边界）→ 失败回滚：revert Task 7.3
- [ ] 三跳贯通证据 `f1-traceid-chain.txt` + api/visualdev 快照零 diff（新路由不在该域）→ 失败回滚：定位 enricher/formatter

监控与告警（纯后台必填）：指标：日志可查率（抽 10 条 TraceId 命中率）=100% 为验收口径；查询命中率（有结果/总请求）观察口径；磁盘日均增量 vs F7 基线（BR-7 回滚触发器）。告警：无 Oncall 载体（2.16 砍除）——回滚条件替代告警：可查率 <100% 或磁盘增量 >3 倍 → 翻回 false，诚实登记待 2.16 承接。

**4.4 矩阵**：➕ `Infrastructure/PiiDestructuringPolicy.cs` ➕ `Infrastructure/OtelJsonFormatter.cs` ➕ `Services/LogQueryService.cs` ➕ 测试两文件 ➕ 证据 ｜ ✏️ `SerilogBootstrap.cs` ✏️ 启动链（请求日志门控行）｜ 🚫 重构轨文件面 🚫 `LogHealthCheckService`（F6 不属本模块）。

**4.5 任务**：Task 7.1 PII 脱敏策略（3h）｜ Task 7.2 全级别 sink 与请求日志（3h）｜ Task 7.3 LogQueryService 查询 API（4h）｜ Task 7.4 F1 验证与三跳贯通（2h）。

**4.6 提问**：无待确认项（BR-4 管理员放行语义为开工核对项，非设计决策）。

---

## Module M8：Outbox 可靠性

预估总工时：9h ｜ 依赖：M11（挂 S3 窗口）

### Stage 0：侦察

| 事实 | 精确来源 | 约束的后续阶段 |
|------|---------|---------------|
| F1 EventOutboxMessage 含 RetryCount/MaxRetry=3/DeadLetter + RetryDeadLetterAsync（master-plan 核验表已确认）；GetPendingAsync 只取 Pending、MarkProcessingAsync 后置 Processing | `JNPF.Extras.EventBus.Outbox/` 实证（专家评估核实） | → Stage 3 Q4 schema 核验步骤 |
| F2 无 Sweeper：进程崩在 MarkProcessing 后消息永久滞留 Processing | grep 无回收器实证 | → Stage 1 核心问题 |
| F3 Outbox 表现行建表机制未核 | [待确认:开工定位 Outbox 表初始化代码（CodeFirst/脚本）] | 阻塞 Task 8.1 锁表集成方式，不阻塞设计 |
| F4 Cache.json memory/redis 二选一，不可假设 Redis 在场 | 配置实证 | → 2.6 DB 锁选型 |
| F5 现有重试退避链最长约 60s 级（16s×次数量级） | PollyRetryHandlerExecutor/Outbox 重试实证 | → Stage 3 Q2 误回收阈值验证 |
| F6 Stage5 已有 Outbox 测试先例（SqlSugarClient 内存库） | `JNPF.Tests.Stage5/Program.cs` | → 测试载体 |

**0.2 边界**：输入=定时轮询（30s）+Outbox 表状态；输出=回置 Pending/升死信（消费方：Outbox 调度器重新拣起；业务方=事件最终被处理）。

### Stage 1：业务分析

**1.1**：核心业务问题=用户提交的事件不会无声消失——即使进程在处理中途崩溃，事件也会在 10 分半内被重试或进入可查的死信。使用角色=平台自身（后台自动，30s 周期）；受益方=所有依赖事件的业务（集成/通知）。业务成功标准=卡死消息滞留时长 P99 < 10分30秒；重复回收不产生重复消费（幂等表兜底）。链路位置=上游：Outbox 调度器遗留的 Processing 消息 → 本模块 → 下游：回置 Pending→调度器重试 / 升死信→人工可查。不解决的后果=每次进程崩溃/重启都在积压永远不被处理的幽灵消息。

**1.2**：主流程：30s 轮询到达 → 抢 DB 锁（抢不到→本轮退出）→ 扫描 Processing 超 10 分钟批（≤100 条）→ 逐条：RetryCount<MaxRetry → 回置 Pending+RetryCount+1；否则 → 转 DeadLetter（复用现有死信路径）→ 释放锁。业务异常流程：单条回收失败 → 记日志跳过本条（不断循环）。终态枚举：消息回 Pending（将被重试）/ 升 DeadLetter（可查人工介入）/ 未超时不动。

**1.3**：负责=卡死回收+锁协调；不负责=正常调度（Outbox 调度器）、消费幂等（既有幂等表）、死信重发（RetryDeadLetterAsync 既有）。状态机（承接 F1，本模块新增转换）：

| 状态 | 业务含义 | 可转换至 | 触发条件 | 不可逆？ |
|------|---------|---------|---------|---------|
| Processing | 处理中 | Pending（本模块新增）/ DeadLetter | 超 10 分钟未完结 | 否 |
| Pending | 待处理 | Processing（既有调度器） | 调度器拣起 | 否 |
| DeadLetter | 死信 | —（RetryDeadLetterAsync 人工/既有路径除外） | RetryCount≥MaxRetry | 是（登记） |

**1.4 业务规则**：

| 编号 | 规则 | 判断条件 | 动作 | 来源 | 优先级 |
|------|------|---------|------|------|--------|
| BR-1 | schema 核验优先 | EventOutboxMessage 缺字段 | 停手上报，F2 降级或并入 2.12 前置，禁私自加列 | v5.2 修订#7 | 高 |
| BR-2 | 误回收防线 | 回收阈值 | 10 分钟 >> 最长退避链 60s 级（F5），余量 10 倍 | 风险对策 | 高 |
| BR-3 | 开关 false=服务不存在 | OutboxSweeper=false | 不注册服务（比注册后退出更干净） | ADR-F | 中 |
| BR-4 | 自身不裸奔 | ExecuteAsync 异常 | 全包 try-catch，单轮异常仅记日志不断循环 | F4 治理对象的自我约束 | 高 |
| BR-5 | 不重复接线 | Sweeper 属自治入口 | M10 台账登记不接线 | v5.2 修订（去重） | 中 |

**1.5 约束传递**：F1/BR-1 → Task 8.1 前置核验；BR-2 → Stage 3 Q2 数学验证；F4 → 2.6 选型；BR-4 → 2.4；F3 → Task 8.1 集成方式[待确认]。

### Stage 2：六维深度设计

- **2.1 算法**：业务锚点=1.2 扫描步。选定=单表索引扫描（Processing+UpdateTime 条件，批量上限 100）+乐观并发条件更新（WHERE 旧值匹配）；否决=悲观锁 SELECT FOR UPDATE（否决理由：持有时间不可控+SQL Server 锁升级风险）。复杂度 O(批大小)，量级天然小。
- **2.2 内存**：BackgroundService 进程级单例；批量上限 100 条防单轮内存峰值；无大对象。
- **2.3 组件化**：选定=IOutboxLock 接口+DbOutboxLock 实现（未来 Redis 在场可换实现不改 Sweeper——防腐层）；否决=Redis 锁（F4：不假设 Redis 在场）；失效条件：多实例 >3 且 DB 锁竞争显著时重评 Redis 方案。DI：锁组件随 Outbox 模块注册；Sweeper 开关门控（BR-3）。
- **2.4 健壮性**：依赖故障=DB 不可用→单轮 try-catch 记日志下轮重试（BR-4）；竞态=条件更新失败即放弃本轮（30s 后重试，业务可容忍）；幂等=回收动作幂等（重复回收仅影响行数语义）；锁过期自愈=心跳 60s 自动可抢，无死锁残留。
- **2.5 运行时**：BackgroundService 单实例内单循环（Task.Delay 30s）；多实例靠 DB 锁互斥；无锁内热路径。重试数学验证：本模块自身不重试（轮询即重试形态，30s 间隔）。
- **2.6 对标**：Hangfire JobExpirationTimeout 回收模型 + MassTransit Outbox sweeper 语义；取其「超时回置+重试上限升死信」，砍其独立存储依赖；切换触发条件：弓|入正式消息中间件时重评。

**2.7 契约骨架**（来源 2.3/2.1）：

```csharp
public interface IOutboxLock
{
    Task<bool> TryAcquireAsync(string instanceId, CancellationToken ct = default);
    Task ReleaseAsync(string instanceId, CancellationToken ct = default);
}
public class EventOutboxLock { [SugarColumn(IsPrimaryKey = true)] public string LockKey { get; set; } = "SWEEPER";
    public string InstanceId { get; set; } = ""; public DateTime Heartbeat { get; set; } }
public class OutboxSweeperService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken);
}
// 注册：if (options.OutboxSweeper) services.AddHostedService<OutboxSweeperService>();
```

伪代码（触发条件②：并发/时序依赖）：

```
TryAcquire(instanceId):
    row = 读锁行(无则插入)
    if row.InstanceId == instanceId 或 now - row.Heartbeat > 60s:
        affected = 条件更新(Heartbeat=now, InstanceId=instanceId, WHERE 旧值匹配)
        return affected == 1
    return false
```

### Stage 3：红队自检

- **Q1 删除-引用矛盾**：无删除。无矛盾。
- **Q2 参数自洽**：回收阈值 10min vs 最长退避链 F5（16s×5=80s 级）：10min ≥ 80s×7.5 ✅；心跳 60s vs 单轮执行时长（≤100 条更新，秒级）：60s > 秒级 ✅；轮询 30s+扫描开销可忽略。全部成立。
- **Q3 DoD 可达**：并发用例（双实例仅一方回收）前置=锁实现+虚拟时钟；可达。
- **Q4 BR 覆盖**：BR-1→Task 8.1 DoD；BR-2→Q2；BR-3→注册代码；BR-4→2.4+DoD；BR-5→M10 台账规则。全覆盖。

### Stage 4：手术刀拆分

**4.3 验收契约（DoD）**：
- [ ] `f2-outbox-schema-check.txt`：字段核对+建表机制结论（缺字段即停手上报在案）→ 失败回滚：停止 M8，上报决策
- [ ] 三用例绿（空闲获取/持锁失败/过期抢锁）；锁表与 Outbox 建表机制同源（无机制→SQL 脚本随仓+部署清单登记，不假设 CodeFirst）→ 失败回滚：revert Task 8.1
- [ ] 四用例绿（超时回收/升死信/持锁跳过/双实例并发）→ 失败回滚：revert Task 8.2
- [ ] Stage5 全绿+快照复核零 diff（`f2-sweeper-concurrency.txt`）→ 失败回滚：定位具体用例

监控与告警：指标：消息卡死数（Processing 超 10 分钟存量）——业务口径「用户提交的事件最长 10分30秒内必被重试或入死信可查」；锁连续抢占失败轮次（>10 轮=实例心跳异常）观察口径。告警：卡死数>0 持续 2 轮记 P2 观察项（工单制，无 Oncall 载体——2.16 砍除，诚实登记）。

**4.4 矩阵**：➕ `IOutboxLock.cs` ➕ `DbOutboxLock.cs` ➕ 锁表实体 ➕ `OutboxSweeperService.cs` ➕ Stage5 用例 ➕ schema 核验证据 ｜ ✏️（若机制同源）Outbox 建表初始化处 ✏️ Outbox 模块注册处 ｜ 🚫 重构轨文件面 🚫 `EventOutboxMessage` 实体（禁止加列，BR-1）。

**4.5 任务**：Task 8.1 Outbox DB 互斥锁（3h，含前置 schema 核验）｜ Task 8.2 OutboxSweeperService 回收器（4h）｜ Task 8.3 F2 验证与并发证据（2h）。

**4.6 提问**：无待确认项（F3 建表机制为开工定位项，非设计决策）。

---

## Module M9：出站韧性（LLM/MCP）

预估总工时：12h ｜ 依赖：M11（挂 S4 窗口）

### Stage 0：侦察

| 事实 | 精确来源 | 约束的后续阶段 |
|------|---------|---------------|
| F1 出站 HTTP 调用面五处：LlmGatewayService/HttpMcpTransport/PipelineAttachmentService/SaOrchestratorAdapter/IntegreateEventSubscriber | gap-analysis §0.2 grep 实证 | → 1.3 边界（本期仅 LLM/MCP 两处） |
| F2 NuGet Polly 包全仓 0 引用；自研 PollyRetryHandlerExecutor 仅 EventBus 注册（指数退避 1s→16s+jitter，10 次失败熔断 30s） | master-plan §1 双向修正实证 | → 2.6 选型 |
| F3 LLM 调用为长响应场景（流式/生成），单次慢响应可达数十秒 | LlmGatewayService 调用形态 | → 2.4 超时参数分层 |
| F4 InteAssistant 模块测试载体既有（phase7-eval 验证链） | CLAUDE.md Eval 节 | → 测试载体 |

**0.2 边界**：输入=LlmGatewayService/HttpMcpTransport 的出站 HTTP 调用；输出=带重试/熔断/超时语义的同一调用（成功响应或受控失败）。消费方不变（调用方无感知，除失败语义更明确）。

### Stage 1：业务分析

**1.1**：核心业务问题=AI 助手对话不因上游 LLM 服务的一次瞬时抖动而直接失败——用户的一次提问在合理等待内自动重试，持续故障时快速得到明确报错而非无限挂起。使用角色=租户用户在 Studio 对话场景触发，频率随 AI 功能使用量增长。业务成功标准=单次瞬时故障对用户不可见（重试吸收）；持续故障 90s 内熔断并返回业务可读错误，不再堆积挂起请求。链路位置=上游：Studio 会话请求 → 本模块（出站管道）→ 下游：LLM/MCP 远端。不解决的后果=LLM 供应商任何抖动都直接变成用户可见故障，且挂起请求拖死线程池（专家评估 P0-3）。

**1.2**：主流程：出站调用到达 → 经韧性管道（总超时闸 → 熔断闸 → 单次尝试超时 → 重试计数）→ 发出 HTTP → 成功返回。关键分支：4a 瞬时失败（超时/5xx/网络）且重试配额未尽 → 指数退避后重试；4b 重试耗尽 → 抛受控异常（业务侧见明确错误）；4c 熔断开启 → 快速失败不进网络。业务异常流程：LLM 持续不可用 → 熔断 30s → 半开探测 1 请求 → 恢复或继续熔断；用户看到「AI 服务暂时不可用，请稍后重试」。终态：成功 / 重试耗尽失败 / 熔断快速失败。

**1.3**：负责=出站调用的重试/熔断/超时管道；不负责=业务级重试语义（调用方决策）、流式响应中途断开（本期降级：仅覆盖建连与首响应阶段，流中断按失败上抛）、其余三处出站（Attachment/SaAdapter/EventSubscriber 留 backlog 2.3）。职责分界线=HttpClient 出站边界：管道只包 HttpRequestMessage 往返，不感知业务负载。

**1.4 业务规则**：

| 编号 | 规则 | 判断条件 | 动作 | 来源 | 优先级 |
|------|------|---------|------|------|--------|
| BR-1 | 超时分层 | 单次尝试 45s；总超时 150s | 总超时 > 单次×3 尝试 + 余量，重试永远有配额可用 | v5.2 修订#9 | 高 |
| BR-2 | 只重试幂等安全类故障 | 超时/5xx/网络重置 | 重试；4xx（含 429 之外）不重试直接上抛 | 重试语义纪律 | 高 |
| BR-3 | 熔断防雪崩 | 60s 窗口连续 5 次失败 | 开启 30s，半开放 1 探测请求 | Polly 标准模式 | 高 |
| BR-4 | 开关门控 | OutboundResilience=false | 管道不装载，行为=现状裸调用 | ADR-F | 中 |
| BR-5 | 流式不吞 | SSE/流式响应建连后 | 管道只覆盖首响应，流中断不静默重试（防重复生成） | 降级声明 | 高 |

**1.5 约束传递**：BR-1 → 2.4 参数表与 Stage 3 Q2；F1/1.3 → 2.3 装载点仅两处；BR-3 → 2.4 熔断拓扑；F2 → 2.6 选型（官方包 vs 续用自研）；BR-5 → 2.5 异步边界。

### Stage 2：六维深度设计

- **2.1 算法**：业务锚点=1.2 重试分支。选定=指数退避 2^n 秒基数 + ±20% jitter（与既有自研执行器退避形态一致，运维认知连续），重试次数 3 次尝试封顶；否决=固定间隔（否决理由：多实例同时重试造成惊群）。复杂度 N/A——管道无数据计算。
- **2.2 内存**：ResiliencePipeline 进程级单例（Polly v8 官方推荐，内部状态=熔断计数器）；HttpClient 经 IHttpClientFactory 既有机制，无 per-call new。无大对象。
- **2.3 组件化**：选定=`OutboundResiliencePipelineFactory` 静态工厂产管道 + 装载于 LlmGatewayService/HttpMcpTransport 两处 HttpClient 注册处（AddResilienceHandler，开关门控 BR-4）；否决=逐调用点手工包裹（否决理由：五处调用面未来扩散时必然漏包，装载点在注册处一处收口）；防腐层=管道与业务之间仅暴露标准 Polly v8 类型，无自研中间类型。失效条件：调用面扩至 InteAssistant 之外模块时，升级为全局 HttpClient 管道注册。
- **2.4 健壮性**：容错拓扑：LLM 远端超时/5xx → 重试（3 尝试，退避 2s/4s+jitter）→ 耗尽上抛受控异常；持续失败 → 熔断 30s 快速失败（业务后果=用户见「AI 服务暂时不可用」而非挂起）。**重试数学验证**：单次超时 45s × 3 尝试 = 135s + 退避（2+4=6s）= 141s < 总超时 150s ✅；最坏用户等待 150s 有上限。幂等：BR-2 限定仅对幂等安全故障重试；LLM 生成请求重复提交风险由 BR-5 流式纪律兜底（不静默重试已建连流）。
- **2.5 运行时**：并发模型=异步 IO（HttpClient 原生 async），管道无锁（Polly v8 内部无锁状态机）；熔断状态可见性由 Polly 内部保证。异步边界：重试/熔断全部在 await 链内同步语义推进，无 fire-and-forget（出站调用结果业务必须感知）。
- **2.6 对标**：业界=Polly v8 + Microsoft.Extensions.Http.Resilience（.NET 官方推荐组合，MIT）；对比自研续写：自研执行器已实证可行（F2），但缺标准熔断状态机与 HttpClient 管道集成，续写成本 > 引包。选型=Polly v8 官方包；保留优点=标准重试/熔断/超时策略组合 + AddResilienceHandler 一行装载；规避缺陷=Polly v7 旧 API（直接用 v8 ResiliencePipeline，不碰 v7 Policy API）。切换触发条件：N/A（即成熟方案本身）。

**2.7 契约骨架**（来源 2.3/2.4）：

```csharp
public static class OutboundResiliencePipelineFactory
{
    // 单例管道：超时(150s总) → 熔断(5次/60s, 30s断开) → 重试(3尝试, 指数退避+jitter) → 单次超时(45s)
    public static ResiliencePipeline<HttpResponseMessage> Create();
}
// 装载（两处，开关门控 BR-4）：
// if (options.OutboundResilience) builder.AddResiliencePipeline(OutboundResiliencePipelineFactory.Create());
```

无伪代码——管道行为由 Polly v8 标准策略组合定义，自然语言+参数表无歧义（不满足三条触发条件）。

### Stage 3：红队自检

- **Q1 删除-引用矛盾**：无删除（仅新增管道）。无矛盾。
- **Q2 参数自洽**：单次 45s×3+退避 6s=141s < 总 150s ✅；熔断窗口 60s ≥ 单轮最坏 45s+退避（一次尝试链 51s 内完成）✅；半开探测 1 请求 ≤ 单次超时 45s ✅。全部成立。
- **Q3 DoD 可达**：行为测试前置=工厂可独立实例化（v5.2 修订#8：测试直接 Create() 管道+Mock handler，不依赖装载与开关）→ 先红后绿可达。
- **Q4 BR 覆盖**：BR-1→2.4 参数表；BR-2→2.4 重试谓词；BR-3→2.4 熔断；BR-4→2.3 装载代码；BR-5→降级声明+DoD。全覆盖。

### Stage 4：手术刀拆分

**4.3 验收契约（DoD）**：
- [ ] 管道工厂存在且行为测试先红后绿（重试 3 次/熔断开启/快速失败/总超时截断四用例）→ 失败回滚：revert Task 9.1/9.2
- [ ] 两处装载点生效且开关 false 时管道不在链（行为=现状）→ 失败回滚：revert Task 9.3
- [ ] 指标已产生并注册（重试计数/熔断状态，MeterListener 单测捕获；展示待 2.16，诚实登记）+ LLM/MCP 出站冒烟 200 → 失败回滚：定位具体用例

监控与告警：指标：重试次数（按调用点）——业务口径「AI 提问的瞬时故障被自动吸收，用户无感」；熔断开启事件——业务口径「AI 服务持续故障时快速报错而非挂死」。告警：无 Oncall 载体（2.16 砍除），熔断开启记 P2 观察项工单。

**4.4 矩阵**：➕ `OutboundResiliencePipelineFactory.cs` ➕ 行为测试（InteAssistant 测试载体）｜ ✏️ LlmGatewayService HttpClient 注册处 ✏️ HttpMcpTransport HttpClient 注册处 ✏️ InteAssistant csproj（Polly v8 包引用）｜ 🚫 重构轨文件面 🚫 其余三处出站调用（Attachment/SaAdapter/EventSubscriber——backlog 2.3，本期禁触）。

**4.5 任务**：Task 9.1 管道工厂+失败测试先行（3h）｜ Task 9.2 行为测试转绿（4h）｜ Task 9.3 两处装载与开关门控（3h）｜ Task 9.4 F3 门禁与指标注册证据（2h）。

**4.6 提问**：无待确认项。

---

## Module M10：异常边界（非 HTTP 入口降级版）

预估总工时：9h ｜ 依赖：M11（挂 S5 窗口，与 M6 Task 6.3 并行）

### Stage 0：侦察

| 事实 | 精确来源 | 约束的后续阶段 |
|------|---------|---------------|
| F1 `IExceptionHandler` 全仓 0 实现；`LogExceptionHandler.OnExceptionAsync(ExceptionContext)` 仅 MVC 管道 | `LogExceptionHandler.cs` 实证（专家评估核验） | → 1.1 核心缺口 |
| F2 SysLogEntity 异常记录 `Json = Message + "\n" + StackTrace` 单字段平铺 | `LogExceptionHandler.cs:61` 实证 | → 2.1 结构化格式 |
| F3 非 HTTP 入口清单未落盘（BackgroundService/IHostedService/IEventHandlerExecutor/SSE/WebSocket 管道） | 待 Task 10.1 grep 采集 | → Task 10.1 台账 |
| F4 OutboxSweeperService 已内置 try-catch 自治（M8 BR-4） | M8 设计 | → 台账登记不接线 |
| F5 重构轨引擎存量裸 throw 纯移动保留（方法体不改纪律） | A+C 绞杀者纪律 | → 2.4 断言口径 |

**0.2 边界**：输入=非 HTTP 入口的未捕获异常；输出=结构化异常记录（SysLog Json 字段内）+ 边界捕获指标。消费方=事故排查人员（可查询）。

### Stage 1：业务分析

**1.1**：核心业务问题=后台任务/事件处理/SSE/WebSocket 崩溃时不再是无声消失——每次故障都有结构化记录（异常类型/链/入口），事故后 10 分钟内能定位「什么入口、什么异常、内层原因」。使用角色=运维/开发排查人员，触发频率=异常时被动。业务成功标准=非 HTTP 入口异常 100% 有结构化记录可查（抽样验证）；不重建 Oops 契约（HTTP 面既有行为不变）。链路位置=上游：各非 HTTP 入口的未捕获异常 → 本模块 → 下游：SysLog 记录+指标。不解决的后果=每次后台故障只能翻进程日志猜，多实例下直接不可查。

**1.2**：主流程：入口异常抛出 → 边界包装器捕获 → 结构化组装（type/code/innerChain/入口标识）→ 写 SysLog（Json 字段内）+ 指标计数 → 按入口类型决定后续（后台任务记日志继续/事件交 Outbox 既有重试）。业务异常流程：写 SysLog 本身失败 → 降级 Console 错误日志（不吞异常信息）。终态：已记录 / 降级记录（写库失败时）。

**1.3**：负责=非 HTTP 入口的统一异常捕获与结构化记录；不负责=HTTP 管道异常（LogExceptionHandler 既有，本期不动）、业务异常语义（Oops.Bah/Oops.Oh 契约不变）、存量裸 throw 治理（登记技术债，v5.2 修订#3 拍板后者）。职责分界线=入口包装点：边界只包入口方法的最外层，不下沉到业务方法内。数据所有权：SysLog 记录由本模块创建，只读可查，禁止修改。

**1.4 业务规则**：

| 编号 | 规则 | 判断条件 | 动作 | 来源 | 优先级 |
|------|------|---------|------|------|--------|
| BR-1 | 降级不动 schema | SysLogEntity 表结构 | 结构化写入 Json 字段内（加列依赖 2.12 迁移能力，当前缺失） | Phase 1 砍刀 | 高 |
| BR-2 | 不重建契约 | HTTP 面 Oops 行为 | 零变更（不碰 LogExceptionHandler） | 降级声明 | 高 |
| BR-3 | 存量不阻塞 | 引擎存量裸 throw | 登记技术债台账，本期不治理不阻塞 | v5.2 修订#3 | 中 |
| BR-4 | 新增抛出面受控 | 本期新增代码的抛出面 | 必须走 IExceptionBoundary；架构断言守护 | v5.2 修订#3 | 高 |
| BR-5 | 自治不重复接线 | OutboxSweeperService | 台账登记自治，不重复接入（M8 BR-5） | 去重 | 中 |
| BR-6 | 开关 false=不接线 | ExceptionBoundary=false | 入口保持现状裸奔（行为不变） | ADR-F | 中 |

**1.5 约束传递**：BR-1 → 2.1 格式设计（Json 内结构化）；F1/1.3 → 2.3 包装点选型；BR-4 → 2.3 架构断言载体；F5 → 2.4 断言口径；BR-6 → 2.5 装载方式。

### Stage 2：六维深度设计

- **2.1 算法**：业务锚点=1.2 结构化组装。选定=Json 字段内写结构化对象：`{type, code, message, innerChain:[{type,message}], entry}`（innerChain 展平 AggregateException/嵌套 InnerException，深度上限 5）；否决=表加列（BR-1：无迁移能力承载）与纯文本堆叠（现状缺陷本身）。格式稳定性：键名固定词表，查询 API（M7）可按 type/entry 过滤。
- **2.2 内存**：异常路径非热路径，无池化需求；innerChain 深度上限 5 防深嵌套异常链膨胀。
- **2.3 组件化**：选定=`IExceptionBoundary` 接口（CaptureAsync(Exception, EntryContext)）+ `SysLogExceptionBoundary` 实现；装载=入口包装器（BackgroundService/事件执行器/SSE/WebSocket 管道入口最外层，开关门控 BR-6）；断言载体=`JNPF.Tests.Common/EngineThrowSiteBaselineTests.cs`（**特性轨测试项目**，v5.2 修订#2：禁触重构轨架构测试文件）；断言口径=**新增抛出面必须走 IExceptionBoundary**，存量裸 throw 不在断言范围（BR-3 台账登记）；否决=全局中间件钩子（否决理由：非 HTTP 入口无统一管道，只能逐入口包装）。失效条件：入口类型 >6 种且包装样板显著重复时，抽包装基类。
- **2.4 健壮性**：依赖故障=写 SysLog DB 失败 → 降级 Console 错误日志（异常信息不丢）；边界自身永不抛（最外层 try-catch 包裹写入逻辑）；指标=边界捕获次数（按入口类型），MeterListener 单测验证「已产生并注册」（展示待 2.16）。存量裸 throw（F5）：纯移动保留，台账登记，不阻塞本期门禁。
- **2.5 运行时**：包装器在入口线程内同步推进（异常路径无并发新语义）；事件执行器包装不影响 Outbox 重试链（异常上抛后 Outbox 语义照旧）。无锁无热路径。
- **2.6 对标**：业界=ASP.NET IExceptionHandler（仅 HTTP）+ Serilog 异常结构化（@x 字段）；取其结构化思想，因平台多入口自研包装点（业界无现成「非 HTTP 统一边界」包）。切换触发条件：引入正式 APM/OTel Collector 后，异常记录切换到 Trace Span 载体（2.14 余留承接）。

**2.7 契约骨架**（来源 2.3/2.1）：

```csharp
public interface IExceptionBoundary
{
    // entry 标识入口类型与名称（如 "HostedService:OutboxSweeperService"）
    Task CaptureAsync(Exception exception, string entry, CancellationToken ct = default);
}
public class SysLogExceptionBoundary : IExceptionBoundary
{
    // Json 字段内结构化：{type, code, message, innerChain:[{type,message}], entry}（深度上限 5）
    public async Task CaptureAsync(Exception exception, string entry, CancellationToken ct = default);
}
```

无伪代码——组装逻辑顺序执行无分支歧义（不满足三条触发条件）。

### Stage 3：红队自检

- **Q1 删除-引用矛盾**：无删除。无矛盾。
- **Q2 参数自洽**：innerChain 深度 5 ≥ 常见嵌套（AggregateException 两层+业务一层）✅；指标注册无参数风险。成立。
- **Q3 DoD 可达**：抽样验证（人造非 HTTP 入口异常→SysLog 可查）前置=包装器+测试 HostedService 桩；可达。存量裸 throw 不在断言范围（BR-3）→ 门禁不会因存量红。
- **Q4 BR 覆盖**：BR-1→2.1；BR-2→1.3 不负责；BR-3→2.4 台账；BR-4→2.3 断言；BR-5→台账规则；BR-6→2.3 装载代码。全覆盖。

### Stage 4：手术刀拆分

**4.3 验收契约（DoD）**：
- [ ] 非 HTTP 入口台账落盘（含 OutboxSweeperService 自治标注 + 存量裸 throw 技术债登记）→ 失败回滚：N/A（纯文档）
- [ ] IExceptionBoundary 契约+失败测试先行 → 实现转绿；接线后抽样验证：人造 HostedService 异常 → SysLog Json 结构化可查（type/entry/innerChain 字段在）→ 失败回滚：revert Task 10.2
- [ ] EngineThrowSiteBaselineTests 绿（新增抛出面受控；存量豁免口径在断言注释中显式声明）+ 开关 false 行为=现状 + 指标已产生并注册（MeterListener 捕获）→ 失败回滚：定位具体用例

监控与告警：指标：边界捕获次数（按入口类型）——业务口径「后台故障有记录可查而非无声崩溃」。告警：N/A（无采集端，2.16 砍除，观察口径登记）。

**4.4 矩阵**：➕ `IExceptionBoundary.cs` ➕ `SysLogExceptionBoundary.cs` ➕ 入口包装器 ➕ `JNPF.Tests.Common/EngineThrowSiteBaselineTests.cs` ➕ 台账文档 ｜ ✏️ 接线点入口最外层（开关门控）✏️ Common 模块注册处 ｜ 🚫 重构轨全部文件（含 RunEngineSqlSugarBoundaryTests）🚫 `LogExceptionHandler.cs`（BR-2）🚫 `SysLogEntity` 表结构（BR-1）。

**4.5 任务**：Task 10.1 入口台账与契约失败测试先行（3h）｜ Task 10.2 实现与入口接线（4h）｜ Task 10.3 F4 门禁与特性终审冒烟（2h，依赖 M6 Task 6.4 完成：四开关全 true 全链冒烟，特性轨终审独立于重构轨终审）。

**4.6 提问**：无待确认项。

---

# 第五部分：Phase 4 — 工程基线检查（五件套）

## §E1 架构决策记录（ADR）索引

| 决策 | 层级 | 位置 | 结论 |
|------|------|------|------|
| ADR-C 混入形态 | 全局 | Phase 2 §2.1 | 方案 C 单轨穿插+双门禁分段裁决；铁律：特性门禁红不阻塞重构门禁 |
| ADR-R+F 回滚轴分工 | 全局 | Phase 2 §2.2 | 重构=阶段级 git revert（快照零 diff 即逃生舱）；特性=四布尔开关 RuntimeFoundation.{ExceptionBoundary, OutboxSweeper, OutboundResilience, QueryableLogging} |
| IRuntimeDataStore 抽象选型 | 模块 | M2 → 2.3 | 接口+SqlSugar 实现；否决恢复临时字段 |
| Queryable 豁免废除 | 模块 | M2 → 2.4 | 27 处全改写；不可等价者经扩展成员承载（M 系列台账） |
| Outbox 锁选型 | 模块 | M8 → 2.3 | DB 单行锁+心跳；否决 Redis（不假设在场） |
| 出站韧性选型 | 模块 | M9 → 2.6 | Polly v8 官方包；否决续写自研执行器 |
| 异常边界断言口径 | 模块 | M10 → 2.3 | 新增抛出面受控；存量登记不阻塞 |
| 终审拆分 | 全局 | M6 Task 6.8 / M10 Task 10.3 | 重构终审仅依赖重构轨；特性终审独立（v5.2 修订#4） |

无 Phase 2 与模块均未覆盖的独立决策。

## §E2 安全与合规检查表

| 检查项 | 具体内容 | 涉及模块 |
|--------|---------|---------|
| 认证与授权 | M7 日志查询 API：权限点拦截+租户过滤（TenantId enricher 字段来源）；跨租户不可见；无租户上下文的管理员放行规则 [待确认] | M7 |
| 敏感数据合规 | PIPL：PII 脱敏策略与日志面扩大同批交付（手机号前3后4/身份证前4后4/密码属性词表整体 ***）；M10 异常上下文禁入栈变量值 | M7、M10 |
| 注入防御 | IRuntimeDataStore 全参数化（L0 硬门控既有）；SqlQueryable 改写不得引入字符串拼接；M7 查询 API 文件路径白名单（防路径穿越） | M2、M7 |
| 新增依赖漏洞 | Polly v8（Microsoft.Extensions.Http.Resilience，MIT）→ 引入时 NuGet 漏洞扫描状态记录在 Task 9.1 交付物 | M9 |

## §E3 数据迁移与灰度发布策略

- **API 向后兼容**：路由快照零 diff 即兼容性证明（S0 基线 vs 每阶段快照，harness `--mode routes`）；IRunService 17→7 仅在 S5 终审一次切换，委托方三处 Name/Route 契约测试守护。
- **DB 变更与过渡方案**：本期零 schema 变更——M10 降级 Json 内结构化（BR-1）、M8 禁私自加列（BR-1）、M8 锁表随 Outbox 现行建表机制同源（无机制→SQL 脚本随仓+部署清单登记，不假设 CodeFirst）。
- **灰度发布与流量调度**：特性四开关 S5 终审按序翻牌（ExceptionBoundary → OutboxSweeper → OutboundResilience → QueryableLogging，每翻一位冒烟一次）；重构轨无运行时开关（纯移动，回滚=git revert）。放量依据=开关位，观察口径=每翻一位后全链冒烟+快照复核。

## §E4 风险登记簿

| 风险 | 来源模块 | 概率 | 影响 | 回滚路径 |
|------|---------|------|------|---------|
| Queryable→SqlQueryable 改写 SQL 不等价（风险7） | M2 | 中 | 运行时查询行为变化 | 逐处 ToSql 比对台账；不等价处经扩展成员承载 |
| 混流污染：冒烟红无法归因 | 全局 ADR-C | 中 | 重构安全网失效 | 双门禁分段裁决；特性红不阻塞重构，独立定位 |
| Outbox 误回收活体消息 | M8 | 低 | 事件重复消费 | 阈值 10min >> 退避链 80s（10 倍余量）；幂等表兜底 |
| LLM 重试配额被总超时吃光 | M9 | 低 | 重试形同虚设 | 参数分层已验证（141s < 150s）；失效时调单次超时 |
| CodeGen 注入点切换破坏导出导入 | M6 Task 6.7 | 中 | 导出导入链路断裂 | CR 门禁先行；既有 46 绿导入导出安全网测试 | 
| 开关翻牌后行为异常 | M11 | 低 | 特性面故障 | 单开关置 false 精确熔断（四特性级粒度） |

## §E5 测试策略与 SLO 总览

- **单测覆盖矩阵**：RunSqlCompiler 特征单测（特征捕获不手写）· RunServiceContractTests 17 成员签名冻结 · WorkFlow 7 方法 nameof 守护 · VisualDevRouteOwnerTests 三委托方契约 · RunEngineSqlSugarBoundaryTests（零 SqlSugar+构造白名单）· EngineThrowSiteBaselineTests（特性轨）· PiiDestructuringPolicyTests 五用例 · DbOutboxLock 三用例 · Sweeper 四用例 · 韧性管道四用例 · MeterListener 指标注册用例。
- **集成/E2E 断言点**：路由快照零 diff（每阶段门禁）· CRUD 全链路冒烟 · 外部数据源活体冒烟 · 四开关全 true 全链冒烟（特性终审）· Stage5 回归。
- **核心 SLO（业务口径）**：日志可查率 100%（抽 10 请求 TraceId 命中）；卡死消息滞留 P99 < 10分30秒；LLM 瞬时故障用户无感；后台故障 100% 结构化可查。
- **并发/性能测试**：M8 双实例并发回收用例（虚拟时钟）；无专项压测（本期无热路径变更，诚实声明）。

---

# 第六部分：Phase 5 — 持续校准与自检闭环

## §5.1 模块依赖图与工时

```
重构轨：M1（安全网）→ M3（编译层）→ M2（数据访问抽象）→ M4（执行层）→ M5（列表层）→ M6（视图层与收尾）
特性轨：M11（开关基建，先行）→ M7（挂 S2）/ M8（挂 S3）/ M9（挂 S4）/ M10（挂 S5，与 M6 并行）
交叉点：每阶段窗口重构段门禁绿后开工同窗特性模块；M10 Task 10.3 依赖 M6 Task 6.4
```

| 模块 | 轨道 | 工时 | 累计 |
|------|------|------|------|
| M11 特性开关基建 | 特性 | 4h | 4h |
| M1 安全网 | 重构 | 7h | 11h |
| M3 编译层 | 重构 | 16h | 27h |
| M2 数据访问抽象 | 重构 | 23h | 50h |
| M7 可查询日志 | 特性 | 12h | 62h |
| M4 执行层 | 重构 | 10h | 72h |
| M8 Outbox 可靠性 | 特性 | 9h | 81h |
| M5 列表层 | 重构 | 8h | 89h |
| M9 出站韧性 | 特性 | 12h | 101h |
| M6 视图层与收尾 | 重构 | 15h | 116h |
| M10 异常边界 | 特性 | 9h | 125h |

**总计：125h ≈ 15.6 人日**（重构轨 79h + 特性轨 46h，与 §1.4 口径一致）。

## §5.2 实施校准钩子

| 校准项 | 验证方式 | 触发时机 |
|--------|---------|---------|
| 纯移动未变行为 | 路由快照逐阶段零 diff | 每阶段门禁 |
| SQL 等价性（风险7） | 逐处 ToSql 前后比对台账 | M2 Task 2.5 完成 |
| IRuntimeDataStore 漏斗完整性 | 架构测试（零 SqlSugar+构造白名单） | M2 完成后常驻 |
| 误回收防线 | 双实例并发用例+阈值数学验证 | M8 Task 8.3 |
| 重试参数有效性 | 管道行为四用例 | M9 Task 9.2 |
| 开关精确熔断 | 逐位翻牌+单开关 false 冒烟 | S5 终审 |

## §5.3 自检清单

| # | 检查项 | 验证方式 | 结果 |
|---|--------|---------|------|
| 1 | 原子性 | 全文搜索模块/任务名 | ✅ 无「和/以及/同时/及」连接词（M6 名称「视图层与收尾」为 S4b+S5 阶段聚合标注，含两个子阶段但任务层已拆开——登记为模板例外，理由：阶段聚合非职责连接） |
| 2 | 工时约束 | 逐任务核对 | ✅ 超 4h 项均已标 ⚠ 探索型（M2 Task 2.4/2.5、M5 Task 5.1 等，输出物在案） |
| 3 | 契约完整性 | 逐模块 2.7 核对 | ✅ IRuntimeDataStore/IOutboxLock/管道工厂/IExceptionBoundary/PII 策略签名均在；纯移动模块豁免声明在案 |
| 4 | 豁免合理性 | 核对豁免模块 | ✅ M1/M3/M4/M5 纯移动豁免：不改签名不改结构（四条全满足） |
| 5 | 矩阵完整性 | 逐模块 4.4 | ✅ 每模块至少一条新增或修改 |
| 6 | 矩阵隔离性 | 🚫 栏交叉核对 | ✅ 特性轨各模块均列重构轨为禁触；M10 额外禁触重构轨架构测试文件（v5.2 修订#2） |
| 7 | 伪代码合规 | 逐模块核对触发条件 | ✅ 仅 M8 锁竞争伪代码（并发/时序触发）；M9/M10 显式声明不满足触发条件 |
| 8 | 无模糊词 | 全文搜索：适当/合理/酌情/相关/根据需要 | ✅ 无命中（「合理等待」出现于 M9 1.1 业务描述，非约束条款，保留） |
| 9 | 边界连续 | 依赖模块对类型核对 | ✅ IRuntimeDataStore/IRunService/管道工厂类型在上下游一致 |
| 10 | 可测试性 | 每个 DoD 拟测试名 | ✅ §E5 矩阵已映射 |
| 11 | 五件套底线 | Phase 4 逐项 | ✅ 五项全实质 |
| 12 | 业务分析完整性 | 每模块 Stage 1 | ✅ 十一模块 1.1-1.5 全实；1.5 约束传递表均非空 |
| 13 | 六维完整性 | 每模块 Stage 2 | ✅ 十一模块六维均有思考痕迹；纯移动模块六维以「移动纪律+边界守护」为实质内容 |
| 14 | 认知闭环连贯 | Stage 0→4 引用链 | ✅ 每模块 F 编号→Stage 1/2 绑定、BR 编号→2.x 承接均可溯 |
| 15 | 红队实质 | Stage 3 四连问 | ✅ 均含具体检查过程（M8 Q2 数学验证/M2 Q1 删除引用核查等），无「无问题」一笔带过 |
| 16 | 推理链完整 | 每个设计决策 | ✅ 选定+否决+失效条件三要素（2.3/2.6 选型处） |
| 17 | 隐形开发防护 | 纯后台模块 | ✅ M7/M8/M9/M10 均有监控指标（业务口径）+告警处置（含 2.16 砍除的诚实登记） |
| 18 | 砍刀真实性 | Phase 1 §1.2 | ✅ 仅针对已提出需求及隐含影响（PII 脱敏/开关基建标注为隐含影响识别） |
| 19 | 跳过合理性 | 「本场景不适用」标注 | ✅ 无滥用（M9 2.1 复杂度 N/A 附理由） |
| 20 | 检查点完整 | 各 Phase | ✅ Phase 1 用户已确认；Phase 2 已拍板；Phase 3 批量豁免在案；Phase 4-5 本轮交付 |

## §5.4 决策回溯机制

实施中遇到问题时，回溯 Phase 2 决策与对应模块 Stage 2 选型，引用具体失效条件判断：
- 特性门禁反复红且定位成本失控 → 回溯 ADR-C，评估特性轨是否整体后移出本期；
- Queryable 改写不可等价处 >5 → 回溯 M2 2.4 豁免废除决策，评估 IRuntimeDataStore 扩展面是否需重新设计；
- LLM 流式场景重试引发重复生成 → 回溯 M9 BR-5，评估流式降级口径是否需收紧；
- 多实例 >3 且 DB 锁竞争显著 → 回溯 M8 2.3 失效条件，重评 Redis 锁。

---

**模板版本：v6.0** ｜ 设计哲学：问题定义先于解决方案，业务分析先于技术设计，推理链先于结论，边界先于中心。
