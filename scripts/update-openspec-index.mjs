#!/usr/bin/env node
/**
 * Regenerate openspec/specs/README.md from spec.md frontmatter / titles.
 * Usage: node scripts/update-openspec-index.mjs
 */
import fs from 'fs';
import path from 'path';
import { getRepoRoot } from './toolchain-lib.mjs';

const root = getRepoRoot();
const specsDir = path.join(root, 'openspec', 'specs');
const indexPath = path.join(specsDir, 'README.md');

function findSpecFiles(dir, acc = []) {
  if (!fs.existsSync(dir)) return acc;
  for (const ent of fs.readdirSync(dir, { withFileTypes: true })) {
    const p = path.join(dir, ent.name);
    if (ent.isDirectory()) findSpecFiles(p, acc);
    else if (ent.name === 'spec.md') acc.push(p);
  }
  return acc;
}

const specs = findSpecFiles(specsDir);
const rows = specs.map((f) => {
  const rel = path.relative(specsDir, f).replace(/\\/g, '/');
  const cap = path.dirname(rel).replace(/\\/g, '/');
  const content = fs.readFileSync(f, 'utf8');
  const title = content.match(/^#\s+(.+)/m)?.[1]?.trim() ?? cap;
  const stat = fs.statSync(f);
  const modified = stat.mtime.toISOString().slice(0, 10);
  return `| ${cap} | [\`${rel}\`](${rel.replace(/ /g, '%20')}) | ${title} | ${modified} |`;
});

const body = `# OpenSpec 知识库（\`openspec/specs/\`）

> **自动生成**：\`node scripts/update-openspec-index.mjs\` — 请勿手工改表格行

| Capability | Spec | 标题 | 文件 mtime |
|------------|------|------|------------|
${rows.join('\n')}

## 维护规则

- 新标准：\`openspec/changes/\` 起草 → \`/opsx:archive\` 归档到本目录
- 开发执行：Superpowers（**禁止** /opsx:apply 编码）
- 无 spec 的架构切面不得进入 \`executing-plans\`
`;

fs.writeFileSync(indexPath, body, 'utf8');
console.log(`[OK] Updated ${indexPath} (${specs.length} specs)`);
