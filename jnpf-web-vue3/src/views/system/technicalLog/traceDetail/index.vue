<template>
  <div class="jnpf-content-wrapper">
    <div class="jnpf-content-wrapper-center">
      <div class="jnpf-content-wrapper-content">
        <div class="jnpf-trace-header">
          <div class="flex items-center gap-2">
            <span class="font-600">TraceId:</span>
            <span class="text-gray-600">{{ traceId }}</span>
            <a-button type="link" size="small" @click="copyTraceId">Copy</a-button>
          </div>
          <div class="flex items-center gap-4 mt-2 text-sm text-gray-400">
            <span>Total: {{ traceData.length }} entries</span>
            <span v-if="traceData.length">Duration: {{ totalDuration }}ms</span>
          </div>
        </div>
        <div class="jnpf-trace-timeline" v-if="traceData.length">
          <a-timeline>
            <a-timeline-item
              v-for="(item, index) in traceData"
              :key="index"
              :color="getTimelineColor(item.type)">
              <div class="jnpf-timeline-item">
                <div class="flex items-center gap-2 mb-1">
                  <span class="text-xs text-gray-400">{{ formatTimestamp(item.timestamp) }}</span>
                  <a-tag :color="getTagColor(item.type)" size="small">{{ item.type }}</a-tag>
                  <span v-if="item.duration" class="text-xs text-gray-500">{{ item.duration }}ms</span>
                </div>
                <div class="font-500">{{ item.message }}</div>
                <div v-if="item.details" class="jnpf-detail-box mt-1">
                  <pre>{{ item.details }}</pre>
                </div>
              </div>
            </a-timeline-item>
          </a-timeline>
        </div>
        <a-empty v-else description="No trace data found" class="mt-10" />
      </div>
    </div>
  </div>
</template>
<script lang="ts" setup>
  import { ref, computed, onMounted } from 'vue';
  import { useRoute } from 'vue-router';
  import { getTraceDetail } from '/@/api/system/technicalLog';
  import { useMessage } from '/@/hooks/web/useMessage';
  import { formatToDateTime } from '/@/utils/dateUtil';

  defineOptions({ name: 'system-trace-detail' });

  interface TraceEntry {
    timestamp: string;
    type: string;
    message: string;
    details?: string;
    duration?: number;
  }

  const route = useRoute();
  const { createMessage } = useMessage();
  const traceId = computed(() => (route.query.traceId as string) || '');
  const traceData = ref<TraceEntry[]>([]);

  const totalDuration = computed(() => {
    if (traceData.value.length < 2) return 0;
    const first = new Date(traceData.value[0].timestamp).getTime();
    const last = new Date(traceData.value[traceData.value.length - 1].timestamp).getTime();
    return last - first;
  });

  function getTimelineColor(type: string): string {
    const colorMap: Record<string, string> = {
      error: 'red',
      warning: 'orange',
      operation: 'blue',
      info: 'green',
    };
    return colorMap[type?.toLowerCase()] || 'gray';
  }

  function getTagColor(type: string): string {
    const colorMap: Record<string, string> = {
      error: 'error',
      warning: 'warning',
      operation: 'processing',
      info: 'success',
    };
    return colorMap[type?.toLowerCase()] || 'default';
  }

  function formatTimestamp(ts: string): string {
    return formatToDateTime(ts, 'YYYY-MM-DD HH:mm:ss.SSS');
  }

  function copyTraceId() {
    navigator.clipboard.writeText(traceId.value).then(() => {
      createMessage.success('TraceId copied');
    });
  }

  async function loadTraceData() {
    if (!traceId.value) {
      createMessage.warning('No TraceId provided');
      return;
    }
    try {
      const res = await getTraceDetail(traceId.value);
      traceData.value = res.data || [];
    } catch {
      traceData.value = [];
    }
  }

  onMounted(() => {
    loadTraceData();
  });
</script>
<style lang="less" scoped>
  .jnpf-trace-header {
    padding: 16px 20px;
    border-bottom: 1px solid @border-color-base;
    background: @component-background;
  }
  .jnpf-trace-timeline {
    padding: 20px;
    overflow-y: auto;
    flex: 1;
  }
  .jnpf-timeline-item {
    padding: 4px 0;
  }
  .jnpf-detail-box {
    background: #f5f5f5;
    padding: 10px 12px;
    border-radius: 4px;
    font-size: 12px;
    max-height: 200px;
    overflow-y: auto;
    pre {
      margin: 0;
      white-space: pre-wrap;
      word-break: break-all;
    }
  }
</style>
