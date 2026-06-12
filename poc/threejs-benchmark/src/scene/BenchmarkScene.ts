/**
 * PoC-B: 基准测试场景
 * 10 万面混合几何体 + 20 CSS2D POI + 5 飞线
 */
import * as THREE from 'three';
import { CSS2DRenderer, CSS2DObject } from 'three/examples/jsm/renderers/CSS2DRenderer.js';
import { generateMixedGeometry, type GeoStats } from '../utils/geometry-generator';
import { createFlyLines, updateFlyLines, type FlyLine } from '../utils/flyline';
import { createMonitor, createHud, type SessionMetrics } from '../utils/monitor';
import { OrbitControls } from 'three/examples/jsm/controls/OrbitControls.js';

export interface BenchmarkResult {
  geoStats: GeoStats;
  session: SessionMetrics;
  passed: boolean;
  failureReason?: string;
}

export class BenchmarkScene {
  private renderer!: THREE.WebGLRenderer;
  private labelRenderer!: CSS2DRenderer;
  private scene!: THREE.Scene;
  private camera!: THREE.PerspectiveCamera;
  private controls!: OrbitControls;
  private monitor!: ReturnType<typeof createMonitor>;
  private hud!: ReturnType<typeof createHud>;
  private flyLines: FlyLine[] = [];
  private animFrameId: number = 0;
  private isRunning: boolean = false;
  private geoStats!: GeoStats;
  private poiObjects: CSS2DObject[] = [];

  private readonly TARGET_FACES = 100_000;
  private readonly POI_COUNT = 20;
  private readonly FLYLINE_COUNT = 5;
  /** 总测试时长 (ms) */
  private readonly DURATION_MS = 10 * 60 * 1000; // 10 min

  constructor(private container: HTMLElement) {}

  /** 初始化场景 */
  init(): GeoStats {
    // ── Renderer ──
    this.renderer = new THREE.WebGLRenderer({ antialias: true, powerPreference: 'high-performance' });
    this.renderer.setPixelRatio(Math.min(window.devicePixelRatio, 2)); // cap for perf
    this.renderer.setSize(window.innerWidth, window.innerHeight);
    this.renderer.shadowMap.enabled = true;
    this.renderer.shadowMap.type = THREE.PCFSoftShadowMap;
    this.container.appendChild(this.renderer.domElement);

    // CSS2D Renderer
    this.labelRenderer = new CSS2DRenderer();
    this.labelRenderer.setSize(window.innerWidth, window.innerHeight);
    this.labelRenderer.domElement.style.position = 'absolute';
    this.labelRenderer.domElement.style.top = '0';
    this.labelRenderer.domElement.style.pointerEvents = 'none';
    this.container.appendChild(this.labelRenderer.domElement);

    // ── Scene ──
    this.scene = new THREE.Scene();
    this.scene.background = new THREE.Color(0x1a1a2e);
    this.scene.fog = new THREE.Fog(0x1a1a2e, 100, 300);

    // ── Lights ──
    const ambient = new THREE.AmbientLight(0x404060, 1.5);
    this.scene.add(ambient);

    const sun = new THREE.DirectionalLight(0xffffff, 2);
    sun.position.set(50, 80, 40);
    sun.castShadow = true;
    sun.shadow.mapSize.width = 2048;
    sun.shadow.mapSize.height = 2048;
    sun.shadow.camera.near = 0.5;
    sun.shadow.camera.far = 400;
    sun.shadow.camera.left = -100;
    sun.shadow.camera.right = 100;
    sun.shadow.camera.top = 100;
    sun.shadow.camera.bottom = -100;
    this.scene.add(sun);

    const hemisphere = new THREE.HemisphereLight(0x606080, 0x202030, 0.8);
    this.scene.add(hemisphere);

    // ── Camera ──
    this.camera = new THREE.PerspectiveCamera(60, window.innerWidth / window.innerHeight, 0.5, 500);
    this.camera.position.set(80, 50, 100);
    this.camera.lookAt(0, 10, 0);

    // ── Controls ──
    this.controls = new OrbitControls(this.camera, this.renderer.domElement);
    this.controls.target.set(0, 10, 0);
    this.controls.enableDamping = true;
    this.controls.dampingFactor = 0.08;
    this.controls.maxDistance = 200;
    this.controls.update();

    // ── Geometry ──
    const { group, stats } = generateMixedGeometry(this.TARGET_FACES);
    this.scene.add(group);
    this.geoStats = stats;

    // ── POI Labels ──
    this.createPOIs();

    // ── FlyLines ──
    this.flyLines = createFlyLines(this.FLYLINE_COUNT);
    for (const line of this.flyLines) {
      this.scene.add(line.particles);
    }

    // ── Grid ──
    const grid = new THREE.GridHelper(200, 40, 0x333355, 0x222244);
    grid.position.y = -0.5;
    this.scene.add(grid);

    // ── Monitor ──
    this.monitor = createMonitor(this.renderer, 1000);
    this.hud = createHud();
    this.container.appendChild(this.hud.element);

    // ── Resize ──
    window.addEventListener('resize', this.onResize);

    return stats;
  }

  /** 创建 20 个 CSS2D POI 标签 */
  private createPOIs(): void {
    const labels = [
      '总控中心', '1号厂房', '2号厂房', '办公楼', '仓库A',
      '仓库B', '配电房', '水泵站', '消防站', '停车场',
      '质检中心', '研发楼', '食堂', '宿舍A', '宿舍B',
      '门卫1', '门卫2', '污水处理', '变电所', '物流中心',
    ];

    for (let i = 0; i < this.POI_COUNT; i++) {
      const div = document.createElement('div');
      div.textContent = labels[i % labels.length];
      div.style.cssText = `
        color: #fff; background: rgba(0,150,255,0.85);
        padding: 4px 10px; border-radius: 4px;
        font-size: 12px; font-family: sans-serif;
        white-space: nowrap; pointer-events: auto;
        border: 1px solid rgba(0,200,255,0.5);
      `;
      const label = new CSS2DObject(div);
      label.position.set(
        (Math.random() - 0.5) * 140,
        3 + Math.random() * 35,
        (Math.random() - 0.5) * 140,
      );
      label.userData = { name: labels[i % labels.length] };
      this.scene.add(label);
      this.poiObjects.push(label);
    }
  }

  /** 启动基准测试循环 */
  async start(): Promise<BenchmarkResult> {
    this.isRunning = true;
    this.monitor.reset();

    return new Promise(resolve => {
      const startTime = performance.now();

      const animate = (): void => {
        if (!this.isRunning) {
          cancelAnimationFrame(this.animFrameId);
          const session = this.monitor.getSession();
          resolve({
            geoStats: this.geoStats,
            session,
            passed: false,
            failureReason: 'stopped early',
          });
          return;
        }

        const now = performance.now();
        const elapsed = now - startTime;

        // 时间到 → 结束
        if (elapsed >= this.DURATION_MS) {
          cancelAnimationFrame(this.animFrameId);
          this.isRunning = false;
          const session = this.monitor.getSession();
          const passed = session.avgFps >= 30 && session.fpsBelow30Count < session.frames.length * 0.05;
          resolve({
            geoStats: this.geoStats,
            session,
            passed,
            failureReason: passed ? undefined : `avg FPS ${session.avgFps} < 30 or too many drops`,
          });
          return;
        }

        const dt = Math.min(now - (this._lastFrame ?? now), 100);
        this._lastFrame = now;

        this.controls.update();

        // 更新飞线
        updateFlyLines(this.flyLines, dt);

        // 渲染
        this.renderer.render(this.scene, this.camera);
        this.labelRenderer.render(this.scene, this.camera);

        // 监控采样
        const metric = this.monitor.tick();
        if (metric) {
          const mem = metric.jsHeapMB;
          this.hud.update(
            metric.fps,
            this.geoStats.totalFaces,
            metric.drawCalls,
            mem,
            elapsed,
          );

          // 阶段性日志 (每分钟)
          if (Math.floor(elapsed / 60000) !== Math.floor((elapsed - 1000) / 60000)) {
            console.log(
              `[PoC-B] ${Math.floor(elapsed / 60000)}m: ` +
                `${metric.fps} FPS | ${mem ?? '?'} MB | ${metric.drawCalls} draws | ${metric.triangles} tris`,
            );
          }
        }

        this.animFrameId = requestAnimationFrame(animate);
      };

      this.animFrameId = requestAnimationFrame(animate);
    });
  }

  stop(): void {
    this.isRunning = false;
    cancelAnimationFrame(this.animFrameId);
  }

  dispose(): void {
    this.stop();
    window.removeEventListener('resize', this.onResize);
    this.renderer.dispose();
    this.container.innerHTML = '';
  }

  private _lastFrame: number = 0;

  private onResize = (): void => {
    this.camera.aspect = window.innerWidth / window.innerHeight;
    this.camera.updateProjectionMatrix();
    this.renderer.setSize(window.innerWidth, window.innerHeight);
    this.labelRenderer.setSize(window.innerWidth, window.innerHeight);
  };
}
