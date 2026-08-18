/**
 * Complexity cabinet — SonarJS cognitive complexity (full src by default).
 *
 * Usage:
 *   node scripts/quality/cab2-sonarjs-run.cjs
 *   node scripts/quality/cab2-sonarjs-run.cjs --scope hot
 */
const fs = require('fs');
const path = require('path');
const { ESLint } = require('eslint');

const root = path.resolve(__dirname, '../..');
const evidence = path.resolve(root, '../.claude/evidence/frontend-ct');
fs.mkdirSync(evidence, { recursive: true });

const scopeHot = process.argv.includes('--scope') && process.argv[process.argv.indexOf('--scope') + 1] === 'hot';
const roots = (
  scopeHot
    ? ['src/views/studio', 'src/views/common/dynamicModel', 'src/components/Jnpf']
    : ['src']
).map((p) => path.join(root, p));

const skipDir = new Set(['node_modules', '__tests__', 'compiler', 'e2e', 'dist']);

function walk(dir, acc = []) {
  if (!fs.existsSync(dir)) return acc;
  for (const ent of fs.readdirSync(dir, { withFileTypes: true })) {
    const full = path.join(dir, ent.name);
    if (ent.isDirectory()) {
      if (skipDir.has(ent.name)) continue;
      // Skip known non-app trees under src/core
      if (dir.replace(/\\/g, '/').endsWith('/src/core') && (ent.name === 'compiler' || ent.name === 'e2e')) {
        continue;
      }
      walk(full, acc);
    } else if (/\.(vue|ts|tsx)$/.test(ent.name) && !/\.(spec|test)\./.test(ent.name)) {
      acc.push(full);
    }
  }
  return acc;
}

(async () => {
  const files = roots.flatMap((r) => walk(r));
  console.log(JSON.stringify({ scope: scopeHot ? 'hot' : 'full-src', files: files.length }));

  const eslint = new ESLint({
    cwd: root,
    overrideConfigFile: path.join(root, '.eslintrc.complexity.cjs'),
    useEslintrc: false,
    ignore: false,
    extensions: ['.vue', '.ts', '.tsx'],
  });

  // Batch to reduce peak memory
  const batchSize = 80;
  const results = [];
  for (let i = 0; i < files.length; i += batchSize) {
    const batch = files.slice(i, i + batchSize);
    process.stdout.write(`lint ${i + 1}-${Math.min(i + batchSize, files.length)}/${files.length}\n`);
    const part = await eslint.lintFiles(batch);
    results.push(...part);
  }

  const jsonPath = path.join(evidence, 'cab2-sonarjs.json');
  fs.writeFileSync(jsonPath, JSON.stringify(results), 'utf8');

  const rows = [];
  let errorCount = 0;
  let warnCount = 0;
  for (const f of results) {
    for (const m of f.messages || []) {
      if (m.severity === 2) errorCount++;
      else if (m.severity === 1) warnCount++;
      if (m.ruleId === 'sonarjs/cognitive-complexity') {
        const mm = String(m.message).match(/from (\d+)/);
        rows.push({
          file: path.relative(root, f.filePath).replace(/\\/g, '/'),
          line: m.line,
          cc: mm ? +mm[1] : 0,
          message: m.message,
        });
      }
    }
  }
  rows.sort((a, b) => b.cc - a.cc);
  const summary = {
    generatedAt: new Date().toISOString(),
    scope: scopeHot ? 'hot' : 'full-src',
    scannedFiles: results.length,
    errorCount,
    warnCount,
    cognitiveHits: rows.length,
    top50: rows.slice(0, 50),
    top20: rows.slice(0, 20),
    compareNote:
      'SonarJS = cognitive complexity; VMD cyclomaticComplexity = different metric. Cross-rank by file, not raw number.',
  };
  fs.writeFileSync(path.join(evidence, 'cab2-sonarjs-top.json'), JSON.stringify(summary, null, 2));
  console.log(JSON.stringify({ ...summary, top50: undefined, top20: summary.top20 }, null, 2));
})().catch((err) => {
  console.error(err);
  process.exit(1);
});
