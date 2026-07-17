/**
 * useLazyModal — 延迟加载 + 弹窗控制的一体化钩子
 *
 * ## 解决的问题
 * 当弹窗组件通过 defineAsyncComponent 延迟加载时，用户可能在组件未就绪时
 * 点击打开按钮 → useModal.openModal 的 `?.` 静默失败，弹窗无法打开。
 *
 * ## 四层防御机制
 * 1. **加载感知队列** — 组件未就绪时 openModal 入队，register 后自动回放
 * 2. **按钮层守卫** — 暴露 isLoaded，按钮绑定 :loading="!isLoaded" + @mouseenter="prefetch"
 * 3. **错误边界** — 页面级 onErrorCaptured 防止级联崩溃
 * 4. **超时收窄** — 生产 10s 快速失败；开发态 120s 适配 Vite 首次编译
 * 5. **延迟挂载** — 进页不渲染弹窗 DOM，仅 prefetch / openModal 时才挂载并拉 chunk
 *
 * ## 用法
 * ```typescript
 * const { component: Form, register: registerForm, openModal: openFormModal, isLoaded } =
 *   useLazyModal(() => import('./Form.vue'), 'onlineDev/webDesign');
 *
 * // 模板: 完全兼容现有 @register 模式
 * // <Form @register="registerForm" @reload="reload" />
 * // <a-button :loading="!isLoaded" @mouseenter="prefetch" @click="openFormModal(true, data)">打开</a-button>
 * ```
 *
 * ## 与 useModal + useLazyComponent 的差异
 * - 接口兼容：register / openModal / closeModal 签名一致，可平滑迁移
 * - 额外暴露：isLoaded / prefetch 用于按钮层守卫
 * - openModal 变为 async：等待组件就绪后再打开（失败时 toast 错误提示）
 */

import { ref, type Component, type Ref } from 'vue';
import { useLazyComponent } from './useLazyComponent';
import type { LazyComponentResult } from './useLazyComponent';
import { useModal } from '/@/components/Modal';
import type { UseModalReturnType, RegisterFn } from '/@/components/Modal/src/typing';
import { useMessage } from '/@/hooks/web/useMessage';
import { isString } from '/@/utils/is';

interface LazyModalResult {
  /** 异步组件，直接用于模板（完全兼容 @register 模式） */
  component: Component;
  /** 注册函数，传给组件的 @register 事件（已内置队列回放逻辑） */
  register: RegisterFn;
  /** 安全打开弹窗：组件未就绪时等待加载，失败时 toast 报错 */
  openModal: <T = any>(visible?: boolean, data?: T, openOnSet?: boolean) => Promise<void>;
  /** 关闭弹窗 */
  closeModal: () => void;
  /** 是否已加载完成 */
  isLoaded: Ref<boolean>;
  /** 手动触发预加载（如绑定到按钮的 @mouseenter） */
  prefetch: () => Promise<void>;
}

/**
 * 一体化延迟加载弹窗钩子。
 *
 * @param loader - 动态 import 函数，如 () => import('./Form.vue')
 * @param moduleOrOptions - 模块名（字符串）或完整 LazyOptions
 */
export function useLazyModal(
  loader: () => Promise<Record<string, any>>,
  moduleOrOptions: string | { moduleName: string; timeout?: number },
): LazyModalResult {
  const { component, isLoaded, prefetch, load } = useLazyComponent(loader, moduleOrOptions);
  const [registerFn, methods] = useModal();
  const { createMessage } = useMessage();

  // ── 第 1 层：加载感知队列 ──
  // 组件未就绪时暂存 openModal 调用参数，组件 register 后自动回放
  const pendingQueue = ref<{
    visible: boolean;
    data?: any;
    openOnSet?: boolean;
  } | null>(null);

  /** 包装 register：在组件挂载注册时回放排队的 openModal 调用 */
  function wrappedRegister(modalMethod: any, uuid: string) {
    registerFn(modalMethod, uuid);

    const pending = pendingQueue.value;
    if (pending) {
      pendingQueue.value = null;
      // 延迟 50ms 确保组件内部状态就绪
      setTimeout(() => {
        methods.openModal(pending.visible, pending.data, pending.openOnSet);
      }, 50);
    }
  }

  /** 安全打开：组件未就绪时等待加载，失败则 toast 报错 */
  async function safeOpen<T = any>(visible = true, data?: T, openOnSet = true): Promise<void> {
    // 组件已就绪 → 直接打开
    if (isLoaded.value) {
      methods.openModal(visible, data, openOnSet);
      return;
    }

    // 组件未就绪 → 入队等待
    pendingQueue.value = { visible, data, openOnSet };

    let hide: (() => void) | undefined;
    try {
      hide = createMessage.loading('组件加载中…', 0);
    } catch {
      // message.loading 在某些上下文可能不可用，忽略
    }

    try {
      await load();
    } catch (err) {
      // 加载失败 → 清空队列、显示错误、静默返回
      pendingQueue.value = null;
      if (hide) hide();
      createMessage.error('组件加载失败，请刷新页面后重试');
      console.error('[useLazyModal] 组件加载失败', { moduleOrOptions, error: err });
      return;
    }

    if (hide) hide();

    // 加载成功但可能在 load() 期间 register 已触发并回放了队列
    // 如果队列还在（说明 register 还没来），保留让 wrappedRegister 回放
    // 否则直接打开
    if (!pendingQueue.value) {
      // 已被 wrappedRegister 回放，不再重复打开
      return;
    }
    pendingQueue.value = null;
    methods.openModal(visible, data, openOnSet);
  }

  return {
    component,
    register: wrappedRegister,
    openModal: safeOpen,
    closeModal: methods.closeModal,
    isLoaded,
    prefetch,
  };
}

export type { LazyModalResult };
