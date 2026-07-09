#!/usr/bin/env node
/**
 * P8 试点 E2E — 请假审批全链编排
 * PM → confirm-skeleton → Analyst → confirm-requirement-spec → Design(3) → confirm-design → Developer → Mapper → VisualDev
 *
 *   node scripts/p8-pilot-e2e.mjs
 */
import http from 'node:http';
import fs from 'node:fs';
import { login as jnpfLogin, apiRequest, isJnpfOk, jnpfData } from './lib/jnpf-auth.mjs';

const API = '127.0.0.1';
const PORT = 5000;
const PIPELINE_ID = 337;

// 用 jnpf-auth 登录（处理 MD5+AES 加密）
let TOKEN = '';
let SESSION = null;
try {
  SESSION = await jnpfLogin({ apiUrl: `http://${API}:${PORT}`, account: 'admin', password: '123456' });
  TOKEN = SESSION.token;
  console.log(`登录成功，token: ${TOKEN.substring(0, 20)}...`);
} catch (e) {
  console.error('登录失败:', e.message);
  process.exit(1);
}

async function api(method, path, body) {
  try {
    const result = await apiRequest(method, path, { body, session: SESSION, timeoutMs: 120000 });
    return { status: isJnpfOk(result) ? 200 : 500, code: result.code, msg: result.msg, data: result.data, raw: result };
  } catch (e) {
    return { status: 500, code: 500, msg: e.message, data: null };
  }
}

function log(tag, msg) { console.log(`[${tag}] ${msg}`); }

const sleep = (ms) => new Promise((r) => setTimeout(r, ms));

async function waitForStage(skillId, timeoutMs = 180000) {
  const deadline = Date.now() + timeoutMs;
  while (Date.now() < deadline) {
    const r = await api('GET', `/api/studio/ir/${PIPELINE_ID}/events`);
    const events = r.data?.items || [];
    const found = events.some((e) =>
      e.skillId === skillId || e.eventType?.includes(skillId.replace('-skill', '')));
    if (found) return true;
    await sleep(5000);
  }
  return false;
}

async function countSnapshots() {
  const r = await api('GET', `/api/studio/ir/${PIPELINE_ID}/snapshots`);
  return r.data?.items?.length || 0;
}

async function run() {
  console.log(`\n═══ P8 试点 E2E — Pipeline ${PIPELINE_ID} 请假审批 ═══\n`);

  // 1. PM Skill (已触发，等待 SkeletonCreated)
  log('1/8', '等待 PM Skill (SkeletonCreated)...');
  let pmDone = await waitForStage('pm-skill', 120000);
  log('PM', pmDone ? '✅ SkeletonCreated' : '⚠️ 超时（可能已完成）');

  // 2. confirm-skeleton
  log('2/8', 'confirm-skeleton...');
  await api('POST', `/api/studio/skills/pipeline/${PIPELINE_ID}/confirm-skeleton`, { autoRunAnalyst: false });
  await sleep(3000);

  // 3. Analyst
  log('3/8', '触发 Analyst Skill...');
  await api('POST', `/api/studio/skills/analyst/${PIPELINE_ID}/run`, {});
  let analystDone = await waitForStage('analyst-skill', 180000);
  log('Analyst', analystDone ? '✅ AnalysisCompleted' : '⚠️ 超时');

  // 4. confirm-requirement-spec (触发 Design Skills)
  log('4/8', 'confirm-requirement-spec (触发 Design Skills)...');
  await api('POST', `/api/studio/skills/pipeline/${PIPELINE_ID}/confirm-requirement-spec`, {});
  log('Design', '等待 DB/UI/System Design Skills...');
  await sleep(90000); // Design skills 需要时间
  const snapCount = await countSnapshots();
  log('Design', `snapshots: ${snapCount}`);

  // 5. confirm-design (触发 Developer)
  log('5/8', 'confirm-design (触发 Developer)...');
  await api('POST', `/api/studio/skills/design/${PIPELINE_ID}/confirm-design`, {});
  log('Developer', '等待 Developer Skill (codegen)...');
  await sleep(10000);

  // 6. Developer run
  log('6/8', '触发 Developer Skill...');
  const devResp = await api('POST', `/api/studio/skills/developer/${PIPELINE_ID}/run`, {});
  log('Developer', `response code=${devResp.code}`);
  await sleep(60000); // codegen 需要时间
  const snapCount2 = await countSnapshots();
  log('Developer', `snapshots: ${snapCount2}`);

  // 7. Mapper
  log('7/8', 'IR → VisualDev Mapper...');
  const mapResp = await api('POST', `/api/studio/visualdev/map/${PIPELINE_ID}`, {});
  const mapData = mapResp.data || {};
  log('Mapper', `mapped=${mapData.mappedFieldCount} gaps=${mapData.gapCount} valid=${mapData.schemaValid} fullName=${mapData.fullName}`);

  if (!mapData.formDataJson) {
    log('Mapper', '❌ 无 formData，FormPageIR 可能未 stable');
    console.log('\n═══ 全链汇总 ═══');
    const events = (await api('GET', `/api/studio/ir/${PIPELINE_ID}/events`)).data?.items || [];
    console.log(`events: ${events.length}`);
    events.forEach((e) => console.log(`  ${e.eventType} [${e.skillId || ''}]`));
    return;
  }

  // 8. VisualDev create
  log('8/8', 'POST VisualDev Base...');
  const fd = JSON.parse(mapData.formDataJson);
  const tables = [{ table: 'leave_request', tableName: 'Leave Request', primaryKey: 'F_Id',
    fields: fd.fields.map((f) => ({ field: f.__vModel__, fieldName: f.__config__?.label || f.__vModel__, dataType: 'varchar' })) }];
  const vdBody = {
    fullName: mapData.fullName, enCode: mapData.enCode, type: mapData.type, webType: mapData.webType,
    formData: mapData.formDataJson, columnData: '[]', appColumnData: '[]',
    tables: JSON.stringify(tables), dbLinkId: '558610773182513093',
    enableFlow: 0, state: 1, isRelease: 0, description: 'P8 pilot leave approval from IR',
  };
  const vdResp = await api('POST', '/api/visualdev/Base', vdBody);
  log('VisualDev', `create code=${vdResp.code} msg=${typeof vdResp.msg === 'string' ? vdResp.msg.substring(0, 120) : JSON.stringify(vdResp.msg).substring(0, 120)}`);

  // 汇总
  console.log('\n═══ P8 试点 E2E 汇总 ═══');
  const events = (await api('GET', `/api/studio/ir/${PIPELINE_ID}/events`)).data?.items || [];
  console.log(`Pipeline ${PIPELINE_ID} events: ${events.length}`);
  events.forEach((e) => console.log(`  ${e.eventType} [${e.skillId || ''}]`));
  console.log(`Snapshots: ${snapCount2}`);
  console.log(`Mapper: ${mapData.mappedFieldCount} fields, ${mapData.gapCount} gaps, valid=${mapData.schemaValid}`);
  console.log(`VisualDev: code=${vdResp.code}`);

  // 输出结果到文件
  const result = {
    pipelineId: PIPELINE_ID,
    eventCount: events.length,
    events: events.map((e) => ({ eventType: e.eventType, skillId: e.skillId })),
    snapshotCount: snapCount2,
    mapper: { mappedFieldCount: mapData.mappedFieldCount, gapCount: mapData.gapCount, schemaValid: mapData.schemaValid, fullName: mapData.fullName, formDataJson: mapData.formDataJson },
    visualDev: { code: vdResp.code, msg: vdResp.msg, data: vdResp.data },
  };
  fs.writeFileSync('.claude/evidence/p8-pilot-e2e.json', JSON.stringify(result, null, 2));
  console.log('\nevidence: .claude/evidence/p8-pilot-e2e.json');
}

run().catch((e) => { console.error('E2E 失败:', e.message); process.exit(1); });
