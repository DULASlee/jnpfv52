<template>
  <div class="industry-knowledge">
    <h2>行业知识设置</h2>
    <a-spin :spinning="loading">
      <a-form layout="vertical" :model="form">
        <a-form-item label="行业类型">
          <a-select v-model:value="form.industry" placeholder="选择行业">
            <a-select-option value="finance">金融</a-select-option>
            <a-select-option value="healthcare">医疗</a-select-option>
            <a-select-option value="education">教育</a-select-option>
            <a-select-option value="ecommerce">电商</a-select-option>
            <a-select-option value="manufacturing">制造业</a-select-option>
            <a-select-option value="technology">科技</a-select-option>
            <a-select-option value="other">其他</a-select-option>
          </a-select>
        </a-form-item>
        <a-form-item label="行业知识描述">
          <a-textarea v-model:value="form.description" :rows="6" placeholder="描述本行业的核心业务流程、术语、规范等" />
        </a-form-item>
        <a-form-item label="业务规则">
          <a-textarea v-model:value="form.businessRules" :rows="4" placeholder="本行业的特殊业务规则和约束" />
        </a-form-item>
        <a-form-item label="合规要求">
          <a-textarea v-model:value="form.complianceRequirements" :rows="4" placeholder="本行业的合规要求和标准" />
        </a-form-item>
        <a-form-item>
          <a-button type="primary" @click="handleSave" :loading="saving">保存设置</a-button>
        </a-form-item>
      </a-form>
    </a-spin>
  </div>
</template>

<script setup lang="ts">
  import { ref, reactive, onMounted } from 'vue';
  import { message } from 'ant-design-vue';
  import { defHttp } from '/@/utils/http/axios';

  const loading = ref(false);
  const saving = ref(false);

  const form = reactive({
    industry: '',
    description: '',
    businessRules: '',
    complianceRequirements: '',
  });

  async function loadConfig() {
    loading.value = true;
    try {
      const res: any = await defHttp.get({ url: '/api/studio/tenant/industry' });
      if (res?.data) {
        Object.assign(form, res.data);
      }
    } catch (e: any) {
      console.error(e);
    }
    loading.value = false;
  }

  async function handleSave() {
    saving.value = true;
    try {
      await defHttp.put({ url: '/api/studio/tenant/industry/update', data: form });
      message.success('保存成功');
    } catch (e: any) {
      message.error('保存失败: ' + (e.message || '未知错误'));
    }
    saving.value = false;
  }

  onMounted(loadConfig);
</script>

<style scoped lang="less">
  .industry-knowledge {
    max-width: 800px;
    margin: 0 auto;
    padding: 24px;
  }
  h2 {
    margin: 0 0 24px;
  }
</style>
