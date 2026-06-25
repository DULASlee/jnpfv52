/**
 * 兼容层
 * 替代现有代码中 eval / new Function 的调用
 * 保持原有函数签名不变
 */

import { ExpressionEngine } from './engine';

const engine = new ExpressionEngine();

/**
 * 替代 eval(str)
 * 现有调用点：jnpf.ts 的 getScriptFunc
 */
export function safeEval(expression: string, data: Record<string, unknown>): unknown {
  return engine.evaluate(expression, data);
}

/**
 * 替代 new Function(body)
 * 返回一个安全的包装函数
 */
export function safeFunction(body: string, ...paramNames: string[]): (...args: unknown[]) => unknown {
  // 构造箭头函数表达式
  const params = paramNames.join(', ');
  const expr = `(${params}) => { ${body} }`;

  return (...args: unknown[]) => {
    const data: Record<string, unknown> = {};
    paramNames.forEach((name, i) => {
      data[name] = args[i];
    });
    return engine.evaluate(expr, data);
  };
}
