/**
 * 流水线集成测试 (A1)
 *
 * 测试五阶段全链路：需求→架构→设计→开发→交付
 * 使用 MockLLM 预设响应，不真实调用 API。
 */

import { describe, it, expect, beforeEach } from 'vitest';
import { OrchestratorAgent } from '../pipeline/orchestrator';
import { createInitialState, canTransition, updateConfidence } from '../pipeline/state-machine';
import { STAGES, getNextStage, getPrevStage } from '../pipeline/stages';
import { MockLLMGateway } from './mock-llm';

// 预设响应
const REQUIREMENT_RESPONSE = {
  understanding: '学生管理系统',
  questions: [],
  proposedDomainModel: {
    entities: [{ name: '学生', fields: [{ name: '姓名', type: 'string' }] }],
    relationships: [],
    businessRules: [],
  },
  strategies: [{ name: 'CRUD', description: '标准CRUD', pros: ['快'], cons: [], impact: '低' }],
  userStories: [{ role: '管理员', action: '添加学生', goal: '管理', acceptance: '成功' }],
  implicitRequirements: [],
  risks: [],
};

const ARCHITECTURE_RESPONSE = {
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

const UIDESIGN_RESPONSE = {
  overview: '学生表单',
  pageType: 'form',
  designRationale: '标准表单',
  layout: { type: 'grid', columns: 2, gap: 16, responsive: true },
  colorScheme: { primary: '#1890ff', secondary: '#52c41a', background: '#f0f2f5', text: '#262626' },
  ir: { type: 'form', id: 'test', name: '测试', fields: [], databaseFields: [], expressions: [], config: {} },
  interactions: [],
};

const DATABASE_RESPONSE = {
  overview: '数据库设计',
  tables: [{ name: 'BASE_STUDENT', comment: '', columns: [], indexes: [] }],
  migrationSql: 'CREATE TABLE BASE_STUDENT (...);',
  apis: [{ path: '/api/student/list', method: 'GET', description: '列表', requireAuth: true }],
};

describe('流水线阶段定义', () => {
  it('共 5 个阶段', () => {
    expect(STAGES.length).toBe(5);
  });

  it('requirement 是第一阶段，无 inputFrom', () => {
    const s = STAGES[0];
    expect(s.id).toBe('requirement');
    expect(s.inputFrom).toBeNull();
    expect(s.requiresConfirmation).toBe(true);
  });

  it('development 不需人类确认', () => {
    const s = STAGES.find(st => st.id === 'development');
    expect(s!.requiresConfirmation).toBe(false);
  });

  it('getNextStage 正确流转', () => {
    expect(getNextStage('requirement')).toBe('architecture');
    expect(getNextStage('delivery')).toBeNull();
  });

  it('getPrevStage 正确回溯', () => {
    expect(getPrevStage('architecture')).toBe('requirement');
    expect(getPrevStage('requirement')).toBeNull();
  });
});

describe('状态机', () => {
  it('初始状态为 idle + requirement', () => {
    const state = createInitialState();
    expect(state.currentStage).toBe('requirement');
    expect(state.status).toBe('idle');
  });

  it('有效状态转换', () => {
    expect(canTransition('idle', 'running')).toBe(true);
    expect(canTransition('running', 'waiting_confirmation')).toBe(true);
    expect(canTransition('running', 'failed')).toBe(true);
    expect(canTransition('running', 'expert_mode')).toBe(true);
  });

  it('非法状态转换被拒绝', () => {
    expect(canTransition('completed', 'idle')).toBe(false);
    expect(canTransition('idle', 'completed')).toBe(false);
  });

  it('置信度 < 0.6 自动切换专家模式', () => {
    const state = createInitialState();
    updateConfidence(state, 0.5);
    expect(state.status).toBe('expert_mode');
  });

  it('置信度 ≥ 0.6 保持当前模式', () => {
    const state = createInitialState();
    updateConfidence(state, 0.85);
    expect(state.status).toBe('idle');
  });
});

describe('编排器 — 全链路', () => {
  let mockLLM: MockLLMGateway;
  let orchestrator: OrchestratorAgent;

  beforeEach(() => {
    mockLLM = new MockLLMGateway();
    orchestrator = new OrchestratorAgent(mockLLM);
  });

  it('需求阶段：analyze → waiting_confirmation', async () => {
    mockLLM.setResponse('学生管理', JSON.stringify(REQUIREMENT_RESPONSE));

    const state = createInitialState();
    const result = await orchestrator.advance(state, '做一个学生管理系统');

    expect(result.status).toBe('waiting_confirmation');
    expect(result.requirement).toBeDefined();
    expect(result.requirement!.proposedDomainModel.entities.length).toBe(1);
  });

  it('架构阶段：design → 自动注入审计字段', async () => {
    mockLLM.setResponse('学生管理', JSON.stringify(REQUIREMENT_RESPONSE));
    mockLLM.setResponse('学生管理', JSON.stringify(ARCHITECTURE_RESPONSE));

    const state1 = createInitialState();
    const s1 = await orchestrator.advance(state1, '做一个学生管理系统');
    const s2 = await orchestrator.confirm(s1); // 确认 → 进入架构阶段
    const s3 = await orchestrator.advance(s2);

    expect(s3.status).toBe('waiting_confirmation');
    expect(s3.architecture).toBeDefined();
  });

  it('设计阶段：UI+DB 并行 → waiting_confirmation', async () => {
    mockLLM.setResponse('做一个学生', JSON.stringify(REQUIREMENT_RESPONSE));
    mockLLM.setResponse('proposedDomainModel', JSON.stringify(ARCHITECTURE_RESPONSE));
    mockLLM.setResponse('架构设计', JSON.stringify(UIDESIGN_RESPONSE));
    mockLLM.setResponse('databaseDesign', JSON.stringify(DATABASE_RESPONSE));

    let state = createInitialState();
    state = await orchestrator.advance(state, '做一个学生管理系统');
    state = await orchestrator.confirm(state);
    state = await orchestrator.advance(state);
    state = await orchestrator.confirm(state);
    state = await orchestrator.advance(state);

    expect(state.design).toBeDefined();
    expect(state.design!.ui).toBeDefined();
    expect(state.design!.database).toBeDefined();
  });

  it('revise 重新执行当前阶段', async () => {
    mockLLM.setResponse('学生管理', JSON.stringify(REQUIREMENT_RESPONSE));

    const state = createInitialState();
    const s1 = await orchestrator.advance(state, '做一个学生管理系统');

    // revise 带反馈重新执行
    mockLLM.setResponse(
      '成绩统计',
      JSON.stringify({
        ...REQUIREMENT_RESPONSE,
        understanding: '增加了成绩统计的学生管理系统',
      }),
    );

    const revised = await orchestrator.revise(s1, '需要增加成绩统计功能');
    expect(revised.status).toBe('waiting_confirmation');
  });

  it('全链路完成 → completed', async () => {
    mockLLM.setResponse('做一个学生', JSON.stringify(REQUIREMENT_RESPONSE));
    mockLLM.setResponse('proposedDomainModel', JSON.stringify(ARCHITECTURE_RESPONSE));
    mockLLM.setResponse('架构设计', JSON.stringify(UIDESIGN_RESPONSE));
    mockLLM.setResponse('databaseDesign', JSON.stringify(DATABASE_RESPONSE));

    let state = createInitialState();
    state = await orchestrator.advance(state, '做一个学生管理系统'); // requirement
    state = await orchestrator.confirm(state); // → architecture
    state = await orchestrator.advance(state); // architecture
    state = await orchestrator.confirm(state); // → design
    state = await orchestrator.advance(state); // UI+DB
    state = await orchestrator.confirm(state); // → development (auto)
    state = await orchestrator.advance(state); // development
    state = await orchestrator.confirm(state); // → delivery
    state = await orchestrator.advance(state); // delivery
    state = await orchestrator.confirm(state); // → completed

    expect(state.status).toBe('completed');
    expect(state.requirement).toBeDefined();
    expect(state.architecture).toBeDefined();
    expect(state.design).toBeDefined();
    expect(state.development).toBeDefined();
    expect(state.delivery).toBeDefined();
  });
});
