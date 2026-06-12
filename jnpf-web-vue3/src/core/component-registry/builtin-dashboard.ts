/**
 * 大屏组件注册
 * 全部注册到 ComponentRegistry，category='chart'/'layout'/'data-display'
 */

import type { ComponentEntry } from './types';

export const DASHBOARD_COMPONENTS: ComponentEntry[] = [
  // ============================================================
  // 图表 (7)
  // ============================================================
  { type: 'ECharts:Bar', name: '柱状图', category: 'chart', pc: 'echarts-bar', app: 'echarts-bar', version: '1.0.0' },
  { type: 'ECharts:Line', name: '折线图', category: 'chart', pc: 'echarts-line', app: 'echarts-line', version: '1.0.0' },
  { type: 'ECharts:Pie', name: '饼图', category: 'chart', pc: 'echarts-pie', app: 'echarts-pie', version: '1.0.0' },
  { type: 'ECharts:Gauge', name: '仪表盘', category: 'chart', pc: 'echarts-gauge', app: 'echarts-gauge', version: '1.0.0' },
  { type: 'ECharts:Radar', name: '雷达图', category: 'chart', pc: 'echarts-radar', app: 'echarts-radar', version: '1.0.0' },
  { type: 'ECharts:Scatter', name: '散点图', category: 'chart', pc: 'echarts-scatter', app: 'echarts-scatter', version: '1.0.0' },
  { type: 'ECharts:Map', name: '地图', category: 'chart', pc: 'echarts-map', app: 'echarts-map', version: '1.0.0' },

  // ============================================================
  // 装饰 (3)
  // ============================================================
  { type: 'Border:Box1', name: '边框-方形', category: 'layout', pc: 'dv-border-box-1', app: 'dv-border-box-1', version: '1.0.0' },
  { type: 'Border:Box2', name: '边框-圆角', category: 'layout', pc: 'dv-border-box-2', app: 'dv-border-box-2', version: '1.0.0' },
  { type: 'Decoration:1', name: '装饰-01', category: 'layout', pc: 'dv-decoration-1', app: 'dv-decoration-1', version: '1.0.0' },

  // ============================================================
  // 数据展示 (4)
  // ============================================================
  { type: 'Text:Title', name: '标题文字', category: 'data-display', pc: 'dv-scroll-board', app: 'dv-scroll-board', version: '1.0.0' },
  { type: 'Text:Scroll', name: '滚动文字', category: 'data-display', pc: 'dv-scroll-board', app: 'dv-scroll-board', version: '1.0.0' },
  { type: 'Data:ScrollBoard', name: '滚动列表', category: 'data-display', pc: 'dv-scroll-board', app: 'dv-scroll-board', version: '1.0.0' },
  { type: 'Data:Number', name: '数字翻牌器', category: 'data-display', pc: 'dv-digital-flop', app: 'dv-digital-flop', version: '1.0.0' },

  // ============================================================
  // 媒体 (3)
  // ============================================================
  { type: 'Media:Image', name: '图片', category: 'data-display', pc: 'img', app: 'img', version: '1.0.0' },
  { type: 'Media:Video', name: '视频', category: 'data-display', pc: 'video', app: 'video', version: '1.0.0' },
  { type: 'Media:Iframe', name: '内嵌页面', category: 'data-display', pc: 'iframe', app: 'iframe', version: '1.0.0' },

  // ============================================================
  // 3D 预留 (5, VIP)
  // ============================================================
  { type: '3D:Scene', name: '3D 场景', category: 'chart', pc: '3d-scene', app: '3d-scene', version: '2.0.0' },
  { type: '3D:POI', name: '3D 点位', category: 'chart', pc: '3d-poi', app: '3d-poi', version: '2.0.0' },
  { type: '3D:FlyLine', name: '3D 飞线', category: 'chart', pc: '3d-flyline', app: '3d-flyline', version: '2.0.0' },
  { type: '3D:Fence', name: '3D 围栏', category: 'chart', pc: '3d-fence', app: '3d-fence', version: '2.0.0' },
  { type: '3D:Heatmap', name: '3D 热力', category: 'chart', pc: '3d-heatmap', app: '3d-heatmap', version: '2.0.0' },
];
