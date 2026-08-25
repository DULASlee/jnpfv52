# NG-0 证据 5/11 — 领域候选与边界（10 维度）

**方法**：不沿用 modularity 项目目录为边界——以 DB ownership（证据 4）+ 调用链（DB-2/3）+ API/权限/租户/工作流多维证据重新聚类。每个候选领域回答 10 个维度。

## 0. 聚类依据（从数据看）

- **DB 侧**：前缀聚类（DB-1 §4）+ 共享读表（DB-2 §3）+ 写路径归属（DB-4 §2）
- **代码侧**：S1-Final 审计（403 ORM 文件模块分布/权限双路径/DataBaseManager 条件注入链）+ P0-B 契约（GetCondition 双路径）
- **不采用的边界**：modularity 项目目录（`inteAssistant` 116 文件横跨 Studio/Skills/IR/SA——项目目录是组织产物，不是领域）

## 1. 候选领域表（10 维度）

### D1 Identity（用户/组织/角色/岗位/组）
| 维度 | 内容 |
|------|------|
| Business Capability | 账号生命周期/认证主体/组织架构/成员关系 |
| Aggregate | User（66 列）；Organize 树；Role；Position；Group |
| Data Ownership | base_user/base_organize/base_role/base_position/base_group（全写路径在 system） |
| Transaction Boundary | 用户创建+组织分配（单库事务足够）；登录尝试计数（乐观） |
| Dependencies | 依赖 Tenant（f_tenant_id）；被全域读（快照/API） |
| API | 登录/OAuth/用户 CRUD/组织树/成员查询 |
| Events | UserCreated/UserOrganizeChanged/UserDisabled |
| Tenant | 强租户（f_tenant_id 列过滤；登录时 Tenant 上下文注入） |
| Permission | 自身是权限主体（角色/组织）；**不做业务授权**（授权在 D3） |
| Migration | **最高优先独立**——所有域依赖；先做 API 化+快照 |

### D2 Tenant（租户注册/连接）
| 维度 | 内容 |
|------|------|
| Business Capability | 租户注册/库注册/连接配置/系统级配置 |
| Aggregate | Tenant（zx_sys_db 5 行）；zx_sys_config |
| Data Ownership | zx_sys_db/zx_sys_config/zx_system_db |
| Transaction Boundary | 租户开通（注册+建库+种子）必须原子——**跨库事务或补偿** |
| Dependencies | 无业务依赖；被 Identity/AI 引用 |
| API | 租户注册/连接测试/系统配置 |
| Events | TenantProvisioned/TenantSuspended |
| Tenant | **租户源头**（不自身带租户列） |
| Permission | 平台管理员 |
| Migration | 独立服务候选（注册表小、边界清晰）；**连接级切库（AsTenant）语义必须保留** |

### D3 Permission（授权/模块/数据权限）
| 维度 | 内容 |
|------|------|
| Business Capability | 菜单/按钮/列授权；数据权限方案；权限评估 |
| Aggregate | Authorize 记录；Module 树（module/button/column/form/link/scheme）；DataAuthorizeScheme |
| Data Ownership | base_authorize/base_module*/base_data_authorize* |
| Transaction Boundary | 授权变更原子（对象×模块×动作批量）；**读取走快照缓存** |
| Dependencies | 依赖 Identity（ObjectId 用户/角色/组织） |
| API | 授权 CRUD；**权限评估 API（GetCondition 双路径的 Next 形态）** |
| Events | AuthorizationChanged（失效缓存/通知全域） |
| Tenant | 强租户（菜单/授权按租户隔离） |
| Permission | 自身是授权模型；数据权限语义（条件生产）是核心 |
| Migration | 权限快照/缓存先行；GetCondition 双路径契约（P0-B 43 特征）是迁移等价基线 |

### D4 Workflow（流程/任务/表单实例）
| 维度 | 内容 |
|------|------|
| Business Capability | 流程定义/实例/任务分派/审批流转/流程事件 |
| Aggregate | FlowEngine（定义）；FlowTask（实例）+ TaskOperator（555）+ FlowEventLog；wform_* 51 表单实例 |
| Data Ownership | flow_* 18 + wform_* 51 |
| Transaction Boundary | 任务流转（审批+下一节点+通知）**必须原子**——流程域内事务 |
| Dependencies | 依赖 Identity（操作者）；依赖 Form（表单数据）；依赖 Permission（流程权限） |
| API | 流程定义/发起/审批/待办/抄送 |
| Events | TaskCreated/TaskCompleted/ProcessEnded（**天然事件源**） |
| Tenant | 强租户（flow 18 全 f_tenant_id） |
| Permission | 流程发起权限+任务操作权限（跨 Permission 域） |
| Migration | 事件化改造高价值（工作流是 Event-Driven 最佳候选）；表单实例与流程实例的耦合要裁 |

### D5 Form / Low-Code（表单/模型/在线开发/运行时）
| 维度 | 内容 |
|------|------|
| Business Capability | 数据模型/表单设计/在线开发/代码生成/运行时数据 |
| Aggregate | VisualModel（元数据）；Form（设计态）；Runtime Data（mt* 动态表） |
| Data Ownership | base_visualdev_* + mt* 动态表 + base_codegen* |
| Transaction Boundary | 模型变更+运行时表结构变更（DDL）——**元数据事务与数据事务分离** |
| Dependencies | 依赖 Identity（审计）；依赖 Permission（模块授权）；运行时数据被业务读 |
| API | 模型 CRUD/表单设计器/运行时数据 API（GetKeyData CC160 链） |
| Events | ModelPublished/FormDeployed（触发 DDL/缓存重建） |
| Tenant | **动态表租户归属待裁**（mt* 双归属——DB-4 §4） |
| Permission | 表单级权限/字段级权限（设计态+运行态） |
| Migration | 元数据注册表化（动态表显式注册）；**GetListQuerySql CC113 链是最大迁移面** |

### D6 Data / Dictionary（数据字典/门户/公共数据）
| 维度 | 内容 |
|------|------|
| Business Capability | 字典/枚举/门户/公共基础数据 |
| Aggregate | DictionaryType+Data（树）；Portal |
| Data Ownership | base_dictionary_data/base_dictionary_type/base_portal* |
| Transaction Boundary | 单表事务 |
| Dependencies | 无强依赖（被全域读） |
| API | 字典 CRUD/缓存查询/门户配置 |
| Events | DictionaryChanged（缓存失效） |
| Tenant | 字典按租户隔离（f_tenant_id） |
| Permission | 基础数据权限 |
| Migration | **缓存/API 化先行**——字典是最容易 API 化的共享读（DB-2 §3） |

### D7 File（文件/附件）
| 维度 | 内容 |
|------|------|
| Business Capability | 文件上传/下载/预览/签名 |
| Aggregate | File（元数据+存储） |
| Data Ownership | base_file/base_signature*（归属待裁） |
| Transaction Boundary | 单文件事务；存储与元数据一致性（先存后记/补偿） |
| Dependencies | 无 |
| API | 上传/下载/签名 URL/头像 |
| Events | FileStored/FileDeleted |
| Tenant | 文件按租户隔离（路径/元数据） |
| Permission | 文件访问授权（业务域委托） |
| Migration | **独立服务最强候选**（无跨域 Join、边界最清晰）；签名 URL + 对象存储是 Next 形态 |

### D8 Message / Notification（站内信/通知）
| 维度 | 内容 |
|------|------|
| Business Capability | 站内信/系统通知/事件出箱 |
| Aggregate | Message；EventOutbox（SYS_EVENT_OUTBOX_MESSAGE） |
| Data Ownership | base_message + SYS_EVENT_OUTBOX_MESSAGE |
| Transaction Boundary | 消息投递（AtLeastOnce 出箱模式） |
| Dependencies | 被 Workflow/AI/业务发事件 |
| API | 消息 CRUD/已读/推送 |
| Events | MessageSent（已事件化雏形——出箱表存在） |
| Tenant | 按租户 |
| Permission | 消息接收者授权 |
| Migration | **出箱表已是事件化种子**——Event Bus 演进的现成挂靠点 |

### D9 Log / Audit（日志/审计）
| 维度 | 内容 |
|------|------|
| Business Capability | 系统日志/API 日志/操作审计 |
| Data Ownership | base_sys_log/base_api_log |
| Transaction Boundary | 无（异步写） |
| Dependencies | 无 |
| API | 日志查询/导出 |
| Events | 无（消费方） |
| Tenant | 日志按租户 |
| Permission | 运维权限 |
| Migration | 独立存储候选（写放大——DB-3 §4）；Aspire 评估关联（OpenTelemetry） |

### D10 AI（InteAssistant：IR/实体字段/SA/知识/评估）
| 维度 | 内容 |
|------|------|
| Business Capability | AI 对话/IR 事件/字段元数据/SA 物化/知识库/评估管线 |
| Aggregate | IrEvent（3780）；EntityField（824，**字段唯一源**）；SaDoc（13 表 FK 家族）；Knowledge |
| Data Ownership | ai_* 8 + sa_* 13 + inte_* 2 + kg_* 2 + EVAL |
| Transaction Boundary | SA 物化（confirm 后 C# 写九表）原子；IR 事件追加 |
| Dependencies | 依赖 Identity（用户）；依赖 Form（字段语义）；调用 Llm 外部 |
| API | IR 事件/实体字段/SA/评估（现有 IDynamicApiController 面） |
| Events | **IrEvent 已是事件表**（3780 行追加）——事件化天然 |
| Tenant | F_TenantId 风格（21 表含 ai/sa/inte）——**与业务区风格不一致待统一** |
| Permission | AI 功能权限+字段级权限 |
| Migration | **领域自治度最高**（sa_* 有 FK）；但依赖 base_user/base_module 读——API 化后即可独立 |

### D11 Report（报表/数据大屏）
| 维度 | 内容 |
|------|------|
| Business Capability | 报表设计/执行/大屏 |
| Data Ownership | report_* 3 |
| Transaction Boundary | 无 |
| Dependencies | 读全域（报表即读模型） |
| API | 报表执行/导出 |
| Events | 无 |
| Tenant | report 3 有 f_tenant_id |
| Permission | 报表授权 |
| Migration | 读模型候选（CQRS 读侧）；依赖 Data 访问 API |

### D12 Demo Business（extend：订单/仓库/工作日志）
| 维度 | 内容 |
|------|------|
| Business Capability | 演示业务（Order/Warehouse/WorkLog） |
| Aggregate | Order+BillDetail；Material+CheckBill；WorkLog+Share |
| Data Ownership | WM_* 21 + WH_* 18 + ext_* 19 |
| Transaction Boundary | 单据+明细原子（单库事务） |
| Dependencies | 依赖 Identity（创建人）；依赖 Workflow（审批）；依赖 Permission（数据权限——OrderService L83 实测） |
| API | 业务 CRUD（现 extend 模块） |
| Events | 单据事件（迁移沙盘验证用） |
| Tenant | 全 f_tenant_id |
| Permission | **数据权限双路径消费方**（OrderService 是路径 B 唯一消费者） |
| Migration | **迁移沙盘**（低风险、依赖面全、可完整验证 Identity/Workflow/Permission API 化） |

## 2. 领域边界裁决要点

1. **12 候选域**中 D1-D5+D10 为核心域；D6-D9+D11 平台服务；D12 沙盘；
2. **不沿用项目目录**：inteAssistant（项目 116 文件）跨 AI 域（D10）+ Studio 基础设施——按领域应拆分；visualdev 项目横跨 D5 设计态/运行态——按事务边界拆；
3. 每个域的「Transaction Boundary」都落在**单库事务内**（现状无跨库事务需求）——这直接指向 **Modular Monolith 首选**（§modular-monolith-vs-microservices 详述）；
4. 域间依赖最重的是 **Identity 读取**（全域）与 **Permission 评估**（全域）——两者 API 化/快照化是解耦的前提顺序；
5. D10 AI 的自洽度（FK 家族+事件表）证明「数据自治 → 服务自治」路径可行——是 Next 的样板参照。
