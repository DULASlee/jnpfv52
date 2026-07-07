<template>
  <div class="ir-observatory-panel">
    <div class="panel-header">
      <span class="panel-title">观测台</span>
      <a-tooltip v-if="connected" :title="lastHeartbeat ? `SSE 心跳 ${lastHeartbeat}` : 'SSE 已连接'">
        <span class="conn-dot connected">●</span>
      </a-tooltip>
      <a-tooltip v-else-if="pipelineId > 0" title="等待 SSE">
        <span class="conn-dot">○</span>
      </a-tooltip>
    </div>
    <a-tabs v-model:activeKey="activeTab" size="small" class="observatory-tabs">
      <a-tab-pane key="deliverables" tab="产物">
        <IrDeliverablesTab :current-stage="currentStage" />
      </a-tab-pane>
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
      <a-tab-pane key="skillQuality" tab="Skill 质量">
        <IrSkillQualityTab />
      </a-tab-pane>
    </a-tabs>
  </div>
</template>

<script setup lang="ts">
  import { computed, inject, ref, watch } from 'vue';
  import { IR_OBSERVATORY_KEY } from '../composables/useIrObservatory';
  import IrDeliverablesTab from './ir/IrDeliverablesTab.vue';
  import IrEventStreamTab from './ir/IrEventStreamTab.vue';
  import IrSnapshotTab from './ir/IrSnapshotTab.vue';
  import Ir2SnapshotTab from './ir/Ir2SnapshotTab.vue';
  import Ir3SnapshotTab from './ir/Ir3SnapshotTab.vue';
  import IrStabilityTab from './ir/IrStabilityTab.vue';
  import IrDiagnosticsTab from './ir/IrDiagnosticsTab.vue';
  import IrSkillQualityTab from './ir/IrSkillQualityTab.vue';

  defineProps<{ currentStage?: number }>();

  const ir = inject(IR_OBSERVATORY_KEY)!;
  const activeTab = ref('deliverables');

  watch(
    () => ir.preferredObservatoryTab.value,
    tab => {
      if (tab) activeTab.value = tab;
    },
  );

  const pipelineId = computed(() => ir.pipelineId.value);
  const connected = computed(() => ir.connected.value);
  const lastHeartbeat = computed(() => ir.lastHeartbeat.value);
</script>

<style scoped lang="less">
  .ir-observatory-panel {
    display: flex;
    flex-direction: column;
    height: 100%;
    min-height: 0;
    background: #fff;
    border-left: 1px solid #f0f0f0;
  }

  .panel-header {
    display: flex;
    align-items: center;
    gap: 8px;
    padding: 10px 12px;
    border-bottom: 1px solid #f0f0f0;
    flex-shrink: 0;

    .panel-title {
      font-size: 13px;
      font-weight: 600;
      color: #262626;
      flex: 1;
    }
  }

  .conn-dot {
    font-size: 10px;
    color: #d9d9d9;

    &.connected {
      color: #52c41a;
    }
  }

  .observatory-tabs {
    flex: 1;
    display: flex;
    flex-direction: column;
    overflow: hidden;
    min-height: 0;
    padding: 0 8px 8px;

    :deep(.ant-tabs-nav) {
      margin-bottom: 8px;
    }

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
</style>
