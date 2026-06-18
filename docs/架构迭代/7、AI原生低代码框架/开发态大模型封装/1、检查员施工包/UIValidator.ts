// UIValidator - UI 校验器
import { ValidationError, ValidationResult } from './types';
import { DataDictionary, FieldElement } from './DictValidator';

export interface UIField {
  name: string;
  type: string;
  required: boolean;
  controlType: 'input' | 'select' | 'date' | 'number';
}

export interface UIScreen {
  id: string;
  name: string;
  dataFlow: string;
  bpmNodeId: string;
  fields: UIField[];
}

export interface UI {
  id: number;
  screens: UIScreen[];
}

export interface BPMNode {
  id: string;
  type: 'user_action' | 'system_action' | 'decision';
}

export class UIValidator {
  constructor(
    private ui: UI,
    private dict: DataDictionary,
    private bpm: { activity_nodes: BPMNode[] }
  ) {}

  validate(): ValidationResult {
    const errors: ValidationError[] = [];
    errors.push(...this.checkFieldsInDict());
    errors.push(...this.checkNoExtraFields());
    errors.push(...this.checkTypeConsistency());
    errors.push(...this.checkBPMMapping());
    return { passed: errors.length === 0, errors };
  }

  private checkFieldsInDict(): ValidationError[] {
    const errors: ValidationError[] = [];
    const dictFieldNames = new Set(this.dict.elements.map(e => e.name));

    this.ui.screens.forEach(screen => {
      screen.fields.forEach(field => {
        if (!dictFieldNames.has(field.name)) {
          errors.push({
            code: 'UI_FIELD_NOT_IN_DICT',
            message: `UI 屏 "${screen.id}" 字段 "${field.name}" 不在数据字典中(LLM 幻觉!)`,
            severity: 'ERROR',
            field: field.name,
            suggestion: `在数据字典中添加 "${field.name}" 字段定义`,
          });
        }
      });
    });
    return errors;
  }

  private checkNoExtraFields(): ValidationError[] {
    const errors: ValidationError[] = [];
    const dictFieldsByFlow = new Map<string, Set<string>>();
    this.dict.dataFlows.forEach(flow => {
      dictFieldsByFlow.set(flow.name, new Set(flow.fields.map(f => f.name)));
    });

    this.ui.screens.forEach(screen => {
      const allowedFields = dictFieldsByFlow.get(screen.dataFlow);
      if (!allowedFields) {
        errors.push({
          code: 'UI_NO_DATA_FLOW',
          message: `UI 屏 "${screen.id}" 绑定的数据流 "${screen.dataFlow}" 不在字典中`,
          severity: 'ERROR',
        });
        return;
      }
      screen.fields.forEach(field => {
        if (!allowedFields.has(field.name)) {
          errors.push({
            code: 'UI_FIELD_NOT_IN_FLOW',
            message: `UI 屏 "${screen.id}" 字段 "${field.name}" 不在数据流 "${screen.dataFlow}" 的字典定义中`,
            severity: 'ERROR',
            field: field.name,
          });
        }
      });
    });
    return errors;
  }

  private checkTypeConsistency(): ValidationError[] {
    const errors: ValidationError[] = [];
    const dictFieldMap = new Map(this.dict.elements.map(e => [e.name, e]));

    this.ui.screens.forEach(screen => {
      screen.fields.forEach(field => {
        const dictField = dictFieldMap.get(field.name);
        if (dictField && dictField.type !== field.type) {
          errors.push({
            code: 'UI_TYPE_MISMATCH',
            message: `UI 屏 "${screen.id}" 字段 "${field.name}" 类型 "${field.type}" 与字典 "${dictField.type}" 不一致`,
            severity: 'ERROR',
            field: field.name,
          });
        }
      });
    });
    return errors;
  }

  private checkBPMMapping(): ValidationError[] {
    const errors: ValidationError[] = [];
    const screenBPMIds = new Set(this.ui.screens.map(s => s.bpmNodeId));

    this.bpm.activity_nodes
      .filter(n => n.type === 'user_action')
      .forEach(node => {
        if (!screenBPMIds.has(node.id)) {
          errors.push({
            code: 'UI_BPM_NODE_MISSING',
            message: `BPM 节点 "${node.id}" 没有对应的 UI 屏`,
            severity: 'WARNING',
          });
        }
      });
    return errors;
  }
}
