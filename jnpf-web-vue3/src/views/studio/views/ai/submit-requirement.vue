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
  import { computed, nextTick, onMounted, provide, ref } from 'vue';
  import { useRoute, useRouter } from 'vue-router';
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
  const router = useRouter();
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
    min-height: 0;
    background: #fafafa;
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
