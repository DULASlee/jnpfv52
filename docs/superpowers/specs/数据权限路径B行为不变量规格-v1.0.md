# 数据权限路径 B 行为不变量规格 v1.0（S1-P0-B）

**日期**：2026-08-25
**目标**：将 `GetConditionAsync/GetDataConditionAsync`（路径 B）行为契约**锁死为 S2 数据访问抽象前的不可破坏基线**——只锁定、不重构、不改变权限语义。
**状态**：S1 特征金标准 **43/43 全绿**（JNPF.Tests.Common 全套 94/94）；无任何实现修改。

---

## 1. 调用路由不变量

| # | 不变量 | 证据 |
|---|--------|------|
| R-1 | 接口签名逐字不变：`IUserManager.GetConditionAsync<T>(string moduleId, string primaryKey="f_id", bool isDataPermissions=true, string tableNumber="")` / `GetDataConditionAsync<T>(string moduleId, string primaryKey, bool isDataPermissions=true)`，均返回 `Task<List<IConditionalModel>>` | R1 反射断言 |
| R-2 | 唯一外部消费者：`OrderService.GetList`（extend）——`GetConditionAsync<OrderListOutput>(menu.Id, "F_ID", true, "a.")`，产出进 SqlSugar `.Where(authorizeWhere)` | OrderService.cs:83/85 |
| R-3 | `GetConditionAsync` 与 `GetCondition`（路径 A）边界：A 经 Append 纯函数（D1.5 锁定，33 特征）；B 经 `AppendTokenStrategy` → `ConditionStrategyRegistry` → `TokenConditionStrategy` → `ConditionClauseAppender.AppendIds`（本规格锁定，43 特征）——**两路径独立，S2 抽象不得合并语义** | D1 Final Review + 本规格 |
| R-4 | fieldName 构造：A=`tableNumber+primaryKey`（如 "a."+"F_ID"）；B=`BindTable.Field`（有 BindTable 时）——前缀透传契约 | UserManager.cs:550/832 |

## 2. 权限语义不变量

| # | 不变量 | 用例 |
|---|--------|------|
| P-1 | `AppendIds` 空 Ids → 不追加条款 | A-1 |
| P-2 | **Q-PB1（怪异保真）**：单条 and + isCurrentRole=true → Key=**Or**（语义偏移，现状如此） | A-2 |
| P-3 | 单条 and + 非当前角色 → Key=And；or → 恒 Or | A-3/A-4 |
| P-4 | 多条 and + Equal 系 → 首条 And/Or（按 isCurrentRole），其余 Or | A-5/A-6 |
| P-5 | 多条 NotEqual/NotIncluded → 首条后全部 And（isCurrentRole 翻转后） | A-7/A-8/A-9 |
| P-6 | 多条 Included/其他 + or → 恒 Or | A-10 |
| P-7 | `ctx.IsCurrentRole` 首条后回写 false（多角色循环状态机） | A-11 |
| P-8 | 字段名/值/ConditionalType 逐字透传 | A-12 |
| P-9 | Registry 六 token（@userId/@userAraSubordinates/@organizeId/@organizationAndSuborganization/@branchManageOrganize/@branchManageOrganizeAndSub）全部可解析且委托 AppendIds | B-1..6 |
| P-10 | `GetConditionalModel` 映射：Contains→Like；Equal/NotEqual/LessThan/LessThanOrEqual/GreaterThan/GreaterThanOrEqual ×（Double→double 转换/Int32→int 转换/默认无转换）；In→In；Included→Like；NotIn→NotIn；NotIncluded→NoLike | D-1..D-6/D-8 |
| P-11 | **E-PB3（怪异保真）**：`Between` 无 case → **空 ConditionalModel**（FieldName/FieldValue=null） | D-7 |
| P-12 | `ReplaceOp` 九符号映射（==/between/>/</<>/>=/<=/like/notLike）→ QueryType 名；未知符号原样透传 | E-1/E-2 |

## 3. 租户不变量

| # | 不变量 | 说明 |
|---|--------|------|
| T-1 | 路径 B **无显式租户参数**——租户隔离完全依赖 ITenantFilter 会话层（AsSugarClient 隐式上下文） | 挂靠点 12 文件（P0-A 实测）；P0-C 固化 |
| T-2 | S2 抽象后：条件生产（本链路）与租户过滤（会话层）**必须保持分离语义**，不得在抽象层合并或丢失 | 本规格 + P0-C |

## 4. 序列化契约

| # | 契约 | 证据 |
|---|------|------|
| S-1 | 匿名对象形态 `{ Key=int(WhereType), Value={ FieldName, FieldValue, ConditionalType=int } }`（JsonToConditionalModels 反序列化硬契约） | A-2..A-12 JSON 断言 |
| S-2 | 枚举数值：WhereType.And/Or、ConditionalType 全部以 int 序列化（与 D1.5 路径 A 同款） | 同 A 组 |
| S-3 | 短路层（Admin 空集/AllowAll NoEqual'0'/DenyAll Equal'0'）+ FieldValueConvertFunc（string/int 形态） | 既有 W1 + C-1 |
| S-4 | SQL 消费形态：`a.F_ID` → SqlSugar 渲染 `[a].[F_ID]`（表别名.列名） | R-3 |

## 5. 异常/怪异登记（不修，逐项分级）

| ID | 行为 | 级别 | 说明 |
|----|------|------|------|
| Q-PB1 | and+isCurrentRole 首条 Key=Or | **L0**（必须保真） | P-2 锁定 |
| E-PB1 | `GetDataConditionAsync` L812 `x.EnCode.Equals("jnpf_alldata")` 无 null 保护（EnCode=null → NRE） | **L2**（已知异常暂不修） | 与 A 路径 `"jnpf_alldata".Equals(x.EnCode)` 保护写法不同 |
| E-PB2 | 尾部 DenyAll 条件差异：A=`resourceList.Count==0 \|\| !Roles.Any()`，B=仅 `resourceList.Count==0` | **L2** | 行为差异登记，不统一 |
| E-PB3 | `GetConditionalModel(Between)` 返回空模型 | **L2** | P-11 锁定 |
| E-PB4 | `GetDataConditionAsync` L810 `.In(it=>it.Id, roleAuthorizeList)` 直接传匿名对象列表（vs A 传 ItemId 列表）——SqlSugar 行为未逐字验证 | **L3**（未来验证/变更候选） | 差异登记 |

## 6. 数据库消费契约（Producer → Adapter → Consumer）

```text
Producer：UserManager.GetConditionAsync/GetDataConditionAsync（本规格锁定，43 特征）
  ↓ List<IConditionalModel>（序列化契约 S-1..S-3）
Consumer：OrderService.GetList → SqlSugar Queryable.Where(authorizeWhere)（S-4 形态）
  ↓
Adapter（S2 未来）：条件模型契约不变，适配层承载 ORM 差异 —— 本规格即为抽象层契约输入
```

## 7. 验收记录

| 项 | 结果 |
|----|------|
| S1 特征金标准 | **43/43**（新增 43：A 12/B 6/C 1/D 9/E 10/R 3/Q 0 并入） |
| JNPF.Tests.Common 全套 | **94/94** |
| 实现修改 | **零**（F1：2 处夹具修正——R3 SQL 渲染形态/D7 Between 空模型，均修用例不修实现） |
| 遗留 | E-PB1/2/3/4 登记不修；S2 前路径 B 已具等价基线 |
