/**
 * Stage 1：类型生成器
 * IR fields → TypeScript interface
 */

import type { FormPageIR, FieldIR } from '../../ir/types';
import type { CompilerConfig } from './types';

export function generateTypes(ir: FormPageIR, config: CompilerConfig): string {
  const entity = capitalize(config.entity);
  const lines: string[] = [];

  lines.push(generateHeader(config));
  lines.push('');
  lines.push(`/** ${config.entityLabel} 实体 */`);
  lines.push(`export interface ${entity}Entity {`);

  for (const field of ir.fields) {
    const tsType = mapFieldToTsType(field);
    const optional = field.config.required ? '' : '?';
    lines.push(`  /** ${field.label} */`);
    lines.push(`  ${field.model}${optional}: ${tsType};`);
  }

  lines.push('}');
  lines.push('');

  lines.push(`/** ${config.entityLabel} 列表查询参数 */`);
  lines.push(`export interface ${entity}QueryParams {`);
  lines.push('  currentPage: number;');
  lines.push('  pageSize: number;');

  if (ir.listConfig?.searchFields) {
    for (const sf of ir.listConfig.searchFields) {
      lines.push(`  /** 搜索：${sf.label} */`);
      lines.push(`  ${sf.field}?: string;`);
    }
  }

  lines.push('}');
  lines.push('');

  lines.push(`/** ${config.entityLabel} 创建参数 */`);
  lines.push(`export type Create${entity}Params = Omit<${entity}Entity, 'id'>;`);
  lines.push('');
  lines.push(`/** ${config.entityLabel} 更新参数 */`);
  lines.push(`export type Update${entity}Params = Partial<${entity}Entity>;`);

  return lines.join('\n');
}

function mapFieldToTsType(field: FieldIR): string {
  const jnpfKey = field.component.jnpfKey;
  const typeMap: Record<string, string> = {
    JnpfInput: 'string',
    JnpfTextarea: 'string',
    JnpfInputNumber: 'number',
    JnpfSwitch: 'boolean',
    JnpfDatePicker: 'string',
    JnpfTimePicker: 'string',
    JnpfRate: 'number',
    JnpfSlider: 'number',
    JnpfSelect: field.config.multiple ? 'string[]' : 'string',
    JnpfRadio: 'string',
    JnpfCheckbox: 'string[]',
    JnpfCascader: 'string[]',
    JnpfTreeSelect: 'string',
    JnpfUploadImg: 'string[]',
    JnpfUploadFile: 'string[]',
    JnpfColorPicker: 'string',
    JnpfEditor: 'string',
  };
  return typeMap[jnpfKey] || 'unknown';
}

function generateHeader(config: CompilerConfig): string {
  const now = new Date().toISOString();
  return [
    `// @jnpf-generated v${config.generatorVersion} entity=${config.entity} type=types`,
    `// 生成时间：${now}`,
    '// 此文件由 JNPF 代码生成器生成，可手动修改',
    '// 重新生成时，未修改的区域将被覆盖',
    '',
    '/* eslint-disable */',
  ].join('\n');
}

function capitalize(s: string): string {
  return s.charAt(0).toUpperCase() + s.slice(1);
}
