import { login, apiRequest, jnpfData, isJnpfOk } from './lib/jnpf-auth.mjs';
import fs from 'node:fs';

const session = await login();
for (const id of [358, 357]) {
  const res = await apiRequest(
    'GET',
    `/api/studio/pipeline/execute/${id}/deliverables/content?relativePath=${encodeURIComponent('00-gate-report.json')}`,
    { session },
  );
  const data = jnpfData(res) ?? res.json?.data ?? res.json;
  const text = typeof data === 'string' ? data : data?.content ?? data?.Content ?? JSON.stringify(data);
  fs.writeFileSync(`scripts/_gate-report-${id}.txt`, String(text), 'utf8');
  console.log(id, 'ok', isJnpfOk(res), 'len', String(text).length, 'head', String(text).slice(0, 500));
  console.log('has object Object', String(text).includes('[object Object]'));
}
