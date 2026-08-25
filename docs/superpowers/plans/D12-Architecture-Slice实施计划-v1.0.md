# D12 Architecture Slice 实施计划 v1.0（NG-1 核心执行单元）

**日期**：2026-08-26 ｜ **关联**：《JNPF-Next-NG1-领域与数据Ownership设计规格》
**定位**：D12 从「沙盘」升级为 **Architecture Slice**——验证一整条真实链（Order → User/Tenant/Permission/Query/Repository/Transaction/Database），**不是验证 Order 自身能否运行**。
**状态**：实施计划（待批准后执行）

---

## 1. 切片定义

```text
范围：Order 域（WM_BillDetail/WM_Material/WM_CheckBillDetail/WH_* 相关）
真实链：Order 数据 → User 引用 → Tenant 引用 → Permission 数据权限 → Query 组装 → Transaction → Database
验证面：数据 / 查询 / 事务 / 迁移 / 性能（五面）
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

**全绿** → 该边界在真实业务压力下成立 → 进入 Gate A-G 汇总 → ARCHITECTURE DECISION。
**任一红** → 边界不成立 → 该域维持单体内（如实记录，不做拆分）。

## 5. 边界与纪律

- 只读/影子执行：不改 Legacy 代码、不改表结构、不建新库、不启新进程；
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

**执行顺序**：六维矩阵（第一批）→ D12 切片（并行）→ 清单汇总 → Gate A-G → ARCHITECTURE DECISION。
