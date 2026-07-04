#!/usr/bin/env node
/**
 * 执行 inteAssistant Migrations/*.sql
 *
 *   node scripts/run-inte-migration.mjs
 *   node scripts/run-inte-migration.mjs 20260801_Phase3_Design_Skills.sql
 */
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { runSqlFile } from './lib/jnpf-db.mjs';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const MIG_DIR = path.join(__dirname, '../backend/modularity/inteAssistant/Migrations');

const files = process.argv.slice(2).length
  ? process.argv.slice(2)
  : [
    '20260704_Phase1_IR_Infrastructure.sql',
    '20260718_Phase2_Skills_Infrastructure.sql',
    '20260801_Phase3_Design_Skills.sql',
  ];

for (const f of files) {
  const p = path.join(MIG_DIR, f);
  console.log('[migrate]', f, '...');
  runSqlFile(p);
  console.log('[migrate]', f, 'OK');
}

console.log('[migrate] done', files.length, 'file(s)');
