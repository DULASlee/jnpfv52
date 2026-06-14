/**
 * UI/UX 设计智能体单元测试
 */

import { describe, it, expect, beforeEach } from 'vitest';
import { UIUXAgent, type UIDesign } from '../agents/ui-ux';
import { MockLLMGateway } from './mock-llm';

describe('UI/UX 设计智能体', () => {
  let mockLLM: MockLLMGateway;
  let agent: UIUXAgent;

  const basicUIDesign: UIDesign = {
    overview: '学生管理表单页面',
    pageType: 'form',
    designRationale: '标准CRUD表单布局，2列网格',
    layout: { type: 'grid', columns: 2, gap: 16, responsive: true },
    colorScheme: {
      primary: '#1890ff',
      secondary: '#52c41a',
      background: '#f0f2f5',
      text: '#262626',
    },
    ir: {
      type: 'form',
      id: 'student-form',
      name: '学生管理',
      fields: [
        {
          id: 'name',
          model: 'name',
          label: '姓名',
          component: { jnpfKey: 'JnpfInput', pc: 'a-input', app: 'uni-easyinput', legacyApp: 'uni-easyinput' },
          config: {
            required: true,
            defaultValue: '',
            placeholder: '请输入姓名',
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
          id: 'unknown',
          model: 'unknown',
          label: '未知组件',
          component: { jnpfKey: 'JnpfMagicWidget', pc: '', app: '', legacyApp: '' },
          config: {
            required: false,
            defaultValue: '',
            placeholder: '',
            disabled: false,
            readonly: false,
            hidden: false,
            span: 12,
            labelWidth: null,
            maxlength: null,
            showWordLimit: false,
            clearable: false,
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
      ],
      databaseFields: [],
      expressions: [],
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
    },
    interactions: [{ trigger: 'hover', action: '按钮高亮', animation: 'ease-in-out 0.2s' }],
  };

  beforeEach(() => {
    mockLLM = new MockLLMGateway();
    agent = new UIUXAgent(mockLLM);
  });

  it('design 返回 UIDesign 结构', async () => {
    mockLLM.setResponse('学生管理', JSON.stringify(basicUIDesign));
    const result = await agent.design('设计一个学生管理表单');
    expect(result.data.pageType).toBe('form');
    expect(result.data.layout.type).toBe('grid');
  });

  it('未知组件降级为 JnpfInput', async () => {
    mockLLM.setResponse('测试', JSON.stringify(basicUIDesign));
    const result = await agent.design('设计一个测试页面');
    const ir = result.data.ir as { fields: Array<{ id: string; component: { jnpfKey: string } }> };
    const unknownField = ir.fields.find(f => f.id === 'unknown');
    expect(unknownField!.component.jnpfKey).toBe('JnpfInput');
  });

  it('自动填充 aiHints.designRationale', async () => {
    mockLLM.setResponse('学生管理', JSON.stringify(basicUIDesign));
    const result = await agent.design('设计一个学生管理表单');
    const ir = result.data.ir as { aiHints?: { designRationale?: string } };
    expect(ir.aiHints?.designRationale).toBeDefined();
  });

  it('自动补全 pc/app 映射', async () => {
    const incompleteDesign = {
      ...basicUIDesign,
      ir: { ...basicUIDesign.ir, fields: [{ ...basicUIDesign.ir.fields[0], component: { ...basicUIDesign.ir.fields[0].component, pc: '', app: '' } }] },
    };
    mockLLM.setResponse('学生管理', JSON.stringify(incompleteDesign));
    const result = await agent.design('设计一个学生管理表单');
    const ir = result.data.ir as { fields: Array<{ component: { pc: string; app: string } }> };
    expect(ir.fields[0].component.pc).toBe('a-input');
    expect(ir.fields[0].component.app).toBe('uni-easyinput');
  });

  it('3D 大屏检测 VIP 标记', async () => {
    const dashboard3D = { ...basicUIDesign, pageType: 'dashboard', overview: '3D 数字孪生大屏', ir: { type: 'dashboard', id: '3d', name: '3D' } };
    mockLLM.setResponse('3D大屏', JSON.stringify(dashboard3D));
    const result = await agent.design('设计一个3D大屏');
    const ir = result.data.ir as { aiHints?: { domain?: string } };
    expect(ir.aiHints?.domain).toBe('3D-digital-twin');
  });
});
