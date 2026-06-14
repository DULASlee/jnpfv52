/**
 * 通义千问 (DashScope) 网关实现
 *
 * 阿里云 DashScope API，兼容 OpenAI Chat Completion 格式。
 * API Key 从环境变量 VITE_TONGYI_API_KEY 读取。
 *
 * @version 1.0.0
 * @module ai/llm/tongyi
 */

import { DeepSeekGateway } from './deepseek';

const DEFAULT_BASE_URL = 'https://dashscope.aliyuncs.com/api/v1';
const DEFAULT_MODEL = 'qwen-plus';

export class TongyiGateway extends DeepSeekGateway {
  constructor(options?: { apiKey?: string; baseUrl?: string; model?: string }) {
    const apiKey = options?.apiKey ?? import.meta.env.VITE_TONGYI_API_KEY ?? '';

    super({
      apiKey,
      baseUrl: options?.baseUrl ?? import.meta.env.VITE_TONGYI_BASE_URL ?? DEFAULT_BASE_URL,
      model: options?.model ?? DEFAULT_MODEL,
    });

    if (!apiKey) {
      console.warn('[通义千问] VITE_TONGYI_API_KEY 未配置，API 调用将失败');
    }
  }
}
