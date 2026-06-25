/**
 * 语法分析器
 * 将 Token 序列解析为 AST
 *
 * 运算符优先级（低→高）：
 *   ?? → || → && → == != > < >= <= → + - → * / % → ! 一元 → () 成员访问
 */

import type { Token, TokenType } from './tokenizer';

// ============================================================
// AST 节点类型
// ============================================================

export type ASTNode =
  | LiteralNode
  | IdentifierNode
  | MemberExprNode
  | CallExprNode
  | UnaryExprNode
  | BinaryExprNode
  | LogicalExprNode
  | ConditionalExprNode
  | IndexExprNode;

export interface LiteralNode {
  type: 'Literal';
  value: string | number | boolean | null;
}

export interface IdentifierNode {
  type: 'Identifier';
  name: string;
}

export interface MemberExprNode {
  type: 'MemberExpression';
  object: ASTNode;
  property: string;
}

export interface IndexExprNode {
  type: 'IndexExpression';
  object: ASTNode;
  index: ASTNode;
}

export interface CallExprNode {
  type: 'CallExpression';
  callee: ASTNode;
  args: ASTNode[];
}

export interface UnaryExprNode {
  type: 'UnaryExpression';
  operator: '!' | '-';
  argument: ASTNode;
}

export interface BinaryExprNode {
  type: 'BinaryExpression';
  operator: '+' | '-' | '*' | '/' | '%' | '==' | '!=' | '>' | '<' | '>=' | '<=';
  left: ASTNode;
  right: ASTNode;
}

export interface LogicalExprNode {
  type: 'LogicalExpression';
  operator: '&&' | '||' | '??';
  left: ASTNode;
  right: ASTNode;
}

export interface ConditionalExprNode {
  type: 'ConditionalExpression';
  test: ASTNode;
  consequent: ASTNode;
  alternate: ASTNode;
}

// ============================================================
// Parser
// ============================================================

export function parse(tokens: Token[]): ASTNode {
  let pos = 0;

  function peek(): Token {
    return tokens[pos];
  }
  function advance(): Token {
    return tokens[pos++];
  }
  function expect(type: TokenType): Token {
    const t = advance();
    if (t.type !== type) throw new Error(`[parser] 期望 ${type}，得到 ${t.type} (pos: ${t.pos})`);
    return t;
  }

  function parseExpression(): ASTNode {
    return parseConditional();
  }

  function parseConditional(): ASTNode {
    const expr = parseNullCoalesce();
    if (peek().type === 'QUESTION') {
      advance();
      const consequent = parseExpression();
      expect('COLON');
      const alternate = parseConditional();
      return { type: 'ConditionalExpression', test: expr, consequent, alternate };
    }
    return expr;
  }

  function parseNullCoalesce(): ASTNode {
    let expr = parseOr();
    while (peek().type === 'NULL_COALESCE') {
      advance();
      const right = parseOr();
      expr = { type: 'LogicalExpression', operator: '??', left: expr, right };
    }
    return expr;
  }

  function parseOr(): ASTNode {
    let expr = parseAnd();
    while (peek().type === 'OR') {
      advance();
      const right = parseAnd();
      expr = { type: 'LogicalExpression', operator: '||', left: expr, right };
    }
    return expr;
  }

  function parseAnd(): ASTNode {
    let expr = parseComparison();
    while (peek().type === 'AND') {
      advance();
      const right = parseComparison();
      expr = { type: 'LogicalExpression', operator: '&&', left: expr, right };
    }
    return expr;
  }

  function parseComparison(): ASTNode {
    let expr = parseAddition();
    while (['EQ', 'NE', 'GT', 'LT', 'GTE', 'LTE'].includes(peek().type)) {
      const op = advance().value as BinaryExprNode['operator'];
      const right = parseAddition();
      expr = { type: 'BinaryExpression', operator: op, left: expr, right };
    }
    return expr;
  }

  function parseAddition(): ASTNode {
    let expr = parseMultiplication();
    while (peek().type === 'PLUS' || peek().type === 'MINUS') {
      const op = advance().value as '+' | '-';
      const right = parseMultiplication();
      expr = { type: 'BinaryExpression', operator: op, left: expr, right };
    }
    return expr;
  }

  function parseMultiplication(): ASTNode {
    let expr = parseUnary();
    while (['STAR', 'SLASH', 'PERCENT'].includes(peek().type)) {
      const op = advance().value as '*' | '/' | '%';
      const right = parseUnary();
      expr = { type: 'BinaryExpression', operator: op, left: expr, right };
    }
    return expr;
  }

  function parseUnary(): ASTNode {
    if (peek().type === 'NOT') {
      advance();
      return { type: 'UnaryExpression', operator: '!', argument: parseUnary() };
    }
    if (peek().type === 'MINUS') {
      advance();
      return { type: 'UnaryExpression', operator: '-', argument: parseUnary() };
    }
    return parsePostfix();
  }

  function parsePostfix(): ASTNode {
    let expr = parsePrimary();
    while (true) {
      if (peek().type === 'DOT') {
        advance();
        const prop = expect('IDENT').value;
        expr = { type: 'MemberExpression', object: expr, property: prop };
      } else if (peek().type === 'LBRACKET') {
        advance();
        const index = parseExpression();
        expect('RBRACKET');
        expr = { type: 'IndexExpression', object: expr, index };
      } else if (peek().type === 'LPAREN') {
        advance();
        const args: ASTNode[] = [];
        if (peek().type !== 'RPAREN') {
          args.push(parseExpression());
          while (peek().type === 'COMMA') {
            advance();
            args.push(parseExpression());
          }
        }
        expect('RPAREN');
        expr = { type: 'CallExpression', callee: expr, args };
      } else {
        break;
      }
    }
    return expr;
  }

  function parsePrimary(): ASTNode {
    const t = peek();
    switch (t.type) {
      case 'NUMBER':
        advance();
        return { type: 'Literal', value: Number(t.value) };
      case 'STRING':
        advance();
        return { type: 'Literal', value: t.value };
      case 'TRUE':
        advance();
        return { type: 'Literal', value: true };
      case 'FALSE':
        advance();
        return { type: 'Literal', value: false };
      case 'NULL':
        advance();
        return { type: 'Literal', value: null };
      case 'UNDEFINED':
        advance();
        return { type: 'Literal', value: null };
      case 'IDENT':
        advance();
        return { type: 'Identifier', name: t.value };
      case 'LPAREN':
        advance();
        const expr = parseExpression();
        expect('RPAREN');
        return expr;
      default:
        throw new Error(`[parser] 无法解析: ${t.type} "${t.value}" (pos: ${t.pos})`);
    }
  }

  const ast = parseExpression();
  if (peek().type !== 'EOF') {
    throw new Error(`[parser] 表达式未结束，剩余: ${peek().type} "${peek().value}"`);
  }
  return ast;
}
