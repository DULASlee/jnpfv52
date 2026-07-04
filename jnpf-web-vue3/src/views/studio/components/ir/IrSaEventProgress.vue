<template>
  <div class="sa-event-progress">
    <div v-if="events.length === 0" class="empty">暂无 EventSpec 分析进度</div>
    <div v-for="evt in events" :key="evt.fragmentId" class="event-row">
      <div class="event-header">
        <code>{{ evt.eventId }}</code>
        <a-tag v-if="evt.isStable" color="green" size="small">stable</a-tag>
        <a-tag v-else color="processing" size="small">{{ evt.completedSteps.length }}/9</a-tag>
      </div>
      <a-progress :percent="evt.percent" size="small" :status="evt.isStable ? 'success' : 'active'" />
      <div class="sa-steps">
        <span v-for="(step, i) in steps" :key="step" class="sa-step" :class="{ done: evt.completedSteps.includes(step) }" :title="step">
          {{ i + 1 }}
        </span>
      </div>
    </div>
    <div v-if="events.length > 0" class="total-bar">
      <span>总体进度</span>
      <a-progress :percent="totalPercent" size="small" />
    </div>
  </div>
</template>

<script setup lang="ts">
  import type { EventSaProgress } from '../../composables/useAnalystSkill';

  defineProps<{
    events: EventSaProgress[];
    steps: string[];
    totalPercent: number;
  }>();
</script>

<style scoped lang="less">
  .sa-event-progress {
    .empty {
      color: #999;
      font-size: 13px;
      padding: 8px 0;
    }

    .event-row {
      padding: 10px 0;
      border-bottom: 1px solid #f0f0f0;

      &:last-child {
        border-bottom: none;
      }

      .event-header {
        display: flex;
        align-items: center;
        gap: 8px;
        margin-bottom: 6px;
        font-size: 12px;
      }

      .sa-steps {
        display: flex;
        flex-wrap: wrap;
        gap: 4px;
        margin-top: 8px;

        .sa-step {
          width: 22px;
          height: 22px;
          border-radius: 4px;
          background: #f5f5f5;
          color: #999;
          font-size: 10px;
          display: flex;
          align-items: center;
          justify-content: center;

          &.done {
            background: #52c41a;
            color: #fff;
          }
        }
      }
    }

    .total-bar {
      margin-top: 12px;
      padding-top: 12px;
      border-top: 1px dashed #e8e8e8;
      font-size: 12px;
      color: #666;
    }
  }
</style>
