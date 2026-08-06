/**
 * Frontend architecture cabinet — dependency-cruiser
 * Alias aligned with vite.config.ts: /@/ → src/, /#/ → types/
 *
 * Run: pnpm exec depcruise src --config .dependency-cruiser.cjs --output-type err
 * Graph: pnpm exec depcruise src --config .dependency-cruiser.cjs --output-type dot > ../.claude/evidence/frontend-ct/arch-deps.dot
 */
const path = require('path');

/** @type {import('dependency-cruiser').IConfiguration} */
module.exports = {
  forbidden: [
    {
      name: 'no-circular',
      severity: 'error',
      comment: 'Circular dependencies make hot-reload and refactoring unsafe.',
      from: {},
      to: { circular: true },
    },
    {
      name: 'no-orphans',
      severity: 'warn',
      comment: 'Orphan modules are candidates for Knip / vue-unused cleanup.',
      from: {
        orphan: true,
        pathNot: [
          '(^|/)main\\.ts$',
          '(^|/)App\\.vue$',
          '\\.d\\.ts$',
          '(^|/)vite-env\\.d\\.ts$',
          '(^|/)router/(index|routes)\\.',
        ],
      },
      to: {},
    },
    {
      name: 'no-views-to-views-deep',
      severity: 'warn',
      comment:
        'Views should not import other top-level feature views (same-feature components/hooks OK).',
      from: { path: '^src/views/' },
      to: {
        path: '^src/views/',
        // Allow same-feature helpers and shared view utilities
        pathNot: [
          '^src/views/[^/]+/(components|composables|hooks|helper|helpers|utils)/',
          // Same first-level feature folder (e.g. workFlow/* ↔ workFlow/*)
          // Handled via pathNot relative patterns below is imperfect; keep advisory.
          '^src/views/common/',
          '^src/views/basic/',
        ],
      },
    },
    {
      name: 'no-deprecated-core',
      severity: 'warn',
      from: {},
      to: { path: 'node_modules/(?:lodash(?!-es)|moment)/' },
    },
    {
      name: 'not-to-dev-dep',
      severity: 'error',
      comment: 'Production code must not import package.json#devDependencies.',
      from: {
        pathNot: '\\.(spec|test)\\.(ts|tsx|js|vue)$|vitest|__tests__',
      },
      to: { dependencyTypes: ['npm-dev'] },
    },
  ],
  options: {
    doNotFollow: {
      path: 'node_modules',
      dependencyTypes: [
        'npm',
        'npm-dev',
        'npm-optional',
        'npm-peer',
        'npm-bundled',
        'npm-no-pkg',
      ],
    },
    exclude: {
      path: [
        'node_modules',
        'dist',
        'mock',
        '\\.cache',
        'src/core/compiler',
        'src/core/e2e',
        '__tests__',
        '\\.(spec|test)\\.(ts|tsx|js)$',
      ],
    },
    includeOnly: '^src',
    tsPreCompilationDeps: true,
    tsConfig: {
      fileName: 'tsconfig.json',
    },
    webpackConfig: {
      fileName: 'depcruise-webpack.resolve.cjs',
    },
    enhancedResolveOptions: {
      exportsFields: ['exports'],
      conditionNames: ['import', 'require', 'node', 'default', 'types'],
      mainFields: ['module', 'main', 'types', 'typings'],
      extensions: ['.ts', '.tsx', '.vue', '.js', '.jsx', '.json', '.mjs', '.cjs'],
    },
    reporterOptions: {
      archi: {
        collapsePattern: '^src/[^/]+|^node_modules/[^/]+',
      },
    },
  },
};
