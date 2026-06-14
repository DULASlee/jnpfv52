/**
 * 统一编译网关测试
 */

import { describe, it, expect } from 'vitest';
import { compileGateway, compileMultiTarget, getAvailableTargets, getTargetMeta } from '../gateway';
import type { CompileTarget } from '../targets';

// 最小 Form Schema（platform-native format）
const minimalFormSchema = {
  data: {
    formData: JSON.stringify({
      fields: [
        {
          __vModel__: 'name',
          __config__: {
            label: '姓名',
            tag: 'JnpfInput',
            jnpfKey: 'JnpfInput',
            required: true,
          },
        },
        {
          __vModel__: 'age',
          __config__: {
            label: '年龄',
            tag: 'JnpfInputNumber',
            jnpfKey: 'JnpfInputNumber',
          },
        },
      ],
      tabs: {},
      virtualFieldList: [],
    }),
  },
};

// Dashboard mock
const mockDashboardSchema = {
  type: 'dashboard',
  id: 'test-dashboard',
  name: '测试大屏',
  size: { width: 1920, height: 1080 },
  background: { type: 'color', value: '#0d0d0d' },
  theme: 'dark',
  widgets: [],
  dataSources: [],
  expressions: [],
};

// ============================================================
// 网关测试
// ============================================================

describe('compileGateway — 统一编译网关', () => {
  it('vue3-web: 从 FormSchema 生成 Vue3 项目', async () => {
    const response = await compileGateway({
      schema: minimalFormSchema,
      target: 'vue3-web',
      config: { entity: 'test-entity' },
    });
    expect(response.success).toBe(true);
    expect(response.project).toBeDefined();
    expect(response.project!.size).toBeGreaterThan(0);
    expect(response.duration).toBeGreaterThan(0);
    expect(response.targetMeta).toBeDefined();
  });

  it('dashboard: 目标元数据正确', () => {
    // cleanSchema 仅处理 form schemas；DashboardIR 走独立入口
    const meta = getTargetMeta('dashboard');
    expect(meta).not.toBeNull();
    expect(meta!.id).toBe('dashboard');
    expect(meta!.irType).toBe('dashboard');
  });

  it('uniapp-weixin: 生成微信小程序', async () => {
    const response = await compileGateway({
      schema: minimalFormSchema,
      target: 'uniapp-weixin',
      config: { entity: 'student' },
    });
    expect(response.success).toBe(true);
    expect(response.project!.has('pages/student/list.vue')).toBe(true);
    expect(response.targetMeta!.id).toBe('uniapp-weixin');
  });

  it('uniapp-alipay: platform 标识正确', async () => {
    const response = await compileGateway({
      schema: minimalFormSchema,
      target: 'uniapp-alipay',
      config: { entity: 'student' },
    });
    expect(response.success).toBe(true);
    const list = response.project!.get('pages/student/list.vue')!;
    expect(list).toContain('platform=mp-alipay');
  });

  // ── 错误处理 ──

  it('未知目标返回错误', async () => {
    const response = await compileGateway({
      schema: minimalFormSchema,
      target: 'unknown-target' as CompileTarget,
      config: { entity: 'test' },
    });
    expect(response.success).toBe(false);
    expect(response.error).toContain('未知编译目标');
  });

  it('无效 Schema 被 IR 验证器捕获', async () => {
    const response = await compileGateway({
      schema: { invalid: true },
      target: 'vue3-web',
      config: { entity: 'test' },
    });
    // cleanSchema 不抛异常但返回空 IR → validateIR 检测到 fields 为空
    expect(response.success).toBe(false);
  });
});

// ============================================================
// 批量编译
// ============================================================

describe('compileMultiTarget — 批量编译', () => {
  it('同时生成 vue3-web + uniapp-weixin', async () => {
    const results = await compileMultiTarget(minimalFormSchema, ['vue3-web', 'uniapp-weixin'], { entity: 'student' });
    expect(results.size).toBe(2);
    expect(results.get('vue3-web')!.success).toBe(true);
    expect(results.get('uniapp-weixin')!.success).toBe(true);
  });

  it('部分目标失败不影响其他', async () => {
    const results = await compileMultiTarget(minimalFormSchema, ['vue3-web', 'unknown-target' as CompileTarget], { entity: 'test' });
    expect(results.get('vue3-web')!.success).toBe(true);
    expect(results.get('unknown-target' as CompileTarget)!.success).toBe(false);
  });
});

// ============================================================
// 工具函数
// ============================================================

describe('getAvailableTargets', () => {
  it('返回 9 个目标（含 workflow）', () => {
    expect(getAvailableTargets().length).toBe(9);
  });

  it('按 form IR 类型过滤', () => {
    const formTargets = getAvailableTargets('form');
    expect(formTargets.every(t => !t.startsWith('dashboard'))).toBe(true);
  });

  it('按 dashboard IR 类型过滤', () => {
    const dashTargets = getAvailableTargets('dashboard');
    expect(dashTargets.every(t => t.startsWith('dashboard'))).toBe(true);
  });
});

describe('getTargetMeta', () => {
  it('dashboard-3d 标记为 VIP', () => {
    const meta = getTargetMeta('dashboard-3d');
    expect(meta).not.toBeNull();
    expect(meta!.vip).toBe(true);
  });

  it('vue3-web 非 VIP', () => {
    const meta = getTargetMeta('vue3-web');
    expect(meta!.vip).toBe(false);
  });
});
