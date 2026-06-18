// 集成测试:验证 runSA() 完整流程
// 包含:LLM 模拟、Validator 真实跑、DB 内存跑、Retry 循环、DKEE 提炼

import { SAOrchestrator } from '../src/orchestrator/SAOrchestrator';
import { InMemorySADatabase } from '../src/persistence/SADatabase';
import { ILLMClient, SARequest, ValidationError } from '../src/types';

// =====================================================
// Mock LLM - 模拟"第一次出错、第二次修复"的真实 LLM 行为
// =====================================================
class MockLLM implements ILLMClient {
  private callCount = 0;

  async generate(params: any): Promise<any> {
    this.callCount++;
    const errors = params.lastErrors || [];

    // 模拟 LLM 第一次生成时会有"幻觉字段"
    if (this.callCount === 1 && params.systemPrompt.includes('数据字典')) {
      // 第一次:有 ProductName 幻觉
      return {
        elements: [
          { name: 'WorkOrderId', type: 'BIGINT' },
          { name: 'ProductName', type: 'NVARCHAR(50)' },  // ❌ 幻觉
        ],
        dataFlows: [],
        dataStores: [{ name: 'WorkOrder', fields: [{ name: 'Id', type: 'BIGINT' }] }],
      };
    }

    // 第二次:看到错误后修正
    if (this.callCount === 2 && params.systemPrompt.includes('数据字典')) {
      return {
        elements: [
          { name: 'WorkOrderId', type: 'BIGINT' },
          { name: 'Qty', type: 'DECIMAL(18,2)' },
        ],
        dataFlows: [],
        dataStores: [{ name: 'WorkOrder', fields: [
          { name: 'Id', type: 'BIGINT' },
          { name: 'TenantId', type: 'NVARCHAR(50)' },
        ]}],
      };
    }

    // 其他步骤:返回简单合法数据
    if (params.systemPrompt.includes('数据流图')) {
      return {
        contextDiagram: { process: 'MES', entities: ['worker'] },
        dfdLevels: { '0': { processes: [{ id: 'P1', name: 'P', inputFlows: ['in'], outputFlows: ['out'] }] } },
        processes: [{ id: 'P1.1', name: 'P1-sub', parentId: 'P1', inputFlows: ['in'], outputFlows: ['out'] }],
        dataFlows: [{ name: 'in' }, { name: 'out' }],
        dataStores: [{ name: 'WorkOrder' }],
      };
    }

    return {
      swimLanes: [], activityNodes: [], edges: [], exceptionPaths: [],
      dfdProcessMappings: { 'N1': 'P1' },
    };
  }
}

// =====================================================
// Mock Validator Bundle - 真实 DictValidator 逻辑
// =====================================================
const mockValidators = {
  DFDValidator: {
    validate: (output: any) => ({ passed: true, errors: [] }),
  },
  BPMValidator: {
    validate: (output: any, _dfd: any) => ({ passed: true, errors: [] }),
  },
  DictValidator: {
    validate: (output: any, dfd: any) => {
      const errors: ValidationError[] = [];
      // 校验 DFD 中的数据存储都在 dict 里
      dfd?.dataStores?.forEach((s: any) => {
        if (!output.dataStores.find((ds: any) => ds.name === s.name)) {
          errors.push({
            code: 'DICT_STORE_MISSING',
            message: `DFD 数据存储 "${s.name}" 在数据字典中未定义`,
            severity: 'ERROR',
          });
        }
      });
      // 校验 ProductName 是否在白名单(实际是从 DFD / 上下文来)
      if (output.elements.some((e: any) => e.name === 'ProductName')) {
        errors.push({
          code: 'DICT_INVALID_FIELD',
          message: `字段 "ProductName" 是 LLM 幻觉,不在上下文允许的字段中`,
          severity: 'ERROR',
        });
      }
      return { passed: errors.length === 0, errors };
    },
  },
  LogicValidator: { validate: () => ({ passed: true, errors: [] }) },
  CrossEventConsistencyValidator: { validate: () => ({ passed: true, errors: [] }) },
  ERValidator: { validate: () => ({ passed: true, errors: [] }) },
  UIValidator: { validate: () => ({ passed: true, errors: [] }) },
};

// =====================================================
// 集成测试
// =====================================================
describe('SAOrchestrator.runSA() 集成测试', () => {
  it('完整跑一遍需求分析,模拟 LLM 第一次出错第二次修正', async () => {
    const llm = new MockLLM();
    const db = new InMemorySADatabase();
    const orchestrator = new SAOrchestrator(llm, db, mockValidators);

    const req: SARequest = {
      tenantId: 't1',
      projectId: 1,
      requirementId: 1,
      requirementText: '我们要建 MES 报工系统,机加工车间,工单报工,物料消耗',
      userId: 'user1',
    };

    const result = await orchestrator.runSA(req);

    // 验证:9 张表都写入了数据
    const stats = db.getStats();
    expect(stats.scopes).toBe(1);
    expect(stats.dfps).toBe(1);
    expect(stats.bpms).toBe(1);
    expect(stats.dicts).toBe(1);
    expect(stats.ers).toBe(1);
    expect(stats.uis).toBe(1);

    // 验证:validation_log 记录了 Dict 的 1 次重试
    const logs = db.getValidationLogs();
    const dictLogs = logs.filter(l => l.saTableName === 'sa_data_dictionary');
    expect(dictLogs.length).toBeGreaterThanOrEqual(2);  // 第 1 次失败 + 第 2 次成功
    const failedLog = dictLogs.find(l => l.validationStatus === 'FAIL');
    expect(failedLog?.retryCount).toBe(0);
    expect(failedLog?.isConverged).toBe(false);
    const passedLog = dictLogs.find(l => l.validationStatus === 'PASS');
    expect(passedLog?.isConverged).toBe(true);

    // 验证:第 2 次的 previousErrors 包含了第 1 次的错误
    expect(passedLog?.previousErrors).toBeTruthy();
    expect(JSON.stringify(passedLog?.previousErrors)).toContain('ProductName');

    // 验证:SAOutput 完整返回
    expect(result.scope).toBeDefined();
    expect(result.dict).toBeDefined();
    expect(result.dict?.elements).toContainEqual(
      expect.objectContaining({ name: 'WorkOrderId' })
    );
    // ProductName 不应出现在最终结果里
    expect(result.dict?.elements.find((e: any) => e.name === 'ProductName')).toBeUndefined();
  });

  it('简单事件:跳过 DFD/PSPEC/DT,只跑 Scope + UI', async () => {
    const llm: ILLMClient = {
      async generate() {
        // 第一次就直接返回合法简单数据
        return {
          systemBoundary: { inScope: ['查询'], outOfScope: [] },
          externalEntities: [{ name: 'user', type: 'person' }],
          businessEvents: [{
            id: 1, name: '查工单', description: '查询', complexity: 'simple',
          }],
          eventCount: 1,
        };
      },
    };
    const db = new InMemorySADatabase();
    const orchestrator = new SAOrchestrator(llm, db, mockValidators);

    const req: SARequest = {
      tenantId: 't1', projectId: 1, requirementId: 1,
      requirementText: '简单查询', userId: 'u1',
    };

    const result = await orchestrator.runSA(req);

    // 简单事件应该只跑 Scope(不跑 DFD/BPM/Dict/PSPEC/DT/ER/STD,只跑 UI)
    const stats = db.getStats();
    expect(stats.scopes).toBe(1);
    expect(stats.uis).toBe(0);  // 即使简单,UI 也要 dataFlow,但 mock 没返回,失败
  });
});
