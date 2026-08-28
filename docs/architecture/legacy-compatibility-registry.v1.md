# Legacy Compatibility Registry v1.0（遗留兼容性登记册）

**版本**：v1.0 定稿 ｜ **日期**：2026-08-26
**任务**：jnpf-v52-goal / T0.4 ｜ **政策依据**：MASTER 总体实施计划 S0 安全网；NG-0 Legacy Compatibility Map 升格
**四态语义**：KEEP（保真迁移）／ REDEFINE（重定义语义，须出规格）／ DEPRECATE（废弃停用，不迁移）／ REMOVE（清理）
**上游工件**：[platform-asset-inventory.v1](./platform-asset-inventory.v1.md) · [data-ownership-profile.v1](./data-ownership-profile.v1.md) · 行为特征考卷（tests/characterization）

## §1 权限契约裁决

| 资产 | 测试基线 | 裁决 | 理由 |
|------|------|:----:|------|
| GetCondition 语义（D1.5 33 特征） | ✅ 33/33 | REDEFINE | 语义保留进权限评估 API，实现重写为权限快照 |
| GetConditionAsync（P0-B 43 特征） | ✅ 43/43 | REDEFINE | 同上；消费者迁移以特征验证 |
| ConditionalModel 序列化契约 | ✅ | KEEP | JSON 形态是跨层硬契约 |
| ConditionalType/WhereType 枚举数值 | ✅ | KEEP | 数值不可变（持久化依赖） |
| ReplaceOp 九符号映射 | ✅ | KEEP | 表单 DSL 契约（前端依赖） |
| GetConditionalModel 23 case 映射 | ✅ | KEEP* | *Between 除外 → REDEFINE |
| 短路层（Admin 空/AllowAll/DenyAll） | ✅ | KEEP | 语义正确，保真迁移 |
| Q-PB1（and+isCurrentRole 怪异 Or） | ✅ | REMOVE | 语义偏移，不复制 |
| E-PB3（Between 无 case 空模型） | ✅ | REMOVE | 缺陷行为 |
| E-PB1（EnCode null NRE） | — | REMOVE | 契约显式化 |
| E-PB2（DenyAll 尾部守卫） | — | REDEFINE | 统一守卫语义 |
| E-PB4（In 匿名对象列表） | — | REMOVE | 显式 ItemId 列表 |

## §2 租户契约裁决（P0-C 冻结区）

| 资产 | 裁决 | 理由 |
|------|:----:|------|
| 连接级切库 AsTenant | KEEP | 多库租户是私有化核心能力 |
| 列级租户过滤 ITenantFilter | REDEFINE | 三风格租户列 → 单一 tenant_id 契约（R4 红线关联） |
| 租户注册表 zx_sys_db | KEEP | 注册表模型正确 |

## §3 领域/数据资产裁决

| 资产 | 裁决 | 理由 / 处置去向 |
|------|:----:|------|
| 主键字符串 ID（217/77%） | REDEFINE | 迁移成本裁决随域设计逐表定 |
| 审计字段非标配 | REDEFINE | EntityBase 全标配契约 |
| JSON nvarchar(max) 关键字段 | REDEFINE | f_form_data/f_property_json schema 化 |
| 动态表 mt* 5 | REDEFINE | 显式注册表+平台托管（ownership-profile §7 待裁#1） |
| base_user 66 列 | REDEFINE | 按聚合拆分；四方写者收敛见 ownership-profile §4 |
| text/ntext 15 列 | REMOVE | 遗留类型不入 Next |
| SYS_EVENT_OUTBOX_MESSAGE | KEEP | 事件化种子 |
| sa_* FK 家族 | KEEP | 领域自治样板 |
| BASE_STUDIO_MENU_BAK_20260617 | REMOVE | 备份表入库（inventory FREEZE/LEGACY 一致） |
| base_signature* 无 PK 双表 | DEPRECATE | 归属待裁（ownership-profile §7 待裁#2），暂不迁移 |

## §4 修正项（v1 对 NG-0 的更正，以强溯源为准）

| NG-0 原结论 | v1 更正 | 依据 |
|---|---|---|
| blade_* 8 表 = BladeX 遗留 → DEPRECATE | **blade_visual* 8 表 → KEEP**：实为 visualdata 模块大屏运行时表（P1_LOWCODE_RUNTIME / MANDATORY / ENTER），code_owner=visualdata，含真实数据 | ng1b provenance-matrix：entity_mapped=Y + creation_source 命中 visualdata + db_rows>0 |

## §5 不复制清单（Legacy 怪异 → Next 显式契约）

| # | Legacy 行为 | Next 处置 |
|---|---|---|
| 1 | Q1-Q11 战役怪异 | Q1-Q6 守卫语义 KEEP；Q7-Q11 REMOVE 或显式化（详见租户权限架构规格 §5） |
| 2 | E1-E4 实测怪异 | E1 REDEFINE / E2 REDEFINE / E3 REMOVE / E4 KEEP(DSL) |
| 3 | 权限三连查 authorize→module→scheme | REDEFINE：权限快照 |
| 4 | 条件注入四段叠加（DataBaseManager L563-566） | REDEFINE：显式 Pipeline 四段契约化 |

## §6 兼容策略与登记纪律

1. 迁移期双跑：Legacy API 与 Next API 并存，行为特征考卷（30 条已入 CI）作为等价判据；
2. KEEP 直接复制语义；REDEFINE 必须先出设计规格再动手；DEPRECATE/REMOVE 不进迁移计划；
3. FREEZE 层 LEGACY 表的逐表退役动作以本登记册为准，执行前回写 inventory disposition；
4. 新发现的 legacy 异常行为：先在考卷补特征用例锁定现状，再走本登记册裁决，禁止未登记就修。
