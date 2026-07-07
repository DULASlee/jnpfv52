import { defineConfig } from 'vitest/config';

export default defineConfig({
  test: {
    environment: 'node',
    include: ['tests/api/**/*.test.mjs'],
    testTimeout: 60_000,
    hookTimeout: 30_000,
    reporters: ['verbose'],
  },
});
