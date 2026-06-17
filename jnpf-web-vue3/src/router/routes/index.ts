import type { AppRouteRecordRaw } from '/@/router/types';

import { PAGE_NOT_FOUND_ROUTE, REDIRECT_ROUTE, COMMON_ROUTE } from '/@/router/routes/basic';
import { LAYOUT } from '/@/router/constant';

import { mainOutRoutes } from './mainOut';
import { PageEnum } from '/@/enums/pageEnum';
import { t } from '/@/hooks/web/useI18n';

export const RootRoute: AppRouteRecordRaw = {
  path: '/',
  name: 'Root',
  redirect: PageEnum.BASE_HOME,
  meta: { title: 'Root' },
};

export const LoginRoute: AppRouteRecordRaw = {
  path: '/login',
  name: 'Login',
  component: () => import('/@/views/basic/login/Login.vue'),
  meta: { title: t('routes.basic.login') },
};

export const FormShortLinkRoute: AppRouteRecordRaw = {
  path: '/formShortLink',
  name: 'FormShortLink',
  component: () => import('/@/views/common/formShortLink/index.vue'),
  meta: { title: '' },
};

// ── Studio routes: all wrapped in LAYOUT for sidebar + topnav + theme ──

// /studio/expert
export const StudioExpertRoute: AppRouteRecordRaw = {
  path: '/studio/expert',
  component: LAYOUT,
  name: 'StudioExpert',
  children: [
    {
      path: 'quick-app-entry',
      name: 'QuickAppEntry',
      component: () => import('/@/views/expert/QuickAppEntry.vue'),
      meta: { title: '快速创建', hideMenu: true },
    },
    {
      path: 'my-projects',
      name: 'ProjectDashboard',
      component: () => import('/@/views/expert/ProjectDashboard.vue'),
      meta: { title: '我的项目' },
      children: [
        { path: ':id', name: 'PipelineDetail', component: () => import('/@/views/expert/PipelineManager.vue'), meta: { title: '流水线详情', hideMenu: true } },
      ],
    },
  ],
};

// /studio/founder
export const StudioFounderRoute: AppRouteRecordRaw = {
  path: '/studio/founder',
  component: LAYOUT,
  name: 'StudioFounder',
  children: [
    {
      path: 'totp-verify',
      name: 'FounderTotpVerify',
      component: () => import('/@/views/founder/FounderLogin.vue'),
      meta: { title: '创始人二次认证', hideMenu: true, ignoreAuth: true },
    },
  ],
};

// /studio/dev
export const StudioDevRoute: AppRouteRecordRaw = {
  path: '/studio/dev',
  component: LAYOUT,
  name: 'StudioDev',
  children: [
    { path: 'model-playground', name: 'ModelPlayground', component: () => import('/@/views/expert/ModelPlayground.vue'), meta: { title: '模型测试场' } },
    { path: 'ai-review', name: 'ArchitectReview', component: () => import('/@/views/expert/ArchitectReview.vue'), meta: { title: 'AI 架构评审' } },
  ],
};

// /studio/ai
export const StudioAiRoute: AppRouteRecordRaw = {
  path: '/studio/ai',
  component: LAYOUT,
  name: 'StudioAi',
  children: [
    {
      path: 'submit-requirement',
      name: 'SubmitRequirement',
      component: () => import('/@/views/studio/views/ai/submit-requirement.vue'),
      meta: { title: '提交需求' },
    },
    {
      path: 'generated-systems',
      name: 'GeneratedSystems',
      component: () => import('/@/views/studio/views/ai/generated-systems.vue'),
      meta: { title: '已生成系统' },
    },
    { path: 'usage-billing', name: 'UsageBilling', component: () => import('/@/views/studio/views/ai/usage-billing.vue'), meta: { title: '用量与计费' } },
    { path: 'ui-templates', name: 'UiTemplates', component: () => import('/@/views/studio/views/ai/ui-templates.vue'), meta: { title: 'UI 模板库' } },
  ],
};

// /studio/pipeline
export const StudioPipelineRoute: AppRouteRecordRaw = {
  path: '/studio/pipeline',
  component: LAYOUT,
  name: 'StudioPipeline',
  children: [
    {
      path: 'model-routing',
      name: 'ModelRouting',
      component: () => import('/@/views/studio/views/pipeline/model-routing.vue'),
      meta: { title: '模型路由策略' },
    },
    { path: 'stages', name: 'PipelineStages', component: () => import('/@/views/studio/views/pipeline/stages.vue'), meta: { title: '流水线阶段设置' } },
  ],
};

// /studio/knowledge
export const StudioKnowledgeRoute: AppRouteRecordRaw = {
  path: '/studio/knowledge',
  component: LAYOUT,
  name: 'StudioKnowledge',
  children: [
    {
      path: 'rule-editor',
      name: 'KnowledgeRuleEditor',
      component: () => import('/@/views/studio/views/knowledge/rule-editor.vue'),
      meta: { title: '业务规则配置中心' },
    },
    {
      path: 'domain-knowledge',
      name: 'DomainKnowledge',
      component: () => import('/@/views/studio/views/knowledge/domain-knowledge.vue'),
      meta: { title: '领域知识管理' },
    },
    {
      path: 'sandbox-config',
      name: 'SandboxConfig',
      component: () => import('/@/views/studio/views/knowledge/sandbox-config.vue'),
      meta: { title: '沙箱部署设置' },
    },
    { path: 'evals', name: 'Evals', component: () => import('/@/views/studio/views/knowledge/evals.vue'), meta: { title: '评测基准管理' } },
  ],
};

// /studio/agent
export const StudioAgentRoute: AppRouteRecordRaw = {
  path: '/studio/agent',
  component: LAYOUT,
  name: 'StudioAgent',
  meta: { title: '智能体管理' },
  children: [
    { path: 'create', name: 'AgentConfig', component: () => import('/@/views/studio/views/agent/create.vue'), meta: { title: '智能体管理' } },
    { path: 'sub-agents', name: 'SubAgents', component: () => import('/@/views/studio/views/agent/sub-agents.vue'), meta: { title: '子智能体管理' } },
    { path: 'skills', name: 'SkillsManagement', component: () => import('/@/views/studio/views/agent/skills.vue'), meta: { title: 'Skills 管理' } },
    { path: 'mcp', name: 'McpConfig', component: () => import('/@/views/studio/views/agent/mcp.vue'), meta: { title: 'MCP 配置' } },
  ],
};

// /studio/tenant
export const StudioTenantRoute: AppRouteRecordRaw = {
  path: '/studio/tenant',
  component: LAYOUT,
  name: 'StudioTenant',
  meta: { title: '租户定制' },
  children: [
    {
      path: 'industry-knowledge',
      name: 'IndustryKnowledge',
      component: () => import('/@/views/studio/views/tenant/industry-knowledge.vue'),
      meta: { title: '行业知识设置' },
    },
    { path: 'glossary', name: 'Glossary', component: () => import('/@/views/studio/views/tenant/glossary.vue'), meta: { title: '业务术语表' } },
  ],
};

// Basic routing without permission
// 未经许可的基本路由
export const basicRoutes = [
  LoginRoute,
  FormShortLinkRoute,
  RootRoute,
  StudioExpertRoute,
  StudioFounderRoute,
  StudioDevRoute,
  StudioAiRoute,
  StudioPipelineRoute,
  StudioKnowledgeRoute,
  StudioAgentRoute,
  StudioTenantRoute,
  ...mainOutRoutes,
  REDIRECT_ROUTE,
  PAGE_NOT_FOUND_ROUTE,
  COMMON_ROUTE,
];
