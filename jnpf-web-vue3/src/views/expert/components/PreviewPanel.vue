<template>
  <div class="preview-panel">
    <!-- D-6: 双视图切换 -->
    <div class="preview-header">
      <a-radio-group v-model:value="localViewMode" size="small" @change="handleViewChange">
        <a-radio-button value="business">业务视图</a-radio-button>
        <a-radio-button value="technical" :disabled="!canViewTechnical">技术视图</a-radio-button>
      </a-radio-group>
      <a-button v-if="hasDiff" size="small" @click="emit('showDiff')">查看变更</a-button>
    </div>

    <div class="preview-content">
      <!-- 业务视图 -->
      <div v-if="localViewMode === 'business'" class="business-view">
        <div class="business-section">
          <h4>功能清单</h4>
          <ul
            ><li v-for="m in modules" :key="m">{{ m }}</li></ul
          >
        </div>
        <div class="business-section">
          <h4>数据概览</h4>
          <p>{{ tableCount }} 张数据表</p>
        </div>
        <a-empty v-if="!modules.length && !pipelineId" description="选择项目后查看" :image="Empty.PRESENTED_IMAGE_SIMPLE" />
      </div>

      <!-- 技术视图 -->
      <div v-else class="technical-view">
        <a-tabs size="small">
          <a-tab-pane key="json" tab="IR JSON">
            <pre class="ir-json">{{ formattedIR }}</pre>
          </a-tab-pane>
          <a-tab-pane key="tree" tab="组件树">
            <div ref="treeContainer" class="tree-container"></div>
          </a-tab-pane>
        </a-tabs>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
  import { ref, computed, watch, nextTick } from 'vue';
  import { Empty } from 'ant-design-vue';
  import * as echarts from 'echarts';

  defineOptions({ name: 'PreviewPanel' });
  const props = defineProps({
    pipelineId: { type: Number, default: undefined },
    viewMode: { type: String, default: 'business' },
    baseIR: { type: Object, default: () => ({}) },
    currentIR: { type: Object, default: () => ({}) },
    canViewTechnical: { type: Boolean, default: false },
    hasDiff: { type: Boolean, default: false },
  });
  const emit = defineEmits(['toggleView', 'showDiff']);

  const localViewMode = ref(props.viewMode);
  const treeContainer = ref<HTMLDivElement>();

  const modules = computed(() => {
    const ir = props.currentIR as any;
    return ir?.architecture?.modules?.map((m: any) => m.name) || ir?.modules || [];
  });
  const tableCount = computed(() => {
    const ir = props.currentIR as any;
    return ir?.architecture?.databaseDesign?.tables?.length || ir?.tables?.length || 0;
  });
  const formattedIR = computed(() => JSON.stringify(props.currentIR, null, 2));

  const handleViewChange = () => emit('toggleView', localViewMode.value);

  watch(localViewMode, async mode => {
    if (mode === 'technical') {
      await nextTick();
      renderTree();
    }
  });

  function renderTree() {
    const el = treeContainer.value;
    if (!el || !modules.value.length) return;
    const chart = echarts.init(el);
    chart.setOption({
      series: [
        {
          type: 'tree',
          data: [{ name: 'IR Root', children: modules.value.map((m: string) => ({ name: m })) }],
          top: '5%',
          left: '10%',
          bottom: '5%',
          right: '20%',
          symbolSize: 10,
          initialTreeDepth: 3,
          expandAndCollapse: true,
        },
      ],
    });
  }
</script>

<style lang="less" scoped>
  .preview-panel {
    display: flex;
    flex-direction: column;
    height: 100%;
    .preview-header {
      display: flex;
      align-items: center;
      justify-content: space-between;
      padding: 8px 12px;
      border-bottom: 1px solid #f0f0f0;
      background: #fafafa;
    }
    .preview-content {
      flex: 1;
      overflow-y: auto;
      padding: 12px;
      .business-view {
        .business-section {
          margin-bottom: 16px;
          h4 {
            margin-bottom: 8px;
          }
          ul {
            padding-left: 20px;
            li {
              line-height: 1.8;
            }
          }
        }
      }
      .technical-view {
        .ir-json {
          font-size: 12px;
          line-height: 1.4;
          white-space: pre-wrap;
        }
        .tree-container {
          height: 400px;
        }
      }
    }
  }
</style>
