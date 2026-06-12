/**
 * Stage 2：API 生成器
 * IR → 标准 RESTful API 函数
 */

import type { FormPageIR } from '../../ir/types';
import type { CompilerConfig } from './types';

export function generateApi(_ir: FormPageIR, config: CompilerConfig): string {
  const entity = capitalize(config.entity);
  const now = new Date().toISOString();

  return `// @jnpf-generated v${config.generatorVersion} entity=${config.entity} type=api
// 生成时间：${now}
// 此文件由 JNPF 代码生成器生成，可手动修改

/* eslint-disable */
import request from '@/utils/request';
import type { ${entity}Entity, ${entity}QueryParams, Create${entity}Params, Update${entity}Params } from './types';

const BASE_URL = '${config.apiBasePath}';

/** ${config.entityLabel} 列表查询 */
export function get${entity}List(params: ${entity}QueryParams) {
  return request.get<${entity}Entity[]>(\`\${BASE_URL}/list\`, { params });
}

/** ${config.entityLabel} 详情 */
export function get${entity}Detail(id: string) {
  return request.get<${entity}Entity>(\`\${BASE_URL}/\${id}\`);
}

/** ${config.entityLabel} 新增 */
export function create${entity}(data: Create${entity}Params) {
  return request.post<${entity}Entity>(BASE_URL, data);
}

/** ${config.entityLabel} 更新 */
export function update${entity}(id: string, data: Update${entity}Params) {
  return request.put(\`\${BASE_URL}/\${id}\`, data);
}

/** ${config.entityLabel} 删除 */
export function delete${entity}(id: string) {
  return request.delete(\`\${BASE_URL}/\${id}\`);
}

/** ${config.entityLabel} 批量删除 */
export function batchDelete${entity}(ids: string[]) {
  return request.delete(\`\${BASE_URL}/batch\`, { data: { ids } });
}
`;
}

function capitalize(s: string): string {
  return s.charAt(0).toUpperCase() + s.slice(1);
}
