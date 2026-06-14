/**
 * LLM 网关统一导出
 *
 * 集中导出所有 LLM 供应商实现、降级网关和核心类型。
 *
 * @version 1.0.0
 * @module ai/llm
 */

// ─── 核心类型 ───
export type {
  LLMProvider,
  LLMConfig,
  ChatRole,
  ChatMessage,
  ChatRequest,
  TokenUsage,
  ChatResponse,
  ChatStreamChunk,
  LLMGateway,
  ProviderUsageStats,
} from './types';

// ─── 供应商实现 ───
export { DeepSeekGateway } from './deepseek';
export { DeepSeekV4Gateway } from './deepseek-v4';
export { MiMoGateway } from './mimo';
export { TongyiGateway } from './tongyi';
export { OpenAIGateway } from './openai';
export { OllamaGateway } from './ollama';

// ─── 降级网关 ───
export { FallbackLLMGateway } from './fallback';
