# Level-1-Context-Acquisition — 静态信息取证规程（R2 机制包 · 交付物 2）

> **版本**：v6.0-R2-M2 | **日期**：2026-08-28 | **地位**：R2 Consumer 实现细则（非 R1 条款）  
> **上游契约（冻结）**：Patch v2 §1（Budget）、§2.2（Confidence 定级）、§3（STOP）、§6（Request/Result）  
> **事实声明**：§2 证据源全部为 2026-08-28 实测 [KNOWN]（R1 示意文本中的假设性写法不作数，见 §5）

---

## 1. 取证总纪律

1. **Level 优先**：先 Level 1 静态可得的，才允许主张 Level 0 问人（`Level-0-Context-Template.md` §1 前置 3）。
2. **针式搜索铁律**（repo 强制，与 Budget 同构）：先窄后宽、并行 ≤3、禁全仓拖网；大文件局部 Read。
3. **工具现实**：本仓库 **CodeGraph 未索引**（实测 no .codegraph）——Level 1 流程只允许 `Read（局部）/ Grep / Serena MCP（find_symbol / find_referencing_symbols / get_symbols_overview）` + 现成脚本（`scripts/arch-module-dependency-scan.ps1`）。任何依赖 codegraph 的规程 = F-E 环境违规。
4. **每轮 checkpoint**：每次取证轮结束，先跑 R1 §3.1 顺序检查（见 `Level-0-1-Validation.md` §4），再决定下一轮——不许"先多查点回头再筛"。

## 2. JNPF 真实证据源清单（Level 1）

| Context Type | 真实源（实测 [KNOWN]） | 锚点样例 | Confidence |
|---|---|---|---|
| **DI 生命周期** | **约定式标记接口**：类声明实现 `ITransient / IScoped / ISingleton`（`backend/framework/JNPF/DependencyInjection/Dependencies/` 三文件 :6 行），由 JNPF.DependencyInjection 自动注册。**不存在**手写 `AddScoped<X>()` 主流程 | `OrderService.cs:36 → ITransient`；`FileManager.cs → IScoped` | High |
| **[UnitOfWork] 可用性** | 属性定义 `framework/JNPF/UnitOfWork/FilterAttributes/UnitOfWorkAttribute.cs:15`（`IAsyncActionFilter`）+ AOP 注册 `application/JNPF.API.Entry/Extensions/SqlSugarConfigureExtensions.cs:54 AddUnitOfWork<SqlSugarUnitOfWork>()`。注意：**与业务类 Transient/Scoped 无关**，AOP 按请求管道包裹 | 同左 | High |
| **Call** | 接口签名（实现体在 `JNPF.Common.Core/Manager/`） | `IFileManager.cs:46 → Task<FileStreamResult>` | High |
| **Ownership** | 本类 grep `using|finally|Dispose|Directory\.|File\.` + 消费端点同文件可达 | `FileService.cs:244 建目录；:263 返回下载 URL；:271 DownloadFile 消费——全仓无 TemporaryFile 清理代码（模拟人回复补充外部消费语义）` | 本类内 High；"外部谁清" → 只能 Level 0（Medium） |
| **DataFlow** | 循环内查询形态静态可判；**实际次数/数据量 = Level 2，全仓无源** | `ScheduleService.cs:809 foreach 内 Queryable<ScheduleUserEntity>().ToListAsync()` | 形态 High / 量级不可得 |
| **CrossLayer / Scope** | `.csproj` ProjectReference 图；模块归属 = 实体表前缀（如 `FlowTaskEntity.cs:9 [SugarTable("FLOW_TASK")]` 属 WorkFlow 域） | `JNPF.Extend.csproj:6 仅引 JNPF.WorkFlow.Interfaces，**未引** .Entitys` | High |
| **NuGet 外部边界** | 第三方类型（如 `IOSSServiceFactory`←`OnceMi.AspNetCore.OSS`，见 `FileManager.cs:15 using`）源码不在仓 → 该 hop 之后 Level 1 **不可得** → 直接进 §3.4 矩阵"不可获取(external)"行 | 同左 | — |

## 3. 轮次操作规程（每轮固定五步）

```
1. 声明本轮缺口 CT（Request 结构 = Patch §6.1，含 claim + budget 快照）
2. 针式取证（只打缺口，不顺手看别的；顺路发现 → 新 Finding 候选，NC04 纪律）
3. 证据登记（source 三元组：file:line / tool-output / human-statement；snippet 必须是命中原文）
4. 计数更新（§4 口径）+ Confidence 定级（Patch §2.2）
5. checkpoint：按 §3.1 顺序跑 STOP-4→5→1→2→3，逐条写结果，再决定停/续
```

## 4. Budget 计数口径（操作化——含 1 条需人工裁定的解释假定）

| 维度 | 计数规则（Validator 重算基准） |
|------|-------------------------------|
| 起点 | `finding_file`（Finding 所在类文件）与 manifest 标注的 P0 基线文件 **不计**；从第一次跨出基线起算。**非 manifest 目标**（外部 NuGet/未命中路径）= 取证未得，不计账也不产证据（引用它作 Evidence 会被 V-5 拒） |
| **Artifact** | **任何模式**（body 正文 read 或定点 grep/symbol lookup）首次触碰一个新 manifest 文件即 +1。锁定语义（R2-GAP-01 ACCEPTED）：定点定位**照常计 Artifact** |
| **Scope** | 仅 `mode=body` 读取**且**文件属他 project 时，去重计新 project 数。定点证据定位免 Scope——**但**：无差别 grep 多文件会线性烧穿 Artifact（"Targeted localization ≠ scope expansion / Broad discovery = expansion"，永久边界） |
| **Depth** | 任何模式的新文件取证均推进 `max(hop)`；Finding 文件=0；body 与定点同权 |
| **Iteration** | 一个"§3 五步"闭环=1；Pending 人卡不占（L0 §4）；轮内并行查询不另加 |
| **Time** | trace.meta 观测字段，**禁入任何判定**（Validator V-4 扫描措辞） |
| 防漂移 | 同文件重复读不 +1；partial class / 同文件换名 = 同 artifact（Auditor 终裁） |

## 5. 示意 ≠ 事实（前提事实免疫条款）

R1 文档中的示例性写法（如 "Startup.cs: services.AddScoped<OrderService>() → Scoped"）**不是事实断言**。实测：OrderService 真实为 `ITransient`。任何 Agent 把设计示意当运行时事实继续推理 = R1 §2.2 定级违规（示意最多 Low），Validator INV-5 会因 snippet 与真实行不符而 FAIL。规则：**证据只认基线文件里 grep/Read 得到的原文**。

## 6. 解释假定 → 已裁定锁定（R2-GAP-01 ACCEPTED，2026-08-28）

若按最严口径（凡触碰新 project 文件即计 Scope），RB-01 类 DI Finding 的框架基础设施证据（JNPF + JNPF.API.Entry 两 project）将结构性超过 Regional S=1，GO 永不可达。架构师裁定采锁定语义：**定点证据定位免 Scope、但照常计 Artifact/Depth/Iteration；无差别 grep 拖网由 Artifact 预算线性封死**（Validator 配双负例验证）。判据本质：Budget 限制无边界扩张，不阻止完成当前 Claim 所必需的最小证据获取。R1 §1.2 表本身一字未动；裁定全文见 `R2-GAP-01.md`。
