/**
 * IR ↔ Platform Schema 双向转换（官方回写通道）
 *
 * IDE 修改 → IR → 同步回 JNPF 平台。
 * 取消任意手改 .vue → IR 反解析方案。
 *
 * @jnpf-generated v5.2.0 type=ir-to-schema platform=universal
 */

import type { FormPageIR, FieldIR } from './types';

// ═══════════════════════════════════════════════════════════
// FormPageIR ↔ Platform Schema
// ═══════════════════════════════════════════════════════════

export function formIRToSchema(ir: FormPageIR): Record<string, unknown> {
  return {
    formData: JSON.stringify({
      fields: ir.fields.map(fieldToSchemaItem),
      tabs: {},
      virtualFieldList: [],
    }),
    formConfig: {
      labelWidth: (ir.config as Record<string, unknown>)?.labelWidth ?? 100,
      labelPosition: (ir.config as Record<string, unknown>)?.labelPosition ?? 'right',
      size: (ir.config as Record<string, unknown>)?.size ?? 'default',
    },
    listConfig: ir.listConfig
      ? {
          searchFields: (ir.listConfig.searchFields ?? []).map(sf => ({
            field: sf.field,
            label: sf.label,
            component: sf.component,
          })),
        }
      : undefined,
  };
}

function fieldToSchemaItem(field: FieldIR): Record<string, unknown> {
  return {
    _Model_: field.model,
    _config_: {
      label: field.label,
      tag: field.component?.pc ?? 'JnpfInput',
      jnpfKey: field.component?.jnpfKey ?? 'JnpfInput',
      required: field.config?.required ?? false,
      defaultValue: field.config?.defaultValue,
      placeholder: field.config?.placeholder,
      options: field.config?.options,
      multiple: field.config?.multiple,
    },
  };
}

export function schemaToFormIR(schema: Record<string, unknown>): FormPageIR | null {
  const formData = schema.formData;
  if (typeof formData !== 'string') return null;

  try {
    const parsed = JSON.parse(formData);
    const fields: FieldIR[] = (parsed.fields ?? []).map((f: Record<string, unknown>) => {
      const cfg = (f._config_ as Record<string, unknown>) ?? {};
      return {
        id: '',
        model: (f._Model_ as string) ?? '',
        label: (cfg.label as string) ?? '',
        component: {
          jnpfKey: (cfg.jnpfKey as string) ?? 'JnpfInput',
          pc: (cfg.tag as string) ?? 'JnpfInput',
          app: mapJnpfKeyToApp((cfg.jnpfKey as string) ?? 'JnpfInput'),
          legacyApp: 'uni-easyinput',
        },
        config: {
          required: (cfg.required as boolean) ?? false,
          defaultValue: cfg.defaultValue,
          placeholder: cfg.placeholder as string,
          options: (cfg.options as FieldIR['config']['options']) ?? [],
          multiple: (cfg.multiple as boolean) ?? false,
        },
      } as FieldIR;
    });

    return {
      type: 'form',
      id: (schema.id as string) ?? '',
      name: (schema.name as string) ?? '',
      config: schema.formConfig as FormPageIR['config'],
      fields,
      databaseFields: [],
      expressions: [],
      listConfig: schema.listConfig as FormPageIR['listConfig'],
    };
  } catch {
    return null;
  }
}

// ═══════════════════════════════════════════════════════════
// DashboardIR → Platform Schema
// ═══════════════════════════════════════════════════════════

export function dashboardIRToSchema(ir: Record<string, unknown>): Record<string, unknown> {
  return {
    dashboardData: JSON.stringify(ir),
    dashboardName: ir.name,
    dashboardSize: ir.size,
    widgetCount: Array.isArray(ir.widgets) ? ir.widgets.length : 0,
    dataSourceCount: Array.isArray(ir.dataSources) ? ir.dataSources.length : 0,
  };
}

// ═══════════════════════════════════════════════════════════
// FlowIR → Platform Schema
// ═══════════════════════════════════════════════════════════

export { flowIRToSchema } from './flow-serializer';

// ═══════════════════════════════════════════════════════════
// JSON Schema 契约导出
// ═══════════════════════════════════════════════════════════

export function exportIRSchemaContract(): Record<string, unknown> {
  return {
    $schema: 'http://json-schema.org/draft-07/schema#',
    title: 'JNPF IR Schema Contract',
    description: 'IR ↔ Platform Schema 双向转换契约 — 官方回写通道',
    version: '1.0.0',
    definitions: {
      FormPageIR: {
        description: '表单页 IR — schemaToFormIR / formIRToSchema',
      },
      DashboardIR: {
        description: '大屏 IR — dashboardIRToSchema',
      },
      FlowIR: {
        description: '工作流 IR — flowIRToSchema (re-exported from flow-serializer)',
      },
    },
    exports: {
      formIRToSchema: {
        input: 'FormPageIR',
        output: 'PlatformSchema',
      },
      schemaToFormIR: {
        input: 'PlatformSchema',
        output: 'FormPageIR | null',
      },
      dashboardIRToSchema: {
        input: 'DashboardIR',
        output: 'PlatformSchema',
      },
      flowIRToSchema: {
        input: 'FlowIR',
        output: 'PlatformSchema',
      },
    },
  };
}

// ═══════════════════════════════════════════════════════════
// 辅助
// ═══════════════════════════════════════════════════════════

function mapJnpfKeyToApp(jnpfKey: string): string {
  const map: Record<string, string> = {
    JnpfInput: 'uni-easyinput',
    JnpfTextarea: 'uni-easyinput',
    JnpfInputNumber: 'uni-easyinput',
    JnpfSelect: 'uni-data-select',
    JnpfDatePicker: 'uni-datetime-picker',
    JnpfTimePicker: 'uni-datetime-picker',
    JnpfSwitch: 'switch',
    JnpfRadio: 'uni-data-select',
    JnpfCheckbox: 'uni-data-select',
  };
  return map[jnpfKey] ?? 'uni-easyinput';
}
