# G0 Final Review（NG-1B Provenance Matrix 完成后）

**日期**：2026-08-26 ｜ **依据**：NG-1 规格 §0A.7（G0 = `PASS-PENDING-PROVENANCE`，Provenance Matrix 完成后终审）
**输入**：`provenance-matrix.csv`（289 × 26 列）+ `provenance-matrix-report.md` + `ng1a-product-boundary/` 全部产出物
**性质**：只读审计（零业务代码修改 / 零数据库修改 / 零删除）

---

## 裁决结论

> ### REFINE
>
> G0 维持 `PASS-PENDING-PROVENANCE`，**不最终 PASS**。
>
> 核心边界证明（平台资产 vs 非平台资产）已闭合且为铁证；
> 但 **P0 集合内仍有 24 张表的来源证明未闭合（21 PARTIAL + 3 UNKNOWN）**，
> 按「尚未完成全部 Provenance Proof 不得进入 Domain Ownership Proof」的纪律，
> 需要一轮**窄范围定向补强**后再次终审。

---

## 1. 铁证已闭合（本批完成，可复算）

| 集合 | 张数 | 定性 | 证据强度 |
|------|-----:|------|---------|
| ext_* | 19 | **P3 DEMO_APPLICATION**（非 Order Domain） | 铁证：菜单实测 62 条 extend.* 中 48 条 Demo 菜单 + 前端 17 个演示目录 + Seed 测试数据实测 + 数据量 0-33 行 |
| WFORM_* | 48 | **P2 PRODUCT_TEMPLATE**（OA 模板族） | 铁证：48 张实测 0 行空表 + generator.flowForm 入口 + IsSysTable 内置清单 L392-535 |
| WM_*/WH_* | 42 | **P6 LEGACY**（含真实客户数据） | 铁证：WM_BillDetail 1629 行 / WM_CheckBillDetail 1613 行 / WM_Material 739 行 / WH_BasicData 208 行 |
| sa_* | 13 | **P1 LOWCODE_RUNTIME** | 铁证：backend Dapper L760 写 + QualityApiService L37/L67 读 + sa-service 13 文件引用 |
| PX | 6 | **保持 UNKNOWN，零猜测** | ✓ 纪律遵守：2 PARTIAL / 4 UNKNOWN，未升级任何一张为 PLATFORM_CORE |
| P1 | 11 | 全部 LOWCODE_RUNTIME | **100% PROVEN** |

**三态统计（脚本驱动，非主观）**：PROVEN 157 / PARTIAL 77 / UNKNOWN 55，覆盖 289 / 289。

**禁止项核查（§0A.7 逐条）**：

| 禁止项 | 状态 |
|--------|------|
| 不进入 Domain Ownership Proof | ✓ 未进入 |
| 不恢复 D12 | ✓ 未恢复 |
| 不进行微服务设计 | ✓ 未进行 |
| 不删除 42 张孤儿表 | ✓ 零删除（仅 ARCHIVE 建议） |
| PX UNKNOWN 不得猜测归属 | ✓ 零猜测 |
| 不删除/修改数据库 | ✓ 只读查询 |

---

## 2. 未闭合项（REFINE 的理由）

### 2.1 P0 集合 24 张边界表（148 张中的 16.2%）

| 子类 | 张数 | 实况 | 补强方向 |
|------|-----:|------|---------|
| P0 UNKNOWN | 3 | SchemaVersions / undo_log / zx_sys_db | ① 框架自建证据（SqlSugar CodeFirst 源码级确认）；② zx_sys_db 与 zx_system_db 并列表关系 → **降级 P6 裁决项** |
| P0 PARTIAL — SA_* | 11 | Dapper 直连，无 SugarTable 实体（正常形态） | 「dapper-first 归档」判定原则 → **人工裁决项** |
| P0 PARTIAL — 其他 | 10 | BASE_SANDBOX / BASE_USER_DEVICE / BASE_IR_EDIT_PATCH / BASE_SCHEDULE_LOG / SYS_EVENT_OUTBOX_MESSAGE / SYS_PROCESSED_EVENT / EVAL_METRIC / PROCESSED_EVENT 等 | 定向补访问证据（实体已存在者应可命中）；真无引用者归档理由 |

### 2.2 方法学近似性（已声明，需裁决确认）

- `api_exposed`：模块级近似（表访问模块 ∩ API 服务模块）；优先集合已 file:line 复核，其余未逐表绑定。
- `startup_impact`：静态推演（P0/P1=REQUIRED，其余=REMOVABLE*）；真删实验归 NG-1C Platform Independence Proof（用户裁决 §④）。
- P2 模板空表 48 张全 PARTIAL：「模板无代码引用」判定为 PARTIAL 属正常形态 → **人工裁决项**。

---

## 3. REFINE 窄范围补强计划（待批准后执行，预计 1 批）

1. **框架证据（2 张）**：SchemaVersions / undo_log —— 查 SqlSugar 框架自建机制（nuget 包源码 / CodeFirst.InitTables 调用链），证据闭合后重评。
2. **访问证据（≤10 张）**：P0 PARTIAL 非 SA_ 表 —— 逐个 file:line 补写/读证据；无证据者归档理由。
3. **降级裁决（1 张）**：zx_sys_db → 提交人工裁决降级 P6 LEGACY（并列表 zx_system_db 有实体）。
4. **判定原则确认（3 项）**：dapper-first 归档 / 模板空表 PARTIAL 正常形态 / 静态 startup_impact 边界。
5. 完成后**再次 G0 Final Review（PASS / REFINE / BLOCK）**，仍 STOP 等人工裁决。

**不进入**：Domain Ownership Proof、D12 恢复、微服务设计、Platform Independence Proof（NG-1C，独立批次批准）。

---

## 4. 人工裁决清单（随本 STOP 一并提交）

| # | 裁决项 | 工程师建议 |
|---|--------|-----------|
| 1 | ext_* 19 张处置 | P3 Demo 包化：从平台默认初始化剥离为独立 Demo 安装包 |
| 2 | WFORM_* 48 张处置 | P2 模板包化：作为 OA 模板族独立模板包（不硬编码进平台） |
| 3 | WM_*/WH_* 42 张处置 | ARCHIVE（含真实数据，禁止删除） |
| 4 | zx_sys_db 归属 | 降级 P6 LEGACY |
| 5 | sa_* 13 张归属 | 下一代架构需定归属（backend Dapper vs sa-service Node 双端） |
| 6 | REFINE 补强计划（§3） | 批准后执行，完成后再次终审 |

---

## 5. STOP 声明

本批（NG-1B Provenance Matrix）到此完成。**停止并等待人工裁决。**

- G0 状态：`PASS-PENDING-PROVENANCE`（维持，未开闸）
- 建议裁决：**REFINE**（窄范围补强后再次终审）
- 最终裁决选项：PASS / REFINE / BLOCK —— 由人工决定
