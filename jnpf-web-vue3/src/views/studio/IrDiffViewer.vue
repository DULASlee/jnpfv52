<!--
  IrDiffViewer — IR 变更差异查看器（P2）
  接收 IREditPatch 或两个 IR 版本，可视化展示变更。
-->
<template>
  <div class="ir-diff-viewer">
    <div v-if="!hasChanges" class="ir-diff-empty">无变更</div>
    <div v-else class="ir-diff-content">
      <div class="ir-diff-header">
        <span>变更操作: {{ totalOps }} 条</span>
        <span class="ir-diff-applied">已应用: {{ appliedCount }}</span>
        <span v-if="failedCount" class="ir-diff-failed">失败: {{ failedCount }}</span>
      </div>
      <div class="ir-diff-ops">
        <div v-for="(op, idx) in displayOps" :key="idx" class="ir-diff-op" :class="{ 'ir-diff-op-failed': op.failed }">
          <span class="ir-diff-op-tag" :class="`ir-diff-op-tag-${op.type}`">{{ op.type }}</span>
          <span class="ir-diff-op-path">{{ op.path }}</span>
          <span v-if="op.reason" class="ir-diff-op-reason">— {{ op.reason }}</span>
          <div v-if="op.failed" class="ir-diff-op-error">{{ op.failureReason }}</div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
  import { computed } from 'vue';

  interface DiffDisplayOp {
    type: 'replace' | 'add' | 'remove';
    path: string;
    reason: string;
    failed?: boolean;
    failureReason?: string;
  }

  const props = defineProps<{
    appliedOps?: Array<{ op: string; path: string; reason: string }>;
    failedOps?: Array<{ operation: { op: string; path: string; reason: string }; failureReason: string }>;
  }>();

  const displayOps = computed<DiffDisplayOp[]>(() => {
    const ops: DiffDisplayOp[] = [];
    for (const a of props.appliedOps ?? []) {
      ops.push({ type: a.op as DiffDisplayOp['type'], path: a.path, reason: a.reason });
    }
    for (const f of props.failedOps ?? []) {
      ops.push({
        type: f.operation.op as DiffDisplayOp['type'],
        path: f.operation.path,
        reason: f.operation.reason,
        failed: true,
        failureReason: f.failureReason,
      });
    }
    return ops;
  });

  const totalOps = computed(() => displayOps.value.length);
  const appliedCount = computed(() => props.appliedOps?.length ?? 0);
  const failedCount = computed(() => props.failedOps?.length ?? 0);
  const hasChanges = computed(() => totalOps.value > 0);
</script>

<style lang="less" scoped>
  .ir-diff-viewer {
    font-family: monospace;
    font-size: 13px;
  }
  .ir-diff-header {
    padding: 8px 12px;
    background: #f5f5f5;
    border-bottom: 1px solid #e8e8e8;
    display: flex;
    gap: 16px;
  }
  .ir-diff-applied {
    color: #52c41a;
  }
  .ir-diff-failed {
    color: #ff4d4f;
  }
  .ir-diff-op {
    padding: 6px 12px;
    border-bottom: 1px solid #f0f0f0;
    display: flex;
    flex-wrap: wrap;
    align-items: baseline;
    gap: 8px;
  }
  .ir-diff-op-failed {
    background: #fff2f0;
  }
  .ir-diff-op-tag {
    display: inline-block;
    padding: 1px 6px;
    border-radius: 3px;
    font-size: 11px;
    font-weight: bold;
    color: #fff;
  }
  .ir-diff-op-tag-replace {
    background: #1890ff;
  }
  .ir-diff-op-tag-add {
    background: #52c41a;
  }
  .ir-diff-op-tag-remove {
    background: #ff4d4f;
  }
  .ir-diff-op-path {
    color: #595959;
  }
  .ir-diff-op-reason {
    color: #8c8c8c;
  }
  .ir-diff-op-error {
    width: 100%;
    color: #ff4d4f;
    font-size: 12px;
    margin-top: 2px;
  }
  .ir-diff-empty {
    padding: 24px;
    text-align: center;
    color: #8c8c8c;
  }
</style>
