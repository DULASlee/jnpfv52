# JNPF-Next NG-1 领域与数据 Ownership 设计规格 v1.0

**日期**：2026-08-26 ｜ **裁决依据**：NG-0 APPROVE（人工裁决）——NG-1 = **Domain & Data Ownership Proof + D12 Architecture Slice**
**状态**：设计规格（本阶段只产出规格+计划，不实施；NG-1 执行待批准）
**不批准清单**（本阶段及 NG-1 执行期均冻结）：12 微服务正式拆分 / 全库迁移 / 大规模数据库改造 / 全面 Aspire 化 / 全面 UniApp 重写 / S2 Legacy 数据访问抽象 / P1 旧代码结构优化

---

## 1. NG-1 目标重定义

> NG-1 不是「把设计做到满意」，而是回答一个核心问题：
> **289 张表的数据 Ownership 是否已经足够清晰到可以支撑领域边界裁决？**

NG-1 唯一产出物：**六维矩阵（289 表全量）** + **Anti-Service/Shared-Core 清单** + **D12 Architecture Slice 实测证据** → 触发 **Architecture Gate A-G** → 产出 `ARCHITECTURE DECISION`（A/B/C 三选一，不提前规定答案）。

## 2. 六维矩阵方法论（核心）

### 2.1 矩阵定义

| 维度 | 定义 | 判定方法 |
|------|------|---------|
| **Domain** | 候选域归属（D1-D12，NG-0 证据 5） | 表前缀 + 实体模块 + 写路径聚类 |
| **Write Owner** | **拥有数据业务生命周期与写入决策权的一方**（≠ 谁调用 IRepository） | 判定规则见 §2.2 |
| **Read Consumers** | 读取方清单（含跨域读） | 调用链提取（Join/查询/API 消费）+ 代码审计 |
| **Tenant Scope** | 租户作用域：列级（f_tenant_id 等）/ 连接级（切库）/ 无租户 | DB 元数据实测（NG-0 证据 1）+ 过滤挂靠点（ITenantFilter 12 文件） |
| **Transaction Boundary** | 必须保持 ACID 的操作集合 | 代码事务扫描（TransactionScope/仓储事务）+ 业务语义 |
| **Cross-Domain Dependency** | 跨域依赖方向与强度 | 六维矩阵自身的 Join/引用关系汇总 |

### 2.2 Write Owner 判定规则（关键）

Owner ≠ 当前 IRepository 调用者。按以下优先级判定：

1. **生命周期权**：谁创建/归档/删除该数据（含软删）——创建与删除路径是强信号；
2. **决策权**：谁决定数据内容变更（业务规则归属），而非谁执行写语句；
3. **契约权**：谁定义该数据的 schema/校验/版本（如 ai_entity_field 的字段唯一源）；
4. 冲突时标记 `OWNERSHIP-CONFLICT` 进入人工裁决表（不猜测）。

### 2.3 分类结果集（Gate A 输入）

每张表最终落入：

```text
OWNED   —— 单一明确 Owner（可进入正式拆分候选）
SHARED  —— 多域读写共享（进入 Anti-Service 评估，§3）
UNKNOWN —— 无法判定（不允许进入任何拆分候选；人工裁决）
```

> **UNKNOWN 不能进入正式拆分**（Gate A 硬约束）。

## 3. Anti-Service / Shared-Core 清单（禁止拆分清单）

### 3.1 判定标准（任一命中即暂不能独立成服务）

```text
A. 5+ 模块写入同一数据
B. 8+ 模块 Join 同一数据
C. 每次查询都同步依赖该数据（如权限评估）
D. 强事务依赖（与其他域的操作必须同事务）
E. 租户上下文强绑定（租户隔离语义内嵌）
```

### 3.2 候选清单（NG-1 必须逐项验证，非预设结论）

| 数据/域 | 命中信号（NG-0 证据） | 初判 |
|---------|---------------------|------|
| Identity（base_user 等） | 全域 Join（B）+ 登录/审计强绑定（D） | **Shared-Core**（暂不可拆——NG-0 证据 2/4 一致） |
| Tenant（zx_sys_db + 列过滤） | 219 表租户列（E）+ 连接切库（E） | **Shared-Core** |
| Authorization（base_authorize/module/scheme） | 每查询权限三连查（C）+ 双路径消费（C） | **Shared-Core**（快照化前） |
| Dynamic Form Metadata（base_visualdev_* + mt*） | 运行时 DDL（E）+ 元数据/数据事务分离（D） | **Shared-Core**（注册表化前） |
| 核心 Dictionary（base_dictionary_data） | 全域渲染读（B）+ 表单 DSL 依赖（D） | **Shared-Core**（API/缓存化前） |
| 跨域事务表（如 flow_form_authorize 等交叉表） | 无 PK + 双域写（A） | **Shared-Core**（归属裁决前） |

### 3.3 清单使用方式

- 清单内数据**不进入任何独立服务拆分候选**（NG-1 至 NG-2 有效）；
- 清单项的「解除条件」显式登记（如 Authorization → 权限快照上线后重评）；
- 清单随六维矩阵实测结果修订（NG-1 产出物之一）。

## 4. 共享数据库的演化路径（Phase 1/2/3）

> 禁止「服务化了但共享 DB」的**分布式单体**形态。共享库逐步消失按三阶段：

```text
Phase 1（NG-1/2 目标）：
JNPF Next ── Modular Monolith（单库，域内逻辑隔离 + 架构测试强制）

Phase 2（NG-3 验证）：
JNPF Next
 ├── Core Modular Monolith（Identity/Tenant/Authorization/Form/Workflow）
 ├── Order Service + Order DB（D12 切片成功后提取）
 └── File Service + File DB（零依赖先行）

Phase 3（远期，触发条件达成才进入）：
JNPF Next
 ├── Identity Service + Identity DB
 ├── Authorization Service
 ├── Workflow Service
 └── Core Modules（剩余）
```

**阶段跃迁条件**（全部满足才进下一阶段）：
1. 该域六维矩阵 OWNED 且无 Anti-Service 命中；
2. 跨域读取已 API/Read Model 化（无直连）；
3. 事务边界已出箱/事件化（D12 验证）；
4. 租户/权限语义在契约层完整（NG-1 Gate D/E 通过）。

## 5. Architecture Gate A-G（NG-1 停止条件）

| Gate | 问题 | 通过判据 |
|------|------|---------|
| **A Ownership** | 289 表能否全量分类 OWNED/SHARED/UNKNOWN？ | UNKNOWN=0 或全部人工裁决且不进拆分；OWNED≥60%（否则回 REFINE） |
| **B Transaction** | 每个候选域哪些事务必须 ACID？ | 逐域 ACID 清单 + 跨域事务显式裁决（同库 or 出箱化） |
| **C Query** | 哪些跨域查询可 API/Read Model 化？ | 跨域 Join 清单 + 每项 API/Read Model 化可行性裁决 |
| **D Tenant** | Tenant 是平台级上下文还是某域数据？ | 租户模型裁决（平台级上下文 + 列/连接双机制契约化——NG-0 租户规格 §2） |
| **E Permission** | 权限计算属 Authorization 域还是 Query 基础设施？ | 裁决：评估 API（Authorization 域）+ 快照注入（Query 侧消费） |
| **F Migration** | Legacy→Next 如何双写/同步/校验/切流？ | 每迁移波次（W1-W8）双写/校验/切流方案出齐 |
| **G D12** | Order Slice 边界在真实业务压力下成立？ | D12 实施计划 §6 验收全绿（数据/查询/事务/迁移/性能五面） |

**Gate 全绿** → 产出 `ARCHITECTURE DECISION`（A Modular Monolith / B Hybrid / C Microservices——按实测证据裁决，不提前预设）。
**任一 Gate 红** → 回 NG-1 REFINE 或人工裁决缩小范围。

## 6. Legacy 的正式定位（新关系）

> **Legacy ≠ Next 的代码模板。** Legacy 仅四作用：

```text
Legacy
 ├── Behavior Reference（行为参考——Q1-Q11/E-PB1…/33+43 特征测试）
 ├── Compatibility Baseline（兼容基线）
 ├── Data Migration Source（迁移数据源）
 └── Business Knowledge Base（业务知识库）
```

- 上述特征资产正式归入 **Legacy Compatibility Registry**（NG-0 证据 8 的 KEEP/REDEFINE/DEPRECATE/REMOVE 裁决是注册表内容）；
- **Next 不复制造 Legacy 实现行为**（怪异项 REMOVE/REDEFINE 按注册表执行）；
- Legacy 代码继续冻结（P0-C ⏸ / S2 🔒 / P1 🔒）。

## 7. Aspire 定位固化

```text
Aspire = Development Orchestration / Service Discovery / Configuration /
         Observability / OpenTelemetry / Health / Local Environment
```
- **架构决定服务，Aspire 负责把服务运行得更好**；
- 禁止「用了 Aspire 所以必须 12 个 Service」的倒推逻辑（NG-0 Aspire 规格 §1 保持）。

## 8. NG-1 产出物清单

| # | 产出 | 形式 |
|---|------|------|
| 1 | 六维矩阵（289 表全量） | CSV + 证据文档 |
| 2 | Ownership 分类（OWNED/SHARED/UNKNOWN）+ 冲突裁决表 | 证据 |
| 3 | Anti-Service/Shared-Core 清单（含解除条件） | 证据 |
| 4 | 跨域 Join 清单 + API/Read Model 化可行性 | 证据（Gate C 输入） |
| 5 | ACID 事务清单（逐域） | 证据（Gate B 输入） |
| 6 | D12 Architecture Slice 实测报告（五面） | 证据（Gate G 输入） |
| 7 | Architecture Gate A-G 逐项判定 | NG-1 Final Review |
| 8 | ARCHITECTURE DECISION（A/B/C） | NG-1 Final Review |

**本规格 + D12 实施计划经人工批准后，NG-1 执行才启动。**
