<template>
  <aside class="ir-observatory-panel" :class="{ collapsed: collapsed }">
    <div class="panel-header">
      <span v-if="!collapsed" class="panel-title">IR 观测台</span>
      <div class="header-actions">
        <a-tooltip v-if="connected" title="SSE 已连接">
          <span class="conn-dot connected">●</span>
        </a-tooltip>
        <a-tooltip v-else-if="pipelineId > 0" title="等待 SSE">
          <span class="conn-dot">○</span>
        </a-tooltip>
        <a-button type="text" size="small" @click="$emit('toggle-collapse')">
          {{ collapsed ? '◀' : '▶' }}
        </a-button>
      </div>
    </div>
    <template v-if="!collapsed">
      <a-tabs v-model:activeKey="activeTab" size="small" class="observatory-tabs">
        <a-tab-pane key="events" tab="事件流">
          <IrEventStreamTab />
        </a-tab-pane>
        <a-tab-pane key="snapshots" tab="快照">
          <IrSnapshotTab />
        </a-tab-pane>
        <a-tab-pane key="ir2" tab="IR-2">
          <Ir2SnapshotTab />
        </a-tab-pane>
        <a-tab-pane key="ir3" tab="IR-3">
          <Ir3SnapshotTab />
        </a-tab-pane>
        <a-tab-pane key="stability" tab="门控">
          <IrStabilityTab />
        </a-tab-pane>
        <a-tab-pane key="diagnostics" tab="诊断">
          <IrDiagnosticsTab />
        </a-tab-pane>
      </a-tabs>
    </template>
  </aside>
</template>

<script setup lang="ts">
  import { computed, inject, ref, watch } from 'vue';
  import { IR_OBSERVATORY_KEY } from '../composables/useIrObservatory';
  import IrEventStreamTab from './ir/IrEventStreamTab.vue';
  import IrSnapshotTab from './ir/IrSnapshotTab.vue';
  import Ir2SnapshotTab from './ir/Ir2SnapshotTab.vue';
  import Ir3SnapshotTab from './ir/Ir3SnapshotTab.vue';
  import IrStabilityTab from './ir/IrStabilityTab.vue';
  import IrDiagnosticsTab from './ir/IrDiagnosticsTab.vue';

  defineProps<{ collapsed?: boolean }>();
  defineEmits<{ 'toggle-collapse': [] }>();

  const ir = inject(IR_OBSERVATORY_KEY)!;
  const activeTab = ref('events');

  watch(
    () => ir.preferredObservatoryTab.value,
    tab => {
      if (tab) activeTab.value = tab;
    },
  );

  const pipelineId = computed(() => ir.pipelineId.value);
  const connected = computed(() => ir.connected.value);
</script>

<style scoped lang="less">
  .ir-observatory-panel {
    display: flex;
    flex-direction: column;
    height: 100%;
    background: #fff;
    border-left: 1px solid #f0f0f0;
    transition: width 0.2s;
    width: 360px;
    min-width: 360px;
    overflow: hidden;

    &.collapsed {
      width: 40px;
      min-width: 40px;
    }

    .panel-header {
      display: flex;
      align-items: center;
      justify-content: space-between;
      padding: 8px 12px;
      border-bottom: 1px solid #f0f0f0;
      flex-shrink: 0;

      .panel-title {
        font-size: 13px;
        font-weight: 600;
        color: #333;
      }

      .header-actions {
        display: flex;
        align-items: center;
        gap: 4px;
      }

      .conn-dot {
        font-size: 10px;
        color: #d9d9d9;

        &.connected {
          color: #52c41a;
        }
      }
    }

    .observatory-tabs {
      flex: 1;
      display: flex;
      flex-direction: column;
      overflow: hidden;
      padding: 0 8px 8px;

      :deep(.ant-tabs-content-holder) {
        flex: 1;
        overflow: hidden;
      }

      :deep(.ant-tabs-content) {
        height: 100%;
      }

      :deep(.ant-tabs-tabpane) {
        height: 100%;
        overflow: hidden;
      }
    }
  }

  @media (max-width: 1439px) {
    .ir-observatory-panel:not(.collapsed) {
      position: absolute;
      right: 0;
      top: 0;
      bottom: 0;
      z-index: 20;
      box-shadow: -4px 0 12px rgba(0, 0, 0, 0.08);
    }
  }
</style>
