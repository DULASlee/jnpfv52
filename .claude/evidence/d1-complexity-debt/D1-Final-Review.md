# D1 Final Review — 复杂度基线技术债拆分重构战役终审

**日期**：2026-08-25
**战役**：D1（5 个历史存量 CC≥30 方法拆分重构，立项于 Task 3.4 裁决 A）
**结论**：**建议正式关闭 D1**（待人工裁决；S2 数据访问抽象闸门保持关闭）

---

## ① 原始 DoD 已达成项

| # | DoD 项 | 标准 | 实测 | 证据 |
|---|--------|------|------|------|
| 1 | 门面复杂度 | ≤10/12/10/12/8 | **10/5/7/11/8 全部达标** | `d1-complexity-audit.txt`（CyclomaticComplexityWalker 同源测量） |
| 2 | 基线销账 | D1 战役登记 5 条目移除 | 5/5 移除 | `complexity-baseline.json` diff（台账现存 456e2d6b 初始 119 条历史存量条目，非本战役范围） |
| 3 | 行为等价 | 不变量逐条特征用例 + 既有套件全绿 | 5 组金标准 32/19/25/13/33 = **122 特征用例**；终态套件 **274+51+92+23=440 全绿** | 各方法测试输出（逐字比对） |
| 4 | 签名契约 | 5 公开方法签名逐字不变 | 5/5 断言通过 | `D1SignatureContractTests`（Rewrite/Bind/ApplyMapRules/ValidateBatchUnique/Append） |
| 5 | 路由 | 1077/107 与 s0 zero-diff | **d11/d12/d13/d14/d15 全部 DIFF_EXIT=0** | `d1-complexity-debt/d1[1-5]-routes.txt` |
| 6 | CI | 无豁免 0 错 | **ExitCode=0，0 error**（5 次销账后均复验 + 终态复验） | `/p:CI_BUILD=true` 构建输出 |
| 7 | 看板 | 卡片销账入「已解决」 | ✅ 已移入（附 5 销账哈希） | `TECH-DEBT.md` |
| 8 | 调用方零改动 | 5 方法签名不变 | ✅（唯一调用点 UserManager.GetCondition<T> 未触碰；ApplyMapRules/ValidateBatchUnique 调用方零改动） | 提交 diff |
| 9 | 分析器自证 | 23/23 绿 | **23/23** | `JNPF.Analyzers.Tests`（基线生成器默认跳过，未改写台账） |
| 10 | 专项加跑 | Common 全量 + 架构测试 | Common **51/51**；Architecture **92/92** | 终态测试输出 |

## ② 原始 DoD 未达成但经人工批准接受的偏差

原始 DoD：所有拆分后的子方法 Cyclomatic Complexity ≤14。
2026-08-25 人工裁决：接受偏差、不启动 D1.7、不重新拆分、不重登/修改已销账基线、不降低 JNPF009 `<30` 门禁。登记详表见规格 §9.1：

| 方法 | 子方法 | 实测 CC | 原始目标 |
|------|--------|---------|---------|
| Rewrite | `EmitSymbolClause` | 17 | ≤14 |
| Rewrite | `EmitExpandedInNotInClauses` | 17 | ≤14 |
| Bind | `TryBindMainSelector` | 15 | ≤14 |
| Bind | `ResolveUserDefault` | 16 | ≤14 |
| Bind | `BindTableChildren` | 24 | ≤14 |

另 4 项超规格逐成员目标但满足 ≤14 原则（单独记录、非阻塞，规格 §9.2）：`TryEmitEmptyClause` 10（≤8）、`EmitLikeClause` 6（≤5）、`ApplyChildMainCross` 13（≤12）、`AppendInNotIn` 14（≤12）。

**接受偏差的工程判断**（规格 §9.4）：5 个门面全部达标；全部方法 <30 强门禁；行为等价已锁定；继续拆分仅服务二级数字目标且存在职责碎片化风险。

## ③ 后续结构优化债务（非 D1 未知风险）

- §② 表中 5 个子方法（EmitSymbolClause/EmitExpandedInNotInClauses/TryBindMainSelector/ResolveUserDefault/BindTableChildren）登记为**后续结构优化债务**（规格 §9.3，TECH-DEBT 卡片关闭条件标注）；处置另立战役，特征保护已就绪，禁止机械拆分。
- 既有怪异行为 **Q1-Q11** 全程保真未修（规格 §2 各方法不变量表登记），修复需另立行为变更战役独立评审。

## ④ 本战役已完全销账的 5 个复杂度基线

| 方法 | 登记 CC | 拆分结构 | 销账提交 | 特征金标准 |
|------|--------|---------|---------|-----------|
| `ListSuperQueryInputRewriter.Rewrite` | 84 | 门面+符号直映表+10 专项发射器 | `e84e96dd` | 32/32 |
| `FieldBindDefaultValueHelpers.Bind` | 82 | 门面+值对象+5 解析器+子表递归 | `be3d372e` | 19/19 |
| `FlowFormDataMapper.ApplyMapRules` | 37 | 门面+守卫器+3 形态发射器 | `c24c6253` | 25/25 |
| `ImportFirstVerifyHelpers.ValidateBatchUnique` | 35 | 门面+4 子方法+错误串接器 | `717929ff` | 13/13 |
| `GetConditionQueryClauseAppender.Append` | 31 | 门面+直映表+3 特判器 | `bae2bf36` | 33/33 |

5 条冻结条目（Task 3.4 登记，均归因 456e2d6b）已全部从 `complexity-baseline.json` 移除，无豁免 CI 通过为强制证明。

---

## 完整提交链（均可独立 revert）

| 提交 | 内容 |
|------|------|
| `b4529577` | Task 3.4 收尾 + D1 立项（5 条基线冻结登记 + 规格/计划 v1.0） |
| `1aafbdf1` | S1 Gate（路由/契约/特征/CI 四验证） |
| `ddd3e88e` | 规格+计划 v1.1（自包含完整版） |
| `e84e96dd` | **D1.1 Rewrite 销账**（5 文件标准结构） |
| `be3d372e` | **D1.2 Bind 销账**（5 文件标准结构） |
| `f5f4a953` | Wave 1 证据包（规格 I3 实测修订 + 路由快照） |
| `c24c6253` | **D1.3 ApplyMapRules 销账**（5 文件标准结构） |
| `717929ff` | **D1.4 ValidateBatchUnique 销账**（5 文件标准结构） |
| `8ba13c98` | Wave 2 证据包（规格 I5 实测修订 + 路由快照） |
| `bae2bf36` | **D1.5 Append 销账**（5 文件标准结构） |
| `ea2592f0` | Wave 3 证据包（规格 §2.5 三项实测补登 + 路由快照） |
| `（本次）` | **D1.6 收尾终审**（偏差登记 + Final Review + 证据归档） |

销账提交文件边界：每个 = 业务源码 1 文件 + 特征测试 1 文件 + 签名契约 1 文件 + 基线(-8 行) + 看板(2 行)，无混入、无跨方法交叉。

## 未解决风险声明

1. **无 D1 战役内未解决风险**：偏差项已人工批准并登记为债务（§②/③），不视为战役未知风险；
2. Q1-Q11 怪异行为为既有行为保真，修复未排期（另立战役）；
3. S2 数据访问抽象未启动（闸门关闭，等待独立裁决）；
4. 并行战役（DLL 化 v2.3、`.agents/`、session 噪声）全程隔离，无交叉污染。

## 最终结论

**D1 建议正式关闭。** 五项重构全部完成、5 条冻结基线全部销账、行为等价锁定、门禁体系健康。战役遗留事项（5 项结构优化债务 + Q1-Q11 行为修复）均以债务形式显式登记，不阻塞关闭。

---

*本 Review 与规格 §9（终审偏差登记）、实施计划 §6（DoD 汇总）、TECH-DEBT 卡片（已解决区）、evidence/d1-complexity-debt/（审计+路由证据）一致。*
