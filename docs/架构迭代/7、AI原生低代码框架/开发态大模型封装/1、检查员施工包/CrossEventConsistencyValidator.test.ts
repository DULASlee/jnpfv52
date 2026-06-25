// CrossEventConsistencyValidator 测试
import { CrossEventConsistencyValidator } from '../src/CrossEventConsistencyValidator';
import { DecisionTableBuilder } from './helpers/builders';

describe('CrossEventConsistencyValidator', () => {
  // ========================================================
  // 1. 跨事件条件阈值一致
  // ========================================================
  describe('跨事件条件阈值一致', () => {
    it('失败 CONSISTENCY_CONDITION_CONFLICT:同条件名不同阈值', () => {
      const table1 = new DecisionTableBuilder()
        .withCondition('报废率>5%', 0.05)
        .withAction('合格')
        .build();
      table1.id = 'DT-1';
      const table2 = new DecisionTableBuilder()
        .withCondition('报废率>5%', 0.03)  // ❌ 阈值不一致
        .withAction('合格')
        .build();
      table2.id = 'DT-2';
      const result = new CrossEventConsistencyValidator(table1, [table1, table2]).validate();
      expect(result.errors).toContainEqual(
        expect.objectContaining({ code: 'CONSISTENCY_CONDITION_CONFLICT' })
      );
    });

    it('通过:同条件名同阈值', () => {
      const table1 = new DecisionTableBuilder()
        .withCondition('报废率>5%', 0.05)
        .withAction('合格')
        .build();
      const table2 = new DecisionTableBuilder()
        .withCondition('报废率>5%', 0.05)  // ✅
        .withAction('合格')
        .build();
      const result = new CrossEventConsistencyValidator(table1, [table1, table2]).validate();
      expect(result.errors.filter(e => e.code === 'CONSISTENCY_CONDITION_CONFLICT')).toHaveLength(0);
    });
  });

  // ========================================================
  // 2. 状态值白名单
  // ========================================================
  describe('状态值白名单', () => {
    it('失败 CONSISTENCY_INVALID_STATE:状态值不在白名单', () => {
      const table = new DecisionTableBuilder()
        .withCondition('工单状态=已审批', '已审批')  // 不在白名单
        .build();
      const result = new CrossEventConsistencyValidator(
        table, [table],
        [{ condition: '工单状态', allowedValues: ['已开工', '已暂停', '已完成'] }]
      ).validate();
      expect(result.errors).toContainEqual(
        expect.objectContaining({ code: 'CONSISTENCY_INVALID_STATE' })
      );
    });

    it('通过:状态值在白名单', () => {
      const table = new DecisionTableBuilder()
        .withCondition('工单状态=已开工', '已开工')
        .build();
      const result = new CrossEventConsistencyValidator(
        table, [table],
        [{ condition: '工单状态', allowedValues: ['已开工', '已暂停'] }]
      ).validate();
      expect(result.passed).toBe(true);
    });
  });

  // ========================================================
  // 3. 动作名一致性
  // ========================================================
  describe('动作名一致性', () => {
    it('警告 CONSISTENCY_NEW_ACTION:引入新动作', () => {
      const oldTable = new DecisionTableBuilder()
        .withAction('合格')
        .withAction('驳回')
        .build();
      const newTable = new DecisionTableBuilder()
        .withAction('合格')
        .withAction('挂起')  // ⚠️ 新动作
        .build();
      const result = new CrossEventConsistencyValidator(newTable, [oldTable, newTable]).validate();
      expect(result.errors).toContainEqual(
        expect.objectContaining({ code: 'CONSISTENCY_NEW_ACTION', severity: 'WARNING' })
      );
    });

    it('通过:所有动作都在白名单', () => {
      const oldTable = new DecisionTableBuilder()
        .withAction('合格').withAction('驳回').build();
      const newTable = new DecisionTableBuilder()
        .withAction('合格').withAction('驳回').build();
      const result = new CrossEventConsistencyValidator(newTable, [oldTable, newTable]).validate();
      expect(result.errors.filter(e => e.code === 'CONSISTENCY_NEW_ACTION')).toHaveLength(0);
    });
  });
});
