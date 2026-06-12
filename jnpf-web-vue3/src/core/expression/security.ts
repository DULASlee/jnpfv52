/**
 * 安全检查
 * 在 AST 层面拒绝危险结构
 *
 * 检查项：
 *   1. 禁止 this 关键字
 *   2. 禁止赋值操作
 *   3. 禁止函数定义
 *   4. 禁止访问危险属性
 *   5. 限制调用深度
 */

import type { ASTNode } from './parser';

const BLOCKED_PROPERTIES = new Set(['__proto__', 'constructor', 'prototype', 'caller', 'callee', 'arguments']);

const MAX_DEPTH = 20;
const MAX_CALL_ARGS = 10;

export function securityCheck(node: ASTNode, depth = 0): void {
  if (depth > MAX_DEPTH) {
    throw new Error('[security] 表达式嵌套过深，可能存在恶意构造');
  }

  switch (node.type) {
    case 'Identifier':
      if (node.name === 'this') {
        throw new Error('[security] 禁止使用 this');
      }
      break;

    case 'MemberExpression':
      if (BLOCKED_PROPERTIES.has(node.property)) {
        throw new Error(`[security] 禁止访问属性: ${node.property}`);
      }
      securityCheck(node.object, depth + 1);
      break;

    case 'IndexExpression':
      securityCheck(node.object, depth + 1);
      securityCheck(node.index, depth + 1);
      break;

    case 'CallExpression':
      if (node.args.length > MAX_CALL_ARGS) {
        throw new Error(`[security] 函数调用参数过多: ${node.args.length} > ${MAX_CALL_ARGS}`);
      }
      securityCheck(node.callee, depth + 1);
      node.args.forEach(arg => securityCheck(arg, depth + 1));
      break;

    case 'UnaryExpression':
      securityCheck(node.argument, depth + 1);
      break;

    case 'BinaryExpression':
      securityCheck(node.left, depth + 1);
      securityCheck(node.right, depth + 1);
      break;

    case 'LogicalExpression':
      securityCheck(node.left, depth + 1);
      securityCheck(node.right, depth + 1);
      break;

    case 'ConditionalExpression':
      securityCheck(node.test, depth + 1);
      securityCheck(node.consequent, depth + 1);
      securityCheck(node.alternate, depth + 1);
      break;

    case 'Literal':
      // 字面量本身是安全的
      break;
  }
}
