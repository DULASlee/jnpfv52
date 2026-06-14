/**
 * 架构师智能体
 *
 * 根据需求分析结果生成完整的系统架构设计。
 * 自动注入多租户和审计字段，校验命名规范。
 *
 * @version 1.0.0
 * @module ai/agents/architect
 */

import type { LLMGateway } from '../llm/types';
import { ARCHITECT_PROMPT } from '../llm/prompts';
import type { FormPageIR } from '../../ir/types';
import { BaseAgent, type AgentContext, type AgentResponse } from './base';

// ============================================================
// 输出类型
// ============================================================

export interface ArchitectureDesign {
  /** 架构概述 */
  overview: string;
  /** 架构详情 */
  architecture: {
    modules: Array<{
      name: string;
      responsibility: string;
      dependencies: string[];
    }>;
    databaseDesign: {
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
    };
    apiDesign: {
      endpoints: Array<{
        path: string;
        method: 'GET' | 'POST' | 'PUT' | 'DELETE';
        description: string;
        requestType?: string;
        responseType?: string;
      }>;
    };
    uiDesign: {
      pages: Array<{
        name: string;
        type: 'form' | 'list' | 'dashboard' | 'detail';
        fields: string[];
      }>;
    };
  };
  /** 生成的 IR 列表 */
  irPages: FormPageIR[];
  /** 技术栈 */
  techStack: {
    framework: string;
    ui: string;
    database: string;
    cache: string;
    mq: string;
  };
  /** 架构决策记录 */
  decisions: Array<{
    decision: string;
    reason: string;
    alternatives: string[];
  }>;
}

// ============================================================
// 审计字段常量
// ============================================================

const TENANT_COLUMN = {
  name: 'TENANT_ID',
  type: 'NVARCHAR',
  length: 50,
  nullable: false,
  comment: '租户ID',
  isTenant: true,
};

const AUDIT_COLUMNS = [
  { name: 'CREATE_USER_ID', type: 'NVARCHAR', length: 50, nullable: true, comment: '创建用户', isAudit: true },
  { name: 'CREATE_TIME', type: 'DATETIME', nullable: false, comment: '创建时间', defaultValue: 'GETDATE()', isAudit: true },
  { name: 'MODIFY_USER_ID', type: 'NVARCHAR', length: 50, nullable: true, comment: '修改用户', isAudit: true },
  { name: 'MODIFY_TIME', type: 'DATETIME', nullable: true, comment: '修改时间', isAudit: true },
  { name: 'IS_DELETED', type: 'BIT', nullable: false, comment: '逻辑删除', defaultValue: '0', isAudit: true },
];

// ============================================================
// ArchitectAgent
// ============================================================

export class ArchitectAgent extends BaseAgent {
  constructor(llm: LLMGateway) {
    super(llm, ARCHITECT_PROMPT);
  }

  /**
   * 设计系统架构。
   *
   * 根据需求分析结果，生成模块划分、数据库设计、API 设计、UI 设计。
   * 返回前自动注入多租户和审计字段。
   *
   * @param requirementAnalysis - JSON 序列化后的需求分析结果
   * @param context - 执行上下文（可传入 eab）
   * @returns 架构设计
   */
  async design(requirementAnalysis: string, context: AgentContext = {}): Promise<AgentResponse<ArchitectureDesign>> {
    const result = await this.execute<ArchitectureDesign>(requirementAnalysis, context);

    // 自动注入多租户和审计字段
    result.data = this.injectAuditFields(result.data);

    // 校验并修正命名规范
    result.data = this.normalizeNaming(result.data);

    return result;
  }

  /**
   * 根据反馈优化架构。
   *
   * @param feedback - 用户反馈
   * @param currentDesign - 当前架构设计
   * @param context - 执行上下文
   * @returns 优化后的架构设计
   */
  async optimize(feedback: string, currentDesign: ArchitectureDesign, context: AgentContext = {}): Promise<AgentResponse<ArchitectureDesign>> {
    const userInput = `请根据以下反馈优化架构设计：\n\n${feedback}`;

    return this.design(userInput, {
      ...context,
      messages: [
        {
          role: 'assistant',
          content: `当前架构设计：\n${JSON.stringify(currentDesign, null, 2)}`,
        },
      ],
    });
  }

  // ============================================================
  // 自动注入
  // ============================================================

  /**
   * 自动注入多租户和审计字段。
   *
   * 如果 LLM 未生成这些字段，在返回前自动补充。
   * 遵循 R4（多租户）和 Trap 7（注入位置正确）。
   */
  private injectAuditFields(design: ArchitectureDesign): ArchitectureDesign {
    for (const table of design.architecture.databaseDesign.tables) {
      const columnNames = table.columns.map(c => c.name.toUpperCase());

      // 注入主键（如果缺失）
      const hasId = columnNames.includes('F_ID') || columnNames.includes('ID');
      if (!hasId) {
        table.columns.unshift({
          name: 'F_ID',
          type: 'BIGINT',
          nullable: false,
          comment: '主键（雪花ID）',
        });
      }

      // 注入 TENANT_ID
      const hasTenantId = columnNames.includes('F_TENANT_ID') || columnNames.includes('TENANT_ID');
      if (!hasTenantId) {
        // 放在主键之后
        const idIndex = table.columns.findIndex(c => c.name.toUpperCase() === 'F_ID' || c.name.toUpperCase() === 'ID');
        const insertAt = idIndex >= 0 ? idIndex + 1 : 0;
        table.columns.splice(insertAt, 0, {
          ...TENANT_COLUMN,
          name: this.ensureFPrefix(TENANT_COLUMN.name),
        });
      }

      // 注入审计字段（追加到末尾）
      for (const auditCol of AUDIT_COLUMNS) {
        const colName = this.ensureFPrefix(auditCol.name);
        const exists = columnNames.includes(colName.toUpperCase());
        if (!exists) {
          table.columns.push({
            ...auditCol,
            name: colName,
          });
        }
      }
    }

    return design;
  }

  /**
   * 校验并修正命名规范。
   *
   * 表名：UPPER_SNAKE_CASE
   * 列名：F_ UPPER_SNAKE_CASE（自动补 F_ 前缀）
   */
  private normalizeNaming(design: ArchitectureDesign): ArchitectureDesign {
    for (const table of design.architecture.databaseDesign.tables) {
      // 规范化表名
      if (table.name !== table.name.toUpperCase()) {
        table.name = table.name.toUpperCase();
      }

      // 规范化列名
      for (const column of table.columns) {
        if (!column.name.startsWith('F_')) {
          column.name = this.ensureFPrefix(column.name);
        }
        column.name = column.name.toUpperCase();
      }
    }

    return design;
  }

  /**
   * 确保列名有 F_ 前缀。
   */
  private ensureFPrefix(name: string): string {
    const clean = name.replace(/^F_/, '');
    return `F_${clean}`;
  }
}
