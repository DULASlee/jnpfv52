-- ============================================================
-- JNPF v5.2 一键初始化脚本 (SQL Server)
-- 文件: DB/jnpf_v52_complete_init.sql
-- 日期: 2026-06-20
-- 用途: 创建数据库 + 全部表结构 + 种子数据，零手工操作
-- 适配: SQL Server 2016+ (Express/Standard/Enterprise)
-- 包含: ZXAF_V1_DevTest1 (128表) + jnpf_sundial (4表) = 132 张表
-- ============================================================
-- 用法:
--   sqlcmd -S localhost -U sa -P YourPassword -i jnpf_v52_complete_init.sql
--   或在 SSMS 中打开此文件，按 F5 执行
-- ============================================================

-- ============================================================
-- 第 0 章: 数据库创建
-- ============================================================
USE [master];
GO

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'ZXAF_V1_DevTest1')
BEGIN
    CREATE DATABASE [ZXAF_V1_DevTest1]
     CONTAINMENT = NONE
     ON PRIMARY
    ( NAME = N'ZXAF_V1_DevTest1', FILENAME = N'C:\Program Files\Microsoft SQL Server\MSSQL16.SQLEXPRESS\MSSQL\DATA\ZXAF_V1_DevTest1.mdf', SIZE = 512MB, MAXSIZE = UNLIMITED, FILEGROWTH = 64MB )
     LOG ON
    ( NAME = N'ZXAF_V1_DevTest1_log', FILENAME = N'C:\Program Files\Microsoft SQL Server\MSSQL16.SQLEXPRESS\MSSQL\DATA\ZXAF_V1_DevTest1_log.ldf', SIZE = 128MB, MAXSIZE = 2048GB, FILEGROWTH = 64MB );
END
GO

ALTER DATABASE [ZXAF_V1_DevTest1] SET COMPATIBILITY_LEVEL = 130;
ALTER DATABASE [ZXAF_V1_DevTest1] SET RECOVERY SIMPLE;
ALTER DATABASE [ZXAF_V1_DevTest1] SET MULTI_USER;
GO

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'jnpf_sundial')
BEGIN
    CREATE DATABASE [jnpf_sundial]
     CONTAINMENT = NONE
     ON PRIMARY
    ( NAME = N'jnpf_sundial', FILENAME = N'C:\Program Files\Microsoft SQL Server\MSSQL16.SQLEXPRESS\MSSQL\DATA\jnpf_sundial.mdf', SIZE = 128MB, MAXSIZE = UNLIMITED, FILEGROWTH = 32MB )
     LOG ON
    ( NAME = N'jnpf_sundial_log', FILENAME = N'C:\Program Files\Microsoft SQL Server\MSSQL16.SQLEXPRESS\MSSQL\DATA\jnpf_sundial_log.ldf', SIZE = 32MB, MAXSIZE = 2048GB, FILEGROWTH = 16MB );
END
GO

ALTER DATABASE [jnpf_sundial] SET COMPATIBILITY_LEVEL = 130;
ALTER DATABASE [jnpf_sundial] SET RECOVERY SIMPLE;
GO

USE [ZXAF_V1_DevTest1];
GO

-- ============================================================
-- 第 1 章: 全部数据表结构 (从 ZXAPINIT.SQL 提取, 128 张核心表)
-- ============================================================

-- 1. base_advanced_query_scheme
IF OBJECT_ID('dbo.base_advanced_query_scheme', 'U') IS NULL
CREATE TABLE [dbo].[base_advanced_query_scheme](
	[f_id] [nvarchar](50) NOT NULL,
	[f_full_name] [nvarchar](200) NULL,
	[f_match_logic] [nvarchar](20) NULL,
	[f_condition_json] [nvarchar](max) NULL,
	[f_module_id] [nvarchar](50) NULL,
	[f_creator_time] [datetime] NULL,
	[f_creator_user_id] [nvarchar](50) NULL,
	[f_last_modify_time] [datetime] NULL,
	[f_last_modify_user_id] [nvarchar](50) NULL,
	[f_delete_time] [datetime] NULL,
	[f_delete_user_id] [nvarchar](50) NULL,
	[f_delete_mark] [int] NULL,
	[f_sort_code] [bigint] NULL,
	[f_tenant_id] [nvarchar](50) NULL,
	[f_inte_assistant] [int] NULL,
	[f_zx_system_id] [nvarchar](50) NULL,
	[f_flow_task_id] [nvarchar](50) NULL,
	[f_flow_id] [nvarchar](50) NULL,
 CONSTRAINT [PK__base_adv__2911CBED97CE517E] PRIMARY KEY CLUSTERED 
(
	[f_id] ASC) ON [PRIMARY]
)
GO

-- 2. BASE_AI_AGENT_CONFIG
IF OBJECT_ID('dbo.BASE_AI_AGENT_CONFIG', 'U') IS NULL
CREATE TABLE [dbo].[BASE_AI_AGENT_CONFIG](
	[F_Id] [bigint] NOT NULL,
	[F_AgentCode] [nvarchar](100) NOT NULL,
	[F_Name] [nvarchar](200) NOT NULL,
	[F_Description] [nvarchar](2000) NULL,
	[F_AgentType] [nvarchar](50) NOT NULL,
	[F_PromptTemplateId] [bigint] NULL,
	[F_SystemPrompt] [nvarchar](max) NULL,
	[F_ModelProvider] [nvarchar](50) NULL,
	[F_ModelName] [nvarchar](100) NULL,
	[F_Temperature] [decimal](3, 2) NULL,
	[F_MaxTokens] [int] NULL,
	[F_Config] [nvarchar](max) NULL,
	[F_Enabled] [bit] NOT NULL,
	[F_Sort] [int] NULL,
	[F_CreatorTime] [datetime] NOT NULL,
	[F_CreatorUserId] [bigint] NULL,
	[F_ModifyTime] [datetime] NULL,
	[F_ModifyUserId] [bigint] NULL,
	[F_DeleteMark] [bit] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[F_Id] ASC) ON [PRIMARY],
 CONSTRAINT [UQ_AGENT_CODE] UNIQUE NONCLUSTERED 
(
	[F_AgentCode] ASC) ON [PRIMARY]
)
GO

-- 3. BASE_AI_AGENT_SKILL
IF OBJECT_ID('dbo.BASE_AI_AGENT_SKILL', 'U') IS NULL
CREATE TABLE [dbo].[BASE_AI_AGENT_SKILL](
	[F_Id] [bigint] NOT NULL,
	[F_AgentId] [bigint] NOT NULL,
	[F_SkillCode] [nvarchar](100) NOT NULL,
	[F_Name] [nvarchar](200) NOT NULL,
	[F_Description] [nvarchar](2000) NULL,
	[F_SkillType] [nvarchar](50) NULL,
	[F_Config] [nvarchar](max) NULL,
	[F_Enabled] [bit] NOT NULL,
	[F_CreatorTime] [datetime] NOT NULL,
	[F_CreatorUserId] [bigint] NULL,
	[F_ModifyTime] [datetime] NULL,
	[F_ModifyUserId] [bigint] NULL,
	[F_DeleteMark] [bit] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[F_Id] ASC) ON [PRIMARY]
)
GO

-- 4. BASE_AI_CALL_LOG
IF OBJECT_ID('dbo.BASE_AI_CALL_LOG', 'U') IS NULL
CREATE TABLE [dbo].[BASE_AI_CALL_LOG](
	[F_ID] [nvarchar](50) NOT NULL,
	[F_PROVIDER] [nvarchar](50) NULL,
	[F_MODEL] [nvarchar](100) NULL,
	[F_PROMPT_TOKENS] [int] NULL,
	[F_COMPLETION_TOKENS] [int] NULL,
	[F_LATENCY_MS] [bigint] NULL,
	[F_STATUS_CODE] [int] NULL,
	[F_REQUEST_BODY] [nvarchar](max) NULL,
	[F_RESPONSE_BODY] [nvarchar](max) NULL,
	[F_FALLBACK] [int] NULL,
	[F_ORIGINAL_MODEL] [nvarchar](50) NULL,
	[F_ACTUAL_MODEL] [nvarchar](50) NULL,
	[F_FALLBACK_REASON] [nvarchar](200) NULL,
	[F_TENANT_ID] [nvarchar](50) NOT NULL,
	[F_CREATOR_TIME] [datetime] NULL,
	[F_CREATOR_USER_ID] [nvarchar](50) NULL,
	[F_LAST_MODIFY_TIME] [datetime] NULL,
	[F_LAST_MODIFY_USER_ID] [nvarchar](50) NULL,
	[F_DELETE_MARK] [int] NULL,
	[F_SORT_CODE] [bigint] NULL,
	[F_ENABLED_MARK] [int] NULL,
PRIMARY KEY CLUSTERED 
(
	[F_ID] ASC) ON [PRIMARY]
)
GO

-- 5. BASE_AI_EVAL_CASE
IF OBJECT_ID('dbo.BASE_AI_EVAL_CASE', 'U') IS NULL
CREATE TABLE [dbo].[BASE_AI_EVAL_CASE](
	[F_Id] [bigint] NOT NULL,
	[F_SetId] [bigint] NOT NULL,
	[F_Name] [nvarchar](200) NOT NULL,
	[F_Requirement] [nvarchar](max) NOT NULL,
	[F_ExpectedIR] [nvarchar](max) NULL,
	[F_Stage] [int] NULL,
	[F_ScoreThreshold] [decimal](3, 2) NULL,
	[F_Enabled] [bit] NULL,
	[F_CreatorTime] [datetime] NULL,
	[F_CreatorUserId] [bigint] NULL,
	[F_ModifyTime] [datetime] NULL,
	[F_ModifyUserId] [bigint] NULL,
	[F_DeleteMark] [bit] NULL,
PRIMARY KEY CLUSTERED 
(
	[F_Id] ASC) ON [PRIMARY]
)
GO

-- 6. BASE_AI_EVAL_GOLDEN_SET
IF OBJECT_ID('dbo.BASE_AI_EVAL_GOLDEN_SET', 'U') IS NULL
CREATE TABLE [dbo].[BASE_AI_EVAL_GOLDEN_SET](
	[F_Id] [bigint] NOT NULL,
	[F_Name] [nvarchar](200) NOT NULL,
	[F_Description] [nvarchar](2000) NULL,
	[F_Domain] [nvarchar](100) NULL,
	[F_TestCaseCount] [int] NULL,
	[F_Enabled] [bit] NULL,
	[F_CreatorTime] [datetime] NULL,
	[F_CreatorUserId] [bigint] NULL,
	[F_ModifyTime] [datetime] NULL,
	[F_ModifyUserId] [bigint] NULL,
	[F_DeleteMark] [bit] NULL,
PRIMARY KEY CLUSTERED 
(
	[F_Id] ASC) ON [PRIMARY]
)
GO

-- 7. BASE_AI_EVAL_RUN
IF OBJECT_ID('dbo.BASE_AI_EVAL_RUN', 'U') IS NULL
CREATE TABLE [dbo].[BASE_AI_EVAL_RUN](
	[F_Id] [bigint] NOT NULL,
	[F_SetId] [bigint] NOT NULL,
	[F_RunAt] [datetime] NOT NULL,
	[F_TotalCases] [int] NOT NULL,
	[F_PassedCases] [int] NOT NULL,
	[F_AverageScore] [decimal](5, 4) NULL,
	[F_PassRate] [decimal](5, 4) NULL,
	[F_DurationMs] [bigint] NULL,
	[F_Details] [nvarchar](max) NULL,
	[F_CreatorTime] [datetime] NULL,
	[F_CreatorUserId] [bigint] NULL,
PRIMARY KEY CLUSTERED 
(
	[F_Id] ASC) ON [PRIMARY]
)
GO

-- 8. BASE_AI_GENERATED_PROJECT
IF OBJECT_ID('dbo.BASE_AI_GENERATED_PROJECT', 'U') IS NULL
CREATE TABLE [dbo].[BASE_AI_GENERATED_PROJECT](
	[F_Id] [bigint] NOT NULL,
	[F_TenantId] [nvarchar](50) NOT NULL,
	[F_UserId] [bigint] NOT NULL,
	[F_ProjectName] [nvarchar](200) NOT NULL,
	[F_Description] [nvarchar](max) NULL,
	[F_PipelineStatus] [nvarchar](20) NULL,
	[F_CurrentStage] [int] NULL,
	[F_SandboxUrl] [nvarchar](500) NULL,
	[F_SandboxAccount] [nvarchar](100) NULL,
	[F_SandboxPassword] [nvarchar](100) NULL,
	[F_SourceZipUrl] [nvarchar](500) NULL,
	[F_DeployDocUrl] [nvarchar](500) NULL,
	[F_RequirementIR] [nvarchar](max) NULL,
	[F_ArchitectureIR] [nvarchar](max) NULL,
	[F_DesignIR] [nvarchar](max) NULL,
	[F_FinalIR] [nvarchar](max) NULL,
	[F_IsRead] [bit] NULL,
	[F_UpdateCount] [int] NULL,
	[F_CreatorTime] [datetime] NULL,
	[F_CreatorUserId] [bigint] NULL,
	[F_CreatorUserName] [nvarchar](50) NULL,
	[F_ModifyTime] [datetime] NULL,
	[F_ModifyUserId] [bigint] NULL,
	[F_ModifyUserName] [nvarchar](50) NULL,
	[F_DeleteMark] [bit] NULL,
PRIMARY KEY CLUSTERED 
(
	[F_Id] ASC) ON [PRIMARY]
)
GO

-- 9. BASE_AI_MCP_CONFIG
IF OBJECT_ID('dbo.BASE_AI_MCP_CONFIG', 'U') IS NULL
CREATE TABLE [dbo].[BASE_AI_MCP_CONFIG](
	[F_Id] [bigint] NOT NULL,
	[F_Name] [nvarchar](200) NOT NULL,
	[F_Endpoint] [nvarchar](500) NOT NULL,
	[F_Protocol] [nvarchar](20) NOT NULL,
	[F_AuthType] [nvarchar](20) NULL,
	[F_AuthConfig] [nvarchar](max) NULL,
	[F_Status] [nvarchar](20) NOT NULL,
	[F_LastTestTime] [datetime] NULL,
	[F_LastTestResult] [nvarchar](2000) NULL,
	[F_Enabled] [bit] NOT NULL,
	[F_CreatorTime] [datetime] NOT NULL,
	[F_CreatorUserId] [bigint] NULL,
	[F_ModifyTime] [datetime] NULL,
	[F_ModifyUserId] [bigint] NULL,
	[F_DeleteMark] [bit] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[F_Id] ASC) ON [PRIMARY]
)
GO

-- 10. BASE_AI_MODEL_PROVIDER
IF OBJECT_ID('dbo.BASE_AI_MODEL_PROVIDER', 'U') IS NULL
CREATE TABLE [dbo].[BASE_AI_MODEL_PROVIDER](
	[F_Id] [bigint] NOT NULL,
	[F_ProviderCode] [nvarchar](50) NOT NULL,
	[F_Name] [nvarchar](100) NOT NULL,
	[F_BaseUrl] [nvarchar](500) NOT NULL,
	[F_ApiKey] [nvarchar](500) NOT NULL,
	[F_DefaultModel] [nvarchar](100) NULL,
	[F_MaxTokens] [bigint] NOT NULL,
	[F_Temperature] [decimal](5, 2) NOT NULL,
	[F_Status] [nvarchar](20) NOT NULL,
	[F_Priority] [int] NOT NULL,
	[F_Enabled] [bit] NOT NULL,
	[F_Description] [nvarchar](500) NULL,
	[F_LastTestTime] [datetime] NULL,
	[F_LastTestResult] [nvarchar](2000) NULL,
	[F_CreatorTime] [datetime] NOT NULL,
	[F_CreatorUserId] [bigint] NULL,
	[F_ModifyTime] [datetime] NULL,
	[F_ModifyUserId] [bigint] NULL,
	[F_DeleteMark] [bit] NOT NULL,
	[F_ApiFormat] [nvarchar](20) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[F_Id] ASC) ON [PRIMARY],
 CONSTRAINT [UQ_PROVIDER_CODE] UNIQUE NONCLUSTERED 
(
	[F_ProviderCode] ASC) ON [PRIMARY]
)
GO

-- 11. BASE_AI_MODEL_ROUTING
IF OBJECT_ID('dbo.BASE_AI_MODEL_ROUTING', 'U') IS NULL
CREATE TABLE [dbo].[BASE_AI_MODEL_ROUTING](
	[F_Id] [bigint] NOT NULL,
	[F_Stage] [int] NOT NULL,
	[F_StageName] [nvarchar](50) NULL,
	[F_Provider] [nvarchar](50) NOT NULL,
	[F_Model] [nvarchar](100) NOT NULL,
	[F_Priority] [int] NOT NULL,
	[F_MaxRetries] [int] NOT NULL,
	[F_TimeoutMs] [int] NOT NULL,
	[F_CircuitBreakerThreshold] [int] NULL,
	[F_CircuitBreakerResetMs] [int] NULL,
	[F_Enabled] [bit] NOT NULL,
	[F_CreatorTime] [datetime] NOT NULL,
	[F_CreatorUserId] [bigint] NULL,
	[F_ModifyTime] [datetime] NULL,
	[F_ModifyUserId] [bigint] NULL,
	[F_DeleteMark] [bit] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[F_Id] ASC) ON [PRIMARY]
)
GO

-- 12. BASE_AI_PIPELINE
IF OBJECT_ID('dbo.BASE_AI_PIPELINE', 'U') IS NULL
CREATE TABLE [dbo].[BASE_AI_PIPELINE](
	[F_ID] [nvarchar](50) NOT NULL,
	[F_NAME] [nvarchar](200) NULL,
	[F_CURRENT_STAGE] [nvarchar](50) NULL,
	[F_STATUS] [nvarchar](50) NULL,
	[F_STAGE_STATUS] [int] NULL,
	[F_STARTED_TIME] [datetime] NULL,
	[F_FINISHED_TIME] [datetime] NULL,
	[F_VALIDATION_ID] [nvarchar](50) NULL,
	[F_STALE_FROM_STAGE] [nvarchar](50) NULL,
	[F_REJECT_COUNT] [int] NOT NULL,
	[F_ABANDONED_AT] [datetime] NULL,
	[F_ABANDONED_BY] [nvarchar](50) NULL,
	[F_ABANDON_REASON] [nvarchar](500) NULL,
	[F_STALE_SINCE] [datetime] NULL,
	[F_STALE_AT] [datetime] NULL,
	[F_FAILURE_COUNTS] [nvarchar](max) NULL,
	[F_TENANT_ID] [nvarchar](50) NOT NULL,
	[F_CREATOR_TIME] [datetime] NULL,
	[F_CREATOR_USER_ID] [nvarchar](50) NULL,
	[F_LAST_MODIFY_TIME] [datetime] NULL,
	[F_LAST_MODIFY_USER_ID] [nvarchar](50) NULL,
	[F_DELETE_MARK] [int] NULL,
	[F_SORT_CODE] [bigint] NULL,
	[F_ENABLED_MARK] [int] NULL,
PRIMARY KEY CLUSTERED 
(
	[F_ID] ASC) ON [PRIMARY]
)
GO

-- 13. BASE_AI_PIPELINE_MESSAGE
IF OBJECT_ID('dbo.BASE_AI_PIPELINE_MESSAGE', 'U') IS NULL
CREATE TABLE [dbo].[BASE_AI_PIPELINE_MESSAGE](
	[F_ID] [nvarchar](50) NOT NULL,
	[F_PIPELINE_ID] [nvarchar](50) NULL,
	[F_ROLE] [nvarchar](50) NULL,
	[F_CONTENT] [nvarchar](max) NULL,
	[F_STAGE] [nvarchar](50) NULL,
	[F_SEQUENCE] [int] NULL,
	[F_NOTIFY_TARGETS] [nvarchar](500) NULL,
	[F_TENANT_ID] [nvarchar](50) NOT NULL,
	[F_CREATOR_TIME] [datetime] NULL,
	[F_CREATOR_USER_ID] [nvarchar](50) NULL,
	[F_LAST_MODIFY_TIME] [datetime] NULL,
	[F_LAST_MODIFY_USER_ID] [nvarchar](50) NULL,
	[F_DELETE_MARK] [int] NULL,
	[F_SORT_CODE] [bigint] NULL,
	[F_ENABLED_MARK] [int] NULL,
PRIMARY KEY CLUSTERED 
(
	[F_ID] ASC) ON [PRIMARY]
)
GO

-- 14. BASE_AI_PIPELINE_STAGE_CONFIG
IF OBJECT_ID('dbo.BASE_AI_PIPELINE_STAGE_CONFIG', 'U') IS NULL
CREATE TABLE [dbo].[BASE_AI_PIPELINE_STAGE_CONFIG](
	[F_Id] [bigint] NOT NULL,
	[F_Stage] [int] NOT NULL,
	[F_StageName] [nvarchar](50) NOT NULL,
	[F_Description] [nvarchar](500) NULL,
	[F_AgentCode] [nvarchar](100) NULL,
	[F_PromptTemplateId] [bigint] NULL,
	[F_TimeoutSeconds] [int] NULL,
	[F_RequireConfirm] [bit] NULL,
	[F_AllowRollback] [bit] NULL,
	[F_Enabled] [bit] NULL,
	[F_CreatorTime] [datetime] NULL,
	[F_CreatorUserId] [bigint] NULL,
	[F_ModifyTime] [datetime] NULL,
	[F_ModifyUserId] [bigint] NULL,
	[F_DeleteMark] [bit] NULL,
PRIMARY KEY CLUSTERED 
(
	[F_Id] ASC) ON [PRIMARY],
 CONSTRAINT [UQ_STAGE_CONFIG] UNIQUE NONCLUSTERED 
(
	[F_Stage] ASC) ON [PRIMARY]
)
GO

-- 15. BASE_AI_PROMPT_TEMPLATE
IF OBJECT_ID('dbo.BASE_AI_PROMPT_TEMPLATE', 'U') IS NULL
CREATE TABLE [dbo].[BASE_AI_PROMPT_TEMPLATE](
	[F_Id] [bigint] NOT NULL,
	[F_TenantId] [nvarchar](50) NOT NULL,
	[F_Name] [nvarchar](200) NOT NULL,
	[F_Category] [nvarchar](100) NULL,
	[F_Template] [nvarchar](max) NOT NULL,
	[F_Version] [int] NOT NULL,
	[F_IsActive] [int] NOT NULL,
	[F_CreatorTime] [datetime] NOT NULL,
	[F_CreatorUserId] [bigint] NULL,
	[F_ModifyTime] [datetime] NULL,
	[F_ModifyUserId] [bigint] NULL,
	[F_DeleteMark] [bit] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[F_Id] ASC) ON [PRIMARY]
)
GO

-- 16. BASE_AI_UI_TEMPLATE
IF OBJECT_ID('dbo.BASE_AI_UI_TEMPLATE', 'U') IS NULL
CREATE TABLE [dbo].[BASE_AI_UI_TEMPLATE](
	[F_Id] [bigint] NOT NULL,
	[F_TenantId] [nvarchar](50) NULL,
	[F_Name] [nvarchar](200) NOT NULL,
	[F_Description] [nvarchar](2000) NULL,
	[F_Category] [nvarchar](100) NULL,
	[F_ThumbnailUrl] [nvarchar](500) NULL,
	[F_TemplateData] [nvarchar](max) NOT NULL,
	[F_Source] [nvarchar](20) NULL,
	[F_DesignerId] [bigint] NULL,
	[F_DesignerName] [nvarchar](50) NULL,
	[F_UseCount] [int] NULL,
	[F_Rating] [decimal](3, 2) NULL,
	[F_Enabled] [bit] NULL,
	[F_CreatorTime] [datetime] NULL,
	[F_CreatorUserId] [bigint] NULL,
	[F_ModifyTime] [datetime] NULL,
	[F_ModifyUserId] [bigint] NULL,
	[F_DeleteMark] [bit] NULL,
PRIMARY KEY CLUSTERED 
(
	[F_Id] ASC) ON [PRIMARY]
)
GO

-- 17. base_api_log
IF OBJECT_ID('dbo.base_api_log', 'U') IS NULL
CREATE TABLE [dbo].[base_api_log](
	[f_id] [nvarchar](50) NOT NULL,
	[f_user_id] [nvarchar](50) NULL,
	[f_user_name] [nvarchar](100) NULL,
	[f_type] [int] NULL,
	[f_level] [int] NULL,
	[f_ip_address] [nvarchar](50) NULL,
	[f_ip_address_name] [nvarchar](50) NULL,
	[f_request_url] [nvarchar](500) NULL,
	[f_request_method] [nvarchar](50) NULL,
	[f_request_duration] [int] NULL,
	[f_json] [nvarchar](max) NULL,
	[f_plat_form] [nvarchar](500) NULL,
	[f_creator_user_id] [nvarchar](50) NULL,
	[f_last_modify_time] [datetime] NULL,
	[f_last_modify_user_id] [nvarchar](50) NULL,
	[f_delete_time] [datetime] NULL,
	[f_delete_user_id] [nvarchar](50) NULL,
	[f_delete_mark] [int] NULL,
	[f_module_id] [nvarchar](50) NULL,
	[f_module_name] [nvarchar](50) NULL,
	[f_object_id] [nvarchar](50) NULL,
	[f_description] [nvarchar](500) NULL,
	[f_sort_code] [bigint] NULL,
	[f_creator_time] [datetime] NULL,
	[f_tenant_id] [nvarchar](50) NULL,
	[f_browser] [nvarchar](50) NULL,
	[f_request_param] [nvarchar](max) NULL,
	[f_request_target] [nvarchar](max) NULL,
	[f_login_mark] [int] NULL,
	[f_login_type] [int] NULL,
	[f_zx_system_id] [nvarchar](50) NULL,
	[F_REQUEST_Body_Type] [varchar](255) NULL,
	[F_REQUEST_Body] [text] NULL,
	[F_REQUEST_Headers] [text] NULL,
	[F_REQUEST_Result] [text] NULL,
	[F_Msg] [text] NULL,
	[F_Status] [int] NULL,
	[f_inte_assistant] [int] NULL,
 CONSTRAINT [PK__base_sys__2911CBED3C589CD7_copy1] PRIMARY KEY CLUSTERED 
(
	[f_id] ASC) ON [PRIMARY]
)
GO

-- 18. base_app_data
IF OBJECT_ID('dbo.base_app_data', 'U') IS NULL
CREATE TABLE [dbo].[base_app_data](
	[f_id] [nvarchar](50) NOT NULL,
	[f_object_type] [nvarchar](50) NULL,
	[f_object_id] [nvarchar](50) NULL,
	[f_object_data] [nvarchar](max) NULL,
	[f_description] [nvarchar](500) NULL,
	[f_enabled_mark] [int] NULL,
	[f_system_id] [nvarchar](50) NULL,
	[f_creator_time] [datetime] NULL,
	[f_creator_user_id] [nvarchar](50) NULL,
	[f_last_modify_time] [datetime] NULL,
	[f_last_modify_user_id] [nvarchar](50) NULL,
	[f_delete_time] [datetime] NULL,
	[f_delete_user_id] [nvarchar](50) NULL,
	[f_delete_mark] [int] NULL,
	[f_sort_code] [bigint] NULL,
	[f_tenant_id] [nvarchar](50) NULL,
	[f_zx_system_id] [nvarchar](50) NULL,
	[f_flow_task_id] [nvarchar](50) NULL,
	[f_flow_id] [nvarchar](50) NULL,
	[f_inte_assistant] [int] NULL,
 CONSTRAINT [PK__base_app__2911CBED196C2D15] PRIMARY KEY CLUSTERED 
(
	[f_id] ASC) ON [PRIMARY]
)
GO

-- 19. base_authorize
IF OBJECT_ID('dbo.base_authorize', 'U') IS NULL
CREATE TABLE [dbo].[base_authorize](
	[f_id] [nvarchar](50) NOT NULL,
	[f_item_type] [nvarchar](50) NULL,
	[f_item_id] [nvarchar](50) NULL,
	[f_object_type] [nvarchar](50) NULL,
	[f_object_id] [nvarchar](50) NULL,
	[f_sort_code] [bigint] NULL,
	[f_creator_time] [datetime] NULL,
	[f_creator_user_id] [nvarchar](50) NULL,
	[f_last_modify_time] [datetime] NULL,
	[f_last_modify_user_id] [nvarchar](50) NULL,
	[f_delete_time] [datetime] NULL,
	[f_delete_user_id] [nvarchar](50) NULL,
	[f_delete_mark] [int] NULL,
	[f_tenant_id] [nvarchar](50) NULL,
	[f_zx_system_id] [nvarchar](50) NULL,
	[F_ENABLED_MARK] [int] NULL,
 CONSTRAINT [PK__base_aut__2911CBEDA321B0F2] PRIMARY KEY CLUSTERED 
(
	[f_id] ASC) ON [PRIMARY]
)
GO

-- 20. base_bill_rule
IF OBJECT_ID('dbo.base_bill_rule', 'U') IS NULL
CREATE TABLE [dbo].[base_bill_rule](
	[f_id] [nvarchar](50) NOT NULL,
	[f_full_name] [nvarchar](200) NULL,
	[f_en_code] [nvarchar](200) NULL,
	[f_prefix] [nvarchar](50) NULL,
	[f_date_format] [nvarchar](50) NULL,
	[f_digit] [int] NULL,
	[f_start_number] [nvarchar](50) NULL,
	[f_example] [nvarchar](100) NULL,
	[f_this_number] [int] NULL,
	[f_output_number] [nvarchar](100) NULL,
	[f_description] [nvarchar](500) NULL,
	[f_enabled_mark] [int] NULL,
	[f_category] [nvarchar](50) NULL,
	[f_sort_code] [bigint] NULL,
	[f_creator_time] [datetime] NULL,
	[f_creator_user_id] [nvarchar](50) NULL,
	[f_last_modify_time] [datetime] NULL,
	[f_last_modify_user_id] [nvarchar](50) NULL,
	[f_delete_time] [datetime] NULL,
	[f_delete_user_id] [nvarchar](50) NULL,
	[f_delete_mark] [int] NULL,
	[f_tenant_id] [nvarchar](50) NULL,
	[f_type] [int] NULL,
	[f_random_digit] [int] NULL,
	[f_random_type] [int] NULL,
	[f_suffix] [nvarchar](50) NULL,
	[f_zx_system_id] [nvarchar](50) NULL,
 CONSTRAINT [PK__base_bil__2911CBED0E01B8C9] PRIMARY KEY CLUSTERED 
(
	[f_id] ASC) ON [PRIMARY]
)
GO

-- 21. base_columns_purview
IF OBJECT_ID('dbo.base_columns_purview', 'U') IS NULL
CREATE TABLE [dbo].[base_columns_purview](
	[f_id] [nvarchar](50) NOT NULL,
	[f_field_list] [nvarchar](max) NULL,
	[f_module_id] [nvarchar](50) NULL,
	[f_enabled_mark] [int] NULL,
	[f_sort_code] [bigint] NULL,
	[f_creator_time] [datetime] NULL,
	[f_creator_user_id] [nvarchar](50) NULL,
	[f_last_modify_time] [datetime] NULL,
	[f_last_modify_user_id] [nvarchar](50) NULL,
	[f_delete_time] [datetime] NULL,
	[f_delete_user_id] [nvarchar](50) NULL,
	[f_delete_mark] [int] NULL,
	[f_tenant_id] [nvarchar](50) NULL,
	[f_zx_system_id] [nvarchar](50) NULL,
 CONSTRAINT [PK__base_col__2911CBED38097D3E] PRIMARY KEY CLUSTERED 
(
	[f_id] ASC) ON [PRIMARY]
)
GO

-- 22. base_common_fields
IF OBJECT_ID('dbo.base_common_fields', 'U') IS NULL
CREATE TABLE [dbo].[base_common_fields](
	[f_id] [nvarchar](50) NOT NULL,
	[f_field_name] [nvarchar](50) NULL,
	[f_data_type] [nvarchar](50) NULL,
	[f_data_length] [nvarchar](50) NULL,
	[f_allow_null] [int] NULL,
	[f_field] [nvarchar](50) NULL,
	[f_description] [nvarchar](500) NULL,
	[f_enabled_mark] [int] NULL,
	[f_sort_code] [bigint] NULL,
	[f_creator_time] [datetime] NULL,
	[f_creator_user_id] [nvarchar](50) NULL,
	[f_last_modify_time] [datetime] NULL,
	[f_last_modify_user_id] [nvarchar](50) NULL,
	[f_delete_time] [datetime] NULL,
	[f_delete_user_id] [nvarchar](50) NULL,
	[f_delete_mark] [int] NULL,
	[f_tenant_id] [nvarchar](50) NULL,
	[f_zx_system_id] [nvarchar](50) NULL,
 CONSTRAINT [PK__base_com__2911CBEDDED261FB] PRIMARY KEY CLUSTERED 
(
	[f_id] ASC) ON [PRIMARY]
)
GO

-- 23. base_common_words
IF OBJECT_ID('dbo.base_common_words', 'U') IS NULL
CREATE TABLE [dbo].[base_common_words](
	[f_id] [nvarchar](50) NOT NULL,
	[f_system_ids] [nvarchar](4000) NULL,
	[f_common_words_text] [nvarchar](4000) NULL,
	[f_common_words_type] [int] NULL,
	[f_sort_code] [bigint] NULL,
	[f_enabled_mark] [int] NULL,
	[f_creator_time] [datetime] NULL,
	[f_creator_user_id] [nvarchar](50) NULL,
	[f_last_modify_time] [datetime] NULL,
	[f_last_modify_user_id] [nvarchar](50) NULL,
	[f_delete_time] [datetime] NULL,
	[f_delete_user_id] [nvarchar](50) NULL,
	[f_delete_mark] [int] NULL,
	[f_tenant_id] [nvarchar](50) NULL,
	[f_zx_system_id] [nvarchar](50) NULL,
 CONSTRAINT [PK__base_com__2911CBED556D3BB6] PRIMARY KEY CLUSTERED 
(
	[f_id] ASC) ON [PRIMARY]
)
GO

-- 24. base_data_interface
IF OBJECT_ID('dbo.base_data_interface', 'U') IS NULL
CREATE TABLE [dbo].[base_data_interface](
	[f_id] [nvarchar](50) NOT NULL,
	[f_full_name] [nvarchar](200) NULL,
	[f_en_code] [nvarchar](200) NULL,
	[f_category] [nvarchar](50) NULL,
	[f_type] [int] NULL,
	[f_action] [int] NULL,
	[f_has_page] [int] NULL,
	[f_is_postposition] [int] NULL,
	[f_data_config_json] [nvarchar](max) NOT NULL,
	[f_data_count_json] [nvarchar](max) NOT NULL,
	[f_data_echo_json] [nvarchar](max) NOT NULL,
	[f_data_exception_json] [nvarchar](max) NULL,
	[f_data_js_json] [nvarchar](max) NOT NULL,
	[f_parameter_json] [nvarchar](max) NULL,
	[f_field_json] [nvarchar](max) NULL,
	[f_description] [nvarchar](500) NULL,
	[f_enabled_mark] [int] NULL,
	[f_sort_code] [bigint] NULL,
	[f_creator_time] [datetime] NULL,
	[f_creator_user_id] [nvarchar](50) NULL,
	[f_last_modify_time] [datetime] NULL,
	[f_last_modify_user_id] [nvarchar](50) NULL,
	[f_delete_time] [datetime] NULL,
	[f_delete_user_id] [nvarchar](50) NULL,
	[f_delete_mark] [int] NULL,
	[f_tenant_id] [nvarchar](50) NULL,
	[f_zx_system_id] [nvarchar](50) NULL,
 CONSTRAINT [PK__base_dat__2911CBED94FC4080] PRIMARY KEY CLUSTERED 
(
	[f_id] ASC) ON [PRIMARY]
)
GO

-- 25. base_data_interface_log
IF OBJECT_ID('dbo.base_data_interface_log', 'U') IS NULL
CREATE TABLE [dbo].[base_data_interface_log](
	[f_id] [nvarchar](50) NOT NULL,
	[f_invok_id] [nvarchar](50) NOT NULL,
	[f_invok_time] [datetime] NULL,
	[f_user_id] [nvarchar](50) NULL,
	[f_invok_ip] [nvarchar](50) NULL,
	[f_invok_device] [nvarchar](500) NULL,
	[f_invok_type] [nvarchar](50) NULL,
	[f_invok_waste_time] [int] NULL,
	[f_oauth_app_id] [nvarchar](50) NULL,
	[f_sort_code] [bigint] NULL,
	[f_creator_time] [datetime] NULL,
	[f_creator_user_id] [nvarchar](50) NULL,
	[f_last_modify_time] [datetime] NULL,
	[f_last_modify_user_id] [nvarchar](50) NULL,
	[f_delete_time] [datetime] NULL,
	[f_delete_user_id] [nvarchar](50) NULL,
	[f_delete_mark] [int] NULL,
	[f_tenant_id] [nvarchar](50) NULL,
	[f_zx_system_id] [nvarchar](50) NULL,
 CONSTRAINT [PK__base_dat__2911CBEDD9B8DA97] PRIMARY KEY CLUSTERED 
(
	[f_id] ASC) ON [PRIMARY]
)
GO

-- 26. base_data_interface_oauth
IF OBJECT_ID('dbo.base_data_interface_oauth', 'U') IS NULL
CREATE TABLE [dbo].[base_data_interface_oauth](
	[f_id] [nvarchar](50) NOT NULL,
	[f_app_id] [nvarchar](200) NOT NULL,
	[f_app_name] [nvarchar](50) NOT NULL,
	[f_app_secret] [nvarchar](200) NOT NULL,
	[f_verify_signature] [int] NULL,
	[f_useful_life] [datetime] NULL,
	[f_white_list] [nvarchar](max) NULL,
	[f_black_list] [nvarchar](max) NULL,
	[f_data_interface_ids] [nvarchar](max) NULL,
	[f_sort_code] [bigint] NULL,
	[f_creator_time] [datetime] NULL,
	[f_creator_user_id] [nvarchar](50) NULL,
	[f_last_modify_time] [datetime] NULL,
	[f_last_modify_user_id] [nvarchar](50) NULL,
	[f_enabled_mark] [int] NULL,
	[f_description] [nvarchar](500) NULL,
	[f_delete_time] [datetime] NULL,
	[f_delete_user_id] [nvarchar](50) NULL,
	[f_delete_mark] [int] NULL,
	[f_tenant_id] [nvarchar](50) NULL,
	[f_zx_system_id] [nvarchar](50) NULL,
 CONSTRAINT [PK__base_dat__2911CBED636625BD] PRIMARY KEY CLUSTERED 
(
	[f_id] ASC) ON [PRIMARY]
)
GO

-- 27. base_data_interface_user
IF OBJECT_ID('dbo.base_data_interface_user', 'U') IS NULL
CREATE TABLE [dbo].[base_data_interface_user](
	[f_id] [nvarchar](50) NOT NULL,
	[f_user_id] [nvarchar](50) NULL,
	[f_user_key] [nvarchar](50) NULL,
	[f_oauth_id] [nvarchar](50) NULL,
	[f_sort_code] [bigint] NULL,
	[f_creator_time] [datetime] NULL,
	[f_creator_user_id] [nvarchar](50) NULL,
	[f_last_modify_time] [datetime] NULL,
	[f_last_modify_user_id] [nvarchar](50) NULL,
	[f_delete_time] [datetime] NULL,
	[f_delete_user_id] [nvarchar](50) NULL,
	[f_delete_mark] [int] NULL,
	[f_tenant_id] [nvarchar](50) NULL,
	[f_zx_system_id] [nvarchar](50) NULL,
 CONSTRAINT [PK__base_dat__2911CBEDD1F963AE] PRIMARY KEY CLUSTERED 
(
	[f_id] ASC) ON [PRIMARY]
)
GO

-- 28. base_data_interface_variate
IF OBJECT_ID('dbo.base_data_interface_variate', 'U') IS NULL
CREATE TABLE [dbo].[base_data_interface_variate](
	[f_id] [nvarchar](50) NOT NULL,
	[f_interface_id] [nvarchar](50) NOT NULL,
	[f_full_name] [nvarchar](200) NULL,
	[f_expression] [nvarchar](500) NULL,
	[f_value] [nvarchar](max) NULL,
	[f_sort_code] [bigint] NULL,
	[f_creator_time] [datetime] NULL,
	[f_creator_user_id] [nvarchar](50) NULL,
	[f_last_modify_time] [datetime] NULL,
	[f_last_modify_user_id] [nvarchar](50) NULL,
	[f_delete_time] [datetime] NULL,
	[f_delete_user_id] [nvarchar](50) NULL,
	[f_delete_mark] [int] NULL,
	[f_tenant_id] [nvarchar](50) NULL,
	[f_zx_system_id] [nvarchar](50) NULL,
 CONSTRAINT [PK__base_dat__2911CBED24DB4885] PRIMARY KEY CLUSTERED 
(
	[f_id] ASC) ON [PRIMARY]
)
GO

-- 29. base_db_link
IF OBJECT_ID('dbo.base_db_link', 'U') IS NULL
CREATE TABLE [dbo].[base_db_link](
	[f_id] [nvarchar](50) NOT NULL,
	[f_full_name] [nvarchar](200) NULL,
	[f_db_type] [nvarchar](50) NULL,
	[f_host] [nvarchar](50) NULL,
	[f_port] [int] NULL,
	[f_user_name] [nvarchar](50) NULL,
	[f_password] [nvarchar](50) NULL,
	[f_service_name] [nvarchar](50) NULL,
	[f_description] [nvarchar](500) NULL,
	[f_db_schema] [nvarchar](50) NULL,
	[f_table_space] [nvarchar](50) NULL,
	[f_oracle_param] [nvarchar](500) NULL,
	[f_oracle_extend] [int] NULL,
	[f_sort_code] [bigint] NULL,
	[f_creator_time] [datetime] NULL,
	[f_creator_user_id] [nvarchar](50) NULL,
	[f_last_modify_time] [datetime] NULL,
	[f_last_modify_user_id] [nvarchar](50) NULL,
	[f_delete_time] [datetime] NULL,
	[f_delete_user_id] [nvarchar](50) NULL,
	[f_delete_mark] [int] NULL,
	[f_tenant_id] [nvarchar](50) NULL,
	[f_zx_system_id] [nvarchar](50) NULL,
 CONSTRAINT [PK__base_db___2911CBED62F182F7] PRIMARY KEY CLUSTERED 
(
	[f_id] ASC) ON [PRIMARY]
)
GO

-- 30. base_dictionary_data
IF OBJECT_ID('dbo.base_dictionary_data', 'U') IS NULL
CREATE TABLE [dbo].[base_dictionary_data](
	[f_id] [nvarchar](50) NOT NULL,
	[f_parent_id] [nvarchar](50) NULL,
	[f_full_name] [nvarchar](200) NULL,
	[f_en_code] [nvarchar](200) NULL,
	[f_simple_spelling] [nvarchar](500) NULL,
	[f_is_default] [int] NULL,
	[f_description] [nvarchar](500) NULL,
	[f_enabled_mark] [int] NULL,
	[f_dictionary_type_id] [nvarchar](50) NULL,
	[f_sort_code] [bigint] NULL,
	[f_creator_time] [datetime] NULL,
	[f_creator_user_id] [nvarchar](50) NULL,
	[f_last_modify_time] [datetime] NULL,
	[f_last_modify_user_id] [nvarchar](50) NULL,
	[f_delete_time] [datetime] NULL,
	[f_delete_user_id] [nvarchar](50) NULL,
	[f_delete_mark] [int] NULL,
	[f_tenant_id] [nvarchar](50) NULL,
	[f_zx_system_id] [nvarchar](50) NULL,
	[f_zx_datatype] [int] NULL,
 CONSTRAINT [PK__base_dic__2911CBEDC0E51BDB] PRIMARY KEY CLUSTERED 
(
	[f_id] ASC) ON [PRIMARY]
)
GO

-- 31. base_dictionary_type
IF OBJECT_ID('dbo.base_dictionary_type', 'U') IS NULL
CREATE TABLE [dbo].[base_dictionary_type](
	[f_id] [nvarchar](50) NOT NULL,
	[f_parent_id] [nvarchar](50) NULL,
	[f_full_name] [nvarchar](200) NULL,
	[f_en_code] [nvarchar](200) NULL,
	[f_is_tree] [int] NULL,
	[f_type] [int] NULL,
	[f_description] [nvarchar](500) NULL,
	[f_enabled_mark] [int] NULL,
	[f_sort_code] [bigint] NULL,
	[f_creator_time] [datetime] NULL,
	[f_creator_user_id] [nvarchar](50) NULL,
	[f_last_modify_time] [datetime] NULL,
	[f_last_modify_user_id] [nvarchar](50) NULL,
	[f_delete_time] [datetime] NULL,
	[f_delete_user_id] [nvarchar](50) NULL,
	[f_delete_mark] [int] NULL,
	[f_tenant_id] [nvarchar](50) NULL,
	[f_zx_system_id] [nvarchar](50) NULL,
	[f_zx_datatype] [int] NULL,
 CONSTRAINT [PK__base_dic__2911CBEDBD15EE4F] PRIMARY KEY CLUSTERED 
(
	[f_id] ASC) ON [PRIMARY]
)
GO

-- 32. base_file
IF OBJECT_ID('dbo.base_file', 'U') IS NULL
CREATE TABLE [dbo].[base_file](
	[f_id] [nvarchar](50) NOT NULL,
	[f_file_version] [nvarchar](500) NULL,
	[f_file_name] [nvarchar](500) NULL,
	[f_type] [int] NULL,
	[f_url] [nvarchar](500) NULL,
	[f_old_file_version_id] [nvarchar](500) NULL,
	[f_sort_code] [bigint] NULL,
	[f_creator_time] [datetime] NULL,
	[f_creator_user_id] [nvarchar](50) NULL,
	[f_last_modify_time] [datetime] NULL,
	[f_last_modify_user_id] [nvarchar](50) NULL,
	[f_delete_time] [datetime] NULL,
	[f_delete_user_id] [nvarchar](50) NULL,
	[f_delete_mark] [int] NULL,
	[f_tenant_id] [nvarchar](50) NULL,
	[f_zx_system_id] [nvarchar](50) NULL,
 CONSTRAINT [PK__base_fil__2911CBEDFD278C03] PRIMARY KEY CLUSTERED 
(
	[f_id] ASC) ON [PRIMARY]
)
GO

-- 33. BASE_FOUNDER_AUTH_LOG
IF OBJECT_ID('dbo.BASE_FOUNDER_AUTH_LOG', 'U') IS NULL
CREATE TABLE [dbo].[BASE_FOUNDER_AUTH_LOG](
	[F_ID] [nvarchar](50) NOT NULL,
	[F_ACTION] [nvarchar](200) NULL,
	[F_RESULT] [nvarchar](50) NULL,
	[F_IP_ADDRESS] [nvarchar](50) NULL,
	[F_USER_AGENT] [nvarchar](500) NULL,
	[F_DEVICE_FINGERPRINT] [nvarchar](100) NULL,
	[F_CREATOR_TIME] [datetime] NULL,
	[F_CREATOR_USER_ID] [nvarchar](50) NULL,
	[F_LAST_MODIFY_TIME] [datetime] NULL,
	[F_LAST_MODIFY_USER_ID] [nvarchar](50) NULL,
	[F_DELETE_MARK] [int] NULL,
	[F_DELETE_TIME] [datetime] NULL,
	[F_DELETE_USER_ID] [nvarchar](50) NULL,
	[F_SORT_CODE] [bigint] NULL,
	[F_ENABLED_MARK] [int] NULL,
	[F_TENANT_ID] [nvarchar](50) NULL,
PRIMARY KEY CLUSTERED 
(
	[F_ID] ASC) ON [PRIMARY]
)
GO

-- 34. base_group
IF OBJECT_ID('dbo.base_group', 'U') IS NULL
CREATE TABLE [dbo].[base_group](
	[f_id] [nvarchar](50) NOT NULL,
	[f_full_name] [nvarchar](200) NULL,
	[f_en_code] [nvarchar](200) NULL,
	[f_category] [nvarchar](50) NULL,
	[f_description] [nvarchar](500) NULL,
	[f_enabled_mark] [int] NULL,
	[f_sort_code] [bigint] NULL,
	[f_creator_time] [datetime] NULL,
	[f_creator_user_id] [nvarchar](50) NULL,
	[f_last_modify_time] [datetime] NULL,
	[f_last_modify_user_id] [nvarchar](50) NULL,
	[f_delete_time] [datetime] NULL,
	[f_delete_user_id] [nvarchar](50) NULL,
	[f_delete_mark] [int] NULL,
	[f_tenant_id] [nvarchar](50) NULL,
 CONSTRAINT [PK__base_gro__2911CBED27A91BF3] PRIMARY KEY CLUSTERED 
(
	[f_id] ASC) ON [PRIMARY]
)
GO

-- 35. base_im_content
IF OBJECT_ID('dbo.base_im_content', 'U') IS NULL
CREATE TABLE [dbo].[base_im_content](
	[f_id] [nvarchar](50) NOT NULL,
	[f_send_user_id] [nvarchar](50) NULL,
	[f_send_time] [datetime] NULL,
	[f_receive_user_id] [nvarchar](50) NULL,
	[f_receive_time] [datetime] NULL,
	[f_content] [nvarchar](max) NULL,
	[f_content_type] [nvarchar](50) NULL,
	[f_enabled_mark] [int] NULL,
	[f_sort_code] [bigint] NULL,
	[f_creator_time] [datetime] NULL,
	[f_creator_user_id] [nvarchar](50) NULL,
	[f_last_modify_time] [datetime] NULL,
	[f_last_modify_user_id] [nvarchar](50) NULL,
	[f_delete_time] [datetime] NULL,
	[f_delete_user_id] [nvarchar](50) NULL,
	[f_delete_mark] [int] NULL,
	[f_tenant_id] [nvarchar](50) NULL,
	[f_zx_system_id] [nvarchar](50) NULL,
 CONSTRAINT [PK__base_im___2911CBED9549E764] PRIMARY KEY CLUSTERED 
(
	[f_id] ASC) ON [PRIMARY]
)
GO

-- 36. base_im_reply
IF OBJECT_ID('dbo.base_im_reply', 'U') IS NULL
CREATE TABLE [dbo].[base_im_reply](
	[f_id] [nvarchar](50) NOT NULL,
	[f_user_id] [nvarchar](50) NULL,
	[f_receive_user_id] [nvarchar](50) NULL,
	[f_receive_time] [datetime] NULL,
	[f_sort_code] [bigint] NULL,
	[f_creator_time] [datetime] NULL,
	[f_creator_user_id] [nvarchar](50) NULL,
	[f_last_modify_time] [datetime] NULL,
	[f_last_modify_user_id] [nvarchar](50) NULL,
	[f_delete_time] [datetime] NULL,
	[f_delete_user_id] [nvarchar](50) NULL,
	[f_delete_mark] [int] NULL,
	[f_tenant_id] [nvarchar](50) NULL,
	[f_zx_system_id] [nvarchar](50) NULL,
 CONSTRAINT [PK__base_im___2911CBEDF243E69C] PRIMARY KEY CLUSTERED 
(
	[f_id] ASC) ON [PRIMARY]
)
GO

-- 37. base_integrate
IF OBJECT_ID('dbo.base_integrate', 'U') IS NULL
CREATE TABLE [dbo].[base_integrate](
	[f_id] [nvarchar](50) NOT NULL,
	[f_full_name] [nvarchar](200) NULL,
	[f_en_code] [nvarchar](200) NULL,
	[f_template_json] [nvarchar](max) NULL,
	[f_trigger_type] [int] NULL,
	[f_resultType] [int] NULL,
	[f_type] [int] NULL,
	[f_form_id] [nvarchar](50) NULL,
	[f_description] [nvarchar](500) NULL,
	[f_enabled_mark] [int] NULL,
	[f_sort_code] [bigint] NULL,
	[f_creator_time] [datetime] NULL,
	[f_creator_user_id] [nvarchar](50) NULL,
	[f_last_modify_time] [datetime] NULL,
	[f_last_modify_user_id] [nvarchar](50) NULL,
	[f_delete_time] [datetime] NULL,
	[f_delete_user_id] [nvarchar](50) NULL,
	[f_delete_mark] [int] NULL,
	[f_tenant_id] [nvarchar](50) NULL,
	[f_zx_system_id] [nvarchar](50) NULL,
 CONSTRAINT [PK__base_int__2911CBEDD7AFE8BC] PRIMARY KEY CLUSTERED 
(
	[f_id] ASC) ON [PRIMARY]
)
GO

-- 38. base_integrate_node
IF OBJECT_ID('dbo.base_integrate_node', 'U') IS NULL
CREATE TABLE [dbo].[base_integrate_node](
	[f_id] [nvarchar](50) NOT NULL,
	[f_task_id] [nvarchar](50) NULL,
	[f_form_Id] [nvarchar](50) NULL,
	[f_node_type] [nvarchar](50) NULL,
	[f_start_time] [datetime] NULL,
	[f_end_time] [datetime] NULL,
	[f_error_msg] [nvarchar](max) NULL,
	[f_node_code] [nvarchar](50) NULL,
	[f_node_name] [nvarchar](50) NULL,
	[f_node_next] [nvarchar](2000) NULL,
	[f_result_type] [int] NULL,
	[f_node_property_json] [nvarchar](max) NULL,
	[f_enabled_mark] [int] NULL,
	[f_sort_code] [bigint] NULL,
	[f_creator_time] [datetime] NULL,
	[f_creator_user_id] [nvarchar](50) NULL,
	[f_last_modify_time] [datetime] NULL,
	[f_last_modify_user_id] [nvarchar](50) NULL,
	[f_delete_time] [datetime] NULL,
	[f_delete_user_id] [nvarchar](50) NULL,
	[f_delete_mark] [int] NULL,
	[f_tenant_id] [nvarchar](50) NULL,
	[f_description] [nvarchar](500) NULL,
	[f_parent_id] [nvarchar](50) NULL,
	[f_is_retry] [int] NULL,
	[f_zx_system_id] [nvarchar](50) NULL,
 CONSTRAINT [PK__base_int__2911CBEDCFAF7174] PRIMARY KEY CLUSTERED 
(
	[f_id] ASC) ON [PRIMARY]
)
GO

-- 39. base_integrate_queue
IF OBJECT_ID('dbo.base_integrate_queue', 'U') IS NULL
CREATE TABLE [dbo].[base_integrate_queue](
	[f_id] [nvarchar](50) NOT NULL,
	[f_full_name] [nvarchar](50) NULL,
	[f_integrate_id] [nvarchar](200) NULL,
	[f_execution_time] [datetime] NULL,
	[f_state] [int] NULL,
	[f_enabled_mark] [int] NULL,
	[f_creator_time] [datetime] NULL,
	[f_creator_user_id] [nvarchar](50) NULL,
	[f_last_modify_time] [datetime] NULL,
	[f_last_modify_user_id] [nvarchar](50) NULL,
	[f_delete_time] [datetime] NULL,
	[f_delete_user_id] [nvarchar](50) NULL,
	[f_delete_mark] [int] NULL,
	[f_tenant_id] [nvarchar](50) NULL,
	[f_description] [nvarchar](4000) NULL,
	[f_sort_code] [bigint] NULL,
	[f_zx_system_id] [nvarchar](50) NULL,
 CONSTRAINT [PK__base_int__2911CBED8C955E92] PRIMARY KEY CLUSTERED 
(
	[f_id] ASC) ON [PRIMARY]
)
GO

-- 40. base_integrate_task
IF OBJECT_ID('dbo.base_integrate_task', 'U') IS NULL
CREATE TABLE [dbo].[base_integrate_task](
	[f_id] [nvarchar](50) NOT NULL,
	[f_process_id] [nvarchar](50) NULL,
	[f_parent_time] [datetime] NULL,
	[f_parent_id] [nvarchar](50) NULL,
	[f_execution_time] [datetime] NULL,
	[f_template_json] [nvarchar](max) NULL,
	[f_data] [nvarchar](max) NULL,
	[f_data_id] [nvarchar](50) NULL,
	[f_type] [int] NULL,
	[f_integrate_id] [nvarchar](200) NULL,
	[f_result_type] [int] NULL,
	[f_description] [nvarchar](500) NULL,
	[f_enabled_mark] [int] NULL,
	[f_sort_code] [bigint] NULL,
	[f_creator_time] [datetime] NULL,
	[f_creator_user_id] [nvarchar](50) NULL,
	[f_last_modify_time] [datetime] NULL,
	[f_last_modify_user_id] [nvarchar](50) NULL,
	[f_delete_time] [datetime] NULL,
	[f_delete_user_id] [nvarchar](50) NULL,
	[f_delete_mark] [int] NULL,
	[f_tenant_id] [nvarchar](50) NULL,
	[f_zx_system_id] [nvarchar](50) NULL,
 CONSTRAINT [PK__base_int__2911CBEDD478624B] PRIMARY KEY CLUSTERED 
(
	[f_id] ASC) ON [PRIMARY]
)
GO

-- 41. BASE_IR_EDIT_PATCH
IF OBJECT_ID('dbo.BASE_IR_EDIT_PATCH', 'U') IS NULL
CREATE TABLE [dbo].[BASE_IR_EDIT_PATCH](
	[F_ID] [bigint] NOT NULL,
	[F_PIPELINE_ID] [bigint] NOT NULL,
	[F_VERSION_ID] [bigint] NULL,
	[F_TARGET_NODE_IDS] [nvarchar](max) NOT NULL,
	[F_OPERATIONS] [nvarchar](max) NOT NULL,
	[F_EXPLANATION] [nvarchar](500) NULL,
	[F_STATUS] [nvarchar](20) NOT NULL,
	[F_APPLIED_COUNT] [int] NOT NULL,
	[F_FAILED_COUNT] [int] NOT NULL,
	[F_CHANGE_TYPE] [nvarchar](20) NOT NULL,
	[F_TENANT_ID] [nvarchar](50) NOT NULL,
	[F_CREATOR_USER_ID] [nvarchar](50) NULL,
	[F_CREATOR_TIME] [datetime] NULL,
	[F_LAST_MODIFY_USER_ID] [nvarchar](50) NULL,
	[F_LAST_MODIFY_TIME] [datetime] NULL,
	[F_DELETE_MARK] [int] NULL,
PRIMARY KEY CLUSTERED 
(
	[F_ID] ASC) ON [PRIMARY]
)
GO

-- 42. BASE_IR_VERSION
IF OBJECT_ID('dbo.BASE_IR_VERSION', 'U') IS NULL
CREATE TABLE [dbo].[BASE_IR_VERSION](
	[F_ID] [nvarchar](50) NOT NULL,
	[F_PIPELINE_ID] [nvarchar](50) NULL,
	[F_VERSION] [int] NOT NULL,
	[F_TRIGGERED_BY] [nvarchar](100) NULL,
	[F_CHANGE_SUMMARY] [nvarchar](500) NULL,
	[F_PARENT_VERSION_ID] [bigint] NULL,
	[F_DIFF] [nvarchar](max) NULL,
	[F_CHANGE_TYPE] [nvarchar](20) NULL,
	[F_EDIT_PATCH_ID] [bigint] NULL,
	[F_VALIDATION_RESULT] [nvarchar](max) NULL,
	[F_SNAPSHOT_AT] [datetime] NULL,
	[F_IR_SNAPSHOT] [nvarchar](max) NULL,
	[F_TENANT_ID] [nvarchar](50) NOT NULL,
	[F_CREATOR_TIME] [datetime] NULL,
	[F_CREATOR_USER_ID] [nvarchar](50) NULL,
	[F_LAST_MODIFY_TIME] [datetime] NULL,
	[F_LAST_MODIFY_USER_ID] [nvarchar](50) NULL,
	[F_DELETE_MARK] [int] NULL,
	[F_SORT_CODE] [bigint] NULL,
	[F_ENABLED_MARK] [int] NULL,
PRIMARY KEY CLUSTERED 
(
	[F_ID] ASC) ON [PRIMARY]
)
GO

-- 43. BASE_KNOWLEDGE_EDGE
IF OBJECT_ID('dbo.BASE_KNOWLEDGE_EDGE', 'U') IS NULL
CREATE TABLE [dbo].[BASE_KNOWLEDGE_EDGE](
	[F_ID] [nvarchar](50) NOT NULL,
	[F_SOURCE_NODE_ID] [nvarchar](50) NOT NULL,
	[F_TARGET_NODE_ID] [nvarchar](50) NOT NULL,
	[F_RELATION_TYPE] [nvarchar](100) NOT NULL,
	[F_PROPERTIES] [nvarchar](max) NULL,
	[F_VERSION] [int] NOT NULL,
	[F_CREATOR_TIME] [datetime] NULL,
	[F_CREATOR_USER_ID] [nvarchar](50) NULL,
	[F_LAST_MODIFY_TIME] [datetime] NULL,
	[F_LAST_MODIFY_USER_ID] [nvarchar](50) NULL,
	[F_DELETE_TIME] [datetime] NULL,
	[F_DELETE_USER_ID] [nvarchar](50) NULL,
	[F_DELETE_MARK] [int] NULL,
	[F_TENANT_ID] [nvarchar](50) NULL,
	[F_SORT_CODE] [bigint] NULL,
	[F_ENABLED_MARK] [int] NULL,
PRIMARY KEY CLUSTERED 
(
	[F_ID] ASC) ON [PRIMARY]
)
GO

-- 44. BASE_KNOWLEDGE_NODE
IF OBJECT_ID('dbo.BASE_KNOWLEDGE_NODE', 'U') IS NULL
CREATE TABLE [dbo].[BASE_KNOWLEDGE_NODE](
	[F_ID] [nvarchar](50) NOT NULL,
	[F_LABEL] [nvarchar](100) NOT NULL,
	[F_NAME] [nvarchar](255) NOT NULL,
	[F_PROPERTIES] [nvarchar](max) NULL,
	[F_VERSION] [int] NOT NULL,
	[F_CREATOR_TIME] [datetime] NULL,
	[F_CREATOR_USER_ID] [nvarchar](50) NULL,
	[F_LAST_MODIFY_TIME] [datetime] NULL,
	[F_LAST_MODIFY_USER_ID] [nvarchar](50) NULL,
	[F_DELETE_TIME] [datetime] NULL,
	[F_DELETE_USER_ID] [nvarchar](50) NULL,
	[F_DELETE_MARK] [int] NULL,
	[F_TENANT_ID] [nvarchar](50) NULL,
	[F_SORT_CODE] [bigint] NULL,
	[F_ENABLED_MARK] [int] NULL,
PRIMARY KEY CLUSTERED 
(
	[F_ID] ASC) ON [PRIMARY]
)
GO

-- 45. BASE_KNOWLEDGE_RULE
IF OBJECT_ID('dbo.BASE_KNOWLEDGE_RULE', 'U') IS NULL
CREATE TABLE [dbo].[BASE_KNOWLEDGE_RULE](
	[F_Id] [bigint] NOT NULL,
	[F_TenantId] [nvarchar](50) NOT NULL,
	[F_Name] [nvarchar](200) NOT NULL,
	[F_Description] [nvarchar](2000) NULL,
	[F_Type] [nvarchar](30) NOT NULL,
	[F_Entity] [nvarchar](100) NULL,
	[F_Fields] [nvarchar](max) NULL,
	[F_Config] [nvarchar](max) NULL,
	[F_Source] [nvarchar](20) NOT NULL,
	[F_Version] [int] NOT NULL,
	[F_Enabled] [bit] NOT NULL,
	[F_CreatorTime] [datetime] NOT NULL,
	[F_CreatorUserId] [bigint] NULL,
	[F_ModifyTime] [datetime] NULL,
	[F_ModifyUserId] [bigint] NULL,
	[F_DeleteMark] [bit] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[F_Id] ASC) ON [PRIMARY]
)
GO

-- 46. BASE_MENU_BADGE
IF OBJECT_ID('dbo.BASE_MENU_BADGE', 'U') IS NULL
CREATE TABLE [dbo].[BASE_MENU_BADGE](
	[F_Id] [bigint] NOT NULL,
	[F_MenuId] [bigint] NOT NULL,
	[F_UserId] [bigint] NOT NULL,
	[F_TenantId] [nvarchar](50) NOT NULL,
	[F_Count] [int] NOT NULL,
	[F_ExtraData] [nvarchar](max) NULL,
	[F_CreatorTime] [datetime] NOT NULL,
	[F_ModifyTime] [datetime] NULL,
PRIMARY KEY CLUSTERED 
(
	[F_Id] ASC) ON [PRIMARY],
 CONSTRAINT [UQ_BADGE] UNIQUE NONCLUSTERED 
(
	[F_MenuId] ASC,
	[F_UserId] ASC,
	[F_TenantId] ASC) ON [PRIMARY]
)
GO

-- 47. base_message
IF OBJECT_ID('dbo.base_message', 'U') IS NULL
CREATE TABLE [dbo].[base_message](
	[f_id] [nvarchar](50) NOT NULL,
	[f_type] [int] NULL,
	[f_title] [nvarchar](200) NULL,
	[f_flow_type] [int] NULL,
	[f_user_id] [nvarchar](50) NULL,
	[f_is_read] [int] NULL,
	[f_read_time] [datetime] NULL,
	[f_read_count] [int] NULL,
	[f_sort_code] [bigint] NULL,
	[f_creator_time] [datetime] NULL,
	[f_creator_user_id] [nvarchar](50) NULL,
	[f_last_modify_time] [datetime] NULL,
	[f_last_modify_user_id] [nvarchar](50) NULL,
	[f_delete_time] [datetime] NULL,
	[f_delete_user_id] [nvarchar](50) NULL,
	[f_delete_mark] [int] NULL,
	[f_tenant_id] [nvarchar](50) NULL,
	[f_body_text] [nvarchar](max) NULL,
	[f_zx_system_id] [nvarchar](50) NULL,
	[f_enabled_mark] [int] NULL,
 CONSTRAINT [PK__base_mes__2911CBED96244A9D] PRIMARY KEY CLUSTERED 
(
	[f_id] ASC) ON [PRIMARY]
)
GO

-- 48. base_module
IF OBJECT_ID('dbo.base_module', 'U') IS NULL
CREATE TABLE [dbo].[base_module](
	[f_id] [nvarchar](50) NOT NULL,
	[f_parent_id] [nvarchar](50) NULL,
	[f_type] [int] NULL,
	[f_full_name] [nvarchar](200) NULL,
	[f_en_code] [nvarchar](200) NULL,
	[f_category] [nvarchar](50) NULL,
	[f_url_address] [nvarchar](500) NULL,
	[f_icon] [nvarchar](500) NULL,
	[f_link_target] [nvarchar](50) NULL,
	[f_is_button_authorize] [int] NULL,
	[f_is_column_authorize] [int] NULL,
	[f_is_data_authorize] [int] NULL,
	[f_is_form_authorize] [int] NULL,
	[f_module_id] [nvarchar](50) NULL,
	[f_system_id] [nvarchar](50) NULL,
	[f_property_json] [nvarchar](max) NULL,
	[f_description] [nvarchar](500) NULL,
	[f_enabled_mark] [int] NULL,
	[f_sort_code] [bigint] NULL,
	[f_creator_time] [datetime] NULL,
	[f_creator_user_id] [nvarchar](50) NULL,
	[f_last_modify_time] [datetime] NULL,
	[f_last_modify_user_id] [nvarchar](50) NULL,
	[f_delete_time] [datetime] NULL,
	[f_delete_user_id] [nvarchar](50) NULL,
	[f_delete_mark] [int] NULL,
	[f_tenant_id] [nvarchar](50) NULL,
	[f_zx_system_id] [nvarchar](50) NULL,
 CONSTRAINT [PK__base_mod__2911CBED98F45AA7] PRIMARY KEY CLUSTERED 
(
	[f_id] ASC) ON [PRIMARY]
)
GO

-- 49. base_module_authorize
IF OBJECT_ID('dbo.base_module_authorize', 'U') IS NULL
CREATE TABLE [dbo].[base_module_authorize](
	[f_id] [nvarchar](50) NOT NULL,
	[f_full_name] [nvarchar](200) NULL,
	[f_en_code] [nvarchar](200) NULL,
	[f_type] [nvarchar](50) NULL,
	[f_condition_symbol] [nvarchar](500) NULL,
	[f_condition_text] [nvarchar](500) NULL,
	[f_property_json] [nvarchar](max) NULL,
	[f_module_id] [nvarchar](50) NULL,
	[f_field_rule] [int] NULL,
	[f_child_table_key] [nvarchar](50) NULL,
	[f_bind_table] [nvarchar](50) NULL,
	[f_format] [nvarchar](20) NULL,
	[f_description] [nvarchar](500) NULL,
	[f_enabled_mark] [int] NULL,
	[f_sort_code] [bigint] NULL,
	[f_creator_time] [datetime] NULL,
	[f_creator_user_id] [nvarchar](50) NULL,
	[f_last_modify_time] [datetime] NULL,
	[f_last_modify_user_id] [nvarchar](50) NULL,
	[f_delete_time] [datetime] NULL,
	[f_delete_user_id] [nvarchar](50) NULL,
	[f_delete_mark] [int] NULL,
	[f_tenant_id] [nvarchar](50) NULL,
	[f_zx_system_id] [nvarchar](50) NULL,
 CONSTRAINT [PK__base_mod__2911CBED0B246DBD] PRIMARY KEY CLUSTERED 
(
	[f_id] ASC) ON [PRIMARY]
)
GO

-- 50. base_module_button
IF OBJECT_ID('dbo.base_module_button', 'U') IS NULL
CREATE TABLE [dbo].[base_module_button](
	[f_id] [nvarchar](50) NOT NULL,
	[f_parent_id] [nvarchar](50) NULL,
	[f_full_name] [nvarchar](200) NULL,
	[f_en_code] [nvarchar](200) NULL,
	[f_icon] [nvarchar](500) NULL,
	[f_url_address] [nvarchar](500) NULL,
	[f_property_json] [nvarchar](max) NULL,
	[f_module_id] [nvarchar](50) NULL,
	[f_description] [nvarchar](500) NULL,
	[f_enabled_mark] [int] NULL,
	[f_sort_code] [bigint] NULL,
	[f_creator_time] [datetime] NULL,
	[f_creator_user_id] [nvarchar](50) NULL,
	[f_last_modify_time] [datetime] NULL,
	[f_last_modify_user_id] [nvarchar](50) NULL,
	[f_delete_time] [datetime] NULL,
	[f_delete_user_id] [nvarchar](50) NULL,
	[f_delete_mark] [int] NULL,
	[f_tenant_id] [nvarchar](50) NULL,
	[f_zx_system_id] [nvarchar](50) NULL,
 CONSTRAINT [PK__base_mod__2911CBEDD43C3762] PRIMARY KEY CLUSTERED 
(
	[f_id] ASC) ON [PRIMARY]
)
GO

-- 51. base_module_column
IF OBJECT_ID('dbo.base_module_column', 'U') IS NULL
CREATE TABLE [dbo].[base_module_column](
	[f_id] [nvarchar](50) NOT NULL,
	[f_parent_id] [nvarchar](50) NULL,
	[f_full_name] [nvarchar](200) NULL,
	[f_en_code] [nvarchar](200) NULL,
	[f_bind_table] [nvarchar](50) NULL,
	[f_bind_table_name] [nvarchar](50) NULL,
	[f_property_json] [nvarchar](max) NULL,
	[f_module_id] [nvarchar](50) NULL,
	[f_field_rule] [int] NULL,
	[f_child_table_key] [nvarchar](50) NULL,
	[f_description] [nvarchar](500) NULL,
	[f_enabled_mark] [int] NULL,
	[f_sort_code] [bigint] NULL,
	[f_creator_time] [datetime] NULL,
	[f_creator_user_id] [nvarchar](50) NULL,
	[f_last_modify_time] [datetime] NULL,
	[f_last_modify_user_id] [nvarchar](50) NULL,
	[f_delete_time] [datetime] NULL,
	[f_delete_user_id] [nvarchar](50) NULL,
	[f_delete_mark] [int] NULL,
	[f_tenant_id] [nvarchar](50) NULL,
	[f_zx_system_id] [nvarchar](50) NULL,
 CONSTRAINT [PK__base_mod__2911CBEDB104A3C5] PRIMARY KEY CLUSTERED 
(
	[f_id] ASC) ON [PRIMARY]
)
GO

-- 52. base_module_form
IF OBJECT_ID('dbo.base_module_form', 'U') IS NULL
CREATE TABLE [dbo].[base_module_form](
	[f_id] [nvarchar](50) NOT NULL,
	[f_parent_id] [nvarchar](50) NULL,
	[f_full_name] [nvarchar](200) NULL,
	[f_en_code] [nvarchar](200) NULL,
	[f_property_json] [nvarchar](max) NULL,
	[f_module_id] [nvarchar](50) NULL,
	[f_field_rule] [int] NULL,
	[f_child_table_key] [nvarchar](50) NULL,
	[f_bind_table] [nvarchar](50) NULL,
	[f_description] [nvarchar](500) NULL,
	[f_enabled_mark] [int] NULL,
	[f_sort_code] [bigint] NULL,
	[f_creator_time] [datetime] NULL,
	[f_creator_user_id] [nvarchar](50) NULL,
	[f_last_modify_time] [datetime] NULL,
	[f_last_modify_user_id] [nvarchar](50) NULL,
	[f_delete_time] [datetime] NULL,
	[f_delete_user_id] [nvarchar](50) NULL,
	[f_delete_mark] [int] NULL,
	[f_tenant_id] [nvarchar](50) NULL,
	[f_zx_system_id] [nvarchar](50) NULL,
 CONSTRAINT [PK__base_mod__2911CBED825258BB] PRIMARY KEY CLUSTERED 
(
	[f_id] ASC) ON [PRIMARY]
)
GO

-- 53. base_module_link
IF OBJECT_ID('dbo.base_module_link', 'U') IS NULL
CREATE TABLE [dbo].[base_module_link](
	[f_id] [nvarchar](50) NOT NULL,
	[f_link_id] [nvarchar](50) NULL,
	[f_link_tables] [nvarchar](200) NULL,
	[f_module_id] [nvarchar](50) NULL,
	[f_type] [int] NULL,
	[f_sort_code] [bigint] NULL,
	[f_creator_time] [datetime] NULL,
	[f_creator_user_id] [nvarchar](50) NULL,
	[f_last_modify_time] [datetime] NULL,
	[f_last_modify_user_id] [nvarchar](50) NULL,
	[f_delete_time] [datetime] NULL,
	[f_delete_user_id] [nvarchar](50) NULL,
	[f_delete_mark] [int] NULL,
	[f_tenant_id] [nvarchar](50) NULL,
	[f_zx_system_id] [nvarchar](50) NULL,
 CONSTRAINT [PK__base_mod__2911CBED9B8C8A2A] PRIMARY KEY CLUSTERED 
(
	[f_id] ASC) ON [PRIMARY]
)
GO

-- 54. base_module_scheme
IF OBJECT_ID('dbo.base_module_scheme', 'U') IS NULL
CREATE TABLE [dbo].[base_module_scheme](
	[f_id] [nvarchar](50) NOT NULL,
	[f_en_code] [nvarchar](200) NULL,
	[f_full_name] [nvarchar](100) NULL,
	[f_condition_json] [nvarchar](max) NULL,
	[f_condition_text] [nvarchar](500) NULL,
	[f_description] [nvarchar](500) NULL,
	[f_module_id] [nvarchar](50) NULL,
	[f_all_data] [int] NULL,
	[f_enabled_mark] [int] NULL,
	[f_sort_code] [bigint] NULL,
	[f_match_logic] [nvarchar](50) NULL,
	[f_creator_time] [datetime] NULL,
	[f_creator_user_id] [nvarchar](50) NULL,
	[f_last_modify_time] [datetime] NULL,
	[f_last_modify_user_id] [nvarchar](50) NULL,
	[f_delete_time] [datetime] NULL,
	[f_delete_user_id] [nvarchar](50) NULL,
	[f_delete_mark] [int] NULL,
	[f_tenant_id] [nvarchar](50) NULL,
	[f_zx_system_id] [nvarchar](50) NULL,
 CONSTRAINT [PK__base_mod__2911CBEDB7FD72B2] PRIMARY KEY CLUSTERED 
(
	[f_id] ASC) ON [PRIMARY]
)
GO

-- 55. base_msg_account
IF OBJECT_ID('dbo.base_msg_account', 'U') IS NULL
CREATE TABLE [dbo].[base_msg_account](
	[f_id] [nvarchar](50) NOT NULL,
	[f_category] [nvarchar](50) NULL,
	[f_full_name] [nvarchar](200) NULL,
	[f_en_code] [nvarchar](200) NULL,
	[f_addressor_name] [nvarchar](50) NULL,
	[f_smtp_server] [nvarchar](50) NULL,
	[f_smtp_port] [int] NULL,
	[f_ssl_link] [int] NULL,
	[f_smtp_user] [nvarchar](50) NULL,
	[f_smtp_password] [nvarchar](50) NULL,
	[f_channel] [int] NULL,
	[f_sms_signature] [nvarchar](50) NULL,
	[f_app_id] [nvarchar](50) NULL,
	[f_app_secret] [nvarchar](500) NULL,
	[f_end_point] [nvarchar](50) NULL,
	[f_sdk_app_id] [nvarchar](50) NULL,
	[f_app_key] [nvarchar](50) NULL,
	[f_zone_name] [nvarchar](50) NULL,
	[f_zone_param] [nvarchar](50) NULL,
	[f_enterprise_id] [nvarchar](50) NULL,
	[f_agent_id] [nvarchar](50) NULL,
	[f_webhook_type] [int] NULL,
	[f_webhook_address] [nvarchar](500) NULL,
	[f_approve_type] [int] NULL,
	[f_bearer] [nvarchar](500) NULL,
	[f_user_name] [nvarchar](50) NULL,
	[f_password] [nvarchar](50) NULL,
	[f_sort_code] [bigint] NULL,
	[f_description] [nvarchar](500) NULL,
	[f_enabled_mark] [int] NULL,
	[f_creator_time] [datetime] NULL,
	[f_creator_user_id] [nvarchar](50) NULL,
	[f_last_modify_time] [datetime] NULL,
	[f_last_modify_user_id] [nvarchar](50) NULL,
	[f_delete_time] [datetime] NULL,
	[f_delete_user_id] [nvarchar](50) NULL,
	[f_delete_mark] [int] NULL,
	[f_tenant_id] [nvarchar](50) NULL,
	[f_zx_system_id] [nvarchar](50) NULL,
 CONSTRAINT [PK__base_msg__2911CBED74B3FB88] PRIMARY KEY CLUSTERED 
(
	[f_id] ASC) ON [PRIMARY]
)
GO

-- 56. base_msg_monitor
IF OBJECT_ID('dbo.base_msg_monitor', 'U') IS NULL
CREATE TABLE [dbo].[base_msg_monitor](
	[f_id] [nvarchar](50) NOT NULL,
	[f_account_id] [nvarchar](50) NULL,
	[f_account_name] [nvarchar](50) NULL,
	[f_account_code] [nvarchar](50) NULL,
	[f_message_type] [nvarchar](50) NULL,
	[f_message_source] [nvarchar](50) NULL,
	[f_send_time] [datetime] NULL,
	[f_message_template_id] [nvarchar](50) NULL,
	[f_title] [nvarchar](200) NULL,
	[f_receive_user] [nvarchar](max) NULL,
	[f_content] [nvarchar](max) NULL,
	[f_sort_code] [bigint] NULL,
	[f_creator_time] [datetime] NULL,
	[f_creator_user_id] [nvarchar](50) NULL,
	[f_last_modify_time] [datetime] NULL,
	[f_last_modify_user_id] [nvarchar](50) NULL,
	[f_delete_time] [datetime] NULL,
	[f_delete_user_id] [nvarchar](50) NULL,
	[f_delete_mark] [int] NULL,
	[f_tenant_id] [nvarchar](50) NULL,
	[f_zx_system_id] [nvarchar](50) NULL,
 CONSTRAINT [PK__base_msg__2911CBED1A9AF3F3] PRIMARY KEY CLUSTERED 
(
	[f_id] ASC) ON [PRIMARY]
)
GO

-- 57. base_msg_send
IF OBJECT_ID('dbo.base_msg_send', 'U') IS NULL
CREATE TABLE [dbo].[base_msg_send](
	[f_id] [nvarchar](50) NOT NULL,
	[f_full_name] [nvarchar](200) NULL,
	[f_en_code] [nvarchar](200) NULL,
	[f_template_type] [nvarchar](50) NULL,
	[f_message_source] [nvarchar](50) NULL,
	[f_description] [nvarchar](500) NULL,
	[f_enabled_mark] [int] NULL,
	[f_sort_code] [bigint] NULL,
	[f_creator_time] [datetime] NULL,
	[f_creator_user_id] [nvarchar](50) NULL,
	[f_last_modify_time] [datetime] NULL,
	[f_last_modify_user_id] [nvarchar](50) NULL,
	[f_delete_time] [datetime] NULL,
	[f_delete_user_id] [nvarchar](50) NULL,
	[f_delete_mark] [int] NULL,
	[f_tenant_id] [nvarchar](50) NULL,
	[f_zx_system_id] [nvarchar](50) NULL,
 CONSTRAINT [PK__base_msg__2911CBED89D5D046] PRIMARY KEY CLUSTERED 
(
	[f_id] ASC) ON [PRIMARY]
)
GO

-- 58. base_msg_send_template
IF OBJECT_ID('dbo.base_msg_send_template', 'U') IS NULL
CREATE TABLE [dbo].[base_msg_send_template](
	[f_id] [nvarchar](50) NOT NULL,
	[f_send_config_id] [nvarchar](50) NULL,
	[f_message_type] [nvarchar](50) NULL,
	[f_template_id] [nvarchar](50) NULL,
	[f_account_config_id] [nvarchar](50) NULL,
	[f_description] [nvarchar](500) NULL,
	[f_enabled_mark] [int] NULL,
	[f_sort_code] [bigint] NULL,
	[f_creator_time] [datetime] NULL,
	[f_creator_user_id] [nvarchar](50) NULL,
	[f_last_modify_time] [datetime] NULL,
	[f_last_modify_user_id] [nvarchar](50) NULL,
	[f_delete_time] [datetime] NULL,
	[f_delete_user_id] [nvarchar](50) NULL,
	[f_delete_mark] [int] NULL,
	[f_tenant_id] [nvarchar](50) NULL,
	[f_zx_system_id] [nvarchar](50) NULL,
 CONSTRAINT [PK__base_msg__2911CBED31701201] PRIMARY KEY CLUSTERED 
(
	[f_id] ASC) ON [PRIMARY]
)
GO

-- 59. base_msg_short_link
IF OBJECT_ID('dbo.base_msg_short_link', 'U') IS NULL
CREATE TABLE [dbo].[base_msg_short_link](
	[f_id] [nvarchar](50) NOT NULL,
	[f_short_link] [nvarchar](200) NULL,
	[f_real_pc_link] [nvarchar](500) NULL,
	[f_real_app_link] [nvarchar](500) NULL,
	[f_body_text] [nvarchar](max) NULL,
	[f_is_used] [int] NULL,
	[f_click_num] [int] NULL,
	[f_unable_num] [int] NULL,
	[f_unable_time] [datetime] NULL,
	[f_user_id] [nvarchar](50) NULL,
	[f_enabled_mark] [int] NULL,
	[f_creator_time] [datetime] NULL,
	[f_creator_user_id] [nvarchar](50) NULL,
	[f_last_modify_time] [datetime] NULL,
	[f_last_modify_user_id] [nvarchar](50) NULL,
	[f_delete_time] [datetime] NULL,
	[f_delete_user_id] [nvarchar](50) NULL,
	[f_delete_mark] [int] NULL,
	[f_tenant_id] [nvarchar](50) NULL,
	[f_sort_code] [bigint] NULL,
	[f_zx_system_id] [nvarchar](50) NULL,
 CONSTRAINT [PK__base_msg__2911CBEDCCDFE2DE] PRIMARY KEY CLUSTERED 
(
	[f_id] ASC) ON [PRIMARY]
)
GO

-- 60. base_msg_sms_field
IF OBJECT_ID('dbo.base_msg_sms_field', 'U') IS NULL
CREATE TABLE [dbo].[base_msg_sms_field](
	[f_id] [nvarchar](50) NOT NULL,
	[f_template_id] [nvarchar](50) NULL,
	[f_field_id] [nvarchar](50) NULL,
	[f_sms_field] [nvarchar](50) NULL,
	[f_field] [nvarchar](50) NULL,
	[f_is_title] [int] NULL,
	[f_enabled_mark] [int] NULL,
	[f_creator_time] [datetime] NULL,
	[f_creator_user_id] [nvarchar](50) NULL,
	[f_last_modify_time] [datetime] NULL,
	[f_last_modify_user_id] [nvarchar](50) NULL,
	[f_delete_time] [datetime] NULL,
	[f_delete_user_id] [nvarchar](50) NULL,
	[f_delete_mark] [int] NULL,
	[f_sort_code] [bigint] NULL,
	[f_tenant_id] [nvarchar](50) NULL,
	[f_zx_system_id] [nvarchar](50) NULL,
 CONSTRAINT [PK__base_msg__2911CBED9B16E2EE] PRIMARY KEY CLUSTERED 
(
	[f_id] ASC) ON [PRIMARY]
)
GO

-- 61. base_msg_template
IF OBJECT_ID('dbo.base_msg_template', 'U') IS NULL
CREATE TABLE [dbo].[base_msg_template](
	[f_id] [nvarchar](50) NOT NULL,
	[f_full_name] [nvarchar](200) NULL,
	[f_en_code] [nvarchar](200) NULL,
	[f_template_type] [nvarchar](50) NULL,
	[f_message_source] [nvarchar](50) NULL,
	[f_message_type] [nvarchar](50) NULL,
	[f_wx_skip] [nvarchar](50) NULL,
	[f_xcx_app_id] [nvarchar](50) NULL,
	[f_title] [nvarchar](50) NULL,
	[f_content] [nvarchar](max) NULL,
	[f_template_code] [nvarchar](50) NULL,
	[f_description] [nvarchar](500) NULL,
	[f_enabled_mark] [int] NULL,
	[f_sort_code] [bigint] NULL,
	[f_creator_time] [datetime] NULL,
	[f_creator_user_id] [nvarchar](50) NULL,
	[f_last_modify_time] [datetime] NULL,
	[f_last_modify_user_id] [nvarchar](50) NULL,
	[f_delete_time] [datetime] NULL,
	[f_delete_user_id] [nvarchar](50) NULL,
	[f_delete_mark] [int] NULL,
	[f_tenant_id] [nvarchar](50) NULL,
	[f_zx_system_id] [nvarchar](50) NULL,
 CONSTRAINT [PK__base_msg__2911CBED24407668] PRIMARY KEY CLUSTERED 
(
	[f_id] ASC) ON [PRIMARY]
)
GO

-- 62. base_msg_template_param
IF OBJECT_ID('dbo.base_msg_template_param', 'U') IS NULL
CREATE TABLE [dbo].[base_msg_template_param](
	[f_id] [nvarchar](50) NOT NULL,
	[f_template_id] [nvarchar](50) NULL,
	[f_field] [nvarchar](50) NULL,
	[f_field_name] [nvarchar](50) NULL,
	[f_enabled_mark] [int] NULL,
	[f_creator_time] [datetime] NULL,
	[f_creator_user_id] [nvarchar](50) NULL,
	[f_last_modify_time] [datetime] NULL,
	[f_last_modify_user_id] [nvarchar](50) NULL,
	[f_delete_time] [datetime] NULL,
	[f_delete_user_id] [nvarchar](50) NULL,
	[f_delete_mark] [int] NULL,
	[f_sort_code] [bigint] NULL,
	[f_tenant_id] [nvarchar](50) NULL,
	[f_zx_system_id] [nvarchar](50) NULL,
 CONSTRAINT [PK__base_msg__2911CBEDC37BB936] PRIMARY KEY CLUSTERED 
(
	[f_id] ASC) ON [PRIMARY]
)
GO

-- 63. base_msg_wechat_user
IF OBJECT_ID('dbo.base_msg_wechat_user', 'U') IS NULL
CREATE TABLE [dbo].[base_msg_wechat_user](
	[f_id] [nvarchar](50) NOT NULL,
	[f_gzh_id] [nvarchar](50) NULL,
	[f_user_id] [nvarchar](50) NULL,
	[f_open_id] [nvarchar](50) NULL,
	[f_close_mark] [int] NULL,
	[f_enabled_mark] [int] NULL,
	[f_creator_time] [datetime] NULL,
	[f_creator_user_id] [nvarchar](50) NULL,
	[f_last_modify_time] [datetime] NULL,
	[f_last_modify_user_id] [nvarchar](50) NULL,
	[f_delete_time] [datetime] NULL,
	[f_delete_user_id] [nvarchar](50) NULL,
	[f_delete_mark] [int] NULL,
	[f_tenant_id] [nvarchar](50) NULL,
	[f_sort_code] [bigint] NULL,
	[f_zx_system_id] [nvarchar](50) NULL,
 CONSTRAINT [PK__base_msg__2911CBED553949E2] PRIMARY KEY CLUSTERED 
(
	[f_id] ASC) ON [PRIMARY]
)
GO

-- 64. base_notice
IF OBJECT_ID('dbo.base_notice', 'U') IS NULL
CREATE TABLE [dbo].[base_notice](
	[f_id] [nvarchar](50) NOT NULL,
	[f_title] [nvarchar](200) NULL,
	[f_body_text] [nvarchar](max) NULL,
	[f_to_user_ids] [nvarchar](max) NULL,
	[f_cover_image] [nvarchar](max) NULL,
	[f_files] [nvarchar](max) NULL,
	[f_expiration_time] [datetime] NULL,
	[f_category] [nvarchar](50) NULL,
	[f_type] [int] NULL,
	[f_send_config_id] [nvarchar](50) NULL,
	[f_description] [nvarchar](500) NULL,
	[f_enabled_mark] [int] NULL,
	[f_sort_code] [bigint] NULL,
	[f_creator_time] [datetime] NULL,
	[f_creator_user_id] [nvarchar](50) NULL,
	[f_last_modify_time] [datetime] NULL,
	[f_last_modify_user_id] [nvarchar](50) NULL,
	[f_delete_time] [datetime] NULL,
	[f_delete_user_id] [nvarchar](50) NULL,
	[f_delete_mark] [int] NULL,
	[f_tenant_id] [nvarchar](50) NULL,
	[f_zx_system_id] [nvarchar](50) NULL,
 CONSTRAINT [PK__base_not__2911CBEDA8DC3ADF] PRIMARY KEY CLUSTERED 
(
	[f_id] ASC) ON [PRIMARY]
)
GO

-- 65. base_organize
IF OBJECT_ID('dbo.base_organize', 'U') IS NULL
CREATE TABLE [dbo].[base_organize](
	[f_id] [nvarchar](50) NOT NULL,
	[f_parent_id] [nvarchar](50) NULL,
	[f_category] [nvarchar](50) NULL,
	[f_en_code] [nvarchar](200) NULL,
	[f_full_name] [nvarchar](200) NULL,
	[f_manager_id] [nvarchar](50) NULL,
	[f_property_json] [nvarchar](max) NULL,
	[f_organize_id_tree] [nvarchar](max) NULL,
	[f_description] [nvarchar](500) NULL,
	[f_enabled_mark] [int] NULL,
	[f_sort_code] [bigint] NULL,
	[f_creator_time] [datetime] NULL,
	[f_creator_user_id] [nvarchar](50) NULL,
	[f_last_modify_time] [datetime] NULL,
	[f_last_modify_user_id] [nvarchar](50) NULL,
	[f_delete_time] [datetime] NULL,
	[f_delete_user_id] [nvarchar](50) NULL,
	[f_delete_mark] [int] NULL,
	[f_tenant_id] [nvarchar](50) NULL,
	[f_zx_system_id] [nvarchar](50) NULL,
 CONSTRAINT [PK__base_org__2911CBEDECD9FDAB] PRIMARY KEY CLUSTERED 
(
	[f_id] ASC) ON [PRIMARY]
)
GO

-- 66. base_organize_administrator
IF OBJECT_ID('dbo.base_organize_administrator', 'U') IS NULL
CREATE TABLE [dbo].[base_organize_administrator](
	[f_id] [nvarchar](50) NOT NULL,
	[f_user_id] [nvarchar](50) NULL,
	[f_organize_id] [nvarchar](50) NULL,
	[f_organize_type] [nvarchar](50) NULL,
	[f_this_layer_add] [int] NULL,
	[f_this_layer_edit] [int] NULL,
	[f_this_layer_delete] [int] NULL,
	[f_sub_layer_add] [int] NULL,
	[f_sub_layer_edit] [int] NULL,
	[f_sub_layer_delete] [int] NULL,
	[f_this_layer_select] [int] NULL,
	[f_sub_layer_select] [int] NULL,
	[f_description] [nvarchar](500) NULL,
	[f_enabled_mark] [int] NULL,
	[f_sort_code] [bigint] NULL,
	[f_creator_time] [datetime] NULL,
	[f_creator_user_id] [nvarchar](50) NULL,
	[f_last_modify_time] [datetime] NULL,
	[f_last_modify_user_id] [nvarchar](50) NULL,
	[f_delete_time] [datetime] NULL,
	[f_delete_user_id] [nvarchar](50) NULL,
	[f_delete_mark] [int] NULL,
	[f_tenant_id] [nvarchar](50) NULL,
	[f_manager_group] [nvarchar](500) NULL,
	[F_ZX_SYSTEM_ID] [nvarchar](50) NULL,
 CONSTRAINT [PK__base_org__2911CBEDB73A68EC] PRIMARY KEY CLUSTERED 
(
	[f_id] ASC) ON [PRIMARY]
)
GO

-- 67. base_organize_relation
IF OBJECT_ID('dbo.base_organize_relation', 'U') IS NULL
CREATE TABLE [dbo].[base_organize_relation](
	[f_id] [nvarchar](50) NOT NULL,
	[f_organize_id] [nvarchar](50) NULL,
	[f_object_type] [nvarchar](50) NULL,
	[f_object_id] [nvarchar](50) NULL,
	[f_sort_code] [bigint] NULL,
	[f_creator_time] [datetime] NULL,
	[f_creator_user_id] [nvarchar](50) NULL,
	[f_last_modify_time] [datetime] NULL,
	[f_last_modify_user_id] [nvarchar](50) NULL,
	[f_delete_time] [datetime] NULL,
	[f_delete_user_id] [nvarchar](50) NULL,
	[f_delete_mark] [int] NULL,
	[f_tenant_id] [nvarchar](50) NULL,
	[f_zx_system_id] [nvarchar](50) NULL,
	[f_enabled_mark] [int] NULL,
 CONSTRAINT [PK__base_org__2911CBEDAA4DF795] PRIMARY KEY CLUSTERED 
(
	[f_id] ASC) ON [PRIMARY]
)
GO

-- 68. base_permission_group
IF OBJECT_ID('dbo.base_permission_group', 'U') IS NULL
CREATE TABLE [dbo].[base_permission_group](
	[F_Id] [nvarchar](50) NOT NULL,
	[F_Full_Name] [nvarchar](200) NULL,
	[F_En_Code] [nvarchar](200) NULL,
	[F_Permission_Member] [nvarchar](4000) NULL,
	[F_Sort_Code] [bigint] NULL,
	[F_Description] [nvarchar](500) NULL,
	[F_Enabled_Mark] [int] NULL,
	[F_Creator_Time] [datetime] NULL,
	[F_Creator_User_Id] [nvarchar](50) NULL,
	[F_Last_Modify_Time] [datetime] NULL,
	[F_Last_Modify_User_Id] [nvarchar](50) NULL,
	[F_Delete_Mark] [int] NULL,
	[F_Delete_Time] [datetime] NULL,
	[F_Delete_User_Id] [nvarchar](50) NULL,
	[F_Tenant_Id] [nvarchar](50) NULL,
	[f_system_id] [nvarchar](50) NULL,
 CONSTRAINT [PK__base_per__2C6EC723680E04E4] PRIMARY KEY CLUSTERED 
(
	[F_Id] ASC) ON [PRIMARY]
)
GO

-- 69. base_portal
IF OBJECT_ID('dbo.base_portal', 'U') IS NULL
CREATE TABLE [dbo].[base_portal](
	[f_id] [nvarchar](50) NOT NULL,
	[f_full_name] [nvarchar](200) NULL,
	[f_en_code] [nvarchar](200) NULL,
	[f_category] [nvarchar](50) NULL,
	[f_type] [int] NULL,
	[f_state] [int] NULL,
	[f_custom_url] [nvarchar](500) NULL,
	[f_link_type] [int] NULL,
	[f_enabled_lock] [int] NULL,
	[f_platform] [nvarchar](50) NULL,
	[f_description] [nvarchar](500) NULL,
	[f_enabled_mark] [int] NULL,
	[f_sort_code] [bigint] NULL,
	[f_creator_time] [datetime] NULL,
	[f_creator_user_id] [nvarchar](50) NULL,
	[f_last_modify_time] [datetime] NULL,
	[f_last_modify_user_id] [nvarchar](50) NULL,
	[f_delete_time] [datetime] NULL,
	[f_delete_user_id] [nvarchar](50) NULL,
	[f_delete_mark] [int] NULL,
	[f_tenant_id] [nvarchar](50) NULL,
	[f_platform_release] [nvarchar](100) NULL,
	[f_system_id] [nvarchar](50) NULL,
	[f_zx_system_id] [nvarchar](50) NULL,
 CONSTRAINT [PK__base_por__2911CBEDD966285C] PRIMARY KEY CLUSTERED 
(
	[f_id] ASC) ON [PRIMARY]
)
GO

-- 70. base_portal_data
IF OBJECT_ID('dbo.base_portal_data', 'U') IS NULL
CREATE TABLE [dbo].[base_portal_data](
	[f_id] [nvarchar](50) NOT NULL,
	[f_portal_id] [nvarchar](50) NULL,
	[f_platform] [nvarchar](50) NULL,
	[f_form_data] [nvarchar](max) NULL,
	[f_system_id] [nvarchar](50) NULL,
	[f_type] [nvarchar](50) NULL,
	[f_sort_code] [bigint] NULL,
	[f_creator_time] [datetime] NULL,
	[f_creator_user_id] [nvarchar](50) NULL,
	[f_last_modify_time] [datetime] NULL,
	[f_last_modify_user_id] [nvarchar](50) NULL,
	[f_delete_time] [datetime] NULL,
	[f_delete_user_id] [nvarchar](50) NULL,
	[f_delete_mark] [int] NULL,
	[f_tenant_id] [nvarchar](50) NULL,
	[f_zx_system_id] [nvarchar](50) NULL,
 CONSTRAINT [PK__base_por__2911CBEDB4C6D593] PRIMARY KEY CLUSTERED 
(
	[f_id] ASC) ON [PRIMARY]
)
GO

-- 71. base_portal_manage
IF OBJECT_ID('dbo.base_portal_manage', 'U') IS NULL
CREATE TABLE [dbo].[base_portal_manage](
	[f_id] [nvarchar](50) NOT NULL,
	[f_portal_id] [nvarchar](50) NOT NULL,
	[f_system_id] [nvarchar](50) NOT NULL,
	[f_platform] [nvarchar](50) NULL,
	[f_description] [nvarchar](500) NULL,
	[f_enabled_mark] [int] NULL,
	[f_sort_code] [bigint] NULL,
	[f_creator_time] [datetime] NULL,
	[f_creator_user_id] [nvarchar](50) NULL,
	[f_last_modify_time] [datetime] NULL,
	[f_last_modify_user_id] [nvarchar](50) NULL,
	[f_delete_time] [datetime] NULL,
	[f_delete_user_id] [nvarchar](50) NULL,
	[f_delete_mark] [int] NULL,
	[f_tenant_id] [nvarchar](50) NULL,
	[f_zx_system_id] [nvarchar](50) NULL,
 CONSTRAINT [PK__base_por__2911CBEDEC1239F8] PRIMARY KEY CLUSTERED 
(
	[f_id] ASC) ON [PRIMARY]
)
GO

-- 72. base_position
IF OBJECT_ID('dbo.base_position', 'U') IS NULL
CREATE TABLE [dbo].[base_position](
	[f_id] [nvarchar](50) NOT NULL,
	[f_full_name] [nvarchar](200) NULL,
	[f_en_code] [nvarchar](200) NULL,
	[f_type] [nvarchar](50) NULL,
	[f_property_json] [nvarchar](max) NULL,
	[f_description] [nvarchar](500) NULL,
	[f_enabled_mark] [int] NULL,
	[f_sort_code] [bigint] NULL,
	[f_creator_time] [datetime] NULL,
	[f_creator_user_id] [nvarchar](50) NULL,
	[f_last_modify_time] [datetime] NULL,
	[f_last_modify_user_id] [nvarchar](50) NULL,
	[f_delete_time] [datetime] NULL,
	[f_delete_user_id] [nvarchar](50) NULL,
	[f_delete_mark] [int] NULL,
	[f_organize_id] [nvarchar](50) NULL,
	[f_tenant_id] [nvarchar](50) NULL,
	[f_zx_system_id] [nvarchar](50) NULL,
 CONSTRAINT [PK__base_pos__2911CBEDF11A9A4A] PRIMARY KEY CLUSTERED 
(
	[f_id] ASC) ON [PRIMARY]
)
GO

-- 73. base_print_log
IF OBJECT_ID('dbo.base_print_log', 'U') IS NULL
CREATE TABLE [dbo].[base_print_log](
	[f_id] [nvarchar](50) NOT NULL,
	[f_print_num] [int] NULL,
	[f_print_title] [nvarchar](255) NULL,
	[f_print_id] [nvarchar](50) NULL,
	[f_sort_code] [bigint] NULL,
	[f_creator_time] [datetime] NULL,
	[f_creator_user_id] [nvarchar](50) NULL,
	[f_last_modify_time] [datetime] NULL,
	[f_last_modify_user_id] [nvarchar](50) NULL,
	[f_delete_time] [datetime] NULL,
	[f_delete_user_id] [nvarchar](50) NULL,
	[f_delete_mark] [int] NULL,
	[f_tenant_id] [nvarchar](50) NULL,
	[f_zx_system_id] [nvarchar](50) NULL,
 CONSTRAINT [PK__base_pri__2911CBED0B82DC7D] PRIMARY KEY CLUSTERED 
(
	[f_id] ASC) ON [PRIMARY]
)
GO

-- 74. base_print_template
IF OBJECT_ID('dbo.base_print_template', 'U') IS NULL
CREATE TABLE [dbo].[base_print_template](
	[f_id] [nvarchar](50) NOT NULL,
	[f_full_name] [nvarchar](200) NOT NULL,
	[f_en_code] [nvarchar](50) NOT NULL,
	[f_category] [nvarchar](50) NOT NULL,
	[f_type] [int] NOT NULL,
	[f_db_link_id] [nvarchar](50) NOT NULL,
	[f_sql_template] [nvarchar](max) NULL,
	[f_left_fields] [nvarchar](max) NULL,
	[f_print_template] [nvarchar](max) NOT NULL,
	[f_page_param] [nvarchar](max) NULL,
	[f_description] [nvarchar](500) NULL,
	[f_enabled_mark] [int] NULL,
	[f_sort_code] [bigint] NULL,
	[f_creator_time] [datetime] NULL,
	[f_creator_user_id] [nvarchar](50) NULL,
	[f_last_modify_time] [datetime] NULL,
	[f_last_modify_user_id] [nvarchar](50) NULL,
	[f_delete_time] [datetime] NULL,
	[f_delete_user_id] [nvarchar](50) NULL,
	[f_delete_mark] [int] NULL,
	[f_tenant_id] [nvarchar](50) NULL,
	[f_source_type] [int] NULL,
	[f_interface_id] [nvarchar](50) NULL,
	[f_parameter_json] [nvarchar](max) NULL,
	[f_zx_system_id] [nvarchar](50) NULL,
 CONSTRAINT [PK__base_pri__2911CBEDDF445C96] PRIMARY KEY CLUSTERED 
(
	[f_id] ASC) ON [PRIMARY]
)
GO

-- 75. base_province
IF OBJECT_ID('dbo.base_province', 'U') IS NULL
CREATE TABLE [dbo].[base_province](
	[f_id] [nvarchar](50) NOT NULL,
	[f_parent_id] [nvarchar](50) NULL,
	[f_en_code] [nvarchar](200) NULL,
	[f_full_name] [nvarchar](200) NULL,
	[f_quick_query] [nvarchar](100) NULL,
	[f_type] [nvarchar](50) NULL,
	[f_description] [nvarchar](500) NULL,
	[f_enabled_mark] [int] NULL,
	[f_sort_code] [bigint] NULL,
	[f_creator_time] [datetime] NULL,
	[f_creator_user_id] [nvarchar](50) NULL,
	[f_last_modify_time] [datetime] NULL,
	[f_last_modify_user_id] [nvarchar](50) NULL,
	[f_delete_time] [datetime] NULL,
	[f_delete_user_id] [nvarchar](50) NULL,
	[f_delete_mark] [int] NULL,
	[f_tenant_id] [nvarchar](50) NULL,
	[f_zx_system_id] [nvarchar](50) NULL,
 CONSTRAINT [PK__base_pro__2911CBEDB90A9FA6] PRIMARY KEY CLUSTERED 
(
	[f_id] ASC) ON [PRIMARY]
)
GO

-- 76. base_province_atlas
IF OBJECT_ID('dbo.base_province_atlas', 'U') IS NULL
CREATE TABLE [dbo].[base_province_atlas](
	[f_id] [nvarchar](50) NOT NULL,
	[f_parent_id] [nvarchar](50) NULL,
	[f_en_code] [nvarchar](200) NULL,
	[f_full_name] [nvarchar](200) NULL,
	[f_quick_query] [nvarchar](50) NULL,
	[f_type] [nvarchar](50) NULL,
	[f_division_code] [nvarchar](50) NULL,
	[f_atlas_center] [nvarchar](128) NULL,
	[f_description] [nvarchar](500) NULL,
	[f_enabled_mark] [int] NULL,
	[f_sort_code] [bigint] NULL,
	[f_creator_time] [datetime] NULL,
	[f_creator_user_id] [nvarchar](50) NULL,
	[f_last_modify_time] [datetime] NULL,
	[f_last_modify_user_id] [nvarchar](50) NULL,
	[f_delete_time] [datetime] NULL,
	[f_delete_user_id] [nvarchar](50) NULL,
	[f_delete_mark] [int] NULL,
	[f_tenant_id] [nvarchar](50) NULL,
	[f_zx_system_id] [nvarchar](50) NULL,
 CONSTRAINT [PK__base_pro__2911CBED843D6D35] PRIMARY KEY CLUSTERED 
(
	[f_id] ASC) ON [PRIMARY]
)
GO

-- 77. base_role
IF OBJECT_ID('dbo.base_role', 'U') IS NULL
CREATE TABLE [dbo].[base_role](
	[f_id] [nvarchar](50) NOT NULL,
	[f_full_name] [nvarchar](200) NULL,
	[f_en_code] [nvarchar](200) NULL,
	[f_type] [nvarchar](50) NULL,
	[f_property_json] [nvarchar](max) NULL,
	[f_global_mark] [int] NULL,
	[f_description] [nvarchar](500) NULL,
	[f_enabled_mark] [int] NULL,
	[f_sort_code] [bigint] NULL,
	[f_creator_time] [datetime] NULL,
	[f_creator_user_id] [nvarchar](50) NULL,
	[f_last_modify_time] [datetime] NULL,
	[f_last_modify_user_id] [nvarchar](50) NULL,
	[f_delete_time] [datetime] NULL,
	[f_delete_user_id] [nvarchar](50) NULL,
	[f_delete_mark] [int] NULL,
	[f_tenant_id] [nvarchar](50) NULL,
	[f_zx_system_id] [nvarchar](50) NULL,
 CONSTRAINT [PK__base_rol__2911CBED9C531A04] PRIMARY KEY CLUSTERED 
(
	[f_id] ASC) ON [PRIMARY]
)
GO

-- 78. BASE_SANDBOX
IF OBJECT_ID('dbo.BASE_SANDBOX', 'U') IS NULL
CREATE TABLE [dbo].[BASE_SANDBOX](
	[F_Id] [bigint] NOT NULL,
	[F_TenantId] [nvarchar](50) NOT NULL,
	[F_ProjectId] [bigint] NULL,
	[F_PipelineId] [bigint] NULL,
	[F_ContainerId] [nvarchar](100) NULL,
	[F_ContainerName] [nvarchar](100) NULL,
	[F_Status] [nvarchar](20) NOT NULL,
	[F_Url] [nvarchar](500) NULL,
	[F_SandboxAccount] [nvarchar](100) NULL,
	[F_SandboxPassword] [nvarchar](100) NULL,
	[F_CpuCount] [int] NOT NULL,
	[F_MemoryMb] [int] NOT NULL,
	[F_TimeoutSeconds] [int] NOT NULL,
	[F_DbStrategy] [nvarchar](20) NOT NULL,
	[F_DatabaseName] [nvarchar](100) NULL,
	[F_ConnectionString] [nvarchar](500) NULL,
	[F_CreatorTime] [datetime] NOT NULL,
	[F_CreatorUserId] [bigint] NULL,
	[F_StartTime] [datetime] NULL,
	[F_DestroyTime] [datetime] NULL,
	[F_ErrorMessage] [nvarchar](2000) NULL,
	[F_DeleteMark] [bit] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[F_Id] ASC) ON [PRIMARY]
)
GO

-- 79. base_schedule
IF OBJECT_ID('dbo.base_schedule', 'U') IS NULL
CREATE TABLE [dbo].[base_schedule](
	[f_id] [nvarchar](50) NOT NULL,
	[f_category] [nvarchar](50) NULL,
	[f_urgent] [int] NULL,
	[f_title] [nvarchar](500) NULL,
	[f_content] [nvarchar](max) NULL,
	[f_all_day] [int] NULL,
	[f_start_day] [datetime] NULL,
	[f_start_time] [nvarchar](50) NULL,
	[f_end_day] [datetime] NULL,
	[f_end_time] [nvarchar](50) NULL,
	[f_duration] [int] NULL,
	[f_color] [nvarchar](50) NULL,
	[f_reminder_time] [int] NULL,
	[f_reminder_type] [int] NULL,
	[f_send_config_id] [nvarchar](50) NULL,
	[f_send_config_name] [nvarchar](200) NULL,
	[f_repetition] [int] NULL,
	[f_repeat_time] [datetime] NULL,
	[f_push_time] [datetime] NULL,
	[f_group_id] [nvarchar](50) NULL,
	[f_description] [nvarchar](500) NULL,
	[f_enabled_mark] [int] NULL,
	[f_sort_code] [bigint] NULL,
	[f_creator_time] [datetime] NULL,
	[f_creator_user_id] [nvarchar](50) NULL,
	[f_last_modify_time] [datetime] NULL,
	[f_last_modify_user_id] [nvarchar](50) NULL,
	[f_delete_time] [datetime] NULL,
	[f_delete_user_id] [nvarchar](50) NULL,
	[f_delete_mark] [int] NULL,
	[f_tenant_id] [nvarchar](50) NULL,
	[f_files] [nvarchar](max) NULL,
	[f_zx_system_id] [nvarchar](50) NULL,
 CONSTRAINT [PK__base_sch__2911CBED8940EBA6] PRIMARY KEY CLUSTERED 
(
	[f_id] ASC) ON [PRIMARY]
)
GO

-- 80. base_schedule_log
IF OBJECT_ID('dbo.base_schedule_log', 'U') IS NULL
CREATE TABLE [dbo].[base_schedule_log](
	[f_id] [nvarchar](50) NOT NULL,
	[f_category] [nvarchar](50) NULL,
	[f_urgent] [int] NULL,
	[f_title] [nvarchar](500) NULL,
	[f_content] [nvarchar](max) NULL,
	[f_all_day] [int] NULL,
	[f_start_day] [datetime] NULL,
	[f_start_time] [nvarchar](50) NULL,
	[f_end_day] [datetime] NULL,
	[f_end_time] [nvarchar](50) NULL,
	[f_duration] [int] NULL,
	[f_color] [nvarchar](50) NULL,
	[f_reminder_time] [int] NULL,
	[f_reminder_type] [int] NULL,
	[f_send_config_id] [nvarchar](50) NULL,
	[f_send_config_name] [nvarchar](200) NULL,
	[f_repetition] [int] NULL,
	[f_repeat_time] [datetime] NULL,
	[f_push_time] [datetime] NULL,
	[f_group_id] [nvarchar](50) NULL,
	[f_user_id] [nvarchar](max) NULL,
	[f_schedule_id] [nvarchar](50) NULL,
	[f_operation_type] [nvarchar](1) NULL,
	[f_description] [nvarchar](500) NULL,
	[f_enabled_mark] [int] NULL,
	[f_sort_code] [bigint] NULL,
	[f_creator_time] [datetime] NULL,
	[f_creator_user_id] [nvarchar](50) NULL,
	[f_last_modify_time] [datetime] NULL,
	[f_last_modify_user_id] [nvarchar](50) NULL,
	[f_delete_time] [datetime] NULL,
	[f_delete_user_id] [nvarchar](50) NULL,
	[f_delete_mark] [int] NULL,
	[f_tenant_id] [nvarchar](50) NULL,
	[f_zx_system_id] [nvarchar](50) NULL,
 CONSTRAINT [PK__base_sch__2911CBED67069D3C] PRIMARY KEY CLUSTERED 
(
	[f_id] ASC) ON [PRIMARY]
)
GO

-- 81. base_schedule_user
IF OBJECT_ID('dbo.base_schedule_user', 'U') IS NULL
CREATE TABLE [dbo].[base_schedule_user](
	[f_id] [nvarchar](50) NOT NULL,
	[f_schedule_id] [nvarchar](50) NULL,
	[f_to_user_id] [nvarchar](50) NULL,
	[f_type] [int] NULL,
	[f_description] [nvarchar](500) NULL,
	[f_enabled_mark] [int] NULL,
	[f_sort_code] [bigint] NULL,
	[f_creator_time] [datetime] NULL,
	[f_creator_user_id] [nvarchar](50) NULL,
	[f_last_modify_time] [datetime] NULL,
	[f_last_modify_user_id] [nvarchar](50) NULL,
	[f_delete_time] [datetime] NULL,
	[f_delete_user_id] [nvarchar](50) NULL,
	[f_delete_mark] [int] NULL,
	[f_tenant_id] [nvarchar](50) NULL,
	[f_zx_system_id] [nvarchar](50) NULL,
 CONSTRAINT [PK__base_sch__2911CBEDAAC03000] PRIMARY KEY CLUSTERED 
(
	[f_id] ASC) ON [PRIMARY]
)
GO

-- 82. base_sign_img
IF OBJECT_ID('dbo.base_sign_img', 'U') IS NULL
CREATE TABLE [dbo].[base_sign_img](
	[f_id] [nvarchar](50) NOT NULL,
	[f_sign_img] [nvarchar](max) NULL,
	[f_is_default] [int] NULL,
	[f_description] [nvarchar](500) NULL,
	[f_enabled_mark] [int] NULL,
	[f_sort_code] [bigint] NULL,
	[f_creator_time] [datetime] NULL,
	[f_creator_user_id] [nvarchar](50) NULL,
	[f_last_modify_time] [datetime] NULL,
	[f_last_modify_user_id] [nvarchar](50) NULL,
	[f_delete_time] [datetime] NULL,
	[f_delete_user_id] [nvarchar](50) NULL,
	[f_delete_mark] [int] NULL,
	[f_tenant_id] [nvarchar](50) NULL,
	[f_zx_system_id] [nvarchar](50) NULL,
 CONSTRAINT [PK__base_sig__2911CBED66E6B836] PRIMARY KEY CLUSTERED 
(
	[f_id] ASC) ON [PRIMARY]
)
GO

-- 83. base_signature
IF OBJECT_ID('dbo.base_signature', 'U') IS NULL
CREATE TABLE [dbo].[base_signature](
	[f_id] [nvarchar](50) NOT NULL,
	[f_full_name] [nvarchar](200) NULL,
	[f_en_code] [nvarchar](200) NULL,
	[f_icon] [nvarchar](max) NULL,
	[f_enabled_mark] [int] NULL,
	[f_sort_code] [bigint] NULL,
	[f_description] [nvarchar](500) NULL,
	[f_creator_time] [datetime] NULL,
	[f_creator_user_id] [nvarchar](50) NULL,
	[f_last_modify_time] [datetime] NULL,
	[f_last_modify_user_id] [nvarchar](50) NULL,
	[f_delete_time] [datetime] NULL,
	[f_delete_user_id] [nvarchar](50) NULL,
	[f_delete_mark] [int] NULL,
	[f_tenant_id] [nvarchar](50) NULL,
	[f_zx_system_id] [nvarchar](50) NULL
)
GO

-- 84. base_signature_user
IF OBJECT_ID('dbo.base_signature_user', 'U') IS NULL
CREATE TABLE [dbo].[base_signature_user](
	[f_id] [nvarchar](50) NOT NULL,
	[f_signature_id] [nvarchar](50) NULL,
	[f_user_id] [nvarchar](50) NULL,
	[f_enabled_mark] [int] NULL,
	[f_sort_code] [bigint] NULL,
	[f_description] [nvarchar](500) NULL,
	[f_creator_time] [datetime] NULL,
	[f_creator_user_id] [nvarchar](50) NULL,
	[f_last_modify_time] [datetime] NULL,
	[f_last_modify_user_id] [nvarchar](50) NULL,
	[f_delete_time] [datetime] NULL,
	[f_delete_user_id] [nvarchar](50) NULL,
	[f_delete_mark] [int] NULL,
	[f_tenant_id] [nvarchar](50) NULL,
	[f_zx_system_id] [nvarchar](50) NULL
)
GO

-- 85. base_socials_users
IF OBJECT_ID('dbo.base_socials_users', 'U') IS NULL
CREATE TABLE [dbo].[base_socials_users](
	[f_id] [nvarchar](50) NOT NULL,
	[f_user_id] [nvarchar](50) NULL,
	[f_social_type] [nvarchar](50) NULL,
	[f_social_id] [nvarchar](100) NULL,
	[f_social_name] [nvarchar](100) NULL,
	[f_sort_code] [bigint] NULL,
	[f_creator_time] [datetime] NULL,
	[f_creator_user_id] [nvarchar](50) NULL,
	[f_last_modify_time] [datetime] NULL,
	[f_last_modify_user_id] [nvarchar](50) NULL,
	[f_delete_time] [datetime] NULL,
	[f_delete_user_id] [nvarchar](50) NULL,
	[f_delete_mark] [int] NULL,
	[f_tenant_id] [nvarchar](50) NULL,
	[f_zx_system_id] [nvarchar](50) NULL,
 CONSTRAINT [PK__base_soc__2911CBEDEA0F42B3] PRIMARY KEY CLUSTERED 
(
	[f_id] ASC) ON [PRIMARY]
)
GO

-- 86. BASE_STUDIO_MENU
IF OBJECT_ID('dbo.BASE_STUDIO_MENU', 'U') IS NULL
CREATE TABLE [dbo].[BASE_STUDIO_MENU](
	[F_Id] [bigint] NOT NULL,
	[F_ParentId] [bigint] NOT NULL,
	[F_Name] [nvarchar](100) NOT NULL,
	[F_Icon] [nvarchar](100) NULL,
	[F_Url] [nvarchar](500) NULL,
	[F_Sort] [int] NOT NULL,
	[F_Enabled] [bit] NOT NULL,
	[F_IsVisible] [bit] NOT NULL,
	[F_IsPublic] [bit] NOT NULL,
	[F_Comment] [nvarchar](500) NULL,
	[F_RequiredRoles] [nvarchar](500) NULL,
	[F_DataScope] [nvarchar](20) NOT NULL,
	[F_ExpandPhase] [char](1) NOT NULL,
	[F_TenantViewConfig] [nvarchar](max) NULL,
	[F_CreatorTime] [datetime] NOT NULL,
	[F_CreatorUserId] [bigint] NULL,
	[F_ModifyTime] [datetime] NULL,
	[F_ModifyUserId] [bigint] NULL,
	[F_DeleteMark] [bit] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[F_Id] ASC) ON [PRIMARY]
)
GO

-- 87. BASE_STUDIO_MENU_BAK_20260617
IF OBJECT_ID('dbo.BASE_STUDIO_MENU_BAK_20260617', 'U') IS NULL
CREATE TABLE [dbo].[BASE_STUDIO_MENU_BAK_20260617](
	[F_Id] [bigint] NOT NULL,
	[F_ParentId] [bigint] NOT NULL,
	[F_Name] [nvarchar](100) NOT NULL,
	[F_Icon] [nvarchar](100) NULL,
	[F_Url] [nvarchar](500) NULL,
	[F_Sort] [int] NOT NULL,
	[F_Enabled] [bit] NOT NULL,
	[F_IsVisible] [bit] NOT NULL,
	[F_IsPublic] [bit] NOT NULL,
	[F_Comment] [nvarchar](500) NULL,
	[F_RequiredRoles] [nvarchar](500) NULL,
	[F_DataScope] [nvarchar](20) NOT NULL,
	[F_ExpandPhase] [char](1) NOT NULL,
	[F_TenantViewConfig] [nvarchar](max) NULL,
	[F_CreatorTime] [datetime] NOT NULL,
	[F_CreatorUserId] [bigint] NULL,
	[F_ModifyTime] [datetime] NULL,
	[F_ModifyUserId] [bigint] NULL,
	[F_DeleteMark] [bit] NOT NULL
)
GO

-- 88. base_syn_third_info
IF OBJECT_ID('dbo.base_syn_third_info', 'U') IS NULL
CREATE TABLE [dbo].[base_syn_third_info](
	[f_id] [nvarchar](50) NOT NULL,
	[f_third_type] [int] NULL,
	[f_data_type] [int] NULL,
	[f_sys_obj_id] [nvarchar](50) NULL,
	[f_third_obj_id] [nvarchar](50) NULL,
	[f_description] [nvarchar](500) NULL,
	[f_enabled_mark] [int] NULL,
	[f_sort_code] [bigint] NULL,
	[f_creator_time] [datetime] NULL,
	[f_creator_user_id] [nvarchar](50) NULL,
	[f_last_modify_time] [datetime] NULL,
	[f_last_modify_user_id] [nvarchar](50) NULL,
	[f_delete_time] [datetime] NULL,
	[f_delete_user_id] [nvarchar](50) NULL,
	[f_delete_mark] [int] NULL,
	[f_tenant_id] [nvarchar](50) NULL,
	[f_zx_system_id] [nvarchar](50) NULL,
 CONSTRAINT [PK__base_syn__2911CBED953E54C1] PRIMARY KEY CLUSTERED 
(
	[f_id] ASC) ON [PRIMARY]
)
GO

-- 89. base_sys_config
IF OBJECT_ID('dbo.base_sys_config', 'U') IS NULL
CREATE TABLE [dbo].[base_sys_config](
	[f_id] [nvarchar](50) NOT NULL,
	[f_full_name] [nvarchar](50) NULL,
	[f_key] [nvarchar](50) NULL,
	[f_value] [nvarchar](max) NULL,
	[f_category] [nvarchar](50) NULL,
	[f_sort_code] [bigint] NULL,
	[f_creator_time] [datetime] NULL,
	[f_creator_user_id] [nvarchar](50) NULL,
	[f_last_modify_time] [datetime] NULL,
	[f_last_modify_user_id] [nvarchar](50) NULL,
	[f_delete_time] [datetime] NULL,
	[f_delete_user_id] [nvarchar](50) NULL,
	[f_delete_mark] [int] NULL,
	[f_tenant_id] [nvarchar](50) NULL,
	[f_zx_system_id] [nvarchar](50) NULL,
	[F_ENABLED_MARK] [int] NULL,
	[f_zx_datatype] [int] NULL,
 CONSTRAINT [PK__base_sys__2911CBED3F49ECA9] PRIMARY KEY CLUSTERED 
(
	[f_id] ASC) ON [PRIMARY]
)
GO

-- 90. base_sys_log
IF OBJECT_ID('dbo.base_sys_log', 'U') IS NULL
CREATE TABLE [dbo].[base_sys_log](
	[f_id] [nvarchar](50) NOT NULL,
	[f_user_id] [nvarchar](50) NULL,
	[f_user_name] [nvarchar](100) NULL,
	[f_type] [int] NULL,
	[f_level] [int] NULL,
	[f_ip_address] [nvarchar](50) NULL,
	[f_ip_address_name] [nvarchar](50) NULL,
	[f_request_url] [nvarchar](500) NULL,
	[f_request_method] [nvarchar](50) NULL,
	[f_request_duration] [int] NULL,
	[f_json] [nvarchar](max) NULL,
	[f_plat_form] [nvarchar](500) NULL,
	[f_creator_user_id] [nvarchar](50) NULL,
	[f_last_modify_time] [datetime] NULL,
	[f_last_modify_user_id] [nvarchar](50) NULL,
	[f_delete_time] [datetime] NULL,
	[f_delete_user_id] [nvarchar](50) NULL,
	[f_delete_mark] [int] NULL,
	[f_module_id] [nvarchar](50) NULL,
	[f_module_name] [nvarchar](50) NULL,
	[f_object_id] [nvarchar](50) NULL,
	[f_description] [nvarchar](500) NULL,
	[f_sort_code] [bigint] NULL,
	[f_creator_time] [datetime] NULL,
	[f_tenant_id] [nvarchar](50) NULL,
	[f_browser] [nvarchar](50) NULL,
	[f_request_param] [nvarchar](max) NULL,
	[f_request_target] [nvarchar](max) NULL,
	[f_login_mark] [int] NULL,
	[f_login_type] [int] NULL,
	[f_zx_system_id] [nvarchar](50) NULL,
	[F_TRACE_ID] [nvarchar](64) NULL,
 CONSTRAINT [PK__base_sys__2911CBED3C589CD7] PRIMARY KEY CLUSTERED 
(
	[f_id] ASC) ON [PRIMARY]
)
GO

-- 91. base_system
IF OBJECT_ID('dbo.base_system', 'U') IS NULL
CREATE TABLE [dbo].[base_system](
	[f_id] [nvarchar](50) NOT NULL,
	[f_full_name] [nvarchar](200) NULL,
	[f_en_code] [nvarchar](200) NULL,
	[f_icon] [nvarchar](200) NULL,
	[f_is_main] [int] NULL,
	[f_property_json] [nvarchar](max) NULL,
	[f_description] [nvarchar](500) NULL,
	[f_enabled_mark] [int] NULL,
	[f_sort_code] [bigint] NULL,
	[f_creator_time] [datetime] NULL,
	[f_creator_user_id] [nvarchar](50) NULL,
	[f_last_modify_time] [datetime] NULL,
	[f_last_modify_user_id] [nvarchar](50) NULL,
	[f_delete_time] [datetime] NULL,
	[f_delete_user_id] [nvarchar](50) NULL,
	[f_delete_mark] [int] NULL,
	[f_tenant_id] [nvarchar](50) NULL,
	[f_navigation_icon] [nvarchar](500) NULL,
	[f_work_logo_icon] [nvarchar](500) NULL,
	[f_workflow_enabled] [int] NULL,
	[f_zx_system_id] [nvarchar](50) NULL,
	[f_inte_assistant] [int] NULL,
	[f_system_api] [varchar](255) NULL,
 CONSTRAINT [PK__base_sys__2911CBED22F7044B] PRIMARY KEY CLUSTERED 
(
	[f_id] ASC) ON [PRIMARY]
)
GO

-- 92. BASE_TENANT_GLOSSARY
IF OBJECT_ID('dbo.BASE_TENANT_GLOSSARY', 'U') IS NULL
CREATE TABLE [dbo].[BASE_TENANT_GLOSSARY](
	[F_Id] [bigint] NOT NULL,
	[F_TenantId] [nvarchar](50) NOT NULL,
	[F_Term] [nvarchar](200) NOT NULL,
	[F_Definition] [nvarchar](2000) NOT NULL,
	[F_Synonyms] [nvarchar](500) NULL,
	[F_Category] [nvarchar](100) NULL,
	[F_Example] [nvarchar](1000) NULL,
	[F_Enabled] [bit] NOT NULL,
	[F_CreatorTime] [datetime] NOT NULL,
	[F_CreatorUserId] [bigint] NULL,
	[F_ModifyTime] [datetime] NULL,
	[F_ModifyUserId] [bigint] NULL,
	[F_DeleteMark] [bit] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[F_Id] ASC) ON [PRIMARY],
 CONSTRAINT [UQ_TENANT_TERM] UNIQUE NONCLUSTERED 
(
	[F_TenantId] ASC,
	[F_Term] ASC) ON [PRIMARY]
)
GO

-- 93. BASE_TENANT_INDUSTRY
IF OBJECT_ID('dbo.BASE_TENANT_INDUSTRY', 'U') IS NULL
CREATE TABLE [dbo].[BASE_TENANT_INDUSTRY](
	[F_Id] [bigint] NOT NULL,
	[F_TenantId] [nvarchar](50) NOT NULL,
	[F_IndustryName] [nvarchar](200) NOT NULL,
	[F_Description] [nvarchar](max) NULL,
	[F_KeyScenarios] [nvarchar](max) NULL,
	[F_SystemPrompt] [nvarchar](max) NULL,
	[F_Enabled] [bit] NOT NULL,
	[F_CreatorTime] [datetime] NOT NULL,
	[F_CreatorUserId] [bigint] NULL,
	[F_ModifyTime] [datetime] NULL,
	[F_ModifyUserId] [bigint] NULL,
	[F_DeleteMark] [bit] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[F_Id] ASC) ON [PRIMARY],
 CONSTRAINT [UQ_TENANT_INDUSTRY] UNIQUE NONCLUSTERED 
(
	[F_TenantId] ASC) ON [PRIMARY]
)
GO

-- 94. base_time_task
IF OBJECT_ID('dbo.base_time_task', 'U') IS NULL
CREATE TABLE [dbo].[base_time_task](
	[f_id] [nvarchar](50) NOT NULL,
	[f_en_code] [nvarchar](200) NULL,
	[f_full_name] [nvarchar](200) NULL,
	[f_execute_type] [int] NULL,
	[f_execute_content] [nvarchar](max) NULL,
	[f_execute_cycle_json] [nvarchar](max) NULL,
	[f_last_run_time] [datetime] NULL,
	[f_next_run_time] [datetime] NULL,
	[f_run_count] [int] NULL,
	[f_description] [nvarchar](500) NULL,
	[f_enabled_mark] [int] NULL,
	[f_sort_code] [bigint] NULL,
	[f_creator_time] [datetime] NULL,
	[f_creator_user_id] [nvarchar](50) NULL,
	[f_last_modify_time] [datetime] NULL,
	[f_last_modify_user_id] [nvarchar](50) NULL,
	[f_delete_time] [datetime] NULL,
	[f_delete_user_id] [nvarchar](50) NULL,
	[f_delete_mark] [int] NULL,
	[f_tenant_id] [nvarchar](50) NULL,
	[f_zx_system_id] [nvarchar](50) NULL,
 CONSTRAINT [PK__base_tim__2911CBED2EF9E67A] PRIMARY KEY CLUSTERED 
(
	[f_id] ASC) ON [PRIMARY]
)
GO

-- 95. base_time_task_log
IF OBJECT_ID('dbo.base_time_task_log', 'U') IS NULL
CREATE TABLE [dbo].[base_time_task_log](
	[f_id] [nvarchar](50) NOT NULL,
	[f_task_id] [nvarchar](50) NULL,
	[f_run_time] [datetime] NULL,
	[f_run_result] [int] NULL,
	[f_description] [nvarchar](500) NULL,
	[f_enabled_mark] [int] NULL,
	[f_sort_code] [bigint] NULL,
	[f_creator_time] [datetime] NULL,
	[f_creator_user_id] [nvarchar](50) NULL,
	[f_last_modify_time] [datetime] NULL,
	[f_last_modify_user_id] [nvarchar](50) NULL,
	[f_delete_time] [datetime] NULL,
	[f_delete_user_id] [nvarchar](50) NULL,
	[f_delete_mark] [int] NULL,
	[f_tenant_id] [nvarchar](50) NULL,
	[f_zx_system_id] [nvarchar](50) NULL,
 CONSTRAINT [PK__base_tim__2911CBED9958089B] PRIMARY KEY CLUSTERED 
(
	[f_id] ASC) ON [PRIMARY]
)
GO

-- 96. base_user
IF OBJECT_ID('dbo.base_user', 'U') IS NULL
CREATE TABLE [dbo].[base_user](
	[f_id] [nvarchar](50) NOT NULL,
	[f_account] [nvarchar](50) NULL,
	[f_real_name] [nvarchar](50) NULL,
	[f_quick_query] [nvarchar](100) NULL,
	[f_nick_name] [nvarchar](50) NULL,
	[f_head_icon] [nvarchar](max) NULL,
	[f_gender] [int] NULL,
	[f_birthday] [datetime] NULL,
	[f_mobile_phone] [nvarchar](20) NULL,
	[f_tele_phone] [nvarchar](20) NULL,
	[f_landline] [nvarchar](50) NULL,
	[f_email] [nvarchar](50) NULL,
	[f_nation] [nvarchar](50) NULL,
	[f_native_place] [nvarchar](50) NULL,
	[f_entry_date] [datetime] NULL,
	[f_certificates_type] [nvarchar](50) NULL,
	[f_certificates_number] [nvarchar](50) NULL,
	[f_education] [nvarchar](50) NULL,
	[f_urgent_contacts] [nvarchar](50) NULL,
	[f_urgent_tele_phone] [nvarchar](50) NULL,
	[f_postal_address] [nvarchar](500) NULL,
	[f_signature] [nvarchar](500) NULL,
	[f_password] [nvarchar](255) NULL,
	[f_secretkey] [nvarchar](50) NULL,
	[f_first_log_time] [datetime] NULL,
	[f_first_log_ip] [nvarchar](50) NULL,
	[f_prev_log_time] [datetime] NULL,
	[f_prev_log_ip] [nvarchar](50) NULL,
	[f_last_log_time] [datetime] NULL,
	[f_last_log_ip] [nvarchar](50) NULL,
	[f_log_success_count] [int] NULL,
	[f_log_error_count] [int] NULL,
	[f_change_password_date] [datetime] NULL,
	[f_language] [nvarchar](50) NULL,
	[f_theme] [nvarchar](50) NULL,
	[f_common_menu] [nvarchar](max) NULL,
	[f_is_administrator] [int] NULL,
	[f_property_json] [nvarchar](max) NULL,
	[f_manager_id] [nvarchar](50) NULL,
	[f_organize_id] [nvarchar](50) NULL,
	[f_position_id] [nvarchar](50) NULL,
	[f_role_id] [nvarchar](max) NULL,
	[f_portal_id] [nvarchar](max) NULL,
	[f_lock_mark] [int] NULL,
	[f_unlock_time] [datetime] NULL,
	[f_group_id] [nvarchar](50) NULL,
	[f_system_id] [nvarchar](50) NULL,
	[f_handover_mark] [int] NULL,
	[f_app_system_id] [nvarchar](50) NULL,
	[f_ding_job_number] [nvarchar](50) NULL,
	[f_description] [nvarchar](500) NULL,
	[f_enabled_mark] [int] NULL,
	[f_sort_code] [bigint] NULL,
	[f_creator_time] [datetime] NULL,
	[f_creator_user_id] [nvarchar](50) NULL,
	[f_last_modify_time] [datetime] NULL,
	[f_last_modify_user_id] [nvarchar](50) NULL,
	[f_delete_time] [datetime] NULL,
	[f_delete_user_id] [nvarchar](50) NULL,
	[f_delete_mark] [int] NULL,
	[f_tenant_id] [nvarchar](50) NULL,
	[f_handover_userid] [nvarchar](100) NULL,
	[f_rank] [nvarchar](50) NULL,
	[f_openId] [varchar](50) NULL,
	[f_is_dev] [int] NULL,
	[f_biz_system_Id] [nvarchar](50) NULL,
	[f_zx_system_id] [nvarchar](50) NULL,
	[f_inte_assistant] [int] NULL,
 CONSTRAINT [PK__base_use__2911CBED65098DC1] PRIMARY KEY CLUSTERED 
(
	[f_id] ASC) ON [PRIMARY]
)
GO

-- 97. base_user_device
IF OBJECT_ID('dbo.base_user_device', 'U') IS NULL
CREATE TABLE [dbo].[base_user_device](
	[f_id] [nvarchar](50) NOT NULL,
	[f_client_id] [nvarchar](50) NULL,
	[f_user_id] [nvarchar](50) NULL,
	[f_enabled_mark] [int] NULL,
	[f_sort_code] [bigint] NULL,
	[f_creator_time] [datetime] NULL,
	[f_creator_user_id] [nvarchar](50) NULL,
	[f_last_modify_time] [datetime] NULL,
	[f_last_modify_user_id] [nvarchar](50) NULL,
	[f_delete_time] [datetime] NULL,
	[f_delete_user_id] [nvarchar](50) NULL,
	[f_delete_mark] [int] NULL,
	[f_tenant_id] [nvarchar](50) NULL,
	[f_zx_system_id] [nvarchar](50) NULL,
 CONSTRAINT [PK__base_use__2911CBEDAD4171B6] PRIMARY KEY CLUSTERED 
(
	[f_id] ASC) ON [PRIMARY]
)
GO

-- 98. base_user_old_password
IF OBJECT_ID('dbo.base_user_old_password', 'U') IS NULL
CREATE TABLE [dbo].[base_user_old_password](
	[f_id] [nvarchar](50) NOT NULL,
	[f_user_id] [nvarchar](50) NULL,
	[f_account] [nvarchar](50) NULL,
	[f_old_password] [nvarchar](50) NULL,
	[f_secretkey] [nvarchar](50) NULL,
	[f_sort_code] [bigint] NULL,
	[f_creator_time] [datetime] NULL,
	[f_creator_user_id] [nvarchar](50) NULL,
	[f_last_modify_time] [datetime] NULL,
	[f_last_modify_user_id] [nvarchar](50) NULL,
	[f_delete_time] [datetime] NULL,
	[f_delete_user_id] [nvarchar](50) NULL,
	[f_delete_mark] [int] NULL,
	[f_tenant_id] [nvarchar](50) NULL,
	[f_zx_system_id] [nvarchar](50) NULL,
 CONSTRAINT [PK__base_use__2911CBEDDD6A83EA] PRIMARY KEY CLUSTERED 
(
	[f_id] ASC) ON [PRIMARY]
)
GO

-- 99. base_user_relation
IF OBJECT_ID('dbo.base_user_relation', 'U') IS NULL
CREATE TABLE [dbo].[base_user_relation](
	[f_id] [nvarchar](50) NOT NULL,
	[f_user_id] [nvarchar](50) NULL,
	[f_object_type] [nvarchar](50) NULL,
	[f_object_id] [nvarchar](50) NULL,
	[f_sort_code] [bigint] NULL,
	[f_creator_time] [datetime] NULL,
	[f_creator_user_id] [nvarchar](50) NULL,
	[f_last_modify_time] [datetime] NULL,
	[f_last_modify_user_id] [nvarchar](50) NULL,
	[f_delete_time] [datetime] NULL,
	[f_delete_user_id] [nvarchar](50) NULL,
	[f_delete_mark] [int] NULL,
	[f_tenant_id] [nvarchar](50) NULL,
	[f_zx_system_id] [nvarchar](50) NULL,
	[f_enabled_mark] [int] NULL,
 CONSTRAINT [PK__base_use__2911CBED183DBB0A] PRIMARY KEY CLUSTERED 
(
	[f_id] ASC) ON [PRIMARY]
)
GO

-- 100. base_visual_dev
IF OBJECT_ID('dbo.base_visual_dev', 'U') IS NULL
CREATE TABLE [dbo].[base_visual_dev](
	[f_id] [nvarchar](50) NOT NULL,
	[f_full_name] [nvarchar](200) NULL,
	[f_en_code] [nvarchar](200) NULL,
	[f_state] [int] NULL,
	[f_type] [int] NULL,
	[f_tables_data] [nvarchar](max) NULL,
	[f_category] [nvarchar](50) NULL,
	[f_form_data] [nvarchar](max) NULL,
	[f_column_data] [nvarchar](max) NULL,
	[f_db_link_id] [nvarchar](50) NULL,
	[f_web_type] [int] NULL,
	[f_flow_id] [nvarchar](50) NULL,
	[f_app_column_data] [nvarchar](max) NULL,
	[f_enable_flow] [int] NULL,
	[f_interface_id] [nvarchar](50) NULL,
	[f_interface_param] [nvarchar](max) NULL,
	[f_description] [nvarchar](500) NULL,
	[f_enabled_mark] [int] NULL,
	[f_sort_code] [bigint] NULL,
	[f_creator_time] [datetime] NULL,
	[f_creator_user_id] [nvarchar](50) NULL,
	[f_last_modify_time] [datetime] NULL,
	[f_last_modify_user_id] [nvarchar](50) NULL,
	[f_delete_time] [datetime] NULL,
	[f_delete_user_id] [nvarchar](50) NULL,
	[f_delete_mark] [int] NULL,
	[f_tenant_id] [nvarchar](50) NULL,
	[f_parent_id] [nvarchar](50) NULL,
	[f_platform_release] [nvarchar](100) NULL,
	[f_zx_system_id] [nvarchar](50) NULL,
 CONSTRAINT [PK__base_vis__2911CBED76B98712] PRIMARY KEY CLUSTERED 
(
	[f_id] ASC) ON [PRIMARY]
)
GO

-- 101. base_visual_filter
IF OBJECT_ID('dbo.base_visual_filter', 'U') IS NULL
CREATE TABLE [dbo].[base_visual_filter](
	[f_id] [nvarchar](50) NOT NULL,
	[f_module_id] [nvarchar](50) NULL,
	[f_config] [nvarchar](max) NULL,
	[f_config_app] [nvarchar](max) NULL,
	[f_sort_code] [bigint] NULL,
	[f_creator_time] [datetime] NULL,
	[f_creator_user_id] [nvarchar](50) NULL,
	[f_last_modify_time] [datetime] NULL,
	[f_last_modify_user_id] [nvarchar](50) NULL,
	[f_delete_time] [datetime] NULL,
	[f_delete_user_id] [nvarchar](50) NULL,
	[f_delete_mark] [int] NULL,
	[f_tenant_id] [nvarchar](50) NULL,
	[f_zx_system_id] [nvarchar](50) NULL,
 CONSTRAINT [PK__base_vis__2911CBED9CFC0F27] PRIMARY KEY CLUSTERED 
(
	[f_id] ASC) ON [PRIMARY]
)
GO

-- 102. base_visual_link
IF OBJECT_ID('dbo.base_visual_link', 'U') IS NULL
CREATE TABLE [dbo].[base_visual_link](
	[f_id] [nvarchar](50) NOT NULL,
	[f_short_link] [nvarchar](500) NULL,
	[f_form_use] [int] NULL,
	[f_form_link] [nvarchar](500) NULL,
	[f_form_pass_use] [int] NULL,
	[f_form_password] [nvarchar](500) NULL,
	[f_column_use] [int] NULL,
	[f_column_link] [nvarchar](500) NULL,
	[f_column_pass_use] [int] NULL,
	[f_column_password] [nvarchar](500) NULL,
	[f_column_condition] [nvarchar](max) NULL,
	[f_column_text] [nvarchar](max) NULL,
	[f_real_pc_link] [nvarchar](500) NULL,
	[f_real_app_link] [nvarchar](500) NULL,
	[f_user_id] [nvarchar](50) NULL,
	[f_enabled_mark] [int] NULL,
	[f_creator_time] [datetime] NULL,
	[f_creator_user_id] [nvarchar](50) NULL,
	[f_last_modify_time] [datetime] NULL,
	[f_last_modify_user_id] [nvarchar](50) NULL,
	[f_delete_time] [datetime] NULL,
	[f_delete_user_id] [nvarchar](50) NULL,
	[f_delete_mark] [int] NULL,
	[f_tenant_id] [nvarchar](50) NULL,
	[f_sort_code] [bigint] NULL,
	[f_zx_system_id] [nvarchar](50) NULL,
 CONSTRAINT [PK__base_vis__2911CBEDE62DF985] PRIMARY KEY CLUSTERED 
(
	[f_id] ASC) ON [PRIMARY]
)
GO

-- 103. base_visual_release
IF OBJECT_ID('dbo.base_visual_release', 'U') IS NULL
CREATE TABLE [dbo].[base_visual_release](
	[f_id] [nvarchar](50) NOT NULL,
	[f_full_name] [nvarchar](200) NULL,
	[f_en_code] [nvarchar](200) NULL,
	[f_state] [int] NULL,
	[f_type] [int] NOT NULL,
	[f_tables_data] [nvarchar](max) NULL,
	[f_category] [nvarchar](50) NULL,
	[f_form_data] [nvarchar](max) NULL,
	[f_column_data] [nvarchar](max) NULL,
	[f_db_link_id] [nvarchar](50) NULL,
	[f_web_type] [int] NULL,
	[f_flow_id] [nvarchar](50) NULL,
	[f_app_column_data] [nvarchar](max) NULL,
	[f_enable_flow] [int] NULL,
	[f_interface_id] [nvarchar](50) NULL,
	[f_interface_param] [nvarchar](max) NULL,
	[f_description] [nvarchar](500) NULL,
	[f_enabled_mark] [int] NULL,
	[f_sort_code] [bigint] NULL,
	[f_creator_time] [datetime] NULL,
	[f_creator_user_id] [nvarchar](50) NULL,
	[f_last_modify_time] [datetime] NULL,
	[f_last_modify_user_id] [nvarchar](50) NULL,
	[f_delete_time] [datetime] NULL,
	[f_delete_user_id] [nvarchar](50) NULL,
	[f_delete_mark] [int] NULL,
	[f_tenant_id] [nvarchar](50) NULL,
	[f_inte_assistant] [int] NULL,
	[f_zx_system_id] [nvarchar](50) NULL,
 CONSTRAINT [PK__base_vis__2911CBEDD94455AE] PRIMARY KEY CLUSTERED 
(
	[f_id] ASC) ON [PRIMARY]
)
GO

-- 104. EVAL_METRIC
IF OBJECT_ID('dbo.EVAL_METRIC', 'U') IS NULL
CREATE TABLE [dbo].[EVAL_METRIC](
	[F_ID] [bigint] NOT NULL,
	[F_METRIC_CODE] [nvarchar](50) NOT NULL,
	[F_METRIC_NAME] [nvarchar](100) NOT NULL,
	[F_METRIC_TYPE] [nvarchar](20) NOT NULL,
	[F_THRESHOLD_WARN] [decimal](10, 4) NULL,
	[F_THRESHOLD_CRIT] [decimal](10, 4) NULL,
	[F_UNIT] [nvarchar](20) NULL,
	[F_DESCRIPTION] [nvarchar](500) NULL,
	[F_TENANT_ID] [nvarchar](50) NOT NULL,
	[F_CREATOR_USER_ID] [nvarchar](50) NULL,
	[F_CREATOR_TIME] [datetime] NULL,
	[F_LAST_MODIFY_USER_ID] [nvarchar](50) NULL,
	[F_LAST_MODIFY_TIME] [datetime] NULL,
PRIMARY KEY CLUSTERED 
(
	[F_ID] ASC) ON [PRIMARY]
)
GO

-- 105. flow_candidates
IF OBJECT_ID('dbo.flow_candidates', 'U') IS NULL
CREATE TABLE [dbo].[flow_candidates](
	[f_id] [nvarchar](50) NOT NULL,
	[f_task_node_id] [nvarchar](50) NULL,
	[f_task_id] [nvarchar](50) NULL,
	[f_handle_id] [nvarchar](50) NULL,
	[f_account] [nvarchar](50) NULL,
	[f_candidates] [nvarchar](max) NULL,
	[f_task_operator_id] [nvarchar](50) NULL,
	[f_type] [int] NULL,
	[f_sort_code] [bigint] NULL,
	[f_creator_time] [datetime] NULL,
	[f_creator_user_id] [nvarchar](50) NULL,
	[f_last_modify_time] [datetime] NULL,
	[f_last_modify_user_id] [nvarchar](50) NULL,
	[f_delete_time] [datetime] NULL,
	[f_delete_user_id] [nvarchar](50) NULL,
	[f_delete_mark] [int] NULL,
	[f_tenant_id] [nvarchar](50) NULL,
	[f_zx_system_id] [nvarchar](50) NULL,
 CONSTRAINT [PK__flow_can__2911CBED07B01EAD] PRIMARY KEY CLUSTERED 
(
	[f_id] ASC) ON [PRIMARY]
)
GO

-- 106. flow_comment
IF OBJECT_ID('dbo.flow_comment', 'U') IS NULL
CREATE TABLE [dbo].[flow_comment](
	[f_id] [nvarchar](50) NOT NULL,
	[f_task_id] [nvarchar](50) NULL,
	[f_text] [nvarchar](max) NULL,
	[f_image] [nvarchar](max) NULL,
	[f_file] [nvarchar](max) NULL,
	[f_enabled_mark] [int] NULL,
	[f_sort_code] [bigint] NULL,
	[f_creator_time] [datetime] NULL,
	[f_creator_user_id] [nvarchar](50) NULL,
	[f_last_modify_time] [datetime] NULL,
	[f_last_modify_user_id] [nvarchar](50) NULL,
	[f_delete_mark] [int] NULL,
	[f_delete_time] [datetime] NULL,
	[f_delete_user_id] [nvarchar](50) NULL,
	[f_tenant_id] [nvarchar](50) NULL,
	[f_zx_system_id] [nvarchar](50) NULL,
 CONSTRAINT [PK__flow_com__2911CBED046BB804] PRIMARY KEY CLUSTERED 
(
	[f_id] ASC) ON [PRIMARY]
)
GO

-- 107. flow_delegate
IF OBJECT_ID('dbo.flow_delegate', 'U') IS NULL
CREATE TABLE [dbo].[flow_delegate](
	[f_id] [nvarchar](50) NOT NULL,
	[f_to_user_id] [nvarchar](50) NULL,
	[f_to_user_name] [nvarchar](50) NULL,
	[f_flow_id] [nvarchar](4000) NULL,
	[f_flow_name] [nvarchar](4000) NULL,
	[f_flow_category] [nvarchar](50) NULL,
	[f_start_time] [datetime] NULL,
	[f_end_time] [datetime] NULL,
	[f_user_id] [nvarchar](50) NULL,
	[f_user_name] [nvarchar](50) NULL,
	[f_type] [int] NULL,
	[f_description] [nvarchar](500) NULL,
	[f_enabled_mark] [int] NULL,
	[f_sort_code] [bigint] NULL,
	[f_creator_time] [datetime] NULL,
	[f_creator_user_id] [nvarchar](50) NULL,
	[f_last_modify_time] [datetime] NULL,
	[f_last_modify_user_id] [nvarchar](50) NULL,
	[f_delete_time] [datetime] NULL,
	[f_delete_user_id] [nvarchar](50) NULL,
	[f_delete_mark] [int] NULL,
	[f_tenant_id] [nvarchar](50) NULL,
	[f_zx_system_id] [nvarchar](50) NULL,
 CONSTRAINT [PK__flow_del__2911CBEDCFF80960] PRIMARY KEY CLUSTERED 
(
	[f_id] ASC) ON [PRIMARY]
)
GO

-- 108. flow_event_log
IF OBJECT_ID('dbo.flow_event_log', 'U') IS NULL
CREATE TABLE [dbo].[flow_event_log](
	[f_id] [nvarchar](50) NOT NULL,
	[f_task_node_id] [nvarchar](50) NULL,
	[f_full_name] [nvarchar](200) NULL,
	[f_interface_id] [nvarchar](50) NULL,
	[f_result] [nvarchar](max) NULL,
	[f_sort_code] [bigint] NULL,
	[f_creator_time] [datetime] NULL,
	[f_creator_user_id] [nvarchar](50) NULL,
	[f_last_modify_time] [datetime] NULL,
	[f_last_modify_user_id] [nvarchar](50) NULL,
	[f_delete_time] [datetime] NULL,
	[f_delete_user_id] [nvarchar](50) NULL,
	[f_delete_mark] [int] NULL,
	[f_tenant_id] [nvarchar](50) NULL,
	[f_zx_system_id] [nvarchar](50) NULL,
 CONSTRAINT [PK__flow_eve__2911CBED840E7E4A] PRIMARY KEY CLUSTERED 
(
	[f_id] ASC) ON [PRIMARY]
)
GO

-- 109. flow_form
IF OBJECT_ID('dbo.flow_form', 'U') IS NULL
CREATE TABLE [dbo].[flow_form](
	[f_id] [nvarchar](50) NOT NULL,
	[f_en_code] [nvarchar](200) NULL,
	[f_state] [int] NULL,
	[f_full_name] [nvarchar](200) NULL,
	[f_category] [nvarchar](50) NULL,
	[f_url_address] [nvarchar](500) NULL,
	[f_app_url_address] [nvarchar](500) NULL,
	[f_property_json] [nvarchar](max) NULL,
	[f_flow_type] [int] NULL,
	[f_form_type] [int] NULL,
	[f_interface_url] [nvarchar](500) NULL,
	[f_draft_json] [nvarchar](max) NULL,
	[f_db_link_id] [nvarchar](50) NULL,
	[f_table_json] [nvarchar](max) NULL,
	[f_flow_id] [nvarchar](50) NULL,
	[f_description] [nvarchar](500) NULL,
	[f_enabled_mark] [int] NULL,
	[f_sort_code] [bigint] NULL,
	[f_creator_time] [datetime] NULL,
	[f_creator_user_id] [nvarchar](50) NULL,
	[f_last_modify_time] [datetime] NULL,
	[f_last_modify_user_id] [nvarchar](50) NULL,
	[f_delete_time] [datetime] NULL,
	[f_delete_user_id] [nvarchar](50) NULL,
	[f_delete_mark] [int] NULL,
	[f_tenant_id] [nvarchar](50) NULL,
	[f_zx_system_id] [nvarchar](50) NULL,
 CONSTRAINT [PK__flow_for__2911CBED3737ED19] PRIMARY KEY CLUSTERED 
(
	[f_id] ASC) ON [PRIMARY]
)
GO

-- 110. flow_form_authorize
IF OBJECT_ID('dbo.flow_form_authorize', 'U') IS NULL
CREATE TABLE [dbo].[flow_form_authorize](
	[f_id] [nvarchar](50) NOT NULL,
	[f_task_id] [nvarchar](50) NULL,
	[f_node_code] [nvarchar](50) NULL,
	[f_form_operate] [nvarchar](max) NULL,
	[f_sort_code] [bigint] NULL,
	[f_creator_time] [datetime] NULL,
	[f_creator_user_id] [nvarchar](50) NULL,
	[f_last_modify_time] [datetime] NULL,
	[f_last_modify_user_id] [nvarchar](50) NULL,
	[f_delete_time] [datetime] NULL,
	[f_delete_user_id] [nvarchar](50) NULL,
	[f_delete_mark] [int] NULL,
	[f_tenant_id] [nvarchar](50) NULL,
	[f_zx_system_id] [nvarchar](50) NULL
)
GO

-- 111. flow_form_relation
IF OBJECT_ID('dbo.flow_form_relation', 'U') IS NULL
CREATE TABLE [dbo].[flow_form_relation](
	[f_id] [nvarchar](50) NOT NULL,
	[f_flow_id] [nvarchar](50) NULL,
	[f_form_id] [nvarchar](50) NULL,
	[f_sort_code] [bigint] NULL,
	[f_creator_time] [datetime] NULL,
	[f_creator_user_id] [nvarchar](50) NULL,
	[f_last_modify_time] [datetime] NULL,
	[f_last_modify_user_id] [nvarchar](50) NULL,
	[f_delete_time] [datetime] NULL,
	[f_delete_user_id] [nvarchar](50) NULL,
	[f_delete_mark] [int] NULL,
	[f_tenant_id] [nvarchar](50) NULL,
	[f_zx_system_id] [nvarchar](50) NULL,
 CONSTRAINT [PK__flow_for__2911CBED9E3A2070] PRIMARY KEY CLUSTERED 
(
	[f_id] ASC) ON [PRIMARY]
)
GO

-- 112. flow_launch_user
IF OBJECT_ID('dbo.flow_launch_user', 'U') IS NULL
CREATE TABLE [dbo].[flow_launch_user](
	[f_id] [nvarchar](50) NOT NULL,
	[f_organize_id] [nvarchar](max) NULL,
	[f_position_id] [nvarchar](max) NULL,
	[f_manager_id] [nvarchar](50) NULL,
	[f_superior] [nvarchar](50) NULL,
	[f_subordinate] [nvarchar](max) NULL,
	[f_task_id] [nvarchar](50) NULL,
	[f_department] [nvarchar](max) NULL,
	[f_sort_code] [bigint] NULL,
	[f_creator_time] [datetime] NULL,
	[f_creator_user_id] [nvarchar](50) NULL,
	[f_last_modify_time] [datetime] NULL,
	[f_last_modify_user_id] [nvarchar](50) NULL,
	[f_delete_time] [datetime] NULL,
	[f_delete_user_id] [nvarchar](50) NULL,
	[f_delete_mark] [int] NULL,
	[f_tenant_id] [nvarchar](50) NULL,
	[f_zx_system_id] [nvarchar](50) NULL,
 CONSTRAINT [PK__flow_lau__2911CBED935FFF49] PRIMARY KEY CLUSTERED 
(
	[f_id] ASC) ON [PRIMARY]
)
GO

-- 113. flow_reject_data
IF OBJECT_ID('dbo.flow_reject_data', 'U') IS NULL
CREATE TABLE [dbo].[flow_reject_data](
	[f_id] [nvarchar](50) NOT NULL,
	[f_task_json] [nvarchar](max) NULL,
	[f_task_node_json] [nvarchar](max) NULL,
	[f_task_operator_json] [nvarchar](max) NULL,
	[f_sort_code] [bigint] NULL,
	[f_creator_time] [datetime] NULL,
	[f_creator_user_id] [nvarchar](50) NULL,
	[f_last_modify_time] [datetime] NULL,
	[f_last_modify_user_id] [nvarchar](50) NULL,
	[f_delete_time] [datetime] NULL,
	[f_delete_user_id] [nvarchar](50) NULL,
	[f_delete_mark] [int] NULL,
	[f_tenant_id] [nvarchar](50) NULL,
	[f_zx_system_id] [nvarchar](50) NULL,
 CONSTRAINT [PK__flow_rej__2911CBED06C10EDC] PRIMARY KEY CLUSTERED 
(
	[f_id] ASC) ON [PRIMARY]
)
GO

-- 114. flow_task
IF OBJECT_ID('dbo.flow_task', 'U') IS NULL
CREATE TABLE [dbo].[flow_task](
	[f_id] [nvarchar](50) NOT NULL,
	[f_process_id] [nvarchar](50) NULL,
	[f_en_code] [nvarchar](200) NULL,
	[f_full_name] [nvarchar](200) NULL,
	[f_flow_urgent] [int] NULL,
	[f_flow_id] [nvarchar](50) NULL,
	[f_flow_code] [nvarchar](50) NULL,
	[f_flow_name] [nvarchar](50) NULL,
	[f_flow_type] [int] NULL,
	[f_flow_version] [nvarchar](50) NULL,
	[f_flow_category] [nvarchar](50) NULL,
	[f_flow_form_data_json] [nvarchar](max) NULL,
	[f_flow_template_json] [nvarchar](max) NULL,
	[f_start_time] [datetime] NULL,
	[f_end_time] [datetime] NULL,
	[f_current_node_code] [nvarchar](2000) NULL,
	[f_current_node_name] [nvarchar](2000) NULL,
	[f_status] [int] NULL,
	[f_completion] [int] NULL,
	[f_parent_id] [nvarchar](50) NULL,
	[f_is_async] [int] NULL,
	[f_is_batch] [int] NULL,
	[f_creator_time] [datetime] NULL,
	[f_creator_user_id] [nvarchar](50) NULL,
	[f_last_modify_time] [datetime] NULL,
	[f_last_modify_user_id] [nvarchar](50) NULL,
	[f_delete_time] [datetime] NULL,
	[f_delete_user_id] [nvarchar](50) NULL,
	[f_delete_mark] [int] NULL,
	[f_tenant_id] [nvarchar](50) NULL,
	[f_revive_node_id] [nvarchar](50) NULL,
	[f_system_id] [nvarchar](50) NULL,
	[f_restore] [int] NULL,
	[f_template_id] [nvarchar](50) NULL,
	[f_delegate_user_id] [nvarchar](50) NULL,
	[f_reject_data_id] [nvarchar](50) NULL,
	[f_suspend] [int] NULL,
	[f_description] [nvarchar](500) NULL,
	[f_enabled_mark] [int] NULL,
	[f_sort_code] [bigint] NULL,
	[f_zx_system_id] [nvarchar](50) NULL,
 CONSTRAINT [PK__flow_tas__2911CBED952A6519] PRIMARY KEY CLUSTERED 
(
	[f_id] ASC) ON [PRIMARY]
)
GO

-- 115. flow_task_circulate
IF OBJECT_ID('dbo.flow_task_circulate', 'U') IS NULL
CREATE TABLE [dbo].[flow_task_circulate](
	[f_id] [nvarchar](50) NOT NULL,
	[f_object_type] [nvarchar](50) NULL,
	[f_object_id] [nvarchar](50) NULL,
	[f_node_code] [nvarchar](50) NULL,
	[f_node_name] [nvarchar](50) NULL,
	[f_task_node_id] [nvarchar](50) NULL,
	[f_task_id] [nvarchar](50) NULL,
	[f_sort_code] [bigint] NULL,
	[f_creator_time] [datetime] NULL,
	[f_creator_user_id] [nvarchar](50) NULL,
	[f_last_modify_time] [datetime] NULL,
	[f_last_modify_user_id] [nvarchar](50) NULL,
	[f_delete_time] [datetime] NULL,
	[f_delete_user_id] [nvarchar](50) NULL,
	[f_delete_mark] [int] NULL,
	[f_tenant_id] [nvarchar](50) NULL,
	[f_zx_system_id] [nvarchar](50) NULL,
 CONSTRAINT [PK__flow_tas__2911CBED6DF63FD0] PRIMARY KEY CLUSTERED 
(
	[f_id] ASC) ON [PRIMARY]
)
GO

-- 116. flow_task_node
IF OBJECT_ID('dbo.flow_task_node', 'U') IS NULL
CREATE TABLE [dbo].[flow_task_node](
	[f_id] [nvarchar](50) NOT NULL,
	[f_node_code] [nvarchar](50) NULL,
	[f_node_name] [nvarchar](50) NULL,
	[f_node_type] [nvarchar](50) NULL,
	[f_node_property_json] [nvarchar](max) NULL,
	[f_node_up] [nvarchar](50) NULL,
	[f_node_next] [nvarchar](2000) NULL,
	[f_completion] [int] NULL,
	[f_creator_user_id] [nvarchar](50) NULL,
	[f_last_modify_time] [datetime] NULL,
	[f_last_modify_user_id] [nvarchar](50) NULL,
	[f_delete_time] [datetime] NULL,
	[f_delete_user_id] [nvarchar](50) NULL,
	[f_delete_mark] [int] NULL,
	[f_tenant_id] [nvarchar](50) NULL,
	[f_task_id] [nvarchar](50) NULL,
	[f_state] [int] NULL,
	[f_candidates] [nvarchar](max) NULL,
	[f_draft_data] [nvarchar](max) NULL,
	[f_form_id] [nvarchar](50) NULL,
	[f_description] [nvarchar](500) NULL,
	[f_sort_code] [bigint] NULL,
	[f_creator_time] [datetime] NULL,
	[f_zx_system_id] [nvarchar](50) NULL,
 CONSTRAINT [PK__flow_tas__2911CBED5DC62CA9] PRIMARY KEY CLUSTERED 
(
	[f_id] ASC) ON [PRIMARY]
)
GO

-- 117. flow_task_operator
IF OBJECT_ID('dbo.flow_task_operator', 'U') IS NULL
CREATE TABLE [dbo].[flow_task_operator](
	[f_id] [nvarchar](50) NOT NULL,
	[f_append_handle_id] [nvarchar](50) NULL,
	[f_handle_id] [nvarchar](50) NULL,
	[f_handle_status] [int] NULL,
	[f_handle_time] [datetime] NULL,
	[f_node_code] [nvarchar](50) NULL,
	[f_node_name] [nvarchar](50) NULL,
	[f_completion] [int] NULL,
	[f_creator_user_id] [nvarchar](50) NULL,
	[f_last_modify_time] [datetime] NULL,
	[f_last_modify_user_id] [nvarchar](50) NULL,
	[f_delete_time] [datetime] NULL,
	[f_delete_user_id] [nvarchar](50) NULL,
	[f_delete_mark] [int] NULL,
	[f_tenant_id] [nvarchar](50) NULL,
	[f_task_node_id] [nvarchar](50) NULL,
	[f_task_id] [nvarchar](50) NULL,
	[f_type] [int] NULL,
	[f_state] [int] NULL,
	[f_parent_id] [nvarchar](50) NULL,
	[f_draft_data] [nvarchar](max) NULL,
	[f_automation] [nvarchar](50) NULL,
	[f_rollback_id] [nvarchar](50) NULL,
	[f_reject] [nvarchar](50) NULL,
	[f_description] [nvarchar](500) NULL,
	[f_sort_code] [bigint] NULL,
	[f_creator_time] [datetime] NULL,
	[f_zx_system_id] [nvarchar](50) NULL,
 CONSTRAINT [PK__flow_tas__2911CBED2A2858CD] PRIMARY KEY CLUSTERED 
(
	[f_id] ASC) ON [PRIMARY]
)
GO

-- 118. flow_task_operator_record
IF OBJECT_ID('dbo.flow_task_operator_record', 'U') IS NULL
CREATE TABLE [dbo].[flow_task_operator_record](
	[f_id] [nvarchar](50) NOT NULL,
	[f_node_code] [nvarchar](50) NULL,
	[f_node_name] [nvarchar](50) NULL,
	[f_handle_status] [int] NULL,
	[f_handle_id] [nvarchar](50) NULL,
	[f_handle_time] [datetime] NULL,
	[f_handle_opinion] [nvarchar](500) NULL,
	[f_task_operator_id] [nvarchar](50) NULL,
	[f_task_node_id] [nvarchar](50) NULL,
	[f_task_id] [nvarchar](50) NULL,
	[f_sign_img] [nvarchar](max) NULL,
	[f_status] [int] NULL,
	[f_operator_id] [nvarchar](50) NULL,
	[f_file_list] [nvarchar](max) NULL,
	[f_draft_data] [nvarchar](max) NULL,
	[f_approver_type] [nvarchar](50) NULL,
	[f_sort_code] [bigint] NULL,
	[f_creator_time] [datetime] NULL,
	[f_creator_user_id] [nvarchar](50) NULL,
	[f_last_modify_time] [datetime] NULL,
	[f_last_modify_user_id] [nvarchar](50) NULL,
	[f_delete_time] [datetime] NULL,
	[f_delete_user_id] [nvarchar](50) NULL,
	[f_delete_mark] [int] NULL,
	[f_tenant_id] [nvarchar](50) NULL,
	[f_zx_system_id] [nvarchar](50) NULL,
 CONSTRAINT [PK__flow_tas__2911CBED9CBBA47C] PRIMARY KEY CLUSTERED 
(
	[f_id] ASC) ON [PRIMARY]
)
GO

-- 119. flow_task_operator_user
IF OBJECT_ID('dbo.flow_task_operator_user', 'U') IS NULL
CREATE TABLE [dbo].[flow_task_operator_user](
	[f_id] [nvarchar](50) NOT NULL,
	[f_append_handle_id] [nvarchar](50) NULL,
	[f_handle_id] [nvarchar](50) NULL,
	[f_handle_status] [int] NULL,
	[f_handle_time] [datetime] NULL,
	[f_node_code] [nvarchar](50) NULL,
	[f_node_name] [nvarchar](50) NULL,
	[f_completion] [int] NULL,
	[f_task_node_id] [nvarchar](50) NULL,
	[f_task_id] [nvarchar](50) NULL,
	[f_type] [int] NULL,
	[f_state] [int] NULL,
	[f_parent_id] [nvarchar](50) NULL,
	[f_creator_user_id] [nvarchar](50) NULL,
	[f_last_modify_time] [datetime] NULL,
	[f_last_modify_user_id] [nvarchar](50) NULL,
	[f_delete_time] [datetime] NULL,
	[f_delete_user_id] [nvarchar](50) NULL,
	[f_delete_mark] [int] NULL,
	[f_draft_data] [nvarchar](max) NULL,
	[f_automation] [nvarchar](50) NULL,
	[f_rollback_id] [nvarchar](50) NULL,
	[f_reject] [nvarchar](50) NULL,
	[f_description] [nvarchar](500) NULL,
	[f_sort_code] [bigint] NULL,
	[f_creator_time] [datetime] NULL,
	[f_tenant_id] [nvarchar](50) NULL,
	[f_zx_system_id] [nvarchar](50) NULL,
 CONSTRAINT [PK__flow_tas__2911CBEDA901980F] PRIMARY KEY CLUSTERED 
(
	[f_id] ASC) ON [PRIMARY]
)
GO

-- 120. flow_template
IF OBJECT_ID('dbo.flow_template', 'U') IS NULL
CREATE TABLE [dbo].[flow_template](
	[f_id] [nvarchar](50) NOT NULL,
	[f_en_code] [nvarchar](200) NULL,
	[f_full_name] [nvarchar](200) NULL,
	[f_type] [int] NULL,
	[f_category] [nvarchar](50) NULL,
	[f_icon] [nvarchar](50) NULL,
	[f_icon_background] [nvarchar](50) NULL,
	[f_description] [nvarchar](500) NULL,
	[f_enabled_mark] [int] NULL,
	[f_sort_code] [bigint] NULL,
	[f_creator_time] [datetime] NULL,
	[f_creator_user_id] [nvarchar](50) NULL,
	[f_last_modify_time] [datetime] NULL,
	[f_last_modify_user_id] [nvarchar](50) NULL,
	[f_delete_time] [datetime] NULL,
	[f_delete_user_id] [nvarchar](50) NULL,
	[f_delete_mark] [int] NULL,
	[f_tenant_id] [nvarchar](50) NULL,
	[f_zx_system_id] [nvarchar](50) NULL,
 CONSTRAINT [PK__flow_tem__2911CBED52757D7D] PRIMARY KEY CLUSTERED 
(
	[f_id] ASC) ON [PRIMARY]
)
GO

-- 121. flow_template_json
IF OBJECT_ID('dbo.flow_template_json', 'U') IS NULL
CREATE TABLE [dbo].[flow_template_json](
	[f_id] [nvarchar](50) NOT NULL,
	[f_template_id] [nvarchar](50) NULL,
	[f_full_name] [nvarchar](200) NULL,
	[f_visible_type] [int] NULL,
	[f_version] [nvarchar](50) NULL,
	[f_flow_template_json] [nvarchar](max) NULL,
	[f_group_id] [nvarchar](50) NULL,
	[f_creator_time] [datetime] NULL,
	[f_creator_user_id] [nvarchar](50) NULL,
	[f_last_modify_time] [datetime] NULL,
	[f_last_modify_user_id] [nvarchar](50) NULL,
	[f_delete_time] [datetime] NULL,
	[f_delete_user_id] [nvarchar](50) NULL,
	[f_delete_mark] [int] NULL,
	[f_tenant_id] [nvarchar](50) NULL,
	[f_send_config_ids] [nvarchar](max) NULL,
	[f_enabled_mark] [int] NULL,
	[f_sort_code] [bigint] NULL,
	[f_zx_system_id] [nvarchar](50) NULL,
 CONSTRAINT [PK__flow_tem__2911CBED6CD4F914] PRIMARY KEY CLUSTERED 
(
	[f_id] ASC) ON [PRIMARY]
)
GO

-- 122. flow_visible
IF OBJECT_ID('dbo.flow_visible', 'U') IS NULL
CREATE TABLE [dbo].[flow_visible](
	[f_id] [nvarchar](50) NOT NULL,
	[f_flow_id] [nvarchar](50) NULL,
	[f_operator_type] [nvarchar](50) NULL,
	[f_operator_id] [nvarchar](50) NULL,
	[f_type] [int] NULL,
	[f_sort_code] [bigint] NULL,
	[f_creator_time] [datetime] NULL,
	[f_creator_user_id] [nvarchar](50) NULL,
	[f_last_modify_time] [datetime] NULL,
	[f_last_modify_user_id] [nvarchar](50) NULL,
	[f_delete_time] [datetime] NULL,
	[f_delete_user_id] [nvarchar](50) NULL,
	[f_delete_mark] [int] NULL,
	[f_tenant_id] [nvarchar](50) NULL,
	[f_zx_system_id] [nvarchar](50) NULL,
 CONSTRAINT [PK__flow_vis__2911CBEDAE98BB16] PRIMARY KEY CLUSTERED 
(
	[f_id] ASC) ON [PRIMARY]
)
GO

-- 123. PROCESSED_EVENT
IF OBJECT_ID('dbo.PROCESSED_EVENT', 'U') IS NULL
CREATE TABLE [dbo].[PROCESSED_EVENT](
	[EventId] [nvarchar](200) NOT NULL,
	[HandlerName] [nvarchar](200) NOT NULL,
	[ProcessedAt] [datetime] NOT NULL,
 CONSTRAINT [PK_PROCESSED_EVENT] PRIMARY KEY CLUSTERED 
(
	[EventId] ASC,
	[HandlerName] ASC) ON [PRIMARY]
)
GO

-- 124. SYS_EVENT_OUTBOX_MESSAGE
IF OBJECT_ID('dbo.SYS_EVENT_OUTBOX_MESSAGE', 'U') IS NULL
CREATE TABLE [dbo].[SYS_EVENT_OUTBOX_MESSAGE](
	[F_ID] [uniqueidentifier] NOT NULL,
	[F_EVENT_NAME] [nvarchar](200) NOT NULL,
	[F_EVENT_PAYLOAD] [text] NOT NULL,
	[F_CREATED_AT] [datetime] NOT NULL,
	[F_PROCESSED_AT] [datetime] NULL,
	[F_RETRY_COUNT] [int] NOT NULL,
	[F_MAX_RETRY_COUNT] [int] NOT NULL,
	[F_STATUS] [int] NOT NULL,
	[F_ERROR] [text] NULL,
 CONSTRAINT [PK_SYS_EVENT_OUTBOX_MESSAGE] PRIMARY KEY CLUSTERED 
(
	[F_ID] ASC) ON [PRIMARY]
)
GO

-- 125. SYS_PROCESSED_EVENT
IF OBJECT_ID('dbo.SYS_PROCESSED_EVENT', 'U') IS NULL
CREATE TABLE [dbo].[SYS_PROCESSED_EVENT](
	[F_EVENT_ID] [nvarchar](200) NOT NULL,
	[F_HANDLER_NAME] [nvarchar](200) NOT NULL,
	[F_PROCESSED_AT] [datetime] NOT NULL,
 CONSTRAINT [PK_SYS_PROCESSED_EVENT] PRIMARY KEY CLUSTERED 
(
	[F_EVENT_ID] ASC,
	[F_HANDLER_NAME] ASC) ON [PRIMARY]
)
GO

-- 126. zx_sys_config
IF OBJECT_ID('dbo.zx_sys_config', 'U') IS NULL
CREATE TABLE [dbo].[zx_sys_config](
	[Id] [varchar](50) NOT NULL,
	[Name] [varchar](50) NULL,
	[KeyName] [varchar](50) NOT NULL,
	[KeyValue] [text] NULL,
	[UpdateBy] [varchar](50) NULL,
	[UpdateDate] [datetime] NULL,
	[Comment] [text] NULL,
	[PID] [int] NULL,
	[VersionNum] [int] NULL,
	[f_inte_assistant] [int] NULL,
	[f_delete_mark] [int] NULL,
	[F_Version] [int] NULL,
	[FormId] [varchar](50) NULL,
	[SortCode] [int] NULL,
	[f_delete_user_id] [nvarchar](50) NULL,
	[f_delete_time] [datetime] NULL,
	[f_zx_system_id] [nvarchar](50) NULL,
 CONSTRAINT [PK_zxConfig] PRIMARY KEY CLUSTERED 
(
	[Id] ASC) ON [PRIMARY]
)
GO

-- 127. zx_sys_db
IF OBJECT_ID('dbo.zx_sys_db', 'U') IS NULL
CREATE TABLE [dbo].[zx_sys_db](
	[id] [varchar](50) NOT NULL,
	[name] [varchar](50) NOT NULL,
	[filename] [text] NULL,
	[status] [int] NULL,
	[comment] [text] NULL,
	[f_inte_assistant] [int] NULL,
	[f_delete_mark] [int] NULL,
	[f_zx_system_id] [nvarchar](50) NULL,
 CONSTRAINT [PK_zx_system_db] PRIMARY KEY CLUSTERED 
(
	[id] ASC) ON [PRIMARY]
)
GO

-- 128. zx_system_db
IF OBJECT_ID('dbo.zx_system_db', 'U') IS NULL
CREATE TABLE [dbo].[zx_system_db](
	[id] [varchar](50) NOT NULL,
	[name] [varchar](50) NOT NULL,
	[filename] [text] NULL,
	[status] [int] NULL,
	[comment] [text] NULL,
	[f_inte_assistant] [int] NULL,
	[f_delete_mark] [int] NULL,
	[f_zx_system_id] [nvarchar](50) NULL
)
GO


-- ============================================================
-- 第 2-9 章: 种子数据 + Studio 菜单 + AI 配置 + 作业库
-- ============================================================

-- ============================================================
-- 第 2 章: 种子数据 — 核心系统初始化
-- ============================================================

-- 2.1 默认租户
IF NOT EXISTS (SELECT 1 FROM dbo.base_tenant WHERE F_Id = 'default')
INSERT INTO dbo.base_tenant (F_Id, F_FullName, F_EnCode, F_SortCode, F_CreatorTime) VALUES
('default', N'默认租户', 'default', 1, GETDATE());
GO

-- 2.2 系统配置
IF NOT EXISTS (SELECT 1 FROM dbo.base_sys_config WHERE F_Id = 'sys_soft_name')
INSERT INTO dbo.base_sys_config (F_Id, F_Name, F_Value, F_Category, F_SortCode, F_TenantId) VALUES
('sys_soft_name',       N'系统名称',     N'面包树科技快速开发平台',   'System',      1, 'default'),
('sys_soft_version',    N'系统版本',     N'V5.2.0',                   'System',      2, 'default'),
('sys_company_name',    N'公司名称',     N'面包树信息科技有限公司',    'System',      3, 'default'),
('sys_aes_key',         N'AES加密密钥',  'MIGfMA0GCSqGSIb3DQEBAQUA', 'Security',    1, 'default'),
('sys_token_expire',    N'Token过期(分)','1440',                     'Security',    2, 'default'),
('sys_login_lock_count',N'登录锁定次数', '5',                        'Security',    3, 'default');
GO

-- 2.3 默认部门
IF NOT EXISTS (SELECT 1 FROM dbo.base_organize WHERE F_Id = 'department_root')
INSERT INTO dbo.base_organize (F_Id, F_ParentId, F_FullName, F_EnCode, F_Type, F_SortCode, F_TenantId) VALUES
('department_root',  '0',   N'总公司',         'root_company',      'company',    1, 'default'),
('dept_admin',       'department_root', N'管理部',   'admin_dept',      'department', 1, 'default'),
('dept_dev',         'department_root', N'研发部',   'dev_dept',        'department', 2, 'default'),
('dept_ops',         'department_root', N'运维部',   'ops_dept',        'department', 3, 'default');
GO

-- 2.4 默认角色
DELETE FROM dbo.base_role WHERE F_Id IN ('role_admin','role_founder','role_platform_admin','role_tenant_admin','role_developer','role_business_expert','role_normal_user');
INSERT INTO dbo.base_role (F_Id, F_FullName, F_EnCode, F_Type, F_EnabledMark, F_SortCode, F_CreatorUserId, F_TenantId) VALUES
('role_admin',          N'超级管理员',       'admin',              '1', 1, 0, '349057407209541', 'default'),
('role_founder',        N'创始人',           'founder',            '2', 1, 1, '349057407209541', 'default'),
('role_platform_admin', N'平台技术负责人',    'platform_admin',     '2', 1, 2, '349057407209541', 'default'),
('role_tenant_admin',   N'租户管理员',       'tenant_admin',       '2', 1, 3, '349057407209541', 'default'),
('role_developer',      N'开发者',           'developer',          '2', 1, 4, '349057407209541', 'default'),
('role_business_expert',N'业务专家',         'business_expert',    '2', 1, 5, '349057407209541', 'default'),
('role_normal_user',    N'普通用户',         'normal_user',        '2', 1, 6, '349057407209541', 'default');
GO

-- 2.5 默认管理员用户 (admin / 123456)
-- 密码使用 AES 加密
IF NOT EXISTS (SELECT 1 FROM dbo.base_user WHERE F_Account = 'admin')
INSERT INTO dbo.base_user (F_Id, F_Account, F_RealName, F_Password, F_Secretkey, F_Gender, F_MobilePhone, F_OrganizeId, F_DepartmentId, F_RoleId, F_IsAdministrator, F_SortCode, F_CreatorUserId, F_TenantId) VALUES
('349057407209541', 'admin', N'管理员', 'MIGfMA0GCSqGSIb3DQEBAQUA', 'MIGfMA0GCSqGSIb3DQEBAQUA', 1, '13800000000', 'department_root', 'dept_admin', 'role_admin', 1, 1, '349057407209541', 'default');
GO

-- 2.6 默认岗位
IF NOT EXISTS (SELECT 1 FROM dbo.base_position WHERE F_Id = 'pos_ceo')
INSERT INTO dbo.base_position (F_Id, F_FullName, F_EnCode, F_OrganizeId, F_SortCode, F_TenantId) VALUES
('pos_ceo',    N'总经理',  'ceo',    'department_root', 1, 'default'),
('pos_manager',N'部门经理','manager','department_root', 2, 'default'),
('pos_dev',    N'开发工程师','dev',  'department_root', 3, 'default'),
('pos_admin',  N'行政专员','admin',  'department_root', 4, 'default');
GO

-- 2.7 字典类型
IF NOT EXISTS (SELECT 1 FROM dbo.base_dictionary_type WHERE F_Id = 'dict_gender')
INSERT INTO dbo.base_dictionary_type (F_Id, F_FullName, F_EnCode, F_IsTree, F_SortCode, F_TenantId) VALUES
('dict_gender',       N'性别',          'Gender',       0, 1, 'default'),
('dict_enabled_mark', N'有效标志',       'EnabledMark',  0, 2, 'default'),
('dict_delete_mark',  N'删除标志',       'DeleteMark',   0, 3, 'default'),
('dict_system_type',  N'系统类型',       'SystemType',   0, 4, 'default');
GO

-- 2.8 字典数据
IF NOT EXISTS (SELECT 1 FROM dbo.base_dictionary_data WHERE F_DictionaryTypeId = 'dict_gender')
INSERT INTO dbo.base_dictionary_data (F_Id, F_DictionaryTypeId, F_ParentId, F_FullName, F_EnCode, F_SortCode, F_TenantId) VALUES
('gender_male',   'dict_gender', '0', N'男', 'Male',   1, 'default'),
('gender_female', 'dict_gender', '0', N'女', 'Female', 2, 'default'),
('enabled_yes',   'dict_enabled_mark', '0', N'启用', '1', 1, 'default'),
('enabled_no',    'dict_enabled_mark', '0', N'禁用', '0', 2, 'default');
GO

-- ============================================================
-- 第 3 章: Studio 菜单体系 (AI 原生开发平台)
-- ============================================================

-- 3.1 Studio 菜单表
IF OBJECT_ID('dbo.BASE_STUDIO_MENU', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.BASE_STUDIO_MENU (
        F_Id              BIGINT          NOT NULL PRIMARY KEY,
        F_ParentId        BIGINT          NOT NULL DEFAULT 0,
        F_Name            NVARCHAR(100)   NOT NULL,
        F_Icon            NVARCHAR(100)   NULL,
        F_Url             NVARCHAR(500)   NULL,
        F_Sort            INT             NOT NULL DEFAULT 0,
        F_Enabled         BIT             NOT NULL DEFAULT 1,
        F_IsVisible       BIT             NOT NULL DEFAULT 1,
        F_IsPublic        BIT             NOT NULL DEFAULT 0,
        F_Comment         NVARCHAR(500)   NULL,
        F_RequiredRoles   NVARCHAR(500)   NULL,
        F_DataScope       NVARCHAR(20)    NOT NULL DEFAULT 'NONE',
        F_ExpandPhase     CHAR(1)         NOT NULL DEFAULT 'A',
        F_TenantViewConfig NVARCHAR(MAX)  NULL,
        F_CreatorTime     DATETIME        NOT NULL DEFAULT GETDATE(),
        F_CreatorUserId   BIGINT          NULL,
        F_ModifyTime      DATETIME        NULL,
        F_ModifyUserId    BIGINT          NULL,
        F_DeleteMark      BIT             NOT NULL DEFAULT 0
    );
    CREATE INDEX IX_STUDIO_MENU_PARENT ON dbo.BASE_STUDIO_MENU(F_ParentId, F_Sort);
END
GO

-- 3.2 清理旧数据并插入 Studio 菜单
DELETE FROM dbo.BASE_STUDIO_MENU;
GO

-- 一级菜单
INSERT INTO BASE_STUDIO_MENU (F_Id, F_ParentId, F_Name, F_Icon, F_Sort, F_RequiredRoles, F_DataScope, F_ExpandPhase, F_IsPublic, F_Comment) VALUES
(100000001, 0, N'AI 原生开发平台',     N'rocket-outlined',     1, N'["platform_admin","founder","tenant_admin","developer","business_expert","normal_user"]', 'NONE', 'A', 1, N'面向全角色的主功能区'),
(200000001, 0, N'智能体与流水线配置',   N'setting-outlined',    2, N'["platform_admin","tenant_admin"]', 'NONE', 'A', 0, NULL),
(300000001, 0, N'JNPF 开发工具箱',     N'tool-outlined',       3, N'["developer"]',                      'NONE', 'A', 0, NULL),
(400000001, 0, N'自博弈训练引擎',       N'experiment-outlined', 4, N'["founder"]',                        'ALL',  'A', 0, NULL);

-- AI 原生开发平台 子菜单
INSERT INTO BASE_STUDIO_MENU (F_Id, F_ParentId, F_Name, F_Icon, F_Sort, F_Url, F_RequiredRoles, F_DataScope, F_ExpandPhase, F_Comment) VALUES
(100000101, 100000001, N'提交需求',     N'edit-outlined',        1, N'/studio/ai/submit-requirement', N'["platform_admin","founder","tenant_admin","developer","business_expert","normal_user"]', 'NONE',   'A', N'五阶段流水线入口'),
(100000102, 100000001, N'已生成系统',   N'appstore-outlined',    2, N'/studio/ai/generated-systems',  N'["platform_admin","founder","tenant_admin","developer","business_expert","normal_user"]', 'TENANT', 'A', N'带红点数字提示'),
(100000103, 100000001, N'UI 模板库',    N'block-outlined',       3, N'/studio/ai/ui-templates',       N'["platform_admin","founder","tenant_admin","developer","business_expert","normal_user"]', 'NONE',   'A', N'模板市场+工坊'),
(100000104, 100000001, N'用量与计费',   N'account-book-outlined',4, N'/studio/ai/usage-billing',      N'["platform_admin","founder","tenant_admin","developer","business_expert","normal_user"]', 'OWN',    'A', N'按角色看不同范围');

-- 智能体与流水线配置 (容器+子菜单)
INSERT INTO BASE_STUDIO_MENU (F_Id, F_ParentId, F_Name, F_Icon, F_Sort, F_RequiredRoles, F_DataScope, F_ExpandPhase) VALUES
(200000100, 200000001, N'智能体管理',       N'robot-outlined',    1, N'["platform_admin"]', 'NONE', 'A'),
(200000200, 200000001, N'流水线配置',       N'branches-outlined', 2, N'["platform_admin"]', 'NONE', 'A'),
(200000300, 200000001, N'业务知识管理',     N'book-outlined',     3, N'["platform_admin","tenant_admin"]', 'NONE', 'A'),
(200000400, 200000001, N'模型供应商配置',   N'api-outlined',      4, N'["platform_admin"]', 'NONE', 'B'),
(200000500, 200000001, N'自博弈引擎',       N'experiment-outlined',5, N'["founder"]', 'ALL',  'B'),
(200000600, 200000001, N'Skills 市场',      N'block-outlined',    6, N'["platform_admin","tenant_admin","developer"]', 'NONE', 'A'),
(200000700, 200000001, N'审计与日志',       N'file-search-outlined', 7, N'["platform_admin"]', 'ALL', 'A');

INSERT INTO BASE_STUDIO_MENU (F_Id, F_ParentId, F_Name, F_Sort, F_Url, F_RequiredRoles, F_DataScope, F_ExpandPhase) VALUES
-- 智能体管理
(200000101, 200000100, N'智能体创建与配置', 1, N'/studio/agent/create',     N'["platform_admin"]', 'ALL', 'A'),
(200000102, 200000100, N'子智能体管理',     2, N'/studio/agent/sub-agents', N'["platform_admin"]', 'ALL', 'A'),
(200000103, 200000100, N'Skills 管理',      3, N'/studio/agent/skills',     N'["platform_admin"]', 'ALL', 'A'),
(200000104, 200000100, N'MCP 配置',         4, N'/studio/agent/mcp',        N'["platform_admin"]', 'ALL', 'A'),
-- 流水线配置
(200000201, 200000200, N'流水线阶段设置',   1, N'/studio/pipeline/stages',       N'["platform_admin"]', 'ALL', 'A'),
(200000202, 200000200, N'模型路由策略',     2, N'/studio/pipeline/model-routing', N'["platform_admin"]', 'ALL', 'A'),
-- 业务知识管理
(200000301, 200000300, N'业务规则配置中心', 1, N'/studio/knowledge/rule-editor',  N'["platform_admin","tenant_admin"]', 'TENANT', 'A'),
(200000302, 200000300, N'知识图谱',         2, N'/studio/knowledge/graph',        N'["platform_admin","tenant_admin"]', 'TENANT', 'A'),
(200000303, 200000300, N'Prompt 模板库',    3, N'/studio/knowledge/prompts',      N'["platform_admin","tenant_admin","developer"]', 'TENANT', 'A'),
-- 模型供应商配置
(200000401, 200000400, N'供应商管理',       1, N'/studio/pipeline/providers',  N'["platform_admin"]', 'ALL', 'B'),
-- 自博弈引擎
(200000501, 200000500, N'红蓝对抗',         1, N'/studio/self-play/battle',   N'["founder"]', 'ALL', 'B'),
(200000502, 200000500, N'知识自进化',       2, N'/studio/self-play/evolution',N'["founder"]', 'ALL', 'B'),
-- Skills 市场
(200000601, 200000600, N'Skills 市场',      1, N'/studio/skills/marketplace', N'["platform_admin","tenant_admin","developer"]', 'NONE', 'A'),
-- 审计与日志
(200000701, 200000700, N'审计追踪',         1, N'/studio/audit/trails',      N'["platform_admin"]', 'ALL', 'A'),
(200000702, 200000700, N'变更对比',         2, N'/studio/audit/diff',        N'["platform_admin"]', 'ALL', 'A');

-- JNPF 开发工具箱
INSERT INTO BASE_STUDIO_MENU (F_Id, F_ParentId, F_Name, F_Icon, F_Sort, F_RequiredRoles, F_DataScope, F_ExpandPhase) VALUES
(300000100, 300000001, N'代码生成器',       N'code-outlined',      1, N'["developer"]', 'NONE', 'A'),
(300000200, 300000001, N'API 文档',         N'api-outlined',       2, N'["developer"]', 'NONE', 'A'),
(300000300, 300000001, N'数据库工具',       N'database-outlined',  3, N'["developer"]', 'NONE', 'A');

INSERT INTO BASE_STUDIO_MENU (F_Id, F_ParentId, F_Name, F_Sort, F_Url, F_RequiredRoles, F_DataScope, F_ExpandPhase) VALUES
(300000101, 300000100, N'代码生成',         1, N'/studio/toolbox/codegen',  N'["developer"]', 'NONE', 'A'),
(300000201, 300000200, N'Knife4j 文档',    1, N'/api/doc/index.html',      N'["developer"]', 'NONE', 'A'),
(300000301, 300000300, N'表结构管理',       1, N'/studio/toolbox/db-tables',N'["developer"]', 'NONE', 'A');

-- 自博弈训练引擎
INSERT INTO BASE_STUDIO_MENU (F_Id, F_ParentId, F_Name, F_Icon, F_Sort, F_RequiredRoles, F_DataScope, F_ExpandPhase) VALUES
(400000100, 400000001, N'训练仪表盘',       N'dashboard-outlined', 1, N'["founder"]', 'ALL', 'A'),
(400000200, 400000001, N'模型测试场',       N'experiment-outlined',2, N'["founder"]', 'ALL', 'A');

INSERT INTO BASE_STUDIO_MENU (F_Id, F_ParentId, F_Name, F_Sort, F_Url, F_RequiredRoles, F_DataScope, F_ExpandPhase) VALUES
(400000101, 400000100, N'训练总览',         1, N'/studio/self-play/dashboard', N'["founder"]', 'ALL', 'A'),
(400000102, 400000100, N'训练历史',         2, N'/studio/self-play/history',   N'["founder"]', 'ALL', 'A'),
(400000201, 400000200, N'模型测试场',       1, N'/studio/self-play/testfield', N'["founder"]', 'ALL', 'A');
GO

-- ============================================================
-- 第 4 章: AI 模型供应商配置
-- ============================================================

-- 4.1 供应商表
IF OBJECT_ID('dbo.BASE_AI_MODEL_PROVIDER', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.BASE_AI_MODEL_PROVIDER (
        F_Id              BIGINT          PRIMARY KEY,
        F_ProviderCode    NVARCHAR(50)    NOT NULL,
        F_Name            NVARCHAR(100)   NOT NULL,
        F_BaseUrl         NVARCHAR(500)   NOT NULL,
        F_ApiKey          NVARCHAR(500)   NOT NULL,
        F_ApiFormat       NVARCHAR(20)    NOT NULL DEFAULT 'openai',
        F_DefaultModel    NVARCHAR(100),
        F_MaxTokens       BIGINT          NOT NULL DEFAULT 1000000,
        F_Temperature     DECIMAL(5,2)    NOT NULL DEFAULT 0.7,
        F_Status          NVARCHAR(20)    NOT NULL DEFAULT 'healthy',
        F_Priority        INT             NOT NULL DEFAULT 1,
        F_Enabled         BIT             NOT NULL DEFAULT 1,
        F_Description     NVARCHAR(500),
        F_LastTestTime    DATETIME,
        F_LastTestResult  NVARCHAR(2000),
        F_CreatorTime     DATETIME        NOT NULL DEFAULT GETDATE(),
        F_CreatorUserId   BIGINT,
        F_ModifyTime      DATETIME,
        F_ModifyUserId    BIGINT,
        F_DeleteMark      BIT             NOT NULL DEFAULT 0,
        CONSTRAINT UQ_PROVIDER_CODE UNIQUE (F_ProviderCode)
    );
    CREATE INDEX IX_PROVIDER_ENABLED ON dbo.BASE_AI_MODEL_PROVIDER(F_Enabled, F_Priority, F_DeleteMark);
END
GO

-- 4.2 模型路由表
IF OBJECT_ID('dbo.BASE_AI_MODEL_ROUTING', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.BASE_AI_MODEL_ROUTING (
        F_Id              BIGINT          PRIMARY KEY,
        F_StageName       NVARCHAR(50)    NOT NULL,
        F_ProviderCode    NVARCHAR(50)    NOT NULL,
        F_ModelName       NVARCHAR(100)   NOT NULL,
        F_Priority        INT             NOT NULL DEFAULT 1,
        F_Enabled         BIT             NOT NULL DEFAULT 1,
        F_Description     NVARCHAR(500),
        F_CreatorTime     DATETIME        NOT NULL DEFAULT GETDATE(),
        F_ModifyTime      DATETIME,
        F_DeleteMark      BIT             NOT NULL DEFAULT 0
    );
    CREATE INDEX IX_MODEL_ROUTING_STAGE ON dbo.BASE_AI_MODEL_ROUTING(F_StageName, F_Enabled);
END
GO

-- 4.3 供应商种子数据
DELETE FROM dbo.BASE_AI_MODEL_PROVIDER;
INSERT INTO dbo.BASE_AI_MODEL_PROVIDER (F_Id, F_ProviderCode, F_Name, F_BaseUrl, F_ApiKey, F_ApiFormat, F_DefaultModel, F_MaxTokens, F_Temperature, F_Status, F_Priority, F_Enabled, F_Description, F_CreatorTime) VALUES
(100000001, 'deepseek', N'DeepSeek',     'https://api.deepseek.com',                'YOUR_DEEPSEEK_API_KEY',  'openai', 'deepseek-v4-pro',       2000000, 0.7, 'healthy', 1, 1, N'DeepSeek V4 Pro — 国产高性价比，2M 上下文', GETDATE()),
(100000002, 'mimo',     N'MiMo',         'https://api.mimo.xiaomi.com',             'YOUR_MIMO_API_KEY',      'openai', 'mimo-2.5-pro',           2500000, 0.7, 'healthy', 2, 1, N'MiMo 2.5 Pro — 小米大模型，2.5M 上下文', GETDATE()),
(100000003, 'tongyi',   N'通义千问',     'https://dashscope.aliyuncs.com/api/v1',   'YOUR_TONGYI_API_KEY',    'openai', 'qwen-max',               1000000, 0.7, 'offline', 3, 1, N'通义千问 — 阿里生态，1M 上下文', GETDATE()),
(100000004, 'openai',   N'OpenAI',       'https://api.openai.com/v1',               'YOUR_OPENAI_API_KEY',    'openai', 'gpt-4o',                 1000000, 0.7, 'offline', 4, 1, N'OpenAI GPT-4o — 通用能力最强', GETDATE()),
(100000005, 'ollama',   N'本地模型(Ollama)','http://localhost:11434/v1',             'ollama',                 'openai', 'llama3',                 4096000, 0.7, 'offline', 5, 1, N'本地 Ollama 离线模型 — 4096k 上下文，无需 API Key', GETDATE());
GO

-- 4.4 模型路由种子数据
DELETE FROM dbo.BASE_AI_MODEL_ROUTING;
INSERT INTO dbo.BASE_AI_MODEL_ROUTING (F_Id, F_StageName, F_ProviderCode, F_ModelName, F_Priority, F_Enabled, F_Description, F_CreatorTime) VALUES
(200000001, 'requirement',   'deepseek', 'deepseek-v4-pro', 1, 1, N'需求分析 — DeepSeek V4 Pro', GETDATE()),
(200000002, 'architecture',  'mimo',     'mimo-2.5-pro',    1, 1, N'架构设计 — MiMo 2.5 Pro', GETDATE()),
(200000003, 'design',        'mimo',     'mimo-2.5-pro',    1, 1, N'总体设计 — MiMo 2.5 Pro', GETDATE()),
(200000004, 'development',   'deepseek', 'deepseek-v4-pro', 1, 1, N'自动开发 — DeepSeek V4 Pro', GETDATE()),
(200000005, 'delivery',      'deepseek', 'deepseek-v4-pro', 1, 1, N'交付验证 — DeepSeek V4 Pro', GETDATE());
GO

-- ============================================================
-- 第 5 章: AI Pipeline 流水线表
-- ============================================================

IF OBJECT_ID('dbo.AI_PIPELINE', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AI_PIPELINE (
        F_Id              BIGINT          IDENTITY(1,1) PRIMARY KEY,
        F_ProjectName     NVARCHAR(200)   NULL,
        F_Description     NVARCHAR(MAX)   NULL,
        F_CurrentStage    NVARCHAR(50)    NOT NULL DEFAULT 'requirement',
        F_StageStatus     NVARCHAR(20)    NOT NULL DEFAULT 'running',
        F_IRData          NVARCHAR(MAX)   NULL,
        F_CreatorUserId   BIGINT          NULL,
        F_TenantId        NVARCHAR(50)    NULL DEFAULT 'default',
        F_CreatorTime     DATETIME        NOT NULL DEFAULT GETDATE(),
        F_ModifyTime      DATETIME        NULL,
        F_DeleteMark      BIT             NOT NULL DEFAULT 0
    );
END
GO

IF OBJECT_ID('dbo.AI_PIPELINE_MESSAGE', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AI_PIPELINE_MESSAGE (
        F_Id              BIGINT          IDENTITY(1,1) PRIMARY KEY,
        F_PipelineId      BIGINT          NOT NULL,
        F_Role            NVARCHAR(20)    NOT NULL,
        F_Content         NVARCHAR(MAX)   NULL,
        F_ContentType     NVARCHAR(20)    NULL DEFAULT 'text',
        F_Stage           NVARCHAR(50)    NULL,
        F_IRData          NVARCHAR(MAX)   NULL,
        F_TokenCount      BIGINT          NULL DEFAULT 0,
        F_CreatorTime     DATETIME        NOT NULL DEFAULT GETDATE(),
        CONSTRAINT FK_PIPELINE_MSG FOREIGN KEY (F_PipelineId) REFERENCES dbo.AI_PIPELINE(F_Id)
    );
    CREATE INDEX IX_PIPELINE_MSG_PID ON dbo.AI_PIPELINE_MESSAGE(F_PipelineId, F_CreatorTime);
END
GO

-- ============================================================
-- 第 6 章: 消息/通知/事件表
-- ============================================================

-- 6.1 站内消息表
IF OBJECT_ID('dbo.base_message', 'U') IS NULL
CREATE TABLE dbo.base_message (
    F_Id              NVARCHAR(50)    PRIMARY KEY,
    F_Title           NVARCHAR(200)   NULL,
    F_Content         NVARCHAR(MAX)   NULL,
    F_Type            INT             NULL DEFAULT 0,
    F_FromUserId      NVARCHAR(50)    NULL,
    F_ToUserId        NVARCHAR(50)    NULL,
    F_IsRead          INT             NULL DEFAULT 0,
    F_ReadTime        DATETIME        NULL,
    F_CreatorTime     DATETIME        NULL DEFAULT GETDATE(),
    F_DeleteMark      INT             NULL DEFAULT 0,
    F_TenantId        NVARCHAR(50)    NULL DEFAULT 'default'
);
GO

-- 6.2 事件总线出箱表
IF OBJECT_ID('dbo.EVENT_OUTBOX', 'U') IS NULL
CREATE TABLE dbo.EVENT_OUTBOX (
    F_Id              BIGINT          IDENTITY(1,1) PRIMARY KEY,
    F_EventId         NVARCHAR(100)   NOT NULL,
    F_EventType       NVARCHAR(200)   NOT NULL,
    F_EventData       NVARCHAR(MAX)   NULL,
    F_Status          NVARCHAR(20)    NOT NULL DEFAULT 'Pending',
    F_RetryCount      INT             NOT NULL DEFAULT 0,
    F_MaxRetries      INT             NOT NULL DEFAULT 3,
    F_CreatorTime     DATETIME        NOT NULL DEFAULT GETDATE(),
    F_ProcessedTime   DATETIME        NULL,
    F_Error           NVARCHAR(MAX)   NULL
);
CREATE INDEX IX_EVENT_OUTBOX_STATUS ON dbo.EVENT_OUTBOX(F_Status, F_CreatorTime);
GO

-- 6.3 操作日志表
IF OBJECT_ID('dbo.SYS_OPERATION_LOG', 'U') IS NULL
CREATE TABLE dbo.SYS_OPERATION_LOG (
    F_Id              BIGINT          IDENTITY(1,1) PRIMARY KEY,
    F_UserId          NVARCHAR(50)    NULL,
    F_UserName        NVARCHAR(100)   NULL,
    F_Module          NVARCHAR(100)   NULL,
    F_Action          NVARCHAR(100)   NULL,
    F_IPAddress       NVARCHAR(50)    NULL,
    F_RequestUrl      NVARCHAR(500)   NULL,
    F_RequestMethod   NVARCHAR(10)    NULL,
    F_RequestParams   NVARCHAR(MAX)   NULL,
    F_ResponseResult  NVARCHAR(MAX)   NULL,
    F_ElapsedTime     BIGINT          NULL,
    F_CreatorTime     DATETIME        NOT NULL DEFAULT GETDATE()
);
CREATE INDEX IX_OP_LOG_TIME ON dbo.SYS_OPERATION_LOG(F_CreatorTime);
GO

-- ============================================================
-- 第 7 章: 工作流表 (FLOW_*)
-- ============================================================

IF OBJECT_ID('dbo.FLOW_TEMPLATE', 'U') IS NULL
CREATE TABLE dbo.FLOW_TEMPLATE (
    F_Id              NVARCHAR(50)    PRIMARY KEY,
    F_FullName        NVARCHAR(200)   NULL,
    F_EnCode          NVARCHAR(100)   NULL,
    F_FlowJson        NVARCHAR(MAX)   NULL,
    F_FormJson        NVARCHAR(MAX)   NULL,
    F_EnabledMark     INT             NULL DEFAULT 1,
    F_CreatorTime     DATETIME        NULL DEFAULT GETDATE(),
    F_CreatorUserId   NVARCHAR(50)    NULL,
    F_DeleteMark      INT             NULL DEFAULT 0,
    F_TenantId        NVARCHAR(50)    NULL DEFAULT 'default'
);
GO

IF OBJECT_ID('dbo.FLOW_TASK', 'U') IS NULL
CREATE TABLE dbo.FLOW_TASK (
    F_Id              NVARCHAR(50)    PRIMARY KEY,
    F_FlowInstanceId  NVARCHAR(50)    NULL,
    F_NodeName        NVARCHAR(100)   NULL,
    F_AssigneeId      NVARCHAR(50)    NULL,
    F_Status          NVARCHAR(20)    NULL DEFAULT 'Pending',
    F_CreatorTime     DATETIME        NULL DEFAULT GETDATE(),
    F_CompletedTime   DATETIME        NULL,
    F_TenantId        NVARCHAR(50)    NULL DEFAULT 'default'
);
CREATE INDEX IX_FLOW_TASK_ASSIGNEE ON dbo.FLOW_TASK(F_AssigneeId, F_Status);
GO

-- ============================================================
-- 第 8 章: 数据可视化表 (EXT_DATAV_*)
-- ============================================================

IF OBJECT_ID('dbo.EXT_DATAV_SCREEN', 'U') IS NULL
CREATE TABLE dbo.EXT_DATAV_SCREEN (
    F_Id              NVARCHAR(50)    PRIMARY KEY,
    F_FullName        NVARCHAR(200)   NULL,
    F_ScreenJson      NVARCHAR(MAX)   NULL,
    F_Thumbnail       NVARCHAR(500)   NULL,
    F_EnabledMark     INT             NULL DEFAULT 1,
    F_CreatorTime     DATETIME        NULL DEFAULT GETDATE(),
    F_CreatorUserId   NVARCHAR(50)    NULL,
    F_DeleteMark      INT             NULL DEFAULT 0,
    F_TenantId        NVARCHAR(50)    NULL DEFAULT 'default'
);
GO

-- ============================================================
-- 第 9 章: jnpf_sundial 任务调度数据库（完整建库+建表+种子数据）
-- ============================================================
USE [jnpf_sundial];
GO

-- 9.1 作业集群表
IF OBJECT_ID('dbo.JOBCLUSTER', 'U') IS NULL
CREATE TABLE dbo.JOBCLUSTER (
    ID              INT             IDENTITY(1,1) NOT NULL,
    CLUSTERID       NVARCHAR(64)    NOT NULL,
    DESCRIPTION     NVARCHAR(128)   NULL,
    STATUS          BIT             NOT NULL DEFAULT 1,
    UPDATEDTIME     DATETIME        NULL,
    CONSTRAINT PK_JOBCLUSTER_ID PRIMARY KEY CLUSTERED (ID)
);
GO

-- 9.2 作业信息表
IF OBJECT_ID('dbo.JOBDETAILS', 'U') IS NULL
CREATE TABLE dbo.JOBDETAILS (
    ID                  INT             IDENTITY(1,1) NOT NULL,
    JOBID               NVARCHAR(64)    NOT NULL,
    GROUPNAME           NVARCHAR(128)   NULL,
    JOBTYPE             NVARCHAR(128)   NULL,
    ASSEMBLYNAME        NVARCHAR(128)   NULL,
    DESCRIPTION         NVARCHAR(128)   NULL,
    CONCURRENT          BIT             NOT NULL DEFAULT 1,
    INCLUDEANNOTATIONS  BIT             NOT NULL DEFAULT 0,
    PROPERTIES          NVARCHAR(MAX)   NULL,
    UPDATEDTIME         DATETIME        NULL,
    CREATETYPE          INT             NOT NULL DEFAULT 1,
    SCRIPTCODE          NVARCHAR(MAX)   NULL,
    TENANTID            NVARCHAR(50)    NULL,
    CONSTRAINT PK_JOBDETAILS_ID PRIMARY KEY CLUSTERED (ID)
);
GO

-- 9.3 作业触发器表（完整字段，与生产环境一致）
IF OBJECT_ID('dbo.JOBTRIGGERS', 'U') IS NULL
CREATE TABLE dbo.JOBTRIGGERS (
    ID                INT             IDENTITY(1,1) NOT NULL,
    TRIGGERID         NVARCHAR(64)    NOT NULL,
    JOBID             NVARCHAR(64)    NOT NULL,
    TRIGGERTYPE       NVARCHAR(128)   NULL,
    ASSEMBLYNAME      NVARCHAR(128)   NULL,
    ARGS              NVARCHAR(128)   NULL,
    DESCRIPTION       NVARCHAR(128)   NULL,
    STATUS            BIT             NOT NULL DEFAULT 1,
    STARTTIME         DATETIME        NULL,
    ENDTIME           DATETIME        NULL,
    LASTRUNTIME       DATETIME        NULL,
    NEXTRUNTIME       DATETIME        NULL,
    NUMBEROFRUNS      INT             NOT NULL DEFAULT 0,
    MAXNUMBEROFRUNS   INT             NOT NULL DEFAULT 0,
    NUMBEROFERRORS    INT             NOT NULL DEFAULT 0,
    MAXNUMBEROFERRORS INT             NOT NULL DEFAULT 0,
    NUMRETRIES        INT             NOT NULL DEFAULT 0,
    RETRYTIMEOUT      INT             NOT NULL DEFAULT 0,
    STARTNOW          BIT             NOT NULL DEFAULT 0,
    RUNONSTART        BIT             NOT NULL DEFAULT 0,
    RESETONLYONCE     BIT             NOT NULL DEFAULT 0,
    UPDATEDTIME       DATETIME        NULL,
    TENANTID          NVARCHAR(50)    NULL,
    CONSTRAINT PK_JOBTRIGGERS_ID PRIMARY KEY CLUSTERED (ID)
);
GO

-- 9.4 作业日志表
IF OBJECT_ID('dbo.JOBLOGS', 'U') IS NULL
CREATE TABLE dbo.JOBLOGS (
    ID          INT             IDENTITY(1,1) NOT NULL,
    JOBID       NVARCHAR(64)    NULL,
    LEVEL       NVARCHAR(20)    NULL DEFAULT 'Info',
    MESSAGE     NVARCHAR(MAX)   NULL,
    CREATEDTIME DATETIME        NULL DEFAULT GETDATE(),
    CONSTRAINT PK_JOBLOGS_ID PRIMARY KEY CLUSTERED (ID)
);
CREATE INDEX IX_JOBLOGS_JOBID ON dbo.JOBLOGS(JOBID, CREATEDTIME);
GO

-- 9.5 种子数据：默认作业集群
IF NOT EXISTS (SELECT 1 FROM dbo.JOBCLUSTER WHERE CLUSTERID = 'Default')
INSERT INTO dbo.JOBCLUSTER (CLUSTERID, DESCRIPTION, STATUS, UPDATEDTIME) VALUES
('Default', N'默认作业集群', 1, GETDATE());
GO

-- 9.6 种子数据：系统内置定时任务
-- 清理旧示例任务
DELETE FROM dbo.JOBTRIGGERS WHERE JOBID IN ('job_sys_heartbeat', 'job_sys_log_cleanup', 'job_event_retry');
DELETE FROM dbo.JOBDETAILS WHERE JOBID IN ('job_sys_heartbeat', 'job_sys_log_cleanup', 'job_event_retry');
GO

-- 心跳检测任务
INSERT INTO dbo.JOBDETAILS (JOBID, GROUPNAME, JOBTYPE, ASSEMBLYNAME, DESCRIPTION, CONCURRENT, CREATETYPE, TENANTID) VALUES
('job_sys_heartbeat', N'系统任务', N'Simple', N'JNPF.Applications.Service', N'系统心跳检测（每5分钟）', 1, 1, 'default');
INSERT INTO dbo.JOBTRIGGERS (TRIGGERID, JOBID, TRIGGERTYPE, DESCRIPTION, STATUS, STARTTIME, NUMBEROFRUNS, MAXNUMBEROFRUNS, TENANTID) VALUES
('trig_heartbeat_1', 'job_sys_heartbeat', N'Simple', N'心跳触发(每5分钟)', 1, GETDATE(), 0, 0, 'default');

-- 日志清理任务
INSERT INTO dbo.JOBDETAILS (JOBID, GROUPNAME, JOBTYPE, ASSEMBLYNAME, DESCRIPTION, CONCURRENT, CREATETYPE, TENANTID) VALUES
('job_sys_log_cleanup', N'系统任务', N'Cron', N'JNPF.Applications.Service', N'操作日志清理（每天凌晨3点）', 1, 1, 'default');
INSERT INTO dbo.JOBTRIGGERS (TRIGGERID, JOBID, TRIGGERTYPE, DESCRIPTION, STATUS, STARTTIME, NUMBEROFRUNS, MAXNUMBEROFRUNS, TENANTID) VALUES
('trig_logclean_1', 'job_sys_log_cleanup', N'Cron', N'日志清理(每日03:00)', 1, GETDATE(), 0, 0, 'default');

-- 事件重试任务
INSERT INTO dbo.JOBDETAILS (JOBID, GROUPNAME, JOBTYPE, ASSEMBLYNAME, DESCRIPTION, CONCURRENT, CREATETYPE, TENANTID) VALUES
('job_event_retry', N'系统任务', N'Simple', N'JNPF.Applications.Service', N'事件总线失败重试（每1分钟）', 1, 1, 'default');
INSERT INTO dbo.JOBTRIGGERS (TRIGGERID, JOBID, TRIGGERTYPE, DESCRIPTION, STATUS, STARTTIME, NUMBEROFRUNS, MAXNUMBEROFRUNS, TENANTID) VALUES
('trig_evtretry_1', 'job_event_retry', N'Simple', N'事件重试(每1分钟)', 1, GETDATE(), 0, 0, 'default');
GO

-- ============================================================
-- 第 10 章: 验证查询
-- ============================================================
PRINT '============================================';
PRINT 'JNPF v5.2 一键初始化完成';
PRINT '============================================';
PRINT '主数据库: ZXAF_V1_DevTest1';
PRINT '作业库:   jnpf_sundial';
PRINT '';
PRINT '默认管理员: admin / 123456';
PRINT '默认租户:   default';
PRINT '';

-- 主库统计
USE [ZXAF_V1_DevTest1];
GO
PRINT '--- 主数据库表统计 ---';
SELECT 'base_user'               AS TableName, COUNT(*) AS Rows FROM dbo.base_user
UNION ALL SELECT 'base_role',               COUNT(*) FROM dbo.base_role
UNION ALL SELECT 'base_organize',           COUNT(*) FROM dbo.base_organize
UNION ALL SELECT 'base_position',           COUNT(*) FROM dbo.base_position
UNION ALL SELECT 'base_tenant',             COUNT(*) FROM dbo.base_tenant
UNION ALL SELECT 'base_sys_config',         COUNT(*) FROM dbo.base_sys_config
UNION ALL SELECT 'base_dictionary_type',    COUNT(*) FROM dbo.base_dictionary_type
UNION ALL SELECT 'base_dictionary_data',    COUNT(*) FROM dbo.base_dictionary_data
UNION ALL SELECT 'BASE_STUDIO_MENU',        COUNT(*) FROM dbo.BASE_STUDIO_MENU
UNION ALL SELECT 'BASE_AI_MODEL_PROVIDER',  COUNT(*) FROM dbo.BASE_AI_MODEL_PROVIDER
UNION ALL SELECT 'BASE_AI_MODEL_ROUTING',   COUNT(*) FROM dbo.BASE_AI_MODEL_ROUTING
UNION ALL SELECT 'AI_PIPELINE',             COUNT(*) FROM dbo.AI_PIPELINE
UNION ALL SELECT 'EVENT_OUTBOX',            COUNT(*) FROM dbo.EVENT_OUTBOX;
GO

-- 作业库统计
USE [jnpf_sundial];
GO
PRINT '';
PRINT '--- 作业数据库表统计 ---';
SELECT 'JOBCLUSTER'  AS TableName, COUNT(*) AS Rows FROM dbo.JOBCLUSTER
UNION ALL SELECT 'JOBDETAILS',  COUNT(*) FROM dbo.JOBDETAILS
UNION ALL SELECT 'JOBTRIGGERS', COUNT(*) FROM dbo.JOBTRIGGERS
UNION ALL SELECT 'JOBLOGS',     COUNT(*) FROM dbo.JOBLOGS;
GO

PRINT '';
PRINT '============================================';
PRINT '初始化全部完成！';
PRINT '请用 admin / 123456 登录系统';
PRINT '============================================';
GO
