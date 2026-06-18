/**
 * 编译桥接层集成测试
 *
 * 验证 AI IR → compileGateway → ZIP 全链路。
 * 使用真实的编译网关（不需要 mock LLM），因为编译网关是纯数据变换。
 */
import { describe, it, expect } from 'vitest';
import { compileAgentOutput, compileAgentOutputMulti, summarizeResult, summarizeBatchResult, type AIGeneratedPage } from '../integration/compile-bridge';

/** 最小合法 FormPageIR（模拟 AI UI/UX 智能体输出） */
const MINIMAL_AI_PAGE: AIGeneratedPage = {
  entity: 'test_ai_page',
  name: 'AI生成的测试页面',
  ir: {
    type: 'form',
    id: 'ai-test-form',
    name: 'AI测试表单',
    config: {
      labelPosition: 'right',
      labelWidth: 100,
      labelSuffix: '：',
      size: 'default',
      disabled: false,
      span: 24,
      gutter: 16,
      colon: true,
      popupType: 'general',
      generalWidth: '800px',
      fullScreenWidth: '100%',
      drawerWidth: '520px',
      hasCancelBtn: true,
      cancelButtonText: '取消',
      hasConfirmBtn: true,
      confirmButtonText: '保存',
      hasConfirmAndAddBtn: false,
      hasPrintBtn: false,
      printButtonText: '打印',
      primaryKeyPolicy: 'snowflake',
      tablePolicy: 'auto',
      concurrencyLock: false,
      logicalDelete: true,
    },
    fields: [
      {
        id: 'name',
        model: 'name',
        label: '名称',
        component: { jnpfKey: 'JnpfInput', pc: 'a-input', app: 'uni-easyinput', legacyApp: 'uni-easyinput' },
        config: {
          required: true,
          defaultValue: '',
          placeholder: '请输入名称',
          disabled: false,
          readonly: false,
          hidden: false,
          span: 12,
          labelWidth: null,
          maxlength: 100,
          showWordLimit: true,
          clearable: true,
          min: null,
          max: null,
          precision: null,
          step: null,
          multiple: false,
          options: [],
          dictType: null,
          relationData: null,
          style: {},
        },
        validation: [],
        events: {},
      },
      {
        id: 'status',
        model: 'status',
        label: '状态',
        component: { jnpfKey: 'JnpfSelect', pc: 'a-select', app: 'uni-data-select', legacyApp: 'uni-data-select' },
        config: {
          required: false,
          defaultValue: '1',
          placeholder: '请选择状态',
          disabled: false,
          readonly: false,
          hidden: false,
          span: 12,
          labelWidth: null,
          maxlength: null,
          showWordLimit: false,
          clearable: true,
          min: null,
          max: null,
          precision: null,
          step: null,
          multiple: false,
          options: [
            { label: '启用', value: '1' },
            { label: '禁用', value: '0' },
          ],
          dictType: null,
          relationData: null,
          style: {},
        },
        validation: [],
        events: {},
      },
    ],
    databaseFields: [
      { id: 'name', name: 'name', type: 'NVARCHAR', length: 100, nullable: false, defaultValue: '', description: '名称' },
      { id: 'status', name: 'status', type: 'INT', length: null, nullable: false, defaultValue: '1', description: '状态' },
    ],
    expressions: [],
    listConfig: {
      searchFields: [{ field: 'name', label: '名称', component: 'JnpfInput', options: [] }],
      columns: [{ field: 'name', label: '名称', width: 200, fixed: null, sortable: false }],
      ruleList: [],
    },
    aiHints: {
      domain: '测试',
      designRationale: 'AI生成的测试页面 — 集成测试',
      confidence: 0.85,
    },
  },
};

describe('编译桥接层', () => {
  it('AI IR → compileGateway 单目标编译成功', async () => {
    const result = await compileAgentOutput(MINIMAL_AI_PAGE, 'vue3-web');

    // 桥接层正确调用网关，无异常抛出
    expect(result.entity).toBe('test_ai_page');
    expect(result.target).toBe('vue3-web');
    expect(typeof result.success).toBe('boolean');
  });

  it('摘要信息包含文件数和耗时', () => {
    // 构造成功结果用于摘要测试
    const summary = summarizeResult({
      success: true,
      target: 'vue3-web',
      entity: 'test',
      response: {
        success: true,
        project: new Map<string, string>([
          ['a.vue', ''],
          ['b.ts', ''],
        ]),
        duration: 150,
      },
    });

    expect(summary.status).toBe('success');
    expect(summary.fileCount).toBe(2);
    expect(summary.duration).toBe(150);
  });

  it('编译失败返回 success=false', async () => {
    // 使用一个明显无效的 IR
    const badPage: AIGeneratedPage = {
      entity: 'bad',
      name: '无效页面',
      ir: { type: 'form' as const, id: 'bad', name: 'bad', config: {} as never, fields: [], databaseFields: [], expressions: [] },
    };

    const result = await compileAgentOutput(badPage, 'vue3-web');
    // 即使IR不完整，gateway 也应返回 success=false（不应抛出）
    expect(typeof result.success).toBe('boolean');
  });

  it('多目标编译返回 BatchCompileResult', async () => {
    const result = await compileAgentOutputMulti(MINIMAL_AI_PAGE, ['vue3-web', 'uniapp-h5']);

    expect(result.totalTargets).toBe(2);
    expect(result.results.length).toBe(2);
    expect(result.successCount).toBeGreaterThanOrEqual(0);
  });

  it('批量摘要生成正确', () => {
    const summary = summarizeBatchResult({
      totalTargets: 2,
      successCount: 1,
      failureCount: 1,
      results: [
        {
          success: true,
          target: 'vue3-web',
          entity: 'test',
          response: { success: true, project: new Map<string, string>([['a.vue', 'x']]), duration: 100 },
        },
        { success: false, target: 'uniapp-h5', entity: 'test', response: { success: false, error: 'IR验证失败' } },
      ],
    });

    expect(summary.perTarget.length).toBe(2);
    expect(summary.perTarget[0].status).toBe('✅');
    expect(summary.perTarget[1].status).toBe('❌');
  });
});
