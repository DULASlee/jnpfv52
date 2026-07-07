-- =============================================================================
-- JNPF V5.2 — 报表模块初始化 (替代独立 Java ReportServer)
-- =============================================================================
-- 用途: 将报表元数据从独立 Java 服务迁移到主 .NET 后端
-- 执行: 在主数据库上运行此脚本
-- =============================================================================

-- 报表元数据表
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='BASE_REPORT' AND xtype='U')
BEGIN
    CREATE TABLE [dbo].[BASE_REPORT] (
        [F_ID]              NVARCHAR(50)    NOT NULL PRIMARY KEY,
        [F_FULL_NAME]       NVARCHAR(200)   NOT NULL,           -- 报表名称
        [F_EN_CODE]         NVARCHAR(100)   NOT NULL,           -- 报表编码
        [F_CATEGORY]        NVARCHAR(50)    NULL,               -- 分类 (字典 ReportSort)
        [F_DESCRIPTION]     NVARCHAR(500)   NULL,               -- 说明
        [F_ENABLED_MARK]    INT             NOT NULL DEFAULT 1, -- 启用标记
        [F_SORT_CODE]       BIGINT          NULL DEFAULT 0,     -- 排序
        [F_REPORT_FILE]     NVARCHAR(200)   NULL,               -- XML 文件名
        [F_CONTENT]         NTEXT           NULL,               -- XML 内容 (冗余)

        -- 标准审计字段 (CLDSEntityBase)
        [F_DELETE_MARK]     INT             NULL,
        [F_CREATOR_USER_ID] NVARCHAR(50)    NULL,
        [F_CREATOR_TIME]    DATETIME        NULL DEFAULT GETDATE(),
        [F_LAST_MODIFY_USER_ID] NVARCHAR(50) NULL,
        [F_LAST_MODIFY_TIME]    DATETIME    NULL DEFAULT GETDATE(),

        -- 索引
        INDEX IX_BASE_REPORT_EN_CODE   NONCLUSTERED ([F_EN_CODE]),
        INDEX IX_BASE_REPORT_CATEGORY  NONCLUSTERED ([F_CATEGORY]),
        INDEX IX_BASE_REPORT_SORT      NONCLUSTERED ([F_SORT_CODE]),
        INDEX IX_BASE_REPORT_ENABLED   NONCLUSTERED ([F_ENABLED_MARK])
    );
END
GO

-- 迁移现有报表数据 (从 Java UReport2 文件系统 → SQL Server)
-- 如果之前有 Java ReportServer 服务，报表 XML 文件存放在:
--   {ureport2-console}/ureportfiles/*.ureport.xml
-- 迁移步骤:
--   1. 停止 Java ReportServer
--   2. 将 XML 文件复制到 {API}/ReportFiles/ 目录
--   3. 可选: 编写迁移脚本解析 XML 并填充 BASE_REPORT 表

PRINT '报表模块初始化完成.';
GO
