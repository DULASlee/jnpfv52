/**
 * IR 逆向转换器（逃生舱）
 *
 * 职责：将平台无关的 FormPageIR 反向转换为 JNPF VisualDev 可编辑的 JSON Schema
 *
 * 这是 Baobab-Studio Phase 1 五阶段流水线的关键环节：
 *   LLM 生成 IR → irToSchema() → VisualDev 编辑器中可视化编辑
 *
 * Round-trip 保证：
 *   Schema → cleanSchema() → FormPageIR → irToSchema() → Schema
 *   diff 应无关键差异（允许字段顺序和空格差异）
 *
 * @version 1.0.0
 */

import type { FormPageIR, FieldIR, FormConfig, ExpressionIR, ValidationRuleIR, DatabaseFieldIR, ListConfigIR, SearchFieldIR, MobileConfigIR } from './types';

// ============================================================
// 主入口
// ============================================================

/**
 * 将 FormPageIR 反向转换为 JNPF VisualDev 原生 Schema JSON
 */
export function irToSchema(ir: FormPageIR): unknown {
  const formData = buildFormData(ir);

  const result: Record<string, any> = {
    data: {
      formData: JSON.stringify(formData),
    },
  };

  // 附加配置（与 formData 平级，在 data 内）
  const data = result.data as Record<string, any>;

  if (ir.listConfig) {
    data.columnData = JSON.stringify(buildColumnData(ir.listConfig));
  }

  if (ir.mobileConfig) {
    data.appColumnData = JSON.stringify(buildAppColumnData(ir.mobileConfig));
    data.appFormData = JSON.stringify(buildAppFormData(ir.mobileConfig));
  }

  if (ir.workflow) {
    data.flowData = JSON.stringify(ir.workflow);
  }

  // 保留 @jnpf-gen:insert-point 占位符（供后续代码注入）
  data['@jnpf-gen:insert-point'] = '';

  return result;
}

// ============================================================
// formData 构建
// ============================================================

function buildFormData(ir: FormPageIR): Record<string, any> {
  // 构建表达式索引
  const exprMap = buildExpressionMap(ir.expressions);

  const formData: Record<string, any> = {
    fields: ir.fields.map(f => fieldToSchemaField(f, exprMap)),
    funcs: expressionsToFuncs(ir.expressions),
    ...configToFormData(ir.config),
  };

  // 附加元数据
  if (ir.id) formData.modelId = ir.id;
  if (ir.name) formData.fullName = ir.name;

  // 数据库字段
  if (ir.databaseFields?.length) {
    formData.virtualFieldList = ir.databaseFields.map(dbFieldToVirtualField);
  }

  return formData;
}

// ============================================================
// 字段逆向：FieldIR → __vModel__ + __config__ 结构
// ============================================================

function fieldToSchemaField(ir: FieldIR, exprMap: Map<string, string>): Record<string, any> {
  const cfg = ir.config;

  // __config__ 重建
  const config: Record<string, any> = {
    label: ir.label,
    tag: ir.component.jnpfKey,
    jnpfKey: ir.component.jnpfKey,
    required: cfg.required ?? false,
    defaultValue: cfg.defaultValue ?? '',
    trigger: ir.validation?.[0]?.trigger ?? 'blur',
    regList: validationToRegList(ir.validation),
    disabled: cfg.disabled ?? false,
    readonly: cfg.readonly ?? false,
    hidden: cfg.hidden ?? false,
    span: cfg.span ?? 24,
  };

  // 可选配置
  if (cfg.labelWidth !== null) config.labelWidth = cfg.labelWidth;
  if (cfg.maxlength !== null) config.maxlength = cfg.maxlength;
  if (cfg.showWordLimit) config.showWordLimit = cfg.showWordLimit;
  if (!cfg.clearable) config.clearable = false;
  if (cfg.min !== null) config.min = cfg.min;
  if (cfg.max !== null) config.max = cfg.max;
  if (cfg.precision !== null) config.precision = cfg.precision;
  if (cfg.step !== null) config.step = cfg.step;
  if (cfg.multiple) config.multiple = cfg.multiple;
  if (Object.keys(cfg.style).length > 0) config.style = cfg.style;

  // 选项
  if (cfg.options?.length > 0) {
    config.options = cfg.options.map(o => ({
      label: o.label,
      value: o.value,
      ...(o.disabled !== undefined && { disabled: o.disabled }),
    }));
  }

  // 字典
  if (cfg.dictType) {
    config.dataType = 'dictionary';
    config.dictionaryType = cfg.dictType;
  }

  // 关联数据
  if (cfg.relationData) {
    config.relationData = cfg.relationData;
  }

  // 重建 on 事件
  const on: Record<string, string> = {};
  if (ir.events.change && exprMap.has(ir.events.change)) {
    on.change = exprMap.get(ir.events.change)!;
  }
  if (ir.events.blur && exprMap.has(ir.events.blur)) {
    on.blur = exprMap.get(ir.events.blur)!;
  }
  if (ir.events.focus && exprMap.has(ir.events.focus)) {
    on.focus = exprMap.get(ir.events.focus)!;
  }
  if (ir.events.click && exprMap.has(ir.events.click)) {
    on.click = exprMap.get(ir.events.click)!;
  }

  const field: Record<string, any> = {
    __vModel__: ir.model,
    __config__: config,
  };

  if (cfg.placeholder) {
    field.placeholder = cfg.placeholder;
  }

  if (Object.keys(on).length > 0) {
    field.on = on;
  }

  return field;
}

// ============================================================
// 校验规则逆向
// ============================================================

function validationToRegList(validation: ValidationRuleIR[]): any[] {
  if (!validation?.length) return [];

  const regList: any[] = [];
  const requiredRule = validation.find(v => v.type === 'required');

  if (requiredRule) {
    regList.push({
      required: true,
      message: requiredRule.message,
      trigger: requiredRule.trigger,
    });
  }

  for (const rule of validation) {
    if (rule.type === 'required') continue;

    if (rule.type === 'pattern' && rule.pattern) {
      regList.push({
        pattern: rule.pattern,
        message: rule.message,
        trigger: rule.trigger,
      });
    } else if (rule.type === 'string' || rule.type === 'number') {
      const entry: any = {
        message: rule.message,
        trigger: rule.trigger,
      };
      if (rule.min !== undefined) entry.min = rule.min;
      if (rule.max !== undefined) entry.max = rule.max;
      regList.push(entry);
    } else {
      // custom, email, phone 等
      regList.push({
        pattern: rule.pattern || '',
        message: rule.message,
        trigger: rule.trigger,
      });
    }
  }

  return regList;
}

// ============================================================
// 表达式逆向
// ============================================================

function buildExpressionMap(expressions: ExpressionIR[]): Map<string, string> {
  const map = new Map<string, string>();
  for (const expr of expressions) {
    map.set(expr.id, expr.originalCode);
  }
  return map;
}

function expressionsToFuncs(expressions: ExpressionIR[]): Record<string, string> {
  const funcs: Record<string, string> = {};
  for (const expr of expressions) {
    if (expr.type === 'form-lifecycle') {
      funcs[expr.name] = expr.originalCode;
    }
  }
  return funcs;
}

// ============================================================
// 表单配置逆向
// ============================================================

function configToFormData(config: FormConfig): Record<string, any> {
  return {
    labelPosition: config.labelPosition,
    labelWidth: config.labelWidth,
    labelSuffix: config.labelSuffix,
    size: config.size,
    disabled: config.disabled,
    span: config.span,
    gutter: config.gutter,
    colon: config.colon,
    popupType: config.popupType,
    generalWidth: config.generalWidth,
    fullScreenWidth: config.fullScreenWidth,
    drawerWidth: config.drawerWidth,
    hasCancelBtn: config.hasCancelBtn,
    cancelButtonText: config.cancelButtonText,
    hasConfirmBtn: config.hasConfirmBtn,
    confirmButtonText: config.confirmButtonText,
    hasConfirmAndAddBtn: config.hasConfirmAndAddBtn,
    hasPrintBtn: config.hasPrintBtn,
    printButtonText: config.printButtonText,
    primaryKeyPolicy: config.primaryKeyPolicy,
    tablePolicy: config.tablePolicy,
    concurrencyLock: config.concurrencyLock,
    logicalDelete: config.logicalDelete,
  };
}

// ============================================================
// 数据库字段逆向
// ============================================================

function dbFieldToVirtualField(ir: DatabaseFieldIR): Record<string, any> {
  return {
    field: ir.name,
    name: ir.name,
    type: ir.type,
    length: ir.length,
    nullable: ir.nullable,
    defaultValue: ir.defaultValue,
    description: ir.description,
  };
}

// ============================================================
// 列表配置逆向
// ============================================================

function buildColumnData(listConfig: ListConfigIR): Record<string, any> {
  return {
    columnList: listConfig.columns.map(col => ({
      prop: col.field,
      label: col.label,
      width: col.width,
      fixed: col.fixed,
      sortable: col.sortable,
      ...(col.formatter && { formatter: col.formatter }),
    })),
    searchList: listConfig.searchFields.map(sf => searchFieldToSchema(sf)),
    ruleList: listConfig.ruleList || [],
  };
}

function searchFieldToSchema(sf: SearchFieldIR): Record<string, any> {
  return {
    __vModel__: sf.field,
    __config__: {
      label: sf.label,
      jnpfKey: sf.component,
      ...(sf.options?.length > 0 && { options: sf.options }),
    },
  };
}

// ============================================================
// 移动端配置逆向
// ============================================================

function buildAppColumnData(mobileConfig: MobileConfigIR): Record<string, any> {
  return {
    columnList: mobileConfig.listColumns.map(col => ({
      prop: col.field,
      label: col.label,
      width: col.width,
      fixed: col.fixed,
      sortable: col.sortable,
    })),
  };
}

function buildAppFormData(mobileConfig: MobileConfigIR): Record<string, any> {
  return {
    fields: mobileConfig.formFields.map(f => fieldToSchemaField(f, new Map())),
    // 移动端无需独立表达式 map
  };
}
