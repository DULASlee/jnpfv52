# JNPF-Next 数据库架构设计规格 v1.0（NG-0 产物 1/5）

**日期**：2026-08-25 ｜ **依据**：NG-0 证据 1-4（database-inventory / relationship-map / query-hotspots / data-ownership-map）
**状态**：设计规格（只读，未实施任何数据库变更）

## 1. 现状事实（已实测）

| 项 | 值 | 来源 |
|----|-----|------|
| 表/列 | 289 / 6134 | 证据 1 |
| PK | nvarchar 217（77%）——字符串 ID 主流，GUID 仅 1 | 证据 1 |
| FK | 14（仅 sa_*）——275 表零外键 | 证据 1/2 |
| 租户列 | 三风格：f_tenant_id 187 / F_TenantId 21 / tenant_id 10（sa_*） | 证据 1 |
| 软删/审计 | F_DeleteMark 24；审计字段非标配 | 证据 1 |
| 大字段 | 161 表含 nvarchar(max)（JSON 无 schema 校验） | 证据 1 |
| 租户主数据 | zx_sys_db（无 base_tenant） | 证据 1 §3 |

## 2. Next 数据库设计裁决

### 2.1 PK 策略（REDEFINE）
- **裁决**：保留字符串 ID 为兼容层，但新 Schema 主键采用 **Snowflake（bigint）**，业务键（code）加唯一约束；
- 理由：字符串 ID 无索引/空间优势（证据 1）；bigint 36 表已有先例；迁移映射层承担兼容（Legacy API 不变）。

### 2.2 租户契约（REDEFINE）
- 单一 `tenant_id`（bigint 或 nvarchar(50) 统一）列契约，全平台业务表标配；
- 三风格列迁移列为 W8（证据 9），迁移前保持 P0-C 冻结语义；
- 连接级切库（AsTenant）保留（KEEP——多库租户能力）。

### 2.3 实体基类契约（REDEFINE）
统一审计列：`tenant_id / created_by / created_at / modified_by / modified_at / deleted_by / deleted_at / deleted_flag`（Next 命名），替代现状 f_creator_user_id 等混合风格。

### 2.4 FK 策略（REDEFINE）
- **不补建物理 FK**（275 表零 FK 是现状事实，补建是迁移风险）；
- 改以**契约层保证**：Next 数据访问 API 强制关系映射注册（显式关系注册表——见数据访问规格 §3）；
- sa_* 家族 FK 保留为样板（KEEP）。

### 2.5 JSON 字段（REDEFINE）
- 关键 JSON（f_form_data/f_property_json/f_draft_data）→ SQL Server JSON 约束或独立文档表（按 D5 裁决）；
- 通用配置 JSON 允许保留，但必须带 JSON Schema 校验注册。

### 2.6 动态表 mt*（REDEFINE）
- 显式注册表：`app_table_registry`（table_name/schema/tenant/app/version/status）；
- 运行时 DDL 走平台托管通道（D5 域内）。

### 2.7 弃迁（DEPRECATE/REMOVE）
blade_* 8 表（DEPRECATE）、BASE_STUDIO_MENU_BAK_20260617（REMOVE）、text/ntext 15 列（REMOVE）、base_signature*（DEPRECATE）。

## 3. 查询/索引方向（NG-1 验证）

1. 权限快照缓存（替换三连查）——DB-3 热点 1；
2. 审计快照（业务表冗余 created_by 姓名）替代 Join base_user——DB-2 风险；
3. 日志族（base_sys_log/base_api_log/BASE_AI_CALL_LOG）独立存储——写放大隔离；
4. 运行期 DMV/慢查询采样在 NG-1 完成（本阶段不臆断索引方案）。

## 4. 待裁决（NG-1 输入）

| # | 事项 | 建议 |
|---|------|------|
| DB-D1 | PK Snowflake 迁移范围（全表 or 新表 only） | 新表 only + 兼容层 |
| DB-D2 | 动态表注册表归属（D5 域内 or 平台级） | D5 域内（Form/LC） |
| DB-D3 | base_user 66 列拆分粒度 | NG-1 原型出拆分规格 |
| DB-D4 | 租户列统一是否需先补跨租户泄漏测试 | 是（P0-C 输入） |
