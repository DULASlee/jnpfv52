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
        <div class="task-submeta">
          <span class="task-creator" :title="t.creatorUserName || '未知'">{{ t.creatorUserName || '未知' }}</span>
          <span class="task-time" :title="formatTimeFull(t.createdAt || t.updatedAt)">{{ formatTime(t.createdAt || t.updatedAt) }}</span>
        </div>
      </li>
    </ul>
  </div>
</template>

<script setup lang="ts">
  import { onMounted, ref } from 'vue';
  import { getPipelineList } from '../api/studio/pipeline';

  defineProps<{ activePipelineId?: number }>();
  defineEmits<{ select: [pipelineId: number] }>();

  type TaskItem = {
    id: number;
    name: string;
    currentStage: string;
    status: string;
    updatedAt?: string | number;
    createdAt?: string | number;
    creatorUserName?: string;
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

  function toDate(value?: string | number): Date | null {
    if (value === undefined || value === null || value === '') return null;
    if (typeof value === 'number') {
      // 后端 DateTime 可能序列化为 Unix ms
      const d = new Date(value > 1e12 ? value : value * 1000);
      return Number.isNaN(d.getTime()) ? null : d;
    }
    const n = Number(value);
    if (!Number.isNaN(n) && String(value).trim() !== '' && /^\d+$/.test(String(value).trim())) {
      const d = new Date(n > 1e12 ? n : n * 1000);
      return Number.isNaN(d.getTime()) ? null : d;
    }
    const d = new Date(value);
    return Number.isNaN(d.getTime()) ? null : d;
  }

  function pad(n: number) {
    return n < 10 ? `0${n}` : String(n);
  }

  /** 列表紧凑显示：同年省略年，同日显示时分 */
  function formatTime(value?: string | number): string {
    const d = toDate(value);
    if (!d) return '—';
    const now = new Date();
    const mm = pad(d.getMonth() + 1);
    const dd = pad(d.getDate());
    const hh = pad(d.getHours());
    const mi = pad(d.getMinutes());
    if (d.getFullYear() === now.getFullYear()) return `${mm}-${dd} ${hh}:${mi}`;
    return `${d.getFullYear()}-${mm}-${dd}`;
  }

  function formatTimeFull(value?: string | number): string {
    const d = toDate(value);
    if (!d) return '';
    return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())} ${pad(d.getHours())}:${pad(d.getMinutes())}:${pad(d.getSeconds())}`;
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
        createdAt: x.createdAt ?? x.CreatedAt,
        creatorUserName: String(x.creatorUserName ?? x.CreatorUserName ?? '').trim() || undefined,
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
    font-family: inherit;
    font-size: 14px;
    color: rgba(0, 0, 0, 0.85);

    .list-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      font-size: 14px;
      font-weight: 600;
      color: rgba(0, 0, 0, 0.65);
      margin-bottom: 8px;
      padding: 0 4px;
    }

    .list-empty {
      font-size: 14px;
      color: rgba(0, 0, 0, 0.45);
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
        padding: 8px 10px;
        border-radius: 4px;
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
          font-size: 14px;
          color: rgba(0, 0, 0, 0.85);
          white-space: nowrap;
          overflow: hidden;
          text-overflow: ellipsis;
        }

        .task-meta,
        .task-submeta {
          display: flex;
          justify-content: space-between;
          gap: 8px;
          font-size: 12px;
          color: rgba(0, 0, 0, 0.45);
          margin-top: 4px;
        }

        .task-creator {
          flex: 1;
          min-width: 0;
          overflow: hidden;
          text-overflow: ellipsis;
          white-space: nowrap;
        }

        .task-time {
          flex-shrink: 0;
          font-variant-numeric: tabular-nums;
        }
      }
    }
  }
</style>
