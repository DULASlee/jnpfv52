/**
 * Complexity cabinet — SonarJS cognitive complexity (advisory report mode).
 * Does NOT replace .eslintrc.js day-to-day lint.
 *
 * Run (see package.json script quality:complexity):
 *   pnpm exec eslint -c .eslintrc.complexity.cjs --no-eslintrc
 *   then glob src views/components (avoid nesting star-slash in this comment).
 *
 * Thresholds start advisory (warn). Raise to error only after baseline.
 */
module.exports = {
  root: true,
  env: { browser: true, node: true, es2020: true },
  parser: 'vue-eslint-parser',
  parserOptions: {
    parser: '@typescript-eslint/parser',
    ecmaVersion: 2020,
    sourceType: 'module',
    extraFileExtensions: ['.vue'],
  },
  plugins: ['sonarjs'],
  extends: ['plugin:sonarjs/recommended-legacy'],
  rules: {
    // Focus metric for this cabinet (advisory baseline)
    'sonarjs/cognitive-complexity': ['warn', 15],
    // Keep noise down for first baseline report
    'sonarjs/no-duplicate-string': 'off',
  },
  ignorePatterns: [
    'node_modules/',
    'dist/',
    'mock/',
    'src/core/compiler/',
    'src/core/e2e/',
    '**/__tests__/**',
  ],
};
