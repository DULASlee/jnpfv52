import { defineConfig } from 'vitest/config';

export default defineConfig({
  test: {
    // Vitest 用 esbuild 转换 TypeScript，原生支持 uuid v14+ ESM
    // 无需 ts-jest / jest，无需 __mocks__/uuid.js shim
    environment: 'node',
    globals: true,
    include: ['__tests__/**/*.test.ts'],

    // 测试超时（SA 异步任务最长 20s + buffer）
    testTimeout: 40_000,

    coverage: {
      provider: 'v8',
      include: ['src/**/*.ts'],
      exclude: ['src/server.ts'],   // 入口文件单独用集成测试覆盖
      reporter: ['text', 'lcov', 'html'],
      thresholds: {
        branches: 80,
        functions: 85,
        lines: 85,
        statements: 85,
      },
    },

    // SA_TEST=1 防止 server.ts 在 import 时调用 app.listen()
    env: {
      SA_TEST: '1',
      LLM_GATEWAY_URL: 'http://mock-llm-gateway/api/llm',
      SA_DB_BACKEND: 'inmemory',
    },
  },
});
