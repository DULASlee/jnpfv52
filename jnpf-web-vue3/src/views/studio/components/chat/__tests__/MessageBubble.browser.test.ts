/**
 * MessageBubble 组件 Browser Mode 测试（2026 Vitest v4 + Playwright provider）
 *
 * 与 JSDOM 测试的本质区别：
 *   - 真实 Chromium 渲染：v-html、CSS 类名、DOM 布局均与生产一致
 *   - await expect.element()：Playwright locator + Vitest 断言无缝结合
 *   - 无 JSDOM 的 matchMedia/ResizeObserver mock 问题
 *
 * 运行：pnpm vitest run --config vitest.browser.config.ts
 */

import { render } from 'vitest-browser-vue';
import { expect, describe, it } from 'vitest';
import MessageBubble from '../MessageBubble.vue';

describe('MessageBubble — Browser Mode', () => {
  it('用户消息显示"U"头像 + user CSS 类', async () => {
    const screen = render(MessageBubble, {
      props: {
        role: 'user',
        content: '请帮我分析这个需求',
        contentType: 'text',
        timestamp: '10:00',
      },
    });

    // 真实 Chromium 渲染后，用 Playwright locator 断言
    await expect.element(screen.getByText('U')).toBeVisible();
    const bubble = screen.getByRole('generic').first();
    await expect.element(bubble).toHaveClass('user');
  });

  it('AI 消息显示"AI"头像 + assistant CSS 类', async () => {
    const screen = render(MessageBubble, {
      props: {
        role: 'assistant',
        content: '我已分析完毕，以下是结果',
        contentType: 'text',
        timestamp: '10:01',
      },
    });

    await expect.element(screen.getByText('AI')).toBeVisible();
  });

  it('text 类型内容通过 v-html 渲染 Markdown', async () => {
    const screen = render(MessageBubble, {
      props: {
        role: 'assistant',
        content: '**需求分析**：系统应支持...',
        contentType: 'text',
        timestamp: '10:02',
      },
    });

    // 真实浏览器渲染 v-html 后，<strong> 标签应存在
    await expect.element(screen.getByRole('generic', { name: /text-content/ }).first()).toBeVisible();
  });

  it('显示 stage 标签（当传入 stage prop）', async () => {
    const screen = render(MessageBubble, {
      props: {
        role: 'assistant',
        content: '门控通过',
        contentType: 'text',
        stage: 'S0-Gate',
        timestamp: '10:03',
      },
    });

    await expect.element(screen.getByText('S0-Gate')).toBeVisible();
  });

  it('timestamp 在 meta 区域可见', async () => {
    const screen = render(MessageBubble, {
      props: {
        role: 'user',
        content: '提交需求',
        contentType: 'text',
        timestamp: '14:30',
      },
    });

    await expect.element(screen.getByText('14:30')).toBeVisible();
  });

  it('未知 contentType 降级显示纯文本', async () => {
    const screen = render(MessageBubble, {
      props: {
        role: 'assistant',
        // @ts-expect-error - 测试边界情况
        contentType: 'unknown-type',
        content: '原始文本内容',
        timestamp: '10:04',
      },
    });

    await expect.element(screen.getByText('原始文本内容')).toBeVisible();
  });
});
