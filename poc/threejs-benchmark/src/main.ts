/**
 * PoC-B: Three.js 性能基线 — 入口
 *
 * 用法: cd poc/threejs-benchmark && pnpm install && pnpm dev
 */
import { BenchmarkScene } from './scene/BenchmarkScene';

const app = document.getElementById('app');
if (!app) throw new Error('#app not found');

app.style.cssText = 'width: 100vw; height: 100vh; position: relative;';

const scene = new BenchmarkScene(app);

// 显示启动信息
const infoEl = document.createElement('div');
infoEl.style.cssText = `
  position: fixed; top: 50%; left: 50%; transform: translate(-50%,-50%);
  color: #fff; font-family: monospace; font-size: 16px;
  text-align: center; z-index: 2000;
  background: rgba(0,0,0,0.8); padding: 24px 36px; border-radius: 12px;
`;
infoEl.innerHTML = `
  <h2 style="margin:0 0 12px; color:#00d4ff">PoC-B: Three.js 性能基线</h2>
  <p style="color:#aaa">10 万面混合几何体 | 20 POI | 5 飞线 | 10 分钟</p>
  <p style="color:#888; margin-top:16px">初始化中...</p>
`;
app.appendChild(infoEl);

// 延迟启动以确保 DOM 就绪
setTimeout(async () => {
  infoEl.querySelector('p:last-child')!.textContent = '正在生成几何体...';

  const stats = scene.init();
  console.log('[PoC-B] Geometry stats:', stats);

  infoEl.innerHTML = `
    <h2 style="margin:0 0 12px; color:#00d4ff">PoC-B Ready</h2>
    <table style="color:#aaa; text-align:left; margin:0 auto; line-height:1.8">
      <tr><td>总面数:</td><td style="color:#0f0">${stats.totalFaces.toLocaleString()}</td></tr>
      <tr><td>建筑:</td><td>${stats.buildings} 栋</td></tr>
      <tr><td>地形段:</td><td>${stats.terrainSegments} seg</td></tr>
      <tr><td>设备:</td><td>${stats.equipment} 个</td></tr>
      <tr><td>POI:</td><td>20 个</td></tr>
      <tr><td>飞线:</td><td>5 条</td></tr>
    </table>
    <button id="pocb-start" style="
      margin-top:16px; padding:10px 32px; font-size:16px; cursor:pointer;
      background:#00d4ff; color:#000; border:none; border-radius:6px; font-weight:bold;
    ">开始 10 分钟测试</button>
    <p style="color:#666; margin-top:10px; font-size:12px">按下按钮后控制台输出每分钟指标</p>
  `;
  app.appendChild(infoEl); // re-append (overwrites old)

  document.getElementById('pocb-start')!.addEventListener('click', async () => {
    infoEl.style.display = 'none';
    console.log('[PoC-B] Starting 10-minute benchmark...');

    const result = await scene.start();
    console.log('[PoC-B] Benchmark complete:', result);

    // 显示结果
    const { session, passed, failureReason } = result;
    const resultEl = document.createElement('div');
    resultEl.style.cssText = `
      position: fixed; top: 50%; left: 50%; transform: translate(-50%,-50%);
      color: #fff; font-family: monospace; font-size: 16px; text-align: center;
      z-index: 2000; background: rgba(0,0,0,0.9); padding: 24px 36px;
      border-radius: 12px; border: 2px solid ${passed ? '#0f0' : '#f00'};
    `;
    resultEl.innerHTML = `
      <h2 style="margin:0 0 16px; color:${passed ? '#0f0' : '#f00'}">
        ${passed ? '✅ 通过' : '❌ 未通过'}
      </h2>
      <table style="color:#aaa; text-align:left; margin:0 auto; line-height:1.8">
        <tr><td>平均 FPS:</td><td style="color:${session.avgFps >= 30 ? '#0f0' : '#f00'}; font-size:20px; font-weight:bold">${session.avgFps}</td></tr>
        <tr><td>最低 FPS:</td><td>${session.minFps}</td></tr>
        <tr><td>最高 FPS:</td><td>${session.maxFps}</td></tr>
        <tr><td>低于30fps次数:</td><td style="color:${session.fpsBelow30Count > 0 ? '#f80' : '#0f0'}">${session.fpsBelow30Count} / ${session.frames.length} 采样</td></tr>
        <tr><td>总帧数:</td><td>${session.totalFrames.toLocaleString()}</td></tr>
        <tr><td>持续时间:</td><td>${Math.floor(session.durationMs / 1000)}s</td></tr>
      </table>
      ${failureReason ? `<p style="color:#f80; margin-top:12px">${failureReason}</p>` : ''}
      <button onclick="location.reload()" style="
        margin-top:16px; padding:8px 24px; font-size:14px; cursor:pointer;
        background:#333; color:#fff; border:1px solid #555; border-radius:6px;
      ">重新测试</button>
    `;
    app.appendChild(resultEl);
  });
}, 500);
