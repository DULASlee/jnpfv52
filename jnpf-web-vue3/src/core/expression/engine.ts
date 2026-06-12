/**
 * 表达式引擎入口
 *
 * 组合 tokenize → parse → securityCheck → compile → execute 管线
 * 提供缓存、批量求值、依赖提取等高级能力
 */

import { tokenize } from './tokenizer';
import { parse } from './parser';
import { securityCheck } from './security';
import { compile, type CompiledExpr } from './compiler';
import { createSafeSnapshot } from './context';

// 编译缓存（表达式文本 → 编译后的函数）
const compileCache = new Map<string, CompiledExpr>();

export class ExpressionEngine {
  /**
   * 求值
   * @param expr 表达式字符串（如 "formData.name == 'VIP'"）
   * @param data 上下文数据（如 { formData: { name: 'VIP' } }）
   * @returns 求值结果
   */
  evaluate(expr: string, data: Record<string, unknown>): unknown {
    try {
      const compiled = this.getOrCompile(expr);
      const snapshot = createSafeSnapshot(data);
      return compiled(snapshot);
    } catch (e) {
      console.warn(`[expression-engine] 求值失败: ${expr}`, (e as Error).message);
      return undefined;
    }
  }

  /**
   * 批量求值
   */
  evaluateBatch(exprs: string[], data: Record<string, unknown>): unknown[] {
    const snapshot = createSafeSnapshot(data);
    return exprs.map(expr => {
      try {
        const compiled = this.getOrCompile(expr);
        return compiled(snapshot);
      } catch {
        return undefined;
      }
    });
  }

  /**
   * 验证表达式语法
   */
  validate(expr: string): { valid: boolean; error?: string } {
    try {
      const tokens = tokenize(expr);
      const ast = parse(tokens);
      securityCheck(ast);
      return { valid: true };
    } catch (e) {
      return { valid: false, error: (e as Error).message };
    }
  }

  /**
   * 提取表达式中的变量依赖
   */
  extractDependencies(expr: string): string[] {
    try {
      const tokens = tokenize(expr);
      const ast = parse(tokens);
      const deps = new Set<string>();
      collectIdentifiers(ast, deps);
      return [...deps];
    } catch {
      return [];
    }
  }

  /**
   * 清空编译缓存
   */
  clearCache(): void {
    compileCache.clear();
  }

  // ——— 内部方法 ———

  private getOrCompile(expr: string): CompiledExpr {
    let compiled = compileCache.get(expr);
    if (!compiled) {
      const tokens = tokenize(expr);
      const ast = parse(tokens);
      securityCheck(ast);
      compiled = compile(ast);
      compileCache.set(expr, compiled);
    }
    return compiled;
  }
}

function collectIdentifiers(node: unknown, deps: Set<string>): void {
  if (!node || typeof node !== 'object') return;
  const n = node as Record<string, unknown>;
  if (n.type === 'Identifier') {
    deps.add(n.name as string);
  }
  for (const value of Object.values(n)) {
    if (Array.isArray(value)) {
      value.forEach(item => collectIdentifiers(item, deps));
    } else if (value && typeof value === 'object') {
      collectIdentifiers(value, deps);
    }
  }
}
