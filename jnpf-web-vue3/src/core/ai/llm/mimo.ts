/**
 * MiMo-2.5-Pro 网关实现
 *
 * MiMo Chat Completion API，遵循 OpenAI 兼容格式。
 * API Key 从环境变量 VITE_MIMO_API_KEY 读取。
 *
 * @version 1.0.0
 * @module ai/llm/mimo
 */

import { DeepSeekGateway } from './deepseek';

const DEFAULT_MODEL = 'mimo-2.5-pro';

export class MiMoGateway extends DeepSeekGateway {
  constructor(options?: { apiKey?: string; baseUrl?: string; model?: string }) {
    const apiKey = options?.apiKey ?? import.meta.env.VITE_MIMO_API_KEY ?? '';
    const baseUrl = options?.baseUrl ?? import.meta.env.VITE_MIMO_BASE_URL;

    if (!baseUrl) {
      console.warn('[MiMo] VITE_MIMO_BASE_URL 未配置，请设置环境变量');
    }

    super({
      apiKey,
      baseUrl: baseUrl ?? undefined,
      model: options?.model ?? DEFAULT_MODEL,
    });

    if (!apiKey) {
      console.warn('[MiMo] VITE_MIMO_API_KEY 未配置，API 调用将失败');
    }
  }
}
