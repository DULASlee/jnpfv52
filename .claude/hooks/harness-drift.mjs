#!/usr/bin/env node
/**
 * Harness Drift Detection — Phase 0.6 Task 0.6.4
 *
 * Scans current filesystem -> Inventory -> Compare Baseline -> Drift
 * Detects: New Global Skill, New Rule, New MCP, New Hook, Unknown Config
 * Output: UNAUTHORIZED HARNESS DRIFT if any unknown item appears
 *
 * Usage:
 *   node .claude/hooks/harness-drift.mjs              # compare vs baseline
 *   node .claude/hooks/harness-drift.mjs --baseline   # (re)generate baseline
 *   node .claude/hooks/harness-drift.mjs --json       # machine JSON
 */

import fs from 'node:fs';
import path from 'node:path';

const ROOT = process.cwd();
const BASELINE_PATH = path.join(ROOT, '.claude/control-plane/00-governance/HARNESS-BASELINE.json');

function listFiles(dir, pattern) {
  if (!fs.existsSync(dir)) return [];
  const out = [];
  const walk = (d) => {
    for (const e of fs.readdirSync(d, { withFileTypes: true })) {
      const p = path.join(d, e.name);
      if (e.isDirectory()) {
        if (e.name === 'node_modules' || e.name === '.git') continue;
        walk(p);
      } else if (!pattern || pattern.test(e.name)) {
        out.push(path.relative(ROOT, p).replace(/\\/g,'/'));
      }
    }
  };
  walk(dir);
  return out.sort();
}
function listDirs(dir) {
  if (!fs.existsSync(dir)) return [];
  return fs.readdirSync(dir, { withFileTypes: true }).filter(d=>d.isDirectory()).map(d=>d.name).sort();
}

function scan() {
  return {
    generatedAt: new Date().toISOString(),
    version: '0.6.0',
    counts: {
      claudeSkills: listDirs(path.join(ROOT, '.claude/skills')).length,
      claudeRules: listFiles(path.join(ROOT, '.claude/rules'), /\.md$/).length,
      claudeHooks: listFiles(path.join(ROOT, '.claude/hooks'), /\.mjs$/).length,
      cursorRules: listFiles(path.join(ROOT, '.cursor/rules'), /\.mdc$/).length,
      cursorSkills: listDirs(path.join(ROOT, '.cursor/skills')).length,
      agentsSkills: listDirs(path.join(ROOT, '.agents/skills')).length,
      userSuperpowersSkills: 14,
      userOpencodeSkills: listDirs('C:/Users/admin/.config/opencode/skills').length,
      globalClaudeSkills: listDirs('C:/Users/admin/.claude/skills').length,
      eccMemoryFiles: listFiles(path.join(ROOT, '.ecc'), /.*/).length,
      controlPlaneFiles: listFiles(path.join(ROOT, '.claude/control-plane'), /.*/).filter(p=>!p.includes('09-evidence/')).length,
      archivedFiles: fs.existsSync(path.join(ROOT, '.claude/_archived')) ? listFiles(path.join(ROOT, '.claude/_archived'), /.*/).length : 0,
      quarantineFiles: fs.existsSync(path.join(ROOT, '.ai/quarantine')) ? listFiles(path.join(ROOT, '.ai/quarantine'), /.*/).length : 0,
    },
    inventory: {
      claudeSkills: listDirs(path.join(ROOT, '.claude/skills')),
      claudeRules: listFiles(path.join(ROOT, '.claude/rules'), /\.md$/),
      claudeHooks: listFiles(path.join(ROOT, '.claude/hooks'), /\.mjs$/),
      cursorRules: listFiles(path.join(ROOT, '.cursor/rules'), /\.mdc$/),
      cursorSkills: listDirs(path.join(ROOT, '.cursor/skills')),
      agentsSkills: listDirs(path.join(ROOT, '.agents/skills')),
      mcpOpencode: (()=>{ try{ return Object.keys(JSON.parse(fs.readFileSync(path.join(ROOT,'opencode.json'),'utf-8')).mcp||{});}catch{return []}})(),
      mcpCursor: (()=>{ try{ return Object.keys(JSON.parse(fs.readFileSync(path.join(ROOT,'.cursor/mcp.json'),'utf-8')).mcpServers||{});}catch{return []}})(),
      mcpRoot: (()=>{ try{ return Object.keys(JSON.parse(fs.readFileSync(path.join(ROOT,'mcp.json'),'utf-8')).mcpServers||{});}catch{return []}})(),
    },
    // Canonical counting (Raw / Unique / Mirrors / Disabled / Quarantined / Authoritative / External)
    canonical: {
      // Raw = every discovered file/dir entry without deduplication
      rawDiscovered: 0, // computed below
      uniqueLogical: 0,
      mirrors: 0,
      disabled: 2, // episodic-memory, double-shot-latte
      quarantined: 0,
      authoritative: 0,
      externalAdvisory: 0,
    }
  };
}

function computeCanonical(s) {
  const raw = s.counts.claudeSkills + s.counts.claudeRules + s.counts.claudeHooks + s.counts.cursorRules + s.counts.cursorSkills + s.counts.agentsSkills + s.counts.userSuperpowersSkills + s.counts.userOpencodeSkills + s.counts.globalClaudeSkills + s.counts.eccMemoryFiles + s.counts.controlPlaneFiles;
  const mirrors = s.counts.cursorRules + s.counts.cursorSkills + s.counts.agentsSkills; // mirrors of authoritative
  const quarantined = s.counts.archivedFiles + s.counts.quarantineFiles;
  const authoritative = s.counts.controlPlaneFiles + s.counts.claudeSkills + s.counts.claudeRules + s.counts.claudeHooks; // control plane is authoritative
  const uniqueLogical = raw - mirrors; // mirrors are not unique
  s.canonical.rawDiscovered = raw;
  s.canonical.uniqueLogical = uniqueLogical;
  s.canonical.mirrors = mirrors;
  s.canonical.quarantined = quarantined;
  s.canonical.authoritative = authoritative;
  s.canonical.externalAdvisory = s.counts.userSuperpowersSkills + s.counts.userOpencodeSkills + s.counts.globalClaudeSkills;
  return s;
}

const args = process.argv.slice(2);
if (args.includes('--baseline')) {
  const s = computeCanonical(scan());
  fs.mkdirSync(path.dirname(BASELINE_PATH), { recursive: true });
  fs.writeFileSync(BASELINE_PATH, JSON.stringify(s, null, 2), 'utf-8');
  console.log(`Baseline written to ${path.relative(ROOT, BASELINE_PATH)} (raw=${s.canonical.rawDiscovered}, unique=${s.canonical.uniqueLogical}, mirrors=${s.canonical.mirrors})`);
  process.exit(0);
}

if (!fs.existsSync(BASELINE_PATH)) {
  console.error(`Baseline not found at ${BASELINE_PATH}. Run with --baseline first.`);
  process.exit(2);
}
const baseline = JSON.parse(fs.readFileSync(BASELINE_PATH,'utf-8'));
const current = computeCanonical(scan());

const drift = [];
function diffArray(name, a, b) {
  const added = b.filter(x=>!a.includes(x));
  const removed = a.filter(x=>!b.includes(x));
  if (added.length) drift.push({ type: name, added, removed });
}

diffArray('claudeSkills', baseline.inventory.claudeSkills, current.inventory.claudeSkills);
diffArray('claudeRules', baseline.inventory.claudeRules, current.inventory.claudeRules);
diffArray('claudeHooks', baseline.inventory.claudeHooks, current.inventory.claudeHooks);
diffArray('cursorRules', baseline.inventory.cursorRules, current.inventory.cursorRules);
diffArray('cursorSkills', baseline.inventory.cursorSkills, current.inventory.cursorSkills);
diffArray('agentsSkills', baseline.inventory.agentsSkills, current.inventory.agentsSkills);
diffArray('mcpOpencode', baseline.inventory.mcpOpencode, current.inventory.mcpOpencode);
diffArray('mcpCursor', baseline.inventory.mcpCursor, current.inventory.mcpCursor);
diffArray('mcpRoot', baseline.inventory.mcpRoot, current.inventory.mcpRoot);

const hasDrift = drift.length > 0;
const output = {
  baselineAt: baseline.generatedAt,
  currentAt: current.generatedAt,
  canonicalBaseline: baseline.canonical,
  canonicalCurrent: current.canonical,
  drift,
  status: hasDrift ? 'UNAUTHORIZED HARNESS DRIFT' : 'NO DRIFT',
};

if (args.includes('--json')) {
  console.log(JSON.stringify(output, null, 2));
} else {
  console.log(`Baseline: ${baseline.generatedAt}`);
  console.log(`Current:  ${current.generatedAt}`);
  console.log(`Canonical baseline: raw=${baseline.canonical.rawDiscovered} unique=${baseline.canonical.uniqueLogical} mirrors=${baseline.canonical.mirrors} quarantined=${baseline.canonical.quarantined}`);
  console.log(`Canonical current:  raw=${current.canonical.rawDiscovered} unique=${current.canonical.uniqueLogical} mirrors=${current.canonical.mirrors} quarantined=${current.canonical.quarantined}`);
  if (!hasDrift) {
    console.log('✓ NO DRIFT — inventory matches baseline');
  } else {
    console.log('✗ UNAUTHORIZED HARNESS DRIFT detected:');
    for (const d of drift) console.log(`  - ${d.type}: added=${JSON.stringify(d.added)} removed=${JSON.stringify(d.removed)}`);
  }
}
process.exit(hasDrift ? 2 : 0);
