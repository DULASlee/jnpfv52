/**
 * 跨领域实战验证 (Day 36-38)
 *
 * 验证3个真实领域场景：
 *   MES制造 — 工单/设备/质检/排程
 *   智慧工地 — 人员/安全帽/塔吊/施工进度
 *   智能更衣柜 — 柜门控制/借用归还/异常报警/支付
 */
import { describe, it, expect, beforeEach } from 'vitest';
import { OrchestratorAgent } from '../pipeline/orchestrator';
import { createInitialState } from '../pipeline/state-machine';
import { ArchitectAgent } from '../agents/architect';
import { DatabaseAgent } from '../agents/database';
import { MockLLMGateway } from './mock-llm';

interface RequirementResult {
  proposedDomainModel: {
    entities: Array<{ name: string }>;
    businessRules: Array<{ name: string; condition: string }>;
  };
  implicitRequirements: string[];
}

// MES领域Mock数据
const MES_REQ = {
  understanding: 'MES制造执行系统',
  questions: [],
  proposedDomainModel: {
    entities: [
      {
        name: '工单',
        fields: [
          { name: '工单号', type: 'string' },
          { name: '产品', type: 'string' },
          { name: '数量', type: 'number' },
        ],
      },
      {
        name: '设备',
        fields: [
          { name: '设备编号', type: 'string' },
          { name: '型号', type: 'string' },
        ],
      },
      {
        name: '质检记录',
        fields: [
          { name: '检验结果', type: 'string' },
          { name: '不合格数', type: 'number' },
        ],
      },
      {
        name: '生产排程',
        fields: [
          { name: '产线', type: 'string' },
          { name: '计划日期', type: 'string' },
        ],
      },
    ],
    relationships: [{ from: '工单', to: '设备', type: 'one-to-many' }],
    businessRules: [{ name: '完工质检', condition: '工单状态=完成', action: '触发质检' }],
  },
  strategies: [{ name: 'MES标准', description: '制造执行', pros: ['快'], cons: [], impact: '中' }],
  userStories: [{ role: '生产主管', action: '创建工单', goal: '管理生产', acceptance: '成功创建' }],
  implicitRequirements: ['设备OEE统计'],
  risks: [],
};
const MES_ARCH = {
  overview: 'MES系统架构',
  architecture: {
    modules: [
      { name: 'WorkOrder', responsibility: '工单管理', dependencies: [] },
      { name: 'Equipment', responsibility: '设备管理', dependencies: [] },
      { name: 'Quality', responsibility: '质检管理', dependencies: ['WorkOrder'] },
      { name: 'Schedule', responsibility: '排程管理', dependencies: ['WorkOrder', 'Equipment'] },
    ],
    databaseDesign: {
      tables: [
        { name: 'mes_work_order', comment: '工单表', columns: [], indexes: [] },
        { name: 'mes_equipment', comment: '设备表', columns: [], indexes: [] },
        { name: 'mes_quality', comment: '质检表', columns: [], indexes: [] },
        { name: 'mes_schedule', comment: '排程表', columns: [], indexes: [] },
      ],
    },
    apiDesign: { endpoints: [{ path: '/api/work-order/list', method: 'GET', description: '工单列表' }] },
    uiDesign: { pages: [{ name: '工单管理', type: 'list', fields: ['orderNo'] }] },
  },
  irPages: [],
  techStack: { framework: '.NET', ui: 'Vue3', database: 'SQL Server', cache: 'Memory', mq: 'Channel' },
  decisions: [],
};
const UI_MOCK = {
  overview: '表单',
  pageType: 'form',
  designRationale: '标准',
  layout: { type: 'grid', columns: 2, gap: 16, responsive: true },
  colorScheme: { primary: '#1890ff', secondary: '#52c41a', background: '#f0f2f5', text: '#262626' },
  ir: {},
  interactions: [],
};
const DB_MOCK = { overview: '数据库', tables: [{ name: 'BASE_T', comment: '', columns: [], indexes: [] }], migrationSql: '', apis: [] };

// 智慧工地Mock
const SITE_REQ = {
  understanding: '智慧工地',
  questions: [],
  proposedDomainModel: {
    entities: [
      { name: '人员', fields: [{ name: '姓名', type: 'string' }] },
      {
        name: '安全帽',
        fields: [
          { name: '设备ID', type: 'string' },
          { name: '报警状态', type: 'boolean' },
        ],
      },
      {
        name: '塔吊',
        fields: [
          { name: '编号', type: 'string' },
          { name: '高度', type: 'number' },
        ],
      },
      {
        name: '施工进度',
        fields: [
          { name: '进度百分比', type: 'number' },
          { name: '里程碑', type: 'string' },
        ],
      },
    ],
    relationships: [],
    businessRules: [],
  },
  strategies: [{ name: '工地标准', description: '智慧管理', pros: [], cons: [], impact: '中' }],
  userStories: [{ role: '安全员', action: '查看告警', goal: '安全管理', acceptance: '实时告警' }],
  implicitRequirements: [],
  risks: [],
};
const SITE_ARCH = {
  overview: '智慧工地架构',
  architecture: {
    modules: [
      { name: 'Personnel', responsibility: '人员', dependencies: [] },
      { name: 'Safety', responsibility: '安全帽', dependencies: ['Personnel'] },
      { name: 'Crane', responsibility: '塔吊', dependencies: [] },
      { name: 'Progress', responsibility: '进度', dependencies: [] },
    ],
    databaseDesign: {
      tables: [
        { name: 'site_personnel', comment: '人员表', columns: [], indexes: [] },
        { name: 'site_helmet', comment: '安全帽告警', columns: [], indexes: [] },
        { name: 'site_crane', comment: '塔吊监控', columns: [], indexes: [] },
        { name: 'site_progress', comment: '施工进度', columns: [], indexes: [] },
      ],
    },
    apiDesign: { endpoints: [] },
    uiDesign: { pages: [{ name: '工地看板', type: 'dashboard', fields: [] }] },
  },
  irPages: [],
  techStack: { framework: '.NET', ui: 'Vue3', database: 'SQL Server', cache: 'Memory', mq: 'Channel' },
  decisions: [],
};

// 智能更衣柜Mock
const LOCKER_REQ = {
  understanding: '智能更衣柜',
  questions: [],
  proposedDomainModel: {
    entities: [
      {
        name: '柜门',
        fields: [
          { name: '门号', type: 'string' },
          { name: '状态', type: 'string' },
        ],
      },
      {
        name: '借用记录',
        fields: [
          { name: '借用人', type: 'string' },
          { name: '借用时间', type: 'string' },
          { name: '归还时间', type: 'string' },
        ],
      },
      {
        name: '异常报警',
        fields: [
          { name: '报警类型', type: 'string' },
          { name: '时间', type: 'string' },
        ],
      },
      {
        name: '支付记录',
        fields: [
          { name: '金额', type: 'number' },
          { name: '支付方式', type: 'string' },
        ],
      },
    ],
    relationships: [
      { from: '柜门', to: '借用记录', type: 'one-to-many' },
      { from: '柜门', to: '异常报警', type: 'one-to-many' },
    ],
    businessRules: [{ name: '超时归还', condition: '借用>24h', action: '自动报警' }],
  },
  strategies: [{ name: 'IoT标准', description: '设备管理', pros: [], cons: [], impact: '低' }],
  userStories: [{ role: '用户', action: '借用柜门', goal: '安全存放', acceptance: '扫码开柜' }],
  implicitRequirements: ['MQTT设备通信'],
  risks: [],
};

describe('跨领域实战验证', () => {
  let mock: MockLLMGateway;
  let orch: OrchestratorAgent;
  let archAgent: ArchitectAgent;
  let dbAgent: DatabaseAgent;

  beforeEach(() => {
    mock = new MockLLMGateway();
    orch = new OrchestratorAgent(mock);
    archAgent = new ArchitectAgent(mock);
    dbAgent = new DatabaseAgent(mock);
  });

  // ─── MES制造 ───
  describe('MES制造执行系统', () => {
    it('需求分析识别工单/设备/质检/排程实体', async () => {
      mock.setResponse('MES', JSON.stringify(MES_REQ));
      const state = createInitialState();
      const result = await orch.advance(state, '我要一个MES制造执行系统');
      expect(result.requirement).toBeDefined();
      const req = result.requirement as RequirementResult;
      const entities = req.proposedDomainModel.entities.map(e => e.name);
      expect(entities).toContain('工单');
      expect(entities).toContain('设备');
      expect(entities).toContain('质检记录');
      expect(entities).toContain('生产排程');
    });

    it('架构设计表名符合UPPER_SNAKE（MES_WORK_ORDER等）', async () => {
      mock.setResponse('MES', JSON.stringify(MES_ARCH));
      const result = await archAgent.design('MES制造执行系统');
      const tables = result.data.architecture.databaseDesign.tables;
      expect(tables.length).toBe(4);
      for (const t of tables) {
        expect(t.name).toBe(t.name.toUpperCase()); // 全大写
        expect(t.name).toMatch(/^MES_/); // MES_前缀
      }
    });

    it('数据库设计自动注入TenantId+审计字段', async () => {
      mock.setResponse('MES', JSON.stringify(DB_MOCK));
      const result = await dbAgent.design('MES数据库');
      const cols = result.data.tables[0].columns.map(c => c.name);
      expect(cols).toContain('F_TENANT_ID');
      expect(cols).toContain('F_CREATE_TIME');
      expect(cols).toContain('F_IS_DELETED');
    });
  });

  // ─── 智慧工地 ───
  describe('智慧工地管理系统', () => {
    it('识别安全帽IoT告警实体', async () => {
      mock.setResponse('智慧工地', JSON.stringify(SITE_REQ));
      const state = createInitialState();
      const result = await orch.advance(state, '智慧工地管理系统');
      const req = result.requirement as RequirementResult;
      const entities = req.proposedDomainModel.entities.map(e => e.name);
      expect(entities).toContain('安全帽');
      expect(entities).toContain('塔吊');
      expect(entities).toContain('施工进度');
    });

    it('架构包含dashboard类型页面（大屏）', async () => {
      mock.setResponse('智慧工地', JSON.stringify(SITE_ARCH));
      const result = await archAgent.design('智慧工地系统');
      const pages = result.data.architecture.uiDesign.pages;
      expect(pages.some(p => p.type === 'dashboard')).toBe(true);
    });

    it('数据库表名SITE_前缀大写', async () => {
      mock.setResponse('智慧工地', JSON.stringify(SITE_ARCH));
      const result = await archAgent.design('智慧工地系统');
      for (const t of result.data.architecture.databaseDesign.tables) {
        expect(t.name).toBe(t.name.toUpperCase());
        expect(t.name).toMatch(/^SITE_/);
      }
    });
  });

  // ─── 智能更衣柜 ───
  describe('智能更衣柜系统 (Foundry自博弈)', () => {
    it('识别IoT设备领域模型（柜门/借用/报警/支付）', async () => {
      mock.setResponse('更衣柜', JSON.stringify(LOCKER_REQ));
      const state = createInitialState();
      const result = await orch.advance(state, '智能更衣柜系统，柜门开关控制、借用归还');
      const req = result.requirement as RequirementResult;
      const entities = req.proposedDomainModel.entities.map(e => e.name);
      expect(entities).toContain('柜门');
      expect(entities).toContain('借用记录');
      expect(entities).toContain('异常报警');
      expect(entities).toContain('支付记录');
    });

    it('业务规则识别超时归还告警', async () => {
      mock.setResponse('更衣柜', JSON.stringify(LOCKER_REQ));
      const state = createInitialState();
      const result = await orch.advance(state, '智能更衣柜系统');
      const req = result.requirement as RequirementResult;
      const rules = req.proposedDomainModel.businessRules;
      expect(rules.some(r => r.name.includes('超时') || r.condition.includes('24h'))).toBe(true);
    });

    it('隐含需求识别IoT通信（MQTT）', async () => {
      mock.setResponse('更衣柜', JSON.stringify(LOCKER_REQ));
      const state = createInitialState();
      const result = await orch.advance(state, '智能更衣柜系统');
      const req = result.requirement as RequirementResult;
      expect(req.implicitRequirements.some(i => i.includes('MQTT') || i.includes('IoT') || i.includes('设备通信'))).toBe(true);
    });
  });

  // ─── 全链路（由 e2e-pipeline.test.ts 覆盖，本测试聚焦单阶段验证） ───
  it.skip('MES全链路5阶段通过', async () => {
    mock.setResponse('MES制造', JSON.stringify(MES_REQ));
    mock.setResponse('strategies', JSON.stringify(MES_ARCH));
    mock.setResponse('架构设计', JSON.stringify(UI_MOCK));
    mock.setResponse('tables', JSON.stringify(DB_MOCK));

    let s = createInitialState();
    s = await orch.advance(s, 'MES制造执行系统');
    expect(s.requirement).toBeDefined();
    s = await orch.confirm(s);
    s = await orch.advance(s);
    expect(s.architecture).toBeDefined();
    s = await orch.confirm(s);
    s = await orch.advance(s);
    expect(s.design).toBeDefined();
    s = await orch.confirm(s);
    s = await orch.advance(s);
    expect(s.development).toBeDefined();
    s = await orch.confirm(s);
    s = await orch.advance(s);
    s = await orch.confirm(s);
    expect(s.status).toBe('completed');
  });
});
