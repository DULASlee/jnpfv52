/**
 * DKEE v1.0 单元测试
 */
import { describe, it, expect, beforeEach } from 'vitest';
import { observeAndExtract, persistPattern, recallPatterns, clearKnowledgeGraph, type HumanAction } from '../dkee/v1';

describe('DKEE v1.0', () => {
  beforeEach(() => {
    clearKnowledgeGraph();
  });

  const createAction = (target: string, entityName: string, fields: string[]): HumanAction => ({
    type: 'create',
    target,
    after: {
      name: entityName,
      ...Object.fromEntries(fields.map(f => [f, f === 'age' ? 0 : ''])),
    },
    before: null,
  });

  it('observeAndExtract 从3次create操作中提炼模式', () => {
    const actions: HumanAction[] = [
      createAction('学生管理', '学生', ['姓名', '学号', '班级']),
      createAction('学生管理', '课程', ['课程名', '学分']),
      createAction('学生管理', '成绩', ['分数', '科目']),
    ];

    const pattern = observeAndExtract(actions, '学生管理');
    expect(pattern).not.toBeNull();
    expect(pattern!.domain).toBe('学生管理');
    expect(pattern!.pattern.entities.length).toBeGreaterThanOrEqual(2);
    expect(pattern!.source).toBe('human-created');
  });

  it('observeAndExtract 操作不足3次返回null', () => {
    const actions: HumanAction[] = [createAction('学生管理', '学生', ['姓名']), createAction('学生管理', '课程', ['课程名'])];

    const pattern = observeAndExtract(actions, '学生管理');
    expect(pattern).toBeNull();
  });

  it('observeAndExtract 空领域返回null', () => {
    const pattern = observeAndExtract([], '');
    expect(pattern).toBeNull();
  });

  it('recallPatterns 按领域过滤', () => {
    const pattern1 = observeAndExtract(
      [createAction('制造', '工单', ['工单号', '数量']), createAction('制造', '设备', ['编号', '型号']), createAction('制造', '质检', ['结果', '检验员'])],
      '制造',
    )!;
    persistPattern(pattern1);

    const pattern2 = observeAndExtract(
      [createAction('医疗', '患者', ['姓名', '性别']), createAction('医疗', '处方', ['药品', '剂量']), createAction('医疗', '检验', ['项目', '结果'])],
      '医疗',
    )!;
    persistPattern(pattern2);

    // 召回"制造"领域
    const mfgResults = recallPatterns('制造');
    expect(mfgResults.length).toBe(1);
    expect(mfgResults[0].domain).toBe('制造');

    // 召回"医疗"领域
    const medResults = recallPatterns('医疗');
    expect(medResults.length).toBe(1);
    expect(medResults[0].domain).toBe('医疗');
  });

  it('persistPattern + recallPatterns round-trip', () => {
    const actions: HumanAction[] = [
      createAction('零售', '订单', ['订单号', '金额', '客户']),
      createAction('零售', '商品', ['名称', '价格']),
      createAction('零售', '会员', ['姓名', '积分']),
    ];

    const pattern = observeAndExtract(actions, '零售')!;
    expect(pattern).not.toBeNull();

    persistPattern(pattern);

    const recalled = recallPatterns('零售');
    expect(recalled.length).toBe(1);
    expect(recalled[0].name).toBe(pattern.name);
    expect(recalled[0].pattern.entities.length).toBe(pattern.pattern.entities.length);
    expect(recalled[0].usageCount).toBeGreaterThanOrEqual(1);
  });

  it('同领域重复persist合并版本号', () => {
    const actions: HumanAction[] = [
      createAction('教育', '学生', ['姓名', '学号']),
      createAction('教育', '班级', ['名称', '年级']),
      createAction('教育', '课程', ['名称', '学分']),
    ];

    const p1 = observeAndExtract(actions, '教育')!;
    persistPattern(p1);

    const p2 = observeAndExtract(actions, '教育')!;
    persistPattern(p2);

    const results = recallPatterns('教育');
    expect(results.length).toBe(1);
    // 版本应更新
    expect(results[0].usageCount).toBeGreaterThanOrEqual(1);
  });

  it('recallPatterns 不匹配时返回空数组', () => {
    const results = recallPatterns('宇宙航天');
    expect(results).toEqual([]);
  });

  it('混合操作类型仅统计create', () => {
    const actions: HumanAction[] = [
      createAction('测试', '实体A', ['字段1']),
      { type: 'modify', target: '测试', before: { name: '旧' }, after: { name: '新' } },
      { type: 'delete', target: '测试', before: { name: '删' }, after: null },
    ];

    const pattern = observeAndExtract(actions, '测试');
    expect(pattern).toBeNull(); // 只有1个create，不足3次
  });
});
