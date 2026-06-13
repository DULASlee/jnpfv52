/**
 * Stage 3：列表页生成器
 * IR + listConfig → views/{entity}/index.vue + columns.ts + search.ts
 */

import type { FormPageIR } from '../../ir/types';
import type { CompilerConfig } from './types';

export function generateListPage(ir: FormPageIR, config: CompilerConfig): string {
  const entity = capitalize(config.entity);
  const searchFields = ir.listConfig?.searchFields ?? [];
  const now = new Date().toISOString();

  const searchItems = searchFields
    .map(
      sf =>
        `      <a-form-item label="${sf.label}">
        <a-input v-model:value="searchParams.${sf.field}" placeholder="请输入${sf.label}" allow-clear />
      </a-form-item>`,
    )
    .join('\n');

  return `<!-- @jnpf-generated v${config.generatorVersion} entity=${config.entity} type=list-page -->
<!-- 生成时间：${now} -->
<!-- 此文件由 JNPF 代码生成器生成，可手动修改 -->

<template>
  <div class="${config.entity}-list">
    <a-card>
      <a-form layout="inline" class="search-bar">
${searchItems}
        <a-form-item>
          <a-space>
            <a-button type="primary" @click="handleSearch">查询</a-button>
            <a-button @click="handleReset">重置</a-button>
          </a-space>
        </a-form-item>
      </a-form>

      <div class="action-bar">
        <a-space>
          <a-button type="primary" @click="handleAdd">新增</a-button>
          <a-button danger :disabled="!selectedRowKeys.length" @click="handleBatchDelete">
            批量删除
          </a-button>
        </a-space>
        <!-- @jnpf-gen:insert-point=custom-actions -->
        <!-- @jnpf-gen:end-insert-point=custom-actions -->
      </div>

      <a-table
        :columns="columns"
        :data-source="tableData"
        :loading="loading"
        :pagination="pagination"
        :row-selection="{ selectedRowKeys, onChange: onSelectChange }"
        row-key="id"
        @change="handleTableChange"
      >
        <template #bodyCell="{ column, record }">
          <template v-if="column.dataIndex === 'action'">
            <a-space>
              <a @click="handleEdit(record)">编辑</a>
              <a-popconfirm title="确定删除？" @confirm="handleDelete(record.id)">
                <a style="color: red">删除</a>
              </a-popconfirm>
            </a-space>
          </template>
        </template>
      </a-table>
    </a-card>

    <${config.entity}Form
      v-model:visible="formVisible"
      :record="currentRecord"
      @success="handleFormSuccess"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue';
import { message } from 'ant-design-vue';
import type { TablePaginationConfig } from 'ant-design-vue';
import type { ${entity}Entity, ${entity}QueryParams } from './types';
import { get${entity}List, delete${entity}, batchDelete${entity} } from './api';
import ${entity}Form from './form.vue';
import { columns } from './columns';
// @jnpf-gen:insert-point=custom-imports
// @jnpf-gen:end-insert-point=custom-imports

const searchParams = reactive<Record<string, string>>({
${searchFields.map(sf => `  ${sf.field}: '',`).join('\n')}
});

const tableData = ref<${entity}Entity[]>([]);
const loading = ref(false);
const selectedRowKeys = ref<string[]>([]);
const pagination = reactive<TablePaginationConfig>({
  current: 1,
  pageSize: 20,
  total: 0,
  showSizeChanger: true,
  showTotal: (total: number) => \`共 \${total} 条\`,
});

const formVisible = ref(false);
const currentRecord = ref<${entity}Entity | undefined>();

async function loadData() {
  loading.value = true;
  try {
    const params: ${entity}QueryParams = {
      currentPage: pagination.current ?? 1,
      pageSize: pagination.pageSize ?? 20,
      ...searchParams,
    };
    const res = await get${entity}List(params);
    tableData.value = res.data ?? [];
    pagination.total = res.data?.length ?? 0;
  } finally {
    loading.value = false;
  }
}

function handleSearch() {
  pagination.current = 1;
  loadData();
}

function handleReset() {
  Object.keys(searchParams).forEach(k => (searchParams[k] = ''));
  handleSearch();
}

function handleAdd() {
  currentRecord.value = undefined;
  formVisible.value = true;
}

function handleEdit(record: ${entity}Entity) {
  currentRecord.value = record;
  formVisible.value = true;
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

function handleTableChange(pag: TablePaginationConfig) {
  pagination.current = pag.current;
  pagination.pageSize = pag.pageSize;
  loadData();
}

function onSelectChange(keys: string[]) {
  selectedRowKeys.value = keys;
}

function handleFormSuccess() {
  formVisible.value = false;
  loadData();
}

// @jnpf-gen:insert-point=custom-logic
// @jnpf-gen:end-insert-point=custom-logic

onMounted(() => {
  loadData();
});
</script>

<style scoped>
.search-bar {
  margin-bottom: 16px;
}
.action-bar {
  margin: 16px 0;
}
</style>
`;
}

export function generateColumns(ir: FormPageIR, config: CompilerConfig): string {
  const columns = ir.listConfig?.columns ?? [];
  const now = new Date().toISOString();

  const colDefs = columns.map(col => ({
    title: col.label,
    dataIndex: col.field,
    width: col.width ?? undefined,
    fixed: col.fixed ?? undefined,
    sorter: col.sortable ?? false,
  }));

  colDefs.push({
    title: '操作',
    dataIndex: 'action',
    width: 200,
    fixed: 'right' as const,
    sorter: false,
  });

  return `// @jnpf-generated v${config.generatorVersion} entity=${config.entity} type=columns
// 生成时间：${now}

/* eslint-disable */
import type { TableColumn } from './types';

export const columns: TableColumn[] = ${JSON.stringify(colDefs, null, 2)};
`;
}

export function generateSearchConfig(ir: FormPageIR, config: CompilerConfig): string {
  const searchFields = ir.listConfig?.searchFields ?? [];
  const now = new Date().toISOString();

  return `// @jnpf-generated v${config.generatorVersion} entity=${config.entity} type=search
// 生成时间：${now}

/* eslint-disable */
export const searchFields = ${JSON.stringify(
    searchFields.map(sf => ({
      field: sf.field,
      label: sf.label,
      component: sf.component,
    })),
    null,
    2,
  )};
`;
}

function capitalize(s: string): string {
  return s.charAt(0).toUpperCase() + s.slice(1);
}
