<template>
  <div class="ir-snapshot-tab">
    <div v-if="!pipelineId" class="tab-empty">
      <span class="empty-icon">📦</span>
      <p>IR 片段快照将在事件写入后投影到此</p>
    </div>
    <div v-else-if="snapshots.length === 0" class="tab-empty compact">
      <p>暂无快照</p>
    </div>
    <div v-else class="snapshot-list">
      <div v-for="snap in snapshots" :key="snap.fragmentId" class="snapshot-item">
        <div class="snap-header">
          <code>{{ snap.fragmentId }}</code>
          <a-tag :color="stabilityColor(snap.stabilityState)">{{ snap.stabilityState }}</a-tag>
        </div>
        <div class="snap-meta">
          <span>{{ snap.fragmentType }}</span>
          <span> · v{{ displayVersion(snap) }}</span>
        </div>
        <div v-if="snap.saStepsCompleted?.length" class="snap-steps"> SA 完成: {{ snap.saStepsCompleted.length }}/9 </div>
        <div class="version-travel">
          <span class="label">时间旅行 (D12)</span>
          <a-select
            v-model:value="selectedVersions[snap.fragmentId]"
            size="small"
            style="width: 100px"
            :options="versionOptions(snap)"
            @change="(v: number) => loadVersion(snap.fragmentId, v)" />
        </div>
        <pre v-if="displayPayload(snap)" class="snap-json">{{ formatJson(displayPayload(snap)) }}</pre>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
  import { computed, inject, reactive } from 'vue';
  import { message } from 'ant-design-vue';
  import { IR_OBSERVATORY_KEY } from '../../composables/useIrObservatory';
  import { getIrSnapshotAtVersion } from '../../api/studio/ir';
  import type { IrFragmentSnapshot } from '../../types/ir';

  const ir = inject(IR_OBSERVATORY_KEY)!;

  const pipelineId = computed(() => ir.pipelineId.value);
  const snapshots = computed(() => ir.snapshots.value);
  const selectedVersions = reactive<Record<string, number>>({});
  const historicalPayloads = reactive<Record<string, Record<number, unknown>>>({});

  function stabilityColor(state: IrFragmentSnapshot['stabilityState']) {
    const map = { draft: 'default', 'in-progress': 'processing', stable: 'success', locked: 'warning' };
    return map[state] || 'default';
  }

  function versionOptions(snap: IrFragmentSnapshot) {
    const max = snap.currentVersion || 1;
    return Array.from({ length: max }, (_, i) => ({
      label: `v${i + 1}`,
      value: i + 1,
    }));
  }

  function displayVersion(snap: IrFragmentSnapshot) {
    return selectedVersions[snap.fragmentId] ?? snap.currentVersion;
  }

  function displayPayload(snap: IrFragmentSnapshot) {
    const v = selectedVersions[snap.fragmentId];
    if (v != null && historicalPayloads[snap.fragmentId]?.[v] != null) {
      return historicalPayloads[snap.fragmentId][v];
    }
    return snap.payload;
  }

  async function loadVersion(fragmentId: string, version: number) {
    if (!pipelineId.value) return;
    selectedVersions[fragmentId] = version;
    try {
      const res = await getIrSnapshotAtVersion(pipelineId.value, fragmentId, version);
      const data = (res as any)?.data ?? res;
      if (!historicalPayloads[fragmentId]) historicalPayloads[fragmentId] = {};
      historicalPayloads[fragmentId][version] = data?.payload ?? data;
    } catch (e: any) {
      message.error(e?.response?.data?.msg ?? '加载历史版本失败');
    }
  }

  function formatJson(payload: unknown) {
    try {
      return JSON.stringify(payload, null, 2);
    } catch {
      return String(payload);
    }
  }
</script>

<style scoped lang="less">
  .ir-snapshot-tab {
    height: 100%;
    overflow-y: auto;

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
        font-size: 32px;
        margin-bottom: 8px;
      }

      p {
        margin: 0;
        font-size: 13px;
      }
    }

    .snapshot-list {
      .snapshot-item {
        padding: 10px;
        border-bottom: 1px solid #f5f5f5;

        .snap-header {
          display: flex;
          justify-content: space-between;
          align-items: center;
          margin-bottom: 4px;

          code {
            font-size: 12px;
            background: #f5f5f5;
            padding: 2px 6px;
            border-radius: 3px;
          }
        }

        .snap-meta {
          font-size: 12px;
          color: #666;
          margin-bottom: 4px;
        }

        .snap-steps {
          font-size: 11px;
          color: #722ed1;
          margin-bottom: 4px;
        }

        .version-travel {
          display: flex;
          align-items: center;
          gap: 8px;
          margin-bottom: 6px;

          .label {
            font-size: 11px;
            color: #999;
          }
        }

        .snap-json {
          margin: 4px 0 0;
          padding: 8px;
          background: #fafafa;
          border-radius: 4px;
          font-size: 11px;
          max-height: 200px;
          overflow: auto;
        }
      }
    }
  }
</style>
