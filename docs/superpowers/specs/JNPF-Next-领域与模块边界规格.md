# JNPF-Next 领域与模块边界规格 v1.0（NG-0 产物 2/5）

**日期**：2026-08-25 ｜ **依据**：NG-0 证据 5（domain-candidates 10 维度）+ 证据 2/4/6
**状态**：设计规格（只读）

## 1. 边界原则

1. **不沿用 modularity 项目目录**（组织产物 ≠ 领域边界——inteAssistant 项目 116 文件横跨 3 域）；
2. 边界依据：DB ownership + 调用链 + 事务边界 + API/权限/租户/工作流证据（证据 5 §0）；
3. 禁止「一个 Service 一个微服务 + HTTP 包起来」。

## 2. 十二候选域（证据 5 汇总）

| 域 | 名称 | 核心聚合 | 数据 | 事务 | 类型 |
|----|------|---------|------|------|------|
| D1 | Identity | User/Organize/Role/Position/Group | base_user 等 | 单库 | 核心 |
| D2 | Tenant | Tenant 注册表 | zx_sys_db/config | 跨库开通（补偿） | 平台 |
| D3 | Permission | Authorize/Module 树/DataAuthorizeScheme | base_authorize/module* | 授权原子+快照读 | 核心 |
| D4 | Workflow | FlowEngine/Task+Operator/wform 表单实例 | flow_*/wform_* | 流转原子 | 核心 |
| D5 | Form/LowCode | VisualModel/Form 设计/Runtime 数据 | base_visualdev_*/mt* | 元数据与数据分离 | 核心 |
| D6 | Data/Dictionary | DictionaryType+Data/Portal | base_dictionary_data 等 | 单表 | 平台 |
| D7 | File | File 元数据+存储 | base_file | 存后记/补偿 | 平台 |
| D8 | Message | Message/EventOutbox | base_message/outbox | 出箱投递 | 平台 |
| D9 | Log/Audit | 日志族 | base_sys_log/api_log | 异步写 | 平台 |
| D10 | AI | IrEvent/EntityField/SaDoc/Knowledge | ai_*/sa_*/inte_*/kg_* | SA 物化原子 | 核心 |
| D11 | Report | 报表/大屏（读模型） | report_* | 无 | 平台 |
| D12 | Demo | Order+BillDetail 等 | WM_*/WH_*/ext_* | 单据原子 | 沙盘 |

## 3. 模块边界设计（Modular Monolith 程序集划分）

```text
JNPF.Next.Domain（域模型：实体/聚合/值对象——12 域分包）
JNPF.Next.Application（应用服务：每个域一个程序集/命名空间分区）
JNPF.Next.Contracts（接口/事件/DTO——域间唯一依赖面）
JNPF.Next.Infrastructure（数据访问/缓存/事件总线/存储——Aspire 管理）
JNPF.Next.Api（入口/动态 API——按域路由）
```

**依赖规则（架构测试强制，现 Architecture 92 用例扩展）**：
1. Domain 零依赖（纯模型）；
2. Application → Contracts + Domain（**禁止跨域 Application 直依赖**——跨域走 Contracts 事件/API）；
3. Infrastructure → Contracts（实现域接口）；
4. Api → Application/Contracts。

## 4. 域间契约（Events 先行）

| 事件 | 发布域 | 消费域 |
|------|--------|--------|
| UserCreated/UserOrganizeChanged | D1 | 全域（审计快照更新） |
| AuthorizationChanged | D3 | 全域（权限缓存失效） |
| TaskCreated/TaskCompleted | D4 | D12/通知 |
| ModelPublished/FormDeployed | D5 | 平台（DDL/缓存重建） |
| IrEventAppended | D10 | D10 内部（自治） |
| FileStored | D7 | 业务域 |

## 5. 事务边界裁决（形态判定输入）

- 全部候选域事务边界 = **单库事务**（现状无跨库事务需求）→ 形态 A（Modular Monolith）首选（证据 6）；
- D2 租户开通是唯一潜在跨库（注册+建库+种子）——用补偿/出箱处理，不引入分布式事务；
- D5 元数据事务与数据事务分离（DDL 不在业务事务内）。

## 6. 待裁决（NG-2 输入）

| # | 事项 | 建议 |
|---|------|------|
| DM-D1 | D5 动态表注册表归属 | D5 域内 |
| DM-D2 | wform 51 表归属（Workflow vs Form） | 表单实例归 D4（流程驱动）；表单设计归 D5 |
| DM-D3 | D12 沙盘先迁哪域 | Order（路径 B 唯一消费者） |
| DM-D4 | AI 域 Studio 基础设施归属 | 基础设施入 D10 内部共享，不单列 |
