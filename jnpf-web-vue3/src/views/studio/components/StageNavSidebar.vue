<template>
  <nav class="stage-nav-sidebar">
    <div class="sidebar-title">流水线阶段</div>
    <ul class="stage-list">
      <li v-for="s in stages" :key="s.stage" class="stage-item" :class="{ active: s.stage === currentStage, completed: s.stage < currentStage }">
        <span class="stage-num">{{ s.stage }}</span>
        <span class="stage-name">{{ s.name }}</span>
        <a-tag v-if="s.stage < currentStage" color="green" size="small">完成</a-tag>
        <a-tag v-else-if="s.stage === currentStage" color="blue" size="small">进行中</a-tag>
      </li>
    </ul>
    <div v-if="(pipelineId ?? 0) > 0" class="pipeline-meta">
      <span class="meta-label">Pipeline</span>
      <span class="meta-value">#{{ pipelineId }}</span>
    </div>
  </nav>
</template>

<script setup lang="ts">
  defineProps<{
    stages: Array<{ stage: number; name: string; code?: string }>;
    currentStage: number;
    pipelineId?: number;
  }>();
</script>

<style scoped lang="less">
  .stage-nav-sidebar {
    display: flex;
    flex-direction: column;
    height: 100%;
    padding: 16px 12px;
    background: #fafafa;
    border-right: 1px solid #f0f0f0;

    .sidebar-title {
      font-size: 12px;
      font-weight: 600;
      color: #999;
      text-transform: uppercase;
      letter-spacing: 0.5px;
      margin-bottom: 12px;
      padding: 0 8px;
    }

    .stage-list {
      list-style: none;
      margin: 0;
      padding: 0;
      flex: 1;

      .stage-item {
        display: flex;
        align-items: center;
        gap: 8px;
        padding: 10px 8px;
        border-radius: 6px;
        margin-bottom: 4px;
        font-size: 13px;
        color: #666;
        transition: background 0.2s;

        &.active {
          background: #e6f7ff;
          color: #1890ff;
          font-weight: 500;
        }

        &.completed .stage-num {
          background: #52c41a;
          color: #fff;
        }

        .stage-num {
          width: 22px;
          height: 22px;
          border-radius: 50%;
          background: #f0f0f0;
          display: flex;
          align-items: center;
          justify-content: center;
          font-size: 11px;
          font-weight: 600;
          flex-shrink: 0;
        }

        .stage-name {
          flex: 1;
          min-width: 0;
        }
      }
    }

    .pipeline-meta {
      margin-top: auto;
      padding: 8px;
      font-size: 11px;
      color: #999;
      border-top: 1px solid #f0f0f0;

      .meta-label {
        display: block;
        margin-bottom: 2px;
      }

      .meta-value {
        font-family: monospace;
        color: #666;
      }
    }
  }
</style>
