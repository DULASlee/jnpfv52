# JNPF V5.2 领域模型全景

> 来源：graph.json (1497 nodes, 1616 edges) + SqlSugar 实体类提取 + 数据库表前缀分析
> 用途：DKEE 知识图谱 — 领域模型层
> 更新日期：2026-06-11

---

## 1. 领域划分表

| 领域 | 模块前缀 | 核心表数 | 状态 | 对应 modularity/ 项目 |
|------|----------|---------|------|----------------------|
| 基础系统 (Base) | `BASE_` | ~25 | ✅ 运行中 | JNPF.Systems, JNPF.Common |
| 工作流 (WorkFlow) | `FLOW_` | 18 | ✅ 运行中 | JNPF.WorkFlow |
| 可视化开发 (VisualDev) | — (F_) | ~10 | ✅ 运行中 | JNPF.VisualDev |
| 数字大屏 (DataScreen) | `BLADE_VISUAL_` | 8 | ✅ 运行中 | JNPF.VisualData |
| 消息中心 (Message) | `BASE_MSG_` | ~5 | ⚠️ Partial | JNPF.Message |
| 扩展模块 (Extend) | `EXT_` | 动态 | ⚠️ 客户定制 | JNPF.Extend |
| OA 办公 | `BASE_OA_` | ~10 | ❌ 禁用 (R5) | JNPF.OA.* (未激活) |
| IoT 物联网 | `IOT_` | — | ❌ 未创建 (R5) | — |
| MES 制造 | `MES_` | — | ❌ 未创建 (R5) | — |
| 定时任务 (Scheduler) | `BASE_TIMETASK` | 1 | ✅ 运行中 | JNPF.TaskScheduler |
| AI 审计日志 | `BASE_AI_CALL_LOG` | 1 | ⬜ Planned (P0-B) | — |

---

## 2. 核心实体关系（per 领域）

### 2.1 基础系统 (Base) — RBAC 体系

```
                          ┌──────────────────────┐
                          │    BASE_USER          │
                          │    (用户)              │
                          └──────┬───────────────┘
                                 │
                    ┌────────────┼────────────┐
                    │            │            │
                    ▼            ▼            ▼
          ┌─────────────┐ ┌───────────┐ ┌────────────┐
          │BASE_USER_   │ │BASE_USER_ │ │BASE_USER_  │
          │RELATION     │ │RELATION   │ │RELATION    │
          │(用户-角色)   │ │(用户-组织) │ │(用户-岗位)  │
          └──────┬──────┘ └─────┬─────┘ └──────┬─────┘
                 │              │               │
        ┌────────┴────────┐     │        ┌──────┴──────┐
        ▼                 ▼     ▼        ▼             ▼
  ┌───────────┐   ┌───────────┐  ┌──────────┐  ┌──────────────┐
  │ BASE_ROLE │   │BASE_ORG   │  │BASE_POS  │  │BASE_AUTHORIZE│
  │ (角色)    │   │ (组织)    │  │ (岗位)   │  │ (权限授权)   │
  └─────┬─────┘   └───────────┘  └──────────┘  └──────┬───────┘
        │                                              │
        └──────────────────────┬───────────────────────┘
                               ▼
                    ┌──────────────────────┐
                    │   BASE_MODULE        │
                    │   (菜单/功能模块)      │
                    └──────────┬───────────┘
                               │
                    ┌──────────┴───────────┐
                    ▼                      ▼
          ┌──────────────────┐  ┌──────────────────────┐
          │BASE_MODULE_      │  │BASE_MODULE_BUTTON    │
          │SCHEME            │  │(模块按钮)             │
          │(数据权限方案)     │  └──────────────────────┘
          └──────────────────┘


其他 Base 实体：

  BASE_DICTIONARY_DATA     — 字典项（下拉选项数据源）
  BASE_SYS_CONFIG          — 系统配置（键值对）
  BASE_SYS_LOG             — 系统审计日志
  BASE_TIMETASK            — 定时任务定义
  BASE_API_LOG             — API 请求日志
  BASE_DATA_CHANGE_LOG     — 数据变更日志（Phase2 S3, Planned）
  BASE_OPENAPI_LOG         — OpenAPI 调用日志（Phase2 S4, Planned）
  BASE_AGGREGATE_QUERY     — 聚合查询方案（Phase2 S1, Planned）
  BASE_AI_CALL_LOG         — AI 调用审计日志（Planned）
  BASE_MSG_DELIVERY_LOG    — 消息下发日志（Phase2 S2, Planned）
  BASE_INTEGRATE_QUEUE     — 集成队列（Webhook 入站）
```

### 2.2 工作流 (WorkFlow) — 状态机系统

```
  FLOW_TEMPLATE_JSON        ← 流程模板（版本化 JSON 定义）
       │
       ▼
  FLOW_TEMPLATE              ← 流程模板元数据
       │
       ├── FLOW_TASK          ← 流程任务实例（F_PARENT_ID 自引用子流程）
       │   ├── F_STATUS: 7 态
       │   │   Draft → Handle → Adopt / Reject / Revoke / Cancel / Suspend
       │   ├── FLOW_TASK_NODE      ← 任务节点（审批步骤）
       │   ├── FLOW_TASK_OPERATOR  ← 任务操作者（审批人）
       │   └── FLOW_BEFORE/AFTER_FLOW ← 前后流程连接
       │
       └── FLOW_PROCESS           ← BPMN 可视化设计器（~134 文件，constructTree JSON 解析）

  工作流引擎特征：
  - 自研 JSON 状态机（非 Activiti/Flowable/Camunda/Elsa）
  - FlowTaskManager（核心运行时 ~2390 行）
  - FlowTemplateUtil（JSON 树 → 平铺节点列表解析器）
```

### 2.3 可视化开发 (VisualDev) — 低代码表单引擎

```
  VisualDevModelEntity      ← 模型定义（type=1 Web设计/2 流程表单/5 自定义）
       │
       ├── Config JSON       ← 表单配置（fields[], __config__）
       ├── FormData JSON     ← 表单数据（funcs: onLoad/beforeSubmit/afterSubmit）
       └── ColumnData JSON   ← 列数据（columnList[], searchList[], hasSuperQuery）

  函数签名体系（从 production-func-analysis.txt）：
  - Form-level: onLoad / beforeSubmit / afterSubmit — 7 params
  - Field-level: on.change / on.blur — 8 params (data + rowIndex extra)

  CodeGen 代码生成：
  - 368 个 .vm 模板（Apache Velocity 语法）
  - 5 种表关系模式
  - CodeGenService → /api/visualdev/Generater
```

### 2.4 数字大屏 (DataScreen) — 可视化组件系统

```
  BLADE_VISUAL_* (8 表)
       │
       ├── BLADE_VISUAL     — 大屏定义
       ├── BLADE_VISUAL_MAP — 大屏地图配置
       ├── BLADE_VISUAL_CATEGORY — 分类
       └── BLADE_VISUAL_DATA — 数据源配置

  动态组件注册：components/index.js → componentMap / option/components.js → optionRegistry
  图表组件：ECharts 全量导入（~1MB，未 tree-shake）
  自定义图表：安全降级（new Function → static template mode）
```

### 2.5 消息中心 (Message)

```
  BASE_MSG_TEMPLATE          ← 消息模板
       │
       ▼
  MessageDeliveryService     ← 消息下发（钉钉/企业微信/短信/邮件 + 重试）
       │
       ▼
  BASE_MSG_DELIVERY_LOG      ← 下发日志
  BASE_MSG_CHANNEL           ← 消息通道配置
```

### 2.6 扩展模块 (Extend)

```
  EXT_EMPLOYEE               ← 示例：雇员表（客户项目定制）
  EXT_*                      ← 动态创建，数量取决于客户项目
```

---

## 3. 关键业务规则

| 规则 ID | 领域 | 触发条件 | 约束 | 注入点 |
|---------|------|----------|------|--------|
| BR-001 | Tenant | 所有查询 | `F_TENANT_ID` = 当前租户 | ITenantFilter (Queryable) |
| BR-002 | Tenant | Insert | 自动填充 TenantId | DataExecuting 委托 |
| BR-003 | Tenant | Update/Delete | 必须显式 `.Where(TenantId = x)` | 手动（无自动过滤） |
| BR-004 | DataScope | 有权限用户查询 | 按组织过滤数据 | IUserManager.DataScope |
| BR-005 | WorkFlow | 流程任务创建 | 状态 = Draft | FlowTaskManager.Create |
| BR-006 | WorkFlow | 任务审批 | 当前操作者在 FLOW_TASK_OPERATOR 中 | FlowTaskManager.Submit |
| BR-007 | WorkFlow | 子流程 | F_PARENT_ID 指向父 FLOW_TASK.F_ID | 自引用层次 |
| BR-008 | VisualDev | 表单提交 | beforeSubmit → Promise.resolve() 才能继续 | 表单校验链 |
| BR-009 | VisualDev | 字段关联 | RelationForm 联动刷新 | handleRelationForParent |
| BR-010 | CodeGen | 代码生成 | 只改 .vm 模板，不改生成输出 | R3 红线 |
| BR-011 | Security | 文件上传 | 路径穿越检查 | FilePathSecurityHelper |
| BR-012 | Security | Idempotency | X-Idempotency-Key + Redis SetNx | PreventDuplicateSubmitFilter |
| BR-013 | Outbox | 事件发布 | 业务操作 + Outbox 写入同事务 | EventOutboxMessage |
| BR-014 | Outbox | 重试 | 指数退避 2^n s，断路器，最终 DeadLetter | Polly Retry Pipeline |

---

## 4. 数据库命名约定

### 4.1 表命名

```
{BUSINESS_PREFIX}_{ENTITY_NAME}

示例：
  BASE_USER           → 基础系统 — 用户
  FLOW_TASK           → 工作流 — 任务
  FLOW_TASK_OPERATOR  → 工作流 — 任务操作者
  EXT_EMPLOYEE        → 扩展 — 雇员
  IOT_DEVICE          → IoT — 设备（未创建）
  MES_WORKSTATION     → MES — 工位（未创建）
```

### 4.2 列命名

- 主键：`F_ID`
- 外键：`F_{ENTITY}_ID`（如 `F_USER_ID`）
- 业务列：`F_{NAME}`（如 `F_USER_NAME`）
- 审计列：`F_CREATE_TIME`、`F_CREATE_USER_ID`、`F_LAST_MODIFY_TIME`、`F_LAST_MODIFY_USER_ID`
- 租户列：`F_TENANT_ID`
- 系统列：`F_ZX_SYSTEM_ID`（多系统标识）
- 删除标记：`F_DELETE_MARK`

---

## 5. 关键枚举

### 5.1 工作流状态 (FlowTaskStatusEnum)

| 值 | 状态 | 说明 |
|----|------|------|
| 0 | Draft | 草稿 |
| 1 | Handle | 处理中 |
| 2 | Adopt | 通过 |
| 3 | Reject | 拒绝 |
| 4 | Revoke | 撤回 |
| 5 | Cancel | 取消 |
| 6 | Suspend | 挂起 |

### 5.2 Outbox 状态

| 值 | 状态 | 说明 |
|----|------|------|
| 0 | Pending | 待发送 |
| 1 | Processing | 处理中 |
| 2 | Completed | 已完成 |
| 3 | DeadLetter | 死信（重试耗尽） |

### 5.3 VisualDev 类型

| 值 | 类型 | 说明 |
|----|------|------|
| 1 | Web设计 | PC 表单 |
| 2 | 流程表单 | 工作流绑定 |
| 5 | 自定义 | 自定义页面 |

---

## 6. 跨领域关系

```
  BASE_USER (用户)
    │
    ├── 创建 → FLOW_TASK (发起流程)
    ├── 操作 → FLOW_TASK_OPERATOR (审批任务)
    ├── 设计 → VisualDevModelEntity (表单设计)
    ├── 配置 → BLADE_VISUAL (大屏配置)
    ├── 生成 → CodeGen (代码生成)
    └── 授权 → BASE_AUTHORIZE (权限分配)

  BASE_ORGANIZE (组织)
    ├── 拥有 → BASE_USER (用户-组织关系)
    ├── 作用域 → DataScope (数据权限范围)
    └── 绑定 → FLOW_TASK_NODE (流程节点审批组织)

  FLOW_TASK (流程任务)
    ├── 关联 → VisualDevModelEntity (绑定的表单模型)
    ├── 触发 → Outbox Event (审批事件 → 消息通知)
    └── 引用 → BASE_USER (发起人/审批人)
```

---

## 7. 领域边界约束（EAB 逻辑层）

| 约束 | 内容 | 验收方式 |
|------|------|----------|
| C1 | Base 模块不依赖 WorkFlow 模块 | 编译期：Roslyn Analyzer JNPF001 |
| C2 | 所有模块只能通过 DI 接口引用 Framework 层 | 编译期：Roslyn Analyzer JNPF002 |
| C3 | 数据库表必须通过模块前缀隔离 | 代码审查：表定义检查 |
| C4 | OA 模块禁止修改（R5） | Git：OA 文件不纳入 scope |
| C5 | IoT/MES 不创建（R5） | Git：无 IOT_/MES_ 实体文件 |
