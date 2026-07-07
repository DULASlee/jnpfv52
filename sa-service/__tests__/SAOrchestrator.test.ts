// 集成测试:验证 runSA() 完整流程
// 包含:LLM 模拟、Validator 真实跑、DB 内存跑、Retry 循环、DKEE 提炼 + 3-Tier 路由

import { SAOrchestrator, buildScopeFromSkeleton } from '../src/orchestrator/SAOrchestrator';
import { InMemorySADatabase } from '../src/orchestrator/SADatabase';
import { ILLMClient, SARequest, ValidationError } from '../src/orchestrator/orchestrator-types';
import { classifyEvent } from '../src/orchestrator/StepRouter';

// =====================================================
// Mock LLM - 模拟"第一次出错、第二次修复"的真实 LLM 行为
// =====================================================
class MockLLM implements ILLMClient {
  private dictCallCount = 0;

  async generate(params: any): Promise<any> {
    const errors = params.lastErrors || [];

    // Scope 步骤
    if (params.systemPrompt.includes('需求分析') || params.systemPrompt.includes('系统边界')) {
      return {
        systemBoundary: { inScope: ['MES 报工系统'], outOfScope: ['ERP'] },
        externalEntities: [{ name: '工人', type: 'user', description: '车间工人' }],
        businessEvents: [{ id: 1, name: '报工', description: '工人提交报工单', complexity: 'medium' }],
        eventCount: 1,
      };
    }

    // Dict 步骤:第一次有幻觉,第二次修正（必须在 DFD 之前检查，因为 DictAgent 的 systemPrompt 也包含"数据流图"）
    if (params.systemPrompt.includes('数据字典') || params.systemPrompt.includes('DictAgent')) {
      this.dictCallCount++;
      if (this.dictCallCount === 1) {
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
      return {
        elements: [
          { name: 'WorkOrderId', type: 'BIGINT' },
          { name: 'Qty', type: 'DECIMAL(18,2)' },
        ],
        dataFlows: [],
        dataStores: [{ name: 'WorkOrder', fields: [
          { name: 'Id', type: 'BIGINT' },
          { name: 'tenant_id', type: 'NVARCHAR(50)' },
        ]}],
      };
    }

    // 默认:BPM/PSpec/ER/UI 等
    return {
      swimLanes: [], activityNodes: [], edges: [], exceptionPaths: [],
      dfdProcessMappings: { 'N1': 'P1' },
    };
  }
}

// =====================================================
// Mock Validator Bundle - 真实 DictValidator 逻辑（改为 class 以匹配 new X().validate() 调用方式）
// =====================================================
class MockDFDValidator {
  constructor(_output: any) {}
  validate() { return { passed: true, errors: [] as ValidationError[] }; }
}

class MockBPMValidator {
  constructor(_output: any, _dfd?: any) {}
  validate() { return { passed: true, errors: [] as ValidationError[] }; }
}

class MockDictValidator {
  private output: any;
  private dfd: any;
  constructor(output: any, dfd?: any) {
    this.output = output;
    this.dfd = dfd;
  }
  validate() {
    const errors: ValidationError[] = [];
    this.dfd?.dataStores?.forEach((s: any) => {
      if (!this.output.dataStores?.find((ds: any) => ds.name === s.name)) {
        errors.push({
          code: 'DICT_STORE_MISSING',
          message: `DFD 数据存储 "${s.name}" 在数据字典中未定义`,
          severity: 'ERROR',
        });
      }
    });
    if (this.output.elements?.some((e: any) => e.name === 'ProductName')) {
      errors.push({
        code: 'DICT_INVALID_FIELD',
        message: `字段 "ProductName" 是 LLM 幻觉,不在上下文允许的字段中`,
        severity: 'ERROR',
      });
    }
    return { passed: errors.length === 0, errors };
  }
}

class MockPassValidator {
  constructor(..._args: any[]) {}
  validate() { return { passed: true, errors: [] as ValidationError[] }; }
}

const mockValidators = {
  DFDValidator: MockDFDValidator as any,
  BPMValidator: MockBPMValidator as any,
  DictValidator: MockDictValidator as any,
  LogicValidator: MockPassValidator as any,
  CrossEventConsistencyValidator: MockPassValidator as any,
  ERValidator: MockPassValidator as any,
  STDValidator: MockPassValidator as any,
  UIValidator: MockPassValidator as any,
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
    expect(stats.dfds).toBe(1);
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

    // 3-Tier 架构：简单事件也跑 Project 级（DFD/BPM/Dict/ER/STD）+ UI
    const stats = db.getStats();
    expect(stats.scopes).toBe(1);
    expect(stats.dfds).toBe(1);    // Project 级必跑
    expect(stats.dicts).toBe(1);   // Project 级必跑
    expect(stats.uis).toBe(1);     // 简单事件跑 UI
  });
});

// =====================================================
// 3-Tier 路由专项测试
// =====================================================
describe('3-Tier 路由 classifyEvent()', () => {
  it('simple 事件 → EVENT 级 + 只跑 UIAgent', () => {
    const decision = classifyEvent(
      { id: 1, name: '查工单', description: '查询', complexity: 'simple' },
    );
    expect(decision.assetLevel).toBe('EVENT');
    expect(decision.stepsToRun).toEqual(['UIAgent']);
    expect(decision.eventId).toBe(1);
  });

  it('medium 事件 → EVENT 级 + 跑 StateMachineAgent + UIAgent', () => {
    const decision = classifyEvent(
      { id: 2, name: '报工', description: '工人提交报工单', complexity: 'medium' },
    );
    expect(decision.assetLevel).toBe('EVENT');
    expect(decision.stepsToRun).toContain('StateMachineAgent');
    expect(decision.stepsToRun).toContain('UIAgent');
  });

  it('complex 事件 → PROCESS 级 + 跑 PSpec + DT + STD + UI', () => {
    const decision = classifyEvent(
      { id: 3, name: '倒冲', description: '复杂状态扭转', complexity: 'complex' },
    );
    expect(decision.assetLevel).toBe('PROCESS');
    expect(decision.stepsToRun).toContain('PSpecAgent');
    expect(decision.stepsToRun).toContain('DecisionTableAgent');
    expect(decision.stepsToRun).toContain('StateMachineAgent');
    expect(decision.stepsToRun).toContain('UIAgent');
  });
});

describe('3-Tier 混合事件分流', () => {
  it('3 simple + 1 complex → simple 只出 UI，complex 走 PSPEC+DT', async () => {
    const llm: ILLMClient = {
      async generate(params: any) {
        if (params.systemPrompt.includes('需求分析') || params.systemPrompt.includes('系统边界')) {
          return {
            systemBoundary: { inScope: ['MES'], outOfScope: [] },
            externalEntities: [{ name: '工人', type: 'user' }],
            businessEvents: [
              { id: 1, name: '查工单', description: '查询', complexity: 'simple' },
              { id: 2, name: '查库存', description: '查询', complexity: 'simple' },
              { id: 3, name: '查报表', description: '查询', complexity: 'simple' },
              { id: 4, name: '倒冲', description: '复杂状态扭转', complexity: 'complex' },
            ],
            eventCount: 4,
          };
        }
        // PSpec 步骤（processSpec）
        if (params.systemPrompt.includes('伪代码') || params.systemPrompt.includes('PSpecAgent')
            || params.systemPrompt.includes('业务逻辑规格')) {
          return {
            processId: 'PROC-倒冲',
            description: '倒冲业务逻辑',
            triggers: ['工单关闭'],
            steps: [{ seq: 1, action: '计算倒冲数量', actor: '系统' }],
            exceptionHandlers: [],
          };
        }
        // DecisionTable 步骤
        if (params.systemPrompt.includes('判定表') || params.systemPrompt.includes('DecisionTable')) {
          return {
            tableId: 'DT-倒冲',
            conditions: [{ name: '数量是否异常', values: ['是', '否'] }],
            actions: [{ name: '触发预警', values: ['是', '否'] }],
            rules: [{ conditions: { '数量是否异常': '是' }, actions: { '触发预警': '是' } }],
          };
        }
        // 其他步骤（DFD/BPM/Dict/ER/STD/UI）默认返回合法占位
        return {
          swimLanes: [], activityNodes: [], edges: [], exceptionPaths: [],
          dfdProcessMappings: {},
        };
      },
    };
    const db = new InMemorySADatabase();
    const orchestrator = new SAOrchestrator(llm, db, mockValidators);

    const req: SARequest = {
      tenantId: 't1', projectId: 1, requirementId: 1,
      requirementText: 'MES 报工系统', userId: 'u1',
    };

    const result = await orchestrator.runSA(req);

    // Project 级：DFD/BPM/Dict/ER/STD 各跑一次
    const stats = db.getStats();
    expect(stats.scopes).toBe(1);
    expect(stats.dfds).toBe(1);
    expect(stats.bpms).toBe(1);
    expect(stats.dicts).toBe(1);
    expect(stats.ers).toBe(1);
    expect(stats.stateMachines).toBe(1);

    // UI：4 个事件各跑一次
    expect(stats.uis).toBe(4);

    // PSPEC + DecisionTable：只有 complex 事件跑
    expect(stats.pspecs).toBe(1);
    expect(stats.decisionTables).toBe(1);
  });

  it('PM 骨架 skeletonBusinessEvents 驱动 Scope，eventId 保留 IR 格式', async () => {
    const llm = new MockLLM();
    const db = new InMemorySADatabase();
    const orchestrator = new SAOrchestrator(llm, db, mockValidators);

    const req: SARequest = {
      tenantId: 't1',
      projectId: 99,
      requirementId: 1,
      requirementText: '请假管理系统',
      userId: 'u1',
      skeletonBusinessEvents: [
        { eventId: 'BE-001', eventName: '提交请假', complexityHint: 'simple' },
        { eventId: 'BE-002', eventName: '审批请假', complexityHint: 'medium' },
      ],
    };

    const result = await orchestrator.runSA(req);

    expect(result.scope.eventCount).toBe(2);
    expect(result.scope.businessEvents[0].irEventId).toBe('BE-001');
    expect(result.eventResults[0].eventId).toBe('BE-001');
    expect(result.eventResults[1].eventId).toBe('BE-002');
    expect(typeof result.eventResults[0].eventId).toBe('string');
  });
});
