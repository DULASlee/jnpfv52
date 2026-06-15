/**
 * AI Pipeline API (Week 1 Day 5 — 对齐后端 AIDevelopmentPipelineService).
 *
 * 流水线 CRUD + 阶段控制 + Bug 修复 + 二次需求
 */
import { defHttp } from '/@/utils/http/axios';
import { getFounderToken } from './index';

function authHeaders(): Record<string, string> {
  const token = getFounderToken();
  return token ? { 'X-Founder-Token': token } : {};
}

const baseUrl = '/api/founder/ai/pipeline';

// ─── 流水线 CRUD ───

/** 创建流水线 */
export function createPipeline(data: PipelineCreateRequest) {
  return defHttp.post({ url: `${baseUrl}/create`, data, headers: authHeaders() });
}

/** 启动流水线 */
export function startPipeline(id: number) {
  return defHttp.post({ url: `${baseUrl}/${id}/start`, headers: authHeaders() });
}

/** 执行当前阶段 */
export function executeNextStage(id: number) {
  return defHttp.post({ url: `${baseUrl}/${id}/execute`, headers: authHeaders() });
}

/** 确认阶段（人工审核） */
export function confirmStage(stageId: number, data: StageConfirmation) {
  return defHttp.post({ url: `${baseUrl}/stage/${stageId}/confirm`, data, headers: authHeaders() });
}

/** 获取流水线详情 */
export function getPipelineDetail(id: number) {
  return defHttp.get({ url: `${baseUrl}/${id}`, headers: authHeaders() });
}

/** 分页查询流水线列表 */
export function getPipelineList(pageIndex = 0, pageSize = 20) {
  return defHttp.get({ url: `${baseUrl}/list`, params: { pageIndex, pageSize }, headers: authHeaders() });
}

/** 下载源码 */
export function downloadSourceCode(id: number) {
  return defHttp.get({ url: `${baseUrl}/${id}/download-source`, responseType: 'blob', headers: authHeaders() }, { isReturnNativeResponse: true });
}

// ─── 迭代开发 ───

const iterationUrl = '/api/founder/ai/iteration';

/** Bug 修复 */
export function fixBug(data: BugFixRequest) {
  return defHttp.post({ url: `${iterationUrl}/fix-bug`, data, headers: authHeaders() });
}

/** 二次需求 */
export function implementFeature(data: FeatureRequest) {
  return defHttp.post({ url: `${iterationUrl}/implement-feature`, data, headers: authHeaders() });
}

// ─── AI 测试（Day 2 对齐）───

const testUrl = '/api/founder/ai';

/** 测试 LLM 聊天 */
export function testChat(data: TestChatRequest) {
  return defHttp.post({ url: `${testUrl}/test`, data, headers: authHeaders() });
}

/** 测试 LLM 健康检查 */
export function testHealth(data?: TestHealthRequest) {
  return defHttp.post({ url: `${testUrl}/health`, data: data ?? {}, headers: authHeaders() });
}

// ─── 类型定义（对齐后端 DTO）───

export interface PipelineCreateRequest {
  name: string;
  pipelineType?: string;
  userRequirement: string;
}

export interface PipelineSummary {
  id: number;
  name: string;
  pipelineType: string;
  currentStage: string;
  status: string;
  updatedAt: string;
}

export interface StageInfo {
  id: number;
  stageName: string;
  status: string;
  stageOrder: number;
  tokensUsed?: number;
}

export interface PipelineDetail {
  id: number;
  name: string;
  currentStage: string;
  status: string;
  stages: StageInfo[];
  generatedSystem?: {
    sandboxId: string;
    accessUrl: string;
    adminUsername: string;
    adminPassword: string;
    expiresAt: string;
  };
  stats: {
    totalTokens: number;
    totalCostUSD: number;
    totalLatencyMs: number;
  };
}

export interface StageConfirmation {
  approved: boolean;
  comment: string;
}

export interface BugFixRequest {
  sandboxId: string;
  bugDescription: string;
  reproductionSteps: string;
  errorLogs?: string;
  screenshots?: string[];
}

export interface FeatureRequest {
  sandboxId: string;
  featureDescription: string;
  attachments?: string[];
}

export interface IterationResult {
  isSuccess: boolean;
  iterationType: 'bug_fix' | 'feature_request';
  description: string;
  changedFiles: ChangedFile[];
  message: string;
  tokensUsed: number;
  latencyMs: number;
}

export interface ChangedFile {
  filePath: string;
  changeType: 'created' | 'modified' | 'deleted';
  diffContent: string;
}

export interface TestChatRequest {
  prompt: string;
  providerCode?: string;
}

export interface TestHealthRequest {
  providerCode?: string;
}
