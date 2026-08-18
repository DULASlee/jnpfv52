/**
 * D9 渲染性能采集 v2 — LCP / 长任务 / JS堆 / TTFB
 *
 * 改进: 登录页也采集; 登录超时则只报登录页性能并标记 SKIPPED
 */
import { test } from '@playwright/test';
import { writeFileSync, mkdirSync } from 'fs';
import { join } from 'path';

const OUT_DIR = join(__dirname, '..', '..', '.claude', 'evidence', 'frontend-ct');
mkdirSync(OUT_DIR, { recursive: true });

const AUTH_ROUTES = [
  { name: 'home', path: '/home' },
  { name: 'workStation', path: '/workStation' },
];

async function collectPerf(page, routeName: string, path: string) {
  // 用 addInitScript 在导航前注入采集器 (解决异步回调时机问题)
  await page.addInitScript(() => {
    (window as any).__perf = { longTasks: [], lcp: 0, lcpReady: false };
    try {
      new PerformanceObserver((list) => {
        for (const e of list.getEntries()) (window as any).__perf.longTasks.push(e.duration);
      }).observe({ type: 'longtask', buffered: true });
    } catch {}
    try {
      new PerformanceObserver((list) => {
        const e = list.getEntries();
        (window as any).__perf.lcp = e[e.length - 1]?.startTime || 0;
      }).observe({ type: 'largest-contentful-paint', buffered: true });
    } catch {}
  });

  await page.goto(path, { waitUntil: 'domcontentloaded', timeout: 60_000 });
  await page.waitForTimeout(6000); // 等渲染稳定 + 让 PerformanceObserver 回调跑完

  const data = await page.evaluate(() => {
    const perf = (window as any).__perf || { longTasks: [], lcp: 0 };
    const n = performance.getEntriesByType('navigation')[0] as any;
    const heapRaw = (performance as any).memory?.usedJSHeapSize || 0;
    return {
      lcp: Math.round(perf.lcp || 0),
      ttfb: Math.round(n?.responseStart || 0),
      domContentLoaded: Math.round(n?.domContentLoadedEventEnd || 0),
      domInteractive: Math.round(n?.domInteractive || 0),
      longTasks: perf.longTasks.slice(),
      heapMB: Math.round(heapRaw / 1024 / 1024), // 修正: 括号包住整个表达式
    };
  });

  return {
    route: routeName, path,
    lcp: data.lcp, ttfb: data.ttfb,
    domContentLoaded: data.domContentLoaded,
    domInteractive: data.domInteractive,
    longTaskCount: data.longTasks.length,
    longTaskOver100ms: data.longTasks.filter((d) => d > 100).length,
    longTaskTotalMs: Math.round(data.longTasks.reduce((s, d) => s + d, 0)),
    heapUsedMB: data.heapMB,
  };
}

test('D9 渲染性能采集', async ({ page }) => {
  const metrics: any[] = [];

  // ① 登录页性能 (无需登录)
  metrics.push(await collectPerf(page, 'login', '/login'));

  // ② 尝试 UI 登录
  let authed = false;
  try {
    await page.goto('/login', { waitUntil: 'domcontentloaded', timeout: 60_000 });
    await page.waitForTimeout(2000);
    await page.locator('input[placeholder*="账号"], input[placeholder*="账户"]').first().fill('admin');
    await page.locator('input[type="password"]').first().fill('123456');
    await page.getByRole('button', { name: /登\s*录|Login/i }).click();
    await page.waitForURL(/\/(home|workStation|dashboard|studio)/, { timeout: 60_000 });
    authed = true;
  } catch {
    console.log('[D9] UI 登录超时, 仅报登录页性能');
  }

  // ③ 登录后路由性能
  if (authed) {
    for (const r of AUTH_ROUTES) {
      try { metrics.push(await collectPerf(page, r.name, r.path)); }
      catch (e) { console.log(`[D9] ${r.name} 采集失败: ${(e as Error).message.slice(0, 80)}`); }
    }
  }

  const report = {
    timestamp: new Date().toISOString(),
    authed,
    summary: {
      routes: metrics.length,
      avgLCP: metrics.length ? Math.round(metrics.reduce((s, m) => s + m.lcp, 0) / metrics.length) : 0,
      maxLCP: metrics.length ? Math.max(...metrics.map((m) => m.lcp)) : 0,
      totalLongTasks: metrics.reduce((s, m) => s + m.longTaskCount, 0),
      maxHeapMB: Math.max(...metrics.map((m) => m.heapUsedMB)),
    },
    details: metrics,
    legend: {
      lcp: 'Largest Contentful Paint (ms): 好<2500 需改进2500-4000 慢>4000',
      longTaskOver100ms: '超100ms长任务(阻塞主线程,影响交互响应INP)',
      heapUsedMB: 'JS堆内存(反映组件/状态规模)',
    },
  };
  writeFileSync(join(OUT_DIR, 'd9-perf-results.json'), JSON.stringify(report, null, 2));
  console.log('[D9] 完成: ' + metrics.length + ' 路由, maxLCP=' + report.summary.maxLCP + 'ms');
});
