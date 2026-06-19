/** @type {import('jest').Config} */
module.exports = {
  preset: 'ts-jest',
  testEnvironment: 'node',
  testMatch: ['**/__tests__/**/*.test.ts'],
  collectCoverageFrom: ['src/**/*.ts'],
  coverageDirectory: 'coverage',
  coverageReporters: ['text', 'lcov', 'html'],
  coverageThreshold: {
    global: {
      branches: 85,
      functions: 90,
      lines: 90,
      statements: 90,
    },
  },
  // 测试顺序:Dict → DFD → BPM → Logic → CrossEvent → ER → UI(从最底层依赖开始)
  testPathIgnorePatterns: ['/node_modules/', '/dist/'],
  verbose: true,
};
