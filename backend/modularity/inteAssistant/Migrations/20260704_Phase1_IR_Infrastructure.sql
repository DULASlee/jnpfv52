-- ========================================
-- 阶段一 IR 基础设施（P1-B01）
-- ai_ir_events / ai_ir_fragment_snapshots / ai_projects / ai_route_table
-- 日期：2026-07-04
-- ========================================

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name = 'ai_ir_events' AND xtype = 'U')
BEGIN
    CREATE TABLE [dbo].[ai_ir_events] (
        [F_Id]              NVARCHAR(50)    NOT NULL,
        [F_ProjectId]       NVARCHAR(50)    NOT NULL,
        [F_TenantId]        NVARCHAR(50)    NOT NULL,
        [F_EventType]       NVARCHAR(100)   NOT NULL,
        [F_FragmentType]    NVARCHAR(50)    NULL,
        [F_FragmentId]      NVARCHAR(50)    NULL,
        [F_FragmentVersion] INT             NOT NULL DEFAULT 1,
        [F_Payload]         NVARCHAR(MAX)   NOT NULL,
        [F_SkillId]         NVARCHAR(100)   NULL,
        [F_SAStepName]      NVARCHAR(50)    NULL,
        [F_Sequence]        BIGINT          IDENTITY(1,1) NOT NULL,
        [F_CreatedAt]       DATETIME2(7)    NOT NULL DEFAULT GETUTCDATE(),
        [F_IsRollback]      BIT             NOT NULL DEFAULT 0,
        CONSTRAINT [PK_ai_ir_events] PRIMARY KEY ([F_Id])
    );

    CREATE INDEX [IX_ir_events_project]
        ON [dbo].[ai_ir_events] ([F_TenantId], [F_ProjectId], [F_Sequence])
        INCLUDE ([F_EventType], [F_FragmentId], [F_CreatedAt]);
END;

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name = 'ai_ir_fragment_snapshots' AND xtype = 'U')
BEGIN
    CREATE TABLE [dbo].[ai_ir_fragment_snapshots] (
        [F_Id]               NVARCHAR(50)    NOT NULL,
        [F_ProjectId]        NVARCHAR(50)    NOT NULL,
        [F_TenantId]         NVARCHAR(50)    NOT NULL,
        [F_FragmentId]       NVARCHAR(50)    NOT NULL,
        [F_FragmentType]     NVARCHAR(50)    NOT NULL,
        [F_CurrentVersion]   INT             NOT NULL,
        [F_StabilityState]   NVARCHAR(20)    NOT NULL DEFAULT 'draft',
        [F_IrContent]        NVARCHAR(MAX)   NOT NULL,
        [F_SAStepsCompleted] NVARCHAR(500)   NULL,
        [F_LastEventId]      NVARCHAR(50)    NOT NULL,
        [F_UpdatedAt]        DATETIME2(7)    NOT NULL DEFAULT GETUTCDATE(),
        [F_DeleteMark]       BIT             NOT NULL DEFAULT 0,
        CONSTRAINT [PK_ai_ir_fragment_snapshots] PRIMARY KEY ([F_Id]),
        CONSTRAINT [UQ_fragment_current] UNIQUE ([F_ProjectId], [F_FragmentId])
    );
END;

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name = 'ai_projects' AND xtype = 'U')
BEGIN
    CREATE TABLE [dbo].[ai_projects] (
        [F_Id]              NVARCHAR(50)    NOT NULL,
        [F_TenantId]        NVARCHAR(50)    NOT NULL,
        [F_ProjectName]     NVARCHAR(200)   NOT NULL,
        [F_Status]          NVARCHAR(50)    NOT NULL DEFAULT 'requirements',
        [F_CurrentPhase]    NVARCHAR(50)    NOT NULL DEFAULT 'pm-skill',
        [F_SandboxId]       NVARCHAR(100)   NULL,
        [F_SkeletonId]      NVARCHAR(50)    NULL,
        [F_TokenConsumed]   BIGINT          NOT NULL DEFAULT 0,
        [F_TokenBudget]     BIGINT          NOT NULL DEFAULT 500000,
        [F_CreatorUserId]   NVARCHAR(50)    NOT NULL,
        [F_CreatedAt]       DATETIME2(7)    NOT NULL DEFAULT GETUTCDATE(),
        [F_LastModifyTime]  DATETIME2(7)    NULL,
        [F_DeleteMark]      BIT             NOT NULL DEFAULT 0,
        CONSTRAINT [PK_ai_projects] PRIMARY KEY ([F_Id])
    );
END;

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name = 'ai_route_table' AND xtype = 'U')
BEGIN
    CREATE TABLE [dbo].[ai_route_table] (
        [F_Id]              NVARCHAR(50)    NOT NULL,
        [F_TenantId]        NVARCHAR(50)    NOT NULL,
        [F_ProjectId]       NVARCHAR(50)    NOT NULL,
        [F_SandboxId]       NVARCHAR(100)   NOT NULL,
        [F_SandboxType]     NVARCHAR(20)    NOT NULL DEFAULT 'shared',
        [F_SandboxStatus]   NVARCHAR(20)    NOT NULL DEFAULT 'creating',
        [F_SandboxEndpoint] NVARCHAR(200)   NULL,
        [F_EtcdKey]         NVARCHAR(500)   NOT NULL,
        [F_CreatedAt]       DATETIME2(7)    NOT NULL DEFAULT GETUTCDATE(),
        [F_LastHeartbeat]   DATETIME2(7)    NULL,
        CONSTRAINT [PK_ai_route_table] PRIMARY KEY ([F_Id]),
        CONSTRAINT [UQ_route_project] UNIQUE ([F_TenantId], [F_ProjectId])
    );
END;
