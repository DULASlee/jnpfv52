# NG-1A 平台产品资产边界审计计划 v1.1

**日期**：2026-08-26 ｜ **裁决依据**：2026-08-26 架构纠偏裁决（NG-1 规格 §0A）+ 资产模型升级裁决（§0A.6）｜ **状态**：已批准执行（分类体系 v1.1 = P0-PX + 二维）
**性质**：只读归属审计（五零约束：零业务代码修改 / 零数据库修改 / 零微服务实现 / 零 Aspire 引入 / 零迁移）

---

## 1. 目标

回答 NG-1 真正的核心问题：

> **如果把所有 Demo、示例、历史业务表从数据库中拿掉，JNPF 这个低代码平台本身究竟还剩下哪些数据？**

NG-1A 不回答「Order 能不能拆成微服务」，只回答「哪些表属于低代码平台产品本身」。

## 2. 分类体系 v1.1：P0-PX 十类 + 二维分类（§0A.6 升级裁决）

**只允许 P0/P1 进入「平台领域与数据 Ownership」分析**（§0A.6.3）。

| 类 | 含义 | 进 NG 设计 |
|----|------|-----------|
| P0 PLATFORM_CORE | 平台自身必须数据（含基础设施表） | ✅ |
| P1 LOWCODE_RUNTIME | 低代码运行时元数据 | ✅ |
| P2 PRODUCT_TEMPLATE | 产品交付模板内容（CRM/ERP/OA 模板、示例表单） | ❌ 独立模板包 |
| P3 DEMO_APPLICATION | 官方演示应用 | ❌ 可删除/隔离 |
| P4 CUSTOMER_APPLICATION | 用户创建的业务系统（动态表 MT_*） | ❌ |
| P5 TEST_FIXTURE | 测试数据 | ❌ 清理或隔离 |
| P6 LEGACY | 历史遗留（有来源已弃用：WM/WH、BAK、KG_*） | ❌ 归档 |
| P7 ORPHAN | 彻底孤儿（无 init 无实体 0 引用） | ❌ 隔离 |
| P8 EXTERNAL | 外部/第三方模块 | ❌ |
| PX UNKNOWN | 无法证明 | ⏸ BLOCKED |

### 2.1 二维分类（PlatformRole × AssetLifecycle）

- **PlatformRole**：CORE / RUNTIME / PRODUCT_CONTENT / EXTERNAL / LEGACY / UNKNOWN
- **AssetLifecycle**：MANDATORY / OPTIONAL / TEMPLATE / DEMO / CUSTOMER_GENERATED / TEST / LEGACY / ORPHAN / UNKNOWN

CSV 每行输出三列：`product_asset_class` + `platform_role` + `asset_lifecycle`。

**Template ≠ Platform Domain**：`ext_order` 若实为「预置模板业务数据」→ PRODUCT_TEMPLATE，而非 `Domain = Order`；存在 OrderService ≠ 存在 Order 领域。

## 3. 证据链（每张表）

```text
DB metadata → Code reference → Runtime reachability → API/UI reachability
→ Migration/configuration → Tests → Deployment dependency → Data characteristics → Historical provenance
```

采集手段（全部只读）：
1. `SugarTable("...")` 全仓实体映射清单（有实体 = 代码引用第一证据）；
2. 服务/Repository/Manager 引用扫描（按前缀族分模块）；
3. `DB/` 目录迁移与初始化 SQL 扫描（历史 provenance）；
4. 前端 `jnpf-web-vue3` API 路由引用扫描（UI 可达性）；
5. tests 目录引用扫描（测试可达性）；
6. DB 元数据特征（行数/租户列/PK，已有 `db-matrix-raw.tsv`）。

## 4. 硬规则（8 条，来自裁决）

1. `UNKNOWN` 不得归入 CORE_PLATFORM。
2. Demo/Sample/Customer/Legacy/Orphan 不得参与 Domain Ownership Proof。
3. 只有 CORE_PLATFORM 或 LOWCODE_RUNTIME 才能进入后续 Domain/Ownership 分析。
4. 业务表不得因名称或 Service 名称自动成为 Domain。
5. WM/WH 42 张孤儿表作为历史污染样本单独登记，不得定义为 Warehouse Domain。
6. ext_* 必须重新证明 ProductAssetClass；NG-1A 初判（演示代码证据）暂定 P3 DEMO_APPLICATION 并登记 P2_TEMPLATE_CANDIDATE，最终 P2/P3 细分由 Provenance Matrix 裁决。
7. D12 Order Slice 改名 Candidate Slice，保留证据但暂停 Architecture Decision。
8. 不删除、不修改任何历史表和数据。

## 5. 产出物（6 项，完成后 STOP）

1. `platform-asset-classification.csv` — 289 表 × ProductAssetClass + 证据列
2. `core-platform-data-inventory.md` — CORE_PLATFORM + LOWCODE_RUNTIME 清单
3. `demo-sample-legacy-registry.md` — DEMO/SAMPLE/LEGACY/TEST/EXTERNAL 登记表
4. `asset-provenance-map.md` — 证据链溯源图（每类资产的证据路径）
5. `product-boundary-proof.md` — G0 初步结论（PASS/REFINE/BLOCK 待人工裁决）
6. `NG-1A Final Review` — 本计划执行总结

产出目录：`.claude/evidence/jnpf-next-architecture/ng1a-product-boundary/`

## 6. Gate G0（Product Boundary Proof）

```text
G0 PASS → 才允许进入 Domain Ownership Proof
        → 才允许重新定义 D12 Candidate Slice
        → 才允许进行 Microservice Boundary Proof
```

NG-1A 完成后不自动裁决 G0——提交 6 项产出物后 STOP，等待人工裁决。

## 7. 下一步：Provenance Matrix（§0A.6.6，替代 D12 Order Ownership）

NG-1A 初判完成并经人工裁决后，执行 **Provenance Matrix**：

- 范围：289 表 + 代码模块 + 初始化 SQL + migration/seed + UI/菜单
- 问题链：谁创建 → 何时 → 哪个 SQL/Migration → 哪个模块拥有 → 哪个代码写 → 哪个 API 暴露 → 哪个 UI 使用 → 是否启动必须 → 是否模板安装后出现 → 是否 Demo 脚本产生 → 是否客户建模产生 → 删掉后平台核心是否正常
- **优先追踪**：ext_* / WM_* / WH_* / base_* / sa_*
- 产出：每张表可证明的来源身份（Provenance Matrix），P2/P3 细分与 PX 处置在此阶段裁决

**不继续 D12 Order Ownership**（已暂停）。
