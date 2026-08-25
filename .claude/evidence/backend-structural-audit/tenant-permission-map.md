# Backend Structural Audit — Tenant & Data Permission Dependency Map

**日期**：2026-08-25 ｜ 方法：grep 实测 + D1 A0 记录继承

## 1. ITenantFilter 挂靠点（全仓 12 文件）

| 文件 | 角色 |
|------|------|
| `common/JNPF.Common/Contracts/EntityBase.cs` | 实体基类契约（租户字段） |
| `common/JNPF.Common/Contracts/TenantEntityBase.cs` | 租户实体基类契约 |
| `common/JNPF.Common.Core/Manager/DataBase/DataBaseManager.cs` | **会话工厂（租户过滤生效点）** |
| `common/JNPF.Common.Core/Manager/Tenant/TenantManager.cs` | **租户解析（隐式上下文来源）** |
| `visualdata/JNPF.VisualData.Entitys/Entity/Visual*Entity.cs`（8 个） | 实体声明 |

**特征**：挂靠点高度集中（2 Manager + 2 契约 + 1 模块实体）——S2 抽象租户语义面小、可控；但**过滤依赖 AsSugarClient 会话隐式生效**（无显式参数传递），抽象时若改显式传递必须保持全链语义。

## 2. 数据权限条件传递链（两条独立路径）

| 路径 | 构造点 | 消费点 | 特征保护 | S2 风险 |
|------|--------|--------|---------|---------|
| **A（VisualDev 列表）** | `UserManager.GetCondition<T>`（CC42，Append 纯函数已拆分+33 特征） | `RunService.cs` L199 | ✅ D1.5 特征 33/33 + 签名契约 | 低（已锁定） |
| **B（Order 等）** | `UserManager.GetConditionAsync<T>`（CC60）+ `GetDataConditionAsync`（CC60）→ `AppendTokenStrategy` + `ConditionStrategyRegistry` | `OrderService`（独立路径，与 Append 零关联，D1 A0 已证） | ❌ **零特征测试** | **高（未保护）** |

## 3. 不变量（S2 抽象绝不能丢失）

1. **枚举数值契约**：`ConditionalType/WhereType` 的 int 数值是序列化硬契约（D1.5 特征锁定）
2. **匿名对象形态**：`{ Key, Value = { FieldName, FieldValue, ConditionalType } }` 属性名是反序列化契约
3. **NotIn 尾部守卫**：`"null"` 字符串 + 空串 IsNot 双条款（Q11 保真）
4. **首条 whereType 序列**：In/NotIn 的 And/Or 序列规则（D1.5 特征锁定）
5. **租户过滤**：ITenantFilter 经会话隐式生效，跨库查询（GetTenantSqlSugarClient CC29）走 DataBaseManager 显式租户
6. **路径 B 语义**：GetConditionAsync/GetDataConditionAsync 产出形态与 A 同构但消费独立——S2 前必须补特征保护

## 4. 审计结论

- 租户挂靠面：小且集中（可抽象）
- 数据权限：路径 A 已保护；**路径 B 未保护（P0-2）**——S2 前需按 D1 同款协议（特征金标准）补保护
