/**
 * promptfoo Provider：UIAgent（测试第九步 UI 原型生成质量）
 *
 * 输入：previousSteps 包含 DFD + Dict + BPM（最小合法上下文）
 * 输出：UIOutput JSON，断言页面数量、字段类型映射、controlType 规则
 */

const SA_BASE = process.env.SA_SERVICE_URL || 'http://localhost:3001';

const MINIMAL_CONTEXT = {
  dfd: {
    dataFlows: [{ id: 'DF-1', name: '请假申请数据', from: '员工', to: '请假申请系统', data: ['申请信息'] }],
    dataStores: [{ id: 'DS-1', name: '请假记录', storedData: ['申请信息'] }],
  },
  dict: {
    elements: [
      { name: 'leave_type', chineseName: '请假类型', type: 'NVARCHAR', isRequired: true, validValues: ['年假', '病假', '事假'] },
      { name: 'start_date', chineseName: '开始日期', type: 'DATETIME', isRequired: true },
      { name: 'days', chineseName: '天数', type: 'DECIMAL', isRequired: true },
      { name: 'reason', chineseName: '原因', type: 'NVARCHAR', isRequired: false },
    ],
    dataFlows: [],
    dataStores: [{ name: '请假记录', fields: [
      { name: 'leave_type', type: 'NVARCHAR' },
      { name: 'tenant_id', type: 'NVARCHAR' },
    ]}],
  },
  bpm: {
    process: { id: 'PROC-1', name: '请假审批', startEvent: 'start', endEvent: 'end' },
    activities: [
      { id: 'ACT-1', name: '提交申请', type: 'userTask', performer: '员工' },
      { id: 'ACT-2', name: '主管审批', type: 'userTask', performer: '主管' },
    ],
    flows: [{ id: 'F-1', from: 'start', to: 'ACT-1' }, { id: 'F-2', from: 'ACT-1', to: 'ACT-2' }],
    gateways: [],
  },
};

module.exports = {
  id: 'jnpf-sa-ui-agent',

  async callApi(prompt, context) {
    const vars = context.vars ?? {};
    const requirementText = vars.requirementText ?? prompt;

    let resp;
    try {
      resp = await fetch(`${SA_BASE}/sa/run-step`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          tenantId: '0',
          projectId: 'promptfoo-ui-eval',
          eventId: 'BE-001',
          agentName: 'UIAgent',
          requirementText,
          skeleton: {},
          previousSteps: MINIMAL_CONTEXT,
        }),
        signal: AbortSignal.timeout(60_000),
      });
    } catch (err) {
      return { error: `SA Service 不可达: ${err.message}` };
    }

    if (!resp.ok) {
      return { error: `SA Service HTTP ${resp.status}` };
    }

    const data = await resp.json();
    const output = data.output ?? data;
    return {
      output: typeof output === 'string' ? output : JSON.stringify(output, null, 2),
    };
  },
};
