<template>
  <div class="ui-templates"
    ><h2>UI 模板库</h2>
    <a-tabs v-model:active-key="tab">
      <a-tab-pane key="market" tab="模板市场">
        <a-spin :spinning="loading"
          ><a-row :gutter="16"
            ><a-col v-for="t in market" :key="t.f_Id" :span="8" style="margin-bottom: 16px"
              ><a-card :title="t.f_Name" size="small"
                ><p>{{ t.f_Description || '暂无描述' }}</p
                ><p>分类: {{ t.f_Category }} | 评分: {{ t.f_Rating }} | 使用: {{ t.f_UseCount }}</p></a-card
              ></a-col
            ></a-row
          ></a-spin
        >
      </a-tab-pane>
      <a-tab-pane key="workshop" tab="开发者工坊">
        <a-button type="primary" @click="showCreate = true" style="margin-bottom: 12px">+ 上传模板</a-button>
        <a-spin :spinning="loading"
          ><a-row :gutter="16"
            ><a-col v-for="t in workshop" :key="t.f_Id" :span="8" style="margin-bottom: 16px"
              ><a-card :title="t.f_Name" size="small"
                ><p>{{ t.f_Description || '暂无描述' }}</p></a-card
              ></a-col
            ></a-row
          ></a-spin
        >
      </a-tab-pane>
    </a-tabs>
    <a-modal v-model:visible="showCreate" title="上传模板" @ok="handleCreate"
      ><a-form
        ><a-form-item label="名称"><a-input v-model:value="form.name" /></a-form-item
        ><a-form-item label="分类"
          ><a-select v-model:value="form.category"
            ><a-select-option value="form">表单</a-select-option><a-select-option value="list">列表</a-select-option
            ><a-select-option value="dashboard">大屏</a-select-option></a-select
          ></a-form-item
        ><a-form-item label="IR JSON"><a-textarea v-model:value="form.templateData" :rows="6" /></a-form-item></a-form
    ></a-modal>
  </div>
</template>
<script setup lang="ts">
  import { ref, onMounted } from 'vue';
  import { defHttp } from '/@/utils/http/axios';
  const tab = ref('market');
  const loading = ref(false);
  const market = ref<any[]>([]);
  const workshop = ref<any[]>([]);
  const showCreate = ref(false);
  const form = ref({ name: '', category: 'form', templateData: '', description: '', thumbnailUrl: '' });
  async function load() {
    loading.value = true;
    try {
      const r1: any = await defHttp.get({ url: '/api/studio/ui-template/market' });
      market.value = r1?.data?.items || [];
    } catch {}
    try {
      const r2: any = await defHttp.get({ url: '/api/studio/ui-template/workshop' });
      workshop.value = r2?.data?.items || [];
    } catch {}
    loading.value = false;
  }
  async function handleCreate() {
    try {
      await defHttp.post({ url: '/api/studio/ui-template/create', data: form.value });
      showCreate.value = false;
      load();
    } catch {}
  }
  onMounted(load);
</script>
<style scoped lang="less">
  .ui-templates {
    max-width: 1100px;
    margin: 0 auto;
    padding: 24px;
  }
  h2 {
    margin: 0 0 16px;
  }
</style>
