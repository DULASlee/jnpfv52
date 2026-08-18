#!/usr/bin/env node
import { login, apiRequest } from './lib/jnpf-auth.mjs';
const session = await login();
const evRes = await apiRequest('GET', `api/studio/ir/311/events`, { session });
const events = evRes.json?.data || [];
// 过滤 IR3_GeneratedCode 相关的事件并按 Sequence 排序
const codegen = events
  .filter(e => (e.fragmentId || e.FragmentId) === 'codegen:311')
  .sort((a, b) => (a.sequence ?? a.Sequence ?? 0) - (b.sequence ?? b.Sequence ?? 0));
console.log('codegen events count:', codegen.length);
codegen.forEach(e => {
  console.log(`seq=${e.sequence ?? e.Sequence ?? '?'} type=${e.eventType || e.EventType} ver=${e.fragmentVersion ?? e.FragmentVersion ?? '?'} ts=${e.createdAt ?? e.CreatedAt}`);
});
