<template>
  <div class="jnpf-content-wrapper">
    <div class="jnpf-content-wrapper-center">
      <div class="jnpf-content-wrapper-content">
        <BasicTable @register="registerTable">
          <template #bodyCell="{ column, record }">
            <template v-if="column.key === 'traceId'">
              <a v-if="record.traceId" @click="goToTrace(record.traceId)">{{ record.traceId }}</a>
            </template>
            <template v-if="column.key === 'message'">
              <a @click="handleExpand(record)">{{ record.message }}</a>
            </template>
          </template>
          <template #expandedRowRender="{ record }">
            <div class="jnpf-error-detail">
              <pre v-if="record.exception" class="jnpf-code-box">{{ record.exception }}</pre>
              <p v-else class="text-gray-400">No exception trace available</p>
            </div>
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
  import { getErrorLogList } from '/@/api/system/technicalLog';
  import dayjs from 'dayjs';

  defineOptions({ name: 'system-error-log' });

  const router = useRouter();

  const columns: BasicColumn[] = [
    { title: '时间', dataIndex: 'timestamp', width: 180, format: 'date|YYYY-MM-DD HH:mm:ss.SSS' },
    { title: '级别', dataIndex: 'level', width: 80, align: 'center' },
    { title: '消息摘要', dataIndex: 'message', width: 400, ellipsis: true },
    { title: 'TraceId', dataIndex: 'traceId', width: 180, ellipsis: true },
  ];

  const [registerTable, { reload }] = useTable({
    api: getErrorLogList,
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
          field: 'keyword',
          label: '关键词',
          component: 'Input',
          componentProps: {
            placeholder: '输入关键词',
            submitOnPressPressEnter: true,
          },
        },
      ],
    },
    pagination: { pageSize: 50 },
    expandRowByClick: true,
  });

  function goToTrace(traceId: string) {
    router.push({ path: '/system/traceDetail', query: { traceId } });
  }
  function handleExpand(record: Recordable) {
    // Expand handled by expandRowByClick
  }

  onMounted(() => {
    reload();
  });
</script>
<style lang="less" scoped>
  .jnpf-error-detail {
    padding: 12px 16px;
    .jnpf-code-box {
      background: #848484;
      padding: 15px;
      color: #fff;
      font-size: 12px;
      border-radius: 4px;
      white-space: pre-wrap;
      word-break: break-all;
      max-height: 400px;
      overflow-y: auto;
    }
  }
</style>
