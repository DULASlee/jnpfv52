# NG-0 证据 1/11 — 数据库 Schema 全量盘点（DB-1）

**来源**：`ZXAF_V1_DevTest1`（SQL Server，`(local)\SQLEXPRESS`）2026-08-25 只读实测 ｜ **配套数据**：`db-tables.tsv`（289 表全清单）/`db-index-stats.tsv`/`db-audit-cols.tsv`/`db-type-dist.tsv`/`db-prefix-clusters.tsv`/`db-nopk.tsv`/`db-noidx.tsv`/`db-fks.tsv`

## 1. 总览

| 指标 | 值 | 说明 |
|------|-----|------|
| 表 | **289** | 含 5 张 mt* 低代码动态表 + 1 张备份表 |
| 列 | **6134** | 平均 21 列/表（base_user 66 列最大） |
| PK | 283（6 表无） | 无 PK：zx_system_db/base_signature/base_signature_user/blade_visual_glob/flow_form_authorize/BASE_STUDIO_MENU_BAK_20260617 |
| FK | **14（仅 sa_\* 家族）** | **275 表零外键——关系全部隐式** |
| 索引 | 598（非堆）/唯一 460 | 无索引表 6 张（与无 PK 同 6 张） |
| 租户列 | **219 表（76%）** | **三风格并存**（详见 §3） |
| 软删列 | F_DeleteMark 24 + f_delete_mark（小写，base 族） | 非标配 |

## 2. 主键/类型契约（Next 必须重新决策）

| 观察 | 数据 | 含义 |
|------|------|------|
| **主键 nvarchar 217（77%）** | bigint 36/int 21/varchar 7/nchar 1/**GUID 仅 1** | JNPF 业务主键 = 字符串 ID（F_ID）；GUID 不是主流 |
| 列类型 nvarchar 3003（49%） | int 1006/datetime 631/bigint 564/decimal 161/bit 133 | 字符串+时间+int 主导；**text/ntext 15 列遗留** |
| 大字段 161 表（56%） | F_FileJson 20/f_property_json 11/validation_errors 9/tags 9/f_form_data 3/f_draft_data 4 | **JSON 以 nvarchar(max) 存储、无 schema 校验**——领域状态与配置混存 |
| 审计字段非标配 | F_CreatorTime 24/F_ModifyTime 18（大写）；f_creator_time（小写 base 族更多） | base_user 全审计标配（含 f_delete_*+f_tenant_id），但多数表只有部分 |
| 命名大小写混乱 | `f_biz_system_Id`（尾部大写 Id）vs `f_zx_system_id`；`f_tenant_id` vs `F_TenantId` | 同一库三种命名风格 |

## 3. 租户模型（P0-C 遗留问题在 DB 层的实锤）

| 风格 | 表数 | 分布 | 说明 |
|------|-----:|------|------|
| `f_tenant_id`（小写） | 187 | base 85/wform 51/ext 19/flow 18/blade 8/report 3/… | 业务表主体（含 base_user） |
| `F_TenantId`（大写） | 21 | BASE 10/ai 6/sa 3/inte 2 | AI 原生区 |
| `tenant_id`（纯小写） | 10 | **全部 sa_\*** | SA 物化区 |

- **租户主数据不在 base_tenant**（不存在此表）——在 **`zx_sys_db`（5 行，id/name/filename/status/comment）**，即私有化租户注册表；`zx_sys_config` 2 行
- 推论：**租户隔离在 DB 层靠列过滤（ITenantFilter 挂靠）+ 连接级切库（DataBaseManager.AsTenant）双机制**；列过滤有 32% 表无租户列（无租户语义 or 漏配待查）

## 4. 前缀聚类（表→模块雏形）

| 前缀 | 表数 | 域 | 备注 |
|------|-----:|----|------|
| BASE/base | 106 | 平台底座 | 系统+权限+文件+消息+日志+低代码元数据混存 |
| wform | 51 | 工作流表单 | 全部 f_tenant_id |
| WM/WH | 21+18 | 演示业务（物料/仓库） | 含种子数据 |
| ext | 19 | 扩展演示 | 全部 f_tenant_id |
| flow | 18 | 工作流 | |
| sa | 13 | **AI SA 物化区** | **唯一有 FK 的家族** + tenant_id 风格 |
| blade | 8 | BladeX 遗留 | f_tenant_id |
| ai | 8 | AI 原生 | F_TenantId |
| mt*（5）/domain/data/undo | 8 | 低代码动态表 | 运行时建表 |
| zx | 3 | 私有化 | 含租户注册 zx_sys_db |
| inte/kg/EVAL/report/Demo/student 等 | 14 | AI/报表/演示 | 杂散 |

## 5. 数据量（行数 Top）

base_province 47512 / base_sys_log 12615 / ai_ir_events 3780 / base_province_atlas 3210 / base_authorize 2553 / WM_BillDetail 1629 / BASE_AI_CALL_LOG 1502 / base_message 1229 / base_dictionary_data 897 / ai_entity_field 824 / flow_task_operator 555

## 6. Next 决策要点

1. **PK 策略**：字符串 ID 是否保留（Legacy 兼容）还是迁移 GUID/雪花——决定迁移成本；
2. **租户列统一**：三风格 → 单一 `tenant_id` 契约（REDEFINE）；
3. **FK 补建 or 保持隐式**：275 表零 FK 是关系图最大障碍（DB-2 详述）；
4. **JSON 字段 schema 化**：f_form_data/f_property_json 等 → 文档/列式 or JSON Schema 校验；
5. **审计字段标准化**：非标配 → 统一 EntityBase 契约；
6. **blade_* 8 表 / BASE_STUDIO_MENU_BAK 备份表**：DEPRECATE 候选。
