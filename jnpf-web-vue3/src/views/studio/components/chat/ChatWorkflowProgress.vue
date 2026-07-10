<template>
  <div v-if="showProgress" class="chat-workflow-progress">
    <div v-if="saEvents.length" class="wf-section">
      <div class="wf-label">EventSpec 九步</div>
      <div v-for="evt in saEvents.slice(0, 3)" :key="evt.fragmentId" class="wf-row">
        <code>{{ evt.eventId }}</code>
        <a-progress :percent="evt.percent" size="small" :status="evt.isStable ? 'success' : 'active'" />
      </div>
      <div v-if="saEvents.length > 3" class="wf-more">+{{ saEvents.length - 3 }} 个 EventSpec…</div>
    </div>
    <div v-if="runningSkills.length" class="wf-section">
      <div class="wf-label">Skill 运行中</div>
      <div v-for="s in runningSkills" :key="s.id" class="wf-skill">
        <span>{{ s.label }}</span>
        <a-tag size="small" color="processing">{{ s.percent }}%</a-tag>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
  import { computed, inject } from 'vue';
  import { ANALYST_SKILL_KEY } from '../../composables/useAnalystSkill';
  import { DESIGN_SKILL_KEY } from '../../composables/useDesignSkills';
  import { DESIGN_SKILL_IDS } from '../../api/studio/designSkills';

  const analyst = inject(ANALYST_SKILL_KEY, null);
  const designSkill = inject(DESIGN_SKILL_KEY, null);

  const saEvents = computed(() => analyst?.eventProgressList.value ?? []);
  const saTotal = computed(() => analyst?.totalProgress.value ?? 0);

  const runningSkills = computed(() => {
    const list: Array<{ id: string; label: string; percent: number }> = [];
    if (analyst?.analystLoading.value) {
      list.push({ id: 'analyst', label: '需求分析 · 三轮编排', percent: saTotal.value });
    }
    const progress = designSkill?.skillProgress.value ?? {};
    const labels: Record<string, string> = {
      [DESIGN_SKILL_IDS.architect]: '架构',
      [DESIGN_SKILL_IDS.dbDesign]: '数据库',
      [DESIGN_SKILL_IDS.uiDesign]: 'UI',
      [DESIGN_SKILL_IDS.systemDesign]: '系统设计',
    };
    for (const [id, p] of Object.entries(progress)) {
      if (p?.phase === 'running') {
        list.push({ id, label: labels[id] ?? id, percent: p.percent ?? 0 });
      }
    }
    return list;
  });

  const showProgress = computed(() => saEvents.value.length > 0 || runningSkills.value.length > 0 || (analyst?.analystLoading.value ?? false));
</script>

<style scoped lang="less">
  .chat-workflow-progress {
    margin-top: 8px;
    padding: 8px 10px;
    background: #f6ffed;
    border: 1px solid #d9f7be;
    border-radius: 6px;
    font-size: 12px;

    .wf-section {
      & + .wf-section {
        margin-top: 10px;
        padding-top: 10px;
        border-top: 1px dashed #e8e8e8;
      }
    }

    .wf-label {
      font-weight: 600;
      color: #389e0d;
      margin-bottom: 6px;
      font-size: 11px;
    }

    .wf-row {
      margin-bottom: 6px;

      code {
        font-size: 10px;
        display: block;
        margin-bottom: 2px;
      }
    }

    .wf-more {
      font-size: 11px;
      color: #8c8c8c;
    }

    .wf-skill {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 8px;
      margin-bottom: 4px;
    }
  }
</style>
