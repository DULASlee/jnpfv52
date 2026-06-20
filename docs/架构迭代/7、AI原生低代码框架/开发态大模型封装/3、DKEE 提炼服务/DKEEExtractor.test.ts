// DKEE 单元测试 - 验证跨项目 Pattern 提炼逻辑

import { InMemoryDKEEQueries } from '../src/dkee/PatternQueries';
import { PatternExtractor } from '../src/dkee/PatternExtractor';
import { PatternRankingService } from '../src/dkee/PatternRankingService';
import { DictSourceRecord, DecisionTableSourceRecord } from '../src/dkee/PatternTypes';

describe('DKEE PatternExtractor', () => {
  let queries: InMemoryDKEEQueries;
  let extractor: PatternExtractor;

  beforeEach(() => {
    queries = new InMemoryDKEEQueries();
    extractor = new PatternExtractor(queries, 2);  // threshold=2 便于测试
  });

  // ========================================================
  // 1. 字段命名 Pattern
  // ========================================================
  describe('字段命名 Pattern 提炼', () => {
    it('提炼:3 个项目有共同字段 WorkOrderId', async () => {
      const records: DictSourceRecord[] = [
        {
          id: 1, project_id: 101,
          elements: [
            { name: 'WorkOrderId', type: 'BIGINT', isFK: true, refEntity: 'WorkOrder' },
            { name: 'Qty', type: 'DECIMAL(18,2)' },
          ],
          data_flows: [], data_stores: [], tags: ['manufacturing'], pattern_tags: [],
        },
        {
          id: 2, project_id: 102,
          elements: [
            { name: 'WorkOrderId', type: 'BIGINT', isFK: true, refEntity: 'WorkOrder' },
            { name: 'ScrapQty', type: 'DECIMAL(18,2)' },
          ],
          data_flows: [], data_stores: [], tags: ['manufacturing'], pattern_tags: [],
        },
        {
          id: 3, project_id: 103,
          elements: [
            { name: 'WorkOrderId', type: 'BIGINT', isFK: true, refEntity: 'WorkOrder' },
            { name: 'OperatorId', type: 'NVARCHAR(50)' },
          ],
          data_flows: [], data_stores: [], tags: ['manufacturing'], pattern_tags: [],
        },
      ];
      queries.injectDictRecords(records);

      const result = await extractor.extractFieldNamingPatterns('manufacturing', 'human-created');

      expect(result.newPatterns).toHaveLength(1);
      const pattern = result.newPatterns[0];
      expect(pattern.type).toBe('field_naming');
      expect(pattern.industry).toBe('manufacturing');
      expect(pattern.fieldCount).toBe(1);  // 只有 WorkOrderId 出现 3 次(>= threshold 2)
      expect(pattern.commonFields[0].name).toBe('WorkOrderId');
      expect(pattern.commonFields[0].frequency).toBe(3);
    });

    it('过滤:只有 1 个项目的字段不进 Pattern', async () => {
      const records: DictSourceRecord[] = [
        {
          id: 1, project_id: 101,
          elements: [{ name: 'UniqueField', type: 'NVARCHAR(50)' }],
          data_flows: [], data_stores: [], tags: [], pattern_tags: [],
        },
      ];
      queries.injectDictRecords(records);
      const result = await extractor.extractFieldNamingPatterns('manufacturing', 'human-created');
      expect(result.newPatterns).toHaveLength(0);
    });
  });

  // ========================================================
  // 2. 业务规则 Pattern
  // ========================================================
  describe('业务规则 Pattern 提炼', () => {
    it('提炼:3 个项目都有"报废率>5% → 让步接收"规则', async () => {
      const records: DecisionTableSourceRecord[] = [
        {
          id: 1, project_id: 101, cross_event_consistency: true,
          tables: [{
            id: 'DT-1',
            conditions: [{ name: '报废率>5%', operator: '>', value: 0.05 }],
            actions: [{ name: '让步接收' }, { name: '驳回' }],
            rules: [
              { conditionMask: [true], actionIndex: 0 },
              { conditionMask: [false], actionIndex: 1 },
            ],
          }],
        },
        {
          id: 2, project_id: 102, cross_event_consistency: true,
          tables: [{
            id: 'DT-1',
            conditions: [{ name: '报废率>5%', operator: '>', value: 0.05 }],
            actions: [{ name: '让步接收' }, { name: '驳回' }],
            rules: [
              { conditionMask: [true], actionIndex: 0 },
              { conditionMask: [false], actionIndex: 1 },
            ],
          }],
        },
        {
          id: 3, project_id: 103, cross_event_consistency: true,
          tables: [{
            id: 'DT-1',
            conditions: [{ name: '报废率>5%', operator: '>', value: 0.05 }],
            actions: [{ name: '让步接收' }, { name: '驳回' }],
            rules: [
              { conditionMask: [true], actionIndex: 0 },
              { conditionMask: [false], actionIndex: 1 },
            ],
          }],
        },
      ];
      queries.injectDecisionTableRecords(records);

      const result = await extractor.extractDecisionRulePatterns('manufacturing', 'human-created');

      expect(result.newPatterns).toHaveLength(1);
      const pattern = result.newPatterns[0];
      expect(pattern.type).toBe('decision_rule');
      expect(pattern.hasDefaultRule).toBe(true);
      expect(pattern.ruleSet).toContainEqual(
        expect.objectContaining({
          condition: '报废率>5%',
          threshold: 0.05,
          action: '让步接收',
          frequency: 3,
        })
      );
    });

    it('检测兜底规则', async () => {
      const records: DecisionTableSourceRecord[] = [
        {
          id: 1, project_id: 101, cross_event_consistency: true,
          tables: [{
            id: 'DT-1',
            conditions: [{ name: 'X', operator: '>', value: 1 }],
            actions: [{ name: 'pass' }, { name: 'default' }],
            rules: [
              { conditionMask: [true], actionIndex: 0 },
              { conditionMask: [false], actionIndex: 1 },  // 兜底
            ],
          }],
        },
        {
          id: 2, project_id: 102, cross_event_consistency: true,
          tables: [{
            id: 'DT-2',
            conditions: [{ name: 'X', operator: '>', value: 1 }],
            actions: [{ name: 'pass' }, { name: 'default' }],
            rules: [
              { conditionMask: [true], actionIndex: 0 },
              { conditionMask: [false], actionIndex: 1 },
            ],
          }],
        },
      ];
      queries.injectDecisionTableRecords(records);
      const result = await extractor.extractDecisionRulePatterns('manufacturing', 'human-created');
      expect(result.newPatterns[0].hasDefaultRule).toBe(true);
    });
  });

  // ========================================================
  // 3. 跨 Pattern 评分更新
  // ========================================================
  describe('PatternRankingService', () => {
    it('使用后根据成功率动态更新评分', async () => {
      const ranker = new PatternRankingService(queries);
      // 模拟 Pattern 被使用,1 次成功 1 次失败
      await queries.savePattern(
        {
          type: 'field_naming', industry: 'manufacturing', source: 'human-created',
          sourceProjects: [1, 2, 3], sourceRecords: [],
          commonFields: [{ name: 'X', type: 'NVARCHAR', frequency: 3, isFK: false, isRequired: true }],
          fieldCount: 1, minOccurrenceThreshold: 2,
        } as any,
        0.5,  // 初始分
        [1, 2, 3]
      );

      await ranker.updateScoresAfterUsage([
        { patternId: 1, projectId: 101, isSuccess: true },
        { patternId: 1, projectId: 102, isSuccess: false },
      ]);

      const patterns = queries.getAllPatterns();
      const updated = patterns.find(p => p.id === 1);
      expect(updated.usage_count).toBe(2);
      expect(updated.success_count).toBe(1);
    });

    it('Top N 选取:按 score 降序,过滤低分', async () => {
      const ranker = new PatternRankingService(queries);
      await queries.savePattern(
        { type: 'field_naming', industry: 'manufacturing', source: 'human-created',
          sourceProjects: [1, 2], sourceRecords: [], commonFields: [], fieldCount: 0, minOccurrenceThreshold: 2 } as any,
        0.8, [1, 2]
      );
      await queries.savePattern(
        { type: 'field_naming', industry: 'manufacturing', source: 'human-created',
          sourceProjects: [1, 2, 3], sourceRecords: [], commonFields: [], fieldCount: 0, minOccurrenceThreshold: 2 } as any,
        0.4, [1, 2, 3]  // 低于 0.6 门禁
      );
      await queries.savePattern(
        { type: 'decision_rule', industry: 'manufacturing', source: 'human-created',
          sourceProjects: [1, 2], sourceRecords: [], ruleSet: [], hasDefaultRule: true, ruleCount: 0 } as any,
        0.9, [1, 2]
      );

      const top = await ranker.getTopPatternsForContext('manufacturing', ['field_naming', 'decision_rule'], 5);
      expect(top).toHaveLength(2);  // 0.8 + 0.9 通过,0.4 被过滤
      expect(top[0].type).toBe('decision_rule');  // 0.9 排第一
    });
  });

  // ========================================================
  // 4. 主流程:extractAll
  // ========================================================
  describe('extractAll 集成', () => {
    it('一次提炼出 3 类 Pattern', async () => {
      queries.injectDictRecords([
        {
          id: 1, project_id: 101,
          elements: [{ name: 'WorkOrderId', type: 'BIGINT' }],
          data_flows: [], data_stores: [], tags: [], pattern_tags: [],
        },
        {
          id: 2, project_id: 102,
          elements: [{ name: 'WorkOrderId', type: 'BIGINT' }],
          data_flows: [], data_stores: [], tags: [], pattern_tags: [],
        },
      ]);
      queries.injectDecisionTableRecords([
        {
          id: 1, project_id: 101, cross_event_consistency: true,
          tables: [{
            id: 'DT',
            conditions: [{ name: 'X', operator: '>', value: 1 }],
            actions: [{ name: 'A' }, { name: 'B' }],
            rules: [{ conditionMask: [true], actionIndex: 0 }, { conditionMask: [false], actionIndex: 1 }],
          }],
        },
        {
          id: 2, project_id: 102, cross_event_consistency: true,
          tables: [{
            id: 'DT',
            conditions: [{ name: 'X', operator: '>', value: 1 }],
            actions: [{ name: 'A' }, { name: 'B' }],
            rules: [{ conditionMask: [true], actionIndex: 0 }, { conditionMask: [false], actionIndex: 1 }],
          }],
        },
      ]);

      const result = await extractor.extractAll('manufacturing');
      expect(result.patternsExtracted).toBeGreaterThan(0);
      expect(result.newPatterns.some(p => p.type === 'field_naming')).toBe(true);
      expect(result.newPatterns.some(p => p.type === 'decision_rule')).toBe(true);
    });
  });
});
