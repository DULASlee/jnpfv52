import {
  buildGateFailedMarkdown,
  normalizeSemanticFitness,
  parseGatePayload,
} from '../jnpf-web-vue3/src/views/studio/composables/gateStreamFormatter.ts';

const sse = {
  reason: '需求材料评估结果：Insufficient（评分 0/100）\n\n❌ 缺失的关键要素（必须补充）：\n  - 系统：评估服务异常 (GATE_LLM_ERR)\n',
  hint: '需求评估服务暂时不可用，请稍后重试。\n错误代码: GATE_LLM_ERR',
  semanticFitness: {
    passed: false,
    score: 0,
    level: 'Insufficient', // or 2
    identified: [],
    missing: [
      {
        category: '系统',
        description: '评估服务异常 (GATE_LLM_ERR)',
        severity: 'critical',
        howToFix: '需求评估服务暂时不可用，请稍后重试。',
      },
    ],
    nextStepGuidance: '需求评估服务暂时不可用，请稍后重试。\n错误代码: GATE_LLM_ERR',
  },
};

const payload = parseGatePayload(JSON.stringify(sse)) ?? {};
const sf = normalizeSemanticFitness(payload);
const md = buildGateFailedMarkdown(payload, sf);
console.log('HAS_OBJECT', md.includes('[object Object]'));
console.log('---MD---\n', md);

// PascalCase from System.Text.Json default
const sse2 = {
  Reason: sse.reason,
  Hint: sse.hint,
  SemanticFitness: {
    Passed: false,
    Score: 0,
    Level: 2,
    Identified: [],
    Missing: [
      {
        Category: '系统',
        Description: '评估服务异常 (GATE_LLM_ERR)',
        Severity: 'critical',
        HowToFix: '需求评估服务暂时不可用，请稍后重试。',
      },
    ],
    NextStepGuidance: '需求评估服务暂时不可用，请稍后重试。\n错误代码: GATE_LLM_ERR',
  },
};
const p2 = parseGatePayload(JSON.stringify(sse2)) ?? {};
const sf2 = normalizeSemanticFitness(p2);
const md2 = buildGateFailedMarkdown(p2, sf2);
console.log('\nPASCAL HAS_OBJECT', md2.includes('[object Object]'));
console.log('sf2.missing', sf2?.missing);
console.log('---MD2---\n', md2.slice(0, 800));

// Bug shape: missing is array of objects but normalize skipped somehow
const bad = buildGateFailedMarkdown(
  { reason: 'x', hint: 'y' },
  {
    passed: false,
    score: 0,
    level: 'insufficient',
    identified: [],
    missing: [{ Category: '系统', Description: 'x' }, { Category: 'y' }, {}, {}],
    nextStepGuidance: undefined,
  },
);
console.log('\nBAD HAS_OBJECT', bad.includes('[object Object]'), bad.includes('undefined'));
console.log(bad);
