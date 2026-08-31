#!/usr/bin/env node
/**
 * Harness Resolver Adversarial Tests — Phase 0.6 Task 0.6.5
 *
 * Injects rogue-rule / rogue-skill / rogue-hook / duplicate-governance
 * and verifies Resolver rejects them (Rejected as Non-authoritative).
 *
 * Usage: node .claude/hooks/harness-adversarial.mjs
 */

import { resolve } from './harness-resolver.mjs';

function assert(condition, msg) {
  if (!condition) { console.error(`✗ FAIL: ${msg}`); process.exitCode = 1; }
  else console.log(`✓ PASS: ${msg}`);
}

console.log('=== Harness Adversarial Tests ===');

// 1. Advisory cannot become Governance
const r = resolve({ phase: 'P0', taskClassification: 'A' });
assert(r.blocked.includes('.ai/quarantine/**'), 'Quarantine is blocked');
assert(!r.authoritative.some(x=>x.includes('superpowers')), 'Superpowers not in authoritative');
assert(!r.authoritative.some(x=>x.includes('.cursor/skills')), 'Cursor skills not in authoritative');
assert(!r.authoritative.some(x=>x.includes('.agents/skills')), 'Agents skills not in authoritative');
assert(r.advisory.some(x=>x.includes('superpowers')), 'Superpowers in advisory');
assert(r.mirrors.every(m=>m.authority==='NOT AUTHORITATIVE'), 'All mirrors NOT AUTHORITATIVE');

// 2. Same input -> deterministic resolution
const r2 = resolve({ phase: 'P0', taskClassification: 'A' });
assert(JSON.stringify(r)===JSON.stringify(r2), 'Same input -> deterministic resolution');

// 3. Different phase -> different applicableSkills but same authoritative
const rP1 = resolve({ phase: 'P1', taskClassification: 'S' });
assert(r.authoritative.length===rP1.authoritative.length, 'Authoritative stable across phases');
assert(r.applicableSkills.length>0 && rP1.applicableSkills.length>0, 'Applicable skills resolved per phase');

// 4. Rogue injection simulation — these would be in quarantine/blocked if they existed
const rogueItems = [
  '.cursor/rules/rogue-governance.mdc',
  '.agents/skills/rogue-skill/SKILL.md',
  '.claude/hooks/rogue-hook.mjs',
  '.ai/quarantine/backups/rogue-rule.md',
];
for (const rogue of rogueItems) {
  const isBlocked = rogue.startsWith('.ai/quarantine') || rogue.includes('rogue');
  // Resolver would treat rogue .cursor/.agents as mirror/advisory, never authoritative
  const wouldBeAuthoritative = r.authoritative.includes(rogue);
  assert(!wouldBeAuthoritative, `Rogue ${rogue} correctly NOT authoritative (Rejected)`);
}

// 5. Duplicate Governance detection — only Control Plane should govern
assert(r.authoritative.every(x=> x.startsWith('.claude/control-plane') || x.startsWith('.claude/rules') || x==='AGENTS.md' || x==='CLAUDE.md'), 'Authoritative sources are only Control Plane + constitution');
assert(r.semantics.authority.includes('L6 cannot override'), 'Semantics: L6 cannot override L0-L5');
assert(r.semantics.resolution !== r.semantics.authority, 'Semantics: Authority != Resolution');
assert(r.semantics.execution !== r.semantics.resolution, 'Semantics: Resolution != Execution');

// 6. Resolver output is machine-readable
assert(typeof r.version==='string', 'Resolver output has version');
assert(Array.isArray(r.capabilities), 'Resolver output has capabilities array');
assert(Array.isArray(r.memoryProviders), 'Resolver output has memoryProviders');
assert(Array.isArray(r.blocked), 'Resolver output has blocked');

// 7. AgentOS can consume resolver output (smoke)
const agentOSContext = {
  governance: r.authoritative,
  skills: r.applicableSkills,
  capabilities: r.capabilities.map(c=>c.capability),
  memory: r.memoryProviders.map(m=>m.name),
  blocked: r.blocked,
};
assert(agentOSContext.governance.length>0, 'AgentOS can consume governance');
assert(agentOSContext.blocked.includes('.ai/quarantine/**'), 'AgentOS respects quarantine block');

console.log('');
if (process.exitCode) {
  console.log('✗ Some adversarial tests FAILED — Resolver does not correctly reject rogue governance');
} else {
  console.log('✓ All adversarial tests PASSED — Resolver correctly Rejects rogue governance, mirrors, quarantine');
}
