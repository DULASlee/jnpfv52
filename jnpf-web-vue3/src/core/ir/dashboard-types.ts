/**
 * 数字大屏 IR 类型定义
 *
 * 与 FormPageIR 平级，都是 PageIR 的联合成员。
 * 22 种 widget 类型覆盖图表/装饰/数据/媒体/3D 五大类。
 *
 * 3D 组件标记 version: '2.0.0'，VIP 隔离。
 *
 * @version 1.0.0
 */

// ============================================================
// 页面级 IR
// ============================================================

export interface DashboardIR {
  type: 'dashboard';
  id: string;
  name: string;
  size: { width: number; height: number };
  background: { type: 'color' | 'image' | 'gradient'; value: string };
  theme: string;
  widgets: DashboardWidget[];
  dataSources: DashboardDataSource[];
  aiHints?: {
    domain?: string;
    scenario?: string;
    designRationale?: string;
  };
}

// ============================================================
// Widget 类型枚举 — 22 种
// ============================================================

/** 图表类 (7) */
export type ChartWidgetType = 'ECharts:Bar' | 'ECharts:Line' | 'ECharts:Pie' | 'ECharts:Gauge' | 'ECharts:Radar' | 'ECharts:Scatter' | 'ECharts:Map';

/** 边框装饰类 (2) */
export type BorderWidgetType = 'Border:Box1' | 'Border:Box2';

/** 通用装饰类 (1) */
export type DecorationWidgetType = 'Decoration:1';

/** 文本类 (2) */
export type TextWidgetType = 'Text:Title' | 'Text:Scroll';

/** 数据展示类 (2) */
export type DataWidgetType = 'Data:ScrollBoard' | 'Data:Number';

/** 媒体类 (3) */
export type MediaWidgetType = 'Media:Image' | 'Media:Video' | 'Media:Iframe';

/** 3D 类 (5) — version: '2.0.0'，VIP 隔离 */
export type ThreeDWidgetType = '3D:Scene' | '3D:POI' | '3D:Flyline' | '3D:Fence' | '3D:Heatmap';

/** 全量 Widget 类型联合 */
export type WidgetType =
  | ChartWidgetType
  | BorderWidgetType
  | DecorationWidgetType
  | TextWidgetType
  | DataWidgetType
  | MediaWidgetType
  | ThreeDWidgetType
  | (string & {}); // 保留扩展位

/** 3D widget 类型列表（用于 VIP 隔离判断） */
export const THREE_D_WIDGETS: readonly string[] = ['3D:Scene', '3D:POI', '3D:Flyline', '3D:Fence', '3D:Heatmap'] as const;

export function is3DWidget(type: string): boolean {
  return THREE_D_WIDGETS.includes(type);
}

/** Widget 分类 */
export type WidgetCategory = 'chart' | 'border' | 'decoration' | 'text' | 'data' | 'media' | '3d';

export function getWidgetCategory(type: WidgetType): WidgetCategory {
  if (type.startsWith('ECharts:')) return 'chart';
  if (type.startsWith('Border:')) return 'border';
  if (type.startsWith('Decoration:')) return 'decoration';
  if (type.startsWith('Text:')) return 'text';
  if (type.startsWith('Data:')) return 'data';
  if (type.startsWith('Media:')) return 'media';
  if (type.startsWith('3D:')) return '3d';
  return 'chart'; // default fallback
}

// ============================================================
// Widget
// ============================================================

export interface DashboardWidget {
  id: string;
  type: WidgetType;
  /** 绝对定位 */
  position: WidgetPosition;
  /** 组件 props */
  props: Record<string, unknown>;
  /** 数据源引用 */
  dataSourceId?: string;
  /** 刷新间隔 (ms)，0 = 不刷新 */
  refreshInterval?: number;
  /** 可见性表达式 */
  visible?: string;
  /**
   * 组件版本
   * - undefined / '1.0.0': 标准组件
   * - '2.0.0': 3D/VIP 组件
   */
  version?: string;
  /** AI 探针 */
  aiHints?: {
    purpose?: string;
    dataExpectation?: string;
    suggested3DTech?: 'three' | 'cesium' | 'babylon';
    lodHint?: 'high' | 'medium' | 'low' | 'auto';
  };
}

export interface WidgetPosition {
  x: number;
  y: number;
  w: number;
  h: number;
  zIndex?: number;
}

// ============================================================
// 数据源
// ============================================================

export interface DashboardDataSource {
  id: string;
  name: string;
  type: 'api' | 'websocket' | 'static' | 'mock';
  url?: string;
  method?: 'GET' | 'POST';
  params?: Record<string, unknown>;
  /** 轮询间隔 (ms)，0 = 不轮询 */
  pollInterval?: number;
  /** 数据转换表达式 (JS 箭头函数字符串) */
  transform?: string;
  /** 静态数据 (type=static 时使用) */
  staticData?: unknown;
  /** WebSocket 事件名 (type=websocket 时使用) */
  event?: string;
}
