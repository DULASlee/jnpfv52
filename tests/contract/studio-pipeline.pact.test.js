/**
 * Studio Pipeline API — Pact Consumer Contract Tests
 *
 * 验证前端 (jnpf-web-vue3) 与后端 (JNPF.API.Entry) 之间的 API 合约。
 * 后端修改 API 响应格式时，Pact 验证会立即捕获破坏性变更。
 *
 * 运行:
 *   cd tests/contract && npm test
 *   npm run pact:verify    # 验证 provider 端
 *
 * 产出:
 *   tests/contract/pacts/ — Pact 合约文件（可提交到 Pact Broker）
 */

import path from 'path';
import { fileURLToPath } from 'url';
import { PactV3, MatchersV3, SpecificationVersion } from '@pact-foundation/pact/v3';
const { like, string, integer, eachLike, boolean } = MatchersV3;

const __dirname = path.dirname(fileURLToPath(import.meta.url));

// ── Provider 配置 ──
const provider = new PactV3({
  consumer: 'jnpf-web-vue3',
  provider: 'jnpf-api-entry',
  dir: path.resolve(__dirname, 'pacts'),
  spec: SpecificationVersion.SPECIFICATION_VERSION_V3,
  host: '127.0.0.1',
  port: 5000,
});

// ═══════════════════════════════════════════════════════════
// 合约 1: POST /api/studio/pipeline/execute — 创建/触发流水线
// ═══════════════════════════════════════════════════════════
describe('Studio Pipeline Execute API', () => {
  it('创建新流水线并返回 pipelineId', () => {
    return provider
      .uponReceiving('创建新 SA 流水线')
      .withRequest({
        method: 'POST',
        path: '/api/studio/pipeline/execute',
        headers: {
          'Content-Type': 'application/json',
          'jnpf-origin': 'pc',
        },
        body: {
          requirement: like('员工请假系统：员工提交请假单，主管审批，HR归档'),
          workMode: like('greenfield'),
        },
      })
      .willRespondWith({
        status: 200,
        headers: { 'Content-Type': 'application/json; charset=utf-8' },
        body: {
          code: integer(200),
          data: {
            pipelineId: integer(311),
          },
          msg: string(''),
        },
      });
  });

  it('返回当前用户信息（冒烟）', () => {
    return provider
      .uponReceiving('获取当前登录用户')
      .withRequest({
        method: 'GET',
        path: '/api/oauth/CurrentUser',
        headers: { 'jnpf-origin': 'pc' },
      })
      .willRespondWith({
        status: 200,
        headers: { 'Content-Type': 'application/json; charset=utf-8' },
        body: {
          code: integer(200),
          data: {
            account: string('admin'),
            id: integer(1),
          },
        },
      });
  });
});

// ═══════════════════════════════════════════════════════════
// 合约 2: GET /api/studio/pipeline/execute/:id/deliverables
// ═══════════════════════════════════════════════════════════
describe('Studio Pipeline Deliverables API', () => {
  it('返回交付物列表', () => {
    return provider
      .uponReceiving('获取流水线交付物列表')
      .withRequest({
        method: 'GET',
        path: '/api/studio/pipeline/execute/311/deliverables',
        headers: { 'jnpf-origin': 'pc' },
      })
      .willRespondWith({
        status: 200,
        headers: { 'Content-Type': 'application/json; charset=utf-8' },
        body: {
          code: integer(200),
          data: eachLike({
            fileName: string('00-merged-requirement.md'),
            relativePath: string('pipeline-311/00-merged-requirement.md'),
            fileSize: integer(1024),
          }, { min: 3 }),
        },
      });
  });
});

// ═══════════════════════════════════════════════════════════
// 合约 3: GET /api/studio/pipeline/execute/:id/events — SSE
// ═══════════════════════════════════════════════════════════
describe('Studio Pipeline IR Events API', () => {
  it('返回 IR 事件列表', () => {
    return provider
      .uponReceiving('获取流水线 IR 事件')
      .withRequest({
        method: 'GET',
        path: '/api/studio/pipeline/execute/311/events',
        headers: { 'jnpf-origin': 'pc' },
      })
      .willRespondWith({
        status: 200,
        headers: { 'Content-Type': 'application/json; charset=utf-8' },
        body: {
          code: integer(200),
          data: eachLike({
            eventId: string('EV-001'),
            eventType: string('SkeletonCreated'),
            fragmentType: string('IR0_Skeleton'),
            payloadPreview: {},
            createdAt: string('2026-07-06T10:00:00Z'),
          }),
        },
      });
  });
});
