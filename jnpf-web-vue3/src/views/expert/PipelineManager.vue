<template>
  <div class="pipeline-manager" :class="`breakpoint-${breakpoint}`">
    <!-- xl: 完整三栏 | md: 顶部折叠栏 | sm: 抽屉式 -->
    <template v-if="breakpoint === 'xl'">
      <PipelineListPanel class="pm-left" :projects="projects" :loading="loading" @select="handleSelect" @create="handleCreate" />
      <AiChatPanel class="pm-center" :pipeline-id="selectedId" @message="handleMessage" />
      <PreviewPanel
        class="pm-right"
        :pipeline-id="selectedId"
        :view-mode="viewMode"
        :base-ir="baseIR"
        :current-ir="currentIR"
        @toggle-view="handleToggleView"
        @show-diff="showDiffViewer = true" />
    </template>

    <template v-if="breakpoint === 'md'">
      <div class="pm-topbar">
        <a-select v-model:value="selectedId" :options="projectOptions" placeholder="选择项目…" style="width: 260px" @change="handleSelect" />
        <a-button type="primary" size="small" @click="handleCreate"><PlusOutlined /> 新建</a-button>
      </div>
      <div class="pm-md-body">
        <AiChatPanel class="pm-md-chat" :pipeline-id="selectedId" @message="handleMessage" />
        <PreviewPanel
          class="pm-md-preview"
          :pipeline-id="selectedId"
          :view-mode="viewMode"
          :base-ir="baseIR"
          :current-ir="currentIR"
          @toggle-view="handleToggleView"
          @show-diff="showDiffViewer = true" />
      </div>
    </template>

    <template v-if="breakpoint === 'sm'">
      <div class="pm-sm-topbar">
        <a-button @click="drawerOpen = true"><MenuOutlined /> 项目列表</a-button>
        <a-radio-group v-model:value="smTab" size="small">
          <a-radio-button value="chat">对话</a-radio-button>
          <a-radio-button value="preview">预览</a-radio-button>
        </a-radio-group>
      </div>
      <AiChatPanel v-if="smTab === 'chat'" class="pm-sm-chat" :pipeline-id="selectedId" @message="handleMessage" />
      <PreviewPanel
        v-if="smTab === 'preview'"
        class="pm-sm-preview"
        :pipeline-id="selectedId"
        :view-mode="viewMode"
        :base-ir="baseIR"
        :current-ir="currentIR"
        @toggle-view="handleToggleView"
        @show-diff="showDiffViewer = true" />
      <a-drawer v-model:visible="drawerOpen" title="项目列表" placement="left" :width="280">
        <PipelineListPanel :projects="projects" :loading="loading" compact @select="handleSelectDrawer" @create="handleCreate" />
      </a-drawer>
    </template>

    <!-- 底部上下文面板 -->
    <div class="pm-context-bar">
      <a-space :size="16">
        <span class="ctx-item"
          >阶段: <a-tag>{{ stageLabel }}</a-tag></span
        >
        <span class="ctx-item">Token: {{ tokensUsed }}</span>
        <span class="ctx-item">耗时: {{ elapsed }}</span>
        <span class="ctx-item"
          >失败: <a-tag v-if="failureCount" color="error">{{ failureCount }}</a-tag
          ><span v-else>0</span></span
        >
      </a-space>
    </div>

    <!-- IR 变更对比弹窗 -->
    <a-modal v-model:visible="showDiffViewer" title="IR 变更对比" width="90%" :footer="null" destroy-on-close>
      <div class="diff-container">
        <div class="diff-panel"
          ><h4>原始 IR</h4><pre>{{ JSON.stringify(baseIR, null, 2) }}</pre>
        </div>
        <div class="diff-panel"
          ><h4>当前 IR</h4><pre>{{ JSON.stringify(currentIR, null, 2) }}</pre>
        </div>
      </div>
    </a-modal>
  </div>
</template>

<script setup lang="ts">
  import { ref, computed, onMounted, onUnmounted } from 'vue';
  import { useRouter } from 'vue-router';
  import { PlusOutlined, MenuOutlined } from '@ant-design/icons-vue';
  import { getPipelineList } from '/@/api/founder/pipeline';
  import PipelineListPanel from './components/PipelineListPanel.vue';
  import AiChatPanel from './components/AiChatPanel.vue';
  import PreviewPanel from './components/PreviewPanel.vue';
  import { useUserStoreWithOut } from '/@/store/modules/user';

  defineOptions({ name: 'PipelineManager' });

  const router = useRouter();
  const userStore = useUserStoreWithOut();

  // 响应式断点
  const breakpoint = ref<'xl' | 'md' | 'sm'>('xl');
  const updateBreakpoint = () => {
    const w = window.innerWidth;
    if (w >= 1400) breakpoint.value = 'xl';
    else if (w >= 1024) breakpoint.value = 'md';
    else breakpoint.value = 'sm';
  };

  // 数据
  const loading = ref(false);
  const projects = ref<any[]>([]);
  const selectedId = ref<number>();
  const stageLabel = ref('—');
  const tokensUsed = ref(0);
  const elapsed = ref('—');
  const failureCount = ref(0);

  // 双视图
  const viewMode = ref<'business' | 'technical'>('business');
  const baseIR = ref<any>({});
  const currentIR = ref<any>({});

  // sm 断点
  const drawerOpen = ref(false);
  const smTab = ref<'chat' | 'preview'>('chat');

  // Diff 弹窗
  const showDiffViewer = ref(false);

  const projectOptions = computed(() => projects.value.map((p: any) => ({ label: p.name || `项目 #${p.id}`, value: p.id })));

  const canViewTechnical = computed(() => {
    const info = userStore.getUserInfo;
    return info?.isAdministrator || info?.roleIds?.includes('founder') || info?.roleIds?.includes('developer');
  });

  const handleToggleView = (mode: 'business' | 'technical') => {
    if (mode === 'technical' && !canViewTechnical.value) return;
    viewMode.value = mode;
  };

  const fetchProjects = async () => {
    loading.value = true;
    try {
      const res = await getPipelineList(0, 50);
      projects.value = res.data?.list || [];
    } catch {
      projects.value = [];
    } finally {
      loading.value = false;
    }
  };

  const handleSelect = (id: number) => {
    selectedId.value = id;
  };
  const handleSelectDrawer = (id: number) => {
    selectedId.value = id;
    drawerOpen.value = false;
  };
  const handleCreate = () => router.push('/studio/expert/quick-app-entry');
  const handleMessage = () => {
    /* SSE streaming placeholder */
  };

  onMounted(() => {
    updateBreakpoint();
    window.addEventListener('resize', updateBreakpoint);
    fetchProjects();
    if (!canViewTechnical.value) viewMode.value = 'business';
  });

  onUnmounted(() => {
    window.removeEventListener('resize', updateBreakpoint);
  });
</script>

<style lang="less" scoped>
  .pipeline-manager {
    display: flex;
    height: 100%;
    position: relative;

    &.breakpoint-xl {
      .pm-left {
        width: 280px;
        flex-shrink: 0;
        border-right: 1px solid #f0f0f0;
        overflow-y: auto;
      }
      .pm-center {
        flex: 1;
        min-width: 0;
      }
      .pm-right {
        width: 420px;
        flex-shrink: 0;
        border-left: 1px solid #f0f0f0;
        overflow-y: auto;
      }
    }

    &.breakpoint-md {
      flex-direction: column;
      .pm-topbar {
        display: flex;
        align-items: center;
        gap: 12px;
        padding: 8px 16px;
        background: #fafafa;
        border-bottom: 1px solid #f0f0f0;
        height: 48px;
        flex-shrink: 0;
      }
      .pm-md-body {
        flex: 1;
        display: flex;
        min-height: 0;
        .pm-md-chat {
          flex: 1;
          min-width: 0;
        }
        .pm-md-preview {
          width: 320px;
          flex-shrink: 0;
          border-left: 1px solid #f0f0f0;
          overflow-y: auto;
        }
      }
    }

    &.breakpoint-sm {
      flex-direction: column;
      .pm-sm-topbar {
        display: flex;
        align-items: center;
        justify-content: space-between;
        padding: 8px 12px;
        background: #fafafa;
        border-bottom: 1px solid #f0f0f0;
        height: 48px;
        flex-shrink: 0;
      }
      .pm-sm-chat,
      .pm-sm-preview {
        flex: 1;
        min-height: 0;
        overflow-y: auto;
      }
    }

    .pm-context-bar {
      position: fixed;
      bottom: 0;
      left: 0;
      right: 0;
      height: 40px;
      display: flex;
      align-items: center;
      padding: 0 16px;
      background: #fff;
      border-top: 1px solid #f0f0f0;
      z-index: 10;

      .ctx-item {
        font-size: 12px;
        color: #595959;
      }
    }
  }
</style>
