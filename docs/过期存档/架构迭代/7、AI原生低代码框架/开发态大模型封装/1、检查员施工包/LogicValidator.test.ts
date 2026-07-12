// LogicValidator 测试
import { LogicValidator } from '../src/LogicValidator';
import { DictBuilder, PSpecBuilder } from './helpers/builders';

describe('LogicValidator', () => {
  it('失败 LOGIC_FIELD_NOT_IN_DICT:PSPEC 引用字典外字段', () => {
    const dict = new DictBuilder()
      .withDataStore('WorkOrder', [{ name: 'Id', type: 'BIGINT' }])
      .build();
    const pspec = new PSpecBuilder()
      .addProcess('P1', '录入', ['WorkOrderId', 'GhostField'])  // ❌ GhostField
      .build();
    const result = new LogicValidator(pspec, dict).validate();
    expect(result.errors).toContainEqual(
      expect.objectContaining({ code: 'LOGIC_FIELD_NOT_IN_DICT', field: 'GhostField' })
    );
  });

  it('通过:PSPEC 字段都在字典中', () => {
    const dict = new DictBuilder()
      .withDataStore('WorkOrder', [{ name: 'Id', type: 'BIGINT' }])
      .withElement('WorkOrderId', 'BIGINT')
      .build();
    const pspec = new PSpecBuilder()
      .addProcess('P1', '录入', ['WorkOrderId', 'Id'])
      .build();
    const result = new LogicValidator(pspec, dict).validate();
    expect(result.passed).toBe(true);
  });
});
