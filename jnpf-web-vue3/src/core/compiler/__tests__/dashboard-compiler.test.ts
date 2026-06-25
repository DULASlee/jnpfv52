import { describe, it, expect } from 'vitest';
import { DashboardCompiler } from '../dashboard/compiler';
import type { DashboardIR } from '../../ir/dashboard-types';

const mockDashboard: DashboardIR = {
  type: 'dashboard',
  id: 'factory',
  name: '工厂大屏',
  size: { width: 1920, height: 1080 },
  background: { type: 'color', value: '#0a0a2e' },
  theme: 'dark',
  widgets: [
    {
      id: 'bar1',
      type: 'ECharts:Bar',
      position: { x: 100, y: 200, w: 800, h: 400 },
      props: { title: '产量统计' },
      dataSourceId: 'api-yield',
      refreshInterval: 30000,
    },
    {
      id: 'pie1',
      type: 'ECharts:Pie',
      position: { x: 1000, y: 200, w: 600, h: 400 },
      props: { title: '设备占比' },
      dataSourceId: 'api-device',
      refreshInterval: 60000,
    },
    {
      id: 'border1',
      type: 'Border:Box1',
      position: { x: 80, y: 180, w: 840, h: 440, zIndex: 0 },
      props: {},
    },
  ],
  dataSources: [
    { id: 'api-yield', name: '产量数据', type: 'api', url: '/api/yield', method: 'GET', pollInterval: 30000 },
    { id: 'api-device', name: '设备数据', type: 'api', url: '/api/device', method: 'GET', pollInterval: 60000 },
  ],
};

describe('DashboardCompiler', () => {
  const compiler = new DashboardCompiler(mockDashboard);
  const result = compiler.compile();

  it('generates correct file count (10 base + 3 widget components)', () => {
    expect(result.project.size).toBe(13);
  });

  it('generates package.json with echarts + vue-echarts + @jiaminghi/data-view', () => {
    const pkg = result.project.get('package.json')!;
    expect(pkg).toContain('echarts');
    expect(pkg).toContain('vue-echarts');
    expect(pkg).toContain('@jiaminghi/data-view');
  });

  it('vite dev server port is 3200', () => {
    const viteCfg = result.project.get('vite.config.ts')!;
    expect(viteCfg).toContain('3200');
  });

  it('generates main page with position absolute layout', () => {
    const page = result.project.get('src/views/factory/index.vue')!;
    expect(page).toContain('widgetStyle');
    expect(page).toContain('dashboard-wrapper');
  });

  it('generates scale hook with window.innerWidth', () => {
    const scale = result.project.get('src/composables/useDashboardScale.ts')!;
    expect(scale).toContain('window.innerWidth');
    expect(scale).toContain('resize');
  });

  it('generates chart data hook with polling', () => {
    const hook = result.project.get('src/composables/useChartData.ts')!;
    expect(hook).toContain('axios');
    expect(hook).toContain('setInterval');
  });

  it('all files contain @jnpf-generated marker', () => {
    for (const [, content] of result.project) {
      expect(content).toContain('@jnpf-generated');
    }
  });

  it('zero eval or new Function in generated code', () => {
    for (const [, content] of result.project) {
      expect(content).not.toMatch(/\beval\b/);
      expect(content).not.toMatch(/new Function/);
    }
  });

  it('includes config backup file', () => {
    const cfg = result.project.get('src/config/factory.config.json')!;
    const parsed = JSON.parse(cfg.replace(/^\/\/.*\n/, ''));
    expect(parsed.type).toBe('dashboard');
    expect(parsed.widgets.length).toBe(3);
  });

  it('generates per-widget component for each unique widget type', () => {
    // 3 unique widget types: ECharts:Bar, ECharts:Pie, Border:Box1
    expect(result.project.has('src/components/ECharts/Bar.vue')).toBe(true);
    expect(result.project.has('src/components/ECharts/Pie.vue')).toBe(true);
    expect(result.project.has('src/components/Border/Box1.vue')).toBe(true);
  });

  it('3D widget component contains placeholder', () => {
    const dash3D: DashboardIR = {
      type: 'dashboard',
      id: 'test3d',
      name: '3D Test',
      size: { width: 1920, height: 1080 },
      background: { type: 'color', value: '#000' },
      theme: 'dark',
      widgets: [{ id: 's1', type: '3D:Scene', position: { x: 0, y: 0, w: 1920, h: 1080 }, props: {} }],
      dataSources: [],
    };
    const c = new DashboardCompiler(dash3D);
    const r = c.compile();
    const comp = r.project.get('src/components/3D/Scene.vue')!;
    expect(comp).toContain('3d-scene');
    expect(comp).toContain('@jnpf-generated');
  });
});
