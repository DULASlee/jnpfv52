/**
 * 将字符串形式的正则表达式安全解析为 RegExp 对象
 * 支持 "/pattern/flags" 格式，避免使用 eval()
 */
export function safeParseRegex(pattern: string): RegExp | null {
  if (pattern instanceof RegExp) return pattern;
  if (typeof pattern !== 'string') return null;
  const match = pattern.match(/^\/(.+)\/([gimsuyd]*)$/);
  if (!match) return null;
  try {
    return new RegExp(match[1], match[2]);
  } catch {
    return null;
  }
}

/**
 * 安全检测字符串是否为有效的正则表达式格式
 * 替代 Object.prototype.toString.call(eval(val)) === '[object RegExp]'
 */
export function isValidRegex(val: string): boolean {
  return safeParseRegex(val) !== null;
}
