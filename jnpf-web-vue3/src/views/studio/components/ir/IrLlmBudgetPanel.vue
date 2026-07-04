<template>
  <div class="ir-llm-budget-panel">
    <div class="section-title">LLM 预算（阶段三）</div>
    <div v-if="!pipelineId" class="hint">选择流水线后显示 Token 用量</div>
    <template v-else>
      <a-spin :spinning="budgetLoading">
        <div v-if="budgetInfo" class="budget-summary">
          <a-progress
            :percent="usagePercent"
            :status="progressStatus"
            size="small"
            :format="() => `${formatK(budgetInfo!.tokenConsumed)} / ${formatK(budgetInfo!.tokenBudget)}`" />
          <div class="budget-meta">
            <span>剩余 {{ formatK(budgetInfo.tokenRemaining) }}</span>
            <a-tag :color="budgetTagColor">{{ budgetInfo.budgetStatus }}</a-tag>
          </div>
          <div v-if="!budgetInfo.canRunDesign" class="budget-warn"> 已达 95% 预检阈值，design/run 将被拒绝（LLM_BUDGET_EXHAUSTED） </div>
        </div>
        <div v-else class="hint">预算 API 未就绪或 DDL 未迁移</div>

        <div v-if="recentCalls.length" class="call-list">
          <div class="call-title">最近 LLM 调用</div>
          <div v-for="(c, i) in recentCalls" :key="i" class="call-row">
            <span class="call-skill">{{ c.skillId || '—' }}</span>
            <span class="call-tokens"> {{ (c.promptTokens ?? 0) + (c.completionTokens ?? 0) }} tok </span>
            <span class="call-model">{{ shortModel(c.model) }}</span>
          </div>
        </div>
      </a-spin>
      <a-button size="small" class="refresh-btn" :loading="budgetLoading" @click="refreshBudget">刷新</a-button>
    </template>
  </div>
</template>

<script setup lang="ts">
  import { computed, inject } from 'vue';
  import { IR_OBSERVATORY_KEY } from '../../composables/useIrObservatory';
  import { DESIGN_SKILL_KEY } from '../../composables/useDesignSkills';

  const ir = inject(IR_OBSERVATORY_KEY)!;
  const designSkill = inject(DESIGN_SKILL_KEY)!;

  const pipelineId = computed(() => ir.pipelineId.value);
  const budgetInfo = designSkill.budgetInfo;
  const budgetLoading = designSkill.budgetLoading;

  const usagePercent = computed(() => {
    if (!budgetInfo.value?.tokenBudget) return 0;
    return Math.min(100, Math.round((budgetInfo.value.tokenConsumed / budgetInfo.value.tokenBudget) * 100));
  });

  const progressStatus = computed(() => {
    const s = budgetInfo.value?.budgetStatus;
    if (s === 'exhausted' || s === 'red') return 'exception';
    if (s === 'yellow') return 'active';
    return 'normal';
  });

  const budgetTagColor = computed(() => {
    const s = budgetInfo.value?.budgetStatus;
    if (s === 'exhausted') return 'error';
    if (s === 'yellow') return 'warning';
    return 'success';
  });

  const recentCalls = computed(() => budgetInfo.value?.recentCalls?.slice(0, 8) ?? []);

  function formatK(n: number) {
    if (n >= 1_000_000) return `${(n / 1_000_000).toFixed(1)}M`;
    if (n >= 1000) return `${(n / 1000).toFixed(1)}K`;
    return String(n);
  }

  function shortModel(model?: string) {
    if (!model) return '';
    const parts = model.split('/');
    return parts[parts.length - 1]?.slice(0, 16) ?? model;
  }

  async function refreshBudget() {
    await designSkill.loadBudget();
  }
</script>

<style scoped lang="less">
  .ir-llm-budget-panel {
    margin-top: 16px;
    padding: 10px;
    border: 1px solid #e8e8e8;
    border-radius: 6px;
    background: #fafafa;

    .section-title {
      font-size: 12px;
      font-weight: 600;
      color: #666;
      margin-bottom: 10px;
    }

    .hint {
      font-size: 12px;
      color: #999;
    }

    .budget-summary {
      .budget-meta {
        display: flex;
        justify-content: space-between;
        align-items: center;
        margin-top: 6px;
        font-size: 11px;
        color: #666;
      }

      .budget-warn {
        margin-top: 8px;
        font-size: 11px;
        color: #cf1322;
      }
    }

    .call-list {
      margin-top: 12px;
      border-top: 1px dashed #e8e8e8;
      padding-top: 8px;

      .call-title {
        font-size: 11px;
        font-weight: 600;
        color: #888;
        margin-bottom: 6px;
      }

      .call-row {
        display: flex;
        gap: 8px;
        font-size: 11px;
        padding: 3px 0;
        border-bottom: 1px solid #f5f5f5;

        .call-skill {
          flex: 1;
          color: #722ed1;
        }

        .call-tokens {
          color: #666;
        }

        .call-model {
          color: #bbb;
          max-width: 80px;
          overflow: hidden;
          text-overflow: ellipsis;
        }
      }
    }

    .refresh-btn {
      margin-top: 8px;
    }
  }
</style>
