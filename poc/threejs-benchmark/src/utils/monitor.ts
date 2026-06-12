/**
 * PoC-B: 性能监控
 * FPS + 帧时间 + 内存 + WebGL 渲染信息
 */
import * as THREE from 'three';

export interface FrameMetric {
  timestamp: number;
  fps: number;
  frameTimeMs: number;
  drawCalls: number;
  triangles: number;
  /** JS heap used (MB), requires performance.memory (Chrome only) */
  jsHeapMB?: number;
}

export interface SessionMetrics {
  startTime: number;
  frames: FrameMetric[];
  minFps: number;
  maxFps: number;
  avgFps: number;
  fpsBelow30Count: number;
  totalFrames: number;
  durationMs: number;
}

/** 创建 FPS 监控器 */
export function createMonitor(
  renderer: THREE.WebGLRenderer,
  sampleInterval: number = 500, // ms between samples
) {
  // Use stats.js-style sampling
  const frameTimes: number[] = [];
  let lastTime = performance.now();
  let frameCount = 0;
  let currentFps = 0;

  const metrics: FrameMetric[] = [];
  let lastSampleTime = performance.now();
  const startTime = performance.now();

  function tick(): FrameMetric | null {
    const now = performance.now();
    const dt = now - lastTime;
    lastTime = now;
    frameCount++;

    // Track FPS via rolling 1-second window
    frameTimes.push(now);
    while (frameTimes.length > 0 && frameTimes[0] < now - 1000) {
      frameTimes.shift();
    }
    currentFps = frameTimes.length;

    // Sample at interval
    if (now - lastSampleTime < sampleInterval) return null;
    lastSampleTime = now;

    const info = renderer.info;
    const metric: FrameMetric = {
      timestamp: now - startTime,
      fps: currentFps,
      frameTimeMs: dt,
      drawCalls: info.render.calls,
      triangles: info.render.triangles,
    };

    // Chrome-only performance.memory
    const perf = performance as any;
    if (perf.memory) {
      metric.jsHeapMB = Math.round(perf.memory.usedJSHeapSize / (1024 * 1024));
    }

    metrics.push(metric);
    return metric;
  }

  function getSession(): SessionMetrics {
    const allFps = metrics.map(m => m.fps);
    const below30 = allFps.filter(f => f < 30).length;
    return {
      startTime,
      frames: metrics,
      minFps: allFps.length ? Math.min(...allFps) : 0,
      maxFps: allFps.length ? Math.max(...allFps) : 0,
      avgFps: allFps.length
        ? Math.round(allFps.reduce((a, b) => a + b, 0) / allFps.length)
        : 0,
      fpsBelow30Count: below30,
      totalFrames: frameCount,
      durationMs: performance.now() - startTime,
    };
  }

  function reset(): void {
    metrics.length = 0;
    frameTimes.length = 0;
    frameCount = 0;
    lastTime = performance.now();
    lastSampleTime = performance.now();
  }

  return { tick, getSession, reset, get currentFps() { return currentFps; } };
}

/** 创建屏幕 HUD 显示 */
export function createHud(): {
  element: HTMLDivElement;
  update: (fps: number, faces: number, drawCalls: number, memMB?: number, elapsed?: number) => void;
} {
  const el = document.createElement('div');
  el.style.cssText = `
    position: fixed; top: 10px; left: 10px;
    background: rgba(0,0,0,0.75); color: #0f0;
    padding: 12px 16px; border-radius: 8px;
    font-family: monospace; font-size: 14px; line-height: 1.6;
    pointer-events: none; z-index: 1000;
    min-width: 240px;
  `;
  document.body.appendChild(el);

  function update(fps: number, faces: number, drawCalls: number, memMB?: number, elapsed?: number) {
    const fpsColor = fps >= 30 ? '#0f0' : '#f00';
    const elapsedStr =
      elapsed !== undefined
        ? `${Math.floor(elapsed / 60000)}m ${Math.floor((elapsed % 60000) / 1000)}s`
        : '—';
    el.innerHTML = [
      `<span style="color:${fpsColor}; font-size:22px; font-weight:bold">${fps} FPS</span>`,
      `<span style="color:#aaa">Faces:</span> ${faces.toLocaleString()}`,
      `<span style="color:#aaa">DrawCalls:</span> ${drawCalls}`,
      `<span style="color:#aaa">Mem:</span> ${memMB !== undefined ? memMB + ' MB' : 'N/A'}`,
      `<span style="color:#aaa">Elapsed:</span> ${elapsedStr}`,
    ].join('<br>');
  }

  return { element: el, update };
}
