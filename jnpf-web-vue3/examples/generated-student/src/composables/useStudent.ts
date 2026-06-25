// @jnpf-generated v1.0.0 entity=student type=hook
// 生成时间：2026-06-16T06:22:20.754Z
// 此文件由 JNPF 代码生成器生成，可手动修改

/* eslint-disable */
import { ref, reactive } from 'vue';
import { message } from 'ant-design-vue';
import type { StudentEntity, StudentQueryParams } from '../types';
import { getStudentList, deleteStudent, batchDeleteStudent } from '../api';

/** 学生管理 列表 Hook */
export function useStudentList() {
  const loading = ref(false);
  const tableData = ref<StudentEntity[]>([]);
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
      const params: StudentQueryParams = {
        currentPage: pagination.current,
        pageSize: pagination.pageSize,
        ...searchParams,
      };
      const res = await getStudentList(params);
      tableData.value = res.data ?? [];
      pagination.total = res.data?.length ?? 0;
    } finally {
      loading.value = false;
    }
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
