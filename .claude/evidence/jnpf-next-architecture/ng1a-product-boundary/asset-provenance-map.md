# NG-1A 产出物 4 — Asset Provenance Map（资产证据溯源图）

**日期**：2026-08-26 ｜ **分类体系**：P0-PX v2（§0A.6）｜ **证据链**：DB metadata → Code reference → Runtime reachability → API/UI reachability → Migration/configuration → Tests → Deployment dependency → Data characteristics → Historical provenance

## 1. 证据源清单（本批实测）

| 证据源 | 内容 | 文件 |
|--------|------|------|
| E1 DB 元数据 | 289 表/行数/列数/租户列/PK（sqlcmd 实测） | `ng1-batch1/db-matrix-raw.tsv` |
| E2 实体映射 | 172 张 SugarTable 实体（含 TableDescription 参数修正） | `_entity-tables.txt` |
| E3 初始化脚本 | ZXAFINIT.sql（515MB）273 张 CREATE TABLE | `_init-sql-tables.txt` |
| E4 代码引用 | 126 张无实体表全量字符串引用计数（backend 全仓） | `_no-entity-refs.tsv` |
| E5 sa-service 引用 | Node 侧 SA_* 引用 196 处 | 本批扫描 |
| E6 服务/API 可达性 | 12 个 ext 子域服务（IDynamicApiController）+ FileService 物理文件路径 | 第一批 + 本批 |

## 2. 每类资产的证据路径（P0-PX v2）

| ProductAssetClass | E1 DB | E2 实体 | E3 init | E4 引用 | E5 sa-svc | 结论 |
|-------------------|-------|---------|---------|---------|-----------|------|
| P0 PLATFORM_CORE 148 | ✅ | ✅ 172/172 | ✅（AI 族除外=运行时自建） | ✅ 活跃 | ✅（sa_ 13 张） | 四证据齐全 |
| P1 LOWCODE_RUNTIME 11 | ✅ | ✅ | ✅ | ✅ | — | 在线开发元数据 |
| P2 PRODUCT_TEMPLATE 48 | ✅ | ❌ 0/48 | ✅ 48/48 | ⚠️ 仅备份清单 | — | 产品模板内容 |
| P3 DEMO_APPLICATION 25 | ✅ | ✅ 22/25 | ✅ 25/25 | ✅ 服务 | — | 演示代码完整 |
| P4 CUSTOMER_APPLICATION 5 | ✅ | ❌ | ✅ | ❌（动态访问） | — | 用户动态表 |
| P6 LEGACY 45 | ✅（WM 行数据真实） | ❌ 0/45 | ✅ 44/45 | ❌ 0 | — | 历史污染 |
| P7 ORPHAN 1 | ✅ | ❌ | ❌ | ❌ 0 | — | 彻底孤儿 |
| PX UNKNOWN 6 | ✅ | ❌ | ✅ 4/6 | ❌ | — | 证据不足 |

## 3. 关键样本溯源（file:line 级）

| 样本 | 溯源路径 |
|------|---------|
| WFORM_* 48 张「唯一引用」 | `backend/modularity/system/JNPF.Systems/System/DataBaseService.cs` L485/L523 —— 数据库备份导出表清单数组，**非业务引用** |
| ext_* 12 服务 | `backend/modularity/extend/JNPF.Extend/` 12 个 Service（Glob 实测），全部 IDynamicApiController API 可达；CSV 登记 P2-template-candidate |
| WM_* 行数据 | sqlcmd 实测：WM_BillDetail 1629 / WM_CheckBillDetail 1613 / WM_Material 739 行 |
| BASE_FILE 0 引用 | FileService.cs（651 行）无 repository/Queryable；文件走 `FileStorage/` 物理存储 |
| AI_/SA_ 无 init | 289-273=16 张不在 ZXAFINIT.sql → 代码 CodeFirst/迁移自建（inteAssistant 模块） |
| sa_* 双端读写 | backend（Dapper，SA_ASSUMPTIONS 14 处）+ sa-service（Node，sa_ 前缀 196 处） |
| KG_PATTERN 0 引用 | init 打包但 backend 全仓 0 → 早期知识图谱实现被移除 |
| BASE_VISUAL_FILTER 三重缺失 | 无 init 无实体 0 引用 → P7 ORPHAN（连来源都没有的孤儿） |

## 4. Database Archaeology 指标（每表）

```text
DB exists → Code referenced? → Runtime reachable? → API exposed?
→ UI reachable? → Migration managed? → Tests referenced? → Deployment required?
→ ACTIVE / DORMANT / DEMO / LEGACY / ORPHAN / UNKNOWN
```

| Archaeology 态 | ProductAssetClass | 说明 |
|----------------|-------------------|------|
| ACTIVE | P0 / P1 | 代码+API+运行时全可达 |
| DEMO | P2 / P3 | 产品模板/演示内容（PRODUCT_CONTENT） |
| CUSTOMER | P4 | 运行时动态创建 |
| ORPHAN | P6 / P7 | DB 有、代码零引用 |
| UNKNOWN | PX | 证据不足 |

## 5. 溯源缺口 → Provenance Matrix（§0A.6.6 下一步）

本批四证据链已覆盖「DB 存在 + 实体映射 + init 打包 + 代码引用」，但以下维度**尚未采集**，正是 Provenance Matrix 的输入：

| 缺口维度 | 未采集原因 | Provenance Matrix 采集动作 |
|---------|-----------|--------------------------|
| **UI/菜单可达性** | 前端路由/菜单扫描未做 | 289 表 × 前端 API 路由引用扫描（UI 是否使用） |
| **Migration/seed 来源** | 仅扫 ZXAFINIT.sql | 逐表定位创建 SQL 精确位置（谁创建/何时） |
| **模板安装机制** | 未验证是否存在 | ext_*/WFORM_* 是否「模板安装后才出现」→ P2/P3 细分裁决 |
| **模块 Ownership** | 未做逐表归属 | 289 表 × 代码模块映射（哪个项目/模块拥有） |
| **启动必需性** | 未做删减推演 | 「删掉后平台核心是否正常」可回答的依赖图 |
| **tests 引用** | 未采集 | 测试代码对表名的引用（P5 TEST_FIXTURE 判据） |

**优先追踪**：ext_* / WM_* / WH_* / base_* / sa_*（§0A.6.6）。
