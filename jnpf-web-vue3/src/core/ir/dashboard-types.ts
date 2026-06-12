/**
 * 数字大屏 IR 类型定义
 * 与 FormPageIR 平级，都是 PageIR 的联合成员
 */

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

export interface DashboardWidget {
  id: string;
  type: string;
  position: { x: number; y: number; w: number; h: number; zIndex?: number };
  props: Record<string, unknown>;
  dataSourceId?: string;
  refreshInterval?: number;
  visible?: string;
  aiHints?: { purpose?: string; dataExpectation?: string };
}

export interface DashboardDataSource {
  id: string;
  name: string;
  type: 'api' | 'websocket' | 'static' | 'mock';
  url?: string;
  method?: 'GET' | 'POST';
  params?: Record<string, unknown>;
  pollInterval?: number;
  transform?: string;
  staticData?: unknown;
}
