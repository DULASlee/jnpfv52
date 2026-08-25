# NG-1 第一批主报告 — 289 表 Ownership Matrix + D12 S1 实测

**日期**：2026-08-26 ｜ **状态**：第一批完成（只读/影子执行，零业务代码改动）｜ **提交边界**：本目录 `ng1-batch1/` 独立提交

> **停止条件遵守**：本批不进行 Gate G 最终裁决、不进入 NG-2、不启动微服务实现。以下全部为实测证据汇报。

---

## 一、执行摘要

| # | 产出物 | 状态 | 关键数字 |
|---|--------|------|---------|
| 1 | Ownership Matrix（六维骨架 v1） | ✅ | `ownership-matrix-v1.csv` 289 行全量 |
| 2 | UNKNOWN / CONFLICT 清单 | ✅ | UNKNOWN **96 张**（孤儿 42 + TBD 54）；CONFLICT **5 项** |
| 3 | D12 Query / Join Map | ✅ | `D12-S1-Report.md` §2 |
| 4 | D12 Transaction Map | ✅ | `D12-S1-Report.md` §3 |
| 5 | D12 Permission / Tenant Dependency Map | ✅ | `D12-S1-Report.md` §4 |
| 6 | D12 Performance Baseline | ✅ | `D12-S1-Report.md` §5 + `perf-baseline.sql` |
| 7 | 初步 BOUNDARY-PROOF | ✅ | 本报告 §四 |
| 8 | 风险与反证清单 | ✅ | 本报告 §五 |

**执行纪律核验**：全程只读（sqlcmd SELECT + dotnet test 复跑 + Grep 只读审计）；未改 Legacy 代码、未改库、未建服务、未迁移数据、未引入任何基础设施。

---

## 二、最重要发现（按冲击力排序）

### F1. D12 计划切片定义与实际不符（第一个证伪点）

D12 计划 §1 定义切片为「WM_BillDetail/WM_Material/WM_CheckBillDetail/WH_*」——**实测这些表在全部后端代码中零引用**（`SugarTable("WM_/WH_/Demo_` Grep = 0 匹配，application 层字符串引用 = 0）。

- **42 张孤儿表**：WM_* 21 + WH_* 18 + Demo_* 3（无实体、无服务、无查询代码；数据行数真实：WM_BillDetail 1629 行、WM_Material 739 行、WM_CheckBillDetail 1613 行——历史遗留或纯 DB 演示数据）
- **真实 Order 代码** = `ext_order` 族 19 张（`OrderService.cs`，`[SugarTable("EXT_ORDER")]`，f_tenant_id 列级租户，nvarchar PK）
- 结论：**D12 切片定义需人工重新裁决**（改为 ext_* 族 or 保留 WM/WH 待裁决）。这本身就是 NG-1「证明或证伪」机制的第一个有效产出：**计划基于的假设被实测证伪，未强行推进**。

### F2. Order 域写路径「无事务包裹」——「单据+明细原子」不成立

`OrderService.Save`（L225-239）对 ext_order/ext_order_entry/ext_order_receivable 的插入/删除**无 BeginTran/TransactionScope 包裹**（全仓事务扫描 25 处，extend 模块 0 处）。且插入顺序为「先 Entry 后主表」（L236-238）。`Delete`（L253-259）软删主表 + 软删 flow_task + 删物理文件，同样无事务。

含义：NG-0 推断的「单据+明细强 ACID」被证伪——现状无强事务依赖，**事务面是 REFINE 项而非 BLOCK 项**（无 ACID 可拆，但需裁决「无事务」是缺陷待修还是可接受的边界条件）。

### F3. Order 每次列表查询同步依赖权限链（路径 B 唯一消费者确认）

`OrderService.GetList` L81-83：每次查询同步执行 `Queryable<ModuleEntity>().FirstAsync(EnCode=="extend.order")` → `GetConditionAsync<OrderListOutput>(menu.Id, "F_ID", true, "a.")`（路径 B 数据权限条件注入，别名 "a."）。**43 项特征测试复跑 43/43 通过（158ms）**，行为基线零漂移。

### F4. 抽样发现 4 处跨域写（OWNERSHIP-CONFLICT 种子）

| # | 写入方（模块） | 被写表 | 域冲突 |
|---|--------------|--------|--------|
| C1 | RoleService.cs L572/658（D3） | base_organize_relation | D3 → D1 |
| C2 | UsersCurrentService.cs L846-868（D1） | base_sign_img | D1 → D7 |
| C3 | DataInterfaceService.cs L1681/1687（TBD-Integration） | base_api_log | Integration → D9 |
| C4 | FlowTaskManager.cs L2308（D4） | JobDetails（Quartz 表） | D4 → INFRA-Job |

### F5. 租户列风格实测 = 5 种（NG-0「三风格」修正）

本批 DB 实测（IN 匹配 + 实际列名导出）：`f_tenant_id` / `F_TenantId` / `F_TENANT_ID` / `F_Tenant_Id` / `tenant_id` 五种写法。sa_* 家族内部混用（sa_assumptions=F_TenantId vs sa_business_process=tenant_id）。WM_/WH_/Demo_/zx_* 无租户列（孤儿表与租户注册表除外）。

---

## 三、UNKNOWN / OWNERSHIP-CONFLICT 裁决表（人工裁决输入，AI 不猜测）

### 3.1 UNKNOWN 类（96 张，禁止进入任何拆分候选）

| 子类 | 数量 | 代表 | 需人工裁决的问题 |
|------|-----|------|----------------|
| WM_*/WH_*/Demo_* 孤儿 | 42 | WM_Bill(151行)/WH_BillDetail | 保留并补代码？删除？归属演示域？ |
| TBD-Base（base_ 未细分） | 31 | base_bill_rule/base_db_link/base_schedule | 逐表 Owner 审计（下一批） |
| TBD-Integration | 10 | base_data_interface* | 是否独立「集成域」？ |
| TBD-Job | 5 | base_schedule*/base_time_task* | 独立 Job 域 or 平台服务？ |
| TBD-Platform | 5 | base_common_fields/base_print* | 平台公共服务归属 |
| TBD-其他 | 3 | domain_model/student/TBD-BillRule | 逐项裁决 |

### 3.2 OWNERSHIP-CONFLICT 类（5 项）

| # | 对象 | 冲突事实 | 建议裁决方向（仅供人工参考，非 AI 判定） |
|---|------|---------|------|
| K1 | flow_form_authorize | 无 PK；FlowFormService 写 + Form 域读（D4×D5） | 归属 D4 或独立关联表，需人裁 |
| K2 | base_organize_relation | RoleService 写（D3→D1） | D1 提供 API 供 D3 调用，或关系表归 D1 |
| K3 | base_sign_img | UsersCurrentService 写（D1→D7） | 归 D7，D1 只读引用 |
| K4 | base_api_log | DataInterfaceService 写（Integration→D9） | 日志表统一归 D9（写方通过事件/审计器） |
| K5 | JobDetails（Quartz） | FlowTaskManager 写（D4→INFRA） | 基础设施表，D4 只能通过调度 API |

---

## 四、初步 BOUNDARY-PROOF（七类证据状态）

| 证据面 | D12（Order/ext_*）状态 | 全域状态 |
|--------|----------------------|---------|
| Ownership | **初步成立**：写路径=OrderService（生命周期权单点）；但 K2-K4 表明全域存在跨域写需裁决 | 抽样覆盖 system/workflow/extend；PENDING |
| Transaction | **证伪原假设**：0 事务（非 ACID 强依赖）；全仓 25 处 BeginTran 分布已测 | 25 处清单 ✅ |
| Query | 3 表 Join（User+FlowTask）已测，API 化可行性高（读投影） | D12 ✅ 其余 PENDING |
| Tenant | ext_* 列级 f_tenant_id 成立；WM/WH 无租户列（孤儿表特征） | 5 风格实测 ✅ |
| Permission | 路径 B 唯一消费者确认；43/43 特征复跑绿 | ✅（P0-B 基线） |
| Migration | 影子双写方案设计待 S2（本批只测等价基线） | 未启动（符合停止条件） |
| Performance | 基线已测（16ms CPU，9 行数据——仅基线记录） | D12 ✅ |

**14 问速答（D12）**：Write Owner=OrderService ✅ ｜ Lifecycle Authority=OrderService ✅ ｜ Decision Authority=OrderService（业务规则单点）✅ ｜ Consumer Map=GetList/GetInfo/GetEntryList/GetReceivableList ✅ ｜ Join Matrix=3 表（User/FlowTask）✅ ｜ Sync Dependency=module 查询+GetConditionAsync（**每查询同步**）⚠️ ｜ ACID Boundary=**无事务包裹** ⚠️ ｜ Tenant=列级 ✅ ｜ Authorization=路径 B 同步条件 ⚠️ ｜ 独立 DB=依赖 User/FlowTask/Permission 读，未证明 ⏸ ｜ 独立部署=API 化后可行（未证明）⏸ ｜ 独立演进=Contract 未定义 ⏸ ｜ 迁移=待 S2 ⏸ ｜ 性能=数据量过小，无法压力证明 ⏸

---

## 五、风险与反证清单

| # | 发现 | 结论 | 性质 |
|---|------|------|------|
| R1 | D12 计划切片（WM/WH）代码零引用 | 计划假设被证伪，切片定义需人工重裁 | **REFINE** |
| R2 | Order Save/Delete 无事务 | 「强 ACID」假设不成立；需裁决无事务是缺陷还是边界 | **REFINE** |
| R3 | Order 每查询同步权限（module+GetConditionAsync） | 同步依赖成立；快照/API 化是拆分前提 | **REFINE** |
| R4 | Order Delete 跨域写 flow_task | 跨域写依赖（D12→D4），拆分前必须事件化/API 化 | **REFINE** |
| R5 | 三表跨域 Join（User/FlowTask） | 可 API/Read Model 化（读投影天然） | **PASS 候选** |
| R6 | Owner 单点（OrderService） | 生命周期/决策权单点成立 | **PASS 候选** |
| R7 | 43 特征复跑全绿 | 权限语义可等价迁移 | **PASS 候选** |
| R8 | ext_* 列级租户 | 租户注入可行 | **PASS 候选** |
| R9 | 42 张孤儿表 | 迁移面不确定性大；WM_BillDetail 1629 行数据处置需人裁 | **BLOCK 候选**（孤儿表处置先于任何拆分） |
| R10 | 全域 96 张 UNKNOWN | OWNED 占比仅 ~57%（<60% Gate A 阈值） | **REFINE**（后续批次补审计） |

**综合初步结论（非 Gate G 裁决）**：Order（ext_*）域**具备独立 Ownership 雏形**（Owner 单点 + 租户列级 + 特征可复跑），但存在 4 项 REFINE 级依赖（同步权限、跨域写 flow_task、无事务语义未裁决、三表 Join），且 **R9 孤儿表与 R10 UNKNOWN 占比必须先解决**。「Order 当前满足/不满足独立服务边界」的最终判定留待 Gate G 阶段，本批如实登记证据。

---

## 六、证据文件清单（本目录）

| 文件 | 内容 |
|------|------|
| `ownership-matrix-v1.csv` | 289 表 × 13 列六维矩阵 v1 |
| `db-matrix-raw.tsv` | DB 实测原始导出（表/前缀/行数/列数/租户列/PK 类型） |
| `db-matrix-query.sql` / `gen-matrix.ps1` | 矩阵生成脚本（可复现） |
| `perf-baseline.sql` | 性能基线 SQL（含执行时间输出） |
| `D12-S1-Report.md` | D12 五面详细报告 |

## 七、状态表

```text
D1 ✅ ｜ 审计 ✅ ｜ P0-A ✅ ｜ P0-B ✅ BASELINED ｜ P0-C ⏸ DEFERRED ｜ S2 🔒 BLOCKED ｜ P1 🔒 BLOCKED
NG-0 ✅ APPROVED ｜ NG-1 ▶ APPROVED（有条件）｜ NG-1 第一批 ✅ 完成（本报告）｜ 第二批 ⏸ 待人工裁决
```

**第一批已停止。等待人工裁决：① D12 切片定义重裁（ext_* vs WM/WH）；② 孤儿表处置方向；③ 是否启动第二批（Owner 全量审计 + Anti-Service 清单验证）。**
