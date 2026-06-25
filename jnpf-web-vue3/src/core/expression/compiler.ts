/**
 * 编译器
 * 将 AST 编译为可执行函数
 *
 * 生成一个接受 context 参数的函数，
 * 在 context 中查找变量值。
 *
 * 安全声明：此模块使用 new Function 构造器，但输入已经过
 * tokenize → parse → securityCheck 三重校验，
 * 输入中不可能包含任意代码，只有生成的安全表达式。
 */

import type { ASTNode } from './parser';

export type CompiledExpr = (context: Record<string, unknown>) => unknown;

export function compile(ast: ASTNode): CompiledExpr {
  const code = generateCode(ast);
  return new Function('context', `"use strict"; return (${code});`) as CompiledExpr;
}

function generateCode(node: ASTNode): string {
  switch (node.type) {
    case 'Literal':
      if (node.value === null) return 'null';
      if (typeof node.value === 'string') return JSON.stringify(node.value);
      return String(node.value);

    case 'Identifier':
      if (node.name === 'undefined') return 'undefined';
      return `context[${JSON.stringify(node.name)}]`;

    case 'MemberExpression': {
      const obj = generateCode(node.object);
      return `(${obj})[${JSON.stringify(node.property)}]`;
    }

    case 'IndexExpression': {
      const obj = generateCode(node.object);
      const idx = generateCode(node.index);
      return `(${obj})[${idx}]`;
    }

    case 'CallExpression': {
      const callee = generateCode(node.callee);
      const args = node.args.map(generateCode).join(', ');
      return `(${callee})(${args})`;
    }

    case 'UnaryExpression':
      return `(${node.operator}${generateCode(node.argument)})`;

    case 'BinaryExpression':
      return `(${generateCode(node.left)} ${node.operator} ${generateCode(node.right)})`;

    case 'LogicalExpression':
      return `(${generateCode(node.left)} ${node.operator} ${generateCode(node.right)})`;

    case 'ConditionalExpression':
      return `(${generateCode(node.test)} ? ${generateCode(node.consequent)} : ${generateCode(node.alternate)})`;

    default:
      throw new Error(`[compiler] 未知节点类型: ${(node as ASTNode).type}`);
  }
}
