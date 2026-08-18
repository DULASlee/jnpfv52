#!/usr/bin/env node
/**
 * SA /sa/run-step 冒烟 — P2-Q01 A/B 对比基线
 * 依赖 sa-service :3001
 */
import { login } from './lib/jnpf-auth.mjs';

const SA_BASE = process.env.SA_SERVICE_URL || 'http://localhost:3001';
const log = (...a) => console.log('[sa-smoke]', ...a);

async function main() {
  const session = await login();
  const tenantId = process.env.JNPF_TENANT_ID || '0';
  const body = {
    tenantId,
    projectId: 'smoke',
    eventId: 'BE-001',
    agentName: 'DictAgent',
    irStepName: 'CommandQuery',
    requirementText: '请假系统冒烟',
    skeleton: { businessEvents: [{ eventId: 'BE-001', eventName: 'Leave' }] },
    previousSteps: {},
  };

  const res = await fetch(`${SA_BASE}/sa/run-step`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'X-Tenant-Id': tenantId,
      'X-Project-Id': 'smoke',
      Authorization: `Bearer ${session.token}`,
    },
    body: JSON.stringify(body),
  });

  const json = await res.json().catch(() => ({}));
  if (!res.ok) {
    console.error('SA run-step failed', res.status, json);
    process.exit(1);
  }

  log('PASS', { status: res.status, hasOutput: json.output != null || json.Output != null });
}

main().catch(e => {
  console.error(e);
  process.exit(1);
});
