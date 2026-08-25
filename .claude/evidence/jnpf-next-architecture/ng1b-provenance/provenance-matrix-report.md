# NG-1B Provenance Matrix 报告（REFINE 收口版）

**日期**：2026-08-26 ｜ **裁决依据**：NG-1 规格 §0A.7 + §0A.8（G0 REFINE 窄范围收口——人工裁决批准）
**性质**：只读审计（零业务代码修改 / 零数据库修改 / 零删除）｜ **配套**：`provenance-matrix.csv`（289 × 26 列）

---

## 1. 执行摘要（REFINE 收口后）

| 指标 | REFINE 前 | REFINE 后 |
|------|----------:|----------:|
| 覆盖表数 | 289 / 289 | **289 / 289**（不变） |
| PROVEN | 157 | **161** |
| PARTIAL | 77 | **75** |
| UNKNOWN | 55 | **53** |
| **P0 UNKNOWN** | 3 | **0** ✅ |
| P0/P1 平台核心 PROVEN 率 | 135 / 159 = 84.9% | **139 / 157 = 88.5%** |

**收口结论（证据驱动，全部 file:line / 源码级 / DB 实测）：**

1. **P0 集合 3 张 UNKNOWN 全部闭合**：SchemaVersions / undo_log 取得源码级框架创建证据 → PROVEN；zx_sys_db 裁决降级 P6 LEGACY（移出 P0）。
2. **P0/P1 剩余 18 张 PARTIAL 全部完成「缺位证明」**：每张表的关键证据（Creation + 访问链）要么 file:line 闭合，要么缺位已被全库扫描证明（0 引用 = 证据而非未知）。
3. **框架运行时表单独登记**（`_framework-evidence.tsv`）：SchemaVersions = DbUp migration journal；undo_log = Seata-AT 惰性预留。源码级证据，非表名推断。
4. **两张降级裁决落地**：zx_sys_db → P6 LEGACY（无实体无访问，5 行）；PROCESSED_EVENT → P6 LEGACY（DbUp 002 建表名与运行时实体 SYS_PROCESSED_EVENT 不一致的废止副本，0 代码访问）。
5. **方法论五原则正式入规**（NG-1 规格 §0A.8）：表存在 ≠ 领域存在；七分支资产谱系；四分类规则；战略顺序锁定。

---

## 2. REFINE 收口明细（24 张边界表逐张）

### 2.1 框架运行时表（2 张，UNKNOWN → PROVEN）

| 表 | 创建证据（源码级） | 框架角色 | 运行时事实 |
|----|-------------------|---------|-----------|
| SchemaVersions | `backend/tools/JNPF.Database.Migrations/Program.cs` L17-22：DbUp `DeployChanges.To.SqlDatabase(...).WithScriptsEmbeddedInAssembly(...)` 无 journal 表自定义 → dbup-core 5.0.87 默认 journal | 平台迁移基础设施（Platform Runtime Infrastructure） | 2 行 = 2 个迁移脚本（001 outbox / 002 processed_event），与 `App.cs` L482-484 排除 `Database.Migrations` 程序集吻合（迁移工具独立于运行时） |
| undo_log | `backend/web/主库脚本.sql` L4235-4246：Seata-AT 标准 schema（branch_id/xid/rollback_info + f_tenant_id 扩展，MS_Description「事务表」） | 事务框架惰性预留（FRAMEWORK_RESERVED） | 全 backend 0 代码引用 0 行 → Seata 未启用；真删实验归 NG-1C Platform Independence Proof |

### 2.2 事件基础设施表（2 张，访问链全闭合）

| 表 | 写路径 | 读路径 | API 暴露 |
|----|--------|--------|---------|
| SYS_EVENT_OUTBOX_MESSAGE | `SqlSugarEventOutboxStore.cs` L20-29 Insertable；创建=DbUp 001 脚本 L3 | 同文件 L34-43 裸 SQL `SELECT TOP(@batchSize) ... WITH (UPDLOCK, READPAST)`（file:line 铁证） | `DeadLetterService.cs`（JNPF.API.Entry）`GET /api/eventbus/deadletters` → **PROVEN** |
| SYS_PROCESSED_EVENT | `IdempotentEventHandler.cs` L51-55 | 同文件 L35-37 | 无独立 API（内部幂等基础设施，正常形态）→ PARTIAL(4) 缺位已证明 |

### 2.3 访问链补全（1 张，PARTIAL → PROVEN）

| 表 | 证据 | 结论 |
|----|------|------|
| BASE_SCHEDULE_LOG | `ScheduleService.cs` L390 Insertable 写 + 实体 `ScheduleLogEntity.cs` | **PROVEN** |

### 2.4 降级裁决（2 张，移出 P0）

| 表 | 证据 | 裁决 |
|----|------|------|
| zx_sys_db | 无实体、0 代码访问、5 行；并列表 zx_system_db 有实体（`SystemDbEntity.cs` L9）+ 读（`ConfigController.cs` L235 CreateDatabale API） | **P6 LEGACY**（运行时基础设施归 zx_system_db，zx_sys_db 为遗留副本） |
| PROCESSED_EVENT | DbUp 002 脚本建表，但运行时实体为 `[SugarTable("SYS_PROCESSED_EVENT")]`（命名不一致）；全库 0 访问 | **P6 LEGACY**（废止副本；技术债登记：迁移脚本与实体命名不一致） |

### 2.5 已证明缺位表（其余 P0/P1 PARTIAL）

| 表 | score | 缺位项 | 证明方式 |
|----|------:|--------|---------|
| BASE_SANDBOX | 4 | 写路径缺失 | SandboxManager.cs L20 纯内存 `ConcurrentDictionary`——全库 0 写引用 = 证据；创建=迁移脚本 `V5.2_002_sprint1_5_patch.sql` L14；读=SandboxConfigService.cs L18/19/33/36 |
| BASE_USER_DEVICE | 4 | 写路径缺失 | 实体 `UserDeviceEntity.cs` L13 + 读 `IMHandler.cs` L486；全库 0 写引用 = 证据 |
| BASE_IR_EDIT_PATCH | 3 | 0 引用 | 实体存在，全库扫描 0 命中 = 设计先行（预留），证据而非未知 |
| EVAL_METRIC | 3 | 0 引用 | 同上 |
| SA_* 13 张 | 3-4 | 无 SugarTable 实体 | **dapper-first 正常形态**（§0A.8.3 规则 1）：写 `AnalystSkillService.cs` L760 / `QualityScoreCalculator.cs` L200 / `ConsistencyChecker.cs` L415 / `SaMaterializer.cs` L312；读 `QualityApiService.cs` L37/L67 / `ConsistencyChecker.cs` L92/L153/L325 / `QualityDesignGate.cs` L34/L41；sa-service 13 文件引用。独立登记，不做 Domain Ownership 裁决 |

> **纪律**：上述「缺位」均为全库扫描 0 命中的**实证结果**，与「未扫描」有本质区别。每张表的关键证据已闭合，剩余缺位如实归档。

---

## 3. 证据源清单（全部可复算）

| # | 证据 | 文件 | 方法 |
|---|------|------|------|
| D | Creation Source | `_creation-sources.tsv`（289 行） | ZXAFINIT.sql 块扫描 + migration 脚本 Grep 行号定位 + DbUp 程序源码级确认 |
| E | Code/Write/Read Owner | `_entity-modules.tsv`（140 实体）+ `_access-map.tsv` + `_saservice-refs.tsv` | SugarTable 实体双向映射 + ISqlSugarRepository/Queryable/字符串 SQL 三通道扫描 + Node 侧表名扫描 |
| F | API/UI/Menu | `_api-services.tsv`（160 API 服务类）+ 菜单实测 | IDynamicApiController 类扫描 + sqlcmd 菜单实测 |
| G | Template/Seed | `_seed-map.tsv` + `_table-rowcounts-priority.txt` | INSERT 全文件扫描 + sys.partitions 行数实测 |
| H | 分类基线 | ng1a `platform-asset-classification.csv` | P0-PX 十类（本轮 2 张降级） |
| **I** | **框架运行时表** | **`_framework-evidence.tsv`（新增）** | **DbUp journal / Seata-AT schema 源码级证据 + 运行时角色** |

**DB 实测**：`codesoft\SQLEXPRESS` / `ZXAF_V1_DevTest1`（只读查询）。

---

## 4. 三态 × 分类交叉（REFINE 收口后）

| 分类 | 总数 | PROVEN | PARTIAL | UNKNOWN | 说明 |
|------|-----:|-------:|--------:|--------:|------|
| P0 PLATFORM_CORE | 146 | 128 | 18 | **0** | UNKNOWN 清零 ✅；18 PARTIAL 全部缺位已证明 |
| P1 LOWCODE_RUNTIME | 11 | 11 | 0 | 0 | **100% PROVEN** |
| P2 PRODUCT_TEMPLATE | 48 | 0 | 48 | 0 | 模板空表无代码引用 = 正常形态（§0A.8.3 规则 2） |
| P3 DEMO_APPLICATION | 25 | 22 | 0 | 3 | 3 张 DEMO_* 无代码引用（可卸载 Demo，不阻塞） |
| P4 CUSTOMER_APPLICATION | 5 | 0 | 0 | 5 | MT_* 在线开发动态表（运行时产物，无静态证据） |
| P6 LEGACY | 47 | 0 | 6 | 41 | 含本轮降级 2 张（zx_sys_db / PROCESSED_EVENT） |
| P7 ORPHAN | 1 | 0 | 1 | 0 | base_visual_filter |
| PX UNKNOWN | 6 | 0 | 2 | 4 | **保持 UNKNOWN，零猜测** ✓ |

---

## 5. 优先集合处置建议（不删除任何表）

| 集合 | 张数 | 处置建议 | 依据 |
|------|-----:|---------|------|
| ext_* | 19 | **DEMO 包化**：从平台默认初始化剥离，独立 Demo 安装包（P3） | 菜单 Demo 群 + Seed 测试数据 + 前端演示目录 |
| WFORM_* | 48+3 | **模板包化**：48 张空表 = OA 模板族定义（P2）；3 张有实体实例 = 模板示例 | 空表 + generator.flowForm 入口 + IsSysTable 清单 |
| WM_*/WH_* | 42 | **ARCHIVE**（有真实数据，禁止删除） | 孤儿 + 真实客户数据 |
| sa_* | 13 | **独立登记**（dapper-first），下一代归属裁决延后 | §0A.8.3 规则 1 + 人工裁决第 5 条 |
| zx_sys_db / PROCESSED_EVENT | 2 | **ARCHIVE**（遗留副本，禁止删除） | 本轮降级裁决 |
| SchemaVersions / undo_log | 2 | **框架基础设施**：保持，真删实验归 NG-1C | §0A.8.4 |
| base_* | 103 | 平台核心主体，Domain Ownership 分析前提资产（G0=PASS 后） | P0 |

---

## 6. 方法论原则（本轮正式入规）

**五否定原则**（NG-1 规格 §0A.8.1）：
数据库表存在 ≠ 平台领域存在；代码存在 ≠ 平台核心能力存在；菜单存在 ≠ 平台核心数据存在；Entity 存在 ≠ Write Owner 存在；有真实数据 ≠ 属于平台核心。

**战略顺序锁定**（§0A.8.5）：

```text
Provenance → Platform Boundary → Domain Ownership → Data Ownership
           → Transaction Boundary → Query Boundary → Migration Proof
           → 最后才决定哪些 Domain 值得微服务化
```

禁止「看到表 → 画微服务 → 拆数据库」。之前「Order 微服务切片」已由 Provenance 证伪（ext_* = Demo 资产），这是 NG-1 最有价值的成果之一。

---

## 7. 产出物索引

| 文件 | 说明 |
|------|------|
| `provenance-matrix.csv` | 289 × 26 列（REFINE 收口后重生成：PROVEN 161 / PARTIAL 75 / UNKNOWN 53） |
| `_creation-sources.tsv` | 289 表 Creation Source（本轮 5 行升级为源码级证据） |
| `_access-map.tsv` | backend 访问模块映射（本轮 3 行补全） |
| `_entity-modules.tsv` | 140 实体 → 模块（本轮补 2 个 infrastructure 实体） |
| `_framework-evidence.tsv` | **新增**：框架运行时表源码级证据 |
| `_saservice-refs.tsv` | sa-service 引用（13 张表） |
| `_seed-map.tsv` | Seed INSERT 映射 |
| `_api-services.tsv` | 160 API 服务类 |
| `_table-rowcounts-priority.txt` | 优先集合实测行数 |
| `_stats-check.ps1` | 三态统计复算脚本 |
| `gen-*.ps1` | 生成脚本（本轮扩展框架表分支，可复算） |

---

## 8. STOP 条件

本批（REFINE 收口）到此完成。**最终裁决见 `G0-Final-Review.md`（PASS / REFINE / BLOCK 三选一）。**

无论结果如何，**停止并等待人工裁决**。G0=PASS 后才允许进入 Domain Ownership Proof。下一批候选（未批准不动工）：Domain Ownership Proof（G0=PASS 后）、Platform Independence Proof（NG-1C，影子环境删除实验）、模板包化工程。
