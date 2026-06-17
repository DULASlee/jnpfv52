<template>
  <div class="model-routing">
    <h2>模型路由策略</h2>
    <p class="hint">按流水线阶段配置 LLM 供应商、模型、熔断阈值。优先级1=主选，失败超阈值后自动降级到备用。</p>
    <a-spin :spinning="loading">
      <div v-for="s in stages" :key="s.stage" class="stage-group">
        <h3>阶段 {{ s.stage }}：{{ s.stageName }}</h3>
        <a-table :columns="columns" :data-source="s.providers" :pagination="false" row-key="id" size="small">
          <template #bodyCell="{ column, record }">
            <template v-if="column.key === 'priority'">
              <a-tag :color="record.priority === 1 ? 'blue' : 'default'">
                {{ record.priority === 1 ? '主选' : '备用' }}
              </a-tag>
            </template>
            <template v-if="column.key === 'enabled'">
              <a-switch :checked="record.enabled" size="small" @change="(v: boolean) => toggleEnabled(record, v)" />
            </template>
            <template v-if="column.key === 'actions'">
              <a-button size="small" type="link" @click="editRecord(record)">编辑</a-button>
            </template>
          </template>
        </a-table>
      </div>
    </a-spin>

    <a-modal v-model:visible="showEdit" title="编辑路由策略" @ok="handleSave">
      <a-form layout="vertical">
        <a-form-item label="供应商">
          <a-select v-model:value="editForm.provider">
            <a-select-option value="deepseek">DeepSeek</a-select-option>
            <a-select-option value="tongyi">通义千问</a-select-option>
            <a-select-option value="openai">OpenAI</a-select-option>
            <a-select-option value="ollama">Ollama</a-select-option>
          </a-select>
        </a-form-item>
        <a-form-item label="模型">
          <a-input v-model:value="editForm.model" />
        </a-form-item>
        <a-row :gutter="16">
          <a-col :span="8"
            ><a-form-item label="优先级"><a-input-number v-model:value="editForm.priority" :min="1" :max="5" /></a-form-item
          ></a-col>
          <a-col :span="8"
            ><a-form-item label="重试次数"><a-input-number v-model:value="editForm.maxRetries" :min="1" :max="10" /></a-form-item
          ></a-col>
          <a-col :span="8"
            ><a-form-item label="超时(ms)"><a-input-number v-model:value="editForm.timeoutMs" :min="10000" :step="10000" /></a-form-item
          ></a-col>
        </a-row>
        <a-form-item label="熔断阈值(连续失败次数)">
          <a-input-number v-model:value="editForm.circuitBreakerThreshold" :min="1" :max="10" />
        </a-form-item>
      </a-form>
    </a-modal>
  </div>
</template>

<script setup lang="ts">
  import { ref, reactive, onMounted } from 'vue';
  import { defHttp } from '/@/utils/http/axios';

  const loading = ref(false);
  const stages = ref<any[]>([]);
  const showEdit = ref(false);
  const editForm = reactive({ id: 0, provider: '', model: '', priority: 1, maxRetries: 3, timeoutMs: 60000, circuitBreakerThreshold: 3 });

  const columns = [
    { title: '供应商', dataIndex: 'provider' },
    { title: '模型', dataIndex: 'model' },
    { title: '优先级', key: 'priority' },
    { title: '重试', dataIndex: 'maxRetries' },
    { title: '超时(ms)', dataIndex: 'timeoutMs' },
    { title: '熔断阈值', dataIndex: 'circuitBreakerThreshold' },
    { title: '启用', key: 'enabled', width: 60 },
    { title: '操作', key: 'actions', width: 60 },
  ];

  async function loadData() {
    loading.value = true;
    try {
      const res: any = await defHttp.get({ url: '/api/studio/pipeline/model-routing' });
      stages.value = res?.data?.stages || [];
    } catch {
      stages.value = [];
    }
    loading.value = false;
  }

  function editRecord(r: any) {
    Object.assign(editForm, {
      id: r.id,
      provider: r.provider,
      model: r.model,
      priority: r.priority,
      maxRetries: r.maxRetries,
      timeoutMs: r.timeoutMs,
      circuitBreakerThreshold: r.circuitBreakerThreshold,
    });
    showEdit.value = true;
  }

  async function handleSave() {
    try {
      await defHttp.put({ url: `/api/studio/pipeline/model-routing/${editForm.id}/update`, data: editForm });
      showEdit.value = false;
      loadData();
    } catch {
      /* ignore */
    }
  }

  async function toggleEnabled(r: any, v: boolean) {
    await defHttp.put({ url: `/api/studio/pipeline/model-routing/${r.id}/update`, data: { enabled: v } });
  }

  onMounted(loadData);
</script>

<style scoped lang="less">
  .model-routing {
    max-width: 1100px;
    margin: 0 auto;
    padding: 24px;
  }
  h2 {
    margin: 0;
  }
  .hint {
    font-size: 12px;
    color: #888;
    margin: 8px 0 20px;
  }
  .stage-group {
    margin-bottom: 24px;
    h3 {
      font-size: 15px;
      margin-bottom: 8px;
    }
  }
</style>
