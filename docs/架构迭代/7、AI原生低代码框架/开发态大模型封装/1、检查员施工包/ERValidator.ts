// ERValidator - ER 图校验器
import { ValidationError, ValidationResult } from './types';
import { DataDictionary } from './DictValidator';

export interface ERColumn {
  name: string;
  type: string;
  isPK?: boolean;
  isFK?: boolean;
  refTable?: string;
}

export interface EREntity {
  name: string;
  columns: ERColumn[];
}

export interface ER {
  entities: EREntity[];
  relationships: any[];
}

export class ERValidator {
  constructor(
    private er: ER,
    private dict: DataDictionary
  ) {}

  validate(): ValidationResult {
    const errors: ValidationError[] = [];
    errors.push(...this.checkFieldsInDict());
    errors.push(...this.checkFKReferences());
    return { passed: errors.length === 0, errors };
  }

  private checkFieldsInDict(): ValidationError[] {
    const errors: ValidationError[] = [];
    const dictFields = new Set(this.dict.elements.map(e => e.name));

    this.er.entities.forEach(entity => {
      entity.columns.forEach(col => {
        if (!dictFields.has(col.name)) {
          errors.push({
            code: 'ER_FIELD_NOT_IN_DICT',
            message: `实体 "${entity.name}" 字段 "${col.name}" 不在数据字典`,
            severity: 'ERROR',
            field: col.name,
          });
        }
      });
    });
    return errors;
  }

  private checkFKReferences(): ValidationError[] {
    const errors: ValidationError[] = [];
    const entityNames = new Set(this.er.entities.map(e => e.name));

    this.er.entities.forEach(entity => {
      entity.columns.forEach(col => {
        if (col.isFK && col.refTable && !entityNames.has(col.refTable)) {
          errors.push({
            code: 'ER_FK_REF_INVALID',
            message: `实体 "${entity.name}" 外键 "${col.name}" 引用 "${col.refTable}" 不存在`,
            severity: 'ERROR',
            field: col.name,
          });
        }
      });
    });
    return errors;
  }
}
