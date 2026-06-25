<template
  ><div class="page"
    ><h2>评测基准管理</h2
    ><a-spin :spinning="loading"
      ><div class="toolbar"><a-button type="primary" @click="showCreateSet = true">+ 创建 Golden Set</a-button></div
      ><a-table :columns="cols" :data-source="sets" row-key="f_Id" size="small" :pagination="{ pageSize: 10 }"
        ><template #bodyCell="{ column, record }"
          ><template v-if="column.key === 'actions'"
            ><a-button size="small" type="link" @click="runEval(record.f_Id)">运行评测</a-button
            ><a-button size="small" type="link" @click="viewHistory(record.f_Id)">历史</a-button></template
          ></template
        ></a-table
      ></a-spin
    >
    <a-modal v-model:visible="showCreateSet" title="创建 Golden Set" @ok="handleCreateSet"
      ><a-form
        ><a-form-item label="名称"><a-input v-model:value="setForm.name" /></a-form-item
        ><a-form-item label="领域"><a-input v-model:value="setForm.domain" /></a-form-item
        ><a-form-item label="描述"><a-textarea v-model:value="setForm.description" :rows="3" /></a-form-item></a-form></a-modal></div
></template>
<script setup lang="ts">
  import { ref, reactive, onMounted } from 'vue';
  import { defHttp } from '/@/utils/http/axios';
  const loading = ref(false);
  const sets = ref<any[]>([]);
  const showCreateSet = ref(false);
  const setForm = reactive({ name: '', domain: '', description: '' });
  const cols = [
    { title: '名称', dataIndex: 'f_Name' },
    { title: '领域', dataIndex: 'f_Domain' },
    { title: '用例数', dataIndex: 'f_TestCaseCount' },
    { title: '操作', key: 'actions' },
  ];
  async function load() {
    loading.value = true;
    try {
      const r: any = await defHttp.get({ url: '/api/studio/eval/golden-set' });
      sets.value = r?.data?.items || [];
    } catch {}
    loading.value = false;
  }
  async function handleCreateSet() {
    try {
      await defHttp.post({ url: '/api/studio/eval/golden-set/create', data: setForm });
      showCreateSet.value = false;
      load();
    } catch {}
  }
  async function runEval(id: number) {
    try {
      const r: any = await defHttp.post({ url: '/api/studio/eval/run', data: { setId: id } });
      alert(`评测已提交: ${r?.data?.totalCases || 0} 个用例`);
    } catch {}
  }
  function viewHistory(_id: number) {
    /* TODO */
  }
  onMounted(load);
</script>
<style scoped lang="less">
  .page {
    max-width: 1100px;
    margin: 0 auto;
    padding: 24px;
  }
  h2 {
    margin: 0 0 16px;
  }
  .toolbar {
    margin-bottom: 16px;
  }
</style>
