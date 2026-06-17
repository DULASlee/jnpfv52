<template
  ><div class="page"
    ><h2>流水线阶段设置</h2
    ><a-spin :spinning="loading"
      ><a-table :columns="cols" :data-source="items" row-key="f_Stage" size="small" :pagination="false"
        ><template #bodyCell="{ column, record }"
          ><template v-if="column.key === 'confirm'">{{ record.f_RequireConfirm ? '是' : '否' }}</template
          ><template v-if="column.key === 'rollback'">{{ record.f_AllowRollback ? '是' : '否' }}</template
          ><template v-if="column.key === 'actions'"><a-button size="small" type="link" @click="edit(record)">编辑</a-button></template></template
        ></a-table
      ></a-spin
    >
    <a-modal v-model:visible="showEdit" title="编辑阶段配置" @ok="handleSave"
      ><a-form
        ><a-form-item label="阶段名称"><a-input v-model:value="editForm.stageName" /></a-form-item
        ><a-form-item label="绑定Agent编码"><a-input v-model:value="editForm.agentCode" /></a-form-item
        ><a-form-item label="超时(秒)"><a-input-number v-model:value="editForm.timeoutSeconds" :min="60" :max="3600" /></a-form-item
        ><a-form-item label="需用户确认"><a-switch v-model:checked="editForm.requireConfirm" /></a-form-item
        ><a-form-item label="允许回退"><a-switch v-model:checked="editForm.allowRollback" /></a-form-item></a-form></a-modal></div
></template>
<script setup lang="ts">
  import { ref, reactive, onMounted } from 'vue';
  import { defHttp } from '/@/utils/http/axios';
  const loading = ref(false);
  const items = ref<any[]>([]);
  const showEdit = ref(false);
  const editForm = reactive({
    stage: 0,
    stageName: '',
    agentCode: '',
    timeoutSeconds: 300,
    requireConfirm: true,
    allowRollback: true,
    enabled: true,
    promptTemplateId: null as number | null,
  });
  const cols = [
    { title: '阶段', dataIndex: 'f_Stage' },
    { title: '名称', dataIndex: 'f_StageName' },
    { title: 'Agent', dataIndex: 'f_AgentCode' },
    { title: '超时', dataIndex: 'f_TimeoutSeconds' },
    { title: '确认', key: 'confirm' },
    { title: '回退', key: 'rollback' },
    { title: '操作', key: 'actions' },
  ];
  async function load() {
    loading.value = true;
    try {
      const r: any = await defHttp.get({ url: '/api/studio/pipeline/stages' });
      items.value = r?.data?.items || [];
    } catch {}
    loading.value = false;
  }
  function edit(r: any) {
    editForm.stage = r.f_Stage;
    editForm.stageName = r.f_StageName;
    editForm.agentCode = r.f_AgentCode || '';
    editForm.timeoutSeconds = r.f_TimeoutSeconds;
    editForm.requireConfirm = r.f_RequireConfirm;
    editForm.allowRollback = r.f_AllowRollback;
    showEdit.value = true;
  }
  async function handleSave() {
    try {
      await defHttp.put({ url: `/api/studio/pipeline/stage/${editForm.stage}/update`, data: editForm });
      showEdit.value = false;
      load();
    } catch {}
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
</style>
