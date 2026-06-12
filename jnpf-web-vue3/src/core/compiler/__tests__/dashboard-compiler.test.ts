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

  it('generates correct file count', () => {
    expect(result.project.size).toBe(10);
  });

  it('generates package.json with echarts + vue-echarts', () => {
    const pkg = result.project.get('package.json')!;
    expect(pkg).toContain('echarts');
    expect(pkg).toContain('vue-echarts');
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
});
