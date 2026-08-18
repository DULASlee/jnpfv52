/**
 * promptfoo 通用 SA Agent Provider
 *
 * 将 agentName 作为变量注入，一个 provider 覆盖所有 9 个 SA Agent。
 *
 * 与 scope-agent.js / ui-agent.js 的差异：
 *   - agentName 从 context.vars.agentName 读取（非硬编码）
 *   - 断言通过 promptfooconfig.yaml 的 javascript assert 定义
 *
 * 用法：
 *   vars: { agentName: 'DFDAgent', requirementText: '...' }
 */
const SA_BASE = process.env.SA_SERVICE_URL || 'http://localhost:3001';

module.exports = {
  id: 'jnpf-sa-generic-agent',

  async callApi(prompt, context) {
    const vars = context.vars ?? {};
    const agentName = vars.agentName ?? 'ScopeAgent';
    const requirementText = vars.requirementText ?? prompt;
    const projectId = vars.projectId ?? 'promptfoo-eval';
    const previousSteps = vars.previousSteps ?? {};

    let resp;
    try {
      resp = await fetch(`${SA_BASE}/sa/run-step`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          tenantId: '0',
          projectId,
          eventId: `BE-${agentName.toLowerCase()}`,
          agentName,
          requirementText,
          skeleton: {},
          previousSteps,
        }),
        signal: AbortSignal.timeout(90_000),
      });
    } catch (err) {
      return { error: `SA Service 不可达 (${SA_BASE}): ${err.message}` };
    }

    if (!resp.ok) {
      const text = await resp.text().catch(() => '');
      return { error: `SA Service HTTP ${resp.status}: ${text.slice(0, 200)}` };
    }

    const data = await resp.json();
    const output = data.output ?? data;

    return {
      output: typeof output === 'string' ? output : JSON.stringify(output, null, 2),
      tokenUsage: data.tokenUsage ?? {},
    };
  },
};
