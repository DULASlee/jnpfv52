/**
 * 读取 ConnectionStrings.json 并通过 sqlcmd 执行 SQL（Dev 迁移 / DoD 种子）
 */
import fs from 'node:fs';
import path from 'node:path';
import { execFileSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const REPO_ROOT = path.resolve(__dirname, '../..');
const CS_PATH = path.join(
  REPO_ROOT,
  'backend/application/JNPF.API.Entry/Configurations/ConnectionStrings.json',
);

let cachedConfig;

export function loadDbConfig() {
  if (cachedConfig) return cachedConfig;
  if (!fs.existsSync(CS_PATH)) {
    throw new Error(`ConnectionStrings.json not found: ${CS_PATH}`);
  }
  const raw = JSON.parse(fs.readFileSync(CS_PATH, 'utf8'));
  const cfg = raw.ConnectionStrings?.ConnectionConfigs?.[0]
    ?? raw.ConnectionStrings?.[0];
  if (!cfg) throw new Error('No ConnectionConfigs in ConnectionStrings.json');

  cachedConfig = {
    server: cfg.Host ?? cfg.host,
    database: cfg.DBName ?? cfg.DbName ?? cfg.dbName,
    user: cfg.UserName ?? cfg.DbUser ?? cfg.user,
    password: cfg.Password ?? cfg.DbPwd ?? cfg.password,
  };
  if (!cachedConfig.server || !cachedConfig.database) {
    throw new Error('Incomplete database config');
  }
  return cachedConfig;
}

export function runSqlQuery(sql, { timeoutMs = 60_000 } = {}) {
  const cfg = loadDbConfig();
  const args = [
    '-S', cfg.server,
    '-d', cfg.database,
    '-U', cfg.user,
    '-P', cfg.password,
    '-Q', sql,
    '-W',
    '-h', '-1',
  ];
  const out = execFileSync('sqlcmd', args, {
    encoding: 'utf8',
    timeout: timeoutMs,
    stdio: ['ignore', 'pipe', 'pipe'],
  });
  return out.trim();
}

export function runSqlFile(filePath, { timeoutMs = 120_000 } = {}) {
  const cfg = loadDbConfig();
  const abs = path.resolve(filePath);
  if (!fs.existsSync(abs)) throw new Error(`SQL file not found: ${abs}`);
  execFileSync('sqlcmd', [
    '-S', cfg.server,
    '-d', cfg.database,
    '-U', cfg.user,
    '-P', cfg.password,
    '-i', abs,
    '-b',
  ], { encoding: 'utf8', timeout: timeoutMs, stdio: ['ignore', 'pipe', 'pipe'] });
}

export function setProjectTokenConsumed(projectId, ratio = 0.96) {
  const id = String(projectId).replace(/'/g, "''");
  runSqlQuery(
    `UPDATE ai_projects SET F_TokenConsumed = CAST(F_TokenBudget * ${ratio} AS BIGINT) WHERE F_Id = '${id}'`,
  );
}

export function resetProjectTokenConsumed(projectId) {
  const id = String(projectId).replace(/'/g, "''");
  runSqlQuery(`UPDATE ai_projects SET F_TokenConsumed = 0, F_LlmBudgetStatus = 'green' WHERE F_Id = '${id}'`);
}

export function getSkillLlmPolicy(skillId) {
  const id = String(skillId).replace(/'/g, "''");
  const out = runSqlQuery(`SET NOCOUNT ON; SELECT F_MaxLlmCalls FROM ai_skill_llm_policy WHERE F_SkillId = '${id}'`);
  const line = out.split(/\r?\n/).map(s => s.trim()).find(s => /^\d+$/.test(s));
  return line != null ? Number(line) : null;
}
