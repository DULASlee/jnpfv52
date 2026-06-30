#!/usr/bin/env node
/**
 * SessionStart Hook — Superpowers 验证（轻量，带防重入）
 * 主入口已迁至 session-scheduler.mjs；本文件保留供手动调用。
 */

import { existsSync } from 'fs';
import { homedir } from 'os';
import { join } from 'path';
import { shouldSkipSessionInit } from './hook-lib.mjs';

const HOME = homedir();

const skip = shouldSkipSessionInit('startup');
if (skip.skip) {
  console.error(`[superpowers-check] 跳过 (${skip.reason})`);
  process.exit(0);
}

const spDir = join(HOME, '.claude', 'plugins', 'cache', 'superpowers-marketplace', 'superpowers');
console.error(`[superpowers-check] ${existsSync(spDir) ? '✅' : '❌'} superpowers`);
process.exit(0);
