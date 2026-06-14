/**
 * 数据库设计智能体
 *
 * 根据领域模型生成数据库表结构、迁移 SQL 和 API 端点。
 * 自动注入多租户字段和审计字段，强制校验命名规范。
 *
 * 硬性约束（不可违反）：
 *   - 表名：{MODULE_PREFIX}_{ENTITY} UPPER_SNAKE_CASE
 *   - 列名：F_ UPPER_SNAKE_CASE
 *   - 主键：F_ID BIGINT（雪花 ID）
 *   - 每表必含：F_TENANT_ID + 审计字段 + F_IS_DELETED
 *
 * @version 1.0.0
 * @module ai/agents/database
 */

import type { LLMGateway } from '../llm/types';
import { DATABASE_DESIGNER_PROMPT } from '../llm/prompts';
import { BaseAgent, type AgentContext, type AgentResponse } from './base';

// ============================================================
// 输出类型
// ============================================================

export interface DatabaseDesign {
  /** 数据库设计概述 */
  overview: string;
  /** 表结构定义 */
  tables: Array<{
    name: string;
    comment: string;
    columns: Array<{
      name: string;
      type: string;
      length?: number | null;
      nullable: boolean;
      defaultValue?: string | null;
      comment: string;
      isAudit?: boolean;
      isTenant?: boolean;
    }>;
    indexes: Array<{
      name: string;
      columns: string[];
      unique: boolean;
    }>;
  }>;
  /** 迁移 SQL */
  migrationSql: string;
  /** API 端点 */
  apis: Array<{
    path: string;
    method: 'GET' | 'POST' | 'PUT' | 'DELETE';
    description: string;
    requestType?: string;
    responseType?: string;
    requireAuth: boolean;
    permissionCode?: string;
  }>;
}

// ============================================================
// 审计字段（与 ArchitectAgent 保持一致）
// ============================================================

const TENANT_COLUMN = {
  name: 'F_TENANT_ID',
  type: 'NVARCHAR',
  length: 50,
  nullable: false,
  defaultValue: null,
  comment: '租户ID',
  isTenant: true,
};

const AUDIT_COLUMNS = [
  { name: 'F_CREATE_USER_ID', type: 'NVARCHAR', length: 50, nullable: true, defaultValue: null, comment: '创建用户', isAudit: true },
  { name: 'F_CREATE_TIME', type: 'DATETIME', nullable: false, defaultValue: 'GETDATE()', comment: '创建时间', isAudit: true },
  { name: 'F_MODIFY_USER_ID', type: 'NVARCHAR', length: 50, nullable: true, defaultValue: null, comment: '修改用户', isAudit: true },
  { name: 'F_MODIFY_TIME', type: 'DATETIME', nullable: true, defaultValue: null, comment: '修改时间', isAudit: true },
  { name: 'F_IS_DELETED', type: 'BIT', nullable: false, defaultValue: '0', comment: '逻辑删除', isAudit: true },
];

// ============================================================
// DatabaseAgent
// ============================================================

export class DatabaseAgent extends BaseAgent {
  constructor(llm: LLMGateway) {
    super(llm, DATABASE_DESIGNER_PROMPT);
  }

  /**
   * 设计数据库。
   *
   * 流程：LLM 生成 → 注入租户/审计字段 → 校验命名规范 → 校验 API 端点
   *
   * @param domainModel - JSON 序列化的领域模型
   * @param context - 执行上下文
   * @returns 数据库设计方案
   */
  async design(domainModel: string, context: AgentContext = {}): Promise<AgentResponse<DatabaseDesign>> {
    const result = await this.execute<DatabaseDesign>(domainModel, context);

    // 自动注入多租户和审计字段
    result.data = this.injectAuditFields(result.data);

    // 校验并修正命名规范
    result.data = this.normalizeNaming(result.data);

    // 校验 API 端点
    result.data = this.validateApis(result.data);

    return result;
  }

  // ============================================================
  // 自动注入
  // ============================================================

  /**
   * 自动注入 TENANT_ID + 审计字段 + IS_DELETED。
   */
  private injectAuditFields(design: DatabaseDesign): DatabaseDesign {
    for (const table of design.tables) {
      const columnNames = table.columns.map(c => c.name.toUpperCase());

      // 注入主键 F_ID
      const hasId = columnNames.includes('F_ID') || columnNames.includes('ID');
      if (!hasId) {
        table.columns.unshift({
          name: 'F_ID',
          type: 'BIGINT',
          nullable: false,
          comment: '主键（雪花ID）',
        });
      }

      // 注入 F_TENANT_ID（紧跟主键）
      const hasTenantId = columnNames.includes('F_TENANT_ID') || columnNames.includes('TENANT_ID');
      if (!hasTenantId) {
        const idIndex = table.columns.findIndex(c => c.name.toUpperCase() === 'F_ID' || c.name.toUpperCase() === 'ID');
        const insertAt = idIndex >= 0 ? idIndex + 1 : 0;
        table.columns.splice(insertAt, 0, { ...TENANT_COLUMN });
      }

      // 注入审计字段（追加到末尾）
      for (const auditCol of AUDIT_COLUMNS) {
        const exists = columnNames.includes(auditCol.name.toUpperCase());
        if (!exists) {
          table.columns.push({ ...auditCol });
        }
      }
    }

    return design;
  }

  /**
   * 校验并修正命名规范。
   */
  private normalizeNaming(design: DatabaseDesign): DatabaseDesign {
    for (const table of design.tables) {
      // 表名：UPPER_SNAKE_CASE
      if (table.name !== table.name.toUpperCase()) {
        console.warn(`[DatabaseAgent] 表名 "${table.name}" → "${table.name.toUpperCase()}"（已规范化）`);
        table.name = table.name.toUpperCase();
      }

      // 列名：F_ UPPER_SNAKE_CASE
      for (const column of table.columns) {
        // 确保 F_ 前缀
        if (!column.name.toUpperCase().startsWith('F_')) {
          const oldName = column.name;
          column.name = `F_${column.name.replace(/^F_/i, '')}`;
          console.warn(`[DatabaseAgent] 列名 "${oldName}" → "${column.name}"（已加 F_ 前缀）`);
        }
        // 确保大写
        if (column.name !== column.name.toUpperCase()) {
          const oldName = column.name;
          column.name = column.name.toUpperCase();
          console.warn(`[DatabaseAgent] 列名 "${oldName}" → "${column.name}"（已大写规范化）`);
        }
      }

      // 索引命名：IDX_{TABLE}_{COLUMN}
      for (const index of table.indexes) {
        if (!index.name.toUpperCase().startsWith('IDX_')) {
          index.name = `IDX_${table.name}_${index.columns.join('_')}`;
        }
        index.name = index.name.toUpperCase();
      }
    }

    return design;
  }

  /**
   * 校验 API 端点基本格式。
   */
  private validateApis(design: DatabaseDesign): DatabaseDesign {
    for (const api of design.apis) {
      // 确保路径以 /api/ 开头
      if (!api.path.startsWith('/api/')) {
        api.path = `/api${api.path.startsWith('/') ? '' : '/'}${api.path}`;
      }
      // 确保方法为大写
      api.method = api.method.toUpperCase() as 'GET' | 'POST' | 'PUT' | 'DELETE';
      // 默认需要认证
      if (api.requireAuth === undefined) {
        api.requireAuth = true;
      }
    }
    return design;
  }
}
