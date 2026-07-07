import { login, apiRequest, jnpfData, pick } from './lib/jnpf-auth.mjs';
import { runSqlQuery } from './lib/jnpf-db.mjs';

const pid = '301';
const s = await login();

// 1. skill run 状态
const runs = jnpfData(await apiRequest('GET', `/api/studio/skills/${pid}/runs`, { session: s })) || [];
console.log('=== skill runs ===');
for (const r of runs) {
  console.log(pick(r, 'SkillId', 'skillId'), pick(r, 'Status', 'status'), pick(r, 'ErrorMessage', 'errorMessage'));
}

// 2. IR 事件统计
const events = jnpfData(await apiRequest('GET', `/api/studio/ir/${pid}/events`, { session: s })) || [];
const types = {};
for (const e of events) {
  const t = pick(e, 'eventType', 'EventType') || '?';
  types[t] = (types[t] || 0) + 1;
}
console.log('\n=== IR event counts ===', types);

// 3. 骨架 businessEvents 数量
const snaps = jnpfData(await apiRequest('GET', `/api/studio/ir/${pid}/snapshots`, { session: s })) || [];
const skel = snaps.find(x => (pick(x, 'fragmentType', 'FragmentType') || '').includes('Skeleton'));
if (skel) {
  const payload = typeof skel.payload === 'string' ? JSON.parse(skel.payload) : skel.payload;
  const be = payload?.businessEvents || payload?.BusinessEvents || [];
  console.log('\n=== skeleton businessEvents ===', be.length, 'events');
  console.log('expected SA calls:', be.length * 9, '(events × 9 steps)');
  console.log('SaStepCompleted so far:', types['SaStepCompleted'] || 0);
}

// 4. SA 单步冒烟（5s 超时，不轮询）
console.log('\n=== SA /sa/run-step smoke (5s timeout) ===');
try {
  const ctrl = AbortSignal.timeout(5000);
  const res = await fetch('http://127.0.0.1:3001/sa/run-step', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', 'X-Tenant-Id': '0', 'X-Project-Id': pid },
    body: JSON.stringify({
      tenantId: '0', projectId: pid, eventId: 'BE-001', agentName: 'DictAgent',
      irStepName: 'CommandQuery', requirementText: '请假', skeleton: {}, previousSteps: {},
    }),
    signal: ctrl,
  });
  const json = await res.json().catch(() => ({}));
  console.log('HTTP', res.status, JSON.stringify(json).slice(0, 300));
} catch (e) {
  console.log('SA FAIL:', e.name, e.message);
}

// 5. 端口
console.log('\n=== ports ===');
for (const port of [5000, 3100, 3001]) {
  try {
    const r = await fetch(`http://127.0.0.1:${port}/`, { signal: AbortSignal.timeout(2000) });
    console.log(port, '→', r.status);
  } catch (e) {
    console.log(port, '→', e.cause?.code || e.message);
  }
}
