<template
  ><div class="page"
    ><h2>沙箱部署设置</h2
    ><a-spin :spinning="loading"
      ><a-row :gutter="16"
        ><a-col :span="12"
          ><a-card title="默认配置"
            ><a-form
              ><a-form-item label="CPU 核数"><a-input-number v-model:value="config.cpuCount" :min="1" :max="8" /></a-form-item
              ><a-form-item label="内存 (MB)"><a-input-number v-model:value="config.memoryMb" :min="256" :max="4096" :step="256" /></a-form-item
              ><a-form-item label="超时 (秒)"><a-input-number v-model:value="config.timeoutSeconds" :min="60" :max="3600" /></a-form-item
              ><a-form-item label="最大并发"><a-input-number v-model:value="config.maxConcurrency" :min="1" :max="20" /></a-form-item></a-form></a-card></a-col
        ><a-col :span="12"
          ><a-card title="当前状态"
            ><p>活跃实例: {{ current.activeInstances }}</p
            ><p>总实例: {{ current.totalInstances }}</p></a-card
          ></a-col
        ></a-row
      ></a-spin
    ></div
  ></template
>
<script setup lang="ts">
  import { ref, reactive, onMounted } from 'vue';
  import { defHttp } from '/@/utils/http/axios';
  const loading = ref(false);
  const config = reactive({ cpuCount: 1, memoryMb: 1024, timeoutSeconds: 300, maxConcurrency: 5 });
  const current = reactive({ activeInstances: 0, totalInstances: 0 });
  async function load() {
    loading.value = true;
    try {
      const r: any = await defHttp.get({ url: '/api/studio/knowledge/sandbox-config' });
      const d = r?.data;
      if (d?.defaults) {
        config.cpuCount = d.defaults.cpuCount;
        config.memoryMb = d.defaults.memoryMb;
        config.timeoutSeconds = d.defaults.timeoutSeconds;
        config.maxConcurrency = d.defaults.maxConcurrency;
      }
      if (d?.current) {
        current.activeInstances = d.current.activeInstances;
        current.totalInstances = d.current.totalInstances;
      }
    } catch {}
    loading.value = false;
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
