# Backend Structural Audit — Legacy Behavior Registry

**日期**：2026-08-25 ｜ 分级：L0 禁改 / L1 S2 必须有回归测试 / L2 已知异常暂不修 / L3 未来变更候选

## 1. D1 战役登记（Q1-Q11 + 施工期发现）

| ID | 行为 | 来源 | 当前测试 | 稳定 | S2 风险 | 级别 |
|----|------|------|---------|------|---------|------|
| Q1 | bool vs string 恒 false 比较 | D1.1（Rewrite） | ✅ D1.1 特征 | 是 | 低 | L0 |
| Q2 | COMSELECT/CURRORGANIZE `"]` 后缀 | D1.1（Rewrite） | ✅ D1.1 特征 | 是 | 低 | L0 |
| Q3 | notIn IsNot 双条款 | D1.1（Rewrite） | ✅ D1.1 特征 | 是 | 低 | L0 |
| Q4 | 子表借父 TABLE 项 multiple | D1.2（Bind） | ✅ D1.2 特征 | 是 | 低 | L0 |
| Q5 | 子表无 custom 校验 | D1.2（Bind） | ✅ D1.2 特征 | 是 | 低 | L0 |
| Q7 | `-` 分隔键豁免置 null | D1.3（ApplyMapRules） | ✅ D1.3 特征 | 是 | 低 | L0 |
| Q8 | N-1 条重复错误 | D1.4（ValidateBatchUnique） | ✅ D1.4 特征 | 是 | 低 | L0 |
| Q9 | ContainsValue 粗匹配（仅预筛） | D1.4（实测修正） | ✅ D1.4 特征 | 是 | 低 | L0 |
| Q10 | 非列表回退 Equal | D1.5（Append） | ✅ D1.5 特征 | 是 | 低 | L0 |
| Q11 | `"null"` 字符串 IsNot | D1.5（Append） | ✅ D1.5 特征 | 是 | 低 | L0 |
| E1 | 空 between 抛 ArgumentOutOfRangeException | D1.5 实测 | ✅ D1.5 特征 | 是 | 低 | L2 |
| E2 | 真实嵌套数组抛 JsonReaderException | D1.5 实测 | ✅ D1.5 特征 | 是 | 低 | L2 |
| E3 | QueryType.Contains 无 case → default 不追加 | D1.5 A0 补登 | ✅ D1.5 特征 | 是 | 低 | L0 |
| E4 | USERSSELECT 单选无 `--user` 后缀 | D1.2 实测修正 | ✅ D1.2 特征 | 是 | 低 | L0 |

## 2. 本审计新发现（S1-Final 扫描 + P0-B 深化）

| ID | 行为/事实 | 来源 | 测试 | 级别 |
|----|----------|------|------|------|
| E5 | `GetConditionAsync`/`GetDataConditionAsync` 路径 B 链路 | P0-B 规格 | ✅ **43/43 特征（2026-08-25 已锁定）** | **L1 → 已解除**（P0-B 完成后升级为 L0 保护基线） |
| E-PB1 | `GetDataConditionAsync` L812 `x.EnCode.Equals("jnpf_alldata")` 无 null 保护（EnCode=null → NRE） | P0-B 源码核对 | 登记（不修） | L2 |
| E-PB2 | 尾部 DenyAll 差异：A 含 `!Roles.Any()`，B 仅 `resourceList.Count==0` | P0-B 源码核对 | 登记（不修） | L2 |
| E-PB3 | `GetConditionalModel(Between)` 无 case → 空模型 | P0-B 实测 | ✅ D-7 特征 | L2 |
| E-PB4 | `GetDataConditionAsync` L810 `.In(it=>it.Id, roleAuthorizeList)` 传匿名对象列表（vs A 传 ItemId） | P0-B 源码核对 | 登记（不修） | L3 |
| E6 | 台账 8 条已自然降级 <30（GetSelector 258→20 等） | complexity-inventory §3 | —（门禁仍保护） | L3（可销账观察） |
| E7 | `ConfigController.cs`（zxdev）1 处 `$"SELECT` 插值 SQL | data-access-coupling §3 | ❌ | L3 |

## 3. 原则声明

- 不允许因行为「看起来错误」自动修复；L0 项任何阶段禁止改变；
- L1 项（E5）为 S2 前置条件，按 D1 五步协议补特征保护（另立小战役或并入 S2 设计前奏）。

## 4. 与 D1 Final Review 的关系

Q1-Q11 与 E1-E4 已在 D1 战役以特征金标准锁定（122 用例）；本表为 S1-Final 统一登记（含分级），供 S2 准入 Gate D 引用。
