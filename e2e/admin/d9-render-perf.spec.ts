/**
 * D9 渲染性能采集 — Chrome Performance + 长任务统计
 *
 * 对每个关键路由采集: LCP / 长任务数 / 长任务总时长 / JS堆内存
 *
 * 运行前置: dev server :3100 已启动
 * 运行: cd e2e && npx playwright test admin/d9-render-perf.spec.ts
 * 证据: .claude/evidence/frontend-ct/d9-perf-results.json
 */
import { test, expect } from '@playwright/test';
import { writeFileSync, mkdirSync } from 'fs';
import { join } from 'path';
import { loginAsAdmin } from '../helpers/login';

const OUT_DIR = join(__dirname, '..', '..', '.claude', 'evidence', 'frontend-ct');
mkdirSync(OUT_DIR, { recursive: true });

const ROUTES = [
  { name: 'home', path: '/home' },
  { name: 'workStation', path: '/workStation' },
  { name: 'onlineDev', path: '/onlineDev' },
  { name: 'studio-submit', path: '/studio/ai/submit-requirement' },
];

test.describe('D9 渲染性能', () => {
  test('关键路由性能采集', async ({ page }) => {
    // 注入 PerformanceObserver 收集长任务 + LCP
    const metrics: any[] = [];

    await loginAsAdmin(page);

    for (const r of ROUTES) {
      // 每个 route 前重置采集
      await page.addInitScript(() => {
        (window as any).__perf = { longTasks: [], lcp: 0, cls: 0 };
        const obs1 = new PerformanceObserver((list) => {
          for (const e of list.getEntries()) {
            (window as any).__perf.longTasks.push({ duration: e.duration, startTime: e.startTime });
          }
        });
        try { obs1.observe({ type: 'longtask', buffered: true }); } catch {}
        const obs2 = new PerformanceObserver((list) => {
          const entries = list.getEntries();
          (window as any).__perf.lcp = entries[entries.length - 1]?.startTime || 0;
        });
        try { obs2.observe({ type: 'largest-contentful-paint', buffered: true }); } catch {}
      });

      await page.goto(r.path, { waitUntil: 'domcontentloaded', timeout: 60_000 });
      // 等待渲染稳定 (首屏组件挂载)
      await page.waitForTimeout(5000);

      const perf = await page.evaluate(() => (window as any).__perf);
      const navTiming = await page.evaluate(() => {
        const n = performance.getEntriesByType('navigation')[0] as PerformanceNavigationTiming;
        return n ? {
          domContentLoaded: n.domContentLoadedEventEnd,
          load: n.loadEventEnd,
          ttfb: n.responseStart,
          domInteractive: n.domInteractive,
        } : null;
      });
      const heap = await page.evaluate(() => (performance as any).memory?.usedJSHeapSize || 0);

      const longTasks = perf?.longTasks || [];
      const longTaskTotal = longTasks.reduce((s: number, t: any) => s + t.duration, 0);
      const longTaskOver100 = longTasks.filter((t: any) => t.duration > 100).length;

      metrics.push({
        route: r.name,
        path: r.path,
        lcp: Math.round(perf?.lcp || 0),
        ttfb: Math.round(navTiming?.ttfb || 0),
        domContentLoaded: Math.round(navTiming?.domContentLoaded || 0),
        domInteractive: Math.round(navTiming?.domInteractive || 0),
        loadEvent: Math.round(navTiming?.load || 0),
        longTaskCount: longTasks.length,
        longTaskOver100ms: longTaskOver100,
        longTaskTotalMs: Math.round(longTaskTotal),
        heapUsedMB: Math.round(heap / 1024 / 1024),
      });
    }

    const report = {
      timestamp: new Date().toISOString(),
      url: process.env.JNPF_WEB_URL || 'http://localhost:3100',
      summary: {
        routes: metrics.length,
        routesWithLongTasks: metrics.filter(m => m.longTaskCount > 0).length,
        avgLCP: Math.round(metrics.reduce((s, m) => s + m.lcp, 0) / metrics.length),
        maxLCP: Math.max(...metrics.map(m => m.lcp)),
        avgHeapMB: Math.round(metrics.reduce((s, m) => s + m.heapUsedMB, 0) / metrics.length),
      },
      details: metrics,
      legend: {
        lcp: 'Largest Contentful Paint (ms), 好<2500 慢>4000',
        longTaskOver100ms: '超 100ms 的长任务数 (阻塞主线程, 影响 INP)',
        heapUsedMB: 'JS 堆内存使用',
      },
    };
    writeFileSync(join(OUT_DIR, 'd9-perf-results.json'), JSON.stringify(report, null, 2));
    console.log('[D9] perf report written');
    // 断言: LCP 不应超过 8s (粗阈值, 仅作冒烟)
    expect(report.summary.maxLCP).toBeLessThan(8000);
  });
});
