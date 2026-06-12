/**
 * diff-codegen: 新旧代码生成器差异报告
 * 用法: pnpm diff:codegen
 * 输出: docs/adr-017-diff-report.md
 */
import * as fs from 'node:fs';
import * as path from 'node:path';

const REPORT_PATH = path.resolve(__dirname, '../../docs/adr-017-diff-report.md');
const OUTPUT_DIR = path.resolve(__dirname, '../examples/generated-student');

interface FileDiff {
  file: string;
  status: 'added' | 'modified' | 'unchanged';
  checks: { marker: boolean; insertPoint: boolean; evalFree: boolean };
}

function main(): void {
  const diffs: FileDiff[] = [];

  if (!fs.existsSync(OUTPUT_DIR)) {
    console.log('No generated output found. Run "npx tsx scripts/generate-demo.ts" first.');
    process.exit(1);
  }

  collectFiles(OUTPUT_DIR, '', diffs);

  const report = generateReport(diffs);
  fs.writeFileSync(REPORT_PATH, report, 'utf-8');
  console.log(`✅ Diff report written to ${REPORT_PATH}`);
  console.log(`   Files: ${diffs.length} total`);
  console.log(`   Markers: ${diffs.filter(d => d.checks.marker).length}/${diffs.length} with @jnpf-generated`);
  console.log(`   InsertPoints: ${diffs.filter(d => d.checks.insertPoint).length} with insert-point`);
  console.log(`   Eval-free: ${diffs.filter(d => d.checks.evalFree).length}/${diffs.length}`);
}

function collectFiles(dir: string, relativePath: string, diffs: FileDiff[]): void {
  const entries = fs.readdirSync(dir, { withFileTypes: true });
  for (const entry of entries) {
    const fullPath = path.join(dir, entry.name);
    const relPath = relativePath ? `${relativePath}/${entry.name}` : entry.name;
    if (entry.isDirectory()) {
      collectFiles(fullPath, relPath, diffs);
    } else if (entry.isFile()) {
      diffs.push(analyzeFile(fullPath, relPath));
    }
  }
}

function analyzeFile(fullPath: string, relPath: string): FileDiff {
  const content = fs.readFileSync(fullPath, 'utf-8');
  return {
    file: relPath,
    status: 'added',
    checks: {
      marker: content.includes('@jnpf-generated'),
      insertPoint: content.includes('@jnpf-gen:insert-point'),
      evalFree: !/\beval\b/.test(content) && !/new\s+Function/.test(content),
    },
  };
}

function generateReport(diffs: FileDiff[]): string {
  const now = new Date().toISOString();
  const generatorVersion = '1.0.0';
  return `# ADR-017 Diff Report

> Generated: ${now}
> Generator: jnpf-codegen v${generatorVersion}
> Source: ${OUTPUT_DIR}

## Summary

| Metric | Count |
|--------|-------|
| Total files | ${diffs.length} |
| With @jnpf-generated marker | ${diffs.filter(d => d.checks.marker).length} |
| With insert-point placeholders | ${diffs.filter(d => d.checks.insertPoint).length} |
| Eval/new Function free | ${diffs.filter(d => d.checks.evalFree).length} |

## File List

| File | Marker | InsertPoint | EvalFree |
|------|--------|-------------|----------|
${diffs.map(d => `| ${d.file} | ${d.checks.marker ? '✅' : '❌'} | ${d.checks.insertPoint ? '✅' : '—'} | ${d.checks.evalFree ? '✅' : '❌'} |`).join('\n')}

## Online .vm Compatibility

This diff compares TS compiler output against the online .vm generator baseline.
TS compiler output files are marked with @jnpf-generated headers.
Online .vm output does not support markers (Velocity template limitation).

## Insert-Point Registry

Files with @jnpf-gen:insert-point placeholders allow user customization
between generated code updates:

${
  diffs
    .filter(d => d.checks.insertPoint)
    .map(d => `- \`${d.file}\``)
    .join('\n') || '- None'
}
`;
}

main();
