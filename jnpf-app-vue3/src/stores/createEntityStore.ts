/**
 * Pinia Store 模板工厂 — UniApp 实体 CRUD Store
 *
 * 编译器调用此函数为每个实体生成独立的 Pinia Store：
 * ```ts
 * const userApi = createEntityApi<UserEntity>('/api/System/User');
 * const useUserStore = createEntityStore('user', userApi);
 * ```
 *
 * @jnpf-generated v5.2.0 type=store platform=uniapp
 */

import { defineStore } from "pinia";
import { ref, reactive } from "vue";
import type { EntityApi } from "../api/request";

/** 分页信息 */
interface PaginationInfo {
  current: number;
  pageSize: number;
  total: number;
}

/** 列表响应（JNPF PageResult 展开后） */
interface ListResult<T> {
  list?: T[];
  pagination?: PaginationInfo;
}

/**
 * 创建通用实体 CRUD Store
 *
 * @param name — Store 唯一名称，如 'user'
 * @param api — createEntityApi 返回的 API 方法集合
 * @returns Pinia Store（setup 语法）
 */
export function createEntityStore<T extends Record<string, unknown>>(
  name: string,
  api: EntityApi<T>,
) {
  return defineStore(name, () => {
    // ==========================================================
    // 状态
    // ==========================================================

    /** 加载中 */
    const loading = ref(false);

    /** 数据列表 */
    const list = ref<T[]>([]) as ReturnType<typeof ref<T[]>>;

    /** 当前编辑/查看的实体 */
    const current = ref<T | null>(null) as ReturnType<typeof ref<T | null>>;

    /** 分页信息 */
    const pagination = reactive<PaginationInfo>({
      current: 1,
      pageSize: 20,
      total: 0,
    });

    /** 搜索参数 */
    const searchParams = ref<Record<string, unknown>>({});

    // ==========================================================
    // 方法
    // ==========================================================

    /** 加载列表（自动合并分页 + 搜索参数） */
    async function loadList(params?: Record<string, unknown>) {
      loading.value = true;
      try {
        const method = api.list({
          ...searchParams.value,
          currentPage: pagination.current,
          pageSize: pagination.pageSize,
          ...params,
        });

        const res = await method.send();

        // JNPF PageResult 格式：{ list: T[], pagination: {...} }
        if (res && typeof res === "object" && "list" in res) {
          const pageResult = res as unknown as ListResult<T>;
          list.value = pageResult.list ?? [];
          if (pageResult.pagination) {
            pagination.current = pageResult.pagination.current;
            pagination.pageSize = pageResult.pagination.pageSize;
            pagination.total = pageResult.pagination.total;
          }
        } else if (Array.isArray(res)) {
          // 直接返回数组
          list.value = res as T[];
        } else {
          list.value = [];
        }
      } finally {
        loading.value = false;
      }
    }

    /** 加载详情 */
    async function loadDetail(id: string) {
      loading.value = true;
      try {
        const method = api.detail(id);
        const res = await method.send();
        current.value = (res as T) ?? null;
      } finally {
        loading.value = false;
      }
    }

    /** 保存（新增或更新） */
    async function save(data: Partial<T>, id?: string) {
      loading.value = true;
      try {
        if (id) {
          await api.update(id, data).send();
        } else {
          await api.create(data).send();
        }
        // 保存后刷新列表
        await loadList();
      } finally {
        loading.value = false;
      }
    }

    /** 删除（单条） */
    async function remove(id: string) {
      loading.value = true;
      try {
        await api.delete(id).send();
        await loadList();
      } finally {
        loading.value = false;
      }
    }

    // ==========================================================
    // 导出
    // ==========================================================

    return {
      // 状态
      loading,
      list,
      current,
      pagination,
      searchParams,
      // 方法
      loadList,
      loadDetail,
      save,
      remove,
    };
  });
}

export type EntityStore<T extends Record<string, unknown>> = ReturnType<
  ReturnType<typeof createEntityStore<T>>
>;
