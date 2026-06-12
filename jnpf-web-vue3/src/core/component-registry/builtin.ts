/**
 * 内置组件注册
 *
 * 来源：Phase 0 组件契约扫描 + F-1 component-mapping.ts
 * 版本号：基于 JNPF V5.2 的实际依赖版本
 */

import type { ComponentEntry } from './types';

export const BUILTIN_COMPONENTS: ComponentEntry[] = [
  // ============================================================
  // 表单输入类 (3)
  // ============================================================
  {
    type: 'JnpfInput',
    name: '输入框',
    category: 'form-input',
    pc: 'a-input',
    app: 'uni-easyinput',
    version: '1.0.0',
  },
  {
    type: 'JnpfInputNumber',
    name: '数字输入框',
    category: 'form-input',
    pc: 'a-input-number',
    app: 'uni-number-box',
    version: '1.0.0',
  },
  {
    type: 'JnpfTextarea',
    name: '文本域',
    category: 'form-input',
    pc: 'a-textarea',
    app: 'uni-easyinput',
    version: '1.0.0',
  },

  // ============================================================
  // 表单选择类 (5)
  // ============================================================
  {
    type: 'JnpfSelect',
    name: '下拉选择',
    category: 'form-select',
    pc: 'a-select',
    app: 'uni-data-select',
    version: '1.0.0',
  },
  {
    type: 'JnpfRadio',
    name: '单选框',
    category: 'form-select',
    pc: 'a-radio-group',
    app: 'uni-data-checkbox',
    version: '1.0.0',
  },
  {
    type: 'JnpfCheckbox',
    name: '多选框',
    category: 'form-select',
    pc: 'a-checkbox-group',
    app: 'uni-data-checkbox',
    version: '1.0.0',
  },
  {
    type: 'JnpfCascader',
    name: '级联选择',
    category: 'form-select',
    pc: 'a-cascader',
    app: 'uni-data-picker',
    version: '1.0.0',
  },
  {
    type: 'JnpfTreeSelect',
    name: '树选择',
    category: 'form-select',
    pc: 'a-tree-select',
    app: 'uni-data-picker',
    version: '1.0.0',
  },

  // ============================================================
  // 日期时间类 (2)
  // ============================================================
  {
    type: 'JnpfDatePicker',
    name: '日期选择',
    category: 'form-datetime',
    pc: 'a-date-picker',
    app: 'uni-datetime-picker',
    version: '1.0.0',
  },
  {
    type: 'JnpfTimePicker',
    name: '时间选择',
    category: 'form-datetime',
    pc: 'a-time-picker',
    app: 'uni-datetime-picker',
    version: '1.0.0',
  },

  // ============================================================
  // 开关/评分/滑块 (4)
  // ============================================================
  {
    type: 'JnpfSwitch',
    name: '开关',
    category: 'form-switch',
    pc: 'a-switch',
    app: 'switch',
    version: '1.0.0',
  },
  {
    type: 'JnpfRate',
    name: '评分',
    category: 'form-switch',
    pc: 'a-rate',
    app: 'uni-rate',
    version: '1.0.0',
  },
  {
    type: 'JnpfSlider',
    name: '滑块',
    category: 'form-switch',
    pc: 'a-slider',
    app: 'uni-slider',
    version: '1.0.0',
  },
  {
    type: 'JnpfColorPicker',
    name: '颜色选择',
    category: 'form-switch',
    pc: 'a-color-picker',
    app: 'view',
    version: '1.0.0',
  },

  // ============================================================
  // 上传类 (2)
  // ============================================================
  {
    type: 'JnpfUploadImg',
    name: '图片上传',
    category: 'form-upload',
    pc: 'a-upload',
    app: 'uni-file-picker',
    version: '1.0.0',
  },
  {
    type: 'JnpfUploadFile',
    name: '文件上传',
    category: 'form-upload',
    pc: 'a-upload',
    app: 'uni-file-picker',
    version: '1.0.0',
  },

  // ============================================================
  // 特殊输入 (3)
  // ============================================================
  {
    type: 'JnpfSign',
    name: '签名',
    category: 'form-special',
    pc: 'signature-pad',
    app: 'signature-pad',
    version: '1.0.0',
  },
  {
    type: 'JnpfSignature',
    name: '电子签章',
    category: 'form-special',
    pc: 'signature-pad',
    app: 'signature-pad',
    version: '1.0.0',
  },
  {
    type: 'JnpfEditor',
    name: '富文本编辑器',
    category: 'form-special',
    pc: 'rich-text-editor',
    app: 'rich-text-editor',
    version: '1.0.0',
  },

  // ============================================================
  // 布局类 (6)
  // ============================================================
  {
    type: 'JnpfRow',
    name: '行',
    category: 'layout',
    pc: 'a-row',
    app: 'view',
    version: '1.0.0',
  },
  {
    type: 'JnpfCol',
    name: '列',
    category: 'layout',
    pc: 'a-col',
    app: 'view',
    version: '1.0.0',
  },
  {
    type: 'JnpfDivider',
    name: '分割线',
    category: 'layout',
    pc: 'a-divider',
    app: 'uni-divider',
    version: '1.0.0',
  },
  {
    type: 'JnpfAlert',
    name: '提示',
    category: 'layout',
    pc: 'a-alert',
    app: 'uni-notice-bar',
    version: '1.0.0',
  },
  {
    type: 'JnpfTabs',
    name: '标签页',
    category: 'layout',
    pc: 'a-tabs',
    app: 'uni-tabs',
    version: '1.0.0',
  },
  {
    type: 'JnpfTabPane',
    name: '标签面板',
    category: 'layout',
    pc: 'a-tab-pane',
    app: 'uni-tab-item',
    version: '1.0.0',
  },

  // ============================================================
  // 数据展示 (4)
  // ============================================================
  {
    type: 'JnpfTable',
    name: '表格',
    category: 'data-display',
    pc: 'a-table',
    app: 'uni-table',
    version: '1.0.0',
  },
  {
    type: 'JnpfList',
    name: '列表',
    category: 'data-display',
    pc: 'a-list',
    app: 'uni-list',
    version: '1.0.0',
  },
  {
    type: 'JnpfCard',
    name: '卡片',
    category: 'data-display',
    pc: 'a-card',
    app: 'uni-card',
    version: '1.0.0',
  },
  {
    type: 'JnpfDescriptions',
    name: '描述列表',
    category: 'data-display',
    pc: 'a-descriptions',
    app: 'view',
    version: '1.0.0',
  },

  // ============================================================
  // 图表/大屏 (4)
  // ============================================================
  {
    type: 'ECharts:Bar',
    name: '柱状图',
    category: 'chart',
    pc: 'echarts-bar',
    app: 'echarts-bar',
    version: '1.0.0',
  },
  {
    type: 'ECharts:Line',
    name: '折线图',
    category: 'chart',
    pc: 'echarts-line',
    app: 'echarts-line',
    version: '1.0.0',
  },
  {
    type: 'ECharts:Pie',
    name: '饼图',
    category: 'chart',
    pc: 'echarts-pie',
    app: 'echarts-pie',
    version: '1.0.0',
  },
  {
    type: 'ECharts:Map',
    name: '地图',
    category: 'chart',
    pc: 'echarts-map',
    app: 'echarts-map',
    version: '1.0.0',
  },
];
