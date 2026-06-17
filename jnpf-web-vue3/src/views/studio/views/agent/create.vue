<template>
  <div class="agent-config">
    <h2>智能体管理</h2>
    <a-spin :spinning="loading">
      <!-- Toolbar -->
      <div class="toolbar">
        <a-input-search v-model:value="keyword" placeholder="搜索名称/编码" style="width: 240px" @search="loadList" />
        <a-select v-model:value="typeFilter" style="width: 160px" placeholder="类型筛选" allow-clear @change="loadList">
          <a-select-option value="requirement-analyst">需求分析师</a-select-option>
          <a-select-option value="architect">架构师</a-select-option>
          <a-select-option value="ui-ux">UI/UX 设计师</a-select-option>
          <a-select-option value="database">数据库设计师</a-select-option>
          <a-select-option value="orchestrator">编排调度器</a-select-option>
          <a-select-option value="custom">自定义</a-select-option>
        </a-select>
        <a-button type="primary" @click="showCreate = true">+ 新建智能体</a-button>
      </div>

      <!-- Agent list -->
      <a-table :columns="columns" :data-source="list" :pagination="{ pageSize: 10 }" row-key="f_Id" size="small">
        <template #bodyCell="{ column, record }">
          <template v-if="column.key === 'type'">
            <a-tag :color="typeColor(record.f_AgentType)">{{ record.f_AgentType }}</a-tag>
          </template>
          <template v-if="column.key === 'enabled'">
            <a-tag :color="record.f_Enabled ? 'green' : 'default'">{{ record.f_Enabled ? '启用' : '禁用' }}</a-tag>
          </template>
          <template v-if="column.key === 'actions'">
            <a-button size="small" type="link" @click="viewDetail(record)">详情</a-button>
            <a-button size="small" type="link" @click="editAgent(record)">编辑</a-button>
          </template>
        </template>
      </a-table>
    </a-spin>

    <!-- Create/Edit Modal -->
    <a-modal v-model:visible="showCreate" :title="editingId ? '编辑智能体' : '新建智能体'" width="600px" @ok="handleSave">
      <a-form layout="vertical">
        <a-form-item label="编码" required>
          <a-input v-model:value="form.agentCode" :disabled="!!editingId" />
        </a-form-item>
        <a-form-item label="名称" required>
          <a-input v-model:value="form.name" />
        </a-form-item>
        <a-form-item label="类型" required>
          <a-select v-model:value="form.agentType">
            <a-select-option value="requirement-analyst">需求分析师</a-select-option>
            <a-select-option value="architect">架构师</a-select-option>
            <a-select-option value="ui-ux">UI/UX 设计师</a-select-option>
            <a-select-option value="database">数据库设计师</a-select-option>
            <a-select-option value="orchestrator">编排调度器</a-select-option>
            <a-select-option value="custom">自定义</a-select-option>
          </a-select>
        </a-form-item>
        <a-form-item label="System Prompt">
          <a-textarea v-model:value="form.systemPrompt" :rows="4" />
        </a-form-item>
        <a-form-item label="模型供应商">
          <a-select v-model:value="form.modelProvider">
            <a-select-option value="deepseek">DeepSeek</a-select-option>
            <a-select-option value="tongyi">通义千问</a-select-option>
            <a-select-option value="openai">OpenAI</a-select-option>
            <a-select-option value="ollama">Ollama</a-select-option>
          </a-select>
        </a-form-item>
        <a-row :gutter="16">
          <a-col :span="12">
            <a-form-item label="Temperature">
              <a-input-number v-model:value="form.temperature" :min="0" :max="2" :step="0.1" style="width: 100%" />
            </a-form-item>
          </a-col>
          <a-col :span="12">
            <a-form-item label="Max Tokens">
              <a-input-number v-model:value="form.maxTokens" :min="256" :max="32768" style="width: 100%" />
            </a-form-item>
          </a-col>
        </a-row>
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
  const typeFilter = ref('');
  const showCreate = ref(false);
  const editingId = ref<number | null>(null);

  const form = reactive({
    agentCode: '',
    name: '',
    agentType: 'custom',
    systemPrompt: '',
    modelProvider: 'deepseek',
    modelName: 'deepseek-chat',
    temperature: 0.7,
    maxTokens: 4096,
  });

  const columns = [
    { title: '名称', dataIndex: 'f_Name', key: 'name' },
    { title: '编码', dataIndex: 'f_AgentCode' },
    { title: '类型', key: 'type' },
    { title: '模型', dataIndex: 'f_ModelName' },
    { title: '状态', key: 'enabled' },
    { title: '操作', key: 'actions', width: 120 },
  ];

  function typeColor(t: string) {
    return { 'requirement-analyst': 'blue', architect: 'green', 'ui-ux': 'purple', database: 'orange', orchestrator: 'red', custom: 'default' }[t] || 'default';
  }

  async function loadList() {
    loading.value = true;
    try {
      const res: any = await defHttp.get({
        url: '/api/studio/agent/list',
        data: { keyword: keyword.value, agentType: typeFilter.value || undefined },
      });
      list.value = res?.data?.items || res?.data || [];
    } catch {
      list.value = [];
    }
    loading.value = false;
  }

  function editAgent(record: any) {
    editingId.value = record.f_Id;
    form.agentCode = record.f_AgentCode;
    form.name = record.f_Name;
    form.agentType = record.f_AgentType;
    form.systemPrompt = record.f_SystemPrompt || '';
    form.modelProvider = record.f_ModelProvider || 'deepseek';
    form.temperature = record.f_Temperature || 0.7;
    form.maxTokens = record.f_MaxTokens || 4096;
    showCreate.value = true;
  }

  function viewDetail(_record: any) {
    /* expand inline */
  }

  async function handleSave() {
    try {
      if (editingId.value) {
        await defHttp.put({ url: `/api/studio/agent/${editingId.value}/update`, data: form });
      } else {
        await defHttp.post({ url: '/api/studio/agent/create', data: { ...form, sort: 0, description: '' } });
      }
      showCreate.value = false;
      editingId.value = null;
      Object.assign(form, { agentCode: '', name: '', agentType: 'custom', systemPrompt: '', modelProvider: 'deepseek', temperature: 0.7, maxTokens: 4096 });
      loadList();
    } catch (e: any) {
      console.error(e);
    }
  }

  onMounted(loadList);
</script>

<style scoped lang="less">
  .agent-config {
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
