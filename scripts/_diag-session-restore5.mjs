import { runSqlQuery } from './lib/jnpf-db.mjs';

const snaps = await runSqlQuery(`
SET NOCOUNT ON;
SELECT F_PIPELINE_ID AS Pid, F_FragmentId, F_FragmentType, F_StabilityState, F_CurrentVersion,
  LEN(ISNULL(F_IrContent,'')) AS ContentLen
FROM ai_ir_fragment_snapshots
WHERE F_PIPELINE_ID IN ('355','356')
  AND (F_DeleteMark IS NULL OR F_DeleteMark=0);
`);
console.log('snaps\n', snaps);

const dels = await runSqlQuery(`
SET NOCOUNT ON;
SELECT F_PipelineId, F_FileName, F_RelativePath, F_StageCode
FROM inte_assistant_deliverable
WHERE F_PipelineId IN ('355','356','354')
  AND (F_DeleteMark IS NULL OR F_DeleteMark=0);
`);
console.log('\ndeliverables\n', dels);

const api = await import('./lib/jnpf-auth.mjs');
const session = await api.login();
for (const id of [355, 356]) {
  const res = await api.apiRequest('GET', `/api/studio/pipeline/execute/${id}`, { session });
  const data = api.jnpfData(res) ?? res.json?.data ?? res.json;
  const msgs = data?.messages ?? data?.Messages ?? [];
  console.log(`\nAPI ${id}: msgCount=${msgs.length}, stage=${data?.currentStage ?? data?.CurrentStage}`);
  for (const m of msgs.slice(0, 5)) {
    const role = m.role ?? m.Role;
    const content = String(m.content ?? m.Content ?? '');
    console.log(`  ${role} len=${content.length} head=${content.slice(0, 80).replace(/\n/g, ' ')}`);
  }
}
