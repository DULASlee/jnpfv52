<template>
  <div class="ir-stability-tab">
    <div v-if="!pipelineId" class="tab-empty">
      <span class="empty-icon">🛡️</span>
      <p>SA 编译稳定性门控进度将在此展示</p>
    </div>
    <template v-else>
      <div v-if="skeletonSnapshots.length" class="section">
        <div class="section-title">IR-0 骨架</div>
        <div v-for="snap in skeletonSnapshots" :key="snap.fragmentId" class="summary-card">
          <div class="card-title">{{ snap.fragmentType || snap.fragmentId }}</div>
          <a-badge :status="badgeStatus(snap.stabilityState)" :text="snap.stabilityState" />
        </div>
      </div>
      <div class="section">
        <div class="section-title">约束门控（ConstraintEngine）</div>
        <div class="constraint-row">
          <a-tag v-if="constraintCritical > 0" color="error">critical {{ constraintCritical }}</a-tag>
          <a-tag v-else-if="constraintWarning > 0" color="warning">warning {{ constraintWarning }}</a-tag>
          <a-tag v-else color="success">通过</a-tag>
          <a-button size="small" :loading="constraintChecking" :disabled="!pipelineId || ir2Count === 0" @click="runConstraintCheck"> 校验约束 </a-button>
        </div>
        <ul v-if="violations.length" class="violation-list">
          <li v-for="(v, i) in violations.slice(0, 5)" :key="i" :class="v.severity">
            <code>{{ v.ruleId }}</code> {{ v.message }}
          </li>
        </ul>
      </div>
      <div class="section">
        <div class="section-title">设计 Skill 进度（IR-2）</div>
        <IrDesignProgress />
      </div>
      <div class="section">
        <div class="section-title">EventSpec 编译进度</div>
        <IrSaEventProgress :events="saEvents" :steps="analyst.SA_STEPS" :total-percent="saTotal" />
      </div>
    </template>
  </div>
</template>

<script setup lang="ts">
  import { computed, inject } from 'vue';
  import { IR_OBSERVATORY_KEY } from '../../composables/useIrObservatory';
  import { ANALYST_SKILL_KEY } from '../../composables/useAnalystSkill';
  import { DESIGN_SKILL_KEY } from '../../composables/useDesignSkills';
  import type { IrFragmentSnapshot } from '../../types/ir';
  import IrSaEventProgress from './IrSaEventProgress.vue';
  import IrDesignProgress from './IrDesignProgress.vue';

  const ir = inject(IR_OBSERVATORY_KEY)!;
  const analyst = inject(ANALYST_SKILL_KEY)!;
  const designSkill = inject(DESIGN_SKILL_KEY)!;

  const pipelineId = computed(() => ir.pipelineId.value);
  const constraintCritical = designSkill.constraintCritical;
  const constraintWarning = designSkill.constraintWarning;
  const constraintChecking = designSkill.constraintChecking;
  const violations = computed(() => designSkill.violations.value);
  const ir2Count = computed(() => designSkill.ir2Snapshots.value.length);
  const skeletonSnapshots = computed(() => ir.snapshots.value.filter(s => s.fragmentType?.startsWith('IR0') || s.fragmentId?.startsWith('skeleton:')));

  const saEvents = computed(() => analyst.eventProgressList.value);
  const saTotal = computed(() => analyst.totalProgress.value);

  function badgeStatus(state: IrFragmentSnapshot['stabilityState']) {
    const map = { draft: 'default', 'in-progress': 'processing', stable: 'success', locked: 'warning' } as const;
    return map[state] || 'default';
  }

  async function runConstraintCheck() {
    await designSkill.checkConstraints(true, true);
  }
</script>

<style scoped lang="less">
  .ir-stability-tab {
    height: 100%;
    overflow-y: auto;
    padding: 0 4px;

    .tab-empty {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      height: 100%;
      color: #999;
      text-align: center;
      padding: 24px;

      .empty-icon {
        font-size: 32px;
        margin-bottom: 8px;
      }

      p {
        margin: 0;
        font-size: 13px;
      }
    }

    .section {
      margin-bottom: 16px;

      .section-title {
        font-size: 12px;
        font-weight: 600;
        color: #666;
        margin-bottom: 8px;
      }
    }

    .summary-card {
      padding: 10px 12px;
      border: 1px solid #f0f0f0;
      border-radius: 6px;
      margin-bottom: 8px;

      .card-title {
        font-size: 13px;
        font-weight: 600;
        margin-bottom: 6px;
      }
    }

    .constraint-row {
      display: flex;
      align-items: center;
      gap: 8px;
      margin-bottom: 8px;
    }

    .violation-list {
      margin: 0;
      padding-left: 16px;
      font-size: 11px;
      color: #666;

      li {
        margin-bottom: 4px;

        &.critical {
          color: #cf1322;
        }

        code {
          background: #f5f5f5;
          padding: 1px 4px;
          border-radius: 3px;
        }
      }
    }
  }
</style>
