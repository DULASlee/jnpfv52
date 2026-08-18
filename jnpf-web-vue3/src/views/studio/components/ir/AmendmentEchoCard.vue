<template>
  <div class="amendment-echo-card">
    <div class="card-header">
      <span class="badge">PM 已理解你的补充要求</span>
      <a-tag color="blue">{{ understanding.severity || 'patch' }}</a-tag>
    </div>

    <p v-if="understanding.summaryMarkdown" class="summary">{{ understanding.summaryMarkdown }}</p>

    <div class="echo-grid">
      <div class="echo-block">
        <div class="block-title">功能补充</div>
        <ul v-if="understanding.features?.length">
          <li v-for="item in understanding.features" :key="item">{{ item }}</li>
        </ul>
        <span v-else class="empty">暂无明确功能项</span>
      </div>
      <div class="echo-block">
        <div class="block-title">流程影响</div>
        <ul v-if="understanding.flows?.length">
          <li v-for="item in understanding.flows" :key="item">{{ item }}</li>
        </ul>
        <span v-else class="empty">暂无明确流程影响</span>
      </div>
      <div class="echo-block">
        <div class="block-title">实体 / 表影响</div>
        <ul v-if="understanding.entitiesOrTables?.length">
          <li v-for="item in understanding.entitiesOrTables" :key="item">{{ item }}</li>
        </ul>
        <span v-else class="empty">暂无明确实体或表</span>
      </div>
    </div>

    <div v-if="understanding.patches?.length" class="echo-block patches">
      <div class="block-title">类型化补丁（将按此列表应用，确认前不改 02）</div>
      <ul>
        <li v-for="(p, idx) in understanding.patches" :key="idx">
          <code>{{ p.operation }}</code>
          <span v-if="p.target"> · {{ p.target }}</span>
          <span v-if="p.name"> / {{ p.name }}</span>
          <span v-if="p.description"> — {{ p.description }}</span>
        </li>
      </ul>
    </div>

    <div class="actions">
      <a-button size="small" type="primary" :loading="applying" :disabled="applied" @click="$emit('apply')">
        {{ applied ? '已应用' : '确认应用' }}
      </a-button>
      <span class="hint">如理解不准确，请直接在输入框继续纠正。</span>
    </div>
  </div>
</template>

<script setup lang="ts">
  import type { AmendmentUnderstanding } from '../../api/studio/skills';

  defineProps<{
    understanding: AmendmentUnderstanding;
    applying?: boolean;
    applied?: boolean;
  }>();

  defineEmits<{ apply: [] }>();
</script>

<style scoped lang="less">
  .amendment-echo-card {
    margin-top: 12px;
    padding: 12px 14px;
    border: 1px solid #91d5ff;
    border-radius: 8px;
    background: #e6f7ff;

    .card-header {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 8px;
      margin-bottom: 8px;

      .badge {
        font-weight: 600;
        color: #096dd9;
      }
    }

    .summary {
      margin: 0 0 10px;
      font-size: 13px;
      color: #262626;
      white-space: pre-wrap;
    }

    .echo-grid {
      display: grid;
      grid-template-columns: repeat(3, minmax(0, 1fr));
      gap: 8px;
    }

    .echo-block {
      padding: 8px;
      border-radius: 6px;
      background: #fff;

      .block-title {
        margin-bottom: 4px;
        font-size: 12px;
        font-weight: 600;
        color: #096dd9;
      }

      ul {
        margin: 0;
        padding-left: 16px;
        font-size: 12px;
        color: #595959;
      }

      .empty {
        font-size: 12px;
        color: #8c8c8c;
      }

      &.patches {
        margin-top: 8px;
      }
    }

    .actions {
      display: flex;
      align-items: center;
      gap: 8px;
      margin-top: 10px;

      .hint {
        font-size: 12px;
        color: #595959;
      }
    }
  }
</style>
