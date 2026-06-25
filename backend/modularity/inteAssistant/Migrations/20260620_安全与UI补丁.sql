-- ========================================
-- 安全与UI层专家扫描补丁
-- 日期：2026-06-20
-- 来源：首席架构师裁定（千问/清言/玛维思 39 项合并去重）
-- 执行顺序：1→2→3→4→5（逐段执行，每段确认无错误后继续）
-- ========================================

-- ==================== 1. BASE_FOUNDER_AUTH_LOG 审计字段（千问 SEC-07）====================
-- 用途：TOTP Session 绑定，记录创始人认证时的客户端指纹信息
ALTER TABLE BASE_FOUNDER_AUTH_LOG ADD F_CLIENT_IP NVARCHAR(50) NULL;
ALTER TABLE BASE_FOUNDER_AUTH_LOG ADD F_USER_AGENT NVARCHAR(500) NULL;
ALTER TABLE BASE_FOUNDER_AUTH_LOG ADD F_DEVICE_FINGERPRINT NVARCHAR(64) NULL;

-- ==================== 2. BASE_FOUNDER_AUTH_LOG 不可删触发器（玛维思 #10）====================
-- 用途：防止任何人（含创始人/管理员/DBA）删除审计记录
-- 注意：需要 DBA 权限执行 CREATE TRIGGER
IF NOT EXISTS (SELECT * FROM sys.triggers WHERE name = 'TRG_FOUNDER_AUTH_LOG_NO_DELETE')
BEGIN
    EXEC('
    CREATE TRIGGER TRG_FOUNDER_AUTH_LOG_NO_DELETE
    ON BASE_FOUNDER_AUTH_LOG
    INSTEAD OF DELETE
    AS
    BEGIN
        SET NOCOUNT ON;
        RAISERROR(''BASE_FOUNDER_AUTH_LOG is immutable. DELETE is prohibited.'', 16, 1);
        ROLLBACK TRANSACTION;
    END;
    ');
END;
GO

-- ==================== 3. BASE_IR_VERSION 补充索引（玛维思 #8）====================
-- 已存在索引（不重复创建）：
--   IDX_IR_VERSION_QUERY (F_PIPELINE_ID, F_SNAPSHOT_AT DESC)
--   IDX_IR_VERSION_CLEANUP (F_PIPELINE_ID, F_CHANGE_TYPE, F_SNAPSHOT_AT)
--   IDX_IR_VERSION_TREE (F_PIPELINE_ID, F_PARENT_VERSION_ID)
-- 以下为新增索引：

-- 3.1 活跃版本查询（pipeline × status）
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IDX_IR_VERSION_ACTIVE')
    CREATE INDEX IDX_IR_VERSION_ACTIVE ON BASE_IR_VERSION(F_PIPELINE_ID, F_STATUS);

-- 3.2 租户隔离
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IDX_IR_VERSION_TENANT')
    CREATE INDEX IDX_IR_VERSION_TENANT ON BASE_IR_VERSION(F_TENANT_ID);

-- 3.3 阶段回溯（pipeline × stage）
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IDX_IR_VERSION_STAGE')
    CREATE INDEX IDX_IR_VERSION_STAGE ON BASE_IR_VERSION(F_PIPELINE_ID, F_STAGE);
GO

-- ==================== 4. BASE_SANDBOX 外键（玛维思 #9）====================
-- ⚠️ DBA 执行前必读：
--   1. 先确认 BASE_AI_PIPELINE.F_ID 存在且类型为 BIGINT
--   2. 先执行脏数据检查：
--      SELECT F_PIPELINE_ID FROM BASE_SANDBOX
--      WHERE F_PIPELINE_ID IS NOT NULL
--        AND F_PIPELINE_ID NOT IN (SELECT F_ID FROM BASE_AI_PIPELINE);
--   3. 如有孤儿引用记录，先清理再执行外键创建
--   4. 确认无误后取消下面两行注释执行：
-- ALTER TABLE BASE_SANDBOX ADD CONSTRAINT FK_SANDBOX_PIPELINE
-- FOREIGN KEY (F_PIPELINE_ID) REFERENCES BASE_AI_PIPELINE(F_ID);
-- 当前外键创建已注释，DBA 确认后手动执行

-- ==================== 5. BASE_EAB_VIOLATION_LOG 新表（玛维思 #11）====================
-- 用途：合规审计——记录 EAB（Ethical AI Boundary）违规事件
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='BASE_EAB_VIOLATION_LOG' AND xtype='U')
CREATE TABLE BASE_EAB_VIOLATION_LOG (
    F_ID                BIGINT NOT NULL PRIMARY KEY,
    F_TENANT_ID         NVARCHAR(50) NOT NULL,
    F_PIPELINE_ID       BIGINT NULL,
    F_AGENT             NVARCHAR(50) NULL,
    F_KNOWLEDGE_ID      NVARCHAR(50) NULL,
    F_VIOLATION_TYPE    NVARCHAR(30) NOT NULL,
    F_VIOLATION_DETAIL  NVARCHAR(MAX) NULL,
    F_ACTION            NVARCHAR(20) NOT NULL DEFAULT 'logged',
    F_CREATOR_USER_ID   NVARCHAR(50) NULL,
    F_CREATOR_TIME      DATETIME NULL
);

-- 索引：按租户+时间查询违规日志
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IDX_EAB_TENANT')
    CREATE INDEX IDX_EAB_TENANT ON BASE_EAB_VIOLATION_LOG(F_TENANT_ID, F_CREATOR_TIME);

-- 索引：按流水线查询关联违规
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IDX_EAB_PIPELINE')
    CREATE INDEX IDX_EAB_PIPELINE ON BASE_EAB_VIOLATION_LOG(F_PIPELINE_ID);

-- ========================================
-- 执行确认清单
-- □ 第 1 段：3 个 ALTER TABLE 执行成功，字段已添加
-- □ 第 2 段：TRIGGER 创建成功（SELECT * FROM sys.triggers WHERE name = 'TRG_FOUNDER_AUTH_LOG_NO_DELETE'）
-- □ 第 3 段：3 个新索引创建成功（SELECT * FROM sys.indexes WHERE name IN ('IDX_IR_VERSION_ACTIVE','IDX_IR_VERSION_TENANT','IDX_IR_VERSION_STAGE')）
-- □ 第 4 段：外键已确认或推迟（脏数据检查 PASS / 已清理 / 已取消注释执行）
-- □ 第 5 段：BASE_EAB_VIOLATION_LOG 已创建，2 个索引已创建
-- ========================================

-- 注意：
-- - EVAL_METRIC 表已在 20260620_第六章补丁.sql（DB-28）中创建，不重复创建 ✅
-- - BASE_AI_PIPELINE_STALE_LOG 已由首席架构师裁定为"不需要独立表"（DB-29 砍掉），stale 状态通过 BASE_AI_PIPELINE_MESSAGE 记录
