// 文件：src/views/studio/composables/sanitize.ts
// 职责：XSS 消毒（架构组件，任何 v-html 场景必须使用）

import DOMPurify from 'dompurify';

/**
 * 消毒 HTML 内容
 *
 * 使用场景：
 *   - AI 回复的 Markdown 渲染后消毒
 *   - 文档标题消毒
 *   - 任何 v-html 绑定前消毒
 *
 * 禁止：
 *   - 直接 v-html="rawContent"（无消毒）
 *   - v-text 不需要消毒（自动转义）
 */
export function sanitizeHtml(html: string): string {
  return DOMPurify.sanitize(html, {
    ADD_ATTR: ['target', 'download'],
    ALLOWED_TAGS: [
      'h1',
      'h2',
      'h3',
      'h4',
      'h5',
      'h6',
      'p',
      'br',
      'hr',
      'ul',
      'ol',
      'li',
      'blockquote',
      'pre',
      'code',
      'table',
      'thead',
      'tbody',
      'tr',
      'th',
      'td',
      'strong',
      'em',
      'a',
      'img',
      'span',
      'div',
    ],
    ALLOWED_ATTR: ['href', 'src', 'alt', 'title', 'class', 'target', 'download'],
  });
}
