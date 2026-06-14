/**
 * DeepSeek 标准网关实现
 *
 * 对接 DeepSeek Chat Completion API（兼容 OpenAI 格式）。
 * API Key 从环境变量 VITE_DEEPSEEK_API_KEY 读取，不硬编码。
 *
 * 特性：
 *   - 指数退避重试（maxRetries=3，默认）
 *   - 流式输出（SSE 解析）
 *   - Token 累计统计
 *   - 健康检查（5s 超时 ping）
 *
 * @version 1.0.0
 * @module ai/llm/deepseek
 */

import type { LLMGateway, ChatRequest, ChatResponse, ChatMessage, ProviderUsageStats, TokenUsage } from './types';

// ============================================================
// 配置
// ============================================================

const DEFAULT_BASE_URL = 'https://api.deepseek.com/v1';
const DEFAULT_MODEL = 'deepseek-chat';
const DEFAULT_MAX_RETRIES = 3;
const DEFAULT_TIMEOUT_MS = 60000;
const HEALTH_CHECK_TIMEOUT_MS = 5000;

/** 可重置的 DeepSeek 请求（用于重试） */
interface PreparedRequest {
  url: string;
  headers: Record<string, string>;
  body: string;
  signal: AbortSignal;
}

// ============================================================
// DeepSeekGateway
// ============================================================

export class DeepSeekGateway implements LLMGateway {
  private readonly apiKey: string;
  private readonly baseUrl: string;
  private readonly model: string;
  private readonly maxRetries: number;
  private readonly defaultTimeout: number;

  // 累计统计
  private stats: ProviderUsageStats = {
    requestCount: 0,
    successCount: 0,
    failureCount: 0,
    totalTokens: { promptTokens: 0, completionTokens: 0, totalTokens: 0 },
    averageLatency: 0,
  };

  constructor(options?: { apiKey?: string; baseUrl?: string; model?: string; maxRetries?: number; defaultTimeout?: number }) {
    this.apiKey = options?.apiKey ?? import.meta.env.VITE_DEEPSEEK_API_KEY ?? '';
    this.baseUrl = options?.baseUrl ?? import.meta.env.VITE_DEEPSEEK_BASE_URL ?? DEFAULT_BASE_URL;
    this.model = options?.model ?? DEFAULT_MODEL;
    this.maxRetries = options?.maxRetries ?? DEFAULT_MAX_RETRIES;
    this.defaultTimeout = options?.defaultTimeout ?? DEFAULT_TIMEOUT_MS;

    if (!this.apiKey) {
      console.warn('[DeepSeek] VITE_DEEPSEEK_API_KEY 未配置，API 调用将失败');
    }
  }

  // ============================================================
  // 公开接口
  // ============================================================

  /** @inheritdoc */
  async chat(request: ChatRequest): Promise<ChatResponse> {
    const startTime = performance.now();
    const maxRetries = request.maxRetries ?? this.maxRetries;
    const timeout = request.timeout ?? this.defaultTimeout;

    let lastError: Error | null = null;

    for (let attempt = 0; attempt <= maxRetries; attempt++) {
      try {
        const signal = AbortSignal.timeout(timeout);
        const prepared = this.prepareRequest(request, signal);
        const response = await this.doChat(prepared);

        // 更新统计
        this.stats.requestCount++;
        this.stats.successCount++;
        this.accumulateTokens(response.usage);
        const latency = performance.now() - startTime;
        this.updateAverageLatency(latency);

        return {
          ...response,
          latency: Math.round(latency),
        };
      } catch (e) {
        lastError = e as Error;
        this.stats.failureCount++;

        if (attempt < maxRetries) {
          const delay = this.calculateBackoff(attempt);
          console.warn(`[DeepSeek] 请求失败 (尝试 ${attempt + 1}/${maxRetries + 1}): ${lastError.message}，${delay}ms 后重试...`);
          await this.sleep(delay);
        }
      }
    }

    throw new Error(`[DeepSeek] 全部 ${maxRetries + 1} 次尝试均失败: ${lastError?.message}`);
  }

  /** @inheritdoc */
  async *chatStream(request: ChatRequest): AsyncGenerator<string, void, undefined> {
    const timeout = request.timeout ?? this.defaultTimeout;
    const signal = AbortSignal.timeout(timeout);
    const prepared = this.prepareRequest({ ...request, responseFormat: 'text' }, signal);
    // 流式请求强制开启 stream
    const body = JSON.parse(prepared.body);
    body.stream = true;
    prepared.body = JSON.stringify(body);

    const startTime = performance.now();

    try {
      const response = await fetch(prepared.url, {
        method: 'POST',
        headers: prepared.headers,
        body: prepared.body,
        signal: prepared.signal,
      });

      if (!response.ok) {
        const errorText = await response.text().catch(() => '');
        throw new Error(`HTTP ${response.status}: ${errorText}`);
      }

      if (!response.body) {
        throw new Error('响应体为空');
      }

      const reader = response.body.getReader();
      const decoder = new TextDecoder();
      let buffer = '';
      let accumulatedContent = '';
      let chunkCount = 0;

      try {
        while (true) {
          const { done, value } = await reader.read();
          if (done) break;

          buffer += decoder.decode(value, { stream: true });
          const lines = buffer.split('\n');
          // 保留最后一个不完整行
          buffer = lines.pop() ?? '';

          for (const line of lines) {
            const trimmed = line.trim();
            if (!trimmed || trimmed.startsWith(':')) continue;

            if (trimmed === 'data: [DONE]') {
              return; // 流结束
            }

            if (trimmed.startsWith('data: ')) {
              const jsonStr = trimmed.slice(6);
              try {
                const parsed = JSON.parse(jsonStr);
                const delta = parsed.choices?.[0]?.delta?.content;
                if (delta) {
                  accumulatedContent += delta;
                  chunkCount++;
                  yield delta;
                }
              } catch {
                // SSE 中偶有非 JSON 行（如空行、注释），跳过
              }
            }
          }
        }
      } finally {
        reader.releaseLock();
      }

      // 更新统计
      this.stats.requestCount++;
      this.stats.successCount++;
      const latency = performance.now() - startTime;
      this.updateAverageLatency(latency);

      // 对非结构化流式输出做估算
      const estimatedPromptTokens = this.estimateTokens(request.messages.map(m => m.content).join(' '));
      const estimatedCompletionTokens = this.estimateTokens(accumulatedContent);
      this.accumulateTokens({
        promptTokens: estimatedPromptTokens,
        completionTokens: estimatedCompletionTokens,
        totalTokens: estimatedPromptTokens + estimatedCompletionTokens,
      });
    } catch (e) {
      this.stats.failureCount++;
      throw e;
    }
  }

  /** @inheritdoc */
  async healthCheck(): Promise<boolean> {
    try {
      const signal = AbortSignal.timeout(HEALTH_CHECK_TIMEOUT_MS);
      const response = await fetch(`${this.baseUrl}/models`, {
        method: 'GET',
        headers: this.authHeaders(),
        signal,
      });
      return response.ok;
    } catch {
      return false;
    }
  }

  /** @inheritdoc */
  getProviderInfo(): { provider: string; model: string } {
    return { provider: 'deepseek', model: this.model };
  }

  /** 获取累计用量统计 */
  getUsageStats(): ProviderUsageStats {
    return { ...this.stats, totalTokens: { ...this.stats.totalTokens } };
  }

  // ============================================================
  // 内部方法
  // ============================================================

  /** 构建请求参数 */
  private prepareRequest(request: ChatRequest, signal: AbortSignal): PreparedRequest {
    const messages = this.buildMessages(request);
    const body: Record<string, unknown> = {
      model: this.model,
      messages,
      stream: false,
    };

    if (request.responseFormat === 'json') {
      body.response_format = { type: 'json_object' };
    }

    return {
      url: `${this.baseUrl}/chat/completions`,
      headers: {
        ...this.authHeaders(),
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(body),
      signal,
    };
  }

  /** 发起非流式 HTTP 请求 */
  private async doChat(prepared: PreparedRequest): Promise<Omit<ChatResponse, 'latency'>> {
    const response = await fetch(prepared.url, {
      method: 'POST',
      headers: prepared.headers,
      body: prepared.body,
      signal: prepared.signal,
    });

    if (!response.ok) {
      const errorText = await response.text().catch(() => '');
      throw new Error(`HTTP ${response.status}: ${errorText}`);
    }

    const data = await response.json();
    return this.parseResponse(data);
  }

  /** 解析 DeepSeek API 响应 */
  private parseResponse(data: Record<string, unknown>): Omit<ChatResponse, 'latency'> {
    const choices = data.choices as Array<Record<string, unknown>> | undefined;
    if (!choices || choices.length === 0) {
      throw new Error('API 响应中无 choices');
    }

    const content = (choices[0].message as Record<string, unknown>)?.content as string | undefined;
    if (content === undefined) {
      throw new Error('API 响应中无 message.content');
    }

    const usage = data.usage as Record<string, number> | undefined;

    return {
      content,
      usage: {
        promptTokens: usage?.prompt_tokens ?? 0,
        completionTokens: usage?.completion_tokens ?? 0,
        totalTokens: usage?.total_tokens ?? 0,
      },
      model: (data.model as string) ?? this.model,
      provider: 'deepseek',
    };
  }

  /** 构建消息列表（必要时注入 system prompt） */
  private buildMessages(request: ChatRequest): ChatMessage[] {
    return request.messages.map(m => ({
      role: m.role,
      content: m.content,
    }));
  }

  /** 认证请求头 */
  private authHeaders(): Record<string, string> {
    return {
      Authorization: `Bearer ${this.apiKey}`,
    };
  }

  /** 指数退避计算（含随机抖动） */
  private calculateBackoff(attempt: number): number {
    const base = Math.pow(2, attempt) * 1000;
    const jitter = Math.random() * 500;
    return Math.min(base + jitter, 30000); // 最大 30s
  }

  /** 累计 Token */
  private accumulateTokens(usage: TokenUsage): void {
    this.stats.totalTokens.promptTokens += usage.promptTokens;
    this.stats.totalTokens.completionTokens += usage.completionTokens;
    this.stats.totalTokens.totalTokens += usage.totalTokens;
  }

  /** 更新平均延迟 */
  private updateAverageLatency(latency: number): void {
    const total = this.stats.successCount + this.stats.failureCount;
    if (total <= 1) {
      this.stats.averageLatency = latency;
    } else {
      // 指数移动平均
      this.stats.averageLatency = this.stats.averageLatency * 0.9 + latency * 0.1;
    }
  }

  /** 估算 Token 数（简易版：英文 4 字符≈1 token，中文 1 字符≈1 token） */
  private estimateTokens(text: string): number {
    const chineseChars = (text.match(/[一-鿿]/g) ?? []).length;
    const otherChars = text.length - chineseChars;
    return Math.ceil(chineseChars + otherChars / 4);
  }

  /** 异步延迟 */
  private sleep(ms: number): Promise<void> {
    return new Promise(resolve => setTimeout(resolve, ms));
  }
}
