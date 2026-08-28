// R1 Ground Truth verification — asserts §6 three real-JNPF cases against extracted samples
const path = require('path');
const base = 'd:/JNPF-v52/analysis/r1/samples';
const fs = require('fs');
const file = JSON.parse(fs.readFileSync(path.join(base, 'FileService.DownloadAll.json'), 'utf8'));
const sched = JSON.parse(fs.readFileSync(path.join(base, 'ScheduleService.Delete.json'), 'utf8'));
const order = JSON.parse(fs.readFileSync(path.join(base, 'OrderService.Save-Delete.json'), 'utf8'));

const out = [];
const check = (name, pass, detail) => out.push({ name, pass, detail });

// ── Case A: FileService.DownloadAll 跨类调用 ──
const dlA = file.methods.find(m => m.name === 'DownloadAll');
const dlCalls = file.calls.filter(c => c.caller_method_id === dlA?.method_id);
const copyFile = dlCalls.filter(c => c.target_method === 'CopyFile');
const aOk = !!dlA && dlCalls.length > 0 && copyFile.length >= 1 &&
  copyFile.every(c => c.target_class.endsWith('IFileManager') && c.dispatch_kind === 'Interface' && c.resolution_status === 'Partial' && c.line > 0 && !!c.file);
check('Case A — FileService.DownloadAll', aOk,
  `DownloadAll=${dlA?.method_id} totalCalls=${dlCalls.length} CopyFile edges=${copyFile.map(c => `${c.target_class}.${c.target_method} @L${c.line} [${c.dispatch_kind}/${c.resolution_status}]`).join(' ; ')}`);
// 反向：IFileManager.CopyFile 的 called_by 应含 DownloadAll
const cfMethod = file.methods.find(m => m.name === 'CopyFile' && m.containing_type.includes('IFileManager'));
check('Case A — reverse edge on IFileManager.CopyFile', !!cfMethod && cfMethod.called_by.some(x => x.includes('DownloadAll')),
  `calledBy=${cfMethod?.called_by?.join(' , ') ?? 'N/A'}`);
// 接口不得被指认成某个具体实现
check('Case A — no fake implementation attribution', !dlCalls.some(c => c.target_method === 'CopyFile' && /FileManager[^C]*$/.test(c.target_class) && !c.target_class.endsWith('IFileManager') && c.dispatch_kind === 'Interface'),
  `interface targets=${[...new Set(dlCalls.filter(c => c.dispatch_kind === 'Interface').map(c => c.target_class))].join(' , ')}`);

// ── Case B: ScheduleService.Delete 调用证据（Query/ToListAsync/foreach 内查询/ScheduleUser）──
const delB = sched.methods.filter(m => m.name === 'Delete').find(m => m.containing_type.endsWith('ScheduleService'));
const delBCalls = sched.calls.filter(c => c.caller_method_id === delB?.method_id);
const qTargets = delBCalls.filter(c => /Query|Queryable|First|ToList|Count|Any|Where/i.test(c.target_method));
const suTargets = delBCalls.filter(c => /ScheduleUser/i.test(JSON.stringify(c.target_class) + JSON.stringify(c.note) + JSON.stringify(c.target_method_id)));
const bOk = !!delB && delBCalls.length > 0 && qTargets.length > 0;
check('Case B — ScheduleService.Delete call evidence', bOk,
  `Delete=${delB?.method_id} totalCalls=${delBCalls.length} query-ish=${qTargets.length} [${[...new Set(qTargets.map(c => c.target_method))].slice(0, 12).join(',')}] distinctTargets=${[...new Set(delBCalls.map(c => c.target_method_id))].size}`);
// tolistasync 类调用（含扩展）
const tlB = delBCalls.filter(c => /ToListAsync|ToList/i.test(c.target_method));
check('Case B — ToList(Async) captured', tlB.length >= 1, JSON.stringify(tlB.map(c => `${c.target_class}.${c.target_method} [${c.dispatch_kind}/${c.resolution_status}]`)));
// foreach 内二次查询：仅证明调用证据存在（循环内 line 密集），不做 N+1 判断
const inLoop = delBCalls.filter(c => c.line >= 2).length;
check('Case B — nested call lines present (evidence only, no N+1 verdict)', inLoop > 0, `calls with line evidence=${inLoop}`);

// ── Case C: OrderService.Save / Delete 解析 _fileManager/_userManager/_repository ──
const svC = order.methods.find(m => m.name === 'Save' && m.containing_type.endsWith('OrderService'));
const delC = order.methods.find(m => m.name === 'Delete' && m.containing_type.endsWith('OrderService'));
const svCalls = order.calls.filter(c => c.caller_method_id === svC?.method_id);
const delCalls = order.calls.filter(c => c.caller_method_id === delC?.method_id);
const allC = [...svCalls, ...delCalls];
const fm = allC.filter(c => c.target_class.endsWith('IFileManager'));
const um = allC.filter(c => /IUserManager|UserManager/.test(c.target_class));
const repo = allC.filter(c => /ISqlSugarRepository|Repository/.test(c.target_class));
check('Case C — OrderService.Save/Delete cross-class', !!svC && !!delC && svCalls.length > 0 && delCalls.length > 0 && fm.length > 0 && um.length > 0 && repo.length > 0,
  `Save calls=${svCalls.length} Delete calls=${delCalls.length} IFileManager=${fm.length} UserManager=${um.length} Repository=${repo.length}`);
// 接口调用不得错指随机实现
const ifaceC = allC.filter(c => c.dispatch_kind === 'Interface');
check('Case C — interface not misattributed', ifaceC.every(c => c.target_class.startsWith('JNPF') && c.resolution_status === 'Partial') && ifaceC.length > 0,
  `iface edges=${ifaceC.length} targets=${[...new Set(ifaceC.map(c => c.target_class + '.' + c.target_method))].slice(0, 8).join(' | ')}`);

let pass = 0;
for (const r of out) { console.log(`${r.pass ? 'PASS' : 'FAIL'}  ${r.name}\n      ${r.detail}`); if (r.pass) pass++; }
console.log(`\n${pass}/${out.length} GROUND-TRUTH ASSERTIONS PASSED`);
process.exit(pass === out.length ? 0 : 1);
