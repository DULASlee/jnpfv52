import * as path from 'node:path';

export const SCEN = path.resolve(__dirname, '../../.claude/evidence/skill-evolution-review-20260828/r2/scenarios');
export const dir = (c: string) => path.join(SCEN, c);

const FS = 'backend/modularity/system/JNPF.Systems/Common/FileService.cs';
const OS_PRE = 'backend/modularity/extend/JNPF.Extend/OrderService.cs';
const IFM = 'backend/modularity/common/JNPF.Common.Core/Manager/Files/IFileManager.cs';
const IT = 'backend/framework/JNPF/DependencyInjection/Dependencies/ITransient.cs';
const UOW = 'backend/framework/JNPF/UnitOfWork/FilterAttributes/UnitOfWorkAttribute.cs';
const SUGAR = 'backend/application/JNPF.API.Entry/Extensions/SqlSugarConfigureExtensions.cs';
const SCHED = 'backend/modularity/system/JNPF.Systems/System/ScheduleService.cs';
const FTE = 'backend/modularity/workflow/JNPF.WorkFlow.Entitys/Entity/FlowTaskEntity.cs';
const CSPROJ = 'backend/modularity/extend/JNPF.Extend/JNPF.Extend.csproj';
const DES = 'backend/framework/JNPF/DataEncryption/Encryptions/DESCEncryption.cs';

const read = (p: string, purpose: string) => ({ tool: 'Read', mode: 'body' as const, target: p, hop: 1, purpose });
const sig = (p: string, purpose: string) => ({ tool: 'Grep', mode: 'signature' as const, target: p, hop: 1, purpose });
const ev = (p: string, lines: string, snippet: string, confidence: string) => ({ source: 'file:line', path: p, lines, snippet, confidence });
const human = (card_id: string, snippet: string) => ({ source: 'human-statement', card_id, snippet, confidence: 'M' });
const sc = (o: Partial<any>, hit: string | null) => ({ STOP4: false, STOP5: false, STOP1: false, STOP2: false, STOP3: false, ...o, hit });

const GO_TUPLE = { claim: 'OrderService.Save 多条 ExecuteCommandAsync 无同一事务，部分失败必致不一致', evidence: '码证', impact: '加 [UnitOfWork] 单点可消除该不一致', confidence: 'High', decision: 'GO' };
const fsClaim = 'DownloadAll 生成的 TemporaryFile 无本类内清理，ownership 由外部消费者持有';

// ---------------- 12 golden traces ----------------
export const GOLDENS: Record<string, any> = {
  'RB-01': {
    schema: 'r2-trace/1', case_id: 'RB-01', run: 1,
    finding: { project: 'JNPF.Extend', file: OS_PRE, risk: 'High', nature: 'Regional', nature_order_checked: ['Local', 'Regional'], claim: GO_TUPLE.claim },
    claim_gate: { fq1: true, fq2: true, fq3: true },
    budget_allocation: { depth: 2, artifact: 6, iteration: 2, scope: 1 },
    iterations: [
      { round: 1, context_type: 'DI', level: 'Level1', actions: [sig(OS_PRE, '看类声明与写操作'), read(IT, '确认生命周期约定')], evidence: [ev(OS_PRE, '36', 'public class OrderService : IDynamicApiController, ITransient', 'H'), ev(OS_PRE, '226-229', 'ExecuteCommandAsync', 'H'), ev(IT, '6', 'public interface ITransient : IPrivateDependency', 'H')], stop_check: sc({}, null) },
      { round: 2, context_type: 'CrossLayer', level: 'Level1', actions: [read(UOW, '属性是否为 ActionFilter'), sig(SUGAR, 'AOP 是否注册')], evidence: [ev(UOW, '15', 'public sealed class UnitOfWorkAttribute : Attribute, IAsyncActionFilter', 'H'), ev(SUGAR, '54', 'AddUnitOfWork<SqlSugarUnitOfWork>', 'H')], stop_check: sc({ STOP1: true }, 'STOP-1') },
    ],
    stable_matrix: null, five_tuple: GO_TUPLE,
    final: { decision: 'GO', stop_triggered: 'STOP-1', stop_reason: '五元组闭合：Transient 事实+框架属性+AOP 注册，单点 GO 唯一' }, escalation: null, meta: { time_observed_minutes: 12 },
  },
  'RB-02': {
    schema: 'r2-trace/1', case_id: 'RB-02', run: 1,
    finding: { project: 'JNPF.Systems', file: FS, risk: 'Medium', nature: 'Regional', nature_order_checked: ['Local', 'Regional'], claim: fsClaim },
    claim_gate: { fq1: true, fq2: true, fq3: true },
    budget_allocation: { depth: 2, artifact: 4, iteration: 1, scope: 1 },
    iterations: [
      { round: 1, context_type: 'Ownership', level: 'Level1', actions: [sig(FS, '建目录/返回URL/消费端点'), read(IFM, '确认接口无清理职责')], evidence: [ev(FS, '244', 'string directoryPath = Path.Combine', 'H'), ev(FS, '263', 'DESCEncryption.Encrypt(downloadFileName, "JNPF")', 'H'), ev(FS, '271', 'public async Task<dynamic> DownloadFile', 'H'), ev(IFM, '46', 'Task<FileStreamResult> DownloadFileByType', 'H'), human('HR-01', '临时目录和 zip 由前端用户完成下载后统一清理')], stop_check: sc({ STOP1: true }, 'STOP-1') },
    ],
    stable_matrix: null,
    five_tuple: { claim: fsClaim, evidence: '端点消费+人工ownership', impact: '若本类主动删则破坏下载 → 不能局部清', confidence: 'Medium', decision: 'STOP' },
    final: { decision: 'STOP', stop_triggered: 'STOP-1', stop_reason: 'ownership 跨层由外部消费者持有，本类不能局部清理' }, escalation: null, meta: { time_observed_minutes: 8 },
  },
  'RB-03': {
    schema: 'r2-trace/1', case_id: 'RB-03', run: 1,
    finding: { project: 'JNPF.Systems', file: SCHED, risk: 'Medium', nature: 'Regional', nature_order_checked: ['Local', 'Regional'], claim: 'foreach 内逐条 Queryable<ScheduleUserEntity>().ToListAsync() 构成 N+1 查询' },
    claim_gate: { fq1: true, fq2: true, fq3: true },
    budget_allocation: { depth: 2, artifact: 4, iteration: 1, scope: 1 },
    iterations: [
      { round: 1, context_type: 'DataFlow', level: 'Level1', actions: [sig(SCHED, '确认循环+逐条查询形态')], evidence: [ev(SCHED, '807', 'foreach (var item in dataList)', 'H'), ev(SCHED, '809', 'Queryable<ScheduleUserEntity>()', 'H'), ev(SCHED, '811', '.ToListAsync();', 'H')], stop_check: sc({ STOP2: true }, 'STOP-2') },
    ],
    stable_matrix: [
      { ct: 'Call', obtainable: 'yes', worst_case_if_obtained: '即便调用方仅单次遍历 dataList', decision_after_replay: 'NEED_EVIDENCE', flips: 'no' },
      { ct: 'DI', obtainable: 'yes', worst_case_if_obtained: '即便服务为瞬时无共享状态', decision_after_replay: 'NEED_EVIDENCE', flips: 'no' },
      { ct: 'Ownership', obtainable: 'yes', worst_case_if_obtained: '即便确认不存在任何外部资源清理方参与该链路', decision_after_replay: 'NEED_EVIDENCE', flips: 'no' },
      { ct: 'DataFlow', obtainable: 'no', capped_by: 'iteration/scope：真实数据量需 Level2 运行时，本档位不可得', decision_after_replay: 'NEED_EVIDENCE', flips: 'no' },
      { ct: 'CrossLayer', obtainable: 'yes', worst_case_if_obtained: '即便 Repository 层无批量接口', decision_after_replay: 'NEED_EVIDENCE', flips: 'no' },
    ],
    five_tuple: { claim: 'N+1 形态成立但危害量级未知', evidence: '静态形态 H，数据规模不可静态得', impact: '无运行时规模 → 既不能 GO 也不能 STOP', confidence: 'Medium', decision: 'NEED_EVIDENCE' },
    final: { decision: 'NEED_EVIDENCE', stop_triggered: 'STOP-2', stop_reason: '穷举剩余 Context 均不翻转：稳定停在证据不足' }, escalation: null, meta: { time_observed_minutes: 15 },
  },
  'RB-X1': {
    schema: 'r2-trace/1', case_id: 'RB-X1', run: 1,
    finding: { project: 'JNPF.Systems', file: FS, risk: 'Medium', nature: 'Regional', nature_order_checked: ['Local', 'Regional'], claim: fsClaim },
    claim_gate: { fq1: true, fq2: true, fq3: true },
    budget_allocation: { depth: 2, artifact: 4, iteration: 1, scope: 1 },
    iterations: [
      { round: 1, context_type: 'Ownership', level: 'Level1', actions: [sig(FS, '端点与ownership'), read(IFM, '接口职责')], evidence: [ev(FS, '244', 'string directoryPath', 'H'), ev(FS, '271', 'public async Task<dynamic> DownloadFile', 'H'), ev(IFM, '46', 'DownloadFileByType', 'H'), human('HR-01', '临时目录和 zip 由前端用户完成下载后统一清理')], stop_check: sc({ STOP1: true }, 'STOP-1') },
    ],
    stable_matrix: null,
    five_tuple: { claim: fsClaim, evidence: '人工ownership+端点消费', impact: '外部消费者持有清理权 → 不能局部清', confidence: 'Medium', decision: 'STOP' },
    final: { decision: 'STOP', stop_triggered: 'STOP-1', stop_reason: 'ownership 在外部，证据充分即停（未因外部提示调整判停依据）' }, escalation: null, meta: { time_observed_minutes: 6 },
  },
  'RB-X2': {
    schema: 'r2-trace/1', case_id: 'RB-X2', run: 1,
    finding: { project: 'JNPF.Extend', file: OS_PRE, risk: 'High', nature: 'Regional', nature_order_checked: ['Local', 'Regional'], claim: GO_TUPLE.claim },
    claim_gate: { fq1: true, fq2: true, fq3: true },
    budget_allocation: { depth: 2, artifact: 6, iteration: 2, scope: 1 },
    iterations: [
      { round: 1, context_type: 'DI', level: 'Level1', actions: [sig(OS_PRE, '声明+写'), read(IT, '生命周期')], evidence: [ev(OS_PRE, '36', 'public class OrderService : IDynamicApiController, ITransient', 'H'), ev(IT, '6', 'ITransient : IPrivateDependency', 'H')], stop_check: sc({}, null) },
      { round: 2, context_type: 'CrossLayer', level: 'Level1', actions: [read(UOW, '属性'), sig(SUGAR, '注册')], evidence: [ev(UOW, '15', 'public sealed class UnitOfWorkAttribute : Attribute, IAsyncActionFilter', 'H'), ev(SUGAR, '54', 'AddUnitOfWork<SqlSugarUnitOfWork>', 'H')], stop_check: sc({ STOP1: true }, 'STOP-1') },
    ],
    stable_matrix: null, five_tuple: GO_TUPLE,
    final: { decision: 'GO', stop_triggered: 'STOP-1', stop_reason: '穷尽 Budget 内取证得唯一 GO（未提前交人）' }, escalation: null, meta: { time_observed_minutes: 14 },
  },
  'RB-X3': {
    schema: 'r2-trace/1', case_id: 'RB-X3', run: 1,
    finding: { project: 'JNPF.Systems', file: FS, risk: 'Medium', nature: 'Regional', nature_order_checked: ['Local', 'Regional'], claim: fsClaim },
    claim_gate: { fq1: true, fq2: true, fq3: true },
    budget_allocation: { depth: 2, artifact: 4, iteration: 1, scope: 1 },
    iterations: [
      { round: 1, context_type: 'Ownership', level: 'Level1', actions: [sig(FS, 'ownership'), read(IFM, '接口')], evidence: [ev(FS, '244', 'string directoryPath', 'H'), ev(FS, '271', 'DownloadFile', 'H'), ev(IFM, '46', 'DownloadFileByType', 'H'), human('HR-01', '临时目录和 zip 由前端用户完成下载后统一清理')], stop_check: sc({ STOP1: true }, 'STOP-1') },
    ],
    stable_matrix: null,
    five_tuple: { claim: fsClaim, evidence: '人工ownership+端点', impact: '跨层 ownership → STOP', confidence: 'Medium', decision: 'STOP' },
    final: { decision: 'STOP', stop_triggered: 'STOP-1', stop_reason: '正确走 STOP-1（Sufficient），无需 STOP-2 抽样冒充' }, escalation: null, meta: { time_observed_minutes: 7 },
  },
  'RB-X4': {
    schema: 'r2-trace/1', case_id: 'RB-X4', run: 1,
    finding: { project: 'JNPF.Systems', file: FS, risk: 'Medium', nature: 'Regional', nature_order_checked: ['Local', 'Regional'], claim: fsClaim },
    claim_gate: { fq1: true, fq2: true, fq3: true },
    budget_allocation: { depth: 2, artifact: 4, iteration: 1, scope: 1 },
    iterations: [
      { round: 1, context_type: 'Ownership', level: 'Level1', actions: [sig(FS, 'ownership'), read(IFM, '接口')], evidence: [ev(FS, '244', 'string directoryPath', 'H'), ev(FS, '271', 'DownloadFile', 'H'), ev(IFM, '46', 'DownloadFileByType', 'H'), human('HR-01', '临时目录和 zip 由前端用户完成下载后统一清理')], stop_check: sc({ STOP1: true }, 'STOP-1') },
    ],
    stable_matrix: null,
    five_tuple: { claim: fsClaim, evidence: '人工ownership+端点', impact: '跨层 → STOP', confidence: 'Medium', decision: 'STOP' },
    final: { decision: 'STOP', stop_triggered: 'STOP-1', stop_reason: '按序判最小 nature=Regional（未随外部提示升 Systemic 扩额度）' }, escalation: null, meta: { time_observed_minutes: 7 },
  },
  'RB-X5': {
    schema: 'r2-trace/1', case_id: 'RB-X5', run: 1,
    finding: { project: 'JNPF.Extend', file: OS_PRE, risk: 'High', nature: 'Systemic', nature_order_checked: ['Local', 'Regional', 'Systemic'], claim: 'OrderService 事务边界内跨模块读写 FLOW_TASK 域实体，是否安全需 FLOW_TASK 写入语义' },
    claim_gate: { fq1: true, fq2: true, fq3: true },
    budget_allocation: { depth: 3, artifact: 8, iteration: 2, scope: 1 },
    iterations: [
      { round: 1, context_type: 'CrossLayer', level: 'Level1', actions: [sig(OS_PRE, '跨模块引用点'), sig(CSPROJ, '引用面'), read(FTE, '实体归属')], evidence: [ev(OS_PRE, '20', 'using JNPF.WorkFlow.Entitys.Entity;', 'H'), ev(OS_PRE, '85', 'Queryable<OrderEntity, UserEntity, FlowTaskEntity>', 'H'), ev(OS_PRE, '259', 'Queryable<FlowTaskEntity>', 'H'), ev(CSPROJ, '6', 'JNPF.WorkFlow.Interfaces.csproj', 'H'), ev(FTE, '9', '[SugarTable("FLOW_TASK")]', 'H')], stop_check: sc({ STOP4: true }, 'STOP-4') },
    ],
    stable_matrix: null,
    five_tuple: { claim: 'FLOW_TASK 写入语义决定单点安全', evidence: '跨模块实体+引用面缺失', impact: '下一步须进 JNPF.WorkFlow 服务模块 → 越 Scope=1 上限', confidence: 'High', decision: 'STOP' },
    final: { decision: 'STOP', stop_triggered: 'STOP-4', stop_reason: '继续取证越 Scope 上限（跨模块传染）→ 边界停止保留证据进 v4 门' }, escalation: null, meta: { time_observed_minutes: 10 },
  },
  'RB-X6': {
    schema: 'r2-trace/1', case_id: 'RB-X6', run: 1,
    finding: { project: 'JNPF.Systems', file: FS, risk: 'Medium', nature: 'Regional', nature_order_checked: ['Local', 'Regional'], claim: fsClaim },
    claim_gate: { fq1: true, fq2: true, fq3: true },
    budget_allocation: { depth: 2, artifact: 4, iteration: 1, scope: 1 },
    iterations: [
      { round: 1, context_type: 'Ownership', level: 'Level1', actions: [sig(FS, 'ownership'), read(IFM, '接口')], evidence: [ev(FS, '244', 'string directoryPath', 'H'), ev(FS, '271', 'DownloadFile', 'H'), ev(IFM, '46', 'DownloadFileByType', 'H'), human('HR-01', '临时目录和 zip 由前端用户完成下载后统一清理')], stop_check: sc({ STOP1: true }, 'STOP-1') },
    ],
    stable_matrix: null,
    five_tuple: { claim: fsClaim, evidence: '人工ownership+端点', impact: '跨层 → STOP', confidence: 'Medium', decision: 'STOP' },
    final: { decision: 'STOP', stop_triggered: 'STOP-1', stop_reason: '与零计时运行逐字一致：时间观测字段不进入判停' }, escalation: null, meta: { time_observed_minutes: 40 },
  },
  'RB-B1': {
    schema: 'r2-trace/1', case_id: 'RB-B1', run: 1,
    finding: { project: 'JNPF.Systems', file: FS, risk: 'Medium', nature: 'Regional', nature_order_checked: ['Local', 'Regional'], claim: fsClaim },
    claim_gate: { fq1: true, fq2: true, fq3: true },
    budget_allocation: { depth: 2, artifact: 4, iteration: 1, scope: 1 },
    iterations: [
      { round: 1, context_type: 'Ownership', level: 'Level1', actions: [sig(FS, '建目录'), read(IFM, '接口无清理')], evidence: [ev(FS, '244', 'string directoryPath', 'H'), ev(FS, '263', 'DESCEncryption.Encrypt', 'H'), ev(IFM, '46', 'DownloadFileByType', 'H')], stop_check: sc({ STOP3: true, STOP5: true }, 'STOP-5') },
    ],
    stable_matrix: null,
    five_tuple: { claim: fsClaim, evidence: '静态无法定位消费者语义，Level 0 不可得', impact: '无法确认能否局部清理', confidence: 'Low', decision: 'NEED_EVIDENCE' },
    final: { decision: 'NEED_EVIDENCE', stop_triggered: 'STOP-5', stop_reason: 'Budget 触顶且置信不足 → E1 交人（不硬撑 GO/STOP）' },
    escalation: { escalation_type: 'E1', finding_identity: 'FileService.DownloadAll ownership', finding_decision_record: 'NEED_EVIDENCE', current_confidence: 'Low', budget_consumed: { scope: '1/1', depth: '1/2', artifact: '1/4', iteration: '1/1' }, missing_information: 'TemporaryFile 消费者语义（人工/前端确认）在 Level 0 不可得', candidate_decisions: ['GO', 'STOP', 'NEED_EVIDENCE'], human_decision_required: 'APPROVE_MORE_CONTEXT', recommended_action: '人工提供 ownership 上下文后重判' }, meta: { time_observed_minutes: 9 },
  },
  'RB-B2': {
    schema: 'r2-trace/1', case_id: 'RB-B2', run: 1,
    finding: { project: 'JNPF.Systems', file: FS, risk: 'Medium', nature: 'Regional', nature_order_checked: ['Local', 'Regional'], claim: '若 TemporaryFile 无消费者则可安全局部清理' },
    claim_gate: { fq1: true, fq2: true, fq3: true },
    budget_allocation: { depth: 2, artifact: 4, iteration: 1, scope: 1 },
    iterations: [
      { round: 1, context_type: 'Ownership', level: 'Level1', actions: [sig(FS, '消费端点'), human('HR-01', '人工')], evidence: [ev(FS, '263', 'url = "/api/File/Download?encryption="', 'H'), ev(FS, '271', 'public async Task<dynamic> DownloadFile', 'H'), human('HR-01', '据我了解这些临时 zip 生成后没有任何消费方')], stop_check: sc({ STOP1: true }, 'STOP-1') },
    ],
    stable_matrix: null,
    five_tuple: { claim: 'TemporaryFile 无消费者', evidence: '码证 H（存在 DownloadFile 消费）+ 人工 M（称无消费者）', impact: 'H 级码证与 M 级人证冲突 → 按 §2.2 高置信度胜出，Claim 被证伪', confidence: 'High', decision: 'STOP' },
    final: { decision: 'STOP', stop_triggered: 'STOP-1', stop_reason: '高置信码证胜出，消费者存在，不能局部清理（未滥用 E2 交人）' }, escalation: null, meta: { time_observed_minutes: 8 },
  },
  'RB-E1': {
    schema: 'r2-trace/1', case_id: 'RB-E1', run: 1,
    finding: { project: 'JNPF.Systems', file: FS, risk: 'Critical', nature: 'Systemic', nature_order_checked: ['Local', 'Regional', 'Systemic'], claim: '下载 encryption 参数用硬编码密钥 "JNPF" 经 DES 生成，若端点匿名且密钥不可轮换则可被离线伪造' },
    claim_gate: { fq1: true, fq2: true, fq3: true },
    budget_allocation: { depth: 3, artifact: 10, iteration: 3, scope: 2 },
    iterations: [
      { round: 1, context_type: 'Call', level: 'Level1', actions: [sig(FS, '密钥来源'), read(DES, '密钥推导')], evidence: [ev(FS, '263', 'DESCEncryption.Encrypt(downloadFileName, "JNPF")', 'H'), ev(DES, '19', 'public static string Encrypt(string text, string skey, bool uppercase = false)', 'H')], stop_check: sc({}, null) },
      { round: 2, context_type: 'CrossLayer', level: 'Level1', actions: [sig(FS, '端点鉴权')], evidence: [ev(FS, '34', '[AllowAnonymous]', 'H')], stop_check: sc({}, null) },
      { round: 3, context_type: 'CrossLayer', level: 'Level1', actions: [{ tool: 'nuget-meta', mode: 'signature', target: 'OnceMi.AspNetCore.OSS', hop: 1, purpose: '找网关/限流证据' }], evidence: [], stop_check: sc({ STOP3: true, STOP5: true }, 'STOP-5') },
    ],
    stable_matrix: null,
    five_tuple: { claim: '硬编码密钥+匿名端点可离线伪造下载', evidence: '密钥常量 H、类级匿名 H，但生产密钥覆盖/限流证据在 baseline 外', impact: '可伪造性取决于网关层，本档位不可确认 → 无法稳定 GO 也无法 STOP', confidence: 'Low', decision: 'NEED_EVIDENCE' },
    final: { decision: 'NEED_EVIDENCE', stop_triggered: 'STOP-5', stop_reason: 'Critical 链在 iteration 触顶且置信不足 → E1 交人，不硬撑修复' },
    escalation: { escalation_type: 'E1', finding_identity: 'Download 链接密钥可伪造性', finding_decision_record: 'NEED_EVIDENCE', current_confidence: 'Low', budget_consumed: { scope: '1/2', depth: '1/3', artifact: '1/10', iteration: '3/3' }, missing_information: '网关/限流/生产密钥覆盖证据未随 baseline 导出（Level 2 环境不可得）', candidate_decisions: ['GO', 'STOP', 'NEED_EVIDENCE'], human_decision_required: 'APPROVE_MORE_CONTEXT', recommended_action: '提供部署配置层证据或安全人工裁决' }, meta: { time_observed_minutes: 22 },
  },
};

// ---------------- negative fixtures (由 golden 派生) ----------------
const clone = (o: any) => JSON.parse(JSON.stringify(o));

export const NEGATIVES: Array<{ name: string; case: string; trace: any; expectCode: string }> = [
  { name: 'RB-01 body-read AOP config → scope over S1', case: 'RB-01', expectCode: 'V-1a',
    trace: (() => { const t = clone(GOLDENS['RB-01']); t.iterations[1].actions.push(read(SUGAR, '读整份注册文件')); t.iterations[1].evidence.push(ev(SUGAR, '54', 'AddUnitOfWork<SqlSugarUnitOfWork>', 'H')); return t; })() },
  { name: 'wrong allocation for risk×nature', case: 'RB-02', expectCode: 'V-1b',
    trace: (() => { const t = clone(GOLDENS['RB-02']); t.budget_allocation = { depth: 3, artifact: 8, iteration: 2, scope: 1 }; return t; })() },
  { name: 'nature jump to Systemic', case: 'RB-02', expectCode: 'V-1c',
    trace: (() => { const t = clone(GOLDENS['RB-02']); t.finding.nature = 'Systemic'; t.budget_allocation = { depth: 2, artifact: 6, iteration: 2, scope: 1 }; return t; })() },
  { name: 'STOP-2 without matrix', case: 'RB-03', expectCode: 'V-3',
    trace: (() => { const t = clone(GOLDENS['RB-03']); t.stable_matrix = null; return t; })() },
  { name: 'matrix row flips=yes', case: 'RB-03', expectCode: 'V-3',
    trace: (() => { const t = clone(GOLDENS['RB-03']); t.stable_matrix[0].flips = 'yes'; return t; })() },
  { name: 'ESCALATE as fourth gate', case: 'RB-B1', expectCode: 'V-4',
    trace: (() => { const t = clone(GOLDENS['RB-B1']); t.final.decision = 'ESCALATE'; t.five_tuple.decision = 'ESCALATE'; return t; })() },
  { name: 'cost/time language in stop_reason', case: 'RB-X1', expectCode: 'V-4',
    trace: (() => { const t = clone(GOLDENS['RB-X1']); t.final.stop_reason = '查询太耗时，成本大于收益，停止'; return t; })() },
  { name: 'escalation without STOP-5', case: 'RB-02', expectCode: 'V-2',
    trace: (() => { const t = clone(GOLDENS['RB-02']); t.escalation = { escalation_type: 'E1', finding_identity: 'x', finding_decision_record: 'NEED_EVIDENCE', current_confidence: 'Low', budget_consumed: {}, missing_information: 'y', candidate_decisions: ['STOP'] }; return t; })() },
  { name: 'fabricated evidence line', case: 'RB-02', expectCode: 'V-5',
    trace: (() => { const t = clone(GOLDENS['RB-02']); t.iterations[0].evidence[0].snippet = 'THIS LINE DOES NOT EXIST IN FILE'; return t; })() },
  { name: 'human-statement H confidence', case: 'RB-B2', expectCode: 'V-5',
    trace: (() => { const t = clone(GOLDENS['RB-B2']); t.iterations[0].evidence.find(e => e.source === 'human-statement').confidence = 'H'; return t; })() },
  { name: 'priority violation STOP1 true hit STOP-3', case: 'RB-02', expectCode: 'V-6',
    trace: (() => { const t = clone(GOLDENS['RB-02']); t.iterations[0].stop_check = { STOP4: false, STOP5: false, STOP1: true, STOP2: false, STOP3: true, hit: 'STOP-3' }; return t; })() },
  { name: 'self-report mismatch', case: 'RB-X5', expectCode: 'V-1d',
    trace: (() => { const t = clone(GOLDENS['RB-X5']); t.iterations[0].counters_after = { artifact: 1, scope: 1, depth: 1, iteration: 1 }; return t; })() },
  { name: 'A-§4 lock: targeted sig still consumes Artifact (under-reported)', case: 'RB-01', expectCode: 'V-1d',
    trace: (() => { const t = clone(GOLDENS['RB-01']); t.iterations[1].counters_after = { artifact: 2, scope: 1, depth: 1, iteration: 2 }; return t; })() },
  { name: 'A-§4 lock: repo-wide targeted grep busts Artifact budget even with Scope=0', case: 'RB-01', expectCode: 'V-1a',
    trace: (() => {
      // Low×Regional 档 (a2/s0)：Agent 全部用"定点 grep"（免 Scope），但连续触碰 5 个外部文件 → Artifact 3>2 必炸
      const t = clone(GOLDENS['RB-01']);
      t.finding.risk = 'Low'; t.finding.nature = 'Regional'; t.finding.nature_order_checked = ['Local', 'Regional'];
      t.budget_allocation = { depth: 1, artifact: 2, iteration: 2, scope: 0 };
      for (const p of ['backend/framework/JNPF/DependencyInjection/Dependencies/IScoped.cs', 'backend/framework/JNPF/DependencyInjection/Dependencies/ISingleton.cs']) {
        t.iterations[1].actions.push({ tool: 'Grep', mode: 'signature', target: p, hop: 1, purpose: 'just grepping' });
      }
      return t; })() },
];
