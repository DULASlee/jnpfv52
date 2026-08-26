# P0 Evidence Pack — Pilot-2 Read-Only

> **目标类**：`JNPF.Extend.EmailService`
> **文件**：`backend/modularity/extend/JNPF.Extend/EmailService.cs:34`（574 行，中等复杂度，非核心路径 extend 模块）
> **选型理由**：Pilot-1 已证明“无证据不优化”；本类含真实技术债（4 分支 switch、事务、同步邮件发送阻塞异步方法、文件 Move、HTML 编解码、跨实体的 object 装箱），但不属权限/租户最危险核心，适合验证“多潜在方向时如何控范围、凭证据决策改多少”。
> **聚合**：Email（`EmailReceiveEntity` / `EmailSendEntity` / `EmailConfigEntity`，共表，草稿/已发送/收件箱多视图）
> **关联表**：`EmailReceiveEntity`、`EmailSendEntity`、`EmailConfigEntity`（待 L1 事实卡确认）
> **日期**：2026-08-27
> **模式**：Read-only → Evidence → Findings → Risk → Performance Gate → Complexity Budget → Safety Gates → Decision（零业务代码修改）

## P0.1 代码事实（静态）

| 项 | 值 | 工具 |
|----|----|------|
| 行数/方法数/字段数 | 574 行 / 公开 API 12（GetList/GetInfo_Api/GetConfigInfo_Api/Delete/ReceiveRead/ReceiveUnread/ReceiveYesStarred/ReceiveNoStarred/Receive/SaveDraft/SaveSent/SaveConfig/CheckLogin/Download）+ 私有 5（GetConfigInfo/GetReceiveList/GetUnreadList/GetStarredList/GetDraftList/GetSentList/GetInfo）/ 4 字段 | 计数 |
| JNPF009 同源 CC | `GetInfo` (id) 含 `AnyAsync`→`FirstAsync` 二次查询 + `if/else` 分支；`SaveSent` 含 `foreach recipient.Split` + 文件 Move + 同步 Send，估 CC 9–11（需 Analyzers 复核） | JNPF.Analyzers |
| God Class 信号 | 方法数 17，字段 4，未超阈值但职责混杂（查询+事务+文件+邮件外发+配置） | 扫描 L1 |
| 依赖数 | 4 注入：`ISqlSugarRepository<EmailReceiveEntity>`、`ITenant`（`_db.AsTenant()`）、`IUserManager`、`IFileManager`；外加 `MailUtil` 静态、`FileHelper` 静态、`HttpUtility` | 构造函数 + 静态 |
| 循环依赖 | 无显式环（待 scan 复核） | dependency-scan |
| 模块边界 I1/N7 | `using JNPF.Extend.Entitys` 合规（本模块）；未跨模块引实现，但 `MailUtil` 在 `JNPF.Extras.Thirdparty.Email` 属基础设施，领域直接依赖具体实现 | 扫描 I/N7 |
| DI 生命周期 | `ITransient` 每请求新建 | 类声明 |
| IDisposable | 未实现；持有 `ITenant _db`（SqlSugar AsTenant 作用域）未显式释放 | 扫描 A |
| 静态可变状态 | 无 | 扫描 D |
| 调用方数 | 待 Serena `find_referencing_symbols`：预估为 Extend 邮件前端调用，低扇出 | 待补 |

## P0.2 运行时事实（基线，未启动性能优化故仅记录，不作优化依据）

| 项 | 当前值 | 证据路径 |
|----|--------|----------|
| CPU/分配 | 未采集（本 Pilot 不涉性能优化，按 Gate 禁止） | — |
| GC | 未采集 | — |
| ThreadPool | 未采集；但 `SaveSent` 在 `async Task` 中同步 `MailUtil.Send`（阻塞），`Delete` 中同步 `MailUtil.Delete`，属 Sync-over-Async 潜在 starvation | 代码路径 `EmailService.cs:346,132` |
| P50/P95 | 未采集 | — |
| DB | 多处 `Queryable.Where(Contains)` + `ToPagedListAsync`；`GetInfo` 对同一 id 先 `AnyAsync` 再 `FirstAsync`（二次往返）；`Delete` 中 `BeginTran/Commit/Rollback` 单事务 | 代码路径 |
| 异常 | `try { BeginTran … Commit } catch(Exception){ Rollback; throw Oh }` 吞原始栈（`catch(Exception)` 无 ex 变量），`ReceiveRead` 等直接 `throw Oh(COM1008)` | 日志抽样 |

> **结论**：无热度量化 → 按 Gate，Span/ArrayPool/ValueTask/池化/并行化 **禁止进场**。

## P0.3 架构事实

- 方向：`Extend` → `Common`/`Extras.Thirdparty`，单向；但 `Service` 直操 `AsSugarClient().Queryable/Updateable/Insertable`（I2），仓储抽象仅部分使用（`_repository` 仅作类型载体，实际走 `AsSugarClient()`）。
- 边界：邮件外发（`MailUtil`）属基础设施端口，应抽象为 `IMailPort`，当前为静态强依赖，难测试。
- 事务：`Delete` 单聚合单事务，范围清晰；其他写操作无显式事务，单语句。

## P0.4 测试事实

| 项 | 值 |
|----|----|
| 行为特征考卷 | 未命中（Extend 邮件未在 30 条基线中，属边缘） |
| 单测/集成/并发 | 0 |
| Benchmark | 无（不涉） |

## P0.5 风险定级

| 风险项 | 等级 |
|--------|------|
| 总体 | **High**（含事务+外发+文件+二次查询，虽非核心路径但可扩散为数据不一致/阻塞） |

## Findings（16 维度去重，问题≠自动改）

| # | 维度 | 规则 | 文件:行号 | 问题摘要 | 影响面（量化） | 证据 |
|---|------|------|-----------|----------|----------------|------|
| F-01 | H1 | H1 | `EmailService.cs:59` `switch(input.type)` | 4 分支 dispatch（inBox/star/draft/sent），当前 4 分支未超阈值，但 `GetList` 仅为分发，仍可演进为字典映射前置（Complexity Budget：当前保持 switch，>5 分支才升 Strategy） | 低（可维护） | 扫描 H1 |
| F-02 | I2 | I2 | `EmailService.cs:124,137,160,240,417...` | Service 直操 `AsSugarClient().Queryable/Updateable/Insertable`，仓储未收敛；`GetInfo` 对同一 id `AnyAsync`→`FirstAsync` 二次往返 | 中（往返+耦合） | 扫描 I2/F1 |
| F-03 | E | E6/E2 | `EmailService.cs:145,147` | `catch(Exception){ Rollback; throw Oh(COM1002); }` 丢弃 `ex`，原始栈丢失；应 `catch(Exception ex){ Log + throw Oh(ex) }` 或 `throw` 保留 | 中（可观测） | 扫描 E2/E |
| F-04 | C | C1 | `EmailService.cs:306,346,132` | `async Task SaveSent/Delete` 中同步 `MailUtil.Send/Delete/CheckConnected` 阻塞线程池（Sync-over-Async），非“死锁”但会 starvation；正确路径：`MailUtil.SendAsync` 或隔离到后台 | 中（吞吐） | 扫描 C1 |
| F-05 | J4 | J4 | `EmailService.cs:335` `Path.Combine` + `FileHelper.MoveFile`；`EmailService.cs:400` `Path.Combine(EmailFilePath, FileId)` | 文件 Move/Download 拼接用户侧 `fileId`，虽用 `Path.Combine` 但未校验 `fileId` 是否含 `../`，需白名单+规范化+目录越界检查 | 中（路径遍历） | 扫描 J4 |
| F-06 | N1/J6 | N1/J6 | `EmailService.cs:417,427,437` 等多处 `Where(CreatorUserId == UserId && DeleteMark==null)` | 无 `TenantId` 显式过滤，依赖 `ITenant` + `CreatorUserId` 隔离；若同库多租户则属 N1 泄漏，待 L1 确认是“分库分连接”还是“同库 TenantId” | 中（潜在泄漏，待证） | 扫描 N1/J6 |
| F-07 | E | E3 | `EmailService.cs:84,559` `ToObject<JObject>().ContainsKey("Read")` + `is EmailReceiveEntity` 二次判定 | 以异常/类型试探替代显式状态（`object` 装箱 + JObject 反射），可用显式查询或枚举区分收/发 | 低（可读+分配） | 扫描 E3/B |
| F-08 | K/M | K4/M4 | 全类日志 | 关键写操作（Delete/Receive/SaveSent）无结构化日志含 TenantId/UserId/RequestId，可观测缺口 | 低 | 扫描 K1/M4 |
| F-09 | F2 | F2 | `EmailService.cs:437` `ToPagedListAsync(currentPage,pageSize)` | 分页合规（未见 `Skip(>1000)` 深分页），暂无 F2；但 `Receive` 中 `Queryable.CountAsync(Between)` + 全量 `MailUtil.Get` 拉取存在批量压力，待运行时证实 | 低 | 扫描 F2 |
| F-10 | D | — | — | 无静态集合/锁，并发低 | — | 扫描 D |
| F-11 | A | A | — | 无事件订阅/Timer/静态集合泄漏 | — | 扫描 A |
| F-12 | G | G | — | 无 NRT/Record 误用；Entity 保持 class 正确，无需 Record 化 | — | 扫描 G |

> **去重**：N1=J6 合并；二次查询 F-02 与架构 I2 合并；H1 与 F-01 合并。

## Risk / Impact Matrix

| Finding | 风险 | 影响 | 成本 | 决策 |
|---------|------|------|------|------|
| F-04 同步邮件阻塞 async | High | 中（吞吐/延迟） | 中（2–8h，需 async 端口或后台隔离） | **P1 本迭代候选**（但本 Pilot 仍只读，不实施；需先补运行时 starvation 证据） |
| F-03 异常吞栈 | High | 中（排障） | 低（<2h） | **P1 本迭代候选**（只读阶段仅记录） |
| F-02 二次往返 + 直操 DB | Medium | 中（耦合+往返） | 高（>8h，需仓储收敛） | **P2 下迭代**（控范围，不在本类大改） |
| F-05 路径遍历 | Medium | 中（安全） | 低（<2h，规范化+越界检查） | **P1 本迭代候选**（需白名单） |
| F-06 租户显式过滤 | Medium | 中（泄漏待证） | 中 | **P2 下迭代**（先 L1 确认连接策略） |
| F-01 4 分支 switch | Low | 低 | 低 | **P3 待观察**（当前保持 switch，>5 分支才升字典/Strategy） |
| F-07 object+JObject 试探 | Low | 低 | 中 | **P3 待观察** |
| F-08 可观测缺口 | Low | 低 | 低 | **P3 待观察** |

> **控范围体现**：雖有 5 项达 P1，但本 Pilot **全部不实施**，仅记录；下一阶段首个真实重构仅选 **1 项中成本以内且证据最实**（如 F-05 或 F-03），禁止“发现 5 项就 5 项全改”。

## Performance Change Gate（7 问，全部“未达”故禁止性能类重构）

| # | 问 | 答 |
|---|----|----|
| 1 | 当前性能？ | 未采（无 BDN） |
| 2 | 热点？ | 未证（仅代码推断 F-04） |
| 3 | Allocation？ | 未采 |
| 4 | GC 影响？ | 未采 |
| 5 | 优化后？ | 无 |
| 6 | 复杂度增加？ | 若上 async 隔离/池化则 + 中高 |
| 7 | 是否值得？ | **no-go**（无证据） |

**判定**：Perf 相关（Span/ValueTask/Pool/并行）**no-go**。

## Complexity Budget（P8 示例）

| 方案 | 新增行数 | 维护成本 | 本类适用 |
|------|----------|----------|----------|
| 保持 switch（当前） | 0 | 低 | ✅ 4 分支时 go |
| 字典映射 dispatch | +10 | 低 | 5 分支时 go |
| Strategy+DI+Scan | +80 | 中 | ❌ 2–4 分支时禁止（过度架构） |

> **结论**：F-01 保持 switch，不升 Strategy。

## 三安全阀

1. 没有证据，是否进行了高级优化？ **否**（P0.2 无量化，P6 全禁）
2. 没有量化验证，是否宣称性能改善？ **否**
3. 发现问题是否自动修改了代码？ **否**（Finding≠Fix，本 Pilot 零改）

## Decision

- **是否允许进入 P1..P10 重构**：**否**（Pilot-2 仍为只读验证，不进入；与 Phase 0 边界一致）
- **若未来进入真实重构，首选**（单项，小切片）：
  - **Option A（推荐）**：F-05 路径规范化（低成本、高安全收益、易验证、易回归）— 或 F-03 异常保留栈（低成本、可观测收益）
  - **禁止首选**：F-02 大范围仓储收敛（高成本）、Strategy 重构（过度）、ValueTask/Pool（无证据）
- **需补证据后才可提审**：F-04 需补 ThreadPool starvation 压测 + `MailUtil.SendAsync` 可行性；F-06 需 L1 连接策略确认
- **Recommendation**：
  1. 下一阶段首个真实重构仅选 **1 个 P1 低/中成本项**（F-05 或 F-03）单类单提交；
  2. 补 `find_referencing_symbols` 调用方数与事务边界回看；
  3. 不得借首个重构顺手改 5 项。

## 验证计划（本 Pilot 已执行）

- [x] 改前快照（`git diff -- backend` 0）
- [x] 只读扫描 16 维度去重
- [x] Risk / Gate / Budget 三门控
- [ ] Benchmark（不涉）
- [x] 架构/依赖抽检
- [ ] 回归（未改）

## 引用

- Spec v4.0 §3–§5
- 扫描清单 v1.1 16 维度
- L2 v2.0 §5 五步循环

---

> **本文件证明**：面对含多潜在方向的中等复杂度类，Skill 仍能控范围（5 项 P1 候选仅记录不改）、控复杂度（H1 不升 Strategy）、控性能门（7 问全禁），输出 `Evidence→Finding→Risk→Decision` 完整可审计链，且零改代码。
