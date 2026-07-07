<template>
  <div class="jnpf-content-wrapper">
    <div class="jnpf-content-wrapper-center">
      <div class="jnpf-content-wrapper-content">
        <BasicTable @register="registerTable">
          <template #tableTitle>
            <a-button type="primary" preIcon="icon-ym icon-ym-btn-add" @click="addOrUpdateHandle()">{{ t('common.addText') }}</a-button>
            <jnpf-upload-btn :url="reportServer + '/Data/Actions/Import'" accept=".json" @on-success="reload" />
          </template>
          <template #bodyCell="{ column, record }">
            <template v-if="column.key === 'enabledMark'">
              <a-tag :color="record.enabledMark === 1 ? 'success' : 'error'">{{ record.enabledMark == 1 ? '启用' : '禁用' }}</a-tag>
            </template>
            <template v-if="column.key === 'action'">
              <TableAction :actions="getTableActions(record)" :dropDownActions="getDropDownActions(record)" />
            </template>
          </template>
        </BasicTable>
      </div>
    </div>
    <PreviewModal @register="registerPreview" type="flow" @preview-pc="previewPc" />
    <Form @register="registerForm" @reload="reload" />
    <PreviewPopup @register="registerPreviewPopup" />
  </div>
</template>
<script lang="ts" setup>
  import { onMounted, ref } from 'vue';
  import { getDataReportList, delDataReport, copy, release } from '/@/api/onlineDev/dataReport';
  import { BasicTable, useTable, TableAction, BasicColumn, ActionItem } from '/@/components/Table';
  import { useMessage } from '/@/hooks/web/useMessage';
  import { useI18n } from '/@/hooks/web/useI18n';
  import { useModal } from '/@/components/Modal';
  import { usePopup } from '/@/components/Popup';
  import { useBaseStore } from '/@/store/modules/base';
  import { downloadByUrl } from '/@/utils/file/download';
  import { useGlobSetting } from '/@/hooks/setting';
  import { getRawToken } from '/@/utils/auth';
  import { useLazyComponent } from '/@/hooks/web/useLazyComponent';

  // 弹窗/预览组件按需异步加载 — 减少首屏 bundle 体积
  const MODULE = 'onlineDev/dataReport';
  const { component: PreviewModal } = useLazyComponent(() => import('/@/components/CommonModal/src/PreviewModal.vue'), MODULE);
  const { component: Form } = useLazyComponent(() => import('./Form.vue'), MODULE);
  const { component: PreviewPopup } = useLazyComponent(() => import('./PreviewPopup.vue'), MODULE);

  defineOptions({ name: 'OnlineDevWebDesign' });

  const { createMessage } = useMessage();
  const baseStore = useBaseStore();
  const { t } = useI18n();
  const [registerPreview, { openModal: openPreviewModal }] = useModal();
  const [registerForm, { openModal: openFormModal }] = useModal();
  const [registerPreviewPopup, { openPopup: openPreviewPopup }] = usePopup();

  const columns: BasicColumn[] = [
    { title: '名称', dataIndex: 'fullName', width: 200 },
    { title: '编码', dataIndex: 'enCode', width: 200 },
    { title: '分类', dataIndex: 'category', width: 150 },
    { title: '创建人', dataIndex: 'creatorUser', width: 120 },
    { title: '创建时间', dataIndex: 'creatorTime', width: 150, format: 'date|YYYY-MM-DD HH:mm:ss' },
    { title: '最后修改时间', dataIndex: 'lastModifyTime', width: 150, format: 'date|YYYY-MM-DD HH:mm:ss' },
    { title: '排序', dataIndex: 'sortCode', width: 70, align: 'center' },
    { title: '状态', dataIndex: 'enabledMark', width: 70, align: 'center' },
  ];
  const { reportServer } = useGlobSetting();
  const currRow = ref<any>({});
  const categoryList = ref<any[]>([]);
  // 预构建 category id→name 映射，避免 afterFetch 中 O(n×m) filter
  const categoryMap = ref<Map<string, string>>(new Map());
  const [registerTable, { reload, getForm }] = useTable({
    api: getDataReportList,
    columns,
    useSearchForm: true,
    immediate: true,
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
          field: 'category',
          label: '分类',
          component: 'Select',
          componentProps: {
            placeholder: '请选择',
            showSearch: true,
          },
        },
        {
          field: 'enabledMark',
          label: '状态',
          component: 'Select',
          componentProps: {
            placeholder: '请选择',
            options: [
              { fullName: '启用', id: 1 },
              { fullName: '禁用', id: 0 },
            ],
          },
        },
      ],
    },
    actionColumn: {
      width: 150,
      title: '操作',
      dataIndex: 'action',
    },
    afterFetch: data => {
      const map = categoryMap.value;
      if (map.size === 0) return data;
      return data.map(o => ({
        ...o,
        category: map.get(o.categoryId) || o.category || '',
      }));
    },
  });
  function getTableActions(record): ActionItem[] {
    return [
      {
        label: t('common.editText'),
        onClick: addOrUpdateHandle.bind(null, record.id),
      },
      {
        label: t('common.delText'),
        color: 'error',
        modelConfirm: {
          onOk: handleDelete.bind(null, record.id),
        },
      },
    ];
  }
  function getDropDownActions(record): ActionItem[] {
    return [
      {
        label: '预览',
        onClick: handlePreview.bind(null, record),
      },
      {
        label: '复制',
        modelConfirm: {
          content: '您确定要复制该报表, 是否继续?',
          onOk: handleCopy.bind(null, record.id),
        },
      },
      {
        label: '导出',
        modelConfirm: {
          content: '您确定要导出该报表, 是否继续?',
          onOk: handleExport.bind(null, record.id),
        },
      },
      {
        ifShow: !record.enabledMark,
        label: '启用',
        modelConfirm: {
          content: '此操作将启用该报表，是否继续?',
          onOk: handleRelease.bind(null, record),
        },
      },
      {
        ifShow: !!record.enabledMark,
        label: '禁用',
        modelConfirm: {
          content: '此操作将禁用该报表，是否继续?',
          onOk: handleRelease.bind(null, record),
        },
      },
    ];
  }
  function addOrUpdateHandle(id = '') {
    openFormModal(true, { id });
  }
  function handlePreview(record) {
    currRow.value = record;
    openPreviewModal(true, { type: 'report', id: record.id, fullName: record.fullName });
  }
  function previewPc() {
    openPreviewPopup(true, { id: currRow.value.id });
  }
  function handleDelete(id) {
    delDataReport(id).then(res => {
      createMessage.success(res.msg);
      reload();
    });
  }
  function handleCopy(id) {
    copy(id).then(res => {
      createMessage.success(res.msg);
      reload();
    });
  }
  function handleExport(id) {
    const token = getRawToken();
    const url = `${reportServer}/Data/${id}/Actions/Export?token=${token}`;
    downloadByUrl({ url });
  }
  async function getOptions() {
    const res = (await baseStore.getDictionaryData('ReportSort')) as any[];
    categoryList.value = res;
    // 预构建 O(1) 查找映射
    const map = new Map<string, string>();
    for (const item of res) {
      if (item.id && item.fullName) map.set(item.id, item.fullName);
    }
    categoryMap.value = map;
    getForm().updateSchema({ field: 'category', componentProps: { options: res } });
    // immediate:true 已触发首次 fetch，此处无需 reload
  }
  function handleRelease(record) {
    release(record.id).then(res => {
      createMessage.success(res.msg);
      reload();
    });
  }

  onMounted(() => {
    getOptions();
  });
</script>
