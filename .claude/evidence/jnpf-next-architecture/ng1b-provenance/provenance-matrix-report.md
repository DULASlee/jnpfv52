# NG-1B Provenance Matrix 报告

**日期**：2026-08-26 ｜ **裁决依据**：NG-1 规格 §0A.7（G0 `PASS-PENDING-PROVENANCE` → 本批为最终裁决输入）
**性质**：只读审计（零业务代码修改 / 零数据库修改）｜ **配套**：`provenance-matrix.csv`（289 × 26 列）

---

## 1. 执行摘要

| 指标 | 值 |
|------|-----|
| 覆盖表数 | **289 / 289**（100%） |
| PROVEN | **157**（54.3%） |
| PARTIAL | **77**（26.6%） |
| UNKNOWN | **55**（19.0%） |
| P0/P1 平台核心 PROVEN 率 | **135 / 159 = 84.9%** |
| PX UNKNOWN 纪律 | 6 张全部如实输出（2 PARTIAL / 4 UNKNOWN），**零猜测** |

**核心结论（证据驱动，全部来自脚本扫描 + DB 实测 + file:line 验证）：**

1. **ext_* 19 张 = Demo 应用资产（P3），不是平台 Order Domain。** 证据：① base_module 菜单实测 62 条 `extend.*` 中 48 条为 formDemo/graphDemo/tableDemo 演示菜单；② `extend.order` 菜单与 `extend/order` 前端页面同处演示目录（`src/views/extend/` 17 个目录全为演示页）；③ Seed 数据实测为测试内容（`f_customer_name='111'`、`f_description='batch update info'`）；④ 数据量 0-33 行。
2. **WFORM_* 48 张 = 产品模板（P2 PRODUCT_TEMPLATE）。** 证据：① 51 张预置表中 48 张实测 **0 行**（空表模板）；② 仅 `wform_salesorder` 有 1 条 Seed；③ UI 入口为 `generator.flowForm` 流程表单设计器（非业务菜单）；④ 平台自身将全部 wform_* 列入 `IsSysTable` 内置表清单（`DataBaseService.cs` L392-535）。
3. **WM_*/WH_* 42 张 = 历史业务资产（P6 LEGACY），含真实客户数据。** 证据：① 无 C# 实体、无代码 owner（`_entity-modules.tsv` 零命中）；② 菜单仅 2 条遗留静态页（`InStorage/index.html` / `OutStorage/index.html`）；③ **实测数据量：WM_BillDetail 1629 行、WM_CheckBillDetail 1613 行、WM_Material 739 行、WH_BasicData 208 行**；④ Seed 覆盖 WM_ 16/21、WH_ 18/18。
4. **PX 6 张保持 UNKNOWN/PARTIAL，未升级任何一张为 PLATFORM_CORE。** ✓
5. **sa_* 13 张 = AI 原生开发运行时（P1）。** 证据：① backend Dapper 写（`AnalystSkillService.cs` L760 `Insertable.AS("sa_assumptions")`）+ API 读（`QualityApiService.cs` L37/L67）；② sa-service（Node）10 张被 13 个文件引用；③ 全部 0 Seed（纯运行时表）。

---

## 2. 证据源清单（全部可复算）

| # | 证据 | 文件 | 方法 |
|---|------|------|------|
| D | Creation Source | `_creation-sources.tsv`（289 行） | ZXAFINIT.sql（491MB UTF-16LE）块扫描：273 张 CREATE TABLE 精确字节偏移；16 张无 init 表逐一 Grep 行号定位 |
| E | Code/Write/Read Owner | `_entity-modules.tsv`（138 实体）+ `_access-map.tsv`（backend 2827 .cs 全量）+ `_saservice-refs.tsv`（sa-service 130 文件） | SugarTable 实体双向映射 + ISqlSugarRepository/Queryable/字符串 SQL 三通道扫描 + Node 侧表名扫描 |
| F | API/UI/Menu | `_api-services.tsv`（160 API 服务类）+ `_menu-extend.txt` / `_menu-wform.txt`（DB 实测） | IDynamicApiController 类扫描 + sqlcmd 菜单实测（base_module 210 条） |
| G | Template/Seed | `_seed-map.tsv`（76019 条 INSERT 全量映射）+ `_table-rowcounts-priority.txt`（DB 实测行数） | INSERT 语句全文件扫描 + sys.partitions 行数实测 |
| H | 分类基线 | ng1a `platform-asset-classification.csv` | P0-PX 十类（上一批产出） |

**DB 实测**：`codesoft\SQLEXPRESS` / `ZXAF_V1_DevTest1`（sa / 只读查询）。

---

## 3. 三态 × 分类交叉

| 分类 | 总数 | PROVEN | PARTIAL | UNKNOWN | 说明 |
|------|-----:|-------:|--------:|--------:|------|
| P0 PLATFORM_CORE | 148 | 124 | 21 | 3 | 84% PROVEN；见 §4 边界 |
| P1 LOWCODE_RUNTIME | 11 | 11 | 0 | 0 | **100% PROVEN** |
| P2 PRODUCT_TEMPLATE | 48 | 0 | 48 | 0 | 全 PARTIAL = 模板空表无代码引用（正常形态） |
| P3 DEMO_APPLICATION | 25 | 22 | 0 | 3 | 3 张 DEMO_* 无代码引用（Demo 资产，不阻塞） |
| P4 CUSTOMER_APPLICATION | 5 | 0 | 0 | 5 | MT_* 在线开发动态表（运行时产物，无静态证据） |
| P6 LEGACY | 45 | 0 | 5 | 40 | 历史资产无代码证据（如实） |
| P7 ORPHAN | 1 | 0 | 1 | 0 | base_visual_filter |
| PX UNKNOWN | 6 | 0 | 2 | 4 | **保持 UNKNOWN，零猜测** ✓ |

---

## 4. P0 边界情况（最终裁决必须过目）

### 4.1 P0 但 UNKNOWN（3 张）——provenance 未闭合

| 表 | 实况 | 机制推断 [INFERRED] | 建议 |
|----|------|---------------------|------|
| SchemaVersions | 2 行 / 3 列，ZXAFINIT 有 CREATE | SqlSugar CodeFirst 框架版本表（自建） | 补框架证据后重评；或标注 P0-FRAMEWORK 子类 |
| undo_log | 0 行 / 10 列，ZXAFINIT 有 CREATE | SqlSugar 框架撤销日志表 | 同上 |
| zx_sys_db | 5 行 / 8 列，ZXAFINIT 有 CREATE | zxdev 老表（`zx_system_db` 新表有实体） | 建议降级 P6 LEGACY（并列表，无代码引用） |

> 纪律：以上仅 [INFERRED]，矩阵如实输出 UNKNOWN，**未猜测归类**。

### 4.2 P0 但 PARTIAL（21 张）——两类原因

- **SA_* 11 张（score 4）**：Dapper 直连访问族，**无 SugarTable 实体属正常形态**（写：`AnalystSkillService.cs` L760；读：`QualityApiService.cs` L37/L67、`ConsistencyChecker.cs` L153/L237、`QualityDesignGate.cs` L34/L41；sa-service 13 文件引用）。**建议接受 PARTIAL 并归档「dapper-first」形态**，不强制实体。
- **BASE_* 4 张 + SYS_* 2 张 + 其他 4 张（score 2-4）**：实体/引用部分缺位（如 BASE_SANDBOX、BASE_USER_DEVICE、BASE_IR_EDIT_PATCH、BASE_SCHEDULE_LOG、SYS_EVENT_OUTBOX_MESSAGE、SYS_PROCESSED_EVENT、EVAL_METRIC、PROCESSED_EVENT）。这些表有实体或部分引用，但「访问证据 + API + UI」不齐。**建议逐张补访问证据（≤ 10 张，量级小）或接受 PARTIAL 归档理由**。

### 4.3 判定规则近似性声明（方法学）

- `api_exposed`：模块级近似（表访问模块 ∩ API 服务模块），非 file:line 级。PROVEN 表中的 API 判定已在优先集合用 file:line 复核（ext_order→OrderService、sa_*→QualityApiService 等）。
- `ui_menu`：前缀族级判定（extend.* 菜单 62 条 / generator.flowForm / BASE_MODULE 族），非逐表绑定。
- `startup_impact`：静态推演（P0/P1=REQUIRED，其余=REMOVABLE*）。**真删实验归 NG-1C Platform Independence Proof**（用户裁决 §④，影子环境）。

---

## 5. 优先集合处置建议（不删除任何表）

| 集合 | 张数 | 处置建议 | 依据 |
|------|-----:|---------|------|
| ext_* | 19 | **DEMO 包化**：从平台默认初始化剥离，作为独立 Demo 安装包（P3） | 菜单 Demo 群 + Seed 测试数据 + 前端演示目录 |
| WFORM_* | 48+3 | **模板包化**：48 张空表 = OA 模板族定义（P2）；3 张有实体实例 = 模板示例（P3） | 空表 + generator.flowForm 入口 + IsSysTable 清单 |
| WM_* | 21 | **ARCHIVE**（有真实数据，不可删除）：WM_BillDetail 1629 行 / WM_Material 739 行 | 孤儿 + 真实客户数据 |
| WH_* | 18 | **ARCHIVE**：WH_BasicData 208 行 | 同上 |
| sa_* | 13 | **MIGRATE 候选**：backend Dapper + sa-service Node 双端访问，下一代架构需定归属 | P1 运行时资产 |
| base_* | 103 | 平台核心主体，进入 Domain Ownership 分析的前提资产 | P0 |

---

## 6. 对「下一代结构」的直接含义

Provenance 结果证实了用户的架构假设：

```text
Next Generation Platform
├── Platform Core        ← P0 148 张（135 PROVEN + 边界 24 张待裁决）
├── Low-Code Runtime     ← P1 11 张（100% PROVEN）+ SA 九表物化器
├── Product Template System ← P2 48 张（WFORM 模板族，独立模板包）
├── Demo / Sample        ← P3 25 张（ext_ + DEMO_*，独立安装包）
└── Customer Applications ← P4 5 张（MT 动态表）+ P6 45 张（历史 WM/WH 等，归档）
```

**旧系统 289 张表不应整体继承**；模板/Demo/历史资产可剥离为独立安装包或归档，微服务/Modular Monolith 边界在 P0/P1 内划定。

---

## 7. 产出物索引

| 文件 | 说明 |
|------|------|
| `provenance-matrix.csv` | 289 × 26 列（14 维 + 三态 + 分类） |
| `_creation-sources.tsv` | 289 表 Creation Source（273 init 字节偏移 + 16 migration/manual 行号） |
| `_access-map.tsv` | backend 访问模块映射（access 88 / write 83 / read 126） |
| `_saservice-refs.tsv` | sa-service 引用（13 张表） |
| `_seed-map.tsv` | Seed INSERT 映射（144 张有 Seed，共 76019 条） |
| `_entity-modules.tsv` | 138 实体 → 模块 |
| `_api-services.tsv` | 160 API 服务类 |
| `_table-rowcounts-priority.txt` | 优先集合实测行数 |
| `_menu-extend.txt` / `_menu-wform.txt` | 菜单实测原始输出 |
| `gen-*.ps1` × 8 | 全部生成脚本（可复算） |

---

## 8. STOP 条件

本批到此完成。**最终裁决见 `G0-Final-Review.md`（PASS / REFINE / BLOCK 三选一）。**

无论结果如何，**停止并等待人工裁决**。下一批候选（未批准不动工）：Platform Independence Proof（NG-1C，影子环境删除实验）、Domain Ownership Proof（需 G0 最终 PASS 后）、模板包化工程。
