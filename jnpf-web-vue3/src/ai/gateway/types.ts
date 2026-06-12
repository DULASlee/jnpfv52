/**
 * AI Gateway 前端类型定义
 *
 * 与后端 InteAssistant DTO 字段对齐。
 * 后端参考：JNPF.InteAssistant.Entitys.Dto.InteAssistant
 *
 * @version 1.0.0 — Sprint 0-B Day 9
 */

// ============================================================
// LLM 调用
// ============================================================

/** LLM 请求 */
export interface LlmRequest {
  prompt: string;
  model?: string;
  /** IR JSON Schema 约束（ir.schema.json），用于结构化输出模式 */
  schema?: unknown;
  stage?: PipelineStage;
}

/** LLM 响应 */
export interface LlmResponse {
  content: string;
  /** 结构化输出时填充（FormPageIR） */
  ir?: Record<string, unknown>;
  tokenUsage: {
    prompt: number;
    completion: number;
  };
}

// ============================================================
// Provider 健康检查（对齐后端 ProviderHealth.cs）
// ============================================================

/** Provider 健康检查结果（对齐后端 ProviderHealth.cs） */
export interface ProviderHealth {
  /** 是否健康 */
  isHealthy: boolean;
  /** Provider 名称 */
  provider: string;
  /** 延迟毫秒 */
  latencyMs: number;
  /** 错误信息（如有） */
  error?: string;
}

// ============================================================
// 五阶段流水线（对齐后端 BASE_AI_PIPELINE / BASE_AI_PIPELINE_MESSAGE）
// ============================================================

/** 流水线阶段 */
export type PipelineStage = 'draft' | 'generating' | 'validating' | 'compiling' | 'done';

/** 流水线消息角色 */
export type PipelineRole = 'user' | 'assistant' | 'system' | 'tool';

/** 流水线运行状态 */
export type PipelineRunStatus = 'running' | 'completed' | 'failed';

/** 流水线消息 */
export interface PipelineMessage {
  /** 消息 ID */
  id: string;
  /** 关联流水线 ID */
  pipelineId: string;
  /** 角色 */
  role: PipelineRole;
  /** 消息内容 */
  content: string;
  /** 所属阶段 */
  stage: PipelineStage;
  /** 序号 */
  sequence: number;
}

/** 流水线状态（对齐后端 BASE_AI_PIPELINE） */
export interface PipelineStatus {
  /** 流水线 ID */
  pipelineId: string;
  /** 流水线名称 */
  name: string;
  /** 当前阶段 */
  currentStage: PipelineStage;
  /** 运行状态 */
  status: PipelineRunStatus;
  /** 开始时间 */
  startedTime?: string;
  /** 完成时间 */
  finishedTime?: string;
  /** 消息列表 */
  messages: PipelineMessage[];
}

// ============================================================
// Prompt 模板（对齐后端 BASE_AI_PROMPT_TEMPLATE）
// ============================================================

/** Prompt 模板分类 */
export type PromptCategory = 'form' | 'dashboard' | 'workflow' | 'code';

/** Prompt 模板 */
export interface PromptTemplate {
  id: string;
  name: string;
  category: PromptCategory;
  template: string;
  version: number;
  isActive: boolean;
}
