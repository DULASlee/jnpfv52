import { defineConfig } from 'vitest/config';

export default defineConfig({
  test: {
    environment: 'node',
    include: ['tests/skill-r2/**/*.test.ts'],
    testTimeout: 30_000,
    reporters: ['verbose'],
  },
});
