<template>
  <div class="ir-event-stream-tab">
    <div v-if="!pipelineId" class="tab-empty">
      <span class="empty-icon">📡</span>
      <p>发送消息后，IR 事件将在此实时显示</p>
    </div>
    <template v-else>
      <div class="skill-toolbar">
        <a-button size="small" type="primary" :loading="pmLoading" @click="runPmSkill">运行 PM Skill</a-button>
        <a-button size="small" :loading="analystLoading" @click="runAnalystSkill">运行 Analyst</a-button>
        <a-button size="small" type="primary" ghost :loading="designLoading" :disabled="!canRunDesign" @click="runDesignSkill"> 运行设计 Skill </a-button>
      </div>
      <div v-if="designBlockedHint" class="design-hint">{{ designBlockedHint }}</div>
      <div v-if="showDevTools" class="dev-toolbar">
        <span class="dev-label">Dev 联调</span>
        <a-button size="small" :loading="loading" @click="simulate('SkeletonCreated')">模拟 SkeletonCreated</a-button>
        <a-button size="small" :loading="loading" @click="simulate('SA_Step_Completed')">模拟 SA 步骤 +1</a-button>
        <a-button size="small" :loading="loading" type="primary" @click="simulateAllSaSteps">模拟 9 步 SA</a-button>
        <a-button size="small" :loading="loading" @click="simulate('EventSpecRevised')">模拟规格修订</a-button>
        <a-button size="small" danger :loading="loading" @click="simulateInvalid">非法 Skeleton (D7)</a-button>
      </div>
      <div v-if="error" class="tab-error">{{ error }}</div>
      <div v-if="events.length === 0 && !loading" class="tab-empty compact">
        <p>暂无事件，可使用 Dev 按钮模拟</p>
      </div>
      <div v-else class="event-list">
        <div v-for="evt in events" :key="evt.eventId" class="event-item">
          <div class="event-header">
            <a-tag color="blue">{{ evt.eventType }}</a-tag>
            <span class="event-time">{{ formatTime(evt.createdAt) }}</span>
          </div>
          <div v-if="evt.fragmentId" class="event-meta">
            <code>{{ evt.fragmentId }}</code>
            <span v-if="evt.fragmentType"> · {{ evt.fragmentType }}</span>
            <span> · v{{ evt.fragmentVersion }}</span>
          </div>
          <div v-if="evt.saStepName" class="event-step">SA: {{ evt.saStepName }}</div>
          <pre v-if="evt.payloadPreview" class="event-preview">{{ evt.payloadPreview }}</pre>
        </div>
      </div>
    </template>
  </div>
</template>

<script setup lang="ts">
  import { computed, inject } from 'vue';
  import { message } from 'ant-design-vue';
  import { IR_OBSERVATORY_KEY } from '../../composables/useIrObservatory';
  import { PM_SKILL_KEY } from '../../composables/usePmSkill';
  import { ANALYST_SKILL_KEY } from '../../composables/useAnalystSkill';
  import { DESIGN_SKILL_KEY } from '../../composables/useDesignSkills';

  const ir = inject(IR_OBSERVATORY_KEY)!;
  const pmSkill = inject(PM_SKILL_KEY)!;
  const analystSkill = inject(ANALYST_SKILL_KEY)!;
  const designSkill = inject(DESIGN_SKILL_KEY)!;

  const pipelineId = computed(() => ir.pipelineId.value);
  const events = computed(() => ir.events.value);
  const loading = computed(() => ir.loading.value);
  const error = computed(() => ir.error.value);

  const pmLoading = pmSkill.pmLoading;
  const analystLoading = analystSkill.analystLoading;
  const designLoading = designSkill.designLoading;
  const canRunDesign = designSkill.canRunDesign;

  const designBlockedHint = computed(() => {
    if (!pipelineId.value) return '';
    if (!designSkill.ir1Stable.value) return '需 IR-1 stable 后才可运行设计 Skill';
    if (budgetInfoBlocked.value) return 'LLM 预算已达 95% 预检阈值';
    return '';
  });

  const budgetInfoBlocked = computed(() => designSkill.budgetInfo.value?.canRunDesign === false);

  const showDevTools = computed(() => import.meta.env.DEV);

  async function runPmSkill() {
    if (!pipelineId.value) return;
    try {
      const res = await pmSkill.runPm();
      message.success(`PM Skill 已启动 (runId: ${res?.runId ?? '—'})`);
    } catch (e: any) {
      message.error(e?.response?.data?.msg ?? e?.message ?? 'PM Skill 启动失败');
    }
  }

  async function runAnalystSkill() {
    if (!pipelineId.value) return;
    try {
      const res = await analystSkill.runAnalyst();
      message.success(`Analyst Skill 已启动 (runId: ${res?.runId ?? '—'})`);
    } catch (e: any) {
      message.error(e?.response?.data?.msg ?? e?.message ?? 'Analyst Skill 启动失败');
    }
  }

  async function runDesignSkill() {
    if (!pipelineId.value) return;
    try {
      await designSkill.runDesign();
      message.success('设计 Skill 编排已启动（architect + db + ui → system-design）');
    } catch (e: any) {
      message.error(e?.response?.data?.msg ?? e?.message ?? designSkill.lastError.value ?? '设计 Skill 启动失败');
    }
  }

  async function simulate(type: 'SkeletonCreated' | 'SA_Step_Completed' | 'EventSpecRevised') {
    try {
      await ir.simulate(type);
    } catch (e: any) {
      message.error(e?.response?.data?.msg ?? e?.message ?? '模拟失败');
    }
  }

  async function simulateAllSaSteps() {
    try {
      await ir.simulateAllSaSteps();
      message.success('9 步 SA 已全部模拟');
    } catch (e: any) {
      message.error(e?.response?.data?.msg ?? e?.message ?? '模拟失败');
    }
  }

  async function simulateInvalid() {
    try {
      await ir.simulate('SkeletonCreated', { useInvalidPayload: true });
    } catch (e: any) {
      message.error(e?.response?.data?.msg ?? e?.message ?? 'Schema 校验应拒绝此 payload');
    }
  }

  function formatTime(iso: string) {
    if (!iso) return '';
    try {
      return new Date(iso).toLocaleTimeString();
    } catch {
      return iso;
    }
  }
</script>

<style scoped lang="less">
  .ir-event-stream-tab {
    height: 100%;
    display: flex;
    flex-direction: column;
    overflow: hidden;

    .skill-toolbar {
      display: flex;
      flex-wrap: wrap;
      gap: 8px;
      padding: 8px 0;
      border-bottom: 1px solid #e8e8e8;
      margin-bottom: 8px;
    }

    .design-hint {
      font-size: 11px;
      color: #fa8c16;
      margin-bottom: 8px;
    }

    .dev-toolbar {
      display: flex;
      flex-wrap: wrap;
      gap: 6px;
      align-items: center;
      padding: 8px 0;
      border-bottom: 1px dashed #e8e8e8;
      margin-bottom: 8px;

      .dev-label {
        font-size: 11px;
        color: #fa8c16;
        font-weight: 600;
        margin-right: 4px;
      }
    }

    .tab-empty {
      flex: 1;
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      color: #999;
      text-align: center;
      padding: 24px;

      &.compact {
        flex: 0;
        padding: 16px;
      }

      .empty-icon {
        font-size: 32px;
        margin-bottom: 8px;
      }

      p {
        margin: 0;
        font-size: 13px;
      }
    }

    .tab-error {
      padding: 8px;
      background: #fff2f0;
      border: 1px solid #ffccc7;
      border-radius: 4px;
      font-size: 12px;
      color: #cf1322;
      margin-bottom: 8px;
    }

    .event-list {
      flex: 1;
      overflow-y: auto;

      .event-item {
        padding: 10px;
        border-bottom: 1px solid #f5f5f5;
        font-size: 12px;

        .event-header {
          display: flex;
          justify-content: space-between;
          align-items: center;
          margin-bottom: 4px;
        }

        .event-time {
          color: #bbb;
          font-size: 11px;
        }

        .event-meta {
          color: #666;
          margin-bottom: 4px;

          code {
            background: #f5f5f5;
            padding: 1px 4px;
            border-radius: 3px;
          }
        }

        .event-step {
          color: #722ed1;
          margin-bottom: 4px;
        }

        .event-preview {
          margin: 4px 0 0;
          padding: 8px;
          background: #fafafa;
          border-radius: 4px;
          font-size: 11px;
          max-height: 120px;
          overflow: auto;
          white-space: pre-wrap;
          word-break: break-all;
        }
      }
    }
  }
</style>
