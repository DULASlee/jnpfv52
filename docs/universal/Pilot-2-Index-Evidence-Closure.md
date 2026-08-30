# Pilot 2 Index Evidence Closure

**Phase**: 7 — Closure Artifact
**Status**: ✅ COMPLETE
**Date**: 2026-08-29
**Target Table**: `BASE_KNOWLEDGE_EDGE`
**Database**: `(local)\SQLEXPRESS` / `ZXAF_V1_DevTest1`

---

## 目的

补齐 Pilot 2 (BASE_KNOWLEDGE_NODE + BASE_KNOWLEDGE_EDGE) 中 EDGE 表索引添加的完整证据链（Before / Decision / Change / After），作为 Phase 7 Exit Gate #5 的 closure 产物。

**不做**：Performance Audit / Benchmark / 重新跑 Pilot / 重新审查 Phase 0–6。

---

## 一、Before（DB metadata — 索引添加前状态）

**采集命令**：

```sql
SELECT 
    i.name AS IndexName,
    i.type_desc AS IndexType,
    STUFF((SELECT ', ' + c.name FROM sys.index_columns ic 
           JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
           WHERE ic.object_id = i.object_id AND ic.index_id = i.index_id 
           ORDER BY ic.key_ordinal FOR XML PATH('')), 1, 2, '') AS Columns,
    i.is_primary_key AS IsPrimaryKey
FROM sys.indexes i
WHERE i.object_id = OBJECT_ID('BASE_KNOWLEDGE_EDGE') AND i.is_hypothetical = 0
ORDER BY i.index_id;
```

**实际输出**：

| IndexName | IndexType | Columns | IsPrimaryKey |
|---|---|---|---|
| `PK__BASE_KNO__2C6EC7C37D5BBF05` | CLUSTERED | F_ID | 1 |

**结论**：EDGE 表**只有 PK 索引**，**没有 F_SOURCE_NODE_ID / F_TARGET_NODE_ID / F_RELATION_TYPE / F_TENANT_ID 的任何二级索引**。所有相关查询触发 Clustered Index Scan（全表扫描）。

这与 Pilot 2 Finding C-1 一致：

> "EDGE 表查询触发全表扫描——无索引 on F_SOURCE_NODE_ID, F_TARGET_NODE_ID, F_TENANT_ID, F_RELATION_TYPE"

---

## 二、Decision（执行计划证据 — 添加索引前）

**采集命令**：先 drop 索引以还原 Before 状态，然后查询执行计划。

**实际执行计划**（BEFORE 状态）：

```sql
SELECT F_ID, F_SOURCE_NODE_ID 
FROM BASE_KNOWLEDGE_EDGE 
WHERE F_TENANT_ID = 'default' AND F_SOURCE_NODE_ID = 'TEST_VALUE';
```

**实际输出**：

```
|--Clustered Index Scan(OBJECT:([...].[BASE_KNOWLEDGE_EDGE].[PK__BASE_KNO__2C6EC7C37D5BBF05]),
   WHERE:([...].[F_TENANT_ID]=CONVERT_IMPLICIT(...) AND [...].[F_SOURCE_NODE_ID]=CONVERT_IMPLICIT(...)))
```

**结论**：WHERE filter 在 **Clustered Index Scan 之后**应用——典型的**全表扫描 + 过滤**模式。效率极低，尤其在 EDGE 表数据量大时。

这印证了 Pilot 2 Finding C-2：

> "In-memory index compensates for missing DB indexes — the ConcurrentDictionary _outEdges/_inEdges is a performance workaround for absent indexes"

也印证了 Finding C-1 中 `EnsureIndexLoadedAsync()` 首次调用会**加载所有边**到内存的事实。

---

## 三、Change（实际 DDL — 已执行）

**执行命令**：

```sql
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='IDX_KNOWLEDGE_EDGE_SOURCE' AND object_id = OBJECT_ID('BASE_KNOWLEDGE_EDGE'))
    CREATE INDEX IDX_KNOWLEDGE_EDGE_SOURCE ON BASE_KNOWLEDGE_EDGE(F_TENANT_ID, F_SOURCE_NODE_ID);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='IDX_KNOWLEDGE_EDGE_TARGET' AND object_id = OBJECT_ID('BASE_KNOWLEDGE_EDGE'))
    CREATE INDEX IDX_KNOWLEDGE_EDGE_TARGET ON BASE_KNOWLEDGE_EDGE(F_TENANT_ID, F_TARGET_NODE_ID);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='IDX_KNOWLEDGE_EDGE_RELTYPE' AND object_id = OBJECT_ID('BASE_KNOWLEDGE_EDGE'))
    CREATE INDEX IDX_KNOWLEDGE_EDGE_RELTYPE ON BASE_KNOWLEDGE_EDGE(F_TENANT_ID, F_RELATION_TYPE);
```

**执行结果**：`Index creation: OK`

**索引设计意图**：

| 索引 | 列 | 服务目的 |
|---|---|---|
| `IDX_KNOWLEDGE_EDGE_SOURCE` | (F_TENANT_ID, F_SOURCE_NODE_ID) | 支持"找出某源节点的所有出边"查询 — BFS 反向索引 |
| `IDX_KNOWLEDGE_EDGE_TARGET` | (F_TENANT_ID, F_TARGET_NODE_ID) | 支持"找出某目标节点的所有入边"查询 — BFS 正向索引 |
| `IDX_KNOWLEDGE_EDGE_RELTYPE` | (F_TENANT_ID, F_RELATION_TYPE) | 支持"按关系类型过滤"查询 — ListEdgesAsync |

**为什么 Tenant 在前**：SQL Server 复合索引第一列是最常用的过滤/连接列。多租户隔离（ITenantFilter 强制 WHERE F_TENANT_ID = X）需要先按 Tenant 过滤，再按业务列过滤。

---

## 四、After（DB metadata + execution plan — 索引添加后）

**索引 metadata（AFTER）**：

| IndexName | IndexType | Columns |
|---|---|---|
| `PK__BASE_KNO__2C6EC7C37D5BBF05` | CLUSTERED | F_ID |
| `IDX_KNOWLEDGE_EDGE_SOURCE` | NONCLUSTERED | F_TENANT_ID, F_SOURCE_NODE_ID |
| `IDX_KNOWLEDGE_EDGE_TARGET` | NONCLUSTERED | F_TENANT_ID, F_TARGET_NODE_ID |
| `IDX_KNOWLEDGE_EDGE_RELTYPE` | NONCLUSTERED | F_TENANT_ID, F_RELATION_TYPE |

**执行计划（AFTER 状态 — 同一查询）**：

```sql
SELECT F_ID, F_SOURCE_NODE_ID 
FROM BASE_KNOWLEDGE_EDGE 
WHERE F_TENANT_ID = 'default' AND F_SOURCE_NODE_ID = 'TEST_VALUE';
```

**实际输出**：

```
|--Index Seek(OBJECT:([...].[BASE_KNOWLEDGE_EDGE].[IDX_KNOWLEDGE_EDGE_SOURCE]),
   SEEK:([F_TENANT_ID]=CONVERT_IMPLICIT(...) AND [F_SOURCE_NODE_ID]=CONVERT_IMPLICIT(...))
   ORDERED FORWARD
```

**结论**：

| 指标 | Before | After |
|---|---|---|
| 执行计划类型 | Clustered Index Scan | Index Seek |
| 索引使用 | 主键（无过滤作用）| IDX_KNOWLEDGE_EDGE_SOURCE |
| WHERE 应用时机 | Scan 后 | Seek 阶段（命中即返）|
| 数据量大时性能 | 线性恶化 | 对数级（log n）|

**核心证据**：**Table Scan → Index Seek** 已确认。

---

## 五、归档文件

| 文件 | 内容 |
|---|---|
| `docs/universal/pilot2-evidence-before-plan.sql` | Before 状态执行计划采集脚本 |
| `docs/universal/pilot2-execution-plan-before.txt` | Before 状态执行计划原始输出 |
| `docs/universal/pilot2-evidence-after.sql` | After 状态执行计划采集脚本 |
| `docs/universal/pilot2-execution-plan-after.txt` | After 状态执行计划原始输出 |
| `docs/universal/Pilot-2-Index-Evidence-Closure.md` | 本文档（汇总）|

---

## 六、归档结论

**Pilot-2 Index Evidence Closure: ✅ COMPLETE**

四项证据齐全：

| 阶段 | 状态 |
|---|---|
| Before（DB metadata） | ✅ 实际采集，EDGE 表只有 PK 索引 |
| Decision（执行计划） | ✅ 实际采集，Clustered Index Scan |
| Change（DDL） | ✅ 实际执行，3 条索引创建 |
| After（DB metadata + execution plan） | ✅ 实际采集，3 条新索引 + Index Seek |

**核心结论**：Pilot 2 提出的"EDGE 表全表扫描问题"已通过 DB metadata + 执行计划的真实证据闭环。

Phase 7 Exit Gate #5 (Pilot evidence archived) **现在真正满足**。

---

## 七、严禁的事（本 Closure 未做）

- ❌ 重新跑 Pilot 2
- ❌ 重新分析 EDGE 表
- ❌ 重做 Performance Audit
- ❌ Benchmark（IOPS / 不同数据规模等）
- ❌ 重新审查 Phase 0–6
- ❌ 修改 Universal Core
- ❌ 修改 Skill
- ❌ 新增 Pilot
