#!/usr/bin/env node
/**
 * Hook 合规测试驱动器
 *
 * 用途：验证 .claude/hooks/ 下的每个 guard hook 能否正确拦截"故意违规的样本"。
 * 这是 Supreme Iron Law 的自我应用 —— 不口头声称 "hook 有效"，而是用 exit code 证明。
 *
 * 运行：node scripts/test-hooks.mjs
 *
 * 通过标准：每个 hook 的违规样本 → exit 2，正常样本 → exit 0。
 */
import { execFileSync } from 'child_process';
import { fileURLToPath } from 'url';
import { dirname, join } from 'path';
import { readFileSync, writeFileSync, existsSync } from 'fs';

const __dirname = dirname(fileURLToPath(import.meta.url));
const hooksDir = join(__dirname, '..', '.claude', 'hooks');
const repoRoot = join(__dirname, '..');
const workflowStatePath = join(repoRoot, '.claude', 'workflow-state.json');

let pass = 0, fail = 0;
const failures = [];

/**
 * 跑一个 hook，喂入模拟的 Claude Code stdin payload。
 * @returns exit code
 */
function runHook(hookName, payload) {
  const hookPath = join(hooksDir, hookName);
  try {
    execFileSync('node', [hookPath], {
      input: JSON.stringify(payload),
      encoding: 'utf-8',
      stdio: ['pipe', 'pipe', 'pipe'],
      timeout: 10000,
    });
    return 0;
  } catch (e) {
    // exit code 非 0 会抛异常
    return e.status ?? -1;
  }
}

/**
 * 单个测试用例
 */
function test(name, hookName, payload, expectExit, why) {
  const actual = runHook(hookName, payload);
  const ok = actual === expectExit;
  const tag = ok ? '✅ PASS' : '❌ FAIL';
  console.log(`${tag}  ${name}`);
  console.log(`        期望 exit=${expectExit}, 实际 exit=${actual}`);
  if (why) console.log(`        ${why}`);
  if (!ok) {
    fail++;
    failures.push(`${name} (期望 ${expectExit}, 实际 ${actual})`);
  } else {
    pass++;
  }
}

// ─── 构造 payload 的辅助函数 ────────────────────────────────
function writePayload(file_path, content) {
  return { tool_name: 'Write', tool_input: { file_path, content } };
}

function editPayload(file_path, new_string) {
  return { tool_name: 'Edit', tool_input: { file_path, new_string } };
}

function multiEditPayload(file_path, edits) {
  // edits: array of { old_string, new_string }
  return { tool_name: 'MultiEdit', tool_input: { file_path, edits } };
}

console.log('='.repeat(60));
console.log('JNPF Hook 合规测试');
console.log('='.repeat(60));

// ─── guard-write.mjs (R5) ────────────────────────────────
console.log('\n[guard-write.mjs — R5 模块边界]');

test(
  'R5-A: 写入 OA 禁用模块应 BLOCK',
  'guard-write.mjs',
  writePayload('backend/application/JNPF.OA.API.Entry/Controllers/Foo.cs', 'x'),
  2,
  'JNPF.OA.API.Entry 是禁用模块'
);

test(
  'R5-B: scaffold IoT 不存在模块应 BLOCK',
  'guard-write.mjs',
  writePayload('backend/modularity/iot/Services/FooService.cs', 'x'),
  2,
  'IoT 模块不存在'
);

test(
  'R5-C: scaffold MES 不存在模块应 BLOCK',
  'guard-write.mjs',
  writePayload('backend/application/JNPF.MES.API.Entry/Foo.cs', 'x'),
  2,
  'MES 模块不存在'
);

test(
  'R5-D: 写入合法 system 模块应放行',
  'guard-write.mjs',
  writePayload('backend/modularity/system/Services/UserService.cs', 'x'),
  0,
  'system 是合法模块'
);

// ─── guard-write.mjs (R8) ─────────────────────────────────────
console.log('\n[guard-write.mjs — R8 权限声明]');

test(
  'R8-A: IDynamicApiController 无权限属性应 BLOCK',
  'guard-write.mjs',
  writePayload('backend/modularity/system/Services/FooService.cs',
    'public class FooService : IDynamicApiController { }'),
  2,
  '控制器缺权限声明'
);

test(
  'R8-B: 带 [AllowAnonymous] 应放行',
  'guard-write.mjs',
  writePayload('backend/modularity/system/Services/FooService.cs',
    '[AllowAnonymous]\npublic class FooService : IDynamicApiController { }'),
  0,
  '已声明 AllowAnonymous'
);

test(
  'R8-C: MultiEdit 无权限属性应 BLOCK',
  'guard-write.mjs',
  multiEditPayload('backend/modularity/system/Services/FooService.cs', [
    { old_string: '// old', new_string: 'public class FooService : IDynamicApiController { }' },
  ]),
  2,
  'MultiEdit 新增 IDynamicApiController 缺权限声明'
);

// ─── guard-write.mjs (R7) ────────────────────────────
console.log('\n[guard-write.mjs — R7 SQL 注入]');

test(
  'R7-A: DROP TABLE 字符串插值应 BLOCK',
  'guard-write.mjs',
  writePayload('backend/Foo.cs',
    'var sql = $"DROP TABLE {tableName}";'),
  2,
  'DROP via string interpolation'
);

test(
  'R7-B: 参数化查询应放行',
  'guard-write.mjs',
  writePayload('backend/Foo.cs',
    'var p = new SqlSugarParameter("@id", id);'),
  0,
  '参数化安全'
);

test(
  'R7-C: MultiEdit DROP TABLE 应 BLOCK',
  'guard-write.mjs',
  multiEditPayload('backend/Foo.cs', [
    { old_string: '// old', new_string: 'var sql = $"DROP TABLE {tableName}";' },
  ]),
  2,
  'MultiEdit DROP via string interpolation'
);

// ─── guard-write.mjs (R4) ────────────────────────────
console.log('\n[guard-write.mjs — R4 多租户]');

test(
  'R4-A: DisableGlobalFilter(Tenant) 应 BLOCK',
  'guard-write.mjs',
  writePayload('backend/Foo.cs',
    'db.Queryable<User>().DisableGlobalFilter("TenantFilter").ToList();'),
  2,
  '显式禁用租户过滤器'
);

test(
  'R4-B: Updateable 无 Where 应 BLOCK',
  'guard-write.mjs',
  writePayload('backend/Foo.cs',
    'db.Updateable<User>(entity).ExecuteCommand();'),
  2,
  '更新无 Where = 全表跨租户修改'
);

test(
  'R4-C: Updateable 带 Where 应放行',
  'guard-write.mjs',
  writePayload('backend/Foo.cs',
    'db.Updateable<User>(entity).Where(x => x.Id == id).ExecuteCommand();'),
  0,
  '有 Where 限定范围'
);

test(
  'R4-D: r4-safe 豁免标记应放行',
  'guard-write.mjs',
  writePayload('backend/Foo.cs',
    'db.Updateable<User>(entity).ExecuteCommand(); // r4-safe: DBA 跨租户清理'),
  0,
  '显式豁免'
);

test(
  'R4-E: MultiEdit Updateable 无 Where 应 BLOCK',
  'guard-write.mjs',
  multiEditPayload('backend/Foo.cs', [
    { old_string: '// old', new_string: 'db.Updateable<User>(entity).ExecuteCommand();' },
  ]),
  2,
  'MultiEdit Updateable 无 Where = 跨租户修改'
);

// ─── guard-write.mjs (R6) ────────────────────────────
console.log('\n[guard-write.mjs — R6 前端泄漏]');

test(
  'R6-A: setInterval 无 clear 应 BLOCK',
  'guard-write.mjs',
  writePayload('jnpf-web-vue3/src/views/Foo.vue',
    '<script setup>\nsetInterval(() => {}, 1000);\n</script>'),
  2,
  'interval 无清理'
);

test(
  'R6-B: EventSource 无 retry cap 应 BLOCK',
  'guard-write.mjs',
  writePayload('jnpf-web-vue3/src/utils/sse.js',
    'const es = new EventSource(url);\nes.onclose = () => {};'),
  2,
  'EventSource 无重试上限'
);

test(
  'R6-C: setTimeout + onUnmounted 应放行',
  'guard-write.mjs',
  writePayload('jnpf-web-vue3/src/views/Foo.vue',
    '<script setup>\nconst t = setTimeout(()=>{}, 1000);\nonUnmounted(() => clearTimeout(t));\n</script>'),
  0,
  '定时器配对清理'
);

test(
  'R6-D: MultiEdit setInterval 无 clear 应 BLOCK',
  'guard-write.mjs',
  multiEditPayload('jnpf-web-vue3/src/views/Foo.vue', [
    { old_string: '// old', new_string: '<script setup>\nsetInterval(() => {}, 1000);\n</script>' },
  ]),
  2,
  'MultiEdit interval 无清理'
);

// ─── guard-write.mjs (基础守卫) ──────────────────────────────
console.log('\n[guard-write.mjs — 基础文件守卫]');

test(
  'GW-A: 写入 .env 应 BLOCK',
  'guard-write.mjs',
  writePayload('.env', 'SECRET=xxx'),
  2,
  '密钥文件受保护'
);

test(
  'GW-B: 写入普通源码应放行',
  'guard-write.mjs',
  writePayload('backend/Foo.cs', 'public class Foo {}'),
  0,
  '正常文件'
);

test(
  'GW-C: 清空 .cs 源文件应 BLOCK（扩展名匹配）',
  'guard-write.mjs',
  writePayload('jnpf-web-vue3/src/views/Foo.vue', ''),
  2,
  '扩展名 .vue 匹配，空内容 = BLOCK'
);

test(
  'GW-D: .cs 文件中硬编码密钥应 BLOCK',
  'guard-write.mjs',
  writePayload('backend/Foo.cs', 'string apiKey = "sk-abc123def4567890abcdef";'),
  2,
  '硬编码密钥 = L3 高危阻断'
);

test(
  'GW-E: .cs 中 MD5 哈希应 WARN（非阻断）',
  'guard-write.mjs',
  writePayload('backend/Foo.cs', 'var hash = MD5.ComputeHash(data);'),
  0,
  '弱加密 = L3 警告不阻断'
);

test(
  'GW-F: MultiEdit .env 写入应 BLOCK',
  'guard-write.mjs',
  multiEditPayload('.env', [
    { old_string: '# old', new_string: 'SECRET=new_value' },
  ]),
  2,
  'MultiEdit 写入 .env = L1 阻断'
);

// ─── guard-write.mjs (L10 需求分析子链铁律) ──────────────────
console.log('\n[guard-write.mjs — L10 需求分析子链铁律]');

test(
  'L10a-A: 新增 .mjs 脚本（非 hooks）应 BLOCK',
  'guard-write.mjs',
  writePayload('scripts/new-e2e-test.mjs', 'import { } from "node:assert";'),
  2,
  '禁令一：禁止新增 mjs 脚本（除 hooks 目录）'
);

test(
  'L10a-B: 新增 .claude/hooks/*.mjs 应放行',
  'guard-write.mjs',
  writePayload('.claude/hooks/new-guard.mjs', '#!/usr/bin/env node\n// hook infra'),
  0,
  'hooks 目录是基础设施，允许 mjs'
);

test(
  'L10b-A: 复活 ScannerValidator 应 BLOCK',
  'guard-write.mjs',
  writePayload('backend/modularity/inteAssistant/JNPF.InteAssistant/Gates/Foo.cs',
    'public class ScannerValidator { }'),
  2,
  '禁令七：ScannerValidator 已废止（25 §0.2）'
);

test(
  'L10b-B: 新建 sa_ddd 表应 BLOCK',
  'guard-write.mjs',
  writePayload('backend/modularity/inteAssistant/Migrations/foo.sql',
    'CREATE TABLE sa_ddd_context (F_Id NVARCHAR(50));'),
  2,
  '禁令七：sa_ddd_* 表已废止（25 决策5/红线5）'
);

test(
  'L10b-C: cascadeUpdate 调用应 BLOCK',
  'guard-write.mjs',
  writePayload('backend/modularity/inteAssistant/JNPF.InteAssistant/Sa/Foo.cs',
    'cascadeUpdate(affectedSteps);'),
  2,
  '禁令七：cascadeUpdate 已废止（25 决策7）'
);

test(
  'L10d-A: 编排器 _llm.ChatAsync 出题应 BLOCK',
  'guard-write.mjs',
  writePayload('backend/modularity/inteAssistant/JNPF.InteAssistant/Skills/RequirementAnalysisOrchestrator.cs',
    'var resp = await _llm.ChatAsync(new { prompt = "generate clarification question" });'),
  2,
  '禁令一/七：编排器禁止直连 LLM 出题（25 红线9）'
);

test(
  'L10-safe: cr-safe 豁免标记应放行废止模块',
  'guard-write.mjs',
  writePayload('backend/modularity/inteAssistant/JNPF.InteAssistant/Gates/Foo.cs',
    'public class ScannerValidator { } // cr-safe: 临时调试历史模块对照'),
  0,
  'cr-safe 豁免（需配合 CR 审批留痕）'
);

// ─── guard-bash.mjs (危险命令) ───────────────────────────────
console.log('\n[guard-bash.mjs — 危险命令拦截]');

function bashPayload(cmd) {
  return { tool_name: 'Bash', tool_input: { command: cmd } };
}

test(
  'GB-A: rm -rf 应 BLOCK',
  'guard-bash.mjs',
  bashPayload('rm -rf /'),
  2,
  '删除根目录'
);

test(
  'GB-B: git push --force 应 BLOCK',
  'guard-bash.mjs',
  bashPayload('git push --force origin main'),
  2,
  '强推覆盖远程'
);

test(
  'GB-C: 正常 git status 应放行',
  'guard-bash.mjs',
  bashPayload('git status'),
  0,
  '安全命令'
);

// ─── guard-write.mjs (L11 零占位符) ─────────────────────────
console.log('\n[guard-write.mjs — L11 零占位符]');

test(
  'L11-A: TODO implement 应 BLOCK',
  'guard-write.mjs',
  writePayload(
    'backend/modularity/inteAssistant/JNPF.InteAssistant/Skills/FooSkillService.cs',
    'public void Run() {\n  // TODO: implement this\n}\n'
  ),
  2,
  '业务源码含 TODO implement'
);

test(
  'L11-B: NotImplementedException 应 BLOCK',
  'guard-write.mjs',
  writePayload(
    'backend/modularity/inteAssistant/JNPF.InteAssistant/Gates/FooGate.cs',
    'public void X() { throw new NotImplementedException(); }\n'
  ),
  2,
  '业务源码抛 NotImplementedException'
);

test(
  'L11-C: placeholder-ok 豁免应放行',
  'guard-write.mjs',
  writePayload(
    'backend/modularity/inteAssistant/JNPF.InteAssistant/Skills/FooSkillService.cs',
    '// placeholder-ok: scaffold for codegen demo only\npublic void Run() { throw new NotImplementedException(); }\n'
  ),
  0,
  '含 placeholder-ok 豁免'
);

test(
  'L11-D: 正常实现应放行',
  'guard-write.mjs',
  writePayload(
    'backend/modularity/inteAssistant/JNPF.InteAssistant/Skills/FooSkillService.cs',
    'public int Add(int a, int b) => a + b;\n'
  ),
  0,
  '无占位符'
);

test(
  'L11-E: docs 路径不扫描',
  'guard-write.mjs',
  writePayload(
    'docs/example.md',
    '// TODO: implement this\n'
  ),
  0,
  '文档不在扫描范围'
);

// ─── guard-write.mjs (L12 ADF 写入锁) ─────────────────────────
console.log('\n[guard-write.mjs — L12 ADF 写入锁]');

const workflowStateBackup = existsSync(workflowStatePath)
  ? readFileSync(workflowStatePath, 'utf8')
  : null;

function withAdfPhase(phase, fn) {
  const base = workflowStateBackup ? JSON.parse(workflowStateBackup) : {};
  writeFileSync(
    workflowStatePath,
    JSON.stringify({ ...base, adfGateEnabled: true, adfPhase: phase, currentSg: null }, null, 2),
  );
  try { fn(); } finally {
    if (workflowStateBackup != null) writeFileSync(workflowStatePath, workflowStateBackup);
  }
}

withAdfPhase('P1', () => {
  test(
    'L12-A: adfPhase=P1 写业务 .cs 应 BLOCK',
    'guard-write.mjs',
    writePayload(
      'backend/modularity/inteAssistant/JNPF.InteAssistant/Skills/FooSkillService.cs',
      'public int Add(int a, int b) => a + b;\n'
    ),
    2,
    'P1 锁定业务源码'
  );
});

withAdfPhase('P1', () => {
  test(
    'L12-B: adfPhase=P1 写 docs 应放行',
    'guard-write.mjs',
    writePayload('docs/adf-note.md', '# architecture draft\n'),
    0,
    '锁定期允许文档'
  );
});

withAdfPhase('P4', () => {
  test(
    'L12-C: adfPhase=P4 写业务 .cs 应放行',
    'guard-write.mjs',
    writePayload(
      'backend/modularity/inteAssistant/JNPF.InteAssistant/Skills/FooSkillService.cs',
      'public int Add(int a, int b) => a + b;\n'
    ),
    0,
    'P4 已批准实现'
  );
});

withAdfPhase(null, () => {
  test(
    'L12-D: adfPhase=null 日常应放行',
    'guard-write.mjs',
    writePayload(
      'backend/modularity/inteAssistant/JNPF.InteAssistant/Skills/FooSkillService.cs',
      'public int Add(int a, int b) => a + b;\n'
    ),
    0,
    '日常不锁'
  );
});

// ─── 汇总 ────────────────────────────────────────────────────
console.log('\n' + '='.repeat(60));
console.log(`汇总: ${pass} PASS / ${fail} FAIL / 共 ${pass + fail} 项`);
console.log('='.repeat(60));

if (fail > 0) {
  console.log('\n失败项:');
  failures.forEach(f => console.log(`  - ${f}`));
  process.exit(1);
}
console.log('\n✅ 全部 hook 拦截能力验证通过。');
process.exit(0);
