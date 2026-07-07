/*
  SA 九步九表 + 校验日志 — 数据存在性与字段合理性审计
  ----------------------------------------------------------------
  对应 DDL: backend/modularity/inteAssistant/Migrations/20260706_SA_NineTables.sql
  九步 → 表映射:
    1 DomainModel        → sa_scope
    2 AggregateDesign    → sa_dfd
    3 EventCatalog       → sa_business_process
    4 CommandQuery       → sa_data_dictionary
    5 IntegrationPoints  → sa_pspec
    6 WorkflowSpec       → sa_decision_table
    7 UISpec             → sa_state_machine
    8 DataModel          → sa_er
    9 DeliveryChecklist  → sa_ui
  附加: sa_validate_log

  用法 (SSMS / Azure Data Studio / sqlcmd):
    -- 三元组过滤（对齐 20260705 三元组 + 20260707 sa_* pipeline_id）
    DECLARE @TenantId NVARCHAR(50) = NULL;   -- NULL = 不限租户
    DECLARE @ProjectId BIGINT = NULL;        -- 逻辑项目 ID（ai_projects）
    DECLARE @PipelineId BIGINT = 309;      -- 流水线实例 ID（BASE_AI_PIPELINE.F_ID）

  查全库汇总: 保持 @ProjectId / @PipelineId = NULL
*/

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;

DECLARE @ProjectId BIGINT = NULL;   -- 逻辑项目 ID；NULL = 全库
DECLARE @PipelineId BIGINT = NULL;  -- 流水线实例 ID（Studio pipelineId）；NULL = 不限
DECLARE @TenantId  NVARCHAR(50) = NULL;

PRINT N'========== 1. 九表是否存在 ==========';
SELECT
    t.expected_step     AS [SA步骤],
    t.table_name        AS [表名],
    CASE WHEN s.name IS NOT NULL THEN N'存在' ELSE N'缺失' END AS [表状态]
FROM (VALUES
    (N'1-DomainModel',       N'sa_scope'),
    (N'2-AggregateDesign',    N'sa_dfd'),
    (N'3-EventCatalog',       N'sa_business_process'),
    (N'4-CommandQuery',       N'sa_data_dictionary'),
    (N'5-IntegrationPoints',  N'sa_pspec'),
    (N'6-WorkflowSpec',       N'sa_decision_table'),
    (N'7-UISpec',             N'sa_state_machine'),
    (N'8-DataModel',          N'sa_er'),
    (N'9-DeliveryChecklist',  N'sa_ui'),
    (N'校验日志',             N'sa_validate_log')
) AS t(expected_step, table_name)
LEFT JOIN sys.tables s ON s.name = t.table_name
ORDER BY t.expected_step;

PRINT N'';
PRINT N'========== 2. 各表行数（按 project） ==========';
IF OBJECT_ID(N'dbo.sa_scope', N'U') IS NULL
BEGIN
    PRINT N'[WARN] sa_scope 不存在，请先执行 20260706_SA_NineTables.sql';
END
ELSE
BEGIN
    SELECT N'sa_scope' AS table_name, COUNT(*) AS row_count
    FROM dbo.sa_scope
    WHERE (@ProjectId IS NULL OR project_id = @ProjectId) AND (@PipelineId IS NULL OR pipeline_id = @PipelineId)
      AND (@TenantId IS NULL OR tenant_id = @TenantId)
    UNION ALL
    SELECT N'sa_dfd', COUNT(*)
    FROM dbo.sa_dfd
    WHERE (@ProjectId IS NULL OR project_id = @ProjectId) AND (@PipelineId IS NULL OR pipeline_id = @PipelineId)
      AND (@TenantId IS NULL OR tenant_id = @TenantId)
    UNION ALL
    SELECT N'sa_business_process', COUNT(*)
    FROM dbo.sa_business_process
    WHERE (@ProjectId IS NULL OR project_id = @ProjectId) AND (@PipelineId IS NULL OR pipeline_id = @PipelineId)
      AND (@TenantId IS NULL OR tenant_id = @TenantId)
    UNION ALL
    SELECT N'sa_data_dictionary', COUNT(*)
    FROM dbo.sa_data_dictionary
    WHERE (@ProjectId IS NULL OR project_id = @ProjectId) AND (@PipelineId IS NULL OR pipeline_id = @PipelineId)
      AND (@TenantId IS NULL OR tenant_id = @TenantId)
    UNION ALL
    SELECT N'sa_pspec', COUNT(*)
    FROM dbo.sa_pspec
    WHERE (@ProjectId IS NULL OR project_id = @ProjectId) AND (@PipelineId IS NULL OR pipeline_id = @PipelineId)
      AND (@TenantId IS NULL OR tenant_id = @TenantId)
    UNION ALL
    SELECT N'sa_decision_table', COUNT(*)
    FROM dbo.sa_decision_table
    WHERE (@ProjectId IS NULL OR project_id = @ProjectId) AND (@PipelineId IS NULL OR pipeline_id = @PipelineId)
      AND (@TenantId IS NULL OR tenant_id = @TenantId)
    UNION ALL
    SELECT N'sa_er', COUNT(*)
    FROM dbo.sa_er
    WHERE (@ProjectId IS NULL OR project_id = @ProjectId) AND (@PipelineId IS NULL OR pipeline_id = @PipelineId)
      AND (@TenantId IS NULL OR tenant_id = @TenantId)
    UNION ALL
    SELECT N'sa_state_machine', COUNT(*)
    FROM dbo.sa_state_machine
    WHERE (@ProjectId IS NULL OR project_id = @ProjectId) AND (@PipelineId IS NULL OR pipeline_id = @PipelineId)
      AND (@TenantId IS NULL OR tenant_id = @TenantId)
    UNION ALL
    SELECT N'sa_ui', COUNT(*)
    FROM dbo.sa_ui
    WHERE (@ProjectId IS NULL OR project_id = @ProjectId) AND (@PipelineId IS NULL OR pipeline_id = @PipelineId)
      AND (@TenantId IS NULL OR tenant_id = @TenantId)
    UNION ALL
    SELECT N'sa_validate_log', COUNT(*)
    FROM dbo.sa_validate_log
    WHERE (@ProjectId IS NULL OR project_id = @ProjectId) AND (@PipelineId IS NULL OR pipeline_id = @PipelineId)
      AND (@TenantId IS NULL OR tenant_id = @TenantId)
    ORDER BY table_name;
END

PRINT N'';
PRINT N'========== 3. 单项目九步链是否齐全（期望: scope≥1, dfd/bpm/dict/er/std 各≥1, ui≥事件数） ==========';
IF OBJECT_ID(N'dbo.sa_scope', N'U') IS NOT NULL
BEGIN
    ;WITH proj AS (
        SELECT DISTINCT tenant_id, project_id
        FROM dbo.sa_scope
        WHERE (@ProjectId IS NULL OR project_id = @ProjectId) AND (@PipelineId IS NULL OR pipeline_id = @PipelineId)
          AND (@TenantId IS NULL OR tenant_id = @TenantId)
          AND is_deleted = 0
    )
    SELECT
        p.project_id,
        p.tenant_id,
        (SELECT COUNT(*) FROM dbo.sa_scope s WHERE s.project_id = p.project_id AND s.tenant_id = p.tenant_id AND s.is_deleted = 0) AS cnt_scope,
        (SELECT COUNT(*) FROM dbo.sa_dfd d WHERE d.project_id = p.project_id AND d.tenant_id = p.tenant_id) AS cnt_dfd,
        (SELECT COUNT(*) FROM dbo.sa_business_process b WHERE b.project_id = p.project_id AND b.tenant_id = p.tenant_id) AS cnt_bpm,
        (SELECT COUNT(*) FROM dbo.sa_data_dictionary d WHERE d.project_id = p.project_id AND d.tenant_id = p.tenant_id) AS cnt_dict,
        (SELECT COUNT(*) FROM dbo.sa_er e WHERE e.project_id = p.project_id AND e.tenant_id = p.tenant_id) AS cnt_er,
        (SELECT COUNT(*) FROM dbo.sa_state_machine m WHERE m.project_id = p.project_id AND m.tenant_id = p.tenant_id) AS cnt_std,
        (SELECT COUNT(*) FROM dbo.sa_pspec ps WHERE ps.project_id = p.project_id AND ps.tenant_id = p.tenant_id) AS cnt_pspec,
        (SELECT COUNT(*) FROM dbo.sa_decision_table dt WHERE dt.project_id = p.project_id AND dt.tenant_id = p.tenant_id) AS cnt_dt,
        (SELECT COUNT(*) FROM dbo.sa_ui u WHERE u.project_id = p.project_id AND u.tenant_id = p.tenant_id) AS cnt_ui,
        CASE
            WHEN (SELECT COUNT(*) FROM dbo.sa_scope s WHERE s.project_id = p.project_id AND s.tenant_id = p.tenant_id AND s.is_deleted = 0) >= 1
             AND (SELECT COUNT(*) FROM dbo.sa_dfd d WHERE d.project_id = p.project_id) >= 1
             AND (SELECT COUNT(*) FROM dbo.sa_business_process b WHERE b.project_id = p.project_id) >= 1
             AND (SELECT COUNT(*) FROM dbo.sa_data_dictionary d WHERE d.project_id = p.project_id) >= 1
             AND (SELECT COUNT(*) FROM dbo.sa_er e WHERE e.project_id = p.project_id) >= 1
            THEN N'PROJECT级齐全'
            ELSE N'PROJECT级缺失'
        END AS project_chain_status
    FROM proj p
    ORDER BY p.project_id DESC;
END

PRINT N'';
PRINT N'========== 4. 外键链断裂检测 ==========';
IF OBJECT_ID(N'dbo.sa_dfd', N'U') IS NOT NULL
BEGIN
    SELECT N'sa_dfd → sa_scope 孤儿' AS issue, d.id, d.project_id, d.scope_id
    FROM dbo.sa_dfd d
    LEFT JOIN dbo.sa_scope s ON s.id = d.scope_id
    WHERE s.id IS NULL
      AND (@ProjectId IS NULL OR d.project_id = @ProjectId)

    UNION ALL
    SELECT N'sa_business_process → sa_dfd 孤儿', b.id, b.project_id, b.dfd_id
    FROM dbo.sa_business_process b
    LEFT JOIN dbo.sa_dfd d ON d.id = b.dfd_id
    WHERE d.id IS NULL
      AND (@ProjectId IS NULL OR b.project_id = @ProjectId)

    UNION ALL
    SELECT N'sa_data_dictionary → sa_dfd 孤儿', dd.id, dd.project_id, dd.dfd_id
    FROM dbo.sa_data_dictionary dd
    LEFT JOIN dbo.sa_dfd d ON d.id = dd.dfd_id
    WHERE d.id IS NULL
      AND (@ProjectId IS NULL OR dd.project_id = @ProjectId)

    UNION ALL
    SELECT N'sa_er → sa_data_dictionary 孤儿', e.id, e.project_id, e.dict_id
    FROM dbo.sa_er e
    LEFT JOIN dbo.sa_data_dictionary dd ON dd.id = e.dict_id
    WHERE dd.id IS NULL
      AND (@ProjectId IS NULL OR e.project_id = @ProjectId);
END

PRINT N'';
PRINT N'========== 5. sa_scope 字段合理性 ==========';
IF OBJECT_ID(N'dbo.sa_scope', N'U') IS NOT NULL
BEGIN
    SELECT
        id,
        tenant_id,
        project_id,
        event_count,
        validation_status,
        created_at,
        created_by,
        CASE WHEN ISJSON(system_boundary) = 1 THEN N'OK' ELSE N'INVALID_JSON' END AS system_boundary_json,
        CASE WHEN ISJSON(business_events) = 1 THEN N'OK' ELSE N'INVALID_JSON' END AS business_events_json,
        CASE
            WHEN event_count > 0
             AND ISJSON(business_events) = 1
             AND event_count = (SELECT COUNT(*) FROM OPENJSON(business_events))
            THEN N'OK'
            WHEN event_count = 0 THEN N'WARN: event_count=0'
            ELSE N'MISMATCH: event_count vs JSON数组长度'
        END AS event_count_check,
        LEFT(system_boundary, 120) AS system_boundary_preview,
        LEFT(business_events, 200) AS business_events_preview
    FROM dbo.sa_scope
    WHERE is_deleted = 0
      AND (@ProjectId IS NULL OR project_id = @ProjectId) AND (@PipelineId IS NULL OR pipeline_id = @PipelineId)
      AND (@TenantId IS NULL OR tenant_id = @TenantId)
    ORDER BY created_at DESC;
END

PRINT N'';
PRINT N'========== 6. payload_json 表 — JSON 合法性与空载荷 ==========';
IF OBJECT_ID(N'dbo.sa_dfd', N'U') IS NOT NULL
BEGIN
    SELECT N'sa_dfd' AS table_name, id, project_id, scope_id, validation_status,
        CASE WHEN ISJSON(payload_json) = 1 THEN N'OK' ELSE N'INVALID_JSON' END AS json_check,
        CASE WHEN LEN(LTRIM(RTRIM(payload_json))) <= 2 THEN N'EMPTY' ELSE N'HAS_DATA' END AS payload_size,
        LEFT(payload_json, 150) AS payload_preview
    FROM dbo.sa_dfd
    WHERE (@ProjectId IS NULL OR project_id = @ProjectId) AND (@PipelineId IS NULL OR pipeline_id = @PipelineId)

    UNION ALL
    SELECT N'sa_business_process', id, project_id, dfd_id, validation_status,
        CASE WHEN ISJSON(payload_json) = 1 THEN N'OK' ELSE N'INVALID_JSON' END,
        CASE WHEN LEN(LTRIM(RTRIM(payload_json))) <= 2 THEN N'EMPTY' ELSE N'HAS_DATA' END,
        LEFT(payload_json, 150)
    FROM dbo.sa_business_process
    WHERE (@ProjectId IS NULL OR project_id = @ProjectId) AND (@PipelineId IS NULL OR pipeline_id = @PipelineId)

    UNION ALL
    SELECT N'sa_data_dictionary', id, project_id, dfd_id, validation_status,
        CASE WHEN ISJSON(payload_json) = 1 THEN N'OK' ELSE N'INVALID_JSON' END,
        CASE WHEN LEN(LTRIM(RTRIM(payload_json))) <= 2 THEN N'EMPTY' ELSE N'HAS_DATA' END,
        LEFT(payload_json, 150)
    FROM dbo.sa_data_dictionary
    WHERE (@ProjectId IS NULL OR project_id = @ProjectId) AND (@PipelineId IS NULL OR pipeline_id = @PipelineId)

    UNION ALL
    SELECT N'sa_er', id, project_id, dict_id, validation_status,
        CASE WHEN ISJSON(payload_json) = 1 THEN N'OK' ELSE N'INVALID_JSON' END,
        CASE WHEN LEN(LTRIM(RTRIM(payload_json))) <= 2 THEN N'EMPTY' ELSE N'HAS_DATA' END,
        LEFT(payload_json, 150)
    FROM dbo.sa_er
    WHERE (@ProjectId IS NULL OR project_id = @ProjectId) AND (@PipelineId IS NULL OR pipeline_id = @PipelineId)

    UNION ALL
    SELECT N'sa_state_machine', id, project_id, dict_id, validation_status,
        CASE WHEN ISJSON(payload_json) = 1 THEN N'OK' ELSE N'INVALID_JSON' END,
        CASE WHEN LEN(LTRIM(RTRIM(payload_json))) <= 2 THEN N'EMPTY' ELSE N'HAS_DATA' END,
        LEFT(payload_json, 150)
    FROM dbo.sa_state_machine
    WHERE (@ProjectId IS NULL OR project_id = @ProjectId) AND (@PipelineId IS NULL OR pipeline_id = @PipelineId)

    UNION ALL
    SELECT N'sa_pspec', id, project_id, dict_id, validation_status,
        CASE WHEN ISJSON(payload_json) = 1 THEN N'OK' ELSE N'INVALID_JSON' END,
        CASE WHEN LEN(LTRIM(RTRIM(payload_json))) <= 2 THEN N'EMPTY' ELSE N'HAS_DATA' END,
        LEFT(payload_json, 150)
    FROM dbo.sa_pspec
    WHERE (@ProjectId IS NULL OR project_id = @ProjectId) AND (@PipelineId IS NULL OR pipeline_id = @PipelineId)

    UNION ALL
    SELECT N'sa_decision_table', id, project_id, dict_id, validation_status,
        CASE WHEN ISJSON(payload_json) = 1 THEN N'OK' ELSE N'INVALID_JSON' END,
        CASE WHEN LEN(LTRIM(RTRIM(payload_json))) <= 2 THEN N'EMPTY' ELSE N'HAS_DATA' END,
        LEFT(payload_json, 150)
    FROM dbo.sa_decision_table
    WHERE (@ProjectId IS NULL OR project_id = @ProjectId) AND (@PipelineId IS NULL OR pipeline_id = @PipelineId)

    UNION ALL
    SELECT N'sa_ui', id, project_id, dict_id, validation_status,
        CASE WHEN ISJSON(payload_json) = 1 THEN N'OK' ELSE N'INVALID_JSON' END,
        CASE WHEN LEN(LTRIM(RTRIM(payload_json))) <= 2 THEN N'EMPTY' ELSE N'HAS_DATA' END,
        LEFT(payload_json, 150)
    FROM dbo.sa_ui
    WHERE (@ProjectId IS NULL OR project_id = @ProjectId) AND (@PipelineId IS NULL OR pipeline_id = @PipelineId)

    ORDER BY table_name, id;
END

PRINT N'';
PRINT N'========== 7. 数据字典 elements 数量（CommandQuery 是否合理） ==========';
IF OBJECT_ID(N'dbo.sa_data_dictionary', N'U') IS NOT NULL
BEGIN
    SELECT
        id,
        project_id,
        tenant_id,
        validation_status,
        CASE WHEN ISJSON(payload_json) = 1 THEN
            (SELECT COUNT(*) FROM OPENJSON(payload_json, '$.elements'))
        ELSE NULL END AS element_count,
        CASE WHEN ISJSON(payload_json) = 1 THEN
            JSON_VALUE(payload_json, '$.source')
        ELSE NULL END AS compiler_source,
        LEFT(payload_json, 300) AS payload_head
    FROM dbo.sa_data_dictionary
    WHERE (@ProjectId IS NULL OR project_id = @ProjectId) AND (@PipelineId IS NULL OR pipeline_id = @PipelineId)
    ORDER BY id DESC;
END

PRINT N'';
PRINT N'========== 8. UI screens 数量（DeliveryChecklist 是否合理） ==========';
IF OBJECT_ID(N'dbo.sa_ui', N'U') IS NOT NULL
BEGIN
    SELECT
        id,
        project_id,
        event_id,
        validation_status,
        CASE WHEN ISJSON(payload_json) = 1 THEN
            (SELECT COUNT(*) FROM OPENJSON(payload_json, '$.screens'))
        ELSE NULL END AS screen_count,
        CASE WHEN ISJSON(payload_json) = 1 THEN
            (SELECT COUNT(*) FROM OPENJSON(payload_json, '$.screens[0].fields'))
        ELSE NULL END AS fields_in_first_screen,
        LEFT(payload_json, 300) AS payload_head
    FROM dbo.sa_ui
    WHERE (@ProjectId IS NULL OR project_id = @ProjectId) AND (@PipelineId IS NULL OR pipeline_id = @PipelineId)
    ORDER BY id DESC;
END

PRINT N'';
PRINT N'========== 9. 最近写入的 project 列表（便于选 @ProjectId） ==========';
IF OBJECT_ID(N'dbo.sa_scope', N'U') IS NOT NULL
BEGIN
    SELECT TOP 20
        project_id,
        tenant_id,
        event_count,
        validation_status,
        created_at,
        created_by
    FROM dbo.sa_scope
    WHERE is_deleted = 0
    ORDER BY created_at DESC;
END
ELSE
BEGIN
    PRINT N'sa_scope 不存在，无 project 列表。';
END

PRINT N'';
PRINT N'========== 10. 快速结论（单 project） ==========';
IF @ProjectId IS NOT NULL AND OBJECT_ID(N'dbo.sa_scope', N'U') IS NOT NULL
BEGIN
    DECLARE @scope INT = (SELECT COUNT(*) FROM dbo.sa_scope WHERE project_id = @ProjectId AND is_deleted = 0);
    DECLARE @dfd INT = (SELECT COUNT(*) FROM dbo.sa_dfd WHERE project_id = @ProjectId);
    DECLARE @bpm INT = (SELECT COUNT(*) FROM dbo.sa_business_process WHERE project_id = @ProjectId);
    DECLARE @dict INT = (SELECT COUNT(*) FROM dbo.sa_data_dictionary WHERE project_id = @ProjectId);
    DECLARE @er INT = (SELECT COUNT(*) FROM dbo.sa_er WHERE project_id = @ProjectId);
    DECLARE @std INT = (SELECT COUNT(*) FROM dbo.sa_state_machine WHERE project_id = @ProjectId);
    DECLARE @pspec INT = (SELECT COUNT(*) FROM dbo.sa_pspec WHERE project_id = @ProjectId);
    DECLARE @dt INT = (SELECT COUNT(*) FROM dbo.sa_decision_table WHERE project_id = @ProjectId);
    DECLARE @ui INT = (SELECT COUNT(*) FROM dbo.sa_ui WHERE project_id = @ProjectId);

    SELECT
        @ProjectId AS project_id,
        @scope AS sa_scope,
        @dfd AS sa_dfd,
        @bpm AS sa_business_process,
        @dict AS sa_data_dictionary,
        @er AS sa_er,
        @std AS sa_state_machine,
        @pspec AS sa_pspec,
        @dt AS sa_decision_table,
        @ui AS sa_ui,
        CASE
            WHEN @scope = 0 THEN N'无 SA 数据（可能未跑 runSA 或未执行 DDL）'
            WHEN @dfd >= 1 AND @bpm >= 1 AND @dict >= 1 AND @er >= 1 THEN N'PROJECT 级九步链已有数据'
            ELSE N'有 scope 但 PROJECT 级链不完整'
        END AS summary;
END
ELSE IF @ProjectId IS NULL
BEGIN
    PRINT N'提示: 设置 DECLARE @ProjectId BIGINT = 309; 可查看单 pipeline 快速结论（第 10 节）。';
END

GO
