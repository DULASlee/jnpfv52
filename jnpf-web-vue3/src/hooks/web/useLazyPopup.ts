import { ref, type Component, type Ref } from 'vue';
import { usePopup } from '/@/components/Popup';
import type { PopupInstance, RegisterFn } from '/@/components/Popup';
import { useLazyComponent } from './useLazyComponent';
import { useMessage } from '/@/hooks/web/useMessage';

interface LazyPopupResult {
  /** 异步组件，直接用于模板（完全兼容 @register 模式） */
  component: Component;
  /** 注册函数，传给组件的 @register 事件（已内置队列回放逻辑） */
  register: RegisterFn;
  /** 安全打开弹窗：组件未就绪时等待加载，失败时 toast 报错 */
  openPopup: <T = any>(visible?: boolean, data?: T, openOnSet?: boolean) => Promise<void>;
  /** 关闭弹窗 */
  closePopup: () => void;
  /** 是否已加载完成 */
  isLoaded: Ref<boolean>;
  /** 手动触发预加载（如绑定到按钮的 @mouseenter） */
  prefetch: () => Promise<void>;
}

/**
 * 延迟加载 + 弹窗控制的一体化钩子（Popup 版，对应 useLazyModal 的 Modal 版）。
 *
 * 解决的问题：重型弹窗（如流程解析器）通过 defineAsyncComponent 延迟加载时，
 * 模板中的无条件挂载仍会在进页即触发 chunk 拉取；本钩子用 shouldMount 机制
 * 保证「进页不渲染弹窗 DOM，仅在 prefetch / openPopup 时才挂载并拉 chunk」，
 * 且 openPopup 在组件未就绪时入队、register 后自动回放，避免点击竞态。
 */
export function useLazyPopup(loader: () => Promise<Record<string, any>>, moduleOrOptions: string | { moduleName: string; timeout?: number }): LazyPopupResult {
  const { component, isLoaded, prefetch, load } = useLazyComponent(loader, moduleOrOptions);
  const [registerFn, methods] = usePopup();
  const { createMessage } = useMessage();

  // 组件未就绪时暂存 openPopup 调用，组件 register 后自动回放
  const pendingQueue = ref<{ visible: boolean; data?: any; openOnSet?: boolean } | null>(null);

  function wrappedRegister(popupInstance: PopupInstance, uuid?: string) {
    registerFn(popupInstance, uuid);

    const pending = pendingQueue.value;
    if (pending) {
      pendingQueue.value = null;
      // 延迟 50ms 确保组件内部状态就绪
      setTimeout(() => {
        methods.openPopup(pending.visible, pending.data, pending.openOnSet);
      }, 50);
    }
  }

  /** 安全打开：组件未就绪时等待加载，失败则 toast 报错 */
  async function safeOpen<T = any>(visible = true, data?: T, openOnSet = true): Promise<void> {
    if (isLoaded.value) {
      methods.openPopup(visible, data, openOnSet);
      return;
    }

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
      pendingQueue.value = null;
      if (hide) hide();
      createMessage.error('组件加载失败，请刷新页面后重试');
      console.error('[useLazyPopup] 组件加载失败', { moduleOrOptions, error: err });
      return;
    }

    if (hide) hide();

    if (!pendingQueue.value) {
      // 已被 wrappedRegister 回放，不再重复打开
      return;
    }
    pendingQueue.value = null;
    methods.openPopup(visible, data, openOnSet);
  }

  return {
    component,
    register: wrappedRegister,
    openPopup: safeOpen,
    closePopup: methods.closePopup,
    isLoaded,
    prefetch,
  };
}
