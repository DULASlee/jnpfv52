// R1 Attack Test — asserts §7 six attack conditions against fixtures-run1/callgraph.json
// 只读断言，不产生业务副作用。
const g = require('./fixtures-run1/callgraph.json');
const M = g.methods, C = g.calls;
const out = [];
const check = (name, pass, detail) => out.push({ name, pass, detail });

// ── Attack A: Overload 不合并 ──
const ovl = M.filter(m => m.containing_type.endsWith('OverloadFoo') && m.name === 'Save');
const ovlIds = new Set(ovl.map(m => m.method_id));
const caller = M.find(m => m.method_id.includes('OverloadFoo.Caller'));
const ovlCalls = C.filter(c => c.caller_method_id === caller.method_id && c.target_class.endsWith('OverloadFoo') && c.target_method === 'Save');
check('Attack A — Overload', ovl.length === 3 && ovlIds.size === 3 && ovlCalls.length === 3 && new Set(ovlCalls.map(c => c.target_method_id)).size === 3,
  `registered=${ovl.length} ids=${[...ovlIds].join(' | ')} edges=${ovlCalls.length} distinctTargets=${new Set(ovlCalls.map(c => c.target_method_id)).size}`);

// ── Attack B: 同名不同类不混淆 ──
const bCalls = C.filter(c => c.caller_method_id.includes('SameNameCrossClass.Caller') && c.target_method === 'Save');
const bTargets = bCalls.map(c => c.target_class).sort();
check('Attack B — Same-name cross-class', bTargets.length === 2 && bTargets[0].endsWith('Alpha') && bTargets[1].endsWith('Beta'),
  `targets=${bTargets.join(' , ')}`);

// ── Attack C: Extension 解析到定义 Symbol ──
const extCalls = C.filter(c => c.target_method === 'ToListAsync');
const cOk = extCalls.length >= 1 && extCalls.every(c => c.dispatch_kind === 'Extension' && c.resolution_status === 'Resolved' && c.target_class.endsWith('QueryExtensions') && c.target_method_id.includes('QueryExtensions'));
check('Attack C — Extension method', cOk,
  JSON.stringify(extCalls.map(c => `${c.target_method_id} [${c.dispatch_kind}/${c.resolution_status}]`)));

// ── Attack D: Interface 调用不指认实现 ──
const dCalls = C.filter(c => c.caller_method_id.includes('InterfaceConsumer.Remove') && c.target_method === 'DeleteFile');
const dOk = dCalls.length === 1 && dCalls[0].target_class.endsWith('IFileManager') && dCalls[0].dispatch_kind === 'Interface' && dCalls[0].resolution_status === 'Partial' && !/LocalFileManager|CloudFileManager/.test(JSON.stringify(dCalls[0]));
check('Attack D — Interface dispatch', dOk, JSON.stringify(dCalls));

// ── Attack E: 反向边多调用者 ──
const shared = M.find(m => m.containing_type.endsWith('SharedTarget') && m.name === 'BeCalled');
const cb = (shared?.called_by ?? []).slice().sort();
const eOk = cb.length === 2 && cb[0].includes('CallerOne.Go') && cb[1].includes('CallerTwo.Go');
const forward = C.filter(c => c.target_method_id === shared?.method_id).map(c => c.caller_method_id).sort();
check('Attack E — Reverse edge', eOk && JSON.stringify(forward) === JSON.stringify(cb),
  `calledBy=${cb.join(' , ')} forwardEdges=${forward.length} symmetric=${JSON.stringify(forward) === JSON.stringify(cb)}`);

// ── Attack F: 无法确定不猜 ──
const fDyn = C.filter(c => c.caller_method_id.includes('UnresolvedCaller.Dyn') && c.target_method === 'obj.MagicCall');
const fDel = C.filter(c => c.caller_method_id.includes('UnresolvedCaller.DelegateInvoke'));
const fOk = fDyn.length === 1 && fDyn[0].resolution_status === 'Unresolved' &&
  fDel.every(c => c.dispatch_kind === 'Delegate' ? c.resolution_status === 'Partial' : true) && fDel.length >= 1;
check('Attack F — Unresolved/Partial honesty', fOk,
  `dynamic=${JSON.stringify(fDyn.map(c => [c.dispatch_kind, c.resolution_status, c.target_method_id ?? null]))} delegate=${JSON.stringify(fDel.map(c => [c.dispatch_kind, c.resolution_status]))}`);

// ── Extra matrix checks (R1-06/08/10/11/13) ──
const gen = C.filter(c => c.target_method === 'Echo');
check('R1-06 — Generic normalized to definition', gen.length === 3 && new Set(gen.map(c => c.target_method_id)).size === 1 && gen[0].target_method_id.includes('Echo`1'),
  JSON.stringify(gen.map(c => c.target_method_id)));

const virt = C.filter(c => c.caller_method_id.includes('VirtualCaller.Drive') && c.target_method === 'Work');
check('R1-10 — Virtual not misresolved', virt.length === 1 && virt[0].dispatch_kind === 'Virtual' && virt[0].resolution_status === 'Partial' && virt[0].target_class.endsWith('BaseWorker'),
  JSON.stringify(virt.map(c => `${c.target_class} ${c.dispatch_kind}/${c.resolution_status}`)));

const cross = C.filter(c => c.caller_method_id.includes('LeafService.Handle') && c.target_method === 'Persist');
const dmCalledBy = M.find(m => m.containing_type.endsWith('DataManager') && m.name === 'Persist')?.called_by ?? [];
check('R1-11/R1-12 — Cross-class + reverse symmetry', cross.length === 1 && dmCalledBy.some(x => x.includes('LeafService.Handle')),
  `edge=${cross.length} Persist.calledBy=${dmCalledBy.join(' , ')}`);

const lineOk = ovlCalls.every((c, i) => [18, 19, 20].includes(c.line));
check('R1-13 — Line evidence', lineOk, `Save call lines=${ovlCalls.map(c => c.line).join(',')}`);

let pass = 0;
for (const r of out) { console.log(`${r.pass ? 'PASS' : 'FAIL'}  ${r.name}\n      ${r.detail}`); if (r.pass) pass++; }
console.log(`\n${pass}/${out.length} ATTACK ASSERTIONS PASSED`);
process.exit(pass === out.length ? 0 : 1);
