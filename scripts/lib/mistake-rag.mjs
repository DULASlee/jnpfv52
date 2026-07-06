#!/usr/bin/env node
/**
 * mistake-rag — JNPF 错题本 RAG 搜索引擎
 *
 * 测试失败时，自动从 .claude/memory/mistake-log.md 匹配历史修复方案。
 * 关键词倒排索引 + TF-IDF 加权，零外部依赖。
 *
 * 用法:
 *   # 搜索匹配的错题
 *   node scripts/lib/mistake-rag.mjs "ReferenceError: is not defined"
 *   node scripts/lib/mistake-rag.mjs "SSE no data" --top=3
 *
 *   # JSON 输出（供 Agent 消费）
 *   node scripts/lib/mistake-rag.mjs --json "import type"
 *
 *   # 从 stdin 读错误日志
 *   cat error.log | node scripts/lib/mistake-rag.mjs --stdin
 *
 *   # 从测试输出文件读
 *   node scripts/lib/mistake-rag.mjs --file=test-results/output.txt
 */

import { readFileSync, existsSync } from 'fs';
import { resolve, dirname } from 'path';
import { fileURLToPath } from 'url';

const __dirname = dirname(fileURLToPath(import.meta.url));

// ── Stop Words（必须在所有函数调用之前定义） ──
const STOP_WORDS = new Set([
  'the', 'a', 'an', 'is', 'are', 'was', 'were', 'be', 'been', 'being',
  'have', 'has', 'had', 'do', 'does', 'did', 'will', 'would', 'could', 'should',
  'may', 'might', 'can', 'shall', 'to', 'of', 'in', 'for', 'on', 'with',
  'at', 'by', 'from', 'as', 'into', 'about', 'this', 'that', 'it', 'its',
  'and', 'or', 'not', 'no', 'but', 'if', 'then', 'else', 'when', 'than',
  '所以', '因为', '但是', '然后', '可以', '已经', '这个', '那个',
  '没有', '不能', '不是', '一个', '如果', '就是', '还是', '或者',
]);
const MISTAKE_LOG = resolve(__dirname, '..', '..', '.claude', 'memory', 'mistake-log.md');

// ── CLI ──
let query = '';
let topN = 5;
let jsonMode = false;
let fromStdin = false;
let fromFile = '';

const args = process.argv.slice(2);
const queryParts = [];
for (let i = 0; i < args.length; i++) {
  if (args[i] === '--json') jsonMode = true;
  else if (args[i] === '--top' || args[i] === '-n') topN = parseInt(args[++i]) || 5;
  else if (args[i].startsWith('--top=')) topN = parseInt(args[i].split('=')[1]) || 5;
  else if (args[i] === '--stdin') fromStdin = true;
  else if (args[i] === '--file' || args[i] === '-f') fromFile = args[++i];
  else queryParts.push(args[i]);
}
query = queryParts.join(' ');

// ── 输入源 ──
if (fromStdin) {
  const chunks = [];
  process.stdin.setEncoding('utf8');
  process.stdin.on('readable', () => {
    let chunk;
    while ((chunk = process.stdin.read()) !== null) chunks.push(chunk);
  });
  process.stdin.on('end', () => {
    query = chunks.join(' ');
    run();
  });
  if (!process.stdin.isTTY) process.stdin.resume();
} else if (fromFile) {
  query = readFileSync(fromFile, 'utf8');
  run();
} else if (query) {
  run();
} else {
  console.log('用法: node scripts/lib/mistake-rag.mjs <query> [--top=5] [--json] [--stdin] [--file=path]');
  process.exit(1);
}

function run() {
  // ── 解析错题本 ──
  if (!existsSync(MISTAKE_LOG)) {
    const result = { error: `错题本不存在: ${MISTAKE_LOG}`, matches: [] };
    console.log(jsonMode ? JSON.stringify(result) : '❌ 错题本文件不存在');
    process.exit(1);
  }

  const raw = readFileSync(MISTAKE_LOG, 'utf8');
  const entries = parseMistakeLog(raw);

  if (entries.length === 0) {
    const result = { query, matches: [] };
    console.log(jsonMode ? JSON.stringify(result) : '📭 错题本中无条目');
    process.exit(0);
  }

  // ── 构建倒排索引 ──
  const index = buildIndex(entries);

  // ── 搜索 ──
  const results = search(query, entries, index, topN);

  // ── 输出 ──
  if (jsonMode) {
    console.log(JSON.stringify({
      query,
      source: MISTAKE_LOG,
      totalEntries: entries.length,
      matches: results.filter(r => r.score > 0).map(r => ({
        id: r.entry.id,
        score: Math.round(r.score * 100) / 100,
        symptom: r.entry.symptom,
        rootCause: r.entry.rootCause,
        fix: r.entry.fix,
        keywords: r.entry.keywords,
        date: r.entry.date,
      })),
    }, null, 2));
  } else {
    console.log(`\n🔍 错题本搜索: "${query.substring(0, 80)}"`);
    console.log(`   共 ${entries.length} 条记录，匹配 ${results.filter(r => r.score > 0).length} 条\n`);

    results.filter(r => r.score > 0).forEach((r, i) => {
      const bar = '█'.repeat(Math.min(10, Math.round(r.score * 10)));
      console.log(`${i + 1}. [${r.entry.id}] ${bar} (${Math.round(r.score * 100)}%)`);
      console.log(`   症状: ${r.entry.symptom}`);
      console.log(`   根因: ${r.entry.rootCause}`);
      console.log(`   修复: ${r.entry.fix}`);
      console.log(`   关键词: ${r.entry.keywords.join(', ')}`);
      console.log();
    });

    if (results.filter(r => r.score > 0).length === 0) {
      console.log('   📭 无匹配结果。考虑追加此错误到错题本。');
    }
  }
}

// ── 解析错题本 Markdown ──
function parseMistakeLog(raw) {
  const entries = [];
  // 匹配 ### MXXX | 标题 格式的条目
  const entryRegex = /### (M\d{3}) \| (.+?)\n\n- \*\*症状\*\*：(.+?)\n- \*\*根因\*\*：(.+?)\n- \*\*(?:修复|规则)\*\*：(.+?)\n- \*\*日期\*\*：(.+?) \| \*\*关键词\*\*：`(.+?)`/gs;

  let match;
  while ((match = entryRegex.exec(raw)) !== null) {
    entries.push({
      id: match[1],
      title: match[2].trim(),
      symptom: match[3].trim(),
      rootCause: match[4].trim(),
      fix: match[5].trim(),
      date: match[6].trim(),
      keywords: match[7].split(/[,，]、?\s*/).map(k => k.replace(/`/g, '').trim()).filter(Boolean),
    });
  }

  return entries;
}

// ── 构建 TF-IDF 倒排索引 ──
function buildIndex(entries) {
  const index = {}; // word → [{entryIdx, tf}]
  const df = {};    // word → document frequency

  entries.forEach((entry, i) => {
    const text = `${entry.symptom} ${entry.rootCause} ${entry.fix} ${entry.title} ${entry.keywords.join(' ')}`;
    const tokens = tokenize(text);
    const termFreq = {};
    tokens.forEach(t => { termFreq[t] = (termFreq[t] || 0) + 1; });

    Object.keys(termFreq).forEach(term => {
      if (!index[term]) index[term] = [];
      index[term].push({ entryIdx: i, tf: termFreq[term] });
      df[term] = (df[term] || 0) + 1;
    });
  });

  const N = entries.length;
  // 预计算 IDF
  const idf = {};
  Object.keys(df).forEach(term => {
    idf[term] = Math.log((N - df[term] + 0.5) / (df[term] + 0.5) + 1);
  });

  return { index, idf, N };
}

// ── 搜索 ──
function search(query, entries, index, topN) {
  const queryTokens = tokenize(query);
  const scores = new Array(entries.length).fill(0);

  // TF-IDF scoring
  queryTokens.forEach(term => {
    const postings = index.index[term];
    if (!postings) return;
    const idf = index.idf[term] || 1;
    postings.forEach(({ entryIdx, tf }) => {
      scores[entryIdx] += tf * idf;
    });
  });

  // 关键词精确匹配加分
  entries.forEach((entry, i) => {
    entry.keywords.forEach(kw => {
      if (query.toLowerCase().includes(kw.toLowerCase())) {
        scores[i] += 2.0;
      }
      // 关键词的各个部分
      kw.split(/[/\s]+/).forEach(part => {
        if (part.length >= 2 && query.toLowerCase().includes(part.toLowerCase())) {
          scores[i] += 0.5;
        }
      });
    });
  });

  // 归一化 + 排序
  const maxScore = Math.max(...scores, 1);
  const results = entries.map((entry, i) => ({
    entry,
    score: scores[i] / maxScore,
  }));

  results.sort((a, b) => b.score - a.score);
  return results.slice(0, topN);
}

// ── 分词 ──
function tokenize(text) {
  return text
    .toLowerCase()
    .replace(/[`'".,;:!?()[\]{}<>|\\/@#$%^&*+=~-]/g, ' ')
    .split(/\s+/)
    .filter(t => t.length >= 2)
    .filter(t => !STOP_WORDS.has(t));
}
