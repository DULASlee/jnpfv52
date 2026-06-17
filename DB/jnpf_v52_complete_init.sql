-- ============================================================
-- JNPF v5.2 一键初始化脚本 (SQL Server)
-- 文件: DB/jnpf_v52_complete_init.sql
-- 日期: 2026-06-17
-- 用途: 创建数据库 + 全部表结构 + 种子数据，零手工操作
-- 适配: SQL Server 2016+ (Express/Standard/Enterprise)
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

-- 0.1 主业务数据库
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

-- 0.2 任务调度数据库 (jnpf_sundial)
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
-- 第 1 章: 基础系统表 (BASE_*)
-- ============================================================

-- 1.1 系统配置表
IF OBJECT_ID('dbo.base_sys_config', 'U') IS NULL
CREATE TABLE dbo.base_sys_config (
    F_Id               NVARCHAR(50)    PRIMARY KEY,
    F_Name             NVARCHAR(200)   NULL,
    F_Value            NVARCHAR(MAX)   NULL,
    F_Category         NVARCHAR(50)    NULL,
    F_SortCode         INT             NULL,
    F_EnabledMark      INT             NULL DEFAULT 1,
    F_Description      NVARCHAR(500)   NULL,
    F_CreatorTime      DATETIME        NULL DEFAULT GETDATE(),
    F_CreatorUserId    NVARCHAR(50)    NULL,
    F_LastModifyTime   DATETIME        NULL,
    F_LastModifyUserId NVARCHAR(50)    NULL,
    F_DeleteMark       INT             NULL DEFAULT 0,
    F_TenantId         NVARCHAR(50)    NULL DEFAULT 'default'
);
GO

-- 1.2 用户表
IF OBJECT_ID('dbo.base_user', 'U') IS NULL
CREATE TABLE dbo.base_user (
    F_Id                NVARCHAR(50)    PRIMARY KEY,
    F_Account           NVARCHAR(50)    NOT NULL,
    F_RealName          NVARCHAR(50)    NULL,
    F_Password          NVARCHAR(200)   NULL,
    F_Secretkey         NVARCHAR(200)   NULL,
    F_Gender            INT             NULL,
    F_MobilePhone       NVARCHAR(20)    NULL,
    F_Email             NVARCHAR(100)   NULL,
    F_HeadIcon          NVARCHAR(200)   NULL,
    F_Birthday          DATETIME        NULL,
    F_OrganizeId        NVARCHAR(50)    NULL DEFAULT 'department_root',
    F_DepartmentId      NVARCHAR(50)    NULL DEFAULT 'department_root',
    F_RoleId            NVARCHAR(50)    NULL,
    F_PositionId        NVARCHAR(50)    NULL,
    F_ManagerId         NVARCHAR(50)    NULL,
    F_IsAdministrator   INT             NULL DEFAULT 0,
    F_Language          NVARCHAR(20)    NULL DEFAULT 'zh-CN',
    F_SortCode          INT             NULL,
    F_EnabledMark       INT             NULL DEFAULT 1,
    F_Description       NVARCHAR(500)   NULL,
    F_CreatorTime       DATETIME        NULL DEFAULT GETDATE(),
    F_CreatorUserId     NVARCHAR(50)    NULL,
    F_LastModifyTime    DATETIME        NULL,
    F_LastModifyUserId  NVARCHAR(50)    NULL,
    F_DeleteMark        INT             NULL DEFAULT 0,
    F_TenantId          NVARCHAR(50)    NULL DEFAULT 'default'
);
GO

-- 1.3 角色表
IF OBJECT_ID('dbo.base_role', 'U') IS NULL
CREATE TABLE dbo.base_role (
    F_Id                NVARCHAR(50)    PRIMARY KEY,
    F_FullName          NVARCHAR(100)   NULL,
    F_EnCode            NVARCHAR(50)    NULL,
    F_Type              NVARCHAR(10)    NULL,
    F_EnabledMark       INT             NULL DEFAULT 1,
    F_SortCode          INT             NULL,
    F_Description       NVARCHAR(500)   NULL,
    F_CreatorTime       DATETIME        NULL DEFAULT GETDATE(),
    F_CreatorUserId     NVARCHAR(50)    NULL,
    F_LastModifyTime    DATETIME        NULL,
    F_LastModifyUserId  NVARCHAR(50)    NULL,
    F_DeleteMark        INT             NULL DEFAULT 0,
    F_TenantId          NVARCHAR(50)    NULL DEFAULT 'default'
);
GO

-- 1.4 菜单表
IF OBJECT_ID('dbo.base_module', 'U') IS NULL
CREATE TABLE dbo.base_module (
    F_Id                NVARCHAR(50)    PRIMARY KEY,
    F_ParentId          NVARCHAR(50)    NULL DEFAULT '0',
    F_FullName          NVARCHAR(100)   NULL,
    F_EnCode            NVARCHAR(100)   NULL,
    F_Icon              NVARCHAR(100)   NULL,
    F_UrlAddress        NVARCHAR(500)   NULL,
    F_Category          NVARCHAR(20)    NULL DEFAULT 'Web',
    F_Type              INT             NULL,
    F_IsPublic          INT             NULL DEFAULT 0,
    F_SortCode          INT             NULL,
    F_EnabledMark       INT             NULL DEFAULT 1,
    F_Description       NVARCHAR(500)   NULL,
    F_CreatorTime       DATETIME        NULL DEFAULT GETDATE(),
    F_CreatorUserId     NVARCHAR(50)    NULL,
    F_LastModifyTime    DATETIME        NULL,
    F_LastModifyUserId  NVARCHAR(50)    NULL,
    F_DeleteMark        INT             NULL DEFAULT 0,
    F_TenantId          NVARCHAR(50)    NULL DEFAULT 'default'
);
GO

-- 1.5 组织/部门表
IF OBJECT_ID('dbo.base_organize', 'U') IS NULL
CREATE TABLE dbo.base_organize (
    F_Id                NVARCHAR(50)    PRIMARY KEY,
    F_ParentId          NVARCHAR(50)    NULL DEFAULT '0',
    F_FullName          NVARCHAR(100)   NULL,
    F_EnCode            NVARCHAR(50)    NULL,
    F_Type              NVARCHAR(20)    NULL DEFAULT 'department',
    F_SortCode          INT             NULL,
    F_EnabledMark       INT             NULL DEFAULT 1,
    F_CreatorTime       DATETIME        NULL DEFAULT GETDATE(),
    F_CreatorUserId     NVARCHAR(50)    NULL,
    F_DeleteMark        INT             NULL DEFAULT 0,
    F_TenantId          NVARCHAR(50)    NULL DEFAULT 'default'
);
GO

-- 1.6 岗位表
IF OBJECT_ID('dbo.base_position', 'U') IS NULL
CREATE TABLE dbo.base_position (
    F_Id                NVARCHAR(50)    PRIMARY KEY,
    F_FullName          NVARCHAR(100)   NULL,
    F_EnCode            NVARCHAR(50)    NULL,
    F_OrganizeId        NVARCHAR(50)    NULL,
    F_SortCode          INT             NULL,
    F_EnabledMark       INT             NULL DEFAULT 1,
    F_CreatorTime       DATETIME        NULL DEFAULT GETDATE(),
    F_CreatorUserId     NVARCHAR(50)    NULL,
    F_DeleteMark        INT             NULL DEFAULT 0,
    F_TenantId          NVARCHAR(50)    NULL DEFAULT 'default'
);
GO

-- 1.7 字典类型表
IF OBJECT_ID('dbo.base_dictionary_type', 'U') IS NULL
CREATE TABLE dbo.base_dictionary_type (
    F_Id                NVARCHAR(50)    PRIMARY KEY,
    F_FullName          NVARCHAR(100)   NULL,
    F_EnCode            NVARCHAR(50)    NULL,
    F_IsTree            INT             NULL DEFAULT 0,
    F_SortCode          INT             NULL,
    F_EnabledMark       INT             NULL DEFAULT 1,
    F_CreatorTime       DATETIME        NULL DEFAULT GETDATE(),
    F_DeleteMark        INT             NULL DEFAULT 0,
    F_TenantId          NVARCHAR(50)    NULL DEFAULT 'default'
);
GO

-- 1.8 字典数据表
IF OBJECT_ID('dbo.base_dictionary_data', 'U') IS NULL
CREATE TABLE dbo.base_dictionary_data (
    F_Id                NVARCHAR(50)    PRIMARY KEY,
    F_DictionaryTypeId  NVARCHAR(50)    NULL,
    F_ParentId          NVARCHAR(50)    NULL DEFAULT '0',
    F_FullName          NVARCHAR(100)   NULL,
    F_EnCode            NVARCHAR(50)    NULL,
    F_SortCode          INT             NULL,
    F_EnabledMark       INT             NULL DEFAULT 1,
    F_CreatorTime       DATETIME        NULL DEFAULT GETDATE(),
    F_DeleteMark        INT             NULL DEFAULT 0,
    F_TenantId          NVARCHAR(50)    NULL DEFAULT 'default'
);
GO

-- 1.9 租户表
IF OBJECT_ID('dbo.base_tenant', 'U') IS NULL
CREATE TABLE dbo.base_tenant (
    F_Id                NVARCHAR(50)    PRIMARY KEY,
    F_FullName          NVARCHAR(100)   NULL,
    F_EnCode            NVARCHAR(50)    NULL,
    F_EnabledMark       INT             NULL DEFAULT 1,
    F_SortCode          INT             NULL,
    F_CreatorTime       DATETIME        NULL DEFAULT GETDATE(),
    F_DeleteMark        INT             NULL DEFAULT 0
);
GO

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
