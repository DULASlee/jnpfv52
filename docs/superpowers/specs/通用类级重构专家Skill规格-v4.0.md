# 通用类级重构专家 Skill 规格 v4.0 — Evidence-Driven Generic Class Refactoring Expert

> **版本**：v4.0｜**日期**：2026-08-27｜**状态**：待审核（由 v3.0 审核意见升级）  
> **上位**：《MASTER-JNPF后端重构与Aspire微服务化总体设计规格》《L2-类级螺旋专家级重构方案-v2.0》《JNPF后端类级代码审计扫描清单-v1.1》《JNPF后端类级代码审计扫描设计规格-v1.0》  
> **性质**：通用 .NET 类级重构专家 Skill 的**规格（Spec）**，非执行手册。上位与本规格冲突时以上位为准。  
> **适用**：JNPF / ASP.NET Core / 通用 .NET Framework 的 Repository / Service / Domain Service / Infrastructure / Middleware / Worker / SDK / 高并发组件。  
> **前置审核**：本规格基于 18 节专家审核意见对 v3.0 的“L1-L2 类级重构”能力进行结构性升级，核心变化一句话：**从“十大深水区技术清单”升级为“证据驱动的类级专家诊断与重构决策系统”**。

---

## 0. 审核结论（对 v3.0 的定性）

| 维度 | 评分 | 结论 |
|------|------|------|
| 重构深度 | 9.0 | 已建立生命周期→内存→异步→并发→异常→热路径→类型→扩展→可观测→依赖的完整审计视野 |
| .NET 技术覆盖 | 8.5 | 覆盖 Span/Memory/ArrayPool/ValueTask/ActivitySource 等，但部分示例需纠正 |
| 性能意识 | 9.0 | 要求 GC/异步/并发/Benchmark 交付物，方向正确 |
| 可观测性 | 9.0 | Activity/Metrics/结构化日志纳入标准检查，正确 |
| 架构意识 | 8.5 | 已提出 Architecture Test 自动化约束，正确 |
| 可量化意识 | 8.5 | 要求量化验证，正确 |
| 实际工程安全性 | 6.5 | 存在过度优化、错误生命周期、技术误用风险（待本规格纠正） |
| AI Agent 可执行性 | 6.5 | 缺少 P0 证据门槛、风险分级、复杂度预算，Agent 易失控 |
| 通用 Skill 潜力 | 9.0 | 已具备 Class-Level Deep Refactoring Framework 雏形 |
| 当前直接作为执行规范 | 7.0 | **不能直接执行**，需升级为 v4.0 后方可作为执行规范 |

**最大问题**：不是“不够深”，而是“深过头”——把技术手段本身当成重构目标。  
**v4.0 唯一总原则**（宪法级）：

> **任何高级重构技术都必须经过“问题证据 → 根因 → 方案 → 风险 → 验证 → 收益”闭环，禁止为了使用高级技术而使用高级技术。**

---

## 1. 定位与三层原则

### 1.1 定位

```
Generic-Class-Refactoring-Expert（通用类级重构专家）
  = 通用 .NET 类级重构 Best Practice
    + 证据驱动决策
    + 风险分级执行
    + JNPF 业务适配（后置）
```

### 1.2 三层原则（与 JNPF 总路线一致）

```
Generic .NET Best Practice
        ↓
Repository / Service / Domain / Infrastructure（模块内三段式 .Interfaces → .Entitys → Service）
        ↓
JNPF-specific behavior（多租户/R12 三元组/IDynamicApiController/Oops/SqlSugar/动态表单）
```

**禁令**：禁止为 JNPF 虚构一套专用抽象；禁止为迁移而迁移；禁止在 L2 引入 Outbox/消息队列/Saga（见 L2 v2.0 §5）。

### 1.3 十六项核心能力（v4.0 能力清单）

```
1.  Class Responsibility Analysis
2.  Dependency & Boundary Analysis
3.  Lifetime / Ownership Analysis
4.  Memory / GC Analysis
5.  Async Model Analysis
6.  Concurrency Analysis
7.  Exception Semantics Analysis
8.  Hot Path Performance Analysis (Toolkit, 非 Default)
9.  Type System / Modern C# Semantic Analysis
10. Extensibility / Complexity Budget Analysis
11. Observability Analysis (Trace+Metrics+Logs+Privacy+Cardinality+Cost)
12. Architecture / Dependency Direction Analysis
13. Testability Analysis
14. Evidence-based Benchmarking
15. Risk-based Refactoring Planning
16. Regression Verification
```

---

## 2. 总体决策流程（v4.0 核心升级：从清单到系统）

**不是**：`P1..P10 = 十个必须做的任务`  
**而是**：

```
              ┌──────────────────────────┐
              │ P0 Evidence Discovery    │ ← 强制先行，不可跳过
              └────────────┬─────────────┘
                           ↓
              ┌──────────────────────────┐
              │ Risk / Impact Matrix     │ ← 分级，决定哪些 P 进场
              └────────────┬─────────────┘
                           ↓
            ┌──────────────┴──────────────┐
            ↓                             ↓
    Structural                     Runtime / Performance
    Refactoring                    Refactoring (Toolkit)
            │                             │
            └──────────────┬──────────────┘
                           ↓
            ┌──────────────────────────────┐
            │ P1–P10 Expert Toolkit        │ ← 按需选用，非全量
            └──────────────┬───────────────┘
                           ↓
                   Test / Benchmark
                           ↓
                 Observability Verify
                           ↓
                  Architecture Verify
                           ↓
                     Regression
                           ↓
                    Final Report
```

**门控语义**：

- **P0 未完成 → P1..P10 不得启动**（L11 硬拦：文档、风险表、Benchmark 基线缺失则阻断业务代码写入）。
- **单一高级技术（Span/ValueTask/ArrayPool/池化/SourceGenerator/Expression/SIMD）启动 → Performance Change Gate 未通过则阻断**。

---

## 3. P0 — Evidence & Risk Assessment（新增，宪法级）

> **不要先改。先证明。**

### 3.1 P0.1 代码事实（静态）

| 项 | 采集内容 | 工具 |
|----|----------|------|
| 规模 | 类行数/方法数/字段数/圈复杂度（JNPF009 同源） | Roslyn Analyzers + 扫描清单 I/L/H |
| 依赖 | 依赖数/依赖方向/循环依赖/模块边界违规数 | arch-module-dependency-scan.ps1 + Roslyn |
| 生命周期 | DI 注册（Singleton/Scoped/Transient）、IDisposable/IAsyncDisposable、Factory 持有关系 | 扫描清单 A + DI 注册表 |
| 线程模型 | 是否被多线程访问、静态可变状态、锁对象 | 扫描清单 D |
| 调用方 | 直接调用方数 / 间接影响面（blast radius） | Serena / CodeGraph `find_referencing_symbols` |

**产出**：`P0-Code-Facts.md`（含量化表，非文字描述）。

### 3.2 P0.2 运行时事实（动态，必选其二以上）

| 项 | 采集内容 | 工具 |
|----|----------|------|
| CPU/Memory/Allocation | 热点方法 CPU%、分配量、LOH | dotnet-counters / dotnet-trace / dotnet-gcmon |
| GC | Gen0/1/2 频率、暂停、LOH 分配 | dotnet-counters `gc-heap` |
| ThreadPool | 队列长度、starvation、延迟 | dotnet-counters `threadpool` + 压测 |
| Latency | P50/P95/P99、吞吐 | BenchmarkDotNet / k6 / 压测 |
| Exceptions | 异常率、吞没点 | 日志 + 扫描清单 E |
| DB/I/O | 慢查询、N+1、事务范围 | SqlSugar ToSql + 日志 + 扫描清单 O/F |

**禁令**：无运行时数据 → 任何 Span/ArrayPool/ValueTask/池化 优化**不得启动**。

### 3.3 P0.3 架构事实

- 依赖方向是否符合洋葱+模块切片（.Interfaces → .Entitys → Service，禁止反向）；
- 循环依赖清单；
- 生命周期与边界职责是否一致（见 L2 v2.0 §0 物理边界）。

### 3.4 P0.4 测试事实

| 项 | 阈值 |
|----|------|
| 行为特征考卷（T0.3） | ≥30 条核心 API 快照，含登录/用户/字典/菜单/一条表单流/一条审批流 |
| 单测/集成/并发/契约/回归 | 按聚合归集，L2 每个聚合新增 ≥3 领域规则单测 |
| Benchmark | 热路径优化项必须附基线 vs 优化后对比 |

### 3.5 P0.5 风险等级（决定执行深度）

| 等级 | 定义 | 后续动作 |
|------|------|----------|
| **Critical** | 数据错乱/泄漏/崩溃/安全漏洞（N1/N2/N3/J6 等） | 立即进 P 修复，阻断发布 |
| **High** | 死锁/泄漏/大事务/性能劣化 | 本迭代修复，需 Benchmark |
| **Medium** | 坏味道/可维护性/复杂度高 | 下迭代，复杂度预算内修复 |
| **Low** | 最佳实践偏离/现代特性缺失 | 待观察，不主动重构 |

**P0 交付门**：`P0-Evidence-Pack/`（含 Code Facts + Runtime Facts + Architecture Facts + Test Facts + Risk Matrix），缺一不可，Agent 不得跳过。

---

## 4. P1–P10 Expert Toolkit（按需选用 + 纠正清单）

> 本章是对 v3.0“十大深水区”的纠正性升级。**每项技术的使用条件、禁令、验证要求均为硬约束**，写入 Skill 的执行协议，Agent 不得自由裁量。

### 4.1 P1 资源生命周期与所有权（方向正确，部分例子纠正）

**正确思想保留**：从代码形态升级到运行时行为（是否泄漏/阻塞/竞争/吞噬/不可追踪/生命周期错）。

**纠正 1 — Weak Event 非默认策略**

```
Subscribe → 明确所有权与取消责任（Dispose/Unsubscribe/CancellationToken）
          → 仅当 Publisher 生命周期长于 Subscriber 且无法自然解除时
            → 才考虑 Weak Event（带可观测与测试）
```

- 禁止全量弱引用化（会导致 handler 被意外 GC、行为不稳定、调试困难）。
- 产出必须含生命周期图 + 退订点。

**纠正 2 — ConditionalWeakTable ≠ TenantId 缓存**

| 场景 | 正确工具 |
|------|----------|
| 业务缓存（TenantId、配置、字典） | `IMemoryCache` / `IDistributedCache` / Redis（显式 TTL、容量、淘汰） |
| 对象生命周期绑定缓存（依附宿主 GC） | `ConditionalWeakTable<TKey,TValue>`（仅当 Key 是对象身份且需随宿主回收） |

`string tenantId` 属前者，禁止用后者。

**纠正 3 — HttpClient 生命周期**

- **默认**：`IHttpClientFactory`（或 `HttpClientFactory` + Typed Client），禁止每个 Service 自建/自 Dispose。
- 反例：`await _http.DisposeAsync()` 不能作为通用示范；`HttpClient` 复用与 DNS 轮转需由 Factory 管理。
- Skill 审计项升级为：**所有权 + DI 生命周期 + Factory + Dispose/IAsyncDispose + CancellationToken + background lifecycle** 六元组。

### 4.2 P2 GC / 内存（方向很好，必须加入证据门槛）

**Performance Change Gate（宪法级）**：任何 `Span<T>` / `Memory<T>` / `ArrayPool<T>` / `ObjectPool<T>` / `ValueTask` / Source Generator / Expression.Compile / SIMD 的引入必须回答：

```
1. 当前性能是多少？（基线 Benchmark）
2. 热点在哪里？（P0.2 证据）
3. Allocation 是多少？（dotnet-counters / BDN Allocation）
4. GC 影响是多少？（Gen2 频率、暂停）
5. 优化后是多少？（对比 Benchmark）
6. 复杂度增加多少？（代码行/生命周期/池化 bug 面）
7. 是否值得？（收益 > 复杂度成本 ?  go : no-go）
```

无 Benchmark → 不得以“性能优化已完成”交付。

### 4.3 P3 异步模型（重要错误修正）

**修正 1 — ASP.NET Core 同步阻塞的本质**

- 错误表述：“ASP.NET Core 中 `.Result` 必然死锁” → **绝对化**。
- 事实：ASP.NET Core 默认无传统 SynchronizationContext 经典死锁，但本质问题是：

```
Sync-over-Async → 阻塞线程池 → ThreadPool starvation → 吞吐下降 → 延迟飙升
```

Skill 必须检测**同步阻塞异步工作**，而非仅标签“死锁”。

**修正 2 — `.GetAwaiter().GetResult()` 非治理方案**

- 仅避免 `AggregateException` 包装，**未消除同步阻塞**。
- 治理路径：

```
同步 API → 能否改 async? → 是 → 全链路 async
                    ↓ 否
              明确同步边界 → 隔离阻塞（独立线程/队列） → 记录架构例外（ADR） → 可观测
```

- 禁止：`.Result` → `.GetAwaiter().GetResult()` 视为“已修复”。

### 4.4 P3 补充 — ValueTask 专项禁令

- **默认**：`Task<T>`。
- 仅当同时满足三条件才考虑 `ValueTask<T>`：

```
高频调用 + 大量同步完成 + Benchmark 证明 allocation 收益显著
```

- 否则收益 ≈ 0，语义复杂度（不可多次 await、需谨慎消费）显著增加，禁止机械替换。

### 4.5 P4 并发（值得保留，升级到原子性）

- 保留：`Dictionary` 并发读写、`++` → `Interlocked` 等典型审计。
- 升级：`ConcurrentDictionary` ≠ 正确。

```csharp
// 仍有竞态（非原子）
if (!dict.ContainsKey(key)) dict[key] = value;
// 正确：原子 API
dict.GetOrAdd(key, value); dict.AddOrUpdate(...); // 或显式锁
```

Skill 必须检查 **Atomicity**，而非仅 Thread Safety。

### 4.6 P5 异常体系（重定义，避免层次地狱）

- **禁止**：几十/几百种业务异常类。
- **推荐层次**：

```
Exception
 ├── Infrastructure / Technical
 ├── Application
 └── Domain
  （+ ErrorCode / ProblemDetails / HTTP mapping / inner exception / logging / correlation/trace）
```

- **Try 模式边界**：

```
TryGetUser() 适用： “不存在”是正常业务分支
throw 适用： 连接失败/超时/事务失败/数据损坏（exceptional failure）
```

- Skill 必须区分 **expected failure vs exceptional failure**，而非一律“异常控制流优化”。

### 4.7 P6 热路径性能（最强也最易误用）

- `Span` / `ArrayPool` / `ObjectPool` / Expression.Compile / Source Generator = **Hot Path Optimization Toolkit**，非 Class Refactoring Default Toolkit。
- **ArrayPool 生命周期风险**（已在 v3.0 自省）：

```csharp
var buf = pool.Rent(n);
try { return buf; } // ❌ 返回后 Return 会让调用方持有已归还数组
finally { pool.Return(buf); }
// → 正确：归到 Ownership / Lifetime（P1），明确所有权转移与归还责任，必要时由调用方归还或改用 MemoryOwner
```

- 任何池化归入 **P1 所有权** 统一分析，而非仅 P6 性能。

### 4.8 P7 现代 C#（从特性覆盖升级为类型语义治理）

| 类型 | 语义 | 推荐 |
|------|------|------|
| Entity（ORM 跟踪、Identity、Lifecycle、Mutation） | 可变、身份、生命周期 | `class`（慎用 record） |
| DTO / Value Object（不可变、值语义） | 不可变、相等按值 | `record` / `readonly record struct` |
| Domain Entity（可能含行为） | 行为+身份 | 视 ORM 与变更追踪定夺，优先 class |

Skill 必须做 **Type Semantic Fit** 分析，禁止“能用 Record 就全用 Record”。

### 4.9 P8 扩展性（加入复杂度预算）

```
简单 if/switch (2 分支)
  ↓
策略表 / Dictionary 映射
  ↓
Strategy + DI
  ↓
Factory
  ↓
Plugin architecture + Assembly Scan (仅当分支>5且开放扩展需求明确)
```

- 2 分支上 Strategy/Factory/DI Scan/Decorator = 过度架构。
- Skill 必须执行 **Complexity Budget**：当前复杂度 vs 引入架构的维护成本，收益>成本才升级。

### 4.10 P9 可观测性（方向正确，增加边界）

```
Observability = Trace + Metrics + Logs + Privacy + Cardinality + Cost
```

- `ActivitySource` / Metrics / 结构化日志纳入标准检查。
- **禁令**：

```csharp
activity?.SetTag("jnpf.user.name", request.Name); // ❌ 高基数/PII/存储成本
```

- 要求：PII 脱敏、高基数标签禁止（user.name、email 等）、采样与成本评估、租户上下文必含（M4/K4）。

### 4.11 P10 架构（保留，强化约束自动化）

```
Domain → Application → Infrastructure → API（方向禁止反向）
```

- 架构测试固化约束（Arch Tests），依赖环 0。
- 模块边界：`.Interfaces` 对外，禁止跨模块引实现（I1/N7）。

---

## 5. 证据门槛与量化验证（Skill 核心原则）

### 5.1 任何“高级技术”必须闭环

```
发现问题 → 确认真实性 → 确认影响度 → 确认根因 → 选择最低复杂度有效方案 → Benchmark/Test 验证 → 证明收益>复杂度成本
```

### 5.2 交付物清单（逐类）

| 交付物 | 来源 | 门控 |
|--------|------|------|
| P0-Evidence-Pack | §3 | 缺失则阻断后续 |
| GC 压力报告 | dotnet-counters | 若动 P2/P6 必含 |
| 异步模型修复报告 | 扫描 C + 压测 | 若动 P3 必含 |
| 并发安全报告 | 扫描 D + 并发测试 | 若动 P4 必含 |
| BenchmarkDotNet | BDN 对比基线vs优化后 | 若动 P6/ValueTask/Span 必含 |
| Activity/Metrics 集成 | OpenTelemetry | 若动 P9 必含 |
| 架构测试 | Arch Tests | 每次必含 |
| 行为回归报告 | 行为特征考卷 T0.3 | 每次必含 |

**无 Benchmark 就不能把“性能优化”标为完成**。

---

## 6. JNPF 特殊原则（与总路线一致）

```
Generic .NET Best Practice
        ↓
Repository / Service / Domain / Infrastructure（模块内三段式）
        ↓
JNPF-specific behavior（多租户/三元组/Oops/SqlSugar/动态表单）
```

- **先把通用 .NET 类级重构做到行业最佳实践，再处理 JNPF 特有复杂性**，而非反向。
- JNPF 铁律（N 维度）在 Skill 中为 **P0 红线**，扫描 N1/N2/N3/N4 命中 = Critical 直接阻断。

---

## 7. AI Agent 执行协议（可控性）

### 7.1 执行顺序（硬约束）

```
P0 → Risk Matrix → 选 P → 单类单批次（Smallest Verifiable Slice）
```

- **禁止**：整阶段塞进一轮；无 P0 直接改业务代码；全量铺开。
- **粒度**：一个 Chat = 一个可演示结果（如“单个聚合×单维度的 P0+修复+验证”），禁止一次处理多类。

### 7.2 每步必含

```
改前快照 → 最小改动 → 单测/考卷 → Benchmark（若涉性能） → 架构测试 → 回看（更新边界/风险/遗留）
```

### 7.3 失败处理

- 考卷变红 → 立即回退到最近提交，分析根因，不得带病前进。
- Benchmark 未达收益阈值 → 回退方案，改选更低复杂度方案。
- 命中不可逆清单（删类/改公共签名/改事务边界）→ 立即 STOP 等人工（见 L2 v2.0 §1 与 MASTER 不可逆清单）。

### 7.4 人话汇报（Boss 模式）

- 正文禁止类名/方法名堆砌，模板：

```
【状态】已修好/卡住/等你批
【人话】发生了什么+现在怎样+你点头后会怎样（各一句）
【你怎么验】命令一行+产物路径
【要你做】继续/通过/打回/重开（单选）
```

---

## 8. 验收协议（与 MASTER/L1/L2 衔接）

### 8.1 单类验收（L2 类级螺旋）

| 项 | 标准 | 证据 |
|----|------|------|
| 行为不变 | 行为特征考卷全绿 | `tests/characterization` CI 绿 |
| 风险闭环 | Risk Matrix 对应项 closed | `P0-Evidence-Pack` 更新 |
| 性能门 | Benchmark 收益>成本 | BDN 报告 |
| 架构门 | 依赖环 0，ARCH-01 通过 | `dotnet test --filter Architecture` |
| 测试门 | 单测≥3 覆盖核心规则 | Coverlet |
| 可观测门 | 日志/Trace/Metrics 含租户上下文 | 日志抽样 + Trace 截图 |

### 8.2 与上位衔接

| 上位 | 衔接点 |
|------|--------|
| MASTER S0–S1 | P0 证据复用：平台资产清单/数据责任映射/行为考卷/Legacy Registry/L1 表事实卡 |
| L1 表级螺旋 | 表-类-事务矩阵 → 聚合候选 → 类级对象 |
| L2 类级螺旋 v2.0 | 本规格为 L2 的“专家决策层”，L2 v2.0 为“执行 SOP”，冲突时以本规格为准的技术纠正生效 |

### 8.3 全局门控（复用现有）

- `dotnet build -c Release -p:CI_BUILD=true` 0 错误（含 JNPF009 同源复杂度）
- `dotnet test backend/zx_lowcode_netcore.sln`
- `dotnet test --filter Architecture`（Common.Core 硬失败）
- `arch-module-dependency-scan.ps1 -Gate`
- `test-hooks.mjs`（28 用例）

---

## 9. 版本与演进

| 版本 | 日期 | 变更 |
|------|------|------|
| v3.0 | 2026-08-27前 | 十大深水区技术清单（已审核，详见本文 §0 与 18 节审核意见） |
| **v4.0** | **2026-08-27** | **证据驱动升级**：新增 P0 证据门槛、风险矩阵、复杂度预算、Performance Change Gate、过度优化禁令；纠正 WeakEvent/ConditionalWeakTable/HttpClient/异步/ValueTask/并发原子性/异常层次/池化所有权/Record语义/策略复杂度/可观测隐私基数成本；建立 AI Agent 执行与验收协议；确立 Generic .NET First 原则 |
| v4.x | 待定 | 根据首批 3–5 个聚合实战回填 Benchmark 与案例库，固化为 `Generic-Class-Refactoring-Expert` Skill Core |

---

## 10. 制品与引用

- **审计底座**：`docs/superpowers/specs/JNPF后端类级代码审计扫描清单-v1.1.md`（16 维度×79 规则，N/O 为 JNPF 铁律专项）
- **扫描设计**：`docs/superpowers/specs/JNPF后端类级代码审计扫描设计规格-v1.0.md`
- **执行 SOP**：`docs/superpowers/plans/L2-类级螺旋专家级重构方案-v2.0.md`
- **表级输入**：`docs/superpowers/plans/L1-表级螺旋执行手册-v1.0.md` + `docs/architecture/platform-asset-inventory.v1.md` + `docs/architecture/data-ownership-profile.v1.md`
- **上位总纲**：`docs/superpowers/specs/MASTER-JNPF后端重构与Aspire微服务化总体设计规格.md` + `docs/superpowers/plans/MASTER-JNPF后端重构与Aspire微服务化总体实施计划.md`

---

> **下一步**：本规格待你“通过”后，产出对应的 **《v4.0 实施计划》**（含与 MASTER/L1/L2 的时序衔接、首批聚合选型、AI Agent 分批执行清单、门控与回退清单），再进入首个聚合的 P0 取证与重构。
