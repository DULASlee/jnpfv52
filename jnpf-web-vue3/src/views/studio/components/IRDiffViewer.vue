<!--
  IRDiffViewer — 三栏 IR 差异查看器（P2）
  左侧：编辑前 IR 渲染    中间：变更列表    右侧：编辑后 IR 渲染
-->
<template>
  <div class="ir-diff-viewer">
    <!-- 头部：diff 汇总 -->
    <div class="ir-diff-header">
      <a-space>
        <a-tag color="blue">操作: {{ patch.operations.length }}</a-tag>
        <a-tag color="green">成功: {{ applied.length }}</a-tag>
        <a-tag v-if="failed.length" color="red">失败: {{ failed.length }}</a-tag>
      </a-space>
      <span class="ir-diff-explanation">{{ patch.explanation }}</span>
      <a-space>
        <a-button size="small" type="primary" @click="$emit('acceptAll')">接受全部</a-button>
        <a-button size="small" @click="toggleReview">{{ reviewMode ? '退出审核' : '逐条审核' }}</a-button>
        <a-button size="small" danger @click="$emit('rollbackAll')">全部撤销</a-button>
      </a-space>
    </div>

    <!-- 三栏主体 -->
    <div class="ir-diff-body">
      <!-- 左侧：编辑前 IR -->
      <div class="ir-diff-panel ir-diff-left">
        <div class="ir-diff-panel-title">编辑前</div>
        <pre class="ir-diff-json">{{ formatIR(originalIr) }}</pre>
      </div>

      <!-- 中间：变更列表 -->
      <div class="ir-diff-panel ir-diff-center">
        <div class="ir-diff-panel-title">变更列表</div>
        <div v-if="!displayOps.length" class="ir-diff-empty">无变更</div>
        <div
          v-for="(op, idx) in displayOps"
          :key="idx"
          class="ir-diff-op"
          :class="{
            'ir-diff-op-applied': op.status === 'applied',
            'ir-diff-op-failed': op.status === 'failed',
            'ir-diff-op-pending': op.status === 'pending',
          }">
          <div class="ir-diff-op-header">
            <span class="ir-diff-op-icon">{{ opIcon(op.type) }}</span>
            <span class="ir-diff-op-status">{{ opStatusLabel(op.status) }}</span>
            <span class="ir-diff-op-path">{{ op.operation.path }}</span>
          </div>
          <div v-if="op.type === 'replace'" class="ir-diff-op-detail">
            <span class="ir-diff-op-old">- {{ truncate(String(op.operation.oldValue)) }}</span>
            <span class="ir-diff-op-new">+ {{ truncate(String(op.operation.value)) }}</span>
          </div>
          <div v-if="op.type === 'add'" class="ir-diff-op-detail">
            <span class="ir-diff-op-new">+ {{ truncate(String(op.operation.value)) }}</span>
          </div>
          <div v-if="op.type === 'remove'" class="ir-diff-op-detail">
            <span class="ir-diff-op-old">- {{ truncate(String(op.operation.oldValue)) }}</span>
          </div>
          <div class="ir-diff-op-reason">— {{ op.operation.reason }}</div>
          <div v-if="op.status === 'failed'" class="ir-diff-op-error">{{ op.failureReason }}</div>
          <div v-if="reviewMode && op.status !== 'failed'" class="ir-diff-op-actions">
            <a-button size="small" type="link" @click="$emit('acceptOne', idx)">接受</a-button>
            <a-button size="small" type="link" danger @click="$emit('rejectOne', idx)">拒绝</a-button>
          </div>
        </div>
      </div>

      <!-- 右侧：编辑后 IR -->
      <div class="ir-diff-panel ir-diff-right">
        <div class="ir-diff-panel-title">编辑后</div>
        <pre class="ir-diff-json">{{ formatIR(patchedIr) }}</pre>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
  import { computed, ref } from 'vue';
  import type { IREditPatch, IREditOperation, FailedOperation } from '../../../core/ir/edit-patch';
  import type { FormPageIR } from '../../../core/ir/types';

  interface DiffDisplayOp {
    type: 'replace' | 'add' | 'remove';
    operation: IREditOperation;
    status: 'applied' | 'failed' | 'pending';
    failureReason?: string;
  }

  const props = defineProps<{
    originalIr: FormPageIR;
    patchedIr: FormPageIR;
    patch: IREditPatch;
    applied: IREditOperation[];
    failed: FailedOperation[];
  }>();

  defineEmits<{
    acceptAll: [];
    rollbackAll: [];
    acceptOne: [index: number];
    rejectOne: [index: number];
  }>();

  const reviewMode = ref(false);

  const displayOps = computed<DiffDisplayOp[]>(() => {
    const ops: DiffDisplayOp[] = [];
    for (const a of props.applied) {
      ops.push({ type: a.op as DiffDisplayOp['type'], operation: a, status: 'applied' });
    }
    for (const f of props.failed) {
      ops.push({
        type: f.operation.op as DiffDisplayOp['type'],
        operation: f.operation,
        status: 'failed',
        failureReason: f.failureReason,
      });
    }
    return ops;
  });

  function toggleReview() {
    reviewMode.value = !reviewMode.value;
  }

  function opIcon(type: string): string {
    return { replace: '✏️', add: '➕', remove: '🗑️' }[type] ?? type;
  }

  function opStatusLabel(status: string): string {
    return { applied: '✅', failed: '❌', pending: '⏳' }[status] ?? status;
  }

  function formatIR(ir: FormPageIR): string {
    return JSON.stringify(ir, null, 2);
  }

  function truncate(value: string): string {
    return value.length > 80 ? value.slice(0, 80) + '...' : value;
  }
</script>

<style lang="less" scoped>
  .ir-diff-viewer {
    border: 1px solid #e8e8e8;
    border-radius: 4px;
    overflow: hidden;
  }
  .ir-diff-header {
    display: flex;
    align-items: center;
    justify-content: space-between;
    padding: 8px 12px;
    background: #fafafa;
    border-bottom: 1px solid #e8e8e8;
    flex-wrap: wrap;
    gap: 8px;
  }
  .ir-diff-explanation {
    color: #8c8c8c;
    font-size: 13px;
    flex: 1;
  }
  .ir-diff-body {
    display: flex;
    height: 500px;
    overflow: hidden;
  }
  .ir-diff-panel {
    flex: 1;
    overflow: auto;
    border-right: 1px solid #f0f0f0;
  }
  .ir-diff-panel:last-child {
    border-right: none;
  }
  .ir-diff-panel-title {
    padding: 6px 12px;
    font-weight: 600;
    font-size: 13px;
    color: #595959;
    background: #fafafa;
    border-bottom: 1px solid #e8e8e8;
    position: sticky;
    top: 0;
  }
  .ir-diff-center {
    flex: 0 0 350px;
  }
  .ir-diff-json {
    font-family: 'Courier New', monospace;
    font-size: 11px;
    padding: 8px;
    margin: 0;
    white-space: pre-wrap;
    word-break: break-all;
    color: #262626;
  }
  .ir-diff-empty {
    padding: 24px;
    text-align: center;
    color: #8c8c8c;
  }
  .ir-diff-op {
    padding: 8px 12px;
    border-bottom: 1px solid #f0f0f0;
    cursor: pointer;
    transition: background 0.1s;
  }
  .ir-diff-op:hover {
    background: #fafafa;
  }
  .ir-diff-op-applied {
    border-left: 3px solid #52c41a;
  }
  .ir-diff-op-failed {
    border-left: 3px solid #ff4d4f;
    background: #fff2f0;
  }
  .ir-diff-op-pending {
    border-left: 3px solid #faad14;
  }
  .ir-diff-op-header {
    display: flex;
    align-items: center;
    gap: 6px;
    margin-bottom: 4px;
  }
  .ir-diff-op-icon {
    font-size: 14px;
  }
  .ir-diff-op-status {
    font-size: 12px;
  }
  .ir-diff-op-path {
    font-family: monospace;
    font-size: 12px;
    color: #595959;
    flex: 1;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
  }
  .ir-diff-op-detail {
    font-family: monospace;
    font-size: 12px;
    margin: 4px 0;
    display: flex;
    flex-direction: column;
    gap: 2px;
  }
  .ir-diff-op-old {
    color: #ff4d4f;
  }
  .ir-diff-op-new {
    color: #52c41a;
  }
  .ir-diff-op-reason {
    color: #8c8c8c;
    font-size: 12px;
  }
  .ir-diff-op-error {
    color: #ff4d4f;
    font-size: 12px;
    margin-top: 2px;
    font-weight: 600;
  }
  .ir-diff-op-actions {
    margin-top: 6px;
    display: flex;
    gap: 8px;
  }
</style>
