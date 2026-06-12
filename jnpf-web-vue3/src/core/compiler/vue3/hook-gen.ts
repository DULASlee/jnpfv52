/**
 * Stage 5：Hook 生成器
 * IR → composables/use{Entity}.ts
 */

import type { CompilerConfig } from './types';

export function generateHook(config: CompilerConfig): string {
  const entity = capitalize(config.entity);
  const now = new Date().toISOString();

  return `// @jnpf-generated v${config.generatorVersion} entity=${config.entity} type=hook
// 生成时间：${now}
// 此文件由 JNPF 代码生成器生成，可手动修改

/* eslint-disable */
import { ref, reactive } from 'vue';
import { message } from 'ant-design-vue';
import type { ${entity}Entity, ${entity}QueryParams } from '../types';
import { get${entity}List, delete${entity}, batchDelete${entity} } from '../api';

/** ${config.entityLabel} 列表 Hook */
export function use${entity}List() {
  const loading = ref(false);
  const tableData = ref<${entity}Entity[]>([]);
  const selectedRowKeys = ref<string[]>([]);
  const pagination = reactive({
    current: 1,
    pageSize: 20,
    total: 0,
  });
  const searchParams = reactive<Record<string, string>>({});

  async function loadData() {
    loading.value = true;
    try {
      const params: ${entity}QueryParams = {
        currentPage: pagination.current,
        pageSize: pagination.pageSize,
        ...searchParams,
      };
      const res = await get${entity}List(params);
      tableData.value = res.data ?? [];
      pagination.total = res.data?.length ?? 0;
    } finally {
      loading.value = false;
    }
  }

  async function handleDelete(id: string) {
    await delete${entity}(id);
    message.success('删除成功');
    loadData();
  }

  async function handleBatchDelete() {
    await batchDelete${entity}(selectedRowKeys.value);
    message.success('批量删除成功');
    selectedRowKeys.value = [];
    loadData();
  }

  function handleSearch() {
    pagination.current = 1;
    loadData();
  }

  function handleReset() {
    Object.keys(searchParams).forEach(k => (searchParams[k] = ''));
    handleSearch();
  }

  return {
    loading,
    tableData,
    selectedRowKeys,
    pagination,
    searchParams,
    loadData,
    handleDelete,
    handleBatchDelete,
    handleSearch,
    handleReset,
  };
}
`;
}

function capitalize(s: string): string {
  return s.charAt(0).toUpperCase() + s.slice(1);
}
