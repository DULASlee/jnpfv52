/**
 * 表达式分类器
 * 输入：嵌入在 JSON 中的箭头函数字符串
 * 输出：分级结果 + AI 业务意图探针
 */

export interface ClassificationResult {
  level: 'empty' | 'simple' | 'medium' | 'complex';
  params: string[];
  body: string;
  isAsync: boolean;
  /** AI 业务意图探针 — 当前为关键词探针，未来接入大模型 */
  intentHints: string[];
}

export function classifyExpression(code: string): ClassificationResult {
  const trimmed = code.trim();

  // 提取参数
  const paramMatch = trimmed.match(/^\(?\s*\{([^}]*)\}\s*\)?\s*=>/);
  const params = paramMatch
    ? paramMatch[1]
        .split(',')
        .map(p => p.trim())
        .filter(Boolean)
    : [];

  // 提取函数体
  const bodyMatch = trimmed.match(/=>\s*\{([\s\S]*)\}$/);
  const body = bodyMatch ? bodyMatch[1].trim() : '';

  // 空函数
  if (!body || /^\s*$/.test(body)) {
    return { level: 'empty', params, body, isAsync: false, intentHints: [] };
  }

  const isAsync = /async\s/.test(trimmed) || /Promise|new Promise|resolve|reject/.test(body);

  // 复杂
  if (/for\s*\(|\.forEach\(|\.map\(|\.filter\(|while\s*\(/.test(body)) {
    return { level: 'complex', params, body, isAsync, intentHints: detectBusinessIntent(body) };
  }
  if (/Promise/.test(body) && /if\s*\(/.test(body)) {
    return { level: 'complex', params, body, isAsync, intentHints: detectBusinessIntent(body) };
  }
  if (/eval\s*\(|new Function/.test(body)) {
    return { level: 'complex', params, body, isAsync, intentHints: detectBusinessIntent(body) };
  }

  // 中等
  if (/Promise|new Promise|resolve|reject/.test(body)) {
    return { level: 'medium', params, body, isAsync, intentHints: detectBusinessIntent(body) };
  }
  if (/if\s*\(|if\s*\{/.test(body)) {
    return { level: 'medium', params, body, isAsync, intentHints: detectBusinessIntent(body) };
  }
  if (/await\s/.test(body)) {
    return { level: 'medium', params, body, isAsync, intentHints: detectBusinessIntent(body) };
  }
  if (/\.get\(|\.post\(|\.put\(|\.delete\(|fetch\s*\(/.test(body)) {
    return { level: 'medium', params, body, isAsync, intentHints: detectBusinessIntent(body) };
  }

  // 简单
  return { level: 'simple', params, body, isAsync, intentHints: detectBusinessIntent(body) };
}

/**
 * AI 业务意图探针
 * 当前：基于关键词的简单分类
 * 未来：接入大模型做语义理解
 */
function detectBusinessIntent(body: string): string[] {
  const intents: string[] = [];
  if (/\b金额|价格|amount|price|money\b/i.test(body)) intents.push('金额计算');
  if (/\b审批|approve|workflow\b/i.test(body)) intents.push('审批流程');
  if (/\b权限|permission|role|authorize\b/i.test(body)) intents.push('权限校验');
  if (/\b设备|machine|equipment\b/i.test(body)) intents.push('设备交互');
  if (/\b库存|stock|inventory\b/i.test(body)) intents.push('库存管理');
  if (/\b告警|alarm|alert|warning\b/i.test(body)) intents.push('告警处理');
  if (/\b用户|user|tenant\b/i.test(body)) intents.push('用户/租户');
  return intents;
}
