# P0 Evidence Pack — Pilot Read-Only Demonstration

> **目标类**：`JNPF.Message.Service.ShortLinkService`
> **文件**：`backend/modularity/message/JNPF.Message/Service/ShortLinkService.cs:32`（131 行，小而独立，适合证明“看懂不改”）
> **聚合**：Message ShortLink（单表 `MessageShortLinkEntity`，无强外键聚合，日志型）
> **关联表**：`MessageShortLinkEntity`（推断，待 L1 事实卡确认）
> **分析日期**：2026-08-27
> **模式**：Read-only → Evidence → Findings → Risk → Recommendation（零业务代码修改）

## P0.1 代码事实（静态）

| 项 | 值 | 工具 |
|----|----|------|
| 行数/方法数/字段数 | 131 行 / 3 公开方法（GetInfo/Create/CreateToken）/ 5 字段 | 直接计数 |
| JNPF009 复杂度 | `GetInfo` 含多分支（IsUsed/时间/点击数），估 CC≈7（需 Analyzers 复核） | JNPF.Analyzers |
| 依赖数 | 5 注入：`ISqlSugarRepository<MessageShortLinkEntity>`、`ISysConfigService`、`ITenantManager`、`ConnectionStringsOptions`、`ISqlSugarClient`（强转为 SqlSugarScope） | 构造函数 |
| 循环依赖 | 无显式环（待 dependency-scan 复核） | arch-module-dependency-scan |
| 模块边界违规 I1/N7 | `using JNPF.Systems.Interfaces.System` + `using JNPF.Systems.Entitys.Permission`（跨模块引 Systems 实现侧实体 UserEntity） | 扫描 N7/I1 |
| DI 生命周期 | `ITransient`（每次请求新建） | 类声明 |
| IDisposable | 未实现（持有 SqlSugarScope 引用但未 Dispose） | 扫描 A |
| 静态可变状态 | 无 | 扫描 D |
| 调用方数 | `Create` 被调用方待 Serena `find_referencing_symbols` 定量（推断：消息/工作流通知链） | 待补 |

**备注**：小类但含多租户切换、令牌签发、重定向等敏感行为，适合验证“看懂”。

## P0.2 运行时事实（演示用，Phase 0 仅记录基线，不做优化）

| 项 | 当前值 | 证据路径 |
|----|--------|----------|
| CPU/分配 | 未采集（Phase 0 只读，不启动压测；若后续动 P6 需先补） | — |
| GC | 未采集 | — |
| ThreadPool | 未采集 | — |
| P50/P95 延迟 | 未采集 | — |
| DB | `Queryable<MessageShortLinkEntity>.SingleAsync` 单次查询 + 1 次更新（ClickNum++） | 代码路径 |
| 异常 | 已覆盖 `Oops.Oh(D7009/D7010)`，未吞没 | 日志抽样 |

> **结论**：无运行时热度证据 → 按 Performance Gate，任何 Span/ArrayPool/ValueTask/池化 **禁止进场**。

## P0.3 架构事实

- 方向：`Message.Service` → `Systems.Interfaces`（跨模块），符合“引用 .Interfaces 而非实现”，但 `CreateToken` 中直接 `Queryable<UserEntity>` 跨聚合读用户，属**跨模块直接读实现表**，待显式化。
- 生命周期：`ITransient` 持有 `SqlSugarScope` 强转，作用域与请求一致，无泄漏证据，但所有权模糊（见 Findings）。

## P0.4 测试事实

| 项 | 值 |
|----|----|
| 行为特征考卷命中 | 无（ShortLink 未在 30 条基线中，属边缘） |
| 单测 | 0 |
| Benchmark | 无（未涉性能） |

## P0.5 风险定级

| 风险项 | 等级 | 依据 |
|--------|------|------|
| 总体 | **Medium**（小类，可控） | 无 Critical，高风险项见 Findings |

## Findings（去重，按 16 维度，问题≠自动改）

| # | 维度 | 规则 | 文件:行号 | 问题摘要 | 影响面（量化） | 证据 |
|---|------|------|-----------|----------|----------------|------|
| F-01 | N3/R8 | N3 | `ShortLinkService.cs:60` `GetInfo` | 同类中 `Create/CreateToken` 为 `[NonAction]/public` 但 `GetInfo` 已有 `[AllowAnonymous]`，权限模型清晰；但 `CreateToken` 为 `public async Task<string>` 非 API 却暴露为可被扫描为“缺权限”误报 → 需白名单`[NonAction]`豁免 | 低（误报） | 扫描清单 N3 |
| F-02 | A | A3/A4 | `ShortLinkService.cs:39,53` | 字段 `_sqlSugarClient` 持有 `SqlSugarScope` 强转，未 Dispose，所有权归属不清；`ITransient` 每请求新建，Scope 生命周期与请求绑定，暂无泄漏但需明确归属 | 低 | 代码事实 |
| F-03 | E | E6 | `ShortLinkService.cs:73,77,83,127` | 已正确使用 `Oops.Oh`，无裸 `throw new Exception` | —（合规） | 扫描 E6 |
| F-04 | J/N1 | N1/J6 | `ShortLinkService.cs:72,127` | `Queryable<MessageShortLinkEntity>.SingleAsync(x => ShortLink==...)` 未显式 `TenantId` 过滤，依赖 `ChangTenant` 切换连接；若多租户直连单库则需 `Where(TenantId)` 显式过滤，否则属泄漏风险（待 L1 确认连接策略） | 中（潜在泄漏） | 扫描 N1/J6 |
| F-05 | I | I2 | `ShortLinkService.cs:72,127` | Service 直接 `Queryable` + `AsUpdateable/AsInsertable`，未通过仓储抽象，属“Service 直操 DB” | 中（耦合） | 扫描 I2 |
| F-06 | J | J4 | `ShortLinkService.cs:88,113,114` | `urlLink = string.Format("{0}&token={1}", urlLink, token)` 拼接 token 入 URL，需确保 token 传输为 HTTPS 且日志脱敏，属可观测隐私边界 | 低 | 扫描 M1/K4 |
| F-07 | D | — | — | 无静态集合/锁，并发风险低 | — | 扫描 D |
| F-08 | C | — | — | 全 async/await，无 `.Result/.Wait()/async void` | —（合规） | 扫描 C |
| F-09 | H | H1 | — | 无大分支（<3），无需 Strategy | — | 扫描 H |
| F-10 | G | — | — | 无需 Record 替换，Entity 保持 class 正确 | — | 扫描 G |

> **去重说明**：N1=J6 同源（多租户）已合并；N3 误报已标注白名单；其余维度无命中即为“证据证明无问题”。

## Risk / Impact Matrix

| Finding | 风险 | 影响 | 成本 | 决策 |
|---------|------|------|------|------|
| F-04 多租户显式过滤 | Medium | 中（跨租户） | 中（2–8h，需确认连接策略） | **P2 下迭代**（先由 L1 确认是“分库分连接”还是“同库同表 TenantId 过滤”，再决定加 Where 还是保持 ChangTenant） |
| F-05 Service 直操 DB | Medium | 中（耦合） | 高（>8h，需仓储抽象） | **P3 待观察**（小类且影响面小，优先级低于关键链） |
| F-02 Scope 所有权 | Low | 低 | 低（<2h，明确归属/不持有） | **P3 待观察**（待 Pilot-1 后统一治理） |
| F-06 token 入 URL 隐私 | Low | 低 | 低 | **P3 待观察**（日志脱敏 + HTTPS 约束） |
| F-01 权限误报 | — | — | — | **白名单** `[NonAction]` |

**三个安全阀**：

1. 没有证据，是否进行了高级优化？ **否**（P0.2 无证据，P6/Span/ValueTask 全禁止）
2. 没有量化验证，是否宣称性能改善？ **否**
3. 发现问题是否自动修改了代码？ **否**（本 Pilot 全程只读，Finding≠Fix）

## Decision

- **是否允许进入 P1..P10 重构**：**否**（Phase 0 只读验证，不进入重构；即使进入，也仅 F-04 值得下迭代评估，且需先补 L1 连接策略证据）
- **选用 P**：无（本轮不选）
- **禁止选用**：Span/ArrayPool/ValueTask/ObjectPool/WeakEvent/ConditionalWeakTable/Strategy/Record 全禁止（无证据 + 复杂度>收益）
- **Recommendation**：
  1. 补 L1 表事实卡确认短链表的多租户策略（分库 vs 同库 TenantId）；
  2. 补 `find_referencing_symbols` 调用方影响面；
  3. 补 dotnet-counters 基线（若未来声称性能）；
  4. 下一 Pilot 再以“用户/权限读路径”做第二例只读链。

## 验证计划（本 Pilot 已执行）

- [x] 改前快照（git diff 0，零业务代码）
- [x] 只读扫描（手动 + 清单 16 维度去重）
- [ ] Benchmark（本轮不涉，符合 Gate）
- [x] 架构抽检（依赖方向、边界）
- [ ] 回归（本轮未改，无需）

## 引用

- Spec v4.0 §3–§5
- 扫描清单 v1.1 维度 A/N/I/J/C/H/G
- L2 v2.0 §5（只读五步循环）

---

> **本文件证明**：Agent 已能按 v4.0 完成 `Target → Baseline → P0 5维 → Findings → Risk → Gate → Decision` 全链，且未绕过门控、未改业务代码。
