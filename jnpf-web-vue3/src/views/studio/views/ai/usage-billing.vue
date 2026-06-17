<template>
  <div class="usage-billing">
    <h2>用量与计费</h2>
    <div class="data-scope-hint">{{ scopeHint }}</div>

    <!-- Summary cards -->
    <a-spin :spinning="loading">
      <div class="summary-cards">
        <div class="stat-card">
          <span class="label">总 Token 数</span>
          <span class="value">{{ summary.totalTokens?.toLocaleString() || '--' }}</span>
        </div>
        <div class="stat-card">
          <span class="label">总费用 (估算)</span>
          <span class="value">¥{{ summary.totalCost?.toFixed(4) || '--' }}</span>
        </div>
        <div class="stat-card">
          <span class="label">总调用次数</span>
          <span class="value">{{ summary.totalCalls?.toLocaleString() || '--' }}</span>
        </div>
        <div class="stat-card">
          <span class="label">平均延迟</span>
          <span class="value">{{ summary.avgLatency ? `${summary.avgLatency}ms` : '--' }}</span>
        </div>
      </div>

      <!-- Provider breakdown -->
      <div v-if="providerStats.length" class="section">
        <h3>按供应商分组</h3>
        <div class="provider-bars">
          <div v-for="p in providerStats" :key="p.provider" class="bar-row">
            <span class="provider-name">{{ p.provider }}</span>
            <div class="bar-track">
              <div class="bar-fill" :style="{ width: p.percent + '%' }"></div>
            </div>
            <span class="bar-value">{{ p.tokens?.toLocaleString() }} tokens</span>
          </div>
        </div>
      </div>

      <!-- Call log table -->
      <div class="section">
        <h3>调用明细</h3>
        <a-table :columns="columns" :data-source="callLogs" :pagination="{ pageSize: 20 }" size="small" row-key="id" />
      </div>
    </a-spin>
  </div>
</template>

<script setup lang="ts">
  import { ref, computed, onMounted } from 'vue';
  import { defHttp } from '/@/utils/http/axios';

  const loading = ref(false);
  const summary = ref<any>({});
  const providerStats = ref<any[]>([]);
  const callLogs = ref<any[]>([]);

  const scopeHint = computed(() => '当前查看范围: 个人用量');

  const columns = [
    { title: '时间', dataIndex: 'createTime', width: 160 },
    { title: '供应商', dataIndex: 'provider', width: 100 },
    { title: '模型', dataIndex: 'model', width: 140 },
    { title: '阶段', dataIndex: 'stage', width: 100 },
    { title: 'Prompt Tokens', dataIndex: 'promptTokens', width: 120 },
    { title: 'Completion Tokens', dataIndex: 'completionTokens', width: 130 },
    { title: '延迟(ms)', dataIndex: 'latency', width: 90 },
    { title: '状态', dataIndex: 'status', width: 80 },
    { title: '费用', dataIndex: 'estimatedCost', width: 100 },
  ];

  async function loadData() {
    loading.value = true;
    try {
      const res: any = await defHttp.get({
        url: '/api/studio/ai/usage/summary',
      });
      summary.value = res?.data || {};
      providerStats.value = (res?.data?.providers || []).map((p: any) => ({
        ...p,
        percent: res?.data?.totalTokens ? ((p.tokens / res.data.totalTokens) * 100).toFixed(0) : 0,
      }));
    } catch {
      // Mock data for demo
      summary.value = { totalTokens: 125000, totalCost: 0.015, totalCalls: 42, avgLatency: 320 };
      providerStats.value = [
        { provider: 'DeepSeek', tokens: 80000, percent: 64 },
        { provider: '通义千问', tokens: 30000, percent: 24 },
        { provider: 'OpenAI', tokens: 15000, percent: 12 },
      ];
    }

    try {
      const res2: any = await defHttp.get({
        url: '/api/studio/ai/usage/call-log',
        data: { page: 1, pageSize: 20 },
      });
      callLogs.value = res2?.data?.items || res2?.data || [];
    } catch {
      callLogs.value = [];
    }
    loading.value = false;
  }

  onMounted(loadData);
</script>

<style scoped lang="less">
  .usage-billing {
    max-width: 1100px;
    margin: 0 auto;
    padding: 24px;

    h2 {
      margin: 0 0 8px;
    }
    .data-scope-hint {
      font-size: 12px;
      color: #888;
      margin-bottom: 20px;
    }

    .summary-cards {
      display: grid;
      grid-template-columns: repeat(4, 1fr);
      gap: 16px;
      margin-bottom: 24px;

      .stat-card {
        background: #fff;
        border-radius: 8px;
        padding: 20px;
        text-align: center;
        box-shadow: 0 1px 3px rgba(0, 0, 0, 0.06);

        .label {
          display: block;
          font-size: 13px;
          color: #888;
          margin-bottom: 8px;
        }
        .value {
          display: block;
          font-size: 24px;
          font-weight: 600;
          color: #1a1a1a;
        }
      }
    }

    .section {
      margin-bottom: 24px;
      h3 {
        font-size: 16px;
        margin-bottom: 12px;
      }
    }

    .provider-bars {
      .bar-row {
        display: flex;
        align-items: center;
        gap: 12px;
        margin-bottom: 8px;

        .provider-name {
          width: 100px;
          font-size: 13px;
        }
        .bar-track {
          flex: 1;
          height: 20px;
          background: #f0f0f0;
          border-radius: 10px;
          overflow: hidden;
          .bar-fill {
            height: 100%;
            background: linear-gradient(90deg, #1890ff, #36cfc9);
            border-radius: 10px;
            transition: width 0.5s;
          }
        }
        .bar-value {
          font-size: 12px;
          color: #888;
          white-space: nowrap;
        }
      }
    }
  }
</style>
