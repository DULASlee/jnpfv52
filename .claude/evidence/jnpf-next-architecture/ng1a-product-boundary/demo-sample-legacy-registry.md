# NG-1A 产出物 3 — Product Content & Legacy Registry（产品内容与遗留资产登记表）

**日期**：2026-08-26 ｜ **分类体系**：P0-PX v2（§0A.6）｜ **原则**：这些资产**不删除、不修改**（只读登记）；不参与 Domain Ownership Proof；不进入 NG 架构设计；不迁移。

---

## 1. P2 PRODUCT_TEMPLATE（48 张）——产品交付模板内容（PlatformRole=PRODUCT_CONTENT, Lifecycle=TEMPLATE）

**Template ≠ Platform Domain**（§0A.6.1）：平台为展示/验证/提供模板而预置的业务数据 ≠ 平台自身业务领域。处置：**独立模板包，不进 Platform Core**。

### WFORM_* 48 张——官方示例表单数据（OA 模板族）

合同审批/差旅申请/付款申请/出入库/采购/报销/加班/用车/办公用品/印章管理等业务表单数据表。

**证据链**：48/48 无实体映射；全仓唯一引用 = `DataBaseService.cs` L485 备份导出表清单数组（**非业务代码**）；48/48 在 init 脚本（官方打包）；全部业务语义 → **「平台历史上用表单/流程功能承载过的业务」而非平台本身**。

**第二种价值（§0A.6.5 Product Capability Fixture）**：这些表可作为**平台能力验收夹具**——验证表单建模/权限/流程/查询/数据权限/性能。**验证对象是平台能力，而非 OA 领域本身。**

---

## 2. P3 DEMO_APPLICATION（25 张）——官方演示应用（PlatformRole=PRODUCT_CONTENT, Lifecycle=DEMO）

### 2.1 ext_* 族（19 张）——JNPF.Extend 演示模块（12 个子域服务）

| 子域服务 | 表 | 行数 |
|---------|-----|------|
| OrderService | ext_order / ext_order_entry / ext_order_receivable | 9 / 6 / 1 |
| ProductService | ext_product / ext_product_entry | 3 / 12 |
| ProductGoodsService | ext_product_goods | 10 |
| ProductClassifyService | ext_product_classify | 6 |
| ProductCustomerService | ext_customer | 7 |
| DocumentService | ext_document / ext_document_share | 4 / 0 |
| EmailService | ext_email_config / ext_email_send / ext_email_receive | 1 / 0 / 0 |
| EmployeeService | ext_employee | 0 |
| ProjectGanttService | ext_project_gantt | 0 |
| WorkLogService | ext_work_log / ext_work_log_share | 0 / 0 |
| TableExampleService | ext_table_example | 33 |
| BigDataService | ext_big_data | 0 |

**证据链**：19/19 实体映射 + 19/19 init 脚本 + 12 服务 API 可达（IDynamicApiController）+ 业务语义（订单/产品/邮件/文档）非平台功能。

**OrderService 四种可能（§0A.6.4）——当前裁决：③ 官方 Demo（暂定），P2 候选待证**：

```text
① 平台核心 Order          → 已排除（业务语义非平台功能 + 演示代码特征）
② 平台官方业务模板        → 若 Provenance Matrix 证实「预置模板业务数据」→ 升 P2 PRODUCT_TEMPLATE
③ 官方 Demo 应用          → 当前证据最支持（12 个编译期演示服务 = 可运行演示内容）
④ 历史客户/开发测试应用  → 若证实为客户/开发残留 → 转 P4/P6
```

CSV 已登记 `P2-template-candidate`（evidence 列）；**最终 P2/P3 细分由 Provenance Matrix 裁决**（§0A.6.6）。**无论 P2 还是 P3，ext_* 均不进 Platform Core**。

**D12 关系**：第一批 D12 Candidate Slice 实测证据保留于 `ng1-batch1/D12-S1-Report.md`；ext_* 不再作为「平台 Order 领域」证据（硬规则 4、§0A.6.1）。

### 2.2 demo_* 族（3 张）——无代码引用的演示表

DEMO_ORDER（实体映射 DEMOORDER 与表名不符，死实体）/ DEMO_ORDERDETAIL / DEMO_EXCELTEST——init 打包 + 代码 0 引用。

### 2.3 演示流程表（3 张）——官方演示流程代码

WFORM_LEAVEAPPLY（请假申请）/ WFORM_SALESORDER / WFORM_SALESORDERENTRY（销售订单）——有实体 + 服务代码（JNPF.WorkFlow.Entitys/WorkFlowForm/），官方演示用途。

---

## 3. P4 CUSTOMER_APPLICATION（5 张）——用户通过平台创建的动态业务表（PlatformRole=EXTERNAL, Lifecycle=CUSTOMER_GENERATED）

MT_* 5 张（MT543406707183714245 等数字后缀）——在线开发动态创建的业务数据表，无实体无代码引用（运行时按表名动态访问），init 打包（含历史用户数据）。**用户真正创建出来的业务系统，不进入平台核心。**

---

## 4. P6 LEGACY（45 张）——历史遗留（PlatformRole=LEGACY, Lifecycle=LEGACY）

### 4.1 WM_*/WH_*（39 张）——历史污染检测样本（第一批发现）

| 族 | 张数 | 数据行数证据 |
|----|-----|------------|
| WM_* | 21 | WM_BillDetail 1629 行 / WM_CheckBillDetail 1613 行 / WM_Material 739 行 |
| WH_* | 18 | 同构（仓库进销存历史系统） |

**特征**：SugarTable 实体 0、字符串引用 0、init 脚本 39/39 在、租户列 0、行数据真实 → **「数据库存在但代码完全不引用」的历史资产**。**不得定义为 Warehouse Domain**（硬规则 5）。

### 4.2 其他 LEGACY（6 张）

| 表 | 证据 |
|----|------|
| BASE_STUDIO_MENU_BAK_20260617 | 备份表（表名自带 BAK+日期），0 引用 |
| BASE_FILE | init 打包 + 全仓 0 引用（FileService 走物理文件存储 FileStorage/，无 DB 记录路径） |
| KG_PATTERN / KG_PATTERN_USAGE | init 打包 + 0 引用（早期知识图谱实现被移除） |
| STUDENT / DOMAIN_MODEL | init 打包 + 0 引用（早期演示/建模残留） |

---

## 5. P7 ORPHAN（1 张）——彻底孤儿（PlatformRole=LEGACY, Lifecycle=ORPHAN）

| 表 | 证据 |
|----|------|
| BASE_VISUAL_FILTER | 无 init 无实体 0 引用——连初始化脚本都不存在的彻底孤儿（旧版在线开发过滤条件残留） |

---

## 6. PX UNKNOWN（6 张）——证据不足，人工裁决（硬规则 1：不归 CORE；§0A.6.3：保持 BLOCKED）

| 表 | 现状 | 需裁决问题 |
|----|------|-----------|
| DATA_REPORT | 独立前端 jnpf-web-datareport；backend 0 引用 | 报表引擎是否第三方（ureport2）？归属 P0 or P8 EXTERNAL？ |
| REPORT_CHARTS / REPORT_USER / REPORT_DEPARTMENT | 同 DATA_REPORT | 数据大屏/报表配置数据归属 |
| BASE_TENANT_GLOSSARY / BASE_TENANT_INDUSTRY | 0 实体 0 引用（无 init） | 租户行业词汇表：平台功能 or 遗留（P7）？ |

---

## 7. 处置约束（硬规则 8）

> 全部 130 张非平台资产**不删除、不修改、不迁移**。本登记表仅用于：(a) 阻止其进入 NG 架构设计；(b) 作为 Provenance Matrix（§0A.6.6）与未来「物理归档/清理」的人工决策输入。
