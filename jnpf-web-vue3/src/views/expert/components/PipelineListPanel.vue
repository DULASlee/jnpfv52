<template>
  <div class="pipeline-list-panel" :class="{ compact }">
    <div v-if="!compact" class="panel-header">
      <a-input-search v-model:value="searchKeyword" placeholder="搜索项目…" @search="emit('search', $event)" />
    </div>
    <a-spin :spinning="loading">
      <div v-if="projects.length === 0" class="empty-state">
        <a-empty description="暂无项目" />
      </div>
      <div v-else class="project-list">
        <div v-for="p in projects" :key="p.id" class="project-item" :class="{ active: p.id === activeId }" @click="emit('select', p.id)">
          <div class="item-title">{{ p.name || '未命名' }}</div>
          <div class="item-meta">
            <a-tag :color="statusColor(p.status || p.stageStatus)">{{ p.status || p.stageStatus || '—' }}</a-tag>
            <span class="item-time">{{ p.updatedAt || p.lastModifyTime || '' }}</span>
          </div>
        </div>
      </div>
    </a-spin>
  </div>
</template>

<script setup lang="ts">
  import { ref } from 'vue';

  defineOptions({ name: 'PipelineListPanel' });
  defineProps<{ projects: any[]; loading: boolean; compact?: boolean; activeId?: number }>();
  const emit = defineEmits<{ select: [id: number]; create: []; search: [keyword: string] }>();

  const searchKeyword = ref('');

  function statusColor(s: string): string {
    const map: Record<string, string> = { running: 'processing', review: 'warning', stale: 'default', completed: 'success', blocked: 'error' };
    return map[s] || 'default';
  }
</script>

<style lang="less" scoped>
  .pipeline-list-panel {
    padding: 12px;
    .panel-header {
      margin-bottom: 12px;
    }
    .empty-state {
      padding: 32px 0;
    }
    .project-list {
      .project-item {
        padding: 10px 12px;
        border-radius: 6px;
        cursor: pointer;
        margin-bottom: 4px;
        transition: background 0.15s;
        &:hover {
          background: #f5f5f5;
        }
        &.active {
          background: #e6f7ff;
          border-left: 3px solid #1890ff;
        }
        .item-title {
          font-size: 14px;
          font-weight: 500;
          margin-bottom: 4px;
          overflow: hidden;
          text-overflow: ellipsis;
          white-space: nowrap;
        }
        .item-meta {
          display: flex;
          justify-content: space-between;
          align-items: center;
          .item-time {
            font-size: 11px;
            color: #bbb;
          }
        }
      }
    }
  }
</style>
