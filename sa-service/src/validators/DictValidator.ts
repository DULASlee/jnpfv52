// DictValidator - 数据字典校验器
import {
  ValidationError, ValidationResult, TYPE_WHITELIST, REQUIRED_AUDIT_FIELDS
} from './types';

export interface FieldElement {
  name: string;
  type: string;
  length?: number;
  isFK?: boolean;
  refEntity?: string;
  isRequired?: boolean;
  scope?: string;
}

export interface DataFlowDef {
  name: string;
  fields: FieldElement[];
}

export interface DataStoreDef {
  name: string;
  fields: FieldElement[];
}

export interface DataDictionary {
  id: number;
  project_id: number;
  asset_level: 'PROJECT' | 'EVENT' | 'PROCESS';
  event_id?: number;
  elements: FieldElement[];
  dataFlows: DataFlowDef[];
  dataStores: DataStoreDef[];
}

export interface DFD {
  dataFlows: { name: string }[];
  dataStores: { name: string }[];
}

export class DictValidator {
  constructor(
    private dict: DataDictionary,
    private dfd: DFD
  ) {}

  validate(): ValidationResult {
    const errors: ValidationError[] = [];
    errors.push(...this.checkDFDCoherence());
    errors.push(...this.checkFieldTypes());
    errors.push(...this.checkFKReferences());
    errors.push(...this.checkAuditFields());
    errors.push(...this.checkTenantIsolation());
    return { passed: errors.length === 0, errors };
  }

  private checkDFDCoherence(): ValidationError[] {
    const errors: ValidationError[] = [];
    const dictFlowNames = new Set(this.dict.dataFlows.map(f => f.name));
    const dictStoreNames = new Set(this.dict.dataStores.map(s => s.name));

    this.dfd.dataFlows.forEach(flow => {
      if (!dictFlowNames.has(flow.name)) {
        errors.push({
          code: 'DICT_FLOW_MISSING',
          message: `DFD 数据流 "${flow.name}" 在数据字典中未定义`,
          severity: 'ERROR',
          suggestion: `在 dict.dataFlows 中添加 "${flow.name}" 定义`,
        });
      }
    });

    this.dfd.dataStores.forEach(store => {
      if (!dictStoreNames.has(store.name)) {
        errors.push({
          code: 'DICT_STORE_MISSING',
          message: `DFD 数据存储 "${store.name}" 在数据字典中未定义`,
          severity: 'ERROR',
        });
      }
    });

    return errors;
  }

  private checkFieldTypes(): ValidationError[] {
    const errors: ValidationError[] = [];
    const allFields = [
      ...this.dict.elements,
      ...this.dict.dataFlows.flatMap(f => f.fields),
      ...this.dict.dataStores.flatMap(s => s.fields),
    ];

    allFields.forEach(field => {
      const baseType = field.type.split('(')[0];
      if (!TYPE_WHITELIST.includes(baseType as any)) {
        errors.push({
          code: 'DICT_INVALID_TYPE',
          message: `字段 "${field.name}" 类型 "${field.type}" 不在白名单中`,
          severity: 'ERROR',
          field: field.name,
        });
      }
      if (field.type.startsWith('NVARCHAR') && !field.type.includes('(')) {
        errors.push({
          code: 'DICT_MISSING_LENGTH',
          message: `NVARCHAR 字段 "${field.name}" 必须指定长度`,
          severity: 'ERROR',
          field: field.name,
        });
      }
      if (field.type.startsWith('DECIMAL') && !field.type.includes('(')) {
        errors.push({
          code: 'DICT_MISSING_PRECISION',
          message: `DECIMAL 字段 "${field.name}" 必须指定精度`,
          severity: 'ERROR',
          field: field.name,
        });
      }
    });

    return errors;
  }

  private checkFKReferences(): ValidationError[] {
    const errors: ValidationError[] = [];
    const allFields = [
      ...this.dict.elements,
      ...this.dict.dataFlows.flatMap(f => f.fields),
      ...this.dict.dataStores.flatMap(s => s.fields),
    ];
    const storeNames = new Set(this.dict.dataStores.map(s => s.name));

    allFields.forEach(field => {
      if (field.isFK && !field.refEntity) {
        errors.push({
          code: 'DICT_FK_NO_REF',
          message: `外键字段 "${field.name}" 必须指定引用的实体`,
          severity: 'ERROR',
          field: field.name,
        });
      }
      if (field.isFK && field.refEntity && !storeNames.has(field.refEntity)) {
        errors.push({
          code: 'DICT_FK_REF_INVALID',
          message: `外键 "${field.name}" 引用 "${field.refEntity}" 在数据存储中不存在`,
          severity: 'ERROR',
          field: field.name,
        });
      }
    });

    return errors;
  }

  private checkAuditFields(): ValidationError[] {
    const errors: ValidationError[] = [];
    const fieldNames = new Set(this.dict.elements.map(e => e.name));
    REQUIRED_AUDIT_FIELDS.forEach(field => {
      if (!fieldNames.has(field)) {
        errors.push({
          code: 'DICT_MISSING_AUDIT',
          message: `数据字典必须包含审计字段 "${field}"`,
          severity: 'ERROR',
          field,
        });
      }
    });
    return errors;
  }

  private checkTenantIsolation(): ValidationError[] {
    const errors: ValidationError[] = [];
    const storeWithoutTenant = this.dict.dataStores.filter(
      store => !store.fields.some(f => f.name === 'tenant_id')
    );
    if (storeWithoutTenant.length > 0) {
      errors.push({
        code: 'DICT_MISSING_TENANT',
        message: `数据存储 [${storeWithoutTenant.map(s => s.name).join(', ')}] 缺少 TenantId 字段(多租户隔离必需)`,
        severity: 'ERROR',
      });
    }
    return errors;
  }
}
