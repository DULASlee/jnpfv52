<template>
  <div v-if="irData" class="ir-preview-card">
    <div class="ir-header" @click="expanded = !expanded">
      <span class="ir-icon">📋</span>
      <span class="ir-label">IR 预览</span>
      <span class="ir-summary">{{ summary }}</span>
      <span class="arrow">{{ expanded ? '▾' : '▸' }}</span>
    </div>
    <div v-show="expanded" class="ir-body">
      <div v-if="irData.pages" class="ir-section">
        <h4>页面 ({{ irData.pages.length }})</h4>
        <div v-for="page in irData.pages" :key="page.id || page.name" class="ir-page-card">
          <strong>{{ page.name || page.label }}</strong>
          <span class="page-type">{{ page.type || 'form' }}</span>
          <div v-if="page.fields" class="field-list">
            <span v-for="f in page.fields.slice(0, 5)" :key="f.model || f.name" class="field-tag">
              {{ f.label || f.model || f.name }}
            </span>
            <span v-if="page.fields.length > 5" class="more">+{{ page.fields.length - 5 }}</span>
          </div>
        </div>
      </div>
      <div v-if="irData.tables" class="ir-section">
        <h4>数据表 ({{ irData.tables.length }})</h4>
        <div v-for="t in irData.tables.slice(0, 3)" :key="t.name" class="ir-table-row">
          <strong>{{ t.name }}</strong>
          <span>{{ t.columns?.length || 0 }} columns</span>
        </div>
      </div>
      <pre v-if="!irData.pages && !irData.tables" class="raw-json">{{ prettyJSON }}</pre>
    </div>
  </div>
</template>

<script setup lang="ts">
  import { ref, computed } from 'vue';

  const props = defineProps<{ irData: any }>();

  const expanded = ref(false);

  const summary = computed(() => {
    if (!props.irData) return '';
    const parts: string[] = [];
    if (props.irData.pages?.length) parts.push(`${props.irData.pages.length} 页面`);
    if (props.irData.tables?.length) parts.push(`${props.irData.tables.length} 表`);
    return parts.join(', ') || '点击展开查看详情';
  });

  const prettyJSON = computed(() => JSON.stringify(props.irData, null, 2));
</script>

<style scoped lang="less">
  .ir-preview-card {
    border: 1px solid #d9d9d9;
    border-radius: 8px;
    overflow: hidden;
    margin: 8px 0;

    .ir-header {
      display: flex;
      align-items: center;
      gap: 8px;
      padding: 10px 12px;
      background: #fafafa;
      cursor: pointer;
      user-select: none;

      &:hover {
        background: #f0f0f0;
      }

      .ir-icon {
        font-size: 16px;
      }
      .ir-label {
        font-weight: 600;
        color: #722ed1;
      }
      .ir-summary {
        font-size: 12px;
        color: #888;
        flex: 1;
      }
      .arrow {
        color: #888;
      }
    }

    .ir-body {
      padding: 12px;
      max-height: 360px;
      overflow-y: auto;

      h4 {
        margin: 0 0 6px;
        font-size: 13px;
        color: #555;
      }

      .ir-section {
        margin-bottom: 12px;
      }

      .ir-page-card {
        padding: 8px;
        border: 1px solid #f0f0f0;
        border-radius: 4px;
        margin-bottom: 6px;

        .page-type {
          font-size: 11px;
          color: #888;
          margin-left: 8px;
        }

        .field-list {
          margin-top: 6px;
          display: flex;
          flex-wrap: wrap;
          gap: 4px;

          .field-tag {
            font-size: 11px;
            background: #f6ffed;
            color: #52c41a;
            padding: 1px 6px;
            border-radius: 4px;
          }
          .more {
            font-size: 11px;
            color: #888;
          }
        }
      }

      .ir-table-row {
        display: flex;
        justify-content: space-between;
        padding: 4px 0;
        font-size: 13px;
        color: #666;
      }

      .raw-json {
        font-size: 12px;
        background: #f5f5f5;
        padding: 8px;
        border-radius: 4px;
        white-space: pre-wrap;
        max-height: 200px;
        overflow: auto;
      }
    }
  }
</style>
