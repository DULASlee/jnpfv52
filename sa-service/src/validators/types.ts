// 通用类型定义(所有 Validator 复用)

export type ValidationSeverity = 'ERROR' | 'WARNING';

export interface ValidationError {
  code: string;
  message: string;
  severity: ValidationSeverity;
  field?: string;
  suggestion?: string;
}

export interface ValidationResult {
  passed: boolean;
  errors: ValidationError[];
}

// 类型白名单(从 EAB 来)
export const TYPE_WHITELIST = [
  'NVARCHAR', 'BIGINT', 'INT', 'DECIMAL', 'DATETIME', 'BOOLEAN', 'JSON'
] as const;

// 必填审计字段
export const REQUIRED_AUDIT_FIELDS = [
  'created_at', 'created_by', 'updated_at', 'updated_by', 'tenant_id'
] as const;
