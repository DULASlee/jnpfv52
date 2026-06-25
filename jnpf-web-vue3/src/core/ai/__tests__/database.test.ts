/**
 * 数据库设计智能体单元测试
 */

import { describe, it, expect, beforeEach } from 'vitest';
import { DatabaseAgent, type DatabaseDesign } from '../agents/database';
import { MockLLMGateway } from './mock-llm';

describe('数据库设计智能体', () => {
  let mockLLM: MockLLMGateway;
  let agent: DatabaseAgent;

  const basicDBDesign: DatabaseDesign = {
    overview: '学生管理系统数据库设计',
    tables: [
      {
        name: 'base_student',
        comment: '学生表',
        columns: [
          { name: 'ID', type: 'BIGINT', nullable: false, comment: 'ID' },
          { name: 'NAME', type: 'NVARCHAR', length: 100, nullable: false, comment: '姓名' },
          { name: 'AGE', type: 'INT', nullable: true, comment: '年龄' },
        ],
        indexes: [{ name: 'idx_name', columns: ['NAME'], unique: false }],
      },
    ],
    migrationSql: 'CREATE TABLE BASE_STUDENT (...);',
    apis: [
      {
        path: '/api/student/list',
        method: 'GET',
        description: '学生列表',
        requireAuth: true,
        permissionCode: 'student.list',
      },
      {
        path: 'student/create',
        method: 'POST' as const,
        description: '创建学生',
        requireAuth: true,
      },
    ],
  };

  beforeEach(() => {
    mockLLM = new MockLLMGateway();
    agent = new DatabaseAgent(mockLLM);
  });

  it('design 返回 DatabaseDesign 结构', async () => {
    mockLLM.setResponse('学生管理', JSON.stringify(basicDBDesign));

    const result = await agent.design('学生管理系统的数据库设计');

    expect(result.data.tables.length).toBeGreaterThanOrEqual(1);
    expect(result.data.tables[0].name).toBeDefined();
    expect(result.data.apis.length).toBeGreaterThanOrEqual(1);
  });

  it('自动注入 TENANT_ID 字段', async () => {
    mockLLM.setResponse('学生管理', JSON.stringify(basicDBDesign));

    const result = await agent.design('学生管理系统的数据库设计');

    const columns = result.data.tables[0].columns;
    const tenantCol = columns.find(c => c.name === 'F_TENANT_ID');
    expect(tenantCol).toBeDefined();
    expect(tenantCol!.type).toBe('NVARCHAR');
    expect(tenantCol!.nullable).toBe(false);
  });

  it('自动注入审计字段', async () => {
    mockLLM.setResponse('学生管理', JSON.stringify(basicDBDesign));

    const result = await agent.design('学生管理系统的数据库设计');

    const columnNames = result.data.tables[0].columns.map(c => c.name);
    expect(columnNames).toContain('F_CREATE_USER_ID');
    expect(columnNames).toContain('F_CREATE_TIME');
    expect(columnNames).toContain('F_MODIFY_USER_ID');
    expect(columnNames).toContain('F_MODIFY_TIME');
    expect(columnNames).toContain('F_IS_DELETED');
  });

  it('自动注入主键 F_ID', async () => {
    mockLLM.setResponse('学生管理', JSON.stringify(basicDBDesign));

    const result = await agent.design('学生管理系统的数据库设计');

    const firstCol = result.data.tables[0].columns[0];
    expect(firstCol.name).toBe('F_ID');
    expect(firstCol.type).toBe('BIGINT');
  });

  it('表名规范化为大写', async () => {
    mockLLM.setResponse('学生管理', JSON.stringify(basicDBDesign));

    const result = await agent.design('学生管理系统的数据库设计');

    expect(result.data.tables[0].name).toBe('BASE_STUDENT');
  });

  it('列名添加 F_ 前缀并大写', async () => {
    mockLLM.setResponse('学生管理', JSON.stringify(basicDBDesign));

    const result = await agent.design('学生管理系统的数据库设计');

    const columns = result.data.tables[0].columns;
    const nameCol = columns.find(c => c.name === 'F_NAME');
    expect(nameCol).toBeDefined();
    expect(nameCol!.type).toBe('NVARCHAR');
  });

  it('索引名规范化为 IDX_ 前缀 + 大写', async () => {
    mockLLM.setResponse('学生管理', JSON.stringify(basicDBDesign));

    const result = await agent.design('学生管理系统的数据库设计');

    const idx = result.data.tables[0].indexes[0];
    expect(idx.name).toContain('IDX_');
    expect(idx.name).toBe(idx.name.toUpperCase());
  });

  it('API 路径自动补全 /api/ 前缀', async () => {
    mockLLM.setResponse('学生管理', JSON.stringify(basicDBDesign));

    const result = await agent.design('学生管理系统的数据库设计');

    const createApi = result.data.apis.find(a => a.description === '创建学生');
    expect(createApi).toBeDefined();
    expect(createApi!.path.startsWith('/api/')).toBe(true);
  });

  it('API 方法名规范化为大写', async () => {
    mockLLM.setResponse('学生管理', JSON.stringify(basicDBDesign));

    const result = await agent.design('学生管理系统的数据库设计');

    const createApi = result.data.apis.find(a => a.description === '创建学生');
    expect(createApi!.method).toBe('POST');
  });

  it('API requireAuth 默认 true', async () => {
    mockLLM.setResponse('学生管理', JSON.stringify(basicDBDesign));

    const result = await agent.design('学生管理系统的数据库设计');

    const createApi = result.data.apis.find(a => a.description === '创建学生');
    expect(createApi!.requireAuth).toBe(true);
  });
});
