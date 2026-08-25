# JNPF-Next NG-1 领域与数据 Ownership 设计规格 v1.0

**日期**：2026-08-26 ｜ **裁决依据**：NG-0 APPROVE + NG-1 APPROVE（有条件）——NG-1 = **Domain & Data Ownership Proof + D12 Architecture Slice 证伪**
**状态**：已批准执行（2026-08-26 人工裁决）——批准范围限定为「Ownership Proof + D12 Slice 证伪」，不批准任何微服务实现
**不批准清单**（本阶段及 NG-1 执行期均冻结）：12 微服务正式拆分 / 全库迁移 / 大规模数据库改造 / 全面 Aspire 化 / 全面 UniApp 重写 / S2 Legacy 数据访问抽象 / P1 旧代码结构优化

---

## 0. 人工批准裁决记录（APPROVE 附条件，2026-08-26）

> **批准范围限定为「Domain & Data Ownership Proof + D12 Architecture Slice 证伪」，不批准任何微服务实现。**

1. 批准执行本规格及《D12 Architecture Slice 实施计划 v1.0》；
2. 必须先完成 289 表六维 Ownership Matrix；`UNKNOWN`、`OWNERSHIP-CONFLICT` 不得自行猜测或进入拆分候选，必须进入人工裁决；
3. D12 不以「证明 Order 应该微服务化」为目标，而以「证明或证伪 Order 是否具备独立领域、数据、事务、查询、租户、权限和迁移边界」为目标；
4. 增加 **BOUNDARY-PROOF** 闸门（§2.4）：任何候选领域必须完成 Ownership / Transaction / Query / Tenant / Permission / Migration / Performance 七类证据后，才允许进入后续 Service Boundary 设计；
5. 增加反证机制：若 D12 无法满足独立边界，必须登记 BLOCK/REFINE 原因，不得为了微服务目标强行拆分；
6. 「独立数据库」暂不作为 NG-1 预设结论，必须由 Ownership + Transaction + Migration Proof 推导（§4 推导链）；
7. Aspire 继续保持工具层定位（§7），不得作为拆分依据；禁止因采用 Aspire 而预设服务数量、消息总线或分布式基础设施；
8. NG-1 全程只读/影子执行，不修改 Legacy 业务代码、数据库和 API，不启动微服务实现；
9. NG-1 完成后必须停止，提交 **ARCHITECTURE DECISION：PASS / REFINE / BLOCK**，由人工裁决后才允许进入 NG-2。

> **当前 Legacy S2 继续保持 BLOCKED；P1、P0-C 不因 NG-1 自动启动。**
> **本次批准的目标不是开始「微服务重构」，而是用一个真实领域 Slice 证明我们是否找到了正确的新架构边界。**

---

## 0A. 架构纠偏裁决记录（NG-1A 启动，2026-08-26）

> **依据第一批实测结果，立即暂停 D12 Order Architecture Slice，不进入第二批 Ownership，也不得基于 Order/ext_*/WM_*/WH_* 推导微服务边界。**

**纠偏依据（实测）**：数据库存在 Order/Product/Customer/Warehouse/WorkLog 等表，不能证明这些是低代码平台自身领域——WM/WH 42 张孤儿表（代码零引用但 DB 有真实数据）恰是「历史/演示/遗留资产」强信号；ext_* 19 张表实为 12 个子域服务的打包容器。**Ownership 之前必须先做产品资产归属识别。**

**NG-1 新增 NG-1A：Platform Product Boundary Audit（平台产品资产边界审计）**，审计顺序修正为：

```text
数据库
 ↓
① 产品资产归属识别（NG-1A）
 ↓
② 平台核心数据集
 ↓
③ 非平台/演示/历史/客户数据排除
 ↓
④ 核心数据 Ownership
 ↓
⑤ Domain Boundary
 ↓
⑥ Architecture Slice
 ↓
⑦ 是否值得微服务化
```

### 0A.1 ProductAssetClass（289 表最高层分类字段）

| 分类 | 含义 |
|------|------|
| CORE_PLATFORM | 低代码平台自身不可缺少的数据 |
| PLATFORM_SUPPORT | 平台运行基础设施数据 |
| LOWCODE_RUNTIME | 动态表、动态字段、表单、流程等运行时数据 |
| DEMO_APPLICATION | 官方演示应用 |
| SAMPLE_APPLICATION | 示例/模板应用 |
| CUSTOMER_GENERATED | 用户通过低代码平台创建的业务数据 |
| TEST_FIXTURE | 测试数据 |
| LEGACY | 历史遗留 |
| UNKNOWN | 无法证明 |
| EXTERNAL | 外部/第三方模块 |

> **UNKNOWN ≠ CORE_PLATFORM**：不能因为「不知道是什么」就纳入平台核心。

### 0A.2 证据链（每张表必须经过）

```text
DB metadata → Code reference → Runtime reachability → API/UI reachability
→ Migration/configuration → Tests → Deployment dependency → Data characteristics → Historical provenance
```

### 0A.3 硬规则（违反任一条 = 违规）

1. `UNKNOWN` 不得归入 CORE_PLATFORM。
2. Demo/Sample/Customer/Legacy/Orphan 不得参与 Domain Ownership Proof。
3. 只有被证明属于 CORE_PLATFORM 或 LOWCODE_RUNTIME 的资产，才能进入后续 Domain/Ownership 分析。
4. Order/Customer/Product/Warehouse 等业务表不得因名称或 Service 名称自动成为 Domain。
5. WM/WH 42 张孤儿表作为**历史污染检测样本**单独登记，不得直接定义为 Warehouse Domain。
6. ext_* 必须重新证明其 ProductAssetClass；当前暂定 UNKNOWN，不得继续作为 D12 Architecture Slice 的既定边界。
7. 现有 D12 Order Slice 改名为 **Candidate Slice（待验证业务资产切片）**，保留已有实测证据，但暂停 Architecture Decision。
8. 不删除、不修改任何历史表和数据，只做只读归属审计。

### 0A.4 新增 Gate G0（Product Boundary Proof）

```text
G0 = Product Boundary Proof
只有 G0 PASS → 才允许进入 Domain Ownership Proof
             → 才允许重新定义 D12 Candidate Slice
             → 才允许进行 Microservice Boundary Proof

无法证明属于平台的资产 → 登记 UNKNOWN / LEGACY，不强行解释。
```

### 0A.5 NG-1A 停止条件（6 项产出物）

1. `platform-asset-classification.csv`
2. `core-platform-data-inventory.md`
3. `demo-sample-legacy-registry.md`
4. `asset-provenance-map.md`
5. `product-boundary-proof.md`
6. `NG-1A Final Review`

**本轮五零约束**：零业务代码修改 / 零数据库修改 / 零微服务实现 / 零 Aspire 引入 / 零迁移。完成 NG-1A 后 STOP，等待人工裁决。

### 0A.6 资产模型升级裁决（P0-PX 十类 + 二维分类，2026-08-26）

**裁决源**：用户对十类体系的架构级升级——「平台为展示/验证/提供模板而预置的业务数据 ≠ 平台自身业务领域」；Template 必须与 Demo 分开建模。

#### 0A.6.1 四层资产模型

```text
JNPF 产品
├── A. Platform Core       —— 平台自身必须存在的数据
├── B. Low-Code Runtime    —— 用户应用/模型运行所依赖的元数据
├── C. Product Templates / Sample Apps —— CRM/ERP/OA/项目模板、示例流程、示例表单、示例数据
└── D. External / Customer Applications —— 用户真正创建出来的业务系统
```

**C 类特殊**：属于 JNPF 产品交付内容，但不属于平台核心架构。`ext_order` 等若实为「预置模板业务数据」→ `PRODUCT_TEMPLATE`，而非 `Domain = Order`。**存在 OrderService ≠ 存在 Order 领域**（硬规则 4 强化）。

#### 0A.6.2 二维分类（替换单字段十类）

- **PlatformRole**：CORE / RUNTIME / PRODUCT_CONTENT / EXTERNAL / LEGACY / UNKNOWN
- **AssetLifecycle**：MANDATORY / OPTIONAL / TEMPLATE / DEMO / CUSTOMER_GENERATED / TEST / LEGACY / ORPHAN / UNKNOWN

典型组合：租户核心表=CORE+MANDATORY；动态表元数据=RUNTIME+MANDATORY；CRM 模板表=PRODUCT_CONTENT+TEMPLATE；官方 Demo=PRODUCT_CONTENT+DEMO；客户订单=EXTERNAL+CUSTOMER_GENERATED；老 WM/WH 表=LEGACY+LEGACY；无代码引用的历史表=LEGACY+ORPHAN。

#### 0A.6.3 最终分类体系（P0-PX，替换 0A.1 旧十类）

| 类 | 含义 | 进 NG 设计 |
|----|------|-----------|
| P0 PLATFORM_CORE | 平台自身必须数据 | ✅ 唯一允许进入领域/Ownership 分析 |
| P1 LOWCODE_RUNTIME | 低代码运行时元数据 | ✅ 唯一允许进入领域/Ownership 分析 |
| P2 PRODUCT_TEMPLATE | 产品交付模板内容 | ❌ 独立模板包，不进 Platform Core |
| P3 DEMO_APPLICATION | 官方演示应用 | ❌ 可删除/隔离，不进新架构 |
| P4 CUSTOMER_APPLICATION | 用户创建的业务系统 | ❌ 不进入平台核心 |
| P5 TEST_FIXTURE | 测试数据 | ❌ 清理或隔离 |
| P6 LEGACY | 历史遗留（有来源已弃用） | ❌ 归档/迁移/清理 |
| P7 ORPHAN | 彻底孤儿（无来源无引用） | ❌ 隔离 |
| P8 EXTERNAL | 外部/第三方模块 | ❌ 不进入平台核心 |
| PX UNKNOWN | 无法证明 | ⏸ BLOCKED 直至证明 |

> **只有 P0/P1 允许进入「平台领域与数据 Ownership」分析。P2/P3 虽属产品交付资产，但不得污染平台核心领域模型。P4 更不能进入平台核心。P5/P6/P7 原则上清理或隔离。PX 必须保持 BLOCKED。**

#### 0A.6.4 OrderService 四种可能（ext_* 判定问题）

```text
① 平台核心 Order          → Domain Ownership → Service Boundary → Microservice Candidate
② 平台官方业务模板        → Product Template → 独立模板包 → 不进 Platform Core
③ 官方 Demo 应用          → Demo Application → 可删除 → 不进新架构
④ 历史客户/开发测试应用  → Legacy/Customer Asset → 归档/迁移/清理
```

四种情况架构结论完全不同。当前证据（12 个编译期服务 + init 打包 + API 可达）只能证明「预置可运行内容」，P2/P3 细分需 Provenance Matrix。

#### 0A.6.5 Product Capability Fixture（模板的第二种价值）

订单数据本身不是平台核心，但「平台能正确创建并运行订单模板」是平台能力的重要验收场景——订单表可作为 Product Capability Fixture，用于验证建模/表单/权限/流程/查询/数据权限/性能。**验证对象是平台能力，而非订单领域本身。**

#### 0A.6.6 下一步指令：Provenance Matrix（替代 D12 Order Ownership）

NG-1A 初判完成后，下一步为 **Provenance Matrix**：对 289 表 + 代码模块 + 初始化 SQL + migration/seed + UI/菜单 做创建来源追踪（谁创建/何时创建/哪个 SQL/哪个模块拥有/哪个代码写/哪个 API 暴露/哪个 UI 使用/是否平台启动必须/是否模板安装后才出现/是否 Demo 脚本产生/是否客户建模产生/删掉后平台核心是否正常），**优先追踪 ext_* / WM_* / WH_* / base_* / sa_***。每张表必须获得**可证明的来源身份**后，才可进入 Domain Ownership Proof。

### 0A.7 G0 条件 PASS 裁决 + NG-1B Provenance Matrix 启动批准（2026-08-26）

**裁决**：NG-1A 验收通过；G0 不最终 PASS，登记 **`PASS-PENDING-PROVENANCE`**。已完成「平台资产 vs 非平台资产」第一轮边界证明（289 → 159/130/6），但尚未完成全部 Provenance Proof，**不得进入 Domain Ownership Proof，不得恢复 D12**。

**批准启动 NG-1B：Provenance Matrix**（只读审计）：
- 对象：289 张表 × 14 维（DB Object / Creation Source / Code Owner / Write Owner / Read Consumers / API / UI-Menu / Template / Demo / Runtime / Startup / Product / Lifecycle / Provenance）
- 每表终态：**`PROVEN` / `PARTIAL` / `UNKNOWN`**（证据驱动，非主观判断）
- 优先追踪：**ext_* / WFORM_* / WM_* / WH_* / base_* / sa_***

**本轮禁止项**：不进入 Domain Ownership Proof；不恢复 D12；不进行任何微服务设计；不删除 42 张孤儿表（仅 Provenance + 处置建议 ARCHIVE/DELETE/MIGRATE/DEMO/LEGACY）；对 PX UNKNOWN 不得猜测归属（**UNKNOWN 永远不能自动变成 PLATFORM_CORE**）；不真删生产数据库。

**完成条件**：Provenance Matrix 完成后重新 G0 Final Review，最终只允许 **`PASS / REFINE / BLOCK`**。无论结果如何，完成后 STOP，等待人工裁决。

**后续候选（不本批执行）**：Platform Independence Proof（删除/隔离实验，影子环境验证核心能力全链：启动 → 登录 → 创建应用 → 建模 → 表单 → 发布 → 运行）。

---

## 1. NG-1 目标重定义

> NG-1 不是「把设计做到满意」，而是回答一个核心问题：
> **289 张表的数据 Ownership 是否已经足够清晰到可以支撑领域边界裁决？**

> **目标边界（裁决锁定）：** NG-1 的目标不是证明「应该微服务化」，而是证明**哪些领域具备独立 Ownership、独立事务边界、可接受的跨域查询成本以及可迁移性**。只有经过 D12 Slice 实测证明的领域，才允许进入后续独立服务设计。Aspire 不得成为架构决策依据，只作为未来编排、可观测性和本地开发基础设施。

NG-1 唯一产出物：**六维矩阵（289 表全量）** + **Anti-Service/Shared-Core 清单** + **D12 Architecture Slice 实测证据** → 触发 **Architecture Gate A-G + BOUNDARY-PROOF** → 产出 `ARCHITECTURE DECISION`（PASS/REFINE/BLOCK，不提前规定答案）。

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

### 2.4 BOUNDARY-PROOF 闸门（裁决新增，任何拆分候选的强制前置）

> 任何候选微服务都必须证明**「为什么应该独立」**，而不是仅仅证明「可以独立」。**没有 Proof，就不能成为 Microservice。**

每个候选域必须回答并给出证据：

| # | 问题 | 证据 |
|---|------|------|
| 1 | 谁拥有数据？ | Write Owner |
| 2 | 谁决定数据生命周期？ | Lifecycle Authority |
| 3 | 谁负责业务规则？ | Decision Authority |
| 4 | 谁读取它？ | Consumer Map |
| 5 | 跨域查询多少？ | Join Matrix |
| 6 | 是否需要同步调用？ | Sync Dependency |
| 7 | 是否存在强事务？ | ACID Boundary |
| 8 | 是否强依赖 Tenant？ | Tenant Boundary |
| 9 | 是否强依赖 Authorization？ | Permission Boundary |
| 10 | 能否独立数据库？ | Database Independence |
| 11 | 能否独立部署？ | Deployment Independence |
| 12 | 能否独立演进？ | Contract Independence |
| 13 | 迁移如何完成？ | Migration Proof |
| 14 | 性能是否可接受？ | Latency/Throughput Proof |

```text
Candidate Domain
      │
      ├── Ownership Proof
      ├── Transaction Proof
      ├── Query Proof
      ├── Tenant Proof
      ├── Permission Proof
      ├── Migration Proof
      └── Performance Proof
                │
                ▼
       SERVICE BOUNDARY PROVEN（否则 BLOCK/REFINE）
```

**反证机制**：无法满足独立边界的发现必须登记 BLOCK/REFINE 及原因（D12 反证表，D12 计划 §4.1），不得为微服务目标强行拆分。「某域当前不具备独立服务条件」是同等有价值的架构结论，可继续寻找更合适的第一个 Slice（如 File/Dictionary/Notification/Workflow）。

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
 ├── Order Service（D12 切片证明后才提取）
 └── File Service（零依赖先行）

Phase 3（远期，触发条件达成才进入）：
JNPF Next
 ├── Identity Service + Identity DB
 ├── Authorization Service
 ├── Workflow Service
 └── Core Modules（剩余）
```

（Phase 2 中的独立 DB 表述按裁决第 6 条不再预设——独立 DB 须经下方推导链证明后才成立。）

**独立数据库推导链**（裁决第 6 条——独立 DB 非 NG-1 预设结论）：

```text
Domain Ownership → Logical Data Boundary → Independent Write Ownership
                 → Migration Proof → Independent Database
```

先证明「Order 是 Order 数据的唯一 Write Owner」，才讨论「Order 数据是否应迁移到独立数据库」。禁止未经证明直接演化为 Order DB + User DB + Permission DB + Tenant DB 的**分布式单体**。

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
| **G D12** | Order Slice 边界在真实业务压力下成立还是被证伪？ | D12 计划 §4 验收（五面，PASS/REFINE/BLOCK 三态） |

**Gate 全绿** → 产出 `ARCHITECTURE DECISION`：**PASS**（形态 A Modular Monolith / B Hybrid / C Microservices——按实测证据裁决，不提前预设）。
**Gate 部分通过** → **REFINE**（回 NG-1 补充证据或缩小范围）。
**Gate 红（D12 证伪）** → **BLOCK**（该域维持单体内，如实登记，不做拆分；寻找更合适 Slice）。
**NG-1 完成后必须停止**，由人工裁决后才允许进入 NG-2（裁决第 9 条）。

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
- 禁止「用了 Aspire 所以必须 12 个 Service」的倒推逻辑（NG-0 Aspire 规格 §1 保持）；
- **Aspire 不得成为架构决策依据**——只作为未来编排、可观测性和本地开发基础设施（裁决第 7 条）。

## 8. NG-1 产出物清单

| # | 产出 | 形式 |
|---|------|------|
| 1 | 六维矩阵（289 表全量） | CSV + 证据文档 |
| 2 | Ownership 分类（OWNED/SHARED/UNKNOWN）+ 冲突裁决表 | 证据 |
| 3 | Anti-Service/Shared-Core 清单（含解除条件） | 证据 |
| 4 | 跨域 Join 清单 + API/Read Model 化可行性 | 证据（Gate C 输入） |
| 5 | ACID 事务清单（逐域） | 证据（Gate B 输入） |
| 6 | D12 Architecture Slice 实测报告（五面） | 证据（Gate G 输入） |
| 7 | Architecture Gate A-G 逐项判定 + BOUNDARY-PROOF 证据 + D12 反证表 | NG-1 Final Review |
| 8 | ARCHITECTURE DECISION（PASS / REFINE / BLOCK） | NG-1 Final Review |

**本规格 + D12 实施计划已于 2026-08-26 获人工有条件批准（§0），NG-1 执行启动。**
