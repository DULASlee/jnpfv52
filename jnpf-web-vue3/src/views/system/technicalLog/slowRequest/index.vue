<template>
  <div class="jnpf-content-wrapper">
    <div class="jnpf-content-wrapper-center">
      <div class="jnpf-content-wrapper-content">
        <BasicTable @register="registerTable">
          <template #bodyCell="{ column, record }">
            <template v-if="column.key === 'traceId'">
              <a v-if="record.traceId" @click="goToTrace(record.traceId)">{{ record.traceId }}</a>
            </template>
            <template v-if="column.key === 'duration'">
              <span :class="{ 'text-red-500': record.duration > 5000 }">{{ record.duration }} ms</span>
            </template>
          </template>
        </BasicTable>
      </div>
    </div>
  </div>
</template>
<script lang="ts" setup>
  import { onMounted } from 'vue';
  import { useRouter } from 'vue-router';
  import { BasicTable, useTable, BasicColumn } from '/@/components/Table';
  import { getSlowRequestList } from '/@/api/system/technicalLog';

  defineOptions({ name: 'SystemSlowRequestLog' });

  const router = useRouter();

  const columns: BasicColumn[] = [
    { title: '时间', dataIndex: 'timestamp', width: 180, format: 'date|YYYY-MM-DD HH:mm:ss.SSS' },
    { title: '耗时(ms)', dataIndex: 'duration', width: 120, align: 'right' },
    { title: 'SQL摘要', dataIndex: 'sqlSummary', width: 400, ellipsis: true },
    { title: 'TraceId', dataIndex: 'traceId', width: 180, ellipsis: true },
  ];

  const [registerTable, { reload }] = useTable({
    api: getSlowRequestList,
    columns,
    useSearchForm: true,
    formConfig: {
      schemas: [
        {
          field: 'date',
          label: '日期',
          component: 'DatePicker',
          componentProps: {
            format: 'YYYY-MM-DD',
            placeholder: '选择日期',
            valueFormat: 'YYYY-MM-DD',
          },
        },
        {
          field: 'thresholdMs',
          label: '阈值(ms)',
          component: 'InputNumber',
          componentProps: {
            placeholder: '默认1000',
            min: 0,
          },
        },
      ],
    },
    pagination: { pageSize: 50 },
  });

  function goToTrace(traceId: string) {
    router.push({ path: '/system/traceDetail', query: { traceId } });
  }

  onMounted(() => {
    reload();
  });
</script>
