// UIValidator 测试 - 覆盖所有 5 个 ERROR code
import { UIValidator } from '../src/validators/UIValidator';
import { DictBuilder, UIBuilder, BPMBuilder } from './helpers/builders';

describe('UIValidator', () => {
  // ========================================================
  // 1. UI 字段在字典中
  // ========================================================
  describe('UI 字段在字典中', () => {
    it('失败 UI_FIELD_NOT_IN_DICT:UI 字段不在字典中(LLM 幻觉)', () => {
      const dict = new DictBuilder()
        .withDataStore('Report', [{ name: 'Id', type: 'BIGINT' }])
        .withDataFlow('报工单', [{ name: 'WorkOrderId', type: 'BIGINT' }])
        .build();
      const ui = new UIBuilder()
        .addScreen('S1', '录入屏', '报工单', 'N1', [
          { name: 'WorkOrderId', type: 'BIGINT', required: true, controlType: 'input' },
          { name: 'GhostField', type: 'NVARCHAR(50)', required: false, controlType: 'input' },  // ❌
        ])
        .build();
      const result = new UIValidator(ui, dict, { activity_nodes: [] }).validate();
      expect(result.errors).toContainEqual(
        expect.objectContaining({ code: 'UI_FIELD_NOT_IN_DICT', field: 'GhostField' })
      );
    });

    it('通过:UI 字段都在字典中', () => {
      const dict = new DictBuilder()
        .withDataStore('Report', [{ name: 'Id', type: 'BIGINT' }])
        .withDataFlow('报工单', [
          { name: 'WorkOrderId', type: 'BIGINT' },
          { name: 'Qty', type: 'DECIMAL(18,2)' },
        ])
        .withElement('WorkOrderId', 'BIGINT')
        .withElement('Qty', 'DECIMAL(18,2)')
        .build();
      const ui = new UIBuilder()
        .addScreen('S1', '录入屏', '报工单', 'N1', [
          { name: 'WorkOrderId', type: 'BIGINT', required: true, controlType: 'input' },
          { name: 'Qty', type: 'DECIMAL(18,2)', required: true, controlType: 'number' },
        ])
        .build();
      const result = new UIValidator(ui, dict, { activity_nodes: [] }).validate();
      expect(result.passed).toBe(true);
    });
  });

  // ========================================================
  // 2. UI 字段在数据流的字典定义中
  // ========================================================
  describe('UI 字段在数据流的字典中', () => {
    it('失败 UI_FIELD_NOT_IN_FLOW:字段不在数据流字典中', () => {
      const dict = new DictBuilder()
        .withDataFlow('报工单', [{ name: 'WorkOrderId', type: 'BIGINT' }])
        .withElement('OtherField', 'NVARCHAR(50)')  // 在 elements 但不在报工单的 dataFlow
        .build();
      const ui = new UIBuilder()
        .addScreen('S1', '屏', '报工单', 'N1', [
          { name: 'WorkOrderId', type: 'BIGINT', required: true, controlType: 'input' },
          { name: 'OtherField', type: 'NVARCHAR(50)', required: false, controlType: 'input' },  // ❌ 不在报工单 dataFlow 里
        ])
        .build();
      const result = new UIValidator(ui, dict, { activity_nodes: [] }).validate();
      expect(result.errors).toContainEqual(
        expect.objectContaining({ code: 'UI_FIELD_NOT_IN_FLOW' })
      );
    });

    it('失败 UI_NO_DATA_FLOW:UI 屏绑定了不存在的 dataFlow', () => {
      const dict = new DictBuilder()
        .withElement('X', 'NVARCHAR(50)')
        .build();
      const ui = new UIBuilder()
        .addScreen('S1', '屏', 'GhostFlow', 'N1', [
          { name: 'X', type: 'NVARCHAR(50)', required: false, controlType: 'input' },
        ])
        .build();
      const result = new UIValidator(ui, dict, { activity_nodes: [] }).validate();
      expect(result.errors).toContainEqual(
        expect.objectContaining({ code: 'UI_NO_DATA_FLOW' })
      );
    });
  });

  // ========================================================
  // 3. UI 字段类型与字典一致
  // ========================================================
  describe('UI 字段类型与字典一致', () => {
    it('失败 UI_TYPE_MISMATCH:UI 类型与字典不一致', () => {
      const dict = new DictBuilder()
        .withDataStore('R', [{ name: 'Id', type: 'BIGINT' }])
        .withDataFlow('报工单', [{ name: 'Qty', type: 'DECIMAL(18,2)' }])
        .withElement('Qty', 'DECIMAL(18,2)')
        .build();
      const ui = new UIBuilder()
        .addScreen('S1', '屏', '报工单', 'N1', [
          { name: 'Qty', type: 'INT', required: true, controlType: 'number' },  // ❌ INT ≠ DECIMAL
        ])
        .build();
      const result = new UIValidator(ui, dict, { activity_nodes: [] }).validate();
      expect(result.errors).toContainEqual(
        expect.objectContaining({ code: 'UI_TYPE_MISMATCH' })
      );
    });
  });

  // ========================================================
  // 4. BPM 节点必须有 UI 屏
  // ========================================================
  describe('BPM 节点必须有 UI 屏', () => {
    it('警告 UI_BPM_NODE_MISSING:user_action 节点没 UI 屏', () => {
      const dict = new DictBuilder().withElement('X', 'NVARCHAR(50)').build();
      const ui = new UIBuilder().build();  // 没有屏
      const bpm = { activity_nodes: [
        { id: 'N1', type: 'user_action' as const },
        { id: 'N2', type: 'system_action' as const },  // system_action 不要求
      ]};
      const result = new UIValidator(ui, dict, bpm).validate();
      expect(result.errors).toContainEqual(
        expect.objectContaining({ code: 'UI_BPM_NODE_MISSING', severity: 'WARNING' })
      );
    });

    it('通过:每个 user_action 节点都有 UI 屏', () => {
      const dict = new DictBuilder()
        .withDataFlow('F1', [{ name: 'X', type: 'NVARCHAR(50)' }])
        .withElement('X', 'NVARCHAR(50)')
        .build();
      const ui = new UIBuilder()
        .addScreen('S1', '屏', 'F1', 'N1', [
          { name: 'X', type: 'NVARCHAR(50)', required: false, controlType: 'input' },
        ])
        .build();
      const bpm = { activity_nodes: [{ id: 'N1', type: 'user_action' as const }] };
      const result = new UIValidator(ui, dict, bpm).validate();
      expect(result.passed).toBe(true);
    });
  });
});
