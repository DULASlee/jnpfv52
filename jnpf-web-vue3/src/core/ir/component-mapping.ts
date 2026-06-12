/**
 * 组件映射表
 * JnpfKey → PC 组件名 + App 组件名
 * 来源：Phase 0 组件契约扫描
 */

export const COMPONENT_MAPPING: Record<string, { pc: string; app: string }> = {
  JnpfInput: { pc: 'a-input', app: 'uni-easyinput' },
  JnpfInputNumber: { pc: 'a-input-number', app: 'uni-number-box' },
  JnpfTextarea: { pc: 'a-textarea', app: 'uni-easyinput' },
  JnpfSelect: { pc: 'a-select', app: 'uni-data-select' },
  JnpfRadio: { pc: 'a-radio-group', app: 'uni-data-checkbox' },
  JnpfCheckbox: { pc: 'a-checkbox-group', app: 'uni-data-checkbox' },
  JnpfSwitch: { pc: 'a-switch', app: 'switch' },
  JnpfDatePicker: { pc: 'a-date-picker', app: 'uni-datetime-picker' },
  JnpfTimePicker: { pc: 'a-time-picker', app: 'uni-datetime-picker' },
  JnpfRate: { pc: 'a-rate', app: 'uni-rate' },
  JnpfSlider: { pc: 'a-slider', app: 'uni-slider' },
  JnpfColorPicker: { pc: 'a-color-picker', app: 'view' },
  JnpfCascader: { pc: 'a-cascader', app: 'uni-data-picker' },
  JnpfTreeSelect: { pc: 'a-tree-select', app: 'uni-data-picker' },
  JnpfUploadImg: { pc: 'a-upload', app: 'uni-file-picker' },
  JnpfUploadFile: { pc: 'a-upload', app: 'uni-file-picker' },
  JnpfSign: { pc: 'signature-pad', app: 'signature-pad' },
  JnpfSignature: { pc: 'signature-pad', app: 'signature-pad' },
  JnpfEditor: { pc: 'rich-text-editor', app: 'rich-text-editor' },
  JnpfRow: { pc: 'a-row', app: 'view' },
  JnpfCol: { pc: 'a-col', app: 'view' },
  JnpfDivider: { pc: 'a-divider', app: 'uni-divider' },
  JnpfAlert: { pc: 'a-alert', app: 'uni-notice-bar' },
  JnpfTabs: { pc: 'a-tabs', app: 'uni-tabs' },
  JnpfTabPane: { pc: 'a-tab-pane', app: 'uni-tab-item' },
  JnpfTable: { pc: 'a-table', app: 'uni-table' },
  JnpfList: { pc: 'a-list', app: 'uni-list' },
  JnpfCard: { pc: 'a-card', app: 'uni-card' },
  JnpfDescriptions: { pc: 'a-descriptions', app: 'view' },
  'ECharts:Bar': { pc: 'echarts-bar', app: 'echarts-bar' },
  'ECharts:Line': { pc: 'echarts-line', app: 'echarts-line' },
  'ECharts:Pie': { pc: 'echarts-pie', app: 'echarts-pie' },
  'ECharts:Map': { pc: 'echarts-map', app: 'echarts-map' },
};

export function resolveComponentMapping(jnpfKey: string): { pc: string; app: string } {
  const mapping = COMPONENT_MAPPING[jnpfKey];
  if (!mapping) {
    console.warn(`[component-mapping] 未知组件类型: ${jnpfKey}，降级为 a-input / uni-easyinput`);
    return { pc: 'a-input', app: 'uni-easyinput' };
  }
  return mapping;
}
