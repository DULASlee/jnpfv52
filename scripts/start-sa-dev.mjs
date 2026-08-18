#!/usr/bin/env node
/** 本地启动 sa-service（读取 JNPF ConnectionStrings.json） */
import { spawn } from 'node:child_process';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { loadSaConnectionString } from './lib/jnpf-db.mjs';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const saDir = path.join(__dirname, '../sa-service');
const cs = loadSaConnectionString();

const env = {
  ...process.env,
  SA_SERVICE_PORT: '3001',
  SA_DB_BACKEND: 'sqlserver',
  SA_DB_CONNECTION_STRING: cs,
  LLM_GATEWAY_URL: 'http://127.0.0.1:5000/api/LlmGateway/ChatAsync',
};

const child = spawn('npx', ['tsx', 'src/server.ts'], {
  cwd: saDir,
  env,
  stdio: 'inherit',
  shell: true,
});

child.on('exit', (code) => process.exit(code ?? 0));
