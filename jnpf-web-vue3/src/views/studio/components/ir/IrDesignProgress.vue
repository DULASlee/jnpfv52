<template>
  <div class="ir-design-progress">
    <div v-if="!pipelineId" class="tab-empty">
      <span class="empty-icon">🏗️</span>
      <p>需求分析 Finalize 后可运行设计四 Skill</p>
    </div>
    <template v-else>
      <div v-if="!canRunDesignGate" class="gate-hint">
        <a-alert type="info" show-icon message="请先完成三轮需求分析（finalized=true + ai_entity_field），再运行设计 Skill" />
      </div>
      <div v-else class="dag-section">
        <div class="section-title">设计 Skill DAG</div>
        <div class="parallel-row">
          <div v-for="skillId in parallelSkills" :key="skillId" class="skill-node" :class="phaseClass(skillId)">
            <div class="node-label">{{ designSkill.skillLabel(skillId) }}</div>
            <a-tag :color="phaseColor(skillId)">{{ phaseText(skillId) }}</a-tag>
            <div v-if="progressMessage(skillId)" class="node-msg">{{ progressMessage(skillId) }}</div>
          </div>
        </div>
        <div class="dag-connector">↓ 三片段 stable 后</div>
        <div class="serial-row">
          <div class="skill-node" :class="phaseClass(systemSkillId)">
            <div class="node-label">{{ designSkill.skillLabel(systemSkillId) }}</div>
            <a-tag :color="phaseColor(systemSkillId)">{{ phaseText(systemSkillId) }}</a-tag>
          </div>
        </div>
      </div>
      <div v-if="designComplete" class="complete-banner">
        <a-tag color="gold">IR-2 locked</a-tag>
        <span>SystemDesign 已锁定</span>
      </div>
      <div v-if="constraintCritical > 0 || constraintWarning > 0" class="constraint-banner">
        <a-tag v-if="constraintCritical > 0" color="error">critical {{ constraintCritical }}</a-tag>
        <a-tag v-if="constraintWarning > 0" color="warning">warning {{ constraintWarning }}</a-tag>
        <span>约束引擎已报告违规</span>
      </div>
    </template>
  </div>
</template>

<script setup lang="ts">
  import { computed, inject } from 'vue';
  import { IR_OBSERVATORY_KEY } from '../../composables/useIrObservatory';
  import { DESIGN_SKILL_KEY } from '../../composables/useDesignSkills';
  import { DESIGN_SKILL_IDS } from '../../api/studio/designSkills';

  const ir = inject(IR_OBSERVATORY_KEY)!;
  const designSkill = inject(DESIGN_SKILL_KEY)!;

  const pipelineId = computed(() => ir.pipelineId.value);
  const canRunDesignGate = computed(
    () => designSkill.analysisFinalized.value && designSkill.hasEntityFields.value,
  );
  const designComplete = designSkill.designComplete;
  const constraintCritical = designSkill.constraintCritical;
  const constraintWarning = designSkill.constraintWarning;

  const parallelSkills = [DESIGN_SKILL_IDS.architect, DESIGN_SKILL_IDS.dbDesign, DESIGN_SKILL_IDS.uiDesign];
  const systemSkillId = DESIGN_SKILL_IDS.systemDesign;

  function phaseClass(skillId: string) {
    return `phase-${designSkill.phaseForSkill(skillId)}`;
  }

  function phaseColor(skillId: string) {
    const phase = designSkill.phaseForSkill(skillId);
    const map: Record<string, string> = {
      pending: 'default',
      running: 'processing',
      completed: 'blue',
      stable: 'success',
      failed: 'error',
    };
    return map[phase] ?? 'default';
  }

  function phaseText(skillId: string) {
    const phase = designSkill.phaseForSkill(skillId);
    const map: Record<string, string> = {
      pending: '待运行',
      running: '运行中',
      completed: '已完成',
      stable: 'stable',
      failed: '失败',
    };
    return map[phase] ?? phase;
  }

  function progressMessage(skillId: string) {
    const p = designSkill.skillProgress.value[skillId];
    if (!p?.message) return '';
    return p.percent != null ? `${p.percent}% · ${p.message}` : p.message;
  }
</script>

<style scoped lang="less">
  .ir-design-progress {
    .tab-empty {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      padding: 24px;
      color: #999;
      text-align: center;

      .empty-icon {
        font-size: 28px;
        margin-bottom: 8px;
      }

      p {
        margin: 0;
        font-size: 13px;
      }
    }

    .gate-hint {
      margin-bottom: 12px;
    }

    .section-title {
      font-size: 12px;
      font-weight: 600;
      color: #666;
      margin-bottom: 10px;
    }

    .parallel-row {
      display: flex;
      flex-direction: column;
      gap: 8px;
    }

    .serial-row {
      margin-top: 4px;
    }

    .skill-node {
      padding: 10px 12px;
      border: 1px solid #f0f0f0;
      border-radius: 6px;
      background: #fafafa;

      &.phase-running {
        border-color: #1890ff;
        background: #e6f7ff;
      }

      &.phase-stable,
      &.phase-completed {
        border-color: #b7eb8f;
        background: #f6ffed;
      }

      &.phase-failed {
        border-color: #ffa39e;
        background: #fff2f0;
      }

      .node-label {
        font-size: 13px;
        font-weight: 600;
        margin-bottom: 4px;
      }

      .node-msg {
        font-size: 11px;
        color: #666;
        margin-top: 4px;
      }
    }

    .dag-connector {
      text-align: center;
      font-size: 11px;
      color: #999;
      padding: 6px 0;
    }

    .complete-banner {
      display: flex;
      align-items: center;
      gap: 8px;
      margin-top: 12px;
      padding: 8px 10px;
      background: #fffbe6;
      border-radius: 6px;
      font-size: 12px;
    }

    .constraint-banner {
      display: flex;
      align-items: center;
      gap: 8px;
      margin-top: 8px;
      padding: 8px 10px;
      background: #fff2f0;
      border-radius: 6px;
      font-size: 12px;
    }
  }
</style>
