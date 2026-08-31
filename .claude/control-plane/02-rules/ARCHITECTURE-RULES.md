# Architecture Rules — 架构规则索引

> **分类：** L1 项目规则
> 
> **来源：** `.claude/rules/architecture-redlines.md`

---

## R1-R12 架构红线

| ID | 规则名称 | 执行层 | 说明 |
|----|---------|--------|------|
| R1 | API Generation | L2 | Service 实现 IDynamicApiController，永远不写 Controller |
| R2 | Unified Response | L2 | RESTfulResult<T> 自动包装，Oops.Bah() vs Oops.Oh() |
| R3 | Codegen Boundary | L2 | 生成代码 bug → 修 .vm 模板 |
| R4 | Multi-Tenant Isolation | **L0** | SqlSugar 查询必须租户过滤 |
| R5 | Module Boundary | **L0** | OA 禁用，IoT/MES 不存在 |
| R6 | Frontend Memory Safety | **L0** | setTimeout/EventSource 6 条铁律 |
| R7 | SQL Injection Defense | **L0** | 动态 SQL 必须参数化 |
| R8 | API Permission | **L0** | 每个 IDynamicApiController 必须权限声明 |
| R9 | Architect Fidelity | L2 | 编码前输出需求提取清单 |
| R10 | Bug Discovery Protocol | L2 | Bug 发现必须上报 |
| R11 | S2 Compile 主链 | L2 | compile vs agent 模式边界 |
| R12 | Triple-Key | L2 | 三元组完整性 |

---

## Hook 覆盖

| 红线 | Hook | 拦截内容 |
|------|------|---------|
| R4 | `guard-tenant-filter.mjs` | 跨租户数据泄漏 |
| R5 | `guard-oa-module.mjs` | OA/IoT/MES 路径写入 |
| R6 | `guard-frontend-leak.mjs` | 内存泄漏 |
| R7 | `guard-sql-injection.mjs` | SQL 注入 |
| R8 | `guard-auth.mjs` | API 权限缺失 |

---

## ADF 三先行

**来源：** `.claude/rules/architecture-design-interface-first.md`

| 阶段 | 职责 |
|------|------|
| P0 | Business First Q1-Q3 |
| P1 | 层边界、唯一源、三元组、≥2 方案 |
| P2 | 模式映射 SkillHarness/Gate/IR |
| P3 | 签名/DTO/事件契约 |
| P4 | 实现 + 节点审批 |

---

## 断言纪律

**来源：** `.claude/rules/assertion-discipline.md`

使用标签：
- [KNOWN] - 已验证的事实
- [COMPUTED] - 计算得出的结论
- [INFERRED] - 推断得出的结论
- [GUESS] - 猜测（需标注置信度）

---

## 关联文档

- `.claude/rules/architecture-redlines.md` — 完整架构红线
- `.claude/rules/architecture-design-interface-first.md` — ADF 三先行
- `.claude/rules/assertion-discipline.md` — 断言纪律
