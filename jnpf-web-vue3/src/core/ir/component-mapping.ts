/**
 * 组件映射表（三层）
 * JnpfKey → PC (AntDV) + App (wot-design-uni) + legacyApp (uni_modules)
 * 来源：Phase 0 组件契约扫描 + ADR-018 UniApp UI 库选型
 *
 * 映射约定：
 *   pc:        Web 端 (Ant Design Vue)
 *   app:       新 UniApp (wot-design-uni, wd- prefix)
 *   legacyApp: 旧 UniApp (uni_modules, uni- prefix)
 */

export interface ThreeLayerMapping {
  pc: string;
  app: string;
  legacyApp: string;
}

export const COMPONENT_MAPPING: Record<string, ThreeLayerMapping> = {
  // ── 输入类 ──
  JnpfInput: { pc: 'a-input', app: 'wd-input', legacyApp: 'uni-easyinput' },
  JnpfInputNumber: { pc: 'a-input-number', app: 'wd-input-number', legacyApp: 'uni-number-box' },
  JnpfTextarea: { pc: 'a-textarea', app: 'wd-textarea', legacyApp: 'uni-easyinput' },

  // ── 选择类 ──
  JnpfSelect: { pc: 'a-select', app: 'wd-select', legacyApp: 'uni-data-select' },
  JnpfRadio: { pc: 'a-radio-group', app: 'wd-radio-group', legacyApp: 'uni-data-checkbox' },
  JnpfCheckbox: { pc: 'a-checkbox-group', app: 'wd-checkbox-group', legacyApp: 'uni-data-checkbox' },
  JnpfCascader: { pc: 'a-cascader', app: 'wd-cascader', legacyApp: 'uni-data-picker' },
  JnpfTreeSelect: { pc: 'a-tree-select', app: 'wd-tree-select', legacyApp: 'uni-data-picker' },

  // ── 日期时间 ──
  JnpfDatePicker: { pc: 'a-date-picker', app: 'wd-datetime-picker', legacyApp: 'uni-datetime-picker' },
  JnpfTimePicker: { pc: 'a-time-picker', app: 'wd-datetime-picker', legacyApp: 'uni-datetime-picker' },

  // ── 开关/评分/滑块 ──
  JnpfSwitch: { pc: 'a-switch', app: 'wd-switch', legacyApp: 'switch' },
  JnpfRate: { pc: 'a-rate', app: 'wd-rate', legacyApp: 'uni-rate' },
  JnpfSlider: { pc: 'a-slider', app: 'wd-slider', legacyApp: 'uni-slider' },
  JnpfColorPicker: { pc: 'a-color-picker', app: 'wd-color-picker', legacyApp: 'view' },

  // ── 上传类 ──
  JnpfUploadImg: { pc: 'a-upload', app: 'wd-upload', legacyApp: 'uni-file-picker' },
  JnpfUploadFile: { pc: 'a-upload', app: 'wd-upload', legacyApp: 'uni-file-picker' },

  // ── 特殊 ──
  JnpfSign: { pc: 'signature-pad', app: 'wd-signature', legacyApp: 'signature-pad' },
  JnpfSignature: { pc: 'signature-pad', app: 'wd-signature', legacyApp: 'signature-pad' },
  JnpfEditor: { pc: 'rich-text-editor', app: 'wd-editor', legacyApp: 'rich-text-editor' },

  // ── 布局 ──
  JnpfRow: { pc: 'a-row', app: 'wd-row', legacyApp: 'view' },
  JnpfCol: { pc: 'a-col', app: 'wd-col', legacyApp: 'view' },
  JnpfDivider: { pc: 'a-divider', app: 'wd-divider', legacyApp: 'uni-divider' },
  JnpfAlert: { pc: 'a-alert', app: 'wd-notice-bar', legacyApp: 'uni-notice-bar' },
  JnpfTabs: { pc: 'a-tabs', app: 'wd-tabs', legacyApp: 'uni-tabs' },
  JnpfTabPane: { pc: 'a-tab-pane', app: 'wd-tab', legacyApp: 'uni-tab-item' },

  // ── 数据展示 ──
  JnpfTable: { pc: 'a-table', app: 'wd-table', legacyApp: 'uni-table' },
  JnpfList: { pc: 'a-list', app: 'wd-list', legacyApp: 'uni-list' },
  JnpfCard: { pc: 'a-card', app: 'wd-card', legacyApp: 'uni-card' },
  JnpfDescriptions: { pc: 'a-descriptions', app: 'wd-descriptions', legacyApp: 'view' },

  // ── 图表 ──
  'ECharts:Bar': { pc: 'echarts-bar', app: 'echarts-bar', legacyApp: 'echarts-bar' },
  'ECharts:Line': { pc: 'echarts-line', app: 'echarts-line', legacyApp: 'echarts-line' },
  'ECharts:Pie': { pc: 'echarts-pie', app: 'echarts-pie', legacyApp: 'echarts-pie' },
  'ECharts:Map': { pc: 'echarts-map', app: 'echarts-map', legacyApp: 'echarts-map' },
};

export function resolveComponentMapping(jnpfKey: string): ThreeLayerMapping {
  const mapping = COMPONENT_MAPPING[jnpfKey];
  if (!mapping) {
    console.warn(`[component-mapping] 未知组件: ${jnpfKey}, 降级为 a-input / wd-input / uni-easyinput`);
    return { pc: 'a-input', app: 'wd-input', legacyApp: 'uni-easyinput' };
  }
  return mapping;
}
