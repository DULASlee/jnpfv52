<template>
  <div class="provider-page">
    <a-card :bordered="false" class="header-card">
      <div class="header-row">
        <div class="header-info">
          <h2>模型供应商配置</h2>
          <p class="desc">管理 LLM 供应商的 API 地址、密钥和默认模型。修改后立即生效，无需重启服务。</p>
        </div>
        <a-button type="primary" @click="showCreateModal">+ 添加供应商</a-button>
      </div>
    </a-card>

    <a-card :bordered="false" style="margin-top: 16px">
      <a-table :columns="columns" :data-source="providers" :loading="loading" row-key="id" :pagination="false" size="small">
        <template #bodyCell="{ column, record }">
          <template v-if="column.key === 'status'">
            <a-tag :color="statusColorMap[record.status]">{{ statusTextMap[record.status] }}</a-tag>
          </template>
          <template v-if="column.key === 'apiKeyMasked'">
            <a-tag color="default">{{ record.apiKeyMasked }}</a-tag>
          </template>
          <template v-if="column.key === 'maxTokens'">
            {{ formatMaxTokens(record.maxTokens) }}
          </template>
          <template v-if="column.key === 'priority'">
            <a-tag :color="record.priority === 1 ? 'blue' : 'default'">
              {{ record.priority === 1 ? '主选' : `备用 #${record.priority}` }}
            </a-tag>
          </template>
          <template v-if="column.key === 'enabled'">
            <a-switch :checked="record.enabled" checked-children="启用" un-checked-children="禁用" @change="handleToggle(record.id)" />
          </template>
          <template v-if="column.key === 'lastTest'">
            <div v-if="record.lastTestTime">
              <div style="font-size: 12px; color: #999">{{ formatTime(record.lastTestTime) }}</div>
              <div style="font-size: 12px; margin-top: 2px">{{ record.lastTestResult?.substring(0, 50) }}</div>
            </div>
            <span v-else style="color: #999">未测试</span>
          </template>
          <template v-if="column.key === 'action'">
            <a-space>
              <a-button size="small" type="primary" ghost @click="handleTest(record.id)" :loading="testingIds.includes(record.id)">测试连接</a-button>
              <a-button size="small" @click="showEditModal(record)">编辑</a-button>
              <a-popconfirm title="确定删除?" @confirm="handleDelete(record.id)">
                <a-button size="small" danger>删除</a-button>
              </a-popconfirm>
            </a-space>
          </template>
        </template>
      </a-table>
    </a-card>

    <a-modal v-model:open="modalVisible" :title="isEdit ? '编辑供应商' : '添加供应商'" :width="640" @ok="handleSubmit">
      <a-form :model="formData" :label-col="{ span: 6 }" :wrapper-col="{ span: 16 }">
        <a-form-item label="供应商编码" required>
          <a-input v-model:value="formData.providerCode" :disabled="isEdit" placeholder="如: deepseek" />
        </a-form-item>
        <a-form-item label="显示名称" required>
          <a-input v-model:value="formData.name" placeholder="如: DeepSeek" />
        </a-form-item>
        <a-form-item label="API 地址" required>
          <a-input v-model:value="formData.baseUrl" placeholder="如: https://api.deepseek.com" />
        </a-form-item>
        <a-form-item label="API Key" required>
          <a-input-password v-model:value="formData.apiKey" placeholder="编辑时留空表示不修改" />
        </a-form-item>
        <a-form-item label="默认模型" required>
          <a-input v-model:value="formData.defaultModel" placeholder="如: deepseek-v4-pro" />
        </a-form-item>
        <a-form-item label="上下文窗口">
          <a-select v-model:value="formData.maxTokens">
            <a-select-option :value="3000000">3M</a-select-option>
            <a-select-option :value="2500000">2.5M</a-select-option>
            <a-select-option :value="2000000">2M</a-select-option>
            <a-select-option :value="1000000">1M</a-select-option>
            <a-select-option :value="4096000">4096k</a-select-option>
          </a-select>
        </a-form-item>
        <a-form-item label="优先级">
          <a-input-number v-model:value="formData.priority" :min="1" :max="99" />
          <span style="margin-left: 8px; color: #999">1 = 主选，2+ = 备用</span>
        </a-form-item>
        <a-form-item label="描述">
          <a-textarea v-model:value="formData.description" :rows="2" />
        </a-form-item>
      </a-form>
    </a-modal>

    <a-modal v-model:open="testResultVisible" title="测试结果" :footer="null" :width="480">
      <a-result :status="testResult?.success ? 'success' : 'error'" :title="testResult?.success ? '连接成功' : '连接失败'" :sub-title="testResult?.message" />
    </a-modal>
  </div>
</template>

<script setup lang="ts">
  import { ref, onMounted } from 'vue';
  import { message } from 'ant-design-vue';
  import { defHttp } from '/@/utils/http/axios';

  const loading = ref(false);
  const providers = ref<any[]>([]);
  const modalVisible = ref(false);
  const isEdit = ref(false);
  const editId = ref<number>(0);
  const testingIds = ref<number[]>([]);
  const testResultVisible = ref(false);
  const testResult = ref<any>(null);

  const formData = ref({
    providerCode: '',
    name: '',
    baseUrl: '',
    apiKey: '',
    defaultModel: '',
    maxTokens: 1000000,
    temperature: 0.7,
    priority: 1,
    description: '',
  });

  const columns = [
    { title: '供应商', dataIndex: 'name', width: 120 },
    { title: '编码', dataIndex: 'providerCode', width: 100 },
    { title: 'API 地址', dataIndex: 'baseUrl', width: 200, ellipsis: true },
    { title: 'API Key', key: 'apiKeyMasked', width: 130 },
    { title: '默认模型', dataIndex: 'defaultModel', width: 140 },
    { title: '上下文', key: 'maxTokens', width: 100 },
    { title: '优先级', key: 'priority', width: 100 },
    { title: '状态', key: 'status', width: 80 },
    { title: '启用', key: 'enabled', width: 80 },
    { title: '最后测试', key: 'lastTest', width: 180 },
    { title: '操作', key: 'action', width: 220, fixed: 'right' },
  ];

  const statusColorMap: Record<string, string> = { healthy: 'green', degraded: 'orange', offline: 'red', testing: 'blue' };
  const statusTextMap: Record<string, string> = { healthy: '健康', degraded: '降级', offline: '离线', testing: '测试中' };

  async function loadProviders() {
    loading.value = true;
    try {
      const res: any = await defHttp.get({ url: '/api/studio/pipeline/providers' });
      providers.value = res?.items ?? res?.data?.items ?? [];
    } catch (e) {
      console.error(e);
    }
    loading.value = false;
  }

  function showCreateModal() {
    isEdit.value = false;
    editId.value = 0;
    formData.value = {
      providerCode: '',
      name: '',
      baseUrl: '',
      apiKey: '',
      defaultModel: '',
      maxTokens: 1000000,
      temperature: 0.7,
      priority: providers.value.length + 1,
      description: '',
    };
    modalVisible.value = true;
  }

  function showEditModal(record: any) {
    isEdit.value = true;
    editId.value = record.id;
    formData.value = {
      providerCode: record.providerCode,
      name: record.name,
      baseUrl: record.baseUrl,
      apiKey: '',
      defaultModel: record.defaultModel,
      maxTokens: record.maxTokens,
      temperature: record.temperature,
      priority: record.priority,
      description: record.description,
    };
    modalVisible.value = true;
  }

  async function handleSubmit() {
    try {
      if (isEdit.value) {
        await defHttp.put({ url: `/api/studio/pipeline/providers/${editId.value}`, data: formData.value });
      } else {
        await defHttp.post({ url: '/api/studio/pipeline/providers', data: formData.value });
      }
      message.success('操作成功');
      modalVisible.value = false;
      await loadProviders();
    } catch (e: any) {
      message.error(e?.message || '操作失败');
    }
  }

  async function handleDelete(id: number) {
    try {
      await defHttp.delete({ url: `/api/studio/pipeline/providers/${id}` });
      message.success('删除成功');
      await loadProviders();
    } catch (e: any) {
      message.error(e?.message || '删除失败');
    }
  }

  async function handleToggle(id: number) {
    try {
      await defHttp.put({ url: `/api/studio/pipeline/providers/${id}/toggle` });
      await loadProviders();
    } catch (e: any) {
      message.error(e?.message || '操作失败');
    }
  }

  async function handleTest(id: number) {
    testingIds.value.push(id);
    try {
      const res: any = await defHttp.post({ url: `/api/studio/pipeline/providers/${id}/test` });
      testResult.value = res;
      testResultVisible.value = true;
      await loadProviders();
    } catch (e: any) {
      message.error(e?.message || '测试失败');
    } finally {
      testingIds.value = testingIds.value.filter(x => x !== id);
    }
  }

  function formatMaxTokens(val: number): string {
    if (val >= 1000000) return `${(val / 1000000).toFixed(1)}M`;
    if (val >= 1000) return `${(val / 1000).toFixed(0)}k`;
    return String(val);
  }

  function formatTime(t: string): string {
    if (!t) return '';
    return new Date(t).toLocaleString('zh-CN');
  }

  onMounted(loadProviders);
</script>

<style scoped lang="less">
  .provider-page {
    padding: 16px;
  }
  .header-card {
    .header-row {
      display: flex;
      justify-content: space-between;
      align-items: center;
    }
    h2 {
      margin: 0 0 4px 0;
      font-size: 18px;
    }
    .desc {
      margin: 0;
      color: #999;
      font-size: 13px;
    }
  }
</style>
