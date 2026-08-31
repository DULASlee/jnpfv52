#!/usr/bin/env node
/**
 * P001 — No Fake Green (HARD @1.0)
 * EnforcementPoint: PreMutationHook (PreToolUse Write/Edit/MultiEdit)
 * Scope: refactoring/feature/bugfix (docs-only exempt), files: *.cs,*.ts,*.vue,*.test.*
 * Hard: always BLOCK. Not AuditOnly.
 */

import fs from 'node:fs';
import { countAsserts, hasSkip, mockReplacesReal, isTestFile } from './policy-lib.mjs';
import { collectFakeGreenEvidence } from './evidence-collector.mjs';

// --- Input parsing (same as guard-write: stdin JSON + env) ---
let input = {};
try {
  const chunks = [];
  for await (const chunk of process.stdin) chunks.push(chunk);
  const raw = Buffer.concat(chunks).toString('utf-8');
  if (raw.trim()) input = JSON.parse(raw);
} catch { input = {}; }

const filePath = (process.env.CLAUDE_FILE_PATH || input.tool_input?.file_path || '').replace(/\\/g, '/');
const toolName = process.env.CLAUDE_TOOL_NAME || input.tool_name || '';
let content = '';
if (toolName === 'Write') content = input.tool_input?.content || '';
else if (toolName === 'Edit') content = input.tool_input?.new_string || input.tool_input?.newText || '';
else if (toolName === 'MultiEdit') {
  const edits = input.tool_input?.edits || [];
  content = edits.map(e => e.new_string || e.newText || '').filter(Boolean).join('\n');
} else {
  content = input.tool_input?.content || input.tool_input?.new_string || input.tool_input?.newText || '';
}

if (!filePath || !content) process.exit(0);

// Only check relevant files — test files OR cs/ts/vue with asserts
if (!/\.cs$|\.ts$|\.vue$|\.js$|\.tsx$/i.test(filePath)) process.exit(0);

// README/docs exempt (Minimum Sufficient Thought)
if (/README\.md|docs\//i.test(filePath) && !isTestFile(filePath)) process.exit(0);

let old = '';
try { if (fs.existsSync(filePath)) old = fs.readFileSync(filePath, 'utf-8'); } catch { old = ''; }

// If old is empty (new file), allow
if (!old.trim()) process.exit(0);

const oldCount = countAsserts(old);
const newCount = countAsserts(content);

// P001 Condition 1: assert weaken (allow -1 noise for refactor)
if (newCount < oldCount - 1) {
  collectFakeGreenEvidence(`assert weakened ${oldCount}→${newCount}`, filePath, oldCount, newCount);
  console.error(`BLOCKED P001@1.0 No Fake Green — assert weakened ${oldCount}→${newCount} in ${filePath}`);
  console.error(`  Hard Policy: weakening assertions is fake green. Evidence: 09-evidence/p001-fake-green.json`);
  process.exit(2);
}

// P001 Condition 2: Skip added
if (hasSkip(content) && !hasSkip(old)) {
  collectFakeGreenEvidence('skip added', filePath, oldCount, newCount);
  console.error(`BLOCKED P001@1.0 No Fake Green — skip added in ${filePath}`);
  process.exit(2);
}

// P001 Condition 3: Mock replaces real (only for test files)
if (isTestFile(filePath) && mockReplacesReal(content, old)) {
  collectFakeGreenEvidence('mock replaces real', filePath, oldCount, newCount);
  console.error(`BLOCKED P001@1.0 No Fake Green — mock replaces real verification in ${filePath}`);
  process.exit(2);
}

// P001 Condition 4: test file deleted (content has no test keywords but old did)
if (isTestFile(filePath) && /\b(test|Fact|Theory|describe|it\()/i.test(old) && !/\b(test|Fact|Theory|describe|it\()/i.test(content)) {
  collectFakeGreenEvidence('test deleted', filePath, oldCount, newCount);
  console.error(`BLOCKED P001@1.0 No Fake Green — test deleted in ${filePath}`);
  process.exit(2);
}

process.exit(0);
