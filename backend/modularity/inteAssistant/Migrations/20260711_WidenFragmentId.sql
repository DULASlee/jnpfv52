-- Widen F_FragmentId for long fragment keys (req-amend:{guid}, clarification:...)
IF COL_LENGTH('ai_ir_events', 'F_FragmentId') IS NOT NULL
BEGIN
    ALTER TABLE [dbo].[ai_ir_events] ALTER COLUMN [F_FragmentId] NVARCHAR(200) NULL;
END
GO

IF COL_LENGTH('ai_ir_fragment_snapshots', 'F_FragmentId') IS NOT NULL
BEGIN
    ALTER TABLE [dbo].[ai_ir_fragment_snapshots] ALTER COLUMN [F_FragmentId] NVARCHAR(200) NOT NULL;
END
GO
