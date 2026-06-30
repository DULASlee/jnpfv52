/**
 * hook-lib.mjs — hooks 共享：项目根、stdin、Session 防重入、Skill 限速
 */
import { execSync } from 'child_process';
import { existsSync, readFileSync, writeFileSync, mkdirSync } from 'fs';
import { join, dirname } from 'path';

export const SESSION_LOCK = '.session-init-lock.json';
export const SKILL_LOAD_STATE = '.skill-load-state.json';
export const SESSION_DEBOUNCE_MS = 60_000;
export const SESSION_RESUME_TTL_MS = 30 * 60_000;
export const SKILL_WINDOW_MS = 15_000;
export const SKILL_MAX_PER_WINDOW = 6;
export const SKILL_MIN_GAP_MS = 2_000;

export function getProjectRoot() {
  try {
    return execSync('git rev-parse --show-toplevel', {
      encoding: 'utf-8', stdio: 'pipe', timeout: 3000,
    }).trim().replace(/\\/g, '/');
  } catch { /* fall through */ }
  let dir = process.cwd();
  for (let i = 0; i < 6; i++) {
    if (existsSync(`${dir}/CLAUDE.md`)) return dir.replace(/\\/g, '/');
    const parent = dir.replace(/[/\\][^/\\]+$/, '');
    if (parent === dir) break;
    dir = parent;
  }
  return process.cwd().replace(/\\/g, '/');
}

export function statePath(name) {
  const root = getProjectRoot();
  const dir = join(root, '.claude');
  mkdirSync(dir, { recursive: true });
  return join(dir, name);
}

export async function readStdin(ms = 3000) {
  return Promise.race([
    (async () => {
      const chunks = [];
      for await (const c of process.stdin) chunks.push(c);
      return Buffer.concat(chunks).toString('utf-8');
    })(),
    new Promise((_, reject) => setTimeout(() => reject(new Error('stdin timeout')), ms)),
  ]);
}

export function isSubprocessReentrant() {
  return process.env.EPISODIC_MEMORY_SUMMARIZER_GUARD === '1';
}

export function readJson(path, fallback = null) {
  try {
    if (!existsSync(path)) return fallback;
    return JSON.parse(readFileSync(path, 'utf-8'));
  } catch {
    return fallback;
  }
}

export function writeJson(path, data) {
  mkdirSync(dirname(path), { recursive: true });
  writeFileSync(path, JSON.stringify(data, null, 2), 'utf-8');
}

export function shouldSkipSessionInit(eventSource = 'startup') {
  if (isSubprocessReentrant()) {
    return { skip: true, reason: 'subprocess-reentrant' };
  }

  const lockPath = statePath(SESSION_LOCK);
  const lock = readJson(lockPath);
  const now = Date.now();

  if (lock?.lastRun && now - lock.lastRun < SESSION_DEBOUNCE_MS) {
    return { skip: true, reason: 'debounce', lock };
  }

  if (
    eventSource === 'resume'
    && lock?.lastRun
    && now - lock.lastRun < SESSION_RESUME_TTL_MS
  ) {
    return { skip: true, reason: 'resume-cached', lock };
  }

  return { skip: false, lock };
}

export function markSessionInit(eventSource = 'startup') {
  const lockPath = statePath(SESSION_LOCK);
  const prev = readJson(lockPath) || {};
  writeJson(lockPath, {
    lastRun: Date.now(),
    pid: process.pid,
    source: eventSource,
    runCount: (prev.runCount || 0) + 1,
  });
}

export function checkSkillLoadRate(skillName = '') {
  const path = statePath(SKILL_LOAD_STATE);
  const now = Date.now();
  let state = readJson(path, { calls: [] });

  state.calls = (state.calls || []).filter((c) => now - c.ts < SKILL_WINDOW_MS);

  const normalized = (skillName || 'unknown').toLowerCase();
  const lastSame = [...state.calls].reverse().find((c) => c.skill === normalized);
  if (lastSame && now - lastSame.ts < SKILL_MIN_GAP_MS) {
    return { allow: false, reason: 'dedupe-same-skill' };
  }

  if (state.calls.length >= SKILL_MAX_PER_WINDOW) {
    return { allow: false, reason: 'storm-limit' };
  }

  state.calls.push({ skill: normalized, ts: now });
  writeJson(path, state);
  return { allow: true };
}
