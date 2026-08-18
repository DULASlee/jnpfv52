/**
 * JNPF Studio Pipeline API — k6 负载测试
 *
 * 场景：
 *   1. 冒烟测试（1 VU × 1 迭代）— 验证基本可用性
 *   2. 负载测试（10 VU × 30s）— 验证响应时间分布
 *   3. 压力测试（50 VU × 60s）— 找拐点
 *
 * 运行:
 *   # 冒烟
 *   k6 run --env SMOKE=1 scripts/load/studio-pipeline.js
 *
 *   # 负载测试（默认）
 *   k6 run scripts/load/studio-pipeline.js
 *
 *   # 压力测试
 *   k6 run --env STRESS=1 --duration=60s scripts/load/studio-pipeline.js
 *
 * 环境变量:
 *   API_URL   — JNPF API 地址 (默认 http://localhost:5000)
 */

import http from 'k6/http';
import { check, sleep, group } from 'k6';
import { Trend, Rate, Counter } from 'k6/metrics';

// ── 自定义指标 ──
const pipelineCreateDuration = new Trend('pipeline_create_duration', true);
const currentUserDuration = new Trend('current_user_duration', true);
const errorRate = new Rate('errors');

// ── 配置 ──
const API_URL = __ENV.API_URL || 'http://localhost:5000';
const SMOKE = __ENV.SMOKE === '1';
const STRESS = __ENV.STRESS === '1';

const COMMON_HEADERS = {
  'Content-Type': 'application/json',
  'jnpf-origin': 'pc',
};

// ── k6 选项 ──
export const options = SMOKE
  ? { vus: 1, iterations: 1, thresholds: { http_req_duration: ['p(95)<3000'] } }
  : STRESS
    ? { stages: [
        { duration: '20s', target: 10 },
        { duration: '30s', target: 50 },
        { duration: '10s', target: 0 },
      ], thresholds: { http_req_duration: ['p(95)<10000', 'p(99)<15000'] } }
    : { vus: 10, duration: '30s', thresholds: { http_req_duration: ['p(95)<5000'] } };

// ── 登录获取 Token（一次，所有 VU 共享） ──
let AUTH_TOKEN = null;

function ensureAuth() {
  if (AUTH_TOKEN) return AUTH_TOKEN;

  const payload = {
    account: __ENV.JNPF_ACCOUNT || 'admin',
    password: __ENV.JNPF_ENCRYPTED_PASSWORD || '',
    origin: 'password',
  };

  // 如果未设置加密密码（需要 MD5+AES），跳过登录，仅测试公开端点
  if (!payload.password) {
    console.warn('⚠️  JNPF_ENCRYPTED_PASSWORD 未设置，仅测试公开端点');
    return null;
  }

  const res = http.post(`${API_URL}/api/oauth/Login`, JSON.stringify(payload), {
    headers: { ...COMMON_HEADERS, 'Content-Type': 'application/x-www-form-urlencoded' },
  });

  if (res.status === 200) {
    const data = res.json();
    AUTH_TOKEN = data?.data?.token || data?.token || null;
    if (AUTH_TOKEN) {
      console.log('✅ Token 已获取');
      COMMON_HEADERS['Authorization'] = `Bearer ${AUTH_TOKEN}`;
    }
  }
  return AUTH_TOKEN;
}

// ── 辅助函数 ──
function apiGet(path) {
  const start = Date.now();
  const res = http.get(`${API_URL}${path}`, { headers: COMMON_HEADERS });
  const elapsed = Date.now() - start;

  const ok = check(res, {
    'status 200': (r) => r.status === 200,
    'has code field': (r) => {
      try { return typeof r.json().code === 'number'; } catch { return false; }
    },
  });

  errorRate.add(!ok);
  return res;
}

function apiPost(path, body) {
  const start = Date.now();
  const res = http.post(`${API_URL}${path}`, JSON.stringify(body), { headers: COMMON_HEADERS });
  const elapsed = Date.now() - start;
  return res;
}

// ── 测试套件 ──
export default function () {
  ensureAuth();

  // ── 冒烟：CurrentUser ──
  group('CurrentUser', () => {
    const res = apiGet('/api/oauth/CurrentUser');
    currentUserDuration.add(res.timings.duration);
    sleep(1);
  });

  // ── 负载：Deliverables ──
  const PIPELINE_ID = __ENV.PIPELINE_ID || '311';

  group('Deliverables', () => {
    const res = apiGet(`/api/studio/pipeline/execute/${PIPELINE_ID}/deliverables`);
    if (res.status === 200) {
      const data = res.json();
      const items = data?.data || [];
      console.log(`   Deliverables: ${Array.isArray(items) ? items.length : '?'} items`);
    }
    sleep(2);
  });

  // ── 压力测试才触发的: Pipeline Create ──
  if (STRESS) {
    group('Pipeline Create', () => {
      const res = apiPost('/api/studio/pipeline/execute', {
        requirement: 'k6 压力测试：员工请假系统（自动生成）',
        workMode: 'greenfield',
      });
      pipelineCreateDuration.add(res.timings.duration);

      const data = res.status === 200 ? res.json() : null;
      if (data?.data?.pipelineId) {
        console.log(`   Created pipeline: ${data.data.pipelineId}`);
      }
      sleep(3);
    });
  }
}

// ── 测试结束时输出摘要 ──
export function handleSummary(data) {
  const summary = {
    timestamp: new Date().toISOString(),
    scenario: SMOKE ? 'smoke' : STRESS ? 'stress' : 'load',
    duration: data.state.testRunDurationMs,
    metrics: {
      http_req_duration: {
        avg: Math.round(data.metrics.http_req_duration?.values?.avg || 0),
        p50: Math.round(data.metrics.http_req_duration?.values?.med || 0),
        p95: Math.round(data.metrics.http_req_duration?.values?.['p(95)'] || 0),
        p99: Math.round(data.metrics.http_req_duration?.values?.['p(99)'] || 0),
      },
      http_req_failed: data.metrics.http_req_failed?.values?.rate || 0,
      iterations: data.metrics.iterations?.values?.count || 0,
    },
    thresholds: data.metrics.http_req_duration?.values?.thresholds || {},
  };

  return {
    'stdout': `\n📊 k6 负载测试报告\n   场景: ${summary.scenario}\n   请求: avg ${summary.metrics.http_req_duration.avg}ms | p95 ${summary.metrics.http_req_duration.p95}ms | 失败 ${(summary.metrics.http_req_failed * 100).toFixed(1)}%\n`,
    '.claude/evidence/k6-summary.json': JSON.stringify(summary, null, 2),
  };
}
