// DictValidator 测试 - 覆盖所有 9 个 ERROR code
import { DictValidator } from '../src/validators/DictValidator';
import { DictBuilder } from './helpers/builders';

describe('DictValidator', () => {
  // ========================================================
  // 1. DFD ↔ 字典 一致性
  // ========================================================
  describe('DFD ↔ 字典一致性', () => {
    it('通过:所有 DFD 数据流/存储都在字典中', () => {
      const dict = new DictBuilder()
        .withDataStore('WorkOrder', [{ name: 'Id', type: 'BIGINT' }])
        .withDataFlow('报工单', [{ name: 'WorkOrderId', type: 'BIGINT' }])
        .build();
      const dfd = {
        dataFlows: [{ name: '报工单' }],
        dataStores: [{ name: 'WorkOrder' }],
      };
      const result = new DictValidator(dict, dfd).validate();
      expect(result.errors.filter(e => e.code === 'DICT_FLOW_MISSING')).toHaveLength(0);
      expect(result.errors.filter(e => e.code === 'DICT_STORE_MISSING')).toHaveLength(0);
    });

    it('失败 DICT_FLOW_MISSING:DFD 数据流不在字典中', () => {
      const dict = new DictBuilder()
        .withDataStore('WorkOrder', [{ name: 'Id', type: 'BIGINT' }])
        .build();
      const dfd = {
        dataFlows: [{ name: '不存在的流' }],  // ❌ 字典里没有
        dataStores: [{ name: 'WorkOrder' }],
      };
      const result = new DictValidator(dict, dfd).validate();
      expect(result.passed).toBe(false);
      expect(result.errors).toContainEqual(
        expect.objectContaining({ code: 'DICT_FLOW_MISSING' })
      );
    });

    it('失败 DICT_STORE_MISSING:DFD 数据存储不在字典中', () => {
      const dict = new DictBuilder().build();
      const dfd = {
        dataFlows: [],
        dataStores: [{ name: 'GhostStore' }],  // ❌
      };
      const result = new DictValidator(dict, dfd).validate();
      expect(result.passed).toBe(false);
      expect(result.errors).toContainEqual(
        expect.objectContaining({ code: 'DICT_STORE_MISSING' })
      );
    });
  });

  // ========================================================
  // 2. 字段类型校验
  // ========================================================
  describe('字段类型校验', () => {
    it('失败 DICT_INVALID_TYPE:字段类型不在白名单', () => {
      const dict = new DictBuilder()
        .withElement('BadField', 'XMLTYPE')  // ❌ XMLTYPE 不在白名单
        .build();
      const result = new DictValidator(dict, { dataFlows: [], dataStores: [] }).validate();
      expect(result.errors).toContainEqual(
        expect.objectContaining({ code: 'DICT_INVALID_TYPE', field: 'BadField' })
      );
    });

    it('失败 DICT_MISSING_LENGTH:NVARCHAR 没指定长度', () => {
      const dict = new DictBuilder()
        .withElement('NoLength', 'NVARCHAR')  // ❌ 缺长度
        .build();
      const result = new DictValidator(dict, { dataFlows: [], dataStores: [] }).validate();
      expect(result.errors).toContainEqual(
        expect.objectContaining({ code: 'DICT_MISSING_LENGTH', field: 'NoLength' })
      );
    });

    it('失败 DICT_MISSING_PRECISION:DECIMAL 没指定精度', () => {
      const dict = new DictBuilder()
        .withElement('NoPrecision', 'DECIMAL')  // ❌
        .build();
      const result = new DictValidator(dict, { dataFlows: [], dataStores: [] }).validate();
      expect(result.errors).toContainEqual(
        expect.objectContaining({ code: 'DICT_MISSING_PRECISION', field: 'NoPrecision' })
      );
    });

    it('通过:NVARCHAR(50) 合法', () => {
      const dict = new DictBuilder()
        .withElement('GoodField', 'NVARCHAR(50)')
        .build();
      const result = new DictValidator(dict, { dataFlows: [], dataStores: [] }).validate();
      expect(result.errors.filter(e => e.field === 'GoodField')).toHaveLength(0);
    });
  });

  // ========================================================
  // 3. 外键引用校验
  // ========================================================
  describe('外键引用校验', () => {
    it('失败 DICT_FK_NO_REF:FK 字段没指定 refEntity', () => {
      const dict = new DictBuilder()
        .withElement('BadFK', 'BIGINT', true)  // ❌ isFK=true 但无 refEntity
        .build();
      const result = new DictValidator(dict, { dataFlows: [], dataStores: [] }).validate();
      expect(result.errors).toContainEqual(
        expect.objectContaining({ code: 'DICT_FK_NO_REF', field: 'BadFK' })
      );
    });

    it('失败 DICT_FK_REF_INVALID:FK 引用不存在的实体', () => {
      const dict = new DictBuilder()
        .withElement('BadFK', 'BIGINT', true, 'GhostEntity')  // ❌
        .build();
      const result = new DictValidator(dict, { dataFlows: [], dataStores: [] }).validate();
      expect(result.errors).toContainEqual(
        expect.objectContaining({ code: 'DICT_FK_REF_INVALID', field: 'BadFK' })
      );
    });

    it('通过:FK 引用合法的实体', () => {
      const dict = new DictBuilder()
        .withDataStore('User', [{ name: 'Id', type: 'BIGINT' }])
        .withElement('UserId', 'BIGINT', true, 'User')  // ✅
        .build();
      const result = new DictValidator(dict, { dataFlows: [], dataStores: [] }).validate();
      expect(result.errors.filter(e => e.code.startsWith('DICT_FK'))).toHaveLength(0);
    });
  });

  // ========================================================
  // 4. 必填审计字段
  // ========================================================
  describe('必填审计字段', () => {
    it('失败 DICT_MISSING_AUDIT:缺少 CreatedAt', () => {
      const dict = new DictBuilder()
        .withDataStore('Test', [{ name: 'Id', type: 'BIGINT' }])
        .withoutAuditFields()
        .build();
      const result = new DictValidator(dict, { dataFlows: [], dataStores: [] }).validate();
      expect(result.errors.some(e => e.code === 'DICT_MISSING_AUDIT' && e.field === 'created_at')).toBe(true);
    });

    it('失败 DICT_MISSING_AUDIT:5 个审计字段全缺时报 5 个错误', () => {
      const dict = new DictBuilder()
        .withDataStore('Test', [{ name: 'Id', type: 'BIGINT' }])
        .withoutAuditFields()
        .build();
      const result = new DictValidator(dict, { dataFlows: [], dataStores: [] }).validate();
      const auditErrors = result.errors.filter(e => e.code === 'DICT_MISSING_AUDIT');
      expect(auditErrors).toHaveLength(5);
    });
  });

  // ========================================================
  // 5. 多租户隔离
  // ========================================================
  describe('多租户隔离', () => {
    it('失败 DICT_MISSING_TENANT:数据存储没有 TenantId', () => {
      const dict = new DictBuilder()
        .withDataStore('NoTenant', [{ name: 'Id', type: 'BIGINT' }], false)  // ❌ 无 TenantId
        .build();
      const result = new DictValidator(dict, { dataFlows: [], dataStores: [] }).validate();
      expect(result.errors).toContainEqual(
        expect.objectContaining({ code: 'DICT_MISSING_TENANT' })
      );
    });

    it('通过:数据存储含 TenantId', () => {
      const dict = new DictBuilder()
        .withDataStore('Good', [{ name: 'Id', type: 'BIGINT' }], true)  // ✅
        .build();
      const result = new DictValidator(dict, { dataFlows: [], dataStores: [] }).validate();
      expect(result.errors.filter(e => e.code === 'DICT_MISSING_TENANT')).toHaveLength(0);
    });
  });

  // ========================================================
  // 6. 综合场景
  // ========================================================
  describe('综合场景', () => {
    it('完整合法字典应全部通过', () => {
      const dict = new DictBuilder()
        .withElement('Id', 'BIGINT')
        .withElement('WorkOrderId', 'BIGINT', true, 'WorkOrder')
        .withDataStore('WorkOrder', [
          { name: 'Id', type: 'BIGINT' },
          { name: 'Qty', type: 'DECIMAL(18,2)' },
          { name: 'Name', type: 'NVARCHAR(50)' },
        ])
        .withDataFlow('报工单', [
          { name: 'WorkOrderId', type: 'BIGINT' },
          { name: 'Qty', type: 'DECIMAL(18,2)' },
        ])
        .build();
      const dfd = {
        dataFlows: [{ name: '报工单' }],
        dataStores: [{ name: 'WorkOrder' }],
      };
      const result = new DictValidator(dict, dfd).validate();
      expect(result.passed).toBe(true);
      expect(result.errors).toHaveLength(0);
    });
  });
});
