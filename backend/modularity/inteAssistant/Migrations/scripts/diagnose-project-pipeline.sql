-- 检查 pipeline 311 周边的 projectId 分布
SET NOCOUNT ON;

PRINT '--- pipeline 311 详情 ---';
SELECT F_Id, F_TenantId, F_Project_Id, F_Name, F_Current_Stage, F_Work_Mode, F_Source_Pipeline_Id, F_Frozen, F_CreatedTime
FROM BASE_AI_PIPELINE
WHERE F_Id = '311';

PRINT '';
PRINT '--- 同项目下的所有 pipeline（按 projectId 分组）---';
SELECT F_Project_Id, COUNT(*) AS pipeline_count, 
       STRING_AGG(F_Id, ',') AS pipeline_ids
FROM BASE_AI_PIPELINE
WHERE F_DeleteMark = 0
GROUP BY F_Project_Id
HAVING COUNT(*) > 1
ORDER BY pipeline_count DESC;

PRINT '';
PRINT '--- 同租户下最近 20 条 pipeline（看 projectId 是否独立）---';
SELECT TOP 20 F_Id, F_TenantId, F_Project_Id, F_Work_Mode, F_Source_Pipeline_Id, F_Current_Stage, F_Frozen
FROM BASE_AI_PIPELINE
WHERE F_DeleteMark = 0
ORDER BY CAST(F_Id AS BIGINT) DESC;

PRINT '';
PRINT '--- ai_projects 中 projectId 与 pipelineId 是否真的一一对应 ---';
SELECT F_Id AS project_id, F_TenantId, F_ProjectName, F_Status,
       (SELECT COUNT(*) FROM BASE_AI_PIPELINE p WHERE p.F_Project_Id = ai_projects.F_Id AND p.F_DeleteMark = 0) AS pipeline_count
FROM ai_projects
WHERE F_DeleteMark = 0
ORDER BY CAST(F_Id AS BIGINT) DESC;
