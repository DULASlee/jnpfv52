/**
 * Schema 清洗器
 *
 * 职责：将 JNPF 平台原生 Schema JSON 转换为平台无关的 IR
 *
 * 处理：
 *   1. 双层 JSON 解包（JNPF 数据是 JSON 套 JSON）
 *   2. __vModel__ → model 字段映射
 *   3. __config__ → config 字段展开
 *   4. tag → JnpfKey 组件映射
 *   5. 嵌入式箭头函数 → ExpressionIR（不清洗代码本身，只标记和分类）
 *   6. DataScreen / UniApp 配置提取到 listConfig / mobileConfig
 *
 * 安全：此模块不执行任何用户代码，只对 JSON 做结构转换 + 字符串分类。
 */

import type {
  FormPageIR,
  FieldIR,
  FormConfig,
  ValidationRuleIR,
  OptionItemIR,
  ExpressionIR,
  DatabaseFieldIR,
  ListConfigIR,
  ColumnIR,
  SearchFieldIR,
  MobileConfigIR,
  WorkflowIR,
} from './types';
import { resolveComponentMapping } from './component-mapping';
import { classifyExpression } from './expression-classifier';

// ============================================================
// 主入口
// ============================================================

export function cleanSchema(rawJson: unknown): FormPageIR {
  const data = unwrapDoubleJson(rawJson);
  const config = extractFormConfig(data);
  const { fields, expressions: fieldExprs } = extractFields(data.fields || []);
  const lifecycleExprs = extractLifecycleExpressions(data.funcs);
  const expressions = [...lifecycleExprs, ...fieldExprs];
  const databaseFields = extractDatabaseFields(data.virtualFieldList || []);
  const listConfig = extractListConfig(data.columnData);
  const mobileConfig = extractMobileConfig(data.appColumnData, data.appFormData);
  const workflow = extractWorkflow(data.flowData);

  return {
    type: 'form',
    id: String(data.id || data.modelId || generateId()),
    name: String(data.fullName || data.enCode || data.name || 'unnamed'),
    config,
    fields,
    databaseFields,
    expressions,
    ...(listConfig && { listConfig }),
    ...(mobileConfig && { mobileConfig }),
    ...(workflow && { workflow }),
  };
}

// ============================================================
// 双层 JSON 解包
// ============================================================

function unwrapDoubleJson(raw: unknown): Record<string, any> {
  if (typeof raw === 'string') {
    try {
      return unwrapDoubleJson(JSON.parse(raw));
    } catch {
      return {};
    }
  }

  if (raw && typeof raw === 'object' && !Array.isArray(raw)) {
    const obj = raw as Record<string, any>;

    // JNPF 的双层 JSON 模式：{ data: { formData: "{ ... }" } }
    if (obj.data && typeof obj.data === 'object') {
      const inner = obj.data as Record<string, any>;
      // formData 是一个 JSON 字符串
      if (typeof inner.formData === 'string') {
        try {
          return { ...inner, ...JSON.parse(inner.formData) };
        } catch {
          return inner;
        }
      }
      return inner;
    }

    // 单层模式：{ formData: "{ ... }" }
    if (typeof obj.formData === 'string') {
      try {
        return { ...obj, ...JSON.parse(obj.formData) };
      } catch {
        return obj;
      }
    }

    return obj;
  }

  return {};
}

// ============================================================
// 表单配置提取
// ============================================================

function extractFormConfig(data: Record<string, any>): FormConfig {
  return {
    labelPosition: data.labelPosition || 'right',
    labelWidth: data.labelWidth ?? 100,
    labelSuffix: data.labelSuffix || '',
    size: data.size || 'default',
    disabled: data.disabled ?? false,
    span: data.span ?? 24,
    gutter: data.gutter ?? 0,
    colon: data.colon ?? true,
    popupType: data.popupType || 'general',
    generalWidth: data.generalWidth || '800px',
    fullScreenWidth: data.fullScreenWidth || '80%',
    drawerWidth: data.drawerWidth || '50%',
    hasCancelBtn: data.hasCancelBtn ?? true,
    cancelButtonText: data.cancelButtonText || '取消',
    hasConfirmBtn: data.hasConfirmBtn ?? true,
    confirmButtonText: data.confirmButtonText || '确认',
    hasConfirmAndAddBtn: data.hasConfirmAndAddBtn ?? false,
    hasPrintBtn: data.hasPrintBtn ?? false,
    printButtonText: data.printButtonText || '打印',
    primaryKeyPolicy: data.primaryKeyPolicy || 'uuid',
    tablePolicy: data.tablePolicy || 'auto',
    concurrencyLock: data.concurrencyLock ?? false,
    logicalDelete: data.logicalDelete ?? false,
  };
}

// ============================================================
// 字段提取
// ============================================================

function extractFields(rawFields: any[]): { fields: FieldIR[]; expressions: ExpressionIR[] } {
  const fields: FieldIR[] = [];
  const expressions: ExpressionIR[] = [];

  for (const raw of rawFields) {
    const cfg = raw.__config__ || raw.config || {};
    const jnpfKey = cfg.jnpfKey || cfg.tag || 'JnpfInput';
    const mapping = resolveComponentMapping(jnpfKey);

    // 提取选项
    const options = extractOptions(raw.options || cfg.options || [], cfg.dataType, cfg.dictionaryType);

    // 提取校验规则
    const validation = extractValidation(cfg.regList || []);

    // 提取字段事件表达式
    const on = raw.on || {};
    for (const [trigger, code] of Object.entries(on)) {
      if (typeof code === 'string' && code.trim()) {
        const exprId = `field-${raw.__vModel__ || 'unknown'}-${trigger}`;
        const classification = classifyExpression(code);
        expressions.push({
          id: exprId,
          name: `${raw.__vModel__ || 'unknown'}.${trigger}`,
          type: 'field-event',
          params: classification.params,
          body: classification.body,
          level: classification.level,
          isAsync: classification.isAsync,
          originalCode: code,
        });
      }
    }

    fields.push({
      id: raw.__vModel__ || `field-${fields.length}`,
      model: raw.__vModel__ || '',
      label: cfg.label || raw.label || '',
      component: {
        jnpfKey,
        pc: mapping.pc,
        app: mapping.app,
        legacyApp: mapping.legacyApp,
      },
      config: {
        required: cfg.required ?? false,
        defaultValue: cfg.defaultValue ?? undefined,
        placeholder: raw.placeholder || '',
        disabled: cfg.disabled ?? false,
        readonly: cfg.readonly ?? false,
        hidden: cfg.hidden ?? false,
        span: cfg.span ?? cfg.colSpan ?? 24,
        labelWidth: cfg.labelWidth ?? null,
        maxlength: cfg.maxlength ?? null,
        showWordLimit: cfg.showWordLimit ?? false,
        clearable: cfg.clearable ?? true,
        min: cfg.min ?? null,
        max: cfg.max ?? null,
        precision: cfg.precision ?? null,
        step: cfg.step ?? null,
        multiple: cfg.multiple ?? cfg.searchMultiple ?? false,
        options,
        dictType: cfg.dictionaryType || cfg.dictType || null,
        relationData: cfg.relationData
          ? {
              url: cfg.relationData.url || null,
              labelField: cfg.relationData.labelField || null,
              valueField: cfg.relationData.valueField || null,
            }
          : null,
        style: cfg.style || {},
      },
      validation,
      events: {
        change: typeof on.change === 'string' ? `field-${raw.__vModel__}-change` : undefined,
        blur: typeof on.blur === 'string' ? `field-${raw.__vModel__}-blur` : undefined,
        focus: typeof on.focus === 'string' ? `field-${raw.__vModel__}-focus` : undefined,
        click: typeof on.click === 'string' ? `field-${raw.__vModel__}-click` : undefined,
      },
      aiHints: cfg.label
        ? {
            semantic: inferSemanticRole(raw.__vModel__, cfg.label),
            suggestedValidation: null as unknown as undefined,
            suggestedDefault: null as unknown as undefined,
          }
        : undefined,
    });
  }

  return { fields, expressions };
}

// ============================================================
// 生命周期表达式提取
// ============================================================

function extractLifecycleExpressions(funcs: Record<string, string> | undefined): ExpressionIR[] {
  if (!funcs) return [];
  const expressions: ExpressionIR[] = [];

  for (const [name, code] of Object.entries(funcs)) {
    if (typeof code !== 'string' || !code.trim()) continue;
    const classification = classifyExpression(code);
    expressions.push({
      id: `lifecycle-${name}`,
      name,
      type: 'form-lifecycle',
      params: classification.params,
      body: classification.body,
      level: classification.level,
      isAsync: classification.isAsync,
      originalCode: code,
    });
  }

  return expressions;
}

// ============================================================
// 数据库字段提取
// ============================================================

function extractDatabaseFields(virtualFieldList: any[]): DatabaseFieldIR[] {
  return virtualFieldList.map(f => ({
    id: f.field || f.name || '',
    name: f.field || f.name || '',
    type: f.type || f.dataType || 'varchar',
    length: f.length ?? null,
    nullable: f.nullable ?? true,
    defaultValue: f.defaultValue ?? null,
    description: f.description || f.comment || '',
  }));
}

// ============================================================
// 列表配置提取
// ============================================================

function extractListConfig(columnData: any): ListConfigIR | undefined {
  if (!columnData) return undefined;

  const parsed = typeof columnData === 'string' ? safeJsonParse(columnData) : columnData;
  if (!parsed) return undefined;

  const columns: ColumnIR[] = (parsed.columnList || []).map((c: any) => ({
    field: c.prop || c.field || '',
    label: c.label || '',
    width: c.width ?? null,
    fixed: c.fixed || null,
    sortable: c.sortable ?? false,
    formatter: c.formatter || undefined,
  }));

  const searchFields: SearchFieldIR[] = extractSearchFields(parsed.searchList || []);

  return {
    searchFields,
    columns,
    ruleList: parsed.ruleList || [],
  };
}

function extractSearchFields(searchList: any[]): SearchFieldIR[] {
  return searchList.map((s: any) => ({
    field: s.__vModel__ || s.field || '',
    label: s.__config__?.label || s.label || '',
    component: s.__config__?.jnpfKey || s.component || 'Input',
    options: extractOptions(s.options || s.__config__?.options || []),
  }));
}

// ============================================================
// 移动端配置提取
// ============================================================

function extractMobileConfig(appColumnData: any, appFormData: any): MobileConfigIR | undefined {
  const formData = appFormData ? (typeof appFormData === 'string' ? safeJsonParse(appFormData) : appFormData) : null;
  const colData = appColumnData ? (typeof appColumnData === 'string' ? safeJsonParse(appColumnData) : appColumnData) : null;

  if (!formData && !colData) return undefined;

  const formFields: FieldIR[] = formData?.fields ? extractFields(formData.fields).fields : [];
  const listColumns: ColumnIR[] = (colData?.columnList || []).map((c: any) => ({
    field: c.prop || c.field || '',
    label: c.label || '',
    width: c.width ?? null,
    fixed: c.fixed || null,
    sortable: c.sortable ?? false,
  }));

  return { formFields, listColumns };
}

// ============================================================
// 工作流配置提取
// ============================================================

function extractWorkflow(flowData: any): WorkflowIR | undefined {
  if (!flowData) return undefined;
  const parsed = typeof flowData === 'string' ? safeJsonParse(flowData) : flowData;
  if (!parsed) return undefined;

  return {
    flowId: parsed.flowId || '',
    flowType: parsed.flowType || '',
    templateList: parsed.templateList || [],
  };
}

// ============================================================
// 选项提取
// ============================================================

function extractOptions(rawOptions: any[], dataType?: string, dictionaryType?: string): OptionItemIR[] {
  if (dataType === 'dictionary' && dictionaryType) {
    return []; // 字典选项在运行时加载，IR 中留空
  }
  if (dataType === 'dynamic') {
    return []; // 动态选项在运行时加载
  }
  return (rawOptions || []).map((o: any) => ({
    label: o.label || o.fullName || o.name || '',
    value: o.value ?? o.id ?? o,
    disabled: o.disabled ?? false,
  }));
}

// ============================================================
// 校验规则提取
// ============================================================

function extractValidation(regList: any[]): ValidationRuleIR[] {
  return (regList || []).map((r: any) => {
    if (r.required !== undefined) {
      return {
        type: 'required',
        message: r.message || '此项必填',
        trigger: (r.trigger as 'blur' | 'change') || 'blur',
      };
    }
    if (r.pattern) {
      const patternStr = typeof r.pattern === 'string' ? r.pattern : String(r.pattern);
      return {
        type: 'pattern',
        pattern: patternStr,
        message: r.message || '格式不正确',
        trigger: (r.trigger as 'blur' | 'change') || 'blur',
      };
    }
    if (r.min !== undefined || r.max !== undefined) {
      return {
        type: 'string',
        min: r.min,
        max: r.max,
        message: r.message || '长度不符合要求',
        trigger: (r.trigger as 'blur' | 'change') || 'blur',
      };
    }
    return {
      type: 'pattern',
      pattern: r.pattern || '',
      message: r.message || '',
      trigger: (r.trigger as 'blur' | 'change') || 'blur',
    };
  });
}

// ============================================================
// 语义角色推断（AI 探针）
// ============================================================

function inferSemanticRole(model: string, label: string): string {
  const combined = `${model} ${label}`.toLowerCase();
  if (/email|邮箱|邮件/.test(combined)) return 'email';
  if (/phone|mobile|tel|电话|手机/.test(combined)) return 'phone';
  if (/price|amount|money|金额|价格|费用/.test(combined)) return 'currency';
  if (/date|time|时间|日期/.test(combined)) return 'datetime';
  if (/url|link|链接|地址/.test(combined)) return 'url';
  if (/name|姓名|名称|title/.test(combined)) return 'name';
  if (/id|编号|序号|code/.test(combined)) return 'identifier';
  if (/desc|remark|note|备注|描述|说明/.test(combined)) return 'description';
  if (/status|state|状态/.test(combined)) return 'status';
  if (/image|img|photo|图片|照片/.test(combined)) return 'image';
  if (/file|attachment|附件|文件/.test(combined)) return 'file';
  return 'text';
}

// ============================================================
// 工具函数
// ============================================================

function safeJsonParse(str: string): any {
  try {
    return JSON.parse(str);
  } catch {
    return null;
  }
}

function generateId(): string {
  return `ir-${Date.now()}-${Math.random().toString(36).slice(2, 9)}`;
}
