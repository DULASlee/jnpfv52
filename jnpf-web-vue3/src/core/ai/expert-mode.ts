/**
 * 无AI专家模式 / 逃生舱
 *
 * 当 LLM 服务不可用或置信度不足时，提供手动工具集。
 * 所有手动工具产出与 AI 生成的 IR 完全同构，可无缝切换。
 *
 * 工具集：
 *   1. 领域模型画板 — 拖拽式实体关系设计
 *   2. 架构图计算器 — 从 EAB 快照选择组件
 *   3. 决策表编辑器 — 引用 rules/engine.ts
 *   4. 表单设计器 — 已有，不改变
 *   5. 大屏设计器 — 已有，不改变
 *
 * @version 1.0.0
 * @module ai/expert-mode
 */

import type { LLMGateway } from './llm/types';
import type { FormPageIR, FieldIR, FormConfig, ComponentMapping } from '../ir/types';
import type { ArchitectureDesign } from './agents/architect';
import type { DatabaseDesign } from './agents/database';

// ============================================================
// 模式配置
// ============================================================

export interface ExpertModeConfig {
  /** AI 是否启用 */
  aiEnabled: boolean;
  /** AI 状态 */
  aiStatus: 'healthy' | 'degraded' | 'offline';
  /** 降级原因 */
  degradeReason?: string;
}

/**
 * 自动检测 AI 模式。
 * 通过健康检查判断 LLM 服务是否可用。
 */
export async function detectAIMode(llm: LLMGateway): Promise<ExpertModeConfig> {
  try {
    const healthy = await llm.healthCheck();
    return {
      aiEnabled: healthy,
      aiStatus: healthy ? 'healthy' : 'degraded',
      degradeReason: healthy ? undefined : 'LLM 服务健康检查失败',
    };
  } catch (e) {
    return {
      aiEnabled: false,
      aiStatus: 'offline',
      degradeReason: `AI 服务不可达: ${(e as Error).message}`,
    };
  }
}

// ============================================================
// IR 同构工具
// ============================================================

/** 空 FormPageIR 模板（与 AI 生成的 IR 结构完全一致） */
export function createEmptyFormIR(overrides: Partial<FormPageIR> = {}): FormPageIR {
  return {
    type: 'form',
    id: overrides.id ?? '',
    name: overrides.name ?? '',
    config: createDefaultFormConfig(overrides.config),
    fields: overrides.fields ?? [],
    databaseFields: overrides.databaseFields ?? [],
    expressions: overrides.expressions ?? [],
    aiHints: {
      domain: overrides.aiHints?.domain ?? '',
      designRationale: '手动创建（专家模式）',
      ...overrides.aiHints,
    },
    ...overrides,
  };
}

/** 默认表单配置 */
function createDefaultFormConfig(overrides?: Partial<FormConfig>): FormConfig {
  return {
    labelPosition: 'right',
    labelWidth: 100,
    labelSuffix: '：',
    size: 'default',
    disabled: false,
    span: 24,
    gutter: 16,
    colon: true,
    popupType: 'general',
    generalWidth: '800px',
    fullScreenWidth: '100%',
    drawerWidth: '520px',
    hasCancelBtn: true,
    cancelButtonText: '取消',
    hasConfirmBtn: true,
    confirmButtonText: '保存',
    hasConfirmAndAddBtn: false,
    hasPrintBtn: false,
    printButtonText: '打印',
    primaryKeyPolicy: 'snowflake',
    tablePolicy: 'auto',
    concurrencyLock: false,
    logicalDelete: true,
    ...overrides,
  };
}

/** 创建字段 IR（与 AI 生成的字段结构一致） */
export function createFieldIR(overrides: Partial<FieldIR> & { model: string; label: string }): FieldIR {
  const jnpfKey = overrides.component?.jnpfKey ?? 'JnpfInput';
  return {
    id: overrides.id ?? `${overrides.model}_${Date.now()}`,
    model: overrides.model,
    label: overrides.label,
    component: overrides.component ?? {
      jnpfKey,
      pc: mapJnpfToPc(jnpfKey),
      app: mapJnpfToApp(jnpfKey),
      legacyApp: mapJnpfToApp(jnpfKey),
    },
    config: {
      required: false,
      defaultValue: '',
      placeholder: `请输入${overrides.label}`,
      disabled: false,
      readonly: false,
      hidden: false,
      span: 12,
      labelWidth: null,
      maxlength: null,
      showWordLimit: false,
      clearable: true,
      min: null,
      max: null,
      precision: null,
      step: null,
      multiple: false,
      options: [],
      dictType: null,
      relationData: null,
      style: {},
      ...overrides.config,
    },
    validation: overrides.validation ?? [],
    events: overrides.events ?? {},
    ...overrides,
  };
}

// ============================================================
// 领域模型画板
// ============================================================

/** 人工构建的实体关系模型（与 AI 领域模型同构） */
export interface ManualDomainModel {
  entities: Array<{
    name: string;
    fields: Array<{ name: string; type: string; label: string }>;
  }>;
  relationships: Array<{
    from: string;
    to: string;
    type: 'one-to-many' | 'many-to-many' | 'one-to-one';
  }>;
  businessRules: Array<{
    name: string;
    condition: string;
    action: string;
  }>;
}

/** 从人工领域模型生成架构设计（与 AI 架构师输出同构） */
export function domainModelToArchitecture(model: ManualDomainModel): ArchitectureDesign {
  const tables = model.entities.map(entity => ({
    name: entity.name.toUpperCase().replace(/\s+/g, '_'),
    comment: `${entity.name}表`,
    columns: [
      { name: 'F_ID', type: 'BIGINT', nullable: false, comment: '主键（雪花ID）' },
      { name: 'F_TENANT_ID', type: 'NVARCHAR', length: 50, nullable: false, comment: '租户ID', isTenant: true },
      ...entity.fields.map(f => ({
        name: `F_${f.name.toUpperCase()}`,
        type: mapDomainTypeToDb(f.type),
        length: f.type === 'string' ? 200 : undefined,
        nullable: true,
        comment: f.label,
      })),
      { name: 'F_CREATE_USER_ID', type: 'NVARCHAR', length: 50, nullable: true, comment: '创建用户', isAudit: true },
      { name: 'F_CREATE_TIME', type: 'DATETIME', nullable: false, comment: '创建时间', defaultValue: 'GETDATE()', isAudit: true },
      { name: 'F_MODIFY_USER_ID', type: 'NVARCHAR', length: 50, nullable: true, comment: '修改用户', isAudit: true },
      { name: 'F_MODIFY_TIME', type: 'DATETIME', nullable: true, comment: '修改时间', isAudit: true },
      { name: 'F_IS_DELETED', type: 'BIT', nullable: false, comment: '逻辑删除', defaultValue: '0', isAudit: true },
    ],
    indexes: [],
  }));

  const modules = model.entities.map(e => ({
    name: e.name.replace(/\s+/g, ''),
    responsibility: `${e.name}管理`,
    dependencies: [] as string[],
  }));

  const pages = model.entities.map(e => ({
    name: e.name,
    type: 'form' as const,
    fields: e.fields.map(f => f.name),
  }));

  return {
    overview: `手动创建的${model.entities.map(e => e.name).join('、')}管理系统`,
    architecture: {
      modules,
      databaseDesign: { tables, indexes: [] },
      apiDesign: {
        endpoints: model.entities.flatMap(e => [
          { path: `/api/${e.name.toLowerCase().replace(/\s+/g, '-')}/list`, method: 'GET' as const, description: `${e.name}列表` },
          { path: `/api/${e.name.toLowerCase().replace(/\s+/g, '-')}`, method: 'POST' as const, description: `创建${e.name}` },
          { path: `/api/${e.name.toLowerCase().replace(/\s+/g, '-')}/{id}`, method: 'PUT' as const, description: `修改${e.name}` },
          { path: `/api/${e.name.toLowerCase().replace(/\s+/g, '-')}/{id}`, method: 'DELETE' as const, description: `删除${e.name}` },
        ]),
      },
      uiDesign: { pages },
    },
    irPages: [],
    techStack: {
      framework: '.NET 8 + JNPF',
      ui: 'Vue3 + Ant Design Vue',
      database: 'SQL Server + SqlSugar',
      cache: 'Memory Cache',
      mq: 'Channel (In-Process)',
    },
    decisions: [{ decision: '使用雪花ID作为主键', reason: '分布式唯一', alternatives: ['GUID', '自增'] }],
  };
}

/** 从人工领域模型生成数据库设计（与 AI 数据库设计师输出同构） */
export function domainModelToDatabase(model: ManualDomainModel): DatabaseDesign {
  const arch = domainModelToArchitecture(model);
  return {
    overview: arch.overview,
    tables: arch.architecture.databaseDesign.tables.map(t => ({
      name: t.name,
      comment: t.comment,
      columns: t.columns.map(c => ({
        name: c.name,
        type: c.type,
        length: c.length ?? null,
        nullable: c.nullable,
        defaultValue: c.defaultValue ?? null,
        comment: c.comment,
        isAudit: (c as Record<string, unknown>).isAudit as boolean | undefined,
        isTenant: (c as Record<string, unknown>).isTenant as boolean | undefined,
      })),
      indexes: [],
    })),
    migrationSql: generateMigrationSql(tables),
    apis: arch.architecture.apiDesign.endpoints.map(ep => ({
      path: ep.path,
      method: ep.method as 'GET' | 'POST' | 'PUT' | 'DELETE',
      description: ep.description,
      requireAuth: true,
    })),
  };
}

// ============================================================
// 辅助函数
// ============================================================

/** JNPF → PC 组件映射 */
function mapJnpfToPc(jnpfKey: string): string {
  const map: Record<string, string> = {
    JnpfInput: 'a-input',
    JnpfInputNumber: 'a-input-number',
    JnpfTextarea: 'a-textarea',
    JnpfSelect: 'a-select',
    JnpfRadio: 'a-radio-group',
    JnpfCheckbox: 'a-checkbox-group',
    JnpfCascader: 'a-cascader',
    JnpfTreeSelect: 'a-tree-select',
    JnpfDatePicker: 'a-date-picker',
    JnpfTimePicker: 'a-time-picker',
    JnpfSwitch: 'a-switch',
    JnpfRate: 'a-rate',
    JnpfSlider: 'a-slider',
    JnpfUploadImg: 'a-upload',
    JnpfUploadFile: 'a-upload',
    JnpfTable: 'a-table',
    JnpfCard: 'a-card',
  };
  return map[jnpfKey] ?? 'a-input';
}

function mapJnpfToApp(jnpfKey: string): string {
  const map: Record<string, string> = {
    JnpfInput: 'uni-easyinput',
    JnpfInputNumber: 'uni-number-box',
    JnpfTextarea: 'uni-easyinput',
    JnpfSelect: 'uni-data-select',
    JnpfRadio: 'uni-data-checkbox',
    JnpfCheckbox: 'uni-data-checkbox',
    JnpfCascader: 'uni-data-picker',
    JnpfTreeSelect: 'uni-data-picker',
    JnpfDatePicker: 'uni-datetime-picker',
    JnpfTimePicker: 'uni-datetime-picker',
    JnpfSwitch: 'switch',
    JnpfRate: 'uni-rate',
    JnpfSlider: 'uni-slider',
    JnpfUploadImg: 'uni-file-picker',
    JnpfUploadFile: 'uni-file-picker',
    JnpfTable: 'uni-table',
    JnpfCard: 'uni-card',
  };
  return map[jnpfKey] ?? 'uni-easyinput';
}

function mapDomainTypeToDb(type: string): string {
  const map: Record<string, string> = {
    string: 'NVARCHAR',
    number: 'DECIMAL',
    boolean: 'BIT',
    datetime: 'DATETIME',
    date: 'DATE',
    text: 'TEXT',
    email: 'NVARCHAR',
    phone: 'NVARCHAR',
  };
  return map[type] ?? 'NVARCHAR';
}

function generateMigrationSql(
  tables: Array<{
    name: string;
    comment: string;
    columns: Array<{ name: string; type: string; length?: number | null; nullable: boolean; defaultValue?: string | null; comment: string }>;
  }>,
): string {
  return tables
    .map(t => {
      const cols = t.columns
        .map(c => {
          const type = c.length ? `${c.type}(${c.length})` : c.type;
          const nullable = c.nullable ? 'NULL' : 'NOT NULL';
          const def = c.defaultValue ? ` DEFAULT ${c.defaultValue}` : '';
          return `  ${c.name} ${type} ${nullable}${def} -- ${c.comment}`;
        })
        .join(',\n');
      return `CREATE TABLE ${t.name} (\n${cols}\n);\n`;
    })
    .join('\n');
}
