<template>
  <div class="submit-requirement-page">
    <div class="page-body">
      <aside class="panel-left" data-testid="panel-left">
        <PipelineTaskList ref="taskListRef" :active-pipeline-id="activePipelineId" @select="onSelectPipeline" />
      </aside>
      <main class="panel-center">
        <AiChatPanel ref="chatPanelRef" :show-observatory-toggle="ENABLE_OBSERVATORY" @pipeline-id-change="onPipelineIdChange" @new-chat="onNewChat" />
      </main>
      <aside v-if="ENABLE_OBSERVATORY" v-show="!panelCollapsed" class="panel-right" data-testid="panel-right">
        <IrObservatoryPanel :current-stage="currentStageNum" />
      </aside>
    </div>
  </div>
</template>

<script setup lang="ts">
  import { computed, nextTick, onMounted, onUnmounted, provide, ref } from 'vue';
  import { useRoute } from 'vue-router';
  import AiChatPanel from '../../components/AiChatPanel.vue';
  import IrObservatoryPanel from '../../components/IrObservatoryPanel.vue';
  import PipelineTaskList from '../../components/PipelineTaskList.vue';
  import { useIrObservatory, IR_OBSERVATORY_KEY } from '../../composables/useIrObservatory';
  import { usePipelineMaterials, PIPELINE_MATERIALS_KEY } from '../../composables/usePipelineMaterials';
  import { usePmSkill, PM_SKILL_KEY } from '../../composables/usePmSkill';
  import { useAnalystSkill, ANALYST_SKILL_KEY } from '../../composables/useAnalystSkill';
  import { useDesignSkills, DESIGN_SKILL_KEY } from '../../composables/useDesignSkills';
  import { useDeveloperSkill, DEVELOPER_SKILL_KEY } from '../../composables/useDeveloperSkill';
  const ENABLE_OBSERVATORY = false;

  const route = useRoute();
  const irObservatory = useIrObservatory();
  provide(IR_OBSERVATORY_KEY, irObservatory);
  provide(PM_SKILL_KEY, usePmSkill(irObservatory.pipelineId, irObservatory.snapshots, irObservatory.refreshAll));
  provide(ANALYST_SKILL_KEY, useAnalystSkill(irObservatory.pipelineId, irObservatory.snapshots, irObservatory.refreshAll, irObservatory.events));
  provide(DESIGN_SKILL_KEY, useDesignSkills(irObservatory.pipelineId, irObservatory.snapshots, irObservatory.refreshAll));
  provide(DEVELOPER_SKILL_KEY, useDeveloperSkill(irObservatory.pipelineId, irObservatory.snapshots, irObservatory.refreshAll));

  const chatPanelRef = ref<InstanceType<typeof AiChatPanel> | null>(null);
  const taskListRef = ref<InstanceType<typeof PipelineTaskList> | null>(null);

  const currentStageNum = computed(() => chatPanelRef.value?.currentStage ?? 1);
  const activePipelineId = computed(() => chatPanelRef.value?.pipelineId ?? 0);
  const panelCollapsed = computed(() => irObservatory.panelCollapsed.value);

  const pipelineMaterials = usePipelineMaterials(irObservatory.pipelineId, currentStageNum);
  provide(PIPELINE_MATERIALS_KEY, pipelineMaterials);

  /**
   * 同步地址栏 pipelineId，但禁止 router.replace。
   * layouts/page/index.vue 使用 :key="route.fullPath"：query 一变整页销毁，
   * 发送中创建流水线会清空对话/附件并掐断 SSE → 「点发送后一片空白」。
   */
  function syncPipelineQuery(id: number | null) {
    const pathOnly = route.path.startsWith('/') ? route.path : `/${route.path}`;
    const params = new URLSearchParams();
    for (const [k, v] of Object.entries(route.query)) {
      if (k === 'pipelineId' || v == null) continue;
      params.set(k, Array.isArray(v) ? String(v[0] ?? '') : String(v));
    }
    if (id && id > 0) params.set('pipelineId', String(id));
    const qs = params.toString();
    const newHash = qs ? `#${pathOnly}?${qs}` : `#${pathOnly}`;
    if (window.location.hash !== newHash) {
      window.history.replaceState(window.history.state, '', `${window.location.pathname}${window.location.search}${newHash}`);
    }
    // #region agent log
    fetch('http://127.0.0.1:7354/ingest/a6dd8c09-a41a-4bdf-b8f4-ed467f774eaa', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json', 'X-Debug-Session-Id': 'ead5d0' },
      body: JSON.stringify({
        sessionId: 'ead5d0',
        runId: 'no-remount',
        hypothesisId: 'H-remount',
        location: 'submit-requirement.vue:syncPipelineQuery',
        message: 'url-synced-without-router-replace',
        timestamp: Date.now(),
        data: { id, newHash, vueFullPath: route.fullPath },
      }),
    }).catch(() => {});
    // #endregion
  }

  function onPipelineIdChange(id: number) {
    irObservatory.setPipelineId(id);
    if (id > 0) {
      syncPipelineQuery(id);
      taskListRef.value?.reload?.();
    }
  }

  function onNewChat() {
    syncPipelineQuery(null);
    // 新对话后立即拉一次列表（轮询兜底，避免新建任务迟迟不出现）
    taskListRef.value?.reload?.();
  }

  async function onSelectPipeline(id: number) {
    await chatPanelRef.value?.switchPipeline(id);
    syncPipelineQuery(id);
  }

  onMounted(async () => {
    // #region agent log
    fetch('http://127.0.0.1:7354/ingest/a6dd8c09-a41a-4bdf-b8f4-ed467f774eaa', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json', 'X-Debug-Session-Id': 'ead5d0' },
      body: JSON.stringify({
        sessionId: 'ead5d0',
        runId: 'no-remount',
        hypothesisId: 'H-remount',
        location: 'submit-requirement.vue:onMounted',
        message: 'page-mounted',
        timestamp: Date.now(),
        data: { fullPath: route.fullPath, pipelineId: route.query.pipelineId ?? null },
      }),
    }).catch(() => {});
    // #endregion
    await nextTick();
    const pid = Number(route.query.pipelineId);
    if (pid > 0) {
      await chatPanelRef.value?.switchPipeline(pid);
    }
  });

  onUnmounted(() => {
    // #region agent log
    fetch('http://127.0.0.1:7354/ingest/a6dd8c09-a41a-4bdf-b8f4-ed467f774eaa', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json', 'X-Debug-Session-Id': 'ead5d0' },
      body: JSON.stringify({
        sessionId: 'ead5d0',
        runId: 'no-remount',
        hypothesisId: 'H-remount',
        location: 'submit-requirement.vue:onUnmounted',
        message: 'page-unmounted',
        timestamp: Date.now(),
        data: { fullPath: route.fullPath },
      }),
    }).catch(() => {});
    // #endregion
  });
</script>

<style scoped lang="less">
  .submit-requirement-page {
    position: absolute;
    top: 0;
    left: 0;
    right: 0;
    bottom: 0;
    overflow: hidden;
    display: flex;
    flex-direction: column;
    font-family: inherit;
    font-size: 14px;
    color: rgba(0, 0, 0, 0.85);
    background: #f4f7f9;
  }

  .page-body {
    flex: 1;
    display: flex;
    min-height: 0;
    overflow: hidden;
  }

  .panel-left {
    width: 240px;
    min-width: 240px;
    flex-shrink: 0;
    overflow: hidden;
    display: flex;
    flex-direction: column;
    min-height: 0;
    background: #fff;
    border-right: 1px solid #f0f0f0;

    :deep(.pipeline-task-list) {
      flex: 1;
      min-height: 0;
    }
  }

  .panel-center {
    flex: 1;
    min-width: 0;
    overflow: hidden;
    display: flex;
    flex-direction: column;
  }

  .panel-right {
    width: 340px;
    min-width: 340px;
    flex-shrink: 0;
    overflow: hidden;
    display: flex;
    flex-direction: column;
  }

  @media (max-width: 1439px) {
    .panel-left {
      width: 200px;
      min-width: 200px;
    }

    .panel-right {
      width: 300px;
      min-width: 300px;
    }
  }

  @media (max-width: 1024px) {
    .panel-left {
      display: none;
    }

    .panel-right {
      display: none;
    }
  }
</style>
