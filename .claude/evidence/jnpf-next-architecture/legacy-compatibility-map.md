# NG-0 证据 8/11 — Legacy Compatibility Map（KEEP / REDEFINE / DEPRECATE / REMOVE）

**原则**：Next 不机械复制 Legacy 异常行为。逐项裁决：KEEP（保真迁移）/ REDEFINE（重定义语义）/ DEPRECATE（废弃停用）/ REMOVE（清理）。

## 1. 行为/契约资产裁决

### 1.1 权限契约（D1/P0-B 锁定资产）

| 资产 | 测试 | 裁决 | 理由 |
|------|------|:----:|------|
| 路径 A `GetCondition` 语义（D1.5 33 特征） | ✅ 33/33 | **REDEFINE** | 语义保留进「权限评估 API」，但实现重写（Next 权限快照）；**33 特征为等价基线** |
| 路径 B `GetConditionAsync`（P0-B 43 特征） | ✅ 43/43 | **REDEFINE** | 同上；消费者 OrderService 迁移时以特征验证新 API |
| `ConditionalModel` 序列化契约（{Key,Value={FieldName,FieldValue,ConditionalType}}） | ✅ | **KEEP** | JSON 形态是跨层硬契约，Next 数据访问 API 继续使用 |
| `ConditionalType`/`WhereType` 枚举数值 | ✅ | **KEEP** | 数值不可变（持久化/序列化依赖） |
| Q-PB1（and+isCurrentRole 首条 Or 怪异） | ✅ | **REMOVE** | 语义偏移（首条 Or 与组内 And 意图矛盾）——Next 按 AND/OR 意图重定义，**不复制** |
| E-PB3（Between 无 case 空模型） | ✅ | **REMOVE** | 空模型是缺陷——Next 明确 Between 处理（或显式不支持） |
| E-PB1（EnCode null NRE） | — | **REMOVE** | Next 数据权限方案 EnCode 可空契约显式化 |
| E-PB2（DenyAll 条件差异） | — | **REDEFINE** | Next 统一尾部守卫语义（授权空 → DenyAll 唯一规则） |
| E-PB4（In 传匿名对象列表） | — | **REMOVE** | Next 显式 ItemId 列表 |
| ReplaceOp 九符号映射 | ✅ | **KEEP** | 符号→QueryType 映射是表单 DSL 契约（前端依赖） |
| GetConditionalModel 23 case 映射 | ✅ | **KEEP**（除 Between） | 类型映射语义保留；Between 按 REDEFINE |
| 短路层（Admin 空/AllowAll NoEqual0/DenyAll Equal0） | ✅ | **KEEP** | 全权限/无权限语义正确，保真迁移 |

### 1.2 租户契约（P0-C 冻结区）

| 资产 | 裁决 | 理由 |
|------|:----:|------|
| 连接级切库（AsTenant） | **KEEP** | 多库租户是私有化核心能力（D2 域保留） |
| 列级租户过滤（ITenantFilter 挂靠） | **REDEFINE** | 三风格租户列（DB-1 §3）→ 单一 `tenant_id` 契约；过滤机制显式化（P0-C 规格输入） |
| 租户注册表（zx_sys_db） | **KEEP** | 注册表模型正确（D2 聚合） |

### 1.3 领域/数据资产

| 资产 | 裁决 | 理由 |
|------|:----:|------|
| 主键字符串 ID（nvarchar 217/77%） | **REDEFINE** | Next 决策 GUID/雪花/保留字符串——迁移成本裁决（证据 1 §6） |
| 审计字段非标配 | **REDEFINE** | Next 统一 EntityBase 契约（created/modified/delete/tenant 全标配） |
| JSON nvarchar(max) 字段 | **REDEFINE** | 关键 JSON（f_form_data/f_property_json）schema 化或 JSON 约束 |
| 动态表 mt* | **REDEFINE** | 显式注册表 + 平台托管（D5） |
| base_user 66 列 | **REDEFINE** | 按聚合拆分（账号/资料/安全/偏好）——Domain 层拆分 |
| blade_* 8 表 | **DEPRECATE** | BladeX 遗留——不迁移 |
| BASE_STUDIO_MENU_BAK_20260617 | **REMOVE** | 备份表入库 |
| base_signature*（无 PK） | **DEPRECATE** | 归属待裁，暂不迁移 |
| text/ntext 15 列 | **REMOVE** | 遗留类型，Next 不用 |
| SYS_EVENT_OUTBOX_MESSAGE | **KEEP** | 事件化种子（D8） |
| sa_* FK 家族 | **KEEP** | 领域自治样板 |

## 2. 不复制清单（Legacy 怪异 → Next 显式契约）

| # | Legacy 行为 | Next 处置 |
|---|------------|----------|
| 1 | Q1-Q11（D1 战役登记的 11 项怪异） | 逐项按 KEEP/REDEFINE/REMOVE 裁决（Q1-Q6 守卫语义 KEEP；Q7-Q11 异常路径 REMOVE 或显式化）——**详见规格《JNPF-Next-租户权限架构规格》§5** |
| 2 | E1-E4（D1 实测怪异） | E1 空 between AOORe → REDEFINE（显式校验）；E2 嵌套数组 JsonReaderException → REDEFINE（拍平仅字符串编码）；E3 Contains 无 case → REMOVE（显式映射）；E4 USERSSELECT 单选 → KEEP（DSL 契约） |
| 3 | E-PB1~4 | 见 §1.1 |
| 4 | 权限三连查（authorize→module→scheme） | REDEFINE：权限快照（DB-3 §5） |
| 5 | 条件注入四段叠加（DataBaseManager L563-566） | REDEFINE：Next 数据访问 API 的显式 Pipeline（规则/查询/超级查询/权限四段 → 契约化） |

## 3. 兼容策略

1. **迁移期双跑**：Legacy API 与 Next API 并存，特征测试（现有 483 用例）作为等价判据；
2. **KEEP 项直接复制语义**；REDEFINE 项必须出设计规格（进入 NG-1 原型验证清单）；DEPRECATE/REMOVE 项不进迁移计划；
3. 本映射是《JNPF-Next-租户权限架构规格》与《JNPF-Next-数据访问架构规格》的输入。
