/**
 * Vitest Browser Mode 配置（2026 标准）
 *
 * 与原 vitest.config.ts 的区别：
 *   - environment: 'node' (JSDOM) → 真实 Chromium（Playwright provider）
 *   - 组件在真实浏览器中渲染，CSS/布局/DOM API 行为与生产一致
 *   - 使用 vitest-browser-vue 的 render() + await expect.element()
 *
 * 使用方式：
 *   pnpm vitest --config vitest.browser.config.ts          # watch 模式
 *   pnpm vitest run --config vitest.browser.config.ts      # CI 单次
 *
 * 覆盖范围：
 *   src/views/studio/**\/\*.browser.test.ts   Studio Vue 组件
 */

import { defineConfig } from 'vitest/config';
import vue from '@vitejs/plugin-vue';
import { resolve } from 'path';

export default defineConfig({
  plugins: [vue()],

  resolve: {
    alias: {
      '/@/': resolve(__dirname, 'src') + '/',
      '@/': resolve(__dirname, 'src') + '/',
    },
  },

  test: {
    // Vitest Browser Mode：使用 Playwright 作为 provider
    browser: {
      enabled: true,
      provider: 'playwright',
      headless: true,
      instances: [
        { browser: 'chromium' },
      ],
    },

    // Browser Mode 测试文件与 Node 单元测试分离
    include: ['src/**/*.browser.test.ts'],
    globals: true,

    // 浏览器测试允许更长时间（组件渲染 + Playwright 启动）
    testTimeout: 15_000,
  },
});
