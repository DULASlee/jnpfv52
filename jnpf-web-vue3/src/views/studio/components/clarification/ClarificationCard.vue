<!--
  ADR-005 交互式澄清问答卡片
  ──────────────────────────────────────────────────────────────
  渲染后端下发的 ClarificationSet（单选 / 多选 / 文本补充）。
  - type=single → JnpfRadio（单选）
  - type=multi  → JnpfCheckbox（多选）
  - type=text   → 直接 a-textarea（纯文本补充）
  每题末项 freeText=true 时联动展开文本输入框。
  required 题未作答时禁止提交（前端预校验，后端再做硬门控）。
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
        <span v-if="q.required" class="question-required">必答</span>
        <span v-else class="question-optional">可选</span>
      </div>

      <!-- 单选 -->
      <JnpfRadio
        v-if="q.type === 'single'"
        v-model:value="answersMap[q.id].optionIds[0]"
        :options="q.options"
        :field-names="{ value: 'id', label: 'label' }"
        direction="vertical"
        @change="val => onSingleChange(q, val)" />

      <!-- 多选 -->
      <JnpfCheckbox
        v-else-if="q.type === 'multi'"
        v-model:value="answersMap[q.id].optionIds"
        :options="q.options"
        :field-names="{ value: 'id', label: 'label' }"
        direction="vertical"
        @change="list => onMultiChange(q, list)" />

      <!-- 纯文本 -->
      <a-textarea v-else v-model:value="answersMap[q.id].freeText" :rows="2" placeholder="请输入补充说明" class="question-freetext" />

      <!-- "其他"项联动文本框 -->
      <a-textarea
        v-if="showFreeTextInput(q)"
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
  import { computed, reactive, ref } from 'vue';
  import { message } from 'ant-design-vue';
  import {
    answerClarification,
    type ClarificationQuestion,
    type ClarificationSet,
    type ClarificationAnswer,
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

  // answersMap[questionId] = { optionIds: string[], freeText: string }
  const answersMap = reactive<Record<string, { optionIds: string[]; freeText: string }>>({});
  for (const q of props.set.questions) {
    answersMap[q.id] = { optionIds: q.type === 'multi' ? [] : [], freeText: '' };
  }

  const requiredCount = computed(() => props.set.questions.filter(q => q.required).length);
  const hasSkippable = computed(() => props.set.questions.some(q => !q.required));

  // 判断某题是否选中了 freeText=true 的"其他"项，需展开文本框
  function showFreeTextInput(q: ClarificationQuestion): boolean {
    if (q.type === 'text') return false; // text 题本身已是文本框，不重复展开
    const sel = answersMap[q.id].optionIds;
    return q.options.some(o => o.freeText && sel.includes(o.id));
  }

  function onSingleChange(q: ClarificationQuestion, val: string) {
    answersMap[q.id].optionIds = val ? [val] : [];
  }
  function onMultiChange(_q: ClarificationQuestion, list: string[]) {
    // JnpfCheckbox change 第二参数是已选 value 数组
    // 但实际它传的是 option 对象数组；做兼容处理
    const ids = (list as unknown[]).map(x => (typeof x === 'string' ? x : (x as { id?: string })?.id ?? ''));
    answersMap[_q.id].optionIds = ids.filter(Boolean);
  }

  const canSubmit = computed(() => {
    return props.set.questions
      .filter(q => q.required)
      .every(q => {
        const a = answersMap[q.id];
        if (q.type === 'text') return !!a.freeText?.trim();
        return a.optionIds.length > 0;
      });
  });

  function buildAnswers(): ClarificationAnswer[] {
    return props.set.questions.map(q => {
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
</style>
