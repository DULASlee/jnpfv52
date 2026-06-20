// ERValidator 测试
import { ERValidator } from '../src/validators/ERValidator';
import { DictBuilder, ERBuilder } from './helpers/builders';

describe('ERValidator', () => {
  it('失败 ER_FIELD_NOT_IN_DICT:ER 字段不在字典中', () => {
    const dict = new DictBuilder()
      .withDataStore('WorkOrder', [{ name: 'Id', type: 'BIGINT' }])
      .build();
    const er = new ERBuilder()
      .addEntity('WorkOrder', [
        { name: 'Id', type: 'BIGINT' },
        { name: 'GhostField', type: 'NVARCHAR(50)' },  // ❌
      ])
      .build();
    const result = new ERValidator(er, dict).validate();
    expect(result.errors).toContainEqual(
      expect.objectContaining({ code: 'ER_FIELD_NOT_IN_DICT', field: 'GhostField' })
    );
  });

  it('失败 ER_FK_REF_INVALID:外键引用不存在的表', () => {
    const dict = new DictBuilder()
      .withDataStore('WorkOrder', [{ name: 'Id', type: 'BIGINT' }])
      .build();
    const er = new ERBuilder()
      .addEntity('WorkOrder', [{ name: 'Id', type: 'BIGINT' }])
      .addEntity('ProductionReport', [
        { name: 'Id', type: 'BIGINT' },
        { name: 'WorkOrderId', type: 'BIGINT', isFK: true, refTable: 'GhostTable' },  // ❌
      ])
      .build();
    const result = new ERValidator(er, dict).validate();
    expect(result.errors).toContainEqual(
      expect.objectContaining({ code: 'ER_FK_REF_INVALID' })
    );
  });

  it('通过:ER 字段都在字典,FK 引用合法', () => {
    const dict = new DictBuilder()
      .withDataStore('WorkOrder', [{ name: 'Id', type: 'BIGINT' }])
      .withDataStore('ProductionReport', [{ name: 'Id', type: 'BIGINT' }, { name: 'WorkOrderId', type: 'BIGINT' }])
      .build();
    const er = new ERBuilder()
      .addEntity('WorkOrder', [{ name: 'Id', type: 'BIGINT' }])
      .addEntity('ProductionReport', [
        { name: 'Id', type: 'BIGINT' },
        { name: 'WorkOrderId', type: 'BIGINT', isFK: true, refTable: 'WorkOrder' },
      ])
      .build();
    const result = new ERValidator(er, dict).validate();
    expect(result.passed).toBe(true);
  });
});
