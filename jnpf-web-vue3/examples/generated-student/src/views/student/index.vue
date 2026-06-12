<!-- @jnpf-generated v1.0.0 entity=student type=list-page -->
<!-- 生成时间：2026-06-12T15:11:18.006Z -->
<!-- 此文件由 JNPF 代码生成器生成，可手动修改 -->

<template>
  <div class="student-list">
    <a-card>
      <a-form layout="inline" class="search-bar">
      <a-form-item label="姓名">
        <a-input v-model:value="searchParams.employeeName" placeholder="请输入姓名" allow-clear />
      </a-form-item>
      <a-form-item label="部门">
        <a-input v-model:value="searchParams.department" placeholder="请输入部门" allow-clear />
      </a-form-item>
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

    <studentForm
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
import type { StudentEntity, StudentQueryParams } from './types';
import { getStudentList, deleteStudent, batchDeleteStudent } from './api';
import StudentForm from './form.vue';
import { columns } from './columns';
// @jnpf-gen:insert-point=custom-imports
// @jnpf-gen:end-insert-point=custom-imports

const searchParams = reactive<Record<string, string>>({
  employeeName: '',
  department: '',
});

const tableData = ref<StudentEntity[]>([]);
const loading = ref(false);
const selectedRowKeys = ref<string[]>([]);
const pagination = reactive<TablePaginationConfig>({
  current: 1,
  pageSize: 20,
  total: 0,
  showSizeChanger: true,
  showTotal: (total: number) => `共 ${total} 条`,
});

const formVisible = ref(false);
const currentRecord = ref<StudentEntity | undefined>();

async function loadData() {
  loading.value = true;
  try {
    const params: StudentQueryParams = {
      currentPage: pagination.current ?? 1,
      pageSize: pagination.pageSize ?? 20,
      ...searchParams,
    };
    const res = await getStudentList(params);
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

function handleEdit(record: StudentEntity) {
  currentRecord.value = record;
  formVisible.value = true;
}

async function handleDelete(id: string) {
  await deleteStudent(id);
  message.success('删除成功');
  loadData();
}

async function handleBatchDelete() {
  await batchDeleteStudent(selectedRowKeys.value);
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
