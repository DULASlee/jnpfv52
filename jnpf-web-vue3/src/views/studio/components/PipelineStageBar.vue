<template>
  <div class="pipeline-stage-bar">
    <!-- 5 阶段步骤条 -->
    <div class="stages">
      <div v-for="(stage, i) in stages" :key="stage.key" class="stage" :class="stageClass(stage, i)" @click="$emit('selectStage', stage.key)">
        <div class="dot">
          <span v-if="isCompleted(stage, i)">✓</span>
          <span v-else-if="isCurrent(stage, i)" class="pulse"></span>
          <span v-else>{{ i + 1 }}</span>
        </div>
        <div class="info">
          <span class="name">{{ stage.label }}</span>
          <span v-if="stage.time" class="time">{{ stage.time }}</span>
        </div>
        <div v-if="i < stages.length - 1" class="connector" :class="{ active: isCompleted(stage, i) }"></div>
      </div>
    </div>

    <!-- 当前阶段详情 -->
    <div v-if="currentStageDetail" class="current-detail">
      <div class="detail-header">
        <strong>当前阶段: {{ currentStageDetail.label }}</strong>
        <span class="toggle" @click="detailExpanded = !detailExpanded">
          {{ detailExpanded ? '收起 ▴' : '查看详情 ▾' }}
        </span>
      </div>
      <div v-if="detailExpanded && currentStageDetail.output" class="detail-body">
        <div class="output-text">{{ currentStageDetail.output }}</div>
        <div v-if="currentStageDetail.irPreview" class="ir-mini">
          <IrPreviewCard :ir-data="currentStageDetail.irPreview" />
        </div>
      </div>
    </div>

    <!-- 操作栏 -->
    <ConfirmBar
      :waiting="waiting"
      :show-rollback="canRollback"
      :show-feedback="canConfirm"
      :show-ask="canConfirm"
      @confirm="fb => $emit('confirm', fb)"
      @rollback="$emit('rollback')"
      @ask="fb => $emit('ask', fb)" />
  </div>
</template>

<script setup lang="ts">
  import { ref, computed } from 'vue';
  import ConfirmBar from './chat/ConfirmBar.vue';
  import IrPreviewCard from './chat/IrPreviewCard.vue';

  export interface StageInfo {
    key: string;
    label: string;
    status: 'completed' | 'running' | 'pending' | 'failed';
    time?: string;
    output?: string;
    irPreview?: any;
  }

  const props = defineProps<{
    stages: StageInfo[];
    waiting?: boolean;
    canRollback?: boolean;
    canConfirm?: boolean;
  }>();

  defineEmits<{
    confirm: [feedback: string];
    rollback: [];
    ask: [feedback: string];
    selectStage: [key: string];
  }>();

  const detailExpanded = ref(false);

  const currentStageDetail = computed(() => props.stages.find(s => s.status === 'running'));

  function stageClass(stage: StageInfo, i: number) {
    return {
      completed: isCompleted(stage, i),
      running: isCurrent(stage, i),
      failed: stage.status === 'failed',
    };
  }

  function isCompleted(stage: StageInfo, i: number) {
    return stage.status === 'completed' || i < props.stages.findIndex(s => s.status === 'running');
  }

  function isCurrent(stage: StageInfo, _i: number) {
    return stage.status === 'running';
  }
</script>

<style scoped lang="less">
  .pipeline-stage-bar {
    background: #fff;
    border-bottom: 1px solid #f0f0f0;

    .stages {
      display: flex;
      align-items: flex-start;
      padding: 16px 24px;
      overflow-x: auto;

      .stage {
        display: flex;
        flex-direction: column;
        align-items: center;
        flex: 1;
        min-width: 80px;
        position: relative;
        cursor: pointer;

        .dot {
          width: 28px;
          height: 28px;
          border-radius: 50%;
          display: flex;
          align-items: center;
          justify-content: center;
          font-size: 12px;
          font-weight: 600;
          background: #f0f0f0;
          color: #999;
          transition: all 0.3s;
          z-index: 1;

          .pulse {
            width: 8px;
            height: 8px;
            border-radius: 50%;
            background: #1890ff;
            animation: pulse 1.5s infinite;
          }
        }

        .info {
          text-align: center;
          margin-top: 6px;
          .name {
            display: block;
            font-size: 12px;
            color: #666;
          }
          .time {
            display: block;
            font-size: 10px;
            color: #bbb;
          }
        }

        .connector {
          position: absolute;
          top: 14px;
          left: 60%;
          width: 80%;
          height: 2px;
          background: #f0f0f0;
          &.active {
            background: #1890ff;
          }
        }

        &.completed .dot {
          background: #52c41a;
          color: #fff;
        }
        &.running .dot {
          background: #e6f7ff;
          border: 2px solid #1890ff;
        }
        &.running .name {
          color: #1890ff;
          font-weight: 600;
        }
        &.failed .dot {
          background: #ff4d4f;
          color: #fff;
        }
      }
    }

    .current-detail {
      padding: 0 24px 12px;

      .detail-header {
        display: flex;
        justify-content: space-between;
        font-size: 13px;
        .toggle {
          color: #1890ff;
          cursor: pointer;
        }
      }

      .detail-body {
        margin-top: 8px;
        .output-text {
          font-size: 13px;
          color: #555;
          background: #fafafa;
          padding: 8px;
          border-radius: 4px;
        }
      }
    }
  }

  @keyframes pulse {
    0%,
    100% {
      transform: scale(0.8);
      opacity: 0.5;
    }
    50% {
      transform: scale(1.2);
      opacity: 1;
    }
  }
</style>
