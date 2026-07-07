import { login, apiRequest, jnpfData, pick } from './lib/jnpf-auth.mjs';

const s = await login();
const pid = process.argv[2] || '301';
const runs = jnpfData(await apiRequest('GET', `/api/studio/skills/${pid}/runs`, { session: s })) || [];
console.log('runs:', JSON.stringify(runs.map(r => ({
  skill: pick(r, 'SkillId', 'skillId'),
  status: pick(r, 'Status', 'status'),
  err: pick(r, 'ErrorMessage', 'errorMessage'),
})), null, 2));
const items = jnpfData(await apiRequest('GET', `/api/studio/pipeline/execute/${pid}/deliverables`, { session: s }))?.items || [];
console.log('files:', items.map(i => pick(i, 'FileName', 'fileName')).join(', '));
