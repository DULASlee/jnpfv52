# JNPF 数据责任与模块边界映射主表 v1.0

> **性质**：PHASE 3 收口产物 —— 把已完成的 289 表资产研究、157/132 平台资产识别、Provenance/证伪链、A–E 平台能力地图、低代码核心闭环、业务流程研究，收敛为一份可直接支撑下一阶段代码模块化设计的**单一主表**。
>
> **上游唯一总基线**：《JNPF 平台整体结构基线》v0.2（`docs/architecture/JNPF平台整体结构基线.md`）
> **本表不是**：Domain Design / 微服务划分 / 数据库重设计。Candidate Module 仅为候选，最终边界由人类裁决（基线 §五、§六.2）。
>
> **版本**：v1.0（2026-08-26）｜ **产出后即 STOP，等待人工批准**

---

## 0. 证据口径与局限声明（先读）

### 0.1 本次收口实际使用的证据源

| 编号 | 证据源 | 性质 | 可复算性 |
|---|---|---|---|
| EV-1 | backend 全量 `[SugarTable("...")]` 扫描：**174 条实体映射**（剔除 7 条测试夹具后 **167 条真实映射**，覆盖 165 张唯一表） | 代码事实 | ✅ 一次性扫描可复算 |
| EV-2 | `backend/web/数据库脚本.sql`（2.2MB init 脚本）：**228 张唯一建表** | DB 事实 | ✅ |
| EV-3 | 跨模块实体引用抽样（Identity 三实体被 12 个区域引用；FlowTask/FlowTemplate 被 app/engine/extend/visualdev 引用） | 代码事实 | ✅ |
| EV-4 | `[UnitOfWork]` 分布：system 30 / visualdev 7 / inteAssistant 5 / extend 3 / taskscheduler 1 / visualdata 1 / workflow 1 | 代码事实 | ✅ |
| EV-5 | 《平台整体结构基线》v0.2 §二/§三：A–E 能力地图、核心闭环、289→157/132、P0–PX 聚类结论、五条证伪链 | 文档结论 | 文档引用 |

### 0.2 局限（如实登记，不掩盖）

- **L1**：EV-5 的**逐表明细 CSV（NG-1A `platform-asset-classification.csv`、NG-1B `provenance-matrix.csv`）不在本仓库内**。因此"157 张平台表"的逐表名单无法在本仓直接核对。本表以「代码实体映射 ∪ 基线聚类结论」双源交叉得出，缺口见 NDH-01。
- **L2**：口径差：基线 289 张（DB 实测，含运行期 mt_\* 动态表）vs init 脚本 228 张。差集 ≈61 张主要为 mt\_\* 动态表、sa\_\*（dapper-first 运行时物化）与历史版本差异。**差集逐表对齐依赖 NDH-01，本表不猜测。**
- **L3**：Reader 维度为**抽样级证据**（EV-3），非全量 157×调用点矩阵；Writer 维度为实体归属模块，为强证据。

### 0.3 Status 判定规则（防"填表猜测"）

| Status | 判定条件 |
|---|---|
| `CONFIRMED` | EV-1 实体映射存在 **且** 能力归属有 EV-5 基线依据 |
| `EXCLUDED` | EV-5 已判非平台资产（P2 模板 / P3 Demo / P4 客户 / P6 遗留）——仅登记排除，不分析 |
| `UNKNOWN` | 证据不足（无实体 / 无文档语义 / 无法归类）——零猜测保留 |
| `NEEDS HUMAN DECISION` | 存在 ≥2 个合理归属方案，或基线明示延后裁决 |

---

## 1. 主表（范围内数据对象）

> 列：Table ｜ Asset Class ｜ Capability(A–E) ｜ Writer(现状写者=实体归属) ｜ Reader ｜ Business Flow(核心闭环位置) ｜ Transaction Boundary ｜ Candidate Module ｜ Evidence ｜ Status

### G1 身份域（A 平台基础能力 · 全域依赖根）

| Table | Asset Class | Cap | Writer | Reader | Flow | Tx Boundary | Candidate Module | Evidence | Status |
|---|---|---|---|---|---|---|---|---|---|
| BASE_USER | P0 平台 | A 身份 | system 模块 | **全域 12 区域引用**（EV-3） | A 基座层 | 同库事务（system UoW×30，EV-4） | JNPF.Identity | EV-1 UserEntity.cs | CONFIRMED |
| BASE_ORGANIZE | P0 | A 身份 | system | 全域（EV-3） | A 基座 | 同库 | JNPF.Identity | EV-1 OrganizeEntity.cs | CONFIRMED |
| BASE_ORGANIZE_RELATION | P0 | A 身份 | system | system/message/workflow 等 | A 基座 | 同库 | JNPF.Identity | EV-1 | CONFIRMED |
| BASE_ORGANIZE_ADMINISTRATOR | P0 | A 身份 | system | system | A 基座 | 同库 | JNPF.Identity | EV-1 | CONFIRMED |
| BASE_ROLE | P0 | A 身份 | system | 全域授权链 | A 基座 | 同库 | JNPF.Identity | EV-1 | CONFIRMED |
| BASE_POSITION | P0 | A 身份 | system | system/workflow | A 基座 | 同库 | JNPF.Identity | EV-1 | CONFIRMED |
| BASE_GROUP | P0 | A 身份 | system | system/authorize | A 基座 | 同库 | JNPF.Identity | EV-1 | CONFIRMED |
| BASE_USER_RELATION | P0 | A 身份 | system | system | A 基座 | 同库 | JNPF.Identity | EV-1 | CONFIRMED |
| BASE_USER_OLD_PASSWORD | P0 | A 身份 | system | system | A 基座 | 同库 | JNPF.Identity | EV-1 | CONFIRMED |
| BASE_SOCIALS_USERS | P0 | A 身份(三方登录) | system | oauth/system | A 基座 | 同库 | JNPF.Identity | EV-1 | CONFIRMED |
| BASE_SIGNATURE / BASE_SIGNATURE_USER | P0 | A 身份(签章) | system | system/workflow | A 基座 | 同库 | JNPF.Identity | EV-1 | CONFIRMED |
| BASE_SIGN_IMG | P0 | A 身份 | system | system | A 基座 | 同库 | JNPF.Identity | EV-1 | CONFIRMED |
| BASE_SYN_THIRD_INFO | P0 | A 身份(三方同步) | system | system | A 基座 | 同库 | JNPF.Identity | EV-1 | CONFIRMED |

### G2 授权域（A 授权 · GetCondition 双路径为迁移基线 [EV-5]）

| Table | Asset Class | Cap | Writer | Reader | Flow | Tx Boundary | Candidate Module | Evidence | Status |
|---|---|---|---|---|---|---|---|---|---|
| BASE_AUTHORIZE | P0 | A 授权 | system | 全域请求链 | A 基座 | 同库 | JNPF.Authorization | EV-1 | CONFIRMED |
| BASE_PERMISSION_GROUP | P0 | A 授权 | system | system | A 基座 | 同库 | JNPF.Authorization | EV-1 | CONFIRMED |
| BASE_COLUMNS_PURVIEW | P0 | A 授权(列权限) | system | system | A 基座 | 同库 | JNPF.Authorization | EV-1 | CONFIRMED |
| BASE_MODULE | P0 | A 授权(菜单) | system | 全域路由/菜单 | A 基座 | 同库 | JNPF.Authorization | EV-1 | CONFIRMED |
| BASE_MODULE_BUTTON / _COLUMN / _FORM / _LINK / _SCHEME / _AUTHORIZE | P0 | A 授权(资源细粒度) | system | system/前端契约 | A 基座 | 同库 | JNPF.Authorization | EV-1 | CONFIRMED（归属争议见 NDH-07） |
| BASE_MENU_BADGE | P0 | A 授权(菜单) | **inteAssistant** ⚠ | system | A 基座 | 同库 | JNPF.Authorization | EV-1 | **NEEDS HUMAN DECISION**（NDH-05：能力属 A，物理写者在 inteAssistant） |
| BASE_FOUNDER_AUTH_LOG | P0 | A 授权(审计) | **inteAssistant** ⚠ | inteAssistant | A 基座 | 同库 | JNPF.Authorization | EV-1 | **NEEDS HUMAN DECISION**（NDH-05） |

### G3 租户域（A 租户 · 连接级切库语义保留 [EV-5]）

| Table | Asset Class | Cap | Writer | Reader | Flow | Tx Boundary | Candidate Module | Evidence | Status |
|---|---|---|---|---|---|---|---|---|---|
| zx_system_db | P0（真身注册表；`zx_sys_db` 为遗留副本——证伪链 [EV-5]） | A 租户 | zxdev 模块 | 连接管理层 | A 基座 | 独立注册库语义 | JNPF.Tenancy | EV-1 + EV-5 §3.2 | CONFIRMED（模块归属见 NDH-02） |
| zx_sys_config | P0 | A 租户/全局配置 | zxdev | 连接管理层 | A 基座 | 同上 | JNPF.Tenancy | EV-1 | CONFIRMED（同 NDH-02） |

### G4 字典与平台配置（A 字典与公共数据 · 最易 API 化的共享读 [EV-5]）

| Table | Asset Class | Cap | Writer | Reader | Flow | Tx Boundary | Candidate Module | Evidence | Status |
|---|---|---|---|---|---|---|---|---|---|
| BASE_DICTIONARY_TYPE | P0 | A 字典 | system | 全域（EV-3 含 DictionaryDataEntity 引用 12 区） | A 基座 | 同库 | JNPF.Dictionary | EV-1 | CONFIRMED |
| BASE_DICTIONARY_DATA | P0 | A 字典 | system | 全域 | A 基座 | 同库 | JNPF.Dictionary | EV-1 | CONFIRMED |
| BASE_COMMON_WORDS | P0 | A 公共数据 | system | system/前端 | A 基座 | 同库 | JNPF.Dictionary | EV-1 | CONFIRMED |
| BASE_COMMON_FIELDS | P0 | A 公共数据 | system | codegen/前端 | A 基座 | 同库 | JNPF.Dictionary | EV-1 | CONFIRMED |
| BASE_PROVINCE / _ATLAS | P0 | A 公共数据 | system | 前端级联 | A 基座 | 只读为主 | JNPF.Dictionary | EV-1 | CONFIRMED |
| BASE_SYS_CONFIG | P0 | A 配置 | system | 全域启动读取 | A 基座 | 同库 | JNPF.PlatformConfig | EV-1 | CONFIRMED |
| BASE_SYSTEM | P0 | A 配置 | system | 全域 | A 基座 | 同库 | JNPF.PlatformConfig | EV-1 | CONFIRMED |
| BASE_ADVANCED_QUERY_SCHEME | P0 | A 公共数据(用户查询方案) | system | visualdev/前端 | C 运行时辅助 | 同库 | JNPF.Dictionary | EV-1 | CONFIRMED |

### G5 平台基础设施（A 基础配置/调度 + D 打印）

| Table | Asset Class | Cap | Writer | Reader | Flow | Tx Boundary | Candidate Module | Evidence | Status |
|---|---|---|---|---|---|---|---|---|---|
| BASE_TIME_TASK / BASE_TIME_TASK_LOG | P0 | A 调度 | taskscheduler | scheduler 宿主 | 平台运行基础设施 | UoW×1（弱，EV-4） | JNPF.Scheduling | EV-1 | CONFIRMED |
| JOBCLUSTER / JOBTRIGGERS | P0 | A 调度(Quartz 自带 schema) | taskscheduler | Quartz.NET | 基础设施 | Quartz 内部 | JNPF.Scheduling | EV-1 | CONFIRMED（是否算平台资产见 NDH-08） |
| JobDetails | P0 | 同上 | taskscheduler | Quartz.NET | 基础设施 | 同上 | JNPF.Scheduling | EV-1 | CONFIRMED |
| BASE_PRINT_TEMPLATE / BASE_PRINT_LOG | P0 | D 打印 | system | system/前端 | D 服务 | 同库 | JNPF.Printing | EV-1 | CONFIRMED |
| BASE_SCHEDULE / _LOG / _USER | P0 | D 日程 | system | system/app | D 服务 | 同库（历史上曾发现无 UoW 缺陷，见过期存档 T1） | JNPF.Scheduling | EV-1 | CONFIRMED |
| SYS_DIFF_LOG | ? | ? | common 模块 | ? | ? | ? | — | EV-1（仅实体存在，语义无文档） | **UNKNOWN** |
| BASE_APP_DATA | ? | ? | app 模块 | app(H5)? | ? | ? | — | EV-1 | **UNKNOWN** |

### G6 消息与事件域（D 消息通知 · Outbox 已是事件化挂靠点 [EV-5]）

| Table | Asset Class | Cap | Writer | Reader | Flow | Tx Boundary | Candidate Module | Evidence | Status |
|---|---|---|---|---|---|---|---|---|---|
| BASE_MESSAGE | P0 | D 消息 | message 模块 | 全域站内信 | D 服务 | 同库 | JNPF.Messaging | EV-1 | CONFIRMED |
| BASE_MSG_ACCOUNT / SEND / SEND_TEMPLATE / TEMPLATE / TEMPLATE_PARAM / SMS_FIELD / SHORT_LINK / MONITOR / WECHAT_USER | P0 | D 消息(通道/模板) | message | message/三方网关 | D 服务 | 同库 | JNPF.Messaging | EV-1 | CONFIRMED |
| BASE_NOTICE | P0 | D 公告 | message | 全域 | D 服务 | 同库 | JNPF.Messaging | EV-1 | CONFIRMED |
| BASE_IM_CONTENT / BASE_IM_REPLY | P0 | D IM | message | 前端 IM | D 服务 | 同库 | JNPF.Messaging | EV-1 | CONFIRMED |
| BASE_USER_DEVICE | P0 | D 推送设备 | message | push 链 | D 服务 | 同库 | JNPF.Messaging | EV-1 | CONFIRMED |
| SYS_EVENT_OUTBOX_MESSAGE | P0 | D 事件出箱 | infrastructure/EventBus.Outbox | EventBusHostedService | D 事件总线 | **Outbox 模式**（同库事务+异步投递，`docs/architecture/outbox-pipeline.md`） | JNPF.Eventing | EV-1 + EV-5 §二D | CONFIRMED |
| SYS_PROCESSED_EVENT | P0 | D 事件幂等 | infrastructure | EventBus | D 事件总线 | Outbox 模式 | JNPF.Eventing | EV-1 | CONFIRMED |

### G7 报表与大屏域（D 报表 · CQRS 读模型候选 [EV-5]）

| Table | Asset Class | Cap | Writer | Reader | Flow | Tx Boundary | Candidate Module | Evidence | Status |
|---|---|---|---|---|---|---|---|---|---|
| BASE_REPORT | P0 | D 报表 | report 模块 | 前端报表 | D 服务 | 同库 | JNPF.Reporting | EV-1 | CONFIRMED |
| BLADE_VISUAL + _CATEGORY/_COMPONENT/_CONFIG/_DB/_GLOB/_MAP/_RECORD | P0(INFERRED：随产品发布+代码在库；基线未逐表定级) | D 大屏 | visualdata | 大屏前端 :3102 | D 服务 | UoW×1（弱） | JNPF.Reporting | EV-1 | CONFIRMED（定级见 NDH-09） |

### G8 AI / InteAssistant 域（D AI · 旧系统中域自治度最高 [EV-5]）

> 40 张实体表中 2 张划入 G2（NDH-05），其余 38 张如下。sa_\* 13 张为 dapper-first 运行时物化表（无实体、不在 init 脚本），见 NDH-04。

| Table | Asset Class | Cap | Writer | Reader | Flow | Tx Boundary | Candidate Module | Evidence | Status |
|---|---|---|---|---|---|---|---|---|---|
| ai_projects / ai_entity_field / ai_ir_events / ai_ir_fragment_snapshots / ai_route_table / ai_seed_templates / ai_skill_llm_policy / ai_skill_runs | P0 | D AI(IR/Skill 底座) | inteAssistant | Skill 执行链 | D 服务 | 同库（UoW×5，EV-4） | JNPF.AiAssistant | EV-1；R12 三元组铁律约束 | CONFIRMED |
| BASE_IR_VERSION / BASE_IR_EDIT_PATCH | P0 | D AI(IR 版本) | inteAssistant | IR 投影链 | D 服务 | 同库 | JNPF.AiAssistant | EV-1 | CONFIRMED |
| BASE_AI_AGENT_CONFIG / _SKILL / _CALL_LOG / _MODEL_PROVIDER / _MODEL_ROUTING / _MCP_CONFIG / _PROMPT_TEMPLATE / _UI_TEMPLATE / _SKILL_REVIEW / _GENERATED_PROJECT | P0 | D AI(Agent/模型治理) | inteAssistant | LLM 路由/Skill | D 服务 | 同库 | JNPF.AiAssistant | EV-1 | CONFIRMED（CALL_LOG 双实体见 NDH-06） |
| BASE_AI_EVAL_CASE / _GOLDEN_SET / _RUN | P0 | D AI(Eval Pipeline 四层评估) | inteAssistant | EvalPipelineRunner | D 服务 | 同库 | JNPF.AiAssistant | EV-1 | CONFIRMED |
| BASE_AI_PIPELINE / _MESSAGE / _S2_PROGRESS / _STAGE_CONFIG | P0 | D AI(Pipeline 编排) | inteAssistant | S2 编译链 | D 服务 | 同库 | JNPF.AiAssistant | EV-1 | CONFIRMED |
| BASE_KNOWLEDGE_NODE / _EDGE / _RULE | P0 | D AI(Knowledge Graph) | inteAssistant | KG 查询 | D 服务 | 同库 | JNPF.AiAssistant | EV-1 | CONFIRMED |
| inte_assistant_attachment / _deliverable | P0 | D AI(附件/交付物) | inteAssistant | Skill 产出链 | D 服务 | 同库 | JNPF.AiAssistant | EV-1 | CONFIRMED |
| BASE_INTEGRATE / _NODE / _QUEUE / _TASK | P0 | D/A 集成(集成四表) | inteAssistant | 集成执行器 | D 服务 | 同库 | （集成候选模块） | EV-1 | **NEEDS HUMAN DECISION**（NDH-03，联动基线待决策 #5） |

### G9 工作流域（C 流程运行时 + B 流程设计器元数据 · 天然事件源 [EV-5]）

| Table | Asset Class | Cap | Writer | Reader | Flow | Tx Boundary | Candidate Module | Evidence | Status |
|---|---|---|---|---|---|---|---|---|---|
| FLOW_TEMPLATE / FLOW_TEMPLATE_JSON | P0 | B 流程设计器(元数据) | workflow | engine/visualdev | B→C 定义态 | 同库 | JNPF.Workflow | EV-1 | CONFIRMED |
| FLOW_FORM / _AUTHORIZE / _RELATION | P0 | B/C 流程表单绑定 | workflow | engine/前端 | B→C | 同库 | JNPF.Workflow | EV-1 | CONFIRMED |
| FLOW_TASK / _NODE / _OPERATOR / _OPERATOR_RECORD / _OPERATOR_USER / _CIRCULATE | P0 | C 流程运行时 | workflow | **app/engine/extend/visualdev 四区引用**（EV-3） | C 运行时·审批闭环 | 审批链多表同库事务（UoW 显式标注少，EV-4，INFERRED 存在隐式事务风险） | JNPF.Workflow | EV-1+EV-3 | CONFIRMED |
| FLOW_REJECT_DATA / _EVENT_LOG / _COMMENT / _DELEGATE / _CANDIDATES / _LAUNCH_USER / _VISIBLE | P0 | C 流程运行时 | workflow | engine/前端 | C 运行时 | 同库 | JNPF.Workflow | EV-1 | CONFIRMED |

### G10 低代码建模与应用运行时（B 设计态元数据 + C 应用/数据运行时 · 最大迁移面 [EV-5]）

| Table | Asset Class | Cap | Writer | Reader | Flow | Tx Boundary | Candidate Module | Evidence | Status |
|---|---|---|---|---|---|---|---|---|---|
| BASE_VISUAL_DEV | P0 | B 建模元数据根 | visualdev | RunService/codegen/前端 | B→C 核心 | 发布链同库（UoW×7，EV-4） | JNPF.LowCodeAuthoring | EV-1；RunService 4157 行为该域读写枢纽（v52 deep-dive） | CONFIRMED |
| BASE_VISUAL_LINK / BASE_VISUAL_RELEASE | P0 | B 建模(关联/发布) | visualdev | engine/前端 | B→C | 同库 | JNPF.LowCodeAuthoring | EV-1 | CONFIRMED |
| BASE_PORTAL / BASE_PORTAL_DATA | P0 | C 应用运行时(门户) | visualdev | 门户前端 | C 应用载体 | 同库 | JNPF.ApplicationRuntime | EV-1 | CONFIRMED |
| mt_\* 动态表（init 仅含 5 张样例 MT543xxx…） | P0(运行期创建) | C 表单运行时 | 运行期引擎动态建表 | RunService 数据链 | C 数据运行时 | 同库（跨动态表） | JNPF.ApplicationRuntime | EV-2（样例）+ EV-5 §二C | EXCLUDED-FROM-STATIC-INVENTORY（动态集合全貌属 L2 缺口） |
| BASE_MODULE_FORM（页面表单元数据） | P0 | B/C | system | 前端渲染 | C | 同库 | JNPF.Authorization（暂随菜单族，NDH-07 一并裁决） | EV-1 | CONFIRMED |

### G11 数据接口与外部连接（集成候选 [EV-5 §二B 集成设计器 HYPOTHESIS]）

| Table | Asset Class | Cap | Writer | Reader | Flow | Tx Boundary | Candidate Module | Evidence | Status |
|---|---|---|---|---|---|---|---|---|---|
| BASE_DATA_INTERFACE / _LOG / _OAUTH / _USER / _VARIATE | P0 | D/A 数据接口 | system | visualdev/前端调用链 | D 服务 | 同库（含 ExcuteSql 特殊分支，Oracle 多语句 bug 曾记录于过期存档 T5） | （集成候选模块） | EV-1 | CONFIRMED（模块归属 NDH-03） |
| BASE_DB_LINK | P0 | D/A 外部数据源 | system | DataBaseManager/报表 | D 服务 | 连接级 | （集成候选模块） | EV-1 | CONFIRMED（同 NDH-03） |

### G12 非平台资产排除登记（EV-5 已裁决 · 仅登记不分析）

| Cluster | 数量(init 脚本口径) | Asset Class | 处置 | 备注 |
|---|---|---|---|---|
| WFORM_\* | 51 | P2 产品模板 | 排除现代化 | 其中 3 张（LEAVEAPPLY/SALESORDER/SALESORDERENTRY）在 workflow 模块存在实体——**模板示例代码随产品发布**，不改其 P2 判定 [EV-5 证伪链] |
| WM_\* / WH_\* | 39 | P6 遗留（含真实数据） | 归档禁删 | 基线口径 42，差额属 L2 口径差 |
| EXT_\* | 19 | P3 Demo | 排除 | extend 模块整套代码为随产品演示代码（Order≠Order Domain，证伪链） |
| Demo_Order / Demo_OrderDetail / Demo_ExcelTest / student | 4 | P3 Demo | 排除 | 种子演示表 |
| undo_log | 1 | 疑似 SEATA 回滚日志遗留 | 排除+确认 | 语义未证实，列 UNKNOWN 佐证项 |
| data_report | 1 | 疑似旧报表遗留 | 排除+确认 | 无代码引用 |
| sa_\* | 13 | P1 特殊基础设施 | 归属延后 | dapper-first，运行时物化，不在 init 脚本 → NDH-04 |

---

## 2. 第二层结果 A：数据责任边界（Capability / Candidate Module → 负责的数据对象）

```text
JNPF.Identity（A 身份）
 ├── BASE_USER / BASE_ORGANIZE(+ADMINISTRATOR/RELATION) / BASE_ROLE
 ├── BASE_POSITION / BASE_GROUP / BASE_USER_RELATION / BASE_USER_OLD_PASSWORD
 └── BASE_SOCIALS_USERS / BASE_SYN_THIRD_INFO / BASE_SIGNATURE(+USER) / BASE_SIGN_IMG     【13 张】

JNPF.Authorization（A 授权）
 ├── BASE_AUTHORIZE / BASE_PERMISSION_GROUP / BASE_COLUMNS_PURVIEW
 └── BASE_MODULE(+BUTTON/COLUMN/FORM/LINK/SCHEME/AUTHORIZE) ＋ BASE_MENU_BADGE⚠ / BASE_FOUNDER_AUTH_LOG⚠ 【14 张】

JNPF.Tenancy（A 租户）
 └── zx_system_db / zx_sys_config                                                        【2 张】

JNPF.Dictionary + JNPF.PlatformConfig（A 字典/配置）
 ├── BASE_DICTIONARY_TYPE / _DATA / BASE_COMMON_WORDS / _FIELDS / BASE_PROVINCE(+_ATLAS)
 ├── BASE_ADVANCED_QUERY_SCHEME
 └── BASE_SYS_CONFIG / BASE_SYSTEM                                                       【9 张】

JNPF.Scheduling + JNPF.Printing（A 调度 / D 打印·日程）
 ├── BASE_TIME_TASK(+LOG) / JOBCLUSTER / JOBDETAILS / JOBTRIGGERS
 ├── BASE_SCHEDULE(+LOG/USER)
 └── BASE_PRINT_TEMPLATE / BASE_PRINT_LOG                                                【10 张】
 （另：SYS_DIFF_LOG⚠、BASE_APP_DATA⚠ 两张 UNKNOWN 待人工归类）

JNPF.Messaging + JNPF.Eventing（D 消息/事件）
 ├── BASE_MESSAGE / BASE_MSG_*（9）/ BASE_NOTICE / BASE_IM_CONTENT / _REPLY / BASE_USER_DEVICE 【14 张】
 └── SYS_EVENT_OUTBOX_MESSAGE / SYS_PROCESSED_EVENT                                      【2 张】

JNPF.Reporting（D 报表/大屏）
 └── BASE_REPORT / BLADE_VISUAL(+7 子表)                                                 【9 张】

JNPF.AiAssistant（D AI）
 └── ai_projects 等 8 张 IR/Skill 底座 + BASE_IR_VERSION/_EDIT_PATCH
     + BASE_AI_*（Agent 10 + Eval 3 + Pipeline 4）
     + BASE_KNOWLEDGE_*（3）+ inte_assistant_*（2）                                       【32 张】
 （BASE_INTEGRATE 四表⚠ → NDH-03；BASE_MENU_BADGE/FOUNDER_AUTH_LOG⚠ → NDH-05）

JNPF.Workflow（B 流程设计元数据 + C 流程运行时）
 └── FLOW_TEMPLATE(+JSON) / FLOW_FORM(+AUTHORIZE/RELATION)
     + FLOW_TASK 族（6）/ REJECT_DATA/EVENT_LOG/COMMENT/DELEGATE/CANDIDATES/LAUNCH_USER/VISIBLE 【21 张】

JNPF.LowCodeAuthoring + JNPF.ApplicationRuntime（B 建模 + C 应用/数据运行时）
 ├── BASE_VISUAL_DEV / _LINK / _RELEASE
 └── BASE_PORTAL / _DATA ＋ mt_* 动态表集合（静态清单外）                                 【5 张 + 动态集】

（集成候选模块，NDH-03）
 └── BASE_DATA_INTERFACE(+LOG/OAUTH/USER/VARIATE) / BASE_DB_LINK / BASE_INTEGRATE(+NODE/QUEUE/TASK) 【9 张】
```

合计：代码实证平台表 **约 125 张** + 集成候选 9 张 + UNKNOWN 2 张 + 动态/sa\_\* 集合；非平台排除登记 116 张（cluster 口径）。

## 3. 第二层结果 B：候选模块边界

| Candidate Module | Capability | Data Objects | Transaction Boundary | Dependencies（证据） |
|---|---|---|---|---|
| **JNPF.Identity** | A 身份 | 13 张（G1） | 单库事务；全域最高频写点之一 | **被 12 区域读**（EV-3）→ 全域依赖根，最先 API 化 [EV-5] |
| **JNPF.Authorization** | A 授权 | 14 张（G2） | 单库事务；GetCondition 双路径为权限运行时基线 [EV-5] | 依赖 Identity 读；被所有业务模块读 |
| **JNPF.Tenancy** | A 租户 | 2 张注册表 | 连接级切库语义（非行级） | ITenantFilter 全域横切；R12 三元组铁律承载方 |
| **JNPF.Dictionary / PlatformConfig** | A 字典/配置 | 9 张 | 只读为主，写低频 | 被全域读（EV-3） |
| **JNPF.Scheduling / Printing** | A/D 基础设施 | 10 张 | 弱事务（UoW×1） | 依赖 Identity；被宿主调度调用 |
| **JNPF.Messaging** | D 消息 | 14 张 | 单库事务 + 发送侧副作用（建议后续接 Eventing） | 被全域发送调用；读 Identity 收件人 |
| **JNPF.Eventing** | D 事件总线 | 2 张 | **Outbox 模式**（唯一已成模式的事务出域机制）[outbox-pipeline.md] | 基础设施层，被各模块投递 |
| **JNPF.Reporting** | D 报表/大屏 | 9 张 | 读模型为主（UoW×1） | 经 BASE_DB_LINK 跨源读（不拥有业务数据所有权） |
| **JNPF.AiAssistant** | D AI | 32 张（+sa_* 运行时物化） | 单库事务；IR 投影受 R12 三元组约束 | 仅依赖 Identity/Authorization 读 [EV-5 §二D]——自治度最高，远期服务化候选 H3 |
| **JNPF.Workflow** | B/C 流程 | 21 张 | 审批链多表同库事务；显式 UoW 标注偏少（风险项） | 读 Identity/Organization/Position；被 app/engine/extend/visualdev 调用（EV-3）；天然事件源→未来对接 Eventing |
| **JNPF.LowCodeAuthoring / ApplicationRuntime** | B/C 低代码核心 | 5 张元数据 + mt_\* 动态集 | 发布链同库事务（UoW×7）；RunService 为读写枢纽（4157 行上帝类，已知重构对象） | 依赖 Identity/Authz/Dictionary/Workflow/DataInterface——核心闭环中枢 |
| （集成候选，未命名） | D/A 集成 | 9 张 | 同库 + 外部调用混合（需模式裁决） | 定位待基线待决策 #5 |

**核心闭环落位**（对应基线 §2.2）：Application(ApplicationRuntime) ← Metadata(LowCodeAuthoring+Workflow.B) ← Runtime(Workflow.C + 数据/权限运行时) ← Tenant/Identity(A 基座)；D 类服务横向挂靠；Eventing 为唯一既成跨域事务机制。

## 4. UNKNOWN 清单（零猜测保留）

| # | 对象 | 状态 | 原因 |
|---|---|---|---|
| U-1 | SYS_DIFF_LOG（common 模块） | UNKNOWN | 仅实体存在，业务语义无任何文档 |
| U-2 | BASE_APP_DATA（app 模块） | UNKNOWN | 仅实体存在，移动端语义未证实 |
| U-3 | sa_\* 13 张 | UNKNOWN（归属） | dapper-first 无实体；基线明示"归属裁决延后" |
| U-4 | mt_\* 动态表全集 | UNKNOWN（集合边界） | 运行期创建；init 脚本仅 5 张样例 |
| U-5 | 289 vs 228 差集其余部分 | UNKNOWN（明细） | 依赖 NDH-01 逐表对齐 |
| U-6 | undo_log / data_report | UNKNOWN（来源） | 无代码引用、无文档；倾向遗留但**不据此删除**（永久禁止事项 #6） |

## 5. NEEDS HUMAN DECISION 清单

| # | 决策项 | 选项 | 影响 |
|---|---|---|---|
| NDH-01 | 恢复/导入 NG-1A/1B 逐表明细 CSV 至本仓（如 `.claude/evidence/` 或 docs/architecture/evidence/），逐表核对 157 名单与本表差异 | 导入 / 重算 | 决定本表能否升级为全量 CONFIRMED |
| NDH-02 | zx_system_db/zx_sys_config 归 Tenancy 独立模块 or 并入 PlatformConfig | 二选一 | G3 边界 |
| NDH-03 | 集成定位：BASE_INTEGRATE 四表 + DATA_INTERFACE 五表 + DB_LINK 是否构成独立 Integration 模块（联动基线待决策 #5"集成能力定位"） | 本期立域 / 留 v2 | G8/G11 共 9 张归属 |
| NDH-04 | sa_\* 13 张归属（基线已延后；与 S2 物化架构相关） | AiAssistant / 独立 IR 基座 | U-3 |
| NDH-05 | BASE_MENU_BADGE、BASE_FOUNDER_AUTH_LOG：能力属 A 授权但物理写者为 inteAssistant——迁移归位 or 维持现状 | 迁移 / 维持 | G2/G8 各 1 张 |
| NDH-06 | BASE_AI_Call_LOG(API.Entry) 与 BASE_AI_CALL_LOG(inteAssistant) 同表双实体定义 | 合并到一处 | 重复实体风险 |
| NDH-07 | BASE_MODULE* 菜单/资源元数据族归 Authorization or LowCodeAuthoring | 二选一 | G2 六张 |
| NDH-08 | Quartz 三表（JOBCLUSTER/JOBDETAILS/JOBTRIGGERS）是否计入平台资产 | 计入 / 视为框架私有 | 资产计数口径 |
| NDH-09 | BLADE_* 八表正式定级（本表 INFERRED P0） | 确认 / 重审 | G7 |

## 6. 十项验收对照（如实自评）

| # | 验收条件 | 结果 | 说明 |
|---|---|---|---|
| 1 | 范围内数据对象全部进入主表 | ⚠️ **REFINE 条件** | 代码实证 136 张全进（125+9+2）；但"157 名单"逐表核对受 NDH-01 阻塞，无法声称全量覆盖 |
| 2 | 每对象有 Capability 映射或明确 UNKNOWN | ✅ | U-1~U-6 显式登记 |
| 3 | 每对象有 Data Responsibility 或 UNKNOWN/NDH | ✅ | NDH-02~05 显式登记 |
| 4 | Writer/Reader 有证据 | ✅（Writer 强证据；Reader 为抽样级，已在 L3 声明） | EV-1/EV-3 |
| 5 | 核心业务流程有对应关系 | ✅ | §3 核心闭环落位段 |
| 6 | 事务边界有结论或明确 UNKNOWN | ✅ | EV-4 + Outbox 显式；Workflow 风险项已标 INFERRED |
| 7 | 已确认边界形成 Candidate Module | ✅ | §2/§3（均为候选，非裁决） |
| 8 | 关键判断有 Evidence | ✅ | 全表 EV-n 标注 |
| 9 | 无填表猜测 | ✅ | 0.3 判定规则强制；两处 INFERRED 均带置信依据并挂 NDH |
| 10 | 可直接作为代码模块化输入 | ✅ | §2/§3 即 PHASE 4 输入格式 |

**总体判定：REFINE** —— 第 1 条因 NDH-01（明细 CSV 不在库）不能宣称全量覆盖，其余 9 条达成。解除条件：NDH-01 完成逐表对齐后升 CONFIRMED。

---

## STOP 声明

主表及两层汇总已交付。**按任务要求立即停止，不进入任何下一阶段。**
提交人工审批：①本主表 ②数据责任边界（§2）③候选模块边界（§3）④UNKNOWN 清单（§4）⑤NDH 清单（§5）⑥十项验收结果（§6，总体 REFINE）。

**本表遵守**：六零约束（零代码/零 DB 变更/零部署/零微服务/零 Aspire）；五否定推导禁令；Candidate Module ≠ 架构裁决。
