/**
 * Vue3 编译器入口
 *
 * 将 FormPageIR 编译为完整的 Vue 3 CRUD 项目文件集合
 */

import type { FormPageIR } from '../../ir/types';
import type { CompilerConfig, CompileResult, GeneratedProject } from './types';
import { generateTypes } from './type-gen';
import { generateApi } from './api-gen';
import { generateListPage, generateColumns, generateSearchConfig } from './list-gen';
import { generateFormPage } from './form-gen';
import { generateHook } from './hook-gen';

const DEFAULT_VERSION = '1.0.0';

export class Vue3Compiler {
  private config: CompilerConfig;

  constructor(config: Partial<CompilerConfig> & { entity: string }) {
    this.config = {
      entity: config.entity,
      entityLabel: config.entityLabel ?? config.entity,
      apiBasePath: config.apiBasePath ?? `/api/${config.entity}`,
      generatorVersion: config.generatorVersion ?? DEFAULT_VERSION,
    };
  }

  compile(ir: FormPageIR): CompileResult {
    const project: GeneratedProject = new Map();
    const warnings: string[] = [];
    const complexExpressions: string[] = [];

    for (const expr of ir.expressions) {
      if (expr.level === 'complex') {
        complexExpressions.push(`${expr.id}: ${expr.body.slice(0, 100)}`);
        warnings.push(`表达式 ${expr.id} 为复杂级别，需人工迁移`);
      }
    }

    const e = this.config.entity;

    project.set(`src/types/${e}.ts`, generateTypes(ir, this.config));
    project.set(`src/api/${e}.ts`, generateApi(ir, this.config));
    project.set(`src/views/${e}/index.vue`, generateListPage(ir, this.config));
    project.set(`src/views/${e}/columns.ts`, generateColumns(ir, this.config));
    project.set(`src/views/${e}/search.ts`, generateSearchConfig(ir, this.config));
    project.set(`src/views/${e}/form.vue`, generateFormPage(ir, this.config));
    project.set(`src/composables/use${capitalize(e)}.ts`, generateHook(this.config));

    return { project, warnings, complexExpressions };
  }
}

function capitalize(s: string): string {
  return s.charAt(0).toUpperCase() + s.slice(1);
}
