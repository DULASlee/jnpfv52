# NG-0 证据 2/11 — 数据库关系图（DB-2）

**来源**：DB-1 实测 + 代码侧调用链（OrderService 三表 Join、UserManager 权限链、SA 物化区）+ FK 清单（`db-fks.tsv`）

## 1. 总体现状：隐式关系单体

- **289 表中 275 表零外键**；仅 sa_* 13 表有 14 个 FK（自成小岛）
- 关系由**命名约定 + 应用层 Join + 前端渲染**承载：
  - `f_creator_user_id` → base_user.f_id（无 FK）
  - `f_organize_id` → base_organize.f_id（无 FK）
  - `f_role_id` → base_role.f_id（无 FK）
  - `f_parent_id` → 同表自引用（base_organize/base_dictionary_data 等）
- **推论：数据库不是约束源，应用层（SqlSugar 查询+命名）是唯一关系契约**——这使「按领域拆库」没有数据库侧障碍（无 FK 可拆），但也没有数据库侧保障（拆错即断）

## 2. 核心实体与聚合

| 聚合 | 核心表 | 卫星表（命名约定关联） | 关系形态 |
|------|--------|----------------------|---------|
| **Identity 用户** | base_user（66 列） | base_organize/base_role/base_position/base_group | 用户→组织/角色/岗位为**字符串列引用**（f_organize_id/f_role_id/f_position_id），非中间表 |
| **租户** | zx_sys_db | zx_sys_config/zx_system_db | 注册表模型（无租户-用户关联表，租户挂在 base_user.f_tenant_id） |
| **权限授权** | base_authorize（2553 行） | base_module/base_module_button/base_module_column/base_module_form/base_module_link/base_module_scheme | 授权记录（ObjectId/ItemId/ItemType）→ 模块树；**数据权限方案在 base_module_scheme（ModuleDataAuthorizeScheme）** |
| **工作流** | flow_task（flow 18 表） | flow_task_operator（555）/flow_event_log/wform 51 表单表 | 任务/操作者/事件日志 |
| **表单/低代码** | base_visualdev_*（BASE 106 内） | mt* 5 运行时表 / base_dictionary_data | 元数据驱动 + 运行时动态表 |
| **AI 原生化** | ai_ir_events/ai_entity_field/ai_* 8 | sa_* 13（**唯一 FK 家族**）/inte_* 2/kg_* 2 | FK 链：sa_business_process→sa_dfd→sa_scope 等 |
| **文件** | base_file | base_signature/base_signature_user（无 PK） | 文件元数据 |
| **消息/日志** | base_message/base_sys_log/base_api_log | SYS_EVENT_OUTBOX_MESSAGE | 事件出箱表（消息基础设施） |
| **演示业务** | WM_*/WH_*/ext_* | Order 相关（OrderService 三表 Join） | 业务表 |

## 3. 跨模块共享表（隐式共享——微服务化最大风险点）

| 共享表 | 被谁读/写 | 风险 |
|--------|----------|------|
| **base_user** | 所有模块 Join（OrderService 查用户姓名；流程查操作者；日志查创建人） | 反规范化读取（用户名/账号冗余到业务表） |
| base_dictionary_data | 所有表单渲染/查询 | 字典缓存挂靠 |
| base_module | 权限校验+菜单+数据权限 scheme 定位 | 权限核心 |
| base_authorize | 所有数据权限入口（GetCondition 双路径） | 权限核心 |
| base_file | 附件/头像/签名 | 文件引用 |
| base_sys_log/base_api_log | 审计/运维 | 日志 |

## 4. 隐式关系清单（需在 Next 显式化）

1. `f_creator_user_id`/`f_last_modify_user_id` → base_user（全库 21+16+ 小写风格遍布）——**审计引用隐式**；
2. `f_organize_id` → base_organize（base_user + 业务表）——**组织引用隐式**；
3. `f_tenant_id` 三风格 → zx_sys_db.id——**租户引用隐式**；
4. `f_parent_id` 自引用（组织/字典/菜单树）；
5. `f_dictionary_type_id` → base_dictionary_type（base_dictionary_data）；
6. `flow_task_operator.task_id` → flow_task（无 FK）；
7. `mt*` 动态表 ↔ base_visualdev 元数据——**运行时表无注册关系**；
8. Order 业务：`OrderEntity.CreatorUserId → base_user`、`OrderEntity.Id → FlowTaskEntity.Id`（OrderService.cs:84 三表 Join 实测）。

## 5. 关系图结论（Next 决策输入）

| 事实 | Next 含义 |
|------|----------|
| 275 表零 FK | **拆库无数据库侧阻力**；但关系契约必须从代码提取（本图 §4 为第一版） |
| base_user 被全模块 Join | Identity 域若独立成服务，**用户名/账号读取必须 API 化或快照化**（REDEFINE） |
| 权限引用遍布（f_creator_user_id 等） | 审计列引用是**跨域隐形耦合**——Next 考虑「审计快照」而非 Join |
| sa_* 有 FK | AI 原生化区是**唯一领域自治样板**（可作 Next 模块化参照） |
| 动态表 mt* 无注册 | 低代码运行时表管理必须设计**显式注册/租户隔离**（REDEFINE） |
