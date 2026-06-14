<template>
  <div class="jnpf-content-wrapper">
    <div class="jnpf-content-wrapper-center">
      <div class="jnpf-content-wrapper-content">
        <BasicTable @register="registerTable">
          <template #tableTitle>
            <a-space>
              <a-select v-model:value="filterResult" placeholder="筛选结果" allowClear style="width: 150px" @change="reload">
                <a-select-option value="allow">allow</a-select-option>
                <a-select-option value="deny">deny</a-select-option>
                <a-select-option value="missing_token">missing_token</a-select-option>
                <a-select-option value="invalid_token">invalid_token</a-select-option>
              </a-select>
              <a-button @click="reload">刷新</a-button>
            </a-space>
          </template>
          <template #bodyCell="{ column, record }">
            <template v-if="column.key === 'result'">
              <a-tag :color="resultColor(record.result)">{{ record.result }}</a-tag>
            </template>
          </template>
        </BasicTable>
      </div>
    </div>
  </div>
</template>

<script lang="ts" setup>
  import { ref } from 'vue';
  import { BasicTable, useTable } from '/@/components/Table';
  import type { BasicColumn } from '/@/components/Table';
  import { getAuthLogs } from '/@/api/founder/index';

  defineOptions({ name: 'FounderAuditLog' });

  const filterResult = ref<string | undefined>(undefined);

  const columns: BasicColumn[] = [
    { title: '时间', dataIndex: 'creatorTime', width: 170, format: 'date|YYYY-MM-DD HH:mm:ss' },
    { title: '操作', dataIndex: 'action', ellipsis: true },
    { title: '结果', dataIndex: 'result', key: 'result', width: 120 },
    { title: 'IP 地址', dataIndex: 'ipAddress', width: 140 },
    { title: 'User-Agent', dataIndex: 'userAgent', ellipsis: true, width: 200 },
  ];

  const [registerTable, { reload }] = useTable({
    api: (params: any) =>
      getAuthLogs({
        currentPage: params.currentPage,
        pageSize: params.pageSize,
        result: filterResult.value,
      }),
    columns,
    showIndexColumn: false,
    immediate: true,
  });

  function resultColor(result: string) {
    if (result?.startsWith('allow')) return 'green';
    if (result === 'deny' || result === 'invalid_token') return 'red';
    if (result === 'missing_token') return 'orange';
    return 'default';
  }
</script>
