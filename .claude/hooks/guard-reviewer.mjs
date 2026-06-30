#!/usr/bin/env node
/**
 * PostToolUse Hook — Reviewer L0 预筛选 (JNPF V3.0 新增)
 *
 * 职责：代码写入后生成轻量级审查标志文件，供 Reviewer L1 读取。
 * 不是替代 Reviewer，而是"预处理"——帮 Reviewer 排除明显问题，聚焦深度审查。
 *
 * 触发条件：Write/Edit/MultiEdit 完成后
 * 执行时间：< 200ms（不阻塞编辑流程）
 * 输出：.claude/review/flags/{file}.json
 */

import { readStdin } from './hook-lib.mjs';
import { writeFileSync, mkdirSync } from 'fs';
import { join } from 'path';

const FLAGS_DIR = '.claude/review/flags';
const STDIN_MS = 1000;

async function quickAudit({ filePath, content }) {
  const flags = [];
  const lines = content.split('\n');

  // D2: TODO/FIXME/HACK/XXX
  for (let i = 0; i < lines.length; i++) {
    if (/TODO|FIXME|HACK|XXX/.test(lines[i]) && !lines[i].trim().startsWith('//')) {
      flags.push({ line: i + 1, rule: 'D2-TODO', level: 'WARN', msg: 'Found TODO/FIXME in code' });
    }
  }

  // D2: 空 catch 块
  for (let i = 0; i < lines.length; i++) {
    if (/catch\s*\([^)]*\)\s*\{\s*\}/.test(lines[i])) {
      flags.push({ line: i + 1, rule: 'D2-SWALLOW', level: 'BLOCK', msg: 'Empty catch block' });
    }
  }

  // D4: 方法长度 > 50 行
  let methodStart = -1, braceCount = 0;
  for (let i = 0; i < lines.length; i++) {
    if (/^\s*(public|private|protected|internal)\s+/.test(lines[i]) && /\{/.test(lines[i])) {
      methodStart = i;
      braceCount = 1;
    } else if (methodStart >= 0) {
      braceCount += (lines[i].match(/\{/g) || []).length;
      braceCount -= (lines[i].match(/\}/g) || []).length;
      if (braceCount === 0) {
        if (i - methodStart + 1 > 50) {
          flags.push({ line: methodStart + 1, rule: 'D4-LENGTH', level: 'WARN', msg: `Method ${i - methodStart + 1} lines (>50)` });
        }
        methodStart = -1;
      }
    }
  }

  // D4: 魔法数字（≥3位数字）
  for (let i = 0; i < lines.length; i++) {
    const magic = lines[i].match(/[^\"'](\b\d{3,}\b)/);
    if (magic && !lines[i].trim().startsWith('//') && !/version|code|status|http|port/i.test(lines[i])) {
      flags.push({ line: i + 1, rule: 'D4-MAGIC', level: 'NOTE', msg: `Magic number: ${magic[1]}` });
    }
  }

  return flags;
}

try {
  let input = {};
  try {
    const raw = await readStdin(STDIN_MS);
    if (raw.trim()) input = JSON.parse(raw);
  } catch {
    process.exit(0);
  }

  const fp = (input.tool_input?.file_path || '').replace(/\\/g, '/');
  const toolName = input.tool_name || '';
  if (!['Write', 'Edit', 'MultiEdit'].includes(toolName)) process.exit(0);

  let content = '';
  if (toolName === 'Write') content = input.tool_input?.content || '';
  else if (toolName === 'Edit') content = input.tool_input?.newText || '';
  else if (toolName === 'MultiEdit') {
    content = (input.tool_input?.edits || []).map(e => e.new_string || '').filter(Boolean).join('\n');
  }
  if (!content) process.exit(0);

  const flags = await quickAudit({ filePath: fp, content });
  const flagPath = join(process.cwd(), FLAGS_DIR, `${fp.replace(/[\\/]/g, '_')}.json`);
  mkdirSync(join(process.cwd(), FLAGS_DIR), { recursive: true });

  writeFileSync(flagPath, JSON.stringify({
    filePath: fp,
    timestamp: Date.now(),
    flags,
    summary: {
      BLOCK: flags.filter(f => f.level === 'BLOCK').length,
      WARN: flags.filter(f => f.level === 'WARN').length,
      NOTE: flags.filter(f => f.level === 'NOTE').length,
    }
  }, null, 2));

  const blks = flags.filter(f => f.level === 'BLOCK');
  if (blks.length > 0) {
    console.error(`[guard-reviewer] ${blks.length} BLOCK in ${fp}`);
    blks.forEach(b => console.error(`  L${b.line}: ${b.msg}`));
  }
  process.exit(0);
} catch (e) {
  console.error('[guard-reviewer] Error:', e.message);
  process.exit(0);
}
