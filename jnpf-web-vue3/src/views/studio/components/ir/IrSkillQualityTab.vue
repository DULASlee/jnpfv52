<template>
  <div class="ir-skill-quality-tab">
    <div v-if="!pipelineId" class="tab-empty">
      <span class="empty-icon">📊</span>
      <p>创建流水线后显示 Skill 质量数据</p>
    </div>
    <template v-else>
      <!-- 整体概览 -->
      <div class="overview-row">
        <a-statistic title="Skill 数" :value="board?.totalSkills ?? 0" />
        <a-statistic
          title="整体成功率"
          :value="board ? (board.overallSuccessRate * 100).toFixed(1) : '—'"
          suffix="%"
          :value-style="{ color: overallColor }"
        />
        <a-statistic title="统计窗口" :value="board?.sinceDays ?? 30" suffix="天" />
      </div>

      <!-- Judge 校准状态卡 -->
      <div class="calibration-card">
        <div class="section-title">
          Judge 校准（Cohen's kappa）
          <a-tag v-if="calibration" :color="calibColor">{{ calibration.status }}</a-tag>
        </div>
        <div v-if="calibration" class="calib-body">
          <span v-if="calibration.kappa != null" class="kappa-value">
            κ = {{ calibration.kappa.toFixed(3) }}
          </span>
          <span class="calib-meta">
            样本 {{ calibration.sampleCount }} · 一致 {{ calibration.agreeCount }} · 分歧
            {{ calibration.disagreeCount }}
          </span>
          <p class="calib-action">{{ calibration.recommendAction }}</p>
        </div>
        <div v-else class="calib-loading">加载中…</div>
      </div>

      <!-- 质量榜表格 -->
      <div class="board-section">
        <div class="section-title">
          Skill 质量排行榜
          <a-button size="small" type="link" :loading="loading" @click="refresh">刷新</a-button>
        </div>
        <a-table
          v-if="board && board.items.length"
          :data-source="board.items"
          :columns="columns"
          :pagination="false"
          size="small"
          :row-key="(r: QualityBoardItem) => r.skillId"
        >
          <template #bodyCell="{ column, record }">
            <template v-if="column.dataIndex === 'grade'">
              <a-tag :color="gradeColor(record.grade)">{{ record.grade }}</a-tag>
            </template>
            <template v-else-if="column.dataIndex === 'successRate'">
              <span :style="{ color: rateColor(record.successRate) }">
                {{ (record.successRate * 100).toFixed(1) }}%
              </span>
            </template>
            <template v-else-if="column.dataIndex === 'avgTokens'">
              {{ record.avgTokens.toLocaleString() }}
            </template>
            <template v-else-if="column.dataIndex === 'lastRunAt'">
              {{ formatTime(record.lastRunAt) }}
            </template>
          </template>
        </a-table>
        <div v-else-if="!loading" class="empty-board">暂无 Skill 运行数据</div>
      </div>
    </template>
  </div>
</template>

<script setup lang="ts">
  import { computed, inject, onMounted, ref } from 'vue';
  import { IR_OBSERVATORY_KEY } from '../../composables/useIrObservatory';
  import {
    getSkillQualityBoard,
    getJudgeCalibration,
    type QualityBoardItem,
    type QualityBoardResult,
    type CalibrationReport,
  } from '../../api/studio/skillQuality';

  const ir = inject(IR_OBSERVATORY_KEY)!;
  const pipelineId = computed(() => ir.pipelineId.value);

  const board = ref<QualityBoardResult | null>(null);
  const calibration = ref<CalibrationReport | null>(null);
  const loading = ref(false);

  const columns = [
    { title: 'Skill', dataIndex: 'skillId', ellipsis: true },
    { title: '等级', dataIndex: 'grade', width: 60 },
    { title: '成功率', dataIndex: 'successRate', width: 90 },
    { title: '运行', dataIndex: 'totalRuns', width: 60 },
    { title: '失败', dataIndex: 'failCount', width: 60 },
    { title: '均 Token', dataIndex: 'avgTokens', width: 100 },
    { title: '最近', dataIndex: 'lastRunAt', width: 130 },
  ];

  const overallColor = computed(() => {
    const r = board.value?.overallSuccessRate ?? 0;
    if (r >= 0.95) return '#52c41a';
    if (r >= 0.8) return '#faad14';
    if (r >= 0.6) return '#fa541c';
    return '#f5222d';
  });

  const calibColor = computed(() => {
    const s = calibration.value?.status;
    if (s === 'trusted') return 'success';
    if (s === 'untrusted') return 'error';
    return 'default';
  });

  function gradeColor(grade: string) {
    return { A: 'success', B: 'warning', C: 'error', D: 'default' }[grade] ?? 'default';
  }

  function rateColor(rate: number) {
    if (rate >= 0.95) return '#52c41a';
    if (rate >= 0.8) return '#faad14';
    if (rate >= 0.6) return '#fa541c';
    return '#f5222d';
  }

  function formatTime(iso: string) {
    if (!iso) return '—';
    const d = new Date(iso);
    return `${d.getMonth() + 1}/${d.getDate()} ${String(d.getHours()).padStart(2, '0')}:${String(d.getMinutes()).padStart(2, '0')}`;
  }

  async function refresh() {
    loading.value = true;
    try {
      const [b, c] = await Promise.all([
        getSkillQualityBoard(30),
        getJudgeCalibration(10).catch(() => null),
      ]);
      board.value = b;
      calibration.value = c;
    } finally {
      loading.value = false;
    }
  }

  onMounted(() => {
    if (pipelineId.value) refresh();
  });
</script>

<style scoped lang="less">
  .ir-skill-quality-tab {
    height: 100%;
    overflow-y: auto;
    padding: 4px 0;

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

    .overview-row {
      display: flex;
      gap: 24px;
      padding: 8px 4px;
      border-bottom: 1px solid #f0f0f0;
      margin-bottom: 12px;

      :deep(.ant-statistic-title) {
        font-size: 12px;
        color: #999;
      }

      :deep(.ant-statistic-content) {
        font-size: 18px;
      }
    }

    .calibration-card {
      padding: 10px 12px;
      border: 1px solid #f0f0f0;
      border-radius: 6px;
      margin-bottom: 12px;
      background: #fafafa;

      .calib-body {
        display: flex;
        align-items: baseline;
        gap: 12px;
        flex-wrap: wrap;

        .kappa-value {
          font-size: 16px;
          font-weight: 600;
        }

        .calib-meta {
          font-size: 12px;
          color: #888;
        }

        .calib-action {
          width: 100%;
          margin: 4px 0 0;
          font-size: 11px;
          color: #666;
        }
      }

      .calib-loading {
        font-size: 12px;
        color: #999;
      }
    }

    .board-section,
    .calibration-card {
      .section-title {
        font-size: 12px;
        font-weight: 600;
        color: #666;
        margin-bottom: 8px;
        display: flex;
        align-items: center;
        justify-content: space-between;
      }
    }

    .empty-board {
      text-align: center;
      color: #999;
      font-size: 12px;
      padding: 24px;
    }
  }
</style>
