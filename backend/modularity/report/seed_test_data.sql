-- =============================================================================
-- JNPF V5.2 — 报表模块测试数据
-- =============================================================================
-- 前置条件:
--   1. 已执行 init_report.sql 创建 BASE_REPORT 表
--   2. 字典 'ReportSort' 已存在（JNPF 种子数据），否则先执行下方字典 SQL
-- 执行方式: 在 SQL Server Management Studio 中运行此脚本
-- =============================================================================

-- ═══════════════════════════════════════════════════════════════
-- 可选: 确保 ReportSort 字典分类存在
-- ═══════════════════════════════════════════════════════════════
IF NOT EXISTS (SELECT 1 FROM [dbo].[BASE_DICTIONARY_TYPE] WHERE [F_EN_CODE] = 'ReportSort')
BEGIN
    INSERT INTO [dbo].[BASE_DICTIONARY_TYPE] ([F_ID], [F_FULL_NAME], [F_EN_CODE], [F_ENABLED_MARK], [F_SORT_CODE], [F_DELETE_MARK])
    VALUES (NEWID(), '报表分类', 'ReportSort', 1, 100, NULL);

    DECLARE @reportSortTypeId NVARCHAR(50);
    SELECT @reportSortTypeId = [F_ID] FROM [dbo].[BASE_DICTIONARY_TYPE] WHERE [F_EN_CODE] = 'ReportSort';

    INSERT INTO [dbo].[BASE_DICTIONARY_DATA] ([F_ID], [F_PARENT_ID], [F_FULL_NAME], [F_EN_CODE], [F_ENABLED_MARK], [F_SORT_CODE], [F_DICTIONARY_TYPE_ID], [F_DELETE_MARK])
    VALUES
        (NEWID(), NULL, '销售报表', 'SalesReport',    1, 1, @reportSortTypeId, NULL),
        (NEWID(), NULL, '财务报表', 'FinanceReport',   1, 2, @reportSortTypeId, NULL),
        (NEWID(), NULL, '运营报表', 'OperationReport', 1, 3, @reportSortTypeId, NULL),
        (NEWID(), NULL, '人力资源', 'HRReport',        1, 4, @reportSortTypeId, NULL);

    PRINT 'ReportSort 字典初始化完成.';
END
GO

-- ═══════════════════════════════════════════════════════════════
-- 获取字典 ID 用于测试数据的 Category 字段
-- ═══════════════════════════════════════════════════════════════
DECLARE @catSales      NVARCHAR(50) = (SELECT TOP 1 [F_ID] FROM [dbo].[BASE_DICTIONARY_DATA] WHERE [F_EN_CODE] = 'SalesReport'    AND [F_DELETE_MARK] IS NULL);
DECLARE @catFinance    NVARCHAR(50) = (SELECT TOP 1 [F_ID] FROM [dbo].[BASE_DICTIONARY_DATA] WHERE [F_EN_CODE] = 'FinanceReport'   AND [F_DELETE_MARK] IS NULL);
DECLARE @catOperation  NVARCHAR(50) = (SELECT TOP 1 [F_ID] FROM [dbo].[BASE_DICTIONARY_DATA] WHERE [F_EN_CODE] = 'OperationReport' AND [F_DELETE_MARK] IS NULL);
DECLARE @catHR         NVARCHAR(50) = (SELECT TOP 1 [F_ID] FROM [dbo].[BASE_DICTIONARY_DATA] WHERE [F_EN_CODE] = 'HRReport'        AND [F_DELETE_MARK] IS NULL);
DECLARE @adminUserId   NVARCHAR(50) = (SELECT TOP 1 [F_ID] FROM [dbo].[BASE_USER] WHERE [F_ACCOUNT] = 'admin' AND [F_DELETE_MARK] IS NULL);

-- ═══════════════════════════════════════════════════════════════
-- 插入 5 条测试报表数据
-- ═══════════════════════════════════════════════════════════════
-- 清理旧测试数据
DELETE FROM [dbo].[BASE_REPORT] WHERE [F_EN_CODE] LIKE 'RP_%';

-- 1. 月度销售汇总（启用，排序靠前）
INSERT INTO [dbo].[BASE_REPORT] ([F_ID], [F_FULL_NAME], [F_EN_CODE], [F_CATEGORY], [F_DESCRIPTION], [F_ENABLED_MARK], [F_SORT_CODE], [F_REPORT_FILE], [F_CREATOR_USER_ID], [F_CREATOR_TIME], [F_LAST_MODIFY_TIME], [F_DELETE_MARK])
VALUES (NEWID(), '月度销售汇总报表', 'RP_SalesMonthly', @catSales, '按月份汇总各区域销售额、回款率、毛利率', 1, 1, 'sales_monthly.ureport.xml', @adminUserId, GETDATE(), GETDATE(), NULL);

-- 2. 财务报表 — 资产负债表（启用）
INSERT INTO [dbo].[BASE_REPORT] ([F_ID], [F_FULL_NAME], [F_EN_CODE], [F_CATEGORY], [F_DESCRIPTION], [F_ENABLED_MARK], [F_SORT_CODE], [F_REPORT_FILE], [F_CREATOR_USER_ID], [F_CREATOR_TIME], [F_LAST_MODIFY_TIME], [F_DELETE_MARK])
VALUES (NEWID(), '资产负债表', 'RP_BalanceSheet', @catFinance, '标准财务报表 — 资产负债表，含期末余额和年初余额', 1, 2, 'balance_sheet.ureport.xml', @adminUserId, DATEADD(DAY, -1, GETDATE()), GETDATE(), NULL);

-- 3. 运营报表 — 订单统计（启用）
INSERT INTO [dbo].[BASE_REPORT] ([F_ID], [F_FULL_NAME], [F_EN_CODE], [F_CATEGORY], [F_DESCRIPTION], [F_ENABLED_MARK], [F_SORT_CODE], [F_REPORT_FILE], [F_CREATOR_USER_ID], [F_CREATOR_TIME], [F_LAST_MODIFY_TIME], [F_DELETE_MARK])
VALUES (NEWID(), '订单统计报表', 'RP_OrderStats', @catOperation, '按天/周/月统计订单量、客单价、退单率', 1, 3, 'order_stats.ureport.xml', @adminUserId, DATEADD(DAY, -3, GETDATE()), GETDATE(), NULL);

-- 4. 人力资源 — 考勤月报（禁用状态，测试过滤）
INSERT INTO [dbo].[BASE_REPORT] ([F_ID], [F_FULL_NAME], [F_EN_CODE], [F_CATEGORY], [F_DESCRIPTION], [F_ENABLED_MARK], [F_SORT_CODE], [F_REPORT_FILE], [F_CREATOR_USER_ID], [F_CREATOR_TIME], [F_LAST_MODIFY_TIME], [F_DELETE_MARK])
VALUES (NEWID(), '员工考勤月报', 'RP_AttendanceMonthly', @catHR, '各部门员工出勤率、加班时长、请假统计', 0, 4, 'attendance_monthly.ureport.xml', @adminUserId, DATEADD(DAY, -7, GETDATE()), GETDATE(), NULL);

-- 5. 销售报表 — 业绩排行（启用，最新创建）
INSERT INTO [dbo].[BASE_REPORT] ([F_ID], [F_FULL_NAME], [F_EN_CODE], [F_CATEGORY], [F_DESCRIPTION], [F_ENABLED_MARK], [F_SORT_CODE], [F_REPORT_FILE], [F_CREATOR_USER_ID], [F_CREATOR_TIME], [F_LAST_MODIFY_TIME], [F_DELETE_MARK])
VALUES (NEWID(), '销售业绩排行', 'RP_SalesRanking', @catSales, '按销售人员和区域排名，含环比增长率', 1, 5, 'sales_ranking.ureport.xml', @adminUserId, GETDATE(), GETDATE(), NULL);

-- ═══════════════════════════════════════════════════════════════
-- 验证
-- ═══════════════════════════════════════════════════════════════
SELECT
    [F_FULL_NAME]     AS [报表名称],
    [F_EN_CODE]       AS [编码],
    d.[F_FULL_NAME]   AS [分类],
    CASE [F_ENABLED_MARK] WHEN 1 THEN '启用' ELSE '禁用' END AS [状态],
    [F_SORT_CODE]     AS [排序],
    [F_CREATOR_TIME]  AS [创建时间]
FROM [dbo].[BASE_REPORT] r
LEFT JOIN [dbo].[BASE_DICTIONARY_DATA] d ON r.[F_CATEGORY] = d.[F_ID]
WHERE r.[F_EN_CODE] LIKE 'RP_%'
ORDER BY r.[F_SORT_CODE];

PRINT '报表测试数据初始化完成. 共 5 条 (4 启用 + 1 禁用).';
GO
