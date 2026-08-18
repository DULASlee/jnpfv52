/**
 * promptfoo 自定义 Provider：调用 sa-service ScopeAgent
 *
 * 与直接测试 LLM 不同，这里测试的是"SA Agent 的完整输入→输出管道"：
 *   requirementText → ScopeAgent.systemPrompt + buildPrompt → LLM → ScopeOutput JSON
 *
 * 优点：
 *   1. 提示词修改后立即在 CI 捕获回归
 *   2. 与真实 LLM Gateway 对话，测试真实输出质量
 *   3. 断言 JSON Schema 合规性 + 语义质量（llm-rubric）
 */

const SA_BASE = process.env.SA_SERVICE_URL || 'http://localhost:3001';

module.exports = {
  id: 'jnpf-sa-scope-agent',

  async callApi(prompt, context) {
    const vars = context.vars ?? {};
    const requirementText = vars.requirementText ?? prompt;
    const projectId = vars.projectId ?? 'promptfoo-eval';

    let resp;
    try {
      resp = await fetch(`${SA_BASE}/sa/run-step`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          tenantId: '0',
          projectId,
          eventId: 'BE-scope',
          agentName: 'ScopeAgent',
          requirementText,
          skeleton: {},
          previousSteps: {},
        }),
        signal: AbortSignal.timeout(60_000),
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
      tokenUsage: {},
    };
  },
};
