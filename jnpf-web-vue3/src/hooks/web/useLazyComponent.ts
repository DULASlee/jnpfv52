/**
 * useLazyComponent — 按需异步加载组件的通用封装
 *
 * ## 解决的问题
 * 将弹窗/设计器等重型组件从入口 bundle 中分离，
 * 仅在真正需要渲染时才加载，显著减少首屏下载/解析/执行时间。
 *
 * ## 三层防护机制
 * 1. **预加载触发** — expose `prefetch()` to start loading on button hover
 * 2. **加载状态守卫** — expose `isLoaded` ref so UI can disable buttons until ready
 * 3. **结构化错误日志** — module/page/line 精确定位，超时控制，优雅降级
 *
 * ## 用法
 * ```typescript
 * // ❌ 之前：静态导入 → 打入主 bundle
 * import Form from './Form.vue';
 *
 * // ✅ 之后：按需加载 → 独立 chunk，hover 预加载，状态可见
 * const { component: Form, isLoaded, prefetch } = useLazyComponent(
 *   () => import('./Form.vue'),
 *   'onlineDev/webDesign'
 * );
 * // 模板中使用: <Form @register="registerForm" />
 * // 按钮上绑定: @mouseenter="prefetch" :disabled="!isLoaded"
 * ```
 */

import { defineAsyncComponent, defineComponent, Component, h, ref, Ref } from 'vue';
import { Spin } from 'ant-design-vue';

/** 开发态 Vite 首次编译重型 chunk 较慢；生产保持快速失败 */
const DEFAULT_LAZY_TIMEOUT = import.meta.env.DEV ? 120_000 : 10_000;

// ── 轻量级加载占位 ──
const DefaultLoadingComponent = {
  setup() {
    return () =>
      h(
        'div',
        {
          class: 'lazy-component-loading',
          style: 'display:flex;align-items:center;justify-content:center;padding:60px 0;',
        },
        [h(Spin, { size: 'large', tip: '组件加载中...' })],
      );
  },
};

// ── 错误占位 + 结构化日志 ──
function createErrorComponent(moduleName: string, componentPath: string) {
  return {
    setup() {
      return () =>
        h(
          'div',
          {
            class: 'lazy-component-error',
            style: 'display:flex;flex-direction:column;align-items:center;justify-content:center;padding:60px 0;color:#ff4d4f;',
          },
          [
            h('div', { style: 'font-size:16px;font-weight:600;margin-bottom:8px;' }, '组件加载失败'),
            h('div', { style: 'font-size:13px;color:#999;' }, `模块: ${moduleName} | 组件: ${componentPath}`),
            h('div', { style: 'font-size:12px;color:#bbb;margin-top:4px;' }, '请检查网络连接后刷新页面重试'),
          ],
        );
    },
  };
}

// ── 选项 ──
interface LazyOptions {
  /** 模块/页面名称，用于错误日志定位 */
  moduleName: string;
  /** 自定义加载中组件 */
  loadingComponent?: Component;
  /** 加载超时时间（ms），超时后触发 errorComponent；开发态默认 120s，生产 10s */
  timeout?: number;
  /** 是否延迟加载（ms），默认 0 表示立即开始加载 */
  delay?: number;
}

interface LazyComponentResult {
  /** 异步组件，直接用于模板 */
  component: Component;
  /** 是否已加载完成，可用于控制按钮 disabled 状态 */
  isLoaded: Ref<boolean>;
  /** 手动触发预加载（如绑定到按钮的 @mouseenter），返回 Promise 以便调用方 await */
  prefetch: () => Promise<void>;
  /** 同 prefetch，语义更明确（用于需要等待加载完成的场景，如 useLazyModal） */
  load: () => Promise<void>;
}

export type { LazyComponentResult };

export function useLazyComponent(loader: () => Promise<Record<string, any>>, moduleOrOptions: string | LazyOptions): LazyComponentResult {
  const opts: LazyOptions = typeof moduleOrOptions === 'string' ? { moduleName: moduleOrOptions } : moduleOrOptions;

  const { moduleName, loadingComponent, timeout = DEFAULT_LAZY_TIMEOUT, delay = 0 } = opts;

  // 从 loader 的 toString 中提取文件路径用于错误定位
  const componentPath = extractPath(loader);

  // 加载状态跟踪
  const isLoaded = ref(false);
  const isLoading = ref(false);
  const loadPromise = ref<Promise<Record<string, any>> | null>(null);
  /** 仅 prefetch / openModal / load 时才挂载异步组件，避免进页并行拉取多个重型 chunk */
  const shouldMount = ref(false);

  let timeoutId: ReturnType<typeof setTimeout> | null = null;

  function ensureMount() {
    shouldMount.value = true;
  }

  /** 开始加载（幂等 — 多次调用只触发一次加载） */
  function triggerLoad(): Promise<Record<string, any>> {
    ensureMount();
    if (isLoaded.value) return Promise.resolve({} as Record<string, any>);
    if (loadPromise.value) return loadPromise.value;

    isLoading.value = true;

    const promise = new Promise<Record<string, any>>((resolve, reject) => {
      // 超时控制
      timeoutId = setTimeout(() => {
        reject(new Error(`[LazyComponent] 加载超时 (${timeout}ms) | Module: ${moduleName} | Path: ${componentPath}`));
      }, timeout) as unknown as ReturnType<typeof setTimeout>;

      // 延迟加载
      const doLoad = () => {
        loader()
          .then(mod => {
            if (timeoutId) clearTimeout(timeoutId);
            isLoaded.value = true;
            isLoading.value = false;
            resolve(mod);
          })
          .catch(err => {
            if (timeoutId) clearTimeout(timeoutId);
            isLoading.value = false;
            loadPromise.value = null; // 失败后允许重试
            console.error(
              `[LazyComponent] 加载失败 | Module: ${moduleName} | Path: ${componentPath}`,
              '\n  错误详情:',
              err,
              '\n  可能原因: 网络异常 / chunk 404 / 磁盘空间不足',
            );
            reject(err);
          });
      };

      if (delay > 0) {
        setTimeout(doLoad, delay);
      } else {
        doLoad();
      }
    });

    loadPromise.value = promise;
    return promise;
  }

  const asyncComp = defineAsyncComponent({
    loader: () => triggerLoad(),
    loadingComponent: loadingComponent || DefaultLoadingComponent,
    errorComponent: createErrorComponent(moduleName, componentPath),
    delay: 0, // 由 triggerLoad 内部处理延迟
    timeout,
    onError(error, _retry, fail) {
      console.error(`[LazyComponent] 重试失败，放弃加载 | Module: ${moduleName} | Path: ${componentPath}`, '\n  最终错误:', error);
      fail();
    },
  });

  const component = defineComponent({
    name: `LazyDeferred_${moduleName.replace(/\W+/g, '_')}`,
    inheritAttrs: true,
    setup(_props, { attrs, slots }) {
      return () => (shouldMount.value ? h(asyncComp, attrs, slots) : null);
    },
  });

  function prefetch(): Promise<void> {
    return triggerLoad().catch(() => {
      // prefetch 失败不阻塞 UI，仅记录
    });
  }

  /** 语义化别名：等待加载完成（失败抛出异常，调用方可 try/catch） */
  function load(): Promise<void> {
    return triggerLoad().then(() => {});
  }

  return {
    component,
    isLoaded,
    prefetch,
    load,
  };
}

/** 从 () => import('./Foo.vue') 中提取 './Foo.vue' */
function extractPath(loader: () => Promise<unknown>): string {
  try {
    const src = loader.toString();
    const match = src.match(/import\s*\(\s*['"]([^'"]+)['"]\s*\)/);
    return match ? match[1] : '<unknown>';
  } catch {
    return '<unknown>';
  }
}
