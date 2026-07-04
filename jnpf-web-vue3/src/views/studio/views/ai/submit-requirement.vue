<template>
  <div class="submit-requirement-page">
    <div class="page-body">
      <aside class="panel-left">
        <StageNavSidebar :stages="stageList" :current-stage="currentStageNum" :pipeline-id="activePipelineId" />
        <PipelineTaskList ref="taskListRef" :active-pipeline-id="activePipelineId" @select="onSelectPipeline" />
      </aside>
      <main class="panel-center">
        <AiChatPanel ref="chatPanelRef" @pipeline-id-change="onPipelineIdChange" @new-chat="onNewChat" />
      </main>
      <IrObservatoryPanel :collapsed="observatoryCollapsed" @toggle-collapse="observatoryCollapsed = !observatoryCollapsed" />
    </div>
  </div>
</template>

<script setup lang="ts">
  import { computed, nextTick, onMounted, provide, ref } from 'vue';
  import { useRoute, useRouter } from 'vue-router';
  import AiChatPanel from '../../components/AiChatPanel.vue';
  import IrObservatoryPanel from '../../components/IrObservatoryPanel.vue';
  import StageNavSidebar from '../../components/StageNavSidebar.vue';
  import PipelineTaskList from '../../components/PipelineTaskList.vue';
  import { useIrObservatory, IR_OBSERVATORY_KEY } from '../../composables/useIrObservatory';
  import { usePmSkill, PM_SKILL_KEY } from '../../composables/usePmSkill';
  import { useAnalystSkill, ANALYST_SKILL_KEY } from '../../composables/useAnalystSkill';
  import { useDesignSkills, DESIGN_SKILL_KEY } from '../../composables/useDesignSkills';

  const DEFAULT_STAGES = [
    { stage: 1, name: '需求分析', code: 'requirement' },
    { stage: 2, name: '架构设计', code: 'architecture' },
    { stage: 3, name: '总体设计', code: 'design' },
    { stage: 4, name: '自动开发', code: 'development' },
    { stage: 5, name: '交付验证', code: 'delivery' },
  ];

  const route = useRoute();
  const router = useRouter();
  const irObservatory = useIrObservatory();
  provide(IR_OBSERVATORY_KEY, irObservatory);
  provide(PM_SKILL_KEY, usePmSkill(irObservatory.pipelineId, irObservatory.snapshots, irObservatory.refreshAll));
  provide(ANALYST_SKILL_KEY, useAnalystSkill(irObservatory.pipelineId, irObservatory.snapshots, irObservatory.refreshAll));
  provide(DESIGN_SKILL_KEY, useDesignSkills(irObservatory.pipelineId, irObservatory.snapshots, irObservatory.refreshAll));

  const chatPanelRef = ref<InstanceType<typeof AiChatPanel> | null>(null);
  const taskListRef = ref<InstanceType<typeof PipelineTaskList> | null>(null);
  const observatoryCollapsed = ref(false);

  const stageList = computed(() => chatPanelRef.value?.stages ?? DEFAULT_STAGES);
  const currentStageNum = computed(() => chatPanelRef.value?.currentStage ?? 1);
  const activePipelineId = computed(() => chatPanelRef.value?.pipelineId ?? 0);

  function onPipelineIdChange(id: number) {
    irObservatory.setPipelineId(id);
    if (id > 0) {
      router.replace({ query: { ...route.query, pipelineId: String(id) } });
      taskListRef.value?.reload?.();
    }
  }

  function onNewChat() {
    router.replace({ query: {} });
  }

  async function onSelectPipeline(id: number) {
    await chatPanelRef.value?.switchPipeline(id);
    router.replace({ query: { pipelineId: String(id) } });
  }

  onMounted(async () => {
    await nextTick();
    const pid = Number(route.query.pipelineId);
    if (pid > 0) {
      await chatPanelRef.value?.switchPipeline(pid);
    }
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
    background: #f5f5f5;
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
    background: #fafafa;
    border-right: 1px solid #f0f0f0;
  }

  .panel-center {
    flex: 1;
    min-width: 0;
    overflow: hidden;
    display: flex;
    flex-direction: column;
  }

  @media (max-width: 1439px) {
    .panel-left {
      width: 200px;
      min-width: 200px;
    }
  }

  @media (max-width: 1024px) {
    .panel-left {
      display: none;
    }
  }
</style>
