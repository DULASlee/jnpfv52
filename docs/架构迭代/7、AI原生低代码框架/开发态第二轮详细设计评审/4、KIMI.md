我来对第六章进行深度评审。首先让我搜索一些关键信息来验证设计的合理性
# 第六章 · 数据模型与数据库设计 · 评审报告

**评审时间**: 2026-06-29 ~ 2026-07-03  
**评审专家**: KIMI（数据模型专家）  
**评审范围**: 第六章全文（§6.0 ~ §6.10）

---

## 一、评审摘要

| 评审维度             | 评分 | 关键结论                                                    |
| -------------------- | ---- | ----------------------------------------------------------- |
| 表结构完整性         | 7/10 | 核心表已覆盖功能需求，但**数量缺口20张**（声称80+，实际60） |
| 命名规范统一性       | 9/10 | 全局一致，F_前缀 + 租户基类 + 审计字段全部合规              |
| BASE_SANDBOX双层模式 | 6/10 | 设计合理但**缺少7个关键运维字段**                           |
| BASE_IR_VERSION      | 7/10 | 满足增量修改需求，但**缺少版本清理机制**                    |
| 知识图谱并发控制     | 5/10 | 乐观锁设计正确，但**事务边界未明确**                        |
| 索引设计             | 6/10 | 基础索引存在，缺少性能优化索引                              |

**综合评分: 6.7/10** — 需修正3项Gate阻塞项后方可进入开发阶段。

---

## 二、核心发现

### 🔴 严重 [DS-6-CRIT-01] 表数量缺口：60 vs 80+

文档声称"80+张表"，经逐章逐节统计，实际仅列出**60张**（含 `EXT_*` 占位）。缺口20张主要分布在：

| 模块         | 缺口说明                                                     |
| ------------ | ------------------------------------------------------------ |
| system       | BASE_ORGANIZE_DETAIL、BASE_DICT_TYPE、BASE_BILLRULE_DETAIL 等子表未展开 |
| extend       | `EXT_*` 未具体化，仅作占位                                   |
| visualdata   | 仅列2张，缺少 VISUALDATA_DATASET、VISUALDATA_CHART 等        |
| 运行态       | 9张中仅5张有DDL，其余为"纸面方案"                            |
| StaleMonitor | §4.9a 定义了服务但无对应表                                   |

**建议**: 修正文档声明为"60+张表（含运行态纸面方案）"，或补充缺失DDL。

---

### 🔴 严重 [DS-6-CRIT-02] BASE_SANDBOX 缺少关键运维字段

当前设计19个字段，但缺少以下**7个运维关键字段**：

| 缺失字段              | 类型          | 用途         | 影响                   |
| --------------------- | ------------- | ------------ | ---------------------- |
| `F_PIPELINE_ID`       | BIGINT        | 关联流水线   | 无法追溯沙箱所属流水线 |
| `F_CREATED_BY`        | NVARCHAR(50)  | 创建者       | 无法审计谁创建了沙箱   |
| `F_LAST_HEALTH_CHECK` | DATETIME      | 最后健康检查 | 无法判断沙箱是否存活   |
| `F_HEALTH_STATUS`     | NVARCHAR(20)  | 健康状态     | 无法筛选健康/异常沙箱  |
| `F_EXIT_CODE`         | INT           | 容器退出码   | 无法分析故障原因       |
| `F_ERROR_LOG`         | NVARCHAR(MAX) | 错误日志     | 无法排查部署失败       |
| `F_RESTART_COUNT`     | INT           | 重启次数     | 无法统计沙箱稳定性     |

**补充DDL**:
```sql
ALTER TABLE BASE_SANDBOX ADD F_PIPELINE_ID BIGINT NULL;
ALTER TABLE BASE_SANDBOX ADD F_CREATED_BY NVARCHAR(50) NULL;
ALTER TABLE BASE_SANDBOX ADD F_LAST_HEALTH_CHECK DATETIME NULL;
ALTER TABLE BASE_SANDBOX ADD F_HEALTH_STATUS NVARCHAR(20) DEFAULT 'unknown';
ALTER TABLE BASE_SANDBOX ADD F_EXIT_CODE INT NULL;
ALTER TABLE BASE_SANDBOX ADD F_ERROR_LOG NVARCHAR(MAX) NULL;
ALTER TABLE BASE_SANDBOX ADD F_RESTART_COUNT INT DEFAULT 0;

CREATE INDEX IDX_SANDBOX_PIPELINE ON BASE_SANDBOX(F_PIPELINE_ID);
CREATE INDEX IDX_SANDBOX_HEALTH ON BASE_SANDBOX(F_TENANT_ID, F_HEALTH_STATUS, F_LAST_HEALTH_CHECK);
```

---

### 🔴 严重 [DS-6-CRIT-03] 知识图谱乐观锁缺少事务边界定义

文档§6.6.3定义的UPSERT流程：

```
1. SELECT F_ID, F_VERSION FROM BASE_KNOWLEDGE_NODE
2. INSERT INTO BASE_KNOWLEDGE_NODE_BACKUP (当前行)
3. UPDATE BASE_KNOWLEDGE_NODE SET ... F_VERSION = F_VERSION + 1 WHERE ...
```

**三个致命问题**:

1. **事务边界未明确**: 步骤1→2→3是否在同一事务？若不在，可能出现"备份成功但UPDATE失败"的数据不一致
2. **版本冲突无重试**: "affected rows = 0 → 返回当前版本号让调用方决定"——调用方如何处理？缺少自动重试策略
3. **备份表无限膨胀**: 每次UPSERT都产生备份记录，无清理机制。按每天100次更新，一年产生36,500条

**修正方案**（SqlSugar事务包装 + 指数退避重试）:

```csharp
public async Task<KnowledgeUpsertResult> UpsertWithOptimisticLockAsync(
    KnowledgeNode node, int maxRetries = 3)
{
    for (int attempt = 0; attempt < maxRetries; attempt++)
    {
        try
        {
            return await _db.Ado.UseTranAsync(async () =>
            {
                // 1. 查询当前版本
                var existing = await _db.Queryable<BASE_KNOWLEDGE_NODE>()
                    .Where(n => n.F_NODE_ID == node.F_NODE_ID)
                    .FirstAsync();
                
                if (existing != null)
                {
                    // 2. 备份旧版本（同一事务内）
                    await _db.Insertable(new BASE_KNOWLEDGE_NODE_BACKUP
                    {
                        F_ORIGINAL_ID = existing.F_ID,
                        F_TENANT_ID = existing.F_TENANT_ID,
                        F_NODE_ID = existing.F_NODE_ID,
                        F_NODE_NAME = existing.F_NAME,
                        F_CONTENT = existing.F_CONTENT,
                        F_VERSION = existing.F_VERSION,
                        F_BACKUP_AT = DateTime.UtcNow,
                        F_BACKUP_REASON = "upsert",
                        F_BACKUP_BY = _currentUserId,
                        F_BACKUP_SEQ = await GetNextBackupSeqAsync(existing.F_NODE_ID)
                    }).ExecuteCommandAsync();
                    
                    // 3. 乐观锁更新
                    var affected = await _db.Updateable<BASE_KNOWLEDGE_NODE>()
                        .SetColumns(n => new BASE_KNOWLEDGE_NODE 
                        { 
                            F_NAME = node.F_NAME,
                            F_CONTENT = node.F_CONTENT,
                            F_VERSION = existing.F_VERSION + 1,
                            F_MODIFY_TIME = DateTime.UtcNow
                        })
                        .Where(n => n.F_ID == existing.F_ID && n.F_VERSION == existing.F_VERSION)
                        .ExecuteCommandAsync();
                    
                    if (affected == 0)
                        throw new OptimisticLockException("版本冲突");
                    
                    return KnowledgeUpsertResult.Success(existing.F_VERSION + 1);
                }
                else
                {
                    node.F_VERSION = 1;
                    await _db.Insertable(node).ExecuteCommandAsync();
                    return KnowledgeUpsertResult.Success(1);
                }
            });
        }
        catch (OptimisticLockException) when (attempt < maxRetries - 1)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(100 * Math.Pow(2, attempt)));
            continue;
        }
    }
    return KnowledgeUpsertResult.Failure("超过最大重试次数");
}
```

---

### 🟠 高 [DS-6-HIGH-01] BASE_IR_VERSION 缺少版本清理机制

文档提到"每个阶段每个流水线最多保留10个版本"，但DDL中**无任何清理机制**。IR JSON可能很大（100KB+），无限增长将导致存储膨胀。

**补充清理存储过程**:

```sql
CREATE PROCEDURE sp_CleanupIrVersions
AS
BEGIN
    SET NOCOUNT ON;
    
    -- 每个流水线每个阶段保留最近10个superseded版本
    WITH RankedVersions AS (
        SELECT F_ID, F_PIPELINE_ID, F_STAGE, F_VERSION,
               ROW_NUMBER() OVER (PARTITION BY F_PIPELINE_ID, F_STAGE ORDER BY F_VERSION DESC) AS rn
        FROM BASE_IR_VERSION
        WHERE F_STATUS = 'superseded'
    )
    DELETE FROM BASE_IR_VERSION
    WHERE F_ID IN (SELECT F_ID FROM RankedVersions WHERE rn > 10);
    
    -- 安全网：保留最近90天
    DELETE FROM BASE_IR_VERSION
    WHERE F_STATUS = 'superseded' AND F_CREATED_AT < DATEADD(day, -90, GETDATE());
END;
```

---

### 🟠 高 [DS-6-HIGH-02] 备份表无清理机制

同上，需为 `BASE_KNOWLEDGE_NODE_BACKUP` 添加保留策略：

```sql
CREATE PROCEDURE sp_CleanupKnowledgeBackups
AS
BEGIN
    SET NOCOUNT ON;
    
    -- 每个节点保留最近5个版本
    WITH RankedBackups AS (
        SELECT F_ID, F_NODE_ID,
               ROW_NUMBER() OVER (PARTITION BY F_NODE_ID ORDER BY F_BACKUP_AT DESC) AS rn
        FROM BASE_KNOWLEDGE_NODE_BACKUP
    )
    DELETE FROM BASE_KNOWLEDGE_NODE_BACKUP
    WHERE F_ID IN (SELECT F_ID FROM RankedBackups WHERE rn > 5);
    
    -- 保留最近30天
    DELETE FROM BASE_KNOWLEDGE_NODE_BACKUP WHERE F_BACKUP_AT < DATEADD(day, -30, GETDATE());
END;
```

---

### 🟠 高 [DS-6-HIGH-03] 缺少 StaleMonitor 专用表

§4.9a 定义了 `StaleMonitorService`，但仅依赖 `BASE_AI_PIPELINE_MESSAGE` 存储告警消息，查询效率低。建议新增：

```sql
CREATE TABLE BASE_AI_PIPELINE_STALE_LOG (
  F_ID BIGINT PRIMARY KEY,
  F_TENANT_ID NVARCHAR(50) NOT NULL,
  F_PIPELINE_ID BIGINT NOT NULL,
  F_FROM_STAGE NVARCHAR(20) NOT NULL,
  F_TO_STATUS NVARCHAR(20) NOT NULL DEFAULT 'stale',
  F_REASON NVARCHAR(200),
  F_DETECTED_AT DATETIME NOT NULL,
  F_RESOLVED_AT DATETIME,
  F_RESOLVED_BY NVARCHAR(50),
  F_NOTIFICATION_SENT BIT DEFAULT 0,
  F_IS_DELETED BIT DEFAULT 0,
  INDEX IDX_PIPELINE (F_PIPELINE_ID),
  INDEX IDX_TENANT_STATUS (F_TENANT_ID, F_TO_STATUS, F_DETECTED_AT)
);
```

---

## 三、命名规范评审结论

| 规则                                        | 状态   | 说明                                               |
| ------------------------------------------- | ------ | -------------------------------------------------- |
| 表名: `{MODULE_PREFIX}_{ENTITY}` 大写下划线 | ✅ 合规 | BASE_AI_PIPELINE, VISUALDEV_FORM, FLOW_TASK 均符合 |
| 列名: `F_` 前缀                             | ✅ 合规 | 所有表统一                                         |
| 用户ID: string (不用long)                   | ✅ 合规 | `F_USER_ID NVARCHAR(50)`                           |
| 租户基类: `F_TENANT_ID`                     | ✅ 合规 | 所有业务表均有                                     |
| 逻辑删除: `F_IS_DELETED BIT DEFAULT 0`      | ✅ 合规 | 所有表均有                                         |
| 主键: `F_ID BIGINT`                         | ✅ 合规 | 所有表均有                                         |
| 审计字段: CREATE/MODIFY                     | ✅ 合规 | 所有表均有                                         |

---

## 四、综合结论与行动项

```
┌─────────────────────────────────────────────────────────────────────┐
│                    第六章评审结论（KIMI · 数据模型专家）               │
├─────────────────────────────────────────────────────────────────────┤
│ 评审维度              │ 评分    │ 说明                              │
├─────────────────────────────────────────────────────────────────────┤
│ 表结构完整性          │ 7/10   │ 核心表已覆盖，但数量缺口20张       │
│ 命名规范统一性        │ 9/10   │ 全局一致，F_前缀+租户基类+审计字段  │
│ BASE_SANDBOX双层模式  │ 6/10   │ 设计合理但缺少7个关键运维字段      │
│ BASE_IR_VERSION       │ 7/10   │ 满足增量修改需求，但缺少清理机制    │
│ 知识图谱并发控制      │ 5/10   │ 乐观锁设计正确，但事务边界未明确    │
│ 索引设计              │ 6/10   │ 基础索引存在，缺少性能优化索引      │
├─────────────────────────────────────────────────────────────────────┤
│ 综合评分              │ 6.7/10 │ 需修正后可进入开发阶段              │
└─────────────────────────────────────────────────────────────────────┘
```

### Gate阻塞项（必须修正）

| 编号         | 问题                      | 修正内容                              | 负责人     |
| ------------ | ------------------------- | ------------------------------------- | ---------- |
| DS-6-CRIT-03 | 知识图谱乐观锁事务边界    | 补充SqlSugar事务包装 + 指数退避重试   | 后端工程师 |
| DS-6-CRIT-02 | BASE_SANDBOX缺少运维字段  | 补充7个字段 + 3个索引                 | 后端工程师 |
| DS-6-HIGH-01 | BASE_IR_VERSION无清理机制 | 添加sp_CleanupIrVersions + Quartz Job | 后端工程师 |

### 强烈建议项（不阻塞但建议同期完成）

| 编号         | 问题               | 修正内容                       |
| ------------ | ------------------ | ------------------------------ |
| DS-6-HIGH-02 | 备份表无清理       | 添加sp_CleanupKnowledgeBackups |
| DS-6-HIGH-03 | 缺少StaleMonitor表 | 新增BASE_AI_PIPELINE_STALE_LOG |
| DS-6-MED-02  | 索引不足           | 补充关键表复合索引             |

### 文档修正

| 编号         | 修正内容                                                     |
| ------------ | ------------------------------------------------------------ |
| DS-6-CRIT-01 | 将"80+张表"修正为"60+张表（含运行态纸面方案）"，或补充缺失DDL |

---

**评审完成。建议修正Gate阻塞项后，由架构师复核并进入开发阶段。**