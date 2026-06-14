/**
 * Ollama 本地模型网关实现
 *
 * 对接本地 Ollama 服务，无需 API Key。
 * 端点格式与 OpenAI 不同（/api/chat），独立实现。
 *
 * @version 1.0.0
 * @module ai/llm/ollama
 */

import type { LLMGateway, ChatRequest, ChatResponse, ChatMessage, ProviderUsageStats, TokenUsage } from './types';

// ============================================================
// 配置
// ============================================================

const DEFAULT_BASE_URL = 'http://localhost:11434';
const DEFAULT_MODEL = 'llama3';
const DEFAULT_TIMEOUT_MS = 120000; // 本地模型可能较慢
const HEALTH_CHECK_TIMEOUT_MS = 5000;

// ============================================================
// OllamaGateway
// ============================================================

export class OllamaGateway implements LLMGateway {
  private readonly baseUrl: string;
  private readonly model: string;
  private readonly defaultTimeout: number;

  private stats: ProviderUsageStats = {
    requestCount: 0,
    successCount: 0,
    failureCount: 0,
    totalTokens: { promptTokens: 0, completionTokens: 0, totalTokens: 0 },
    averageLatency: 0,
  };

  constructor(options?: { baseUrl?: string; model?: string; defaultTimeout?: number }) {
    this.baseUrl = options?.baseUrl ?? import.meta.env.VITE_OLLAMA_BASE_URL ?? DEFAULT_BASE_URL;
    this.model = options?.model ?? DEFAULT_MODEL;
    this.defaultTimeout = options?.defaultTimeout ?? DEFAULT_TIMEOUT_MS;
  }

  /** @inheritdoc */
  async chat(request: ChatRequest): Promise<ChatResponse> {
    const startTime = performance.now();
    const timeout = request.timeout ?? this.defaultTimeout;

    try {
      const signal = AbortSignal.timeout(timeout);
      const response = await fetch(`${this.baseUrl}/api/chat`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          model: this.model,
          messages: request.messages.map(m => ({ role: m.role, content: m.content })),
          stream: false,
          format: request.responseFormat === 'json' ? 'json' : undefined,
        }),
        signal,
      });

      if (!response.ok) {
        const errorText = await response.text().catch(() => '');
        throw new Error(`HTTP ${response.status}: ${errorText}`);
      }

      const data = await response.json();
      const content = (data.message?.content as string) ?? '';

      const usage: TokenUsage = {
        promptTokens: (data.prompt_eval_count as number) ?? 0,
        completionTokens: (data.eval_count as number) ?? 0,
        totalTokens: ((data.prompt_eval_count as number) ?? 0) + ((data.eval_count as number) ?? 0),
      };

      this.stats.requestCount++;
      this.stats.successCount++;
      this.accumulateTokens(usage);
      const latency = performance.now() - startTime;
      this.updateAverageLatency(latency);

      return {
        content,
        usage,
        model: (data.model as string) ?? this.model,
        provider: 'ollama',
        latency: Math.round(latency),
      };
    } catch (e) {
      this.stats.failureCount++;
      throw new Error(`[Ollama] 请求失败: ${(e as Error).message}`);
    }
  }

  /** @inheritdoc */
  async *chatStream(request: ChatRequest): AsyncGenerator<string, void, undefined> {
    const timeout = request.timeout ?? this.defaultTimeout;
    const signal = AbortSignal.timeout(timeout);

    try {
      const response = await fetch(`${this.baseUrl}/api/chat`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          model: this.model,
          messages: request.messages.map(m => ({ role: m.role, content: m.content })),
          stream: true,
        }),
        signal,
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

      try {
        while (true) {
          const { done, value } = await reader.read();
          if (done) break;

          buffer += decoder.decode(value, { stream: true });
          const lines = buffer.split('\n');
          buffer = lines.pop() ?? '';

          for (const line of lines) {
            const trimmed = line.trim();
            if (!trimmed) continue;

            try {
              const parsed = JSON.parse(trimmed);
              const delta = parsed.message?.content as string | undefined;
              if (delta) {
                accumulatedContent += delta;
                yield delta;
              }
              if (parsed.done) {
                return;
              }
            } catch {
              // 跳过无法解析的行
            }
          }
        }
      } finally {
        reader.releaseLock();
      }

      this.stats.requestCount++;
      this.stats.successCount++;
      this.accumulateTokens({
        promptTokens: this.estimateTokens(request.messages.map(m => m.content).join(' ')),
        completionTokens: this.estimateTokens(accumulatedContent),
        totalTokens: 0,
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
      const response = await fetch(`${this.baseUrl}/api/tags`, { signal });
      return response.ok;
    } catch {
      return false;
    }
  }

  /** @inheritdoc */
  getProviderInfo(): { provider: string; model: string } {
    return { provider: 'ollama', model: this.model };
  }

  /** 获取累计用量统计 */
  getUsageStats(): ProviderUsageStats {
    return { ...this.stats, totalTokens: { ...this.stats.totalTokens } };
  }

  // ============================================================
  // 内部方法
  // ============================================================

  private accumulateTokens(usage: TokenUsage): void {
    this.stats.totalTokens.promptTokens += usage.promptTokens;
    this.stats.totalTokens.completionTokens += usage.completionTokens;
    this.stats.totalTokens.totalTokens += usage.totalTokens;
  }

  private updateAverageLatency(latency: number): void {
    const total = this.stats.successCount + this.stats.failureCount;
    if (total <= 1) {
      this.stats.averageLatency = latency;
    } else {
      this.stats.averageLatency = this.stats.averageLatency * 0.9 + latency * 0.1;
    }
  }

  private estimateTokens(text: string): number {
    const chineseChars = (text.match(/[一-鿿]/g) ?? []).length;
    const otherChars = text.length - chineseChars;
    return Math.ceil(chineseChars + otherChars / 4);
  }
}
