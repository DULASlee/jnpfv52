<template
  ><div class="page"
    ><h2>领域知识管理</h2
    ><a-spin :spinning="loading"
      ><div class="stats"
        ><a-card size="small" title="节点总数">{{ stats.totalNodes || 0 }}</a-card
        ><a-card size="small" title="边总数">{{ stats.totalEdges || 0 }}</a-card></div
      ><a-table :columns="cols" :data-source="items" row-key="f_Id" size="small" :pagination="{ pageSize: 20 }" /></a-spin></div
></template>
<script setup lang="ts">
  import { ref, onMounted } from 'vue';
  import { defHttp } from '/@/utils/http/axios';
  const loading = ref(false);
  const items = ref<any[]>([]);
  const stats = ref<any>({});
  const cols = [
    { title: 'ID', dataIndex: 'f_Id' },
    { title: 'Label', dataIndex: 'f_Label' },
    { title: 'Name', dataIndex: 'f_Name' },
    { title: 'Type', dataIndex: 'f_NodeType' },
  ];
  async function load() {
    loading.value = true;
    try {
      const r: any = await defHttp.get({ url: '/api/studio/knowledge/domain' });
      items.value = r?.data?.items || [];
    } catch {}
    try {
      const s: any = await defHttp.get({ url: '/api/studio/knowledge/domain/stats' });
      stats.value = s?.data || {};
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
  .stats {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 16px;
    margin-bottom: 20px;
  }
</style>
