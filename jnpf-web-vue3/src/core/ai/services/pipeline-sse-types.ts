/**
 * PipelineSSEEvent — SSE 语义事件协议类型定义（P1 · 审判令三）
 *
 * AI 流水线运行期间，通过 SSE 向前端推送结构化语义事件，
 * 实现 AI 思考过程的白盒化。Agent 通过 checkpoint() 方法在关键节点推送事件。
 *
 * @module ai/services/pipeline-sse-types
 * @version 1.0.0
 */

// ============================================================
// 核心事件类型
// ============================================================

export type PipelineStage = 'requirement' | 'architecture' | 'design' | 'development' | 'delivery';

export interface PipelineSSEEvent {
  /** 当前阶段 */
  stage: PipelineStage;
  /** 当前子阶段（如 "analyzing_requirements", "generating_architecture"） */
  phase: string;
  /** 0-100 进度 */
  progress: number;
  /** 人类可读的思考描述 */
  thought: string;
  /** 当前执行的智能体名称 */
  agent: string;
  /** 可选：风险提示 */
  warning?: string;
  /** 可选：超时警报 */
  timeout_alert?: boolean;
  /** 事件时间戳 */
  timestamp: string;
  /** 已耗时毫秒 */
  elapsed_ms: number;
  /** 可选：预计剩余时间毫秒 */
  estimated_remaining_ms?: number;
}

// ============================================================
// 超时配置
// ============================================================

export interface AgentTimeoutConfig {
  /** 预期完成时间（毫秒） */
  expected_ms: number;
  /** 最大允许时间（毫秒） */
  max_ms: number;
}

// ============================================================
// 默认超时配置
// ============================================================

export const DEFAULT_TIMEOUT_CONFIGS: Record<string, AgentTimeoutConfig> = {
  RequirementAnalystAgent: { expected_ms: 30000, max_ms: 60000 },
  ArchitectAgent: { expected_ms: 45000, max_ms: 90000 },
  UIAgent: { expected_ms: 30000, max_ms: 60000 },
  DatabaseAgent: { expected_ms: 30000, max_ms: 60000 },
  WorkflowAgent: { expected_ms: 30000, max_ms: 60000 },
  RuleEngineAgent: { expected_ms: 30000, max_ms: 60000 },
};
