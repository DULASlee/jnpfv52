<template>
  <div class="pipeline-task-list">
    <div class="list-header">
      <span>我的任务{{ tasks.length > 0 ? ` (${tasks.length})` : '' }}</span>
      <a-button type="link" size="small" :loading="loading" @click="loadTasks">刷新</a-button>
    </div>
    <div v-if="tasks.length === 0" class="list-empty">暂无任务</div>
    <ul v-else class="task-items">
      <li v-for="t in tasks" :key="t.id" class="task-item" :class="{ active: t.id === activePipelineId }" @click="$emit('select', t.id)">
        <div class="task-name">{{ t.name || '未命名' }}</div>
        <div class="task-meta">
          <span>#{{ t.id }}</span>
          <span>{{ stageLabel(t.currentStage) }}</span>
        </div>
      </li>
    </ul>
  </div>
</template>

<script setup lang="ts">
  import { onMounted, ref } from 'vue';
  import { getPipelineList, type PipelineSummaryItem } from '../api/studio/pipeline';

  defineProps<{ activePipelineId?: number }>();
  defineEmits<{ select: [pipelineId: number] }>();

  type TaskItem = {
    id: number;
    name: string;
    currentStage: string;
    status: string;
    updatedAt?: string;
  };

  const tasks = ref<TaskItem[]>([]);
  const loading = ref(false);

  const STAGE_LABELS: Record<string, string> = {
    requirement: '需求分析',
    architecture: '架构设计',
    design: '总体设计',
    development: '自动开发',
    delivery: '交付验证',
  };

  function stageLabel(code: string) {
    return STAGE_LABELS[code] || code || '进行中';
  }

  function normalizeTasks(payload: any): TaskItem[] {
    const rawList = Array.isArray(payload)
      ? payload
      : Array.isArray(payload?.data)
      ? payload.data
      : Array.isArray(payload?.data?.data)
      ? payload.data.data
      : Array.isArray(payload?.list)
      ? payload.list
      : [];

    return rawList
      .map((x: any) => ({
        id: Number(x.id ?? x.Id ?? 0),
        name: String(x.name ?? x.Name ?? ''),
        currentStage: String(x.currentStage ?? x.CurrentStage ?? ''),
        status: String(x.status ?? x.Status ?? ''),
        updatedAt: x.updatedAt ?? x.UpdatedAt,
      }))
      .filter(x => x.id > 0);
  }

  async function loadTasks() {
    loading.value = true;
    try {
      const res = await getPipelineList(0, 200);
      tasks.value = normalizeTasks(res);
    } catch {
      tasks.value = [];
    } finally {
      loading.value = false;
    }
  }

  onMounted(loadTasks);

  defineExpose({ reload: loadTasks });
</script>

<style scoped lang="less">
  .pipeline-task-list {
    flex: 1;
    min-height: 0;
    display: flex;
    flex-direction: column;
    padding: 12px 8px;
    overflow: hidden;

    .list-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      font-size: 12px;
      font-weight: 600;
      color: #999;
      margin-bottom: 8px;
      padding: 0 4px;
    }

    .list-empty {
      font-size: 12px;
      color: #bbb;
      text-align: center;
      padding: 12px 4px;
    }

    .task-items {
      flex: 1;
      min-height: 0;
      overflow-y: auto;
      list-style: none;
      margin: 0;
      padding: 0;

      .task-item {
        padding: 8px;
        border-radius: 6px;
        cursor: pointer;
        margin-bottom: 4px;
        border: 1px solid transparent;

        &:hover {
          background: #f5f5f5;
        }

        &.active {
          background: #e6f7ff;
          border-color: #91d5ff;
        }

        .task-name {
          font-size: 13px;
          color: #333;
          white-space: nowrap;
          overflow: hidden;
          text-overflow: ellipsis;
        }

        .task-meta {
          display: flex;
          justify-content: space-between;
          font-size: 11px;
          color: #999;
          margin-top: 2px;
        }
      }
    }
  }
</style>
