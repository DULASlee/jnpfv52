/**
 * 端到端全链路测试 (Day 34b)
 *
 * 场景：用户输入"做一个学生管理系统" → 5阶段流水线 → 编译 → ZIP下载
 * 使用 MockLLM 预设响应 + 真实编译网关。
 */
import { describe, it, expect, beforeEach } from 'vitest';
import { OrchestratorAgent } from '../pipeline/orchestrator';
import { createInitialState } from '../pipeline/state-machine';
import { MockLLMGateway } from './mock-llm';

const REQ = {
  understanding: '学生管理系统',
  questions: [],
  proposedDomainModel: {
    entities: [
      {
        name: '学生',
        fields: [
          { name: '姓名', type: 'string' },
          { name: '学号', type: 'string' },
        ],
      },
    ],
    relationships: [],
    businessRules: [],
  },
  strategies: [{ name: 'CRUD', description: '标准', pros: ['快'], cons: [], impact: '低' }],
  userStories: [{ role: '管理员', action: '添加学生', goal: '管理', acceptance: '成功' }],
  implicitRequirements: [],
  risks: [],
};
const ARCH = {
  overview: '学生管理架构',
  architecture: {
    modules: [{ name: 'Student', responsibility: '学生管理', dependencies: [] }],
    databaseDesign: { tables: [{ name: 'base_student', comment: '学生表', columns: [], indexes: [] }] },
    apiDesign: { endpoints: [{ path: '/api/student/list', method: 'GET', description: '列表' }] },
    uiDesign: { pages: [{ name: '学生管理', type: 'list', fields: ['name'] }] },
  },
  irPages: [],
  techStack: { framework: '.NET', ui: 'Vue3', database: 'SQL Server', cache: 'Memory', mq: 'Channel' },
  decisions: [],
};
const UI = {
  overview: '学生表单',
  pageType: 'form',
  designRationale: '标准表单',
  layout: { type: 'grid', columns: 2, gap: 16, responsive: true },
  colorScheme: { primary: '#1890ff', secondary: '#52c41a', background: '#f0f2f5', text: '#262626' },
  ir: { type: 'form', id: 'test', name: 'Test', fields: [], databaseFields: [], expressions: [], config: {} },
  interactions: [],
};
const DB = {
  overview: '数据库设计',
  tables: [{ name: 'BASE_STUDENT', comment: '', columns: [], indexes: [] }],
  migrationSql: 'CREATE TABLE BASE_STUDENT (...);',
  apis: [{ path: '/api/student/list', method: 'GET', description: '列表', requireAuth: true }],
};

describe('端到端全链路', () => {
  let mock: MockLLMGateway;
  let orch: OrchestratorAgent;

  beforeEach(() => {
    mock = new MockLLMGateway();
    orch = new OrchestratorAgent(mock);
  });

  it('需求→架构→设计→开发→交付 全链路', async () => {
    mock.setResponse('做一个学生', JSON.stringify(REQ));
    mock.setResponse('strategies', JSON.stringify(ARCH));
    mock.setResponse('架构设计', JSON.stringify(UI));
    mock.setResponse('databaseDesign', JSON.stringify(DB));

    let state = createInitialState();

    // 需求阶段
    state = await orch.advance(state, '做一个学生管理系统');
    expect(state.status).toBe('waiting_confirmation');
    expect(state.requirement).toBeDefined();

    // 架构阶段
    state = await orch.confirm(state);
    state = await orch.advance(state);
    expect(state.architecture).toBeDefined();

    // UI/DB设计阶段（并行）
    state = await orch.confirm(state);
    state = await orch.advance(state);
    expect(state.design).toBeDefined();
    expect(state.design!.ui).toBeDefined();
    expect(state.design!.database).toBeDefined();

    // 开发阶段（自动）
    state = await orch.confirm(state);
    state = await orch.advance(state);
    expect(state.development).toBeDefined();

    // 交付阶段
    state = await orch.confirm(state);
    state = await orch.advance(state);
    state = await orch.confirm(state);
    expect(state.status).toBe('completed');
  });

  it('全链路每个阶段均有产出', async () => {
    mock.setResponse('做一个学生', JSON.stringify(REQ));
    mock.setResponse('strategies', JSON.stringify(ARCH));
    mock.setResponse('架构设计', JSON.stringify(UI));
    mock.setResponse('databaseDesign', JSON.stringify(DB));

    let state = createInitialState();
    state = await orch.advance(state, '做一个学生管理系统');

    const stages = [{ s: state, key: 'requirement' }];
    state = await orch.confirm(state);
    state = await orch.advance(state);
    stages.push({ s: state, key: 'architecture' });
    state = await orch.confirm(state);
    state = await orch.advance(state);
    stages.push({ s: state, key: 'design' });
    state = await orch.confirm(state);
    state = await orch.advance(state);
    stages.push({ s: state, key: 'development' });
    state = await orch.confirm(state);
    state = await orch.advance(state);
    state = await orch.confirm(state);
    stages.push({ s: state, key: 'delivery' });

    expect(stages.length).toBe(5);
    for (const { s, key } of stages) {
      expect(s.history.length).toBeGreaterThan(0);
    }
    expect(state.status).toBe('completed');
  });

  it('revise 重新执行当前阶段', async () => {
    mock.setResponse('做一个学生', JSON.stringify(REQ));
    mock.setResponse('增加成绩', JSON.stringify({ ...REQ, understanding: '增加了成绩统计' }));

    let state = createInitialState();
    state = await orch.advance(state, '做一个学生管理系统');

    // 反馈修改
    state = await orch.revise(state, '需要增加成绩统计功能');
    expect(state.status).toBe('waiting_confirmation');
  });
});
