<template>
  <div class="jnpf-content-wrapper">
    <div class="jnpf-content-wrapper-center">
      <div class="jnpf-content-wrapper-content">
        <BasicTable @register="registerTable">
          <template #tableTitle>
            <a-button type="error" preIcon="icon-ym icon-ym-btn-clearn" @click="handleBatchDelete">
              {{ t('common.delText') }}
            </a-button>
          </template>
          <template #bodyCell="{ column, record }">
            <template v-if="column.key === 'operationResult'">
              <a-tag :color="record.operationResult == 1 ? 'success' : 'error'">
                {{ record.operationResult == 1 ? '成功' : '失败' }}
              </a-tag>
            </template>
            <template v-if="column.key === 'action'">
              <TableAction :actions="getTableActions(record)" />
            </template>
          </template>
        </BasicTable>
      </div>
    </div>
    <Detail @register="registerDetail" />
  </div>
</template>
<script lang="ts" setup>
  import { onMounted } from 'vue';
  import { BasicTable, useTable, TableAction, BasicColumn, ActionItem } from '/@/components/Table';
  import { getOperationLogList, delOperationLog } from '/@/api/extend/operationLog';
  import { useDrawer } from '/@/components/Drawer';
  import { useI18n } from '/@/hooks/web/useI18n';
  import { useMessage } from '/@/hooks/web/useMessage';
  import { useBaseStore } from '/@/store/modules/base';
  import { Modal } from 'ant-design-vue';
  import Detail from './Detail.vue';
  import dayjs from 'dayjs';

  defineOptions({ name: 'ExtendOperationLog' });

  const { t } = useI18n();
  const { createMessage } = useMessage();
  const baseStore = useBaseStore();
  const [registerDetail, { openDrawer: openDetailDrawer }] = useDrawer();

  const columns: BasicColumn[] = [
    { title: '操作人', dataIndex: 'userName', width: 120, ellipsis: true },
    { title: '操作类型', dataIndex: 'operationType', width: 150, ellipsis: true },
    { title: '操作时间', dataIndex: 'creatorTime', width: 180, format: 'date|YYYY-MM-DD HH:mm:ss' },
    { title: 'IP地址', dataIndex: 'ipAddress', width: 160, ellipsis: true },
    { title: '操作结果', dataIndex: 'operationResult', width: 100, align: 'center' },
  ];

  const [registerTable, { reload, getSelectRows, getForm }] = useTable({
    api: getOperationLogList,
    columns,
    useSearchForm: true,
    formConfig: {
      schemas: [
        {
          field: 'keyword',
          label: t('common.keyword'),
          component: 'Input',
          componentProps: {
            placeholder: t('common.enterKeyword'),
            submitOnPressEnter: true,
          },
        },
        {
          field: 'operationType',
          label: '操作类型',
          component: 'Select',
          componentProps: {
            placeholder: '请选择',
          },
        },
        {
          field: 'pickerVal',
          label: '操作时间',
          component: 'DateRange',
          componentProps: {
            format: 'YYYY-MM-DD HH:mm:ss',
            showTime: { defaultValue: [dayjs('00:00:00', 'HH:mm:ss'), dayjs('23:59:59', 'HH:mm:ss')] },
            placeholder: ['开始时间', '结束时间'],
          },
        },
      ],
    },
    actionColumn: {
      width: 120,
      title: '操作',
      dataIndex: 'action',
    },
    rowSelection: {},
    pagination: { pageSize: 20 },
  });

  function getTableActions(record): ActionItem[] {
    return [
      {
        label: t('common.detailText'),
        onClick: handleDetail.bind(null, record),
      },
      {
        label: t('common.delText'),
        color: 'error',
        popConfirm: {
          title: '确认删除该记录？',
          confirm: handleDelete.bind(null, record.id),
        },
      },
    ];
  }

  function handleDetail(record) {
    openDetailDrawer(true, { id: record.id });
  }

  function handleDelete(id) {
    delOperationLog({ ids: [id] }).then(res => {
      createMessage.success(res.msg);
      reload();
    });
  }

  function handleBatchDelete() {
    const rows = getSelectRows();
    if (!rows.length) return createMessage.warning('请先选择记录');
    Modal.confirm({
      title: '确认删除',
      content: `确认删除选中的 ${rows.length} 条记录？`,
      onOk: async () => {
        await delOperationLog({ ids: rows.map(r => r.id) });
        createMessage.success('删除成功');
        reload();
      },
    });
  }

  async function loadDict() {
    const operationTypeList = (await baseStore.getDictionaryData('operationType')) as any[];
    getForm().updateSchema({ field: 'operationType', componentProps: { options: operationTypeList } });
  }

  onMounted(() => {
    loadDict();
    reload();
  });
</script>
