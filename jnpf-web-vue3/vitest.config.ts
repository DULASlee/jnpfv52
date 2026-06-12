import { defineConfig } from 'vitest/config';
import { resolve } from 'path';

export default defineConfig({
  resolve: {
    alias: {
      '/@/': resolve(__dirname, 'src') + '/',
    },
  },
  test: {
    include: ['src/core/**/*.test.ts'],
    globals: true,
    environment: 'node',
  },
});
