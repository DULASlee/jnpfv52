# JNPF-Next 租户权限架构规格 v1.0（NG-0 产物 4/5）

**日期**：2026-08-25 ｜ **依据**：P0-B 不变量规格（43 特征）+ P0-A 边界 + NG-0 证据 1/3/8
**状态**：设计规格（只读；P0-C 保持 DEFERRED——本规格承接其冻结语义并给出 Next 形态）

## 1. Legacy 现状（已锁定）

| 事实 | 证据 |
|------|------|
| 租户列三风格（f_tenant_id 187/F_TenantId 21/tenant_id 10） | DB-1 §3 |
| 租户主数据 zx_sys_db；连接级切库 AsTenant + 列级 ITenantFilter 双机制 | DB-1 §3 |
| 权限评估三连查（authorize→module→scheme）每列表查询执行 | DB-3 §2 |
| 条件注入四段叠加（dataRuleJson/querJson/superQueryJson/dataPermissions） | DB-3 §2 |
| 双路径 76 特征（A 33 + B 43）已锁定行为 | P0-B 规格 |
| 权限怪异 Q-PB1/E-PB1~4 已登记 | P0-B 规格 §5 |

## 2. Next 租户架构

### 2.1 租户模型（REDEFINE）
- **单一租户标识**：`tenant_id`（业务表列）+ 租户注册表（D2 域：zx_sys_db 演进）——列过滤与连接切库双机制保留但**契约统一**；
- 租户上下文：请求头/Claim 显式注入（替代隐式 HttpContext 传递——P0-C 登记项）；
- 跨租户泄漏回归套件为迁移前置（DB-D4）。

### 2.2 租户过滤管线（REDEFINE）
```text
请求 → TenantContext（显式）
  → 数据访问 API 强制校验（tenant_id 列存在 + 过滤生效）
  → 例外路径显式声明（无租户语义表登记表——32% 无租户列表）
  → 连接级切库（私有化多库——KEEP）
```

### 2.3 权限架构（REDEFINE）
```text
授权模型（D3）：Authorize 记录/Module 树/DataAuthorizeScheme（KEEP 语义）
  ↓ 权限评估 API（DA-2）
评估输入：用户/角色/组织（Identity API）+ 模块 + 方案
  ↓
评估输出：条件契约（ConditionalModel——KEEP 序列化形态）
  ↓
快照缓存：AuthorizationSnapshot（授权变更事件失效——D3 Events）
```

### 2.4 条件生产语义（按 Compatibility Map 裁决）

| Legacy | Next | 等价基线 |
|--------|------|---------|
| 路径 A/B 双实现 | 单一评估 API（语义合并不合并怪异） | 33+43 特征 |
| 短路层（Admin/AllowAll/DenyAll） | KEEP（全权限/无权限语义正确） | 既有测试 |
| Q-PB1（首条 Or） | REMOVE（按 AND/OR 意图重建） | 新契约测试 |
| E-PB1~4 | REMOVE/REDEFINE（显式契约） | 新契约测试 |
| ReplaceOp/23-case | KEEP（DSL 契约） | 既有测试 |

## 3. 权限不变量清单（Next 必须保持的语义核心——非怪异部分）

1. 管理员短路 = 空条件（全权限）；
2. 关闭数据权限 = primaryKey<>'0'（AllowAll）；
3. 无授权 = primaryKey='0'（DenyAll）；
4. 分级管理全部放开（SystemId 命中）；
5. token 语义（@userId/@organizeId/@organizationAndSuborganization/@branchManageOrganize 等 6 token）保留；
6. 条件组合顺序（role→group→clause 三层嵌套）保留；
7. 字段名 tableNumber 前缀透传（OrderService "a." 形态）保留。

## 4. 租户不变量清单（P0-C 冻结语义 → Next 契约）

1. 每业务查询携带租户约束（列过滤或连接切库——二选一显式）；
2. 跨租户数据不可见（泄漏回归套件）；
3. 租户上下文从请求头显式注入，禁止隐式；
4. 无租户语义表（32%）显式登记（非默认豁免）。

## 5. 怪异裁决（Q1-Q11 + E 系列全量）

| 组 | 裁决 | 说明 |
|----|:----:|------|
| Q1-Q6（守卫语义：空 Ids 跳过/越界补行/空字段跳过/CanTransfer 门/`-` 豁免） | **KEEP** | 守卫语义正确（D1 战役验证） |
| Q7-Q11（异常路径：USERSSELECT 单选/Contains 无 case/嵌套拍平等） | **REDEFINE/REMOVE** | 逐项显式契约或弃用（E3 Contains→显式映射；E2 嵌套→显式拍平规则） |
| E-PB1（EnCode NRE）/E-PB3（Between 空模型） | **REMOVE** | 缺陷不复制 |
| E-PB2（DenyAll 差异）/E-PB4（In 匿名对象） | **REDEFINE** | 统一尾部守卫 + 显式 ItemId |
| E1（空 between AOORe） | **REDEFINE** | 显式校验错误契约 |

## 6. 待裁决（NG-1 输入）

| # | 事项 | 建议 |
|---|------|------|
| TP-D1 | 权限快照失效粒度（租户级/角色级/用户级） | 租户+角色级（base_authorize 变更事件） |
| TP-D2 | 无租户语义表登记表是否先建 | 是（P0-C 输入材料） |
| TP-D3 | 6 token 是否扩展（Next 新 token） | 暂不扩展（KEEP） |
| TP-D4 | 跨租户泄漏回归套件落地时间 | W8 前（迁移波次证据 9） |
