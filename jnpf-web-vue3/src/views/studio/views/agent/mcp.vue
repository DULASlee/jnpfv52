<template>
  <div class="mcp-config">
    <h2>MCP 配置</h2>
    <a-spin :spinning="loading">
      <!-- Toolbar -->
      <div class="toolbar">
        <a-input-search v-model:value="keyword" placeholder="搜索名称" style="width: 240px" @search="loadList" />
        <a-button type="primary" @click="showCreate = true">+ 新建 MCP</a-button>
      </div>

      <!-- MCP list -->
      <a-table :columns="columns" :data-source="list" :pagination="{ pageSize: 10 }" row-key="f_Id" size="small">
        <template #bodyCell="{ column, record }">
          <template v-if="column.key === 'status'">
            <a-tag :color="record.f_Status === 'connected' ? 'green' : record.f_Status === 'error' ? 'red' : 'default'">
              {{ record.f_Status === 'connected' ? '已连接' : record.f_Status === 'error' ? '错误' : '未测试' }}
            </a-tag>
          </template>
          <template v-if="column.key === 'actions'">
            <a-button size="small" type="link" @click="testConnection(record)">测试连接</a-button>
            <a-button size="small" type="link" @click="editItem(record)">编辑</a-button>
            <a-popconfirm title="确认删除?" @confirm="deleteItem(record.f_Id)">
              <a-button size="small" type="link" danger>删除</a-button>
            </a-popconfirm>
          </template>
        </template>
      </a-table>
    </a-spin>

    <!-- Create/Edit Modal -->
    <a-modal v-model:visible="showCreate" :title="editingId ? '编辑 MCP' : '新建 MCP'" width="600px" @ok="handleSave">
      <a-form layout="vertical">
        <a-form-item label="名称" required>
          <a-input v-model:value="form.name" />
        </a-form-item>
        <a-form-item label="服务地址" required>
          <a-input v-model:value="form.serverUrl" placeholder="https://api.example.com" />
        </a-form-item>
        <a-form-item label="API Key">
          <a-input-password v-model:value="form.apiKey" />
        </a-form-item>
        <a-form-item label="描述">
          <a-textarea v-model:value="form.description" :rows="3" />
        </a-form-item>
      </a-form>
    </a-modal>
  </div>
</template>

<script setup lang="ts">
  import { ref, reactive, onMounted } from 'vue';
  import { message } from 'ant-design-vue';
  import { defHttp } from '/@/utils/http/axios';

  const loading = ref(false);
  const list = ref<any[]>([]);
  const keyword = ref('');
  const showCreate = ref(false);
  const editingId = ref<number | null>(null);

  const form = reactive({
    name: '',
    serverUrl: '',
    apiKey: '',
    description: '',
  });

  const columns = [
    { title: '名称', dataIndex: 'f_Name' },
    { title: '服务地址', dataIndex: 'f_ServerUrl' },
    { title: '状态', key: 'status', width: 100 },
    { title: '操作', key: 'actions', width: 200 },
  ];

  async function loadList() {
    loading.value = true;
    try {
      const res: any = await defHttp.get({
        url: '/api/studio/agent/mcp/list',
        data: { keyword: keyword.value },
      });
      list.value = res?.data?.items || res?.data || [];
    } catch {
      list.value = [];
    }
    loading.value = false;
  }

  async function testConnection(record: any) {
    try {
      await defHttp.post({ url: `/api/studio/agent/mcp/${record.f_Id}/test` });
      message.success('连接测试成功');
      loadList();
    } catch (e: any) {
      message.error('连接测试失败: ' + (e.message || '未知错误'));
    }
  }

  function editItem(record: any) {
    editingId.value = record.f_Id;
    form.name = record.f_Name;
    form.serverUrl = record.f_ServerUrl;
    form.apiKey = record.f_ApiKey || '';
    form.description = record.f_Description || '';
    showCreate.value = true;
  }

  async function deleteItem(id: number) {
    try {
      await defHttp.delete({ url: `/api/studio/agent/mcp/${id}/delete` });
      loadList();
    } catch (e: any) {
      console.error(e);
    }
  }

  async function handleSave() {
    try {
      if (editingId.value) {
        await defHttp.put({ url: `/api/studio/agent/mcp/${editingId.value}/update`, data: form });
      } else {
        await defHttp.post({ url: '/api/studio/agent/mcp/create', data: form });
      }
      showCreate.value = false;
      editingId.value = null;
      Object.assign(form, { name: '', serverUrl: '', apiKey: '', description: '' });
      loadList();
    } catch (e: any) {
      console.error(e);
    }
  }

  onMounted(loadList);
</script>

<style scoped lang="less">
  .mcp-config {
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
