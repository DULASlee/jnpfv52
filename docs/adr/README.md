# JNPF V5.2 架构决策记录（ADR）索引

> 共计 24 项 ADR（基础架构 15 项 + 架构部署 3 项 + 数据库治理 5 项 + AI 工程组织 1 项）。
> ADR-024 状态 Proposed（Phase A 启动后转 Accepted，v1.0-final 5 项硬门槛达成后转 Final）；其余 23 项全状态 Final。
> ADR 格式：标题 / 状态 / 决策内容 / 理由 / 备选方案 / 后果 / 验证结果 / 相关引用

---

## 基础架构 ADR（ADR-001 ~ 015）

| ADR | 标题 | 状态 | 阶段 | 文件 |
|---|---|---|---|---|
| ADR-001 | ISqlSugarClient 注册方式 — Scoped + CopyNew | Final | 0 | [ADR-001.md](ADR-001.md) |
| ADR-002 | DataExecuting 实现策略 — 统一委托取代 Aop 覆盖 | Final | 0 | [ADR-002.md](ADR-002.md) |
| ADR-003 | TenantContext 解析方式 — AsyncLocal + 静态访问点 | Final | 2 | [ADR-003.md](ADR-003.md) |
| ADR-004 | 匿名端点降级策略 — 四级 Fallback | Final | 2 | [ADR-004.md](ADR-004.md) |
| ADR-005 | 模块系统主从关系 — LegacyModule 桥接 | Final | 2 | [ADR-005.md](ADR-005.md) |
| ADR-006 | CopyNew 行为 — 保留过滤器 | Final | 0 | [ADR-006.md](ADR-006.md) |
| ADR-007 | Repository 构造函数目标行数 — ≤5 行 | Final | 4 | [ADR-007.md](ADR-007.md) |
| ADR-008 | Outbox 投递策略与多实例安全 — UPDLOCK READPAST | Final | 5 | [ADR-008.md](ADR-008.md) |
| ADR-009 | API 契约不可修改 — 方法签名冻结 | Final | All | [ADR-009.md](ADR-009.md) |
| ADR-010 | 业务冻结期与热补丁通道 | Final | 1 | [ADR-010.md](ADR-010.md) |
| ADR-011 | DiffLog 发布解耦 — 独立模块 | Final | 1 | [ADR-011.md](ADR-011.md) |
| ADR-012 | Updateable/Deleteable 全局租户保护 — Safe* 方法 | Final | 4 | [ADR-012.md](ADR-012.md) |
| ADR-013 | 非 HTTP 入口租户上下文传播 — EventBus + Schedule | Final | 2 | [ADR-013.md](ADR-013.md) |
| ADR-014 | Repository IDisposable 保障 | Final | 4 | [ADR-014.md](ADR-014.md) |
| ADR-015 | Outbox Dispatcher 优雅停机 — Channel 排空 | Final | 5 | [ADR-015.md](ADR-015.md) |

## 架构部署 ADR（ADR-016 ~ 018）

| ADR | 标题 | 状态 | 阶段 | 文件 |
|---|---|---|---|---|
| ADR-016 | JNPF 架构部署策略 — 逻辑微服务 + 物理单体 | Final | 前端架构重构 | [ADR-016.md](ADR-016.md) |
| ADR-017 | 新旧代码生成器共存策略 | Final | 前端架构重构 | [ADR-017-new-old-codegen-coexistence.md](ADR-017-new-old-codegen-coexistence.md) |
| ADR-018 | Unapp UI 库选型 | Final | 前端架构重构 | [ADR-018-unapp-ui-library-selection.md](ADR-018-unapp-ui-library-selection.md) |

## 数据库治理 ADR（ADR-019 ~ 023 — Phase 8）

| ADR | 标题 | 状态 | 阶段 | 文件 |
|---|---|---|---|---|
| ADR-019 | Table Refactoring Expert Skill v1.0 冻结决策 | Final | Phase 8 / P8-E | [ADR-019-table-refactoring-expert-skill-v1-freeze.md](ADR-019-table-refactoring-expert-skill-v1-freeze.md) |
| ADR-020 | R2-COMP 独立 AI 验证作为主要验证机制 | Final | Phase 8 / P8-A.6 | [ADR-020-r2-comp-primary-validation.md](ADR-020-r2-comp-primary-validation.md) |
| ADR-021 | Triple-Key Iron Law（AI/IR/SA 表强制三键） | Final | Phase 8 / 数据库治理 | [ADR-021-triple-key-iron-law.md](ADR-021-triple-key-iron-law.md) |
| ADR-022 | NO-CHANGE 主动判断原则（数据库治理成熟度核心） | Final | Phase 8 / 治理文化 | [ADR-022-no-change-active-judgment.md](ADR-022-no-change-active-judgment.md) |
| ADR-023 | Schema 漂移检测执行前强制规则 | Final | Phase 8 / 风险预防 | [ADR-023-schema-drift-pre-execution.md](ADR-023-schema-drift-pre-execution.md) |

## AI 工程组织 ADR（ADR-024 — Phase 8 后）

| ADR | 标题 | 状态 | 阶段 | 文件 |
|---|---|---|---|---|
| ADR-024 | AI 软件工程组织架构方案 v1.0（组织级 AI Agent 家族） | Proposed | Phase 8 后 / Phase A | [ADR-024-AI软件工程组织架构方案.md](ADR-024-AI软件工程组织架构方案.md) |

---

## 状态说明

| 状态 | 含义 |
|---|---|
| Proposed | 提议阶段，尚未决策 |
| Accepted | 已接受，尚未实施 |
| Final | 已实施并验证 |
| Superseded | 被后续 ADR 替代 |
| Deprecated | 不再适用 |

---

## ADR 编写规范

每份 ADR 包含以下章节：
1. **标题** — 简短明确的决策名称
2. **状态** — Final / Accepted / Proposed
3. **日期** — 决策日期
4. **决策内容** — 做了什么决策
5. **理由** — 为什么这样做（含备选方案）
6. **后果** — 正面和负面影响
7. **验证结果** — 如何验证决策正确性
8. **相关 ADR** — 引用关联决策
