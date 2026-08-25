# D12 Architecture Slice 实施计划 v1.0（NG-1 核心执行单元）

**日期**：2026-08-26 ｜ **关联**：《JNPF-Next-NG1-领域与数据Ownership设计规格》
**定位**：D12 从「沙盘」升级为 **Architecture Slice**——验证一整条真实链（Order → User/Tenant/Permission/Query/Repository/Transaction/Database），**不是验证 Order 自身能否运行**。
**状态**：已批准执行（2026-08-26 人工裁决——NG-1 有条件批准范围内）

---

## 0. 第一批批准裁决记录（2026-08-26）

> **批准启动 NG-1 第一批。** 严格按照 NG-1 规格 v1.0 与 D12 计划 v1.0 执行，不再扩大范围。

### 0.1 A. 289 表 Ownership Matrix

1. 建立 289 表 × 六维矩阵；
2. Domain / Tenant Scope 优先使用数据库与代码实测证据；
3. Write Owner 必须追踪真实写入路径，不得以 Repository 所属项目或 Service 名称推断 Owner；
4. Read Consumer 必须追踪实际查询/Join/调用关系；
5. Transaction Boundary 必须以真实事务包裹和一致性要求为依据；
6. Cross-Domain Dependency 必须登记实际依赖；
7. `UNKNOWN` 与 `OWNERSHIP-CONFLICT` 一律进入人工裁决表，禁止 AI 猜测归属。

### 0.2 B. D12 Order Slice S1

1. 全量盘点 Order 写入路径；
2. 全量盘点 Order → User/Tenant/Authorization/DataPermission/Dictionary/Dynamic Metadata 等依赖；
3. 建立实际 Join/查询清单；
4. 识别同步调用与同步权限计算；
5. 识别真实 ACID 事务边界；
6. 复跑并确认已有 43 项路径 B 行为基线，但**不得修改实现**；
7. 建立 D12 当前性能/延迟基线；
8. 不进行数据库拆分、不建立新服务、不修改 Legacy API。

### 0.3 强制执行纪律

- 全程只读/影子分析；不修改 Legacy 业务代码；不修改数据库结构及数据；不创建微服务；不迁移数据；不引入 Aspire Runtime/Broker/Redis/MQ 等基础设施；不因为发现「适合拆分」而提前设计实现；
- 每一个 Ownership 结论必须有 `文件:行号 / SQL / 调用链 / 测试 / 数据库元数据` 等可追溯证据。

### 0.4 反证要求（审计强制）

不要只寻找支持拆分的证据，也必须主动寻找反证。对 D12 每一个潜在边界同时回答：

```text
为什么可以独立？
为什么不能独立？
哪些依赖可以通过 API / Event / Read Model 消除？
哪些依赖属于真正的强一致性依赖？
哪些依赖只是 Legacy 实现造成的偶合？
```

若发现 Order 当前不具备独立服务条件，必须如实输出 `BLOCK` 或 `REFINE`，**不得为了 NG 架构目标强行证明 PASS**。

### 0.5 第一批停止条件

第一批完成后立即停止，仅提交：

1. Ownership Matrix
2. UNKNOWN / OWNERSHIP-CONFLICT 清单
3. D12 Query / Join Map
4. D12 Transaction Map
5. D12 Permission / Tenant Dependency Map
6. D12 Performance Baseline
7. 初步 BOUNDARY-PROOF
8. 风险与反证清单

**暂不进行 Gate G 最终裁决，不进入 NG-2，不开始微服务实现。** 第一批完成后汇报实测结果，由人工决定是否进入下一阶段。

---

## 1. 切片定义

```text
范围：Order 域（WM_BillDetail/WM_Material/WM_CheckBillDetail/WH_* 相关）
真实链：Order 数据 → User 引用 → Tenant 引用 → Permission 数据权限 → Query 组装 → Transaction → Database
验证面：数据 / 查询 / 事务 / 迁移 / 性能（五面）

**目标（裁决锁定）**：不以「证明 Order 应该微服务化」为目标，而以**证明或证伪 Order 是否具备独立领域、数据、事务、查询、租户、权限和迁移边界**为目标。证伪（BLOCK）是同等有价值的架构结论。
```

**禁止**：改造 Legacy 实现 / 建新库 / 新服务进程 / 修改任何生产表结构。D12 是**测量与验证实验**（只读 + 影子执行），不是改造。

## 2. 五面验证设计

### 2.1 数据面（Data Ownership Proof）

| 项 | 内容 |
|----|------|
| Order 数据 Owner | WM_*/WH_* 写路径归属（创建/归档/删除=OrderService 生命周期权） |
| User 引用 | OrderEntity.CreatorUserId → base_user 引用形态（Join or 快照） |
| Tenant 引用 | Order 表 f_tenant_id 列级作用域 + 连接切库是否参与 |
| Permission 引用 | OrderService L83 GetConditionAsync 消费（路径 B 唯一消费者） |

**产出**：Order 域六维矩阵条目 + 冲突检查。

### 2.2 查询面（Query Proof）

| 项 | 测量 |
|----|------|
| Join 盘点 | OrderService.GetList 三表 Join（Order+User+FlowTask）+ 子列表/详情 Join 清单 |
| API 化可行性 | User 读取 → Identity API；FlowTask 读取 → Workflow API；权限 → 评估 API（影子实现） |
| Read Model 评估 | OrderListOutput 是否天然读模型（现状 Select 投影）——能否缓存/物化 |
| 缓存评估 | 权限条件（路径 B 43 特征）缓存化收益 |
| 延迟基线 | 当前查询 ToSql() 实测 + 执行时间基线（记录，不优化） |

### 2.3 事务面（Transaction Proof）

```text
Create Order 链（现状）：Permission → User → Order(主表+明细) → Audit
```

| 问题 | 验证方式 |
|------|---------|
| 该链是否必须单库 ACID？ | 代码事务扫描（TransactionScope/仓储事务包裹范围）+ 语义判定 |
| 哪些段可出箱/事件化？ | 候选：Audit → 事件；Search/Notification → 事件（SYS_EVENT_OUTBOX_MESSAGE 模式参照） |
| 出箱化后一致性模型？ | AtLeastOnce + 幂等（现有 outbox 表验证） |

**结论形式**：ACID 保留段清单 + 事件化段清单（Gate B 输入）。

### 2.4 迁移面（Migration Proof）

| 项 | 内容 |
|----|------|
| 双写方案 | Next 查询侧影子执行（同请求双查比对） |
| 校验 | 结果集特征比对（行数/键集合/条件注入形态） |
| 切流 | 灰度比例切流方案（10%→100% + 回滚条件） |
| 约束 | Legacy 不停摆；特征测试（P0-B 43 特征）为判据 |

### 2.5 性能面（Performance Proof）

| 项 | 测量 |
|----|------|
| 基线 | 现状 GetList 延迟/查询数（Join 数/子查询） |
| 影子对比 | API 化/Read Model 化后的理论延迟差（测量，不实施） |
| 权限条件成本 | 三连查（authorize→module→scheme）耗时占比 |
| 输出 | 延迟对比表（现状 vs 影子）——Gate G 判据 |

## 3. 执行步骤（S1-S5 协议，测量导向）

| 步骤 | 内容 | 产出 |
|------|------|------|
| S1 测量基线 | Order 链全量只读盘点：Join 清单/SQL 形态/事务包裹/权限条件（43 特征复跑） | 五面基线证据 |
| S2 影子设计 | 影子实现设计（API 化/Read Model/缓存——**仅设计+测量脚本**，不改业务） | 设计稿 |
| S3 影子执行 | 测试环境影子查询 + 结果比对（不改 Legacy 代码） | 比对报告 |
| S4 事务/迁移判定 | ACID 段 + 事件化段 + 双写/切流方案定稿 | Gate B/F 输入 |
| S5 验收 | Gate G 五项全绿 + Slice 报告 | Final Review |

## 4. 验收标准（Gate G）

| # | 判据 |
|---|------|
| G-1 | Order 域六维矩阵完成，OWNED 或冲突已人工裁决 |
| G-2 | 跨域 Join 清单 + 每项 API/Read Model 化可行性裁决出齐 |
| G-3 | ACID 保留段/事件化段清单出齐（事务边界成立或明确不可拆） |
| G-4 | 影子比对通过（特征一致，无权限/租户语义漂移） |
| G-5 | 性能基线+影子对比数据出齐（不要求优化，要求数据） |

**全绿** → 该边界在真实业务压力下成立 → 进入 Gate A-G 汇总 → ARCHITECTURE DECISION（PASS）。
**部分满足** → REFINE（调整 Slice 范围或补充证据）。
**证伪** → BLOCK（该域维持单体内，登记 BLOCK 原因进 §4.1 反证表，如实记录，不做拆分；寻找更合适的第一个 Slice）。

### 4.1 D12 反证表（证伪机制，裁决第 5 条）

| 发现 | 结论 |
|------|------|
| Order 写入 7 个领域表 | BLOCK |
| Order 创建必须同步权限计算 | BLOCK |
| Order 与 User 强 ACID | BLOCK |
| Order 读取跨 9 个领域 Join | REFINE |
| 95% 查询可通过 Read Model 消除 | PASS |
| 权限可转换为 Snapshot | PASS |
| Tenant 依赖可通过 Context 注入 | PASS |
| 迁移可双写校验 | PASS |

> 上表为**判定形式示例（非预设结论）**。D12 实测发现逐项登记于此。最终结论不是「我们觉得 Order 可以拆」，而是「经过 7 个维度验证，Order 满足/不满足独立服务边界条件」——这就是 Architecture Proof。

## 5. 边界与纪律

- 只读/影子执行：不改 Legacy 代码、不改表结构、不建新库、不启新进程、不启动微服务实现；
- 特征测试为等价判据（P0-B 43 + 路径 A 33）；
- 不修任何怪异（Q/E 登记）；
- 执行中发现的 Ownership 冲突进裁决表（不猜测）；
- 完成后独立提交 + 停顿等人工 Gate 判定。

## 6. 与 NG-1 其余工作的并行关系

| 工作 | 依赖 |
|------|------|
| 六维矩阵 289 表全量 | 与 D12 并行（矩阵方法同源） |
| Anti-Service 清单验证 | 依赖矩阵 OWNED/SHARED 分类 |
| 跨域 Join 清单 | D12 查询面 + 矩阵 Read Consumers 汇总 |
| ACID 事务清单 | D12 事务面 + 代码事务扫描 |
| 租户模型裁决（Gate D） | NG-0 租户规格 + 矩阵 Tenant Scope |
| 权限模型裁决（Gate E） | P0-B 契约 + D12 权限面 |

**执行顺序**：六维矩阵（第一批）→ D12 切片（并行）→ 清单汇总 → Gate A-G → ARCHITECTURE DECISION（PASS / REFINE / BLOCK）。
