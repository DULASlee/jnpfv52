<template>
  <div class="pipeline-task-list">
    <div class="list-header">
      <span class="list-title">我的任务{{ tasks.length > 0 ? ` (${tasks.length})` : '' }}</span>
      <a-button type="link" size="small" class="refresh-btn" :loading="loading" @click="() => loadTasks(true)">刷新</a-button>
    </div>
    <div v-if="tasks.length === 0 && !loading" class="list-empty">暂无任务</div>
    <ul v-else class="task-items">
      <li
        v-for="t in tasks"
        :key="t.id"
        class="task-item"
        :class="{ active: t.id === activePipelineId }"
        @click="$emit('select', t.id)"
      >
        <div class="task-name">{{ t.name || '未命名' }}</div>
        <div class="task-meta">
          <span>#{{ t.id }}</span>
          <span>{{ stageLabel(t.currentStage) }}</span>
        </div>
        <div class="task-submeta">
          <span class="task-creator" :title="t.creatorUserName || '未知'">{{ t.creatorUserName || '未知' }}</span>
          <span class="task-time" :title="formatTimeFull(t.createdAt || t.updatedAt)">{{
            formatTime(t.createdAt || t.updatedAt)
          }}</span>
        </div>
      </li>
    </ul>
  </div>
</template>

<script setup lang="ts">
  import { onMounted, onUnmounted, ref } from 'vue';
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

  /** 静默轮询间隔（阶段/名称变更无需手点刷新） */
  const AUTO_REFRESH_MS = 15_000;

  const tasks = ref<TaskItem[]>([]);
  const loading = ref(false);
  let pollTimer: ReturnType<typeof setInterval> | null = null;
  let inFlight = false;

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
    return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())} ${pad(d.getHours())}:${pad(d.getMinutes())}:${pad(
      d.getSeconds(),
    )}`;
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

  async function loadTasks(showSpinner = false) {
    if (inFlight) return;
    inFlight = true;
    if (showSpinner) loading.value = true;
    try {
      const res = await getPipelineList(0, 200);
      tasks.value = normalizeTasks(res);
      // #region agent log
      fetch('http://127.0.0.1:7354/ingest/a6dd8c09-a41a-4bdf-b8f4-ed467f774eaa', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', 'X-Debug-Session-Id': 'ead5d0' },
        body: JSON.stringify({
          sessionId: 'ead5d0',
          runId: 'task-list',
          hypothesisId: 'T1',
          location: 'PipelineTaskList.vue:loadTasks',
          message: 'task list refreshed',
          data: { count: tasks.value.length, showSpinner, topId: tasks.value[0]?.id ?? null },
          timestamp: Date.now(),
        }),
      }).catch(() => {});
      // #endregion
    } catch {
      if (showSpinner) tasks.value = [];
    } finally {
      loading.value = false;
      inFlight = false;
    }
  }

  function startPolling() {
    stopPolling();
    pollTimer = setInterval(() => {
      if (document.visibilityState === 'visible') loadTasks(false);
    }, AUTO_REFRESH_MS);
  }

  function stopPolling() {
    if (pollTimer != null) {
      clearInterval(pollTimer);
      pollTimer = null;
    }
  }

  function onVisibilityChange() {
    if (document.visibilityState === 'visible') {
      loadTasks(false);
      startPolling();
    } else {
      stopPolling();
    }
  }

  onMounted(() => {
    loadTasks(true);
    startPolling();
    document.addEventListener('visibilitychange', onVisibilityChange);
  });

  onUnmounted(() => {
    stopPolling();
    document.removeEventListener('visibilitychange', onVisibilityChange);
  });

  defineExpose({ reload: () => loadTasks(false) });
</script>

<style scoped lang="less">
  /* 对齐 JNPF：继承全局字体；字号 14 / 辅助 12；色用框架变量 */
  .pipeline-task-list {
    flex: 1;
    min-height: 0;
    display: flex;
    flex-direction: column;
    padding: 12px 8px;
    overflow: hidden;
    font-family: inherit;
    font-size: 14px;
    line-height: 1.5715;
    color: @text-color-base;
    background: @white;

    .list-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 8px;
      padding: 0 4px 8px;
      border-bottom: 1px solid @border-color-base;

      .list-title {
        font-size: 14px;
        font-weight: 600;
        color: @text-color-label;
      }

      .refresh-btn {
        padding: 0;
        height: auto;
        font-size: 14px;
      }
    }

    .list-empty {
      font-size: 14px;
      color: @text-color-help-dark;
      text-align: center;
      padding: 24px 8px;
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
        border-radius: 2px;
        cursor: pointer;
        margin-bottom: 2px;
        border: 1px solid transparent;
        transition: background-color 0.2s;

        &:hover {
          background: @content-bg;
        }

        &.active {
          background: fade(@primary-color, 8%);
          border-color: fade(@primary-color, 35%);
        }

        .task-name {
          font-size: 14px;
          font-weight: 400;
          color: @text-color-base;
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
          color: @text-color-help-dark;
          margin-top: 4px;
          line-height: 1.5;
        }

        .task-creator {
          flex: 1;
          min-width: 0;
          overflow: hidden;
          text-overflow: ellipsis;
          white-space: nowrap;
          color: @text-color-label;
        }

        .task-time {
          flex-shrink: 0;
          font-variant-numeric: tabular-nums;
          color: @text-color-help-dark;
        }
      }
    }
  }
</style>
