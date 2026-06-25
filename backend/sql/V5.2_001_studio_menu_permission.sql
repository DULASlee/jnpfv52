-- ============================================================
-- 文件: backend/sql/V5.2_001_studio_menu_permission.sql
-- 描述: AI原生开发平台菜单权限体系
-- 日期: 2026-06-17
-- 适配: JNPF v5.2 实际 schema (base_role lowercase columns)
-- ============================================================

-- 1. 角色（只插入不存在的，按 f_en_code 去重）
INSERT INTO base_role (f_id, f_full_name, f_en_code, f_type, f_enabled_mark, f_sort_code, f_creator_time, f_creator_user_id, f_delete_mark)
SELECT 'role_founder',       N'创始人',           'founder',       '2', 1, 1, GETDATE(), '349057407209541', 0 WHERE NOT EXISTS (SELECT 1 FROM base_role WHERE f_en_code='founder');
INSERT INTO base_role (f_id, f_full_name, f_en_code, f_type, f_enabled_mark, f_sort_code, f_creator_time, f_creator_user_id, f_delete_mark)
SELECT 'role_platform_admin',N'平台技术负责人',    'platform_admin','2', 1, 2, GETDATE(), '349057407209541', 0 WHERE NOT EXISTS (SELECT 1 FROM base_role WHERE f_en_code='platform_admin');
INSERT INTO base_role (f_id, f_full_name, f_en_code, f_type, f_enabled_mark, f_sort_code, f_creator_time, f_creator_user_id, f_delete_mark)
SELECT 'role_tenant_admin',  N'租户管理员',       'tenant_admin',  '2', 1, 3, GETDATE(), '349057407209541', 0 WHERE NOT EXISTS (SELECT 1 FROM base_role WHERE f_en_code='tenant_admin');
INSERT INTO base_role (f_id, f_full_name, f_en_code, f_type, f_enabled_mark, f_sort_code, f_creator_time, f_creator_user_id, f_delete_mark)
SELECT 'role_developer',     N'开发者',           'developer',     '2', 1, 4, GETDATE(), '349057407209541', 0 WHERE NOT EXISTS (SELECT 1 FROM base_role WHERE f_en_code='developer');
INSERT INTO base_role (f_id, f_full_name, f_en_code, f_type, f_enabled_mark, f_sort_code, f_creator_time, f_creator_user_id, f_delete_mark)
SELECT 'role_business_expert',N'业务专家',        'business_expert','2',1, 5, GETDATE(), '349057407209541', 0 WHERE NOT EXISTS (SELECT 1 FROM base_role WHERE f_en_code='business_expert');
INSERT INTO base_role (f_id, f_full_name, f_en_code, f_type, f_enabled_mark, f_sort_code, f_creator_time, f_creator_user_id, f_delete_mark)
SELECT 'role_normal_user',   N'普通用户',         'normal_user',   '2', 1, 6, GETDATE(), '349057407209541', 0 WHERE NOT EXISTS (SELECT 1 FROM base_role WHERE f_en_code='normal_user');

-- 2. Studio 菜单表
IF OBJECT_ID('dbo.BASE_STUDIO_MENU', 'U') IS NOT NULL DROP TABLE dbo.BASE_STUDIO_MENU;
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

-- 3. 一级菜单
INSERT INTO BASE_STUDIO_MENU (F_Id,F_ParentId,F_Name,F_Icon,F_Sort,F_RequiredRoles,F_DataScope,F_ExpandPhase,F_IsPublic,F_Comment) VALUES
(100000001,0,N'AI 原生开发平台',N'rocket-outlined',1,N'["platform_admin","founder","tenant_admin","developer","business_expert","normal_user"]','NONE','A',1,N'面向全角色的主功能区'),
(200000001,0,N'智能体与流水线配置',N'setting-outlined',2,N'["platform_admin","tenant_admin"]','NONE','A',0,NULL),
(300000001,0,N'JNPF 开发工具箱',N'tool-outlined',3,N'["developer"]','NONE','A',0,NULL),
(400000001,0,N'自博弈训练引擎',N'experiment-outlined',4,N'["founder"]','ALL','A',0,NULL);

-- 4. 一、AI 原生开发平台 子菜单
INSERT INTO BASE_STUDIO_MENU (F_Id,F_ParentId,F_Name,F_Icon,F_Sort,F_Url,F_RequiredRoles,F_DataScope,F_ExpandPhase,F_Comment) VALUES
(100000101,100000001,N'提交需求',N'edit-outlined',1,N'/studio/ai/submit-requirement',N'["platform_admin","founder","tenant_admin","developer","business_expert","normal_user"]','NONE','A',N'五阶段流水线入口'),
(100000102,100000001,N'已生成系统',N'appstore-outlined',2,N'/studio/ai/generated-systems',N'["platform_admin","founder","tenant_admin","developer","business_expert","normal_user"]','TENANT','A',N'带红点数字提示'),
(100000103,100000001,N'UI 模板库',N'block-outlined',3,N'/studio/ai/ui-templates',N'["platform_admin","founder","tenant_admin","developer","business_expert","normal_user"]','NONE','A',N'模板市场+工坊'),
(100000104,100000001,N'用量与计费',N'account-book-outlined',4,N'/studio/ai/usage-billing',N'["platform_admin","founder","tenant_admin","developer","business_expert","normal_user"]','OWN','A',N'按角色看不同范围');

-- 5. 二、智能体与流水线配置 子菜单
-- 2.1 智能体管理（容器）
INSERT INTO BASE_STUDIO_MENU (F_Id,F_ParentId,F_Name,F_Icon,F_Sort,F_RequiredRoles,F_DataScope,F_ExpandPhase) VALUES
(200000100,200000001,N'智能体管理',N'robot-outlined',1,N'["platform_admin"]','NONE','A');
INSERT INTO BASE_STUDIO_MENU (F_Id,F_ParentId,F_Name,F_Sort,F_Url,F_RequiredRoles,F_DataScope,F_ExpandPhase) VALUES
(200000101,200000100,N'智能体创建与配置',1,N'/studio/agent/create',N'["platform_admin"]','ALL','A'),
(200000102,200000100,N'子智能体管理',2,N'/studio/agent/sub-agents',N'["platform_admin"]','ALL','A'),
(200000103,200000100,N'Skills 管理',3,N'/studio/agent/skills',N'["platform_admin"]','ALL','A'),
(200000104,200000100,N'MCP 配置',4,N'/studio/agent/mcp',N'["platform_admin"]','ALL','A');
-- 2.2 流水线配置（容器）
INSERT INTO BASE_STUDIO_MENU (F_Id,F_ParentId,F_Name,F_Icon,F_Sort,F_RequiredRoles,F_DataScope,F_ExpandPhase) VALUES
(200000200,200000001,N'流水线配置',N'branches-outlined',2,N'["platform_admin"]','NONE','A');
INSERT INTO BASE_STUDIO_MENU (F_Id,F_ParentId,F_Name,F_Sort,F_Url,F_RequiredRoles,F_DataScope,F_ExpandPhase) VALUES
(200000201,200000200,N'流水线阶段设置',1,N'/studio/pipeline/stages',N'["platform_admin"]','ALL','A'),
(200000202,200000200,N'模型路由策略',2,N'/studio/pipeline/model-routing',N'["platform_admin"]','ALL','A');
-- 2.3 业务知识管理（容器）
INSERT INTO BASE_STUDIO_MENU (F_Id,F_ParentId,F_Name,F_Icon,F_Sort,F_RequiredRoles,F_DataScope,F_ExpandPhase) VALUES
(200000300,200000001,N'业务知识管理',N'book-outlined',3,N'["platform_admin","tenant_admin"]','NONE','A');
INSERT INTO BASE_STUDIO_MENU (F_Id,F_ParentId,F_Name,F_Sort,F_Url,F_RequiredRoles,F_DataScope,F_ExpandPhase) VALUES
(200000301,200000300,N'业务规则配置中心',1,N'/studio/knowledge/rule-editor',N'["platform_admin","tenant_admin"]','TENANT','C'),
(200000302,200000300,N'领域知识管理',2,N'/studio/knowledge/domain-knowledge',N'["platform_admin","tenant_admin"]','TENANT','C'),
(200000303,200000300,N'沙箱部署设置',3,N'/studio/knowledge/sandbox-config',N'["platform_admin"]','ALL','A'),
(200000304,200000300,N'评测基准管理',4,N'/studio/knowledge/evals',N'["platform_admin"]','ALL','A');
-- 2.4 租户定制（容器）
INSERT INTO BASE_STUDIO_MENU (F_Id,F_ParentId,F_Name,F_Icon,F_Sort,F_RequiredRoles,F_DataScope,F_ExpandPhase) VALUES
(200000400,200000001,N'租户定制',N'shop-outlined',4,N'["tenant_admin"]','NONE','C');
INSERT INTO BASE_STUDIO_MENU (F_Id,F_ParentId,F_Name,F_Sort,F_Url,F_RequiredRoles,F_DataScope,F_ExpandPhase) VALUES
(200000401,200000400,N'行业知识设置',1,N'/studio/tenant/industry-knowledge',N'["tenant_admin"]','TENANT','C'),
(200000402,200000400,N'业务术语表',2,N'/studio/tenant/glossary',N'["tenant_admin"]','TENANT','C');

-- 6. 三、JNPF 开发工具箱 子菜单
INSERT INTO BASE_STUDIO_MENU (F_Id,F_ParentId,F_Name,F_Icon,F_Sort,F_Url,F_RequiredRoles,F_DataScope,F_ExpandPhase) VALUES
(300000101,300000001,N'领域模型画板',N'partition-outlined',1,N'/studio/jnpf/domain-canvas',N'["developer"]','TENANT','A'),
(300000102,300000001,N'架构图设计器',N'apartment-outlined',2,N'/studio/jnpf/arch-designer',N'["developer"]','TENANT','A'),
(300000103,300000001,N'决策表编辑器',N'table-outlined',3,N'/studio/jnpf/decision-table',N'["developer"]','TENANT','A'),
(300000104,300000001,N'表单设计器',N'form-outlined',4,N'/studio/jnpf/form-designer',N'["developer"]','TENANT','A'),
(300000105,300000001,N'大屏设计器',N'dashboard-outlined',5,N'/studio/jnpf/dashboard-designer',N'["developer"]','TENANT','A'),
(300000106,300000001,N'工作流设计器',N'schedule-outlined',6,N'/studio/jnpf/workflow-designer',N'["developer"]','TENANT','A');

-- 7. 四、自博弈训练引擎 子菜单
INSERT INTO BASE_STUDIO_MENU (F_Id,F_ParentId,F_Name,F_Icon,F_Sort,F_RequiredRoles,F_DataScope,F_ExpandPhase) VALUES
(400000100,400000001,N'引擎总控',N'control-outlined',1,N'["founder"]','ALL','A');
INSERT INTO BASE_STUDIO_MENU (F_Id,F_ParentId,F_Name,F_Sort,F_Url,F_RequiredRoles,F_DataScope,F_ExpandPhase) VALUES
(400000101,400000100,N'引擎开关与参数',1,N'/studio/foundry/engine-control',N'["founder"]','ALL','A');

INSERT INTO BASE_STUDIO_MENU (F_Id,F_ParentId,F_Name,F_Icon,F_Sort,F_RequiredRoles,F_DataScope,F_ExpandPhase) VALUES
(400000200,400000001,N'对抗角色配置',N'team-outlined',2,N'["founder"]','ALL','A');
INSERT INTO BASE_STUDIO_MENU (F_Id,F_ParentId,F_Name,F_Sort,F_Url,F_RequiredRoles,F_DataScope,F_ExpandPhase) VALUES
(400000201,400000200,N'需求攻击者',1,N'/studio/foundry/agents/attacker',N'["founder"]','ALL','A'),
(400000202,400000200,N'系统构建者',2,N'/studio/foundry/agents/builder',N'["founder"]','ALL','A'),
(400000203,400000200,N'对抗性判官',3,N'/studio/foundry/agents/judge',N'["founder"]','ALL','A'),
(400000204,400000200,N'知识蒸馏师',4,N'/studio/foundry/agents/distiller',N'["founder"]','ALL','A');

INSERT INTO BASE_STUDIO_MENU (F_Id,F_ParentId,F_Name,F_Icon,F_Sort,F_RequiredRoles,F_DataScope,F_ExpandPhase) VALUES
(400000300,400000001,N'训练运行',N'thunderbolt-outlined',3,N'["founder"]','ALL','A');
INSERT INTO BASE_STUDIO_MENU (F_Id,F_ParentId,F_Name,F_Sort,F_Url,F_RequiredRoles,F_DataScope,F_ExpandPhase) VALUES
(400000301,400000300,N'自博弈仪表盘',1,N'/studio/foundry/dashboard',N'["founder"]','ALL','A'),
(400000302,400000300,N'因果回放池',2,N'/studio/foundry/causal-replay',N'["founder"]','ALL','A'),
(400000303,400000300,N'沙箱集群管理',3,N'/studio/foundry/sandbox-cluster',N'["founder"]','ALL','A');

INSERT INTO BASE_STUDIO_MENU (F_Id,F_ParentId,F_Name,F_Icon,F_Sort,F_RequiredRoles,F_DataScope,F_ExpandPhase) VALUES
(400000400,400000001,N'领域知识进化',N'bulb-outlined',4,N'["founder"]','ALL','A');
INSERT INTO BASE_STUDIO_MENU (F_Id,F_ParentId,F_Name,F_Sort,F_Url,F_RequiredRoles,F_DataScope,F_ExpandPhase) VALUES
(400000401,400000400,N'领域模式',1,N'/studio/foundry/knowledge/patterns',N'["founder"]','ALL','A'),
(400000402,400000400,N'知识节点',2,N'/studio/foundry/knowledge/nodes',N'["founder"]','ALL','A'),
(400000403,400000400,N'知识图谱',3,N'/studio/foundry/knowledge/graph',N'["founder"]','ALL','A'),
(400000404,400000400,N'使用统计',4,N'/studio/foundry/knowledge/stats',N'["founder"]','ALL','A'),
(400000405,400000400,N'版本历史',5,N'/studio/foundry/knowledge/versions',N'["founder"]','ALL','A'),
(400000406,400000400,N'反模式记录',6,N'/studio/foundry/knowledge/anti-patterns',N'["founder"]','ALL','A'),
(400000407,400000400,N'叙事式说明',7,N'/studio/foundry/knowledge/narratives',N'["founder"]','ALL','A'),
(400000408,400000400,N'冷启动种子',8,N'/studio/foundry/knowledge/cold-start',N'["founder"]','ALL','A'),
(400000409,400000400,N'遗忘机制',9,N'/studio/foundry/knowledge/forgetting',N'["founder"]','ALL','A');

INSERT INTO BASE_STUDIO_MENU (F_Id,F_ParentId,F_Name,F_Icon,F_Sort,F_RequiredRoles,F_DataScope,F_ExpandPhase) VALUES
(400000500,400000001,N'知识补丁',N'cloud-upload-outlined',5,N'["founder"]','ALL','A');
INSERT INTO BASE_STUDIO_MENU (F_Id,F_ParentId,F_Name,F_Sort,F_Url,F_RequiredRoles,F_DataScope,F_ExpandPhase) VALUES
(400000501,400000500,N'Patch 审核与签发',1,N'/studio/foundry/patch/review',N'["founder"]','ALL','A'),
(400000502,400000500,N'Patch 接收日志',2,N'/studio/foundry/patch/logs',N'["founder"]','ALL','A');

-- 8. 菜单红点表
IF OBJECT_ID('dbo.BASE_MENU_BADGE', 'U') IS NOT NULL DROP TABLE dbo.BASE_MENU_BADGE;
CREATE TABLE dbo.BASE_MENU_BADGE (
    F_Id              BIGINT          NOT NULL PRIMARY KEY,
    F_MenuId          BIGINT          NOT NULL,
    F_UserId          BIGINT          NOT NULL,
    F_TenantId        NVARCHAR(50)    NOT NULL,
    F_Count           INT             NOT NULL DEFAULT 0,
    F_ExtraData       NVARCHAR(MAX)   NULL,
    F_CreatorTime     DATETIME        NOT NULL DEFAULT GETDATE(),
    F_ModifyTime      DATETIME        NULL,
    CONSTRAINT UQ_BADGE UNIQUE (F_MenuId, F_UserId, F_TenantId)
);

-- 9. 验证
SELECT 'BASE_STUDIO_MENU' AS Tbl, COUNT(*) AS Cnt FROM BASE_STUDIO_MENU
UNION ALL SELECT 'BASE_MENU_BADGE', COUNT(*) FROM BASE_MENU_BADGE
UNION ALL SELECT 'BASE_ROLE (total)', COUNT(*) FROM base_role WHERE f_delete_mark IS NULL;
