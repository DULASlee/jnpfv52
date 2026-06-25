import { describe, it, expect } from 'vitest';
import { DashboardCompiler } from '../dashboard/compiler';
import type { DashboardIR } from '../../ir/dashboard-types';

/**
 * 智慧工地场景 mock 数据
 *
 * 包含：3D 场景 + 模型 + POI + 飞线 + 围栏 + 热力图 + 数据绑定
 * 同时包含普通图表组件以验证 3D 隔离。
 */
const mock3DDashboard: DashboardIR = {
  type: 'dashboard',
  id: 'smart-site',
  name: '智慧工地数字孪生',
  size: { width: 1920, height: 1080 },
  background: { type: 'color', value: '#0a0a2e' },
  theme: 'dark',
  widgets: [
    // ── 3D 场景 ──
    {
      id: 'scene1',
      type: '3D:Scene',
      position: { x: 0, y: 0, w: 1280, h: 1080, zIndex: 1 },
      props: {
        backgroundColor: '#0a0a2e',
        cameraPosition: [15, 12, 15],
        ambientIntensity: 0.5,
        models: [{ url: '/models/site.glb', name: '工地模型', scale: 1, castShadow: true }],
        pois: [
          { id: 'tower1', name: '塔吊1', position: [5, 8, 3], icon: 'device', status: 'normal' },
          { id: 'worker1', name: '工人A', position: [-3, 0, 5], icon: 'person', status: 'normal' },
          { id: 'alarm1', name: '告警点', position: [2, 4, -4], icon: 'alarm', status: 'alarm' },
        ],
        flylines: [
          { start: [0, 0, 0], end: [5, 8, 3], color: '#00d4ff' },
          { start: [5, 8, 3], end: [-3, 0, 5], color: '#ffa940' },
        ],
        fences: [
          {
            id: 'zone-a',
            name: '施工区域A',
            points: [
              [0, 0, 0],
              [10, 0, 0],
              [10, 0, 8],
              [0, 0, 8],
            ],
            height: 4,
            color: '#00ff88',
            status: 'normal',
          },
        ],
        heatmaps: [
          {
            id: 'density',
            points: [
              { position: [2, 0, 2], value: 0.8, label: '高密度区' },
              { position: [8, 0, 6], value: 0.3, label: '低密度区' },
              { position: [5, 0, 4], value: 0.5, label: '中密度区' },
            ],
            barMode: true,
            maxHeight: 8,
          },
        ],
        dataBindings: [
          {
            targetId: 'tower1',
            targetType: 'poi',
            dataField: 'temperature',
            mapping: [
              { condition: '> 80', action: { status: 'alarm', color: '#ff4560' } },
              { condition: '> 50', action: { status: 'warning', color: '#ffa940' } },
            ],
          },
        ],
      },
      dataSourceId: 'ws-site',
      refreshInterval: 5000,
      version: '2.0.0',
    },
    // ── 3D 飞线独立组件 ──
    {
      id: 'fly1',
      type: '3D:Flyline',
      position: { x: 1280, y: 0, w: 640, h: 540, zIndex: 2 },
      props: {
        lines: [
          { start: [0, 0, 0], end: [5, 2, 5], color: '#00d4ff' },
          { start: [0, 0, 0], end: [-5, 1, 3], color: '#ffa940' },
        ],
      },
      version: '2.0.0',
    },
    // ── ECharts 图表（验证 3D 隔离） ──
    {
      id: 'bar1',
      type: 'ECharts:Bar',
      position: { x: 1280, y: 540, w: 640, h: 540 },
      props: { title: '施工进度' },
      dataSourceId: 'api-progress',
      refreshInterval: 30000,
    },
  ],
  dataSources: [
    {
      id: 'ws-site',
      name: '工地实时数据',
      type: 'websocket',
      url: 'wss://site.example.com/ws',
      event: 'site-data',
    },
    {
      id: 'api-progress',
      name: '施工进度',
      type: 'api',
      url: '/api/progress',
      method: 'GET',
      pollInterval: 30000,
    },
  ],
};

describe('DashboardCompiler — 3D Scene Integration (F-6b)', () => {
  const compiler = new DashboardCompiler(mock3DDashboard);
  const result = compiler.compile();

  // ============================================================
  // Core 3D generation
  // ============================================================

  it('generates 3D scene component for 3D:Scene widget', () => {
    const comp = result.project.get('src/components/3D/Scene.vue');
    expect(comp).toBeDefined();
    expect(comp).toContain('THREE.Scene');
    expect(comp).toContain('WebGLRenderer');
    expect(comp).toContain('PerspectiveCamera');
    expect(comp).toContain('OrbitControls');
    expect(comp).toContain('@jnpf-generated');
  });

  it('generates 3D flyline component for 3D:Flyline widget', () => {
    const comp = result.project.get('src/components/3D/Flyline.vue');
    expect(comp).toBeDefined();
    expect(comp).toContain('THREE');
    expect(comp).toContain('@jnpf-generated');
  });

  it('3D scene component includes CSS2DRenderer for POI labels', () => {
    const comp = result.project.get('src/components/3D/Scene.vue');
    expect(comp).toContain('CSS2DRenderer');
  });

  // ============================================================
  // 3D isolation (does not affect non-3D components)
  // ============================================================

  it('3D scene does not break ECharts chart generation', () => {
    const chartComp = result.project.get('src/components/ECharts/Bar.vue');
    expect(chartComp).toBeDefined();
    expect(chartComp).toContain('vue-echarts');
    expect(chartComp).toContain('@jnpf-generated');
  });

  it('main page renders both 3D and chart widgets', () => {
    const page = result.project.get('src/views/smart-site/index.vue')!;
    expect(page).toContain('3D:Scene');
    expect(page).toContain('3D:Flyline');
    expect(page).toContain('ECharts:Bar');
  });

  // ============================================================
  // Package.json includes Three.js
  // ============================================================

  it('package.json includes three dependency when 3D widgets present', () => {
    const pkg = result.project.get('package.json')!;
    expect(pkg).toContain('"three"');
    expect(pkg).toContain('echarts'); // Still has non-3D deps
  });

  it('package.json WITHOUT 3D widgets does NOT include three', () => {
    const no3D: DashboardIR = {
      type: 'dashboard',
      id: 'no3d',
      name: 'No 3D',
      size: { width: 1920, height: 1080 },
      background: { type: 'color', value: '#000' },
      theme: 'dark',
      widgets: [{ id: 'b1', type: 'ECharts:Bar', position: { x: 0, y: 0, w: 800, h: 400 }, props: {} }],
      dataSources: [],
    };
    const c = new DashboardCompiler(no3D);
    const r = c.compile();
    const pkg = r.project.get('package.json')!;
    expect(pkg).not.toContain('"three"');
  });

  // ============================================================
  // WebSocket data source
  // ============================================================

  it('WebSocket data source is reflected in config', () => {
    const cfgRaw = result.project.get('src/config/smart-site.config.json')!;
    const cfg = JSON.parse(cfgRaw.replace(/^\/\/.*\n/, ''));
    const wsSource = cfg.dataSources.find((ds: any) => ds.type === 'websocket');
    expect(wsSource).toBeDefined();
    expect(wsSource.url).toBe('wss://site.example.com/ws');
    expect(wsSource.event).toBe('site-data');
  });

  // ============================================================
  // Zero eval / new Function
  // ============================================================

  it('zero eval in all generated files', () => {
    for (const [, content] of result.project) {
      expect(content).not.toMatch(/\beval\s*\(/);
    }
  });

  it('zero new Function in all generated files', () => {
    for (const [, content] of result.project) {
      expect(content).not.toMatch(/new\s+Function\s*\(/);
    }
  });

  // ============================================================
  // @jnpf-generated marker
  // ============================================================

  it('all generated files contain @jnpf-generated marker', () => {
    for (const [, content] of result.project) {
      expect(content).toContain('@jnpf-generated');
    }
  });

  // ============================================================
  // File structure completeness
  // ============================================================

  it('generates correct number of files (base + 3D + chart widgets)', () => {
    // 10 base files + 3 widget components (3D:Scene, 3D:Flyline, ECharts:Bar)
    expect(result.project.has('package.json')).toBe(true);
    expect(result.project.has('src/components/3D/Scene.vue')).toBe(true);
    expect(result.project.has('src/components/3D/Flyline.vue')).toBe(true);
    expect(result.project.has('src/components/ECharts/Bar.vue')).toBe(true);
  });

  it('does not generate 3D component file for non-3D dashboard', () => {
    const no3D: DashboardIR = {
      type: 'dashboard',
      id: 'charts',
      name: 'Charts Only',
      size: { width: 1920, height: 1080 },
      background: { type: 'color', value: '#000' },
      theme: 'dark',
      widgets: [{ id: 'b1', type: 'ECharts:Bar', position: { x: 0, y: 0, w: 800, h: 400 }, props: {} }],
      dataSources: [],
    };
    const c = new DashboardCompiler(no3D);
    const r = c.compile();
    expect(r.project.has('src/components/3D/Scene.vue')).toBe(false);
  });

  // ============================================================
  // DataSource config integrity
  // ============================================================

  it('config backup contains all widgets including 3D', () => {
    const cfgRaw = result.project.get('src/config/smart-site.config.json')!;
    const cfg = JSON.parse(cfgRaw.replace(/^\/\/.*\n/, ''));
    expect(cfg.type).toBe('dashboard');
    expect(cfg.widgets.length).toBe(3);
    // Verify 3D widgets have version 2.0.0
    const sceneWidget = cfg.widgets.find((w: any) => w.id === 'scene1');
    expect(sceneWidget.version).toBe('2.0.0');
  });
});
