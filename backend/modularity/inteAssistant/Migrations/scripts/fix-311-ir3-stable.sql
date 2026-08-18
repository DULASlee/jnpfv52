-- 临时修复 pipeline 311 IR3_GeneratedCode 状态投影不一致
-- 背景：phase4-green-path.mjs 多轮重跑导致 CodeGenerated(draft) 事件在
--       CodeGeneratedStablePromoted(stable) 事件之后写入，投影把片段降级回 draft。
-- 实际 3 次 StablePromoted 都已发生（证据：events-311-raw.json），
--       但被后续 CodeGenerated 覆盖。
-- 修复策略：把 IR3_GeneratedCode 状态强制对齐到 stable（与 IR3_TestSuite 一致）。
-- 待办：在 IrProjectionEngine.RebuildAsync 中识别乱序 StablePromoted（见 22 号文档 §13）

DECLARE @PipelineId INT = 311;
DECLARE @ProjectId NVARCHAR(64) = CAST(@PipelineId AS NVARCHAR(64));
DECLARE @FragmentId NVARCHAR(128) = N'codegen:' + CAST(@PipelineId AS NVARCHAR(64));

UPDATE ai_ir_fragment_snapshots
SET F_StabilityState = N'stable',
    F_CurrentVersion = 3,
    F_UpdatedAt = SYSUTCDATETIME()
WHERE F_ProjectId = @ProjectId
  AND F_FragmentId = @FragmentId
  AND F_FragmentType = N'IR3_GeneratedCode'
  AND F_DeleteMark = 0;

SELECT F_FragmentId, F_StabilityState, F_CurrentVersion, F_UpdatedAt
FROM ai_ir_fragment_snapshots
WHERE F_ProjectId = @ProjectId
  AND F_FragmentId = @FragmentId;
