/**
 * 多供应商降级网关
 *
 * 按优先级顺序尝试多个 LLM 供应商，当前供应商连续失败达到阈值后自动切换。
 * 支持运行时动态切换、全量健康检查、当前供应商信息查询。
 *
 * 默认降级链：DeepSeek V4 Pro → MiMo-2.5-Pro → DeepSeek 标准 → 通义千问 → Ollama
 *
 * @version 1.0.0
 * @module ai/llm/fallback
 */

import type { LLMGateway, ChatRequest, ChatResponse, ChatMessage, ProviderUsageStats, TokenUsage } from './types';

/** 连续失败阈值，超过后自动切换到下一个供应商 */
const DEFAULT_MAX_CONSECUTIVE_FAILURES = 3;

export class FallbackLLMGateway implements LLMGateway {
  private readonly gateways: LLMGateway[];
  private readonly maxConsecutiveFailures: number;
  private currentIndex = 0;
  private failureCounts: number[];

  constructor(gateways: LLMGateway[], options?: { maxConsecutiveFailures?: number }) {
    if (!gateways || gateways.length === 0) {
      throw new Error('[FallbackLLMGateway] 必须提供至少一个 LLM 供应商');
    }
    this.gateways = gateways;
    this.maxConsecutiveFailures = options?.maxConsecutiveFailures ?? DEFAULT_MAX_CONSECUTIVE_FAILURES;
    this.failureCounts = new Array(gateways.length).fill(0);
  }

  // ============================================================
  // 公开接口
  // ============================================================

  /** @inheritdoc */
  async chat(request: ChatRequest): Promise<ChatResponse> {
    const errors: string[] = [];
    const tried = new Set<number>();

    // 从当前供应商开始轮询
    for (let offset = 0; offset < this.gateways.length; offset++) {
      const idx = (this.currentIndex + offset) % this.gateways.length;
      if (tried.has(idx)) continue;
      tried.add(idx);

      const gw = this.gateways[idx];
      try {
        const response = await gw.chat(request);
        // 成功 — 重置该供应商失败计数，更新当前索引
        this.failureCounts[idx] = 0;
        this.currentIndex = idx;
        return response;
      } catch (e) {
        const msg = (e as Error).message;
        errors.push(`[${gw.getProviderInfo().provider}] ${msg}`);
        this.failureCounts[idx]++;

        // 连续失败超阈值 → 降级
        if (this.failureCounts[idx] >= this.maxConsecutiveFailures) {
          console.warn(`[FallbackLLMGateway] ${gw.getProviderInfo().provider} 连续失败 ${this.failureCounts[idx]} 次，已降级`);
        }
      }
    }

    throw new Error(`[FallbackLLMGateway] 全部 ${this.gateways.length} 个供应商均失败:\n${errors.join('\n')}`);
  }

  /** @inheritdoc */
  async *chatStream(request: ChatRequest): AsyncGenerator<string, void, undefined> {
    const gw = this.gateways[this.currentIndex];
    try {
      yield* gw.chatStream(request);
      // 流式成功，重置失败计数
      this.failureCounts[this.currentIndex] = 0;
    } catch (e) {
      this.failureCounts[this.currentIndex]++;
      // 如果当前供应商流式失败，尝试下一个
      if (this.failureCounts[this.currentIndex] >= this.maxConsecutiveFailures) {
        await this.trySwitchProvider();
      }
      throw new Error(`[FallbackLLMGateway] 流式输出失败 (${gw.getProviderInfo().provider}): ${(e as Error).message}`);
    }
  }

  /** @inheritdoc */
  async healthCheck(): Promise<boolean> {
    const results = await Promise.all(
      this.gateways.map(async gw => {
        try {
          const alive = await gw.healthCheck();
          return { provider: gw.getProviderInfo().provider, alive };
        } catch {
          return { provider: gw.getProviderInfo().provider, alive: false };
        }
      }),
    );

    // 至少一个可用即认为整体健康
    const anyAlive = results.some(r => r.alive);
    if (!anyAlive) {
      console.warn('[FallbackLLMGateway] 所有供应商均不可用');
    }

    // 记录不健康的供应商
    for (const r of results) {
      if (!r.alive) {
        console.warn(`[FallbackLLMGateway] ${r.provider} 健康检查失败`);
      }
    }

    return anyAlive;
  }

  /** @inheritdoc */
  getProviderInfo(): { provider: string; model: string } {
    const current = this.gateways[this.currentIndex];
    if (!current) {
      return { provider: 'fallback', model: 'none' };
    }
    const info = current.getProviderInfo();
    return { provider: `fallback:${info.provider}`, model: info.model };
  }

  // ============================================================
  // 扩展方法
  // ============================================================

  /** 获取当前主供应商 */
  getCurrentGateway(): LLMGateway {
    return this.gateways[this.currentIndex];
  }

  /** 获取所有供应商列表 */
  getGateways(): LLMGateway[] {
    return [...this.gateways];
  }

  /** 获取所有供应商的健康状态 */
  async getHealthStatus(): Promise<Array<{ provider: string; model: string; alive: boolean; consecutiveFailures: number }>> {
    const results = await Promise.all(
      this.gateways.map(async (gw, i) => {
        let alive = false;
        try {
          alive = await gw.healthCheck();
        } catch {
          // alive stays false
        }
        const info = gw.getProviderInfo();
        return {
          provider: info.provider,
          model: info.model,
          alive,
          consecutiveFailures: this.failureCounts[i] ?? 0,
        };
      }),
    );
    return results;
  }

  /** 手动切换到指定索引的供应商 */
  switchTo(index: number): void {
    if (index < 0 || index >= this.gateways.length) {
      throw new Error(`[FallbackLLMGateway] 无效的供应商索引: ${index}`);
    }
    this.currentIndex = index;
  }

  /** 重置所有失败计数 */
  resetFailures(): void {
    this.failureCounts = new Array(this.gateways.length).fill(0);
  }

  // ============================================================
  // 内部方法
  // ============================================================

  /** 尝试切换到下一个健康供应商 */
  private async trySwitchProvider(): Promise<void> {
    const startIdx = this.currentIndex;
    for (let offset = 1; offset < this.gateways.length; offset++) {
      const idx = (startIdx + offset) % this.gateways.length;
      if (this.failureCounts[idx] < this.maxConsecutiveFailures) {
        const alive = await this.gateways[idx].healthCheck().catch(() => false);
        if (alive) {
          console.log(
            `[FallbackLLMGateway] 切换供应商: ${this.gateways[startIdx].getProviderInfo().provider} → ${this.gateways[idx].getProviderInfo().provider}`,
          );
          this.currentIndex = idx;
          return;
        }
      }
    }
  }
}
