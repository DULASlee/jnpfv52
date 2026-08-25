# D12 Architecture Slice S1 实测报告 — Order 子域五面详证

**日期**：2026-08-26 ｜ **性质**：只读/影子分析（零业务代码改动、零 DB 变更）｜ **主报告**：`NG1-Batch1-Report.md`

> 本报告承载第一批 8 项产出物中的第 3–6 项：Query/Join Map（§2）、Transaction Map（§3）、Permission/Tenant Dependency Map（§4）、Performance Baseline（§5）。

---

## 0. 切片定义修正（本批第一个证伪结果）

D12 计划 v1.0 §1 定义的切片为「WM_BillDetail/WM_Material/WM_CheckBillDetail/WH_*」，经实测：

| 假设 | 实测 | 判定 |
|------|------|------|
| WM_*/WH_* 是 Order 切片表 | `SugarTable("WM_/WH_/Demo_` 代码匹配 = 0，application 层字符串引用 = 0；**42 张孤儿表** | 假设被证伪 |
| ext_* 族 = 单一 Order 域 | ext_* 19 张表 = JNPF.Extend 模块内 **12 个独立子域服务**，Order 只是其一 | 粒度需重裁 |

**修正后的真实 D12 Order 子域** = `ext_order` + `ext_order_entry` + `ext_order_receivable`（3 表）+ `OrderService`（1 服务，324 行）。

---

## 1. 数据面（Data & Write Owner Map）

### 1.1 ext_* 族全景：19 表 × 12 子域服务（实测 SugarTable 18 匹配 + 1）

| 子域服务 | 表 | 行数 | 说明 |
|---------|-----|------|------|
| **OrderService** | ext_order / ext_order_entry / ext_order_receivable | 9 / 6 / 1 | **本批切片主体** |
| ProductService | ext_product / ext_product_entry | 3 / 12 | 产品族（独立于 Order） |
| ProductGoodsService | ext_product_goods | 10 | 产品库存 |
| ProductClassifyService | ext_product_classify | 6 | 产品分类 |
| ProductCustomerService | ext_customer | 7 | 客户主数据 |
| DocumentService | ext_document / ext_document_share | 4 / 0 | 文档库 |
| EmailService | ext_email_config / ext_email_send / ext_email_receive | 1 / 0 / 0 | 邮件 |
| EmployeeService | ext_employee | 0 | 员工 |
| ProjectGanttService | ext_project_gantt | 0 | 甘特图 |
| WorkLogService | ext_work_log / ext_work_log_share | 0 / 0 | 工作日志 |
| TableExampleService | ext_table_example | 33 | 表格示例 |
| BigDataService | ext_big_data | 0 | 大数据演示 |

**边界结论**：ext_ 前缀 ≠ Order 域。「哪些看似属于 Order 的东西实际上不属于 Order」的答案之一：ext_product/ext_customer/ext_document/ext_email 等 16 张表与 Order 无代码级写依赖，同模块纯属 Legacy 打包行为。Order 的候选子域边界 = 3 表。

### 1.2 Write Owner 判定（三级逻辑：生命周期权 → 决策权 → 契约权）

| 表 | 写方 | 证据 | 判定 |
|----|------|------|------|
| ext_order | OrderService.Save L225-239（Insertable/Updateable）· Delete L253-268 | OrderService.cs | OWNED（生命周期权单点） |
| ext_order_entry | OrderService.Save L227-236 · Delete L253 | 同上 | OWNED |
| ext_order_receivable | OrderService.Save L228-237 · Delete L254 | 同上 | OWNED |
| ext_customer 等 16 张 | 各自子域服务（ProductService/DocumentService/…） | JNPF.Extend 12 服务 Glob | OWNED（各自子域，非 Order） |

**跨域读**：OrderService.GetList 只读 base_user / flow_task（见 §2），不写他域表。
**跨域写（反向）**：Order Delete L256-260 写 `flow_task`（D4 域）→ 已在主报告登记 R4/C 类冲突。

### 1.3 孤儿表（WM/WH/Demo，42 张）

WM_* 21 + WH_* 18 + Demo_* 3：无实体、无服务、无查询代码。数据行数真实（WM_BillDetail 1629 行、WM_CheckBillDetail 1613 行、WM_Material 739 行），无租户列。处置方向待人工裁决（保留/删除/归属演示域）——已进主报告裁决表 3.1。

---

## 2. D12 Query / Join Map

### 2.1 列表查询：三表跨域 Join（OrderService.GetList，L81-106）

```csharp
// L81: 每次查询同步查模块表（权限链前置）
var menu = await _repository.AsSugarClient().Queryable<ModuleEntity>().FirstAsync(x => x.EnCode == "extend.order");
// L83: 路径 B 数据权限条件注入（别名 "a." 绑定 OrderEntity）
authorizeWhere = await _userManager.GetConditionAsync<OrderListOutput>(menu.Id, "F_ID", true, "a.");
// L84-106: ext_order × base_user × flow_task 三表 Join
Queryable<OrderEntity, UserEntity, FlowTaskEntity>((a, b, c) => new JoinQueryInfos(
    JoinType.Left, a.CreatorUserId == b.Id,   // Order → User（显示创建人 RealName/Account）
    JoinType.Left, a.Id == c.Id))             // Order → FlowTask（显示流程状态 Status）
```

| Join 边 | 方向 | 用途 | 可否 API 化 |
|---------|------|------|------------|
| ext_order → base_user（D1） | 读投影 | L88 `MergeString(b.RealName,"/",b.Account)` 显示 | ✅ 读投影天然可 API |
| ext_order → flow_task（D4） | 读投影 | L90 `currentState = c.Status` 显示 | ✅ 读投影天然可 API |

### 2.2 子查询：明细与收款（GetEntryList L115-120 / GetReceivableList 同构）

纯同域单表查询（`OrderEntryEntity.Where(x => x.OrderId == id)`），无跨域依赖。

### 2.3 其他读路径

- `GetInfo`/详情：主表单查 + 同域子表；`GetCustomerList`（L162 附近）/`GetGoodsList`（L179 附近）返回**硬编码大 JSON**（假客户/商品数据，未读 ext_customer/ext_product_goods 表）——即 ext_customer/ext_product_goods 表与 Order 无查询依赖，再次印证 1.1 边界结论。
- `GetUserName` 类依赖：通过 base_user 显示创建人（二次同步读）。

### 2.4 Query Map 小结

- 跨域读：仅 3 边（module、base_user、flow_task），全部为**读投影/权限链**性质，无写。
- 同域查询：ext_order × entry × receivable 三表，均为单表或 OrderId 关联。
- 无任何「Order → 其他业务域」的业务语义 Join（无 product 行级 Join、无 customer 行级 Join）。

---

## 3. D12 Transaction Map

### 3.1 Order 域事务 = 0（重要证伪）

`OrderService.Save`（L199-240）/`Delete`（L248-269）**无 BeginTran / TransactionScope / UseTran 包裹**。extend 模块全模块 0 处事务（全仓 25 处事务扫描，extend 0 命中）。

Save 执行序列（新建分支 L233-238）：
1. 删缓存（bill rule 编号缓存）L235
2. `Insertable(orderEntryList)` L236 —— **先插明细**
3. `Insertable(orderReceivableList)` L237 —— 再插收款
4. `Insertable(orderEntity)` L238 —— **最后插主表**

Delete 执行序列（L253-267）：软删明细 → 软删收款 → 软删主表 → **跨域写 flow_task 软删**（L256-260）→ 删物理文件（L261-267）。

**证伪结论**：NG-0 推断的「单据+明细强 ACID 原子性」**不成立**。现状「无事务」意味着：(a) 无强一致性依赖可拆；(b) 但「先插子表后插主表」+ 无事务是数据完整性隐患（中途失败会留孤儿明细）——属**缺陷待裁**还是**可接受边界**需人工裁决（主报告 R2=REFINE）。

### 3.2 全仓 25 处 BeginTran 分布（实测）

| 模块 | 处数 | 位置 |
|------|-----|------|
| workflow / FlowTaskManager.cs | 11 | 流程审批核心（多表状态流转） |
| visualdev / VisualDevService.cs | 4 | 可视化开发 |
| visualdev / RunService.cs | 5 | 在线开发运行时 |
| common / ExportImportDataHelper.cs | 1 | 导入导出 |
| inteAssistant / EntityDesignRepository.cs | 1 | 实体设计 |
| tests / SqlSugarVerification（测试项目） | 3 | 验证程序 |
| **extend（Order 所在模块）** | **0** | — |

含义：全仓真正的事务密集区是 **workflow（11/25 ≈ 44%）**，而非 Order。Order 是事务最薄弱的切片——独立拆分时**没有 ACID 阻力**，但必须先裁决「无事务」语义。

---

## 4. D12 Permission / Tenant Dependency Map

### 4.1 权限依赖：路径 B 唯一消费者确认（同步链）

`OrderService.GetList` 是 P0-B「路径 B 数据权限条件」的唯一外部消费者（`UserManagerPathBDataPermissionTests.cs` L12 注释 + 实测引用）。每次列表查询同步执行：

```
L81  base_module 查询（EnCode=="extend.order"）→ menu.Id
L83  GetConditionAsync<OrderListOutput>(menu.Id, "F_ID", true, "a.")
       → 数据权限条件注入（条件绑定主表别名 "a."，字段 F_ID）
L82  IsAdministrator==0 判断（超管跳过权限链）
```

**43 项特征基线复跑**（本批执行）：`dotnet test --filter` UserManagerPathBDataPermissionTests → **43/43 通过（158ms）**，行为基线零漂移。实现**未修改**（符合停止条件「复跑但不得修改实现」）。

### 4.2 租户依赖

- ext_order / ext_order_entry / ext_order_receivable 均为 **列级 `f_tenant_id`**（DB 实测导出确认，nvarchar）。
- OrderService 通过 `_userManager.TenantId`（L235 缓存 key）+ SqlSugar 全局租户过滤器注入租户——**无显式 tenant 传参**，租户隔离依赖框架过滤器。
- 拆分含义：租户面**无阻力**（列级租户 + 过滤器可平移），但必须携带三元组 TenantId 注入（R12 铁律）。

### 4.3 权限面小结

| 依赖 | 类型 | 消除方式 |
|------|------|---------|
| module 查询（每查询同步） | 同步 RPC 性质 | 启动缓存 / 配置快照 |
| GetConditionAsync 路径 B | 同步权限条件计算 | 条件计算 API 化（读模型）或条件快照 |
| IsAdministrator | 用户身份同步读 | 认证上下文传递 |
| f_tenant_id 过滤器 | 框架级租户隔离 | 平移 |

**结论**：权限面是 Order 独立化的**最大 REFINE 项**（主报告 R3）：每查询 2 次同步依赖（module + 权限条件），但 43 特征全绿证明语义可等价迁移。

---

## 5. D12 Performance Baseline

**环境**：本机 SQL Server（(local)\SQLEXPRESS，ZXAF_V1_DevTest1）｜ **执行**：`perf-baseline.sql`（SET STATISTICS TIME ON）

| 基线项 | SQL | 实测 |
|--------|-----|------|
| 三表 Join 列表（TOP 20） | ext_order ⋈ base_user ⋈ flow_task | **CPU 16ms / 占用 4ms** |
| 明细全表 | ext_order_entry | <1ms（6 行） |
| 收款全表 | ext_order_receivable | <1ms（1 行） |
| 权限链 module 查询 | base_module WHERE F_EN_CODE='extend.order' | <1ms（TOP 5） |

**数据量现状**：ext_order 9 行 / entry 6 行 / receivable 1 行——**极小数据**。本基线仅作「当前行为基线」记录（符合停止条件「建立基线，不做压力证明」），不构成任何性能结论；拆分的性能损益（跨服务 Join → 多次 RPC）必须在数据量逼近真实场景后再测（主报告 §四 Performance 面标注 ⏸）。

---

## 6. 边界五问（反证要求逐项作答）

> 用户强制要求：对每一个潜在边界同时回答「为什么可以/不能独立」。

| 问 | 答（基于本批实测） |
|----|------|
| 为什么可以独立？ | ① Write Owner 单点（OrderService 生命周期权唯一）；② 3 表同域自洽（entry/receivable 仅 OrderId 关联）；③ 0 事务——无 ACID 拆分阻力；④ 列级租户可平移；⑤ 43 特征证明权限语义可等价迁移；⑥ 跨域读仅 3 边且全为读投影 |
| 为什么不能独立？ | ① 每查询 2 次同步权限依赖（module + GetConditionAsync）未 API 化；② Delete 跨域写 flow_task（D4 表）未事件化；③ 「无事务」语义未裁决（是缺陷还是边界）；④ 性能基线数据量过小，拆分后延迟损益未证明 |
| 哪些依赖可通过 API/Event/Read Model 消除？ | module 查询 → 配置快照/API；GetConditionAsync → 权限条件 API；base_user/flow_task Join → 读投影 Read Model；Delete 写 flow_task → Event 或 D4 API |
| 哪些是真正强一致性依赖？ | **本批实测：Order 域内无**（0 事务、无同请求多表原子要求证据）；跨域无强一致读写对。全域最强者在 workflow（11 处事务）——Order 非强一致域 |
| 哪些只是 Legacy 偶合？ | ext_ 同模块打包 12 子域（打包偶合）；GetCustomerList/GetGoodsList 硬编码 JSON 假数据（未用真实表）；「先插子表后插主表」顺序（实现随意性）；Delete 内联 flow_task 软删（跨域逻辑耦合） |

**切片级初步结论（非 Gate G 裁决）**：Order（ext_ 3 表）**Owner 面 PASS 候选**、**租户面 PASS 候选**、**事务面 REFINE**（无事务语义待裁）、**权限面 REFINE**（同步链 API 化前提）、**查询面 REFINE→PASS 过渡**（读投影可 API 化但未实施）、**迁移面 ⏸ 未启动**（符合停止条件）。三态最终判定留待 Gate G，本批如实登记证据、不强行证明 PASS。
