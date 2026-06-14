/**
 * LLM 网关核心类型定义
 *
 * 定义大模型网关的核心接口和类型，所有供应商实现必须遵循此契约。
 * 设计原则：
 *   1. 零 `any` — 全部精确类型
 *   2. 供应商无关 — 上层代码只依赖 LLMGateway 接口
 *   3. 可测试 — 所有类型可被 Mock 实现
 *
 * @version 1.0.0
 * @module ai/llm
 */

// ============================================================
// 供应商枚举
// ============================================================

/** 支持的 LLM 供应商 */
export type LLMProvider = 'deepseek' | 'tongyi' | 'openai' | 'ollama';

// ============================================================
// 配置
// ============================================================

/** LLM 供应商配置 */
export interface LLMConfig {
  /** 供应商类型 */
  provider: LLMProvider;
  /** API 密钥（从环境变量读取，不硬编码） */
  apiKey?: string;
  /** API 基础 URL */
  baseUrl: string;
  /** 模型名称 */
  model: string;
  /** 最大输出 Token 数 */
  maxTokens?: number;
  /** 温度参数（0-2，越低越确定） */
  temperature?: number;
  /** 是否启用流式输出 */
  stream?: boolean;
}

// ============================================================
// 消息
// ============================================================

/** 聊天消息角色 */
export type ChatRole = 'system' | 'user' | 'assistant';

/** 聊天消息 */
export interface ChatMessage {
  /** 消息角色 */
  role: ChatRole;
  /** 消息内容 */
  content: string;
}

// ============================================================
// 请求 / 响应
// ============================================================

/** 聊天请求 */
export interface ChatRequest {
  /** 消息列表 */
  messages: ChatMessage[];
  /** 响应格式（text=纯文本，json=结构化 JSON） */
  responseFormat?: 'text' | 'json';
  /** 最大重试次数，默认 3 */
  maxRetries?: number;
  /** 超时时间（毫秒），默认 60000 */
  timeout?: number;
}

/** Token 用量统计 */
export interface TokenUsage {
  /** 提示词 Token 数 */
  promptTokens: number;
  /** 补全 Token 数 */
  completionTokens: number;
  /** 总 Token 数 */
  totalTokens: number;
}

/** 聊天响应 */
export interface ChatResponse {
  /** 响应内容 */
  content: string;
  /** Token 用量 */
  usage: TokenUsage;
  /** 实际使用的模型 */
  model: string;
  /** 实际使用的供应商 */
  provider: string;
  /** 请求延迟（毫秒） */
  latency: number;
}

/** 流式输出块 */
export interface ChatStreamChunk {
  /** 增量内容 */
  delta: string;
  /** 是否为最后一个块 */
  done: boolean;
  /** 完成后的用量统计（仅 done=true 时有值） */
  usage?: TokenUsage;
}

// ============================================================
// 网关接口（所有供应商实现此接口）
// ============================================================

/**
 * LLM 网关接口
 *
 * 所有 LLM 供应商（DeepSeek、通义千问、OpenAI、Ollama）
 * 以及降级网关（FallbackLLMGateway）都必须实现此接口。
 */
export interface LLMGateway {
  /**
   * 发送聊天请求（非流式）
   *
   * @param request - 聊天请求
   * @returns 聊天响应
   * @throws {Error} 网络超时、HTTP 错误、JSON 解析失败时抛出
   */
  chat(request: ChatRequest): Promise<ChatResponse>;

  /**
   * 发送聊天请求（流式输出）
   *
   * 返回 AsyncGenerator，每次 yield 增量内容字符串。
   * 调用方用 `for await (const chunk of generator)` 消费。
   *
   * @param request - 聊天请求
   * @returns 异步生成器，逐块产出文本
   */
  chatStream(request: ChatRequest): AsyncGenerator<string, void, undefined>;

  /**
   * 健康检查
   *
   * 发送轻量 ping 请求验证服务可用。
   *
   * @returns true=可用，false=不可用
   */
  healthCheck(): Promise<boolean>;

  /**
   * 获取供应商信息
   *
   * @returns 当前供应商和模型名
   */
  getProviderInfo(): { provider: string; model: string };
}

// ============================================================
// 供应商累计统计（可选，DeepSeek 等实现可用）
// ============================================================

/** 供应商用量累计统计 */
export interface ProviderUsageStats {
  /** 总请求数 */
  requestCount: number;
  /** 总成功数 */
  successCount: number;
  /** 总失败数 */
  failureCount: number;
  /** 累计 Token 用量 */
  totalTokens: TokenUsage;
  /** 平均延迟（毫秒） */
  averageLatency: number;
}
