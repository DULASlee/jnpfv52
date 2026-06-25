<template>
  <div class="skills-management">
    <h2>Skills 管理</h2>
    <a-spin :spinning="loading">
      <!-- Toolbar -->
      <div class="toolbar">
        <a-select v-model:value="agentId" style="width: 200px" placeholder="选择智能体" @change="loadList">
          <a-select-option v-for="agent in agents" :key="agent.f_Id" :value="agent.f_Id">
            {{ agent.f_Name }}
          </a-select-option>
        </a-select>
        <a-input-search v-model:value="keyword" placeholder="搜索名称" style="width: 240px" @search="loadList" />
        <a-button type="primary" :disabled="!agentId" @click="showCreate = true">+ 新建 Skill</a-button>
      </div>

      <!-- Skills list -->
      <a-table :columns="columns" :data-source="list" :pagination="{ pageSize: 10 }" row-key="f_Id" size="small">
        <template #bodyCell="{ column, record }">
          <template v-if="column.key === 'enabled'">
            <a-tag :color="record.f_Enabled ? 'green' : 'default'">{{ record.f_Enabled ? '启用' : '禁用' }}</a-tag>
          </template>
          <template v-if="column.key === 'actions'">
            <a-button size="small" type="link" @click="editItem(record)">编辑</a-button>
            <a-popconfirm title="确认删除?" @confirm="deleteItem(record.f_Id)">
              <a-button size="small" type="link" danger>删除</a-button>
            </a-popconfirm>
          </template>
        </template>
      </a-table>
    </a-spin>

    <!-- Create/Edit Modal -->
    <a-modal v-model:visible="showCreate" :title="editingId ? '编辑 Skill' : '新建 Skill'" width="600px" @ok="handleSave">
      <a-form layout="vertical">
        <a-form-item label="编码" required>
          <a-input v-model:value="form.skillCode" :disabled="!!editingId" />
        </a-form-item>
        <a-form-item label="名称" required>
          <a-input v-model:value="form.name" />
        </a-form-item>
        <a-form-item label="描述">
          <a-textarea v-model:value="form.description" :rows="3" />
        </a-form-item>
        <a-form-item label="触发条件">
          <a-input v-model:value="form.triggerCondition" placeholder="例如: when user asks about code" />
        </a-form-item>
      </a-form>
    </a-modal>
  </div>
</template>

<script setup lang="ts">
  import { ref, reactive, onMounted } from 'vue';
  import { defHttp } from '/@/utils/http/axios';

  const loading = ref(false);
  const list = ref<any[]>([]);
  const keyword = ref('');
  const agentId = ref<string | null>(null);
  const agents = ref<any[]>([]);
  const showCreate = ref(false);
  const editingId = ref<number | null>(null);

  const form = reactive({
    skillCode: '',
    name: '',
    description: '',
    triggerCondition: '',
  });

  const columns = [
    { title: '编码', dataIndex: 'f_SkillCode' },
    { title: '名称', dataIndex: 'f_Name' },
    { title: '描述', dataIndex: 'f_Description' },
    { title: '状态', key: 'enabled', width: 80 },
    { title: '操作', key: 'actions', width: 120 },
  ];

  async function loadAgents() {
    try {
      const res: any = await defHttp.get({ url: '/api/studio/agent/list' });
      agents.value = res?.data?.items || res?.data || [];
    } catch {
      agents.value = [];
    }
  }

  async function loadList() {
    if (!agentId.value) return;
    loading.value = true;
    try {
      const res: any = await defHttp.get({
        url: `/api/studio/agent/${agentId.value}/skills`,
        data: { keyword: keyword.value },
      });
      list.value = res?.data?.items || res?.data || [];
    } catch {
      list.value = [];
    }
    loading.value = false;
  }

  function editItem(record: any) {
    editingId.value = record.f_Id;
    form.skillCode = record.f_SkillCode;
    form.name = record.f_Name;
    form.description = record.f_Description || '';
    form.triggerCondition = record.f_TriggerCondition || '';
    showCreate.value = true;
  }

  async function deleteItem(id: number) {
    try {
      await defHttp.delete({ url: `/api/studio/agent/skill/${id}/delete` });
      loadList();
    } catch (e: any) {
      console.error(e);
    }
  }

  async function handleSave() {
    try {
      if (editingId.value) {
        await defHttp.put({ url: `/api/studio/agent/skill/${editingId.value}/update`, data: form });
      } else {
        await defHttp.post({ url: '/api/studio/agent/skill/create', data: { ...form, agentId: agentId.value } });
      }
      showCreate.value = false;
      editingId.value = null;
      Object.assign(form, { skillCode: '', name: '', description: '', triggerCondition: '' });
      loadList();
    } catch (e: any) {
      console.error(e);
    }
  }

  onMounted(loadAgents);
</script>

<style scoped lang="less">
  .skills-management {
    max-width: 1100px;
    margin: 0 auto;
    padding: 24px;
  }
  h2 {
    margin: 0 0 16px;
  }
  .toolbar {
    display: flex;
    gap: 8px;
    margin-bottom: 16px;
    align-items: center;
  }
</style>
