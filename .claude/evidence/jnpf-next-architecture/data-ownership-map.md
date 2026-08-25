# NG-0 证据 4/11 — 数据 Ownership 地图（DB-4）

**核心问题**：每张核心表「谁拥有它」——所有权不清 = 微服务化后数据库仍是隐形单体。

## 1. Ownership 判定方法

1. 表前缀聚类（DB-1 §4）+ C# 实体模块归属（174 实体文件 → modularity 模块）；
2. 写路径归属（代码侧：该实体被哪个模块 Service 增删改——以模块目录为准）；
3. 共享读例外显式标注（§3）。

## 2. 核心表 Ownership 表

| 域（候选） | 核心表 | Owner（现状模块） | 写路径证据 |
|-----------|--------|------------------|-----------|
| **Identity** | base_user（66 列） | system | UserService/UserManager（登录/CRUD/审计字段全写） |
| Identity | base_organize/base_role/base_position/base_group | system | OrganizeService/RoleService/PositionService/GroupService |
| **Tenant** | zx_sys_db/zx_sys_config | zxdev（私有化） | zx 模块租户注册 |
| **Permission** | base_authorize（2553） | system | AuthorizeService（授权写） |
| Permission | base_module*（module/button/column/form/link/scheme） | system | ModuleService 系列（菜单/数据权限方案） |
| Permission | base_data_authorize*（数据权限规则） | system | DataAuthorizeService |
| **Workflow** | flow_task/flow_task_operator/flow_event_log/flow_* 18 | workflow | FlowTaskService/FlowEngineService |
| Workflow-Form | wform_* 51 | workflow（表单引擎） | 表单实例写 |
| **Form/LowCode** | base_visualdev_*（模型/表单/功能/在线开发） | visualdev | VisualDevService 系列 |
| Form/LowCode | mt* 5 动态表 | visualdev（运行时） | 运行时业务写（租户应用数据） |
| **File** | base_file | system（文件服务） | FileService |
| **Message** | base_message | message | MessageService |
| **Log/Audit** | base_sys_log/base_api_log | system（日志） | Log 写（全模块读） |
| **AI 原生化** | ai_ir_events/ai_entity_field/ai_* 8 | inteAssistant | IR/实体字段服务（ai_entity_field=字段唯一源） |
| AI-SA | sa_* 13 | inteAssistant（C# SaMaterializer 物化） | SA 物化写 |
| AI-Infra | inte_*/kg_*/EVAL | inteAssistant | 知识/评估 |
| **演示业务** | WM_*/WH_* 39 | extend（Order/Warehouse） | OrderService/WarehouseService |
| 演示业务 | ext_* 19 | extend | WorkLog 等 |
| 字典 | base_dictionary_data/base_dictionary_type | system | DictionaryService（全模块读） |
| 门户 | base_portal* | system | PortalService |

## 3. 共享读表（有 Owner，但被多方读——需 API 化/快照化）

| 表 | Owner | 读者 | Next 处置 |
|----|-------|------|----------|
| base_user | Identity | **全部业务模块**（Join 姓名/账号/组织） | 读取 API 化 + 审计快照（REDEFINE） |
| base_dictionary_data | system | 全部表单渲染/查询 | 字典缓存/API（KEEP 语义，API 化） |
| base_module/base_authorize | Permission | 全部数据权限入口（GetCondition 双路径） | 权限快照/缓存（REDEFINE） |
| base_file | File | 全部附件场景 | 文件 API + 签名 URL（KEEP） |
| base_sys_log/base_api_log | Log | 运维/审计 | 独立存储（Aspire 关联评估） |

## 4. 无主/争议表（Ownership 冲突——Next 必须裁决）

| 表 | 问题 |
|----|------|
| blade_* 8 表 | BladeX 遗留——DEPRECATE 候选（无主） |
| BASE_STUDIO_MENU_BAK_20260617 | 备份表入库——REMOVE 候选 |
| base_signature/base_signature_user | 无 PK 无索引——归属 File or Identity 待裁 |
| flow_form_authorize | 无 PK——Workflow×Permission 交叉 |
| mt* 动态表 | 属于租户应用还是平台？——**低代码运行时表的租户/平台双归属**是 Next 核心裁决 |
| BASE_TENANT_GLOSSARY/BASE_TENANT_INDUSTRY（0 行） | AI 行业术语——空表观察 |
| sa_validation_log/undo_log/domain/data | AI/工具杂散 |

## 5. Ownership 结论（Next 决策输入）

1. **五大候选域已清晰**：Identity / Permission / Workflow / Form-LowCode / AI；Tenant/File/Message/Log 为平台服务；
2. **base_user 是最强跨域耦合源**——「谁拥有用户数据」= Identity，其余域必须经 API/快照，禁止直连（DB-2 §5 一致）；
3. **动态表 mt\* 归属**决定低代码域能否独立成服务——建议「租户应用表注册表 + 平台托管」模型（REDEFINE）；
4. **演示业务（WM/WH/ext）是验证迁移策略的理想沙盘**——低风险域，可先做 Modular Monolith 内拆分试点（§migration-strategy 关联）；
5. 数据所有权裁决表将回填 domain-candidates（每个候选领域的 Data Ownership 维度）。
