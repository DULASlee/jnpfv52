/**
 * DeepSeek V4 Pro 网关实现
 *
 * DeepSeek V4 Pro 直接 API 入口，与标准 DeepSeekGateway 共享相同的 API 格式，
 * 仅覆盖 model 和环境变量来源。不走降级网关，用于快速测试验证。
 *
 * 环境变量：
 *   - VITE_DEEPSEEK_V4_API_KEY  — API 密钥
 *   - VITE_DEEPSEEK_V4_BASE_URL — 基础 URL（可选，默认同标准 DeepSeek）
 *
 * @version 1.0.0
 * @module ai/llm/deepseek-v4
 */

import { DeepSeekGateway } from './deepseek';

const DEFAULT_V4_MODEL = 'deepseek-v4-pro';

export class DeepSeekV4Gateway extends DeepSeekGateway {
  constructor(options?: { apiKey?: string; baseUrl?: string; model?: string }) {
    const apiKey = options?.apiKey ?? import.meta.env.VITE_DEEPSEEK_V4_API_KEY ?? '';
    const baseUrl = options?.baseUrl ?? import.meta.env.VITE_DEEPSEEK_V4_BASE_URL;

    super({
      apiKey,
      baseUrl: baseUrl || undefined,
      model: options?.model ?? DEFAULT_V4_MODEL,
    });

    if (!apiKey) {
      console.warn('[DeepSeek V4] VITE_DEEPSEEK_V4_API_KEY 未配置，API 调用将失败');
    }
  }
}
