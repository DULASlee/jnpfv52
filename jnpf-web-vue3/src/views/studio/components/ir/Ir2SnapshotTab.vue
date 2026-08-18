<template>
  <div class="ir2-snapshot-tab">
    <div v-if="!pipelineId" class="tab-empty">
      <span class="empty-icon">📐</span>
      <p>设计 Skill 产出后，IR-2 片段将在此展示</p>
    </div>
    <template v-else>
      <a-tabs v-model:activeKey="activeIr2Tab" size="small" class="ir2-sub-tabs">
        <a-tab-pane v-for="tab in ir2Tabs" :key="tab.key" :tab="tab.label">
          <div v-if="!tab.snapshot" class="tab-empty compact">
            <p>{{ tab.emptyHint }}</p>
          </div>
          <div v-else class="ir2-detail">
            <div class="detail-header">
              <code>{{ tab.snapshot.fragmentId }}</code>
              <a-tag :color="stabilityColor(tab.snapshot.stabilityState)">
                {{ tab.snapshot.stabilityState }}
              </a-tag>
            </div>
            <div class="detail-meta">{{ tab.snapshot.fragmentType }} · v{{ tab.snapshot.currentVersion }}</div>
            <pre v-if="tab.snapshot.payload" class="detail-json">{{ formatJson(tab.snapshot.payload) }}</pre>
            <pre v-else-if="extractDdl(tab.snapshot)" class="detail-json ddl-block">{{ extractDdl(tab.snapshot) }}</pre>
          </div>
        </a-tab-pane>
      </a-tabs>
    </template>
  </div>
</template>

<script setup lang="ts">
  import { computed, inject, ref } from 'vue';
  import { IR_OBSERVATORY_KEY } from '../../composables/useIrObservatory';
  import { IR2_FRAGMENT_TYPES } from '../../composables/useDesignSkills';
  import type { IrFragmentSnapshot } from '../../types/ir';

  const ir = inject(IR_OBSERVATORY_KEY)!;
  const activeIr2Tab = ref('architecture');

  const pipelineId = computed(() => ir.pipelineId.value);

  const ir2Snapshots = computed(() => ir.snapshots.value.filter(s => IR2_FRAGMENT_TYPES.includes(s.fragmentType as (typeof IR2_FRAGMENT_TYPES)[number])));

  function findSnapshot(type: string) {
    return ir2Snapshots.value.find(s => s.fragmentType === type);
  }

  const ir2Tabs = computed(() => [
    {
      key: 'architecture',
      label: 'Architecture',
      snapshot: findSnapshot('IR2_Architecture'),
      emptyHint: '运行 architect-skill 后显示架构决策',
    },
    {
      key: 'ddl',
      label: 'DDL',
      snapshot: findSnapshot('IR2_DDL'),
      emptyHint: '运行 db-design-skill 后显示 DDL',
    },
    {
      key: 'ui',
      label: 'FormPageIR',
      snapshot: findSnapshot('IR2_FormPageIR'),
      emptyHint: '运行 ui-design-skill 后显示 FormPageIR',
    },
    {
      key: 'system',
      label: 'SystemDesign',
      snapshot: findSnapshot('IR2_SystemDesign'),
      emptyHint: '三片段 stable 后 system-design-skill 锁定',
    },
  ]);

  function stabilityColor(state: IrFragmentSnapshot['stabilityState']) {
    const map = { draft: 'default', 'in-progress': 'processing', stable: 'success', locked: 'warning' };
    return map[state] || 'default';
  }

  function formatJson(payload: unknown) {
    try {
      return JSON.stringify(payload, null, 2);
    } catch {
      return String(payload);
    }
  }

  function extractDdl(snap: IrFragmentSnapshot) {
    if (snap.fragmentType !== 'IR2_DDL' || !snap.payload) return '';
    try {
      const p = typeof snap.payload === 'string' ? JSON.parse(snap.payload) : snap.payload;
      return (p as { ddl?: string })?.ddl ?? '';
    } catch {
      return '';
    }
  }
</script>

<style scoped lang="less">
  .ir2-snapshot-tab {
    height: 100%;
    display: flex;
    flex-direction: column;
    overflow: hidden;

    .tab-empty {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      height: 100%;
      color: #999;
      text-align: center;
      padding: 24px;

      &.compact {
        height: auto;
        padding: 16px;
      }

      .empty-icon {
        font-size: 28px;
        margin-bottom: 8px;
      }

      p {
        margin: 0;
        font-size: 13px;
      }
    }

    .ir2-sub-tabs {
      flex: 1;
      display: flex;
      flex-direction: column;
      overflow: hidden;

      :deep(.ant-tabs-content-holder) {
        flex: 1;
        overflow: hidden;
      }

      :deep(.ant-tabs-tabpane) {
        height: 100%;
        overflow-y: auto;
      }
    }

    .ir2-detail {
      padding: 4px 0;

      .detail-header {
        display: flex;
        justify-content: space-between;
        align-items: center;
        margin-bottom: 4px;

        code {
          font-size: 11px;
          background: #f5f5f5;
          padding: 2px 6px;
          border-radius: 3px;
        }
      }

      .detail-meta {
        font-size: 11px;
        color: #666;
        margin-bottom: 8px;
      }

      .detail-json {
        margin: 0;
        padding: 8px;
        background: #fafafa;
        border-radius: 4px;
        font-size: 11px;
        max-height: 280px;
        overflow: auto;
        white-space: pre-wrap;
        word-break: break-all;
      }

      .ddl-block {
        font-family: Consolas, monospace;
      }
    }
  }
</style>
