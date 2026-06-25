/**
 * 词法分析器
 * 将表达式字符串拆分为 Token 序列
 *
 * 支持的 Token 类型：
 *   NUMBER, STRING, IDENT, DOT, COMMA,
 *   PLUS, MINUS, STAR, SLASH, PERCENT,
 *   EQ, NE, GT, LT, GTE, LTE,
 *   AND, OR, NOT,
 *   LPAREN, RPAREN, LBRACKET, RBRACKET,
 *   QUESTION, COLON,
 *   NULL_COALESCE,
 *   TRUE, FALSE, NULL, UNDEFINED,
 *   EOF
 */

export type TokenType =
  | 'NUMBER'
  | 'STRING'
  | 'IDENT'
  | 'DOT'
  | 'COMMA'
  | 'PLUS'
  | 'MINUS'
  | 'STAR'
  | 'SLASH'
  | 'PERCENT'
  | 'EQ'
  | 'NE'
  | 'GT'
  | 'LT'
  | 'GTE'
  | 'LTE'
  | 'AND'
  | 'OR'
  | 'NOT'
  | 'LPAREN'
  | 'RPAREN'
  | 'LBRACKET'
  | 'RBRACKET'
  | 'QUESTION'
  | 'COLON'
  | 'NULL_COALESCE'
  | 'TRUE'
  | 'FALSE'
  | 'NULL'
  | 'UNDEFINED'
  | 'EOF';

export interface Token {
  type: TokenType;
  value: string;
  pos: number;
}

// 危险关键字——在词法阶段直接拒绝
const BLOCKED_IDENTS = new Set([
  'window',
  'document',
  'globalThis',
  'global',
  'self',
  'eval',
  'Function',
  'setTimeout',
  'setInterval',
  'fetch',
  'XMLHttpRequest',
  'ActiveXObject',
  'import',
  'require',
  'module',
  'exports',
  '__proto__',
  'constructor',
  'prototype',
]);

const KEYWORDS: Record<string, TokenType> = {
  true: 'TRUE',
  false: 'FALSE',
  null: 'NULL',
  undefined: 'UNDEFINED',
};

export function tokenize(input: string): Token[] {
  const tokens: Token[] = [];
  let pos = 0;

  while (pos < input.length) {
    const ch = input[pos];

    // 跳过空白
    if (/\s/.test(ch)) {
      pos++;
      continue;
    }

    // 数字
    if (/[0-9]/.test(ch) || (ch === '.' && /[0-9]/.test(input[pos + 1] ?? ''))) {
      let num = '';
      while (pos < input.length && /[0-9.]/.test(input[pos])) num += input[pos++];
      tokens.push({ type: 'NUMBER', value: num, pos: pos - num.length });
      continue;
    }

    // 字符串（单引号或双引号）
    if (ch === "'" || ch === '"') {
      const quote = ch;
      let str = '';
      const start = pos;
      pos++; // 跳过开头引号
      while (pos < input.length && input[pos] !== quote) {
        if (input[pos] === '\\') {
          str += input[pos++];
        } // 保留转义
        str += input[pos++];
      }
      pos++; // 跳过结尾引号
      tokens.push({ type: 'STRING', value: str, pos: start });
      continue;
    }

    // 标识符
    if (/[a-zA-Z_$]/.test(ch)) {
      let ident = '';
      const start = pos;
      while (pos < input.length && /[a-zA-Z0-9_$]/.test(input[pos])) ident += input[pos++];

      // 安全检查：拒绝危险关键字
      if (BLOCKED_IDENTS.has(ident)) {
        throw new Error(`[tokenizer] 安全拒绝: 禁止使用 "${ident}" (pos: ${start})`);
      }

      const keywordType = KEYWORDS[ident];
      tokens.push({ type: keywordType || 'IDENT', value: ident, pos: start });
      continue;
    }

    // 运算符和标点
    switch (ch) {
      case '.':
        tokens.push({ type: 'DOT', value: '.', pos });
        pos++;
        break;
      case ',':
        tokens.push({ type: 'COMMA', value: ',', pos });
        pos++;
        break;
      case '+':
        tokens.push({ type: 'PLUS', value: '+', pos });
        pos++;
        break;
      case '-':
        tokens.push({ type: 'MINUS', value: '-', pos });
        pos++;
        break;
      case '*':
        tokens.push({ type: 'STAR', value: '*', pos });
        pos++;
        break;
      case '/':
        tokens.push({ type: 'SLASH', value: '/', pos });
        pos++;
        break;
      case '%':
        tokens.push({ type: 'PERCENT', value: '%', pos });
        pos++;
        break;
      case '(':
        tokens.push({ type: 'LPAREN', value: '(', pos });
        pos++;
        break;
      case ')':
        tokens.push({ type: 'RPAREN', value: ')', pos });
        pos++;
        break;
      case '[':
        tokens.push({ type: 'LBRACKET', value: '[', pos });
        pos++;
        break;
      case ']':
        tokens.push({ type: 'RBRACKET', value: ']', pos });
        pos++;
        break;
      case '?':
        if (input[pos + 1] === '?') {
          tokens.push({ type: 'NULL_COALESCE', value: '??', pos });
          pos += 2;
        } else {
          tokens.push({ type: 'QUESTION', value: '?', pos });
          pos++;
        }
        break;
      case ':':
        tokens.push({ type: 'COLON', value: ':', pos });
        pos++;
        break;
      case '=':
        if (input[pos + 1] === '=') {
          tokens.push({ type: 'EQ', value: '==', pos });
          pos += 2;
        } else {
          throw new Error(`[tokenizer] 不支持赋值操作 (pos: ${pos})`);
        }
        break;
      case '!':
        if (input[pos + 1] === '=') {
          tokens.push({ type: 'NE', value: '!=', pos });
          pos += 2;
        } else {
          tokens.push({ type: 'NOT', value: '!', pos });
          pos++;
        }
        break;
      case '>':
        if (input[pos + 1] === '=') {
          tokens.push({ type: 'GTE', value: '>=', pos });
          pos += 2;
        } else {
          tokens.push({ type: 'GT', value: '>', pos });
          pos++;
        }
        break;
      case '<':
        if (input[pos + 1] === '=') {
          tokens.push({ type: 'LTE', value: '<=', pos });
          pos += 2;
        } else {
          tokens.push({ type: 'LT', value: '<', pos });
          pos++;
        }
        break;
      case '&':
        if (input[pos + 1] === '&') {
          tokens.push({ type: 'AND', value: '&&', pos });
          pos += 2;
        } else {
          throw new Error(`[tokenizer] 不支持位运算 (pos: ${pos})`);
        }
        break;
      case '|':
        if (input[pos + 1] === '|') {
          tokens.push({ type: 'OR', value: '||', pos });
          pos += 2;
        } else {
          throw new Error(`[tokenizer] 不支持位运算 (pos: ${pos})`);
        }
        break;
      default:
        throw new Error(`[tokenizer] 未知字符: "${ch}" (pos: ${pos})`);
    }
  }

  tokens.push({ type: 'EOF', value: '', pos });
  return tokens;
}
