<template>
  <div class="ir-diagnostics-tab">
    <div v-if="!pipelineId" class="tab-empty">
      <span class="empty-icon">🔧</span>
      <p>创建流水线后显示租户 / 项目 / 路由诊断</p>
    </div>
    <template v-else>
      <a-descriptions size="small" :column="1" bordered>
        <a-descriptions-item label="Pipeline ID">{{ pipelineId }}</a-descriptions-item>
        <a-descriptions-item label="Project ID">{{ diagnostics?.projectId || '—' }}</a-descriptions-item>
        <a-descriptions-item label="Tenant ID">{{ diagnostics?.tenantId || '—' }}</a-descriptions-item>
        <a-descriptions-item label="Workspace">{{ diagnostics?.workspacePath || '—' }}</a-descriptions-item>
        <a-descriptions-item label="事件数">{{ diagnostics?.eventCount ?? '—' }}</a-descriptions-item>
        <a-descriptions-item label="快照数">{{ diagnostics?.snapshotCount ?? '—' }}</a-descriptions-item>
      </a-descriptions>

      <IrLlmBudgetPanel />

      <div v-if="showDevTools" class="dev-section">
        <div class="section-title">Dev 诊断</div>
        <a-button size="small" :loading="loading" @click="runRebuild">投影 Rebuild（D9）</a-button>
        <div v-if="lastRebuild" class="rebuild-result">
          <span>{{ lastRebuild.eventCount }} 事件 → {{ lastRebuild.fragmentCount }} 片段</span>
          <a-tag :color="lastRebuild.passedPerformanceGate === false ? 'error' : 'success'"> {{ lastRebuild.elapsedMs }}ms </a-tag>
        </div>
      </div>

      <div v-if="diagnostics?.routeTable?.length" class="route-table">
        <div class="section-title">路由表</div>
        <div v-for="(r, i) in diagnostics.routeTable" :key="i" class="route-row">
          <code>{{ r.path }}</code>
          <span>→</span>
          <code>{{ r.target }}</code>
        </div>
      </div>
    </template>
  </div>
</template>

<script setup lang="ts">
  import { computed, inject } from 'vue';
  import { message } from 'ant-design-vue';
  import { IR_OBSERVATORY_KEY } from '../../composables/useIrObservatory';
  import IrLlmBudgetPanel from './IrLlmBudgetPanel.vue';

  const ir = inject(IR_OBSERVATORY_KEY)!;

  const pipelineId = computed(() => ir.pipelineId.value);
  const diagnostics = computed(() => ir.diagnostics.value);
  const loading = computed(() => ir.loading.value);
  const lastRebuild = computed(() => diagnostics.value?.lastRebuild);
  const showDevTools = computed(() => import.meta.env.DEV);

  async function runRebuild() {
    try {
      const result = await ir.rebuildProject();
      if (result) {
        message.success(`Rebuild 完成：${result.elapsedMs}ms`);
      }
    } catch (e: any) {
      message.error(e?.response?.data?.msg ?? e?.message ?? 'Rebuild 失败');
    }
  }
</script>

<style scoped lang="less">
  .ir-diagnostics-tab {
    height: 100%;
    overflow-y: auto;
    padding: 4px 0;

    .tab-empty {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      height: 100%;
      color: #999;
      text-align: center;
      padding: 24px;

      .empty-icon {
        font-size: 32px;
        margin-bottom: 8px;
      }

      p {
        margin: 0;
        font-size: 13px;
      }
    }

    .dev-section {
      margin-top: 16px;
      padding: 10px;
      border: 1px dashed #ffd591;
      border-radius: 6px;
      background: #fffbe6;

      .rebuild-result {
        display: flex;
        align-items: center;
        gap: 8px;
        margin-top: 8px;
        font-size: 12px;
        color: #666;
      }
    }

    .route-table {
      margin-top: 16px;

      .section-title {
        font-size: 12px;
        font-weight: 600;
        color: #666;
        margin-bottom: 8px;
      }

      .route-row {
        display: flex;
        align-items: center;
        gap: 8px;
        font-size: 11px;
        padding: 4px 0;
        border-bottom: 1px solid #f5f5f5;

        code {
          background: #f5f5f5;
          padding: 2px 6px;
          border-radius: 3px;
        }
      }
    }

    .section-title {
      font-size: 12px;
      font-weight: 600;
      color: #666;
      margin-bottom: 8px;
    }
  }
</style>
