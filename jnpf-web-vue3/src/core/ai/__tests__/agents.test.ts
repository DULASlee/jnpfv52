/**
 * 智能体单元测试
 *
 * 使用 MockLLMGateway 预设响应，不真实调用 LLM API。
 */

import { describe, it, expect, beforeEach } from 'vitest';
import { BaseAgent, type AgentContext } from '../agents/base';
import { RequirementAnalystAgent } from '../agents/requirement-analyst';
import { ArchitectAgent, type ArchitectureDesign } from '../agents/architect';
import { MockLLMGateway } from './mock-llm';
import { REQUIREMENT_ANALYST_PROMPT } from '../llm/prompts';
import type { LLMGateway } from '../llm/types';

// ============================================================
// 测试用具体子类（暴露 protected 方法）
// ============================================================

class TestAgent extends BaseAgent {
  constructor(llm: LLMGateway) {
    super(llm, REQUIREMENT_ANALYST_PROMPT);
  }
  public testParse<T>(content: string): T {
    return this.parseResponse<T>(content);
  }
  public testBuildPrompt(context: AgentContext): string {
    return this.buildSystemPrompt(context);
  }
}

// ============================================================
// parseResponse 测试
// ============================================================

describe('BaseAgent.parseResponse', () => {
  let agent: TestAgent;

  beforeEach(() => {
    agent = new TestAgent(new MockLLMGateway());
  });

  it('解析纯 JSON', () => {
    const result = agent.testParse<{ name: string }>('{"name":"测试"}');
    expect(result.name).toBe('测试');
  });

  it('解析 Markdown 包裹的 JSON', () => {
    const result = agent.testParse<{ name: string }>('```json\n{"name":"测试"}\n```');
    expect(result.name).toBe('测试');
  });

  it('解析 Markdown 无 lang 标注的 JSON', () => {
    const result = agent.testParse<{ name: string }>('```\n{"name":"测试"}\n```');
    expect(result.name).toBe('测试');
  });

  it('解析混合文本中的 JSON', () => {
    const result = agent.testParse<{ name: string }>('分析结果如下：\n{"name":"测试"}\n以上是分析。');
    expect(result.name).toBe('测试');
  });

  it('解析嵌套 JSON 对象', () => {
    const result = agent.testParse<{ user: { name: string; age: number } }>('{"user":{"name":"张三","age":25}}');
    expect(result.user.name).toBe('张三');
    expect(result.user.age).toBe(25);
  });

  it('无法解析时抛出异常', () => {
    expect(() => {
      agent.testParse('这是纯文本，没有JSON');
    }).toThrow('[BaseAgent]');
  });

  it('解析 JSON 数组', () => {
    const result = agent.testParse<number[]>('[1,2,3]');
    expect(result).toEqual([1, 2, 3]);
  });
});

// ============================================================
// 需求分析师测试
// ============================================================

describe('需求分析师', () => {
  let mockLLM: MockLLMGateway;
  let agent: RequirementAnalystAgent;

  const basicAnalysis = {
    understanding: '一个学生管理系统',
    questions: ['需要支持选课吗？'],
    proposedDomainModel: {
      entities: [{ name: '学生', fields: [{ name: '姓名', type: 'string' }] }],
      relationships: [],
      businessRules: [],
    },
    strategies: [
      {
        name: '标准CRUD',
        description: '标准增删改查',
        pros: ['简单'],
        cons: ['功能有限'],
        impact: '低',
      },
    ],
    userStories: [
      {
        role: '管理员',
        action: '添加学生',
        goal: '管理学生信息',
        acceptance: '能成功添加学生',
      },
    ],
    implicitRequirements: ['权限控制'],
    risks: ['数据安全'],
  };

  beforeEach(() => {
    mockLLM = new MockLLMGateway();
    agent = new RequirementAnalystAgent(mockLLM);
  });

  it('analyze 返回 RequirementAnalysis 结构', async () => {
    mockLLM.setResponse('学生管理', JSON.stringify(basicAnalysis));

    const result = await agent.analyze('做一个学生管理系统');

    expect(result.data.understanding).toBeDefined();
    expect(result.data.questions.length).toBe(1);
    expect(result.data.proposedDomainModel.entities.length).toBe(1);
    expect(result.data.strategies.length).toBe(1);
    expect(result.data.userStories.length).toBe(1);
    expect(result.confidence).toBeGreaterThanOrEqual(0.7);
  });

  it('followUp 继承上一轮上下文', async () => {
    const updatedAnalysis = {
      ...basicAnalysis,
      questions: [],
      proposedDomainModel: {
        entities: [
          { name: '学生', fields: [{ name: '姓名', type: 'string' }] },
          { name: '课程', fields: [{ name: '课程名', type: 'string' }] },
        ],
        relationships: [{ from: '学生', to: '课程', type: 'many-to-many' }],
        businessRules: [],
      },
    };

    mockLLM.setResponse('补充以下信息', JSON.stringify(updatedAnalysis));

    const result = await agent.followUp({ '需要支持选课吗？': '是的，学生可以选修多门课程' }, basicAnalysis);

    expect(result.data.proposedDomainModel.entities.length).toBe(2);
    expect(result.data.proposedDomainModel.relationships.length).toBe(1);
  });
});

// ============================================================
// 架构师测试
// ============================================================

describe('架构师', () => {
  let mockLLM: MockLLMGateway;
  let agent: ArchitectAgent;

  const basicArchitecture: ArchitectureDesign = {
    overview: '学生管理系统架构',
    architecture: {
      modules: [{ name: 'Student', responsibility: '学生管理', dependencies: [] }],
      databaseDesign: {
        tables: [
          {
            name: 'base_student',
            comment: '学生表',
            columns: [
              { name: 'ID', type: 'BIGINT', nullable: false, comment: '主键' },
              { name: 'NAME', type: 'NVARCHAR', length: 100, nullable: false, comment: '姓名' },
            ],
            indexes: [],
          },
        ],
      },
      apiDesign: {
        endpoints: [{ path: '/api/student/list', method: 'GET', description: '学生列表' }],
      },
      uiDesign: {
        pages: [{ name: '学生管理', type: 'list', fields: ['name'] }],
      },
    },
    irPages: [],
    techStack: {
      framework: '.NET 8 + JNPF',
      ui: 'Vue3 + Ant Design Vue',
      database: 'SQL Server',
      cache: 'Memory Cache',
      mq: 'Channel',
    },
    decisions: [{ decision: '使用雪花ID', reason: '分布式唯一', alternatives: ['GUID', '自增'] }],
  };

  beforeEach(() => {
    mockLLM = new MockLLMGateway();
    agent = new ArchitectAgent(mockLLM);
  });

  it('design 自动注入多租户字段', async () => {
    mockLLM.setResponse('学生管理系统', JSON.stringify(basicArchitecture));

    const result = await agent.design('设计一个学生管理系统');

    const tables = result.data.architecture.databaseDesign.tables;
    expect(tables.length).toBe(1);

    const studentTable = tables[0];
    const columnNames = studentTable.columns.map(c => c.name);

    // 自动注入的字段
    expect(columnNames.some(c => c.toUpperCase().includes('TENANT_ID'))).toBe(true);
    expect(columnNames.some(c => c.toUpperCase().includes('CREATE_USER_ID'))).toBe(true);
    expect(columnNames.some(c => c.toUpperCase().includes('CREATE_TIME'))).toBe(true);
    expect(columnNames.some(c => c.toUpperCase().includes('MODIFY_USER_ID'))).toBe(true);
    expect(columnNames.some(c => c.toUpperCase().includes('MODIFY_TIME'))).toBe(true);
    expect(columnNames.some(c => c.toUpperCase().includes('IS_DELETED'))).toBe(true);
  });

  it('design 自动注入主键 F_ID（如果缺失）', async () => {
    mockLLM.setResponse('学生管理系统', JSON.stringify(basicArchitecture));

    const result = await agent.design('设计一个学生管理系统');

    const columns = result.data.architecture.databaseDesign.tables[0].columns;
    const hasId = columns.some(c => c.name === 'F_ID');
    expect(hasId).toBe(true);
  });

  it('design 规范化表名为大写', async () => {
    mockLLM.setResponse('学生管理系统', JSON.stringify(basicArchitecture));

    const result = await agent.design('设计一个学生管理系统');

    const tableName = result.data.architecture.databaseDesign.tables[0].name;
    expect(tableName).toBe('BASE_STUDENT'); // 原 'base_student' → 'BASE_STUDENT'
  });

  it('design 规范化列名添加 F_ 前缀', async () => {
    mockLLM.setResponse('学生管理系统', JSON.stringify(basicArchitecture));

    const result = await agent.design('设计一个学生管理系统');

    const columns = result.data.architecture.databaseDesign.tables[0].columns;
    // 原 'NAME' → 'F_NAME'
    const nameCol = columns.find(c => c.name === 'F_NAME');
    expect(nameCol).toBeDefined();
  });

  it('optimize 根据反馈修改设计', async () => {
    const updatedArchitecture = {
      ...basicArchitecture,
      overview: '优化后的学生管理系统架构',
      architecture: {
        ...basicArchitecture.architecture,
        modules: [...basicArchitecture.architecture.modules, { name: 'Course', responsibility: '课程管理', dependencies: ['Student'] }],
      },
    };

    mockLLM.setResponse('增加课程管理', JSON.stringify(updatedArchitecture));

    const result = await agent.optimize('增加课程管理模块', basicArchitecture);

    expect(result.data.architecture.modules.length).toBe(2);
  });
});
