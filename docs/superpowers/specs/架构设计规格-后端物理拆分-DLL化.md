# 架构设计规格 — 后端物理拆分（framework/JNPF DLL 化）v1.0

> 结构重构型战役（本模板 §4 模块类型分流）：所有模块的业务成功标准 = **重构证据标准**（行为不变性证明），不新增任何业务能力。
> **P0 执行前提：** T1 工具完备（文件读写 + dotnet build/test + git）。人工闸门：G1 本规格审批 → G2 S0 核验结论审批 → G3/G4… 每批次节点审批 → G5 终审。

---

## 1. 目的与范围

### 1.1 要解决的问题

`backend/framework/JNPF/` 单工程承载 37 个功能区（约全框架层代码主体），导致：

1. **AI 上下文污染**：改任何一处框架代码，AI 面对整包源码，注意力被稀释，且无物理边界阻止越界修改；
2. **框架层参与日常全量编译**：业务侧每次 `dotnet build` 都重建框架源码，编译时间被巨无霸拖累；
3. **打包管线只产不销**：`framework/nupkgs/` 已有 5 个 3.4.7 成品包，但 `nuget.config` 无本地源，业务侧全部走源码工程引用，锁版本消费闭环缺失。

### 1.2 本期排除（砍刀只砍已提出的）

- modularity 43 个业务工程：**已经是独立 DLL**（每个 csproj 即一个程序集），且高频变更，包化只会增加发版摩擦——不动。
- 内核 11 个成环功能区解环：FriendlyException↔UnifyResult、DataValidation↔DynamicApiController、App↔ConfigurableOptions 成环（2026-08-23 扫描实锤），解环属远期专项。
- OA 宿主、workflow 内部、前端拆分（另战役）。

### 1.3 硬约束

1. **零行为变更**：纯移动纪律，方法体逐字不改；只允许新增工程文件与引用行。
2. **零业务源码改动**：所有迁移保持原命名空间，业务工程 `.cs` 文件零改动。
3. **零 schema 变更**：不碰任何数据库表。
4. **与 RunService 战役（S0~S5）文件面零交集**（见 3.3）。

---

## 2. 架构决策记录

| # | 决策 | 备选与否决理由 | 失效条件 |
|---|---|---|---|
| D1 | **程序集先行，NuGet 消费闭环后置**（两步走） | 直接包化被否：消费管线未验证即切包引用风险大；只拆工程不上包被否：编译提速不兑现 | 本地源在 CI 不可用 → 退回工程引用，拆分收益保留 |
| D2 | **命名空间冻结**：迁移后命名空间不变，业务侧 `using` 零改动 | 改命名空间被否：43 个业务工程源码全动，违背硬约束 2 | 无（单向承诺） |
| D3 | **内核 11 区不拆**，整体留 `JNPF`（瘦身后即事实上的 JNPF.Core） | 解环拆分被否：成环证据在手，工程量大收益低 | 解环专项立项时重评 |
| D4 | **防过碎分组**：零依赖 9 区合并为 3 个 DLL，不做 9 个 | 一区一包被否：程序集爆炸，启动与加载开销反增 | 无 |
| D5 | **消费闭环走本地源**：`nuget.config` 追加 `framework/nupkgs` 本地路径，不动华为云/nuget.org 源 | 私有 registry 被否：机级安装需另行审批，收益不配 | 团队规模扩大需正式 feed 时升级 |
| D6 | **B3 批次（依赖内核区）为可选阶段**，拆分需给消费方业务工程批量加引用行（仅 csproj，仍不动 `.cs`） | 不拆被否于远期：RemoteRequest/Cache 等 AI 上下文收益真实；立即拆被否：需先量化消费方清单 | B1/B2 落地后按收益决定 |

---

## 3. 系统分解

### 3.1 批次清单（依赖证据：2026-08-23 全 37 区 using 扫描）

| 批次 | 新工程（DLL） | 迁入功能区 | 依赖方向 | 扫描证据 |
|---|---|---|---|---|
| B1 | `JNPF.Extensions.Cryptography` | DataEncryption | 零内部依赖 | using 扫描零命中 |
| B1 | `JNPF.Extensions.Utils` | TimeCrontab + DistributedIDGenerator + LinqBuilder | 零内部依赖 | 三区均零命中 |
| B2 | `JNPF.Abstractions` | Reflection + Modules + Authorization + Configuration + VirtualFileServer | 零内部依赖；被内核单向依赖（App/DependencyInjection 等引用它们，方向合法） | 五区均零命中 |
| B3（可选） | `JNPF.Caching` / `JNPF.RemoteRequest` / `JNPF.WebAssets`(SpecificationDocument+CorsAccessor+AspNetCore) / `JNPF.BackgroundJobs`(Schedule+TaskQueue+IPCChannel) | 17 个内核依赖区按组 | 单向依赖内核（内核不反向引用，扫描证实） | 各区仅 using 内核命名空间 |
| 不拆 | `JNPF`（内核，事实 JNPF.Core） | App、DependencyInjection、DynamicApiController、ConfigurableOptions、Options、DataValidation、FriendlyException、UnifyResult、Templates、ClayObject+JsonSerialization（互缠簇） | — | 三组成环实锤 |

**引用接线规则：** B1/B2 工程←被 `JNPF.csproj` 引用（JNPF → 新 DLL），业务工程经 JNPF 传递获得，**43 个业务 csproj 零改动**。B3 相反：新工程 → 引用 JNPF（内核），消费方业务工程需直接引用新工程（csproj 加行，`.cs` 不动）——这是 B3 列为可选的原因（D6）。

### 3.2 依赖不变量（终审第 9 查口径）

合法方向：`新DLL(B1/B2) ← JNPF内核 ← 业务工程 ← 宿主`；`B3新DLL → JNPF内核`。禁止：新 DLL 反向引用内核以外的未声明工程；任何循环。

### 3.3 多轨隔离（与 RunService 战役）

- **本战役文件面：** `framework/JNPF/{迁入区}/**`、`framework/JNPF.Extensions.*/`、`framework/JNPF.Abstractions/`、`JNPF.csproj`、`zx_lowcode_netcore.sln`（新增工程时独占写入）、`nuget.config`、本战役两份文档。
- **战役面（禁触）：** `modularity/visualdev|engine/**`、`Program.cs`、`App.json`、`docs/architecture/contract-registry.md`（本战役只追加条目，不改战役条目）。
- **协调规则：** 每批次开工前确认战役节点状态；战役路由快照采集/比对窗口内本战役不提交；sln 写入与战役节点错窗。

---

## 4. 模块详细设计（结构重构型 — 重构证据标准）

每个批次的统一证据标准（五件套，缺一不可）：

1. `dotnet build` 0 错误（backend 全解决方案）；
2. 既有测试全绿（`dotnet test backend/zx_lowcode_netcore.sln`）；
3. **路由快照零 diff**（`dotnet run --project tools/JNPF.Startup.Benchmarks -- --mode routes` 与基线比对，复用 RunService 战役基线机制）；
4. **Public API 面冻结**：迁移前生成每区 public 类型/成员清单（文本），迁移后 diff = 0；
5. **文件守恒**：`git status` 显示纯移动（rename 检测），无内容 diff（除新增 using 行，若 GlobalUsings 分析要求）。

### 4.1 B1-Cryptography（DataEncryption）

- 验收：五件套 + `grep -r "JNPF.DataEncryption"` 命名空间命中数守恒。
- 回滚：批次级 `git revert`。

### 4.2 B1-Utils（TimeCrontab/DistributedIDGenerator/LinqBuilder）

- 同上；额外确认 Schedule/TaskQueue/Options（内核消费方）经传递引用编译通过。

### 4.3 B2-Abstractions（五区）

- 同上；额外确认内核区（App/DependencyInjection/Localization/ObjectMapper/SpecificationDocument）编译通过。

### 4.4 NuGet 消费闭环（S4）

- `nuget.config` 追加：`<add key="jnpf-local" value="../framework/nupkgs" />`（相对路径以 backend/ 为锚，S0 核验正确写法）；
- `JNPF_PACK=true dotnet build -c Release` 产出 3.5.0 包；
- 试点消费方：任一业务工程切 `PackageReference JNPF.Extensions.Utils@3.5.0` 替换传递 ProjectReference 验证；
- 证据：restore 成功 + 构建/测试绿 + **全量构建时间前后对比（各 3 次取中位，落盘 evidence）**。

---

## 5. 横切关注点

### 5.2 数据迁移

无 schema 变更、无数据迁移。

### 5.4 架构假设登记（S0 闭环后回填状态）

- A-1：`JNPF.Extensions` 命名空间物理归属在内核目录（推测）——S0 溯源；若落在 B1/B2 区内则调整归组。
- A-2：`Cache` 区 `using JNPF.Common.Cache` 来源为业务公共层 `JNPF.Common.Core`（疑似框架反向依赖业务层）——S0 溯源；影响 B3-JNPF.Caching 设计，不影响 B1/B2。
- A-3：`EventBus` 区 `JNPF.Extensitions.EventBus` 为拼写命名空间，物理位置待定位——S0 溯源。

### 5.7 契约

对外公共面 = 迁移区 public API 清单（4 件套之 4），按 RunService 战役同款登记入 `docs/architecture/contract-registry.md`（追加条目 `C-SPLIT-{区名}@v1`，含 SHA256），下游编写/生成前以清单为准。

---

## 6. 风险与 SLO

| 风险 | 概率 | 影响 | 缓解 |
|---|---|---|---|
| GlobalUsings 隐式依赖导致迁移编译失败 | 中 | 低（可当场补 using） | S0 Task 0.2 预扫描 + 允许"仅增 using 行"豁免 |
| 与 RunService 战役窗口冲突 | 中 | 中 | 3.3 协调规则 + 节点错窗 |
| 包引用切换后传递依赖断裂 | 低 | 中 | S4 试点单工程先行，绿后再推广 |

**SLO（业务口径）：** B1~B2 完成后，"AI 修改框架工具代码"任务的可见上下文从整包缩至单工程；S4 完成后全量构建时间下降（目标值 S0 基线采集后登记，不预先编造）。
