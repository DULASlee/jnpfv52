# NG-1B Provenance Matrix 实施计划 v1.0

**日期**：2026-08-26 ｜ **裁决依据**：NG-1 规格 §0A.7（G0 条件 PASS 裁决 + 启动批准）｜ **状态**：已批准执行
**性质**：只读审计（五零约束不变：零业务代码修改 / 零数据库修改 / 零微服务实现 / 零 Aspire 引入 / 零迁移）

---

## 1. 目标

回答每一张表的根本问题：**「这张表为什么存在？」**——谁创建、谁拥有、谁读写、是否平台依赖、删除后发生什么。

最终每张表获得 `PROVEN` / `PARTIAL` / `UNKNOWN` 三态，**证据驱动，非主观判断**。

## 2. 14 维定义

| # | 维度 | 必须回答 | 证据源 |
|---|------|---------|--------|
| 1 | DB Object | 表/列/索引 | E1 db-matrix-raw.tsv（rows/cols/pk） |
| 2 | Creation Source | SQL / Migration / Seed 精确位置 | E3 + 新增：ZXAFINIT.sql 字节偏移/行号、DB/ 目录其他 migration、CodeFirst 证据 |
| 3 | Code Owner | 哪个项目/模块 | 前缀族 → backend/modularity 模块映射 |
| 4 | Write Owner | 谁实际写 | 第一批 ownership-matrix-v1.csv + 服务代码 |
| 5 | Read Consumers | 谁读取 | E4 _no-entity-refs.tsv + E5 sa-service |
| 6 | API | 是否被 API 暴露 | IDynamicApiController 服务扫描 |
| 7 | UI/Menu | 是否存在平台 UI 入口 | 新增：前端表名/API 引用扫描 + BASE_SYSTEM_MENU 菜单数据实测 |
| 8 | Template | 是否由模板安装产生 | 模板安装机制验证 |
| 9 | Demo | 是否仅用于演示 | ext 服务 + demo 代码特征 |
| 10 | Runtime | 平台运行是否必须 | 分类（P0/P1=必须，其余=非必须） |
| 11 | Startup | 删除后平台能否启动 | 依赖图推演（静态证据；真删实验归 NG-1C） |
| 12 | Product | 是否属于产品交付内容 | P2/P3 判定 |
| 13 | Lifecycle | Mandatory/Optional/Template/Demo/Legacy | 已产出 asset_lifecycle 列 |
| 14 | Provenance | PROVEN / PARTIAL / UNKNOWN | 综合判定（规则见 §3） |

## 3. Provenance 三态判定规则（脚本可复算）

```text
score = creation_position(0/1) + entity_mapped(0/1) + code_refs>0(0/1)
      + api_exposed(0/1) + ui_menu(0/1) + module_owner(0/1)      # 满分 6
PROVEN   : score >= 5 且 creation + owner + (write 或 read) 均有证据
PARTIAL  : 2 <= score < 5 或 score >= 5 但 creation/owner/write/read 任一缺位
UNKNOWN  : score < 2
```

**PX UNKNOWN 6 张不得猜测归属**——分类保持 PX，provenance 如实输出 PARTIAL/UNKNOWN。

## 4. 优先追踪集合

| 集合 | 张数 | 深挖问题 |
|------|-----|---------|
| ext_* | 19 | 谁创建？哪个 Seed？哪个菜单？哪个模板？哪个安装包？删除后平台是否完整？（暂不接受「Order Domain」结论） |
| WFORM_* | 51 | 模板定义 → 模板安装 → 创建哪些表/字段/菜单/流程（产品模板链） |
| WM_*/WH_* | 39 | DB 有 / Code 无 / Migration? / Seed? / UI? / API? / 历史版本? / 客户数据? / Demo? → ARCHIVE/DELETE/MIGRATE/DEMO/LEGACY 建议 |
| base_* | 103 | 模块归属 + 菜单/API 可达性 |
| sa_* | 13 | backend Dapper + sa-service 双端读写位置 |

## 5. 产出物（完成后 STOP）

1. `provenance-matrix.csv` — 289 表 × 14 维 + 三态
2. `provenance-matrix-report.md` — 优先集合深挖 + 三态统计 + 处置建议
3. `G0-Final-Review.md` — **PASS / REFINE / BLOCK 三选一**（非 PASS-PENDING）

产出目录：`.claude/evidence/jnpf-next-architecture/ng1b-provenance/`

## 6. 禁止项（§0A.7）

不进入 Domain Ownership Proof；不恢复 D12；不进行微服务设计；不删除任何表（42 张孤儿仅 Provenance + 处置建议）；PX UNKNOWN 不猜测归属；不真删生产数据库。

**完成后 STOP，等待人工裁决（G0 Final：PASS/REFINE/BLOCK）。**
