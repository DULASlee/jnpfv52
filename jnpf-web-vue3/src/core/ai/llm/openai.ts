/**
 * OpenAI 网关实现
 *
 * 标准 OpenAI Chat Completion API。
 * API Key 从环境变量 VITE_OPENAI_API_KEY 读取。
 *
 * @version 1.0.0
 * @module ai/llm/openai
 */

import { DeepSeekGateway } from './deepseek';

const DEFAULT_BASE_URL = 'https://api.openai.com/v1';
const DEFAULT_MODEL = 'gpt-4o';

export class OpenAIGateway extends DeepSeekGateway {
  constructor(options?: { apiKey?: string; baseUrl?: string; model?: string }) {
    const apiKey = options?.apiKey ?? import.meta.env.VITE_OPENAI_API_KEY ?? '';

    super({
      apiKey,
      baseUrl: options?.baseUrl ?? import.meta.env.VITE_OPENAI_BASE_URL ?? DEFAULT_BASE_URL,
      model: options?.model ?? DEFAULT_MODEL,
    });

    if (!apiKey) {
      console.warn('[OpenAI] VITE_OPENAI_API_KEY 未配置，API 调用将失败');
    }
  }
}
