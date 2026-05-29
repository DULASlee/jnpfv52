#!/usr/bin/env node
/** Cursor stop hook: background episodic sync only (no follow-up loop). */
import { spawn } from 'child_process';
import path from 'path';
import { fileURLToPath } from 'url';

const repoRoot = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..', '..');
spawn(process.execPath, [path.join(repoRoot, 'scripts', 'episodic-sync.mjs'), '--background'], {
  detached: true,
  stdio: 'ignore',
  windowsHide: true,
}).unref();

process.stdout.write(JSON.stringify({ ok: true }));
process.exit(0);
