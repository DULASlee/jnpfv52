# JNPF-Next 数据访问架构规格 v1.0（NG-0 产物 3/5）

**日期**：2026-08-25 ｜ **依据**：P0-A 边界规格（229 耦合面）+ P0-B 路径 B 契约（43 特征）+ NG-0 证据 1/3/8
**状态**：设计规格（只读；S2 保持 BLOCKED——本规格是其 Next 形态的输入）

## 1. 输入资产（已锁定）

| 资产 | 状态 | 用途 |
|------|------|------|
| P0-A 语义分类（403→229 耦合面；E 174 实体冻结/R 111/B 118/I/S 冻结） | ✅ | 改造面清单 |
| P0-B 路径 B 契约（43 特征+12 不变量） | ✅ | 权限条件生产等价基线 |
| 路径 A 契约（D1.5 33 特征） | ✅ | 同上 |
| `ConditionalModel` 序列化契约 + 枚举数值 | ✅ | Next API 沿用（KEEP） |
| 条件注入四段链（DataBaseManager L563-566） | ✅ | REDEFINE 对象 |

## 2. 架构形态：Producer → Adapter → Consumer

```text
Producer（条件生产）
  ├── 权限评估 API（Next 重写 GetCondition 双路径——33+43 特征等价）
  ├── 查询 DSL（ReplaceOp/GetConditionalModel 映射——KEEP）
  └── 超级查询/数据规则（四段链输入）
        ↓ 条件契约（ConditionalModel 序列化形态——KEEP）
Adapter（数据访问适配层）
  ├── 显式关系注册表（替代零 FK 隐式关系——DB 规格 §2.4）
  ├── 租户过滤管线（tenant_id 契约化——P0-C 冻结区语义）
  ├── 条件注入 Pipeline（规则/查询/超级查询/权限四段 → 契约化）
  └── 多库方言适配（7 方言资产冻结登记——P0-A §4.2）
        ↓
Consumer（查询执行）
  ├── Queryable/分页/排序（统一契约：ToPagedList 等）
  └── 日志族独立存储（写放大隔离）
```

## 3. 核心设计裁决

| # | 裁决 | 内容 |
|---|------|------|
| DA-1 | **契约层关系映射** | 不补物理 FK；新建关系注册表（entity/relation/cardinality/tenant-aware），数据访问 API 强制校验 |
| DA-2 | **权限评估 API** | 双路径语义合入单一评估 API；33+43 特征为等价基线；快照缓存（替换三连查） |
| DA-3 | **条件 Pipeline 契约化** | 四段叠加显式化（DataRule→Query→SuperQuery→Permission 顺序不可变——P0-B 不变量 Q-PB1 等按 REMOVE/REDEFINE 裁决后重建） |
| DA-4 | **审计快照** | 业务表冗余 created_by 快照（姓名/账号），替代 Join base_user（DB-2/3） |
| DA-5 | **动态表注册** | app_table_registry 托管 DDL/查询（D5 域内） |
| DA-6 | **多库兼容保留** | 方言模板资产冻结（P0-A）；Adapter 以策略模式承载 |
| DA-7 | **参数化强制** | 动态 SQL 一律参数化（hook L0 语义延续）；零插值（zxdev 1 处已登记） |

## 4. 迁移等价判据

1. **KEEP 项**（序列化契约/枚举/短路层/ReplaceOp/23-case 映射）：逐字等价——现有 483 用例直接复用；
2. **REDEFINE 项**（权限评估 API/条件 Pipeline）：语义等价 + 新契约测试双标——影子读比对（W3/W6）；
3. 每个域迁移前补该域权限特征（成本风险登记 §2 触发信号）。

## 5. 禁止项

- 不复制 Q-PB1/E-PB3 等怪异（Compatibility Map 证据 8）；
- 不机械逐文件重构 B 类 118 文件（P0-A D-A1 裁决：S2 设计期分层适配）；
- 不引入 SqlSugarFilterable 之外的参数化旁路。

## 6. 待裁决（NG-1/2 输入）

| # | 事项 | 建议 |
|---|------|------|
| DA-D1 | 权限评估 API 是否先于 Modular Monolith 骨架做原型 | 是（NG-1 原型 1） |
| DA-D2 | 关系注册表数据源（从代码提取 or 手建） | 从 8 类隐式关系清单（证据 2 §4）手建 v1 |
| DA-D3 | 条件 Pipeline 是否以 C# 管线替换 JSON 字符串传递 | 是（REDEFINE） |
| DA-D4 | 审计快照字段命名 | tenant_id/created_by/created_at 契约（DB 规格 §2.3） |
