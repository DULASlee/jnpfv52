// LogicValidator - PSPEC 校验器
import { ValidationError, ValidationResult } from './types';
import { DataDictionary } from './DictValidator';

export interface ProcessSpec {
  id: string;
  name: string;
  input: string[];
  output: string[];
  validation?: string;
  algorithm?: string;
}

export interface PSpec {
  process_specs: ProcessSpec[];
}

export class LogicValidator {
  constructor(
    private pspec: PSpec,
    private dict: DataDictionary
  ) {}

  validate(): ValidationResult {
    const errors: ValidationError[] = [];
    errors.push(...this.checkFieldsInDict());
    return { passed: errors.length === 0, errors };
  }

  private checkFieldsInDict(): ValidationError[] {
    const errors: ValidationError[] = [];
    // 收集所有字段名：elements + dataFlows[*].fields + dataStores[*].fields
    const dictFieldNames = new Set(this.dict.elements.map(e => e.name));
    this.dict.dataFlows?.forEach(f => f.fields?.forEach(e => dictFieldNames.add(e.name)));
    this.dict.dataStores?.forEach(s => s.fields?.forEach(e => dictFieldNames.add(e.name)));

    this.pspec.process_specs.forEach(spec => {
      spec.input.forEach(f => {
        if (!dictFieldNames.has(f)) {
          errors.push({
            code: 'LOGIC_FIELD_NOT_IN_DICT',
            message: `PSPEC 过程 "${spec.id}" 输入字段 "${f}" 不在数据字典`,
            severity: 'ERROR',
            field: f,
          });
        }
      });
    });
    return errors;
  }
}
