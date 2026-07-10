<!--
  ADR-005 交互式澄清问答卡片
  ──────────────────────────────────────────────────────────────
  渲染后端下发的 ClarificationSet（单选 / 多选 / 文本补充 / 矩阵题）。
  - type=single             → JnpfRadio（单选）
  - type=multi              → JnpfCheckbox（多选）
  - type=text               → 直接 a-textarea（纯文本补充）
  - questionFormat=MATRIX_* → 矩阵表（独立行单选/多选）
  每题末项 freeText=true 时联动展开文本输入框。
  required 题未作答时禁止提交（前端预校验，后端再做硬门控）。
  contextHint 以 tooltip ℹ️ 图标展示；defaultOption 在 mount 时预填。
  底部三个出口：提交作答 / 跳过非关键题 / 全部跳过直接分析（逃生口）。
  ──────────────────────────────────────────────────────────────
-->
<template>
  <div class="clarification-card">
    <div class="clarification-header">
      <span class="clarification-badge">需求澄清 · 第 {{ set.round }} 轮</span>
      <span v-if="requiredCount > 0" class="clarification-tag">必答 {{ requiredCount }}</span>
    </div>
    <div class="clarification-title">{{ set.title }}</div>
    <div class="clarification-intro">{{ set.intro }}</div>

    <div v-for="(q, qi) in set.questions" :key="q.id" class="clarification-question">
      <div class="question-head">
        <span class="question-text">{{ qi + 1 }}. {{ q.text }}</span>
        <a-tooltip v-if="q.contextHint" :title="q.contextHint" placement="top">
          <span class="question-context-hint">ℹ️</span>
        </a-tooltip>
        <span v-if="q.required" class="question-required">必答</span>
        <span v-else class="question-optional">可选</span>
      </div>

      <!-- ═══ 矩阵题渲染 ═══ -->
      <div v-if="isMatrixQuestion(q)" class="matrix-wrapper">
        <table class="matrix-table">
          <thead>
            <tr>
              <th class="matrix-row-label-header"></th>
              <th v-for="opt in q.options" :key="opt.id" class="matrix-option-header">
                {{ opt.label }}
              </th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="row in q.matrixSubItems!" :key="row.rowId">
              <td class="matrix-row-label">{{ row.rowLabel }}</td>
              <td
                v-for="opt in q.options"
                :key="opt.id"
                class="matrix-cell"
                :class="{ 'matrix-cell-selected': isMatrixCellSelected(q, row.rowId, opt.id) }"
              >
                <!-- MATRIX_SINGLE: 每行单选 -->
                <a-radio
                  v-if="q.questionFormat === 'MATRIX_SINGLE'"
                  :checked="getMatrixSelectedOption(q.id, row.rowId) === opt.id"
                  @change="() => setMatrixOption(q, row.rowId, opt)" />
                <!-- MATRIX_MULTI: 每行多选 -->
                <a-checkbox
                  v-else-if="q.questionFormat === 'MATRIX_MULTI'"
                  :checked="getMatrixMultiSelected(q.id, row.rowId).includes(opt.id)"
                  @change="(e: any) => toggleMatrixMultiOption(q, row.rowId, opt, e.target.checked)" />
              </td>
            </tr>
          </tbody>
        </table>

        <!-- 矩阵"其他"文本框：选中 freeText=true 的行展示输入 -->
        <div
          v-for="row in q.matrixSubItems!"
          :key="'ft-' + row.rowId"
          class="matrix-freetext-row"
        >
          <template v-if="matrixRowNeedsFreeText(q, row.rowId)">
            <span class="matrix-freetext-label">{{ row.rowLabel }} · 补充说明</span>
            <a-textarea
              :model-value="getMatrixFreeText(q.id, row.rowId)"
              @update:value="(v: string) => setMatrixFreeText(q.id, row.rowId, v)"
              :rows="2"
              placeholder="请补充说明"
              class="matrix-freetext-input" />
          </template>
        </div>
      </div>

      <!-- ═══ 单选（非矩阵）═══ -->
      <JnpfRadio
        v-else-if="q.type === 'single'"
        v-model:value="answersMap[q.id].optionIds[0]"
        :options="q.options"
        :field-names="{ value: 'id', label: 'label' }"
        direction="vertical"
        @change="val => onSingleChange(q, val)" />

      <!-- ═══ 多选（非矩阵）═══ -->
      <JnpfCheckbox
        v-else-if="q.type === 'multi'"
        v-model:value="answersMap[q.id].optionIds"
        :options="q.options"
        :field-names="{ value: 'id', label: 'label' }"
        direction="vertical"
        @change="list => onMultiChange(q, list)" />

      <!-- ═══ 纯文本 ═══ -->
      <a-textarea v-else v-model:value="answersMap[q.id].freeText" :rows="2" placeholder="请输入补充说明" class="question-freetext" />

      <!-- "其他"项联动文本框（非矩阵）-->
      <a-textarea
        v-if="showFreeTextInput(q) && !isMatrixQuestion(q)"
        v-model:value="answersMap[q.id].freeText"
        :rows="2"
        placeholder="请补充说明（其他）"
        class="question-freetext other-freetext" />
    </div>

    <div class="clarification-actions">
      <a-button type="primary" :loading="submitting" :disabled="!canSubmit" @click="handleSubmit"> 提交作答 </a-button>
      <a-button v-if="set.allowSkipNonCritical && hasSkippable" :disabled="submitting" @click="handleSkipNonCritical"> 跳过可选题 </a-button>
      <a-button type="link" :disabled="submitting" @click="handleSkipAll">全部跳过直接分析</a-button>
    </div>
    <div v-if="errorMsg" class="clarification-error">{{ errorMsg }}</div>
  </div>
</template>

<script lang="ts" setup>
  import { computed, reactive, ref, onMounted } from 'vue';
  import { message } from 'ant-design-vue';
  import {
    answerClarification,
    type ClarificationQuestion,
    type ClarificationOption,
    type ClarificationSet,
    type ClarificationAnswer,
    type MatrixSubItem,
    type AnswerClarificationRequest,
  } from '/@/views/studio/api/studio/skills';

  const props = defineProps<{
    set: ClarificationSet;
    pipelineId: number;
  }>();

  const emit = defineEmits<{
    (e: 'answered', payload: { setId: string; triggerNextRound: boolean; nextAction: string; stage: string }): void;
    (e: 'skipAll'): void;
  }>();

  const submitting = ref(false);
  const errorMsg = ref('');

  // ── 非矩阵题状态：answersMap[questionId] = { optionIds: string[], freeText: string } ──
  const answersMap = reactive<Record<string, { optionIds: string[]; freeText: string }>>({});
  for (const q of props.set.questions) {
    answersMap[q.id] = { optionIds: q.type === 'multi' ? [] : [], freeText: '' };
  }

  // ── 矩阵题状态：matrixMap[questionId][rowId] = { selectedOption?: string, freeText?: string } ──
  // MATRIX_MULTI 多选用 selectedOptions: string[]，存在 selectedOption 中（逗号分隔）
  interface MatrixRowState {
    selectedOption?: string;
    selectedOptions?: string[];
    freeText?: string;
  }
  const matrixMap = reactive<Record<string, Record<string, MatrixRowState>>>({});

  onMounted(() => {
    // ── defaultOption 预填 ──
    for (const q of props.set.questions) {
      if (isMatrixQuestion(q) && q.matrixSubItems) {
        // 矩阵题 defaultOption 应用：如果设置了 defaultOption，为每行的默认选项预填
        if (q.defaultOption) {
          ensureMatrixMap(q.id);
          for (const row of q.matrixSubItems) {
            if (!matrixMap[q.id][row.rowId]) {
              matrixMap[q.id][row.rowId] = {};
            }
            if (!matrixMap[q.id][row.rowId].selectedOption) {
              matrixMap[q.id][row.rowId].selectedOption = q.defaultOption;
            }
          }
        }
      } else if (q.defaultOption && q.options.some(o => o.id === q.defaultOption)) {
        // 非矩阵题：预填 defaultOption
        if (q.type === 'multi') {
          if (!answersMap[q.id].optionIds.includes(q.defaultOption)) {
            answersMap[q.id].optionIds.push(q.defaultOption);
          }
        } else if (q.type === 'single') {
          answersMap[q.id].optionIds = [q.defaultOption];
        }
      }
    }
  });

  const requiredCount = computed(() => props.set.questions.filter(q => q.required).length);
  const hasSkippable = computed(() => props.set.questions.some(q => !q.required));

  // ── 矩阵题检测 ──
  function isMatrixQuestion(q: ClarificationQuestion): boolean {
    return !!q.questionFormat?.startsWith('MATRIX_') && !!q.matrixSubItems?.length;
  }

  // ── 矩阵状态管理 ──
  function ensureMatrixMap(qId: string) {
    if (!matrixMap[qId]) {
      matrixMap[qId] = reactive({});
    }
  }

  function getMatrixState(qId: string, rowId: string): MatrixRowState {
    ensureMatrixMap(qId);
    if (!matrixMap[qId][rowId]) {
      matrixMap[qId][rowId] = reactive({});
    }
    return matrixMap[qId][rowId];
  }

  function getMatrixSelectedOption(qId: string, rowId: string): string | undefined {
    return getMatrixState(qId, rowId).selectedOption;
  }

  function setMatrixOption(q: ClarificationQuestion, rowId: string, opt: ClarificationOption) {
    const s = getMatrixState(q.id, rowId);
    // 同格再次点击取消选中（toggle off）
    if (s.selectedOption === opt.id) {
      s.selectedOption = undefined;
    } else {
      s.selectedOption = opt.id;
    }
    // 选中的不是 freeText 项时，清空该行 freeText
    if (!opt.freeText) {
      s.freeText = undefined;
    }
  }

  function getMatrixMultiSelected(qId: string, rowId: string): string[] {
    return getMatrixState(qId, rowId).selectedOptions ?? [];
  }

  function toggleMatrixMultiOption(q: ClarificationQuestion, rowId: string, opt: ClarificationOption, checked: boolean) {
    const s = getMatrixState(q.id, rowId);
    if (!s.selectedOptions) s.selectedOptions = [];
    const idx = s.selectedOptions.indexOf(opt.id);
    if (checked && idx < 0) {
      s.selectedOptions.push(opt.id);
    } else if (!checked && idx >= 0) {
      s.selectedOptions.splice(idx, 1);
    }
  }

  function getMatrixFreeText(qId: string, rowId: string): string {
    return getMatrixState(qId, rowId).freeText ?? '';
  }

  function setMatrixFreeText(qId: string, rowId: string, text: string) {
    getMatrixState(qId, rowId).freeText = text;
  }

  function isMatrixCellSelected(q: ClarificationQuestion, rowId: string, optId: string): boolean {
    if (q.questionFormat === 'MATRIX_SINGLE') {
      return getMatrixSelectedOption(q.id, rowId) === optId;
    }
    if (q.questionFormat === 'MATRIX_MULTI') {
      return getMatrixMultiSelected(q.id, rowId).includes(optId);
    }
    return false;
  }

  function matrixRowNeedsFreeText(q: ClarificationQuestion, rowId: string): boolean {
    // 选中了 freeText=true 的选项时，展示文本框
    const selectedIds = q.questionFormat === 'MATRIX_MULTI'
      ? getMatrixMultiSelected(q.id, rowId)
      : ([getMatrixSelectedOption(q.id, rowId)].filter(Boolean) as string[]);
    return selectedIds.some(oid => q.options.find(o => o.id === oid)?.freeText);
  }

  // ── 非矩阵题交互 ──
  function showFreeTextInput(q: ClarificationQuestion): boolean {
    if (q.type === 'text') return false;
    const sel = answersMap[q.id].optionIds;
    return q.options.some(o => o.freeText && sel.includes(o.id));
  }

  function onSingleChange(q: ClarificationQuestion, val: string) {
    answersMap[q.id].optionIds = val ? [val] : [];
  }
  function onMultiChange(_q: ClarificationQuestion, list: string[]) {
    const ids = (list as unknown[]).map(x => (typeof x === 'string' ? x : (x as { id?: string })?.id ?? ''));
    answersMap[_q.id].optionIds = ids.filter(Boolean);
  }

  // ── 提交判定 ──
  const canSubmit = computed(() => {
    return props.set.questions
      .filter(q => q.required)
      .every(q => {
        if (isMatrixQuestion(q)) {
          // 矩阵必答题：至少一行有选中选项
          return (q.matrixSubItems ?? []).some(row => {
            const s = getMatrixState(q.id, row.rowId);
            if (q.questionFormat === 'MATRIX_MULTI') {
              return (s.selectedOptions?.length ?? 0) > 0;
            }
            return !!s.selectedOption;
          });
        }
        if (q.type === 'text') return !!answersMap[q.id].freeText?.trim();
        return answersMap[q.id].optionIds.length > 0;
      });
  });

  function buildAnswers(): ClarificationAnswer[] {
    return props.set.questions.map(q => {
      if (isMatrixQuestion(q)) {
        const matrixRowAnswers: MatrixSubItem[] = (q.matrixSubItems ?? []).map(row => {
          const s = getMatrixState(q.id, row.rowId);
          const selectedOption = q.questionFormat === 'MATRIX_MULTI'
            ? ((s.selectedOptions ?? []).join(',') || undefined)
            : (s.selectedOption || undefined);
          return {
            rowId: row.rowId,
            rowLabel: row.rowLabel,
            selectedOption,
            freeText: s.freeText?.trim() || undefined,
          };
        });
        return {
          questionId: q.id,
          optionIds: [],
          matrixRowAnswers,
        };
      }
      const a = answersMap[q.id];
      return {
        questionId: q.id,
        optionIds: a.optionIds.slice(),
        freeText: a.freeText?.trim() ? a.freeText.trim() : undefined,
      };
    });
  }

  async function submit(skipAll = false, skipNonCritical = false): Promise<void> {
    submitting.value = true;
    errorMsg.value = '';
    try {
      const payload: AnswerClarificationRequest = {
        setId: props.set.setId,
        answers: skipAll ? [] : buildAnswers(),
        skippedQuestionIds: skipNonCritical ? props.set.questions.filter(q => !q.required).map(q => q.id) : [],
        skipAll,
      };
      const res = await answerClarification(props.pipelineId, payload);
      message.success(skipAll ? '已跳过，开始分析' : '已提交作答');
      emit('answered', {
        setId: props.set.setId,
        triggerNextRound: res.triggerNextRound,
        nextAction: res.nextAction,
        stage: res.stage,
      });
    } catch (e: any) {
      errorMsg.value = e?.message || '提交失败，请重试';
    } finally {
      submitting.value = false;
    }
  }

  function handleSubmit() {
    if (!canSubmit.value) {
      errorMsg.value = '请完成所有必答问题';
      return;
    }
    submit(false, false);
  }
  function handleSkipNonCritical() {
    submit(false, true);
  }
  function handleSkipAll() {
    submit(true, false);
    emit('skipAll');
  }
</script>

<style lang="less" scoped>
  .clarification-card {
    margin: 8px 0;
    padding: 16px;
    background: #f6f9ff;
    border: 1px solid #d6e4ff;
    border-radius: 8px;
  }
  .clarification-header {
    display: flex;
    align-items: center;
    gap: 8px;
    margin-bottom: 8px;
  }
  .clarification-badge {
    font-size: 12px;
    font-weight: 600;
    color: #2f54eb;
    background: #e6f4ff;
    padding: 2px 8px;
    border-radius: 4px;
  }
  .clarification-tag {
    font-size: 12px;
    color: #d4380d;
    background: #fff2e8;
    padding: 2px 8px;
    border-radius: 4px;
  }
  .clarification-title {
    font-size: 15px;
    font-weight: 600;
    color: #1f1f1f;
    margin-bottom: 4px;
  }
  .clarification-intro {
    font-size: 13px;
    color: #595959;
    margin-bottom: 12px;
    line-height: 1.6;
  }
  .clarification-question {
    margin-bottom: 14px;
    padding: 10px 12px;
    background: #fff;
    border-radius: 6px;
    border: 1px solid #f0f0f0;
  }
  .question-head {
    display: flex;
    align-items: center;
    gap: 8px;
    margin-bottom: 8px;
  }
  .question-text {
    font-size: 14px;
    color: #262626;
    font-weight: 500;
  }
  .question-context-hint {
    font-size: 14px;
    color: #2f54eb;
    cursor: help;
    line-height: 1;
  }
  .question-required {
    font-size: 11px;
    color: #d4380d;
    background: #fff2e8;
    padding: 1px 6px;
    border-radius: 3px;
  }
  .question-optional {
    font-size: 11px;
    color: #8c8c8c;
    background: #f5f5f5;
    padding: 1px 6px;
    border-radius: 3px;
  }
  .question-freetext {
    margin-top: 8px;
  }
  .other-freetext {
    border-left: 2px solid #2f54eb;
  }
  .clarification-actions {
    display: flex;
    align-items: center;
    gap: 8px;
    margin-top: 12px;
    flex-wrap: wrap;
  }
  .clarification-error {
    margin-top: 8px;
    font-size: 12px;
    color: #d4380d;
  }

  // ── 矩阵题样式 ──
  .matrix-wrapper {
    margin-top: 6px;
    overflow-x: auto;
  }
  .matrix-table {
    width: 100%;
    border-collapse: collapse;
    font-size: 13px;
    border: 1px solid #e8e8e8;
  }
  .matrix-table th,
  .matrix-table td {
    padding: 8px 10px;
    text-align: center;
    border: 1px solid #e8e8e8;
  }
  .matrix-row-label-header {
    min-width: 100px;
    background: #fafafa;
    font-weight: 600;
    text-align: left;
  }
  .matrix-option-header {
    background: #fafafa;
    font-weight: 500;
    color: #595959;
    min-width: 70px;
  }
  .matrix-row-label {
    text-align: left;
    font-weight: 500;
    color: #1f1f1f;
    background: #fafafa;
  }
  .matrix-cell {
    cursor: pointer;
    transition: background 0.15s;
  }
  .matrix-cell:hover {
    background: #e6f4ff;
  }
  .matrix-cell-selected {
    background: #e6f4ff;
  }
  .matrix-freetext-row {
    display: flex;
    align-items: flex-start;
    gap: 8px;
    margin-top: 6px;
    padding: 6px 8px;
    background: #fffbe6;
    border-radius: 4px;
    border: 1px solid #ffe58f;
  }
  .matrix-freetext-label {
    flex-shrink: 0;
    font-size: 12px;
    color: #ad6800;
    font-weight: 500;
    min-width: 80px;
    padding-top: 4px;
  }
  .matrix-freetext-input {
    flex: 1;
    min-width: 200px;
  }
</style>
